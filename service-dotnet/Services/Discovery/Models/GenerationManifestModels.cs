using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class GenerationManifestContract
{
    internal const string SchemaVersionV1 = "generation-manifest/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "Metadata",
        "Metadata.ManifestId",
        "Metadata.SchemaVersion",
        "Metadata.CreatedUtc",
        "SourceReferences",
        "SourceReferences.DesignPackageRef",
        "SourceReferences.GenerationRequestRef",
        "SourceReferences.ExecutionPlanRef",
        "SourceReferences.PlanningOutcomeRef",
        "SourceReferences.RuntimeProviderRef",
        "SourceReferences.GenerationProviderRequestRef",
        "SourceReferences.GenerationProviderExecutionPlanRef",
        "SourceReferences.PbirGenerationSpecificationRef",
        "CapabilitySummary",
        "CapabilitySummary.NegotiatedCapabilities",
        "CapabilitySummary.SelectedGenerationProvider",
        "CapabilitySummary.SelectedGenerationProvider.ProviderId",
        "CapabilitySummary.SelectedGenerationProvider.ProviderName",
        "CapabilitySummary.SelectedGenerationProvider.ProviderVersion",
        "CapabilitySummary.SelectedMicrosoftRuntimeProvider",
        "CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderId",
        "CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderName",
        "CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderVersion",
        "CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderCategory",
        "CapabilitySummary.SelectedSkills",
        "CapabilitySummary.SelectedProviderCandidates",
        "ExecutionConstraints",
        "ExecutionConstraints.DryRunOnly",
        "ExecutionConstraints.DeploymentAllowed",
        "ExecutionConstraints.ProviderInvocationAllowed",
        "ExecutionConstraints.ApiInvocationAllowed",
        "ExecutionConstraints.CliInvocationAllowed",
        "ReadinessSummary",
        "ReadinessSummary.PlanningReadiness",
        "ReadinessSummary.RuntimeReadiness",
        "ReadinessSummary.ProviderReadiness",
        "ReadinessSummary.GenerationReadiness",
        "ApprovalSummary",
        "ApprovalSummary.DesignApproval",
        "ApprovalSummary.DesignApproval.DesignApprovalRequired",
        "ApprovalSummary.DesignApproval.GenerationApprovalRequired",
        "ApprovalSummary.DesignApproval.AnalyzerValidationRequired",
        "ApprovalSummary.DesignApproval.DesignApproved",
        "ApprovalSummary.DesignApproval.GenerationApproved",
        "ApprovalSummary.PlanningApproval",
        "ApprovalSummary.PlanningApproval.OutcomeStatus",
        "ApprovalSummary.PlanningApproval.PlanningReadiness",
        "ApprovalSummary.PlanningApproval.ExecutionProviderReadiness",
        "ApprovalSummary.RuntimeApproval",
        "ApprovalSummary.RuntimeApproval.RuntimeProviderId",
        "ApprovalSummary.RuntimeApproval.RuntimeReadiness",
        "ApprovalSummary.RuntimeApproval.AcceptsExecutionCandidate",
        "ApprovalSummary.ProviderApproval",
        "ApprovalSummary.ProviderApproval.ProviderId",
        "ApprovalSummary.ProviderApproval.ProviderReadiness",
        "ApprovalSummary.ProviderApproval.ProviderApproved",
        "Lineage",
        "Lineage.UpstreamLineage",
        "Lineage.UpstreamLineage.Stage",
        "Lineage.UpstreamLineage.ReferenceId",
        "Lineage.UpstreamLineage.Label",
        "Lineage.ImmutableUpstreamLineage",
    ];
}

internal enum GenerationManifestReadinessState
{
    Incomplete,
    Blocked,
    ReadyForGenerator,
}

internal sealed record GenerationManifestMetadata(
    [property: JsonPropertyName("manifestId")] string ManifestId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("createdUtc")] DateTime CreatedUtc);

internal sealed record GenerationManifestSourceReferences(
    [property: JsonPropertyName("designPackageRef")] string DesignPackageRef,
    [property: JsonPropertyName("generationRequestRef")] string GenerationRequestRef,
    [property: JsonPropertyName("executionPlanRef")] string ExecutionPlanRef,
    [property: JsonPropertyName("planningOutcomeRef")] string PlanningOutcomeRef,
    [property: JsonPropertyName("runtimeProviderRef")] string RuntimeProviderRef,
    [property: JsonPropertyName("generationProviderRequestRef")] string GenerationProviderRequestRef,
    [property: JsonPropertyName("generationProviderExecutionPlanRef")] string GenerationProviderExecutionPlanRef,
    [property: JsonPropertyName("pbirGenerationSpecificationRef")] string PbirGenerationSpecificationRef);

internal sealed record GenerationManifestProviderSummary(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerName")] string ProviderName,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion);

internal sealed record GenerationManifestMicrosoftRuntimeProviderSummary(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerName")] string ProviderName,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory);

internal sealed record GenerationManifestCapabilitySummary(
    [property: JsonPropertyName("negotiatedCapabilities")] IReadOnlyList<string> NegotiatedCapabilities,
    [property: JsonPropertyName("selectedGenerationProvider")] GenerationManifestProviderSummary SelectedGenerationProvider,
    [property: JsonPropertyName("selectedMicrosoftRuntimeProvider")] GenerationManifestMicrosoftRuntimeProviderSummary SelectedMicrosoftRuntimeProvider,
    [property: JsonPropertyName("selectedSkills")] IReadOnlyList<string> SelectedSkills,
    [property: JsonPropertyName("selectedProviderCandidates")] IReadOnlyList<string> SelectedProviderCandidates);

internal sealed record GenerationManifestExecutionConstraints(
    [property: JsonPropertyName("dryRunOnly")] bool DryRunOnly,
    [property: JsonPropertyName("deploymentAllowed")] bool DeploymentAllowed,
    [property: JsonPropertyName("providerInvocationAllowed")] bool ProviderInvocationAllowed,
    [property: JsonPropertyName("apiInvocationAllowed")] bool ApiInvocationAllowed,
    [property: JsonPropertyName("cliInvocationAllowed")] bool CliInvocationAllowed);

internal sealed record GenerationManifestReadinessSummary(
    [property: JsonPropertyName("planningReadiness")] PlanningReadinessStatus PlanningReadiness,
    [property: JsonPropertyName("runtimeReadiness")] RuntimeProviderReadinessState RuntimeReadiness,
    [property: JsonPropertyName("providerReadiness")] GenerationProviderReadinessState ProviderReadiness,
    [property: JsonPropertyName("generationReadiness")] GenerationProviderExecutionPlanReadinessState GenerationReadiness);

internal sealed record GenerationManifestPlanningApprovalSummary(
    [property: JsonPropertyName("outcomeStatus")] PlanningOutcomeStatus OutcomeStatus,
    [property: JsonPropertyName("planningReadiness")] PlanningReadinessStatus PlanningReadiness,
    [property: JsonPropertyName("executionProviderReadiness")] ExecutionProviderReadinessState ExecutionProviderReadiness);

internal sealed record GenerationManifestRuntimeApprovalSummary(
    [property: JsonPropertyName("runtimeProviderId")] string RuntimeProviderId,
    [property: JsonPropertyName("runtimeReadiness")] MicrosoftRuntimeReadinessState RuntimeReadiness,
    [property: JsonPropertyName("acceptsExecutionCandidate")] bool AcceptsExecutionCandidate);

internal sealed record GenerationManifestProviderApprovalSummary(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerReadiness")] GenerationProviderReadinessState ProviderReadiness,
    [property: JsonPropertyName("providerApproved")] bool ProviderApproved);

internal sealed record GenerationManifestApprovalSummary(
    [property: JsonPropertyName("designApproval")] PlanningApprovalStatus DesignApproval,
    [property: JsonPropertyName("planningApproval")] GenerationManifestPlanningApprovalSummary PlanningApproval,
    [property: JsonPropertyName("runtimeApproval")] GenerationManifestRuntimeApprovalSummary RuntimeApproval,
    [property: JsonPropertyName("providerApproval")] GenerationManifestProviderApprovalSummary ProviderApproval);

internal sealed record GenerationManifestLineage(
    [property: JsonPropertyName("upstreamLineage")] IReadOnlyList<PlanningLineageEntry> UpstreamLineage,
    [property: JsonPropertyName("immutableUpstreamLineage")] IReadOnlyList<string> ImmutableUpstreamLineage);

internal sealed record GenerationManifest(
    [property: JsonPropertyName("metadata")] GenerationManifestMetadata Metadata,
    [property: JsonPropertyName("sourceReferences")] GenerationManifestSourceReferences SourceReferences,
    [property: JsonPropertyName("capabilitySummary")] GenerationManifestCapabilitySummary CapabilitySummary,
    [property: JsonPropertyName("executionConstraints")] GenerationManifestExecutionConstraints ExecutionConstraints,
    [property: JsonPropertyName("readinessSummary")] GenerationManifestReadinessSummary ReadinessSummary,
    [property: JsonPropertyName("approvalSummary")] GenerationManifestApprovalSummary ApprovalSummary,
    [property: JsonPropertyName("lineage")] GenerationManifestLineage Lineage);

internal sealed record GenerationManifestValidationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> InvalidReferences,
    IReadOnlyList<string> UnsupportedSchemaVersions,
    IReadOnlyList<string> LineageIntegrityFailures,
    IReadOnlyList<string> ReadinessConsistencyFailures,
    IReadOnlyList<string> ProviderCompatibilityFailures,
    IReadOnlyList<string> GenerationSpecificationCompletenessFailures,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static GenerationManifestValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], [], []);

    internal bool HasIncompleteFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0;

    internal bool HasBlockingFailures =>
        InvalidReferences.Count > 0 ||
        UnsupportedSchemaVersions.Count > 0 ||
        LineageIntegrityFailures.Count > 0 ||
        ReadinessConsistencyFailures.Count > 0 ||
        ProviderCompatibilityFailures.Count > 0 ||
        GenerationSpecificationCompletenessFailures.Count > 0 ||
        BoundaryViolations.Count > 0;
}

internal sealed record GenerationManifestValidationResult(
    GenerationManifestValidationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasIncompleteFailures && !Diagnostics.HasBlockingFailures;
}

internal sealed record GenerationManifestState(
    GenerationManifest? Manifest,
    GenerationManifestValidationResult Validation,
    GenerationManifestReadinessState Readiness);
