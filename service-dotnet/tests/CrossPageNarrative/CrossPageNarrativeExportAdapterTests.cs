using System.Text.Json;
using StoryAssessmentValidationExport;
using Xunit;

namespace PowerBIModelingService.Tests.CrossPageNarrative;

public sealed class CrossPageNarrativeExportAdapterTests
{
    [Fact(DisplayName = "Validation JSON export includes internal Cross-Page Narrative report section")]
    public void JsonRenderer_WritesCrossPageNarrativeSection()
    {
        var report = CreateExportReport();

        var json = StoryAssessmentValidationJsonRenderer.Render(report);
        using var document = JsonDocument.Parse(json);
        var narrative = document.RootElement.GetProperty("crossPageNarrative");

        Assert.Equal("executive performance review", narrative.GetProperty("dominantReportObjective").GetString());
        Assert.Equal("Overview", narrative.GetProperty("pageRoles")[0].GetProperty("role").GetString());
        Assert.Equal("UnusedDrillTarget", narrative.GetProperty("orphanDecisions")[1].GetProperty("orphanState").GetString());
    }

    [Fact(DisplayName = "Validation Markdown export includes Cross-Page Narrative review sections")]
    public void MarkdownRenderer_WritesCrossPageNarrativeSection()
    {
        var report = CreateExportReport();

        var markdown = StoryAssessmentValidationMarkdownRenderer.Render(report);

        Assert.Contains("## Cross-Page Narrative", markdown, StringComparison.Ordinal);
        Assert.Contains("Dominant Report Objective: executive performance review", markdown, StringComparison.Ordinal);
        Assert.Contains("### Page Roles", markdown, StringComparison.Ordinal);
        Assert.Contains("### Narrative Dimension Scores", markdown, StringComparison.Ordinal);
        Assert.Contains("### Report-Level Narrative Gaps", markdown, StringComparison.Ordinal);
    }

    private static StoryAssessmentValidationExportReport CreateExportReport()
    {
        return new StoryAssessmentValidationExportReport
        {
            Title = "Internal Validation Export",
            ContractNotice = "Not User-Facing Contract",
            ReportPath = "/tmp/TestReport.Report",
            GeneratedAtUtc = "2026-06-12T00:00:00.0000000+00:00",
            Pages = [],
            CrossPageNarrative = new StoryAssessmentValidationExportCrossPageNarrative
            {
                DominantReportObjective = "executive performance review",
                MainNarrativePath = ["Overview", "Region Detail"],
                PageRoles =
                [
                    new StoryAssessmentValidationExportPageRole
                    {
                        PageName = "Overview",
                        Role = "Overview",
                        Confidence = "High",
                    },
                    new StoryAssessmentValidationExportPageRole
                    {
                        PageName = "Region Detail",
                        Role = "DetailDrill",
                        Confidence = "Medium",
                    },
                ],
                OrphanDecisions =
                [
                    new StoryAssessmentValidationExportOrphanDecision
                    {
                        PageName = "Overview",
                        OrphanState = "Connected",
                    },
                    new StoryAssessmentValidationExportOrphanDecision
                    {
                        PageName = "Region Detail",
                        OrphanState = "UnusedDrillTarget",
                    },
                ],
                DimensionScores =
                [
                    new StoryAssessmentValidationExportNarrativeDimension
                    {
                        DimensionId = "Flow",
                        Score = 82,
                        Confidence = "High",
                    },
                ],
                ReportLevelGaps =
                [
                    new StoryAssessmentValidationExportNarrativeGap
                    {
                        GapId = "OrphanDetailPage",
                        StableId = "gap.report.orphan-detail-page",
                        Summary = "Detail page lacks an inbound narrative parent.",
                        Confidence = "High",
                    },
                ],
            },
        };
    }
}
