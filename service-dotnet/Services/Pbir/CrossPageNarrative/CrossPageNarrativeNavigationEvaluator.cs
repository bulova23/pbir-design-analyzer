using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir.CrossPageNarrative;

internal static class CrossPageNarrativeNavigationEvaluator
{
    public static IReadOnlyList<CrossPageNarrativeDimensionScore> Evaluate(
        CrossPageNarrativeReportInput input,
        CrossPageNarrativeGraph graph,
        IReadOnlyDictionary<string, string> orphanStates)
    {
        var navigationScore = 55d;
        var actionabilityScore = 55d;
        var navigationEvidence = new List<string>();
        var actionabilityEvidence = new List<string>();
        var navigationWeakening = new List<string>();
        var actionabilityWeakening = new List<string>();

        if (graph.Edges.Any(edge => edge.EdgeType == CrossPageNarrativeEdgeType.Drillthrough))
        {
            navigationScore += 25d;
            actionabilityScore += 20d;
            navigationEvidence.Add("explicit drillthrough path present");
            actionabilityEvidence.Add("explicit drillthrough path supports investigation");
        }

        var disconnectedCount = orphanStates.Values.Count(state =>
            state is "UnusedDrillTarget" or "OrphanedPage" or "UnreachablePage" or "IsolatedAnalysisIsland");

        if (disconnectedCount > 0)
        {
            navigationScore -= disconnectedCount * 20d;
            actionabilityScore -= disconnectedCount * 15d;
            navigationWeakening.Add("orphaned or unreachable pages weaken report navigation");
            actionabilityWeakening.Add("disconnected detail pages break actionability");
        }

        return
        [
            CreateDimension(CrossPageNarrativeDimensionId.Navigation, navigationScore, navigationEvidence, navigationWeakening),
            CreateDimension(CrossPageNarrativeDimensionId.Actionability, actionabilityScore, actionabilityEvidence, actionabilityWeakening),
        ];
    }

    private static CrossPageNarrativeDimensionScore CreateDimension(
        CrossPageNarrativeDimensionId dimensionId,
        double score,
        IReadOnlyList<string> strongestEvidence,
        IReadOnlyList<string> weakeningEvidence)
    {
        return new CrossPageNarrativeDimensionScore
        {
            DimensionId = dimensionId,
            Score = Math.Clamp(score, 0d, 100d),
            Confidence = CrossPageNarrativeAssessmentConfidence.Medium,
            StrongestEvidence = strongestEvidence,
            WeakeningEvidence = weakeningEvidence,
            MissingEvidence = [],
            AffectedPageIds = [],
        };
    }
}
