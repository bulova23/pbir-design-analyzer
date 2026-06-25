using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class GenerationProviderContract
{
    internal const string SchemaVersionV1 = "generation-provider/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "Provider",
        "Provider.SchemaVersion",
        "Provider.ProviderId",
        "Provider.ProviderName",
        "Provider.ProviderVersion",
        "Request",
        "Context",
        "Result",
        "Validation",
        "Validation.Diagnostics",
        "Validation.Diagnostics.MissingRequiredSections",
        "Validation.Diagnostics.MissingRequiredFields",
        "Validation.Diagnostics.UnsupportedSchemaVersions",
        "Validation.Diagnostics.UnsupportedArtifactTypes",
        "Validation.Diagnostics.UnsupportedTargetProfiles",
        "Validation.Diagnostics.UnsupportedGenerationModes",
        "Validation.Diagnostics.ProviderCompatibilityFailures",
        "Validation.Diagnostics.SpecificationCompletenessFailures",
        "Validation.Diagnostics.BoundaryViolations",
        "Readiness",
    ];
}

internal static class GenerationProviderDefinitionContract
{
    internal const string SchemaVersionV1 = "generation-provider-definition/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ProviderId",
        "ProviderName",
        "ProviderVersion",
        "SupportedArtifactTypes",
        "SupportedCapabilities",
        "SupportedTargetProfiles",
        "SupportedGenerationModes",
        "Status",
    ];
}

internal static class GenerationProviderRequestContract
{
    internal const string SchemaVersionV1 = "generation-provider-request/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "Metadata",
        "Metadata.RequestId",
        "References",
        "References.PlanningOutcomeReference",
        "References.PlanningOutcomeReference.OutcomeId",
        "References.PlanningOutcomeReference.SchemaVersion",
        "References.ExecutionCandidateReference",
        "References.ExecutionCandidateReference.CandidateId",
        "References.ExecutionCandidateReference.SchemaVersion",
        "References.ExecutionCandidateReference.CandidateRef",
        "References.PbirSpecificationReference",
        "References.PbirSpecificationReference.SpecificationId",
        "References.PbirSpecificationReference.SchemaVersion",
        "References.PbirSpecificationReference.ArtifactSpecificationIds",
        "Requirements",
        "Requirements.CapabilityRequirements",
        "Requirements.CapabilityRequirements.ArtifactType",
        "Requirements.CapabilityRequirements.TargetProfileId",
        "Requirements.CapabilityRequirements.RequiredCapabilities",
        "Requirements.ProviderRequirements",
        "Requirements.ProviderRequirements.ProviderDefinitionSchemaVersion",
        "Requirements.ProviderRequirements.AllowedStatuses",
        "Requirements.ProviderRequirements.RequiredGenerationModes",
        "Requirements.Constraints",
        "Requirements.Constraints.AllowApiInvocation",
        "Requirements.Constraints.AllowCliInvocation",
        "Requirements.Constraints.AllowDeployment",
        "Requirements.Constraints.AllowReportMutation",
    ];
}

internal static class GenerationProviderContextContract
{
    internal const string SchemaVersionV1 = "generation-provider-context/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ContextId",
        "ProviderMetadata",
        "ProviderMetadata.ProviderId",
        "ProviderMetadata.ProviderName",
        "ProviderMetadata.ProviderVersion",
        "ProviderMetadata.Status",
        "SpecificationMetadata",
        "SpecificationMetadata.SpecificationId",
        "SpecificationMetadata.SchemaVersion",
        "SpecificationMetadata.ArtifactType",
        "SpecificationMetadata.TargetProfileId",
        "SpecificationMetadata.ArtifactCount",
        "PlanningMetadata",
        "PlanningMetadata.PlanningOutcomeId",
        "PlanningMetadata.ExecutionCandidateId",
        "PlanningMetadata.Lineage",
        "ReadinessMetadata",
        "ReadinessMetadata.Readiness",
        "ReadinessMetadata.BlockingIssues",
        "ReadinessMetadata.UnsupportedIssues",
    ];
}

internal static class GenerationProviderResultContract
{
    internal const string SchemaVersionV1 = "generation-provider-result/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ResultId",
        "RequestId",
        "Status",
        "Reasons",
    ];
}

internal enum GenerationProviderArtifactType
{
    PbirReport,
    FabricDataApp,
    FabricApp,
}

internal enum GenerationProviderMode
{
    StructuredRequest,
    Assisted,
    Mock,
}

internal enum GenerationProviderStatus
{
    Available,
    Planned,
    Deprecated,
    Unsupported,
}

internal enum GenerationProviderReadinessState
{
    Unsupported,
    Blocked,
    Candidate,
    ReadyForGenerationProvider,
}

internal enum GenerationProviderResultStatus
{
    Accepted,
    Rejected,
    Unsupported,
    Blocked,
}

internal sealed record GenerationProviderDefinition(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerName")] string ProviderName,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("supportedArtifactTypes")] IReadOnlyList<GenerationProviderArtifactType> SupportedArtifactTypes,
    [property: JsonPropertyName("supportedCapabilities")] IReadOnlyList<string> SupportedCapabilities,
    [property: JsonPropertyName("supportedTargetProfiles")] IReadOnlyList<string> SupportedTargetProfiles,
    [property: JsonPropertyName("supportedGenerationModes")] IReadOnlyList<GenerationProviderMode> SupportedGenerationModes,
    [property: JsonPropertyName("status")] GenerationProviderStatus Status);

internal sealed record GenerationProviderRequestMetadata(
    [property: JsonPropertyName("requestId")] string RequestId);

internal sealed record GenerationProviderPlanningOutcomeReference(
    [property: JsonPropertyName("outcomeId")] string OutcomeId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion);

internal sealed record GenerationProviderExecutionCandidateReference(
    [property: JsonPropertyName("candidateId")] string CandidateId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("candidateRef")] string CandidateRef);

internal sealed record GenerationProviderPbirSpecificationReference(
    [property: JsonPropertyName("specificationId")] string SpecificationId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("artifactSpecificationIds")] IReadOnlyList<string> ArtifactSpecificationIds);

internal sealed record GenerationProviderRequestReferences(
    [property: JsonPropertyName("planningOutcomeReference")] GenerationProviderPlanningOutcomeReference PlanningOutcomeReference,
    [property: JsonPropertyName("executionCandidateReference")] GenerationProviderExecutionCandidateReference ExecutionCandidateReference,
    [property: JsonPropertyName("pbirSpecificationReference")] GenerationProviderPbirSpecificationReference PbirSpecificationReference);

internal sealed record GenerationProviderCapabilityRequirements(
    [property: JsonPropertyName("artifactType")] GenerationProviderArtifactType ArtifactType,
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<string> RequiredCapabilities);

internal sealed record GenerationProviderProviderRequirements(
    [property: JsonPropertyName("providerDefinitionSchemaVersion")] string ProviderDefinitionSchemaVersion,
    [property: JsonPropertyName("allowedStatuses")] IReadOnlyList<GenerationProviderStatus> AllowedStatuses,
    [property: JsonPropertyName("requiredGenerationModes")] IReadOnlyList<GenerationProviderMode> RequiredGenerationModes);

internal sealed record GenerationProviderConstraints(
    [property: JsonPropertyName("allowApiInvocation")] bool AllowApiInvocation,
    [property: JsonPropertyName("allowCliInvocation")] bool AllowCliInvocation,
    [property: JsonPropertyName("allowDeployment")] bool AllowDeployment,
    [property: JsonPropertyName("allowReportMutation")] bool AllowReportMutation);

internal sealed record GenerationProviderRequirements(
    [property: JsonPropertyName("capabilityRequirements")] GenerationProviderCapabilityRequirements CapabilityRequirements,
    [property: JsonPropertyName("providerRequirements")] GenerationProviderProviderRequirements ProviderRequirements,
    [property: JsonPropertyName("constraints")] GenerationProviderConstraints Constraints);

internal sealed record GenerationProviderRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("metadata")] GenerationProviderRequestMetadata Metadata,
    [property: JsonPropertyName("references")] GenerationProviderRequestReferences References,
    [property: JsonPropertyName("requirements")] GenerationProviderRequirements Requirements);

internal sealed record GenerationProviderContextProviderMetadata(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerName")] string ProviderName,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("status")] GenerationProviderStatus Status);

internal sealed record GenerationProviderSpecificationMetadata(
    [property: JsonPropertyName("specificationId")] string SpecificationId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("artifactType")] GenerationProviderArtifactType ArtifactType,
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("artifactCount")] int ArtifactCount);

internal sealed record GenerationProviderPlanningMetadata(
    [property: JsonPropertyName("planningOutcomeId")] string PlanningOutcomeId,
    [property: JsonPropertyName("executionCandidateId")] string ExecutionCandidateId,
    [property: JsonPropertyName("lineage")] IReadOnlyList<PlanningLineageEntry> Lineage);

internal sealed record GenerationProviderReadinessMetadata(
    [property: JsonPropertyName("readiness")] GenerationProviderReadinessState Readiness,
    [property: JsonPropertyName("blockingIssues")] IReadOnlyList<string> BlockingIssues,
    [property: JsonPropertyName("unsupportedIssues")] IReadOnlyList<string> UnsupportedIssues);

internal sealed record GenerationProviderContext(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("contextId")] string ContextId,
    [property: JsonPropertyName("providerMetadata")] GenerationProviderContextProviderMetadata ProviderMetadata,
    [property: JsonPropertyName("specificationMetadata")] GenerationProviderSpecificationMetadata SpecificationMetadata,
    [property: JsonPropertyName("planningMetadata")] GenerationProviderPlanningMetadata PlanningMetadata,
    [property: JsonPropertyName("readinessMetadata")] GenerationProviderReadinessMetadata ReadinessMetadata);

internal sealed record GenerationProviderResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("resultId")] string ResultId,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("status")] GenerationProviderResultStatus Status,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record GenerationProviderValidationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> UnsupportedSchemaVersions,
    IReadOnlyList<string> UnsupportedArtifactTypes,
    IReadOnlyList<string> UnsupportedTargetProfiles,
    IReadOnlyList<string> UnsupportedGenerationModes,
    IReadOnlyList<string> ProviderCompatibilityFailures,
    IReadOnlyList<string> SpecificationCompletenessFailures,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static GenerationProviderValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], [], []);

    internal bool HasBlockingFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        SpecificationCompletenessFailures.Count > 0 ||
        BoundaryViolations.Count > 0;

    internal bool HasUnsupportedFailures =>
        UnsupportedSchemaVersions.Count > 0 ||
        UnsupportedArtifactTypes.Count > 0 ||
        UnsupportedTargetProfiles.Count > 0 ||
        UnsupportedGenerationModes.Count > 0 ||
        ProviderCompatibilityFailures.Count > 0;
}

internal sealed record GenerationProviderValidationResult(
    GenerationProviderValidationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasBlockingFailures && !Diagnostics.HasUnsupportedFailures;
}

internal sealed record GenerationProviderFrameworkState(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("provider")] GenerationProviderDefinition? Provider,
    [property: JsonPropertyName("request")] GenerationProviderRequest? Request,
    [property: JsonPropertyName("context")] GenerationProviderContext? Context,
    [property: JsonPropertyName("result")] GenerationProviderResult? Result,
    [property: JsonPropertyName("validation")] GenerationProviderValidationResult Validation,
    [property: JsonPropertyName("readiness")] GenerationProviderReadinessState Readiness);
