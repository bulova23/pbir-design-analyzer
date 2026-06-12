using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir.CrossPageNarrative;

internal static class CrossPageNarrativeScorer
{
    public static CrossPageNarrativeScoreSummary Score(
        IReadOnlyList<CrossPageNarrativeDimensionScore> dimensions,
        string dominantNarrativeSummary)
    {
        var scores = dimensions.ToDictionary(dimension => dimension.DimensionId, dimension => dimension);
        var compositeScore =
            GetScore(scores, CrossPageNarrativeDimensionId.Flow) * 0.25d +
            GetScore(scores, CrossPageNarrativeDimensionId.Consistency) * 0.20d +
            GetScore(scores, CrossPageNarrativeDimensionId.Navigation) * 0.20d +
            GetScore(scores, CrossPageNarrativeDimensionId.Continuity) * 0.20d +
            GetScore(scores, CrossPageNarrativeDimensionId.Actionability) * 0.15d;

        var confidence = dimensions.Any(dimension => dimension.Confidence == CrossPageNarrativeAssessmentConfidence.Low)
            ? CrossPageNarrativeAssessmentConfidence.Low
            : dimensions.Any(dimension => dimension.Confidence == CrossPageNarrativeAssessmentConfidence.Medium)
                ? CrossPageNarrativeAssessmentConfidence.Medium
                : CrossPageNarrativeAssessmentConfidence.High;

        return new CrossPageNarrativeScoreSummary
        {
            CompositeScore = Math.Round(compositeScore, 1),
            Confidence = confidence,
            PromotionState = StoryAssessmentPromotionState.Internal,
            SurfaceScope = StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
            Dimensions = dimensions,
            DominantNarrativeSummary = dominantNarrativeSummary,
        };
    }

    private static double GetScore(
        IReadOnlyDictionary<CrossPageNarrativeDimensionId, CrossPageNarrativeDimensionScore> scores,
        CrossPageNarrativeDimensionId dimensionId)
    {
        return scores.TryGetValue(dimensionId, out var score) ? score.Score : 0d;
    }
}
