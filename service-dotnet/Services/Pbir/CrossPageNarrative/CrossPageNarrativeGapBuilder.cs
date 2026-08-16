using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir.CrossPageNarrative;

internal static class CrossPageNarrativeGapBuilder
{
    public static IReadOnlyList<CrossPageNarrativeGap> Build(
        IReadOnlyList<CrossPageNarrativePageInput> pages,
        IReadOnlyDictionary<string, CrossPageNarrativeRoleAssignment> roleAssignments,
        IReadOnlyDictionary<string, string> orphanStates,
        double compositeScore)
    {
        var gaps = new List<CrossPageNarrativeGap>();
        if (!roleAssignments.Values.Any(assignment =>
                assignment.PrimaryRole is CrossPageNarrativeRoleId.Overview or CrossPageNarrativeRoleId.ExecutiveSummary))
        {
            gaps.Add(new CrossPageNarrativeGap
            {
                GapId = CrossPageNarrativeGapId.MissingExecutiveEntryPoint,
                StableId = "gap.report.missing-executive-entry-point",
                Title = "Add a framing entry page",
                Summary = "The report starts without an overview or executive summary that explains the story.",
                WhyItMatters = "Users need a clear entry point before moving into comparison or detail pages.",
                ExpectedImpact = "A framing page improves report flow, continuity, and decision confidence.",
                AffectedPageIds = pages.Select(page => page.PageId).ToList(),
                EvidenceReferences = [CreateEvidence("crossPageNarrative", "missingExecutiveEntryPoint", "No overview or executive summary role was detected.")],
                Confidence = compositeScore < 60d ? StoryGapConfidence.Medium : StoryGapConfidence.High,
                ActionabilityAssessment = StoryGapActionabilityAssessment.Actionable,
                RemediationLayer = StoryGapRemediationLayer.Report,
            });
        }

        foreach (var page in pages)
        {
            if (!orphanStates.TryGetValue(page.PageId, out var orphanState) ||
                !string.Equals(orphanState, CrossPageNarrativeOrphanState.UnusedDrillTarget.ToString(), StringComparison.Ordinal))
            {
                continue;
            }

            gaps.Add(new CrossPageNarrativeGap
            {
                GapId = CrossPageNarrativeGapId.OrphanDetailPage,
                StableId = "gap.report.orphan-detail-page",
                Title = "Reconnect the detail page to the main journey",
                Summary = $"'{page.PageName}' behaves like a drill/detail page but no narrative parent currently leads into it.",
                WhyItMatters = "Detail pages without an inbound path increase navigation friction and weaken actionability.",
                ExpectedImpact = "Restoring a summary-to-detail path will make the report easier to explore.",
                AffectedPageIds = [page.PageId],
                EvidenceReferences = [CreateEvidence("crossPageNarrative", page.PageId, "Detail page is orphaned from the primary narrative path.")],
                Confidence = StoryGapConfidence.High,
                ActionabilityAssessment = StoryGapActionabilityAssessment.Actionable,
                RemediationLayer = StoryGapRemediationLayer.Restructure,
            });
        }

        return gaps;
    }

    private static StoryGapEvidenceReference CreateEvidence(string sourceType, string referenceId, string summary)
    {
        return new StoryGapEvidenceReference
        {
            SourceType = sourceType,
            ReferenceId = referenceId,
            Summary = summary,
        };
    }
}
