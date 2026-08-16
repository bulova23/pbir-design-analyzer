using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirGenerationSpecificationContract
{
    internal const string SchemaVersionV1 = "pbir-generation-specification/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "SpecificationId",
        "SourceReferences",
        "SourceReferences.DesignPackageRef",
        "SourceReferences.GenerationRequestRef",
        "SourceReferences.PlanningOutcomeRef",
        "SourceReferences.Lineage",
        "DesignReferences",
        "DesignReferences.DesignPackageReference",
        "DesignReferences.GenerationRequestReference",
        "DesignReferences.PlanningOutcomeReference",
        "ArtifactSpecifications",
        "ArtifactSpecifications.SchemaVersion",
        "ArtifactSpecifications.ArtifactSpecificationId",
        "ArtifactSpecifications.TargetProfileId",
        "ArtifactSpecifications.PageSpecifications",
        "ArtifactSpecifications.VisualSpecifications",
        "ArtifactSpecifications.SemanticSpecifications",
        "ArtifactSpecifications.NavigationSpecifications",
        "ArtifactSpecifications.SuccessCriteria",
    ];
}

internal static class PbirArtifactSpecificationContract
{
    internal const string SchemaVersionV1 = "pbir-artifact-specification/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ArtifactSpecificationId",
        "TargetProfileId",
        "DesignReferences",
        "DesignReferences.DesignPackageReference",
        "DesignReferences.GenerationRequestReference",
        "DesignReferences.PlanningOutcomeReference",
        "PageSpecifications",
        "PageSpecifications.PageId",
        "PageSpecifications.Purpose",
        "PageSpecifications.Audience",
        "PageSpecifications.NavigationBehavior",
        "VisualSpecifications",
        "VisualSpecifications.PageId",
        "VisualSpecifications.VisualType",
        "VisualSpecifications.Placement",
        "VisualSpecifications.IntendedKpi",
        "VisualSpecifications.IntendedDimensions",
        "VisualSpecifications.IntendedInteractions",
        "SemanticSpecifications",
        "SemanticSpecifications.PageId",
        "SemanticSpecifications.KpiBinding",
        "SemanticSpecifications.FilterBindings",
        "SemanticSpecifications.DrillBehavior",
        "SemanticSpecifications.IntendedMeasures",
        "NavigationSpecifications",
        "NavigationSpecifications.LandingPage",
        "NavigationSpecifications.PageTransitions",
        "NavigationSpecifications.DrillPaths",
        "SuccessCriteria",
        "SuccessCriteria.BusinessSuccessCriteria",
        "SuccessCriteria.AnalyticalSuccessCriteria",
        "SuccessCriteria.PlanningOutcomeRequirements",
    ];
}

internal sealed record PbirGenerationSpecification(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("specificationId")] string SpecificationId,
    [property: JsonPropertyName("sourceReferences")] PbirGenerationSpecificationSourceReferences SourceReferences,
    [property: JsonPropertyName("designReferences")] PbirGenerationSpecificationDesignReferences DesignReferences,
    [property: JsonPropertyName("artifactSpecifications")] IReadOnlyList<PbirArtifactSpecification> ArtifactSpecifications);

internal sealed record PbirGenerationSpecificationSourceReferences(
    [property: JsonPropertyName("designPackageRef")] string DesignPackageRef,
    [property: JsonPropertyName("generationRequestRef")] string GenerationRequestRef,
    [property: JsonPropertyName("planningOutcomeRef")] string PlanningOutcomeRef,
    [property: JsonPropertyName("lineage")] IReadOnlyList<PlanningLineageEntry> Lineage);

internal sealed record PbirGenerationSpecificationDesignReferences(
    [property: JsonPropertyName("designPackageReference")] string DesignPackageReference,
    [property: JsonPropertyName("generationRequestReference")] string GenerationRequestReference,
    [property: JsonPropertyName("planningOutcomeReference")] string PlanningOutcomeReference);

internal sealed record PbirArtifactSpecification(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("artifactSpecificationId")] string ArtifactSpecificationId,
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("designReferences")] PbirGenerationSpecificationDesignReferences DesignReferences,
    [property: JsonPropertyName("pageSpecifications")] IReadOnlyList<PbirPageSpecification> PageSpecifications,
    [property: JsonPropertyName("visualSpecifications")] IReadOnlyList<PbirVisualSpecification> VisualSpecifications,
    [property: JsonPropertyName("semanticSpecifications")] IReadOnlyList<PbirSemanticSpecification> SemanticSpecifications,
    [property: JsonPropertyName("navigationSpecifications")] PbirNavigationSpecification NavigationSpecifications,
    [property: JsonPropertyName("successCriteria")] PbirArtifactSuccessCriteria SuccessCriteria);

internal sealed record PbirPageSpecification(
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("audience")] string Audience,
    [property: JsonPropertyName("navigationBehavior")] string NavigationBehavior);

internal sealed record PbirVisualSpecification(
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("visualType")] string VisualType,
    [property: JsonPropertyName("placement")] string Placement,
    [property: JsonPropertyName("intendedKpi")] string IntendedKpi,
    [property: JsonPropertyName("intendedDimensions")] string IntendedDimensions,
    [property: JsonPropertyName("intendedInteractions")] IReadOnlyList<string> IntendedInteractions);

internal sealed record PbirSemanticSpecification(
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("kpiBinding")] string KpiBinding,
    [property: JsonPropertyName("filterBindings")] IReadOnlyList<string> FilterBindings,
    [property: JsonPropertyName("drillBehavior")] string DrillBehavior,
    [property: JsonPropertyName("intendedMeasures")] IReadOnlyList<string> IntendedMeasures);

internal sealed record PbirNavigationSpecification(
    [property: JsonPropertyName("landingPage")] string LandingPage,
    [property: JsonPropertyName("pageTransitions")] IReadOnlyList<string> PageTransitions,
    [property: JsonPropertyName("drillPaths")] IReadOnlyList<string> DrillPaths);

internal sealed record PbirArtifactSuccessCriteria(
    [property: JsonPropertyName("businessSuccessCriteria")] IReadOnlyList<string> BusinessSuccessCriteria,
    [property: JsonPropertyName("analyticalSuccessCriteria")] IReadOnlyList<string> AnalyticalSuccessCriteria,
    [property: JsonPropertyName("planningOutcomeRequirements")] IReadOnlyList<string> PlanningOutcomeRequirements);

internal enum PbirGenerationSpecificationReadinessState
{
    Incomplete,
    PartiallySpecified,
    Specified,
    ReadyForGenerationProvider,
}

internal sealed record PbirGenerationSpecificationValidationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> MissingDesignIntent,
    IReadOnlyList<string> InvalidPageDefinitions,
    IReadOnlyList<string> InvalidVisualDefinitions,
    IReadOnlyList<string> InvalidSemanticDefinitions,
    IReadOnlyList<string> InvalidNavigationDefinitions,
    IReadOnlyList<string> IncompleteSuccessCriteria,
    IReadOnlyList<string> UnsupportedSchemaVersions,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static PbirGenerationSpecificationValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], [], [], []);

    internal bool HasFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        MissingDesignIntent.Count > 0 ||
        InvalidPageDefinitions.Count > 0 ||
        InvalidVisualDefinitions.Count > 0 ||
        InvalidSemanticDefinitions.Count > 0 ||
        InvalidNavigationDefinitions.Count > 0 ||
        IncompleteSuccessCriteria.Count > 0 ||
        UnsupportedSchemaVersions.Count > 0 ||
        BoundaryViolations.Count > 0;
}

internal sealed record PbirGenerationSpecificationValidationResult(
    PbirGenerationSpecificationValidationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasFailures;
}

internal sealed record PbirGenerationSpecificationState(
    PbirGenerationSpecification? Specification,
    PbirGenerationSpecificationValidationDiagnostics Diagnostics,
    PbirGenerationSpecificationReadinessState Readiness,
    bool AcceptsGenerationProvider);
