using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationManifestService
{
    private readonly GenerationManifestValidator _validator;
    private readonly GenerationManifestReadinessService _readinessService;

    internal GenerationManifestService()
        : this(new GenerationManifestValidator(), new GenerationManifestReadinessService())
    {
    }

    internal GenerationManifestService(
        GenerationManifestValidator validator,
        GenerationManifestReadinessService readinessService)
    {
        _validator = validator;
        _readinessService = readinessService;
    }

    internal GenerationManifestState CreateManifestState(
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        RuntimeProviderFrameworkState runtimeProviderState,
        MicrosoftRuntimeProviderFrameworkState microsoftRuntimeState,
        DateTimeOffset createdUtc)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(specificationState);
        ArgumentNullException.ThrowIfNull(providerState);
        ArgumentNullException.ThrowIfNull(executionPlanningState);
        ArgumentNullException.ThrowIfNull(runtimeProviderState);
        ArgumentNullException.ThrowIfNull(microsoftRuntimeState);

        if (specificationState.Specification is null ||
            providerState.Provider is null ||
            providerState.Request is null ||
            executionPlanningState.Plan is null ||
            runtimeProviderState.Request is null ||
            microsoftRuntimeState.Definition is null ||
            microsoftRuntimeState.Context is null)
        {
            var validation = new GenerationManifestValidationResult(
                new GenerationManifestValidationDiagnostics(
                    MissingRequiredSections:
                    new[]
                    {
                        specificationState.Specification is null ? "sourceReferences.pbirGenerationSpecificationRef" : string.Empty,
                        providerState.Provider is null ? "capabilitySummary.selectedGenerationProvider" : string.Empty,
                        providerState.Request is null ? "sourceReferences.generationProviderRequestRef" : string.Empty,
                        executionPlanningState.Plan is null ? "sourceReferences.generationProviderExecutionPlanRef" : string.Empty,
                        runtimeProviderState.Request is null ? "sourceReferences.runtimeProviderRef" : string.Empty,
                        microsoftRuntimeState.Definition is null ? "capabilitySummary.selectedMicrosoftRuntimeProvider" : string.Empty,
                        microsoftRuntimeState.Context is null ? "capabilitySummary.selectedSkills" : string.Empty,
                    }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray(),
                    MissingRequiredFields: [],
                    InvalidReferences: [],
                    UnsupportedSchemaVersions: [],
                    LineageIntegrityFailures: [],
                    ReadinessConsistencyFailures: [],
                    ProviderCompatibilityFailures: [],
                    GenerationSpecificationCompletenessFailures: [],
                    BoundaryViolations: []));

            return new GenerationManifestState(
                Manifest: null,
                Validation: validation,
                Readiness: _readinessService.Evaluate(validation));
        }

        var manifest = CreateManifest(
            planning,
            specificationState.Specification,
            providerState,
            executionPlanningState,
            runtimeProviderState,
            microsoftRuntimeState,
            createdUtc);
        var manifestValidation = _validator.Validate(
            manifest,
            planning,
            specificationState,
            providerState,
            executionPlanningState,
            runtimeProviderState,
            microsoftRuntimeState);
        var readiness = _readinessService.Evaluate(manifestValidation);

        return new GenerationManifestState(
            Manifest: manifest,
            Validation: manifestValidation,
            Readiness: readiness);
    }

    private static GenerationManifest CreateManifest(
        PlanningOrchestrationResult planning,
        PbirGenerationSpecification specification,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        RuntimeProviderFrameworkState runtimeProviderState,
        MicrosoftRuntimeProviderFrameworkState microsoftRuntimeState,
        DateTimeOffset createdUtc)
    {
        var provider = providerState.Provider!;
        var providerRequest = providerState.Request!;
        var executionPlan = executionPlanningState.Plan!;
        var runtimeProviderRequest = runtimeProviderState.Request!;
        var runtimeDefinition = microsoftRuntimeState.Definition!;
        var runtimeContext = microsoftRuntimeState.Context!;
        var immutableReferences = BuildImmutableReferences(
            planning.Outcome,
            specification.SpecificationId,
            providerRequest.Metadata.RequestId,
            executionPlan.Metadata.ExecutionPlanId,
            runtimeProviderRequest.RequestId);

        return new GenerationManifest(
            Metadata: new GenerationManifestMetadata(
                ManifestId: $"generationManifest:{planning.Outcome.Metadata.OutcomeId}",
                SchemaVersion: GenerationManifestContract.SchemaVersionV1,
                CreatedUtc: createdUtc.UtcDateTime),
            SourceReferences: new GenerationManifestSourceReferences(
                DesignPackageRef: planning.Outcome.References.DesignPackageRef,
                GenerationRequestRef: planning.Outcome.References.GenerationRequestRef,
                ExecutionPlanRef: planning.Outcome.References.ExecutionPlanRef,
                PlanningOutcomeRef: planning.Outcome.Metadata.OutcomeId,
                RuntimeProviderRef: runtimeProviderRequest.RequestId,
                GenerationProviderRequestRef: providerRequest.Metadata.RequestId,
                GenerationProviderExecutionPlanRef: executionPlan.Metadata.ExecutionPlanId,
                PbirGenerationSpecificationRef: specification.SpecificationId),
            CapabilitySummary: new GenerationManifestCapabilitySummary(
                NegotiatedCapabilities: PreserveOrder(planning.Outcome.ReadinessSummary.CapabilitySummary.RequiredCapabilities),
                SelectedGenerationProvider: new GenerationManifestProviderSummary(
                    ProviderId: provider.ProviderId,
                    ProviderName: provider.ProviderName,
                    ProviderVersion: provider.ProviderVersion),
                SelectedMicrosoftRuntimeProvider: new GenerationManifestMicrosoftRuntimeProviderSummary(
                    ProviderId: runtimeDefinition.ProviderId,
                    ProviderName: runtimeDefinition.ProviderName,
                    ProviderVersion: runtimeDefinition.ProviderVersion,
                    ProviderCategory: runtimeDefinition.ProviderCategory),
                SelectedSkills: PreserveOrder(runtimeContext.MicrosoftSkillSummary.RequiredSkillIds),
                SelectedProviderCandidates: PreserveOrder(runtimeContext.MicrosoftSkillSummary.CandidateProviderIds)),
            ExecutionConstraints: new GenerationManifestExecutionConstraints(
                DryRunOnly: true,
                DeploymentAllowed: false,
                ProviderInvocationAllowed: false,
                ApiInvocationAllowed: false,
                CliInvocationAllowed: false),
            ReadinessSummary: new GenerationManifestReadinessSummary(
                PlanningReadiness: planning.Outcome.ReadinessSummary.Status,
                RuntimeReadiness: runtimeProviderState.Readiness,
                ProviderReadiness: providerState.Readiness,
                GenerationReadiness: executionPlanningState.Readiness),
            ApprovalSummary: new GenerationManifestApprovalSummary(
                DesignApproval: planning.Outcome.ReadinessSummary.ApprovalStatus,
                PlanningApproval: new GenerationManifestPlanningApprovalSummary(
                    OutcomeStatus: planning.Outcome.Status,
                    PlanningReadiness: planning.Outcome.ReadinessSummary.Status,
                    ExecutionProviderReadiness: planning.Outcome.ReadinessSummary.ExecutionProviderReadiness),
                RuntimeApproval: new GenerationManifestRuntimeApprovalSummary(
                    RuntimeProviderId: runtimeDefinition.ProviderId,
                    RuntimeReadiness: microsoftRuntimeState.Readiness,
                    AcceptsExecutionCandidate: microsoftRuntimeState.AcceptsExecutionCandidate),
                ProviderApproval: new GenerationManifestProviderApprovalSummary(
                    ProviderId: provider.ProviderId,
                    ProviderReadiness: providerState.Readiness,
                    ProviderApproved: providerState.Readiness == GenerationProviderReadinessState.ReadyForGenerationProvider)),
            Lineage: new GenerationManifestLineage(
                UpstreamLineage: BuildLineageEntries(
                    planning.Outcome,
                    specification.SpecificationId,
                    providerRequest.Metadata.RequestId,
                    executionPlan.Metadata.ExecutionPlanId,
                    runtimeProviderRequest.RequestId),
                ImmutableUpstreamLineage: immutableReferences));
    }

    private static PlanningLineageEntry[] BuildLineageEntries(
        PlanningOutcome outcome,
        string specificationRef,
        string generationProviderRequestRef,
        string generationProviderExecutionPlanRef,
        string runtimeProviderRef)
    {
        return outcome.Lineage.UpstreamLineage
            .Concat(outcome.Lineage.PlanningLineage)
            .Concat(
            [
                new PlanningLineageEntry("generationProviderExecutionPlan", generationProviderExecutionPlanRef, "Generation provider execution plan"),
                new PlanningLineageEntry("generationProviderRequest", generationProviderRequestRef, "Generation provider request"),
                new PlanningLineageEntry("pbirGenerationSpecification", specificationRef, "PBIR generation specification"),
                new PlanningLineageEntry("runtimeProvider", runtimeProviderRef, "Runtime provider request"),
            ])
            .Distinct()
            .OrderBy(entry => entry.Stage, StringComparer.Ordinal)
            .ThenBy(entry => entry.ReferenceId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Label, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] BuildImmutableReferences(
        PlanningOutcome outcome,
        string specificationRef,
        string generationProviderRequestRef,
        string generationProviderExecutionPlanRef,
        string runtimeProviderRef)
    {
        return new[]
        {
            outcome.References.DesignPackageRef,
            outcome.References.GenerationRequestRef,
            outcome.References.ExecutionPlanRef,
            outcome.Metadata.OutcomeId,
            specificationRef,
            generationProviderRequestRef,
            generationProviderExecutionPlanRef,
            runtimeProviderRef
        }
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] PreserveOrder(IReadOnlyList<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
