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
