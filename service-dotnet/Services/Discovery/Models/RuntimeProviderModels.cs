using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class RuntimeProviderContract
{
    internal const string SchemaVersionV1 = "runtime-provider/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "CandidateId",
        "RequestRef",
        "ContextRef",
        "ResultRef",
        "ReadinessStatus",
    ];
}

internal static class RuntimeProviderRequestContract
{
    internal const string SchemaVersionV1 = "runtime-provider-request/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "RequestId",
        "PlanningOutcomeRef",
        "ExecutionProviderRef",
        "ExecutionPlanRef",
        "CapabilityResolutionRef",
        "SourceContractVersions",
        "SourceContractVersions.PlanningOutcomeSchemaVersion",
        "SourceContractVersions.ExecutionProviderSchemaVersion",
        "SourceContractVersions.ExecutionPlanSchemaVersion",
        "SourceContractVersions.CapabilityResolutionSchemaVersion",
        "ApprovalState",
        "ApprovalState.DesignApprovalRequired",
        "ApprovalState.GenerationApprovalRequired",
        "ApprovalState.AnalyzerValidationRequired",
        "ApprovalState.DesignApproved",
        "ApprovalState.GenerationApproved",
        "ExecutionConstraints",
        "ExecutionConstraints.RequiredCapabilities",
        "ExecutionConstraints.UnresolvedCapabilities",
        "ExecutionConstraints.RequiredTargetProfileId",
        "ExecutionConstraints.RequiredProviderCategory",
    ];
}

internal static class RuntimeProviderContextContract
{
    internal const string SchemaVersionV1 = "runtime-provider-context/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ContextId",
        "ExecutionLineage",
        "ExecutionLineage.RequestRef",
        "ExecutionLineage.PlanningOutcomeRef",
        "ExecutionLineage.ExecutionProviderRef",
        "ExecutionLineage.ExecutionPlanRef",
        "ExecutionLineage.CapabilityResolutionRef",
        "PlanningLineage",
        "PlanningLineage.UpstreamLineage",
        "PlanningLineage.PlanningLineage",
        "ApprovalLineage",
        "ApprovalLineage.DesignApprovalRequired",
        "ApprovalLineage.GenerationApprovalRequired",
        "ApprovalLineage.AnalyzerValidationRequired",
        "ApprovalLineage.DesignApproved",
        "ApprovalLineage.GenerationApproved",
        "TargetProfileId",
        "ProviderCategory",
    ];
}

internal static class RuntimeProviderResultContract
{
    internal const string SchemaVersionV1 = "runtime-provider-result/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ResultId",
        "RequestId",
        "Status",
        "ReadinessStatus",
        "Reasons",
    ];
}

internal enum RuntimeProviderReadinessState
{
    Invalid,
    Blocked,
    Unsupported,
    Candidate,
    ReadyForRuntimeProvider,
}

internal enum RuntimeProviderResultStatus
{
    Accepted,
    Rejected,
    Unsupported,
    Blocked,
    ValidationFailed,
}

internal sealed record RuntimeProviderSourceContractVersions(
    [property: JsonPropertyName("planningOutcomeSchemaVersion")] string PlanningOutcomeSchemaVersion,
    [property: JsonPropertyName("executionProviderSchemaVersion")] string ExecutionProviderSchemaVersion,
    [property: JsonPropertyName("executionPlanSchemaVersion")] string ExecutionPlanSchemaVersion,
    [property: JsonPropertyName("capabilityResolutionSchemaVersion")] string CapabilityResolutionSchemaVersion);

internal sealed record RuntimeProviderApprovalState(
    [property: JsonPropertyName("designApprovalRequired")] bool DesignApprovalRequired,
    [property: JsonPropertyName("generationApprovalRequired")] bool GenerationApprovalRequired,
    [property: JsonPropertyName("analyzerValidationRequired")] bool AnalyzerValidationRequired,
    [property: JsonPropertyName("designApproved")] bool DesignApproved,
    [property: JsonPropertyName("generationApproved")] bool GenerationApproved);

internal sealed record RuntimeProviderExecutionConstraints(
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<string> RequiredCapabilities,
    [property: JsonPropertyName("unresolvedCapabilities")] IReadOnlyList<string> UnresolvedCapabilities,
    [property: JsonPropertyName("requiredTargetProfileId")] string RequiredTargetProfileId,
    [property: JsonPropertyName("requiredProviderCategory")] string RequiredProviderCategory);

internal sealed record RuntimeProviderRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("planningOutcomeRef")] string PlanningOutcomeRef,
    [property: JsonPropertyName("executionProviderRef")] string ExecutionProviderRef,
    [property: JsonPropertyName("executionPlanRef")] string ExecutionPlanRef,
    [property: JsonPropertyName("capabilityResolutionRef")] string CapabilityResolutionRef,
    [property: JsonPropertyName("sourceContractVersions")] RuntimeProviderSourceContractVersions SourceContractVersions,
    [property: JsonPropertyName("approvalState")] RuntimeProviderApprovalState ApprovalState,
    [property: JsonPropertyName("executionConstraints")] RuntimeProviderExecutionConstraints ExecutionConstraints);

internal sealed record RuntimeExecutionLineage(
    [property: JsonPropertyName("requestRef")] string RequestRef,
    [property: JsonPropertyName("planningOutcomeRef")] string PlanningOutcomeRef,
    [property: JsonPropertyName("executionProviderRef")] string ExecutionProviderRef,
    [property: JsonPropertyName("executionPlanRef")] string ExecutionPlanRef,
    [property: JsonPropertyName("capabilityResolutionRef")] string CapabilityResolutionRef);

internal sealed record RuntimeProviderContextLineage(
    [property: JsonPropertyName("upstreamLineage")] IReadOnlyList<PlanningLineageEntry> UpstreamLineage,
    [property: JsonPropertyName("planningLineage")] IReadOnlyList<PlanningLineageEntry> PlanningLineage);

internal sealed record RuntimeProviderContext(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("contextId")] string ContextId,
    [property: JsonPropertyName("executionLineage")] RuntimeExecutionLineage ExecutionLineage,
    [property: JsonPropertyName("planningLineage")] RuntimeProviderContextLineage PlanningLineage,
    [property: JsonPropertyName("approvalLineage")] RuntimeProviderApprovalState ApprovalLineage,
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory);

internal sealed record RuntimeProviderResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("resultId")] string ResultId,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("status")] RuntimeProviderResultStatus Status,
    [property: JsonPropertyName("readinessStatus")] RuntimeProviderReadinessState ReadinessStatus,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record RuntimeExecutionCandidate(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("candidateId")] string CandidateId,
    [property: JsonPropertyName("requestRef")] string RequestRef,
    [property: JsonPropertyName("contextRef")] string ContextRef,
    [property: JsonPropertyName("resultRef")] string ResultRef,
    [property: JsonPropertyName("readinessStatus")] RuntimeProviderReadinessState ReadinessStatus);

internal sealed record RuntimeProviderRegistration(
    string ProviderId,
    string ProviderName,
    string ProviderVersion,
    string ProviderCategory,
    string ExecutionProviderRef,
    IReadOnlyList<string> SupportedRequestSchemaVersions,
    IReadOnlyList<string> SupportedContextSchemaVersions,
    IReadOnlyList<string> SupportedResultSchemaVersions,
    IReadOnlyList<string> SupportedTargetProfiles,
    IReadOnlyList<string> SupportedCapabilities);

internal sealed record RuntimeProviderValidationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> InvalidReferences,
    IReadOnlyList<string> InvalidLineage,
    IReadOnlyList<string> InvalidApprovalState,
    IReadOnlyList<string> CapabilityResolutionFailures,
    IReadOnlyList<string> ExecutionConstraintFailures,
    IReadOnlyList<string> VersionMismatches)
{
    internal static RuntimeProviderValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], []);

    internal bool HasBlockingFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        InvalidReferences.Count > 0 ||
        InvalidLineage.Count > 0 ||
        InvalidApprovalState.Count > 0 ||
        VersionMismatches.Count > 0;
}

internal sealed record RuntimeProviderValidationResult(
    RuntimeProviderValidationDiagnostics Diagnostics)
{
    internal bool IsValid =>
        !Diagnostics.HasBlockingFailures &&
        Diagnostics.CapabilityResolutionFailures.Count == 0 &&
        Diagnostics.ExecutionConstraintFailures.Count == 0;
}

internal sealed record RuntimeProviderFrameworkState(
    PlanningOutcome PlanningOutcome,
    ExecutionProviderFrameworkState? ExecutionProviderState,
    RuntimeProviderRegistration? Registration,
    RuntimeProviderRequest? Request,
    RuntimeProviderContext? Context,
    RuntimeProviderResult? Result,
    RuntimeExecutionCandidate? ExecutionCandidate,
    RuntimeProviderReadinessState Readiness,
    RuntimeProviderValidationResult Validation);
