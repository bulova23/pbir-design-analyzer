using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PlanningOrchestrationContract
{
    internal const string SchemaVersionV1 = "planning-orchestration/v1";
    internal const string TransitionRuleVersionV1 = "planning-stage-transition/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "OrchestrationId",
        "CurrentStage",
        "StageHistory",
        "StageHistory.Stage",
        "StageHistory.Status",
        "StageHistory.ReferenceId",
        "TransitionHistory",
        "TransitionHistory.FromStage",
        "TransitionHistory.ToStage",
        "TransitionHistory.RuleVersion",
    ];
}

internal enum PlanningStage
{
    DesignPackage,
    GenerationRequest,
    ExecutionPlan,
    ProviderAdapterEvaluation,
    MicrosoftPlanningTranslation,
    CapabilityNegotiation,
    MicrosoftSkillsCatalogResolution,
    MicrosoftSkillProviderSelection,
    ExecutionProviderEligibility,
    PlanningOutcome,
}

internal enum PlanningStageStatus
{
    Pending,
    Completed,
    Blocked,
    Failed,
}

internal sealed record PlanningStageHistoryEntry(
    [property: JsonPropertyName("stage")] PlanningStage Stage,
    [property: JsonPropertyName("status")] PlanningStageStatus Status,
    [property: JsonPropertyName("referenceId")] string ReferenceId);

internal sealed record PlanningTransitionRecord(
    [property: JsonPropertyName("fromStage")] PlanningStage FromStage,
    [property: JsonPropertyName("toStage")] PlanningStage ToStage,
    [property: JsonPropertyName("ruleVersion")] string RuleVersion);

internal sealed record PlanningOrchestrationState(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("orchestrationId")] string OrchestrationId,
    [property: JsonPropertyName("currentStage")] PlanningStage CurrentStage,
    [property: JsonPropertyName("stageHistory")] IReadOnlyList<PlanningStageHistoryEntry> StageHistory,
    [property: JsonPropertyName("transitionHistory")] IReadOnlyList<PlanningTransitionRecord> TransitionHistory);

internal sealed record PlanningTransitionContext(
    DesignPackage? DesignPackage,
    GenerationRequest? GenerationRequest,
    ExecutionPlan? ExecutionPlan,
    ProviderAdapterFrameworkState? ProviderAdapterState,
    MicrosoftAdapterPlanningState? MicrosoftPlanningState,
    CapabilityNegotiationResult? CapabilityNegotiationResult,
    MicrosoftSkillPlanningState? MicrosoftSkillState,
    MicrosoftSkillProviderPlanningState? MicrosoftSkillProviderState,
    ExecutionProviderFrameworkState? ExecutionProviderState);

internal sealed record PlanningTransitionValidationResult(
    IReadOnlyList<string> InvalidTransitions,
    IReadOnlyList<string> MissingDependencies,
    IReadOnlyList<string> InvalidReferences,
    IReadOnlyList<string> VersionMismatches,
    IReadOnlyList<string> ReadinessConflicts)
{
    internal bool IsValid =>
        InvalidTransitions.Count == 0 &&
        MissingDependencies.Count == 0 &&
        InvalidReferences.Count == 0 &&
        VersionMismatches.Count == 0 &&
        ReadinessConflicts.Count == 0;
}

internal sealed record PlanningOrchestrationOptions(
    ProviderAdapterDefinition? AdapterDefinition,
    MicrosoftAdapterSpecification? MicrosoftSpecification,
    MicrosoftSkillsCatalogDocument? MicrosoftSkillsCatalog,
    IReadOnlyList<MicrosoftSkillProviderDefinition>? MicrosoftSkillProviders,
    ExecutionProviderDefinition? ExecutionProviderDefinition,
    ExecutionProviderMode ExecutionProviderMode,
    bool DesignApproved,
    bool GenerationApproved)
{
    internal static PlanningOrchestrationOptions Default { get; } =
        new(
            AdapterDefinition: null,
            MicrosoftSpecification: null,
            MicrosoftSkillsCatalog: null,
            MicrosoftSkillProviders: null,
            ExecutionProviderDefinition: null,
            ExecutionProviderMode: ExecutionProviderMode.Manual,
            DesignApproved: true,
            GenerationApproved: true);
}

internal sealed record PlanningOrchestrationResult(
    DesignPackageConsumptionResult ConsumptionResult,
    GenerationRequestFrameworkState GenerationRequestState,
    ExecutionPlanFrameworkState ExecutionPlanState,
    ProviderAdapterFrameworkState ProviderAdapterState,
    MicrosoftAdapterPlanningState? MicrosoftPlanningState,
    CapabilityNegotiationFrameworkState? CapabilityNegotiationState,
    MicrosoftSkillPlanningState? MicrosoftSkillState,
    MicrosoftSkillProviderPlanningState? MicrosoftSkillProviderState,
    ExecutionProviderFrameworkState? ExecutionProviderState,
    PlanningOrchestrationState OrchestrationState,
    PlanningOutcome Outcome);
