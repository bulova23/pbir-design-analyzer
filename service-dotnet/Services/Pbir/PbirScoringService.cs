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
                noVizFeedback[key] = [new(false, noVisualsMsg)];
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
            };
#pragma warning restore CS0618
        }

        // ── Compute all six frameworks for this page ────────────────────────
        var (gestaltScore, gestaltFeedback)          = ComputeGestaltScore(pageList);
        var (cogLoadScore, cogLoadFeedback)          = ComputeCognitiveLoadScore(pageList, recommendations, navigationScoring);
        var (dataInkScore, dataInkFeedback)          = ComputeDataInkScore(pageList, recommendations, navigationScoring);
        var (accessibilityScore, a11yFeedback)       = ComputeAccessibilityScore(themeColors, recommendations);
        var (vbpScore, vbpFeedback)                  = ComputeVisualBestPracticesScore(pageList, themeColors, recommendations);
        var (governanceScore, governanceFeedback)    = ComputeGovernanceScore(pageList, config);
        var (fewScore, fewFeedback)                  = ComputeStephenFewScore(pageList);
        var (tufteScore, tufteFeedback)              = ComputeTufteScore(pageList);
        var (graphicalScore, graphicalFeedback)      = ComputeGraphicalPerceptionScore(pageList);
        var (densityScore, densityFeedback)          = ComputeDashboardDensityScore(pageList, navigationScoring);
        var (narrativeScore, narrativeFeedback)      = ComputeNarrativeDesignScore(pageList);

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
        };
#pragma warning restore CS0618

        // ── Phase 2: Check for bookmarks and score layout states ─────────────
        var bookmarks = BookmarkParser.ParseBookmarks(reportJson);
        if (bookmarks.Count > 0)
        {
            _logger.LogInformation("[Bookmark State] Detected {Count} bookmarks on page '{Page}'", bookmarks.Count, pageName);
            
            var pageVisualIds = pageList.SelectMany(p => p.Visuals).Select(v => v.Id).ToList();
            var layoutStates = LayoutStateGenerator.GenerateStates(pageVisualIds, bookmarks);
            
            _logger.LogInformation("[Bookmark State] Generated {Count} layout states", layoutStates.Count);
            
            // Score each layout state (placeholder implementation)
            var perStateScores = new Dictionary<string, double>();
            foreach (var state in layoutStates)
            {
                // For now, use the full page score for all states as a placeholder
                // In production, this would filter visuals and recompute scores
                perStateScores[state.StateName] = result.CompositeScore;
            }
            
            result.PerStateScores = perStateScores;
            
            // Overall page score = average of all state scores
            var averageScore = BookmarkStateAnalyzer.ComputeAverageStateScore(perStateScores);
            _logger.LogInformation("[Bookmark State] Page '{Page}' average state score: {Score}", pageName, averageScore);
            
            recommendations.Add($"[Info] Report contains {bookmarks.Count} bookmarks with {layoutStates.Count} total layout states.");
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
                noVizFeedback[key] = [new(false, noVisualsMsg)];
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
            };
#pragma warning restore CS0618
        }

        // ── Compute all six frameworks for the full report ────────────────────
        var (gestaltScore, gestaltFeedback)          = ComputeGestaltScore(pages);
        var (cogLoadScore, cogLoadFeedback)          = ComputeCognitiveLoadScore(pages, recommendations, navigationScoring);
        var (dataInkScore, dataInkFeedback)          = ComputeDataInkScore(pages, recommendations, navigationScoring);
        var (accessibilityScore, a11yFeedback)       = ComputeAccessibilityScore(themeColors, recommendations);
        var (vbpScore, vbpFeedback)                  = ComputeVisualBestPracticesScore(pages, themeColors, recommendations);
        var (governanceScore, governanceFeedback)    = ComputeGovernanceScore(pages, config);
        var (fewScore, fewFeedback)                  = ComputeStephenFewScore(pages);
        var (tufteScore, tufteFeedback)              = ComputeTufteScore(pages);
        var (graphicalScore, graphicalFeedback)      = ComputeGraphicalPerceptionScore(pages);
        var (densityScore, densityFeedback)          = ComputeDashboardDensityScore(pages, navigationScoring);
        var (narrativeScore, narrativeFeedback)      = ComputeNarrativeDesignScore(pages);

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
                            ["gestalt"] = [new(false, "No data visuals on this page.")],
                            ["cognitiveLoad"] = [new(false, "No data visuals on this page.")],
                            ["dataInk"] = [new(false, "No data visuals on this page.")],
                            ["accessibility"] = [new(false, "No data visuals on this page.")],
                            ["visualBestPractices"] = [new(false, "No data visuals on this page.")],
                            ["governance"] = [new(false, "No data visuals on this page.")],
                            ["stephenFew"] = [new(false, "No data visuals on this page.")],
                            ["tufte"] = [new(false, "No data visuals on this page.")],
                            ["graphicalPerception"] = [new(false, "No data visuals on this page.")],
                            ["density"] = [new(false, "No data visuals on this page.")],
                            ["narrative"] = [new(false, "No data visuals on this page.")],
                        },
                        Recommendations = [],
                        FrameworkWeights = frameworkWeights,
                        DataVisualCount = pageComposition.DataVisualCount,
                        NavigationVisualCount = pageComposition.NavigationVisualCount,
                        HiddenVisualCount = pageComposition.HiddenVisualCount,
                    });
                    continue;
                }

                // Score this page
                var (pGestalt, pGestaltFeedback)          = ComputeGestaltScore(pageList);
                var (pCogLoad, pCogLoadFeedback)          = ComputeCognitiveLoadScore(pageList, new(), navigationScoring);
                var (pDataInk, pDataInkFeedback)          = ComputeDataInkScore(pageList, new(), navigationScoring);
                var (pAccessibility, pA11yFeedback)       = ComputeAccessibilityScore(themeColors, new());
                var (pVbp, pVbpFeedback)                  = ComputeVisualBestPracticesScore(pageList, themeColors, new());
                var (pGovernance, pGovernanceFeedback)    = ComputeGovernanceScore(pageList, config);
                var (pFew, pFewFeedback)                  = ComputeStephenFewScore(pageList);
                var (pTufte, pTufteFeedback)              = ComputeTufteScore(pageList);
                var (pGraphical, pGraphicalFeedback)      = ComputeGraphicalPerceptionScore(pageList);
                var (pDensity, pDensityFeedback)          = ComputeDashboardDensityScore(pageList, navigationScoring);
                var (pNarrative, pNarrativeFeedback)      = ComputeNarrativeDesignScore(pageList);

                var pagePageRecommendations = new List<string>();
                result.PageScores!.Add(new PageScore
                {
                    PageName = page.DisplayName,
                    GestaltScore = Clamp(pGestalt),
                    CognitiveLoadScore = Clamp(pCogLoad),
                    DataInkScore = Clamp(pDataInk),
                    AccessibilityScore = Clamp(pAccessibility),
                    VisualBestPracticesScore = Clamp(pVbp),
                    EnterpriseGovernanceScore = Clamp(pGovernance),
                    StephenFewScore = Clamp(pFew),
                    TufteScore = Clamp(pTufte),
                    GraphicalPerceptionScore = Clamp(pGraphical),
                    DensityScore = Clamp(pDensity),
                    NarrativeScore = Clamp(pNarrative),
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
                });

                _logger.LogDebug(
                    "[Scoring] Page '{Page}' — Composite: {Composite} (G={G} C={C} D={D} A={A} V={V} Gov={Gov} F={F})",
                    page.DisplayName,
                    result.PageScores[^1].CompositeScore,
                    Clamp(pGestalt), Clamp(pCogLoad), Clamp(pDataInk), Clamp(pAccessibility), Clamp(pVbp), Clamp(pGovernance), Clamp(pFew));
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

    // ── 1. Gestalt Principles score ──────────────────────────────────────────

    /// <summary>
    /// Scores four Gestalt sub-criteria: grid alignment (35 pts), figure/ground (30 pts),
    /// similarity (20 pts), and visual presence (15 pts).
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeGestaltScore(List<PageData> pages)
    {
        var feedback   = new List<FrameworkFeedbackItem>();
        var allVisuals = pages.SelectMany(p => p.Visuals).ToList();

        // Sub 1: Grid alignment (35 pts)
        int totalV = 0, alignedV = 0;
        var misalignedPages = new List<string>();
        var misalignedVisuals = new List<(string PageName, VisualData Visual)>();
        foreach (var page in pages)
        {
            foreach (var v in page.Visuals)
            {
                // Skip hidden visuals from alignment scoring
                if (v.IsHidden) continue;
                
                totalV++;
                if (IsNearMultiple(v.X, ColWidthPx) && IsNearMultiple(v.Y, RowHeightPx))
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
        double sub1 = alignPct * 0.35;
        feedback.Add(ScoredFeedback(
            misalignedPages.Count == 0,
            misalignedPages.Count == 0
                ? "Grid alignment: All visuals align to the 12-column grid — strong spatial organisation."
                : $"Grid alignment: {misalignedPages.Count} page(s) have off-grid visuals — snap to the 12-column grid for visual harmony.",
            sub1,
            35.0,
            misalignedPages.Count == 0 ? null : BuildAffectedVisuals(misalignedVisuals)));

        // Sub 2: Figure/ground contrast — has KPI/card AND at least one chart (30 pts)
        bool hasFigGround = allVisuals.Any(v => v.IsKpiCard) && allVisuals.Any(v => !v.IsKpiCard && !v.IsDecorative);
        double sub2 = hasFigGround ? 30.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasFigGround,
            hasFigGround
                ? "Figure/ground: KPI cards contrast with supporting charts — effective visual hierarchy."
                : "Figure/ground: Add at least one KPI/card visual alongside charts to create a clear focal point.",
            sub2,
            30.0));

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
            20.0));

        // Sub 4: Visual presence (15 pts)
        bool hasViz = allVisuals.Any(v => !v.IsDecorative);
        double sub4 = hasViz ? 15.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasViz,
            hasViz ? "Visual presence: Report contains data visuals."
                   : "Visual presence: No data visuals detected — add charts, tables, or KPI cards.",
            sub4,
            15.0));

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
                100.0));
            return (100.0, feedback);
        }

        double total = 0.0;
        var dense = new List<(PageData Page, VisualComposition Composition)>();
        var navHotspots = new List<(PageData Page, VisualComposition Composition)>();

        foreach (var page in pages)
        {
            var composition = BuildVisualComposition(page.Visuals, navigationScoring);
            double v = composition.WeightedVisibleCount;
            double s = v > 6 ? Math.Max(0, 100 - Math.Log2(v / 6.0) * 15) : 100;
            total    += s;
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
                denseVisuals));
        }
        else
        {
            feedback.Add(ScoredFeedback(
                true,
                "Visual density: All pages have ≤6 visuals — comfortable viewing density.",
                score,
                100.0));
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
            feedback.Add(new(false,
                $"Navigation complexity: '{hotspot.Page.DisplayName}' combines {hotspot.Composition.NavigationVisualCount} navigation control(s) with {hotspot.Composition.HiddenVisualCount} hidden visual(s) — this often signals an interaction-heavy layout that can be simplified.",
                hotspotVisuals));
        }
        else if (navigationScoring.Enabled)
        {
            feedback.Add(new(true,
                $"Navigation treatment: Navigation controls count at {navigationScoring.WeightPercent:F0}% of a standard visual in cognitive load scoring."));
        }

        // Add positive feedback for low-density pages
        int idealPages = pages.Count(p => BuildVisualComposition(p.Visuals, navigationScoring).WeightedVisibleCount <= 4);
        if (idealPages > 0)
        {
            feedback.Add(new(true,
                $"Optimal density: {idealPages} page(s) have ≤4 visuals — excellent focus."));
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
                100.0));
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
                100.0));
        }

        if (navigationScoring.Enabled)
        {
            feedback.Add(new(true,
                excludedNavigation > 0
                    ? $"Navigation treatment: {excludedNavigation} navigation visual(s) excluded from Data-Ink Ratio."
                    : "Navigation treatment: No navigation visuals were excluded from Data-Ink Ratio."));
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
            feedback.Add(new(false,
                $"Decorative types present: {string.Join(", ", decorativeTypes)} — consider removal to improve data-ink ratio."));
        }

        return (ratio * 100.0, feedback);
    }

    // ── 4. Accessibility score ────────────────────────────────────────────────

    /// <summary>
    /// % of theme data colours that pass WCAG 2.1 AA (≥4.5:1) against white.
    /// If no theme colours are available, returns 100 with no recommendation.
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeAccessibilityScore(
        List<string> themeColors, List<string> recs)
    {
        var feedback = new List<FrameworkFeedbackItem>();

        if (themeColors.Count == 0)
        {
            feedback.Add(ScoredFeedback(
                true,
                "WCAG 2.1 AA: No custom theme detected — using default Power BI theme colours.",
                100.0,
                100.0));
            return (100.0, feedback);
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

        if (failing.Count > 0)
        {
            recs.Add($"[High] Accessibility: {failing.Count} theme colour(s) fail WCAG 2.1 AA " +
                     $"contrast against white: {string.Join(", ", failing.Take(3))}" +
                     (failing.Count > 3 ? $" (+{failing.Count - 3} more)" : "") +
                     ". Update the report theme colours to replace them.");
            feedback.Add(ScoredFeedback(
                false,
                $"WCAG 2.1 AA: {failing.Count} colour(s) fail contrast ratio ≥4.5:1 against white: " +
                $"{string.Join(", ", failing.Take(3))}{(failing.Count > 3 ? $" and {failing.Count - 3} more" : "")}. " +
                "Update the report theme with accessible colours to fix.",
                (double)passing / themeColors.Count * 100.0,
                100.0));
        }
        else
        {
            feedback.Add(ScoredFeedback(
                true,
                $"WCAG 2.1 AA: All {passing} theme colour(s) pass contrast ratio ≥4.5:1 — accessible for most users.",
                100.0,
                100.0));
        }

        double score = (double)passing / themeColors.Count * 100.0;
        return (score, feedback);
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

        // Sub 1: Pie/donut avoidance (20 pts) — 0 pie→20, 1 pie→12, 2+→0
        int pieCount = allVisuals.Count(v => v.IsPieDonut);
        double sub1  = pieCount == 0 ? 20.0 : pieCount == 1 ? 12.0 : 0.0;
        feedback.Add(ScoredFeedback(
            pieCount == 0,
            pieCount == 0
                ? "Pie avoidance: No pie/donut charts — bar/column charts make comparisons easier."
                : $"Pie avoidance: {pieCount} pie/donut chart(s) detected — replace with bar or column charts for accurate comparison.",
            sub1,
            20.0));

        // Sub 2: Trend/comparison chart presence (20 pts)
        bool hasTrendOrComparison = allVisuals.Any(v => v.IsTrend || v.IsComparison);
        double sub2 = hasTrendOrComparison ? 20.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasTrendOrComparison,
            hasTrendOrComparison
                ? "Chart variety: Includes trend or comparison charts — supports analytical reasoning."
                : "Chart variety: Add at least one line or bar chart to enable trend or comparison analysis.",
            sub2,
            20.0));

        // Sub 3: Slicer presence (20 pts)
        bool hasSlicer = allVisuals.Any(v => v.IsSlicer);
        double sub3   = hasSlicer ? 20.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasSlicer,
            hasSlicer
                ? "Slicer: At least one slicer provides interactive filtering — good for exploration."
                : "Slicer: Add a slicer to allow filtering by dimension (date, region, product, etc.).",
            sub3,
            20.0));

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
                20.0));
        }
        else
        {
            feedback.Add(ScoredFeedback(
                true,
                $"Colour palette: {(c == 0 ? "Default" : c.ToString())} data colour(s) — palette is concise.",
                sub4,
                20.0));
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
            20.0));

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
            feedback.Add(new(false, "No pages found to evaluate against governance rules."));
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
            40.0));

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
            30.0));

        bool titleCompliant = !rules.RequirePageTitle || pages.All(page => !string.IsNullOrWhiteSpace(page.DisplayName));
        double sub3 = titleCompliant ? 30.0 : 0.0;
        feedback.Add(ScoredFeedback(
            titleCompliant,
            titleCompliant
                ? "Page title policy: All evaluated pages have titles, satisfying the configured governance rule."
                : "Page title policy: One or more pages are missing a title while titles are required by governance.",
            sub3,
            30.0));

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
            feedback.Add(new(false, "No pages found to evaluate against Stephen Few's guidelines."));
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
            30.0));

        // Sub 2: KPI prominence (25 pts)
        bool hasKpi = allVisuals.Any(v => v.IsKpiCard);
        double sub2 = hasKpi ? 25.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasKpi,
            hasKpi
                ? "KPI prominence: KPI/card visual(s) present — key metric immediately visible."
                : "KPI prominence: Add a KPI or card visual to make the report's headline metric immediately visible.",
            sub2,
            25.0));

        // Sub 3: Pie avoidance — strict (25 pts); Few: any pie = failure
        bool noPie  = !allVisuals.Any(v => v.IsPieDonut);
        double sub3 = noPie ? 25.0 : 0.0;
        feedback.Add(ScoredFeedback(
            noPie,
            noPie
                ? "Pie avoidance: No pie/donut charts — aligns with Stephen Few's strict guidance."
                : "Pie avoidance: Stephen Few strongly recommends replacing pie/donut charts with bar charts for accurate magnitude comparison.",
            sub3,
            25.0));

        // Sub 4: Contextual slicer (20 pts)
        bool hasSlicer = allVisuals.Any(v => v.IsSlicer);
        double sub4   = hasSlicer ? 20.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasSlicer,
            hasSlicer
                ? "Contextual slicer: Slicer present — provides context-switching for the audience."
                : "Contextual slicer: Few recommends filtering controls — add a slicer to support audience exploration.",
            sub4,
            20.0));

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
            feedback.Add(new(false, "No visuals found to evaluate against Tufte's minimalism principles."));
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
            40.0));

        // Sub 2: Pie/donut avoidance (30 pts) — Tufte considers radial slices poor for comparison
        bool noPie = !allVisuals.Any(v => v.IsPieDonut);
        double sub2 = noPie ? 30.0 : 0.0;
        feedback.Add(ScoredFeedback(
            noPie,
            noPie
                ? "Pie avoidance: No pie/donut charts — aligns with Tufte's guidance on precise quantitative comparison."
                : "Pie avoidance: Tufte recommends replacing pie/donut charts with bar or dot plots for more accurate visual comparison.",
            sub2,
            30.0));

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
            30.0));

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

        if (dataVisuals.Count == 0)
        {
            feedback.Add(new(false, "No data visuals found to evaluate against graphical perception principles."));
            return (0.0, feedback);
        }

        // Sub 1: Perceptually accurate encodings (40 pts)
        // Cleveland & McGill hierarchy: position > length > angle > area > color
        // Bar/column/line/scatter/waterfall use position or length — most accurate
        int positional = dataVisuals.Count(v => v.IsComparison || v.IsTrend ||
            v.Type is "scatterChart" or "barChart" or "columnChart" or "waterfallChart" or "funnel");
        double positionalRatio = (double)positional / dataVisuals.Count;
        double sub1 = positionalRatio * 40.0;
        feedback.Add(ScoredFeedback(
            positionalRatio >= 0.7,
            positionalRatio >= 0.7
                ? $"Perceptual accuracy: {positional}/{dataVisuals.Count} visuals use position/length encodings — high perceptual accuracy (Cleveland & McGill)."
                : $"Perceptual accuracy: {positional}/{dataVisuals.Count} visuals use position/length encodings — replace angle/area charts with bar or line charts for more accurate data perception.",
            sub1,
            40.0));

        // Sub 2: Pie/donut avoidance (35 pts)
        // Radial angle encodings rank lowest in the Cleveland & McGill perceptual hierarchy
        bool noPie  = !allVisuals.Any(v => v.IsPieDonut);
        double sub2 = noPie ? 35.0 : 0.0;
        feedback.Add(ScoredFeedback(
            noPie,
            noPie
                ? "Radial avoidance: No pie/donut charts — using bar/column charts provides more accurate quantitative comparisons."
                : "Radial avoidance: Pie/donut charts detected — replace with bar charts for more accurate quantitative judgment per the perceptual ranking.",
            sub2,
            35.0));

        // Sub 3: Comparison-optimised structure (25 pts)
        // Clustered bars/columns allow direct side-by-side comparison
        bool hasComparison = dataVisuals.Any(v => v.IsComparison);
        double sub3 = hasComparison ? 25.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasComparison,
            hasComparison
                ? "Comparative structure: Clustered bar/column charts present — enables direct side-by-side comparison."
                : "Comparative structure: Add a clustered bar or column chart to support direct comparative judgments between categories.",
            sub3,
            25.0));

        return (Clamp(sub1 + sub2 + sub3), feedback);
    }

    // ── 9. Dashboard Density score ──────────────────────────────────────────────

    /// <summary>
    /// Scores three Dashboard Density sub-criteria: optimal visual count per page (40 pts),
    /// content type diversity (30 pts), and navigation support via slicers (30 pts).
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeDashboardDensityScore(
        List<PageData> pages,
        NavigationScoringSettings navigationScoring)
    {
        var feedback = new List<FrameworkFeedbackItem>();

        if (pages.Count == 0)
        {
            feedback.Add(new(false, "No pages found to evaluate dashboard density."));
            return (0.0, feedback);
        }

        var allVisuals = pages.SelectMany(p => p.Visuals).Where(v => !v.IsHidden).ToList();
        var compositions = pages.Select(page => BuildVisualComposition(page.Visuals, navigationScoring)).ToList();

        // Sub 1: Optimal visual count per page (40 pts) — 3–8 weighted visuals per page is ideal
        double avgVisuals = compositions.Sum(composition => composition.WeightedVisibleCount) / pages.Count;
        bool optimalDensity = avgVisuals is >= 3.0 and <= 8.0;
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
        feedback.Add(ScoredFeedback(
            optimalDensity,
            densityMsg,
            sub1,
            40.0));

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
            30.0));

        // Sub 3: Navigation support (30 pts) — slicers help users navigate dense content
        bool hasSlicers = allVisuals.Any(v => v.IsSlicer);
        double sub3 = hasSlicers ? 30.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasSlicers,
            hasSlicers
                ? "Navigation support: Slicers present — interactive filters help users navigate and explore the dashboard efficiently."
                : "Navigation support: No slicers found — add at least one slicer to enable interactive filtering and improve dashboard navigation.",
            sub3,
            30.0));

        if (navigationScoring.Enabled)
        {
            var navigationVisualCount = compositions.Sum(composition => composition.NavigationVisualCount);
            feedback.Add(new(true,
                $"Navigation treatment: {navigationVisualCount} navigation visual(s) counted at {navigationScoring.WeightPercent:F0}% weight in dashboard density scoring."));
        }

        return (Clamp(sub1 + sub2 + sub3), feedback);
    }

    // ── 10. Narrative Design score ──────────────────────────────────────────────

    /// <summary>
    /// Scores three Narrative Design sub-criteria: headline metric (35 pts),
    /// temporal/trend context (35 pts), and comparative context (30 pts).
    /// </summary>
    private static (double score, List<FrameworkFeedbackItem> feedback) ComputeNarrativeDesignScore(List<PageData> pages)
    {
        var feedback   = new List<FrameworkFeedbackItem>();
        var allVisuals = pages.SelectMany(p => p.Visuals).Where(v => !v.IsHidden).ToList();

        if (allVisuals.Count == 0)
        {
            feedback.Add(new(false, "No visuals found to evaluate against narrative design principles."));
            return (0.0, feedback);
        }

        // Sub 1: Headline metric present (35 pts) — KPI/card establishes the primary outcome
        bool hasHeadline = allVisuals.Any(v => v.IsKpiCard);
        double sub1 = hasHeadline ? 35.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasHeadline,
            hasHeadline
                ? "Headline metric: KPI/card visual present — leads the narrative with the key performance outcome."
                : "Headline metric: Add a KPI or card visual to anchor the narrative and immediately communicate the most important outcome.",
            sub1,
            35.0));

        // Sub 2: Temporal context (35 pts) — trend chart gives the story arc over time
        bool hasTrend = allVisuals.Any(v => v.IsTrend);
        double sub2 = hasTrend ? 35.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasTrend,
            hasTrend
                ? "Temporal context: Line/area chart present — provides historical context and guides the reader through change over time."
                : "Temporal context: Add a line or area chart to provide temporal context and guide the reader through change over time.",
            sub2,
            35.0));

        // Sub 3: Comparative context (30 pts) — bar/column chart provides supporting categorisation
        bool hasComparison = allVisuals.Any(v => v.IsComparison);
        double sub3 = hasComparison ? 30.0 : 0.0;
        feedback.Add(ScoredFeedback(
            hasComparison,
            hasComparison
                ? "Comparative context: Bar/column chart present — supports the narrative with categorical comparisons."
                : "Comparative context: Add a bar or column chart to provide comparative evidence that supports the report's narrative.",
            sub3,
            30.0));

        return (Clamp(sub1 + sub2 + sub3), feedback);
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
                
                pages.Add(new PageData(displayName, visuals));
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

    private static List<VisualData> ParseVisuals(JsonObject pageJson)
    {
        var visuals = new List<VisualData>();

        if (pageJson["visuals"] is not JsonArray arr) return visuals;

        foreach (var item in arr)
        {
            if (item is not JsonObject vo) continue;
            bool isHidden = vo["isHidden"]?.GetValue<bool>() ?? false;
            visuals.Add(new VisualData(
                Id:   vo["id"]?.GetValue<string>()   ?? string.Empty,
                Type: vo["type"]?.GetValue<string>() ?? string.Empty,
                X:    TryDouble(vo, "x"),
                Y:    TryDouble(vo, "y"),
                W:    TryDouble(vo, "width"),
                H:    TryDouble(vo, "height"),
                IsHidden: isHidden));
        }

        return visuals;
    }
    
    /// <summary>
    /// Parses visuals from the visuals/ subdirectory within a page folder.
    /// This handles Power BI Desktop-authored reports where each visual is stored as visual.json.
    /// </summary>
    private static List<VisualData> ParseVisualsFromDirectory(string pageDir)
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
                
                visuals.Add(new VisualData(
                    Id:   visualName,
                    Type: visualType,
                    X:    x,
                    Y:    y,
                    W:    w,
                    H:    h,
                    IsHidden: isHidden));
            }
            catch (Exception ex)
            {
                // Skip malformed visuals and continue
                System.Diagnostics.Debug.WriteLine($"Warning: Could not parse visual at {visualJsonPath}: {ex.Message}");
            }
        }
        
        return visuals;
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

    private static FrameworkFeedbackItem ScoredFeedback(
        bool ok,
        string text,
        double earnedPoints,
        double possiblePoints,
        List<AffectedVisualReference>? affectedVisuals = null) =>
        new(
            ok,
            text,
            affectedVisuals,
            Math.Max(0.0, Math.Min(possiblePoints, earnedPoints)),
            possiblePoints);

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
        ["gestalt"] = 25,
        ["cognitiveLoad"] = 20,
        ["dataInk"] = 15,
        ["graphicalPerception"] = 0,
        ["accessibility"] = 15,
        ["visualBestPractices"] = 15,
        ["governance"] = 10,
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

    private sealed record PageData(string DisplayName, List<VisualData> Visuals);

    private sealed record VisualData(string Id, string Type, double X, double Y, double W, double H, bool IsHidden = false)
    {
        public bool IsSlicer     => Type is "slicer" or "advancedSlicerVisual";
        public bool IsKpiCard    => Type is "card" or "kpiVisual" or "multiRowCard";
        public bool IsPieDonut   => Type is "pieChart" or "donutChart";
        public bool IsTrend      => Type is "lineChart" or "areaChart" or "lineAndStackedColumnChart";
        public bool IsComparison => Type is "clusteredColumnChart" or "clusteredBarChart"
                                          or "stackedColumnChart" or "stackedBarChart";
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
}
