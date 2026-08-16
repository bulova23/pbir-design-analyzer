using System.Text;

namespace StoryAssessmentValidationExport;

public static class StoryAssessmentValidationMarkdownRenderer
{
    public static string Render(StoryAssessmentValidationExportReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {report.Title}");
        builder.AppendLine();
        builder.AppendLine($"**{report.ContractNotice}**");
        builder.AppendLine();
        builder.AppendLine($"Report Path: {report.ReportPath}");
        builder.AppendLine($"Generated At UTC: {report.GeneratedAtUtc}");

        if (report.CrossPageNarrative is not null)
        {
            builder.AppendLine();
            builder.AppendLine("## Cross-Page Narrative");
            builder.AppendLine();
            builder.AppendLine($"Dominant Report Objective: {report.CrossPageNarrative.DominantReportObjective}");
            builder.AppendLine($"Main Narrative Path: {string.Join(" -> ", report.CrossPageNarrative.MainNarrativePath)}");

            builder.AppendLine();
            builder.AppendLine("### Page Roles");
            foreach (var role in report.CrossPageNarrative.PageRoles)
            {
                builder.AppendLine($"- {role.PageName}: {role.Role} [{role.Confidence}]");
            }

            builder.AppendLine();
            builder.AppendLine("### Orphan Decisions");
            foreach (var orphan in report.CrossPageNarrative.OrphanDecisions)
            {
                builder.AppendLine($"- {orphan.PageName}: {orphan.OrphanState}");
            }

            builder.AppendLine();
            builder.AppendLine("### Narrative Dimension Scores");
            foreach (var dimension in report.CrossPageNarrative.DimensionScores)
            {
                builder.AppendLine($"- {dimension.DimensionId}: {dimension.Score:F1} [{dimension.Confidence}]");
            }

            builder.AppendLine();
            builder.AppendLine("### Report-Level Narrative Gaps");
            foreach (var gap in report.CrossPageNarrative.ReportLevelGaps)
            {
                builder.AppendLine($"- {gap.GapId}: {gap.Summary} [{gap.Confidence}]");
            }
        }

        foreach (var page in report.Pages)
        {
            builder.AppendLine();
            builder.AppendLine($"## Page: {page.PageName}");
            builder.AppendLine();
            builder.AppendLine($"Detected Story: {page.DetectedStory}");
            builder.AppendLine($"Internal Special Page Result: {page.SpecialPageResult}");
            builder.AppendLine($"Internal Archetype Classification: {page.ArchetypeClassification}");
            builder.AppendLine($"Archetype Suppression Status: {page.ArchetypeSuppressionStatus}");
            builder.AppendLine($"Internal Semantic Coherence Result: {page.SemanticCoherenceResult}");
            builder.AppendLine($"Internal Competing-Story Status: {page.CompetingStoryStatus}");
            builder.AppendLine($"Internal Filter Topology Result: {page.FilterTopologyResult}");
            builder.AppendLine($"Promotion States: {string.Join(", ", page.PromotionStates)}");
            builder.AppendLine($"Surface Scopes: {string.Join(", ", page.SurfaceScopes)}");

            builder.AppendLine();
            builder.AppendLine("### Internal Signal Registry Summary");
            foreach (var item in page.SignalRegistrySummary)
            {
                builder.AppendLine($"- {item}");
            }

            builder.AppendLine();
            builder.AppendLine("### Internal Coherence Tuning Details");
            foreach (var detail in page.CoherenceTuningDetails)
            {
                builder.AppendLine($"- {detail}");
            }

            builder.AppendLine();
            builder.AppendLine("### Internal Story Gaps");
            foreach (var gap in page.StoryGaps)
            {
                builder.AppendLine($"- {gap.GapId}: {gap.Description} [{gap.RemediationLayer}/{gap.Confidence}]");
                builder.AppendLine($"- Future Contract Candidate: {(gap.IsFutureContractCandidate ? "Yes" : "No")}");
            }

            builder.AppendLine();
            builder.AppendLine("### Internal Confidence Breakdown");
            foreach (var dimension in page.ConfidenceBreakdown)
            {
                builder.AppendLine($"#### {dimension.DimensionLabel}");
                builder.AppendLine($"- Rating: {dimension.Rating}");
                builder.AppendLine($"- Explanation: {dimension.Explanation}");
                builder.AppendLine($"- Actionability: {dimension.Actionability}");
                builder.AppendLine($"- Promotion State: {dimension.PromotionState}");
                builder.AppendLine($"- Surface Scope: {dimension.SurfaceScope}");
                builder.AppendLine($"- Confidence Drivers: {string.Join("; ", dimension.ConfidenceDrivers)}");
                builder.AppendLine($"- Confidence Reducers: {string.Join("; ", dimension.ConfidenceReducers)}");
                builder.AppendLine($"- Missing Signals: {string.Join("; ", dimension.MissingSignals)}");
                builder.AppendLine($"- Evidence References: {string.Join("; ", dimension.EvidenceReferences)}");
                builder.AppendLine();
            }
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }
}
