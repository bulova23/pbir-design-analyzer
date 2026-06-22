using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class ExecutionPlanContract
{
    internal const string SchemaVersionV1 = "execution-plan/v1";
    internal const string ProviderNeutralPlanningCategory = "providerNeutralPlanning";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ExecutionPlanId",
        "SourceReferences",
        "SourceReferences.GenerationRequestRef",
        "SourceReferences.SourceDesignPackageRef",
        "TargetDefinition",
        "TargetDefinition.TargetArtifactProfile",
        "TargetDefinition.TargetArtifactProfile.ArtifactType",
        "TargetDefinition.TargetArtifactProfile.ProfileId",
        "TargetDefinition.TargetArtifactProfile.SourceExperienceType",
        "TargetDefinition.ExperienceType",
        "ProviderPlanningMetadata",
        "ProviderPlanningMetadata.ProviderCategory",
        "ProviderPlanningMetadata.CapabilityModel",
        "ProviderPlanningMetadata.CapabilityModel.SupportsLayoutGeneration",
        "ProviderPlanningMetadata.CapabilityModel.SupportsSemanticGeneration",
        "ProviderPlanningMetadata.CapabilityModel.SupportsArtifactGeneration",
        "ProviderPlanningMetadata.CapabilityModel.SupportsValidation",
        "ProviderPlanningMetadata.SupportedCapabilities",
        "ProviderPlanningMetadata.UnsupportedCapabilities",
        "PlannedWorkUnits",
        "PlannedWorkUnits.WorkUnitId",
        "PlannedWorkUnits.Title",
        "PlannedWorkUnits.Objective",
        "DependencyGraph",
        "DependencyGraph.ExecutionOrder",
        "DependencyGraph.Dependencies",
        "DependencyGraph.Dependencies.WorkUnitId",
        "DependencyGraph.Dependencies.Prerequisites",
        "PlanningConstraints",
        "PlanningConstraints.UnsupportedTargets",
        "PlanningConstraints.UnsupportedCapabilities",
        "PlanningConstraints.ReviewRequirements",
        "PlanningConstraints.ValidationRequirements",
        "ReviewRequirements",
        "ReviewRequirements.DesignApprovalRequired",
        "ReviewRequirements.GenerationApprovalRequired",
        "ReviewRequirements.AnalyzerReviewRequired",
        "SuccessContract",
        "SuccessContract.BusinessSuccessCriteria",
        "SuccessContract.AnalyticalSuccessCriteria",
        "SuccessContract.ValidationRequirements",
    ];
}

internal sealed record ExecutionPlan(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("executionPlanId")] string ExecutionPlanId,
    [property: JsonPropertyName("sourceReferences")] ExecutionPlanSourceReferences SourceReferences,
    [property: JsonPropertyName("targetDefinition")] ExecutionPlanTargetDefinition TargetDefinition,
    [property: JsonPropertyName("providerPlanningMetadata")] ExecutionPlanProviderPlanningMetadata ProviderPlanningMetadata,
    [property: JsonPropertyName("plannedWorkUnits")] IReadOnlyList<ExecutionPlanWorkUnit> PlannedWorkUnits,
    [property: JsonPropertyName("dependencyGraph")] ExecutionPlanDependencyGraph DependencyGraph,
    [property: JsonPropertyName("planningConstraints")] ExecutionPlanPlanningConstraints PlanningConstraints,
    [property: JsonPropertyName("reviewRequirements")] ExecutionPlanReviewRequirements ReviewRequirements,
    [property: JsonPropertyName("successContract")] GenerationRequestSuccessContract SuccessContract);

internal sealed record ExecutionPlanSourceReferences(
    [property: JsonPropertyName("generationRequestRef")] string GenerationRequestRef,
    [property: JsonPropertyName("sourceDesignPackageRef")] string SourceDesignPackageRef);

internal sealed record ExecutionPlanTargetDefinition(
    [property: JsonPropertyName("targetArtifactProfile")] GenerationRequestTargetArtifactProfile TargetArtifactProfile,
    [property: JsonPropertyName("experienceType")] OpportunityExperienceType ExperienceType);

internal sealed record ExecutionPlanProviderPlanningMetadata(
    [property: JsonPropertyName("providerCategory")] string ProviderCategory,
    [property: JsonPropertyName("capabilityModel")] ExecutionPlanProviderCapabilityModel CapabilityModel,
    [property: JsonPropertyName("supportedCapabilities")] IReadOnlyList<string> SupportedCapabilities,
    [property: JsonPropertyName("unsupportedCapabilities")] IReadOnlyList<string> UnsupportedCapabilities);

internal sealed record ExecutionPlanProviderCapabilityModel(
    [property: JsonPropertyName("supportsLayoutGeneration")] bool SupportsLayoutGeneration,
    [property: JsonPropertyName("supportsSemanticGeneration")] bool SupportsSemanticGeneration,
    [property: JsonPropertyName("supportsArtifactGeneration")] bool SupportsArtifactGeneration,
    [property: JsonPropertyName("supportsValidation")] bool SupportsValidation);

internal sealed record ExecutionPlanWorkUnit(
    [property: JsonPropertyName("workUnitId")] string WorkUnitId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("objective")] string Objective);

internal sealed record ExecutionPlanDependencyGraph(
    [property: JsonPropertyName("executionOrder")] IReadOnlyList<string> ExecutionOrder,
    [property: JsonPropertyName("dependencies")] IReadOnlyList<ExecutionPlanDependency> Dependencies);

internal sealed record ExecutionPlanDependency(
    [property: JsonPropertyName("workUnitId")] string WorkUnitId,
    [property: JsonPropertyName("prerequisites")] IReadOnlyList<string> Prerequisites);

internal sealed record ExecutionPlanPlanningConstraints(
    [property: JsonPropertyName("unsupportedTargets")] IReadOnlyList<string> UnsupportedTargets,
    [property: JsonPropertyName("unsupportedCapabilities")] IReadOnlyList<string> UnsupportedCapabilities,
    [property: JsonPropertyName("reviewRequirements")] IReadOnlyList<string> ReviewRequirements,
    [property: JsonPropertyName("validationRequirements")] IReadOnlyList<string> ValidationRequirements);

internal sealed record ExecutionPlanReviewRequirements(
    [property: JsonPropertyName("designApprovalRequired")] bool DesignApprovalRequired,
    [property: JsonPropertyName("generationApprovalRequired")] bool GenerationApprovalRequired,
    [property: JsonPropertyName("analyzerReviewRequired")] bool AnalyzerReviewRequired);

internal enum ExecutionPlanReadinessState
{
    Draft,
    Valid,
    Blocked,
    ReadyForProviderAdapter,
}

internal sealed record ExecutionPlanValidationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> UnsupportedTargetProfiles,
    IReadOnlyList<string> UnsupportedSchemaVersions,
    IReadOnlyList<string> DependencyFailures,
    IReadOnlyList<string> CapabilityInconsistencies,
    IReadOnlyList<string> TargetCompatibilityFailures,
    IReadOnlyList<string> ReviewRequirementFailures)
{
    internal static ExecutionPlanValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], []);

    internal bool HasFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        UnsupportedTargetProfiles.Count > 0 ||
        UnsupportedSchemaVersions.Count > 0 ||
        DependencyFailures.Count > 0 ||
        CapabilityInconsistencies.Count > 0 ||
        TargetCompatibilityFailures.Count > 0 ||
        ReviewRequirementFailures.Count > 0;
}

internal sealed record ExecutionPlanValidationResult(
    ExecutionPlanValidationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasFailures;
}

internal sealed record ExecutionPlanCreationResult(
    ExecutionPlan? Plan,
    ExecutionPlanValidationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasFailures;
}

internal sealed record ExecutionPlanFrameworkState(
    GenerationRequest? Request,
    ExecutionPlan? Plan,
    ExecutionPlanReadinessState Readiness,
    ExecutionPlanValidationDiagnostics Diagnostics);
