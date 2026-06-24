using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PlanningOutcomeContract
{
    internal const string SchemaVersionV1 = "planning-outcome/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "Metadata",
        "Metadata.SchemaVersion",
        "Metadata.OutcomeId",
        "References",
        "References.DesignPackageRef",
        "References.GenerationRequestRef",
        "References.ExecutionPlanRef",
        "References.NegotiationRef",
        "References.ExecutionProviderRef",
        "Status",
        "ReadinessSummary",
        "ReadinessSummary.Status",
        "ReadinessSummary.BlockingIssues",
        "ReadinessSummary.UnresolvedRequirements",
        "ReadinessSummary.CapabilitySummary",
        "ReadinessSummary.CapabilitySummary.RequiredCapabilities",
        "ReadinessSummary.CapabilitySummary.ResolvedCapabilities",
        "ReadinessSummary.CapabilitySummary.UnresolvedCapabilities",
        "ReadinessSummary.ApprovalStatus",
        "ReadinessSummary.ApprovalStatus.DesignApprovalRequired",
        "ReadinessSummary.ApprovalStatus.GenerationApprovalRequired",
        "ReadinessSummary.ApprovalStatus.AnalyzerValidationRequired",
        "ReadinessSummary.ApprovalStatus.DesignApproved",
        "ReadinessSummary.ApprovalStatus.GenerationApproved",
        "ReadinessSummary.ExecutionProviderReadiness",
        "Lineage",
        "Lineage.UpstreamLineage",
        "Lineage.PlanningLineage",
        "Lineage.ApprovalLineage",
        "Lineage.ApprovalLineage.DesignApprovalRequired",
        "Lineage.ApprovalLineage.GenerationApprovalRequired",
        "Lineage.ApprovalLineage.AnalyzerValidationRequired",
        "Lineage.ApprovalLineage.DesignApproved",
        "Lineage.ApprovalLineage.GenerationApproved",
    ];
}

internal enum PlanningOutcomeStatus
{
    Draft,
    PlanningComplete,
    PlanningBlocked,
    PlanningFailed,
    ApprovedForExecutionProvider,
}

internal enum PlanningReadinessStatus
{
    Draft,
    Blocked,
    ReadyForProviderPlanning,
    ReadyForProviderAdapter,
    ReadyForMicrosoftAdapter,
    ReadyForExecutionProvider,
    ApprovedForExecutionProvider,
}

internal enum PlanningFailureType
{
    InvalidInput,
    MissingDependency,
    BlockedCapability,
    UnsupportedTarget,
    IncompatibleProvider,
    InvalidTransition,
    InvalidReference,
    InvalidVersion,
    ReadinessConflict,
}

internal sealed record PlanningOutcomeMetadata(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("outcomeId")] string OutcomeId);

internal sealed record PlanningOutcomeReferences(
    [property: JsonPropertyName("designPackageRef")] string DesignPackageRef,
    [property: JsonPropertyName("generationRequestRef")] string GenerationRequestRef,
    [property: JsonPropertyName("executionPlanRef")] string ExecutionPlanRef,
    [property: JsonPropertyName("negotiationRef")] string NegotiationRef,
    [property: JsonPropertyName("executionProviderRef")] string ExecutionProviderRef);

internal sealed record PlanningCapabilitySummary(
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<string> RequiredCapabilities,
    [property: JsonPropertyName("resolvedCapabilities")] IReadOnlyList<string> ResolvedCapabilities,
    [property: JsonPropertyName("unresolvedCapabilities")] IReadOnlyList<string> UnresolvedCapabilities);

internal sealed record PlanningApprovalStatus(
    [property: JsonPropertyName("designApprovalRequired")] bool DesignApprovalRequired,
    [property: JsonPropertyName("generationApprovalRequired")] bool GenerationApprovalRequired,
    [property: JsonPropertyName("analyzerValidationRequired")] bool AnalyzerValidationRequired,
    [property: JsonPropertyName("designApproved")] bool DesignApproved,
    [property: JsonPropertyName("generationApproved")] bool GenerationApproved);

internal sealed record PlanningReadinessSummary(
    [property: JsonPropertyName("status")] PlanningReadinessStatus Status,
    [property: JsonPropertyName("blockingIssues")] IReadOnlyList<string> BlockingIssues,
    [property: JsonPropertyName("unresolvedRequirements")] IReadOnlyList<string> UnresolvedRequirements,
    [property: JsonPropertyName("capabilitySummary")] PlanningCapabilitySummary CapabilitySummary,
    [property: JsonPropertyName("approvalStatus")] PlanningApprovalStatus ApprovalStatus,
    [property: JsonPropertyName("executionProviderReadiness")] ExecutionProviderReadinessState ExecutionProviderReadiness);

internal sealed record PlanningFailure(
    [property: JsonPropertyName("failureType")] PlanningFailureType FailureType,
    [property: JsonPropertyName("stage")] PlanningStage Stage,
    [property: JsonPropertyName("message")] string Message);

internal sealed record PlanningLineageEntry(
    [property: JsonPropertyName("stage")] string Stage,
    [property: JsonPropertyName("referenceId")] string ReferenceId,
    [property: JsonPropertyName("label")] string Label);

internal sealed record PlanningOutcomeLineage(
    [property: JsonPropertyName("upstreamLineage")] IReadOnlyList<PlanningLineageEntry> UpstreamLineage,
    [property: JsonPropertyName("planningLineage")] IReadOnlyList<PlanningLineageEntry> PlanningLineage,
    [property: JsonPropertyName("approvalLineage")] PlanningApprovalStatus ApprovalLineage);

internal sealed record PlanningOutcome(
    [property: JsonPropertyName("metadata")] PlanningOutcomeMetadata Metadata,
    [property: JsonPropertyName("references")] PlanningOutcomeReferences References,
    [property: JsonPropertyName("status")] PlanningOutcomeStatus Status,
    [property: JsonPropertyName("readinessSummary")] PlanningReadinessSummary ReadinessSummary,
    [property: JsonPropertyName("lineage")] PlanningOutcomeLineage Lineage,
    [property: JsonPropertyName("failures")] IReadOnlyList<PlanningFailure> Failures);
