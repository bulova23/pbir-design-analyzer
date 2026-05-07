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

        Assert.Collection(
            result.Feedback["gestalt"],
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
}
