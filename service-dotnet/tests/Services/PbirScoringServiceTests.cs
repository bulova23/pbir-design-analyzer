using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

/// <summary>
/// Unit tests for <see cref="PbirScoringService"/> scoring formulae (Phase 9 / T043).
/// </summary>
public sealed class PbirScoringServiceTests : IDisposable
{
    private sealed record StorySignalRegistryEntrySnapshot(string Id, string? RawValue, bool Fired);
    private sealed record StoryArchetypeMatchSnapshot(
        string ArchetypeId,
        double MatchScore,
        string MatchConfidence,
        List<string> MatchedSignals,
        List<string> MissedSignals,
        List<string> ExplanationHooks,
        string ValidationStatus,
        string PromotionEligibilityState);

    private sealed record StoryAssessmentLevel1ValidationHarnessSnapshot(
        string? ReviewerChoice,
        string SystemChoice,
        string? DisagreementReason,
        string AccuracyRating,
        string ConsistencyRating,
        string ExplainabilityRating,
        string ActionabilityRating);

    private sealed record StoryAssessmentPromotionGateDefinitionSnapshot(
        double MinimumClassificationAccuracy,
        string MinimumExplanationQuality,
        string MinimumGapUsefulnessPotential,
        double MaximumFalsePositiveRate,
        double ReviewerAgreementThresholdPlaceholder);

    private sealed record StoryAssessmentArchetypeClassificationSnapshot(
        string BestFitArchetypeId,
        List<StoryArchetypeMatchSnapshot> ArchetypeResults,
        StoryAssessmentLevel1ValidationHarnessSnapshot Level1ValidationHarness,
        StoryAssessmentPromotionGateDefinitionSnapshot PromotionGateDefinition);

    private sealed record StorySemanticTermEvidenceSnapshot(
        string CanonicalTerm,
        string RawText,
        string Source,
        double Weight);

    private sealed record StorySemanticTermClusterSnapshot(
        string ClusterId,
        double Weight,
        int SupportCount,
        List<string> Terms,
        string ExplanationHook);

    private sealed record StorySemanticCoherenceLevel1ValidationHarnessSnapshot(
        string? ReviewerCoherenceChoice,
        string SystemCoherenceChoice,
        string? ReviewerDominantConcept,
        string SystemDominantConcept,
        string? DisagreementReason,
        string AccuracyRating,
        string ConsistencyRating,
        string ExplainabilityRating,
        string ActionabilityRating);

    private sealed record StorySemanticCoherenceAssessmentSnapshot(
        double CoherenceScore,
        string CoherenceClassification,
        string? DominantConcept,
        List<StorySemanticTermEvidenceSnapshot> ExtractedTerms,
        List<StorySemanticTermClusterSnapshot> TermClusters,
        string CompetingStoryStatus,
        List<string> WeakDisagreementSignals,
        List<string> ExplanationHooks,
        string Confidence,
        string ValidationStatus,
        StorySemanticCoherenceLevel1ValidationHarnessSnapshot Level1ValidationHarness);

    private readonly List<string> _tempDirs = [];

    // ── CognitiveLoad: exactly 6 visuals ─────────────────────────────────────

    /// <summary>
    /// A page with exactly 6 visuals sits at the density threshold, so the penalty
    /// does not apply and CognitiveLoadScore must equal 100.
    /// </summary>
    [Fact]
    public async Task CognitiveLoadScore_With6Visuals_Returns100()
    {
        // Arrange
        var tempDir = CreateTempPbirFolder(6);
        var svc     = BuildScoringService();

        // Act
        var result = await svc.ScoreAsync(tempDir);

        // Assert
        Assert.Equal(100.0, result.CognitiveLoadScore);
    }

    // ── CognitiveLoad: 12 visuals ─────────────────────────────────────────────

    /// <summary>
    /// 12 visuals trigger the log penalty: 100 − Log₂(12/6) × 15 = 85.
    /// The result must fall in [80, 90].
    /// </summary>
    [Fact]
    public async Task CognitiveLoadScore_With12Visuals_AppliesLogPenalty()
    {
        // Arrange
        var tempDir = CreateTempPbirFolder(12);
        var svc     = BuildScoringService();

        // Act
        var result = await svc.ScoreAsync(tempDir);

        // Assert — formula yields 85; allow ±5 to account for floating-point rounding
        Assert.True(result.CognitiveLoadScore >= 80.0 && result.CognitiveLoadScore <= 90.0,
            $"Expected CognitiveLoadScore in [80, 90] but got {result.CognitiveLoadScore}.");
    }

    [Fact]
    public async Task NavigationScoring_ReducesActionButtonWeightInCognitiveLoad()
    {
        var tempDir = CreateTempPbirFolder(2, navigationVisualCount: 8, navigationVisualType: "actionButton");
        var svc = BuildScoringService();
        using var doc = JsonDocument.Parse("""{"navigationScoring":{"enabled":true,"weight":25}}""");

        var result = await svc.ScoreAsync(tempDir, doc.RootElement.Clone());

        Assert.Equal(100.0, result.CognitiveLoadScore);
        Assert.Equal(2, result.DataVisualCount);
        Assert.Equal(8, result.NavigationVisualCount);
        Assert.Contains(result.Feedback["cognitiveLoad"], item => item.Text.Contains("25%", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NavigationScoring_Disabled_UsesLegacyFullWeightInCognitiveLoad()
    {
        var tempDir = CreateTempPbirFolder(2, navigationVisualCount: 8, navigationVisualType: "actionButton");
        var svc = BuildScoringService();
        using var doc = JsonDocument.Parse("""{"navigationScoring":{"enabled":false,"weight":25}}""");

        var result = await svc.ScoreAsync(tempDir, doc.RootElement.Clone());

        Assert.True(result.CognitiveLoadScore < 95.0);
        Assert.Equal(8, result.NavigationVisualCount);
    }

    [Fact]
    public async Task NavigationScoring_ExcludesActionButtonsFromDataInkRatio()
    {
        var tempDir = CreateTempPbirFolder(2, navigationVisualCount: 8, navigationVisualType: "actionButton");
        var svc = BuildScoringService();
        using var doc = JsonDocument.Parse("""{"navigationScoring":{"enabled":true,"weight":25}}""");

        var result = await svc.ScoreAsync(tempDir, doc.RootElement.Clone());

        Assert.Equal(100.0, result.DataInkScore);
        Assert.Contains(result.Feedback["dataInk"], item => item.Text.Contains("excluded", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DataInkScore_DecorativeFindingsIncludeAffectedVisuals()
    {
        var tempDir = CreateTempPbirFolder(1, navigationVisualCount: 1, navigationVisualType: "textbox");
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var feedbackItem = Assert.Single(result.Feedback["dataInk"].Where(item =>
            !item.Ok &&
            item.Text.Contains("Data-ink ratio", StringComparison.Ordinal)));
        var affectedVisual = Assert.Single(feedbackItem.AffectedVisuals!);

        Assert.Equal("Page 1", affectedVisual.PageName);
        Assert.Equal("n1", affectedVisual.VisualId);
        Assert.Equal("textbox", affectedVisual.VisualType);
    }

    [Fact]
    public async Task ScoreAsync_NoDataVisuals_ClassifiesFallbackFindingsAsObjective()
    {
        var tempDir = CreateTempPbirFolder(0);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.All(result.Feedback.Values.SelectMany(items => items), item =>
            Assert.Equal(FindingTypes.Objective, item.FindingType));
    }

    [Fact]
    public async Task ScoreAsync_MultiPageReport_UsesPagesJsonOrder()
    {
        var tempDir = CreateTempPbirFolder(
            [
                ("section-3", "Order Detail"),
                ("section-1", "Overview"),
                ("section-2", "Customer Analysis"),
            ],
            ["section-1", "section-2", "section-3"]);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.PageScores);
        Assert.Collection(
            result.PageScores!,
            page => Assert.Equal("Overview", page.PageName),
            page => Assert.Equal("Customer Analysis", page.PageName),
            page => Assert.Equal("Order Detail", page.PageName));
    }

    [Fact]
    public async Task ScoreAsync_MultiPageReport_UsesDeterministicDirectoryOrderWhenPagesJsonMissing()
    {
        var tempDir = CreateTempPbirFolderWithoutPagesJson(
            ("section-3", "Order Detail"),
            ("section-1", "Overview"),
            ("section-2", "Customer Analysis"));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.PageScores);
        Assert.Collection(
            result.PageScores!,
            page => Assert.Equal("Overview", page.PageName),
            page => Assert.Equal("Customer Analysis", page.PageName),
            page => Assert.Equal("Order Detail", page.PageName));
    }

    [Fact]
    public async Task ScoreAsync_SinglePageMode_UsesStablePageName()
    {
        var tempDir = CreateTempPbirFolderFromPages(
            ("OverviewPage", """{"name":"OverviewPage","displayName":"Overview","visuals":[{"id":"v1","type":"barChart","x":0,"y":0,"width":100,"height":100}]}"""),
            ("DetailPage", """{"name":"DetailPage","displayName":"Details","visuals":[{"id":"v1","type":"barChart","x":0,"y":0,"width":100,"height":100}]}"""));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir, config: null, pageName: "OverviewPage");

        Assert.Equal("OverviewPage", result.ScoredPageName);
        Assert.Equal(1, result.DataVisualCount);
    }

    [Fact]
    public async Task ScoreAsync_GestaltFeedbackIncludesCriterionPoints()
    {
        var tempDir = CreateTempPbirFolder(3);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var firstFourGestaltItems = result.Feedback["gestalt"].Take(4).ToList();

        Assert.Equal(4, firstFourGestaltItems.Count);
        Assert.Collection(
            firstFourGestaltItems,
            item =>
            {
                Assert.StartsWith("Grid alignment:", item.Text, StringComparison.Ordinal);
                Assert.NotNull(item.EarnedPoints);
                Assert.Equal(35.0, item.PossiblePoints);
            },
            item =>
            {
                Assert.StartsWith("Figure/ground:", item.Text, StringComparison.Ordinal);
                Assert.NotNull(item.EarnedPoints);
                Assert.Equal(30.0, item.PossiblePoints);
            },
            item =>
            {
                Assert.StartsWith("Similarity:", item.Text, StringComparison.Ordinal);
                Assert.NotNull(item.EarnedPoints);
                Assert.Equal(20.0, item.PossiblePoints);
            },
            item =>
            {
                Assert.StartsWith("Visual presence:", item.Text, StringComparison.Ordinal);
                Assert.NotNull(item.EarnedPoints);
                Assert.Equal(15.0, item.PossiblePoints);
            });
    }

    [Fact]
    public async Task ScoreAsync_GovernanceTitleRule_FailsWithoutVisibleTitleIntent()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":320,"height":180}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var titleFeedback = Assert.Single(result.Feedback["governance"].Where(item =>
            item.Text.StartsWith("Page title policy:", StringComparison.Ordinal)));
        Assert.False(titleFeedback.Ok);
        Assert.Equal(FindingTypes.Objective, titleFeedback.FindingType);
        Assert.Contains("lack a meaningful visible title", titleFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'Page 1'", titleFeedback.Text, StringComparison.Ordinal);
        Assert.Null(result.VisualMetadata!.StrictVisiblePageTitle);
    }

    [Fact]
    public async Task ScoreAsync_GovernanceTitleRule_FailsWhenVisibleTitleIsBelowTopBand()
    {
        // Default canvas is 720px tall. 15% top band = 108px. Place the titled visual at y=400.
        // Include a data visual so the governance evaluator runs (the zero-data-visual guard
        // would otherwise short-circuit Feedback["governance"]).
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"textbox","x":0,"y":400,"width":320,"height":60,
               "title":{"visible":true,"text":"Bottom Band Title"}},
              {"id":"v2","type":"barChart","x":340,"y":120,"width":420,"height":220}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var titleFeedback = Assert.Single(result.Feedback["governance"].Where(item =>
            item.Text.StartsWith("Page title policy:", StringComparison.Ordinal)));
        Assert.False(titleFeedback.Ok);
        Assert.Contains("top band", titleFeedback.Text, StringComparison.OrdinalIgnoreCase);
        // Non-strict helper still returns the text; strict helper should not.
        Assert.Equal("Bottom Band Title", result.VisualMetadata!.VisiblePageTitle);
        Assert.Null(result.VisualMetadata.StrictVisiblePageTitle);
    }

    [Fact]
    public async Task ScoreAsync_GovernanceTitleRule_FailsWhenVisibleTitleIsVague()
    {
        // Include a data visual so the governance evaluator runs and emits the page-title feedback.
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"textbox","x":0,"y":0,"width":320,"height":60,
               "title":{"visible":true,"text":"Page 1"}},
              {"id":"v2","type":"barChart","x":340,"y":120,"width":420,"height":220}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var titleFeedback = Assert.Single(result.Feedback["governance"].Where(item =>
            item.Text.StartsWith("Page title policy:", StringComparison.Ordinal)));
        Assert.False(titleFeedback.Ok);
        Assert.Null(result.VisualMetadata!.StrictVisiblePageTitle);
    }

    [Fact]
    public async Task ScoreAsync_GovernanceTitleRule_ExposesStrictVisibleTitleOnPass()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"textbox","x":40,"y":20,"width":400,"height":60,
               "title":{"visible":true,"text":"Q3 Revenue Overview"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.Equal("Q3 Revenue Overview", result.VisualMetadata!.StrictVisiblePageTitle);
    }

    // ── Accessibility scoring tests (REC-05) ──────────────────────────────

    [Fact]
    public async Task ScoreAsync_Accessibility_AwardsFullMarksWhenNoFormattingMetadata()
    {
        // No theme overrides, no background/font colours — all three sub-criteria award full marks.
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":320,"height":180}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.Equal(100, result.AccessibilityScore);
        Assert.Contains(result.Feedback["accessibility"], item =>
            item.Text.StartsWith("WCAG 2.1 AA palette:", StringComparison.Ordinal) && item.Ok);
        Assert.Contains(result.Feedback["accessibility"], item =>
            item.Text.StartsWith("On-canvas text contrast:", StringComparison.Ordinal) && item.Ok);
        Assert.Contains(result.Feedback["accessibility"], item =>
            item.Text.StartsWith("Colorblind-safe palette:", StringComparison.Ordinal) && item.Ok);
    }

    [Fact]
    public async Task ScoreAsync_Accessibility_FlagsOnCanvasContrastFailure()
    {
        // Background #444444 with font #555555 — contrast ratio is well below 4.5:1.
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"card","x":0,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"Revenue"},
               "background":{"color":"444444"},
               "font":{"color":"555555"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var onCanvasFeedback = result.Feedback["accessibility"]
            .FirstOrDefault(item => item.Text.StartsWith("On-canvas text contrast:", StringComparison.Ordinal));
        Assert.NotNull(onCanvasFeedback);
        Assert.False(onCanvasFeedback!.Ok);
        Assert.Contains("fail WCAG 2.1 AA", onCanvasFeedback.Text, StringComparison.OrdinalIgnoreCase);
        // The failing visual should be cited
        Assert.NotNull(onCanvasFeedback.AffectedVisuals);
        Assert.Contains(onCanvasFeedback.AffectedVisuals!, v => v.VisualId == "v1");
    }

    [Fact]
    public async Task ScoreAsync_GovernanceTitleRule_PassesWithVisibleChartTitle()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"Sales Overview"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var titleFeedback = Assert.Single(result.Feedback["governance"].Where(item =>
            item.Text.StartsWith("Page title policy:", StringComparison.Ordinal)));
        Assert.True(titleFeedback.Ok);
        Assert.Contains("Sales Overview", titleFeedback.Text, StringComparison.Ordinal);
        Assert.NotNull(result.VisualMetadata);
        Assert.Equal("Sales Overview", result.VisualMetadata!.VisiblePageTitle);
        Assert.Single(result.VisualMetadata.Visuals);
        Assert.Equal("Sales Overview", result.VisualMetadata.Visuals[0].VisibleTitleText);
    }

    [Fact]
    public async Task ScoreAsync_InlineVisualMetadata_AddsSurfaceTreatmentFeedback()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"Executive Overview"},
               "background":{"color":"F2F2F2"},
               "border":{"visible":true},
               "shadow":{"visible":false},
               "corners":{"radius":0}},
              {"id":"v2","type":"barChart","x":340,"y":0,"width":320,"height":180,
               "background":{"color":"FFFFFF"},
               "border":{"visible":false},
               "shadow":{"visible":true},
               "corners":{"radius":12}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var surfaceFeedback = Assert.Single(result.Feedback["visualBestPractices"].Where(item =>
            item.Text.StartsWith("Surface treatment:", StringComparison.Ordinal)));
        Assert.False(surfaceFeedback.Ok);
        Assert.Contains("borders", surfaceFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("corner radii vary", surfaceFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("background fills vary", surfaceFeedback.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScoreAsync_DirectoryVisualParser_UsesVisualJsonTitleMetadata()
    {
        var tempDir = CreateTempPbirFolderWithDirectoryVisuals(
            """{"displayName":"Page 1"}""",
            ("vc1",
            """
            {"name":"vc1","position":{"x":0,"y":0,"width":320,"height":180},
             "visual":{"visualType":"barChart"},
             "title":{"visible":true,"text":"Revenue Overview"},
             "legend":{"visible":true},
             "fieldRoles":{"category":["Region"],"value":["Revenue"]}}
            """));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.Equal(1, result.DataVisualCount);
        var titleFeedback = Assert.Single(result.Feedback["governance"].Where(item =>
            item.Text.StartsWith("Page title policy:", StringComparison.Ordinal)));
        Assert.True(titleFeedback.Ok);
        Assert.Contains("Revenue Overview", titleFeedback.Text, StringComparison.Ordinal);
        Assert.NotNull(result.VisualMetadata);
        Assert.Equal("Revenue Overview", result.VisualMetadata!.VisiblePageTitle);
        Assert.Equal("Region", Assert.Single(result.VisualMetadata.Visuals[0].CategoryHints));
    }

    [Fact]
    public async Task ScoreAsync_DirectoryVisualParser_OrdersVisualsDeterministically()
    {
        var tempDir = CreateTempPbirFolderWithDirectoryVisuals(
            """{"displayName":"Page 1"}""",
            ("visual-z",
            """
            {"name":"visual-z","position":{"x":240,"y":160,"width":120,"height":100},
             "visual":{"visualType":"barChart"}}
            """),
            ("visual-a",
            """
            {"name":"visual-a","position":{"x":20,"y":20,"width":120,"height":100},
             "visual":{"visualType":"card"}}
            """),
            ("visual-m",
            """
            {"name":"visual-m","position":{"x":120,"y":20,"width":120,"height":100},
             "visual":{"visualType":"lineChart"}}
            """));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.Equal(
            new[] { "visual-a", "visual-m", "visual-z" },
            result.VisualMetadata!.Visuals.Select(visual => visual.VisualId).ToArray());
    }

    [Fact]
    public async Task ScoreAsync_VisualMetadata_CapturesRepeatedStatusSemanticColors()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"card","x":0,"y":0,"width":180,"height":120,
               "title":{"visible":true,"text":"At Risk Orders"},
               "font":{"color":"#ff0000"}},
              {"id":"v2","type":"card","x":200,"y":0,"width":180,"height":120,
               "title":{"visible":true,"text":"At Risk Margin"},
               "font":{"color":"#ff0000"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.VisualMetadata);
        var semanticAssignments = result.VisualMetadata!.SemanticColorMap;
        Assert.Equal(2, semanticAssignments.Count);
        Assert.All(semanticAssignments, assignment =>
        {
            Assert.Equal("status:at-risk", assignment.SemanticKey);
            Assert.Equal("#FF0000", assignment.Color);
        });
        Assert.All(result.VisualMetadata!.Visuals, visual =>
        {
            var assignment = Assert.Single(visual.SemanticColors);
            Assert.Equal("status:at-risk", assignment.SemanticKey);
            Assert.Equal("#FF0000", assignment.Color);
        });
    }

    [Fact]
    public async Task ScoreAsync_VisualMetadata_CapturesSameSemanticKeyWithDifferentColors()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"card","x":0,"y":0,"width":180,"height":120,
               "title":{"visible":true,"text":"At Risk Orders"},
               "font":{"color":"#ff0000"}},
              {"id":"v2","type":"card","x":200,"y":0,"width":180,"height":120,
               "title":{"visible":true,"text":"At Risk Margin"},
               "font":{"color":"#ff8800"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.VisualMetadata);
        var semanticAssignments = result.VisualMetadata!.SemanticColorMap
            .Where(assignment => assignment.SemanticKey == "status:at-risk")
            .Select(assignment => assignment.Color)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.Equal(2, semanticAssignments.Count);
        Assert.Contains("#FF0000", semanticAssignments);
        Assert.Contains("#FF8800", semanticAssignments);
    }

    [Fact]
    public async Task ScoreAsync_VisualMetadata_CapturesRoleAnchoredSemanticColors()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"North Revenue"},
               "fieldRoles":{"category":["Region"],"value":["Revenue"]},
               "background":{"color":"#3366cc"}},
              {"id":"v2","type":"barChart","x":340,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"South Revenue"},
               "fieldRoles":{"category":["Region"],"value":["Revenue"]},
               "background":{"color":"#ff9900"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.VisualMetadata);
        var semanticAssignments = result.VisualMetadata!.SemanticColorMap;
        Assert.Contains(semanticAssignments, assignment =>
            assignment.SemanticKey == "region:north" &&
            assignment.Color == "#3366CC");
        Assert.Contains(semanticAssignments, assignment =>
            assignment.SemanticKey == "region:south" &&
            assignment.Color == "#FF9900");
    }

    [Fact]
    public async Task ScoreAsync_VisualBestPractices_FlagsSemanticColorDrift()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"card","x":0,"y":0,"width":180,"height":120,
               "title":{"visible":true,"text":"At Risk Orders"},
               "font":{"color":"#ff0000"}},
              {"id":"v2","type":"card","x":200,"y":0,"width":180,"height":120,
               "title":{"visible":true,"text":"At Risk Margin"},
               "font":{"color":"#ff8800"}},
              {"id":"s1","type":"slicer","x":0,"y":160,"width":220,"height":120}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var semanticFeedback = Assert.Single(result.Feedback["visualBestPractices"].Where(item =>
            item.Text.StartsWith("Semantic color consistency:", StringComparison.Ordinal)));
        Assert.False(semanticFeedback.Ok);
        Assert.Contains("multiple colors", semanticFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(semanticFeedback.AffectedVisuals);
        Assert.Equal(2, semanticFeedback.AffectedVisuals!.Count);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("same category or status meaning on the same color", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_Accessibility_FlagsContradictoryStatusColors()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"card","x":0,"y":0,"width":180,"height":120,
               "title":{"visible":true,"text":"At Risk Orders"},
               "font":{"color":"#00aa00"}},
              {"id":"v2","type":"card","x":200,"y":0,"width":180,"height":120,
               "title":{"visible":true,"text":"On Track Margin"},
               "font":{"color":"#cc0000"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var semanticFeedback = Assert.Single(result.Feedback["accessibility"].Where(item =>
            item.Text.StartsWith("Status color semantics:", StringComparison.Ordinal)));
        Assert.False(semanticFeedback.Ok);
        Assert.Contains("Reserve red/green", semanticFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(semanticFeedback.AffectedVisuals);
        Assert.Equal(2, semanticFeedback.AffectedVisuals!.Count);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("Reserve red/green for consistent bad/good status semantics only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_VisualMetadata_InferTrendChartIntent()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"lineChart","x":0,"y":0,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue Trend"},
               "fieldRoles":{"category":["Month"],"value":["Revenue"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.VisualMetadata);
        Assert.NotNull(result.VisualMetadata!.ChartIntentSummary);
        Assert.Equal("trend", result.VisualMetadata.ChartIntentSummary!.Intent);
        Assert.Equal("high", result.VisualMetadata.ChartIntentSummary.Confidence);

        var visualIntent = Assert.Single(result.VisualMetadata.Visuals).ChartIntent;
        Assert.NotNull(visualIntent);
        Assert.Equal("trend", visualIntent!.Intent);
        Assert.Equal("good", visualIntent.FitStatus);
        Assert.Empty(visualIntent.RecommendedAlternatives);
    }

    [Fact]
    public async Task ScoreAsync_VisualMetadata_InferWeakFitForCategoricalLineChart()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"lineChart","x":0,"y":0,"width":420,"height":220,
               "title":{"visible":true,"text":"Sales by Region"},
               "fieldRoles":{"category":["Region"],"value":["Sales"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.VisualMetadata);
        var visualIntent = Assert.Single(result.VisualMetadata!.Visuals).ChartIntent;
        Assert.NotNull(visualIntent);
        Assert.Equal("comparison", visualIntent!.Intent);
        Assert.Equal("weak", visualIntent.FitStatus);
        Assert.Contains("clusteredColumnChart", visualIntent.RecommendedAlternatives);
    }

    [Fact]
    public async Task ScoreAsync_MalformedFormattingMetadata_DoesNotFailScoring()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"Quality Overview"},
               "border":["unexpected"],
               "shadow":"not-a-bool",
               "corners":{"radius":"not-a-number"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.Equal(1, result.DataVisualCount);
        Assert.Empty(result.ScoringErrors);
        Assert.True(result.CompositeScore > 0);
    }

    [Fact]
    public async Task ScoreAsync_SlicerCountsAsVisibleFilterControl()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"Sales Overview"}},
              {"id":"s1","type":"slicer","x":0,"y":200,"width":200,"height":80,
               "title":{"visible":true,"text":"Region"}}
            ]}
            """);
        var svc = BuildScoringService();
        using var doc = JsonDocument.Parse("""{"navigationScoring":{"enabled":true,"weight":25}}""");

        var result = await svc.ScoreAsync(tempDir, doc.RootElement.Clone());

        Assert.Equal(1, result.DataVisualCount);
        Assert.Equal(1, result.NavigationVisualCount);
        Assert.Contains(result.Feedback["dataInk"], item => item.Text.Contains("excluded", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_NarrativeFeedback_FlagsMissingStoryAnchor()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":320,"height":180},
              {"id":"v2","type":"lineChart","x":340,"y":0,"width":320,"height":180}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var purposeFeedback = Assert.Single(result.Feedback["narrative"].Where(item =>
            item.Text.StartsWith("Visible page purpose:", StringComparison.Ordinal)));
        Assert.False(purposeFeedback.Ok);
        Assert.Contains("no visible title or question anchor", purposeFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.NarrativeScore < 60);
    }

    [Fact]
    public async Task ScoreAsync_NarrativeFeedback_FlagsKpiPagesWithoutContext()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Summary","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Revenue Performance"}},
              {"id":"k1","type":"card","x":0,"y":60,"width":180,"height":120,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"k2","type":"card","x":200,"y":60,"width":180,"height":120,
               "title":{"visible":true,"text":"Margin"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var contextFeedback = Assert.Single(result.Feedback["narrative"].Where(item =>
            item.Text.Contains("no target, variance, prior-period, or trend context", StringComparison.OrdinalIgnoreCase)));
        Assert.False(contextFeedback.Ok);
        Assert.Contains("Revenue KPI", contextFeedback.Text, StringComparison.Ordinal);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("KPI cards need a target, variance, or prior-period comparison", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_NarrativeScore_RewardsDecisionLedPageWithContext()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Summary","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":460,"height":40,
               "textbox":{"visible":true,"text":"Revenue vs Target"}},
              {"id":"k1","type":"card","x":0,"y":60,"width":180,"height":120,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"v1","type":"lineChart","x":200,"y":60,"width":320,"height":180,
               "title":{"visible":true,"text":"Revenue Trend"}},
              {"id":"v2","type":"clusteredBarChart","x":0,"y":260,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Region"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.True(result.NarrativeScore >= 80);
        Assert.All(result.Feedback["narrative"], item => Assert.True(item.Ok));
        Assert.DoesNotContain(result.Recommendations, recommendation =>
            recommendation.Contains("Narrative:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_NarrativeFeedback_RecognizesOverviewToDetailFlow()
    {
        var tempDir = CreateTempPbirFolderFromPages(
            ("section-1",
            """
            {"displayName":"Overview","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":460,"height":40,
               "textbox":{"visible":true,"text":"Executive Revenue Overview"}},
              {"id":"k1","type":"card","x":0,"y":60,"width":180,"height":120,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"v1","type":"lineChart","x":200,"y":60,"width":320,"height":180,
               "title":{"visible":true,"text":"Revenue Trend"}}
            ]}
            """),
            ("section-2",
            """
            {"displayName":"Details","visuals":[
              {"id":"v2","type":"clusteredBarChart","x":0,"y":0,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Region"}}
            ]}
            """));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var overviewFeedback = Assert.Single(result.Feedback["narrative"].Where(item =>
            item.Text.StartsWith("Overview-to-detail readability:", StringComparison.Ordinal)));
        Assert.True(overviewFeedback.Ok);
        Assert.Contains("'Overview' provides a clear overview", overviewFeedback.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScoreAsync_GestaltFeedback_FlagsTopBandKpiSpacingIssues()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Summary","visuals":[
              {"id":"k1","type":"card","x":0,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"k2","type":"card","x":210,"y":58,"width":180,"height":120,
               "title":{"visible":true,"text":"Margin"}},
              {"id":"k3","type":"card","x":520,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"Units"}},
              {"id":"v1","type":"clusteredBarChart","x":0,"y":240,"width":480,"height":220,
               "title":{"visible":true,"text":"Revenue by Region"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var kpiFeedback = Assert.Single(result.Feedback["gestalt"].Where(item =>
            item.Text.StartsWith("Top-band KPI consistency:", StringComparison.Ordinal)));
        Assert.False(kpiFeedback.Ok);
        Assert.Contains("top-row KPI cards", kpiFeedback.Text, StringComparison.Ordinal);
        Assert.NotNull(kpiFeedback.AffectedVisuals);
        Assert.Equal(3, kpiFeedback.AffectedVisuals!.Count);
        Assert.True(result.GestaltScore < 100);
    }

    [Fact]
    public async Task ScoreAsync_CognitiveLoadFeedback_FlagsLowerRightFilterPlacement()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"clusteredBarChart","x":0,"y":80,"width":420,"height":220,
               "title":{"visible":true,"text":"Sales by Region"}},
              {"id":"s1","type":"slicer","x":920,"y":420,"width":220,"height":160,
               "title":{"visible":true,"text":"Region"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var filterFeedback = Assert.Single(result.Feedback["cognitiveLoad"].Where(item =>
            item.Text.StartsWith("Filter placement:", StringComparison.Ordinal)));
        Assert.False(filterFeedback.Ok);
        Assert.Contains("lower-right area", filterFeedback.Text, StringComparison.OrdinalIgnoreCase);
        var affectedVisual = Assert.Single(filterFeedback.AffectedVisuals!);
        Assert.Equal("s1", affectedVisual.VisualId);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("Move slicers", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_CognitiveLoadFeedback_FlagsScatteredFiltersAcrossZones()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Overview","visuals":[
              {"id":"v1","type":"clusteredBarChart","x":240,"y":120,"width":520,"height":260,
               "title":{"visible":true,"text":"Revenue by Region"}},
              {"id":"s1","type":"slicer","x":0,"y":0,"width":180,"height":80,
               "title":{"visible":true,"text":"Region"}},
              {"id":"s2","type":"slicer","x":980,"y":140,"width":220,"height":120,
               "title":{"visible":true,"text":"Segment"}},
              {"id":"s3","type":"slicer","x":420,"y":560,"width":260,"height":100,
               "title":{"visible":true,"text":"Channel"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var filterFeedback = Assert.Single(result.Feedback["cognitiveLoad"].Where(item =>
            item.Text.StartsWith("Filter consolidation:", StringComparison.Ordinal)));
        Assert.False(filterFeedback.Ok);
        Assert.Contains("distributes 3 slicer(s) across", filterFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(filterFeedback.AffectedVisuals);
        Assert.Equal(3, filterFeedback.AffectedVisuals!.Count);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("single top band or left rail", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_CognitiveLoadFeedback_FlagsDenseOverviewFilterBand()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Overview","visuals":[
              {"id":"v1","type":"clusteredColumnChart","x":260,"y":140,"width":560,"height":260,
               "title":{"visible":true,"text":"Revenue vs Plan"}},
              {"id":"s1","type":"slicer","x":0,"y":60,"width":220,"height":120,
               "title":{"visible":true,"text":"Region"}},
              {"id":"s2","type":"slicer","x":0,"y":200,"width":220,"height":120,
               "title":{"visible":true,"text":"Segment"}},
              {"id":"s3","type":"slicer","x":0,"y":340,"width":220,"height":120,
               "title":{"visible":true,"text":"Channel"}},
              {"id":"s4","type":"slicer","x":0,"y":480,"width":220,"height":120,
               "title":{"visible":true,"text":"Product"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var densityFeedback = Assert.Single(result.Feedback["cognitiveLoad"].Where(item =>
            item.Text.StartsWith("Overview filter density:", StringComparison.Ordinal)));
        Assert.False(densityFeedback.Ok);
        Assert.Contains("uses 4 slicer(s)", densityFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("reduce or merge slicers", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_DensityFeedback_FlagsLongPageRisk()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Long Page","width":1280,"height":1180,"visuals":[
              {"id":"k1","type":"card","x":0,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"v1","type":"lineChart","x":220,"y":40,"width":520,"height":220,
               "title":{"visible":true,"text":"Revenue Trend"}},
              {"id":"v2","type":"clusteredBarChart","x":0,"y":360,"width":620,"height":260,
               "title":{"visible":true,"text":"Revenue by Region"}},
              {"id":"v3","type":"tableEx","x":0,"y":860,"width":760,"height":220,
               "title":{"visible":true,"text":"Detailed Transactions"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var longPageFeedback = Assert.Single(result.Feedback["density"].Where(item =>
            item.Text.StartsWith("Long-page risk:", StringComparison.Ordinal)));
        Assert.False(longPageFeedback.Ok);
        Assert.Contains("1180", longPageFeedback.Text, StringComparison.Ordinal);
        Assert.NotNull(longPageFeedback.AffectedVisuals);
        Assert.Contains(longPageFeedback.AffectedVisuals!, visual => visual.VisualId == "v3");
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("reads like a long page", StringComparison.OrdinalIgnoreCase));
        Assert.True(result.DensityScore < 90);
    }

    [Fact]
    public async Task ScoreAsync_DensityFeedback_FlagsWeakOverviewDetailSeparation()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Summary","visuals":[
              {"id":"k1","type":"card","x":0,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"k2","type":"card","x":200,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"Margin"}},
              {"id":"v1","type":"lineChart","x":420,"y":60,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue Trend"}},
              {"id":"v2","type":"clusteredBarChart","x":0,"y":170,"width":520,"height":240,
               "title":{"visible":true,"text":"Revenue by Region"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var separationFeedback = Assert.Single(result.Feedback["density"].Where(item =>
            item.Text.StartsWith("Overview/detail separation:", StringComparison.Ordinal)));
        Assert.False(separationFeedback.Ok);
        Assert.Contains("vertical separation", separationFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(separationFeedback.AffectedVisuals);
        Assert.True(separationFeedback.AffectedVisuals!.Count >= 3);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("vertical separation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_VisualBestPractices_FlagsMetricLabelInconsistency()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Summary","visuals":[
              {"id":"k1","type":"card","x":0,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"YTD Sales"}},
              {"id":"k2","type":"card","x":200,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"Sales YTD"}},
              {"id":"k3","type":"card","x":400,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"Sum of Margin"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var labelFeedback = Assert.Single(result.Feedback["visualBestPractices"].Where(item =>
            item.Text.StartsWith("Metric label consistency:", StringComparison.Ordinal)));
        Assert.False(labelFeedback.Ok);
        Assert.Equal(FindingTypes.StylePreference, labelFeedback.FindingType);
        Assert.Contains("YTD Sales", labelFeedback.Text, StringComparison.Ordinal);
        Assert.Contains("Sales YTD", labelFeedback.Text, StringComparison.Ordinal);
        Assert.Contains("Sum of Margin", labelFeedback.Text, StringComparison.Ordinal);
        Assert.NotNull(labelFeedback.AffectedVisuals);
        Assert.Equal(3, labelFeedback.AffectedVisuals!.Count);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("modifier placement consistent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_VisualBestPractices_FlagsPageStyleLanguageShift()
    {
        var tempDir = CreateTempPbirFolderFromPages(
            ("section-1",
            """
            {"displayName":"Overview","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"Revenue Overview"},
               "background":{"color":"F2F2F2"},
               "shadow":{"visible":true},
               "corners":{"radius":12}},
              {"id":"v2","type":"barChart","x":340,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"Margin Overview"},
               "background":{"color":"F2F2F2"},
               "shadow":{"visible":true},
               "corners":{"radius":12}}
            ]}
            """),
            ("section-2",
            """
            {"displayName":"Detail","visuals":[
              {"id":"v3","type":"barChart","x":0,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"Revenue Detail"},
               "background":{"color":"FFFFFF"},
               "shadow":{"visible":false},
               "corners":{"radius":0}},
              {"id":"v4","type":"barChart","x":340,"y":0,"width":320,"height":180,
               "title":{"visible":true,"text":"Margin Detail"},
               "background":{"color":"FFFFFF"},
               "shadow":{"visible":false},
               "corners":{"radius":0}}
            ]}
            """));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var styleFeedback = Assert.Single(result.Feedback["visualBestPractices"].Where(item =>
            item.Text.StartsWith("Page style language:", StringComparison.Ordinal)));
        Assert.False(styleFeedback.Ok);
        Assert.Contains("rounded surfaces", styleFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("square surfaces", styleFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(styleFeedback.AffectedVisuals);
        Assert.Equal(4, styleFeedback.AffectedVisuals!.Count);
    }

    [Fact]
    public async Task ScoreAsync_VisualBestPractices_FlagsLayoutConventionShiftAcrossPages()
    {
        var tempDir = CreateTempPbirFolderFromPages(
            ("section-1",
            """
            {"displayName":"Overview","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Overview"}},
              {"id":"s1","type":"slicer","x":0,"y":220,"width":180,"height":180,
               "title":{"visible":true,"text":"Region"}},
              {"id":"v1","type":"barChart","x":220,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Region"}}
            ]}
            """),
            ("section-2",
            """
            {"displayName":"Overview Detail","visuals":[
              {"id":"t2","type":"textbox","x":360,"y":0,"width":500,"height":40,
               "textbox":{"visible":true,"text":"Overview Detail"}},
              {"id":"s2","type":"slicer","x":280,"y":0,"width":220,"height":80,
               "title":{"visible":true,"text":"Segment"}},
              {"id":"v2","type":"barChart","x":0,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Margin by Segment"}}
            ]}
            """));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var conventionFeedback = Assert.Single(result.Feedback["visualBestPractices"].Where(item =>
            item.Text.StartsWith("Layout convention:", StringComparison.Ordinal)));
        Assert.False(conventionFeedback.Ok);
        Assert.Contains("title anchors shift", conventionFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("filter conventions shift", conventionFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("title alignment and filter-band conventions stable", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_ReportConsistencySummary_CapturesCrossPageConsistencySignals()
    {
        var tempDir = CreateTempPbirFolderFromPages(
            ("section-1",
            """
            {"displayName":"Overview","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Overview"}},
              {"id":"s1","type":"slicer","x":0,"y":220,"width":180,"height":180,
               "title":{"visible":true,"text":"Region"}},
              {"id":"k1","type":"card","x":220,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"YTD Sales"}},
              {"id":"status1","type":"card","x":420,"y":40,"width":180,"height":80,
               "title":{"visible":true,"text":"On Track"},
               "font":{"color":"#00AA00"}},
              {"id":"v1","type":"barChart","x":220,"y":180,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Region"}}
            ]}
            """),
            ("section-2",
            """
            {"displayName":"Overview Detail","visuals":[
              {"id":"t2","type":"textbox","x":360,"y":0,"width":500,"height":40,
               "textbox":{"visible":true,"text":"Overview Detail"}},
              {"id":"s2","type":"slicer","x":280,"y":0,"width":220,"height":80,
               "title":{"visible":true,"text":"Segment"}},
              {"id":"k2","type":"card","x":540,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"Sales YTD"}},
              {"id":"status2","type":"card","x":740,"y":40,"width":180,"height":80,
               "title":{"visible":true,"text":"On Track"},
               "font":{"color":"#CC3333"}},
              {"id":"v2","type":"barChart","x":0,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Margin by Segment"}}
            ]}
            """));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.ReportConsistencySummary);
        Assert.False(result.ReportConsistencySummary!.ConsistentTitleAnchors);
        Assert.False(result.ReportConsistencySummary.ConsistentFilterBand);
        Assert.False(result.ReportConsistencySummary.ConsistentMetricLabels);
        Assert.False(result.ReportConsistencySummary.ConsistentSemanticColors);
        Assert.Contains(result.ReportConsistencySummary.Findings, finding =>
            finding.Contains("title anchors", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ReportConsistencySummary.Findings, finding =>
            finding.Contains("filter", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ReportConsistencySummary.Findings, finding =>
            finding.Contains("metric label", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ReportConsistencySummary.Findings, finding =>
            finding.Contains("semantic color", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(result.PageScores);
        var overview = Assert.Single(result.PageScores!.Where(page => page.PageName == "Overview"));
        var detail = Assert.Single(result.PageScores.Where(page => page.PageName == "Overview Detail"));
        Assert.Contains(overview.ReportConsistencyNotes, note =>
            note.Contains("title anchor", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(overview.ReportConsistencyNotes, note =>
            note.Contains("semantic color", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(detail.ReportConsistencyNotes, note =>
            note.Contains("metric label", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(detail.ReportConsistencyNotes, note =>
            note.Contains("semantic color", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_ReportConsistencySummary_GroupsLayoutAndNavigationIssues()
    {
        var tempDir = CreateTempPbirFolderFromPages(
            ("section-1",
            """
            {"displayName":"Overview","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Overview"}},
              {"id":"home1","type":"actionButton","x":1120,"y":20,"width":120,"height":40,
               "title":{"visible":true,"text":"Home"}},
              {"id":"reset1","type":"actionButton","x":1120,"y":72,"width":120,"height":40,
               "title":{"visible":true,"text":"Reset Filters"}},
              {"id":"filter1","type":"slicer","x":0,"y":120,"width":180,"height":180,
               "title":{"visible":true,"text":"Region"}},
              {"id":"kpi1","type":"card","x":220,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"YTD Revenue"}},
              {"id":"chart1","type":"barChart","x":220,"y":180,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Region"}}
            ]}
            """),
            ("section-2",
            """
            {"displayName":"Detail","visuals":[
              {"id":"title2","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Detail"}},
              {"id":"home2","type":"actionButton","x":1120,"y":20,"width":120,"height":40,
               "title":{"visible":true,"text":"Home"}},
              {"id":"reset2","type":"actionButton","x":1120,"y":72,"width":120,"height":40,
               "title":{"visible":true,"text":"Reset Filters"}},
              {"id":"filter2","type":"slicer","x":0,"y":120,"width":180,"height":180,
               "title":{"visible":true,"text":"Segment"}},
              {"id":"kpi2","type":"card","x":220,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"YTD Revenue"}},
              {"id":"chart2","type":"barChart","x":220,"y":180,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Segment"}}
            ]}
            """),
            ("section-3",
            """
            {"displayName":"Exceptions","visuals":[
              {"id":"title3","type":"textbox","x":360,"y":0,"width":560,"height":40,
               "textbox":{"visible":true,"text":"Exceptions"}},
              {"id":"filter3","type":"slicer","x":280,"y":0,"width":220,"height":80,
               "title":{"visible":true,"text":"Business Unit"}},
              {"id":"kpi3","type":"card","x":560,"y":180,"width":180,"height":100,
               "title":{"visible":true,"text":"YTD Revenue"}},
              {"id":"chart3","type":"barChart","x":120,"y":300,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue Exceptions"}}
            ]}
            """));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.ReportConsistencySummary);
        var summary = result.ReportConsistencySummary!;
        var layoutIssue = Assert.Single(summary!.Issues.Where(issue =>
            issue.IssueCategory == "layoutPattern"));
        Assert.Contains("Exceptions", layoutIssue.AffectedPages);
        Assert.Equal("medium", layoutIssue.Severity);
        Assert.Equal("high", layoutIssue.Confidence);
        Assert.Contains("layout pattern", layoutIssue.RecommendedRemediation, StringComparison.OrdinalIgnoreCase);

        var navigationIssue = Assert.Single(summary.Issues.Where(issue =>
            issue.Category == "navigation"));
        Assert.Contains("Exceptions", navigationIssue.AffectedPages);
        Assert.Contains("partially detectable", navigationIssue.OverallFinding, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("medium", navigationIssue.Confidence);

        var exceptionPage = Assert.Single(result.PageScores!.Where(page => page.PageName == "Exceptions"));
        Assert.Contains(exceptionPage.ReportConsistencyNotes, note =>
            note.Contains("navigation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(exceptionPage.ReportConsistencyNotes, note =>
            note.Contains("layout", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_ReportConsistencySummary_FlagsFuzzyMetricLabelDriftWithCanonicalSuggestion()
    {
        var tempDir = CreateTempPbirFolderFromPages(
            ("section-1",
            """
            {"displayName":"Overview","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Overview"}},
              {"id":"kpi1","type":"card","x":220,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"CY Sales"}},
              {"id":"kpi2","type":"card","x":420,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Margin %"}}
            ]}
            """),
            ("section-2",
            """
            {"displayName":"Finance Detail","visuals":[
              {"id":"title2","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Finance Detail"}},
              {"id":"kpi3","type":"card","x":220,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Current Year Sales"}},
              {"id":"kpi4","type":"card","x":420,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Gross Margin %"}}
            ]}
            """));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.ReportConsistencySummary);
        var summary = result.ReportConsistencySummary!;
        var metricIssue = Assert.Single(summary!.Issues.Where(issue =>
            issue.Category == "metricGovernance"));
        Assert.Equal("low", metricIssue.Severity);
        Assert.Equal("medium", metricIssue.Confidence);
        Assert.Contains("Current Year Sales", metricIssue.RecommendedRemediation, StringComparison.Ordinal);
        Assert.Contains("CY Sales", metricIssue.OverallFinding, StringComparison.Ordinal);
        Assert.Contains("Current Year Sales", metricIssue.OverallFinding, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScoreAsync_ReportConsistencySummary_FlagsExtendedSemanticRoleColorDrift()
    {
        var tempDir = CreateTempPbirFolderFromPages(
            ("section-1",
            """
            {"displayName":"Overview","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Overview"}},
              {"id":"actual1","type":"card","x":220,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Actual"},
               "font":{"color":"#0055CC"}},
              {"id":"budget1","type":"card","x":420,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Budget"},
               "font":{"color":"#999999"}}
            ]}
            """),
            ("section-2",
            """
            {"displayName":"Variance","visuals":[
              {"id":"title2","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Variance"}},
              {"id":"actual2","type":"card","x":220,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Actual"},
               "font":{"color":"#FF9900"}},
              {"id":"budget2","type":"card","x":420,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Budget"},
               "font":{"color":"#555555"}}
            ]}
            """));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.ReportConsistencySummary);
        var summary = result.ReportConsistencySummary!;
        var semanticIssue = Assert.Single(summary!.Issues.Where(issue =>
            issue.Category == "semanticColors"));
        Assert.Equal("medium", semanticIssue.Severity);
        Assert.Equal("high", semanticIssue.Confidence);
        Assert.Contains("Actual", semanticIssue.OverallFinding, StringComparison.Ordinal);
        Assert.Contains("Budget", semanticIssue.OverallFinding, StringComparison.Ordinal);
        Assert.Contains("same semantic roles", semanticIssue.RecommendedRemediation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScoreAsync_InferredStorySummary_DetectsExecutiveOverviewTrendStory()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Overview","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Revenue Overview"}},
              {"id":"kpi1","type":"card","x":220,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"kpi2","type":"card","x":420,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Margin"}},
              {"id":"trend1","type":"lineChart","x":220,"y":180,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Month"},
               "fieldRoles":{"category":["Month"],"value":["Revenue"],"measure":["Revenue"]}},
              {"id":"comp1","type":"barChart","x":680,"y":180,"width":360,"height":220,
               "title":{"visible":true,"text":"Revenue by Region"},
               "fieldRoles":{"category":["Region"],"value":["Revenue"],"measure":["Revenue"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.InferredStorySummary);
        Assert.Equal("executiveOverview", result.InferredStorySummary!.IntentProfile);
        Assert.Equal("executive overview + trend + comparison", result.InferredStorySummary.StoryArchetype);
        Assert.Equal("high", result.InferredStorySummary.Confidence);
        Assert.Contains("Revenue", result.InferredStorySummary.InferredStory, StringComparison.Ordinal);
        Assert.Contains("region", result.InferredStorySummary.InferredStory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.InferredStorySummary.Evidence, evidence =>
            evidence.Contains("2 KPI cards", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.InferredStorySummary.Evidence, evidence =>
            evidence.Contains("lineChart", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_InferredStorySummary_PrefersBusinessLabelsFromRoleMetadata()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Overview","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Overview"}},
              {"id":"trend1","type":"lineChart","x":220,"y":180,"width":420,"height":220,
               "title":{"visible":true,"text":"Trend"},
               "fieldRoles":{
                 "category":[{"queryRef":"dimRegion[RegionKey]","displayName":"Sales Region","synonyms":["Region"],"description":"Regional business grouping"}],
                 "value":[{"queryRef":"factSales[RevVar]","displayName":"Revenue Variance","synonyms":["Revenue Gap"],"description":"Difference between actual revenue and target revenue"}],
                 "measure":[{"queryRef":"factSales[RevVar]","displayName":"Revenue Variance","synonyms":["Revenue Gap"],"description":"Difference between actual revenue and target revenue"}]
               }}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.InferredStorySummary);
        Assert.Contains("Revenue Variance", result.InferredStorySummary!.InferredStory, StringComparison.Ordinal);
        Assert.DoesNotContain("factSales", result.InferredStorySummary.InferredStory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sales region", result.InferredStorySummary.InferredStory, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("medium", result.InferredStorySummary.Confidence);
        Assert.Contains(result.InferredStorySummary.Evidence, evidence =>
            evidence.Contains("semantic metadata", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.InferredStorySummary.Evidence, evidence =>
            evidence.Contains("Revenue Variance", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ScoreAsync_InferredStorySummary_AvoidsRepeatedPerformancePhrasing()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Overview","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Executive Overview"}},
              {"id":"kpi1","type":"card","x":220,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Performance"}},
              {"id":"comp1","type":"barChart","x":680,"y":180,"width":360,"height":220,
               "title":{"visible":true,"text":"Performance by Region"},
               "fieldRoles":{"category":["Region"],"value":["Performance"],"measure":["Performance"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.InferredStorySummary);
        Assert.DoesNotContain("performance performance", result.InferredStorySummary!.InferredStory, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScoreAsync_PageScores_IncludeDetailReferenceStorySummary()
    {
        var tempDir = CreateTempPbirFolderFromPages(
            ("section-1",
            """
            {"displayName":"Overview","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Overview"}},
              {"id":"kpi1","type":"card","x":220,"y":40,"width":180,"height":100,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"trend1","type":"lineChart","x":220,"y":180,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Month"},
               "fieldRoles":{"category":["Month"],"value":["Revenue"],"measure":["Revenue"]}}
            ]}
            """),
            ("section-2",
            """
            {"displayName":"Transaction Detail","visuals":[
              {"id":"title2","type":"textbox","x":0,"y":0,"width":460,"height":40,
               "textbox":{"visible":true,"text":"Transaction Detail"}},
              {"id":"table1","type":"table","x":0,"y":80,"width":980,"height":420,
               "title":{"visible":true,"text":"Transaction Detail Table"},
               "fieldRoles":{"category":["Customer","Order Date"],"value":["Revenue"],"measure":["Revenue"]}}
            ]}
            """));
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var detail = Assert.Single(result.PageScores!.Where(page => page.PageName == "Transaction Detail"));
        Assert.NotNull(detail.InferredStorySummary);
        Assert.Equal("detailReference", detail.InferredStorySummary!.IntentProfile);
        Assert.Equal("detail reference", detail.InferredStorySummary.StoryArchetype);
        Assert.Contains("detailed reference", detail.InferredStorySummary.InferredStory, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(detail.InferredStorySummary.Evidence, evidence =>
            evidence.Contains("table", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_ReviewIntelligence_ProducesExecutiveActionabilityProfileAndBenchmark()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Review","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":520,"height":40,
               "textbox":{"visible":true,"text":"Revenue vs Target"}},
              {"id":"kpi1","type":"card","x":40,"y":60,"width":180,"height":100,
               "title":{"visible":true,"text":"Revenue vs Budget"}},
              {"id":"kpi2","type":"card","x":250,"y":60,"width":180,"height":100,
               "title":{"visible":true,"text":"Margin YoY"}},
              {"id":"trend1","type":"lineChart","x":40,"y":200,"width":460,"height":220,
               "title":{"visible":true,"text":"Revenue by Month"},
               "fieldRoles":{"category":["Month"],"value":["Revenue"],"measure":["Revenue"]}},
              {"id":"comp1","type":"barChart","x":540,"y":200,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue at Risk by Region"},
               "fieldRoles":{"category":["Region"],"value":["Revenue"],"measure":["Revenue"]}},
              {"id":"table1","type":"table","x":40,"y":460,"width":920,"height":220,
               "title":{"visible":true,"text":"Driver Detail"},
               "fieldRoles":{"category":["Region"],"value":["Revenue Variance"],"measure":["Revenue Variance"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.PageIntentProfile);
        Assert.Equal("executive", result.PageIntentProfile!.InferredProfile);
        Assert.Equal("high", result.PageIntentProfile.ActionabilityExpectation);
        Assert.Contains(result.PageIntentProfile.ReviewGuidance, guidance =>
            guidance.Contains("target", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(result.ActionabilityBreakdown);
        Assert.True(result.ActionabilityBreakdown!.TargetBenchmarkPresent);
        Assert.True(result.ActionabilityBreakdown.ExceptionVisibility);
        Assert.True(result.ActionabilityBreakdown.PriorPeriodContext);
        Assert.True(result.ActionabilityBreakdown.DrillPathPresent);
        Assert.True(result.ActionabilityBreakdown.Score >= 80.0);

        Assert.NotNull(result.BenchmarkComparison);
        Assert.Equal("executive scorecard", result.BenchmarkComparison!.Archetype);
        Assert.False(result.BenchmarkComparison.BeautifulButUseless);
        Assert.Contains("decision", result.BenchmarkComparison.Insight, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScoreAsync_ReviewIntelligence_FlagsBeautifulButUselessExecutivePage()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Review","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Executive Review"}},
              {"id":"kpi1","type":"card","x":40,"y":60,"width":180,"height":100,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"kpi2","type":"card","x":250,"y":60,"width":180,"height":100,
               "title":{"visible":true,"text":"Margin"}},
              {"id":"comp1","type":"clusteredColumnChart","x":40,"y":200,"width":460,"height":220,
               "title":{"visible":true,"text":"Revenue by Region"},
               "fieldRoles":{"category":["Region"],"value":["Revenue"],"measure":["Revenue"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.PageIntentProfile);
        Assert.Equal("executive", result.PageIntentProfile!.InferredProfile);

        Assert.NotNull(result.ActionabilityBreakdown);
        Assert.False(result.ActionabilityBreakdown!.TargetBenchmarkPresent);
        Assert.False(result.ActionabilityBreakdown.PriorPeriodContext);
        Assert.True(result.ActionabilityBreakdown.DrillPathPresent);
        Assert.True(result.ActionabilityBreakdown.Score < 50.0);
        Assert.Contains(result.ActionabilityBreakdown.Gaps, gap =>
            gap.Contains("target", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(result.BenchmarkComparison);
        Assert.True(result.BenchmarkComparison!.BeautifulButUseless);
        Assert.Contains("beautiful but useless", result.BenchmarkComparison.Insight, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScoreAsync_GraphicalPerception_FlagsCategoricalLineChart()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"lineChart","x":0,"y":0,"width":420,"height":220,
               "title":{"visible":true,"text":"Sales by Region"},
               "fieldRoles":{"category":["Region"],"value":["Sales"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var sequentialFitFeedback = Assert.Single(result.Feedback["graphicalPerception"].Where(item =>
            item.Text.StartsWith("Sequential fit:", StringComparison.Ordinal)));
        Assert.False(sequentialFitFeedback.Ok);
        Assert.Equal(FindingTypes.StrongHeuristic, sequentialFitFeedback.FindingType);
        Assert.Contains("categorical rather than sequential", sequentialFitFeedback.Text, StringComparison.OrdinalIgnoreCase);
        var affectedVisual = Assert.Single(sequentialFitFeedback.AffectedVisuals!);
        Assert.Equal("v1", affectedVisual.VisualId);
    }

    [Fact]
    public async Task ScoreAsync_VisualBestPractices_FlagsRedundantLabels()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"clusteredColumnChart","x":0,"y":0,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Region"},
               "axis":{"visible":true},
               "dataLabels":{"visible":true}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var labelFeedback = Assert.Single(result.Feedback["visualBestPractices"].Where(item =>
            item.Text.StartsWith("Redundant labeling:", StringComparison.Ordinal)));
        Assert.False(labelFeedback.Ok);
        Assert.Contains("direct data labels and axis labels", labelFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("axis labels or direct data labels", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_GraphicalPerception_FlagsKpiPagesWithoutComparisonVisual()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Summary","visuals":[
              {"id":"k1","type":"card","x":0,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"k2","type":"card","x":200,"y":40,"width":180,"height":120,
               "title":{"visible":true,"text":"Margin"}},
              {"id":"v1","type":"lineChart","x":0,"y":220,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue Trend"},
               "fieldRoles":{"category":["Month"],"value":["Revenue"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var comparisonFeedback = Assert.Single(result.Feedback["graphicalPerception"].Where(item =>
            item.Text.StartsWith("Comparative structure:", StringComparison.Ordinal)));
        Assert.False(comparisonFeedback.Ok);
        Assert.Contains("strong bar/column comparison visual", comparisonFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(comparisonFeedback.AffectedVisuals);
        Assert.Equal(2, comparisonFeedback.AffectedVisuals!.Count);
    }

    [Fact]
    public async Task ScoreAsync_VisualBestPractices_FlagsExecutiveVarianceGap()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Summary","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Executive Summary"}},
              {"id":"k1","type":"card","x":0,"y":60,"width":180,"height":120,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"k2","type":"card","x":200,"y":60,"width":180,"height":120,
               "title":{"visible":true,"text":"Margin"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var varianceFeedback = Assert.Single(result.Feedback["visualBestPractices"].Where(item =>
            item.Text.StartsWith("Executive variance context:", StringComparison.Ordinal)));
        Assert.False(varianceFeedback.Ok);
        Assert.Contains("lacks target, variance, prior-period, or trend context", varianceFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.Recommendations, recommendation =>
            recommendation.Contains("target, variance, prior-period, or trend context", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_VisualBestPractices_EscalatesPieFeedbackOnOverviewPage()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Overview","visuals":[
              {"id":"v1","type":"donutChart","x":0,"y":0,"width":320,"height":220,
               "title":{"visible":true,"text":"Revenue Mix"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        var pieFeedback = Assert.Single(result.Feedback["visualBestPractices"].Where(item =>
            item.Text.StartsWith("Pie avoidance:", StringComparison.Ordinal)));
        Assert.False(pieFeedback.Ok);
        Assert.Contains("overview-page use", pieFeedback.Text, StringComparison.OrdinalIgnoreCase);
        var affectedVisual = Assert.Single(pieFeedback.AffectedVisuals!);
        Assert.Equal("v1", affectedVisual.VisualId);
    }

    // ── Bookmark-aware (per-state) scoring ───────────────────────────────────

    [Fact]
    public async Task ScoreAsync_PageWithoutBookmarks_LeavesPerStateScoresNull()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":200,"height":160},
              {"id":"v2","type":"barChart","x":220,"y":0,"width":200,"height":160},
              {"id":"v3","type":"barChart","x":440,"y":0,"width":200,"height":160}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.Null(result.PerStateScores);
    }

    [Fact]
    public async Task ScoreAsync_PageWithBookmarks_PopulatesPerStateScoresAndAveragesFrameworks()
    {
        // 8 barChart visuals so the default state exceeds the cognitive-load threshold (6) and
        // the bookmark states fall below it. The composite scores should therefore differ
        // between states and the page's framework scores should be the state averages.
        var pageJson = """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":100,"height":100},
              {"id":"v2","type":"barChart","x":110,"y":0,"width":100,"height":100},
              {"id":"v3","type":"barChart","x":220,"y":0,"width":100,"height":100},
              {"id":"v4","type":"barChart","x":330,"y":0,"width":100,"height":100},
              {"id":"v5","type":"barChart","x":440,"y":0,"width":100,"height":100},
              {"id":"v6","type":"barChart","x":550,"y":0,"width":100,"height":100},
              {"id":"v7","type":"barChart","x":660,"y":0,"width":100,"height":100},
              {"id":"v8","type":"barChart","x":770,"y":0,"width":100,"height":100}
            ]}
            """;
        var bookmarksJson = """
            [
              {"id":"bm1","displayName":"Filter A","state":{"Page1":{"visuals":{"v1":{},"v2":{},"v3":{}}}}},
              {"id":"bm2","displayName":"Filter B","state":{"Page1":{"visuals":{"v4":{},"v5":{}}}}}
            ]
            """;
        var tempDir = CreateTempPbirFolderWithBookmarks(pageJson, bookmarksJson);
        var svc = BuildScoringService();

        // Single-page mode so top-level result.PerStateScores carries the bookmark overlay.
        var result = await svc.ScoreAsync(tempDir, config: null, pageName: "Page1");

        Assert.NotNull(result.PerStateScores);
        Assert.Equal(3, result.PerStateScores!.Count);
        Assert.Contains("Default", result.PerStateScores.Keys);
        Assert.Contains("Filter A", result.PerStateScores.Keys);
        Assert.Contains("Filter B", result.PerStateScores.Keys);

        // The 8-visual default state has a different cognitive-load profile than the 3- and
        // 2-visual bookmark states, so the per-state composites must not all be identical.
        Assert.True(
            result.PerStateScores.Values.Distinct().Count() > 1,
            $"Expected per-state composites to differ but got {string.Join(", ", result.PerStateScores)}.");

        // The page composite must be the average of the per-state composites (within rounding).
        var expectedComposite = Math.Round(result.PerStateScores.Values.Average(), 2);
        Assert.InRange(result.CompositeScore, expectedComposite - 0.5, expectedComposite + 0.5);

        // The overlay should surface in the recommendations list.
        Assert.Contains(result.Recommendations, r => r.Contains("Bookmark-aware scoring", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreAsync_BookmarkTargetingOtherPageOnly_LeavesPagePerStateScoresNull()
    {
        // The page has v1..v3 but the bookmark only references w1, which lives on another page.
        // The bookmark must not produce a layout state for this page.
        var pageJson = """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":100,"height":100},
              {"id":"v2","type":"barChart","x":110,"y":0,"width":100,"height":100},
              {"id":"v3","type":"barChart","x":220,"y":0,"width":100,"height":100}
            ]}
            """;
        var bookmarksJson = """
            [
              {"id":"bm1","displayName":"Filter Other","state":{"OtherPage":{"visuals":{"w1":{}}}}}
            ]
            """;
        var tempDir = CreateTempPbirFolderWithBookmarks(pageJson, bookmarksJson);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.Null(result.PerStateScores);
    }

    [Fact]
    public async Task ScoreAsync_ReportMode_PageWithBookmarksExposesPerStateScoresOnPageScore()
    {
        // Single-page report so the per-page bookmark overlay lands in PageScores[0].PerStateScores.
        var pageJson = """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":0,"width":100,"height":100},
              {"id":"v2","type":"barChart","x":110,"y":0,"width":100,"height":100},
              {"id":"v3","type":"barChart","x":220,"y":0,"width":100,"height":100},
              {"id":"v4","type":"barChart","x":330,"y":0,"width":100,"height":100}
            ]}
            """;
        var bookmarksJson = """
            [
              {"id":"bm1","displayName":"Focus Mode","state":{"Page1":{"visuals":{"v1":{},"v2":{}}}}}
            ]
            """;
        var tempDir = CreateTempPbirFolderWithBookmarks(pageJson, bookmarksJson);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);

        Assert.NotNull(result.PageScores);
        var pageScore = Assert.Single(result.PageScores!);
        Assert.NotNull(pageScore.PerStateScores);
        Assert.Equal(2, pageScore.PerStateScores!.Count);
        Assert.Contains("Default", pageScore.PerStateScores.Keys);
        Assert.Contains("Focus Mode", pageScore.PerStateScores.Keys);
    }

    // ── Parallel per-page scoring (REC-09) ───────────────────────────────────

    [Fact]
    public async Task ScoreAsync_MultiPageReport_ParallelRunProducesDeterministicComposite()
    {
        // Six-page report exercises the parallel per-page loop. Running the same fixture twice
        // must produce identical composite scores and identical per-page composite scores —
        // parallelization must not introduce nondeterminism.
        var tempDir = CreateTempPbirFolder(
            [
                ("section-1", "Overview"),
                ("section-2", "Customer Analysis"),
                ("section-3", "Order Detail"),
                ("section-4", "Revenue Trends"),
                ("section-5", "Margin Mix"),
                ("section-6", "Region Drilldown"),
            ],
            ["section-1", "section-2", "section-3", "section-4", "section-5", "section-6"]);
        var svc = BuildScoringService();

        var first = await svc.ScoreAsync(tempDir);
        var second = await svc.ScoreAsync(tempDir);

        Assert.Equal(first.CompositeScore, second.CompositeScore);
        Assert.NotNull(first.PageScores);
        Assert.NotNull(second.PageScores);
        Assert.Equal(first.PageScores!.Count, second.PageScores!.Count);

        // Page order preserved and per-page composites identical between runs.
        for (var i = 0; i < first.PageScores.Count; i++)
        {
            Assert.Equal(first.PageScores[i].PageName, second.PageScores[i].PageName);
            Assert.Equal(first.PageScores[i].CompositeScore, second.PageScores[i].CompositeScore);
        }
    }

    // ── Composite: all sub-scores 100 ────────────────────────────────────────

    /// <summary>
    /// When all configured framework sub-scores are 100, the weighted composite must be 100.
    /// </summary>
    [Fact]
    public void CompositeScore_AllSubScores100_Returns100()
    {
        // Arrange — use an explicit config-driven weight set.
        var result = new ScoreResult
        {
            GestaltScore             = 100,
            CognitiveLoadScore       = 100,
            DataInkScore             = 100,
            AccessibilityScore       = 100,
            VisualBestPracticesScore = 100,
            StephenFewScore          = 100,
            FrameworkWeights         = CreateFrameworkWeights(
                gestalt: 15,
                cognitiveLoad: 20,
                dataInk: 15,
                accessibility: 15,
                visualBestPractices: 20,
                stephenFew: 15),
        };

        // Assert
        Assert.Equal(100.0, result.CompositeScore);
    }

    // ── Composite: all sub-scores 0 ──────────────────────────────────────────

    /// <summary>
    /// When all configured framework sub-scores are 0, the weighted composite must be 0.
    /// </summary>
    [Fact]
    public void CompositeScore_AllSubScores0_Returns0()
    {
        // Arrange — use an explicit config-driven weight set.
        var result = new ScoreResult
        {
            GestaltScore             = 0,
            CognitiveLoadScore       = 0,
            DataInkScore             = 0,
            AccessibilityScore       = 0,
            VisualBestPracticesScore = 0,
            StephenFewScore          = 0,
            FrameworkWeights         = CreateFrameworkWeights(
                gestalt: 15,
                cognitiveLoad: 20,
                dataInk: 15,
                accessibility: 15,
                visualBestPractices: 20,
                stephenFew: 15),
        };

        // Assert
        Assert.Equal(0.0, result.CompositeScore);
    }

    [Fact]
    public async Task ScoreAsync_InternalStorySignalRegistry_CapturesRepresentativeLayoutSignals()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Summary","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":520,"height":40,
               "textbox":{"visible":true,"text":"Revenue vs Target"}},
              {"id":"k1","type":"card","x":0,"y":60,"width":180,"height":120,
               "title":{"visible":true,"text":"Revenue"}},
              {"id":"k2","type":"card","x":200,"y":60,"width":180,"height":120,
               "title":{"visible":true,"text":"Margin"}},
              {"id":"v1","type":"lineChart","x":0,"y":220,"width":480,"height":220,
               "title":{"visible":true,"text":"Revenue Trend"},
               "fieldRoles":{"category":["Month"],"value":["Revenue"],"measure":["Revenue"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var entries = GetInternalStorySignalRegistryEntries(result);

        var titleSignal = Assert.Single(entries, entry => entry.Id == "layout.meaningfulVisibleTitle");
        Assert.True(titleSignal.Fired);
        Assert.Equal("Revenue vs Target", titleSignal.RawValue);

        var kpiSignal = Assert.Single(entries, entry => entry.Id == "layout.topScanKpiCount");
        Assert.True(kpiSignal.Fired);
        Assert.Equal("2", kpiSignal.RawValue);

        var leadVisualSignal = Assert.Single(entries, entry => entry.Id == "layout.leadVisualType");
        Assert.True(leadVisualSignal.Fired);
        Assert.Equal("lineChart", leadVisualSignal.RawValue);
    }

    [Fact]
    public async Task ScoreAsync_InternalStorySignalRegistry_CapturesRepresentativeSemanticSignals()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Overview","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Revenue Variance by Region"}},
              {"id":"trend1","type":"lineChart","x":220,"y":180,"width":420,"height":220,
               "title":{"visible":true,"text":"Trend"},
               "fieldRoles":{
                 "category":[{"queryRef":"dimRegion[RegionKey]","displayName":"Sales Region","synonyms":["Region"],"description":"Regional business grouping"}],
                 "value":[{"queryRef":"factSales[RevVar]","displayName":"Revenue Variance","synonyms":["Revenue Gap"],"description":"Difference between actual revenue and target revenue"}],
                 "measure":[{"queryRef":"factSales[RevVar]","displayName":"Revenue Variance","synonyms":["Revenue Gap"],"description":"Difference between actual revenue and target revenue"}]
               }}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var entries = GetInternalStorySignalRegistryEntries(result);

        var metricSignal = Assert.Single(entries, entry => entry.Id == "semantic.primaryMetric");
        Assert.True(metricSignal.Fired);
        Assert.Equal("Revenue Variance", metricSignal.RawValue);

        var dimensionSignal = Assert.Single(entries, entry => entry.Id == "semantic.primaryDimension");
        Assert.True(dimensionSignal.Fired);
        Assert.Equal("Sales Region", dimensionSignal.RawValue);

        var metadataSignal = Assert.Single(entries, entry => entry.Id == "semantic.richMetadataSupport");
        Assert.True(metadataSignal.Fired);
    }

    [Fact]
    public async Task ScoreAsync_InternalStorySignalRegistry_CapturesRepresentativeContextSignals()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Executive Review","visuals":[
              {"id":"title1","type":"textbox","x":0,"y":0,"width":520,"height":40,
               "textbox":{"visible":true,"text":"Revenue vs Target"}},
              {"id":"s1","type":"slicer","x":0,"y":60,"width":220,"height":120,
               "title":{"visible":true,"text":"Region"}},
              {"id":"k1","type":"card","x":260,"y":60,"width":180,"height":120,
               "title":{"visible":true,"text":"Revenue vs Budget"}},
              {"id":"k2","type":"card","x":460,"y":60,"width":180,"height":120,
               "title":{"visible":true,"text":"Margin YoY"}},
              {"id":"v1","type":"lineChart","x":0,"y":220,"width":480,"height":220,
               "title":{"visible":true,"text":"Revenue Last Year Trend"},
               "fieldRoles":{"category":["Month"],"value":["Revenue"],"measure":["Revenue"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var entries = GetInternalStorySignalRegistryEntries(result);

        Assert.True(Assert.Single(entries, entry => entry.Id == "context.targetBenchmarkPresent").Fired);
        Assert.True(Assert.Single(entries, entry => entry.Id == "context.priorPeriodContext").Fired);
        Assert.True(Assert.Single(entries, entry => entry.Id == "context.slicerPresent").Fired);
    }

    [Fact]
    public async Task ScoreAsync_InternalStorySignalRegistry_PartialInputDegradesGracefully()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Sparse Page","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":420,"height":40,
               "textbox":{"visible":true,"text":"Revenue Overview"}},
              {"id":"v1","type":"barChart","x":0,"y":120,"width":420,"height":220}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var entries = GetInternalStorySignalRegistryEntries(result);

        Assert.NotEmpty(entries);
        Assert.True(Assert.Single(entries, entry => entry.Id == "layout.meaningfulVisibleTitle").Fired);
        Assert.False(Assert.Single(entries, entry => entry.Id == "semantic.primaryDimension").Fired);
        Assert.False(Assert.Single(entries, entry => entry.Id == "semantic.richMetadataSupport").Fired);
    }

    [Theory]
    [InlineData(
        "PerformanceMonitor",
        """
        {"displayName":"Performance Monitor","visuals":[
          {"id":"t1","type":"textbox","x":0,"y":0,"width":520,"height":40,
           "textbox":{"visible":true,"text":"Performance Monitor"}},
          {"id":"k1","type":"card","x":0,"y":60,"width":180,"height":120,
           "title":{"visible":true,"text":"Revenue vs Target"}},
          {"id":"k2","type":"card","x":200,"y":60,"width":180,"height":120,
           "title":{"visible":true,"text":"Margin"}},
          {"id":"v1","type":"barChart","x":0,"y":220,"width":480,"height":220,
           "title":{"visible":true,"text":"Revenue by Region"},
           "fieldRoles":{"category":["Region"],"value":["Revenue"],"measure":["Revenue"]}}
        ]}
        """)]
    [InlineData(
        "TrendException",
        """
        {"displayName":"Trend and Exception","visuals":[
          {"id":"t1","type":"textbox","x":0,"y":0,"width":560,"height":40,
           "textbox":{"visible":true,"text":"Trend and Exception Monitor"}},
          {"id":"k1","type":"card","x":0,"y":60,"width":180,"height":120,
           "title":{"visible":true,"text":"Revenue vs Target"}},
          {"id":"v1","type":"lineChart","x":0,"y":220,"width":520,"height":220,
           "title":{"visible":true,"text":"Revenue Last Year Trend"},
           "fieldRoles":{"category":["Month"],"value":["Revenue"],"measure":["Revenue"]}}
        ]}
        """)]
    [InlineData(
        "Ranking",
        """
        {"displayName":"Top Regions","visuals":[
          {"id":"t1","type":"textbox","x":0,"y":0,"width":520,"height":40,
           "textbox":{"visible":true,"text":"Top Regions by Revenue"}},
          {"id":"v1","type":"barChart","x":0,"y":140,"width":520,"height":260,
           "title":{"visible":true,"text":"Top 10 Regions"},
           "fieldRoles":{"category":["Region"],"value":["Revenue"],"measure":["Revenue"]}}
        ]}
        """)]
    [InlineData(
        "Comparison",
        """
        {"displayName":"Product Comparison","visuals":[
          {"id":"t1","type":"textbox","x":0,"y":0,"width":560,"height":40,
           "textbox":{"visible":true,"text":"Actual vs Budget by Product"}},
          {"id":"v1","type":"clusteredColumnChart","x":0,"y":140,"width":520,"height":260,
           "title":{"visible":true,"text":"Actual vs Budget"},
           "fieldRoles":{"category":["Product"],"value":["Revenue"],"measure":["Revenue"]}}
        ]}
        """)]
    [InlineData(
        "Decomposition",
        """
        {"displayName":"Revenue Mix","visuals":[
          {"id":"t1","type":"textbox","x":0,"y":0,"width":520,"height":40,
           "textbox":{"visible":true,"text":"Revenue Share by Segment"}},
          {"id":"v1","type":"stackedColumnChart","x":0,"y":140,"width":520,"height":260,
           "title":{"visible":true,"text":"Segment Share"},
           "fieldRoles":{"category":["Segment"],"value":["Revenue"],"measure":["Revenue"]}}
        ]}
        """)]
    [InlineData(
        "NarrativeWalkthrough",
        """
        {"displayName":"Sales Story","visuals":[
          {"id":"t1","type":"textbox","x":0,"y":0,"width":560,"height":40,
           "textbox":{"visible":true,"text":"Why Revenue Changed: Story Walkthrough"}},
          {"id":"k1","type":"card","x":0,"y":60,"width":180,"height":120,
           "title":{"visible":true,"text":"Revenue"}},
          {"id":"v1","type":"lineChart","x":0,"y":220,"width":520,"height":220,
           "title":{"visible":true,"text":"Revenue Trend"},
           "fieldRoles":{
             "category":[{"queryRef":"Date[Month]","displayName":"Month","synonyms":["Month"],"description":"Month of sale"}],
             "value":[{"queryRef":"Sales[Revenue]","displayName":"Revenue","synonyms":["Revenue"],"description":"Net revenue"}],
             "measure":[{"queryRef":"Sales[Revenue]","displayName":"Revenue","synonyms":["Revenue"],"description":"Net revenue"}]
           }},
          {"id":"v2","type":"barChart","x":560,"y":220,"width":420,"height":220,
           "title":{"visible":true,"text":"Revenue by Segment"},
           "fieldRoles":{
             "category":[{"queryRef":"DimSegment[Segment]","displayName":"Segment","synonyms":["Segment"],"description":"Customer segment"}],
             "value":[{"queryRef":"Sales[Revenue]","displayName":"Revenue","synonyms":["Revenue"],"description":"Net revenue"}],
             "measure":[{"queryRef":"Sales[Revenue]","displayName":"Revenue","synonyms":["Revenue"],"description":"Net revenue"}]
           }}
        ]}
        """)]
    public async Task ScoreAsync_InternalArchetypeClassification_SelectsExpectedArchetype(string expectedArchetypeId, string pageJson)
    {
        var tempDir = CreateTempPbirFolderFromPageJson(pageJson);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var classification = GetInternalArchetypeClassification(result);

        Assert.Equal(expectedArchetypeId, classification.BestFitArchetypeId);
        Assert.Equal(expectedArchetypeId, classification.Level1ValidationHarness.SystemChoice);
        Assert.NotEmpty(classification.ArchetypeResults);
        Assert.Equal(6, classification.ArchetypeResults.Count);
        Assert.Contains(classification.ArchetypeResults, match => match.ArchetypeId == expectedArchetypeId && match.MatchScore > 0.5d);
    }

    [Fact]
    public async Task ScoreAsync_InternalArchetypeClassification_WeakMixedSignalsProduceLowerConfidence()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Mixed Signals","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":560,"height":40,
               "textbox":{"visible":true,"text":"Revenue Overview"}},
              {"id":"v1","type":"lineChart","x":0,"y":140,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Segment"},
               "fieldRoles":{"category":["Segment"],"value":["Revenue"],"measure":["Revenue"]}},
              {"id":"v2","type":"stackedColumnChart","x":460,"y":140,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue Share"},
               "fieldRoles":{"category":["Category"],"value":["Revenue"],"measure":["Revenue"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var classification = GetInternalArchetypeClassification(result);
        var bestMatch = Assert.Single(classification.ArchetypeResults.Where(match => match.ArchetypeId == classification.BestFitArchetypeId));

        Assert.Equal("Low", bestMatch.MatchConfidence);
        Assert.True(bestMatch.MatchScore < 0.75d, $"Expected a subdued match score but got {bestMatch.MatchScore}.");
    }

    [Fact]
    public async Task ScoreAsync_InternalArchetypeClassification_RecordsMatchedAndMissedSignals()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Top Regions","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":520,"height":40,
               "textbox":{"visible":true,"text":"Top Regions by Revenue"}},
              {"id":"v1","type":"barChart","x":0,"y":140,"width":520,"height":260,
               "title":{"visible":true,"text":"Top 10 Regions"},
               "fieldRoles":{"category":["Region"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var classification = GetInternalArchetypeClassification(result);
        var ranking = Assert.Single(classification.ArchetypeResults.Where(match => match.ArchetypeId == "Ranking"));

        Assert.Contains(ranking.MatchedSignals, signal => signal.Contains("layout.leadVisualType", StringComparison.Ordinal));
        Assert.Contains(ranking.MatchedSignals, signal => signal.Contains("semantic.primaryDimension", StringComparison.Ordinal));
        Assert.Contains(ranking.MissedSignals, signal => signal.Contains("semantic.primaryMetric", StringComparison.Ordinal));
        Assert.NotEmpty(ranking.ExplanationHooks);
    }

    [Fact]
    public async Task ScoreAsync_InternalArchetypeClassification_AmbiguousContextsDoNotOverclaimHighConfidence()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Comparison Mix","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":560,"height":40,
               "textbox":{"visible":true,"text":"Revenue Share vs Trend"}},
              {"id":"v1","type":"lineChart","x":0,"y":140,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue Trend"},
               "fieldRoles":{"category":["Month"],"value":["Revenue"],"measure":["Revenue"]}},
              {"id":"v2","type":"stackedColumnChart","x":460,"y":140,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue Share by Segment"},
               "fieldRoles":{"category":["Segment"],"value":["Revenue"],"measure":["Revenue"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var classification = GetInternalArchetypeClassification(result);
        var bestMatch = Assert.Single(classification.ArchetypeResults.Where(match => match.ArchetypeId == classification.BestFitArchetypeId));

        Assert.NotEqual("High", bestMatch.MatchConfidence);
        Assert.NotEqual("ReadyForPromotionReview", bestMatch.PromotionEligibilityState);
    }

    [Fact]
    public async Task ScoreAsync_InternalArchetypeClassification_RemainsInternalOnly()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Performance Monitor","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":520,"height":40,
               "textbox":{"visible":true,"text":"Performance Monitor"}},
              {"id":"v1","type":"barChart","x":0,"y":140,"width":520,"height":260,
               "title":{"visible":true,"text":"Revenue by Region"},
               "fieldRoles":{"category":["Region"],"value":["Revenue"],"measure":["Revenue"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var publicPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("InternalStoryAssessmentArchetypeClassification", publicPropertyNames);
        Assert.DoesNotContain("StoryAssessmentArchetypeClassification", publicPropertyNames);
        Assert.NotNull(GetInternalArchetypeClassification(result));
    }

    [Fact]
    public async Task ScoreAsync_InternalArchetypeClassification_DefinesLevel1HarnessAndPromotionGate()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Performance Monitor","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":520,"height":40,
               "textbox":{"visible":true,"text":"Performance Monitor"}},
              {"id":"v1","type":"barChart","x":0,"y":140,"width":520,"height":260,
               "title":{"visible":true,"text":"Revenue by Region"},
               "fieldRoles":{"category":["Region"],"value":["Revenue"],"measure":["Revenue"]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var classification = GetInternalArchetypeClassification(result);

        Assert.Null(classification.Level1ValidationHarness.ReviewerChoice);
        Assert.Null(classification.Level1ValidationHarness.DisagreementReason);
        Assert.Equal("NotAssessed", classification.Level1ValidationHarness.AccuracyRating);
        Assert.Equal("NotAssessed", classification.Level1ValidationHarness.ConsistencyRating);
        Assert.Equal("NotAssessed", classification.Level1ValidationHarness.ExplainabilityRating);
        Assert.Equal("NotAssessed", classification.Level1ValidationHarness.ActionabilityRating);

        Assert.True(classification.PromotionGateDefinition.MinimumClassificationAccuracy > 0.0d);
        Assert.Equal("Strong", classification.PromotionGateDefinition.MinimumExplanationQuality);
        Assert.Equal("Mixed", classification.PromotionGateDefinition.MinimumGapUsefulnessPotential);
        Assert.True(classification.PromotionGateDefinition.MaximumFalsePositiveRate >= 0.0d);
        Assert.True(classification.PromotionGateDefinition.ReviewerAgreementThresholdPlaceholder > 0.0d);
    }

    [Fact]
    public async Task ScoreAsync_InternalSemanticCoherence_HighCoherencePagesScoreHigh()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Revenue Performance","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":520,"height":40,
               "textbox":{"visible":true,"text":"Revenue Performance Overview"}},
              {"id":"v1","type":"lineChart","x":0,"y":120,"width":520,"height":220,
               "title":{"visible":true,"text":"Revenue Trend"},
               "fieldRoles":{
                 "category":[{"displayName":"Revenue Month","description":"Revenue month trend"}],
                 "measure":[{"displayName":"Revenue","synonyms":["Revenue"],"description":"Revenue performance"}]
               }},
              {"id":"v2","type":"barChart","x":540,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Region"},
               "fieldRoles":{
                 "category":[{"displayName":"Revenue Region","description":"Revenue region view"}],
                 "measure":[{"displayName":"Revenue","synonyms":["Revenue"],"description":"Revenue performance"}]
               }}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var coherence = GetInternalSemanticCoherenceAssessment(result);

        Assert.True(coherence.CoherenceScore >= 60d, $"Expected high coherence but got {coherence.CoherenceScore}.");
        Assert.Equal("Focused", coherence.CoherenceClassification);
        Assert.Equal("revenue", coherence.DominantConcept);
        Assert.Equal("None", coherence.CompetingStoryStatus);
        Assert.Equal("High", coherence.Confidence);
    }

    [Fact]
    public async Task ScoreAsync_InternalSemanticCoherence_NoisyPagesScoreLow()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Mixed Operational Notes","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Inventory Backlog"},
               "fieldRoles":{"category":[{"displayName":"Warehouse"}],"measure":[{"displayName":"Backlog"}]}},
              {"id":"v2","type":"lineChart","x":460,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Training Completion"},
               "fieldRoles":{"category":[{"displayName":"Employee"}],"measure":[{"displayName":"Completion"}]}},
              {"id":"v3","type":"table","x":0,"y":380,"width":420,"height":220,
               "title":{"visible":true,"text":"Support Tickets"},
               "fieldRoles":{"category":[{"displayName":"Support Queue"}],"measure":[{"displayName":"Ticket Count"}]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var coherence = GetInternalSemanticCoherenceAssessment(result);

        Assert.True(coherence.CoherenceScore < 40d, $"Expected low coherence but got {coherence.CoherenceScore}.");
        Assert.NotEqual("High", coherence.Confidence);
        Assert.NotEmpty(coherence.TermClusters);
    }

    [Fact]
    public async Task ScoreAsync_InternalSemanticCoherence_SplitTopicPagesDetectCompetingStory()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Revenue and Inventory Review","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":620,"height":40,
               "textbox":{"visible":true,"text":"Revenue and Inventory Review"}},
              {"id":"v1","type":"lineChart","x":0,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue Trend"},
               "fieldRoles":{
                 "category":[{"displayName":"Revenue Month","description":"Revenue month"}],
                 "measure":[{"displayName":"Revenue","description":"Revenue value"}]
               }},
              {"id":"v2","type":"barChart","x":460,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Inventory Backlog"},
               "fieldRoles":{
                 "category":[{"displayName":"Inventory Warehouse","description":"Inventory warehouse"}],
                 "measure":[{"displayName":"Inventory","description":"Inventory backlog"}]
               }},
              {"id":"v3","type":"card","x":0,"y":380,"width":200,"height":120,
               "title":{"visible":true,"text":"Revenue KPI"}},
              {"id":"v4","type":"card","x":240,"y":380,"width":200,"height":120,
               "title":{"visible":true,"text":"Inventory KPI"}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var coherence = GetInternalSemanticCoherenceAssessment(result);

        Assert.Equal("Split", coherence.CoherenceClassification);
        Assert.Equal("StrongCandidatePromotionDelayed", coherence.CompetingStoryStatus);
        Assert.Equal("PromotionDelayedRequiresStrongerValidation", coherence.ValidationStatus);
    }

    [Fact]
    public async Task ScoreAsync_InternalSemanticCoherence_DominantConceptIsDeterministic()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Revenue Overview","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Segment"},
               "fieldRoles":{"category":[{"displayName":"Revenue Segment"}],"measure":[{"displayName":"Revenue"}]}},
              {"id":"v2","type":"lineChart","x":460,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Monthly Revenue"},
               "fieldRoles":{"category":[{"displayName":"Revenue Month"}],"measure":[{"displayName":"Revenue"}]}}
            ]}
            """);
        var svc = BuildScoringService();

        var first = GetInternalSemanticCoherenceAssessment(await svc.ScoreAsync(tempDir));
        var second = GetInternalSemanticCoherenceAssessment(await svc.ScoreAsync(tempDir));

        Assert.Equal("revenue", first.DominantConcept);
        Assert.Equal(first.DominantConcept, second.DominantConcept);
    }

    [Fact]
    public async Task ScoreAsync_InternalSemanticCoherence_TermOrderingIsDeterministic()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Revenue Overview","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Segment"},
               "fieldRoles":{"category":[{"displayName":"Revenue Segment"}],"measure":[{"displayName":"Revenue"}]}},
              {"id":"v2","type":"lineChart","x":460,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Monthly Revenue"},
               "fieldRoles":{"category":[{"displayName":"Revenue Month"}],"measure":[{"displayName":"Revenue"}]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var coherence = GetInternalSemanticCoherenceAssessment(result);

        var orderedTerms = coherence.ExtractedTerms.Select(term => term.CanonicalTerm).ToList();
        var sortedTerms = orderedTerms.OrderBy(term => term, StringComparer.Ordinal).ToList();

        Assert.Equal(sortedTerms, orderedTerms);
    }

    [Fact]
    public async Task ScoreAsync_InternalSemanticCoherence_SparseMetadataDegradesGracefully()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Page 1","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":120,"width":420,"height":220},
              {"id":"v2","type":"card","x":460,"y":120,"width":220,"height":120}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var coherence = GetInternalSemanticCoherenceAssessment(result);

        Assert.Equal("Sparse", coherence.CoherenceClassification);
        Assert.Equal("None", coherence.CompetingStoryStatus);
        Assert.Equal("Low", coherence.Confidence);
        Assert.Equal("Internal", coherence.ValidationStatus);
    }

    [Fact]
    public async Task ScoreAsync_InternalSemanticCoherence_RemainsInternalOnly()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Revenue Overview","visuals":[
              {"id":"v1","type":"barChart","x":0,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue by Segment"},
               "fieldRoles":{"category":[{"displayName":"Revenue Segment"}],"measure":[{"displayName":"Revenue"}]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var publicPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("InternalStorySemanticCoherenceAssessment", publicPropertyNames);
        Assert.DoesNotContain("StorySemanticCoherenceAssessment", publicPropertyNames);
        Assert.NotNull(GetInternalSemanticCoherenceAssessment(result));
    }

    [Fact]
    public async Task ScoreAsync_InternalSemanticCoherence_WeakMetadataDisagreementStaysDiagnosticOnly()
    {
        var tempDir = CreateTempPbirFolderFromPageJson(
            """
            {"displayName":"Revenue Overview","visuals":[
              {"id":"t1","type":"textbox","x":0,"y":0,"width":520,"height":40,
               "textbox":{"visible":true,"text":"Revenue Overview"}},
              {"id":"v1","type":"lineChart","x":0,"y":120,"width":420,"height":220,
               "title":{"visible":true,"text":"Revenue Trend"},
               "fieldRoles":{"category":[{"displayName":"Revenue Month"}],"measure":[{"displayName":"Revenue"}]}},
              {"id":"v2","type":"card","x":460,"y":120,"width":220,"height":120,
               "title":{"visible":true,"text":"Margin Watch"},
               "fieldRoles":{"measure":[{"displayName":"Margin","description":"Margin change"}]}}
            ]}
            """);
        var svc = BuildScoringService();

        var result = await svc.ScoreAsync(tempDir);
        var coherence = GetInternalSemanticCoherenceAssessment(result);

        Assert.Equal("WeakDiagnosticOnly", coherence.CompetingStoryStatus);
        Assert.NotEmpty(coherence.WeakDisagreementSignals);
        Assert.NotEqual("PromotionDelayedRequiresStrongerValidation", coherence.ValidationStatus);
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private PbirScoringService BuildScoringService() =>
        new PbirScoringService(
            new PbirProjectService(NullLogger<PbirProjectService>.Instance),
            NullLogger<PbirScoringService>.Instance);

    private static Dictionary<string, double> CreateFrameworkWeights(
        double gestalt = 0,
        double cognitiveLoad = 0,
        double dataInk = 0,
        double accessibility = 0,
        double visualBestPractices = 0,
        double governance = 0,
        double stephenFew = 0,
        double tufte = 0,
        double graphicalPerception = 0,
        double density = 0,
        double narrative = 0)
    {
        return new Dictionary<string, double>
        {
            ["gestalt"] = gestalt,
            ["cognitiveLoad"] = cognitiveLoad,
            ["dataInk"] = dataInk,
            ["accessibility"] = accessibility,
            ["visualBestPractices"] = visualBestPractices,
            ["governance"] = governance,
            ["stephenFew"] = stephenFew,
            ["tufte"] = tufte,
            ["graphicalPerception"] = graphicalPerception,
            ["density"] = density,
            ["narrative"] = narrative,
        };
    }

    private static List<StorySignalRegistryEntrySnapshot> GetInternalStorySignalRegistryEntries(ScoreResult result)
    {
        var registryProperty = typeof(ScoreResult).GetProperty(
            "InternalStorySignalRegistry",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(registryProperty);

        var registry = registryProperty!.GetValue(result);
        Assert.NotNull(registry);

        var entriesProperty = registry!.GetType().GetProperty("Entries", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(entriesProperty);

        var entries = entriesProperty!.GetValue(registry) as System.Collections.IEnumerable;
        Assert.NotNull(entries);

        return entries!
            .Cast<object>()
            .Select(entry =>
            {
                var type = entry.GetType();
                return new StorySignalRegistryEntrySnapshot(
                    Id: type.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)!.GetValue(entry)?.ToString() ?? string.Empty,
                    RawValue: type.GetProperty("RawValue", BindingFlags.Instance | BindingFlags.Public)!.GetValue(entry)?.ToString(),
                    Fired: (bool)(type.GetProperty("Fired", BindingFlags.Instance | BindingFlags.Public)!.GetValue(entry) ?? false));
            })
            .ToList();
    }

    private static StoryAssessmentArchetypeClassificationSnapshot GetInternalArchetypeClassification(ScoreResult result)
    {
        var property = typeof(ScoreResult).GetProperty(
            "InternalStoryAssessmentArchetypeClassification",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);

        var classification = property!.GetValue(result);
        Assert.NotNull(classification);

        var classificationType = classification!.GetType();
        var bestFitArchetypeId = classificationType.GetProperty("BestFitArchetypeId", BindingFlags.Instance | BindingFlags.Public)!.GetValue(classification)?.ToString() ?? string.Empty;

        var archetypeResults = ((System.Collections.IEnumerable)classificationType.GetProperty("ArchetypeResults", BindingFlags.Instance | BindingFlags.Public)!.GetValue(classification)!)
            .Cast<object>()
            .Select(match =>
            {
                var type = match.GetType();
                return new StoryArchetypeMatchSnapshot(
                    ArchetypeId: type.GetProperty("ArchetypeId", BindingFlags.Instance | BindingFlags.Public)!.GetValue(match)?.ToString() ?? string.Empty,
                    MatchScore: (double)(type.GetProperty("MatchScore", BindingFlags.Instance | BindingFlags.Public)!.GetValue(match) ?? 0d),
                    MatchConfidence: type.GetProperty("MatchConfidence", BindingFlags.Instance | BindingFlags.Public)!.GetValue(match)?.ToString() ?? string.Empty,
                    MatchedSignals: ReadStringList(type.GetProperty("MatchedSignals", BindingFlags.Instance | BindingFlags.Public)!.GetValue(match)),
                    MissedSignals: ReadStringList(type.GetProperty("MissedSignals", BindingFlags.Instance | BindingFlags.Public)!.GetValue(match)),
                    ExplanationHooks: ReadStringList(type.GetProperty("ExplanationHooks", BindingFlags.Instance | BindingFlags.Public)!.GetValue(match)),
                    ValidationStatus: type.GetProperty("ValidationStatus", BindingFlags.Instance | BindingFlags.Public)!.GetValue(match)?.ToString() ?? string.Empty,
                    PromotionEligibilityState: type.GetProperty("PromotionEligibilityState", BindingFlags.Instance | BindingFlags.Public)!.GetValue(match)?.ToString() ?? string.Empty);
            })
            .ToList();

        var level1Harness = classificationType.GetProperty("Level1ValidationHarness", BindingFlags.Instance | BindingFlags.Public)!.GetValue(classification)!;
        var level1Type = level1Harness.GetType();
        var level1Snapshot = new StoryAssessmentLevel1ValidationHarnessSnapshot(
            ReviewerChoice: level1Type.GetProperty("ReviewerChoice", BindingFlags.Instance | BindingFlags.Public)!.GetValue(level1Harness)?.ToString(),
            SystemChoice: level1Type.GetProperty("SystemChoice", BindingFlags.Instance | BindingFlags.Public)!.GetValue(level1Harness)?.ToString() ?? string.Empty,
            DisagreementReason: level1Type.GetProperty("DisagreementReason", BindingFlags.Instance | BindingFlags.Public)!.GetValue(level1Harness)?.ToString(),
            AccuracyRating: level1Type.GetProperty("AccuracyRating", BindingFlags.Instance | BindingFlags.Public)!.GetValue(level1Harness)?.ToString() ?? string.Empty,
            ConsistencyRating: level1Type.GetProperty("ConsistencyRating", BindingFlags.Instance | BindingFlags.Public)!.GetValue(level1Harness)?.ToString() ?? string.Empty,
            ExplainabilityRating: level1Type.GetProperty("ExplainabilityRating", BindingFlags.Instance | BindingFlags.Public)!.GetValue(level1Harness)?.ToString() ?? string.Empty,
            ActionabilityRating: level1Type.GetProperty("ActionabilityRating", BindingFlags.Instance | BindingFlags.Public)!.GetValue(level1Harness)?.ToString() ?? string.Empty);

        var promotionGate = classificationType.GetProperty("PromotionGateDefinition", BindingFlags.Instance | BindingFlags.Public)!.GetValue(classification)!;
        var promotionType = promotionGate.GetType();
        var promotionSnapshot = new StoryAssessmentPromotionGateDefinitionSnapshot(
            MinimumClassificationAccuracy: (double)(promotionType.GetProperty("MinimumClassificationAccuracy", BindingFlags.Instance | BindingFlags.Public)!.GetValue(promotionGate) ?? 0d),
            MinimumExplanationQuality: promotionType.GetProperty("MinimumExplanationQuality", BindingFlags.Instance | BindingFlags.Public)!.GetValue(promotionGate)?.ToString() ?? string.Empty,
            MinimumGapUsefulnessPotential: promotionType.GetProperty("MinimumGapUsefulnessPotential", BindingFlags.Instance | BindingFlags.Public)!.GetValue(promotionGate)?.ToString() ?? string.Empty,
            MaximumFalsePositiveRate: (double)(promotionType.GetProperty("MaximumFalsePositiveRate", BindingFlags.Instance | BindingFlags.Public)!.GetValue(promotionGate) ?? 0d),
            ReviewerAgreementThresholdPlaceholder: (double)(promotionType.GetProperty("ReviewerAgreementThresholdPlaceholder", BindingFlags.Instance | BindingFlags.Public)!.GetValue(promotionGate) ?? 0d));

        return new StoryAssessmentArchetypeClassificationSnapshot(
            BestFitArchetypeId: bestFitArchetypeId,
            ArchetypeResults: archetypeResults,
            Level1ValidationHarness: level1Snapshot,
            PromotionGateDefinition: promotionSnapshot);
    }

    private static StorySemanticCoherenceAssessmentSnapshot GetInternalSemanticCoherenceAssessment(ScoreResult result)
    {
        var property = typeof(ScoreResult).GetProperty(
            "InternalStorySemanticCoherenceAssessment",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(property);

        var assessment = property!.GetValue(result);
        Assert.NotNull(assessment);

        var type = assessment!.GetType();
        var extractedTerms = ((System.Collections.IEnumerable)type.GetProperty("ExtractedTerms", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment)!)
            .Cast<object>()
            .Select(term =>
            {
                var termType = term.GetType();
                return new StorySemanticTermEvidenceSnapshot(
                    CanonicalTerm: termType.GetProperty("CanonicalTerm", BindingFlags.Instance | BindingFlags.Public)!.GetValue(term)?.ToString() ?? string.Empty,
                    RawText: termType.GetProperty("RawText", BindingFlags.Instance | BindingFlags.Public)!.GetValue(term)?.ToString() ?? string.Empty,
                    Source: termType.GetProperty("Source", BindingFlags.Instance | BindingFlags.Public)!.GetValue(term)?.ToString() ?? string.Empty,
                    Weight: (double)(termType.GetProperty("Weight", BindingFlags.Instance | BindingFlags.Public)!.GetValue(term) ?? 0d));
            })
            .ToList();

        var termClusters = ((System.Collections.IEnumerable)type.GetProperty("TermClusters", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment)!)
            .Cast<object>()
            .Select(cluster =>
            {
                var clusterType = cluster.GetType();
                return new StorySemanticTermClusterSnapshot(
                    ClusterId: clusterType.GetProperty("ClusterId", BindingFlags.Instance | BindingFlags.Public)!.GetValue(cluster)?.ToString() ?? string.Empty,
                    Weight: (double)(clusterType.GetProperty("Weight", BindingFlags.Instance | BindingFlags.Public)!.GetValue(cluster) ?? 0d),
                    SupportCount: (int)(clusterType.GetProperty("SupportCount", BindingFlags.Instance | BindingFlags.Public)!.GetValue(cluster) ?? 0),
                    Terms: ReadStringList(clusterType.GetProperty("Terms", BindingFlags.Instance | BindingFlags.Public)!.GetValue(cluster)),
                    ExplanationHook: clusterType.GetProperty("ExplanationHook", BindingFlags.Instance | BindingFlags.Public)!.GetValue(cluster)?.ToString() ?? string.Empty);
            })
            .ToList();

        var harness = type.GetProperty("Level1ValidationHarness", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment)!;
        var harnessType = harness.GetType();
        var harnessSnapshot = new StorySemanticCoherenceLevel1ValidationHarnessSnapshot(
            ReviewerCoherenceChoice: harnessType.GetProperty("ReviewerCoherenceChoice", BindingFlags.Instance | BindingFlags.Public)!.GetValue(harness)?.ToString(),
            SystemCoherenceChoice: harnessType.GetProperty("SystemCoherenceChoice", BindingFlags.Instance | BindingFlags.Public)!.GetValue(harness)?.ToString() ?? string.Empty,
            ReviewerDominantConcept: harnessType.GetProperty("ReviewerDominantConcept", BindingFlags.Instance | BindingFlags.Public)!.GetValue(harness)?.ToString(),
            SystemDominantConcept: harnessType.GetProperty("SystemDominantConcept", BindingFlags.Instance | BindingFlags.Public)!.GetValue(harness)?.ToString() ?? string.Empty,
            DisagreementReason: harnessType.GetProperty("DisagreementReason", BindingFlags.Instance | BindingFlags.Public)!.GetValue(harness)?.ToString(),
            AccuracyRating: harnessType.GetProperty("AccuracyRating", BindingFlags.Instance | BindingFlags.Public)!.GetValue(harness)?.ToString() ?? string.Empty,
            ConsistencyRating: harnessType.GetProperty("ConsistencyRating", BindingFlags.Instance | BindingFlags.Public)!.GetValue(harness)?.ToString() ?? string.Empty,
            ExplainabilityRating: harnessType.GetProperty("ExplainabilityRating", BindingFlags.Instance | BindingFlags.Public)!.GetValue(harness)?.ToString() ?? string.Empty,
            ActionabilityRating: harnessType.GetProperty("ActionabilityRating", BindingFlags.Instance | BindingFlags.Public)!.GetValue(harness)?.ToString() ?? string.Empty);

        return new StorySemanticCoherenceAssessmentSnapshot(
            CoherenceScore: (double)(type.GetProperty("CoherenceScore", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment) ?? 0d),
            CoherenceClassification: type.GetProperty("CoherenceClassification", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment)?.ToString() ?? string.Empty,
            DominantConcept: type.GetProperty("DominantConcept", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment)?.ToString(),
            ExtractedTerms: extractedTerms,
            TermClusters: termClusters,
            CompetingStoryStatus: type.GetProperty("CompetingStoryStatus", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment)?.ToString() ?? string.Empty,
            WeakDisagreementSignals: ReadStringList(type.GetProperty("WeakDisagreementSignals", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment)),
            ExplanationHooks: ReadStringList(type.GetProperty("ExplanationHooks", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment)),
            Confidence: type.GetProperty("Confidence", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment)?.ToString() ?? string.Empty,
            ValidationStatus: type.GetProperty("ValidationStatus", BindingFlags.Instance | BindingFlags.Public)!.GetValue(assessment)?.ToString() ?? string.Empty,
            Level1ValidationHarness: harnessSnapshot);
    }

    private static List<string> ReadStringList(object? value)
    {
        return value is System.Collections.IEnumerable enumerable
            ? enumerable.Cast<object>().Select(item => item?.ToString() ?? string.Empty).ToList()
            : [];
    }

    /// <summary>
    /// Creates a minimal PBIR folder structure under a new temp directory with
    /// <paramref name="visualCount"/> visuals of <paramref name="visualType"/> on one page.
    /// Returns the parent temp directory path (not the .Report folder).
    /// </summary>
    private string CreateTempPbirFolder(
        int visualCount,
        string visualType = "barChart",
        int navigationVisualCount = 0,
        string navigationVisualType = "actionButton")
    {
        var tmp        = Path.Combine(Path.GetTempPath(), "pbir-score-" + Guid.NewGuid().ToString("N"));
        var reportRoot = Path.Combine(tmp, "TestReport.Report");
        var defDir     = Path.Combine(reportRoot, "definition");
        var pagesDir   = Path.Combine(defDir, "pages", "Page1");
        Directory.CreateDirectory(pagesDir);
        _tempDirs.Add(tmp);

        File.WriteAllText(Path.Combine(defDir, "report.json"),
            """{"id":"test","name":"TestReport","pages":["Page1"],"theme":{"name":"CY24SU10"}}""");

        var dataVisuals = Enumerable.Range(1, visualCount).Select(i =>
            $$"""{"id":"v{{i}}","type":"{{visualType}}","x":{{(i - 1) * 100}},"y":0,"width":100,"height":100}""");
        var navigationVisuals = Enumerable.Range(1, navigationVisualCount).Select(i =>
            $$"""{"id":"n{{i}}","type":"{{navigationVisualType}}","x":{{(i - 1) * 50}},"y":100,"width":40,"height":20}""");
        var visuals = dataVisuals.Concat(navigationVisuals);
        File.WriteAllText(Path.Combine(pagesDir, "page.json"),
            $$"""{"displayName":"Page 1","visuals":[{{string.Join(",", visuals)}}]}""");

        return tmp;
    }

    private string CreateTempPbirFolder(
        IReadOnlyList<(string PageId, string DisplayName)> pages,
        IReadOnlyList<string> pageOrder)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pbir-score-" + Guid.NewGuid().ToString("N"));
        var reportRoot = Path.Combine(tmp, "TestReport.Report");
        var defDir = Path.Combine(reportRoot, "definition");
        var pagesRoot = Path.Combine(defDir, "pages");
        Directory.CreateDirectory(pagesRoot);
        _tempDirs.Add(tmp);

        File.WriteAllText(Path.Combine(defDir, "report.json"),
            """{"id":"test","name":"TestReport","theme":{"name":"CY24SU10"}}""");

        var pageOrderJson = string.Join(",", pageOrder.Select(pageId => $"\"{pageId}\""));
        File.WriteAllText(Path.Combine(pagesRoot, "pages.json"),
            $$"""{"pageOrder":[{{pageOrderJson}}]}""");

        foreach (var (pageId, displayName) in pages)
        {
            var pageDir = Path.Combine(pagesRoot, pageId);
            Directory.CreateDirectory(pageDir);
            File.WriteAllText(Path.Combine(pageDir, "page.json"),
                $$"""{"displayName":"{{displayName}}","visuals":[{"id":"v1","type":"barChart","x":0,"y":0,"width":100,"height":100}]}""");
        }

        return tmp;
    }

    private string CreateTempPbirFolderWithoutPagesJson(
        params (string PageId, string DisplayName)[] pages)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pbir-score-" + Guid.NewGuid().ToString("N"));
        var reportRoot = Path.Combine(tmp, "TestReport.Report");
        var defDir = Path.Combine(reportRoot, "definition");
        var pagesRoot = Path.Combine(defDir, "pages");
        Directory.CreateDirectory(pagesRoot);
        _tempDirs.Add(tmp);

        File.WriteAllText(Path.Combine(defDir, "report.json"),
            """{"id":"test","name":"TestReport","theme":{"name":"CY24SU10"}}""");

        foreach (var (pageId, displayName) in pages)
        {
            var pageDir = Path.Combine(pagesRoot, pageId);
            Directory.CreateDirectory(pageDir);
            File.WriteAllText(Path.Combine(pageDir, "page.json"),
                $$"""{"displayName":"{{displayName}}","visuals":[{"id":"v1","type":"barChart","x":0,"y":0,"width":100,"height":100}]}""");
        }

        return tmp;
    }

    private string CreateTempPbirFolderFromPageJson(string pageJson)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pbir-score-" + Guid.NewGuid().ToString("N"));
        var reportRoot = Path.Combine(tmp, "TestReport.Report");
        var defDir = Path.Combine(reportRoot, "definition");
        var pagesDir = Path.Combine(defDir, "pages", "Page1");
        Directory.CreateDirectory(pagesDir);
        _tempDirs.Add(tmp);

        File.WriteAllText(Path.Combine(defDir, "report.json"),
            """{"id":"test","name":"TestReport","pages":["Page1"],"theme":{"name":"CY24SU10"}}""");
        File.WriteAllText(Path.Combine(pagesDir, "page.json"), pageJson);

        return tmp;
    }

    /// <summary>
    /// Creates a minimal single-page PBIR folder with the given page.json and a report.json that
    /// embeds the supplied bookmarks array. Used by per-state scoring tests.
    /// </summary>
    private string CreateTempPbirFolderWithBookmarks(string pageJson, string bookmarksJson)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pbir-score-" + Guid.NewGuid().ToString("N"));
        var reportRoot = Path.Combine(tmp, "TestReport.Report");
        var defDir = Path.Combine(reportRoot, "definition");
        var pagesDir = Path.Combine(defDir, "pages", "Page1");
        Directory.CreateDirectory(pagesDir);
        _tempDirs.Add(tmp);

        File.WriteAllText(Path.Combine(defDir, "report.json"),
            $$"""{"id":"test","name":"TestReport","pages":["Page1"],"theme":{"name":"CY24SU10"},"bookmarks":{{bookmarksJson}}}""");
        File.WriteAllText(Path.Combine(pagesDir, "page.json"), pageJson);

        return tmp;
    }

    private string CreateTempPbirFolderWithDirectoryVisuals(
        string pageJson,
        params (string VisualId, string VisualJson)[] visuals)
    {
        var tmp = CreateTempPbirFolderFromPageJson(pageJson);
        var visualsRoot = Path.Combine(tmp, "TestReport.Report", "definition", "pages", "Page1", "visuals");
        Directory.CreateDirectory(visualsRoot);

        foreach (var (visualId, visualJson) in visuals)
        {
            var visualDir = Path.Combine(visualsRoot, visualId);
            Directory.CreateDirectory(visualDir);
            File.WriteAllText(Path.Combine(visualDir, "visual.json"), visualJson);
        }

        return tmp;
    }

    private string CreateTempPbirFolderFromPages(
        params (string PageId, string PageJson)[] pages)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pbir-score-" + Guid.NewGuid().ToString("N"));
        var reportRoot = Path.Combine(tmp, "TestReport.Report");
        var defDir = Path.Combine(reportRoot, "definition");
        var pagesRoot = Path.Combine(defDir, "pages");
        Directory.CreateDirectory(pagesRoot);
        _tempDirs.Add(tmp);

        File.WriteAllText(Path.Combine(defDir, "report.json"),
            """{"id":"test","name":"TestReport","theme":{"name":"CY24SU10"}}""");

        var pageOrderJson = string.Join(",", pages.Select(page => $"\"{page.PageId}\""));
        File.WriteAllText(Path.Combine(pagesRoot, "pages.json"),
            $$"""{"pageOrder":[{{pageOrderJson}}]}""");

        foreach (var (pageId, pageJson) in pages)
        {
            var pageDir = Path.Combine(pagesRoot, pageId);
            Directory.CreateDirectory(pageDir);
            File.WriteAllText(Path.Combine(pageDir, "page.json"), pageJson);
        }

        return tmp;
    }
}
