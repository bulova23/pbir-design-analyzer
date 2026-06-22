using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class ProviderAdapterContract
{
    internal const string SchemaVersionV1 = "provider-adapter/v1";
    internal const string ProviderNeutralCategory = "providerNeutral";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "Definition.AdapterId",
        "Definition.AdapterName",
        "Definition.AdapterVersion",
        "Definition.ProviderCategory",
        "Definition.SupportedTargetProfiles",
        "Definition.SupportedCapabilities",
        "Definition.UnsupportedCapabilities",
        "Definition.SupportedGenerationRequestSchemaVersions",
        "Definition.SupportedExecutionPlanSchemaVersions",
        "Request.SchemaVersion",
        "Request.ExecutionPlanRef",
        "Request.GenerationRequestRef",
        "Request.SourceContractVersions",
        "Request.SourceContractVersions.GenerationRequestSchemaVersion",
        "Request.SourceContractVersions.ExecutionPlanSchemaVersion",
        "Request.TargetArtifactProfile",
        "Request.TargetArtifactProfile.ArtifactType",
        "Request.TargetArtifactProfile.ProfileId",
        "Request.TargetArtifactProfile.SourceExperienceType",
        "Request.CapabilityRequirements",
        "Request.Constraints",
        "Request.Constraints.UnsupportedTargets",
        "Request.Constraints.UnsupportedCapabilities",
        "Request.Constraints.ReviewRequirements",
        "Request.Constraints.ValidationRequirements",
        "Request.ReviewRequirements",
        "Request.ReviewRequirements.DesignApprovalRequired",
        "Request.ReviewRequirements.GenerationApprovalRequired",
        "Request.ReviewRequirements.AnalyzerReviewRequired",
        "Request.SuccessContract",
        "Request.SuccessContract.BusinessSuccessCriteria",
        "Request.SuccessContract.AnalyticalSuccessCriteria",
        "Request.SuccessContract.ValidationRequirements",
    ];
}

internal sealed record ProviderAdapterDefinition(
    [property: JsonPropertyName("adapterId")] string AdapterId,
    [property: JsonPropertyName("adapterName")] string AdapterName,
    [property: JsonPropertyName("adapterVersion")] string AdapterVersion,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory,
    [property: JsonPropertyName("supportedTargetProfiles")] IReadOnlyList<string> SupportedTargetProfiles,
    [property: JsonPropertyName("supportedCapabilities")] IReadOnlyList<string> SupportedCapabilities,
    [property: JsonPropertyName("unsupportedCapabilities")] IReadOnlyList<string> UnsupportedCapabilities,
    [property: JsonPropertyName("supportedGenerationRequestSchemaVersions")] IReadOnlyList<string> SupportedGenerationRequestSchemaVersions,
    [property: JsonPropertyName("supportedExecutionPlanSchemaVersions")] IReadOnlyList<string> SupportedExecutionPlanSchemaVersions);

internal sealed record ProviderAdapterRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("executionPlanRef")] string ExecutionPlanRef,
    [property: JsonPropertyName("generationRequestRef")] string GenerationRequestRef,
    [property: JsonPropertyName("sourceContractVersions")] ProviderAdapterSourceContractVersions SourceContractVersions,
    [property: JsonPropertyName("targetArtifactProfile")] GenerationRequestTargetArtifactProfile TargetArtifactProfile,
    [property: JsonPropertyName("capabilityRequirements")] IReadOnlyList<string> CapabilityRequirements,
    [property: JsonPropertyName("constraints")] ProviderAdapterConstraintSet Constraints,
    [property: JsonPropertyName("reviewRequirements")] ExecutionPlanReviewRequirements ReviewRequirements,
    [property: JsonPropertyName("successContract")] GenerationRequestSuccessContract SuccessContract);

internal sealed record ProviderAdapterSourceContractVersions(
    [property: JsonPropertyName("generationRequestSchemaVersion")] string GenerationRequestSchemaVersion,
    [property: JsonPropertyName("executionPlanSchemaVersion")] string ExecutionPlanSchemaVersion);

internal sealed record ProviderAdapterConstraintSet(
    [property: JsonPropertyName("unsupportedTargets")] IReadOnlyList<string> UnsupportedTargets,
    [property: JsonPropertyName("unsupportedCapabilities")] IReadOnlyList<string> UnsupportedCapabilities,
    [property: JsonPropertyName("reviewRequirements")] IReadOnlyList<string> ReviewRequirements,
    [property: JsonPropertyName("validationRequirements")] IReadOnlyList<string> ValidationRequirements);

internal enum ProviderAdapterCompatibilityStatus
{
    Compatible,
    Incompatible,
    Unsupported,
}

internal enum ProviderAdapterPlanningResponseStatus
{
    Accepted,
    Rejected,
    Unsupported,
    Incompatible,
}

internal enum ProviderAdapterPlanningReadinessState
{
    Discovered,
    Compatible,
    Incompatible,
    Unsupported,
    ReadyForExecutionProvider,
}

internal sealed record ProviderAdapterCompatibilityDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> TargetCompatibilityFailures,
    IReadOnlyList<string> CapabilityCompatibilityFailures,
    IReadOnlyList<string> ExecutionPlanCompatibilityFailures,
    IReadOnlyList<string> VersionCompatibilityFailures)
{
    internal static ProviderAdapterCompatibilityDiagnostics Empty { get; } =
        new([], [], [], [], [], []);

    internal bool HasStructuralFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        ExecutionPlanCompatibilityFailures.Count > 0 ||
        VersionCompatibilityFailures.Count > 0;

    internal bool HasUnsupportedFailures =>
        TargetCompatibilityFailures.Count > 0 ||
        CapabilityCompatibilityFailures.Count > 0;
}

internal sealed record ProviderAdapterCompatibilityEvaluation(
    ProviderAdapterCompatibilityStatus Status,
    ProviderAdapterCompatibilityDiagnostics Diagnostics);

internal sealed record ProviderAdapterPlanningResponse(
    [property: JsonPropertyName("adapterId")] string AdapterId,
    [property: JsonPropertyName("status")] ProviderAdapterPlanningResponseStatus Status,
    [property: JsonPropertyName("compatibility")] ProviderAdapterCompatibilityEvaluation Compatibility);

internal sealed record ProviderAdapterRequestCreationResult(
    ProviderAdapterRequest? Request,
    ProviderAdapterCompatibilityDiagnostics Diagnostics)
{
    internal bool IsValid =>
        Request is not null &&
        !Diagnostics.HasStructuralFailures &&
        !Diagnostics.HasUnsupportedFailures;
}

internal sealed record ProviderAdapterFrameworkState(
    GenerationRequest? GenerationRequest,
    ExecutionPlan? ExecutionPlan,
    ProviderAdapterRequest? AdapterRequest,
    ProviderAdapterDefinition? AdapterDefinition,
    ProviderAdapterPlanningResponse? PlanningResponse,
    ProviderAdapterPlanningReadinessState Readiness,
    ProviderAdapterCompatibilityDiagnostics Diagnostics);
