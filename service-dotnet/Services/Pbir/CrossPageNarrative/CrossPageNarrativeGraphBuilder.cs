using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir.CrossPageNarrative;

internal static class CrossPageNarrativeGraphBuilder
{
    public static CrossPageNarrativeGraph Build(
        CrossPageNarrativeReportInput input,
        IReadOnlyDictionary<string, CrossPageNarrativeRoleAssignment> roleAssignments)
    {
        var pages = input.Pages
            .OrderBy(page => page.PageIndex)
            .ToList();
        var edges = new List<CrossPageNarrativeEdge>();

        for (var index = 0; index < pages.Count - 1; index++)
        {
            var current = pages[index];
            var next = pages[index + 1];

            edges.Add(CreateEdge(current.PageId, next.PageId, CrossPageNarrativeEdgeType.OrderedNext, CrossPageNarrativeEdgeObservationKind.Observed, 1.0d, "pageOrder"));
            edges.Add(CreateEdge(next.PageId, current.PageId, CrossPageNarrativeEdgeType.OrderedPrevious, CrossPageNarrativeEdgeObservationKind.Observed, 1.0d, "pageOrder"));

            if (roleAssignments.TryGetValue(current.PageName, out var currentRole) &&
                roleAssignments.TryGetValue(next.PageName, out var nextRole) &&
                IsSummaryToDetailTransition(currentRole.PrimaryRole, nextRole.PrimaryRole))
            {
                edges.Add(CreateEdge(current.PageId, next.PageId, CrossPageNarrativeEdgeType.SummaryToDetail, CrossPageNarrativeEdgeObservationKind.Inferred, 0.7d, "roleCompatibility"));
            }
        }

        edges.AddRange(input.ExplicitEdges);

        var segments = BuildSegments(pages, roleAssignments);
        var mainPath = segments.OrderByDescending(segment => segment.Count).FirstOrDefault() ?? [];

        return new CrossPageNarrativeGraph
        {
            PageIds = pages.Select(page => page.PageId).ToList(),
            Edges = edges,
            Segments = segments,
            MainNarrativePath = mainPath,
        };
    }

    private static List<IReadOnlyList<string>> BuildSegments(
        IReadOnlyList<CrossPageNarrativePageInput> pages,
        IReadOnlyDictionary<string, CrossPageNarrativeRoleAssignment> roleAssignments)
    {
        var segments = new List<IReadOnlyList<string>>();
        var current = new List<string>();

        foreach (var page in pages)
        {
            var role = roleAssignments.TryGetValue(page.PageName, out var assignment)
                ? assignment.PrimaryRole
                : CrossPageNarrativeRoleId.SupportingContext;

            if (IsAppendixLike(role))
            {
                if (current.Count > 0)
                {
                    segments.Add(current.ToList());
                    current.Clear();
                }

                segments.Add([page.PageId]);
                continue;
            }

            current.Add(page.PageId);
        }

        if (current.Count > 0)
        {
            segments.Add(current);
        }

        return segments;
    }

    private static bool IsAppendixLike(CrossPageNarrativeRoleId role)
    {
        return role is CrossPageNarrativeRoleId.ReferenceLegal
            or CrossPageNarrativeRoleId.Tooltip
            or CrossPageNarrativeRoleId.Qna
            or CrossPageNarrativeRoleId.ValidationSandbox;
    }

    private static bool IsSummaryToDetailTransition(CrossPageNarrativeRoleId source, CrossPageNarrativeRoleId target)
    {
        return (source is CrossPageNarrativeRoleId.Overview
                or CrossPageNarrativeRoleId.ExecutiveSummary
                or CrossPageNarrativeRoleId.ComparativeAnalysis)
               && (target is CrossPageNarrativeRoleId.DetailDrill
                   or CrossPageNarrativeRoleId.DiagnosticInvestigation);
    }

    private static CrossPageNarrativeEdge CreateEdge(
        string sourcePageId,
        string targetPageId,
        CrossPageNarrativeEdgeType edgeType,
        CrossPageNarrativeEdgeObservationKind observationKind,
        double strength,
        string evidence)
    {
        return new CrossPageNarrativeEdge
        {
            SourcePageId = sourcePageId,
            TargetPageId = targetPageId,
            EdgeType = edgeType,
            ObservationKind = observationKind,
            Strength = strength,
            Evidence = [evidence],
        };
    }
}
