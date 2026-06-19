namespace PowerBIModelingService.Services.Discovery.Models;

internal sealed record DesignPackageReference(
    string Stage,
    string ReferenceId,
    string Label);

internal sealed record DesignPackageDiscoveryContext(
    DesignPackageReference SemanticModelSource,
    DesignPackageReference DiscoveryProfileReference,
    DesignPackageReference OpportunityReference,
    DesignPackageReference RecommendationReference,
    DesignPackageReference ExperienceBlueprintReference);

internal sealed record DesignPackagePersona(
    string Name,
    string Role,
    string Perspective);

internal sealed record DesignPackageAudience(
    string PrimaryAudience,
    IReadOnlyList<string> SecondaryAudiences,
    IReadOnlyList<DesignPackagePersona> Personas);

internal sealed record DesignPackageExperienceDefinition(
    OpportunityExperienceType ExperienceType,
    string BusinessOutcome,
    DiscoveryConfidenceLevel Confidence,
    RecommendationBusinessValueLevel BusinessValue,
    RecommendationComplexityLevel Complexity);

internal sealed record DesignPackagePage(
    string PageName,
    string PagePurpose,
    string NavigationIntent);

internal sealed record DesignPackageKpi(
    string Name,
    string Purpose,
    string Grouping);

internal sealed record DesignPackagePageFilter(
    string PageName,
    IReadOnlyList<string> Filters);

internal sealed record DesignPackageFilterSet(
    IReadOnlyList<string> GlobalFilters,
    IReadOnlyList<DesignPackagePageFilter> PageFilters);

internal sealed record DesignPackageVisualRecommendation(
    string PageName,
    string VisualType,
    string VisualPurpose);

internal sealed record DesignPackageNavigation(
    IReadOnlyList<string> Hierarchy,
    IReadOnlyList<string> WorkflowPath);

internal sealed record DesignPackageAnalyticalFlow(
    string Question,
    string Investigation,
    string Evidence,
    string Decision);

internal sealed record DesignPackageSuccessCriteria(
    IReadOnlyList<string> BusinessSuccessCriteria,
    IReadOnlyList<string> AnalyticalSuccessCriteria);

internal sealed record DesignPackageRecommendationRationale(
    string RecommendationExplanation,
    IReadOnlyList<string> SupportingSemanticSignals,
    IReadOnlyList<string> LimitingFactors,
    string AudienceRationale,
    string BusinessOutcomeRationale,
    IReadOnlyList<string> KpiRationale,
    IReadOnlyList<string> PageRationale,
    string NavigationRationale,
    string AnalyticalFlowRationale,
    IReadOnlyList<string> ProvenanceNotes);

internal sealed record DesignPackageProvenance(
    string PackageReference,
    IReadOnlyList<DesignPackageReference> Lineage);

internal sealed record DesignPackage(
    string PackageId,
    DesignPackageDiscoveryContext DiscoveryContext,
    DesignPackageAudience Audience,
    DesignPackageExperienceDefinition ExperienceDefinition,
    IReadOnlyList<DesignPackagePage> Pages,
    IReadOnlyList<DesignPackageKpi> Kpis,
    DesignPackageFilterSet Filters,
    IReadOnlyList<DesignPackageVisualRecommendation> VisualRecommendations,
    DesignPackageNavigation Navigation,
    DesignPackageAnalyticalFlow AnalyticalFlow,
    DesignPackageSuccessCriteria SuccessCriteria,
    DesignPackageRecommendationRationale RecommendationRationale,
    DesignPackageProvenance Provenance);
