using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir.CrossPageNarrative;

internal static class CrossPageNarrativeAssessmentBuilder
{
    public static CrossPageNarrativeAssessment? Build(IReadOnlyList<PageScore>? pageScores)
    {
        if (pageScores is not { Count: > 1 })
        {
            return null;
        }

        var input = CrossPageNarrativeInputBuilder.Build(pageScores);
        var roleAssignments = input.Pages.ToDictionary(
            page => page.PageName,
            page => PageNarrativeRoleClassifier.Classify(page, input.Pages.Count),
            StringComparer.Ordinal);
        var graph = CrossPageNarrativeGraphBuilder.Build(input, roleAssignments);
        var consistencyDimensions = CrossPageNarrativeConsistencyEvaluator.Evaluate(input, roleAssignments, graph);
        var orphanStates = CrossPageNarrativeOrphanEvaluator.Evaluate(input, roleAssignments, graph);
        var navigationDimensions = CrossPageNarrativeNavigationEvaluator.Evaluate(
            input,
            graph,
            orphanStates.ToDictionary(entry => entry.Key, entry => entry.Value.ToString(), StringComparer.Ordinal));
        var dimensions = consistencyDimensions
            .Concat(navigationDimensions)
            .ToList();
        var dominantObjective = InferDominantReportObjective(roleAssignments.Values);
        var scoreSummary = CrossPageNarrativeScorer.Score(dimensions, dominantObjective);
        var gaps = CrossPageNarrativeGapBuilder.Build(
            input.Pages,
            roleAssignments,
            orphanStates.ToDictionary(entry => entry.Key, entry => entry.Value.ToString(), StringComparer.Ordinal),
            scoreSummary.CompositeScore);

        return new CrossPageNarrativeAssessment
        {
            DominantReportObjective = dominantObjective,
            PromotionState = StoryAssessmentPromotionState.Internal,
            SurfaceScope = StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
            Graph = graph,
            Pages = input.Pages.Select(page => new CrossPageNarrativePageAssessment
            {
                PageId = page.PageId,
                PageName = page.PageName,
                RoleAssignment = roleAssignments[page.PageName],
                OrphanState = orphanStates.TryGetValue(page.PageId, out var orphanState)
                    ? orphanState
                    : CrossPageNarrativeOrphanState.Connected,
                Evidence = roleAssignments[page.PageName].Evidence,
                RelatedPageIds = graph.Edges
                    .Where(edge => edge.SourcePageId == page.PageId)
                    .Select(edge => edge.TargetPageId)
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
            }).ToList(),
            ScoreSummary = scoreSummary,
            Gaps = gaps,
        };
    }

    private static string InferDominantReportObjective(IEnumerable<CrossPageNarrativeRoleAssignment> assignments)
    {
        var dominantRole = assignments
            .GroupBy(assignment => assignment.PrimaryRole)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();

        return dominantRole switch
        {
            CrossPageNarrativeRoleId.Overview or CrossPageNarrativeRoleId.ExecutiveSummary => "executive performance review",
            CrossPageNarrativeRoleId.OperationalMonitor => "operational monitoring",
            CrossPageNarrativeRoleId.ComparativeAnalysis => "comparative business analysis",
            CrossPageNarrativeRoleId.DiagnosticInvestigation or CrossPageNarrativeRoleId.DetailDrill => "diagnostic investigation",
            _ => "general analytical review",
        };
    }
}
