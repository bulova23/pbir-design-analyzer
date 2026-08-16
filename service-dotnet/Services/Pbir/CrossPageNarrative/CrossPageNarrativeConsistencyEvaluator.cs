using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir.CrossPageNarrative;

internal static class CrossPageNarrativeConsistencyEvaluator
{
    public static IReadOnlyList<CrossPageNarrativeDimensionScore> Evaluate(
        CrossPageNarrativeReportInput input,
        IReadOnlyDictionary<string, CrossPageNarrativeRoleAssignment> roleAssignments,
        CrossPageNarrativeGraph graph)
    {
        var pagesById = input.Pages.ToDictionary(page => page.PageId, StringComparer.Ordinal);
        var orderedEdges = graph.Edges
            .Where(edge => edge.EdgeType == CrossPageNarrativeEdgeType.OrderedNext)
            .ToList();

        var flowScore = 60d;
        var consistencyScore = 75d;
        var continuityScore = 70d;
        var flowEvidence = new List<string>();
        var consistencyEvidence = new List<string>();
        var consistencyWeakening = new List<string>();
        var continuityEvidence = new List<string>();
        var continuityWeakening = new List<string>();

        if (graph.Edges.Any(edge => edge.EdgeType == CrossPageNarrativeEdgeType.SummaryToDetail))
        {
            flowScore += 25d;
            continuityScore += 10d;
            flowEvidence.Add("summary-to-detail transition present");
            continuityEvidence.Add("summary-to-detail transition reinforces narrative depth");
        }

        foreach (var edge in orderedEdges)
        {
            if (!pagesById.TryGetValue(edge.SourcePageId, out var sourcePage) ||
                !pagesById.TryGetValue(edge.TargetPageId, out var targetPage))
            {
                continue;
            }

            if (SharesDominantTerms(sourcePage.InferredStory, targetPage.InferredStory))
            {
                consistencyScore += 10d;
                continuityScore += 8d;
                consistencyEvidence.Add($"shared topic continuity between {sourcePage.PageName} and {targetPage.PageName}");
                continuityEvidence.Add($"adjacent pages stay on the same business topic: {sourcePage.PageName} -> {targetPage.PageName}");
            }
            else
            {
                consistencyScore -= 20d;
                continuityScore -= 15d;
                consistencyWeakening.Add($"context shift between {sourcePage.PageName} and {targetPage.PageName}");
                continuityWeakening.Add($"adjacent pages break topic continuity: {sourcePage.PageName} -> {targetPage.PageName}");
            }
        }

        if (!orderedEdges.Any())
        {
            flowScore -= 20d;
            flowEvidence.Add("no ordered transitions available");
        }

        return
        [
            CreateDimension(CrossPageNarrativeDimensionId.Flow, flowScore, flowEvidence, [], []),
            CreateDimension(CrossPageNarrativeDimensionId.Consistency, consistencyScore, consistencyEvidence, consistencyWeakening, []),
            CreateDimension(CrossPageNarrativeDimensionId.Continuity, continuityScore, continuityEvidence, continuityWeakening, []),
        ];
    }

    private static bool SharesDominantTerms(string left, string right)
    {
        var leftTerms = Tokenize(left);
        var rightTerms = Tokenize(right);
        return leftTerms.Intersect(rightTerms, StringComparer.Ordinal).Any();
    }

    private static HashSet<string> Tokenize(string text)
    {
        return text.Split([' ', '-', '/', ',', '.', ':'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.ToLowerInvariant())
            .Where(token => token.Length >= 4)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static CrossPageNarrativeDimensionScore CreateDimension(
        CrossPageNarrativeDimensionId dimensionId,
        double score,
        IReadOnlyList<string> strongestEvidence,
        IReadOnlyList<string> weakeningEvidence,
        IReadOnlyList<string> affectedPageIds)
    {
        return new CrossPageNarrativeDimensionScore
        {
            DimensionId = dimensionId,
            Score = Math.Clamp(score, 0d, 100d),
            Confidence = CrossPageNarrativeAssessmentConfidence.Medium,
            StrongestEvidence = strongestEvidence,
            WeakeningEvidence = weakeningEvidence,
            MissingEvidence = [],
            AffectedPageIds = affectedPageIds,
        };
    }
}
