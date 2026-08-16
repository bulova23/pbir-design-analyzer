using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir.CrossPageNarrative;

internal static class CrossPageNarrativeOrphanEvaluator
{
    public static IReadOnlyDictionary<string, CrossPageNarrativeOrphanState> Evaluate(
        CrossPageNarrativeReportInput input,
        IReadOnlyDictionary<string, CrossPageNarrativeRoleAssignment> roleAssignments,
        CrossPageNarrativeGraph graph)
    {
        var inboundCounts = graph.Edges
            .GroupBy(edge => edge.TargetPageId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var results = new Dictionary<string, CrossPageNarrativeOrphanState>(StringComparer.Ordinal);
        foreach (var page in input.Pages)
        {
            roleAssignments.TryGetValue(page.PageName, out var roleAssignment);
            var role = roleAssignment?.PrimaryRole ?? CrossPageNarrativeRoleId.SupportingContext;
            var hasInbound = inboundCounts.TryGetValue(page.PageId, out var count) && count > 0;

            if (role is CrossPageNarrativeRoleId.ReferenceLegal
                or CrossPageNarrativeRoleId.Tooltip
                or CrossPageNarrativeRoleId.Qna
                or CrossPageNarrativeRoleId.ValidationSandbox)
            {
                results[page.PageId] = CrossPageNarrativeOrphanState.AdvisoryDisconnectedSpecialPage;
            }
            else if (role == CrossPageNarrativeRoleId.DetailDrill && !hasInbound)
            {
                results[page.PageId] = CrossPageNarrativeOrphanState.UnusedDrillTarget;
            }
            else if (!hasInbound && input.Pages.Count > 1)
            {
                results[page.PageId] = CrossPageNarrativeOrphanState.OrphanedPage;
            }
            else
            {
                results[page.PageId] = CrossPageNarrativeOrphanState.Connected;
            }
        }

        return results;
    }
}
