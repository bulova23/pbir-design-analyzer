using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class GenerationProviderExecutionPlanContract
{
    internal const string SchemaVersionV1 = "generation-provider-execution-plan/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "Metadata",
        "Metadata.ExecutionPlanId",
        "Metadata.SchemaVersion",
        "References",
        "References.GenerationProviderRequestRef",
        "References.PbirGenerationSpecificationRef",
        "References.PlanningOutcomeRef",
        "ExecutionStages",
        "ExecutionStages.StageId",
        "ExecutionStages.StageName",
        "ExecutionStages.Sequence",
        "ExecutionStages.RequiredDependencyIds",
        "ExecutionConstraints",
        "ExecutionConstraints.DryRunOnly",
        "ExecutionConstraints.MockExecutionPermitted",
        "ExecutionConstraints.DeploymentProhibited",
        "ExecutionConstraints.ProviderInvocationProhibited",
        "ExecutionConstraints.ApiInvocationProhibited",
        "ExecutionConstraints.CliInvocationProhibited",
        "ExecutionConstraints.ReportMutationProhibited",
        "ExecutionDependencies",
        "ExecutionDependencies.RequiredApprovals",
        "ExecutionDependencies.RequiredApprovals.DesignApprovalRequired",
        "ExecutionDependencies.RequiredApprovals.GenerationApprovalRequired",
        "ExecutionDependencies.RequiredApprovals.AnalyzerReviewRequired",
        "ExecutionDependencies.RequiredApprovals.DesignApproved",
        "ExecutionDependencies.RequiredApprovals.GenerationApproved",
        "ExecutionDependencies.ProviderReadiness",
        "ExecutionDependencies.ProviderReadiness.CurrentReadiness",
        "ExecutionDependencies.ProviderReadiness.RequiredReadiness",
        "ExecutionDependencies.RuntimeReadiness",
        "ExecutionDependencies.RuntimeReadiness.CurrentReadiness",
        "ExecutionDependencies.RuntimeReadiness.RequiredReadiness",
        "ExecutionDependencies.SpecificationCompleteness",
        "ExecutionDependencies.SpecificationCompleteness.CurrentReadiness",
        "ExecutionDependencies.SpecificationCompleteness.RequiredReadiness",
    ];
}

internal enum GenerationProviderExecutionPlanReadinessState
{
    Blocked,
    PartiallyPrepared,
    Prepared,
    ReadyForExecutionProvider,
}

internal sealed record GenerationProviderExecutionPlanMetadata(
    [property: JsonPropertyName("executionPlanId")] string ExecutionPlanId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion);

internal sealed record GenerationProviderExecutionPlanReferences(
    [property: JsonPropertyName("generationProviderRequestRef")] string GenerationProviderRequestRef,
    [property: JsonPropertyName("pbirGenerationSpecificationRef")] string PbirGenerationSpecificationRef,
    [property: JsonPropertyName("planningOutcomeRef")] string PlanningOutcomeRef);

internal sealed record GenerationProviderExecutionStage(
    [property: JsonPropertyName("stageId")] string StageId,
    [property: JsonPropertyName("stageName")] string StageName,
    [property: JsonPropertyName("sequence")] int Sequence,
    [property: JsonPropertyName("requiredDependencyIds")] IReadOnlyList<string> RequiredDependencyIds);

internal sealed record GenerationProviderExecutionConstraints(
    [property: JsonPropertyName("dryRunOnly")] bool DryRunOnly,
    [property: JsonPropertyName("mockExecutionPermitted")] bool MockExecutionPermitted,
    [property: JsonPropertyName("deploymentProhibited")] bool DeploymentProhibited,
    [property: JsonPropertyName("providerInvocationProhibited")] bool ProviderInvocationProhibited,
    [property: JsonPropertyName("apiInvocationProhibited")] bool ApiInvocationProhibited,
    [property: JsonPropertyName("cliInvocationProhibited")] bool CliInvocationProhibited,
    [property: JsonPropertyName("reportMutationProhibited")] bool ReportMutationProhibited);

internal sealed record GenerationProviderExecutionApprovalDependencies(
    [property: JsonPropertyName("designApprovalRequired")] bool DesignApprovalRequired,
    [property: JsonPropertyName("generationApprovalRequired")] bool GenerationApprovalRequired,
    [property: JsonPropertyName("analyzerReviewRequired")] bool AnalyzerReviewRequired,
    [property: JsonPropertyName("designApproved")] bool DesignApproved,
    [property: JsonPropertyName("generationApproved")] bool GenerationApproved);

internal sealed record GenerationProviderExecutionProviderDependency(
    [property: JsonPropertyName("currentReadiness")] GenerationProviderReadinessState CurrentReadiness,
    [property: JsonPropertyName("requiredReadiness")] GenerationProviderReadinessState RequiredReadiness);

internal sealed record GenerationProviderExecutionRuntimeDependency(
    [property: JsonPropertyName("currentReadiness")] PlanningReadinessStatus CurrentReadiness,
    [property: JsonPropertyName("requiredReadiness")] PlanningReadinessStatus RequiredReadiness);

internal sealed record GenerationProviderExecutionSpecificationDependency(
    [property: JsonPropertyName("currentReadiness")] PbirGenerationSpecificationReadinessState CurrentReadiness,
    [property: JsonPropertyName("requiredReadiness")] PbirGenerationSpecificationReadinessState RequiredReadiness);

internal sealed record GenerationProviderExecutionDependencies(
    [property: JsonPropertyName("requiredApprovals")] GenerationProviderExecutionApprovalDependencies RequiredApprovals,
    [property: JsonPropertyName("providerReadiness")] GenerationProviderExecutionProviderDependency ProviderReadiness,
    [property: JsonPropertyName("runtimeReadiness")] GenerationProviderExecutionRuntimeDependency RuntimeReadiness,
    [property: JsonPropertyName("specificationCompleteness")] GenerationProviderExecutionSpecificationDependency SpecificationCompleteness);

internal sealed record GenerationProviderExecutionPlan(
    [property: JsonPropertyName("metadata")] GenerationProviderExecutionPlanMetadata Metadata,
    [property: JsonPropertyName("references")] GenerationProviderExecutionPlanReferences References,
    [property: JsonPropertyName("executionStages")] IReadOnlyList<GenerationProviderExecutionStage> ExecutionStages,
    [property: JsonPropertyName("executionConstraints")] GenerationProviderExecutionConstraints ExecutionConstraints,
    [property: JsonPropertyName("executionDependencies")] GenerationProviderExecutionDependencies ExecutionDependencies);

internal sealed record GenerationProviderExecutionPlanValidationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> InvalidReferences,
    IReadOnlyList<string> StageOrderingFailures,
    IReadOnlyList<string> ReadinessCompatibilityFailures,
    IReadOnlyList<string> ProviderCompatibilityFailures,
    IReadOnlyList<string> UnsupportedSchemaVersions,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static GenerationProviderExecutionPlanValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], []);

    internal bool HasBlockingFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        InvalidReferences.Count > 0 ||
        StageOrderingFailures.Count > 0 ||
        UnsupportedSchemaVersions.Count > 0 ||
        BoundaryViolations.Count > 0;

    internal bool HasCompatibilityFailures =>
        ReadinessCompatibilityFailures.Count > 0 ||
        ProviderCompatibilityFailures.Count > 0;
}

internal sealed record GenerationProviderExecutionPlanValidationResult(
    GenerationProviderExecutionPlanValidationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasBlockingFailures && !Diagnostics.HasCompatibilityFailures;
}

internal sealed record GenerationProviderExecutionPlanningState(
    GenerationProviderExecutionPlan? Plan,
    GenerationProviderExecutionPlanValidationResult Validation,
    GenerationProviderExecutionPlanReadinessState Readiness);
