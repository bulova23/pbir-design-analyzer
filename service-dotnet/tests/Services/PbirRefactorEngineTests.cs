using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

/// <summary>
/// Unit tests for <see cref="PbirRefactorEngine"/> grid snap, pie-chart flagging,
/// and color-variance reduction (Phase 9 / T045).
/// </summary>
public sealed class PbirRefactorEngineTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    // ── SnapToGrid: off-grid visual ───────────────────────────────────────────

    /// <summary>
    /// A visual at x=50, y=40 is off-grid; after snapToGrid it must move to the
    /// nearest column/row intersection (x=0, y=60 for 106.67 px columns / 60 px rows).
    /// </summary>
    [Fact]
    public async Task SnapToGrid_VisualAtArbitraryPosition_SnapsToNearestColumn()
    {
        // Arrange
        var tmpDir = CreateRefactorTestFolder(
            """{"id":"v1","type":"barChart","x":50,"y":40,"width":100,"height":100}""");
        var engine = BuildRefactorEngine();

        // Act
        var result = await engine.RefactorAsync(tmpDir, ["snapToGrid"]);

        // Assert – at least one snap operation was recorded
        Assert.Contains(result.AppliedOperations,
            op => op.Contains("snapToGrid", StringComparison.OrdinalIgnoreCase));

        // Verify the page.json reflects snapped coordinates (nearest column = 0, nearest row = 60)
        var pageJson = ReadPageJson(tmpDir, "Test.Report");
        var visual   = pageJson["visuals"]![0]!;
        var x        = visual["x"]!.GetValue<double>();
        var y        = visual["y"]!.GetValue<double>();
        Assert.True(x % (1280.0 / 12.0) < 1.0 || (1280.0 / 12.0) - x % (1280.0 / 12.0) < 1.0,
            $"Expected x snapped to grid column but got {x}.");
        Assert.True(y % 60.0 < 1.0 || 60.0 - y % 60.0 < 1.0,
            $"Expected y snapped to grid row but got {y}.");
    }

    // ── SnapToGrid: already on grid ───────────────────────────────────────────

    /// <summary>
    /// A visual already at x=0, y=0 must not trigger any snap operations.
    /// </summary>
    [Fact]
    public async Task SnapToGrid_VisualAlreadyOnGrid_Unchanged()
    {
        // Arrange
        var tmpDir = CreateRefactorTestFolder(
            """{"id":"v1","type":"barChart","x":0,"y":0,"width":100,"height":100}""");
        var engine = BuildRefactorEngine();

        // Act
        var result = await engine.RefactorAsync(tmpDir, ["snapToGrid"]);

        // Assert – no snap operations recorded (visual was already aligned)
        Assert.DoesNotContain(result.AppliedOperations,
            op => op.Contains("snapToGrid", StringComparison.OrdinalIgnoreCase));
    }

    // ── FlagPieCharts ─────────────────────────────────────────────────────────

    /// <summary>
    /// A page containing a pieChart visual must produce a warning in the result;
    /// no file modification is made (flag only per FR-014).
    /// </summary>
    [Fact]
    public async Task FlagPieCharts_PieWithMoreThan5Slices_IsFlagged()
    {
        // Arrange
        var tmpDir = CreateRefactorTestFolder(
            """{"id":"v1","type":"pieChart","x":0,"y":0,"width":200,"height":200}""");
        var engine = BuildRefactorEngine();

        // Act
        var result = await engine.RefactorAsync(tmpDir, ["flagPieCharts"]);

        // Assert – warning added for the pie chart visual
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings,
            w => w.Contains("pieChart", StringComparison.OrdinalIgnoreCase) ||
                 w.Contains("v1", StringComparison.OrdinalIgnoreCase));
    }

    // ── ReduceColorVariance ───────────────────────────────────────────────────

    /// <summary>
    /// A theme file with 8 data colors must be reduced to 5 after the
    /// reduceColorVariance operation runs.
    /// </summary>
    [Fact]
    public async Task ReduceColorVariance_MoreThan5Colors_ReducesToFive()
    {
        // Arrange – 8 visually distinct data colors
        const string themeJson =
            """{"name":"TestTheme","dataColors":["#FF0000","#00FF00","#0000FF","#FFFF00","#FF00FF","#00FFFF","#FF8000","#8000FF"]}""";

        var tmpDir = CreateRefactorTestFolder(
            """{"id":"v1","type":"barChart","x":0,"y":0,"width":200,"height":200}""",
            themeFileContent: themeJson);
        var engine = BuildRefactorEngine();

        // Act
        var result = await engine.RefactorAsync(tmpDir, ["reduceColorVariance"]);

        // Assert – operation was recorded
        Assert.Contains(result.AppliedOperations,
            op => op.Contains("reduceColorVariance", StringComparison.OrdinalIgnoreCase));

        // Assert – theme file now has ≤5 data colors
        var themeFile = Path.Combine(tmpDir, "Test.Report", "definition", "themes", "theme.json");
        Assert.True(File.Exists(themeFile), $"Expected theme file at: {themeFile}");
        var updatedJson = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(themeFile));
        var dataColors  = updatedJson!["dataColors"]?.AsArray();
        Assert.NotNull(dataColors);
        Assert.True(dataColors!.Count <= 5,
            $"Expected ≤5 data colors after reduction but got {dataColors.Count}.");
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

    private PbirRefactorEngine BuildRefactorEngine() =>
        new PbirRefactorEngine(
            new PbirProjectService(NullLogger<PbirProjectService>.Instance),
            NullLogger<PbirRefactorEngine>.Instance);

    /// <summary>
    /// Creates a minimal PBIR folder under a fresh temp directory.
    /// </summary>
    /// <param name="visualsJson">JSON array items (no surrounding brackets) for the page's visuals array.</param>
    /// <param name="themeFileContent">Optional theme JSON written to <c>definition/themes/theme.json</c>.</param>
    /// <returns>The parent temp directory (not the .Report folder).</returns>
    private string CreateRefactorTestFolder(string visualsJson, string? themeFileContent = null)
    {
        var tmp        = Path.Combine(Path.GetTempPath(), "pbir-refactor-" + Guid.NewGuid().ToString("N"));
        var reportRoot = Path.Combine(tmp, "Test.Report");
        var defDir     = Path.Combine(reportRoot, "definition");
        var pagesDir   = Path.Combine(defDir, "pages", "Page1");
        Directory.CreateDirectory(pagesDir);
        _tempDirs.Add(tmp);

        string themeRef;
        if (themeFileContent != null)
        {
            var themesDir  = Path.Combine(defDir, "themes");
            Directory.CreateDirectory(themesDir);
            var themeFilePath = Path.Combine(themesDir, "theme.json");
            File.WriteAllText(themeFilePath, themeFileContent);

            // href must be relative to WorkspaceRootPath (= tmp, no .git present)
            var hrefRelative = Path.GetRelativePath(tmp, themeFilePath).Replace('\\', '/');
            themeRef = $$"""{"name":"TestTheme","href":"{{hrefRelative}}"}""";
        }
        else
        {
            themeRef = """{"name":"CY24SU10"}""";
        }

        File.WriteAllText(Path.Combine(defDir, "report.json"),
            $$"""{"id":"test","name":"Test","pages":["Page1"],"theme":{{themeRef}}}""");
        File.WriteAllText(Path.Combine(pagesDir, "page.json"),
            $$"""{"displayName":"Page 1","visuals":[{{visualsJson}}]}""");

        return tmp;
    }

    /// <summary>
    /// Reads and returns the parsed page.json for the first Page1 of a test report.
    /// </summary>
    private static System.Text.Json.Nodes.JsonObject ReadPageJson(string tmpDir, string reportFolderName)
    {
        var path = Path.Combine(tmpDir, reportFolderName, "definition", "pages", "Page1", "page.json");
        var text = File.ReadAllText(path);
        return (System.Text.Json.Nodes.JsonNode.Parse(text) as System.Text.Json.Nodes.JsonObject)!;
    }
}
