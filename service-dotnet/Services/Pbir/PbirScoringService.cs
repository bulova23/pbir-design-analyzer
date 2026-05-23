using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

/// <summary>
/// Computes a composite quality score for an existing PBIR report against 11 design frameworks,
/// producing a <see cref="ScoreResult"/> with per-framework sub-scores, collapsible feedback items,
/// and actionable recommendations.
///
/// <para><b>11-framework scoring model:</b></para>
/// <list type="bullet">
/// <item>Core and optional principles are normalized to the same framework IDs used by the Design Analyzer configuration panel.</item>
/// <item>The composite score uses the enabled principles and weights supplied in the Design Analyzer configuration.</item>
/// <item>When no configuration is provided, the service falls back to the default Design Analyzer configuration.</item>
/// </list>
/// </summary>
public sealed class PbirScoringService
{
    // Canvas constants — must match PbirCreationService / PbirLayoutPatternLibrary
    private const int CanvasWidth  = 1280;
    private const int CanvasHeight = 720;
    private const int GridCols     = 12;
    private const int GridRows     = 12;
    private const double ColWidthPx  = (double)CanvasWidth  / GridCols;  // 106.666…
    private const double RowHeightPx = (double)CanvasHeight / GridRows;  // 60.0

    // White is used as the background reference for accessibility contrast checks.
    private const string BackgroundWhite = "#FFFFFF";

    private readonly PbirProjectService _projectService;
    private readonly ILogger<PbirScoringService> _logger;

    /// <summary>Initializes a new instance of <see cref="PbirScoringService"/>.</summary>
    public PbirScoringService(PbirProjectService projectService, ILogger<PbirScoringService> logger)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _logger         = logger         ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the composite quality score for the PBIR report at <paramref name="reportPath"/>.
    /// Dispatch method that supports both full-report and single-page scoring modes.
    /// 
    /// <para><b>Full Report Mode (pageName = null):</b></para>
    /// <list type="bullet">
    /// <item>Scores all pages, computes report-level composite score</item>
    /// <item>Returns <c>ScoreResult.PageScores</c> containing per-page breakdown</item>
    /// <item>Supports partial failure: some pages may fail; successful pages included in overall score</item>
    /// <item>Typical execution time: ~6-8 seconds for 20-page report</item>
    /// </list>
    /// 
    /// <para><b>Single-Page Mode (pageName provided):</b></para>
    /// <list type="bullet">
    /// <item>Scores only the specified page (case-sensitive exact match)</item>
    /// <item>Returns page score in top-level <c>ScoreResult</c> properties</item>
    /// <item>Sets <c>ScoreResult.ScoredPageName</c> to identify the scored page</item>
    /// <item>Typical execution time: ~0.5 seconds per page</item>
    /// </list>
    /// 
    /// <para><b>Error Handling:</b></para>
    /// <list type="bullet">
    /// <item>Page not found: throws <c>ArgumentException</c> with list of available pages</item>
    /// <item>Partial failure in full-report mode: errors recorded in <c>ScoreResult.ScoringErrors</c>; composite score computed from successful pages only</item>
    /// <item>Invalid report: throws <c>InvalidOperationException</c></item>
    /// </list>
    /// </summary>
    /// <param name="reportPath">Path to the PBIP project root or <c>.Report</c> folder containing the report definition.</param>
    /// <param name="pageName">Optional page name to score. If <c>null</c>, scores entire report with per-page breakdown.
    /// If provided, must match page display name exactly (case-sensitive). Example: <c>"Sales Analysis"</c>, <c>"Page 1"</c>.
    /// For pages with duplicate names, use the disambiguated form from tree view (e.g., <c>"Analysis (1)"</c>).</param>
    /// <returns>A <see cref="ScoreResult"/> with all sub-scores (0-100), framework feedback, recommendations, and optional per-page breakdown.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="reportPath"/> is blank, or when <paramref name="pageName"/> is provided but no matching page found.
    /// The exception message includes the list of available pages for diagnosis.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no PBIR report definition (report.json) is found at the specified location, or when report structure is malformed.
    /// </exception>
    /// <example>
    /// <para><b>Score entire report:</b></para>
    /// <code>
    /// var result = await scoringService.ScoreAsync("/path/to/pbip");
    /// Console.WriteLine($"Overall: {result.CompositeScore}/100");
    /// foreach (var page in result.PageScores)
    /// {
    ///     Console.WriteLine($"  {page.PageName}: {page.CompositeScore}/100");
    ///     if (page.ScoringError != null) Console.WriteLine($"    Error: {page.ScoringError}");
    /// }
    /// </code>
    /// </example>
    /// <example>
    /// <para><b>Score single page:</b></para>
    /// <code>
    /// var result = await scoringService.ScoreAsync("/path/to/pbip", "Sales Analysis");
    /// Console.WriteLine($"Sales Analysis: {result.CompositeScore}/100");
    /// Console.WriteLine($"Gestalt: {result.GestaltScore}, CogLoad: {result.CognitiveLoadScore}, ...");
    /// </code>
    /// </example>
    public Task<ScoreResult> ScoreAsync(string reportPath, JsonElement? config = null, string? pageName = null)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            throw new ArgumentException("Parameter 'reportPath' is required.", nameof(reportPath));
        }

        var location = _projectService.TryGetReportLocation(reportPath);
        if (location is null)
        {
            throw new InvalidOperationException(
                $"No PBIR report definition found at '{reportPath}'.");
        }

        // Dispatch: single-page vs. full-report scoring
        if (!string.IsNullOrWhiteSpace(pageName))
        {
            return Task.FromResult(ComputePageScore(location, pageName, config));
        }
        else
        {
            return Task.FromResult(ComputeReportScore(location, config));
        }
    }


    // ── Core scoring ─────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the composite quality score for a single page within the report.
    /// Called when <c>ScoreAsync(reportPath, pageName)</c> is invoked with a specific page name.
    /// 
    /// <para><b>Process:</b></para>
    /// <list type="bullet">
    /// <item>Locates the page by exact name match (case-sensitive)</item>
    /// <item>Extracts the page's visuals and applies all six frameworks</item>
    /// <item>Returns top-level <c>ScoreResult</c> properties (GestaltScore, CognitiveLoadScore, etc.) matching the page's scores</item>
    /// <item>Sets <c>ScoredPageName</c> to the page name for clarity</item>
    /// </list>
    /// 
    /// <para><b>Zero-Visual Handling:</b></para>
    /// If the page contains no data visuals (only decorative elements), returns a zero-score result
    /// with a message in all framework feedback items.
    /// </summary>
    /// <param name="location">The PBIR report location (contains path to report.json and page definitions).</param>
    /// <param name="pageName">The exact page name to score (case-sensitive). Must match a page's DisplayName.</param>
    /// <returns>A <see cref="ScoreResult"/> with page-specific scores in the top-level properties.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the page name is not found. The exception message lists all available page names.
    /// </exception>
    private ScoreResult ComputePageScore(PbirReportLocation location, string pageName, JsonElement? config = null)
    {
        _logger.LogInformation("[Scoring] Scoring single page '{Page}' in report: {Name}", pageName, location.ReportName);

        var recommendations = new List<string>();
        var reportJson      = ReadJsonObject(location.ReportJsonPath);
        var themeColors     = ResolveThemeColors(reportJson, location, recommendations);
        var allPages        = LoadAllPages(location);
        var frameworkWeights = this.ExtractFrameworkWeights(config);  // Extract framework weights from config
        var navigationScoring = ExtractNavigationScoringSettings(config);

        // Find the requested page
        var page = allPages.FirstOrDefault(p => p.DisplayName == pageName);
        if (page is null)
        {
            var availablePages = string.Join(", ", allPages.Select(p => $"'{p.DisplayName}'"));
            var errorMsg = $"Page '{pageName}' not found. Available pages: {availablePages}";
            _logger.LogWarning("[Scoring] {Error}", errorMsg);
            
            throw new ArgumentException(errorMsg, nameof(pageName));
        }

        // Wrap page in a single-item list for framework methods
        var pageList = new List<PageData> { page };
        var pageVisuals = page.Visuals;
        var pageComposition = BuildVisualComposition(pageVisuals, navigationScoring);
        bool hasDataVisuals = pageComposition.DataVisualCount > 0;

        // Zero-visual guard
        if (!hasDataVisuals)
        {
            const string noVisualsMsg =
                "No data visuals found on this page — add charts, tables, or KPI cards to score this page.";
            var noVizFeedback = new Dictionary<string, List<FrameworkFeedbackItem>>();
            foreach (var key in new[] { "gestalt", "cognitiveLoad", "dataInk", "accessibility", "visualBestPractices", "governance", "stephenFew", "tufte", "graphicalPerception", "density", "narrative" })
            {
                noVizFeedback[key] = [FeedbackItem(false, noVisualsMsg, FindingTypes.Objective)];
            }

#pragma warning disable CS0618
            return new ScoreResult
            {
                GestaltScore             = 0, CognitiveLoadScore       = 0,
                DataInkScore             = 0, AccessibilityScore       = 0,
                VisualBestPracticesScore = 0, StephenFewScore          = 0,
                EnterpriseGovernanceScore = 0,
                TufteScore               = 0,
                GraphicalPerceptionScore = 0, DensityScore             = 0, NarrativeScore = 0,
                LayoutScore = 0, ThemeScore = 0, GovernanceScore = 0,
                Feedback        = noVizFeedback,
                PageCount       = 1,
                Recommendations = recommendations,
                ReportPath      = location.ReportRootPath,
                ScoredPageName  = pageName,
                ScoredAt        = DateTimeOffset.UtcNow,
                FrameworkWeights = frameworkWeights,
                DataVisualCount = pageComposition.DataVisualCount,
                NavigationVisualCount = pageComposition.NavigationVisualCount,
                HiddenVisualCount = pageComposition.HiddenVisualCount,
                VisualMetadata = BuildPageVisualMetadataSummary(page),
            };
#pragma warning restore CS0618
        }

        // ── Compute all six frameworks for this page ────────────────────────
        var (gestaltScore, gestaltFeedback)          = ComputeGestaltScore(pageList);
        var (cogLoadScore, cogLoadFeedback)          = ComputeCognitiveLoadScore(pageList, recommendations, navigationScoring);
        var (dataInkScore, dataInkFeedback)          = ComputeDataInkScore(pageList, recommendations, navigationScoring);
        var (accessibilityScore, a11yFeedback)       = ComputeAccessibilityScore(themeColors, pageList, recommendations);
        var (vbpScore, vbpFeedback)                  = ComputeVisualBestPracticesScore(pageList, themeColors, recommendations);
        var (governanceScore, governanceFeedback)    = ComputeGovernanceScore(pageList, config);
        var (fewScore, fewFeedback)                  = ComputeStephenFewScore(pageList);
        var (tufteScore, tufteFeedback)              = ComputeTufteScore(pageList);
        var (graphicalScore, graphicalFeedback)      = ComputeGraphicalPerceptionScore(pageList);
        var (densityScore, densityFeedback)          = ComputeDashboardDensityScore(pageList, recommendations, navigationScoring);
        var (narrativeScore, narrativeFeedback)      = ComputeNarrativeDesignScore(pageList, recommendations);

        _logger.LogDebug(
            "[Scoring] Page '{Page}' sub-scores — Gestalt={G:F1} CogLoad={C:F1} DataInk={D:F1} A11y={A:F1} VBP={V:F1} Few={F:F1} Tufte={T:F1}",
            pageName, gestaltScore, cogLoadScore, dataInkScore, accessibilityScore, vbpScore, fewScore, tufteScore);

#pragma warning disable CS0618
        var result = new ScoreResult
        {
            GestaltScore             = Clamp(gestaltScore),
            CognitiveLoadScore       = Clamp(cogLoadScore),
            DataInkScore             = Clamp(dataInkScore),
            AccessibilityScore       = Clamp(accessibilityScore),
            VisualBestPracticesScore = Clamp(vbpScore),
            EnterpriseGovernanceScore = Clamp(governanceScore),
            StephenFewScore          = Clamp(fewScore),
            TufteScore               = Clamp(tufteScore),
            GraphicalPerceptionScore = Clamp(graphicalScore),
            DensityScore             = Clamp(densityScore),
            NarrativeScore           = Clamp(narrativeScore),
            // Keep legacy fields in sync
            LayoutScore    = Clamp(gestaltScore),
            ThemeScore     = Clamp(vbpScore),
            GovernanceScore = Clamp(governanceScore),
            Feedback = new()
            {
                ["gestalt"]             = gestaltFeedback,
                ["cognitiveLoad"]       = cogLoadFeedback,
                ["dataInk"]             = dataInkFeedback,
                ["accessibility"]       = a11yFeedback,
                ["visualBestPractices"] = vbpFeedback,
                ["governance"]          = governanceFeedback,
                ["stephenFew"]          = fewFeedback,
                ["tufte"]               = tufteFeedback,
                ["graphicalPerception"] = graphicalFeedback,
                ["density"]             = densityFeedback,
                ["narrative"]           = narrativeFeedback,
            },
            PageCount       = 1,
            Recommendations = recommendations,
            ReportPath      = location.ReportRootPath,
            ScoredPageName  = pageName,
            ScoredAt        = DateTimeOffset.UtcNow,
            FrameworkWeights = frameworkWeights,
            DataVisualCount = pageComposition.DataVisualCount,
            NavigationVisualCount = pageComposition.NavigationVisualCount,
            HiddenVisualCount = pageComposition.HiddenVisualCount,
            VisualMetadata = BuildPageVisualMetadataSummary(page),
        };
#pragma warning restore CS0618

        // ── Bookmark-aware (per-state) scoring ──────────────────────────────
        // When bookmarks affect this page, re-score the page once per layout state with the
        // visuals filtered to that state and replace the full-page framework scores with the
        // per-state averages. The per-state composites surface on result.PerStateScores so the
        // panel can break out individual state quality.
        var overlay = ComputeBookmarkAwareOverlay(
            page, reportJson, themeColors, navigationScoring, config, frameworkWeights);
        if (overlay is not null)
        {
#pragma warning disable CS0618
            result.GestaltScore              = overlay.AveragedFrameworks["gestalt"];
            result.CognitiveLoadScore        = overlay.AveragedFrameworks["cognitiveLoad"];
            result.DataInkScore              = overlay.AveragedFrameworks["dataInk"];
            result.AccessibilityScore        = overlay.AveragedFrameworks["accessibility"];
            result.VisualBestPracticesScore  = overlay.AveragedFrameworks["visualBestPractices"];
            result.EnterpriseGovernanceScore = overlay.AveragedFrameworks["governance"];
            result.StephenFewScore           = overlay.AveragedFrameworks["stephenFew"];
            result.TufteScore                = overlay.AveragedFrameworks["tufte"];
            result.GraphicalPerceptionScore  = overlay.AveragedFrameworks["graphicalPerception"];
            result.DensityScore              = overlay.AveragedFrameworks["density"];
            result.NarrativeScore            = overlay.AveragedFrameworks["narrative"];
            result.LayoutScore               = result.GestaltScore;
            result.ThemeScore                = result.VisualBestPracticesScore;
            result.GovernanceScore           = result.EnterpriseGovernanceScore;
#pragma warning restore CS0618
            result.PerStateScores = overlay.PerStateScores;

            recommendations.Add(
                $"[Info] Bookmark-aware scoring active: page scored across {overlay.PerStateScores.Count} layout states (Default + {overlay.PerStateScores.Count - 1} bookmark state{(overlay.PerStateScores.Count == 2 ? string.Empty : "s")}).");

            _logger.LogInformation(
                "[Bookmark State] Page '{Page}' scored across {Count} states, composite={Score}",
                pageName, overlay.PerStateScores.Count, result.CompositeScore);
        }

        _logger.LogInformation(
            "[Scoring] Page '{Page}' Composite: {Score}",
            pageName, result.CompositeScore);

        return result;
    }

    /// <summary>
    /// Computes the composite quality score for the entire report.
    /// Includes per-page breakdown in <see cref="ScoreResult.PageScores"/>.
    /// </summary>
    /// <summary>
    /// Computes the composite quality score for the entire PBIR report, including a per-page breakdown.
    /// Called when <c>ScoreAsync(reportPath, pageName: null)</c> is invoked without a specific page.
    /// 
    /// <para><b>Process:</b></para>
    /// <list type="bullet">
    /// <item>Step 1: Load all pages from the report definition</item>
    /// <item>Step 2: Compute report-level scores (frameworks applied to all pages combined)</item>
    /// <item>Step 3: Compute per-page breakdown (frameworks applied to each page individually, catching errors)</item>
    /// <item>Step 4: Return result with both top-level scores and <c>PageScores</c> list</item>
    /// </list>
    /// 
    /// <para><b>Partial Failure Handling:</b></para>
    /// Each page is scored independently within a try-catch block. If a page fails:
    /// <list type="bullet">
    /// <item>The error is recorded in <c>ScoringErrors[pageName]</c></item>
    /// <item>The page is excluded from the overall composite score calculation</item>
    /// <item>Other pages continue to be scored</item>
    /// <item>User still receives scores for successful pages</item>
    /// </list>
    /// 
    /// <para><b>Composite Score Calculation:</b></para>
    /// The overall composite score is the weighted average of all six frameworks across all pages,
    /// computed before the per-page loop. If some pages fail later, the overall score is NOT recalculated;
    /// it reflects the full-report analysis. This provides continuity and clarity.
    /// 
    /// <para><b>Performance:</b></para>
    /// Typical execution time is ~0.3-0.5 seconds per page, so a 20-page report takes ~6-10 seconds.
    /// </summary>
    /// <param name="location">The PBIR report location (contains path to report.json and page definitions).</param>
    /// <returns>A <see cref="ScoreResult"/> with top-level scores, full report feedback, and <c>PageScores</c> list.
    /// The <c>ScoringErrors</c> dictionary will contain entries for any pages that failed to score.</returns>
    private ScoreResult ComputeReportScore(PbirReportLocation location, JsonElement? config = null)
    {
        var recommendations = new List<string>();
        var reportJson    = ReadJsonObject(location.ReportJsonPath);
        var themeColors   = ResolveThemeColors(reportJson, location, recommendations);
        var pages         = LoadAllPages(location);
        var frameworkWeights = this.ExtractFrameworkWeights(config);  // Extract framework weights from config
        var navigationScoring = ExtractNavigationScoringSettings(config);
        var reportComposition = BuildVisualComposition(pages.SelectMany(p => p.Visuals), navigationScoring);
        bool hasDataVisuals = reportComposition.DataVisualCount > 0;

        // Zero-visual guard — return 0 with explanation across all frameworks.
        if (!hasDataVisuals)
        {
            const string noVisualsMsg =
                "No data visuals found on any page — add charts, tables, or KPI cards to score this report.";
            var noVizFeedback = new Dictionary<string, List<FrameworkFeedbackItem>>();
            foreach (var key in new[] { "gestalt", "cognitiveLoad", "dataInk", "accessibility", "visualBestPractices", "governance", "stephenFew", "tufte", "graphicalPerception", "density", "narrative" })
            {
                noVizFeedback[key] = [FeedbackItem(false, noVisualsMsg, FindingTypes.Objective)];
            }

#pragma warning disable CS0618
            return new ScoreResult
            {
                GestaltScore             = 0, CognitiveLoadScore       = 0,
                DataInkScore             = 0, AccessibilityScore       = 0,
                VisualBestPracticesScore = 0, StephenFewScore          = 0,
                EnterpriseGovernanceScore = 0,
                TufteScore               = 0,
                GraphicalPerceptionScore = 0, DensityScore             = 0, NarrativeScore = 0,
                LayoutScore = 0, ThemeScore = 0, GovernanceScore = 0,
                Feedback        = noVizFeedback,
                PageCount       = pages.Count,
                Recommendations = recommendations,
                ReportPath      = location.ReportRootPath,
                PageScores      = new(),
                ScoringErrors   = new(),
                ScoredAt        = DateTimeOffset.UtcNow,
                FrameworkWeights = frameworkWeights,
                DataVisualCount = reportComposition.DataVisualCount,
                NavigationVisualCount = reportComposition.NavigationVisualCount,
                HiddenVisualCount = reportComposition.HiddenVisualCount,
                VisualMetadata = pages.Count == 1 ? BuildPageVisualMetadataSummary(pages[0]) : null,
            };
#pragma warning restore CS0618
        }

        // ── Compute all six frameworks for the full report ────────────────────
        var (gestaltScore, gestaltFeedback)          = ComputeGestaltScore(pages);
        var (cogLoadScore, cogLoadFeedback)          = ComputeCognitiveLoadScore(pages, recommendations, navigationScoring);
        var (dataInkScore, dataInkFeedback)          = ComputeDataInkScore(pages, recommendations, navigationScoring);
        var (accessibilityScore, a11yFeedback)       = ComputeAccessibilityScore(themeColors, pages, recommendations);
        var (vbpScore, vbpFeedback)                  = ComputeVisualBestPracticesScore(pages, themeColors, recommendations);
        var (governanceScore, governanceFeedback)    = ComputeGovernanceScore(pages, config);
        var (fewScore, fewFeedback)                  = ComputeStephenFewScore(pages);
        var (tufteScore, tufteFeedback)              = ComputeTufteScore(pages);
        var (graphicalScore, graphicalFeedback)      = ComputeGraphicalPerceptionScore(pages);
        var (densityScore, densityFeedback)          = ComputeDashboardDensityScore(pages, recommendations, navigationScoring);
        var (narrativeScore, narrativeFeedback)      = ComputeNarrativeDesignScore(pages, recommendations);

        _logger.LogDebug(
            "[Scoring] sub-scores — Gestalt={G:F1} CogLoad={C:F1} DataInk={D:F1} A11y={A:F1} VBP={V:F1} Few={F:F1} Tufte={T:F1}",
            gestaltScore, cogLoadScore, dataInkScore, accessibilityScore, vbpScore, fewScore, tufteScore);

#pragma warning disable CS0618
        var result = new ScoreResult
        {
            GestaltScore             = Clamp(gestaltScore),
            CognitiveLoadScore       = Clamp(cogLoadScore),
            DataInkScore             = Clamp(dataInkScore),
            AccessibilityScore       = Clamp(accessibilityScore),
            VisualBestPracticesScore = Clamp(vbpScore),
            EnterpriseGovernanceScore = Clamp(governanceScore),
            StephenFewScore          = Clamp(fewScore),
            TufteScore               = Clamp(tufteScore),
            GraphicalPerceptionScore = Clamp(graphicalScore),
            DensityScore             = Clamp(densityScore),
            NarrativeScore           = Clamp(narrativeScore),
            // Keep legacy fields in sync so any existing integration still works.
            LayoutScore    = Clamp(gestaltScore),
            ThemeScore     = Clamp(vbpScore),
            GovernanceScore = Clamp(governanceScore),
            Feedback = new()
            {
                ["gestalt"]             = gestaltFeedback,
                ["cognitiveLoad"]       = cogLoadFeedback,
                ["dataInk"]             = dataInkFeedback,
                ["accessibility"]       = a11yFeedback,
                ["visualBestPractices"] = vbpFeedback,
                ["governance"]          = governanceFeedback,
                ["stephenFew"]          = fewFeedback,
                ["tufte"]               = tufteFeedback,
                ["graphicalPerception"] = graphicalFeedback,
                ["density"]             = densityFeedback,
                ["narrative"]           = narrativeFeedback,
            },
            PageCount       = pages.Count,
            Recommendations = recommendations,
            ReportPath      = location.ReportRootPath,
            PageScores      = new(),
            ScoringErrors   = new(),
            ScoredAt        = DateTimeOffset.UtcNow,
            FrameworkWeights = frameworkWeights,
            DataVisualCount = reportComposition.DataVisualCount,
            NavigationVisualCount = reportComposition.NavigationVisualCount,
            HiddenVisualCount = reportComposition.HiddenVisualCount,
            VisualMetadata = pages.Count == 1 ? BuildPageVisualMetadataSummary(pages[0]) : null,
        };
#pragma warning restore CS0618

        // ── Compute per-page breakdown ────────────────────────────────────────
        foreach (var page in pages)
        {
            try
            {
                var pageList = new List<PageData> { page };
                var pageComposition = BuildVisualComposition(page.Visuals, navigationScoring);
                var pageHasDataVisuals = pageComposition.DataVisualCount > 0;

                if (!pageHasDataVisuals)
                {
                    // Page with no visuals — set all scores to 0 with note
                    result.PageScores!.Add(new PageScore
                    {
                        PageName = page.DisplayName,
                        GestaltScore = 0,
                        CognitiveLoadScore = 0,
                        DataInkScore = 0,
                        AccessibilityScore = 0,
                        VisualBestPracticesScore = 0,
                        StephenFewScore = 0,
                        EnterpriseGovernanceScore = 0,
                        TufteScore = 0,
                        GraphicalPerceptionScore = 0,
                        DensityScore = 0,
                        NarrativeScore = 0,
                        Feedback = new()
                        {
                            ["gestalt"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                            ["cognitiveLoad"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                            ["dataInk"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                            ["accessibility"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                            ["visualBestPractices"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                            ["governance"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                            ["stephenFew"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                            ["tufte"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                            ["graphicalPerception"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                            ["density"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                            ["narrative"] = [FeedbackItem(false, "No data visuals on this page.", FindingTypes.Objective)],
                        },
                        Recommendations = [],
                        FrameworkWeights = frameworkWeights,
                        DataVisualCount = pageComposition.DataVisualCount,
                        NavigationVisualCount = pageComposition.NavigationVisualCount,
                        HiddenVisualCount = pageComposition.HiddenVisualCount,
                        VisualMetadata = BuildPageVisualMetadataSummary(page),
                    });
                    continue;
                }

                // Score this page
                var pagePageRecommendations = new List<string>();
                var (pGestalt, pGestaltFeedback)          = ComputeGestaltScore(pageList);
                var (pCogLoad, pCogLoadFeedback)          = ComputeCognitiveLoadScore(pageList, new(), navigationScoring);
                var (pDataInk, pDataInkFeedback)          = ComputeDataInkScore(pageList, new(), navigationScoring);
                var (pAccessibility, pA11yFeedback)       = ComputeAccessibilityScore(themeColors, pageList, new());
                var (pVbp, pVbpFeedback)                  = ComputeVisualBestPracticesScore(pageList, themeColors, new());
                var (pGovernance, pGovernanceFeedback)    = ComputeGovernanceScore(pageList, config);
                var (pFew, pFewFeedback)                  = ComputeStephenFewScore(pageList);
                var (pTufte, pTufteFeedback)              = ComputeTufteScore(pageList);
                var (pGraphical, pGraphicalFeedback)      = ComputeGraphicalPerceptionScore(pageList);
                var (pDensity, pDensityFeedback)          = ComputeDashboardDensityScore(pageList, pagePageRecommendations, navigationScoring);
                var (pNarrative, pNarrativeFeedback)      = ComputeNarrativeDesignScore(pageList, pagePageRecommendations);

                // Bookmark-aware overlay: replace per-framework scores with state averages when
                // bookmarks affect this page, and surface the per-state composite map.
                var pageOverlay = ComputeBookmarkAwareOverlay(
                    page, reportJson, themeColors, navigationScoring, config, frameworkWeights);

                var finalGestalt        = pageOverlay?.AveragedFrameworks["gestalt"]              ?? Clamp(pGestalt);
                var finalCogLoad        = pageOverlay?.AveragedFrameworks["cognitiveLoad"]        ?? Clamp(pCogLoad);
                var finalDataInk        = pageOverlay?.AveragedFrameworks["dataInk"]              ?? Clamp(pDataInk);
                var finalAccessibility  = pageOverlay?.AveragedFrameworks["accessibility"]        ?? Clamp(pAccessibility);
                var finalVbp            = pageOverlay?.AveragedFrameworks["visualBestPractices"]  ?? Clamp(pVbp);
                var finalGovernance     = pageOverlay?.AveragedFrameworks["governance"]           ?? Clamp(pGovernance);
                var finalFew            = pageOverlay?.AveragedFrameworks["stephenFew"]           ?? Clamp(pFew);
                var finalTufte          = pageOverlay?.AveragedFrameworks["tufte"]                ?? Clamp(pTufte);
                var finalGraphical      = pageOverlay?.AveragedFrameworks["graphicalPerception"]  ?? Clamp(pGraphical);
                var finalDensity        = pageOverlay?.AveragedFrameworks["density"]              ?? Clamp(pDensity);
                var finalNarrative      = pageOverlay?.AveragedFrameworks["narrative"]            ?? Clamp(pNarrative);

                if (pageOverlay is not null)
                {
                    pagePageRecommendations.Add(
                        $"[Info] Bookmark-aware scoring active: page scored across {pageOverlay.PerStateScores.Count} layout states (Default + {pageOverlay.PerStateScores.Count - 1} bookmark state{(pageOverlay.PerStateScores.Count == 2 ? string.Empty : "s")}).");

                    _logger.LogInformation(
                        "[Bookmark State] Page '{Page}' scored across {Count} states",
                        page.DisplayName, pageOverlay.PerStateScores.Count);
                }

                result.PageScores!.Add(new PageScore
                {
                    PageName = page.DisplayName,
                    GestaltScore = finalGestalt,
                    CognitiveLoadScore = finalCogLoad,
                    DataInkScore = finalDataInk,
                    AccessibilityScore = finalAccessibility,
                    VisualBestPracticesScore = finalVbp,
                    EnterpriseGovernanceScore = finalGovernance,
                    StephenFewScore = finalFew,
                    TufteScore = finalTufte,
                    GraphicalPerceptionScore = finalGraphical,
                    DensityScore = finalDensity,
                    NarrativeScore = finalNarrative,
                    Feedback = new()
                    {
                        ["gestalt"] = pGestaltFeedback,
                        ["cognitiveLoad"] = pCogLoadFeedback,
                        ["dataInk"] = pDataInkFeedback,
                        ["accessibility"] = pA11yFeedback,
                        ["visualBestPractices"] = pVbpFeedback,
                        ["governance"] = pGovernanceFeedback,
                        ["stephenFew"] = pFewFeedback,
                        ["tufte"] = pTufteFeedback,
                        ["graphicalPerception"] = pGraphicalFeedback,
                        ["density"] = pDensityFeedback,
                        ["narrative"] = pNarrativeFeedback,
                    },
                    Recommendations = pagePageRecommendations,
                    FrameworkWeights = frameworkWeights,
                    DataVisualCount = pageComposition.DataVisualCount,
                    NavigationVisualCount = pageComposition.NavigationVisualCount,
                    HiddenVisualCount = pageComposition.HiddenVisualCount,
                    VisualMetadata = BuildPageVisualMetadataSummary(page),
                    PerStateScores = pageOverlay?.PerStateScores,
                });

                _logger.LogDebug(
                    "[Scoring] Page '{Page}' — Composite: {Composite} (G={G} C={C} D={D} A={A} V={V} Gov={Gov} F={F})",
                    page.DisplayName,
                    result.PageScores[^1].CompositeScore,
                    finalGestalt, finalCogLoad, finalDataInk, finalAccessibility, finalVbp, finalGovernance, finalFew);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to score page '{page.DisplayName}': {ex.Message}";
                _logger.LogWarning("[Scoring] {Error}", errorMsg);
                result.ScoringErrors[page.DisplayName] = errorMsg;
            }
        }

        _logger.LogInformation(
            "[Scoring] Composite: {Score} (Gestalt={G} Cog={C} DataInk={D} A11y={A} VBP={V} Few={F})",
            result.CompositeScore,
            result.GestaltScore, result.CognitiveLoadScore, result.DataInkScore,
            result.AccessibilityScore, result.VisualBestPracticesScore, result.StephenFewScore);

        return result;
    }

    // ── Bookmark-aware (per-state) scoring ───────────────────────────────────

    /// <summary>
    /// Captures the bookmark-aware overlay for a page: per-state composite scores and the
    /// state-averaged per-framework scores that should replace the page's full-page scores
    /// when bookmarks are present.
    /// </summary>
    private sealed record BookmarkAwareOverlay(
        Dictionary<string, double> AveragedFrameworks,
        Dictionary<string, double> PerStateScores);

    /// <summary>
    /// Filters the set of report-level bookmarks to those whose controlled visuals overlap
    /// with the supplied page visual ids. Bookmarks that do not touch this page are excluded.
    /// </summary>
    private static List<BookmarkParser.BookmarkDefinition> FilterBookmarksForPage(
        IReadOnlyList<BookmarkParser.BookmarkDefinition> allBookmarks,
        HashSet<string> pageVisualIds)
    {
        if (allBookmarks.Count == 0 || pageVisualIds.Count == 0)
        {
            return [];
        }

        var bookmarksForPage = new List<BookmarkParser.BookmarkDefinition>(allBookmarks.Count);
        foreach (var bookmark in allBookmarks)
        {
            foreach (var visualId in bookmark.ControlledVisualIds)
            {
                if (pageVisualIds.Contains(visualId))
                {
                    bookmarksForPage.Add(bookmark);
                    break;
                }
            }
        }
        return bookmarksForPage;
    }

    /// <summary>
    /// Scores a "shadow" page (a page projected to a subset of its visuals) by running every
    /// framework against the single-page list and returning the composite + per-framework map.
    /// Side-effect-free: framework recommendations are discarded so per-state scoring does not
    /// pollute the page-level recommendations list.
    /// </summary>
    private (double composite, Dictionary<string, double> frameworks) ScoreShadowPage(
        PageData shadowPage,
        List<string> themeColors,
        NavigationScoringSettings navigationScoring,
        JsonElement? config,
        Dictionary<string, double>? frameworkWeights)
    {
        var shadowList = new List<PageData> { shadowPage };
        var throwaway = new List<string>();

        var (gestalt, _)        = ComputeGestaltScore(shadowList);
        var (cogLoad, _)        = ComputeCognitiveLoadScore(shadowList, throwaway, navigationScoring);
        var (dataInk, _)        = ComputeDataInkScore(shadowList, throwaway, navigationScoring);
        var (accessibility, _)  = ComputeAccessibilityScore(themeColors, shadowList, throwaway);
        var (vbp, _)            = ComputeVisualBestPracticesScore(shadowList, themeColors, throwaway);
        var (governance, _)     = ComputeGovernanceScore(shadowList, config);
        var (few, _)            = ComputeStephenFewScore(shadowList);
        var (tufte, _)          = ComputeTufteScore(shadowList);
        var (graphical, _)      = ComputeGraphicalPerceptionScore(shadowList);
        var (density, _)        = ComputeDashboardDensityScore(shadowList, throwaway, navigationScoring);
        var (narrative, _)      = ComputeNarrativeDesignScore(shadowList, throwaway);

#pragma warning disable CS0618
        var shadowResult = new ScoreResult
        {
            GestaltScore              = Clamp(gestalt),
            CognitiveLoadScore        = Clamp(cogLoad),
            DataInkScore              = Clamp(dataInk),
            AccessibilityScore        = Clamp(accessibility),
            VisualBestPracticesScore  = Clamp(vbp),
            EnterpriseGovernanceScore = Clamp(governance),
            StephenFewScore           = Clamp(few),
            TufteScore                = Clamp(tufte),
            GraphicalPerceptionScore  = Clamp(graphical),
            DensityScore              = Clamp(density),
            NarrativeScore            = Clamp(narrative),
            FrameworkWeights          = frameworkWeights,
        };
#pragma warning restore CS0618

        var frameworks = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["gestalt"]              = shadowResult.GestaltScore,
            ["cognitiveLoad"]        = shadowResult.CognitiveLoadScore,
            ["dataInk"]              = shadowResult.DataInkScore,
            ["accessibility"]        = shadowResult.AccessibilityScore,
            ["visualBestPractices"]  = shadowResult.VisualBestPracticesScore,
            ["governance"]           = shadowResult.EnterpriseGovernanceScore,
            ["stephenFew"]           = shadowResult.StephenFewScore,
            ["tufte"]                = shadowResult.TufteScore,
            ["graphicalPerception"]  = shadowResult.GraphicalPerceptionScore,
            ["density"]              = shadowResult.DensityScore,
            ["narrative"]            = shadowResult.NarrativeScore,
        };

        return (shadowResult.CompositeScore, frameworks);
    }

    /// <summary>
    /// Computes a bookmark-aware overlay for a page. For each layout state derived from the
    /// page's bookmarks the page visuals are filtered to those visible in the state and re-scored,
    /// producing a per-state composite map and per-framework state averages that the caller may
    /// use to replace the full-page scores. Returns <c>null</c> when no bookmarks affect the page.
    /// </summary>
    private BookmarkAwareOverlay? ComputeBookmarkAwareOverlay(
        PageData page,
        JsonObject reportJson,
        List<string> themeColors,
        NavigationScoringSettings navigationScoring,
        JsonElement? config,
        Dictionary<string, double>? frameworkWeights)
    {
        var allBookmarks = BookmarkParser.ParseBookmarks(reportJson);
        if (allBookmarks.Count == 0)
        {
            return null;
        }

        var pageVisualIds = new HashSet<string>(
            page.Visuals.Select(v => v.Id),
            StringComparer.Ordinal);
        if (pageVisualIds.Count == 0)
        {
            return null;
        }

        var bookmarksForPage = FilterBookmarksForPage(allBookmarks, pageVisualIds);
        if (bookmarksForPage.Count == 0)
        {
            return null;
        }

        var states = LayoutStateGenerator.GenerateStates(
            pageVisualIds.ToList(),
            bookmarksForPage);
        if (states.Count == 0)
        {
            return null;
        }

        var perStateScores = new Dictionary<string, double>(StringComparer.Ordinal);
        var stateFrameworks = new List<Dictionary<string, double>>(states.Count);

        foreach (var state in states)
        {
            var visibleIds = new HashSet<string>(state.VisibleVisualIds, StringComparer.Ordinal);
            var filteredVisuals = page.Visuals.Where(v => visibleIds.Contains(v.Id)).ToList();
            var shadowPage = page with { Visuals = filteredVisuals };
            var (composite, frameworks) = ScoreShadowPage(
                shadowPage, themeColors, navigationScoring, config, frameworkWeights);

            // Disambiguate any duplicate state names so the perStateScores map preserves all states.
            var key = state.StateName;
            if (perStateScores.ContainsKey(key))
            {
                var suffix = 2;
                while (perStateScores.ContainsKey($"{key} ({suffix})"))
                {
                    suffix++;
                }
                key = $"{key} ({suffix})";
            }
            perStateScores[key] = composite;
            stateFrameworks.Add(frameworks);
        }

        var averaged = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var frameworkKey in stateFrameworks[0].Keys)
        {
            averaged[frameworkKey] = Math.Round(
                stateFrameworks.Average(d => d[frameworkKey]), 2);
        }

        return new BookmarkAwareOverlay(averaged, perStateScores);
    }

    // ── 1. Gestalt Principles score ──────────────────────────────────────────

    /// <summary>
    /// Scores four Gestalt sub-criteria: grid alignment (35 pts), figure/ground (30 pts),
    /// similarity (20 pts), and visual presence (15 pts).
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeGestaltScore(List<PageData> pages)
    {
        var feedback   = new List<FrameworkFeedbackItem>();
        var allVisuals = pages.SelectMany(p => p.Visuals).ToList();
        var kpiConsistencyIssues = CollectLayoutIssues(pages, AnalyzeTopBandKpiConsistency);
        var spacingRhythmIssues = CollectLayoutIssues(pages, AnalyzeSpacingRhythm);
        PageLayoutIssue? worstKpiConsistencyIssue = kpiConsistencyIssues.Count > 0
            ? kpiConsistencyIssues
                .OrderByDescending(issue => issue.Visuals.Count)
                .ThenByDescending(issue => issue.Penalty)
                .First()
            : null;
        PageLayoutIssue? worstSpacingRhythmIssue = spacingRhythmIssues.Count > 0
            ? spacingRhythmIssues
                .OrderByDescending(issue => issue.Visuals.Count)
                .ThenByDescending(issue => issue.Penalty)
                .First()
            : null;

        // Sub 1: Grid alignment (35 pts)
        int totalV = 0, alignedV = 0;
        var misalignedPages = new List<string>();
        var misalignedVisuals = new List<(string PageName, VisualData Visual)>();
        foreach (var page in pages)
        {
            double colWidthPx = GetColumnWidth(page);
            double rowHeightPx = GetRowHeight(page);
            foreach (var v in page.Visuals)
            {
                // Skip hidden visuals from alignment scoring
                if (v.IsHidden) continue;
                
                totalV++;
                if (IsNearMultiple(v.X, colWidthPx) && IsNearMultiple(v.Y, rowHeightPx))
                    alignedV++;
                else
                {
                    misalignedVisuals.Add((page.DisplayName, v));
                    if (!misalignedPages.Contains(page.DisplayName))
                    {
                        misalignedPages.Add(page.DisplayName);
                    }
                }
            }
        }
        double alignPct = totalV == 0 ? 100.0 : (double)alignedV / totalV * 100.0;
        var alignmentConcerns = new List<string>();
        var alignmentAffectedVisuals = new List<(string PageName, VisualData Visual)>(misalignedVisuals);
        double alignmentPenalty = 0.0;
        if (misalignedPages.Count > 0)
        {
            alignmentConcerns.Add($"{misalignedPages.Count} page(s) have off-grid visuals");
        }

        if (worstKpiConsistencyIssue is { } kpiConsistencyIssue)
        {
            alignmentConcerns.Add(kpiConsistencyIssue.Message);
            alignmentAffectedVisuals.AddRange(kpiConsistencyIssue.Visuals.Select(visual => (kpiConsistencyIssue.Page.DisplayName, visual)));
            alignmentPenalty += kpiConsistencyIssue.Penalty;
        }

        if (worstSpacingRhythmIssue is { } spacingRhythmIssue)
        {
            alignmentConcerns.Add(spacingRhythmIssue.Message);
            alignmentAffectedVisuals.AddRange(spacingRhythmIssue.Visuals.Select(visual => (spacingRhythmIssue.Page.DisplayName, visual)));
            alignmentPenalty += spacingRhythmIssue.Penalty;
        }

        double sub1 = Math.Max(0.0, alignPct * 0.35 - alignmentPenalty);
        feedback.Add(ScoredFeedback(
            alignmentConcerns.Count == 0,
            alignmentConcerns.Count == 0
                ? "Grid alignment: All visuals align to the 12-column grid — strong spatial organisation."
                : $"Grid alignment: {string.Join("; ", alignmentConcerns)} — tighten the layout so peer visuals read as a deliberate system.",
            sub1,
            35.0,
            FindingTypes.StrongHeuristic,
            alignmentConcerns.Count == 0 ? null : BuildAffectedVisuals(alignmentAffectedVisuals)));

        // Sub 2: Figure/ground contrast — has KPI/card AND at least one chart (30 pts)
        bool hasFigGround = allVisuals.Any(v => v.IsKpiCard) && allVisuals.Any(v => !v.IsKpiCard && !v.IsDecorative);
        double sub2 = hasFigGround ? 30.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasFigGround,
            hasFigGround
                ? "Figure/ground: KPI cards contrast with supporting charts — effective visual hierarchy."
                : "Figure/ground: Add at least one KPI/card visual alongside charts to create a clear focal point.",
            sub2,
            30.0,
            FindingTypes.StrongHeuristic));

        // Sub 3: Similarity — 2–5 unique non-decorative visual types (20 pts)
        int uniqueTypes = allVisuals.Where(v => !v.IsDecorative).Select(v => v.Type).Distinct().Count();
        bool similarityOk = uniqueTypes is >= 2 and <= 5;
        double sub3 = similarityOk ? 20.0 : 10.0;
        feedback.Add(ScoredFeedback(
            similarityOk,
            similarityOk
                ? $"Similarity: {uniqueTypes} distinct visual types guide attention without visual noise."
                : uniqueTypes < 2
                    ? "Similarity: Only one visual type in use — add 2–5 distinct chart types for grouping cues."
                    : $"Similarity: {uniqueTypes} visual types may cause noise — aim for 2–5 distinct types.",
            sub3,
            20.0,
            FindingTypes.StrongHeuristic));

        // Sub 4: Visual presence (15 pts)
        bool hasViz = allVisuals.Any(v => !v.IsDecorative);
        double sub4 = hasViz ? 15.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasViz,
            hasViz ? "Visual presence: Report contains data visuals."
                   : "Visual presence: No data visuals detected — add charts, tables, or KPI cards.",
            sub4,
            15.0,
            FindingTypes.Objective));

        if (worstKpiConsistencyIssue is { } topBandIssue)
        {
            feedback.Add(FeedbackItem(
                false,
                $"Top-band KPI consistency: {topBandIssue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(topBandIssue.Page.DisplayName, topBandIssue.Visuals)));
        }

        if (worstSpacingRhythmIssue is { } rowRhythmIssue)
        {
            feedback.Add(FeedbackItem(
                false,
                $"Spacing rhythm: {rowRhythmIssue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(rowRhythmIssue.Page.DisplayName, rowRhythmIssue.Visuals)));
        }

        return (Clamp(sub1 + sub2 + sub3 + sub4), feedback);
    }

    private static bool IsNearMultiple(double value, double multiple, double tolerance = 1.0) =>
        Math.Abs(value % multiple) <= tolerance || Math.Abs(value % multiple - multiple) <= tolerance;

    // ── 2. Cognitive load score ──────────────────────────────────────────────

    /// <summary>
    /// Per page: <c>V &gt; 6 ? Max(0, 100 − Log₂(V/6)×15) : 100</c>.
    /// Final score is the mean across pages.
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeCognitiveLoadScore(
        List<PageData> pages,
        List<string> recs,
        NavigationScoringSettings navigationScoring)
    {
        var feedback = new List<FrameworkFeedbackItem>();
        if (pages.Count == 0)
        {
            feedback.Add(ScoredFeedback(
                true,
                "Visual density: No pages to evaluate.",
                100.0,
                100.0,
                FindingTypes.Objective));
            return (100.0, feedback);
        }

        double total = 0.0;
        var dense = new List<(PageData Page, VisualComposition Composition)>();
        var navHotspots = new List<(PageData Page, VisualComposition Composition)>();
        var filterPlacementIssues = new List<PageLayoutIssue>();
        var filterScatterIssues = new List<PageLayoutIssue>();
        var overviewFilterDensityIssues = new List<PageLayoutIssue>();
        var scanPathIssues = new List<PageLayoutIssue>();

        foreach (var page in pages)
        {
            var composition = BuildVisualComposition(page.Visuals, navigationScoring);
            double v = composition.WeightedVisibleCount;
            double s = v > 6 ? Math.Max(0, 100 - Math.Log2(v / 6.0) * 15) : 100;

            if (AnalyzeFilterPlacement(page) is { } filterPlacementIssue)
            {
                filterPlacementIssues.Add(filterPlacementIssue);
                s = Math.Max(0.0, s - filterPlacementIssue.Penalty);
            }

            if (AnalyzeFilterScatter(page) is { } filterScatterIssue)
            {
                filterScatterIssues.Add(filterScatterIssue);
                s = Math.Max(0.0, s - filterScatterIssue.Penalty);
            }

            if (AnalyzeOverviewFilterDensity(page) is { } overviewFilterDensityIssue)
            {
                overviewFilterDensityIssues.Add(overviewFilterDensityIssue);
                s = Math.Max(0.0, s - overviewFilterDensityIssue.Penalty);
            }

            if (AnalyzePrimaryScanPath(page) is { } scanPathIssue)
            {
                scanPathIssues.Add(scanPathIssue);
                s = Math.Max(0.0, s - scanPathIssue.Penalty);
            }

            total += s;
            if (v > 6)
            {
                dense.Add((page, composition));
            }

            if (navigationScoring.Enabled &&
                composition.NavigationVisualCount > navigationScoring.WarningNavigationCount &&
                composition.HiddenVisualCount > navigationScoring.WarningHiddenVisualCount)
            {
                navHotspots.Add((page, composition));
            }
        }

        double score = total / pages.Count;

        if (dense.Count > 0)
        {
            var worst = dense.OrderByDescending(item => item.Composition.WeightedVisibleCount).First();
            var navigationSummary = navigationScoring.Enabled && worst.Composition.NavigationVisualCount > 0
                ? $" ({worst.Composition.DataVisualCount} data, {worst.Composition.NavigationVisualCount} navigation at {navigationScoring.WeightPercent:F0}% weight)"
                : string.Empty;
            var denseVisuals = BuildAffectedVisuals(
                worst.Page.DisplayName,
                worst.Page.Visuals
                    .Where(v => !v.IsHidden)
                    .OrderByDescending(v => v.IsNavigationElement)
                    .ThenBy(v => v.Type, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(v => v.Id, StringComparer.OrdinalIgnoreCase));
            recs.Add($"[Medium] Cognitive Load: '{worst.Page.DisplayName}' has a weighted visual load of {worst.Composition.WeightedVisibleCount:F1}{navigationSummary} " +
                     "(>6 recommended max). Consider simplifying the page or splitting it into sub-pages.");
            feedback.Add(ScoredFeedback(
                false,
                $"Visual density: '{worst.Page.DisplayName}' has a weighted load of {worst.Composition.WeightedVisibleCount:F1}{navigationSummary} (target ≤6 per page) — consider simplifying controls or splitting the page to reduce cognitive overload.",
                score,
                100.0,
                FindingTypes.StrongHeuristic,
                denseVisuals));
        }
        else
        {
            feedback.Add(ScoredFeedback(
                true,
                "Visual density: All pages have ≤6 visuals — comfortable viewing density.",
                score,
                100.0,
                FindingTypes.StrongHeuristic));
        }

        if (navigationScoring.Enabled && navHotspots.Count > 0)
        {
            var hotspot = navHotspots
                .OrderByDescending(item => item.Composition.NavigationVisualCount)
                .ThenByDescending(item => item.Composition.HiddenVisualCount)
                .First();
            var hotspotVisuals = BuildAffectedVisuals(
                hotspot.Page.DisplayName,
                hotspot.Page.Visuals
                    .Where(v => v.IsNavigationElement || v.IsHidden)
                    .OrderByDescending(v => v.IsNavigationElement)
                    .ThenBy(v => v.Type, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(v => v.Id, StringComparer.OrdinalIgnoreCase));
            recs.Add(
                $"[Medium] Navigation: '{hotspot.Page.DisplayName}' uses {hotspot.Composition.NavigationVisualCount} navigation control(s) and {hotspot.Composition.HiddenVisualCount} hidden visual(s). Consider consolidating bookmark-driven interaction with button slicers or field parameters.");
            feedback.Add(FeedbackItem(
                false,
                $"Navigation complexity: '{hotspot.Page.DisplayName}' combines {hotspot.Composition.NavigationVisualCount} navigation control(s) with {hotspot.Composition.HiddenVisualCount} hidden visual(s) — this often signals an interaction-heavy layout that can be simplified.",
                FindingTypes.StrongHeuristic,
                hotspotVisuals));
        }
        else if (navigationScoring.Enabled)
        {
            feedback.Add(FeedbackItem(
                true,
                $"Navigation treatment: Navigation controls count at {navigationScoring.WeightPercent:F0}% of a standard visual in cognitive load scoring.",
                FindingTypes.Objective));
        }

        if (filterPlacementIssues.Count > 0)
        {
            var filterPlacementIssue = filterPlacementIssues
                .OrderByDescending(issue => issue.Visuals.Count)
                .ThenByDescending(issue => issue.Penalty)
                .First();
            recs.Add(
                $"[Medium] Cognitive Load: Move slicers on '{filterPlacementIssue.Page.DisplayName}' into a left rail or top control band so they do not interrupt the primary reading flow.");
            feedback.Add(FeedbackItem(
                false,
                $"Filter placement: {filterPlacementIssue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(filterPlacementIssue.Page.DisplayName, filterPlacementIssue.Visuals)));
        }

        if (filterScatterIssues.Count > 0)
        {
            var filterScatterIssue = filterScatterIssues
                .OrderByDescending(issue => issue.Visuals.Count)
                .ThenByDescending(issue => issue.Penalty)
                .First();
            recs.Add(
                $"[Medium] Cognitive Load: Consolidate slicers on '{filterScatterIssue.Page.DisplayName}' into a single top band or left rail instead of scattering them across the page.");
            feedback.Add(FeedbackItem(
                false,
                $"Filter consolidation: {filterScatterIssue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(filterScatterIssue.Page.DisplayName, filterScatterIssue.Visuals)));
        }

        if (overviewFilterDensityIssues.Count > 0)
        {
            var overviewFilterDensityIssue = overviewFilterDensityIssues
                .OrderByDescending(issue => issue.Visuals.Count)
                .ThenByDescending(issue => issue.Penalty)
                .First();
            recs.Add(
                $"[Medium] Cognitive Load: Reduce or merge slicers on '{overviewFilterDensityIssue.Page.DisplayName}' so the overview page stays focused on the main evidence.");
            feedback.Add(FeedbackItem(
                false,
                $"Overview filter density: {overviewFilterDensityIssue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(overviewFilterDensityIssue.Page.DisplayName, overviewFilterDensityIssue.Visuals)));
        }

        if (scanPathIssues.Count > 0)
        {
            var scanPathIssue = scanPathIssues
                .OrderByDescending(issue => issue.Visuals.Count)
                .ThenByDescending(issue => issue.Penalty)
                .First();
            recs.Add(
                $"[Medium] Cognitive Load: Give '{scanPathIssue.Page.DisplayName}' a clearer upper-left entry point so the first scan lands on the main evidence before secondary controls.");
            feedback.Add(FeedbackItem(
                false,
                $"Primary scan path: {scanPathIssue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(scanPathIssue.Page.DisplayName, scanPathIssue.Visuals)));
        }

        // Add positive feedback for low-density pages
        int idealPages = pages.Count(p => BuildVisualComposition(p.Visuals, navigationScoring).WeightedVisibleCount <= 4);
        if (idealPages > 0)
        {
            feedback.Add(FeedbackItem(
                true,
                $"Optimal density: {idealPages} page(s) have ≤4 visuals — excellent focus.",
                FindingTypes.StrongHeuristic));
        }

        return (score, feedback);
    }

    // ── 3. Data-ink score ────────────────────────────────────────────────────

    // Visual types treated as decorative (no data-ink).
    private static readonly HashSet<string> _decorativeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image", "textbox", "shape", "basicShape", "actionButton", "navigationButton",
    };

    /// <summary>
    /// % of non-decorative visuals.  Decorative = image, textbox, shape, actionButton.
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeDataInkScore(
        List<PageData> pages,
        List<string> recs,
        NavigationScoringSettings navigationScoring)
    {
        var feedback = new List<FrameworkFeedbackItem>();
        int total = 0, dataInk = 0;
        int excludedNavigation = 0;
        var decorativeVisuals = new List<(string PageName, VisualData Visual)>();

        foreach (var page in pages)
        {
            foreach (var v in page.Visuals)
            {
                // Skip hidden visuals from data-ink ratio
                if (v.IsHidden) continue;

                if (navigationScoring.Enabled && v.IsNavigationElement)
                {
                    excludedNavigation++;
                    continue;
                }

                total++;
                if (!v.IsDecorative)
                {
                    dataInk++;
                }
                else
                {
                    decorativeVisuals.Add((page.DisplayName, v));
                }
            }
        }

        if (total == 0)
        {
            feedback.Add(ScoredFeedback(
                true,
                "Data-ink ratio: No visuals to evaluate.",
                100.0,
                100.0,
                FindingTypes.Objective));
            return (100.0, feedback);
        }

        double ratio = (double)dataInk / total;
        if (ratio < 0.80)
        {
            int decorativeCount = total - dataInk;
            recs.Add($"[Medium] Data-Ink: {(1 - ratio) * 100:F0}% of visuals are decorative " +
                     "(images, shapes, text boxes). Remove non-essential decorative elements.");
            feedback.Add(ScoredFeedback(
                false,
                $"Data-ink ratio: {ratio * 100:F0}% of visuals carry data; {decorativeCount} decorative visual(s) remain ({(1 - ratio) * 100:F0}% of total) — remove images, shapes, and text boxes that don't carry data.",
                ratio * 100.0,
                100.0,
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(
                    decorativeVisuals
                        .OrderBy(entry => entry.PageName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(entry => entry.Visual.Type, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(entry => entry.Visual.Id, StringComparer.OrdinalIgnoreCase))));
        }
        else
        {
            feedback.Add(ScoredFeedback(
                true,
                $"Data-ink ratio: {ratio * 100:F0}% of visuals carry data — minimal decorative overhead.",
                ratio * 100.0,
                100.0,
                FindingTypes.StrongHeuristic));
        }

        if (navigationScoring.Enabled)
        {
            feedback.Add(FeedbackItem(
                true,
                excludedNavigation > 0
                    ? $"Navigation treatment: {excludedNavigation} navigation visual(s) excluded from Data-Ink Ratio."
                    : "Navigation treatment: No navigation visuals were excluded from Data-Ink Ratio.",
                FindingTypes.Objective));
        }

        // Surface the decorative types present
        var decorativeTypes = pages
            .SelectMany(p => p.Visuals)
            .Where(v => !v.IsHidden)
            .Where(v => !navigationScoring.Enabled || !v.IsNavigationElement)
            .Where(v => v.IsDecorative && !string.IsNullOrWhiteSpace(v.Type))
            .Select(v => v.Type)
            .Distinct()
            .ToList();
        if (decorativeTypes.Count > 0)
        {
            feedback.Add(FeedbackItem(
                false,
                $"Decorative types present: {string.Join(", ", decorativeTypes)} — consider removal to improve data-ink ratio.",
                FindingTypes.StrongHeuristic));
        }

        return (ratio * 100.0, feedback);
    }

    // ── 4. Accessibility score ────────────────────────────────────────────────

    /// <summary>
    /// Sub-criteria weights for the accessibility score (must sum to 100).
    /// </summary>
    private const double A11yPalettePoints = 40.0;
    private const double A11yOnCanvasPoints = 40.0;
    private const double A11yColorblindPoints = 20.0;

    /// <summary>
    /// Computes the accessibility score across three sub-criteria:
    /// <list type="number">
    /// <item><b>Theme palette contrast against white (40 pts)</b> — percentage of theme colours
    /// that meet WCAG 2.1 AA against a white background.</item>
    /// <item><b>On-canvas text contrast (40 pts)</b> — for each visual that exposes both a
    /// <see cref="VisualFormattingMetadata.BackgroundFillColor"/> and a
    /// <see cref="VisualFormattingMetadata.FontColor"/>, compute the actual contrast ratio
    /// instead of assuming a white background.</item>
    /// <item><b>Colorblind-safe palette (20 pts)</b> — flag theme palettes that contain
    /// red/green pairs that simulate to indistinguishable values for deuteranopia.</item>
    /// </list>
    /// If no theme colours or formatted visuals are available, the corresponding sub-criterion
    /// awards full marks rather than failing.
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeAccessibilityScore(
        List<string> themeColors,
        List<PageData> pages,
        List<string> recs)
    {
        var feedback = new List<FrameworkFeedbackItem>();
        double total = 0;

        // ── Sub 1: Theme palette contrast vs. white (40 pts) ────────────────
        total += ScoreA11yPaletteContrast(themeColors, feedback, recs);

        // ── Sub 2: On-canvas text contrast (40 pts) ─────────────────────────
        total += ScoreA11yOnCanvasContrast(pages, feedback, recs);

        // ── Sub 3: Colorblind-safe palette (20 pts) ─────────────────────────
        total += ScoreA11yColorblindPalette(themeColors, feedback, recs);

        return (Clamp(total), feedback);
    }

    private static double ScoreA11yPaletteContrast(
        List<string> themeColors,
        List<FrameworkFeedbackItem> feedback,
        List<string> recs)
    {
        if (themeColors.Count == 0)
        {
            feedback.Add(ScoredFeedback(
                true,
                "WCAG 2.1 AA palette: No custom theme detected — using default Power BI theme colours.",
                A11yPalettePoints,
                A11yPalettePoints,
                FindingTypes.Objective));
            return A11yPalettePoints;
        }

        int passing = 0;
        var failing = new List<string>();
        foreach (var hex in themeColors)
        {
            try
            {
                if (WcagContrastCalculator.MeetsNormalTextAA(hex, BackgroundWhite))
                    passing++;
                else
                    failing.Add(hex);
            }
            catch
            {
                // Skip malformed hex values.
            }
        }

        double earned = (double)passing / themeColors.Count * A11yPalettePoints;

        if (failing.Count > 0)
        {
            recs.Add($"[High] Accessibility: {failing.Count} theme colour(s) fail WCAG 2.1 AA contrast against white: " +
                     $"{string.Join(", ", failing.Take(3))}{(failing.Count > 3 ? $" (+{failing.Count - 3} more)" : "")}. " +
                     "Update the report theme colours to replace them.");
            feedback.Add(ScoredFeedback(
                false,
                $"WCAG 2.1 AA palette: {failing.Count} colour(s) fail contrast ratio ≥4.5:1 against white: " +
                $"{string.Join(", ", failing.Take(3))}{(failing.Count > 3 ? $" and {failing.Count - 3} more" : "")}. " +
                "Update the report theme with accessible colours to fix.",
                earned,
                A11yPalettePoints,
                FindingTypes.Objective));
        }
        else
        {
            feedback.Add(ScoredFeedback(
                true,
                $"WCAG 2.1 AA palette: All {passing} theme colour(s) pass contrast ratio ≥4.5:1 against white.",
                earned,
                A11yPalettePoints,
                FindingTypes.Objective));
        }

        return earned;
    }

    private static double ScoreA11yOnCanvasContrast(
        List<PageData> pages,
        List<FrameworkFeedbackItem> feedback,
        List<string> recs)
    {
        var pairs = pages
            .SelectMany(page => page.Visuals.Select(visual => (page, visual)))
            .Where(entry => !entry.visual.IsHidden)
            .Where(entry =>
                TryNormalizeHex(entry.visual.Formatting.BackgroundFillColor) is not null &&
                TryNormalizeHex(entry.visual.Formatting.FontColor) is not null)
            .ToList();

        if (pairs.Count == 0)
        {
            feedback.Add(ScoredFeedback(
                true,
                "On-canvas text contrast: No visuals expose both background and font colour metadata — unable to evaluate. Awarding full marks; theme palette check still applies.",
                A11yOnCanvasPoints,
                A11yOnCanvasPoints,
                FindingTypes.Objective));
            return A11yOnCanvasPoints;
        }

        int passing = 0;
        var failingRefs = new List<AffectedVisualReference>();

        foreach (var (page, visual) in pairs)
        {
            var bg = TryNormalizeHex(visual.Formatting.BackgroundFillColor)!;
            var fg = TryNormalizeHex(visual.Formatting.FontColor)!;
            try
            {
                if (WcagContrastCalculator.MeetsNormalTextAA(bg, fg))
                {
                    passing++;
                }
                else
                {
                    failingRefs.Add(new AffectedVisualReference(page.DisplayName, visual.Id, visual.Type));
                }
            }
            catch
            {
                // Skip pairs that can't be parsed; do not penalize the visual.
            }
        }

        int evaluated = passing + failingRefs.Count;
        if (evaluated == 0)
        {
            feedback.Add(ScoredFeedback(
                true,
                "On-canvas text contrast: No parseable background/font colour pairs found.",
                A11yOnCanvasPoints,
                A11yOnCanvasPoints,
                FindingTypes.Objective));
            return A11yOnCanvasPoints;
        }

        double earned = (double)passing / evaluated * A11yOnCanvasPoints;

        if (failingRefs.Count > 0)
        {
            recs.Add(
                $"[High] Accessibility: {failingRefs.Count} visual(s) have on-canvas background/font pairs below the WCAG 2.1 AA threshold (4.5:1). " +
                "Increase contrast between visual backgrounds and their text colours.");
            feedback.Add(ScoredFeedback(
                false,
                $"On-canvas text contrast: {failingRefs.Count} visual(s) fail WCAG 2.1 AA ≥4.5:1 against their own background fill — increase contrast on these visuals.",
                earned,
                A11yOnCanvasPoints,
                FindingTypes.Objective,
                failingRefs));
        }
        else
        {
            feedback.Add(ScoredFeedback(
                true,
                $"On-canvas text contrast: All {passing} visual(s) with parsed background/font colours pass WCAG 2.1 AA against their actual backgrounds.",
                earned,
                A11yOnCanvasPoints,
                FindingTypes.Objective));
        }

        return earned;
    }

    private static double ScoreA11yColorblindPalette(
        List<string> themeColors,
        List<FrameworkFeedbackItem> feedback,
        List<string> recs)
    {
        var normalized = themeColors
            .Select(TryNormalizeHex)
            .Where(hex => hex is not null)
            .Select(hex => hex!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count < 2)
        {
            feedback.Add(ScoredFeedback(
                true,
                "Colorblind-safe palette: Fewer than two parseable theme colours — colourblind pair check skipped.",
                A11yColorblindPoints,
                A11yColorblindPoints,
                FindingTypes.Objective));
            return A11yColorblindPoints;
        }

        var problemPairs = new List<(string A, string B)>();
        for (int i = 0; i < normalized.Count; i++)
        {
            for (int j = i + 1; j < normalized.Count; j++)
            {
                if (LooksLikeRedGreenPair(normalized[i], normalized[j]) &&
                    SimulatesToSimilarUnderDeuteranopia(normalized[i], normalized[j]))
                {
                    problemPairs.Add((normalized[i], normalized[j]));
                }
            }
        }

        if (problemPairs.Count == 0)
        {
            feedback.Add(ScoredFeedback(
                true,
                "Colorblind-safe palette: No red/green theme pairs simulate to indistinguishable values for deuteranopia.",
                A11yColorblindPoints,
                A11yColorblindPoints,
                FindingTypes.Objective));
            return A11yColorblindPoints;
        }

        var sample = problemPairs.Take(2).Select(p => $"{p.A}/{p.B}").ToList();
        recs.Add(
            $"[High] Accessibility: {problemPairs.Count} theme colour pair(s) are likely indistinguishable for users with deuteranopia: {string.Join("; ", sample)}. " +
            "Choose distinct hues or add non-colour cues (icons, patterns).");
        feedback.Add(ScoredFeedback(
            false,
            $"Colorblind-safe palette: {problemPairs.Count} red/green theme pair(s) simulate to similar values for deuteranopia ({string.Join("; ", sample)}). " +
            "Replace them or add non-colour distinguishing cues.",
            0,
            A11yColorblindPoints,
            FindingTypes.StrongHeuristic));

        return 0;
    }

    /// <summary>
    /// Returns a normalized #RRGGBB string when the input is a parseable hex colour;
    /// returns <c>null</c> for null, empty, or malformed inputs. Tolerates the # prefix
    /// being optional and the 3-digit shorthand (#RGB).
    /// </summary>
    private static string? TryNormalizeHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        var trimmed = hex.Trim().TrimStart('#');
        if (trimmed.Length == 3)
        {
            trimmed = $"{trimmed[0]}{trimmed[0]}{trimmed[1]}{trimmed[1]}{trimmed[2]}{trimmed[2]}";
        }
        if (trimmed.Length != 6) return null;
        for (int i = 0; i < 6; i++)
        {
            if (!Uri.IsHexDigit(trimmed[i])) return null;
        }
        return "#" + trimmed.ToUpperInvariant();
    }

    /// <summary>
    /// Returns <c>true</c> when one colour reads as predominantly red and the other as predominantly green
    /// (the classic red/green colourblindness failure pattern). Uses a coarse RGB dominance heuristic.
    /// </summary>
    private static bool LooksLikeRedGreenPair(string a, string b) =>
        (IsRedDominant(a) && IsGreenDominant(b)) || (IsGreenDominant(a) && IsRedDominant(b));

    private static bool IsRedDominant(string hex)
    {
        var (r, g, bl) = HexToRgb(hex);
        return r > g + 40 && r > bl + 40;
    }

    private static bool IsGreenDominant(string hex)
    {
        var (r, g, bl) = HexToRgb(hex);
        return g > r + 40 && g > bl + 40;
    }

    /// <summary>
    /// Applies a simple deuteranopia simulation (Brettel/Viénot-style projection collapsed to a
    /// linearised channel mix) and reports whether the two simulated colours fall within a small
    /// perceptual distance. Intentionally conservative — only flag pairs that are clearly at risk.
    /// </summary>
    private static bool SimulatesToSimilarUnderDeuteranopia(string a, string b)
    {
        var simA = SimulateDeuteranopia(a);
        var simB = SimulateDeuteranopia(b);
        double dr = simA.R - simB.R;
        double dg = simA.G - simB.G;
        double db = simA.B - simB.B;
        double distance = Math.Sqrt(dr * dr + dg * dg + db * db);
        // sRGB values in [0,1]; 0.15 is a coarse perceptual threshold for "looks similar".
        return distance < 0.15;
    }

    private static (double R, double G, double B) SimulateDeuteranopia(string hex)
    {
        var (rByte, gByte, bByte) = HexToRgb(hex);
        double r = rByte / 255.0;
        double g = gByte / 255.0;
        double b = bByte / 255.0;
        // Approximate deuteranopia projection in sRGB space (linear approximation of the
        // Brettel/Viénot model). Sufficient for "indistinguishable hue" warnings; not a full
        // CIE simulation.
        double simR = 0.625 * r + 0.375 * g + 0.0 * b;
        double simG = 0.700 * r + 0.300 * g + 0.0 * b;
        double simB = 0.0 * r + 0.300 * g + 0.700 * b;
        return (Math.Clamp(simR, 0, 1), Math.Clamp(simG, 0, 1), Math.Clamp(simB, 0, 1));
    }

    private static (int R, int G, int B) HexToRgb(string hex)
    {
        var h = hex.TrimStart('#');
        return (
            Convert.ToInt32(h[..2], 16),
            Convert.ToInt32(h[2..4], 16),
            Convert.ToInt32(h[4..], 16));
    }

    // ── 5. Visual Best Practices score ──────────────────────────────────────

    /// <summary>
    /// Scores five VBP sub-criteria at 20 pts each: pie avoidance, trend/comparison presence,
    /// slicer presence, palette size, and data binding completeness.
    /// Also populates <paramref name="recs"/> with actionable [Low]/[Medium] recommendations.
    /// </summary>
    private (double score, List<FrameworkFeedbackItem> feedback) ComputeVisualBestPracticesScore(
        List<PageData> pages, List<string> themeColors, List<string> recs)
    {
        var feedback   = new List<FrameworkFeedbackItem>();
        var allVisuals = pages.SelectMany(p => p.Visuals).ToList();
        var pieUsage = AnalyzePieUsage(pages);
        var comparisonGapIssues = CollectSemanticPageIssues(pages, AnalyzeMissingComparisonVisualIssue);
        var executiveVarianceIssues = CollectSemanticPageIssues(pages, AnalyzeExecutiveVarianceContextIssue);
        var redundantLabelIssues = CollectSemanticIssues(pages, AnalyzeRedundantLabelIssues);
        var metricLabelIssue = AnalyzeMetricLabelConsistency(pages);
        var pageStyleLanguageIssue = AnalyzePageStyleLanguageConsistency(pages);
        var layoutConventionIssue = AnalyzeLayoutConventionConsistency(pages);
        var semanticRelevantPages = pages
            .Select(AnalyzeNarrativePage)
            .Where(analysis => analysis.VisibleDataVisuals.Count > 0)
            .Where(analysis => analysis.KpiCards.Count >= 2 || HasComparisonIntent(analysis) || IsOverviewPage(analysis.Page, analysis.VisibleTitle))
            .ToList();

        // Sub 1: Pie/donut avoidance (20 pts) — severity scales by count and overview-page usage.
        double sub1 = pieUsage.PieCount switch
        {
            0 => 20.0,
            1 when pieUsage.OverviewPieCount == 0 => 12.0,
            1 => 6.0,
            2 when pieUsage.OverviewPieCount == 0 => 4.0,
            _ => 0.0,
        };
        feedback.Add(ScoredFeedback(
            pieUsage.PieCount == 0,
            pieUsage.PieCount == 0
                ? "Pie avoidance: No pie/donut charts — bar/column charts make comparisons easier."
                : pieUsage.OverviewPieCount > 0
                    ? $"Pie avoidance: {pieUsage.PieCount} pie/donut chart(s) detected, including overview-page use — exact comparison is usually more important than part-to-whole emphasis on landing pages."
                    : $"Pie avoidance: {pieUsage.PieCount} pie/donut chart(s) detected — replace with bar or column charts when exact comparison matters.",
            sub1,
            20.0,
            FindingTypes.StrongHeuristic,
            pieUsage.PieCount == 0 ? null : BuildAffectedVisuals(pieUsage.PieVisuals)));

        // Sub 2: Analytical fit (20 pts) — KPI/overview pages should include fitting comparison or variance context.
        var semanticIssuePages = comparisonGapIssues
            .Concat(executiveVarianceIssues)
            .Select(issue => issue.Page.DisplayName)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        bool hasTrendOrComparison = allVisuals.Any(v => !v.IsHidden && (v.IsTrend || IsComparisonOptimizedVisual(v)));
        double sub2;
        string analyticalFitMessage;
        List<AffectedVisualReference>? analyticalFitVisuals = null;
        if (semanticRelevantPages.Count == 0)
        {
            sub2 = hasTrendOrComparison ? 20.0 : 0.0;
            analyticalFitMessage = hasTrendOrComparison
                ? "Analytical fit: The report includes trend or comparison visuals that support common decision tasks."
                : "Analytical fit: Add at least one line or bar/column chart to support trend or comparison analysis.";
        }
        else
        {
            int supportedPages = Math.Max(0, semanticRelevantPages.Count - semanticIssuePages.Count);
            sub2 = 20.0 * supportedPages / semanticRelevantPages.Count;
            if (semanticIssuePages.Count == 0)
            {
                analyticalFitMessage = "Analytical fit: KPI-heavy and overview pages include fitting comparison or variance context.";
            }
            else if (comparisonGapIssues.Count > 0)
            {
                var issue = comparisonGapIssues
                    .OrderByDescending(item => item.Visuals.Count)
                    .ThenByDescending(item => item.Penalty)
                    .First();
                analyticalFitMessage = $"Analytical fit: {issue.Message}.";
                analyticalFitVisuals = BuildAffectedVisuals(issue.Page.DisplayName, issue.Visuals);
            }
            else
            {
                var issue = executiveVarianceIssues
                    .OrderByDescending(item => item.Visuals.Count)
                    .ThenByDescending(item => item.Penalty)
                    .First();
                analyticalFitMessage = $"Analytical fit: {issue.Message}.";
                analyticalFitVisuals = BuildAffectedVisuals(issue.Page.DisplayName, issue.Visuals);
            }
        }
        feedback.Add(ScoredFeedback(
            semanticIssuePages.Count == 0 && (semanticRelevantPages.Count == 0 ? hasTrendOrComparison : true),
            analyticalFitMessage,
            sub2,
            20.0,
            FindingTypes.StrongHeuristic,
            analyticalFitVisuals));

        // Sub 3: Slicer presence (20 pts)
        bool hasSlicer = allVisuals.Any(v => v.IsSlicer);
        double sub3   = hasSlicer ? 20.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasSlicer,
            hasSlicer
                ? "Slicer: At least one slicer provides interactive filtering — good for exploration."
                : "Slicer: Add a slicer to allow filtering by dimension (date, region, product, etc.).",
            sub3,
            20.0,
            FindingTypes.StrongHeuristic));

        // Sub 4: Theme palette size (20 pts) — same formula as legacy ThemeScore, scaled
        int c = themeColors.Count;
        double sub4 = c > 5 ? Math.Max(0.0, 20.0 * Math.Max(0, 100 - Math.Log2(c / 5.0) * 12) / 100.0) : 20.0;
        if (c > 5)
        {
            recs.Add($"[Low] Theme: {c} data colours defined (recommended ≤5). " +
                     "Reduce color variance in the report theme to cluster similar colours.");
            feedback.Add(ScoredFeedback(
                false,
                $"Colour palette: {c} data colours defined (recommended ≤5) — reduce for visual consistency.",
                sub4,
                20.0,
                FindingTypes.StrongHeuristic));
        }
        else
        {
            feedback.Add(ScoredFeedback(
                true,
                $"Colour palette: {(c == 0 ? "Default" : c.ToString())} data colour(s) — palette is concise.",
                sub4,
                20.0,
                FindingTypes.StrongHeuristic));
        }

        // Sub 5: Data binding completeness (20 pts) — % of non-decorative visuals with non-empty Type
        var nonDecorative = allVisuals.Where(v => !v.IsDecorative).ToList();
        double sub5 = nonDecorative.Count == 0 ? 0.0
            : (double)nonDecorative.Count(v => !string.IsNullOrWhiteSpace(v.Type)) / nonDecorative.Count * 20.0;
        int unbound = nonDecorative.Count(v => string.IsNullOrWhiteSpace(v.Type));
        feedback.Add(ScoredFeedback(
            unbound == 0,
            unbound == 0
                ? "Data bindings: All data visuals have a chart type assigned."
                : $"Data bindings: {unbound} visual(s) have no type assigned — verify all visuals are properly configured.",
            sub5,
            20.0,
            FindingTypes.Objective));

        if (pieUsage.PieCount > 0)
        {
            recs.Add(
                pieUsage.OverviewPieCount > 0
                    ? "[Medium] Visual Best Practices: Replace overview-page pie/donut charts with bar or column charts so the landing page supports exact comparison."
                    : "[Low] Visual Best Practices: Replace pie/donut charts with bar or column charts when users need exact comparisons.");
        }

        if (comparisonGapIssues.Count > 0)
        {
            var issue = comparisonGapIssues
                .OrderByDescending(item => item.Visuals.Count)
                .ThenByDescending(item => item.Penalty)
                .First();
            recs.Add(
                $"[Medium] Visual Best Practices: Add a clustered bar or column comparison visual to {FormatSemanticPageLabel(issue.Page)} so the KPI layer has categorical evidence.");
        }

        if (executiveVarianceIssues.Count > 0)
        {
            var issue = executiveVarianceIssues
                .OrderByDescending(item => item.Visuals.Count)
                .ThenByDescending(item => item.Penalty)
                .First();
            recs.Add(
                $"[Medium] Visual Best Practices: Add target, variance, prior-period, or trend context to {FormatSemanticPageLabel(issue.Page)} so the KPI summary can be interpreted quickly.");
            feedback.Add(FeedbackItem(
                false,
                $"Executive variance context: {issue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(issue.Page.DisplayName, issue.Visuals)));
        }

        if (redundantLabelIssues.Count > 0)
        {
            var issue = redundantLabelIssues
                .OrderByDescending(item => item.Penalty)
                .ThenByDescending(item => item.Visuals.Count)
                .First();
            recs.Add(
                $"[Low] Visual Best Practices: Remove either axis labels or direct data labels from {FormatSemanticVisualLabel(issue.Page, issue.Visuals.First())} if both are not needed.");
            feedback.Add(FeedbackItem(
                false,
                $"Redundant labeling: {issue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(issue.Page.DisplayName, issue.Visuals)));
        }

        if (metricLabelIssue is { } labelIssue)
        {
            recs.Add(
                "[Low] Visual Best Practices: Standardize KPI and card label naming. Keep modifier placement consistent and replace auto-generated labels such as `Sum of ...`.");
            feedback.Add(FeedbackItem(
                false,
                $"Metric label consistency: {labelIssue.Message}.",
                FindingTypes.StylePreference,
                BuildAffectedVisuals(labelIssue.Visuals)));
        }

        if (pageStyleLanguageIssue is { } styleIssue)
        {
            recs.Add(
                "[Low] Visual Best Practices: Keep rounded corners, shadows, and filled surfaces consistent across repeated page templates unless the style shift signals a deliberate mode change.");
            feedback.Add(FeedbackItem(
                false,
                $"Page style language: {styleIssue.Message}.",
                FindingTypes.StylePreference,
                BuildAffectedVisuals(styleIssue.Visuals)));
        }

        if (layoutConventionIssue is { } conventionIssue)
        {
            recs.Add(
                "[Low] Visual Best Practices: Keep title alignment and filter-band conventions stable across repeated report pages unless the layout change serves a clear user purpose.");
            feedback.Add(FeedbackItem(
                false,
                $"Layout convention: {conventionIssue.Message}.",
                FindingTypes.StylePreference,
                BuildAffectedVisuals(conventionIssue.Visuals)));
        }

        AddSurfaceTreatmentFeedback(feedback, pages);

        return (Clamp(sub1 + sub2 + sub3 + sub4 + sub5), feedback);
    }

    // ── 6. Enterprise Governance score ──────────────────────────────────────

    /// <summary>
    /// Scores report compliance against the governance rules supplied in the Design Analyzer config.
    /// Current rules cover max visuals per page, pie chart policy, and page title requirement.
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeGovernanceScore(
        List<PageData> pages,
        JsonElement? config)
    {
        var feedback = new List<FrameworkFeedbackItem>();
        var rules = ExtractGovernanceRules(config);
        var allVisuals = pages.SelectMany(p => p.Visuals).Where(v => !v.IsHidden).ToList();

        if (pages.Count == 0)
        {
            feedback.Add(FeedbackItem(false, "No pages found to evaluate against governance rules.", FindingTypes.Objective));
            return (0.0, feedback);
        }

        int compliantPages = pages.Count(page => page.Visuals.Count(v => !v.IsHidden && !v.IsDecorative) <= rules.MaxVisualsPerPage);
        double pageComplianceRatio = (double)compliantPages / pages.Count;
        double sub1 = pageComplianceRatio * 40.0;
        feedback.Add(ScoredFeedback(
            compliantPages == pages.Count,
            compliantPages == pages.Count
                ? $"Visual limit: All {pages.Count} page(s) are within the configured maximum of {rules.MaxVisualsPerPage} data visuals."
                : $"Visual limit: {pages.Count - compliantPages} page(s) exceed the configured maximum of {rules.MaxVisualsPerPage} data visuals.",
            sub1,
            40.0,
            FindingTypes.Objective));

        bool pieCompliant = rules.AllowPieCharts || !allVisuals.Any(v => v.IsPieDonut);
        double sub2 = pieCompliant ? 30.0 : 0.0;
        feedback.Add(ScoredFeedback(
            pieCompliant,
            pieCompliant
                ? rules.AllowPieCharts
                    ? "Pie chart policy: Pie and donut charts are allowed by the current governance configuration."
                    : "Pie chart policy: No pie or donut charts detected, matching the configured governance rule."
                : "Pie chart policy: Pie or donut charts are present, but the current governance configuration disallows them.",
            sub2,
            30.0,
            FindingTypes.Objective));

        var titledPages = pages
            .Select(page => new
            {
                Page = page,
                StrictVisibleTitle = GetStrictVisibleTitleText(page),
            })
            .ToList();
        var missingVisibleTitlePages = titledPages
            .Where(entry => string.IsNullOrWhiteSpace(entry.StrictVisibleTitle))
            .Select(entry => entry.Page.DisplayName)
            .ToList();
        bool titleCompliant = !rules.RequirePageTitle || missingVisibleTitlePages.Count == 0;
        double sub3 = titleCompliant ? 30.0 : 0.0;
        var titleExamples = titledPages
            .Select(entry => entry.StrictVisibleTitle)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(title => $"'{title}'")
            .Distinct(StringComparer.Ordinal)
            .Take(2)
            .ToList();
        feedback.Add(ScoredFeedback(
            titleCompliant,
            titleCompliant
                ? titleExamples.Count > 0
                    ? $"Page title policy: All evaluated pages anchor a meaningful title in the top band of the canvas, satisfying the configured governance rule. Example titles: {string.Join(", ", titleExamples)}."
                    : "Page title policy: Visible title intent is not required by the current governance configuration."
                : $"Page title policy: {missingVisibleTitlePages.Count} page(s) lack a meaningful visible title in the top band ({string.Join(", ", missingVisibleTitlePages.Select(pageName => $"'{pageName}'"))}) — add a non-vague title near the top of the canvas.",
            sub3,
            30.0,
            FindingTypes.Objective));

        return (Clamp(sub1 + sub2 + sub3), feedback);
    }

    // ── 7. Stephen Few score ─────────────────────────────────────────────────

    /// <summary>
    /// Scores four Stephen Few sub-criteria: one-screen rule (30 pts), KPI prominence (25 pts),
    /// pie avoidance — strict (25 pts), and contextual slicer (20 pts).
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeStephenFewScore(List<PageData> pages)
    {
        var feedback   = new List<FrameworkFeedbackItem>();
        var allVisuals = pages.SelectMany(p => p.Visuals).ToList();

        if (pages.Count == 0)
        {
            feedback.Add(FeedbackItem(false, "No pages found to evaluate against Stephen Few's guidelines.", FindingTypes.Objective));
            return (0.0, feedback);
        }

        // Sub 1: One-screen rule (30 pts) — all pages ≤8 visuals (visible only)
        var exceeding   = pages.Where(p => p.Visuals.Count(v => !v.IsHidden) > 8).ToList();
        double oneScreen = exceeding.Count == 0 ? 100.0
            : (double)(pages.Count - exceeding.Count) / pages.Count * 100.0;
        double sub1 = oneScreen * 0.30;
        feedback.Add(ScoredFeedback(
            exceeding.Count == 0,
            exceeding.Count == 0
                ? "One-screen rule: All pages have ≤8 visuals — within Stephen Few's recommended density."
                : $"One-screen rule: {exceeding.Count} page(s) exceed 8 visuals — split dense pages to reduce cognitive load.",
            sub1,
            30.0,
            FindingTypes.StrongHeuristic));

        // Sub 2: KPI prominence (25 pts)
        bool hasKpi = allVisuals.Any(v => v.IsKpiCard);
        double sub2 = hasKpi ? 25.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasKpi,
            hasKpi
                ? "KPI prominence: KPI/card visual(s) present — key metric immediately visible."
                : "KPI prominence: Add a KPI or card visual to make the report's headline metric immediately visible.",
            sub2,
            25.0,
            FindingTypes.StrongHeuristic));

        // Sub 3: Pie avoidance — strict (25 pts); Few: any pie = failure
        bool noPie  = !allVisuals.Any(v => v.IsPieDonut);
        double sub3 = noPie ? 25.0 : 0.0;
        feedback.Add(ScoredFeedback(
            noPie,
            noPie
                ? "Pie avoidance: No pie/donut charts — aligns with Stephen Few's strict guidance."
                : "Pie avoidance: Stephen Few strongly recommends replacing pie/donut charts with bar charts for accurate magnitude comparison.",
            sub3,
            25.0,
            FindingTypes.StrongHeuristic));

        // Sub 4: Contextual slicer (20 pts)
        bool hasSlicer = allVisuals.Any(v => v.IsSlicer);
        double sub4   = hasSlicer ? 20.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasSlicer,
            hasSlicer
                ? "Contextual slicer: Slicer present — provides context-switching for the audience."
                : "Contextual slicer: Few recommends filtering controls — add a slicer to support audience exploration.",
            sub4,
            20.0,
            FindingTypes.StrongHeuristic));

        return (Clamp(sub1 + sub2 + sub3 + sub4), feedback);
    }

    // ── 7. Tufte Minimalism score ────────────────────────────────────────────

    /// <summary>
    /// Scores three Tufte Minimalism sub-criteria: data-ink fitness (40 pts),
    /// pie/chartjunk avoidance (30 pts), and decoration minimalism (30 pts).
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeTufteScore(List<PageData> pages)
    {
        var feedback   = new List<FrameworkFeedbackItem>();
        var allVisuals = pages.SelectMany(p => p.Visuals).Where(v => !v.IsHidden).ToList();

        if (pages.Count == 0 || allVisuals.Count == 0)
        {
            feedback.Add(FeedbackItem(false, "No visuals found to evaluate against Tufte's minimalism principles.", FindingTypes.Objective));
            return (0.0, feedback);
        }

        int totalVisible    = allVisuals.Count;
        int decorativeCount = allVisuals.Count(v => v.IsDecorative);
        int dataVisuals     = totalVisible - decorativeCount;

        // Sub 1: Data-ink fitness (40 pts) — reward high proportion of data-bearing visuals
        double dataInkRatio = (double)dataVisuals / totalVisible;
        double sub1 = dataInkRatio * 40.0;
        feedback.Add(ScoredFeedback(
            dataInkRatio >= 0.8,
            dataInkRatio >= 0.8
                ? $"Data-ink ratio: {dataVisuals}/{totalVisible} visuals carry data — strong data-ink discipline."
                : $"Data-ink ratio: {dataVisuals}/{totalVisible} visuals carry data; {decorativeCount} decorative visual(s) remain ({Math.Round(decorativeCount * 100.0 / totalVisible)}% of total) — remove images, shapes, and text boxes that don't carry data.",
            sub1,
            40.0,
            FindingTypes.StrongHeuristic));

        // Sub 2: Pie/donut avoidance (30 pts) — Tufte considers radial slices poor for comparison
        bool noPie = !allVisuals.Any(v => v.IsPieDonut);
        double sub2 = noPie ? 30.0 : 0.0;
        feedback.Add(ScoredFeedback(
            noPie,
            noPie
                ? "Pie avoidance: No pie/donut charts — aligns with Tufte's guidance on precise quantitative comparison."
                : "Pie avoidance: Tufte recommends replacing pie/donut charts with bar or dot plots for more accurate visual comparison.",
            sub2,
            30.0,
            FindingTypes.StrongHeuristic));

        // Sub 3: Decoration minimalism (30 pts) — penalise > 2 decorative visuals per page
        double avgDecorativePerPage = (double)decorativeCount / pages.Count;
        bool minimalDecoration = avgDecorativePerPage <= 2.0;
        double sub3 = minimalDecoration
            ? 30.0
            : Math.Max(0.0, 30.0 - (avgDecorativePerPage - 2.0) * 5.0);
        feedback.Add(ScoredFeedback(
            minimalDecoration,
            minimalDecoration
                ? "Decoration minimalism: Minimal use of non-data elements — ink is maximally devoted to data."
                : $"Decoration minimalism: {decorativeCount} decorative element(s) across {pages.Count} page(s) ({avgDecorativePerPage:F1} per page) — every visual element should carry quantitative information.",
            sub3,
            30.0,
            FindingTypes.StrongHeuristic));

        return (Clamp(sub1 + sub2 + sub3), feedback);
    }


    // ── 8. Graphical Perception score ──────────────────────────────────────────

    /// <summary>
    /// Scores three Graphical Perception sub-criteria based on Cleveland &amp; McGill (1984):
    /// perceptually accurate encodings (40 pts), radial/angle avoidance (35 pts),
    /// and comparison-optimised structure (25 pts).
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeGraphicalPerceptionScore(List<PageData> pages)
    {
        var feedback    = new List<FrameworkFeedbackItem>();
        var allVisuals  = pages.SelectMany(p => p.Visuals).Where(v => !v.IsHidden).ToList();
        var dataVisuals = allVisuals.Where(v => !v.IsDecorative).ToList();
        var lineSequenceIssues = CollectSemanticIssues(pages, AnalyzeLineChartSequenceIssues);
        var funnelSemanticsIssues = CollectSemanticIssues(pages, AnalyzeWeakFunnelMeaningIssues);
        var comparisonGapIssues = CollectSemanticPageIssues(pages, AnalyzeMissingComparisonVisualIssue);
        var pieUsage = AnalyzePieUsage(pages);

        if (dataVisuals.Count == 0)
        {
            feedback.Add(FeedbackItem(false, "No data visuals found to evaluate against graphical perception principles.", FindingTypes.Objective));
            return (0.0, feedback);
        }

        // Sub 1: Perceptually accurate encodings (40 pts)
        // Cleveland & McGill hierarchy: position > length > angle > area > color
        // Bar/column/line/scatter/waterfall use position or length — most accurate
        int positional = dataVisuals.Count(v => v.IsComparison || v.IsTrend ||
            v.Type is "scatterChart" or "barChart" or "columnChart" or "waterfallChart" or "funnel");
        double positionalRatio = (double)positional / dataVisuals.Count;
        double encodingPenalty = Math.Min(20.0,
            lineSequenceIssues.Sum(issue => issue.Penalty) +
            funnelSemanticsIssues.Sum(issue => issue.Penalty));
        double sub1 = Math.Max(0.0, positionalRatio * 40.0 - encodingPenalty);
        var perceptualIssues = lineSequenceIssues.Concat(funnelSemanticsIssues).ToList();
        feedback.Add(ScoredFeedback(
            positionalRatio >= 0.7 && perceptualIssues.Count == 0,
            positionalRatio >= 0.7 && perceptualIssues.Count == 0
                ? $"Perceptual accuracy: {positional}/{dataVisuals.Count} visuals use position/length encodings — high perceptual accuracy (Cleveland & McGill)."
                : perceptualIssues.Count > 0
                    ? $"Perceptual accuracy: {positional}/{dataVisuals.Count} visuals use position/length encodings, but some charts still use an encoding that does not match the task implied by their titles or field roles."
                    : $"Perceptual accuracy: {positional}/{dataVisuals.Count} visuals use position/length encodings — replace angle/area charts with bar or line charts for more accurate data perception.",
            sub1,
            40.0,
            FindingTypes.StrongHeuristic,
            perceptualIssues.Count == 0 ? null : BuildAffectedVisuals(perceptualIssues.SelectMany(issue => issue.Visuals.Select(visual => (issue.Page.DisplayName, visual))))));

        // Sub 2: Pie/donut avoidance (35 pts)
        // Radial angle encodings rank lowest in the Cleveland & McGill perceptual hierarchy
        bool noPie  = pieUsage.PieCount == 0;
        double sub2 = noPie
            ? 35.0
            : pieUsage.OverviewPieCount > 0 || pieUsage.PieCount >= 2
                ? 0.0
                : 18.0;
        feedback.Add(ScoredFeedback(
            noPie,
            noPie
                ? "Radial avoidance: No pie/donut charts — using bar/column charts provides more accurate quantitative comparisons."
                : pieUsage.OverviewPieCount > 0
                    ? "Radial avoidance: Pie/donut charts appear on an overview page, where aligned bars or columns usually support more accurate first-pass comparison."
                    : "Radial avoidance: Pie/donut charts detected — replace with bar charts for more accurate quantitative judgment per the perceptual ranking.",
            sub2,
            35.0,
            FindingTypes.StrongHeuristic,
            noPie ? null : BuildAffectedVisuals(pieUsage.PieVisuals)));

        // Sub 3: Comparison-optimised structure (25 pts)
        // Clustered bars/columns allow direct side-by-side comparison on KPI-heavy or comparison-led pages.
        var comparisonRelevantPages = pages
            .Select(AnalyzeNarrativePage)
            .Where(analysis => analysis.VisibleDataVisuals.Count > 0)
            .Where(analysis => analysis.KpiCards.Count >= 2 || HasComparisonIntent(analysis))
            .ToList();
        double sub3;
        bool hasComparison;
        string comparisonMessage;
        List<AffectedVisualReference>? comparisonAffectedVisuals = null;
        if (comparisonRelevantPages.Count == 0)
        {
            hasComparison = dataVisuals.Any(IsComparisonOptimizedVisual);
            sub3 = hasComparison ? 25.0 : 0.0;
            comparisonMessage = hasComparison
                ? "Comparative structure: Clustered bar/column charts present — enables direct side-by-side comparison."
                : "Comparative structure: Add a clustered bar or column chart to support direct comparative judgments between categories.";
        }
        else
        {
            int supportedPages = comparisonRelevantPages.Count(analysis =>
                analysis.VisibleDataVisuals.Any(IsComparisonOptimizedVisual));
            sub3 = 25.0 * supportedPages / comparisonRelevantPages.Count;
            hasComparison = supportedPages == comparisonRelevantPages.Count;
            if (hasComparison)
            {
                comparisonMessage = "Comparative structure: Pages that ask for comparison include a strong bar/column comparison view.";
            }
            else
            {
                var issue = comparisonGapIssues
                    .OrderByDescending(item => item.Visuals.Count)
                    .ThenByDescending(item => item.Penalty)
                    .First();
                comparisonMessage = $"Comparative structure: {issue.Message}.";
                comparisonAffectedVisuals = BuildAffectedVisuals(issue.Page.DisplayName, issue.Visuals);
            }
        }
        feedback.Add(ScoredFeedback(
            hasComparison,
            comparisonMessage,
            sub3,
            25.0,
            FindingTypes.StrongHeuristic,
            comparisonAffectedVisuals));

        if (lineSequenceIssues.Count > 0)
        {
            var issue = lineSequenceIssues
                .OrderByDescending(item => item.Penalty)
                .ThenByDescending(item => item.Visuals.Count)
                .First();
            feedback.Add(FeedbackItem(
                false,
                $"Sequential fit: {issue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(issue.Page.DisplayName, issue.Visuals)));
        }

        if (funnelSemanticsIssues.Count > 0)
        {
            var issue = funnelSemanticsIssues
                .OrderByDescending(item => item.Penalty)
                .ThenByDescending(item => item.Visuals.Count)
                .First();
            feedback.Add(FeedbackItem(
                false,
                $"Funnel semantics: {issue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(issue.Page.DisplayName, issue.Visuals)));
        }

        return (Clamp(sub1 + sub2 + sub3), feedback);
    }

    // ── 9. Dashboard Density score ──────────────────────────────────────────────

    /// <summary>
    /// Scores three Dashboard Density sub-criteria and supplements them with
    /// page-composition checks for long-page risk, whitespace balance, and overview/detail separation.
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeDashboardDensityScore(
        List<PageData> pages,
        List<string> recs,
        NavigationScoringSettings navigationScoring)
    {
        var feedback = new List<FrameworkFeedbackItem>();

        if (pages.Count == 0)
        {
            feedback.Add(FeedbackItem(false, "No pages found to evaluate dashboard density.", FindingTypes.Objective));
            return (0.0, feedback);
        }

        var allVisuals = pages.SelectMany(p => p.Visuals).Where(v => !v.IsHidden).ToList();
        var compositions = pages.Select(page => BuildVisualComposition(page.Visuals, navigationScoring)).ToList();
        var longPageIssues = CollectLayoutIssues(pages, AnalyzeLongPageRisk);
        var overviewDetailIssues = CollectLayoutIssues(pages, AnalyzeOverviewDetailSeparation);
        var deadZoneIssues = CollectLayoutIssues(pages, AnalyzeDeadZoneBalance);
        PageLayoutIssue? worstLongPageIssue = longPageIssues.Count > 0
            ? longPageIssues
                .OrderByDescending(issue => issue.Penalty)
                .ThenByDescending(issue => issue.Visuals.Count)
                .First()
            : null;
        PageLayoutIssue? worstOverviewDetailIssue = overviewDetailIssues.Count > 0
            ? overviewDetailIssues
                .OrderByDescending(issue => issue.Penalty)
                .ThenByDescending(issue => issue.Visuals.Count)
                .First()
            : null;
        PageLayoutIssue? worstDeadZoneIssue = deadZoneIssues.Count > 0
            ? deadZoneIssues
                .OrderByDescending(issue => issue.Penalty)
                .ThenByDescending(issue => issue.Visuals.Count)
                .First()
            : null;

        // Sub 1: Optimal visual count per page (40 pts) — 3–8 weighted visuals per page is ideal
        double avgVisuals = compositions.Sum(composition => composition.WeightedVisibleCount) / pages.Count;
        double sub1;
        string densityMsg;
        if (avgVisuals < 1.0)
        {
            sub1 = 0.0;
            densityMsg = $"Visual density: {avgVisuals:F1} weighted visuals per page on average — too sparse; add data visuals to increase information density.";
        }
        else if (avgVisuals < 3.0)
        {
            sub1 = 20.0;
            densityMsg = $"Visual density: {avgVisuals:F1} weighted visuals per page on average — below ideal range (3–8); consider adding related metrics.";
        }
        else if (avgVisuals <= 8.0)
        {
            sub1 = 40.0;
            densityMsg = $"Visual density: {avgVisuals:F1} weighted visuals per page on average — optimal range (3–8) balances information richness and cognitive load.";
        }
        else if (avgVisuals <= 12.0)
        {
            sub1 = 20.0;
            densityMsg = $"Visual density: {avgVisuals:F1} weighted visuals per page on average — above ideal range; consider splitting dense pages to reduce cognitive overload.";
        }
        else
        {
            sub1 = 0.0;
            densityMsg = $"Visual density: {avgVisuals:F1} weighted visuals per page on average — too crowded; split into multiple pages to maintain scanability.";
        }
        if (worstLongPageIssue is { } longPageIssue)
        {
            sub1 = Math.Min(sub1, 20.0);
            densityMsg += $" {longPageIssue.Page.DisplayName} also extends beyond a standard one-screen scan.";
        }
        bool optimalDensity = avgVisuals is >= 3.0 and <= 8.0 && worstLongPageIssue is null;
        feedback.Add(ScoredFeedback(
            optimalDensity,
            densityMsg,
            sub1,
            40.0,
            FindingTypes.StrongHeuristic));

        // Sub 2: Content diversity (30 pts) — ≥3 distinct visual types indicates rich information architecture
        int distinctTypes = navigationScoring.Enabled
            ? allVisuals
                .Where(v => !v.IsNavigationElement)
                .Select(v => v.Type)
                .Distinct()
                .Count() + (allVisuals.Any(v => v.IsNavigationElement) ? 1 : 0)
            : allVisuals.Select(v => v.Type).Distinct().Count();
        bool goodDiversity = distinctTypes >= 3;
        double sub2 = distinctTypes >= 3 ? 30.0 : distinctTypes == 2 ? 15.0 : 0.0;
        feedback.Add(ScoredFeedback(
            goodDiversity,
            goodDiversity
                ? $"Content diversity: {distinctTypes} distinct visual types — varied chart types support different perceptual tasks."
                : $"Content diversity: Only {distinctTypes} distinct visual type(s) — add complementary chart types (e.g. KPI card, line chart, table) to support multiple analytical tasks.",
            sub2,
            30.0,
            FindingTypes.StrongHeuristic));

        // Sub 3: Navigation support (30 pts) — slicers help users navigate dense content
        bool hasSlicers = allVisuals.Any(v => v.IsSlicer);
        double sub3 = hasSlicers ? 30.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasSlicers,
            hasSlicers
                ? "Navigation support: Slicers present — interactive filters help users navigate and explore the dashboard efficiently."
                : "Navigation support: No slicers found — add at least one slicer to enable interactive filtering and improve dashboard navigation.",
            sub3,
            30.0,
            FindingTypes.StrongHeuristic));

        if (navigationScoring.Enabled)
        {
            var navigationVisualCount = compositions.Sum(composition => composition.NavigationVisualCount);
            feedback.Add(FeedbackItem(
                true,
                $"Navigation treatment: {navigationVisualCount} navigation visual(s) counted at {navigationScoring.WeightPercent:F0}% weight in dashboard density scoring.",
                FindingTypes.Objective));
        }

        if (worstLongPageIssue is { } longPageFinding)
        {
            recs.Add(
                $"[Medium] Density: '{longPageFinding.Page.DisplayName}' reads like a long page. Move the lower visual cluster to a separate page or shorten the canvas so the main story fits one screen.");
            feedback.Add(FeedbackItem(
                false,
                $"Long-page risk: {longPageFinding.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(longPageFinding.Page.DisplayName, longPageFinding.Visuals)));
        }

        if (worstOverviewDetailIssue is { } overviewDetailIssue)
        {
            recs.Add(
                $"[Medium] Density: Add more vertical separation between the KPI overview band and detail visuals on '{overviewDetailIssue.Page.DisplayName}' so the page reads in layers.");
            feedback.Add(FeedbackItem(
                false,
                $"Overview/detail separation: {overviewDetailIssue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(overviewDetailIssue.Page.DisplayName, overviewDetailIssue.Visuals)));
        }

        if (worstDeadZoneIssue is { } deadZoneIssue)
        {
            feedback.Add(FeedbackItem(
                false,
                $"Whitespace balance: {deadZoneIssue.Message}.",
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(deadZoneIssue.Page.DisplayName, deadZoneIssue.Visuals)));
        }

        var score = sub1 + sub2 + sub3;
        if (worstOverviewDetailIssue is not null)
        {
            score -= 10.0;
        }

        if (worstDeadZoneIssue is not null)
        {
            score -= 5.0;
        }

        return (Clamp(score), feedback);
    }

    // ── 10. Narrative Design score ──────────────────────────────────────────────

    /// <summary>
    /// Scores five narrative sub-criteria at 20 points each:
    /// visible page purpose, headline outcome clarity, KPI comparison context,
    /// supporting evidence flow, and overview-to-detail readability.
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeNarrativeDesignScore(
        List<PageData> pages,
        List<string> recs)
    {
        var feedback = new List<FrameworkFeedbackItem>();
        var analyses = pages
            .Select(AnalyzeNarrativePage)
            .Where(analysis => analysis.VisibleDataVisuals.Count > 0)
            .ToList();

        if (analyses.Count == 0)
        {
            feedback.Add(FeedbackItem(false, "No data visuals found to evaluate against narrative design principles.", FindingTypes.Objective));
            return (0.0, feedback);
        }

        // Sub 1: visible page purpose
        int purposePages = analyses.Count(analysis => analysis.HasMeaningfulVisibleTitle);
        double sub1 = 20.0 * purposePages / analyses.Count;
        if (purposePages == analyses.Count)
        {
            var examples = analyses
                .Select(analysis => analysis.VisibleTitle)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Distinct(StringComparer.Ordinal)
                .Take(2)
                .Select(title => $"'{title}'")
                .ToList();
            feedback.Add(ScoredFeedback(
                true,
                $"Visible page purpose: Every evaluated page exposes a clear visible title or question anchor{(examples.Count > 0 ? $" ({string.Join(", ", examples)})" : string.Empty)}.",
                sub1,
                20.0,
                FindingTypes.StrongHeuristic));
        }
        else
        {
            var missingPurpose = analyses.Where(analysis => !analysis.HasMeaningfulVisibleTitle).ToList();
            var vagueTitles = missingPurpose.Where(analysis => analysis.HasAnyVisibleTitle).Select(analysis => $"'{analysis.Page.DisplayName}'").ToList();
            var noTitles = missingPurpose.Where(analysis => !analysis.HasAnyVisibleTitle).Select(analysis => $"'{analysis.Page.DisplayName}'").ToList();
            var purposeMessage = BuildNarrativePurposeMessage(vagueTitles, noTitles);
            feedback.Add(ScoredFeedback(
                false,
                purposeMessage,
                sub1,
                20.0,
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(BuildNarrativeEvidenceVisuals(missingPurpose))));
            recs.Add(
                $"[Medium] Narrative: {BuildNarrativeRecommendationSubject(missingPurpose.First())} needs a visible title or question anchor so users understand the page purpose in the first scan.");
        }

        // Sub 2: headline outcome clarity
        int clearHeadlinePages = analyses.Count(analysis => analysis.HasHeadlineOutcome);
        double sub2 = 20.0 * clearHeadlinePages / analyses.Count;
        if (clearHeadlinePages == analyses.Count)
        {
            feedback.Add(ScoredFeedback(
                true,
                "Headline outcome clarity: Each evaluated page signals what the user should focus on first, either through a KPI layer or a clearly framed lead visual.",
                sub2,
                20.0,
                FindingTypes.StrongHeuristic));
        }
        else
        {
            var unclearPages = analyses.Where(analysis => !analysis.HasHeadlineOutcome).ToList();
            var mostUnclear = unclearPages
                .OrderByDescending(analysis => analysis.VisibleDataVisuals.Count)
                .First();
            feedback.Add(ScoredFeedback(
                false,
                $"Headline outcome clarity: {string.Join(", ", unclearPages.Select(analysis => $"'{analysis.Page.DisplayName}'"))} contain data but no obvious primary outcome — decide what the user should answer in the first 5–10 seconds.",
                sub2,
                20.0,
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(BuildNarrativeEvidenceVisuals(unclearPages))));
            recs.Add(
                $"[Medium] Narrative: {BuildNarrativeRecommendationSubject(mostUnclear)} needs a clearer primary outcome before the detail visuals.");
        }

        // Sub 3: KPI comparison context
        var kpiPages = analyses.Where(analysis => analysis.KpiCards.Count > 0).ToList();
        double sub3;
        if (kpiPages.Count == 0)
        {
            sub3 = 20.0;
            feedback.Add(ScoredFeedback(
                true,
                "KPI comparison context: No KPI-heavy pages detected, so comparison context is not missing from a KPI layer.",
                sub3,
                20.0,
                FindingTypes.StrongHeuristic));
        }
        else
        {
            int contextRichPages = kpiPages.Count(analysis => analysis.HasKpiComparisonContext);
            sub3 = 20.0 * contextRichPages / kpiPages.Count;
            if (contextRichPages == kpiPages.Count)
            {
                feedback.Add(ScoredFeedback(
                    true,
                    "KPI comparison context: KPI pages include at least one target, trend, variance, or supporting comparison so the numbers can be interpreted.",
                    sub3,
                    20.0,
                    FindingTypes.StrongHeuristic));
            }
            else
            {
                var missingContextPages = kpiPages.Where(analysis => !analysis.HasKpiComparisonContext).ToList();
                var focusPage = missingContextPages
                    .OrderByDescending(analysis => analysis.KpiCards.Count)
                    .First();
                var kpiLabel = GetPrimaryKpiLabel(focusPage);
                feedback.Add(ScoredFeedback(
                    false,
                    $"{kpiLabel} has no target, variance, prior-period, or trend context. Add one comparison so the value can be interpreted.",
                    sub3,
                    20.0,
                    FindingTypes.StrongHeuristic,
                    BuildAffectedVisuals(focusPage.Page.DisplayName, focusPage.KpiCards)));
                recs.Add(
                    $"[Medium] Narrative: {BuildNarrativeRecommendationSubject(focusPage)} KPI cards need a target, variance, or prior-period comparison.");
            }
        }

        // Sub 4: supporting evidence flow
        int evidencePages = analyses.Count(analysis => analysis.HasSupportingEvidenceFlow);
        double sub4 = 20.0 * evidencePages / analyses.Count;
        if (evidencePages == analyses.Count)
        {
            feedback.Add(ScoredFeedback(
                true,
                "Supporting evidence flow: Pages pair the lead outcome with at least one supporting detail visual or are already focused on a single explanatory view.",
                sub4,
                20.0,
                FindingTypes.StrongHeuristic));
        }
        else
        {
            var unsupportedPages = analyses.Where(analysis => !analysis.HasSupportingEvidenceFlow).ToList();
            var focusPage = unsupportedPages
                .OrderByDescending(analysis => analysis.KpiCards.Count)
                .First();
            feedback.Add(ScoredFeedback(
                false,
                $"{string.Join(", ", unsupportedPages.Select(analysis => $"'{analysis.Page.DisplayName}'"))} lead with KPI cards but do not include supporting evidence visuals. Add a chart or table that explains why the KPI moved.",
                sub4,
                20.0,
                FindingTypes.StrongHeuristic,
                BuildAffectedVisuals(focusPage.Page.DisplayName, focusPage.KpiCards)));
            recs.Add(
                $"[Medium] Narrative: {BuildNarrativeRecommendationSubject(focusPage)} needs at least one supporting chart or table behind the KPI layer.");
        }

        // Sub 5: overview-to-detail readability
        bool overviewFlowOk;
        double sub5;
        string overviewMessage;
        List<AffectedVisualReference>? overviewAffectedVisuals = null;
        if (analyses.Count == 1)
        {
            var single = analyses[0];
            overviewFlowOk = single.HasMeaningfulVisibleTitle && single.HasHeadlineOutcome &&
                (single.HasSupportingEvidenceFlow || single.VisibleDataVisuals.Count <= 2);
            sub5 = overviewFlowOk ? 20.0 : single.HasMeaningfulVisibleTitle ? 10.0 : 0.0;
            overviewMessage = overviewFlowOk
                ? "Overview-to-detail readability: The page tells users what matters first and provides immediate supporting evidence."
                : $"Overview-to-detail readability: '{single.Page.DisplayName}' contains data but no fast overview path before the detail — anchor the page with a clearer lead message and supporting context.";
            if (!overviewFlowOk)
            {
                overviewAffectedVisuals = BuildAffectedVisuals(BuildNarrativeEvidenceVisuals([single]));
                recs.Add(
                    $"[Medium] Narrative: {BuildNarrativeRecommendationSubject(single)} should guide the first scan before users read the detail visuals.");
            }
        }
        else
        {
            var firstPage = analyses[0];
            var laterDetailPages = analyses.Skip(1).Any(analysis => analysis.VisibleDataVisuals.Count > 0);
            overviewFlowOk = firstPage.HasMeaningfulVisibleTitle && firstPage.HasHeadlineOutcome && laterDetailPages;
            sub5 = overviewFlowOk ? 20.0 : firstPage.HasMeaningfulVisibleTitle && laterDetailPages ? 10.0 : 0.0;
            overviewMessage = overviewFlowOk
                ? $"Overview-to-detail readability: '{firstPage.Page.DisplayName}' provides a clear overview before the report moves into deeper detail."
                : $"Overview-to-detail readability: The report lacks a strong overview-first path. Use '{firstPage.Page.DisplayName}' as a decision-led landing page before deeper breakdowns.";
            if (!overviewFlowOk)
            {
                overviewAffectedVisuals = BuildAffectedVisuals(BuildNarrativeEvidenceVisuals([firstPage]));
                recs.Add(
                    $"[Medium] Narrative: Strengthen '{firstPage.Page.DisplayName}' as the overview page before the report branches into detail.");
            }
        }

        feedback.Add(ScoredFeedback(
            overviewFlowOk,
            overviewMessage,
            sub5,
            20.0,
            FindingTypes.StrongHeuristic,
            overviewAffectedVisuals));

        return (Clamp(sub1 + sub2 + sub3 + sub4 + sub5), feedback);
    }

    // ── Data loaders ─────────────────────────────────────────────────────────

    private List<PageData> LoadAllPages(PbirReportLocation location)
    {
        var pagesRoot = Path.Combine(location.DefinitionPath, "pages");
        if (!Directory.Exists(pagesRoot)) return [];

        var pages = new List<PageData>();
        foreach (var pageId in GetOrderedPageIds(pagesRoot))
        {
            if (string.IsNullOrWhiteSpace(pageId))
            {
                continue;
            }

            var pageDir = Path.Combine(pagesRoot, pageId);
            if (!Directory.Exists(pageDir))
            {
                _logger.LogDebug("[Scoring] Skipping page id without folder: {PageId}", pageId);
                continue;
            }

            var pageJsonPath = Path.Combine(pageDir, "page.json");
            if (!File.Exists(pageJsonPath)) continue;

            try
            {
                var pageJson = ReadJsonObject(pageJsonPath);
                var displayName = pageJson["displayName"]?.GetValue<string>()
                    ?? Path.GetFileName(pageDir);
                
                // Try to parse visuals from page.json first (new format)
                var visuals = ParseVisuals(pageJson);
                
                // If no visuals found in page.json, scan visuals/ subdirectory (Power BI Desktop format)
                if (visuals.Count == 0)
                {
                    visuals = ParseVisualsFromDirectory(pageDir);
                }
                
                pages.Add(new PageData
                {
                    DisplayName = displayName,
                    Visuals = visuals,
                    Canvas = ParseCanvasMetadata(pageJson),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Scoring] Could not read page {Dir}", pageDir);
            }
        }

        return pages;
    }

    private List<string> GetOrderedPageIds(string pagesRoot)
    {
        var pagesMetadataPath = Path.Combine(pagesRoot, "pages.json");
        if (File.Exists(pagesMetadataPath))
        {
            try
            {
                var pagesMetadata = ReadJsonObject(pagesMetadataPath);
                if (pagesMetadata["pageOrder"] is JsonArray pageOrder)
                {
                    var orderedPageIds = pageOrder
                        .Select(node => node?.GetValue<string>())
                        .Where(pageId => !string.IsNullOrWhiteSpace(pageId))
                        .Select(pageId => pageId!)
                        .ToList();

                    if (orderedPageIds.Count > 0)
                    {
                        return orderedPageIds;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Scoring] Failed to parse page order metadata: {Path}", pagesMetadataPath);
            }
        }

        return Directory.GetDirectories(pagesRoot)
            .Select(Path.GetFileName)
            .Where(pageId => !string.IsNullOrWhiteSpace(pageId))
            .Select(pageId => pageId!)
            .ToList();
    }

    private List<VisualData> ParseVisuals(JsonObject pageJson)
    {
        var visuals = new List<VisualData>();

        if (pageJson["visuals"] is not JsonArray arr) return visuals;

        foreach (var item in arr)
        {
            if (item is not JsonObject vo) continue;
            var visualId = vo["id"]?.GetValue<string>() ?? string.Empty;
            bool isHidden = ReadBooleanNode(vo["isHidden"]) ?? false;
            visuals.Add(CreateVisualData(
                visualJson: vo,
                visualId: visualId,
                visualType: vo["type"]?.GetValue<string>() ?? string.Empty,
                x: TryDouble(vo, "x"),
                y: TryDouble(vo, "y"),
                w: TryDouble(vo, "width"),
                h: TryDouble(vo, "height"),
                isHidden: isHidden,
                sourceContext: $"page visual '{visualId}'"));
        }

        return visuals;
    }
    
    /// <summary>
    /// Parses visuals from the visuals/ subdirectory within a page folder.
    /// This handles Power BI Desktop-authored reports where each visual is stored as visual.json.
    /// </summary>
    private List<VisualData> ParseVisualsFromDirectory(string pageDir)
    {
        var visuals = new List<VisualData>();
        var visualsDir = Path.Combine(pageDir, "visuals");
        
        if (!Directory.Exists(visualsDir)) return visuals;
        
        foreach (var visualDir in Directory.GetDirectories(visualsDir))
        {
            var visualJsonPath = Path.Combine(visualDir, "visual.json");
            if (!File.Exists(visualJsonPath)) continue;
            
            try
            {
                var visualJson = ReadJsonObject(visualJsonPath);
                
                // Extract visual name (folder name or from visual.json)
                var visualName = visualJson["name"]?.GetValue<string>() 
                    ?? Path.GetFileName(visualDir);
                
                // Extract position information
                var position = visualJson["position"] as JsonObject;
                double x = 0, y = 0, w = 0, h = 0;
                
                if (position is not null)
                {
                    x = position["x"]?.GetValue<double>() ?? 0;
                    y = position["y"]?.GetValue<double>() ?? 0;
                    w = position["width"]?.GetValue<double>() ?? 0;
                    h = position["height"]?.GetValue<double>() ?? 0;
                }
                
                // Extract visual type
                var visual = visualJson["visual"] as JsonObject;
                var visualType = visual?["visualType"]?.GetValue<string>() ?? "unknown";
                
                // Extract hidden state
                bool isHidden = visualJson["isHidden"]?.GetValue<bool>() ?? false;
                
                visuals.Add(CreateVisualData(
                    visualJson: visualJson,
                    visualId: visualName,
                    visualType: visualType,
                    x: x,
                    y: y,
                    w: w,
                    h: h,
                    isHidden: isHidden,
                    sourceContext: visualJsonPath));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Scoring] Could not parse visual definition at {Path}", visualJsonPath);
            }
        }
        
        return visuals;
    }

    private static CanvasMetadata? ParseCanvasMetadata(JsonObject pageJson)
    {
        var width = ReadDoubleNode(pageJson["width"])
            ?? ReadNestedDouble(pageJson, "canvas", "width")
            ?? ReadNestedDouble(pageJson, "pageSize", "width")
            ?? ReadNestedDouble(pageJson, "size", "width");
        var height = ReadDoubleNode(pageJson["height"])
            ?? ReadNestedDouble(pageJson, "canvas", "height")
            ?? ReadNestedDouble(pageJson, "pageSize", "height")
            ?? ReadNestedDouble(pageJson, "size", "height");

        if (width is > 0 && height is > 0)
        {
            return new CanvasMetadata(width.Value, height.Value);
        }

        return null;
    }

    private VisualData CreateVisualData(
        JsonObject visualJson,
        string visualId,
        string visualType,
        double x,
        double y,
        double w,
        double h,
        bool isHidden,
        string sourceContext)
    {
        return new VisualData
        {
            Id = visualId,
            Type = visualType,
            X = x,
            Y = y,
            W = w,
            H = h,
            IsHidden = isHidden,
            Text = ParseVisualTextMetadata(visualJson, visualId, visualType, sourceContext),
            Labels = ParseVisualLabelMetadata(visualJson, visualId, sourceContext),
            FieldRoles = ParseVisualFieldRoleMetadata(visualJson, visualId, sourceContext),
            Formatting = ParseVisualFormattingMetadata(visualJson, visualId, sourceContext),
        };
    }

    private VisualTextMetadata ParseVisualTextMetadata(
        JsonObject visualJson,
        string visualId,
        string visualType,
        string sourceContext)
    {
        return TryParseVisualComponent(
            visualId,
            "text",
            sourceContext,
            () =>
            {
                var titleText = FirstNonBlank(
                    ExtractVisibleObjectText(visualJson, ["title", "visualTitle", "header"]),
                    ExtractVisibleScalarText(visualJson, ["titleText", "visualTitleText"]));
                var subtitleText = FirstNonBlank(
                    ExtractVisibleObjectText(visualJson, ["subtitle", "subTitle"]),
                    ExtractVisibleScalarText(visualJson, ["subtitleText", "subTitleText"]));
                string? textBoxText = null;
                if (IsTextVisualType(visualType))
                {
                    textBoxText = FirstNonBlank(
                        ExtractVisibleObjectText(visualJson, ["textbox", "textBox", "body"]),
                        ExtractVisibleScalarText(visualJson, ["textBoxText", "bodyText", "text"]),
                        ExtractTextRunContent(visualJson));
                }

                return new VisualTextMetadata(
                    VisibleTitleText: titleText,
                    VisibleSubtitleText: subtitleText,
                    TextBoxText: textBoxText);
            },
            VisualTextMetadata.Empty);
    }

    private VisualLabelMetadata ParseVisualLabelMetadata(
        JsonObject visualJson,
        string visualId,
        string sourceContext)
    {
        return TryParseVisualComponent(
            visualId,
            "labels",
            sourceContext,
            () =>
            {
                var hasLegend = ExtractPresenceFlag(visualJson, ["legend"], ["hasLegend", "showLegend"]);
                var hasAxisLabels = ExtractPresenceFlag(
                    visualJson,
                    ["axis", "xAxis", "yAxis", "categoryAxis", "valueAxis"],
                    ["hasAxisLabels", "showAxisLabels"]);
                var hasDataLabels = ExtractPresenceFlag(
                    visualJson,
                    ["dataLabels", "labels"],
                    ["hasDataLabels", "showDataLabels"]);

                return new VisualLabelMetadata(
                    HasLegend: hasLegend,
                    HasAxisLabels: hasAxisLabels,
                    HasDataLabels: hasDataLabels);
            },
            VisualLabelMetadata.Empty);
    }

    private VisualFieldRoleMetadata ParseVisualFieldRoleMetadata(
        JsonObject visualJson,
        string visualId,
        string sourceContext)
    {
        return TryParseVisualComponent(
            visualId,
            "field roles",
            sourceContext,
            () =>
            {
                var categoryHints = CollectRoleHints(visualJson, ["fieldRoles", "roles", "projections"], ["category", "categories"]);
                var valueHints = CollectRoleHints(visualJson, ["fieldRoles", "roles", "projections"], ["value", "values"]);
                var seriesHints = CollectRoleHints(visualJson, ["fieldRoles", "roles", "projections"], ["series", "legend"]);
                var measureHints = CollectRoleHints(visualJson, ["fieldRoles", "roles", "projections"], ["measure", "measures", "value", "values"]);

                return new VisualFieldRoleMetadata(
                    CategoryHints: categoryHints,
                    ValueHints: valueHints,
                    SeriesHints: seriesHints,
                    MeasureHints: measureHints);
            },
            VisualFieldRoleMetadata.Empty);
    }

    private VisualFormattingMetadata ParseVisualFormattingMetadata(
        JsonObject visualJson,
        string visualId,
        string sourceContext)
    {
        return TryParseVisualComponent(
            visualId,
            "formatting",
            sourceContext,
            () =>
            {
                var backgroundFillColor = FirstNonBlank(
                    ExtractColorFromObjects(visualJson, ["background", "backgroundFill"]),
                    ExtractColorFromObjects(visualJson, ["fill"]));
                var fontColor = FirstNonBlank(
                    ExtractColorFromObjects(visualJson, ["font", "foreground", "fontColor"]),
                    ExtractColorFromObjects(visualJson, ["labelColor", "textColor", "foregroundColor"]));
                var hasBorder = ExtractPresenceFlag(visualJson, ["border", "outline"], ["showBorder", "hasBorder"]);
                var cornerRadius = ExtractNumericSetting(visualJson, ["corners"], ["radius"])
                    ?? ExtractScalarNumber(visualJson, ["cornerRadius"]);
                var hasShadow = ExtractPresenceFlag(visualJson, ["shadow", "dropShadow", "elevation"], ["showShadow", "hasShadow"]);

                return new VisualFormattingMetadata(
                    BackgroundFillColor: backgroundFillColor,
                    FontColor: fontColor,
                    HasBorder: hasBorder,
                    CornerRadius: cornerRadius,
                    HasShadow: hasShadow);
            },
            VisualFormattingMetadata.Empty);
    }

    /// <summary>
    /// Resolves the theme data colours array from <c>report.json</c>.
    /// Handles both built-in themes (empty colour list) and local <c>href</c> theme files.
    /// </summary>
    private List<string> ResolveThemeColors(
        JsonObject reportJson,
        PbirReportLocation location,
        List<string> recs)
    {
        var themeNode = reportJson["theme"];
        if (themeNode is null) return [];

        // Local theme file reference?
        var href = themeNode["href"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(href) && !href.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var themeFilePath = Path.GetFullPath(
                Path.Combine(location.WorkspaceRootPath, href.TrimStart('/', '\\')));

            if (File.Exists(themeFilePath))
            {
                return ParseThemeFile(themeFilePath);
            }
        }

        // Built-in theme — name only, no colour data available for scoring.
        return [];
    }

    private List<string> ParseThemeFile(string filePath)
    {
        try
        {
            var json = ReadJsonObject(filePath);

            // Power BI theme JSON: { "dataColors": ["#hex", …] }
            if (json["dataColors"] is JsonArray arr)
            {
                return arr
                    .Select(n => n?.GetValue<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s) && s!.StartsWith('#'))
                    .Select(s => s!)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Scoring] Could not parse theme file: {Path}", filePath);
        }

        return [];
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static JsonObject ReadJsonObject(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException($"File is not a JSON object: {filePath}");
    }

    private static double TryDouble(JsonObject obj, string key)
    {
        if (obj[key] is JsonNode node)
        {
            try { return node.GetValue<double>(); } catch { /* fall through */ }
        }
        return 0.0;
    }

    private T TryParseVisualComponent<T>(
        string visualId,
        string componentName,
        string sourceContext,
        Func<T> parser,
        T fallback)
    {
        try
        {
            return parser();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "[Scoring] Could not parse {Component} metadata for visual '{VisualId}' from {Source}",
                componentName,
                visualId,
                sourceContext);
            return fallback;
        }
    }

    private static VisualComposition BuildVisualComposition(
        IEnumerable<VisualData> visuals,
        NavigationScoringSettings navigationScoring)
    {
        int dataVisualCount = 0;
        int navigationVisualCount = 0;
        int hiddenVisualCount = 0;
        double weightedVisibleCount = 0;

        foreach (var visual in visuals)
        {
            if (visual.IsHidden)
            {
                hiddenVisualCount++;
                continue;
            }

            if (visual.IsNavigationElement)
            {
                navigationVisualCount++;
                weightedVisibleCount += navigationScoring.Enabled
                    ? navigationScoring.WeightMultiplier
                    : 1.0;
                continue;
            }

            if (!visual.IsDecorative)
            {
                dataVisualCount++;
            }

            weightedVisibleCount += 1.0;
        }

        return new VisualComposition(
            DataVisualCount: dataVisualCount,
            NavigationVisualCount: navigationVisualCount,
            HiddenVisualCount: hiddenVisualCount,
            WeightedVisibleCount: weightedVisibleCount);
    }

    private static List<PageLayoutIssue> CollectLayoutIssues(
        IEnumerable<PageData> pages,
        Func<PageData, PageLayoutIssue?> analyzer) =>
        pages
            .Select(analyzer)
            .Where(issue => issue.HasValue)
            .Select(issue => issue!.Value)
            .ToList();

    private static double GetCanvasWidth(PageData page) =>
        page.Canvas?.Width is > 0 ? page.Canvas.Value.Width : CanvasWidth;

    private static double GetCanvasHeight(PageData page) =>
        page.Canvas?.Height is > 0 ? page.Canvas.Value.Height : CanvasHeight;

    private static double GetColumnWidth(PageData page)
    {
        return GetCanvasWidth(page) / GridCols;
    }

    private static double GetRowHeight(PageData page)
    {
        return GetCanvasHeight(page) / GridRows;
    }

    private static string? GetPageVisibleTitle(PageData page) =>
        page.Visuals
            .Where(visual => !visual.IsHidden && visual.HasVisibleTitleIntent)
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .Select(visual => visual.BestVisibleText)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

    /// <summary>
    /// Fraction of the canvas height that defines the "top band" used by strict visible-title detection.
    /// Visible-title candidates whose Y coordinate falls above this threshold (numerically smaller Y) are
    /// considered to be in a position consistent with a page title anchor.
    /// </summary>
    private const double TopBandTitleFraction = 0.15;

    /// <summary>
    /// Returns the first visible page-title text that satisfies the strict rule used by the
    /// <c>requirePageTitle</c> governance check and the page-title sub-criterion of the
    /// Enterprise Governance framework:
    /// <list type="bullet">
    /// <item>the visual is not hidden and has visible title intent;</item>
    /// <item>the visual sits in the top <c>TopBandTitleFraction</c> of the canvas;</item>
    /// <item>the text is non-empty and is not a vague placeholder such as <c>"Page 1"</c> or <c>"Overview"</c>.</item>
    /// </list>
    /// Returns <c>null</c> when the page has no visible title that meets all three rules.
    /// </summary>
    private static string? GetStrictVisibleTitleText(PageData page)
    {
        double canvasHeight = GetCanvasHeight(page);
        double topBandLimit = canvasHeight * TopBandTitleFraction;

        return page.Visuals
            .Where(visual => !visual.IsHidden && visual.HasVisibleTitleIntent)
            .Where(visual => visual.Y <= topBandLimit)
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .Select(visual => visual.BestVisibleText)
            .Where(text => !string.IsNullOrWhiteSpace(text) && !IsVagueNarrativeText(text!))
            .FirstOrDefault();
    }

    /// <summary>
    /// Returns <c>true</c> when the page has a visible title that satisfies the strict rule.
    /// See <see cref="GetStrictVisibleTitleText"/> for the exact criteria.
    /// </summary>
    private static bool HasStrictVisibleTitle(PageData page) =>
        !string.IsNullOrWhiteSpace(GetStrictVisibleTitleText(page));

    private static PageVisualMetadataSummary BuildPageVisualMetadataSummary(PageData page)
    {
        var orderedVisuals = page.Visuals
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ThenBy(visual => visual.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var visibleVisuals = orderedVisuals.Where(visual => !visual.IsHidden).ToList();

        return new PageVisualMetadataSummary
        {
            PageName = page.DisplayName,
            VisiblePageTitle = GetPageVisibleTitle(page),
            StrictVisiblePageTitle = GetStrictVisibleTitleText(page),
            CanvasWidth = page.Canvas?.Width,
            CanvasHeight = page.Canvas?.Height,
            VisualCount = orderedVisuals.Count,
            VisibleTitleVisualCount = visibleVisuals.Count(visual => visual.HasVisibleTitleIntent),
            TextVisualCount = visibleVisuals.Count(visual => IsTextVisualType(visual.Type)),
            SlicerCount = visibleVisuals.Count(visual => visual.IsSlicer),
            LegendVisualCount = visibleVisuals.Count(visual => visual.Labels.HasLegend == true),
            AxisLabelVisualCount = visibleVisuals.Count(visual => visual.Labels.HasAxisLabels == true),
            DataLabelVisualCount = visibleVisuals.Count(visual => visual.Labels.HasDataLabels == true),
            FormattedVisualCount = visibleVisuals.Count(HasAnyFormattingMetadata),
            Visuals = orderedVisuals.Select(BuildVisualMetadataItem).ToList(),
        };
    }

    private static VisualMetadataItem BuildVisualMetadataItem(VisualData visual) => new()
    {
        VisualId = visual.Id,
        VisualType = visual.Type,
        X = visual.X,
        Y = visual.Y,
        Width = visual.W,
        Height = visual.H,
        IsHidden = visual.IsHidden,
        IsNavigationElement = visual.IsNavigationElement,
        IsDecorative = visual.IsDecorative,
        IsSlicer = visual.IsSlicer,
        VisibleTitleText = visual.VisibleTitleText,
        VisibleSubtitleText = visual.VisibleSubtitleText,
        TextBoxText = visual.TextBoxText,
        BestVisibleText = visual.BestVisibleText,
        HasVisibleTitleIntent = visual.HasVisibleTitleIntent,
        HasLegend = visual.Labels.HasLegend,
        HasAxisLabels = visual.Labels.HasAxisLabels,
        HasDataLabels = visual.Labels.HasDataLabels,
        CategoryHints = visual.FieldRoles.CategoryHints.ToList(),
        ValueHints = visual.FieldRoles.ValueHints.ToList(),
        SeriesHints = visual.FieldRoles.SeriesHints.ToList(),
        MeasureHints = visual.FieldRoles.MeasureHints.ToList(),
        BackgroundFillColor = visual.Formatting.BackgroundFillColor,
        FontColor = visual.Formatting.FontColor,
        HasBorder = visual.Formatting.HasBorder,
        CornerRadius = visual.Formatting.CornerRadius,
        HasShadow = visual.Formatting.HasShadow,
    };

    private static bool HasAnyFormattingMetadata(VisualData visual) =>
        !string.IsNullOrWhiteSpace(visual.Formatting.BackgroundFillColor) ||
        !string.IsNullOrWhiteSpace(visual.Formatting.FontColor) ||
        visual.Formatting.HasBorder.HasValue ||
        visual.Formatting.CornerRadius.HasValue ||
        visual.Formatting.HasShadow.HasValue;

    private static void AddSurfaceTreatmentFeedback(List<FrameworkFeedbackItem> feedback, List<PageData> pages)
    {
        var formattedVisuals = pages
            .SelectMany(page => page.Visuals, (page, visual) => (PageName: page.DisplayName, Visual: visual))
            .Where(entry => !entry.Visual.IsHidden && !entry.Visual.IsNavigationElement && !entry.Visual.IsDecorative)
            .ToList();

        if (formattedVisuals.Count < 2)
        {
            return;
        }

        var issues = new List<string>();

        var borderVisuals = formattedVisuals.Where(entry => entry.Visual.Formatting.HasBorder.HasValue).ToList();
        if (borderVisuals.Count >= 2)
        {
            int withBorder = borderVisuals.Count(entry => entry.Visual.Formatting.HasBorder == true);
            int withoutBorder = borderVisuals.Count(entry => entry.Visual.Formatting.HasBorder == false);
            if (withBorder > 0 && withoutBorder > 0)
            {
                issues.Add($"{withBorder} visual(s) show borders while {withoutBorder} do not");
            }
        }

        var shadowVisuals = formattedVisuals.Where(entry => entry.Visual.Formatting.HasShadow.HasValue).ToList();
        if (shadowVisuals.Count >= 2)
        {
            int withShadow = shadowVisuals.Count(entry => entry.Visual.Formatting.HasShadow == true);
            int withoutShadow = shadowVisuals.Count(entry => entry.Visual.Formatting.HasShadow == false);
            if (withShadow > 0 && withoutShadow > 0)
            {
                issues.Add($"{withShadow} visual(s) use elevation while {withoutShadow} stay flat");
            }
        }

        var cornerVisuals = formattedVisuals
            .Where(entry => entry.Visual.Formatting.CornerRadius is not null)
            .ToList();
        if (cornerVisuals.Count >= 2)
        {
            var radii = cornerVisuals
                .Select(entry => Math.Round(entry.Visual.Formatting.CornerRadius!.Value, 1))
                .Distinct()
                .OrderBy(radius => radius)
                .ToList();
            if (radii.Count > 1)
            {
                issues.Add($"corner radii vary ({string.Join(", ", radii.Select(radius => $"{radius:0.#}"))} px)");
            }
        }

        var fillVisuals = formattedVisuals
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Visual.Formatting.BackgroundFillColor))
            .ToList();
        if (fillVisuals.Count >= 2)
        {
            var fillColors = fillVisuals
                .Select(entry => entry.Visual.Formatting.BackgroundFillColor!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(color => color, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (fillColors.Count > 1)
            {
                issues.Add($"background fills vary ({string.Join(", ", fillColors.Take(3))}{(fillColors.Count > 3 ? $" +{fillColors.Count - 3} more" : string.Empty)})");
            }
        }

        if (issues.Count == 0)
        {
            return;
        }

        feedback.Add(FeedbackItem(
            false,
            $"Surface treatment: Formatting metadata shows inconsistent peer styling — {string.Join("; ", issues)}.",
            FindingTypes.StylePreference,
            BuildAffectedVisuals(formattedVisuals)));
    }

    private static PageLayoutIssue? AnalyzeTopBandKpiConsistency(PageData page)
    {
        var canvasHeight = GetCanvasHeight(page);
        var rowHeight = GetRowHeight(page);
        var colWidth = GetColumnWidth(page);
        var topBandCards = page.Visuals
            .Where(visual => !visual.IsHidden && !visual.IsNavigationElement && visual.IsKpiCard && visual.Y <= canvasHeight * 0.28)
            .OrderBy(visual => visual.X)
            .ToList();

        if (topBandCards.Count < 2)
        {
            return null;
        }

        double verticalSpread = topBandCards.Max(visual => visual.Y) - topBandCards.Min(visual => visual.Y);
        double heightSpread = topBandCards.Max(visual => visual.H) - topBandCards.Min(visual => visual.H);
        var gaps = GetHorizontalGaps(topBandCards);
        bool overlap = gaps.Any(gap => gap < -4.0);
        double gapRange = gaps.Count > 1 ? gaps.Max() - gaps.Min() : 0.0;
        var concerns = new List<string>();

        if (verticalSpread > Math.Max(12.0, rowHeight * 0.35))
        {
            concerns.Add($"vary by {verticalSpread:0.#} px vertically");
        }

        if (heightSpread > Math.Max(16.0, rowHeight * 0.4))
        {
            concerns.Add($"use {heightSpread:0.#} px different heights");
        }

        if (overlap)
        {
            concerns.Add("overlap instead of holding a clean band");
        }
        else if (gaps.Count > 1 && gapRange > Math.Max(24.0, colWidth * 0.75))
        {
            concerns.Add($"use uneven gaps ({gaps.Min():0.#}-{gaps.Max():0.#} px)");
        }

        if (concerns.Count == 0)
        {
            return null;
        }

        return new PageLayoutIssue(
            page,
            $"'{page.DisplayName}' top-row KPI cards {string.Join(" and ", concerns)}",
            topBandCards,
            Penalty: 5.0);
    }

    private static PageLayoutIssue? AnalyzeSpacingRhythm(PageData page)
    {
        var visibleDataVisuals = page.Visuals
            .Where(visual => !visual.IsHidden && !visual.IsNavigationElement && !visual.IsDecorative)
            .ToList();
        if (visibleDataVisuals.Count < 3)
        {
            return null;
        }

        var rows = BuildVisualRows(visibleDataVisuals, Math.Max(24.0, GetRowHeight(page) * 0.45));
        PageLayoutIssue? worstIssue = null;
        double worstGapRange = 0.0;

        foreach (var row in rows.Where(row => row.Visuals.Count >= 3))
        {
            var gaps = GetHorizontalGaps(row.Visuals);
            if (gaps.Count < 2)
            {
                continue;
            }

            bool overlap = gaps.Any(gap => gap < -4.0);
            double gapRange = gaps.Max() - gaps.Min();
            double averageGap = gaps.Average();
            if (!overlap && gapRange <= Math.Max(30.0, Math.Abs(averageGap) * 0.9 + 12.0))
            {
                continue;
            }

            var message = overlap
                ? $"'{page.DisplayName}' has peer visuals in the same row that overlap or collide"
                : $"'{page.DisplayName}' has peer visuals in the same row with uneven spacing ({gaps.Min():0.#}-{gaps.Max():0.#} px)";
            if (worstIssue is null || gapRange > worstGapRange || overlap)
            {
                worstIssue = new PageLayoutIssue(
                    page,
                    message,
                    row.Visuals,
                    Penalty: 5.0);
                worstGapRange = overlap ? double.MaxValue : gapRange;
            }
        }

        return worstIssue;
    }

    private static PageLayoutIssue? AnalyzeFilterPlacement(PageData page)
    {
        var filters = page.Visuals
            .Where(visual => !visual.IsHidden && visual.IsSlicer)
            .ToList();
        if (filters.Count == 0)
        {
            return null;
        }

        double canvasWidth = GetCanvasWidth(page);
        double canvasHeight = GetCanvasHeight(page);
        var unusualFilters = filters
            .Where(filter => !IsPreferredFilterZone(filter, canvasWidth, canvasHeight))
            .ToList();
        if (unusualFilters.Count == 0)
        {
            return null;
        }

        bool lowerRightFilters = unusualFilters.All(filter =>
            filter.X > canvasWidth * 0.5 &&
            filter.Y > canvasHeight * 0.3);
        var message = lowerRightFilters
            ? $"'{page.DisplayName}' places primary filters in the lower-right area of the page instead of a top or left control zone"
            : $"'{page.DisplayName}' places filters inside the main evidence area instead of a top or left control zone";
        return new PageLayoutIssue(page, message, unusualFilters, Penalty: 10.0);
    }

    private static PageLayoutIssue? AnalyzeFilterScatter(PageData page)
    {
        var filters = page.Visuals
            .Where(visual => !visual.IsHidden && visual.IsSlicer)
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ToList();
        if (filters.Count < 2)
        {
            return null;
        }

        double canvasWidth = GetCanvasWidth(page);
        double canvasHeight = GetCanvasHeight(page);
        var zones = filters
            .Select(filter => ClassifyFilterZone(filter, canvasWidth, canvasHeight))
            .ToList();
        var distinctZones = zones
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        double minX = filters.Min(filter => filter.X);
        double minY = filters.Min(filter => filter.Y);
        double maxRight = filters.Max(filter => filter.X + filter.W);
        double maxBottom = filters.Max(filter => filter.Y + filter.H);
        bool widelyDistributed = (maxRight - minX) > canvasWidth * 0.45 &&
            (maxBottom - minY) > canvasHeight * 0.28;
        bool touchesSecondaryZone = distinctZones.Any(zone =>
            zone is "center" or "right rail" or "bottom band");
        if (!touchesSecondaryZone && !widelyDistributed && distinctZones.Count < 3)
        {
            return null;
        }

        return new PageLayoutIssue(
            page,
            $"{FormatSemanticPageLabel(page)} distributes {filters.Count} slicer(s) across {distinctZones.Count} page zones ({string.Join(", ", distinctZones)}) instead of a single filter band",
            filters,
            Penalty: 8.0);
    }

    private static PageLayoutIssue? AnalyzeOverviewFilterDensity(PageData page)
    {
        if (!IsOverviewPage(page))
        {
            return null;
        }

        var filters = page.Visuals
            .Where(visual => !visual.IsHidden && visual.IsSlicer)
            .ToList();
        if (filters.Count == 0)
        {
            return null;
        }

        double canvasArea = GetCanvasWidth(page) * GetCanvasHeight(page);
        double filterAreaRatio = canvasArea <= 0
            ? 0
            : filters.Sum(filter => filter.W * filter.H) / canvasArea;
        bool denseOverviewControls = filters.Count >= 4 ||
            (filters.Count >= 3 && filterAreaRatio >= 0.08) ||
            filterAreaRatio >= 0.14;
        if (!denseOverviewControls)
        {
            return null;
        }

        return new PageLayoutIssue(
            page,
            $"{FormatSemanticPageLabel(page)} uses {filters.Count} slicer(s) across {filterAreaRatio * 100:0.#}% of the canvas, which is heavy for an overview page",
            filters,
            Penalty: 8.0);
    }

    private static PageLayoutIssue? AnalyzePrimaryScanPath(PageData page)
    {
        var visibleVisuals = page.Visuals.Where(visual => !visual.IsHidden).ToList();
        var visibleDataVisuals = visibleVisuals
            .Where(visual => !visual.IsNavigationElement && !visual.IsDecorative)
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ToList();
        if (visibleDataVisuals.Count == 0)
        {
            return null;
        }

        double canvasWidth = GetCanvasWidth(page);
        double canvasHeight = GetCanvasHeight(page);
        double rowHeight = GetRowHeight(page);
        var leadVisual = visibleDataVisuals[0];
        bool hasTitleAnchor = visibleVisuals.Any(visual => visual.HasVisibleTitleIntent && visual.Y <= canvasHeight * 0.18);
        bool hasKpiAnchor = visibleDataVisuals.Any(visual => visual.IsKpiCard && visual.Y <= canvasHeight * 0.25);
        bool delayedLeadVisual = leadVisual.Y > canvasHeight * 0.28 || leadVisual.X > canvasWidth * 0.22;
        var topLeftFilters = visibleVisuals
            .Where(visual =>
                visual.IsSlicer &&
                visual.X <= canvasWidth * 0.3 &&
                visual.Y <= canvasHeight * 0.28)
            .ToList();
        bool filterHeavyEntry = topLeftFilters.Count > 0 &&
            leadVisual.Y >= topLeftFilters.Max(filter => filter.Y + filter.H) + rowHeight * 0.4 &&
            leadVisual.X > canvasWidth * 0.18;

        if ((!hasTitleAnchor && !hasKpiAnchor && delayedLeadVisual) || filterHeavyEntry)
        {
            var affectedVisuals = DeduplicateVisuals([leadVisual, .. topLeftFilters]);
            var message = filterHeavyEntry
                ? $"'{page.DisplayName}' opens with filters before the first data visual, so the evidence starts late in the scan path"
                : $"'{page.DisplayName}' has no strong upper-left title or KPI anchor, and the first data visual begins well below the expected entry zone";
            return new PageLayoutIssue(page, message, affectedVisuals, Penalty: 10.0);
        }

        return null;
    }

    private static PageLayoutIssue? AnalyzeLongPageRisk(PageData page)
    {
        var visibleVisuals = page.Visuals
            .Where(visual => !visual.IsHidden && !visual.IsDecorative)
            .ToList();
        if (visibleVisuals.Count == 0)
        {
            return null;
        }

        double canvasHeight = GetCanvasHeight(page);
        double usedBottom = visibleVisuals.Max(visual => visual.Y + visual.H);
        bool tallCanvas = canvasHeight > CanvasHeight * 1.25;
        bool extendsBeyondStandardScan = usedBottom > CanvasHeight * 1.05;
        bool nearCanvasBottom = usedBottom > canvasHeight * 0.9;
        if (!extendsBeyondStandardScan || !(tallCanvas || nearCanvasBottom))
        {
            return null;
        }

        double rowHeight = GetRowHeight(page);
        var bottomBandVisuals = visibleVisuals
            .Where(visual => visual.Y + visual.H >= usedBottom - Math.Max(24.0, rowHeight))
            .ToList();
        return new PageLayoutIssue(
            page,
            $"'{page.DisplayName}' uses a {canvasHeight:0.#} px canvas and visible content reaches {usedBottom:0.#} px, which extends beyond a standard one-screen scan",
            bottomBandVisuals,
            Penalty: 15.0);
    }

    private static PageLayoutIssue? AnalyzeOverviewDetailSeparation(PageData page)
    {
        var visibleDataVisuals = page.Visuals
            .Where(visual => !visual.IsHidden && !visual.IsNavigationElement && !visual.IsDecorative)
            .ToList();
        if (visibleDataVisuals.Count < 2)
        {
            return null;
        }

        double canvasHeight = GetCanvasHeight(page);
        double rowHeight = GetRowHeight(page);
        var topBandKpis = visibleDataVisuals
            .Where(visual => visual.IsKpiCard && visual.Y <= canvasHeight * 0.3)
            .OrderBy(visual => visual.X)
            .ToList();
        var detailVisuals = visibleDataVisuals
            .Where(visual => !visual.IsKpiCard)
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ToList();
        if (topBandKpis.Count == 0 || detailVisuals.Count == 0)
        {
            return null;
        }

        double topBandBottom = topBandKpis.Max(visual => visual.Y + visual.H);
        double firstDetailTop = detailVisuals.Min(visual => visual.Y);
        double gap = firstDetailTop - topBandBottom;
        if (gap >= Math.Max(18.0, rowHeight * 0.4))
        {
            return null;
        }

        var firstDetailBand = detailVisuals
            .Where(visual => visual.Y <= firstDetailTop + Math.Max(16.0, rowHeight * 0.35))
            .ToList();
        return new PageLayoutIssue(
            page,
            $"'{page.DisplayName}' mixes KPI overview cards and supporting detail visuals with only {gap:0.#} px of vertical separation",
            DeduplicateVisuals(topBandKpis.Concat(firstDetailBand)),
            Penalty: 10.0);
    }

    private static PageLayoutIssue? AnalyzeDeadZoneBalance(PageData page)
    {
        var visibleDataVisuals = page.Visuals
            .Where(visual => !visual.IsHidden && !visual.IsNavigationElement && !visual.IsDecorative)
            .ToList();
        if (visibleDataVisuals.Count < 4)
        {
            return null;
        }

        double canvasHeight = GetCanvasHeight(page);
        var rows = BuildVisualRows(visibleDataVisuals, Math.Max(24.0, GetRowHeight(page) * 0.45));
        if (rows.Count < 2)
        {
            return null;
        }

        double largestGap = 0.0;
        VisualRow? upperRow = null;
        VisualRow? lowerRow = null;
        for (int i = 0; i < rows.Count - 1; i++)
        {
            double gap = rows[i + 1].Top - rows[i].Bottom;
            if (gap > largestGap)
            {
                largestGap = gap;
                upperRow = rows[i];
                lowerRow = rows[i + 1];
            }
        }

        if (largestGap < canvasHeight * 0.28 || upperRow is null || lowerRow is null)
        {
            return null;
        }

        return new PageLayoutIssue(
            page,
            $"'{page.DisplayName}' leaves a {largestGap:0.#} px mid-page gap between peer visual clusters, which can feel like an accidental dead zone",
            DeduplicateVisuals(upperRow.Value.Visuals.Concat(lowerRow.Value.Visuals)),
            Penalty: 5.0);
    }

    private static bool IsPreferredFilterZone(VisualData filter, double canvasWidth, double canvasHeight)
    {
        double right = filter.X + filter.W;
        return filter.X <= canvasWidth * 0.24 ||
            right <= canvasWidth * 0.32 ||
            filter.Y <= canvasHeight * 0.22;
    }

    private static string ClassifyFilterZone(VisualData filter, double canvasWidth, double canvasHeight)
    {
        double right = filter.X + filter.W;
        double bottom = filter.Y + filter.H;
        bool top = filter.Y <= canvasHeight * 0.22;
        bool left = filter.X <= canvasWidth * 0.24 || right <= canvasWidth * 0.32;
        if (top && left)
        {
            return "top-left";
        }

        if (top)
        {
            return "top band";
        }

        if (left)
        {
            return "left rail";
        }

        if (right >= canvasWidth * 0.72)
        {
            return "right rail";
        }

        if (bottom >= canvasHeight * 0.72)
        {
            return "bottom band";
        }

        return "center";
    }

    private static List<VisualRow> BuildVisualRows(IEnumerable<VisualData> visuals, double tolerance)
    {
        var rows = new List<List<VisualData>>();
        foreach (var visual in visuals.OrderBy(visual => visual.Y).ThenBy(visual => visual.X))
        {
            var centerY = visual.Y + visual.H / 2.0;
            var matchingRow = rows.FirstOrDefault(row =>
            {
                double rowTop = row.Min(item => item.Y);
                double rowBottom = row.Max(item => item.Y + item.H);
                double rowCenter = (rowTop + rowBottom) / 2.0;
                return Math.Abs(centerY - rowCenter) <= tolerance;
            });

            if (matchingRow is null)
            {
                rows.Add([visual]);
            }
            else
            {
                matchingRow.Add(visual);
            }
        }

        return rows
            .Select(row => new VisualRow(
                row.OrderBy(visual => visual.X).ToList(),
                Top: row.Min(visual => visual.Y),
                Bottom: row.Max(visual => visual.Y + visual.H)))
            .OrderBy(row => row.Top)
            .ToList();
    }

    private static List<double> GetHorizontalGaps(IReadOnlyList<VisualData> visuals)
    {
        var gaps = new List<double>();
        for (int i = 0; i < visuals.Count - 1; i++)
        {
            gaps.Add(visuals[i + 1].X - (visuals[i].X + visuals[i].W));
        }

        return gaps;
    }

    private static List<VisualData> DeduplicateVisuals(IEnumerable<VisualData> visuals) =>
        visuals
            .Where(visual => !string.IsNullOrWhiteSpace(visual.Id))
            .GroupBy(visual => visual.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

    private static List<SemanticChartIssue> CollectSemanticIssues(
        IEnumerable<PageData> pages,
        Func<PageData, IEnumerable<SemanticChartIssue>> analyzer) =>
        pages.SelectMany(analyzer).ToList();

    private static List<SemanticChartIssue> CollectSemanticPageIssues(
        IEnumerable<PageData> pages,
        Func<PageData, SemanticChartIssue?> analyzer) =>
        pages
            .Select(analyzer)
            .Where(issue => issue.HasValue)
            .Select(issue => issue!.Value)
            .ToList();

    private static PieUsageAnalysis AnalyzePieUsage(IEnumerable<PageData> pages)
    {
        var pieVisuals = pages
            .SelectMany(page => page.Visuals
                .Where(visual => !visual.IsHidden && visual.IsPieDonut)
                .Select(visual => (PageName: page.DisplayName, Visual: visual)))
            .ToList();
        int overviewPieCount = pages.Count(page =>
            IsOverviewPage(page) &&
            page.Visuals.Any(visual => !visual.IsHidden && visual.IsPieDonut));
        return new PieUsageAnalysis(
            PieCount: pieVisuals.Count,
            OverviewPieCount: overviewPieCount,
            PieVisuals: pieVisuals);
    }

    private static IEnumerable<SemanticChartIssue> AnalyzeLineChartSequenceIssues(PageData page)
    {
        foreach (var visual in page.Visuals.Where(visual => !visual.IsHidden && IsLineLikeVisual(visual)))
        {
            if (!HasExplicitCategoricalContext(visual) || HasSequentialContext(visual, page))
            {
                continue;
            }

            var hint = visual.FieldRoles.CategoryHints.FirstOrDefault(LooksCategoricalHint)
                ?? ExtractCategoricalCue(visual.BestVisibleText)
                ?? ExtractCategoricalCue(visual.VisibleTitleText)
                ?? "the visible category cues";
            yield return new SemanticChartIssue(
                page,
                $"{FormatSemanticVisualLabel(page, visual)} uses a line/area encoding, but '{hint}' looks categorical rather than sequential",
                [visual],
                Penalty: 8.0);
        }
    }

    private static IEnumerable<SemanticChartIssue> AnalyzeWeakFunnelMeaningIssues(PageData page)
    {
        foreach (var visual in page.Visuals.Where(visual => !visual.IsHidden && IsFunnelVisual(visual)))
        {
            if (HasFunnelContext(visual, page))
            {
                continue;
            }

            yield return new SemanticChartIssue(
                page,
                $"{FormatSemanticVisualLabel(page, visual)} uses a funnel encoding without stage, pipeline, or conversion context",
                [visual],
                Penalty: 6.0);
        }
    }

    private static IEnumerable<SemanticChartIssue> AnalyzeRedundantLabelIssues(PageData page)
    {
        foreach (var visual in page.Visuals.Where(visual =>
                     !visual.IsHidden &&
                     !visual.IsNavigationElement &&
                     !visual.IsDecorative &&
                     (visual.IsTrend || IsComparisonOptimizedVisual(visual)) &&
                     visual.Labels.HasDataLabels == true &&
                     visual.Labels.HasAxisLabels == true))
        {
            yield return new SemanticChartIssue(
                page,
                $"{FormatSemanticVisualLabel(page, visual)} shows both direct data labels and axis labels, which may be redundant for an already labeled chart",
                [visual],
                Penalty: 4.0);
        }
    }

    private static SemanticChartIssue? AnalyzeMissingComparisonVisualIssue(PageData page)
    {
        var analysis = AnalyzeNarrativePage(page);
        if (analysis.VisibleDataVisuals.Count == 0)
        {
            return null;
        }

        bool needsComparisonVisual = analysis.KpiCards.Count >= 2 || HasComparisonIntent(analysis);
        bool hasComparisonVisual = analysis.VisibleDataVisuals.Any(IsComparisonOptimizedVisual);
        if (!needsComparisonVisual || hasComparisonVisual)
        {
            return null;
        }

        var evidenceVisuals = analysis.KpiCards.Count > 0
            ? analysis.KpiCards
            : analysis.VisibleDataVisuals.Take(2).ToList();
        return new SemanticChartIssue(
            page,
            $"{FormatSemanticPageLabel(page)} asks users to compare results but does not include a strong bar/column comparison visual",
            evidenceVisuals,
            Penalty: 8.0);
    }

    private static SemanticChartIssue? AnalyzeExecutiveVarianceContextIssue(PageData page)
    {
        var analysis = AnalyzeNarrativePage(page);
        if (analysis.VisibleDataVisuals.Count == 0 ||
            !IsOverviewPage(page, analysis.VisibleTitle) ||
            analysis.KpiCards.Count < 2 ||
            analysis.HasKpiComparisonContext)
        {
            return null;
        }

        return new SemanticChartIssue(
            page,
            $"{FormatSemanticPageLabel(page)} reads like an executive overview but lacks target, variance, prior-period, or trend context for the KPI layer",
            analysis.KpiCards,
            Penalty: 8.0);
    }

    private static bool IsOverviewPage(PageData page, string? visibleTitle = null)
    {
        var texts = new[]
        {
            visibleTitle,
            GetPageVisibleTitle(page),
            page.DisplayName,
        };

        return texts.Any(text =>
            ContainsTextKeyword(text, "executive") ||
            ContainsTextKeyword(text, "overview") ||
            ContainsTextKeyword(text, "summary") ||
            ContainsTextKeyword(text, "dashboard") ||
            ContainsTextKeyword(text, "leadership") ||
            ContainsTextKeyword(text, "headline"));
    }

    private static bool HasComparisonIntent(NarrativePageAnalysis analysis) =>
        analysis.KpiCards.Count >= 2 ||
        ContainsComparisonIntentKeywords(analysis.VisibleTitle) ||
        ContainsComparisonIntentKeywords(analysis.Page.DisplayName) ||
        analysis.VisibleTexts.Any(ContainsComparisonIntentKeywords);

    private static bool IsComparisonOptimizedVisual(VisualData visual) =>
        visual.IsComparison ||
        visual.Type is "barChart" or "columnChart" or "waterfallChart";

    private static bool IsLineLikeVisual(VisualData visual) =>
        visual.Type is "lineChart" or "areaChart" or "lineAndStackedColumnChart" or "lineAndClusteredColumnChart";

    private static bool IsFunnelVisual(VisualData visual) =>
        visual.Type.Equals("funnel", StringComparison.OrdinalIgnoreCase) ||
        visual.Type.Equals("funnelChart", StringComparison.OrdinalIgnoreCase);

    private static bool HasSequentialContext(VisualData visual, PageData page)
    {
        var evidenceTexts = BuildSemanticEvidenceTexts(page, visual);
        return evidenceTexts.Any(ContainsSequentialKeywords);
    }

    private static bool HasExplicitCategoricalContext(VisualData visual)
    {
        if (visual.FieldRoles.CategoryHints.Any(LooksCategoricalHint))
        {
            return true;
        }

        return BuildVisualTextEvidence(visual).Any(text => ExtractCategoricalCue(text) is not null);
    }

    private static bool HasFunnelContext(VisualData visual, PageData page) =>
        BuildSemanticEvidenceTexts(page, visual).Any(ContainsFunnelContextKeywords);

    private static IEnumerable<string> BuildSemanticEvidenceTexts(PageData page, VisualData visual) =>
        BuildVisualTextEvidence(visual)
            .Concat(visual.FieldRoles.CategoryHints)
            .Concat(visual.FieldRoles.SeriesHints)
            .Concat(visual.FieldRoles.MeasureHints)
            .Append(GetPageVisibleTitle(page) ?? string.Empty)
            .Append(page.DisplayName)
            .Where(text => !string.IsNullOrWhiteSpace(text));

    private static IEnumerable<string> BuildVisualTextEvidence(VisualData visual) =>
        new[] { visual.BestVisibleText, visual.VisibleTitleText, visual.VisibleSubtitleText, visual.TextBoxText }
            .Where(text => !string.IsNullOrWhiteSpace(text))!
            .Select(text => text!);

    private static string FormatSemanticPageLabel(PageData page) =>
        !string.IsNullOrWhiteSpace(GetPageVisibleTitle(page))
            ? $"'{GetPageVisibleTitle(page)}'"
            : $"'{page.DisplayName}'";

    private static string FormatSemanticVisualLabel(PageData page, VisualData visual)
    {
        if (!string.IsNullOrWhiteSpace(visual.BestVisibleText))
        {
            return $"'{visual.BestVisibleText}'";
        }

        return !string.IsNullOrWhiteSpace(GetPageVisibleTitle(page))
            ? $"{FormatSemanticPageLabel(page)} {visual.Type}"
            : $"'{visual.Id}' {visual.Type}";
    }

    private static bool ContainsComparisonIntentKeywords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return ContainsTextKeyword(text, "compare") ||
            ContainsTextKeyword(text, "comparison") ||
            ContainsTextKeyword(text, "vs ") ||
            ContainsTextKeyword(text, "versus") ||
            ContainsTextKeyword(text, "variance") ||
            ContainsTextKeyword(text, "difference") ||
            ContainsTextKeyword(text, "gap") ||
            ContainsTextKeyword(text, "ranking") ||
            ContainsTextKeyword(text, "rank") ||
            ContainsTextKeyword(text, "top ") ||
            ContainsTextKeyword(text, "bottom");
    }

    private static bool ContainsSequentialKeywords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return ContainsTextKeyword(text, "trend") ||
            ContainsTextKeyword(text, "over time") ||
            ContainsTextKeyword(text, "time") ||
            ContainsTextKeyword(text, "timeline") ||
            ContainsTextKeyword(text, "month") ||
            ContainsTextKeyword(text, "monthly") ||
            ContainsTextKeyword(text, "quarter") ||
            ContainsTextKeyword(text, "quarterly") ||
            ContainsTextKeyword(text, "year") ||
            ContainsTextKeyword(text, "yearly") ||
            ContainsTextKeyword(text, "week") ||
            ContainsTextKeyword(text, "weekly") ||
            ContainsTextKeyword(text, "day") ||
            ContainsTextKeyword(text, "daily") ||
            ContainsTextKeyword(text, "yoy") ||
            ContainsTextKeyword(text, "mom") ||
            ContainsTextKeyword(text, "qoq");
    }

    private static bool ContainsFunnelContextKeywords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return ContainsTextKeyword(text, "funnel") ||
            ContainsTextKeyword(text, "pipeline") ||
            ContainsTextKeyword(text, "stage") ||
            ContainsTextKeyword(text, "step") ||
            ContainsTextKeyword(text, "conversion") ||
            ContainsTextKeyword(text, "journey") ||
            ContainsTextKeyword(text, "drop off") ||
            ContainsTextKeyword(text, "lead") ||
            ContainsTextKeyword(text, "opportunity") ||
            ContainsTextKeyword(text, "prospect") ||
            ContainsTextKeyword(text, "application");
    }

    private static bool LooksCategoricalHint(string? text)
    {
        var cue = ExtractCategoricalCue(text);
        return cue is not null;
    }

    private static string? ExtractCategoricalCue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        foreach (var cue in new[]
                 {
                     "region", "product", "category", "segment", "customer", "channel",
                     "country", "state", "city", "store", "department", "brand", "gender", "age"
                 })
        {
            if (ContainsTextKeyword(text, cue))
            {
                return cue;
            }
        }

        return null;
    }

    private static bool ContainsTextKeyword(string? text, string keyword)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(keyword))
        {
            return false;
        }

        var normalizedText = NormalizeNarrativeText(text);
        var normalizedKeyword = NormalizeNarrativeText(keyword);
        if (string.IsNullOrWhiteSpace(normalizedText) || string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return false;
        }

        if (normalizedKeyword.Contains(' '))
        {
            return normalizedText.Contains(normalizedKeyword, StringComparison.Ordinal);
        }

        return normalizedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(normalizedKeyword, StringComparer.Ordinal);
    }

    private static ConsistencyIssue? AnalyzeMetricLabelConsistency(IEnumerable<PageData> pages)
    {
        var metricLabels = pages
            .SelectMany(page => page.Visuals
                .Where(visual => !visual.IsHidden && !visual.IsNavigationElement && visual.IsKpiCard && !string.IsNullOrWhiteSpace(visual.BestVisibleText))
                .Select(visual => new
                {
                    PageName = page.DisplayName,
                    Visual = visual,
                    Label = visual.BestVisibleText!,
                    Pattern = ClassifyMetricLabelPattern(visual.BestVisibleText!),
                }))
            .ToList();
        if (metricLabels.Count < 2)
        {
            return null;
        }

        bool hasPrefix = metricLabels.Any(entry => entry.Pattern == MetricLabelPattern.PrefixModifier);
        bool hasSuffix = metricLabels.Any(entry => entry.Pattern == MetricLabelPattern.SuffixModifier);
        bool hasGeneric = metricLabels.Any(entry => entry.Pattern == MetricLabelPattern.GenericAggregate);
        if (!(hasPrefix && hasSuffix) && !hasGeneric)
        {
            return null;
        }

        var messageParts = new List<string>();
        if (hasPrefix && hasSuffix)
        {
            var prefixExample = metricLabels.First(entry => entry.Pattern == MetricLabelPattern.PrefixModifier).Label;
            var suffixExample = metricLabels.First(entry => entry.Pattern == MetricLabelPattern.SuffixModifier).Label;
            messageParts.Add($"metric labels mix prefix modifiers such as '{prefixExample}' and suffix modifiers such as '{suffixExample}'");
        }

        if (hasGeneric)
        {
            var genericExample = metricLabels.First(entry => entry.Pattern == MetricLabelPattern.GenericAggregate).Label;
            messageParts.Add($"generic labels such as '{genericExample}' remain in the KPI layer");
        }

        return new ConsistencyIssue(
            string.Join("; ", messageParts),
            metricLabels
                .Where(entry => entry.Pattern != MetricLabelPattern.Plain)
                .Select(entry => (entry.PageName, entry.Visual))
                .ToList());
    }

    private static ConsistencyIssue? AnalyzePageStyleLanguageConsistency(IEnumerable<PageData> pages)
    {
        var profiles = pages
            .Select(BuildPageStyleProfile)
            .Where(profile => profile.RepresentativeVisuals.Count > 0)
            .ToList();
        if (profiles.Count < 2)
        {
            return null;
        }

        var distinctProfiles = profiles
            .Select(profile => profile.Signature)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (distinctProfiles.Count < 2)
        {
            return null;
        }

        var primary = profiles[0];
        var contrast = profiles.First(profile => !string.Equals(profile.Signature, primary.Signature, StringComparison.Ordinal));
        var visuals = primary.RepresentativeVisuals
            .Select(visual => (primary.Page.DisplayName, visual))
            .Concat(contrast.RepresentativeVisuals.Select(visual => (contrast.Page.DisplayName, visual)))
            .ToList();
        return new ConsistencyIssue(
            $"{FormatSemanticPageLabel(primary.Page)} uses {DescribePageStyleProfile(primary)} while {FormatSemanticPageLabel(contrast.Page)} uses {DescribePageStyleProfile(contrast)}",
            visuals);
    }

    private static ConsistencyIssue? AnalyzeLayoutConventionConsistency(IEnumerable<PageData> pages)
    {
        var profiles = pages
            .Select(BuildLayoutConventionProfile)
            .Where(profile => profile.TitleVisual is not null || profile.FilterVisuals.Count > 0)
            .ToList();
        if (profiles.Count < 2)
        {
            return null;
        }

        var messageParts = new List<string>();
        var visuals = new List<(string PageName, VisualData Visual)>();

        var titleProfiles = profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.TitleAlignment) && profile.TitleVisual is not null)
            .ToList();
        var distinctTitleAlignments = titleProfiles
            .Select(profile => profile.TitleAlignment)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (distinctTitleAlignments.Count >= 2)
        {
            var first = titleProfiles[0];
            var second = titleProfiles.First(profile =>
                !string.Equals(profile.TitleAlignment, first.TitleAlignment, StringComparison.Ordinal));
            messageParts.Add($"title anchors shift between {first.TitleAlignment} and {second.TitleAlignment} alignment");
            visuals.Add((first.Page.DisplayName, first.TitleVisual!));
            visuals.Add((second.Page.DisplayName, second.TitleVisual!));
        }

        var filterProfiles = profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.FilterConvention) && profile.FilterVisuals.Count > 0)
            .ToList();
        var distinctFilterConventions = filterProfiles
            .Select(profile => profile.FilterConvention)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (distinctFilterConventions.Count >= 2)
        {
            var first = filterProfiles[0];
            var second = filterProfiles.First(profile =>
                !string.Equals(profile.FilterConvention, first.FilterConvention, StringComparison.Ordinal));
            messageParts.Add($"filter conventions shift between a {first.FilterConvention} and a {second.FilterConvention}");
            visuals.AddRange(first.FilterVisuals.Take(2).Select(visual => (first.Page.DisplayName, visual)));
            visuals.AddRange(second.FilterVisuals.Take(2).Select(visual => (second.Page.DisplayName, visual)));
        }

        return messageParts.Count == 0
            ? null
            : new ConsistencyIssue(
                string.Join("; ", messageParts),
                DeduplicateAffectedVisualTuples(visuals));
    }

    private static PageStyleProfile BuildPageStyleProfile(PageData page)
    {
        var dataVisuals = page.Visuals
            .Where(visual => !visual.IsHidden && !visual.IsNavigationElement && !visual.IsDecorative)
            .ToList();
        var formattedVisuals = dataVisuals
            .Where(HasAnyFormattingMetadata)
            .ToList();
        if (formattedVisuals.Count == 0)
        {
            return new PageStyleProfile(page, false, false, false, null, []);
        }

        var roundedKnown = formattedVisuals.Where(visual => visual.Formatting.CornerRadius.HasValue).ToList();
        var shadowKnown = formattedVisuals.Where(visual => visual.Formatting.HasShadow.HasValue).ToList();
        var fillKnown = formattedVisuals.Where(visual => !string.IsNullOrWhiteSpace(visual.Formatting.BackgroundFillColor)).ToList();
        bool usesRounded = roundedKnown.Count > 0 &&
            roundedKnown.Count(visual => visual.Formatting.CornerRadius >= 4) >= Math.Max(1, (int)Math.Ceiling(roundedKnown.Count / 2.0));
        bool usesShadow = shadowKnown.Count > 0 &&
            shadowKnown.Count(visual => visual.Formatting.HasShadow == true) >= Math.Max(1, (int)Math.Ceiling(shadowKnown.Count / 2.0));
        bool usesFill = fillKnown.Count > 0 &&
            fillKnown.Count >= Math.Max(1, (int)Math.Ceiling(formattedVisuals.Count / 2.0));
        string? dominantFillColor = fillKnown
            .GroupBy(visual => visual.Formatting.BackgroundFillColor!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();

        return new PageStyleProfile(
            page,
            usesRounded,
            usesShadow,
            usesFill,
            dominantFillColor,
            formattedVisuals.Take(2).ToList());
    }

    private static string DescribePageStyleProfile(PageStyleProfile profile)
    {
        var parts = new List<string>
        {
            profile.UsesRoundedCorners ? "rounded surfaces" : "square surfaces",
            profile.UsesShadow ? "elevated cards" : "flat cards",
        };

        parts.Add(profile.UsesBackgroundFill
            ? $"filled surfaces{(string.IsNullOrWhiteSpace(profile.DominantFillColor) ? string.Empty : $" ({profile.DominantFillColor})")}"
            : "unfilled surfaces");
        return string.Join(", ", parts);
    }

    private static LayoutConventionProfile BuildLayoutConventionProfile(PageData page)
    {
        double canvasWidth = GetCanvasWidth(page);
        double canvasHeight = GetCanvasHeight(page);
        var titleVisual = page.Visuals
            .Where(visual => !visual.IsHidden && visual.HasVisibleTitleIntent)
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .FirstOrDefault();
        string? titleAlignment = titleVisual is null
            ? null
            : ClassifyTitleAlignment(titleVisual, canvasWidth);
        var filterVisuals = page.Visuals
            .Where(visual => !visual.IsHidden && visual.IsSlicer)
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ToList();
        string? filterConvention = filterVisuals.Count == 0
            ? null
            : ClassifyFilterConvention(filterVisuals, canvasWidth, canvasHeight);

        return new LayoutConventionProfile(
            page,
            titleAlignment,
            titleVisual,
            filterConvention,
            filterVisuals);
    }

    private static string ClassifyTitleAlignment(VisualData visual, double canvasWidth)
    {
        double centerX = visual.X + visual.W / 2.0;
        if (visual.X <= canvasWidth * 0.18)
        {
            return "left";
        }

        if (centerX >= canvasWidth * 0.35 && centerX <= canvasWidth * 0.65)
        {
            return "center";
        }

        return "offset";
    }

    private static string ClassifyFilterConvention(IReadOnlyList<VisualData> filters, double canvasWidth, double canvasHeight)
    {
        bool allTop = filters.All(filter => filter.Y <= canvasHeight * 0.22);
        bool allLeft = filters.All(filter => filter.X <= canvasWidth * 0.24 || filter.X + filter.W <= canvasWidth * 0.32);
        if (allTop)
        {
            return "top band";
        }

        if (allLeft)
        {
            return "left rail";
        }

        return "mixed filter placement";
    }

    private static MetricLabelPattern ClassifyMetricLabelPattern(string label)
    {
        var normalized = NormalizeNarrativeText(label);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return MetricLabelPattern.Plain;
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^(sum|average|avg|count|min|max|median)\s+of\s+", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
        {
            return MetricLabelPattern.GenericAggregate;
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^(ytd|mtd|qtd|fy|yoy|mom|qoq|rolling)\b", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
        {
            return MetricLabelPattern.PrefixModifier;
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"\b(ytd|mtd|qtd|fy|yoy|mom|qoq|rolling)$", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
        {
            return MetricLabelPattern.SuffixModifier;
        }

        return MetricLabelPattern.Plain;
    }

    private static List<(string PageName, VisualData Visual)> DeduplicateAffectedVisualTuples(
        IEnumerable<(string PageName, VisualData Visual)> visuals) =>
        visuals
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Visual.Id))
            .GroupBy(entry => (entry.PageName, entry.Visual.Id))
            .Select(group => group.First())
            .ToList();

    private static NarrativePageAnalysis AnalyzeNarrativePage(PageData page)
    {
        var visibleVisuals = page.Visuals.Where(visual => !visual.IsHidden).ToList();
        var visibleDataVisuals = visibleVisuals
            .Where(visual => !visual.IsDecorative && !visual.IsNavigationElement)
            .ToList();
        var kpiCards = visibleDataVisuals.Where(visual => visual.IsKpiCard).ToList();
        var supportingDataVisuals = visibleDataVisuals.Where(visual => !visual.IsKpiCard).ToList();
        var trendVisuals = visibleDataVisuals.Where(visual => visual.IsTrend).ToList();
        var comparisonVisuals = visibleDataVisuals.Where(visual => visual.IsComparison).ToList();
        var titleBearingVisuals = visibleVisuals.Where(visual => visual.HasVisibleTitleIntent).ToList();
        var visibleTexts = visibleVisuals
            .SelectMany(visual => new[] { visual.VisibleTitleText, visual.VisibleSubtitleText, visual.TextBoxText, visual.BestVisibleText })
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var visibleTitle = GetPageVisibleTitle(page);
        bool hasAnyVisibleTitle = !string.IsNullOrWhiteSpace(visibleTitle);
        bool hasMeaningfulVisibleTitle = hasAnyVisibleTitle && !IsVagueNarrativeText(visibleTitle!);
        bool hasHeadlineOutcome =
            kpiCards.Count > 0 ||
            (hasMeaningfulVisibleTitle && (trendVisuals.Count > 0 || comparisonVisuals.Count > 0 || visibleDataVisuals.Count <= 2));
        bool hasKpiComparisonContext =
            kpiCards.Count == 0 ||
            trendVisuals.Count > 0 ||
            comparisonVisuals.Count > 0 ||
            visibleTexts.Any(ContainsNarrativeContextKeywords) ||
            visibleDataVisuals.Any(visual => visual.Labels.HasDataLabels == true);
        bool hasSupportingEvidenceFlow =
            kpiCards.Count == 0 ||
            supportingDataVisuals.Count > 0;

        return new NarrativePageAnalysis(
            Page: page,
            VisibleTitle: visibleTitle,
            HasAnyVisibleTitle: hasAnyVisibleTitle,
            HasMeaningfulVisibleTitle: hasMeaningfulVisibleTitle,
            VisibleDataVisuals: visibleDataVisuals,
            KpiCards: kpiCards,
            SupportingDataVisuals: supportingDataVisuals,
            TrendVisuals: trendVisuals,
            ComparisonVisuals: comparisonVisuals,
            TitleBearingVisuals: titleBearingVisuals,
            VisibleTexts: visibleTexts,
            HasHeadlineOutcome: hasHeadlineOutcome,
            HasKpiComparisonContext: hasKpiComparisonContext,
            HasSupportingEvidenceFlow: hasSupportingEvidenceFlow);
    }

    private static string BuildNarrativePurposeMessage(
        IReadOnlyList<string> vagueTitlePages,
        IReadOnlyList<string> noTitlePages)
    {
        if (vagueTitlePages.Count > 0 && noTitlePages.Count > 0)
        {
            return $"Visible page purpose: {string.Join(", ", vagueTitlePages)} use vague visible titles and {string.Join(", ", noTitlePages)} have no visible title or question anchor. Replace generic labels such as 'Overview' or 'Page 1' with a clearer purpose statement.";
        }

        if (vagueTitlePages.Count > 0)
        {
            return $"Visible page purpose: {string.Join(", ", vagueTitlePages)} use vague visible titles. Replace generic labels such as 'Overview' or 'Page 1' with a clearer decision-led title.";
        }

        return $"Visible page purpose: {string.Join(", ", noTitlePages)} have no visible title or question anchor. Add one so users understand what the page is trying to say.";
    }

    private static IEnumerable<(string PageName, VisualData Visual)> BuildNarrativeEvidenceVisuals(
        IEnumerable<NarrativePageAnalysis> analyses) =>
        analyses.SelectMany(analysis =>
        {
            var visuals = analysis.TitleBearingVisuals.Count > 0
                ? analysis.TitleBearingVisuals
                : analysis.KpiCards.Count > 0
                    ? analysis.KpiCards
                    : analysis.VisibleDataVisuals.Take(1).ToList();

            return visuals.Select(visual => (analysis.Page.DisplayName, visual));
        });

    private static string BuildNarrativeRecommendationSubject(NarrativePageAnalysis analysis) =>
        !string.IsNullOrWhiteSpace(analysis.VisibleTitle) && !IsVagueNarrativeText(analysis.VisibleTitle!)
            ? $"'{analysis.VisibleTitle}'"
            : $"'{analysis.Page.DisplayName}'";

    private static string GetPrimaryKpiLabel(NarrativePageAnalysis analysis)
    {
        var titledCard = analysis.KpiCards
            .Select(card => card.BestVisibleText)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text) && !IsVagueNarrativeText(text));

        return !string.IsNullOrWhiteSpace(titledCard)
            ? $"{titledCard} KPI"
            : $"'{analysis.Page.DisplayName}' KPI layer";
    }

    private static bool ContainsNarrativeContextKeywords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.ToLowerInvariant();
        return normalized.Contains("target", StringComparison.Ordinal) ||
            normalized.Contains("budget", StringComparison.Ordinal) ||
            normalized.Contains("variance", StringComparison.Ordinal) ||
            normalized.Contains("prior", StringComparison.Ordinal) ||
            normalized.Contains("previous", StringComparison.Ordinal) ||
            normalized.Contains("trend", StringComparison.Ordinal) ||
            normalized.Contains("delta", StringComparison.Ordinal) ||
            normalized.Contains("change", StringComparison.Ordinal) ||
            normalized.Contains("growth", StringComparison.Ordinal) ||
            normalized.Contains("decline", StringComparison.Ordinal) ||
            normalized.Contains("on track", StringComparison.Ordinal) ||
            normalized.Contains("at risk", StringComparison.Ordinal) ||
            normalized.Contains("below target", StringComparison.Ordinal) ||
            normalized.Contains("above target", StringComparison.Ordinal) ||
            normalized.Contains("vs ", StringComparison.Ordinal) ||
            normalized.Contains("versus", StringComparison.Ordinal) ||
            normalized.Contains("yoy", StringComparison.Ordinal) ||
            normalized.Contains("mom", StringComparison.Ordinal) ||
            normalized.Contains("qoq", StringComparison.Ordinal);
    }

    private static bool IsVagueNarrativeText(string text)
    {
        var normalized = NormalizeNarrativeText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^page\s+\d+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
        {
            return true;
        }

        return normalized is "page"
            or "overview"
            or "summary"
            or "dashboard"
            or "analysis"
            or "details"
            or "detail"
            or "report"
            or "metrics"
            or "kpis"
            or "kpi"
            or "home";
    }

    private static string NormalizeNarrativeText(string text)
    {
        var cleaned = new string(text
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ')
            .ToArray());
        return string.Join(" ", cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsTextVisualType(string visualType) =>
        visualType.Equals("textbox", StringComparison.OrdinalIgnoreCase);

    private static string? ExtractVisibleObjectText(JsonNode? root, IReadOnlyList<string> objectNames)
    {
        foreach (var obj in FindObjectsRecursive(root, objectNames))
        {
            if (!IsObjectVisible(obj))
            {
                continue;
            }

            var directText = NormalizeText(ReadFirstString(obj, ["text", "value", "displayText", "titleText", "subtitleText", "content"]));
            if (!string.IsNullOrWhiteSpace(directText))
            {
                return directText;
            }

            var runText = ExtractTextRunContent(obj);
            if (!string.IsNullOrWhiteSpace(runText))
            {
                return runText;
            }
        }

        return null;
    }

    private static string? ExtractVisibleScalarText(JsonNode? root, IReadOnlyList<string> propertyNames)
    {
        foreach (var node in FindValuesRecursive(root, propertyNames))
        {
            var text = NormalizeText(ReadStringNode(node));
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static string? ExtractTextRunContent(JsonNode? root)
    {
        var values = FindValuesRecursive(root, new[] { "textRuns", "runs", "paragraphs" })
            .SelectMany(CollectTextLeaves)
            .Select(NormalizeText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        return values.Count == 0
            ? null
            : string.Join(" ", values.Distinct(StringComparer.Ordinal));
    }

    private static IEnumerable<string> CollectTextLeaves(JsonNode? node)
    {
        if (node is null)
        {
            yield break;
        }

        if (node is JsonObject obj)
        {
            var text = NormalizeText(ReadFirstString(obj, ["text", "value", "content", "displayText"]));
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }

            foreach (var child in obj)
            {
                foreach (var nested in CollectTextLeaves(child.Value))
                {
                    yield return nested;
                }
            }

            yield break;
        }

        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                foreach (var nested in CollectTextLeaves(item))
                {
                    yield return nested;
                }
            }
        }
    }

    private static bool? ExtractPresenceFlag(
        JsonNode? root,
        IReadOnlyList<string> objectNames,
        IReadOnlyList<string> scalarNames)
    {
        foreach (var obj in FindObjectsRecursive(root, objectNames))
        {
            var explicitValue = ReadFirstBoolean(obj, ["visible", "show", "enabled"]);
            if (explicitValue.HasValue)
            {
                return explicitValue.Value;
            }

            return true;
        }

        foreach (var node in FindValuesRecursive(root, scalarNames))
        {
            var boolValue = ReadBooleanNode(node);
            if (boolValue.HasValue)
            {
                return boolValue.Value;
            }
        }

        return null;
    }

    private static List<string> CollectRoleHints(
        JsonNode? root,
        IReadOnlyList<string> containerNames,
        IReadOnlyList<string> roleNames)
    {
        var hints = new List<string>();
        foreach (var container in FindObjectsRecursive(root, containerNames))
        {
            foreach (var roleName in roleNames)
            {
                if (TryGetPropertyCaseInsensitive(container, roleName, out var roleNode))
                {
                    hints.AddRange(ReadStringValues(roleNode));
                }
            }
        }

        return hints
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ExtractColorFromObjects(JsonNode? root, IReadOnlyList<string> objectNames)
    {
        foreach (var obj in FindObjectsRecursive(root, objectNames))
        {
            var color = ExtractColor(obj);
            if (!string.IsNullOrWhiteSpace(color))
            {
                return color;
            }
        }

        return null;
    }

    private static string? ExtractColor(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue)
        {
            return NormalizeColor(ReadStringNode(node));
        }

        if (node is JsonObject obj)
        {
            var direct = NormalizeColor(ReadFirstString(obj, ["color", "hex", "value"]));
            if (!string.IsNullOrWhiteSpace(direct))
            {
                return direct;
            }

            foreach (var key in new[] { "solid", "fill", "foreground", "background" })
            {
                if (TryGetPropertyCaseInsensitive(obj, key, out var nested))
                {
                    var color = ExtractColor(nested);
                    if (!string.IsNullOrWhiteSpace(color))
                    {
                        return color;
                    }
                }
            }
        }

        return null;
    }

    private static double? ExtractNumericSetting(
        JsonNode? root,
        IReadOnlyList<string> objectNames,
        IReadOnlyList<string> valueNames)
    {
        foreach (var obj in FindObjectsRecursive(root, objectNames))
        {
            var number = ReadFirstDouble(obj, valueNames);
            if (number.HasValue)
            {
                return number.Value;
            }
        }

        return null;
    }

    private static double? ExtractScalarNumber(JsonNode? root, IReadOnlyList<string> propertyNames)
    {
        foreach (var node in FindValuesRecursive(root, propertyNames))
        {
            var number = ReadDoubleNode(node);
            if (number.HasValue)
            {
                return number.Value;
            }
        }

        return null;
    }

    private static IEnumerable<JsonObject> FindObjectsRecursive(JsonNode? node, IReadOnlyList<string> propertyNames) =>
        FindValuesRecursive(node, propertyNames).OfType<JsonObject>();

    private static IEnumerable<JsonNode> FindValuesRecursive(JsonNode? node, IReadOnlyList<string> propertyNames)
    {
        if (node is null)
        {
            yield break;
        }

        var nameSet = new HashSet<string>(propertyNames, StringComparer.OrdinalIgnoreCase);
        foreach (var value in FindValuesRecursive(node, nameSet))
        {
            yield return value;
        }
    }

    private static IEnumerable<JsonNode> FindValuesRecursive(JsonNode? node, HashSet<string> propertyNames)
    {
        if (node is null)
        {
            yield break;
        }

        if (node is JsonObject obj)
        {
            foreach (var child in obj)
            {
                if (child.Value is null)
                {
                    continue;
                }

                if (propertyNames.Contains(child.Key))
                {
                    yield return child.Value;
                }

                foreach (var nested in FindValuesRecursive(child.Value, propertyNames))
                {
                    yield return nested;
                }
            }

            yield break;
        }

        if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                foreach (var nested in FindValuesRecursive(item, propertyNames))
                {
                    yield return nested;
                }
            }
        }
    }

    private static string? ReadFirstString(JsonObject obj, IReadOnlyList<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetPropertyCaseInsensitive(obj, propertyName, out var node))
            {
                var text = ReadStringNode(node);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static bool? ReadFirstBoolean(JsonObject obj, IReadOnlyList<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetPropertyCaseInsensitive(obj, propertyName, out var node))
            {
                var value = ReadBooleanNode(node);
                if (value.HasValue)
                {
                    return value.Value;
                }
            }
        }

        return null;
    }

    private static double? ReadFirstDouble(JsonObject obj, IReadOnlyList<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetPropertyCaseInsensitive(obj, propertyName, out var node))
            {
                var value = ReadDoubleNode(node);
                if (value.HasValue)
                {
                    return value.Value;
                }
            }
        }

        return null;
    }

    private static double? ReadNestedDouble(JsonObject obj, string objectName, string propertyName)
    {
        if (!TryGetPropertyCaseInsensitive(obj, objectName, out var child) || child is not JsonObject childObject)
        {
            return null;
        }

        return ReadFirstDouble(childObject, [propertyName]);
    }

    private static bool TryGetPropertyCaseInsensitive(JsonObject obj, string propertyName, out JsonNode? value)
    {
        foreach (var child in obj)
        {
            if (string.Equals(child.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = child.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static IReadOnlyList<string> ReadStringValues(JsonNode? node)
    {
        if (node is null)
        {
            return [];
        }

        if (node is JsonArray arr)
        {
            return arr
                .SelectMany(ReadStringValues)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
        }

        if (node is JsonObject obj)
        {
            var values = new List<string>();
            var direct = ReadFirstString(obj, ["name", "value", "field", "displayName", "queryRef"]);
            if (!string.IsNullOrWhiteSpace(direct))
            {
                values.Add(direct);
            }

            foreach (var child in obj)
            {
                values.AddRange(ReadStringValues(child.Value));
            }

            return values;
        }

        var scalar = ReadStringNode(node);
        return string.IsNullOrWhiteSpace(scalar) ? [] : [scalar];
    }

    private static bool IsObjectVisible(JsonObject obj)
    {
        var hidden = ReadFirstBoolean(obj, ["isHidden", "hidden"]);
        if (hidden == true)
        {
            return false;
        }

        var visible = ReadFirstBoolean(obj, ["visible", "show", "enabled"]);
        return visible != false;
    }

    private static string? ReadStringNode(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<string>();
        }
        catch
        {
            return null;
        }
    }

    private static bool? ReadBooleanNode(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<bool>();
        }
        catch
        {
            var text = ReadStringNode(node);
            return bool.TryParse(text, out var result) ? result : null;
        }
    }

    private static double? ReadDoubleNode(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<double>();
        }
        catch
        {
            var text = ReadStringNode(node);
            return double.TryParse(text, out var result) ? result : null;
        }
    }

    private static string? NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return string.Join(' ', text.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string? NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith("#", StringComparison.Ordinal))
        {
            normalized = $"#{normalized}";
        }

        if (normalized.Length is not (7 or 9))
        {
            return null;
        }

        for (int i = 1; i < normalized.Length; i++)
        {
            if (!Uri.IsHexDigit(normalized[i]))
            {
                return null;
            }
        }

        return normalized.ToUpperInvariant();
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static List<AffectedVisualReference> BuildAffectedVisuals(
        string pageName,
        IEnumerable<VisualData> visuals) =>
        BuildAffectedVisuals(visuals.Select(visual => (PageName: pageName, Visual: visual)));

    private static List<AffectedVisualReference> BuildAffectedVisuals(
        IEnumerable<(string PageName, VisualData Visual)> visuals) =>
        visuals
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Visual.Id))
            .GroupBy(entry => (entry.PageName, entry.Visual.Id))
            .Select(group => group.First())
            .Select(entry => new AffectedVisualReference(
                entry.PageName,
                entry.Visual.Id,
                string.IsNullOrWhiteSpace(entry.Visual.Type) ? "visual" : entry.Visual.Type))
            .ToList();

    private static FrameworkFeedbackItem FeedbackItem(
        bool ok,
        string text,
        string findingType,
        List<AffectedVisualReference>? affectedVisuals = null) =>
        new(
            ok,
            text,
            affectedVisuals,
            FindingType: findingType);

    private static FrameworkFeedbackItem ScoredFeedback(
        bool ok,
        string text,
        double earnedPoints,
        double possiblePoints,
        string findingType,
        List<AffectedVisualReference>? affectedVisuals = null) =>
        new(
            ok,
            text,
            affectedVisuals,
            Math.Max(0.0, Math.Min(possiblePoints, earnedPoints)),
            possiblePoints,
            findingType);

    /// <summary>
    /// Extracts framework weights from the configuration JsonElement passed from the frontend.
    /// Returns a dictionary mapping framework names to their configured weights (0-100).
    /// If config is null or doesn't contain a framework, returns 0 for that framework.
    /// </summary>
    /// <param name="config">The configuration JsonElement from the frontend (optional).</param>
    /// <returns>A dictionary of framework weights. If config is null, returns an empty dictionary.</returns>
    private Dictionary<string, double> ExtractFrameworkWeights(JsonElement? config)
    {
        var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (!config.HasValue || config.Value.ValueKind != JsonValueKind.Object)
        {
            _logger.LogWarning("[ExtractFrameworkWeights] Config is null or not an object, using default Design Analyzer configuration");
            return GetDefaultFrameworkWeights();
        }

        // Try to extract from "frameworks" array (new format from Design Analyzer Config panel)
        if (config.Value.TryGetProperty("frameworks", out var frameworksArray) &&
            frameworksArray.ValueKind == JsonValueKind.Array)
        {
            _logger.LogInformation("[ExtractFrameworkWeights] Found {FrameworkCount} frameworks in config array", frameworksArray.GetArrayLength());
            
            foreach (var framework in frameworksArray.EnumerateArray())
            {
                if (framework.ValueKind != JsonValueKind.Object)
                    continue;

                // Get the framework ID
                string? id = null;
                if (framework.TryGetProperty("id", out var idProp))
                {
                    id = idProp.GetString();
                }

                if (string.IsNullOrEmpty(id))
                    continue;

                // Normalize ID to match internal framework names
                var normalizedId = NormalizeFrameworkId(id);

                // Check if framework is enabled
                bool isEnabled = false;
                if (framework.TryGetProperty("enabled", out var enabledProp))
                {
                    isEnabled = enabledProp.ValueKind == JsonValueKind.True;
                }

                // Extract weight
                double weight = 0;
                if (isEnabled && framework.TryGetProperty("weight", out var weightProp))
                {
                    try
                    {
                        weight = weightProp.GetDouble();
                    }
                    catch { /* fall through */ }
                }

                weights[normalizedId] = weight;
                _logger.LogDebug("[ExtractFrameworkWeights]   {FrameworkId} → {NormalizedId}: enabled={Enabled}, weight={Weight}%", id, normalizedId, isEnabled, weight);
            }

            if (weights.Count > 0)
            {
                _logger.LogInformation("[ExtractFrameworkWeights] Extracted {WeightCount} framework weights: {Weights}", 
                    weights.Count, 
                    string.Join(", ", weights.Select(kv => $"{kv.Key}={kv.Value}%")));
            }
            else
            {
                _logger.LogWarning("[ExtractFrameworkWeights] Frameworks array was present but no enabled frameworks found!");
            }
            return weights;
        }

        _logger.LogWarning("[ExtractFrameworkWeights] No 'frameworks' array found in config, trying legacy format");
        
        // Fall back to old flat object format (legacy support)
        var legacyFrameworks = new[]
        {
            "gestalt", "cognitiveLoad", "dataInk", "graphicalPerception", "accessibility",
            "visualBestPractices", "governance", "stephenFew", "tufte", "density", "narrative"
        };

        foreach (var framework in legacyFrameworks)
        {
            if (config.Value.TryGetProperty(framework, out var frameworkObj) &&
                frameworkObj.ValueKind == JsonValueKind.Object)
            {
                // Check if framework is enabled
                bool isEnabled = true;
                if (frameworkObj.TryGetProperty("enabled", out var enabledProp))
                {
                    isEnabled = enabledProp.ValueKind == JsonValueKind.True;
                }

                // Extract weight
                double weight = 0;
                if (isEnabled && frameworkObj.TryGetProperty("weight", out var weightProp))
                {
                    try
                    {
                        weight = weightProp.GetDouble();
                    }
                    catch { /* fall through */ }
                }

                weights[framework] = weight;
                _logger.LogDebug("[ExtractFrameworkWeights] (legacy) {Framework}: enabled={Enabled}, weight={Weight}%", framework, isEnabled, weight);
            }
        }

        if (weights.Count > 0)
        {
            _logger.LogInformation("[ExtractFrameworkWeights] Extracted {WeightCount} legacy weights: {Weights}", 
                weights.Count, 
                string.Join(", ", weights.Select(kv => $"{kv.Key}={kv.Value}%")));
        }
        else
        {
            _logger.LogWarning("[ExtractFrameworkWeights] No weights found in either new or legacy format - using default Design Analyzer configuration");
            return GetDefaultFrameworkWeights();
        }

        return weights;
    }

    private static Dictionary<string, double> GetDefaultFrameworkWeights() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["gestalt"] = 30,
        ["cognitiveLoad"] = 20,
        ["dataInk"] = 15,
        ["graphicalPerception"] = 0,
        ["accessibility"] = 15,
        ["visualBestPractices"] = 20,
        ["governance"] = 0,
        ["stephenFew"] = 0,
        ["tufte"] = 0,
        ["density"] = 0,
        ["narrative"] = 0,
    };

    private static NavigationScoringSettings ExtractNavigationScoringSettings(JsonElement? config)
    {
        var defaults = new NavigationScoringSettings(
            Enabled: true,
            WeightPercent: 25,
            WarningNavigationCount: 8,
            WarningHiddenVisualCount: 5);

        if (!config.HasValue || config.Value.ValueKind != JsonValueKind.Object)
        {
            return defaults;
        }

        if (!config.Value.TryGetProperty("navigationScoring", out var navigationScoring) ||
            navigationScoring.ValueKind != JsonValueKind.Object)
        {
            return defaults;
        }

        var enabled = defaults.Enabled;
        if (navigationScoring.TryGetProperty("enabled", out var enabledProp) &&
            enabledProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            enabled = enabledProp.GetBoolean();
        }

        var weightPercent = defaults.WeightPercent;
        if (navigationScoring.TryGetProperty("weight", out var weightProp) &&
            weightProp.ValueKind == JsonValueKind.Number)
        {
            try
            {
                weightPercent = Math.Clamp(weightProp.GetDouble(), 0, 100);
            }
            catch
            {
                weightPercent = defaults.WeightPercent;
            }
        }

        return defaults with
        {
            Enabled = enabled,
            WeightPercent = weightPercent,
        };
    }

    private static GovernanceRules ExtractGovernanceRules(JsonElement? config)
    {
        var rules = new GovernanceRules(MaxVisualsPerPage: 10, AllowPieCharts: false, RequirePageTitle: true);

        if (!config.HasValue || !config.Value.TryGetProperty("governance", out var governanceArray) ||
            governanceArray.ValueKind != JsonValueKind.Array)
        {
            return rules;
        }

        foreach (var rule in governanceArray.EnumerateArray())
        {
            if (rule.ValueKind != JsonValueKind.Object || !rule.TryGetProperty("id", out var idProp))
            {
                continue;
            }

            var id = idProp.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(id) || !rule.TryGetProperty("value", out var valueProp))
            {
                continue;
            }

            switch (id)
            {
                case "maxVisuals":
                case "maxVisualsPerPage":
                    if (valueProp.ValueKind == JsonValueKind.Number && valueProp.TryGetInt32(out var maxVisuals))
                    {
                        rules = rules with { MaxVisualsPerPage = Math.Max(1, maxVisuals) };
                    }
                    break;
                case "allowPie":
                case "allowPieCharts":
                    if (valueProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        rules = rules with { AllowPieCharts = valueProp.GetBoolean() };
                    }
                    break;
                case "requireTitle":
                case "requirePageTitle":
                    if (valueProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        rules = rules with { RequirePageTitle = valueProp.GetBoolean() };
                    }
                    break;
            }
        }

        return rules;
    }

    /// <summary>
    /// Normalizes framework ID from Design Analyzer panel format to internal framework names.
    /// Supports all 11 frameworks: 7 core + 4 optional.
    /// E.g.: "gestalt" → "gestalt", "cognitive" → "cognitiveLoad", "dataink" → "dataInk"
    /// </summary>
    private static string NormalizeFrameworkId(string id)
    {
        return id.ToLowerInvariant() switch
        {
            // Core frameworks (7)
            "gestalt" => "gestalt",
            "cognitive" or "cognitivelload" => "cognitiveLoad",
            "dataink" or "data-ink" => "dataInk",
            "graphical" or "graphicalperception" => "graphicalPerception",
            "accessibility" or "wcag" => "accessibility",
            "visual" or "visualbestpractices" => "visualBestPractices",
            "governance" or "enterprisegovernance" => "governance",
            // Optional frameworks (4)
            "stephen" or "stephenfew" => "stephenFew",
            "tufte" or "tufeminimalism" => "tufte",
            "density" or "dashboarddensity" => "density",
            "narrative" or "narrativedesign" => "narrative",
            _ => id // Return original if no mapping found
        };
    }

    private static double Clamp(double value) => Math.Max(0.0, Math.Min(100.0, value));

    // ── Inner types ──────────────────────────────────────────────────────────

    private readonly record struct GovernanceRules(int MaxVisualsPerPage, bool AllowPieCharts, bool RequirePageTitle);

    private readonly record struct NavigationScoringSettings(
        bool Enabled,
        double WeightPercent,
        int WarningNavigationCount,
        int WarningHiddenVisualCount)
    {
        public double WeightMultiplier => WeightPercent / 100.0;
    }

    private readonly record struct VisualComposition(
        int DataVisualCount,
        int NavigationVisualCount,
        int HiddenVisualCount,
        double WeightedVisibleCount);

    private readonly record struct CanvasMetadata(double Width, double Height);

    private readonly record struct VisualRow(
        List<VisualData> Visuals,
        double Top,
        double Bottom);

    private readonly record struct PageLayoutIssue(
        PageData Page,
        string Message,
        List<VisualData> Visuals,
        double Penalty);

    private readonly record struct SemanticChartIssue(
        PageData Page,
        string Message,
        List<VisualData> Visuals,
        double Penalty);

    private readonly record struct PieUsageAnalysis(
        int PieCount,
        int OverviewPieCount,
        List<(string PageName, VisualData Visual)> PieVisuals);

    private readonly record struct ConsistencyIssue(
        string Message,
        List<(string PageName, VisualData Visual)> Visuals,
        double Penalty = 0.0);

    private readonly record struct PageStyleProfile(
        PageData Page,
        bool UsesRoundedCorners,
        bool UsesShadow,
        bool UsesBackgroundFill,
        string? DominantFillColor,
        List<VisualData> RepresentativeVisuals)
    {
        public string Signature =>
            $"{(UsesRoundedCorners ? "rounded" : "square")}|{(UsesShadow ? "elevated" : "flat")}|{(UsesBackgroundFill ? DominantFillColor ?? "filled" : "unfilled")}";
    }

    private readonly record struct LayoutConventionProfile(
        PageData Page,
        string? TitleAlignment,
        VisualData? TitleVisual,
        string? FilterConvention,
        List<VisualData> FilterVisuals);

    private enum MetricLabelPattern
    {
        Plain,
        PrefixModifier,
        SuffixModifier,
        GenericAggregate,
    }

    private sealed record PageData
    {
        public required string DisplayName { get; init; }
        public List<VisualData> Visuals { get; init; } = [];
        public CanvasMetadata? Canvas { get; init; }
    }

    private sealed record VisualData
    {
        public required string Id { get; init; }
        public required string Type { get; init; }
        public double X { get; init; }
        public double Y { get; init; }
        public double W { get; init; }
        public double H { get; init; }
        public bool IsHidden { get; init; }
        public VisualTextMetadata Text { get; init; } = VisualTextMetadata.Empty;
        public VisualLabelMetadata Labels { get; init; } = VisualLabelMetadata.Empty;
        public VisualFieldRoleMetadata FieldRoles { get; init; } = VisualFieldRoleMetadata.Empty;
        public VisualFormattingMetadata Formatting { get; init; } = VisualFormattingMetadata.Empty;

        public bool IsSlicer     => Type is "slicer" or "advancedSlicerVisual";
        public bool IsKpiCard    => Type is "card" or "kpiVisual" or "multiRowCard";
        public bool IsPieDonut   => Type is "pieChart" or "donutChart";
        public bool IsTrend      => Type is "lineChart" or "areaChart" or "lineAndStackedColumnChart" or "lineAndClusteredColumnChart";
        public bool IsComparison => Type is "clusteredColumnChart" or "clusteredBarChart"
                                          or "stackedColumnChart" or "stackedBarChart"
                                          or "barChart" or "columnChart" or "waterfallChart";
        public string? VisibleTitleText => Text.VisibleTitleText;
        public string? VisibleSubtitleText => Text.VisibleSubtitleText;
        public string? TextBoxText => Text.TextBoxText;
        public string? BestVisibleText => FirstNonBlank(VisibleTitleText, TextBoxText, VisibleSubtitleText);
        public bool HasVisibleTitleIntent => !string.IsNullOrWhiteSpace(BestVisibleText);
        public bool IsNavigationElement
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Type))
                {
                    return false;
                }

                var normalized = Type.Trim().ToLowerInvariant();
                return normalized is "actionbutton"
                    or "navigationbutton"
                    or "basicshape"
                    or "shape"
                    or "image"
                    or "slicer"
                    or "advancedslicervisual"
                    or "qnavisual"
                    || normalized.Contains("button", StringComparison.Ordinal)
                    || normalized.Contains("image", StringComparison.Ordinal)
                    || normalized.EndsWith("slicer", StringComparison.Ordinal);
            }
        }

        public bool IsDecorative => string.IsNullOrWhiteSpace(Type) || _decorativeTypes.Contains(Type);
    }

    private sealed record VisualTextMetadata(string? VisibleTitleText, string? VisibleSubtitleText, string? TextBoxText)
    {
        public static VisualTextMetadata Empty { get; } = new(null, null, null);
    }

    private sealed record VisualLabelMetadata(bool? HasLegend, bool? HasAxisLabels, bool? HasDataLabels)
    {
        public static VisualLabelMetadata Empty { get; } = new(null, null, null);
    }

    private sealed record VisualFieldRoleMetadata(
        IReadOnlyList<string> CategoryHints,
        IReadOnlyList<string> ValueHints,
        IReadOnlyList<string> SeriesHints,
        IReadOnlyList<string> MeasureHints)
    {
        public static VisualFieldRoleMetadata Empty { get; } = new([], [], [], []);
    }

    private sealed record VisualFormattingMetadata(
        string? BackgroundFillColor,
        string? FontColor,
        bool? HasBorder,
        double? CornerRadius,
        bool? HasShadow)
    {
        public static VisualFormattingMetadata Empty { get; } = new(null, null, null, null, null);
    }

    private sealed record NarrativePageAnalysis(
        PageData Page,
        string? VisibleTitle,
        bool HasAnyVisibleTitle,
        bool HasMeaningfulVisibleTitle,
        List<VisualData> VisibleDataVisuals,
        List<VisualData> KpiCards,
        List<VisualData> SupportingDataVisuals,
        List<VisualData> TrendVisuals,
        List<VisualData> ComparisonVisuals,
        List<VisualData> TitleBearingVisuals,
        List<string> VisibleTexts,
        bool HasHeadlineOutcome,
        bool HasKpiComparisonContext,
        bool HasSupportingEvidenceFlow);
}
