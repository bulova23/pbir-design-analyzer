namespace PowerBIModelingService.Services.Discovery.Models;

internal enum RecommendationBusinessValueLevel
{
    Low,
    Medium,
    High,
}

internal enum RecommendationComplexityLevel
{
    Low,
    Medium,
    High,
}

internal enum RecommendationPlacement
{
    Primary,
    Alternate,
}

internal sealed record DiscoveryRecommendation(
    string RecommendationId,
    string RecommendationName,
    OpportunityExperienceType RecommendedExperienceType,
    DiscoveryConfidenceLevel Confidence,
    RecommendationBusinessValueLevel BusinessValue,
    RecommendationComplexityLevel ImplementationComplexity,
    string WhyWeRecommendIt,
    string ExpectedAudience,
    string ExpectedBusinessOutcome,
    IReadOnlyList<string> SupportingSignals,
    IReadOnlyList<string> LimitingFactors,
    string ConfidenceNote,
    string ComplexityNote,
    RecommendationPlacement Placement,
    double RankingScore,
    ExperienceBlueprint? ExperienceBlueprint = null);

internal sealed record RecommendationSet(
    IReadOnlyList<DiscoveryRecommendation> PrimaryRecommendations,
    IReadOnlyList<DiscoveryRecommendation> AlternateRecommendations);
