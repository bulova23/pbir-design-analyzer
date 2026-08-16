using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirExecutionPrototypeBoundaryService
{
    private readonly PbirExecutionSafetyGate _safetyGate;

    internal PbirExecutionPrototypeBoundaryService()
        : this(new PbirExecutionSafetyGate())
    {
    }

    internal PbirExecutionPrototypeBoundaryService(PbirExecutionSafetyGate safetyGate)
    {
        _safetyGate = safetyGate;
    }

    internal PbirExecutionPrototypeState CreatePrototypeBoundary(
        PlanningOrchestrationResult planning,
        MicrosoftRuntimeProviderFrameworkState runtime,
        PbirExecutionPrototypeOptions options)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(options);

        var gate = _safetyGate.Validate(planning, runtime, options);
        if (!gate.IsAllowed)
        {
            return new PbirExecutionPrototypeState(
                SchemaVersion: PbirExecutionPrototypeContract.SchemaVersionV1,
                SafetyGate: gate,
                Request: null,
                DryRunSummary: null,
                MockResult: null,
                AcceptsExecutionPrototype: false);
        }

        var request = CreateRequestEnvelope(planning, runtime, options);
        var dryRunSummary = CreateDryRunSummary(planning, request);
        var mockResult = options.ExecutionMode == PbirExecutionMode.MockedExecution
            ? CreateMockResult(request, dryRunSummary, options)
            : null;

        return new PbirExecutionPrototypeState(
            SchemaVersion: PbirExecutionPrototypeContract.SchemaVersionV1,
            SafetyGate: gate,
            Request: request,
            DryRunSummary: dryRunSummary,
            MockResult: mockResult,
            AcceptsExecutionPrototype: true);
    }

    private static PbirExecutionRequestEnvelope CreateRequestEnvelope(
        PlanningOrchestrationResult planning,
        MicrosoftRuntimeProviderFrameworkState runtime,
        PbirExecutionPrototypeOptions options)
    {
        var request = runtime.Request!;
        var context = runtime.Context!;
        var selectedProviderIds = planning.MicrosoftSkillProviderState?.Selection?.SelectedProviderCandidates
            .Select(candidate => candidate.ProviderId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(providerId => providerId, StringComparer.Ordinal)
            .ToArray() ?? [];

        return new PbirExecutionRequestEnvelope(
            SchemaVersion: PbirExecutionRequestContract.SchemaVersionV1,
            RequestId: $"pbirExecutionRequest:{planning.Outcome.Metadata.OutcomeId}",
            RequestMetadata: new PbirExecutionRequestMetadata(
                PlanningOutcomeSchemaVersion: planning.Outcome.Metadata.SchemaVersion,
                MicrosoftRuntimeRequestSchemaVersion: request.SchemaVersion,
                MicrosoftRuntimeContextSchemaVersion: context.SchemaVersion,
                ExecutionCandidateSchemaVersion: request.ExecutionCandidateReference.SchemaVersion),
            PlanningOutcomeReference: new PbirExecutionPlanningOutcomeReference(
                OutcomeId: planning.Outcome.Metadata.OutcomeId,
                SchemaVersion: planning.Outcome.Metadata.SchemaVersion),
            ExecutionCandidateReference: new PbirExecutionCandidateReference(
                CandidateId: request.ExecutionCandidateReference.CandidateId,
                SchemaVersion: request.ExecutionCandidateReference.SchemaVersion,
                RuntimeRequestRef: request.ExecutionCandidateReference.RuntimeRequestRef),
            MicrosoftRuntimeContextReference: new PbirExecutionMicrosoftRuntimeContextReference(
                ProviderId: runtime.Definition!.ProviderId,
                ProviderCategory: runtime.Definition.ProviderCategory,
                RuntimeRequestId: request.RequestId,
                RuntimeContextId: context.ContextId,
                RuntimeReadiness: runtime.Readiness),
            SelectedSkillProviderMetadata: new PbirExecutionSelectedSkillProviderMetadata(
                RequiredSkillIds: request.SkillRequirements.RequiredSkillIds,
                OptionalSkillIds: request.SkillRequirements.OptionalSkillIds,
                CandidateProviderIds: request.SkillRequirements.CandidateProviderIds,
                SelectedProviderIds: selectedProviderIds),
            TargetProfile: new PbirExecutionTargetProfile(
                TargetProfileId: request.TargetProfile.TargetProfileId,
                ArtifactType: request.TargetProfile.ArtifactType),
            PbirConstraints: new PbirExecutionConstraints(
                AllowedArtifactTypes: [request.TargetProfile.ArtifactType],
                ProhibitLiveExecution: true,
                ProhibitDeployment: true,
                RequireDryRunByDefault: true,
                AllowFixtureArtifactRefsOnly: true),
            ApprovalState: new PbirExecutionApprovalState(
                DesignApprovalRequired: request.ReviewRequirements.DesignApprovalRequired,
                GenerationApprovalRequired: request.ReviewRequirements.GenerationApprovalRequired,
                AnalyzerValidationRequired: request.ReviewRequirements.AnalyzerValidationRequired,
                DesignApproved: request.ReviewRequirements.DesignApproved,
                GenerationApproved: request.ReviewRequirements.GenerationApproved),
            ExecutionMode: options.ExecutionMode,
            DryRun: options.DryRun);
    }

    private static PbirExecutionDryRunSummary CreateDryRunSummary(
        PlanningOrchestrationResult planning,
        PbirExecutionRequestEnvelope request)
    {
        var generationRequest = planning.GenerationRequestState.Request!;
        var plannedPages = generationRequest.StructuralIntent.Pages
            .Select(page => page.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(page => page, StringComparer.Ordinal)
            .ToArray();
        var plannedVisuals = generationRequest.StructuralIntent.VisualHints
            .Select(visual => $"{visual.PageName}:{visual.VisualType}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(visual => visual, StringComparer.Ordinal)
            .ToArray();
        var plannedSemanticBindings = new[]
            {
                generationRequest.DataIntent.SemanticBinding.SemanticModelRef,
                generationRequest.DataIntent.SemanticBinding.SemanticModelLabel
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var constraints = new[]
        {
            $"targetProfile:{request.TargetProfile.TargetProfileId}",
            "dryRunOnlyByDefault",
            "noLiveProviderInvocation",
            "noDeployment",
            "noRealArtifactGeneration",
        };
        var warnings = new List<string>
        {
            "PBIR execution prototype boundary remains advisory-only.",
            "Generated artifact refs remain empty unless explicit mock fixture output paths are supplied.",
        };

        if (request.ExecutionMode == PbirExecutionMode.MockedExecution)
        {
            warnings.Add("Mocked execution uses deterministic fixtures only.");
        }

        return new PbirExecutionDryRunSummary(
            SummaryKind: "dryRun",
            PlannedPages: plannedPages,
            PlannedVisuals: plannedVisuals,
            PlannedSemanticBindings: plannedSemanticBindings,
            Constraints: constraints,
            Warnings: warnings.ToArray());
    }

    private static PbirMockExecutionResult CreateMockResult(
        PbirExecutionRequestEnvelope request,
        PbirExecutionDryRunSummary dryRunSummary,
        PbirExecutionPrototypeOptions options)
    {
        return new PbirMockExecutionResult(
            SchemaVersion: PbirMockExecutionResultContract.SchemaVersionV1,
            ResultMetadata: new PbirMockExecutionResultMetadata(
                ResultId: $"pbirMockExecutionResult:{request.RequestId}",
                MockFixtureId: options.MockFixtureId!),
            RequestReference: new PbirMockExecutionRequestReference(
                RequestId: request.RequestId,
                SchemaVersion: request.SchemaVersion),
            ExecutionMode: PbirExecutionMode.MockedExecution,
            PlannedPages: dryRunSummary.PlannedPages,
            PlannedVisuals: dryRunSummary.PlannedVisuals,
            PlannedSemanticBindings: dryRunSummary.PlannedSemanticBindings,
            Constraints: dryRunSummary.Constraints,
            Warnings: dryRunSummary.Warnings,
            GeneratedArtifactRefs: options.MockOutputPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray());
    }
}
