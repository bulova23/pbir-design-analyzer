using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

/// <summary>
/// Applies automated refactoring operations to an existing PBIR report definition.
///
/// <para><b>Available operations:</b></para>
/// <list type="table">
/// <item><term>snapToGrid</term>
///   <description>Rounds each visual's x/y to the nearest 12-column (≈106.67 px) / 12-row (60 px)
///   grid intersection. Detects and warns about post-snap overlaps.</description></item>
/// <item><term>normalizeFonts</term>
///   <description>Sets KPI-type visual title fontSize to 32, chart axis/label fontSize to 16,
///   and textbox/supporting-text fontSize to 12, per the spec visual hierarchy.</description></item>
/// <item><term>reduceColorVariance</term>
///   <description>If the theme defines more than 5 data colours, clusters them by perceived-distance
///   (using WCAG contrast ratio as the distance metric) and retains the 5 most distinct.</description></item>
/// <item><term>flagPieCharts</term>
///   <description>Returns the IDs of all pieChart/donutChart visuals as warnings
///   (flag only — no structural modification).</description></item>
/// </list>
///
/// <para>Every file write is guarded by <see cref="PbirFileWriteGuard.CheckAndGuard"/>.</para>
/// </summary>
public sealed class PbirRefactorEngine
{
    // Grid constants — must match PbirCreationService / PbirScoringService.
    private const double ColumnWidthPx  = 1280.0 / 12.0;  // ≈ 106.666...
    private const double RowBaselinePx  = 720.0 / 12.0;   // 60.0

    // Font-size targets (spec visual hierarchy).
    private const double FontSizeKpiTitle    = 32.0;
    private const double FontSizeAxisLabel   = 16.0;
    private const double FontSizeSupporting  = 12.0;

    // Maximum number of data colours per brand-guideline.
    private const int MaxDataColors = 5;

    private readonly PbirProjectService _projectService;
    private readonly ILogger<PbirRefactorEngine> _logger;

    /// <summary>Initializes a new instance of <see cref="PbirRefactorEngine"/>.</summary>
    public PbirRefactorEngine(PbirProjectService projectService, ILogger<PbirRefactorEngine> logger)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _logger         = logger         ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the requested <paramref name="operations"/> to the PBIR report at
    /// <paramref name="reportPath"/> and returns a summary of what changed.
    /// Feature 003: Supports optional <paramref name="pageName"/> for per-page refactoring.
    /// </summary>
    /// <param name="reportPath">PBIP project root or <c>.Report</c> folder path.</param>
    /// <param name="operations">
    /// One or more of: <c>"snapToGrid"</c>, <c>"normalizeFonts"</c>,
    /// <c>"reduceColorVariance"</c>, <c>"flagPieCharts"</c>.
    /// </param>
    /// <param name="pageName">Optional page name to scope operations to. If null, operates on all pages.</param>
    /// <exception cref="ArgumentException">When path is blank or no operations are provided.</exception>
    /// <exception cref="InvalidOperationException">When no PBIR definition is found.</exception>
    public async Task<RefactorResult> RefactorAsync(string reportPath, IEnumerable<string> operations, string? pageName = null)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            throw new ArgumentException("Parameter 'reportPath' is required.", nameof(reportPath));
        }

        var ops = operations?.ToList() ?? [];
        if (ops.Count == 0)
        {
            throw new ArgumentException("At least one operation must be specified.", nameof(operations));
        }

        var location = _projectService.TryGetReportLocation(reportPath);
        if (location is null)
        {
            throw new InvalidOperationException(
                $"No PBIR report definition found at '{reportPath}'.");
        }

        var result = new RefactorResult { ReportPath = location.ReportRootPath };

        // Feature 003: Log whether operating in page-scoped or full-report mode
        if (!string.IsNullOrWhiteSpace(pageName))
        {
            _logger.LogInformation("[Refactor] Starting operations [{Ops}] on page '{Page}' in: {Name}",
                string.Join(", ", ops), pageName, location.ReportName);
        }
        else
        {
            _logger.LogInformation("[Refactor] Starting operations [{Ops}] on: {Name}",
                string.Join(", ", ops), location.ReportName);
        }

        // ── Per-page operations ───────────────────────────────────────────────
        var pages = LoadPageFiles(location);

        // Feature 003: Filter to single page if pageName provided
        if (!string.IsNullOrWhiteSpace(pageName))
        {
            pages = pages
                .Where(p => p.Json["displayName"]?.GetValue<string>() == pageName)
                .ToList();
            
            if (pages.Count == 0)
            {
                var availablePages = string.Join(", ", LoadPageFiles(location)
                    .Select(p => $"'{p.Json["displayName"]?.GetValue<string>() ?? "?"}'"));
                throw new ArgumentException(
                    $"Page '{pageName}' not found. Available pages: {availablePages}",
                    nameof(pageName));
            }
        }

        bool wantsSnap    = ops.Contains("snapToGrid",    StringComparer.OrdinalIgnoreCase);
        bool wantsFonts   = ops.Contains("normalizeFonts", StringComparer.OrdinalIgnoreCase);
        bool wantsPie     = ops.Contains("flagPieCharts",  StringComparer.OrdinalIgnoreCase);

        foreach (var (pagePath, pageJson) in pages)
        {
            bool pageModified = false;

            if (wantsSnap)
            {
                pageModified |= SnapToGrid(pagePath, pageJson, result);
            }

            if (wantsFonts)
            {
                pageModified |= NormalizeFonts(pagePath, pageJson, result);
            }

            if (wantsPie)
            {
                FlagPieCharts(pageJson, result);
            }

            if (pageModified)
            {
                WriteJson(pagePath, pageJson);
            }
        }

        // ── Theme-wide operations ─────────────────────────────────────────────
        // Feature 003: Skip theme-wide operations when page-scoped (colors should apply to entire theme)
        if (!string.IsNullOrWhiteSpace(pageName))
        {
            if (ops.Contains("reduceColorVariance", StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[Refactor] reduceColorVariance is report-wide; applying to all pages even though page '{Page}' was specified", pageName);
                await ReduceColorVarianceAsync(location, result).ConfigureAwait(false);
            }
        }
        else
        {
            if (ops.Contains("reduceColorVariance", StringComparer.OrdinalIgnoreCase))
            {
                await ReduceColorVarianceAsync(location, result).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("[Refactor] Completed: {Applied} operations, {Warnings} warnings.",
            result.AppliedOperations.Count, result.Warnings.Count);

        return result;
    }

    // ── SnapToGrid ────────────────────────────────────────────────────────────

    /// <summary>
    /// Rounds each visual's position to the nearest grid intersection and detects overlaps.
    /// </summary>
    private bool SnapToGrid(string pagePath, JsonObject pageJson, RefactorResult result)
    {
        if (pageJson["visuals"] is not JsonArray visuals || visuals.Count == 0) return false;

        var pageName = pageJson["displayName"]?.GetValue<string>() ?? pagePath;
        var snapped  = 0;
        var positions = new List<(string Id, double X, double Y, double W, double H)>();

        foreach (var item in visuals)
        {
            if (item is not JsonObject v) continue;

            var id = v["id"]?.GetValue<string>() ?? "?";
            var x  = GetDouble(v, "x");
            var y  = GetDouble(v, "y");
            var w  = GetDouble(v, "width");
            var h  = GetDouble(v, "height");

            var sx = SnapValue(x, ColumnWidthPx);
            var sy = SnapValue(y, RowBaselinePx);

            if (Math.Abs(sx - x) > 0.01 || Math.Abs(sy - y) > 0.01)
            {
                SetDouble(v, "x", sx);
                SetDouble(v, "y", sy);
                snapped++;
            }

            positions.Add((id, sx, sy, w, h));
        }

        if (snapped > 0)
        {
            result.AppliedOperations.Add(
                $"snapToGrid: moved {snapped} visual(s) to grid in page '{pageName}'.");
            _logger.LogDebug("[Refactor] snapToGrid: {Snapped} visual(s) moved on page '{Page}'.", snapped, pageName);
        }

        // Overlap detection — O(n²) but pages typically have <20 visuals.
        for (var i = 0; i < positions.Count; i++)
        {
            for (var j = i + 1; j < positions.Count; j++)
            {
                var (ai, ax, ay, aw, ah) = positions[i];
                var (bi, bx, by, bw, bh) = positions[j];

                if (ax < bx + bw && ax + aw > bx && ay < by + bh && ay + ah > by)
                {
                    result.Warnings.Add(
                        $"[Medium] Overlap: visuals '{ai}' and '{bi}' overlap on page '{pageName}' after snap.");
                }
            }
        }

        return snapped > 0;
    }

    private static double SnapValue(double value, double step) =>
        Math.Round(value / step) * step;

    // ── NormalizeFonts ────────────────────────────────────────────────────────

    // Visual types treated as KPI / headline.
    private static readonly HashSet<string> _kpiTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "kpiVisual", "card", "scorecard", "gauge",
    };

    // Visual types that carry axis / data labels.
    private static readonly HashSet<string> _chartTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "barChart", "lineChart", "columnChart", "areaChart", "ribbonChart",
        "clusteredBarChart", "clusteredColumnChart", "waterfallChart", "funnelChart",
        "scatterChart", "bubbleChart", "pieChart", "donutChart", "treemap",
    };

    /// <summary>
    /// Traverses visual config and normalises fontSize values based on visual type.
    /// </summary>
    private bool NormalizeFonts(string pagePath, JsonObject pageJson, RefactorResult result)
    {
        if (pageJson["visuals"] is not JsonArray visuals || visuals.Count == 0) return false;

        var pageName = pageJson["displayName"]?.GetValue<string>() ?? pagePath;
        var changed  = 0;

        foreach (var item in visuals)
        {
            if (item is not JsonObject v) continue;

            var type   = v["type"]?.GetValue<string>() ?? string.Empty;
            var config = v["config"] as JsonObject;
            if (config is null) continue;

            double targetSize;
            if (_kpiTypes.Contains(type))
            {
                targetSize = FontSizeKpiTitle;
            }
            else if (_chartTypes.Contains(type))
            {
                targetSize = FontSizeAxisLabel;
            }
            else
            {
                targetSize = FontSizeSupporting;
            }

            changed += NormalizeFontSizesInNode(config, targetSize);
        }

        if (changed > 0)
        {
            result.AppliedOperations.Add(
                $"normalizeFonts: updated {changed} fontSize value(s) in page '{pageName}'.");
        }

        return changed > 0;
    }

    /// <summary>
    /// Recursively finds every <c>"fontSize"</c> property in a JSON tree and sets it to
    /// <paramref name="targetSize"/> using Power BI's Literal expression format.
    /// </summary>
    private static int NormalizeFontSizesInNode(JsonNode node, double targetSize)
    {
        var count = 0;
        switch (node)
        {
            case JsonObject obj:
                if (obj.ContainsKey("fontSize"))
                {
                    // Power BI stores font sizes as {"expr":{"Literal":{"Value":"16D"}}}
                    // or as a plain numeric JSON value.
                    var existing = obj["fontSize"];
                    if (existing is JsonObject fontObj &&
                        fontObj["expr"] is JsonObject exprObj &&
                        exprObj["Literal"] is JsonObject litObj)
                    {
                        litObj["Value"] = JsonValue.Create($"{(int)targetSize}D");
                        count++;
                    }
                    else if (existing is JsonValue)
                    {
                        obj["fontSize"] = JsonValue.Create(targetSize);
                        count++;
                    }
                }

                foreach (var prop in obj)
                {
                    if (prop.Value is not null)
                    {
                        count += NormalizeFontSizesInNode(prop.Value, targetSize);
                    }
                }
                break;

            case JsonArray arr:
                foreach (var element in arr)
                {
                    if (element is not null)
                    {
                        count += NormalizeFontSizesInNode(element, targetSize);
                    }
                }
                break;
        }

        return count;
    }

    // ── ReduceColorVariance ───────────────────────────────────────────────────

    /// <summary>
    /// Reads the theme file referenced by <c>report.json</c>; if it contains more than
    /// <see cref="MaxDataColors"/> data colours, selects the <see cref="MaxDataColors"/> most
    /// perceptually distinct colours via greedy maximum-diversity selection (using WCAG contrast
    /// ratio as the distance metric) and writes the reduced palette back to the theme file.
    /// </summary>
    private async Task ReduceColorVarianceAsync(PbirReportLocation location, RefactorResult result)
    {
        var reportJson = ReadJsonObject(location.ReportJsonPath);
        var themeHref  = reportJson["theme"]?["href"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(themeHref) ||
            themeHref.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add("[Low] reduceColorVariance: no local theme file found; skipping.");
            return;
        }

        var themeFilePath = Path.GetFullPath(
            Path.Combine(location.WorkspaceRootPath, themeHref.TrimStart('/', '\\')));

        if (!File.Exists(themeFilePath))
        {
            result.Warnings.Add(
                $"[Low] reduceColorVariance: theme file not found at '{themeFilePath}'; skipping.");
            return;
        }

        var themeJson = ReadJsonObject(themeFilePath);

        if (themeJson["dataColors"] is not JsonArray colorsArr) return;

        var colors = colorsArr
            .Select(n => n?.GetValue<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s) && s!.StartsWith('#'))
            .Select(s => s!)
            .ToList();

        if (colors.Count <= MaxDataColors)
        {
            result.AppliedOperations.Add(
                $"reduceColorVariance: palette has {colors.Count} colour(s) (≤{MaxDataColors}); no change.");
            return;
        }

        var reduced = SelectMostDistinctColors(colors, MaxDataColors);
        var newArr  = new JsonArray(reduced.Select(c => (JsonNode)JsonValue.Create(c)!).ToArray());
        themeJson["dataColors"] = newArr;

        PbirFileWriteGuard.CheckAndGuard(themeFilePath);
        var text = themeJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(themeFilePath, text).ConfigureAwait(false);

        result.AppliedOperations.Add(
            $"reduceColorVariance: reduced palette from {colors.Count} to {reduced.Count} colour(s): " +
            string.Join(", ", reduced));
    }

    /// <summary>
    /// Greedy maximum-diversity selection: starts with the darkest colour (highest diversity
    /// as an anchor), then iteratively picks the colour whose minimum contrast ratio to any
    /// already-selected colour is greatest.
    /// </summary>
    private static List<string> SelectMostDistinctColors(List<string> colors, int count)
    {
        if (colors.Count <= count) return colors;

        // Seed with the colour that has the greatest mean contrast to all others.
        var selected = new List<string>();
        var best     = colors.OrderByDescending(c => MeanContrastToAll(c, colors)).First();
        selected.Add(best);
        var remaining = colors.Where(c => c != best).ToList();

        while (selected.Count < count && remaining.Count > 0)
        {
            var next = remaining
                .OrderByDescending(candidate =>
                    selected.Min(s => SafeContrast(candidate, s)))
                .First();
            selected.Add(next);
            remaining.Remove(next);
        }

        return selected;
    }

    private static double MeanContrastToAll(string color, List<string> all) =>
        all.Where(c => c != color).Average(c => SafeContrast(color, c));

    private static double SafeContrast(string a, string b)
    {
        try { return WcagContrastCalculator.ContrastRatio(a, b); }
        catch { return 1.0; }
    }

    // ── FlagPieCharts ─────────────────────────────────────────────────────────

    private static readonly HashSet<string> _pieTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "pieChart", "donutChart",
    };

    /// <summary>
    /// Adds warnings for every pie/donut chart visual found in the page.
    /// Does not modify any file; flag only (per spec FR-014).
    /// </summary>
    private static void FlagPieCharts(JsonObject pageJson, RefactorResult result)
    {
        if (pageJson["visuals"] is not JsonArray visuals) return;

        var pageName = pageJson["displayName"]?.GetValue<string>() ?? "unknown page";

        foreach (var item in visuals)
        {
            if (item is not JsonObject v) continue;
            var type = v["type"]?.GetValue<string>() ?? string.Empty;
            if (!_pieTypes.Contains(type)) continue;

            var id = v["id"]?.GetValue<string>() ?? "?";
            result.Warnings.Add(
                $"[Medium] flagPieCharts: visual '{id}' on page '{pageName}' is a {type}. " +
                "Pie/donut charts are hard to read at scale — consider a bar or stacked column chart.");
        }
    }

    // ── I/O helpers ───────────────────────────────────────────────────────────

    private List<(string Path, JsonObject Json)> LoadPageFiles(PbirReportLocation location)
    {
        var pagesRoot = Path.Combine(location.DefinitionPath, "pages");
        if (!Directory.Exists(pagesRoot)) return [];

        var pages = new List<(string, JsonObject)>();
        foreach (var dir in Directory.GetDirectories(pagesRoot))
        {
            var pageJsonPath = Path.Combine(dir, "page.json");
            if (!File.Exists(pageJsonPath)) continue;

            try
            {
                pages.Add((pageJsonPath, ReadJsonObject(pageJsonPath)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Refactor] Could not read page {Dir}", dir);
            }
        }

        return pages;
    }

    private static void WriteJson(string filePath, JsonObject json)
    {
        PbirFileWriteGuard.CheckAndGuard(filePath);
        File.WriteAllText(filePath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static JsonObject ReadJsonObject(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException($"File is not a JSON object: {filePath}");
    }

    private static double GetDouble(JsonObject obj, string key) =>
        obj[key] is JsonNode n ? n.GetValue<double>() : 0.0;

    private static void SetDouble(JsonObject obj, string key, double value) =>
        obj[key] = JsonValue.Create(value);
}
