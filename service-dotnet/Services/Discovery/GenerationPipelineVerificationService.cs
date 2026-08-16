using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationPipelineVerificationService
{
    private readonly PlanningOrchestrationService _planningOrchestrationService;
    private readonly RuntimeProviderAbstractionFrameworkService _runtimeProviderService;
    private readonly MicrosoftRuntimeProviderContractFrameworkService _microsoftRuntimeProviderService;
    private readonly PbirGenerationSpecificationService _pbirGenerationSpecificationService;
    private readonly GenerationProviderFrameworkService _generationProviderFrameworkService;
    private readonly GenerationProviderExecutionPlanningService _generationProviderExecutionPlanningService;
    private readonly GenerationManifestService _generationManifestService;
    private readonly RuntimeProviderRegistry _runtimeProviderRegistry;

    internal GenerationPipelineVerificationService()
    {
        _planningOrchestrationService = new PlanningOrchestrationService();
        _runtimeProviderRegistry = new RuntimeProviderRegistry();
        _runtimeProviderService = new RuntimeProviderAbstractionFrameworkService(_runtimeProviderRegistry);
        _microsoftRuntimeProviderService = new MicrosoftRuntimeProviderContractFrameworkService(_runtimeProviderRegistry);
        _pbirGenerationSpecificationService = new PbirGenerationSpecificationService();
        _generationProviderFrameworkService = new GenerationProviderFrameworkService();
        _generationProviderExecutionPlanningService = new GenerationProviderExecutionPlanningService();
        _generationManifestService = new GenerationManifestService();
    }

    internal GenerationPipelineVerificationState VerifyPipeline(DesignPackage package, DateTimeOffset createdUtc)
    {
        ArgumentNullException.ThrowIfNull(package);

        var planning = _planningOrchestrationService.Orchestrate(package);
        var specificationState = _pbirGenerationSpecificationService.PrepareForGenerationProvider(
            _pbirGenerationSpecificationService.CreateSpecification(planning));
        var providerState = _generationProviderFrameworkService.CreateProviderState(specificationState);
        var executionPlanningState = _generationProviderExecutionPlanningService.CreatePlanState(
            providerState.Request!,
            providerState.Provider!,
            specificationState,
            planning.Outcome);

        var runtimeRegistration = _runtimeProviderService.CreateDefaultRegistration(
            planning.ExecutionProviderState!.ProviderDefinition!,
            planning.ExecutionProviderState.ProviderRequest!);
        _runtimeProviderRegistry.Register(runtimeRegistration);
        var runtimeProviderState = _runtimeProviderService.CreateRuntimeCandidate(planning, runtimeRegistration.ProviderId);

        var microsoftRuntimeDefinition = _microsoftRuntimeProviderService.CreateDefaultProviderDefinition();
        _runtimeProviderRegistry.Register(_microsoftRuntimeProviderService.CreateDefaultRegistration(microsoftRuntimeDefinition, planning));
        var microsoftRuntimeState = _microsoftRuntimeProviderService.CreateMicrosoftRuntimeState(planning, microsoftRuntimeDefinition.ProviderId);

        var manifestState = _generationManifestService.CreateManifestState(
            planning,
            specificationState,
            providerState,
            executionPlanningState,
            runtimeProviderState,
            microsoftRuntimeState,
            createdUtc);

        return VerifyPipeline(
            planning,
            specificationState,
            providerState,
            executionPlanningState,
            runtimeProviderState,
            microsoftRuntimeState,
            manifestState);
    }

    internal GenerationPipelineVerificationState VerifyPipeline(
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        RuntimeProviderFrameworkState runtimeProviderState,
        MicrosoftRuntimeProviderFrameworkState microsoftRuntimeState,
        GenerationManifestState manifestState)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(specificationState);
        ArgumentNullException.ThrowIfNull(providerState);
        ArgumentNullException.ThrowIfNull(executionPlanningState);
        ArgumentNullException.ThrowIfNull(runtimeProviderState);
        ArgumentNullException.ThrowIfNull(microsoftRuntimeState);
        ArgumentNullException.ThrowIfNull(manifestState);

        var incompleteStages = new List<string>();
        var missingReferences = new List<string>();
        var invalidReadinessTransitions = new List<string>();
        var lineageFailures = new List<string>();
        var incompatibleProviders = new List<string>();
        var boundaryViolations = new List<string>();

        var stageResults = BuildStageResults(
            planning,
            specificationState,
            providerState,
            executionPlanningState,
            runtimeProviderState,
            microsoftRuntimeState,
            manifestState,
            incompleteStages);

        if (manifestState.Manifest is null)
        {
            incompleteStages.Add("generationManifest");
        }
        else
        {
            ValidateReferencePreservation(manifestState.Manifest, stageResults, missingReferences);
            ValidateReadinessTransitions(planning, providerState, executionPlanningState, runtimeProviderState, microsoftRuntimeState, manifestState, invalidReadinessTransitions);
            ValidateLineage(manifestState.Manifest, stageResults, lineageFailures);
            ValidateProviderCompatibility(providerState, microsoftRuntimeState, incompatibleProviders);
            ValidateBoundary(manifestState.Manifest, boundaryViolations);
        }

        var diagnostics = new GenerationPipelineVerificationDiagnostics(
            DistinctAndOrder(incompleteStages),
            DistinctAndOrder(missingReferences),
            DistinctAndOrder(invalidReadinessTransitions),
            DistinctAndOrder(lineageFailures),
            DistinctAndOrder(incompatibleProviders),
            DistinctAndOrder(boundaryViolations));

        if (diagnostics.HasFailures || manifestState.Manifest is null)
        {
            return new GenerationPipelineVerificationState(
                Verification: null,
                Diagnostics: diagnostics);
        }

        var verification = new GenerationPipelineVerification(
            SchemaVersion: GenerationPipelineVerificationContract.SchemaVersionV1,
            VerificationId: $"generationPipelineVerification:{planning.Outcome.References.DesignPackageRef}",
            ManifestRef: manifestState.Manifest.Metadata.ManifestId,
            StageResults: stageResults,
            PreservedReferences: manifestState.Manifest.Lineage.ImmutableUpstreamLineage,
            LineageReferenceIds: manifestState.Manifest.Lineage.UpstreamLineage
                .Select(entry => entry.ReferenceId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reference => reference, StringComparer.Ordinal)
                .ToArray());

        return new GenerationPipelineVerificationState(
            Verification: verification,
            Diagnostics: diagnostics);
    }

    private static IReadOnlyList<GenerationPipelineStageVerification> BuildStageResults(
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        RuntimeProviderFrameworkState runtimeProviderState,
        MicrosoftRuntimeProviderFrameworkState microsoftRuntimeState,
        GenerationManifestState manifestState,
        ICollection<string> incompleteStages)
    {
        var stages = new[]
        {
            CreateStage("designPackage", 1, planning.Outcome.References.DesignPackageRef, "completed", !string.IsNullOrWhiteSpace(planning.Outcome.References.DesignPackageRef), incompleteStages),
            CreateStage("generationRequest", 2, planning.GenerationRequestState.Request?.RequestId ?? string.Empty, planning.GenerationRequestState.Readiness.ToString(), planning.GenerationRequestState.Request is not null, incompleteStages),
            CreateStage("executionPlan", 3, planning.ExecutionPlanState.Plan?.ExecutionPlanId ?? string.Empty, planning.ExecutionPlanState.Readiness.ToString(), planning.ExecutionPlanState.Plan is not null, incompleteStages),
            CreateStage("planningOutcome", 4, planning.Outcome.Metadata.OutcomeId, planning.Outcome.ReadinessSummary.Status.ToString(), true, incompleteStages),
            CreateStage("runtimeProvider", 5, runtimeProviderState.Request?.RequestId ?? string.Empty, runtimeProviderState.Readiness.ToString(), runtimeProviderState.Request is not null, incompleteStages),
            CreateStage("microsoftRuntimeProvider", 6, microsoftRuntimeState.Request?.RequestId ?? string.Empty, microsoftRuntimeState.Readiness.ToString(), microsoftRuntimeState.Request is not null, incompleteStages),
            CreateStage("skillResolution", 7, microsoftRuntimeState.Context?.ContextId ?? string.Empty, microsoftRuntimeState.Context?.MicrosoftSkillSummary.Readiness.ToString() ?? "invalid", microsoftRuntimeState.Context is not null, incompleteStages),
            CreateStage("generationProvider", 8, providerState.Request?.Metadata.RequestId ?? string.Empty, providerState.Readiness.ToString(), providerState.Request is not null, incompleteStages),
            CreateStage("generationProviderExecutionPlan", 9, executionPlanningState.Plan?.Metadata.ExecutionPlanId ?? string.Empty, executionPlanningState.Readiness.ToString(), executionPlanningState.Plan is not null, incompleteStages),
            CreateStage("generationManifest", 10, manifestState.Manifest?.Metadata.ManifestId ?? string.Empty, manifestState.Readiness.ToString(), manifestState.Manifest is not null, incompleteStages),
        };

        return stages;
    }

    private static GenerationPipelineStageVerification CreateStage(
        string stageId,
        int sequence,
        string referenceId,
        string readiness,
        bool completed,
        ICollection<string> incompleteStages)
    {
        if (!completed)
        {
            incompleteStages.Add(stageId);
        }

        return new GenerationPipelineStageVerification(stageId, sequence, referenceId, readiness, completed);
    }

    private static void ValidateReferencePreservation(
        GenerationManifest manifest,
        IReadOnlyList<GenerationPipelineStageVerification> stageResults,
        ICollection<string> missingReferences)
    {
        var requiredStageRefs = stageResults
            .Where(stage => stage.StageId is "designPackage" or "generationRequest" or "executionPlan" or "planningOutcome" or "runtimeProvider" or "generationProvider" or "generationProviderExecutionPlan")
            .Select(stage => stage.ReferenceId)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .ToArray();

        foreach (var reference in requiredStageRefs)
        {
            if (!manifest.Lineage.ImmutableUpstreamLineage.Contains(reference, StringComparer.Ordinal))
            {
                missingReferences.Add(reference);
            }
        }

        if (string.IsNullOrWhiteSpace(manifest.SourceReferences.GenerationRequestRef))
        {
            missingReferences.Add("generationRequest");
        }
    }

    private static void ValidateReadinessTransitions(
        PlanningOrchestrationResult planning,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        RuntimeProviderFrameworkState runtimeProviderState,
        MicrosoftRuntimeProviderFrameworkState microsoftRuntimeState,
        GenerationManifestState manifestState,
        ICollection<string> invalidReadinessTransitions)
    {
        var manifest = manifestState.Manifest;

        if (planning.Outcome.ReadinessSummary.Status != PlanningReadinessStatus.ApprovedForExecutionProvider)
        {
            invalidReadinessTransitions.Add("planningOutcome");
        }

        if (runtimeProviderState.Readiness != RuntimeProviderReadinessState.ReadyForRuntimeProvider)
        {
            invalidReadinessTransitions.Add("runtimeProvider");
        }
        else if (manifest is not null && manifest.ReadinessSummary.RuntimeReadiness != runtimeProviderState.Readiness)
        {
            invalidReadinessTransitions.Add("runtimeProvider");
        }

        if (microsoftRuntimeState.Readiness != MicrosoftRuntimeReadinessState.ReadyForMicrosoftRuntimeProvider)
        {
            invalidReadinessTransitions.Add("microsoftRuntimeProvider");
        }

        if (providerState.Readiness != GenerationProviderReadinessState.ReadyForGenerationProvider)
        {
            invalidReadinessTransitions.Add("generationProvider");
        }
        else if (manifest is not null && manifest.ReadinessSummary.ProviderReadiness != providerState.Readiness)
        {
            invalidReadinessTransitions.Add("generationProvider");
        }

        if (executionPlanningState.Readiness != GenerationProviderExecutionPlanReadinessState.ReadyForExecutionProvider)
        {
            invalidReadinessTransitions.Add("generationProviderExecutionPlan");
        }
        else if (manifest is not null && manifest.ReadinessSummary.GenerationReadiness != executionPlanningState.Readiness)
        {
            invalidReadinessTransitions.Add("generationProviderExecutionPlan");
        }

        if (manifestState.Readiness != GenerationManifestReadinessState.ReadyForGenerator)
        {
            invalidReadinessTransitions.Add("generationManifest");
        }
    }

    private static void ValidateLineage(
        GenerationManifest manifest,
        IReadOnlyList<GenerationPipelineStageVerification> stageResults,
        ICollection<string> lineageFailures)
    {
        var stageReferenceIds = stageResults
            .Select(stage => stage.ReferenceId)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();
        var manifestReferenceIds = manifest.Lineage.UpstreamLineage
            .Select(entry => entry.ReferenceId)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        if (!stageReferenceIds.Intersect(manifestReferenceIds, StringComparer.Ordinal).Any())
        {
            lineageFailures.Add("stage lineage does not intersect manifest lineage.");
        }
    }

    private static void ValidateProviderCompatibility(
        GenerationProviderFrameworkState providerState,
        MicrosoftRuntimeProviderFrameworkState microsoftRuntimeState,
        ICollection<string> incompatibleProviders)
    {
        var providerCapabilities = providerState.Provider?.SupportedCapabilities ?? [];
        var requiredSkills = microsoftRuntimeState.Context?.MicrosoftSkillSummary.RequiredSkillIds ?? [];

        if (!providerCapabilities.Contains("pageGeneration", StringComparer.Ordinal))
        {
            incompatibleProviders.Add("generationProvider");
        }

        if (requiredSkills.Count == 0)
        {
            incompatibleProviders.Add("skillResolution");
        }
    }

    private static void ValidateBoundary(GenerationManifest manifest, ICollection<string> boundaryViolations)
    {
        if (!manifest.ExecutionConstraints.DryRunOnly ||
            manifest.ExecutionConstraints.DeploymentAllowed ||
            manifest.ExecutionConstraints.ProviderInvocationAllowed ||
            manifest.ExecutionConstraints.ApiInvocationAllowed ||
            manifest.ExecutionConstraints.CliInvocationAllowed)
        {
            boundaryViolations.Add("generationManifest");
        }
    }

    private static string[] DistinctAndOrder(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }
}
