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
        Assert.Contains("metadata alone", titleFeedback.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'Page 1'", titleFeedback.Text, StringComparison.Ordinal);
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
