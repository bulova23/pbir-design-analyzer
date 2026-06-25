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
        "References",
        "References.DesignPackageRef",
        "References.GenerationRequestRef",
        "References.ExecutionPlanRef",
        "References.PlanningOutcomeRef",
        "References.RuntimeProviderRef",
        "References.GenerationProviderRequestRef",
        "References.GenerationProviderExecutionPlanRef",
        "GenerationSpecification",
        "GenerationSpecification.PbirGenerationSpecificationRef",
        "CapabilitySummary",
        "CapabilitySummary.NegotiatedCapabilities",
        "CapabilitySummary.ProviderCapabilities",
        "CapabilitySummary.SelectedProvider",
        "CapabilitySummary.SelectedProvider.ProviderId",
        "CapabilitySummary.SelectedProvider.ProviderName",
        "CapabilitySummary.SelectedProvider.ProviderVersion",
        "CapabilitySummary.SelectedSkills",
        "ExecutionConstraints",
        "ExecutionConstraints.DryRunOnly",
        "ExecutionConstraints.DeploymentAllowed",
        "ExecutionConstraints.ProviderInvocationAllowed",
        "ExecutionConstraints.ApiInvocationAllowed",
        "ExecutionConstraints.CliInvocationAllowed",
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
        "ApprovalSummary.RuntimeReadiness",
        "ApprovalSummary.GenerationReadiness",
        "Lineage",
        "Lineage.UpstreamLineage",
        "Lineage.UpstreamLineage.Stage",
        "Lineage.UpstreamLineage.ReferenceId",
        "Lineage.UpstreamLineage.Label",
        "Lineage.ImmutableReferences",
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

internal sealed record GenerationManifestReferences(
    [property: JsonPropertyName("designPackageRef")] string DesignPackageRef,
    [property: JsonPropertyName("generationRequestRef")] string GenerationRequestRef,
    [property: JsonPropertyName("executionPlanRef")] string ExecutionPlanRef,
    [property: JsonPropertyName("planningOutcomeRef")] string PlanningOutcomeRef,
    [property: JsonPropertyName("runtimeProviderRef")] string RuntimeProviderRef,
    [property: JsonPropertyName("generationProviderRequestRef")] string GenerationProviderRequestRef,
    [property: JsonPropertyName("generationProviderExecutionPlanRef")] string GenerationProviderExecutionPlanRef);

internal sealed record GenerationManifestSpecificationSummary(
    [property: JsonPropertyName("pbirGenerationSpecificationRef")] string PbirGenerationSpecificationRef);

internal sealed record GenerationManifestSelectedProvider(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerName")] string ProviderName,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion);

internal sealed record GenerationManifestCapabilitySummary(
    [property: JsonPropertyName("negotiatedCapabilities")] IReadOnlyList<string> NegotiatedCapabilities,
    [property: JsonPropertyName("providerCapabilities")] IReadOnlyList<string> ProviderCapabilities,
    [property: JsonPropertyName("selectedProvider")] GenerationManifestSelectedProvider SelectedProvider,
    [property: JsonPropertyName("selectedSkills")] IReadOnlyList<string> SelectedSkills);

internal sealed record GenerationManifestExecutionConstraints(
    [property: JsonPropertyName("dryRunOnly")] bool DryRunOnly,
    [property: JsonPropertyName("deploymentAllowed")] bool DeploymentAllowed,
    [property: JsonPropertyName("providerInvocationAllowed")] bool ProviderInvocationAllowed,
    [property: JsonPropertyName("apiInvocationAllowed")] bool ApiInvocationAllowed,
    [property: JsonPropertyName("cliInvocationAllowed")] bool CliInvocationAllowed);

internal sealed record GenerationManifestPlanningApprovalSummary(
    [property: JsonPropertyName("outcomeStatus")] PlanningOutcomeStatus OutcomeStatus,
    [property: JsonPropertyName("planningReadiness")] PlanningReadinessStatus PlanningReadiness,
    [property: JsonPropertyName("executionProviderReadiness")] ExecutionProviderReadinessState ExecutionProviderReadiness);

internal sealed record GenerationManifestApprovalSummary(
    [property: JsonPropertyName("designApproval")] PlanningApprovalStatus DesignApproval,
    [property: JsonPropertyName("planningApproval")] GenerationManifestPlanningApprovalSummary PlanningApproval,
    [property: JsonPropertyName("runtimeReadiness")] MicrosoftRuntimeReadinessState RuntimeReadiness,
    [property: JsonPropertyName("generationReadiness")] GenerationProviderExecutionPlanReadinessState GenerationReadiness);

internal sealed record GenerationManifestLineage(
    [property: JsonPropertyName("upstreamLineage")] IReadOnlyList<PlanningLineageEntry> UpstreamLineage,
    [property: JsonPropertyName("immutableReferences")] IReadOnlyList<string> ImmutableReferences);

internal sealed record GenerationManifest(
    [property: JsonPropertyName("metadata")] GenerationManifestMetadata Metadata,
    [property: JsonPropertyName("references")] GenerationManifestReferences References,
    [property: JsonPropertyName("generationSpecification")] GenerationManifestSpecificationSummary GenerationSpecification,
    [property: JsonPropertyName("capabilitySummary")] GenerationManifestCapabilitySummary CapabilitySummary,
    [property: JsonPropertyName("executionConstraints")] GenerationManifestExecutionConstraints ExecutionConstraints,
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
    IReadOnlyList<string> BoundaryViolations)
{
    internal static GenerationManifestValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], []);

    internal bool HasIncompleteFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0;

    internal bool HasBlockingFailures =>
        InvalidReferences.Count > 0 ||
        UnsupportedSchemaVersions.Count > 0 ||
        LineageIntegrityFailures.Count > 0 ||
        ReadinessConsistencyFailures.Count > 0 ||
        ProviderCompatibilityFailures.Count > 0 ||
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
