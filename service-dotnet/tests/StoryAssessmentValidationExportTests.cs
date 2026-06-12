using System.Text.Json;
using System.Reflection;
using StoryAssessmentValidationExport;
using Xunit;

namespace PowerBIModelingService.Tests;

public sealed class StoryAssessmentValidationExportTests : IDisposable
{
    private sealed class NarrativeAssessmentWithMissingNestedArtifacts
    {
        public string DominantReportObjective { get; init; } = "diagnostic investigation";
        public object? Graph { get; init; }
        public IReadOnlyList<object> Pages { get; init; } = [];
        public object? ScoreSummary { get; init; }
        public IReadOnlyList<object> Gaps { get; init; } = [];
    }

    private sealed class NarrativePageAssessmentWithMissingMetadata
    {
        public string PageName { get; init; } = "Overview";
        public object? RoleAssignment { get; init; }
        public string OrphanState { get; init; } = "Connected";
    }

    private sealed class NarrativeAssessmentWithNestedPublicArtifacts
    {
        public string DominantReportObjective { get; init; } = "executive performance review";
        public object Graph { get; init; } = new NarrativeGraphArtifact();
        public IReadOnlyList<object> Pages { get; init; } =
        [
            new NarrativePageArtifact(),
            new NarrativePageArtifact
            {
                PageId = "page-2",
                PageName = "Region Detail",
                RoleAssignment = new NarrativeRoleAssignmentArtifact
                {
                    PrimaryRole = "DetailDrill",
                    Confidence = "Medium",
                },
            },
        ];
        public object ScoreSummary { get; init; } = new NarrativeScoreSummaryArtifact();
        public IReadOnlyList<object> Gaps { get; init; } = [];
    }

    private sealed class NarrativeGraphArtifact
    {
        public IReadOnlyList<string> MainNarrativePath { get; init; } = ["page-1", "page-2"];
    }

    private sealed class NarrativePageArtifact
    {
        public string PageId { get; init; } = "page-1";
        public string PageName { get; init; } = "Overview";
        public object RoleAssignment { get; init; } = new NarrativeRoleAssignmentArtifact();
        public string OrphanState { get; init; } = "Connected";
    }

    private sealed class NarrativeRoleAssignmentArtifact
    {
        public string PrimaryRole { get; init; } = "Overview";
        public string Confidence { get; init; } = "High";
    }

    private sealed class NarrativeScoreSummaryArtifact
    {
        public IReadOnlyList<object> Dimensions { get; init; } =
        [
            new NarrativeDimensionArtifact(),
        ];
    }

    private sealed class NarrativeDimensionArtifact
    {
        public string DimensionId { get; init; } = "Flow";
        public double Score { get; init; } = 82d;
        public string Confidence { get; init; } = "Medium";
    }

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

    [Fact]
    public void CrossPageNarrativeShaper_MissingNestedArtifacts_DegradesGracefully()
    {
        var method = typeof(StoryAssessmentValidationExportService).GetMethod(
            "ShapeCrossPageNarrative",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var shaped = method!.Invoke(
            null,
            [new NarrativeAssessmentWithMissingNestedArtifacts
            {
                Pages =
                [
                    new NarrativePageAssessmentWithMissingMetadata(),
                ],
            }]);

        var narrative = Assert.IsType<StoryAssessmentValidationExportCrossPageNarrative>(shaped);
        Assert.Equal("diagnostic investigation", narrative.DominantReportObjective);
        Assert.Single(narrative.MainNarrativePath);
        Assert.Equal("No internal main narrative path available.", narrative.MainNarrativePath[0]);
        Assert.Single(narrative.PageRoles);
        Assert.Equal("Overview", narrative.PageRoles[0].PageName);
        Assert.Equal("Unavailable", narrative.PageRoles[0].Role);
        Assert.Equal("Unavailable", narrative.PageRoles[0].Confidence);
        Assert.Single(narrative.DimensionScores);
        Assert.Equal("Unavailable", narrative.DimensionScores[0].DimensionId);
        Assert.Equal("Unavailable", narrative.DimensionScores[0].Confidence);
        Assert.Single(narrative.ReportLevelGaps);
        Assert.Equal("unavailable", narrative.ReportLevelGaps[0].GapId);
    }

    [Fact]
    public void CrossPageNarrativeShaper_NestedPublicArtifacts_ExportsConcreteValues()
    {
        var method = typeof(StoryAssessmentValidationExportService).GetMethod(
            "ShapeCrossPageNarrative",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var shaped = method!.Invoke(
            null,
            [new NarrativeAssessmentWithNestedPublicArtifacts()]);

        var narrative = Assert.IsType<StoryAssessmentValidationExportCrossPageNarrative>(shaped);
        Assert.Equal("executive performance review", narrative.DominantReportObjective);
        Assert.Equal(["Overview", "Region Detail"], narrative.MainNarrativePath);
        Assert.Equal(2, narrative.PageRoles.Count);
        Assert.Equal("Overview", narrative.PageRoles[0].Role);
        Assert.Equal("High", narrative.PageRoles[0].Confidence);
        Assert.Single(narrative.DimensionScores);
        Assert.Equal("Flow", narrative.DimensionScores[0].DimensionId);
        Assert.Equal(82d, narrative.DimensionScores[0].Score);
        Assert.Equal("Medium", narrative.DimensionScores[0].Confidence);
    }

    [Fact]
    public async Task ExportAsync_SparseReport_WritesMissingEvidenceInsteadOfCrashing()
    {
        var reportRoot = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Sparse Overview","visuals":[]}
            """);
        var exportDir = Path.Combine(reportRoot, "story-assessment-validation-export");
        var service = new StoryAssessmentValidationExportService();

        await service.ExportAsync(reportRoot);

        var markdown = await File.ReadAllTextAsync(Path.Combine(exportDir, "story-assessment-validation.md"));
        Assert.Contains("## Page:", markdown, StringComparison.Ordinal);
        Assert.Contains("### Internal Story Gaps", markdown, StringComparison.Ordinal);
        Assert.Contains("### Internal Confidence Breakdown", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportAsync_MalformedMetadataReport_UsesFallbackPageLabels()
    {
        var reportRoot = CreateTempPbirFolderFromPageJson(
            """
            {"visuals":[
              {"id":"v1","type":"lineChart","x":0,"y":120,"width":520,"height":220}
            ]}
            """);
        var exportDir = Path.Combine(reportRoot, "story-assessment-validation-export");
        var service = new StoryAssessmentValidationExportService();

        await service.ExportAsync(reportRoot);

        var json = await File.ReadAllTextAsync(Path.Combine(exportDir, "story-assessment-validation.json"));
        using var document = JsonDocument.Parse(json);
        var page = document.RootElement.GetProperty("pages")[0];

        Assert.Equal("Page1", page.GetProperty("pageName").GetString());
        Assert.NotEmpty(page.GetProperty("storyGaps").EnumerateArray());
    }

    [Fact]
    public async Task ExportAsync_RealCorpusFixture_IsDeterministicWhenAvailable()
    {
        var reportPath = GetAvailableRealReportPath();
        if (reportPath is null)
        {
            return;
        }

        var exportDir1 = Path.Combine(Path.GetTempPath(), "pbir-validation-export-real-1-" + Guid.NewGuid().ToString("N"));
        var exportDir2 = Path.Combine(Path.GetTempPath(), "pbir-validation-export-real-2-" + Guid.NewGuid().ToString("N"));
        _tempDirs.Add(exportDir1);
        _tempDirs.Add(exportDir2);

        var service = new StoryAssessmentValidationExportService();

        await service.ExportAsync(reportPath, exportDir1);
        await service.ExportAsync(reportPath, exportDir2);

        var json1 = await File.ReadAllTextAsync(Path.Combine(exportDir1, "story-assessment-validation.json"));
        var json2 = await File.ReadAllTextAsync(Path.Combine(exportDir2, "story-assessment-validation.json"));
        var markdown1 = await File.ReadAllTextAsync(Path.Combine(exportDir1, "story-assessment-validation.md"));
        var markdown2 = await File.ReadAllTextAsync(Path.Combine(exportDir2, "story-assessment-validation.md"));

        Assert.Equal(RemoveGeneratedAt(json1), RemoveGeneratedAt(json2));
        Assert.Equal(RemoveGeneratedAt(markdown1), RemoveGeneratedAt(markdown2));
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

    private static string? GetAvailableRealReportPath()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("PBIR_REAL_FIXTURE_PATH"),
            "/Users/bcrowell/Documents/GitHub/PBITesting/Sales & Production.pbip",
            "/Users/bcrowell/Documents/GitHub/PBITest2/Sales Analysis.pbip",
        };

        return candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
    }

    private static string RemoveGeneratedAt(string content)
    {
        return string.Join(
            Environment.NewLine,
            content.Split(["\r\n", "\n"], StringSplitOptions.None)
                .Where(line => !line.Contains("Generated At UTC", StringComparison.Ordinal) &&
                               !line.Contains("\"generatedAtUtc\"", StringComparison.Ordinal)));
    }
}
