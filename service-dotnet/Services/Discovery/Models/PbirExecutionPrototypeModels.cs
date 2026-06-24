using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirExecutionPrototypeContract
{
    internal const string SchemaVersionV1 = "pbir-execution-prototype/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "SafetyGate",
        "SafetyGate.IsAllowed",
        "SafetyGate.TargetProfileId",
        "SafetyGate.RuntimeReadiness",
        "SafetyGate.ExecutionMode",
        "SafetyGate.DryRun",
        "SafetyGate.Reasons",
        "AcceptsExecutionPrototype",
        "Request",
        "Request.SchemaVersion",
        "Request.RequestId",
        "Request.ExecutionMode",
        "Request.DryRun",
        "DryRunSummary",
        "DryRunSummary.SummaryKind",
        "DryRunSummary.PlannedPages",
        "DryRunSummary.PlannedVisuals",
        "DryRunSummary.PlannedSemanticBindings",
        "MockResult",
    ];
}

internal static class PbirExecutionRequestContract
{
    internal const string SchemaVersionV1 = "pbir-execution-request/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "RequestId",
        "RequestMetadata",
        "RequestMetadata.PlanningOutcomeSchemaVersion",
        "RequestMetadata.MicrosoftRuntimeRequestSchemaVersion",
        "RequestMetadata.MicrosoftRuntimeContextSchemaVersion",
        "RequestMetadata.ExecutionCandidateSchemaVersion",
        "PlanningOutcomeReference",
        "PlanningOutcomeReference.OutcomeId",
        "PlanningOutcomeReference.SchemaVersion",
        "ExecutionCandidateReference",
        "ExecutionCandidateReference.CandidateId",
        "ExecutionCandidateReference.SchemaVersion",
        "ExecutionCandidateReference.RuntimeRequestRef",
        "MicrosoftRuntimeContextReference",
        "MicrosoftRuntimeContextReference.ProviderId",
        "MicrosoftRuntimeContextReference.ProviderCategory",
        "MicrosoftRuntimeContextReference.RuntimeRequestId",
        "MicrosoftRuntimeContextReference.RuntimeContextId",
        "MicrosoftRuntimeContextReference.RuntimeReadiness",
        "SelectedSkillProviderMetadata",
        "SelectedSkillProviderMetadata.RequiredSkillIds",
        "SelectedSkillProviderMetadata.OptionalSkillIds",
        "SelectedSkillProviderMetadata.CandidateProviderIds",
        "SelectedSkillProviderMetadata.SelectedProviderIds",
        "TargetProfile",
        "TargetProfile.TargetProfileId",
        "TargetProfile.ArtifactType",
        "PbirConstraints",
        "PbirConstraints.AllowedArtifactTypes",
        "PbirConstraints.ProhibitLiveExecution",
        "PbirConstraints.ProhibitDeployment",
        "PbirConstraints.RequireDryRunByDefault",
        "PbirConstraints.AllowFixtureArtifactRefsOnly",
        "ApprovalState",
        "ApprovalState.DesignApprovalRequired",
        "ApprovalState.GenerationApprovalRequired",
        "ApprovalState.AnalyzerValidationRequired",
        "ApprovalState.DesignApproved",
        "ApprovalState.GenerationApproved",
        "ExecutionMode",
        "DryRun",
    ];
}

internal static class PbirMockExecutionResultContract
{
    internal const string SchemaVersionV1 = "pbir-mock-execution-result/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ResultMetadata",
        "ResultMetadata.ResultId",
        "ResultMetadata.MockFixtureId",
        "RequestReference",
        "RequestReference.RequestId",
        "RequestReference.SchemaVersion",
        "ExecutionMode",
        "PlannedPages",
        "PlannedVisuals",
        "PlannedSemanticBindings",
        "Constraints",
        "Warnings",
        "GeneratedArtifactRefs",
    ];
}

internal enum PbirExecutionMode
{
    DryRun,
    MockedExecution,
}

internal sealed record PbirExecutionPrototypeOptions(
    PbirExecutionMode ExecutionMode,
    bool DryRun,
    bool AllowLiveProviderInvocation,
    bool AllowDeployment,
    string? MockFixtureId,
    IReadOnlyList<string> MockOutputPaths)
{
    internal static PbirExecutionPrototypeOptions DryRunDefault { get; } =
        new(
            ExecutionMode: PbirExecutionMode.DryRun,
            DryRun: true,
            AllowLiveProviderInvocation: false,
            AllowDeployment: false,
            MockFixtureId: null,
            MockOutputPaths: []);
}

internal sealed record PbirExecutionSafetyGateResult(
    [property: JsonPropertyName("isAllowed")] bool IsAllowed,
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("runtimeReadiness")] MicrosoftRuntimeReadinessState RuntimeReadiness,
    [property: JsonPropertyName("executionMode")] PbirExecutionMode ExecutionMode,
    [property: JsonPropertyName("dryRun")] bool DryRun,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record PbirExecutionRequestMetadata(
    [property: JsonPropertyName("planningOutcomeSchemaVersion")] string PlanningOutcomeSchemaVersion,
    [property: JsonPropertyName("microsoftRuntimeRequestSchemaVersion")] string MicrosoftRuntimeRequestSchemaVersion,
    [property: JsonPropertyName("microsoftRuntimeContextSchemaVersion")] string MicrosoftRuntimeContextSchemaVersion,
    [property: JsonPropertyName("executionCandidateSchemaVersion")] string ExecutionCandidateSchemaVersion);

internal sealed record PbirExecutionPlanningOutcomeReference(
    [property: JsonPropertyName("outcomeId")] string OutcomeId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion);

internal sealed record PbirExecutionCandidateReference(
    [property: JsonPropertyName("candidateId")] string CandidateId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("runtimeRequestRef")] string RuntimeRequestRef);

internal sealed record PbirExecutionMicrosoftRuntimeContextReference(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory,
    [property: JsonPropertyName("runtimeRequestId")] string RuntimeRequestId,
    [property: JsonPropertyName("runtimeContextId")] string RuntimeContextId,
    [property: JsonPropertyName("runtimeReadiness")] MicrosoftRuntimeReadinessState RuntimeReadiness);

internal sealed record PbirExecutionSelectedSkillProviderMetadata(
    [property: JsonPropertyName("requiredSkillIds")] IReadOnlyList<string> RequiredSkillIds,
    [property: JsonPropertyName("optionalSkillIds")] IReadOnlyList<string> OptionalSkillIds,
    [property: JsonPropertyName("candidateProviderIds")] IReadOnlyList<string> CandidateProviderIds,
    [property: JsonPropertyName("selectedProviderIds")] IReadOnlyList<string> SelectedProviderIds);

internal sealed record PbirExecutionTargetProfile(
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("artifactType")] string ArtifactType);

internal sealed record PbirExecutionConstraints(
    [property: JsonPropertyName("allowedArtifactTypes")] IReadOnlyList<string> AllowedArtifactTypes,
    [property: JsonPropertyName("prohibitLiveExecution")] bool ProhibitLiveExecution,
    [property: JsonPropertyName("prohibitDeployment")] bool ProhibitDeployment,
    [property: JsonPropertyName("requireDryRunByDefault")] bool RequireDryRunByDefault,
    [property: JsonPropertyName("allowFixtureArtifactRefsOnly")] bool AllowFixtureArtifactRefsOnly);

internal sealed record PbirExecutionApprovalState(
    [property: JsonPropertyName("designApprovalRequired")] bool DesignApprovalRequired,
    [property: JsonPropertyName("generationApprovalRequired")] bool GenerationApprovalRequired,
    [property: JsonPropertyName("analyzerValidationRequired")] bool AnalyzerValidationRequired,
    [property: JsonPropertyName("designApproved")] bool DesignApproved,
    [property: JsonPropertyName("generationApproved")] bool GenerationApproved);

internal sealed record PbirExecutionRequestEnvelope(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("requestMetadata")] PbirExecutionRequestMetadata RequestMetadata,
    [property: JsonPropertyName("planningOutcomeReference")] PbirExecutionPlanningOutcomeReference PlanningOutcomeReference,
    [property: JsonPropertyName("executionCandidateReference")] PbirExecutionCandidateReference ExecutionCandidateReference,
    [property: JsonPropertyName("microsoftRuntimeContextReference")] PbirExecutionMicrosoftRuntimeContextReference MicrosoftRuntimeContextReference,
    [property: JsonPropertyName("selectedSkillProviderMetadata")] PbirExecutionSelectedSkillProviderMetadata SelectedSkillProviderMetadata,
    [property: JsonPropertyName("targetProfile")] PbirExecutionTargetProfile TargetProfile,
    [property: JsonPropertyName("pbirConstraints")] PbirExecutionConstraints PbirConstraints,
    [property: JsonPropertyName("approvalState")] PbirExecutionApprovalState ApprovalState,
    [property: JsonPropertyName("executionMode")] PbirExecutionMode ExecutionMode,
    [property: JsonPropertyName("dryRun")] bool DryRun);

internal sealed record PbirExecutionDryRunSummary(
    [property: JsonPropertyName("summaryKind")] string SummaryKind,
    [property: JsonPropertyName("plannedPages")] IReadOnlyList<string> PlannedPages,
    [property: JsonPropertyName("plannedVisuals")] IReadOnlyList<string> PlannedVisuals,
    [property: JsonPropertyName("plannedSemanticBindings")] IReadOnlyList<string> PlannedSemanticBindings,
    [property: JsonPropertyName("constraints")] IReadOnlyList<string> Constraints,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings);

internal sealed record PbirMockExecutionResultMetadata(
    [property: JsonPropertyName("resultId")] string ResultId,
    [property: JsonPropertyName("mockFixtureId")] string MockFixtureId);

internal sealed record PbirMockExecutionRequestReference(
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion);

internal sealed record PbirMockExecutionResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("resultMetadata")] PbirMockExecutionResultMetadata ResultMetadata,
    [property: JsonPropertyName("requestReference")] PbirMockExecutionRequestReference RequestReference,
    [property: JsonPropertyName("executionMode")] PbirExecutionMode ExecutionMode,
    [property: JsonPropertyName("plannedPages")] IReadOnlyList<string> PlannedPages,
    [property: JsonPropertyName("plannedVisuals")] IReadOnlyList<string> PlannedVisuals,
    [property: JsonPropertyName("plannedSemanticBindings")] IReadOnlyList<string> PlannedSemanticBindings,
    [property: JsonPropertyName("constraints")] IReadOnlyList<string> Constraints,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("generatedArtifactRefs")] IReadOnlyList<string> GeneratedArtifactRefs);

internal sealed record PbirExecutionPrototypeState(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("safetyGate")] PbirExecutionSafetyGateResult SafetyGate,
    [property: JsonPropertyName("request")] PbirExecutionRequestEnvelope? Request,
    [property: JsonPropertyName("dryRunSummary")] PbirExecutionDryRunSummary? DryRunSummary,
    [property: JsonPropertyName("mockResult")] PbirMockExecutionResult? MockResult,
    [property: JsonPropertyName("acceptsExecutionPrototype")] bool AcceptsExecutionPrototype);
