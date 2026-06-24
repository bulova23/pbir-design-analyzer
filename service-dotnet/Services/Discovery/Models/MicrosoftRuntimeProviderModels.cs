using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class MicrosoftRuntimeProviderContract
{
    internal const string SchemaVersionV1 = "microsoft-runtime-provider/v1";
    internal const string ProviderId = "microsoft.runtime-provider.contract";
    internal const string ProviderName = "Microsoft Runtime Provider Contract";
    internal const string ProviderVersion = "1.0.0";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ProviderId",
        "ProviderName",
        "ProviderVersion",
        "ProviderCategory",
        "SupportedTargetProfiles",
        "SupportedTargetProfiles.TargetProfileId",
        "SupportedTargetProfiles.ArtifactType",
        "SupportedTargetProfiles.SupportStatus",
        "SupportedTargetProfiles.RequiredCapabilities",
        "SupportedCapabilities",
        "SupportedCapabilities.CapabilityId",
        "SupportedCapabilities.SupportStatus",
        "SupportedCapabilities.ProviderCapabilityRequirements",
        "SupportedExecutionModes",
    ];
}

internal static class MicrosoftRuntimeRequestContract
{
    internal const string SchemaVersionV1 = "microsoft-runtime-request/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "RequestId",
        "RequestMetadata",
        "RequestMetadata.RuntimeProviderRequestSchemaVersion",
        "RequestMetadata.ExecutionCandidateSchemaVersion",
        "RequestMetadata.MicrosoftAdapterSpecificationSchemaVersion",
        "RequestMetadata.MicrosoftSkillsCatalogSchemaVersion",
        "RequestMetadata.SkillProviderSelectionSchemaVersion",
        "PlanningOutcomeReference",
        "PlanningOutcomeReference.OutcomeId",
        "PlanningOutcomeReference.SchemaVersion",
        "ExecutionCandidateReference",
        "ExecutionCandidateReference.CandidateId",
        "ExecutionCandidateReference.SchemaVersion",
        "ExecutionCandidateReference.RuntimeRequestRef",
        "TargetProfile",
        "TargetProfile.TargetProfileId",
        "TargetProfile.ArtifactType",
        "TargetProfile.SupportStatus",
        "CapabilityRequirements",
        "CapabilityRequirements.RequiredCapabilities",
        "CapabilityRequirements.ProviderCapabilityRequirements",
        "SkillRequirements",
        "SkillRequirements.RequiredSkillIds",
        "SkillRequirements.OptionalSkillIds",
        "SkillRequirements.CandidateProviderIds",
        "SkillRequirements.UnsupportedCapabilities",
        "SkillRequirements.Readiness",
        "SkillRequirements.SkillProviderReadiness",
        "ReviewRequirements",
        "ReviewRequirements.DesignApprovalRequired",
        "ReviewRequirements.GenerationApprovalRequired",
        "ReviewRequirements.AnalyzerValidationRequired",
        "ReviewRequirements.DesignApproved",
        "ReviewRequirements.GenerationApproved",
        "ExecutionConstraints",
        "ExecutionConstraints.RequiredProviderCategory",
        "ExecutionConstraints.RequiredExecutionModes",
        "ExecutionConstraints.UnresolvedCapabilities",
        "Provenance",
        "Provenance.GenerationRequestRef",
        "Provenance.ExecutionPlanRef",
        "Provenance.CapabilityNegotiationRef",
        "Provenance.ExecutionProviderRef",
        "Provenance.Lineage",
    ];
}

internal static class MicrosoftRuntimeContextContract
{
    internal const string SchemaVersionV1 = "microsoft-runtime-context/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ContextId",
        "RuntimeProviderReference",
        "RuntimeProviderReference.ProviderId",
        "RuntimeProviderReference.ProviderVersion",
        "RuntimeProviderReference.ProviderCategory",
        "PlanningLineage",
        "PlanningLineage.RuntimeRequestRef",
        "PlanningLineage.PlanningOutcomeRef",
        "PlanningLineage.ExecutionCandidateRef",
        "GenerationRequestLineage",
        "GenerationRequestLineage.GenerationRequestRef",
        "ExecutionPlanLineage",
        "ExecutionPlanLineage.ExecutionPlanRef",
        "CapabilityNegotiationLineage",
        "CapabilityNegotiationLineage.CapabilityNegotiationRef",
        "ApprovalLineage",
        "ApprovalLineage.DesignApprovalRequired",
        "ApprovalLineage.GenerationApprovalRequired",
        "ApprovalLineage.AnalyzerValidationRequired",
        "ApprovalLineage.DesignApproved",
        "ApprovalLineage.GenerationApproved",
        "TargetProfile",
        "TargetProfile.TargetProfileId",
        "TargetProfile.ArtifactType",
        "TargetProfile.SupportStatus",
        "MicrosoftCapabilitySummary",
        "MicrosoftCapabilitySummary.RequiredCapabilities",
        "MicrosoftCapabilitySummary.ProviderCapabilityRequirements",
        "MicrosoftSkillSummary",
        "MicrosoftSkillSummary.RequiredSkillIds",
        "MicrosoftSkillSummary.OptionalSkillIds",
        "MicrosoftSkillSummary.CandidateProviderIds",
        "MicrosoftSkillSummary.UnsupportedCapabilities",
        "MicrosoftSkillSummary.Readiness",
        "MicrosoftSkillSummary.SkillProviderReadiness",
    ];
}

internal enum MicrosoftRuntimeSupportStatus
{
    Supported,
    Planned,
    Unsupported,
}

internal enum MicrosoftRuntimeReadinessState
{
    Invalid,
    Unsupported,
    PlannedOnly,
    Blocked,
    Candidate,
    ReadyForMicrosoftRuntimeProvider,
}

internal sealed record MicrosoftRuntimeTargetProfileSupport(
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("artifactType")] string ArtifactType,
    [property: JsonPropertyName("supportStatus")] MicrosoftRuntimeSupportStatus SupportStatus,
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<string> RequiredCapabilities,
    [property: JsonPropertyName("notes")] string Notes);

internal sealed record MicrosoftRuntimeCapabilitySupport(
    [property: JsonPropertyName("capabilityId")] string CapabilityId,
    [property: JsonPropertyName("supportStatus")] MicrosoftRuntimeSupportStatus SupportStatus,
    [property: JsonPropertyName("providerCapabilityRequirements")] IReadOnlyList<string> ProviderCapabilityRequirements,
    [property: JsonPropertyName("notes")] string Notes);

internal sealed record MicrosoftRuntimeProviderDefinition(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerName")] string ProviderName,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory,
    [property: JsonPropertyName("supportedTargetProfiles")] IReadOnlyList<MicrosoftRuntimeTargetProfileSupport> SupportedTargetProfiles,
    [property: JsonPropertyName("supportedCapabilities")] IReadOnlyList<MicrosoftRuntimeCapabilitySupport> SupportedCapabilities,
    [property: JsonPropertyName("supportedExecutionModes")] IReadOnlyList<ExecutionProviderMode> SupportedExecutionModes);

internal sealed record MicrosoftRuntimeRequestMetadata(
    [property: JsonPropertyName("runtimeProviderRequestSchemaVersion")] string RuntimeProviderRequestSchemaVersion,
    [property: JsonPropertyName("executionCandidateSchemaVersion")] string ExecutionCandidateSchemaVersion,
    [property: JsonPropertyName("microsoftAdapterSpecificationSchemaVersion")] string MicrosoftAdapterSpecificationSchemaVersion,
    [property: JsonPropertyName("microsoftSkillsCatalogSchemaVersion")] string MicrosoftSkillsCatalogSchemaVersion,
    [property: JsonPropertyName("skillProviderSelectionSchemaVersion")] string SkillProviderSelectionSchemaVersion);

internal sealed record MicrosoftRuntimePlanningOutcomeReference(
    [property: JsonPropertyName("outcomeId")] string OutcomeId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion);

internal sealed record MicrosoftRuntimeExecutionCandidateReference(
    [property: JsonPropertyName("candidateId")] string CandidateId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("runtimeRequestRef")] string RuntimeRequestRef);

internal sealed record MicrosoftRuntimeTargetProfile(
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("artifactType")] string ArtifactType,
    [property: JsonPropertyName("supportStatus")] MicrosoftRuntimeSupportStatus SupportStatus);

internal sealed record MicrosoftRuntimeCapabilityRequirements(
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<string> RequiredCapabilities,
    [property: JsonPropertyName("providerCapabilityRequirements")] IReadOnlyList<string> ProviderCapabilityRequirements);

internal sealed record MicrosoftRuntimeSkillRequirements(
    [property: JsonPropertyName("requiredSkillIds")] IReadOnlyList<string> RequiredSkillIds,
    [property: JsonPropertyName("optionalSkillIds")] IReadOnlyList<string> OptionalSkillIds,
    [property: JsonPropertyName("candidateProviderIds")] IReadOnlyList<string> CandidateProviderIds,
    [property: JsonPropertyName("unsupportedCapabilities")] IReadOnlyList<string> UnsupportedCapabilities,
    [property: JsonPropertyName("readiness")] MicrosoftSkillReadinessState Readiness,
    [property: JsonPropertyName("skillProviderReadiness")] MicrosoftSkillProviderReadinessState SkillProviderReadiness);

internal sealed record MicrosoftRuntimeReviewRequirements(
    [property: JsonPropertyName("designApprovalRequired")] bool DesignApprovalRequired,
    [property: JsonPropertyName("generationApprovalRequired")] bool GenerationApprovalRequired,
    [property: JsonPropertyName("analyzerValidationRequired")] bool AnalyzerValidationRequired,
    [property: JsonPropertyName("designApproved")] bool DesignApproved,
    [property: JsonPropertyName("generationApproved")] bool GenerationApproved);

internal sealed record MicrosoftRuntimeExecutionConstraints(
    [property: JsonPropertyName("requiredProviderCategory")] string RequiredProviderCategory,
    [property: JsonPropertyName("requiredExecutionModes")] IReadOnlyList<ExecutionProviderMode> RequiredExecutionModes,
    [property: JsonPropertyName("unresolvedCapabilities")] IReadOnlyList<string> UnresolvedCapabilities);

internal sealed record MicrosoftRuntimeProvenance(
    [property: JsonPropertyName("generationRequestRef")] string GenerationRequestRef,
    [property: JsonPropertyName("executionPlanRef")] string ExecutionPlanRef,
    [property: JsonPropertyName("capabilityNegotiationRef")] string CapabilityNegotiationRef,
    [property: JsonPropertyName("executionProviderRef")] string ExecutionProviderRef,
    [property: JsonPropertyName("lineage")] IReadOnlyList<PlanningLineageEntry> Lineage);

internal sealed record MicrosoftRuntimeRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("requestMetadata")] MicrosoftRuntimeRequestMetadata RequestMetadata,
    [property: JsonPropertyName("planningOutcomeReference")] MicrosoftRuntimePlanningOutcomeReference PlanningOutcomeReference,
    [property: JsonPropertyName("executionCandidateReference")] MicrosoftRuntimeExecutionCandidateReference ExecutionCandidateReference,
    [property: JsonPropertyName("targetProfile")] MicrosoftRuntimeTargetProfile TargetProfile,
    [property: JsonPropertyName("capabilityRequirements")] MicrosoftRuntimeCapabilityRequirements CapabilityRequirements,
    [property: JsonPropertyName("skillRequirements")] MicrosoftRuntimeSkillRequirements SkillRequirements,
    [property: JsonPropertyName("reviewRequirements")] MicrosoftRuntimeReviewRequirements ReviewRequirements,
    [property: JsonPropertyName("executionConstraints")] MicrosoftRuntimeExecutionConstraints ExecutionConstraints,
    [property: JsonPropertyName("provenance")] MicrosoftRuntimeProvenance Provenance);

internal sealed record MicrosoftRuntimeProviderReference(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory);

internal sealed record MicrosoftRuntimePlanningLineage(
    [property: JsonPropertyName("runtimeRequestRef")] string RuntimeRequestRef,
    [property: JsonPropertyName("planningOutcomeRef")] string PlanningOutcomeRef,
    [property: JsonPropertyName("executionCandidateRef")] string ExecutionCandidateRef);

internal sealed record MicrosoftRuntimeGenerationRequestLineage(
    [property: JsonPropertyName("generationRequestRef")] string GenerationRequestRef);

internal sealed record MicrosoftRuntimeExecutionPlanLineage(
    [property: JsonPropertyName("executionPlanRef")] string ExecutionPlanRef);

internal sealed record MicrosoftRuntimeCapabilityNegotiationLineage(
    [property: JsonPropertyName("capabilityNegotiationRef")] string CapabilityNegotiationRef);

internal sealed record MicrosoftRuntimeCapabilitySummary(
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<string> RequiredCapabilities,
    [property: JsonPropertyName("providerCapabilityRequirements")] IReadOnlyList<string> ProviderCapabilityRequirements,
    [property: JsonPropertyName("plannedCapabilities")] IReadOnlyList<string> PlannedCapabilities);

internal sealed record MicrosoftRuntimeSkillSummary(
    [property: JsonPropertyName("requiredSkillIds")] IReadOnlyList<string> RequiredSkillIds,
    [property: JsonPropertyName("optionalSkillIds")] IReadOnlyList<string> OptionalSkillIds,
    [property: JsonPropertyName("candidateProviderIds")] IReadOnlyList<string> CandidateProviderIds,
    [property: JsonPropertyName("unsupportedCapabilities")] IReadOnlyList<string> UnsupportedCapabilities,
    [property: JsonPropertyName("readiness")] MicrosoftSkillReadinessState Readiness,
    [property: JsonPropertyName("skillProviderReadiness")] MicrosoftSkillProviderReadinessState SkillProviderReadiness);

internal sealed record MicrosoftRuntimeContext(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("contextId")] string ContextId,
    [property: JsonPropertyName("runtimeProviderReference")] MicrosoftRuntimeProviderReference RuntimeProviderReference,
    [property: JsonPropertyName("planningLineage")] MicrosoftRuntimePlanningLineage PlanningLineage,
    [property: JsonPropertyName("generationRequestLineage")] MicrosoftRuntimeGenerationRequestLineage GenerationRequestLineage,
    [property: JsonPropertyName("executionPlanLineage")] MicrosoftRuntimeExecutionPlanLineage ExecutionPlanLineage,
    [property: JsonPropertyName("capabilityNegotiationLineage")] MicrosoftRuntimeCapabilityNegotiationLineage CapabilityNegotiationLineage,
    [property: JsonPropertyName("approvalLineage")] MicrosoftRuntimeReviewRequirements ApprovalLineage,
    [property: JsonPropertyName("targetProfile")] MicrosoftRuntimeTargetProfile TargetProfile,
    [property: JsonPropertyName("microsoftCapabilitySummary")] MicrosoftRuntimeCapabilitySummary MicrosoftCapabilitySummary,
    [property: JsonPropertyName("microsoftSkillSummary")] MicrosoftRuntimeSkillSummary MicrosoftSkillSummary);

internal sealed record MicrosoftRuntimeValidationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> UnsupportedTargetProfiles,
    IReadOnlyList<string> PlannedTargetProfiles,
    IReadOnlyList<string> IncompatibleCapabilities,
    IReadOnlyList<string> ApprovalFailures,
    IReadOnlyList<string> ProvenanceFailures,
    IReadOnlyList<string> VersionMismatches,
    IReadOnlyList<string> BlockingFailures)
{
    internal static MicrosoftRuntimeValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], [], []);
}

internal sealed record MicrosoftRuntimeProviderValidationResult(
    MicrosoftRuntimeValidationDiagnostics Diagnostics)
{
    internal bool IsValid =>
        Diagnostics.MissingRequiredSections.Count == 0 &&
        Diagnostics.MissingRequiredFields.Count == 0 &&
        Diagnostics.UnsupportedTargetProfiles.Count == 0 &&
        Diagnostics.PlannedTargetProfiles.Count == 0 &&
        Diagnostics.IncompatibleCapabilities.Count == 0 &&
        Diagnostics.ApprovalFailures.Count == 0 &&
        Diagnostics.ProvenanceFailures.Count == 0 &&
        Diagnostics.VersionMismatches.Count == 0 &&
        Diagnostics.BlockingFailures.Count == 0;
}

internal sealed record MicrosoftRuntimeProviderFrameworkState(
    MicrosoftRuntimeProviderDefinition? Definition,
    RuntimeProviderRegistration? Registration,
    MicrosoftRuntimeRequest? Request,
    MicrosoftRuntimeContext? Context,
    MicrosoftRuntimeProviderValidationResult Validation,
    MicrosoftRuntimeReadinessState Readiness,
    bool AcceptsExecutionCandidate);
