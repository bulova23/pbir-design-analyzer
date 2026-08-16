using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class ExecutionProviderContract
{
    internal const string SchemaVersionV1 = "execution-provider/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "ProviderId",
        "ProviderName",
        "ProviderVersion",
        "ProviderCategory",
        "SupportedCapabilities",
        "SupportedTargetProfiles",
        "SupportedExecutionModes",
        "SupportedGenerationRequestSchemaVersions",
        "SupportedExecutionPlanSchemaVersions",
        "SupportedCapabilityNegotiationSchemaVersions",
        "SchemaVersion",
        "RequestId",
        "GenerationRequestRef",
        "ExecutionPlanRef",
        "NegotiationResultRef",
        "SourceContractVersions",
        "SourceContractVersions.GenerationRequestSchemaVersion",
        "SourceContractVersions.ExecutionPlanSchemaVersion",
        "SourceContractVersions.CapabilityNegotiationSchemaVersion",
        "ReviewRequirements",
        "ReviewRequirements.DesignApprovalRequired",
        "ReviewRequirements.GenerationApprovalRequired",
        "ReviewRequirements.AnalyzerReviewRequired",
        "SuccessContract",
        "SuccessContract.BusinessSuccessCriteria",
        "SuccessContract.AnalyticalSuccessCriteria",
        "SuccessContract.ValidationRequirements",
        "ExecutionConstraints",
        "ExecutionConstraints.RequiredCapabilities",
        "ExecutionConstraints.RequiredTargetProfileId",
        "ExecutionConstraints.RequiredProviderCategory",
        "RequestedExecutionMode",
        "ApprovalPolicy",
        "ApprovalPolicy.DesignApprovalRequired",
        "ApprovalPolicy.GenerationApprovalRequired",
        "ApprovalPolicy.AnalyzerValidationRequired",
        "ApprovalPolicy.DesignApproved",
        "ApprovalPolicy.GenerationApproved",
        "Status",
        "Eligibility",
        "ReadinessStatus",
        "Reasons",
        "ExecutionRequestLineage",
        "ExecutionRequestLineage.GenerationRequestRef",
        "ExecutionRequestLineage.ExecutionPlanRef",
        "ExecutionRequestLineage.ProviderRequestRef",
        "NegotiationLineage",
        "NegotiationLineage.NegotiationResultRef",
        "NegotiationLineage.NegotiationSchemaVersion",
        "ProviderLineage",
        "ProviderLineage.ProviderId",
        "ProviderLineage.ProviderVersion",
        "ProviderLineage.ProviderCategory",
        "ApprovalLineage",
        "ApprovalLineage.DesignApprovalRequired",
        "ApprovalLineage.GenerationApprovalRequired",
        "ApprovalLineage.AnalyzerValidationRequired",
        "ApprovalLineage.DesignApproved",
        "ApprovalLineage.GenerationApproved",
    ];
}

internal enum ExecutionProviderMode
{
    Manual,
    Assisted,
    Automated,
}

internal enum ExecutionEligibilityStatus
{
    Eligible,
    ConditionallyEligible,
    Ineligible,
    Blocked,
}

internal enum ExecutionProviderReadinessState
{
    NotEligible,
    ConditionallyEligible,
    Eligible,
    ApprovedForExecutionProvider,
}

internal enum ExecutionProviderResponseStatus
{
    Accepted,
    Rejected,
    Blocked,
    Unsupported,
}

internal sealed record ExecutionProviderDefinition(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerName")] string ProviderName,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory,
    [property: JsonPropertyName("supportedCapabilities")] IReadOnlyList<string> SupportedCapabilities,
    [property: JsonPropertyName("supportedTargetProfiles")] IReadOnlyList<string> SupportedTargetProfiles,
    [property: JsonPropertyName("supportedExecutionModes")] IReadOnlyList<ExecutionProviderMode> SupportedExecutionModes,
    [property: JsonPropertyName("supportedGenerationRequestSchemaVersions")] IReadOnlyList<string> SupportedGenerationRequestSchemaVersions,
    [property: JsonPropertyName("supportedExecutionPlanSchemaVersions")] IReadOnlyList<string> SupportedExecutionPlanSchemaVersions,
    [property: JsonPropertyName("supportedCapabilityNegotiationSchemaVersions")] IReadOnlyList<string> SupportedCapabilityNegotiationSchemaVersions);

internal sealed record ExecutionApprovalPolicy(
    [property: JsonPropertyName("designApprovalRequired")] bool DesignApprovalRequired,
    [property: JsonPropertyName("generationApprovalRequired")] bool GenerationApprovalRequired,
    [property: JsonPropertyName("analyzerValidationRequired")] bool AnalyzerValidationRequired,
    [property: JsonPropertyName("designApproved")] bool DesignApproved,
    [property: JsonPropertyName("generationApproved")] bool GenerationApproved);

internal sealed record ExecutionProviderSourceContractVersions(
    [property: JsonPropertyName("generationRequestSchemaVersion")] string GenerationRequestSchemaVersion,
    [property: JsonPropertyName("executionPlanSchemaVersion")] string ExecutionPlanSchemaVersion,
    [property: JsonPropertyName("capabilityNegotiationSchemaVersion")] string CapabilityNegotiationSchemaVersion);

internal sealed record ExecutionProviderConstraintSet(
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<string> RequiredCapabilities,
    [property: JsonPropertyName("requiredTargetProfileId")] string RequiredTargetProfileId,
    [property: JsonPropertyName("requiredProviderCategory")] string RequiredProviderCategory);

internal sealed record ExecutionProviderRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("generationRequestRef")] string GenerationRequestRef,
    [property: JsonPropertyName("executionPlanRef")] string ExecutionPlanRef,
    [property: JsonPropertyName("negotiationResultRef")] string NegotiationResultRef,
    [property: JsonPropertyName("sourceContractVersions")] ExecutionProviderSourceContractVersions SourceContractVersions,
    [property: JsonPropertyName("reviewRequirements")] ExecutionPlanReviewRequirements ReviewRequirements,
    [property: JsonPropertyName("successContract")] GenerationRequestSuccessContract SuccessContract,
    [property: JsonPropertyName("executionConstraints")] ExecutionProviderConstraintSet ExecutionConstraints,
    [property: JsonPropertyName("requestedExecutionMode")] ExecutionProviderMode RequestedExecutionMode,
    [property: JsonPropertyName("approvalPolicy")] ExecutionApprovalPolicy ApprovalPolicy);

internal sealed record ExecutionProviderResponse(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("status")] ExecutionProviderResponseStatus Status,
    [property: JsonPropertyName("eligibility")] ExecutionEligibilityStatus Eligibility,
    [property: JsonPropertyName("readinessStatus")] ExecutionProviderReadinessState ReadinessStatus,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record ExecutionRequestLineage(
    [property: JsonPropertyName("generationRequestRef")] string GenerationRequestRef,
    [property: JsonPropertyName("executionPlanRef")] string ExecutionPlanRef,
    [property: JsonPropertyName("providerRequestRef")] string ProviderRequestRef);

internal sealed record ExecutionNegotiationLineage(
    [property: JsonPropertyName("negotiationResultRef")] string NegotiationResultRef,
    [property: JsonPropertyName("negotiationSchemaVersion")] string NegotiationSchemaVersion);

internal sealed record ExecutionProviderLineage(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory);

internal sealed record ExecutionApprovalLineage(
    [property: JsonPropertyName("designApprovalRequired")] bool DesignApprovalRequired,
    [property: JsonPropertyName("generationApprovalRequired")] bool GenerationApprovalRequired,
    [property: JsonPropertyName("analyzerValidationRequired")] bool AnalyzerValidationRequired,
    [property: JsonPropertyName("designApproved")] bool DesignApproved,
    [property: JsonPropertyName("generationApproved")] bool GenerationApproved);

internal sealed record ExecutionAuditRecord(
    [property: JsonPropertyName("executionRequestLineage")] ExecutionRequestLineage ExecutionRequestLineage,
    [property: JsonPropertyName("negotiationLineage")] ExecutionNegotiationLineage NegotiationLineage,
    [property: JsonPropertyName("providerLineage")] ExecutionProviderLineage ProviderLineage,
    [property: JsonPropertyName("approvalLineage")] ExecutionApprovalLineage ApprovalLineage);

internal sealed record ExecutionProviderDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> InvalidLineage,
    IReadOnlyList<string> InvalidApprovalChains,
    IReadOnlyList<string> UnsupportedProviderDefinitions,
    IReadOnlyList<string> IncompatibleExecutionModes,
    IReadOnlyList<string> VersionMismatches,
    IReadOnlyList<string> CapabilityRequirementFailures,
    IReadOnlyList<string> ReadinessRequirementFailures,
    IReadOnlyList<string> ApprovalRequirementFailures)
{
    internal static ExecutionProviderDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], [], [], []);

    internal bool HasBlockingFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        InvalidLineage.Count > 0 ||
        InvalidApprovalChains.Count > 0 ||
        VersionMismatches.Count > 0;

    internal bool HasUnsupportedFailures =>
        UnsupportedProviderDefinitions.Count > 0 ||
        IncompatibleExecutionModes.Count > 0 ||
        CapabilityRequirementFailures.Count > 0;

    internal bool HasConditionalFailures =>
        ApprovalRequirementFailures.Count > 0 ||
        ReadinessRequirementFailures.Count > 0;
}

internal sealed record ExecutionProviderValidationResult(
    ExecutionProviderDiagnostics Diagnostics)
{
    internal bool IsValid =>
        Diagnostics.MissingRequiredSections.Count == 0 &&
        Diagnostics.MissingRequiredFields.Count == 0;
}

internal sealed record ExecutionEligibilityEvaluation(
    ExecutionEligibilityStatus Status,
    ExecutionProviderDiagnostics Diagnostics);

internal sealed record ExecutionProviderFrameworkState(
    GenerationRequest? GenerationRequest,
    ExecutionPlan? ExecutionPlan,
    CapabilityNegotiationResult? NegotiationResult,
    ExecutionProviderDefinition? ProviderDefinition,
    ExecutionProviderRequest? ProviderRequest,
    ExecutionProviderResponse? ProviderResponse,
    ExecutionApprovalPolicy? ApprovalPolicy,
    ExecutionAuditRecord? AuditRecord,
    ExecutionEligibilityStatus Eligibility,
    ExecutionProviderReadinessState Readiness,
    ExecutionProviderDiagnostics Diagnostics);
