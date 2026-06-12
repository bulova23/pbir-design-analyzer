using System.Text.Json;
using StoryAssessmentValidationExport;
using Xunit;

namespace PowerBIModelingService.Tests;

public sealed class StoryAssessmentValidationExportTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    [Fact]
    public void JsonRenderer_WritesInternalValidationLabelAndPageData()
    {
        var report = new StoryAssessmentValidationExportReport
        {
            Title = "Internal Validation Export",
            ContractNotice = "Not User-Facing Contract",
            ReportPath = "/tmp/TestReport.Report",
            GeneratedAtUtc = "2026-06-11T00:00:00.0000000+00:00",
            Pages =
            [
                new StoryAssessmentValidationExportPage
                {
                    PageName = "Overview",
                    DetectedStory = "executive overview",
                    SignalRegistrySummary = ["layout.meaningfulVisibleTitle: fired"],
                    SpecialPageResult = "PageType=Unknown; Confidence=Low; TreatAsPrimaryNarrativePage=True; SuppressNormalStoryGaps=False; SuppressGenericArchetypePromotion=False",
                    ArchetypeClassification = "PerformanceMonitor",
                    ArchetypeSuppressionStatus = "Disposition=Normal; SuppressedBySpecialPageType=False",
                    SemanticCoherenceResult = "Focused",
                    CoherenceTuningDetails = ["Weighted page display name and primary visual title."],
                    CompetingStoryStatus = "None",
                    FilterTopologyResult = "single left control band",
                    StoryGaps =
                    [
                        new StoryAssessmentValidationExportGap
                        {
                            GapId = "gap.missing.context.targetBenchmarkPresent",
                            Description = "Add a visible target or benchmark.",
                            RemediationLayer = "Report",
                            Confidence = "High",
                            IsFutureContractCandidate = true,
                        },
                    ],
                    ConfidenceBreakdown =
                    [
                        new StoryAssessmentValidationExportConfidenceDimension
                        {
                            DimensionId = "Accuracy",
                            DimensionLabel = "Accuracy",
                            Rating = "Strong",
                            ConfidenceDrivers = ["Direct evidence fired"],
                            ConfidenceReducers = [],
                            MissingSignals = [],
                            EvidenceReferences = ["signalRegistry:layout.meaningfulVisibleTitle"],
                            Explanation = "Aligned direct evidence and archetype signals.",
                            Actionability = "PartlyActionable",
                            PromotionState = "Internal",
                            SurfaceScope = "CrossSurfaceCandidate",
                        },
                    ],
                    PromotionStates = ["Internal"],
                    SurfaceScopes = ["CrossSurfaceCandidate"],
                },
            ],
        };

        var json = StoryAssessmentValidationJsonRenderer.Render(report);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("Internal Validation Export", root.GetProperty("title").GetString());
        Assert.Equal("Not User-Facing Contract", root.GetProperty("contractNotice").GetString());
        Assert.Equal("Overview", root.GetProperty("pages")[0].GetProperty("pageName").GetString());
        Assert.Equal("Disposition=Normal; SuppressedBySpecialPageType=False", root.GetProperty("pages")[0].GetProperty("archetypeSuppressionStatus").GetString());
        Assert.True(root.GetProperty("pages")[0].GetProperty("storyGaps")[0].GetProperty("isFutureContractCandidate").GetBoolean());
        Assert.Equal("Accuracy", root.GetProperty("pages")[0].GetProperty("confidenceBreakdown")[0].GetProperty("dimensionId").GetString());
    }

    [Fact]
    public void MarkdownRenderer_WritesInternalValidationLabelAndReviewSections()
    {
        var report = new StoryAssessmentValidationExportReport
        {
            Title = "Internal Validation Export",
            ContractNotice = "Not User-Facing Contract",
            ReportPath = "/tmp/TestReport.Report",
            GeneratedAtUtc = "2026-06-11T00:00:00.0000000+00:00",
            Pages =
            [
                new StoryAssessmentValidationExportPage
                {
                    PageName = "Overview",
                    DetectedStory = "executive overview",
                    SignalRegistrySummary = ["layout.meaningfulVisibleTitle: fired"],
                    SpecialPageResult = "PageType=Unknown; Confidence=Low; TreatAsPrimaryNarrativePage=True; SuppressNormalStoryGaps=False; SuppressGenericArchetypePromotion=False",
                    ArchetypeClassification = "PerformanceMonitor",
                    ArchetypeSuppressionStatus = "Disposition=Normal; SuppressedBySpecialPageType=False",
                    SemanticCoherenceResult = "Focused",
                    CoherenceTuningDetails = ["Weighted page display name and primary visual title."],
                    CompetingStoryStatus = "None",
                    FilterTopologyResult = "single left control band",
                    StoryGaps =
                    [
                        new StoryAssessmentValidationExportGap
                        {
                            GapId = "gap.missing.context.targetBenchmarkPresent",
                            Description = "Add a visible target or benchmark.",
                            RemediationLayer = "Report",
                            Confidence = "High",
                            IsFutureContractCandidate = true,
                        },
                    ],
                    ConfidenceBreakdown =
                    [
                        new StoryAssessmentValidationExportConfidenceDimension
                        {
                            DimensionId = "Accuracy",
                            DimensionLabel = "Accuracy",
                            Rating = "Strong",
                            ConfidenceDrivers = ["Direct evidence fired"],
                            ConfidenceReducers = [],
                            MissingSignals = [],
                            EvidenceReferences = ["signalRegistry:layout.meaningfulVisibleTitle"],
                            Explanation = "Aligned direct evidence and archetype signals.",
                            Actionability = "PartlyActionable",
                            PromotionState = "Internal",
                            SurfaceScope = "CrossSurfaceCandidate",
                        },
                    ],
                    PromotionStates = ["Internal"],
                    SurfaceScopes = ["CrossSurfaceCandidate"],
                },
            ],
        };

        var markdown = StoryAssessmentValidationMarkdownRenderer.Render(report);

        Assert.Contains("# Internal Validation Export", markdown, StringComparison.Ordinal);
        Assert.Contains("Not User-Facing Contract", markdown, StringComparison.Ordinal);
        Assert.Contains("## Page: Overview", markdown, StringComparison.Ordinal);
        Assert.Contains("Internal Special Page Result", markdown, StringComparison.Ordinal);
        Assert.Contains("Archetype Suppression Status", markdown, StringComparison.Ordinal);
        Assert.Contains("Future Contract Candidate: Yes", markdown, StringComparison.Ordinal);
        Assert.Contains("### Internal Confidence Breakdown", markdown, StringComparison.Ordinal);
        Assert.Contains("Accuracy", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProgramMain_WithReportPath_WritesDefaultJsonAndMarkdownExports()
    {
        var reportRoot = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Revenue Overview","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":560,"height":40,
               "textbox":{"visible":true,"text":"Revenue Overview"}},
              {"id":"v1","type":"lineChart","x":0,"y":120,"width":520,"height":220,
               "title":{"visible":true,"text":"Revenue Trend"},
               "fieldRoles":{
                 "category":[{"displayName":"Revenue Month","description":"Revenue month trend"}],
                 "measure":[{"displayName":"Revenue","description":"Revenue performance"}]
               }}
            ]}
            """);
        var exportDir = Path.Combine(reportRoot, "story-assessment-validation-export");

        var exitCode = await Program.Main([reportRoot]);

        Assert.Equal(0, exitCode);
        Assert.True(Directory.Exists(exportDir));
        Assert.True(File.Exists(Path.Combine(exportDir, "story-assessment-validation.json")));
        Assert.True(File.Exists(Path.Combine(exportDir, "story-assessment-validation.md")));

        var json = await File.ReadAllTextAsync(Path.Combine(exportDir, "story-assessment-validation.json"));
        var markdown = await File.ReadAllTextAsync(Path.Combine(exportDir, "story-assessment-validation.md"));

        Assert.Contains("Internal Validation Export", json, StringComparison.Ordinal);
        Assert.Contains("Not User-Facing Contract", json, StringComparison.Ordinal);
        Assert.Contains("Internal Validation Export", markdown, StringComparison.Ordinal);
        Assert.Contains("Not User-Facing Contract", markdown, StringComparison.Ordinal);
        Assert.Contains("Revenue Overview", markdown, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    private string CreateTempPbirFolderFromPageJson(string pageJson)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pbir-validation-export-" + Guid.NewGuid().ToString("N"));
        var reportRoot = Path.Combine(tmp, "TestReport.Report");
        var definitionDir = Path.Combine(reportRoot, "definition");
        var pagesDir = Path.Combine(definitionDir, "pages", "Page1");
        Directory.CreateDirectory(pagesDir);
        _tempDirs.Add(tmp);

        File.WriteAllText(Path.Combine(definitionDir, "report.json"), """{"id":"test","name":"TestReport","pages":["Page1"],"theme":{"name":"CY24SU10"}}""");
        File.WriteAllText(Path.Combine(pagesDir, "page.json"), pageJson);

        return tmp;
    }
}
