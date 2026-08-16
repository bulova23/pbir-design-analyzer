namespace PowerBIModelingService.Services.Discovery.Models;

internal enum DesignPackageConsumptionRequirement
{
    Required,
    Optional,
}

internal enum DesignPackageConsumptionHandling
{
    Direct,
    Transformed,
    Ignored,
}

internal enum GenerationArtifactType
{
    PbirReport,
    FabricDataApp,
    FabricApp,
}

internal sealed record DesignPackageFieldConsumptionMetadata(
    string FieldPath,
    DesignPackageConsumptionRequirement Requirement,
    DesignPackageConsumptionHandling Handling,
    string Notes);

internal sealed record ConsumedDesignPackageView(
    string SourceDesignPackageRef,
    DesignPackageDiscoveryContext DiscoveryContext,
    string PrimaryAudience,
    IReadOnlyList<string> SecondaryAudiences,
    IReadOnlyList<DesignPackagePersona> Personas,
    OpportunityExperienceType ExperienceType,
    string BusinessOutcome,
    IReadOnlyList<DesignPackagePage> Pages,
    IReadOnlyList<DesignPackageKpi> Kpis,
    DesignPackageFilterSet Filters,
    IReadOnlyList<DesignPackageVisualRecommendation> VisualRecommendations,
    DesignPackageNavigation Navigation,
    DesignPackageAnalyticalFlow AnalyticalFlow,
    DesignPackageSuccessCriteria SuccessCriteria,
    DesignPackageRecommendationRationale? RecommendationRationale,
    DesignPackageProviderGuidance? ProviderGuidance,
    DesignPackageProvenance Provenance,
    IReadOnlyList<string> IgnoredFieldPaths);

internal sealed record NormalizedGenerationPageOrRoute(
    string Name,
    string Purpose,
    string NavigationIntent);

internal sealed record NormalizedGenerationKpi(
    string Name,
    string Purpose,
    string Grouping);

internal sealed record NormalizedGenerationPageFilter(
    string PageName,
    IReadOnlyList<string> Filters);

internal sealed record NormalizedGenerationFilters(
    IReadOnlyList<string> GlobalFilters,
    IReadOnlyList<NormalizedGenerationPageFilter> PageFilters);

internal sealed record NormalizedGenerationVisualHint(
    string PageName,
    string VisualType,
    string VisualPurpose);

internal sealed record NormalizedAnalyticalFlow(
    string Question,
    string Investigation,
    string Evidence,
    string Decision);

internal sealed record NormalizedSuccessContract(
    IReadOnlyList<string> BusinessSuccessCriteria,
    IReadOnlyList<string> AnalyticalSuccessCriteria,
    bool ReviewRequired,
    bool ValidationRequired);

internal sealed record NormalizedGenerationInput(
    string SourceDesignPackageRef,
    GenerationArtifactType TargetArtifactType,
    OpportunityExperienceType SourceExperienceType,
    string PrimaryAudience,
    IReadOnlyList<string> SecondaryAudiences,
    string BusinessOutcome,
    IReadOnlyList<NormalizedGenerationPageOrRoute> PagesOrRoutes,
    IReadOnlyList<string> NavigationHierarchy,
    IReadOnlyList<string> WorkflowPath,
    IReadOnlyList<NormalizedGenerationKpi> Kpis,
    NormalizedGenerationFilters Filters,
    IReadOnlyList<NormalizedGenerationVisualHint> VisualHints,
    NormalizedAnalyticalFlow AnalyticalFlow,
    NormalizedSuccessContract SuccessContract,
    IReadOnlyList<DesignPackageReference> Lineage);

internal sealed record DesignPackageConsumptionDiagnostics(
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> UnsupportedExperienceTypes,
    IReadOnlyList<string> IncompatiblePackageStates)
{
    internal bool HasFailures =>
        MissingRequiredFields.Count > 0 ||
        UnsupportedExperienceTypes.Count > 0 ||
        IncompatiblePackageStates.Count > 0;
}

internal sealed record DesignPackageConsumptionResult(
    ConsumedDesignPackageView? ConsumedPackage,
    NormalizedGenerationInput? NormalizedGenerationInput,
    DesignPackageConsumptionDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasFailures;
}
