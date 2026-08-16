namespace PowerBIModelingService.Services.Discovery.Models;

internal sealed record ExperienceBlueprintPage(
    string PageName,
    string PageIntent,
    IReadOnlyList<string> SuggestedFilters,
    IReadOnlyList<string> SuggestedVisualTypes);

internal sealed record ExperienceBlueprintAnalyticalFlow(
    string Question,
    string Investigation,
    string Evidence,
    string Decision);

internal sealed record ExperienceBlueprintNavigationIntent(
    string Flow,
    IReadOnlyList<string> Sequence);

internal sealed record ExperienceBlueprintProvenance(
    string RecommendationId,
    string OpportunityId,
    OpportunityCategory OpportunityCategory,
    OpportunityExperienceType ExperienceType,
    DiscoveryConfidenceLevel DiscoveryConfidence,
    IReadOnlyList<string> SupportingSignals,
    IReadOnlyList<string> SemanticEvidenceReferences,
    IReadOnlyList<string> InfluencingModelStructures,
    IReadOnlyList<string> AmbiguityNotes,
    string SemanticModelReferenceId,
    string DiscoveryProfileReferenceId);

internal sealed record ExperienceBlueprint(
    string BlueprintId,
    OpportunityExperienceType ExperienceType,
    IReadOnlyList<ExperienceBlueprintPage> RecommendedPages,
    IReadOnlyList<string> PrimaryKpis,
    IReadOnlyList<string> SuggestedGlobalFilters,
    ExperienceBlueprintAnalyticalFlow AnalyticalFlow,
    ExperienceBlueprintNavigationIntent NavigationIntent,
    string ExpectedAudience,
    string ExpectedBusinessOutcome,
    IReadOnlyList<string> SuccessCriteriaSeed,
    ExperienceBlueprintProvenance Provenance);
