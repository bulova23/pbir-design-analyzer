using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PowerBIModelingService.Services.Pbir.Models;
using PowerBIModelingService.Services.Pbir.CrossPageNarrative;

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

    // White is used as the background reference for accessibility contrast checks.
    private const string BackgroundWhite = "#FFFFFF";

    private static readonly (string SemanticKey, string DisplayLabel, string[] Terms)[] _statusSemanticPatterns =
    [
        ("status:on-track", "On Track", ["on track", "healthy"]),
        ("status:at-risk", "At Risk", ["at risk", "warning"]),
        ("status:off-track", "Off Track", ["off track"]),
        ("status:critical", "Critical", ["critical"]),
        ("status:good", "Good", ["good"]),
        ("status:bad", "Bad", ["bad"]),
    ];

    private static readonly (string SemanticKey, string DisplayLabel, string[] Terms)[] _directSemanticPatterns =
    [
        ("role:actual", "Actual", ["actual"]),
        ("role:budget", "Budget", ["budget", "plan"]),
        ("role:target", "Target", ["target", "goal"]),
        ("role:forecast", "Forecast", ["forecast"]),
        ("period:current", "Current Year", ["current year", "cy"]),
        ("period:prior", "Prior Year", ["prior year", "previous year", "last year", "py"]),
        ("selection:selected", "Selected", ["selected"]),
        ("selection:unselected", "Unselected", ["unselected", "not selected"]),
    ];

    private static readonly Dictionary<string, string[]> _roleSemanticValueHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["region"] = ["north", "south", "east", "west", "central", "apac", "emea", "amer", "latam"],
        ["segment"] = ["consumer", "corporate", "enterprise", "commercial", "smb"],
        ["category"] = ["technology", "furniture", "software", "hardware", "services"],
        ["productcategory"] = ["technology", "furniture", "software", "hardware", "services"],
        ["scenario"] = ["actual", "budget", "target", "forecast"],
        ["version"] = ["actual", "budget", "target", "forecast"],
        ["period"] = ["current year", "prior year", "selected", "unselected"],
    };

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
    /// <param name="pageName">The exact page name to score (case-sensitive). Must match a page's stable PBIR name.</param>
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
        var reportFilters = ParseScopedFilterDefinitions(reportJson["reportFilters"], StoryFilterScope.Report, "report");
        var frameworkWeights = this.ExtractFrameworkWeights(config);  // Extract framework weights from config
        var navigationScoring = ExtractNavigationScoringSettings(config);

        // Find the requested page
        var page = allPages.FirstOrDefault(p => p.Name == pageName);
        if (page is null)
        {
            var availablePages = string.Join(", ", allPages.Select(p => $"'{p.Name}'"));
            var errorMsg = $"Page '{pageName}' not found. Available pages: {availablePages}";
            _logger.LogWarning("[Scoring] {Error}", errorMsg);
            
            throw new ArgumentException(errorMsg, nameof(pageName));
        }

        // Wrap page in a single-item list for framework methods
        var pageList = new List<PageData> { page };
        var pageVisuals = page.Visuals;
        var pageComposition = BuildVisualComposition(pageVisuals, navigationScoring);
        bool hasDataVisuals = pageComposition.DataVisualCount > 0;
        var storySignalRegistry = BuildStorySignalRegistry(page);
        var storyFilterTopologyAssessment = BuildStoryFilterTopologyAssessment(page, reportFilters);
        var storySpecialPageAssessment = BuildStorySpecialPageAssessment(page);
        var storyAssessmentArchetypeClassification = BuildStoryAssessmentArchetypeClassification(
            storySignalRegistry,
            storyFilterTopologyAssessment,
            storySpecialPageAssessment);
        var storySemanticCoherenceAssessment = BuildStorySemanticCoherenceAssessment(page, storySpecialPageAssessment);
        var storyGapAssessment = BuildStoryGapAssessment(
            storySignalRegistry,
            storyAssessmentArchetypeClassification,
            storySemanticCoherenceAssessment,
            storyFilterTopologyAssessment,
            storySpecialPageAssessment);
        var storyConfidenceBreakdownAssessment = BuildStoryConfidenceBreakdownAssessment(
            storySignalRegistry,
            storyAssessmentArchetypeClassification,
            storySemanticCoherenceAssessment,
            storyFilterTopologyAssessment,
            storyGapAssessment);
        var guidedStoryImprovements = BuildGuidedStoryImprovements(
            storyGapAssessment,
            storySpecialPageAssessment);
        var pageStorySummary = InferPageStorySummary(page);
        var pageIntentProfile = pageStorySummary is null ? null : BuildPageIntentProfileSummary(page, pageStorySummary);
        var actionabilityBreakdown = pageStorySummary is null || pageIntentProfile is null
            ? null
            : BuildActionabilityBreakdown(page, pageStorySummary, pageIntentProfile);
        var benchmarkComparison = pageStorySummary is null || pageIntentProfile is null || actionabilityBreakdown is null
            ? null
            : BuildBenchmarkComparison(page, pageStorySummary, pageIntentProfile, actionabilityBreakdown);

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
                ScoredPageId    = page.Name,
                ScoredPageName  = pageName,
                ScoredAt        = DateTimeOffset.UtcNow,
                FrameworkWeights = frameworkWeights,
                DataVisualCount = pageComposition.DataVisualCount,
                NavigationVisualCount = pageComposition.NavigationVisualCount,
                HiddenVisualCount = pageComposition.HiddenVisualCount,
                VisualMetadata = BuildPageVisualMetadataSummary(page),
                InferredStorySummary = pageStorySummary,
                PageIntentProfile = pageIntentProfile,
                ActionabilityBreakdown = actionabilityBreakdown,
                BenchmarkComparison = benchmarkComparison,
                GuidedStoryImprovements = guidedStoryImprovements,
                InternalStorySignalRegistry = storySignalRegistry,
                InternalStoryAssessmentArchetypeClassification = storyAssessmentArchetypeClassification,
                InternalStorySpecialPageAssessment = storySpecialPageAssessment,
                InternalStorySemanticCoherenceAssessment = storySemanticCoherenceAssessment,
                InternalStoryFilterTopologyAssessment = storyFilterTopologyAssessment,
                InternalStoryGapAssessment = storyGapAssessment,
                InternalStoryConfidenceBreakdownAssessment = storyConfidenceBreakdownAssessment,
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
            ScoredPageId    = page.Name,
            ScoredPageName  = pageName,
            ScoredAt        = DateTimeOffset.UtcNow,
            FrameworkWeights = frameworkWeights,
            DataVisualCount = pageComposition.DataVisualCount,
            NavigationVisualCount = pageComposition.NavigationVisualCount,
            HiddenVisualCount = pageComposition.HiddenVisualCount,
            VisualMetadata = BuildPageVisualMetadataSummary(page),
            InferredStorySummary = pageStorySummary,
            PageIntentProfile = pageIntentProfile,
            ActionabilityBreakdown = actionabilityBreakdown,
            BenchmarkComparison = benchmarkComparison,
            GuidedStoryImprovements = guidedStoryImprovements,
            InternalStorySignalRegistry = storySignalRegistry,
            InternalStoryAssessmentArchetypeClassification = storyAssessmentArchetypeClassification,
            InternalStorySpecialPageAssessment = storySpecialPageAssessment,
            InternalStorySemanticCoherenceAssessment = storySemanticCoherenceAssessment,
            InternalStoryFilterTopologyAssessment = storyFilterTopologyAssessment,
            InternalStoryGapAssessment = storyGapAssessment,
            InternalStoryConfidenceBreakdownAssessment = storyConfidenceBreakdownAssessment,
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
        var reportFilters = ParseScopedFilterDefinitions(reportJson["reportFilters"], StoryFilterScope.Report, "report");
        var reportConsistencyContext = BuildReportConsistencyContext(pages);
        var frameworkWeights = this.ExtractFrameworkWeights(config);  // Extract framework weights from config
        var navigationScoring = ExtractNavigationScoringSettings(config);
        var reportComposition = BuildVisualComposition(pages.SelectMany(p => p.Visuals), navigationScoring);
        bool hasDataVisuals = reportComposition.DataVisualCount > 0;
        var topLevelStorySignalRegistry = pages.Count == 1
            ? BuildStorySignalRegistry(pages[0], reportConsistencyContext?.Summary.Findings)
            : null;
        var topLevelStoryFilterTopologyAssessment = pages.Count == 1
            ? BuildStoryFilterTopologyAssessment(pages[0], reportFilters)
            : null;
        var topLevelStorySpecialPageAssessment = pages.Count == 1
            ? BuildStorySpecialPageAssessment(pages[0])
            : null;
        var topLevelStoryAssessmentArchetypeClassification = BuildStoryAssessmentArchetypeClassification(
            topLevelStorySignalRegistry,
            topLevelStoryFilterTopologyAssessment,
            topLevelStorySpecialPageAssessment);
        var topLevelStorySemanticCoherenceAssessment = pages.Count == 1
            ? BuildStorySemanticCoherenceAssessment(pages[0], topLevelStorySpecialPageAssessment)
            : null;
        var topLevelStoryGapAssessment = pages.Count == 1
            ? BuildStoryGapAssessment(
                topLevelStorySignalRegistry,
                topLevelStoryAssessmentArchetypeClassification,
                topLevelStorySemanticCoherenceAssessment,
                topLevelStoryFilterTopologyAssessment,
                topLevelStorySpecialPageAssessment)
            : null;
        var topLevelStoryConfidenceBreakdownAssessment = pages.Count == 1
            ? BuildStoryConfidenceBreakdownAssessment(
                topLevelStorySignalRegistry,
                topLevelStoryAssessmentArchetypeClassification,
                topLevelStorySemanticCoherenceAssessment,
                topLevelStoryFilterTopologyAssessment,
                topLevelStoryGapAssessment)
            : null;
        var topLevelGuidedStoryImprovements = pages.Count == 1
            ? BuildGuidedStoryImprovements(
                topLevelStoryGapAssessment,
                topLevelStorySpecialPageAssessment)
            : new GuidedStoryImprovements();

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
                InferredStorySummary = pages.Count == 1 ? InferPageStorySummary(pages[0]) : null,
                PageIntentProfile = pages.Count == 1 && InferPageStorySummary(pages[0]) is { } zeroStorySummary
                    ? BuildPageIntentProfileSummary(pages[0], zeroStorySummary)
                    : null,
                ActionabilityBreakdown = pages.Count == 1 && InferPageStorySummary(pages[0]) is { } zeroActionStory
                    ? BuildActionabilityBreakdown(
                        pages[0],
                        zeroActionStory,
                        BuildPageIntentProfileSummary(pages[0], zeroActionStory))
                    : null,
                BenchmarkComparison = pages.Count == 1 && InferPageStorySummary(pages[0]) is { } zeroBenchmarkStory
                    ? BuildBenchmarkComparison(
                        pages[0],
                        zeroBenchmarkStory,
                        BuildPageIntentProfileSummary(pages[0], zeroBenchmarkStory),
                        BuildActionabilityBreakdown(
                            pages[0],
                            zeroBenchmarkStory,
                            BuildPageIntentProfileSummary(pages[0], zeroBenchmarkStory)))
                    : null,
                GuidedStoryImprovements = topLevelGuidedStoryImprovements,
                ReportConsistencySummary = reportConsistencyContext?.Summary,
                InternalStorySignalRegistry = topLevelStorySignalRegistry,
                InternalStoryAssessmentArchetypeClassification = topLevelStoryAssessmentArchetypeClassification,
                InternalStorySpecialPageAssessment = topLevelStorySpecialPageAssessment,
                InternalStorySemanticCoherenceAssessment = topLevelStorySemanticCoherenceAssessment,
                InternalStoryFilterTopologyAssessment = topLevelStoryFilterTopologyAssessment,
                InternalStoryGapAssessment = topLevelStoryGapAssessment,
                InternalStoryConfidenceBreakdownAssessment = topLevelStoryConfidenceBreakdownAssessment,
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
                InferredStorySummary = pages.Count == 1 ? InferPageStorySummary(pages[0]) : null,
                PageIntentProfile = pages.Count == 1 && InferPageStorySummary(pages[0]) is { } singleStorySummary
                    ? BuildPageIntentProfileSummary(pages[0], singleStorySummary)
                    : null,
                ActionabilityBreakdown = pages.Count == 1 && InferPageStorySummary(pages[0]) is { } singleActionStory
                    ? BuildActionabilityBreakdown(
                        pages[0],
                        singleActionStory,
                        BuildPageIntentProfileSummary(pages[0], singleActionStory))
                    : null,
                BenchmarkComparison = pages.Count == 1 && InferPageStorySummary(pages[0]) is { } singleBenchmarkStory
                    ? BuildBenchmarkComparison(
                        pages[0],
                        singleBenchmarkStory,
                        BuildPageIntentProfileSummary(pages[0], singleBenchmarkStory),
                        BuildActionabilityBreakdown(
                            pages[0],
                            singleBenchmarkStory,
                            BuildPageIntentProfileSummary(pages[0], singleBenchmarkStory)))
                    : null,
                GuidedStoryImprovements = topLevelGuidedStoryImprovements,
                ReportConsistencySummary = reportConsistencyContext?.Summary,
                InternalStorySignalRegistry = topLevelStorySignalRegistry,
                InternalStoryAssessmentArchetypeClassification = topLevelStoryAssessmentArchetypeClassification,
                InternalStorySpecialPageAssessment = topLevelStorySpecialPageAssessment,
                InternalStorySemanticCoherenceAssessment = topLevelStorySemanticCoherenceAssessment,
                InternalStoryFilterTopologyAssessment = topLevelStoryFilterTopologyAssessment,
                InternalStoryGapAssessment = topLevelStoryGapAssessment,
                InternalStoryConfidenceBreakdownAssessment = topLevelStoryConfidenceBreakdownAssessment,
        };
#pragma warning restore CS0618

        // ── Compute per-page breakdown (parallel) ─────────────────────────────
        // Each page is scored independently: framework methods are pure (the only mutation is to
        // a per-iteration recommendations list) and the only shared writes are PageScores and
        // ScoringErrors. Collect into thread-safe structures and re-order at the end so the
        // display order matches the original page order.
        var concurrentPageScores = new System.Collections.Concurrent.ConcurrentBag<(int Index, PageScore Score)>();
        var concurrentScoringErrors = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        Parallel.ForEach(
            pages.Select((page, index) => (page, index)),
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            tuple =>
            {
                var page = tuple.page;
                var pageIndex = tuple.index;
                try
                {
                    var pageList = new List<PageData> { page };
                    var pageComposition = BuildVisualComposition(page.Visuals, navigationScoring);
                    var pageHasDataVisuals = pageComposition.DataVisualCount > 0;

                    if (!pageHasDataVisuals)
                    {
                        var emptyPageConsistencyNotes = BuildReportConsistencyNotes(page.DisplayName, reportConsistencyContext);
                        var emptyStorySummary = InferPageStorySummary(page, emptyPageConsistencyNotes);
                        var emptyIntentProfile = emptyStorySummary is null ? null : BuildPageIntentProfileSummary(page, emptyStorySummary);
                        var emptyActionability = emptyStorySummary is null || emptyIntentProfile is null
                            ? null
                            : BuildActionabilityBreakdown(page, emptyStorySummary, emptyIntentProfile);
                        var emptyBenchmark = emptyStorySummary is null || emptyIntentProfile is null || emptyActionability is null
                            ? null
                            : BuildBenchmarkComparison(page, emptyStorySummary, emptyIntentProfile, emptyActionability);
                        var emptyStorySignalRegistry = BuildStorySignalRegistry(page, emptyPageConsistencyNotes);
                        var emptyStoryFilterTopologyAssessment = BuildStoryFilterTopologyAssessment(page, reportFilters);
                        var emptyStorySpecialPageAssessment = BuildStorySpecialPageAssessment(page);
                        var emptyStoryAssessmentArchetypeClassification = BuildStoryAssessmentArchetypeClassification(
                            emptyStorySignalRegistry,
                            emptyStoryFilterTopologyAssessment,
                            emptyStorySpecialPageAssessment);
                        var emptyStorySemanticCoherenceAssessment = BuildStorySemanticCoherenceAssessment(page, emptyStorySpecialPageAssessment);
                        var emptyStoryGapAssessment = BuildStoryGapAssessment(
                            emptyStorySignalRegistry,
                            emptyStoryAssessmentArchetypeClassification,
                            emptyStorySemanticCoherenceAssessment,
                            emptyStoryFilterTopologyAssessment,
                            emptyStorySpecialPageAssessment);
                        var emptyStoryConfidenceBreakdownAssessment = BuildStoryConfidenceBreakdownAssessment(
                            emptyStorySignalRegistry,
                            emptyStoryAssessmentArchetypeClassification,
                            emptyStorySemanticCoherenceAssessment,
                            emptyStoryFilterTopologyAssessment,
                            emptyStoryGapAssessment);
                        var emptyGuidedStoryImprovements = BuildGuidedStoryImprovements(
                            emptyStoryGapAssessment,
                            emptyStorySpecialPageAssessment);
                        concurrentPageScores.Add((pageIndex, new PageScore
                        {
                            PageId = page.Name,
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
                            ReportConsistencyNotes = emptyPageConsistencyNotes,
                            InferredStorySummary = emptyStorySummary,
                            PageIntentProfile = emptyIntentProfile,
                            ActionabilityBreakdown = emptyActionability,
                            BenchmarkComparison = emptyBenchmark,
                            GuidedStoryImprovements = emptyGuidedStoryImprovements,
                            InternalStorySignalRegistry = emptyStorySignalRegistry,
                            InternalStoryAssessmentArchetypeClassification = emptyStoryAssessmentArchetypeClassification,
                            InternalStorySpecialPageAssessment = emptyStorySpecialPageAssessment,
                            InternalStorySemanticCoherenceAssessment = emptyStorySemanticCoherenceAssessment,
                            InternalStoryFilterTopologyAssessment = emptyStoryFilterTopologyAssessment,
                            InternalStoryGapAssessment = emptyStoryGapAssessment,
                            InternalStoryConfidenceBreakdownAssessment = emptyStoryConfidenceBreakdownAssessment,
                        }));
                        return;
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

                    var reportConsistencyNotes = BuildReportConsistencyNotes(page.DisplayName, reportConsistencyContext);
                    var pageStorySummary = InferPageStorySummary(page, reportConsistencyNotes);
                    var pageIntentProfile = pageStorySummary is null ? null : BuildPageIntentProfileSummary(page, pageStorySummary);
                    var actionabilityBreakdown = pageStorySummary is null || pageIntentProfile is null
                        ? null
                        : BuildActionabilityBreakdown(page, pageStorySummary, pageIntentProfile);
                    var benchmarkComparison = pageStorySummary is null || pageIntentProfile is null || actionabilityBreakdown is null
                        ? null
                        : BuildBenchmarkComparison(page, pageStorySummary, pageIntentProfile, actionabilityBreakdown);
                    var storySignalRegistry = BuildStorySignalRegistry(page, reportConsistencyNotes);
                    var storyFilterTopologyAssessment = BuildStoryFilterTopologyAssessment(page, reportFilters);
                    var storySpecialPageAssessment = BuildStorySpecialPageAssessment(page);
                    var storyAssessmentArchetypeClassification = BuildStoryAssessmentArchetypeClassification(
                        storySignalRegistry,
                        storyFilterTopologyAssessment,
                        storySpecialPageAssessment);
                    var storySemanticCoherenceAssessment = BuildStorySemanticCoherenceAssessment(page, storySpecialPageAssessment);
                    var storyGapAssessment = BuildStoryGapAssessment(
                        storySignalRegistry,
                        storyAssessmentArchetypeClassification,
                        storySemanticCoherenceAssessment,
                        storyFilterTopologyAssessment,
                        storySpecialPageAssessment);
                    var storyConfidenceBreakdownAssessment = BuildStoryConfidenceBreakdownAssessment(
                        storySignalRegistry,
                        storyAssessmentArchetypeClassification,
                        storySemanticCoherenceAssessment,
                        storyFilterTopologyAssessment,
                        storyGapAssessment);
                    var guidedStoryImprovements = BuildGuidedStoryImprovements(
                        storyGapAssessment,
                        storySpecialPageAssessment);
                    var pageScore = new PageScore
                    {
                        PageId = page.Name,
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
                        ReportConsistencyNotes = reportConsistencyNotes,
                        InferredStorySummary = pageStorySummary,
                        PageIntentProfile = pageIntentProfile,
                        ActionabilityBreakdown = actionabilityBreakdown,
                        BenchmarkComparison = benchmarkComparison,
                        GuidedStoryImprovements = guidedStoryImprovements,
                        InternalStorySignalRegistry = storySignalRegistry,
                        InternalStoryAssessmentArchetypeClassification = storyAssessmentArchetypeClassification,
                        InternalStorySpecialPageAssessment = storySpecialPageAssessment,
                        InternalStorySemanticCoherenceAssessment = storySemanticCoherenceAssessment,
                        InternalStoryFilterTopologyAssessment = storyFilterTopologyAssessment,
                        InternalStoryGapAssessment = storyGapAssessment,
                        InternalStoryConfidenceBreakdownAssessment = storyConfidenceBreakdownAssessment,
                        PerStateScores = pageOverlay?.PerStateScores,
                    };
                    concurrentPageScores.Add((pageIndex, pageScore));

                    _logger.LogDebug(
                        "[Scoring] Page '{Page}' — Composite: {Composite} (G={G} C={C} D={D} A={A} V={V} Gov={Gov} F={F})",
                        page.DisplayName,
                        pageScore.CompositeScore,
                        finalGestalt, finalCogLoad, finalDataInk, finalAccessibility, finalVbp, finalGovernance, finalFew);
                }
                catch (Exception ex)
                {
                    var errorMsg = $"Failed to score page '{page.DisplayName}': {ex.Message}";
                    _logger.LogWarning("[Scoring] {Error}", errorMsg);
                    concurrentScoringErrors[page.DisplayName] = errorMsg;
                }
            });

        // Restore the original page order before exposing PageScores.
        foreach (var entry in concurrentPageScores.OrderBy(item => item.Index))
        {
            result.PageScores!.Add(entry.Score);
        }
        foreach (var entry in concurrentScoringErrors)
        {
            result.ScoringErrors[entry.Key] = entry.Value;
        }

        result.InternalCrossPageNarrativeAssessment = CrossPageNarrativeAssessmentBuilder.Build(result.PageScores);

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
        total -= AddSemanticStatusAccessibilityFeedback(feedback, recs, pages);

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

        var semanticColorPenalty = AddSemanticColorConsistencyFeedback(feedback, recs, pages);
        AddSurfaceTreatmentFeedback(feedback, pages);

        return (Clamp(sub1 + sub2 + sub3 + sub4 + sub5 - semanticColorPenalty), feedback);
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
                    Name = pageJson["name"]?.GetValue<string>() ?? Path.GetFileName(pageDir),
                    DisplayName = displayName,
                    Visuals = visuals,
                    Canvas = ParseCanvasMetadata(pageJson),
                    PageFilters = ParseScopedFilterDefinitions(
                        pageJson["pageFilters"] ?? pageJson["filterConfig"]?["filters"],
                        StoryFilterScope.Page,
                        displayName),
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
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
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

        return OrderVisualsDeterministically(visuals);
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
        
        foreach (var visualDir in Directory.GetDirectories(visualsDir)
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal))
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
        
        return OrderVisualsDeterministically(visuals);
    }

    private static List<VisualData> OrderVisualsDeterministically(IEnumerable<VisualData> visuals)
    {
        return visuals
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ThenBy(visual => visual.W)
            .ThenBy(visual => visual.H)
            .ThenBy(visual => visual.Id, StringComparer.Ordinal)
            .ThenBy(visual => visual.Type, StringComparer.Ordinal)
            .ToList();
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

    private static List<FilterDefinitionData> ParseScopedFilterDefinitions(
        JsonNode? filtersNode,
        StoryFilterScope scope,
        string sourcePrefix)
    {
        if (filtersNode is not JsonArray filtersArray)
        {
            return [];
        }

        var definitions = new List<FilterDefinitionData>();
        for (int index = 0; index < filtersArray.Count; index++)
        {
            var filterNode = filtersArray[index];
            if (filterNode is not JsonObject filterObject)
            {
                definitions.Add(new FilterDefinitionData(
                    SourceId: $"{sourcePrefix}-{index + 1}",
                    Scope: scope,
                    DisplayLabel: $"{scope} filter {index + 1}",
                    FieldHints: Array.Empty<string>(),
                    HierarchyPattern: null,
                    HierarchyDepth: 0,
                    FilterType: null,
                    PlacementZone: null,
                    IsMalformed: true));
                continue;
            }

            var fieldHints = ReadStringValues(filterObject["field"])
                .Concat(ReadStringValues(filterObject["fields"]))
                .Concat(ReadStringValues(filterObject["target"]))
                .Concat(ReadStringValues(filterObject["column"]))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var hierarchyLevels = FindValuesRecursive(filterObject, (IReadOnlyList<string>)["hierarchy"])
                .SelectMany(ReadStringValues)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var hierarchyPattern = hierarchyLevels.Count >= 2
                ? string.Join(" > ", hierarchyLevels)
                : null;
            var displayLabel = FirstNonBlank(
                ReadFirstString(filterObject, ["displayName", "label", "name", "title"]),
                fieldHints.FirstOrDefault(),
                $"{scope} filter {index + 1}")!;
            var filterType = ReadFirstString(filterObject, ["filterType", "type", "mode"]);

            definitions.Add(new FilterDefinitionData(
                SourceId: $"{sourcePrefix}-{index + 1}",
                Scope: scope,
                DisplayLabel: displayLabel,
                FieldHints: fieldHints,
                HierarchyPattern: hierarchyPattern,
                HierarchyDepth: hierarchyLevels.Count > 0 ? hierarchyLevels.Count : InferHierarchyDepth(fieldHints, hierarchyPattern),
                FilterType: filterType,
                PlacementZone: null,
                IsMalformed: fieldHints.Count == 0 && string.IsNullOrWhiteSpace(filterType)));
        }

        return definitions;
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
            Filter = ParseVisualFilterTopologyMetadata(visualJson, visualId, sourceContext),
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

    private FilterTopologyMetadata ParseVisualFilterTopologyMetadata(
        JsonObject visualJson,
        string visualId,
        string sourceContext)
    {
        return TryParseVisualComponent(
            visualId,
            "filter topology",
            sourceContext,
            () =>
            {
                var fieldHints = CollectRoleHints(
                    visualJson,
                    ["fieldRoles", "roles", "projections", "filter", "slicer", "field"],
                    ["category", "categories", "field", "fields", "column", "columns"]);
                var hierarchyLevels = FindValuesRecursive(visualJson, (IReadOnlyList<string>)["hierarchy"])
                    .SelectMany(ReadStringValues)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var hierarchyPattern = hierarchyLevels.Count >= 2
                    ? string.Join(" > ", hierarchyLevels)
                    : fieldHints.FirstOrDefault(hint =>
                        hint.Contains("hierarchy", StringComparison.OrdinalIgnoreCase) ||
                        hint.Contains("year", StringComparison.OrdinalIgnoreCase) ||
                        hint.Contains("quarter", StringComparison.OrdinalIgnoreCase) ||
                        hint.Contains("month", StringComparison.OrdinalIgnoreCase));
                var filterType = FirstNonBlank(
                    ReadStringNode(visualJson["filterType"]),
                    ReadFirstString(visualJson, ["mode", "selectionMode", "type"]));

                return new FilterTopologyMetadata(
                    FieldHints: fieldHints,
                    HierarchyPattern: hierarchyPattern,
                    HierarchyDepth: hierarchyLevels.Count > 0 ? hierarchyLevels.Count : InferHierarchyDepth(fieldHints, hierarchyPattern),
                    FilterType: filterType);
            },
            FilterTopologyMetadata.Empty);
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
        var semanticColorAssignments = ExtractSemanticColorAssignments(page);
        var semanticAssignmentsByVisualId = semanticColorAssignments
            .GroupBy(assignment => assignment.SourceVisualId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SemanticColorAssignment>)group.ToList(),
                StringComparer.OrdinalIgnoreCase);
        var chartIntentByVisualId = orderedVisuals
            .ToDictionary(
                visual => visual.Id,
                visual => InferChartIntent(visual, page),
                StringComparer.OrdinalIgnoreCase);

        return new PageVisualMetadataSummary
        {
            PageName = page.DisplayName,
            VisiblePageTitle = GetPageVisibleTitle(page),
            StrictVisiblePageTitle = GetStrictVisibleTitleText(page),
            CanvasWidth = page.Canvas?.Width,
            CanvasHeight = page.Canvas?.Height,
            SemanticColorMap = semanticColorAssignments,
            ChartIntentSummary = BuildPageChartIntentSummary(page, orderedVisuals, chartIntentByVisualId),
            VisualCount = orderedVisuals.Count,
            VisibleTitleVisualCount = visibleVisuals.Count(visual => visual.HasVisibleTitleIntent),
            TextVisualCount = visibleVisuals.Count(visual => IsTextVisualType(visual.Type)),
            SlicerCount = visibleVisuals.Count(visual => visual.IsSlicer),
            LegendVisualCount = visibleVisuals.Count(visual => visual.Labels.HasLegend == true),
            AxisLabelVisualCount = visibleVisuals.Count(visual => visual.Labels.HasAxisLabels == true),
            DataLabelVisualCount = visibleVisuals.Count(visual => visual.Labels.HasDataLabels == true),
            FormattedVisualCount = visibleVisuals.Count(HasAnyFormattingMetadata),
            Visuals = orderedVisuals
                .Select(visual => BuildVisualMetadataItem(
                    visual,
                    semanticAssignmentsByVisualId.TryGetValue(visual.Id, out var assignments)
                        ? assignments
                        : [],
                    chartIntentByVisualId.TryGetValue(visual.Id, out var chartIntent)
                        ? chartIntent
                        : null))
                .ToList(),
        };
    }

    private static VisualMetadataItem BuildVisualMetadataItem(
        VisualData visual,
        IReadOnlyList<SemanticColorAssignment> semanticAssignments,
        ChartIntentSummary? chartIntent) => new()
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
        SemanticColors = semanticAssignments.ToList(),
        ChartIntent = chartIntent,
    };

    private static List<SemanticColorAssignment> ExtractSemanticColorAssignments(PageData page)
    {
        var assignments = new List<SemanticColorAssignment>();

        foreach (var visual in page.Visuals.Where(visual => !visual.IsHidden && !visual.IsNavigationElement && !visual.IsDecorative))
        {
            var signalColor = FirstNonBlank(
                TryNormalizeHex(visual.Formatting.FontColor),
                TryNormalizeHex(visual.Formatting.BackgroundFillColor));
            if (string.IsNullOrWhiteSpace(signalColor))
            {
                continue;
            }

            foreach (var descriptor in InferSemanticDescriptors(visual))
            {
                assignments.Add(new SemanticColorAssignment
                {
                    SemanticKey = descriptor.SemanticKey,
                    DisplayLabel = descriptor.DisplayLabel,
                    Color = signalColor!,
                    SourceVisualId = visual.Id,
                    SourcePageName = page.DisplayName,
                });
            }
        }

        return assignments;
    }

    private static List<(string SemanticKey, string DisplayLabel)> InferSemanticDescriptors(VisualData visual)
    {
        var descriptors = new List<(string SemanticKey, string DisplayLabel)>();
        var text = visual.BestVisibleText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return descriptors;
        }

        var statusDescriptor = TryInferStatusSemanticDescriptor(text);
        if (statusDescriptor is not null)
        {
            descriptors.Add(statusDescriptor.Value);
        }

        var directDescriptor = TryInferDirectSemanticDescriptor(text);
        if (directDescriptor is not null &&
            !descriptors.Any(existing => existing.SemanticKey.Equals(directDescriptor.Value.SemanticKey, StringComparison.OrdinalIgnoreCase)))
        {
            descriptors.Add(directDescriptor.Value);
        }

        var roleHints = visual.FieldRoles.CategoryHints
            .Concat(visual.FieldRoles.SeriesHints)
            .Concat(visual.FieldRoles.MeasureHints)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var roleHint in roleHints)
        {
            var semanticKey = TryInferSemanticKey(visual, roleHint);
            if (string.IsNullOrWhiteSpace(semanticKey))
            {
                continue;
            }

            var displayLabel = TryInferSemanticDisplayLabel(text!, roleHint);
            if (string.IsNullOrWhiteSpace(displayLabel))
            {
                continue;
            }

            if (!descriptors.Any(existing => existing.SemanticKey.Equals(semanticKey, StringComparison.OrdinalIgnoreCase)))
            {
                descriptors.Add((semanticKey, displayLabel!));
            }
        }

        return descriptors;
    }

    private static (string SemanticKey, string DisplayLabel)? TryInferStatusSemanticDescriptor(string text)
    {
        foreach (var (semanticKey, displayLabel, terms) in _statusSemanticPatterns)
        {
            if (terms.Any(term => TextContainsPhrase(text, term)))
            {
                return (semanticKey, displayLabel);
            }
        }

        return null;
    }

    private static (string SemanticKey, string DisplayLabel)? TryInferDirectSemanticDescriptor(string text)
    {
        foreach (var (semanticKey, displayLabel, terms) in _directSemanticPatterns)
        {
            if (terms.Any(term => TextContainsPhrase(text, term)))
            {
                return (semanticKey, displayLabel);
            }
        }

        return null;
    }

    private static string? TryInferSemanticKey(VisualData visual, string roleHint)
    {
        var text = visual.BestVisibleText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalizedRoleHint = NormalizeSemanticKey(roleHint);
        if (!_roleSemanticValueHints.TryGetValue(normalizedRoleHint.Replace("-", string.Empty, StringComparison.Ordinal), out var candidateValues))
        {
            return null;
        }

        var matchedValue = candidateValues.FirstOrDefault(candidate => TextContainsPhrase(text!, candidate));
        return matchedValue is null
            ? null
            : NormalizeSemanticKey($"{normalizedRoleHint}:{matchedValue}");
    }

    private static string? TryInferSemanticDisplayLabel(string text, string roleHint)
    {
        var normalizedRoleHint = NormalizeSemanticKey(roleHint);
        if (!_roleSemanticValueHints.TryGetValue(normalizedRoleHint.Replace("-", string.Empty, StringComparison.Ordinal), out var candidateValues))
        {
            return null;
        }

        var matchedValue = candidateValues.FirstOrDefault(candidate => TextContainsPhrase(text, candidate));
        return matchedValue is null
            ? null
            : string.Join(' ', matchedValue.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ToTitleToken));
    }

    private static string NormalizeSemanticKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var normalized = raw.Trim().ToLowerInvariant();
        normalized = normalized.Replace("_", "-", StringComparison.Ordinal);
        normalized = Regex.Replace(normalized, @"\s+", "-");
        normalized = Regex.Replace(normalized, @"[^a-z0-9:\-]", string.Empty);
        normalized = Regex.Replace(normalized, @"-+", "-");
        return normalized.Trim('-');
    }

    private static bool TextContainsPhrase(string text, string phrase) =>
        Regex.IsMatch(
            text,
            $@"\b{Regex.Escape(phrase)}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static string ToTitleToken(string token) =>
        string.IsNullOrWhiteSpace(token)
            ? string.Empty
            : char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant();

    private static ChartIntentSummary? InferChartIntent(VisualData visual, PageData page)
    {
        if (visual.IsHidden || visual.IsNavigationElement || visual.IsDecorative || visual.IsSlicer)
        {
            return null;
        }

        var intent = ClassifyAnalyticalTask(
            visual.Type,
            visual.FieldRoles.CategoryHints,
            visual.FieldRoles.SeriesHints,
            visual.FieldRoles.MeasureHints,
            visual.BestVisibleText);
        if (string.IsNullOrWhiteSpace(intent))
        {
            return null;
        }

        var evidence = InferChartIntentEvidence(visual, page);
        var fitStatus = "good";
        var recommendedAlternatives = new List<string>();
        if (IsLineLikeVisual(visual) && HasExplicitCategoricalContext(visual) && !HasSequentialContext(visual, page))
        {
            fitStatus = "weak";
            recommendedAlternatives.AddRange(["clusteredColumnChart", "clusteredBarChart"]);
            intent = "comparison";
        }
        else if (IsFunnelVisual(visual) && !HasFunnelContext(visual, page))
        {
            fitStatus = "weak";
            recommendedAlternatives.AddRange(["clusteredBarChart", "waterfallChart"]);
        }

        return new ChartIntentSummary
        {
            Intent = intent,
            Confidence = InferChartIntentConfidence(visual, evidence),
            Evidence = evidence,
            FitStatus = fitStatus,
            RecommendedAlternatives = recommendedAlternatives,
        };
    }

    private static ChartIntentSummary? BuildPageChartIntentSummary(
        PageData page,
        IReadOnlyList<VisualData> orderedVisuals,
        IReadOnlyDictionary<string, ChartIntentSummary?> chartIntentByVisualId)
    {
        var visualIntents = orderedVisuals
            .Where(visual => !visual.IsHidden && !visual.IsNavigationElement && !visual.IsDecorative && !visual.IsSlicer)
            .Select(visual => chartIntentByVisualId.TryGetValue(visual.Id, out var intent) ? intent : null)
            .Where(intent => intent is not null)
            .Cast<ChartIntentSummary>()
            .ToList();
        if (visualIntents.Count == 0)
        {
            return null;
        }

        var dominantIntent = visualIntents
            .GroupBy(intent => intent.Intent, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .First();
        var leadIntent = visualIntents.First(intent => intent.Intent.Equals(dominantIntent.Key, StringComparison.OrdinalIgnoreCase));

        return new ChartIntentSummary
        {
            Intent = dominantIntent.Key,
            Confidence = visualIntents.Count == 1 || dominantIntent.Count() > visualIntents.Count / 2 ? "high" : "medium",
            Evidence = leadIntent.Evidence,
            FitStatus = leadIntent.FitStatus,
            RecommendedAlternatives = leadIntent.RecommendedAlternatives,
        };
    }

    private static PageStorySummary? InferPageStorySummary(
        PageData page,
        IReadOnlyList<string>? reportConsistencyNotes = null)
    {
        var analysis = AnalyzeNarrativePage(page);
        if (analysis.VisibleDataVisuals.Count == 0)
        {
            return null;
        }

        var visibleDataVisuals = analysis.VisibleDataVisuals
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ToList();
        var leadVisual = analysis.SupportingDataVisuals
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .FirstOrDefault()
            ?? visibleDataVisuals.FirstOrDefault();
        var leadIntent = leadVisual is null ? null : InferChartIntent(leadVisual, page);
        var visualIntents = visibleDataVisuals
            .Select(visual => InferChartIntent(visual, page))
            .Where(intent => intent is not null)
            .Cast<ChartIntentSummary>()
            .ToList();
        var dominantIntent = visualIntents
            .GroupBy(intent => intent.Intent, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Key)
            .FirstOrDefault();

        var intentProfile = InferPageIntentProfile(analysis, dominantIntent);
        var storyArchetype = BuildStoryArchetype(intentProfile, analysis, visualIntents);
        var semanticSignals = AnalyzeSemanticNarrativeSignals(analysis, leadVisual);
        var primaryMetric = semanticSignals.PrimaryMetric;
        var primaryDimension = semanticSignals.PrimaryDimension;
        var confidence = InferStoryConfidence(analysis, leadIntent, semanticSignals, reportConsistencyNotes);
        var evidence = BuildStoryEvidence(analysis, leadVisual, leadIntent, semanticSignals, reportConsistencyNotes);

        return new PageStorySummary
        {
            IntentProfile = intentProfile,
            StoryArchetype = storyArchetype,
            InferredStory = BuildStoryHypothesis(intentProfile, primaryMetric, primaryDimension, analysis, visualIntents),
            Confidence = confidence,
            Evidence = evidence,
        };
    }

    private static PageIntentProfileSummary BuildPageIntentProfileSummary(
        PageData page,
        PageStorySummary storySummary)
    {
        var inferredProfile = NormalizeIntentProfile(storySummary.IntentProfile);
        return new PageIntentProfileSummary
        {
            InferredProfile = inferredProfile,
            ActionabilityExpectation = inferredProfile is "executive" or "operational" ? "high" : inferredProfile == "analytical" ? "medium" : "low",
            ReviewGuidance = BuildIntentProfileGuidance(inferredProfile),
            Evidence = BuildIntentProfileEvidence(page, storySummary, inferredProfile),
        };
    }

    private static ActionabilityBreakdown BuildActionabilityBreakdown(
        PageData page,
        PageStorySummary storySummary,
        PageIntentProfileSummary intentProfile)
    {
        var analysis = AnalyzeNarrativePage(page);
        var visibleTexts = analysis.VisibleTexts;
        bool targetBenchmarkPresent = ContainsAnyNarrativeKeyword(visibleTexts, "target", "budget", "benchmark", "goal", "plan", "actual");
        bool exceptionVisibility =
            ContainsAnyNarrativeKeyword(visibleTexts, "variance", "exception", "risk", "alert", "below target", "above target", "at risk")
            || analysis.ComparisonVisuals.Count > 0;
        bool urgencySignaling =
            ContainsAnyNarrativeKeyword(visibleTexts, "today", "daily", "weekly", "now", "urgent", "overdue", "critical")
            || intentProfile.InferredProfile == "operational";
        bool priorPeriodContext = ContainsAnyNarrativeKeyword(visibleTexts, "prior", "previous", "yoy", "mom", "qoq", "last month", "last year");
        bool drillPathPresent = analysis.HasSupportingEvidenceFlow && (
            analysis.SupportingDataVisuals.Count > 0 ||
            analysis.VisibleDataVisuals.Any(IsTableLikeVisual));

        double score = 0.0;
        if (targetBenchmarkPresent) score += 20.0;
        if (exceptionVisibility) score += 20.0;
        if (urgencySignaling) score += 20.0;
        if (priorPeriodContext) score += 20.0;
        if (drillPathPresent) score += 20.0;

        var strengths = new List<string>();
        var gaps = new List<string>();
        AddActionabilitySignal(targetBenchmarkPresent, strengths, gaps,
            "Targets or benchmarks are visible next to the headline numbers.",
            "Add a target or benchmark next to the KPI so the value can be interpreted.");
        AddActionabilitySignal(exceptionVisibility, strengths, gaps,
            "Exceptions or at-risk conditions are surfaced clearly.",
            "Call out the main exception or at-risk segment so the page points to a decision.");
        AddActionabilitySignal(urgencySignaling, strengths, gaps,
            "Urgency or recency cues help users judge whether action is needed now.",
            "Add a recency or urgency cue if this page is expected to trigger action.");
        AddActionabilitySignal(priorPeriodContext, strengths, gaps,
            "Prior-period or delta context explains movement over time.",
            "Add prior-period or delta context so users can judge movement, not just the current value.");
        AddActionabilitySignal(drillPathPresent, strengths, gaps,
            "A supporting evidence path exists behind the headline result.",
            "Add a drill path or supporting chart/table so users can investigate why the KPI moved.");

        var summary = score switch
        {
            >= 80.0 => $"This {intentProfile.InferredProfile} page supports action well: the decision anchor, context, and supporting evidence are all visible.",
            >= 50.0 => $"This {intentProfile.InferredProfile} page has partial decision support, but key actionability cues are still missing.",
            _ => $"This {intentProfile.InferredProfile} page still lacks the decision context needed for confident action."
        };

        return new ActionabilityBreakdown
        {
            Score = score,
            TargetBenchmarkPresent = targetBenchmarkPresent,
            ExceptionVisibility = exceptionVisibility,
            UrgencySignaling = urgencySignaling,
            PriorPeriodContext = priorPeriodContext,
            DrillPathPresent = drillPathPresent,
            ExpectationLevel = intentProfile.ActionabilityExpectation,
            Strengths = strengths,
            Gaps = gaps,
            Summary = summary,
        };
    }

    private static BenchmarkComparisonSummary BuildBenchmarkComparison(
        PageData page,
        PageStorySummary storySummary,
        PageIntentProfileSummary intentProfile,
        ActionabilityBreakdown actionability)
    {
        string archetype = intentProfile.InferredProfile switch
        {
            "executive" => "executive scorecard",
            "operational" => "operational watchlist",
            "appendix" => "appendix reference",
            _ => "analytical deep dive",
        };

        string benchmarkLabel = intentProfile.InferredProfile switch
        {
            "executive" => "Executive-ready benchmark",
            "operational" => "Operational monitoring benchmark",
            "appendix" => "Appendix/reference benchmark",
            _ => "Analytical deep-dive benchmark",
        };

        bool beautifulButUseless =
            intentProfile.InferredProfile is "executive" or "operational"
            && actionability.Score < 50.0
            && page.Visuals.Count(visual => !visual.IsHidden) <= 5
            && storySummary.Confidence is "high" or "medium";

        var strengths = new List<string>();
        if (storySummary.Confidence == "high")
        {
            strengths.Add("The page story reads clearly on the first scan.");
        }

        if (actionability.Score >= 80.0)
        {
            strengths.Add("Decision-support context is stronger than the typical benchmark.");
        }

        var gaps = actionability.Gaps.Take(2).ToList();
        string comparativePosition = actionability.Score >= 80.0 ? "above" : actionability.Score >= 50.0 ? "mixed" : "below";
        string insight = beautifulButUseless
            ? "Beautiful but useless: the page looks polished, but the decision path is still weak."
            : comparativePosition switch
            {
                "above" => $"Compared with a typical {archetype}, this page supports decisions strongly.",
                "mixed" => $"Compared with a typical {archetype}, this page is readable but still leaves some decision work to the user.",
                _ => $"Compared with a typical {archetype}, this page still lacks the decision support expected for this profile.",
            };

        return new BenchmarkComparisonSummary
        {
            Archetype = archetype,
            BenchmarkLabel = benchmarkLabel,
            ComparativePosition = comparativePosition,
            BeautifulButUseless = beautifulButUseless,
            Insight = insight,
            Strengths = strengths,
            Gaps = gaps,
        };
    }

    private static string InferPageIntentProfile(
        NarrativePageAnalysis analysis,
        string? dominantIntent)
    {
        var visibleDataVisuals = analysis.VisibleDataVisuals;
        int tableCount = visibleDataVisuals.Count(visual => IsTableLikeVisual(visual));
        if (tableCount > 0 && tableCount >= Math.Max(1, visibleDataVisuals.Count - 1) && analysis.KpiCards.Count == 0)
        {
            return "detailReference";
        }

        if (analysis.KpiCards.Count >= 2 && analysis.SupportingDataVisuals.Count >= 1)
        {
            return "executiveOverview";
        }

        if (analysis.TrendVisuals.Count > 0 && (analysis.KpiCards.Count > 0 || ContainsOperationalMonitoringCue(analysis.VisibleTitle)))
        {
            return "operationalMonitoring";
        }

        if (analysis.Page.Visuals.Count(visual => !visual.IsHidden && visual.IsSlicer) > 0 && visibleDataVisuals.Count >= 2)
        {
            return "analyticalDeepDive";
        }

        if (string.Equals(dominantIntent, "table-reference", StringComparison.OrdinalIgnoreCase))
        {
            return "detailReference";
        }

        return "analyticalDeepDive";
    }

    private static string NormalizeIntentProfile(string inferredIntentProfile) =>
        inferredIntentProfile switch
        {
            "executiveOverview" => "executive",
            "operationalMonitoring" => "operational",
            "detailReference" => "appendix",
            _ => "analytical",
        };

    private static List<string> BuildIntentProfileGuidance(string inferredProfile) =>
        inferredProfile switch
        {
            "executive" =>
            [
                "Lead with the decision, not just the metric.",
                "Executive pages should expose the target, exception, and supporting evidence quickly.",
            ],
            "operational" =>
            [
                "Operational pages should make urgent exceptions easy to spot.",
                "Use recency, thresholds, or service-level signals so users know what needs intervention now.",
            ],
            "appendix" =>
            [
                "Appendix pages can trade speed for completeness, but labels and navigation still need to stay explicit.",
                "Reference pages should support lookup and auditability instead of headline storytelling.",
            ],
            _ =>
            [
                "Analytical pages should support exploration without hiding the main analytical question.",
                "Use filters and supporting visuals to explain differences, not just to add density.",
            ],
        };

    private static List<string> BuildIntentProfileEvidence(
        PageData page,
        PageStorySummary storySummary,
        string inferredProfile)
    {
        var evidence = new List<string>();
        if (!string.IsNullOrWhiteSpace(storySummary.InferredStory))
        {
            evidence.Add(storySummary.InferredStory);
        }

        var analysis = AnalyzeNarrativePage(page);
        if (analysis.KpiCards.Count > 0)
        {
            evidence.Add($"{analysis.KpiCards.Count} KPI card(s) detected");
        }

        if (analysis.Page.Visuals.Count(visual => !visual.IsHidden && visual.IsSlicer) > 0)
        {
            evidence.Add("Interactive slicers suggest exploratory use");
        }

        evidence.Add($"Profile normalized as {inferredProfile}");
        return evidence.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool ContainsAnyNarrativeKeyword(IEnumerable<string> texts, params string[] keywords) =>
        texts.Any(text => keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

    private static void AddActionabilitySignal(
        bool present,
        List<string> strengths,
        List<string> gaps,
        string strength,
        string gap)
    {
        if (present)
        {
            strengths.Add(strength);
        }
        else
        {
            gaps.Add(gap);
        }
    }

    private static string BuildStoryArchetype(
        string intentProfile,
        NarrativePageAnalysis analysis,
        IReadOnlyList<ChartIntentSummary> visualIntents)
    {
        if (intentProfile == "detailReference")
        {
            return "detail reference";
        }

        var archetypes = new List<string>();
        if (visualIntents.Any(intent => string.Equals(intent.Intent, "trend", StringComparison.OrdinalIgnoreCase)))
        {
            archetypes.Add("trend");
        }

        if (visualIntents.Any(intent => string.Equals(intent.Intent, "comparison", StringComparison.OrdinalIgnoreCase)))
        {
            archetypes.Add("comparison");
        }

        if (visualIntents.Any(intent => string.Equals(intent.Intent, "composition", StringComparison.OrdinalIgnoreCase)))
        {
            archetypes.Add("composition");
        }

        if (analysis.KpiCards.Count >= 2 && intentProfile == "executiveOverview")
        {
            archetypes.Insert(0, "executive overview");
        }

        return archetypes.Count == 0
            ? "analysis overview"
            : string.Join(" + ", archetypes.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string BuildStoryHypothesis(
        string intentProfile,
        string primaryMetric,
        string? primaryDimension,
        NarrativePageAnalysis analysis,
        IReadOnlyList<ChartIntentSummary> visualIntents)
    {
        var metricLabel = string.IsNullOrWhiteSpace(primaryMetric) ? "performance" : primaryMetric;
        var dimensionPhrase = string.IsNullOrWhiteSpace(primaryDimension)
            ? "supporting breakdowns"
            : $"{primaryDimension.ToLowerInvariant()} comparison";
        var executiveHeadlinePhrase = BuildExecutiveHeadlinePhrase(metricLabel);
        var metricPerformancePhrase = BuildMetricPerformancePhrase(metricLabel);

        return intentProfile switch
        {
            "executiveOverview" when visualIntents.Any(intent => string.Equals(intent.Intent, "trend", StringComparison.OrdinalIgnoreCase)) &&
                                     visualIntents.Any(intent => string.Equals(intent.Intent, "comparison", StringComparison.OrdinalIgnoreCase)) =>
                $"This page appears to summarize {metricPerformancePhrase} over time, with {dimensionPhrase} as supporting evidence.",
            "executiveOverview" =>
                $"This page appears to summarize {executiveHeadlinePhrase} for quick executive review.",
            "operationalMonitoring" =>
                $"This page appears to monitor {metricPerformancePhrase} over time and highlight recent movement or exceptions.",
            "detailReference" =>
                $"This page appears to be a detailed reference view for {metricLabel}.",
            _ when analysis.Page.Visuals.Count(visual => !visual.IsHidden && visual.IsSlicer) > 0 =>
                $"This page appears to support exploratory analysis of {metricLabel}, using multiple views and filters to explain differences.",
            _ =>
                $"This page appears to compare {metricLabel} across {dimensionPhrase}.",
        };
    }

    private static string ExtractPrimaryStoryMetric(NarrativePageAnalysis analysis, VisualData? leadVisual)
    {
        var kpiMetric = analysis.KpiCards
            .Select(visual => CleanMetricLabel(visual.BestVisibleText))
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        if (!string.IsNullOrWhiteSpace(kpiMetric))
        {
            return kpiMetric!;
        }

        var measureHint = SelectPreferredSemanticHint(leadVisual?.FieldRoles.MeasureHints)
            ?? SelectPreferredSemanticHint(analysis.VisibleDataVisuals.SelectMany(visual => visual.FieldRoles.MeasureHints));
        if (!string.IsNullOrWhiteSpace(measureHint))
        {
            return measureHint!;
        }

        return "performance";
    }

    private static string? ExtractPrimaryStoryDimension(NarrativePageAnalysis analysis, VisualData? leadVisual)
    {
        var categoryHint = SelectPreferredSemanticHint(analysis.ComparisonVisuals.SelectMany(visual => visual.FieldRoles.CategoryHints));
        categoryHint ??= SelectPreferredSemanticHint(leadVisual?.FieldRoles.CategoryHints);
        categoryHint ??= SelectPreferredSemanticHint(analysis.VisibleDataVisuals.SelectMany(visual => visual.FieldRoles.CategoryHints));
        return string.IsNullOrWhiteSpace(categoryHint) ? null : categoryHint;
    }

    private static string InferStoryConfidence(
        NarrativePageAnalysis analysis,
        ChartIntentSummary? leadIntent,
        SemanticNarrativeSignals semanticSignals,
        IReadOnlyList<string>? reportConsistencyNotes)
    {
        int score = 0;
        if (analysis.HasMeaningfulVisibleTitle)
        {
            score += 2;
        }

        if (analysis.KpiCards.Count >= 2)
        {
            score += 2;
        }

        if (leadIntent?.Confidence == "high")
        {
            score += 2;
        }
        else if (leadIntent?.Confidence == "medium")
        {
            score += 1;
        }

        if (analysis.HasSupportingEvidenceFlow)
        {
            score += 1;
        }

        score += semanticSignals.ConfidenceBonus;

        if (reportConsistencyNotes is { Count: > 0 })
        {
            score -= 1;
        }

        return score >= 6 ? "high" : score >= 4 ? "medium" : "low";
    }

    private static List<string> BuildStoryEvidence(
        NarrativePageAnalysis analysis,
        VisualData? leadVisual,
        ChartIntentSummary? leadIntent,
        SemanticNarrativeSignals semanticSignals,
        IReadOnlyList<string>? reportConsistencyNotes)
    {
        var evidence = new List<string>();
        if (!string.IsNullOrWhiteSpace(analysis.VisibleTitle))
        {
            evidence.Add($"Visible title: {analysis.VisibleTitle}");
        }

        if (analysis.KpiCards.Count > 0)
        {
            evidence.Add($"{analysis.KpiCards.Count} KPI cards in the top scan path");
        }

        if (leadVisual is not null)
        {
            var leadMetric = leadVisual.FieldRoles.MeasureHints.FirstOrDefault();
            var leadCategory = leadVisual.FieldRoles.CategoryHints.FirstOrDefault();
            var leadSummary = $"Lead visual: {leadVisual.Type}";
            if (!string.IsNullOrWhiteSpace(leadMetric) || !string.IsNullOrWhiteSpace(leadCategory))
            {
                leadSummary += $" using {string.Join(" / ", new[] { leadMetric, leadCategory }.Where(text => !string.IsNullOrWhiteSpace(text)))}";
            }

            evidence.Add(leadSummary);
        }

        if (leadIntent is not null)
        {
            evidence.Add($"Lead chart intent: {leadIntent.Intent}");
        }

        evidence.AddRange(semanticSignals.Evidence);

        if (analysis.SupportingDataVisuals.Count > 1)
        {
            evidence.Add($"{analysis.SupportingDataVisuals.Count} supporting data visuals reinforce the page story");
        }

        if (reportConsistencyNotes is { Count: > 0 })
        {
            evidence.Add("Cross-page inconsistencies may reduce story clarity");
        }

        return evidence;
    }

    private static StorySignalRegistry BuildStorySignalRegistry(
        PageData page,
        IReadOnlyList<string>? reportConsistencyNotes = null)
    {
        var analysis = AnalyzeNarrativePage(page);
        var visibleDataVisuals = analysis.VisibleDataVisuals
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ToList();
        var leadVisual = analysis.SupportingDataVisuals
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .FirstOrDefault()
            ?? visibleDataVisuals.FirstOrDefault();
        var leadIntent = leadVisual is null ? null : InferChartIntent(leadVisual, page);
        var semanticSignals = AnalyzeSemanticNarrativeSignals(analysis, leadVisual);
        var targetBenchmarkPresent = ContainsAnyNarrativeKeyword(analysis.VisibleTexts, "target", "budget", "benchmark", "goal", "plan", "actual");
        var priorPeriodContext = ContainsAnyNarrativeKeyword(analysis.VisibleTexts, "prior", "previous", "yoy", "mom", "qoq", "last month", "last year");
        var slicerPresent = page.Visuals.Any(visual => !visual.IsHidden && visual.IsSlicer);

        var entries = new List<StorySignalRegistryEntry>
        {
            CreateStorySignalEntry(
                id: "layout.meaningfulVisibleTitle",
                category: StorySignalCategory.Layout,
                rawValue: analysis.VisibleTitle,
                fired: analysis.HasMeaningfulVisibleTitle,
                contributionIntent: StorySignalContributionIntent.ClarifiesStoryIntent,
                remediability: StorySignalRemediability.ReportLayer,
                explanationHook: "Visible title or question anchor in the page scan path.",
                surfaceScope: StoryAssessmentSurfaceScope.PbirSpecific,
                requirementRole: StorySignalRequirementRole.Required,
                evidenceRole: StorySignalEvidenceRole.DirectEvidence,
                explanationType: StoryAssessmentExplanationType.DirectEvidence,
                actionabilityType: StoryAssessmentActionabilityType.DirectRemediation),
            CreateStorySignalEntry(
                id: "layout.topScanKpiCount",
                category: StorySignalCategory.Layout,
                rawValue: analysis.KpiCards.Count.ToString(CultureInfo.InvariantCulture),
                fired: analysis.KpiCards.Count > 0,
                contributionIntent: StorySignalContributionIntent.ClarifiesStoryIntent,
                remediability: StorySignalRemediability.ReportLayer,
                explanationHook: "Count of KPI cards visible in the top scan path.",
                surfaceScope: StoryAssessmentSurfaceScope.PbirSpecific,
                requirementRole: StorySignalRequirementRole.Supportive,
                evidenceRole: StorySignalEvidenceRole.DirectEvidence,
                explanationType: StoryAssessmentExplanationType.DirectEvidence,
                actionabilityType: StoryAssessmentActionabilityType.IndirectGuidance),
            CreateStorySignalEntry(
                id: "layout.leadVisualType",
                category: StorySignalCategory.Layout,
                rawValue: leadVisual?.Type,
                fired: !string.IsNullOrWhiteSpace(leadVisual?.Type),
                contributionIntent: StorySignalContributionIntent.ClarifiesStoryIntent,
                remediability: StorySignalRemediability.ReportLayer,
                explanationHook: "The leading visible data visual used in the narrative scan path.",
                surfaceScope: StoryAssessmentSurfaceScope.PbirSpecific,
                requirementRole: StorySignalRequirementRole.Required,
                evidenceRole: StorySignalEvidenceRole.DirectEvidence,
                explanationType: StoryAssessmentExplanationType.DirectEvidence,
                actionabilityType: StoryAssessmentActionabilityType.IndirectGuidance),
            CreateStorySignalEntry(
                id: "layout.supportingEvidenceFlow",
                category: StorySignalCategory.Layout,
                rawValue: analysis.HasSupportingEvidenceFlow.ToString(),
                fired: analysis.HasSupportingEvidenceFlow,
                contributionIntent: StorySignalContributionIntent.ShapesNarrativeConfidence,
                remediability: StorySignalRemediability.ReportLayer,
                explanationHook: "Whether headline results are backed by supporting visuals in the same page flow.",
                surfaceScope: StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
                requirementRole: StorySignalRequirementRole.Supportive,
                evidenceRole: StorySignalEvidenceRole.ReinforcementOnly,
                explanationType: StoryAssessmentExplanationType.DerivedButExplainable,
                actionabilityType: StoryAssessmentActionabilityType.IndirectGuidance),
            CreateStorySignalEntry(
                id: "semantic.primaryMetric",
                category: StorySignalCategory.Semantic,
                rawValue: IsGenericStoryMetric(semanticSignals.PrimaryMetric) ? null : semanticSignals.PrimaryMetric,
                fired: !IsGenericStoryMetric(semanticSignals.PrimaryMetric),
                contributionIntent: StorySignalContributionIntent.ClarifiesStoryIntent,
                remediability: StorySignalRemediability.Mixed,
                explanationHook: "Primary measure inferred from field roles, titles, and semantic hints.",
                surfaceScope: StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
                requirementRole: StorySignalRequirementRole.Required,
                evidenceRole: StorySignalEvidenceRole.DirectEvidence,
                explanationType: StoryAssessmentExplanationType.DerivedButExplainable,
                actionabilityType: StoryAssessmentActionabilityType.IndirectGuidance),
            CreateStorySignalEntry(
                id: "semantic.primaryDimension",
                category: StorySignalCategory.Semantic,
                rawValue: semanticSignals.PrimaryDimension,
                fired: !string.IsNullOrWhiteSpace(semanticSignals.PrimaryDimension),
                contributionIntent: StorySignalContributionIntent.ClarifiesStoryIntent,
                remediability: StorySignalRemediability.Mixed,
                explanationHook: "Primary comparison or grouping dimension inferred from category metadata.",
                surfaceScope: StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
                requirementRole: StorySignalRequirementRole.Supportive,
                evidenceRole: StorySignalEvidenceRole.DirectEvidence,
                explanationType: StoryAssessmentExplanationType.DerivedButExplainable,
                actionabilityType: StoryAssessmentActionabilityType.IndirectGuidance),
            CreateStorySignalEntry(
                id: "semantic.richMetadataSupport",
                category: StorySignalCategory.Semantic,
                rawValue: semanticSignals.HasRichMetadataSupport.ToString(),
                fired: semanticSignals.HasRichMetadataSupport,
                contributionIntent: StorySignalContributionIntent.ShapesNarrativeConfidence,
                remediability: StorySignalRemediability.SemanticModel,
                explanationHook: "Aliases and descriptions reinforce the same business concept.",
                surfaceScope: StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
                requirementRole: StorySignalRequirementRole.Supportive,
                evidenceRole: StorySignalEvidenceRole.ReinforcementOnly,
                explanationType: StoryAssessmentExplanationType.DerivedButExplainable,
                actionabilityType: StoryAssessmentActionabilityType.IndirectGuidance),
            CreateStorySignalEntry(
                id: "context.targetBenchmarkPresent",
                category: StorySignalCategory.Context,
                rawValue: targetBenchmarkPresent.ToString(),
                fired: targetBenchmarkPresent,
                contributionIntent: StorySignalContributionIntent.SupportsDecisionContext,
                remediability: StorySignalRemediability.ReportLayer,
                explanationHook: "Target, budget, or benchmark text appears in the visible narrative surface.",
                surfaceScope: StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
                requirementRole: StorySignalRequirementRole.Required,
                evidenceRole: StorySignalEvidenceRole.DirectEvidence,
                explanationType: StoryAssessmentExplanationType.DirectEvidence,
                actionabilityType: StoryAssessmentActionabilityType.DirectRemediation),
            CreateStorySignalEntry(
                id: "context.priorPeriodContext",
                category: StorySignalCategory.Context,
                rawValue: priorPeriodContext.ToString(),
                fired: priorPeriodContext,
                contributionIntent: StorySignalContributionIntent.SupportsDecisionContext,
                remediability: StorySignalRemediability.ReportLayer,
                explanationHook: "Prior-period or delta wording appears in the visible narrative surface.",
                surfaceScope: StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
                requirementRole: StorySignalRequirementRole.Supportive,
                evidenceRole: StorySignalEvidenceRole.DirectEvidence,
                explanationType: StoryAssessmentExplanationType.DirectEvidence,
                actionabilityType: StoryAssessmentActionabilityType.DirectRemediation),
            CreateStorySignalEntry(
                id: "context.slicerPresent",
                category: StorySignalCategory.Context,
                rawValue: slicerPresent.ToString(),
                fired: slicerPresent,
                contributionIntent: StorySignalContributionIntent.SupportsExplorationContext,
                remediability: StorySignalRemediability.ReportLayer,
                explanationHook: "At least one visible slicer is present on the page.",
                surfaceScope: StoryAssessmentSurfaceScope.PbirSpecific,
                requirementRole: StorySignalRequirementRole.Optional,
                evidenceRole: StorySignalEvidenceRole.ReinforcementOnly,
                explanationType: StoryAssessmentExplanationType.DirectEvidence,
                actionabilityType: StoryAssessmentActionabilityType.IndirectGuidance),
            CreateStorySignalEntry(
                id: "context.crossPageConsistencyPenalty",
                category: StorySignalCategory.Context,
                rawValue: reportConsistencyNotes?.Count.ToString(CultureInfo.InvariantCulture),
                fired: reportConsistencyNotes is { Count: > 0 },
                contributionIntent: StorySignalContributionIntent.ShapesNarrativeConfidence,
                remediability: StorySignalRemediability.ReportLayer,
                explanationHook: "Cross-page consistency notes may reduce narrative clarity confidence.",
                surfaceScope: StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
                requirementRole: StorySignalRequirementRole.Optional,
                evidenceRole: StorySignalEvidenceRole.ReinforcementOnly,
                explanationType: StoryAssessmentExplanationType.DerivedButExplainable,
                actionabilityType: StoryAssessmentActionabilityType.DiagnosticOnly),
        };

        if (leadIntent is not null)
        {
            entries.Add(CreateStorySignalEntry(
                id: "layout.leadIntent",
                category: StorySignalCategory.Layout,
                rawValue: leadIntent.Intent,
                fired: !string.IsNullOrWhiteSpace(leadIntent.Intent),
                contributionIntent: StorySignalContributionIntent.ClarifiesStoryIntent,
                remediability: StorySignalRemediability.ReportLayer,
                explanationHook: "Inferred analytical intent of the lead chart.",
                surfaceScope: StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
                requirementRole: StorySignalRequirementRole.Supportive,
                evidenceRole: StorySignalEvidenceRole.ReinforcementOnly,
                explanationType: StoryAssessmentExplanationType.DerivedButExplainable,
                actionabilityType: StoryAssessmentActionabilityType.IndirectGuidance));
        }

        return new StorySignalRegistry { Entries = entries };
    }

    private static StoryFilterTopologyAssessment BuildStoryFilterTopologyAssessment(
        PageData page,
        IReadOnlyList<FilterDefinitionData> reportFilters)
    {
        var canvasWidth = page.Canvas?.Width ?? CanvasWidth;
        var canvasHeight = page.Canvas?.Height ?? CanvasHeight;
        var visibleSlicers = page.Visuals
            .Where(visual => !visual.IsHidden && visual.IsSlicer)
            .ToList();
        var extractedFilters = new List<StoryFilterTopologyFilter>();
        var hierarchyPatterns = new List<string>();
        var topologyCharacteristics = new List<string>();
        var diagnosticNotes = new List<string>();

        foreach (var slicer in visibleSlicers)
        {
            extractedFilters.Add(new StoryFilterTopologyFilter
            {
                SourceId = slicer.Id,
                Scope = StoryFilterScope.Slicer,
                DisplayLabel = FirstNonBlank(slicer.BestVisibleText, slicer.Filter.FieldHints.FirstOrDefault(), slicer.Id) ?? slicer.Id,
                FieldHints = slicer.Filter.FieldHints,
                HierarchyPattern = slicer.Filter.HierarchyPattern,
                HierarchyDepth = slicer.Filter.HierarchyDepth,
                PlacementZone = ClassifyFilterZone(slicer, canvasWidth, canvasHeight),
            });

            if (!string.IsNullOrWhiteSpace(slicer.Filter.HierarchyPattern))
            {
                hierarchyPatterns.Add(slicer.Filter.HierarchyPattern!);
            }
        }

        foreach (var pageFilter in page.PageFilters)
        {
            extractedFilters.Add(new StoryFilterTopologyFilter
            {
                SourceId = pageFilter.SourceId,
                Scope = StoryFilterScope.Page,
                DisplayLabel = pageFilter.DisplayLabel,
                FieldHints = pageFilter.FieldHints,
                HierarchyPattern = pageFilter.HierarchyPattern,
                HierarchyDepth = pageFilter.HierarchyDepth,
                PlacementZone = null,
            });

            if (pageFilter.IsMalformed)
            {
                diagnosticNotes.Add($"Page filter '{pageFilter.SourceId}' had partial or malformed metadata.");
            }

            if (!string.IsNullOrWhiteSpace(pageFilter.HierarchyPattern))
            {
                hierarchyPatterns.Add(pageFilter.HierarchyPattern!);
            }
        }

        foreach (var reportFilter in reportFilters)
        {
            extractedFilters.Add(new StoryFilterTopologyFilter
            {
                SourceId = reportFilter.SourceId,
                Scope = StoryFilterScope.Report,
                DisplayLabel = reportFilter.DisplayLabel,
                FieldHints = reportFilter.FieldHints,
                HierarchyPattern = reportFilter.HierarchyPattern,
                HierarchyDepth = reportFilter.HierarchyDepth,
                PlacementZone = null,
            });

            if (reportFilter.IsMalformed)
            {
                diagnosticNotes.Add($"Report filter '{reportFilter.SourceId}' had partial or malformed metadata.");
            }

            if (!string.IsNullOrWhiteSpace(reportFilter.HierarchyPattern))
            {
                hierarchyPatterns.Add(reportFilter.HierarchyPattern!);
            }
        }

        var distinctZones = visibleSlicers
            .Select(slicer => ClassifyFilterZone(slicer, canvasWidth, canvasHeight))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(zone => zone, StringComparer.Ordinal)
            .ToList();
        if (visibleSlicers.Count > 0)
        {
            topologyCharacteristics.Add(distinctZones.Count == 1
                ? $"single {distinctZones[0]} control band"
                : $"distributed slicer controls across {distinctZones.Count} zones");
            if (distinctZones.Any(zone => zone.Contains("left", StringComparison.OrdinalIgnoreCase)))
            {
                topologyCharacteristics.Add("left control zone present");
            }

            if (distinctZones.Any(zone => zone.Contains("top", StringComparison.OrdinalIgnoreCase)))
            {
                topologyCharacteristics.Add("top control zone present");
            }
        }

        if (page.PageFilters.Count > 0)
        {
            topologyCharacteristics.Add("page-scoped filter context present");
        }

        if (reportFilters.Count > 0)
        {
            topologyCharacteristics.Add("report-scoped filter context present");
        }

        if (hierarchyPatterns.Count > 0)
        {
            topologyCharacteristics.Add("hierarchical filter metadata present");
        }

        var signals = new List<StoryFilterTopologySignal>();
        bool hasMeaningfulFieldHints = extractedFilters.Any(filter =>
            filter.FieldHints.Any(hint => !string.IsNullOrWhiteSpace(NormalizeSemanticHint(hint))));
        bool hasHierarchicalTimeSignal = extractedFilters.Any(filter =>
            filter.HierarchyDepth >= 2 &&
            (filter.HierarchyPattern?.Contains("year", StringComparison.OrdinalIgnoreCase) == true ||
             filter.HierarchyPattern?.Contains("month", StringComparison.OrdinalIgnoreCase) == true ||
             filter.DisplayLabel.Contains("date", StringComparison.OrdinalIgnoreCase) ||
             filter.FieldHints.Any(hint =>
                 hint.Contains("date", StringComparison.OrdinalIgnoreCase) ||
                 hint.Contains("year", StringComparison.OrdinalIgnoreCase) ||
                 hint.Contains("month", StringComparison.OrdinalIgnoreCase))));
        signals.Add(new StoryFilterTopologySignal
        {
            Id = "topology.hierarchicalTimeFilter",
            Classification = StoryFilterTopologySignalClassification.CrossSurfaceCandidate,
            SurfaceScope = StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
            Scope = StoryFilterScope.Slicer,
            Fired = hasHierarchicalTimeSignal,
            SupportsArchetypeReinforcement = hasHierarchicalTimeSignal,
            PromotionState = StoryAssessmentPromotionState.Internal,
            AccuracyContribution = hasHierarchicalTimeSignal ? StoryAssessmentValidationRating.Mixed : StoryAssessmentValidationRating.NotAssessed,
            ExplainabilityContribution = hasHierarchicalTimeSignal ? StoryAssessmentValidationRating.Strong : StoryAssessmentValidationRating.NotAssessed,
            ActionabilityContribution = hasHierarchicalTimeSignal ? StoryAssessmentValidationRating.Mixed : StoryAssessmentValidationRating.NotAssessed,
        });

        bool hasScopedContextSignal = hasMeaningfulFieldHints && (page.PageFilters.Count > 0 || reportFilters.Count > 0);
        signals.Add(new StoryFilterTopologySignal
        {
            Id = "topology.scopedFilterContext",
            Classification = StoryFilterTopologySignalClassification.PbirSpecific,
            SurfaceScope = StoryAssessmentSurfaceScope.PbirSpecific,
            Scope = reportFilters.Count > 0 ? StoryFilterScope.Report : StoryFilterScope.Page,
            Fired = hasScopedContextSignal,
            SupportsArchetypeReinforcement = hasScopedContextSignal,
            PromotionState = StoryAssessmentPromotionState.Internal,
            AccuracyContribution = hasScopedContextSignal ? StoryAssessmentValidationRating.Mixed : StoryAssessmentValidationRating.NotAssessed,
            ExplainabilityContribution = hasScopedContextSignal ? StoryAssessmentValidationRating.Strong : StoryAssessmentValidationRating.NotAssessed,
            ActionabilityContribution = hasScopedContextSignal ? StoryAssessmentValidationRating.Mixed : StoryAssessmentValidationRating.NotAssessed,
        });

        bool hasReportLevelContextSignal = hasMeaningfulFieldHints && reportFilters.Count > 0;
        signals.Add(new StoryFilterTopologySignal
        {
            Id = "topology.reportLevelContext",
            Classification = StoryFilterTopologySignalClassification.PbirSpecific,
            SurfaceScope = StoryAssessmentSurfaceScope.PbirSpecific,
            Scope = StoryFilterScope.Report,
            Fired = hasReportLevelContextSignal,
            SupportsArchetypeReinforcement = hasReportLevelContextSignal,
            PromotionState = StoryAssessmentPromotionState.Internal,
            AccuracyContribution = hasReportLevelContextSignal ? StoryAssessmentValidationRating.Mixed : StoryAssessmentValidationRating.NotAssessed,
            ExplainabilityContribution = hasReportLevelContextSignal ? StoryAssessmentValidationRating.Mixed : StoryAssessmentValidationRating.NotAssessed,
            ActionabilityContribution = hasReportLevelContextSignal ? StoryAssessmentValidationRating.Weak : StoryAssessmentValidationRating.NotAssessed,
        });

        bool hasConsistentControlBand = hasMeaningfulFieldHints && visibleSlicers.Count > 0 && distinctZones.Count == 1;
        signals.Add(new StoryFilterTopologySignal
        {
            Id = "topology.consistentControlBand",
            Classification = StoryFilterTopologySignalClassification.CrossSurfaceCandidate,
            SurfaceScope = StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
            Scope = StoryFilterScope.Slicer,
            Fired = hasConsistentControlBand,
            SupportsArchetypeReinforcement = hasConsistentControlBand,
            PromotionState = StoryAssessmentPromotionState.Internal,
            AccuracyContribution = hasConsistentControlBand ? StoryAssessmentValidationRating.Mixed : StoryAssessmentValidationRating.NotAssessed,
            ExplainabilityContribution = hasConsistentControlBand ? StoryAssessmentValidationRating.Strong : StoryAssessmentValidationRating.NotAssessed,
            ActionabilityContribution = hasConsistentControlBand ? StoryAssessmentValidationRating.Strong : StoryAssessmentValidationRating.NotAssessed,
        });

        bool diagnosticScatter = visibleSlicers.Count >= 2 && (!hasMeaningfulFieldHints || distinctZones.Count > 1);
        signals.Add(new StoryFilterTopologySignal
        {
            Id = "topology.scatteredGenericFilters",
            Classification = StoryFilterTopologySignalClassification.DiagnosticOnly,
            SurfaceScope = StoryAssessmentSurfaceScope.PbirSpecific,
            Scope = StoryFilterScope.Slicer,
            Fired = diagnosticScatter,
            SupportsArchetypeReinforcement = false,
            PromotionState = StoryAssessmentPromotionState.Internal,
            AccuracyContribution = diagnosticScatter ? StoryAssessmentValidationRating.Weak : StoryAssessmentValidationRating.NotAssessed,
            ExplainabilityContribution = diagnosticScatter ? StoryAssessmentValidationRating.Weak : StoryAssessmentValidationRating.NotAssessed,
            ActionabilityContribution = diagnosticScatter ? StoryAssessmentValidationRating.Weak : StoryAssessmentValidationRating.NotAssessed,
        });

        if (diagnosticScatter)
        {
            diagnosticNotes.Add("Scattered or generic slicer topology was retained as diagnostic-only because it did not improve story inference quality.");
        }

        var reinforcedArchetypes = BuildTopologyReinforcementBonusMap(signals)
            .Where(entry => entry.Value > 0d)
            .Select(entry => entry.Key.ToString())
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        var meaningfulSignals = signals.Where(signal => signal.Fired && signal.SupportsArchetypeReinforcement).ToList();

        return new StoryFilterTopologyAssessment
        {
            SlicerCount = visibleSlicers.Count,
            SurfaceScope = DetermineTopologySurfaceScope(signals),
            PromotionState = StoryAssessmentPromotionState.Internal,
            PageFilterCount = page.PageFilters.Count,
            ReportFilterCount = reportFilters.Count,
            ExtractedFilters = extractedFilters,
            HierarchyPatterns = hierarchyPatterns
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(pattern => pattern, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            TopologyCharacteristics = topologyCharacteristics
                .Distinct(StringComparer.Ordinal)
                .OrderBy(characteristic => characteristic, StringComparer.Ordinal)
                .ToList(),
            Signals = signals,
            ReinforcedArchetypes = reinforcedArchetypes,
            DiagnosticNotes = diagnosticNotes
                .Distinct(StringComparer.Ordinal)
                .OrderBy(note => note, StringComparer.Ordinal)
                .ToList(),
            AccuracyContribution = DetermineAggregateTopologyRating(meaningfulSignals.Select(signal => signal.AccuracyContribution)),
            ExplainabilityContribution = DetermineAggregateTopologyRating(meaningfulSignals.Select(signal => signal.ExplainabilityContribution)),
            ActionabilityContribution = DetermineAggregateTopologyRating(meaningfulSignals.Select(signal => signal.ActionabilityContribution)),
        };
    }

    private static StorySpecialPageAssessment BuildStorySpecialPageAssessment(PageData page)
    {
        var candidates = new List<StorySpecialPageAssessment>();

        AddIfDetected(candidates, EvaluateSpecialPageCandidate(
            page,
            StorySpecialPageType.Tooltip,
            keywordGroups:
            [
                new[] { "tooltip" },
            ],
            matchingVisualTypes: [],
            matchingFieldHints: ["tooltip"],
            treatAsPrimaryNarrativePage: false,
            suppressNormalStoryGaps: false,
            suppressGenericArchetypePromotion: true,
            reason: "Tooltip pages provide supporting hover detail and should not be treated as a primary narrative page."));
        AddIfDetected(candidates, EvaluateSpecialPageCandidate(
            page,
            StorySpecialPageType.Qna,
            keywordGroups:
            [
                new[] { "q&a", "qna", "ask a question" },
            ],
            matchingVisualTypes: ["qnavisual"],
            matchingFieldHints: ["question", "q&a", "qna"],
            treatAsPrimaryNarrativePage: false,
            suppressNormalStoryGaps: false,
            suppressGenericArchetypePromotion: true,
            reason: "Q&A pages are question-driven exploration surfaces and should not overclaim a generic analytical archetype."));
        AddIfDetected(candidates, EvaluateSpecialPageCandidate(
            page,
            StorySpecialPageType.WhatIf,
            keywordGroups:
            [
                new[] { "what if", "what-if", "scenario" },
            ],
            matchingVisualTypes: [],
            matchingFieldHints: ["parameter", "scenario", "what if", "what-if"],
            treatAsPrimaryNarrativePage: true,
            suppressNormalStoryGaps: false,
            suppressGenericArchetypePromotion: true,
            reason: "What-if pages are scenario and exploration pages that should not be reduced to a generic comparison summary."));
        AddIfDetected(candidates, EvaluateSpecialPageCandidate(
            page,
            StorySpecialPageType.KeyInfluencers,
            keywordGroups:
            [
                new[] { "key influencers", "keyinfluencers", "keyinf", "keyinfluence", "keyinfluencer", "retkeyinf", "influencer", "driver", "drivers" },
            ],
            matchingVisualTypes: ["keyinfluencers"],
            matchingFieldHints: ["influencer", "drivers", "driver"],
            treatAsPrimaryNarrativePage: true,
            suppressNormalStoryGaps: false,
            suppressGenericArchetypePromotion: true,
            reason: "Key Influencers pages are driver and explanation pages that should not be promoted as generic comparison pages."));
        AddIfDetected(candidates, EvaluateCustomerSegmentationDiagnosticPage(page));
        AddIfDetected(candidates, EvaluateSpecialPageCandidate(
            page,
            StorySpecialPageType.MarketBasket,
            keywordGroups:
            [
                new[] { "market basket", "basket analysis" },
                new[] { "association rules", "product pair" },
            ],
            matchingVisualTypes: [],
            matchingFieldHints: ["support", "lift", "confidence", "product pair", "association"],
            treatAsPrimaryNarrativePage: true,
            suppressNormalStoryGaps: false,
            suppressGenericArchetypePromotion: true,
            reason: "Market Basket pages are association and explanatory analysis pages that should not overclaim KPI-summary archetypes."));
        AddIfDetected(candidates, EvaluateSpecialPageCandidate(
            page,
            StorySpecialPageType.ReferenceLegal,
            keywordGroups:
            [
                new[] { "legal", "disclaimer" },
                new[] { "terms of use", "terms" },
            ],
            matchingVisualTypes: ["textbox"],
            matchingFieldHints: ["legal", "disclaimer"],
            treatAsPrimaryNarrativePage: false,
            suppressNormalStoryGaps: true,
            suppressGenericArchetypePromotion: true,
            reason: "Reference and legal pages are non-analytical support pages and should not produce normal analytical gaps."));
        AddIfDetected(candidates, EvaluateSpecialPageCandidate(
            page,
            StorySpecialPageType.ValidationSandbox,
            keywordGroups:
            [
                new[] { "validation", "sandbox" },
                new[] { "test page", "debug", "duplicate" },
            ],
            matchingVisualTypes: [],
            matchingFieldHints: ["test", "validation", "sandbox"],
            treatAsPrimaryNarrativePage: false,
            suppressNormalStoryGaps: true,
            suppressGenericArchetypePromotion: true,
            reason: "Validation and sandbox pages are non-reviewable diagnostic pages and should not be treated as normal analytical pages."));

        return candidates
            .OrderByDescending(candidate => GetSpecialPageConfidenceRank(candidate.Confidence))
            .ThenByDescending(candidate => candidate.EvidenceReferences.Count)
            .ThenBy(candidate => candidate.PageType.ToString(), StringComparer.Ordinal)
            .FirstOrDefault()
            ?? new StorySpecialPageAssessment
            {
                PageType = StorySpecialPageType.Unknown,
                Confidence = StorySpecialPageConfidence.Low,
                EvidenceReferences = Array.Empty<StorySpecialPageEvidenceReference>(),
                Reason = "No special-page classification met the conservative evidence threshold.",
                PromotionState = StoryAssessmentPromotionState.Internal,
                SurfaceScope = StoryAssessmentSurfaceScope.PbirSpecific,
                TreatAsPrimaryNarrativePage = true,
                SuppressNormalStoryGaps = false,
                SuppressGenericArchetypePromotion = false,
            };
    }

    private static StorySpecialPageAssessment? EvaluateSpecialPageCandidate(
        PageData page,
        StorySpecialPageType pageType,
        IReadOnlyList<string[]> keywordGroups,
        IReadOnlyList<string> matchingVisualTypes,
        IReadOnlyList<string> matchingFieldHints,
        bool treatAsPrimaryNarrativePage,
        bool suppressNormalStoryGaps,
        bool suppressGenericArchetypePromotion,
        string reason)
    {
        var evidenceReferences = new List<StorySpecialPageEvidenceReference>();
        int score = 0;
        int textCueCount = 0;
        var matchedPhrases = new List<string>();

        if (TryAddSpecialPageKeywordEvidence(evidenceReferences, "page", "displayName", page.DisplayName, keywordGroups, out var displayNameMatch))
        {
            score += 2;
            textCueCount++;
            if (!string.IsNullOrWhiteSpace(displayNameMatch))
            {
                matchedPhrases.Add(displayNameMatch);
            }
        }

        var strictVisibleTitle = GetStrictVisibleTitleText(page);
        if (TryAddSpecialPageKeywordEvidence(evidenceReferences, "page", "strictVisibleTitle", strictVisibleTitle, keywordGroups, out var strictTitleMatch))
        {
            score += 2;
            textCueCount++;
            if (!string.IsNullOrWhiteSpace(strictTitleMatch))
            {
                matchedPhrases.Add(strictTitleMatch);
            }
        }

        foreach (var visual in page.Visuals.Where(visual => !visual.IsHidden))
        {
            if (matchingVisualTypes.Any(type => string.Equals(type, visual.Type, StringComparison.OrdinalIgnoreCase)))
            {
                evidenceReferences.Add(new StorySpecialPageEvidenceReference
                {
                    SourceType = "visualType",
                    ReferenceId = visual.Id,
                    Summary = $"Visual '{visual.Id}' uses the special-page type '{visual.Type}'.",
                });
                score += 2;
                break;
            }
        }

        foreach (var visual in page.Visuals.Where(visual => !visual.IsHidden))
        {
            if (TryAddSpecialPageKeywordEvidence(evidenceReferences, "visualTitle", visual.Id, visual.BestVisibleText, keywordGroups, out var visualMatch))
            {
                score += 1;
                textCueCount++;
                if (!string.IsNullOrWhiteSpace(visualMatch))
                {
                    matchedPhrases.Add(visualMatch);
                }
                break;
            }
        }

        var fieldHints = page.Visuals
            .Where(visual => !visual.IsHidden)
            .SelectMany(visual => visual.FieldRoles.CategoryHints
                .Concat(visual.FieldRoles.SeriesHints)
                .Concat(visual.FieldRoles.MeasureHints)
                .Concat(visual.FieldRoles.ValueHints))
            .ToList();
        var matchingHint = fieldHints.FirstOrDefault(hint => matchingFieldHints.Any(match => TextContainsPhrase(hint, match)));
        if (!string.IsNullOrWhiteSpace(matchingHint))
        {
            evidenceReferences.Add(new StorySpecialPageEvidenceReference
            {
                SourceType = "fieldHint",
                ReferenceId = pageType.ToString(),
                Summary = $"Field or measure metadata contains special-page cue '{matchingHint}'.",
            });
            score += 1;
            textCueCount++;
        }

        if (pageType is StorySpecialPageType.KeyInfluencers &&
            RequiresAdditionalKeyInfluencerSupport(matchedPhrases, evidenceReferences))
        {
            return null;
        }

        if (pageType is StorySpecialPageType.ReferenceLegal && page.Visuals.Count(visual => !visual.IsDecorative && !visual.IsNavigationElement) <= 1)
        {
            evidenceReferences.Add(new StorySpecialPageEvidenceReference
            {
                SourceType = "layout",
                ReferenceId = pageType.ToString(),
                Summary = "Page uses little or no analytical visual evidence, which supports a non-analytical reference posture.",
            });
            score += 1;
        }

        if (pageType is StorySpecialPageType.Tooltip && page.Visuals.Count(visual => !visual.IsHidden) <= 3)
        {
            evidenceReferences.Add(new StorySpecialPageEvidenceReference
            {
                SourceType = "layout",
                ReferenceId = pageType.ToString(),
                Summary = "Tooltip page uses a compact supporting layout rather than a full narrative canvas.",
            });
            score += 1;
        }

        var distinctSourceCount = evidenceReferences
            .Select(reference => $"{reference.SourceType}:{reference.ReferenceId}")
            .Distinct(StringComparer.Ordinal)
            .Count();
        if (score < 2 || distinctSourceCount == 0 || (textCueCount == 0 && score < 4))
        {
            return null;
        }

        var confidence = score >= 4 && distinctSourceCount >= 2
            ? StorySpecialPageConfidence.High
            : StorySpecialPageConfidence.Medium;
        return new StorySpecialPageAssessment
        {
            PageType = pageType,
            Confidence = confidence,
            EvidenceReferences = evidenceReferences
                .GroupBy(reference => $"{reference.SourceType}:{reference.ReferenceId}:{reference.Summary}", StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList(),
            Reason = reason,
            PromotionState = StoryAssessmentPromotionState.Internal,
            SurfaceScope = StoryAssessmentSurfaceScope.PbirSpecific,
            TreatAsPrimaryNarrativePage = treatAsPrimaryNarrativePage,
            SuppressNormalStoryGaps = suppressNormalStoryGaps,
            SuppressGenericArchetypePromotion = suppressGenericArchetypePromotion,
        };
    }

    private static bool TryAddSpecialPageKeywordEvidence(
        ICollection<StorySpecialPageEvidenceReference> evidenceReferences,
        string sourceType,
        string referenceId,
        string? candidateText,
        IReadOnlyList<string[]> keywordGroups,
        out string? matchedPhrase)
    {
        matchedPhrase = null;

        if (string.IsNullOrWhiteSpace(candidateText))
        {
            return false;
        }

        foreach (var group in keywordGroups)
        {
            var matchedCandidate = group.FirstOrDefault(phrase =>
                TextContainsPhrase(candidateText, phrase) ||
                NormalizeSpecialPageCue(candidateText).Contains(NormalizeSpecialPageCue(phrase), StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(matchedCandidate))
            {
                matchedPhrase = matchedCandidate;
                evidenceReferences.Add(new StorySpecialPageEvidenceReference
                {
                    SourceType = sourceType,
                    ReferenceId = referenceId,
                    Summary = $"Text cue '{matchedPhrase}' matched in '{candidateText}'.",
                });
                return true;
            }
        }

        return false;
    }

    private static StorySpecialPageAssessment? EvaluateCustomerSegmentationDiagnosticPage(PageData page)
    {
        var evidenceReferences = new List<StorySpecialPageEvidenceReference>();
        int score = 0;

        bool hasCustomerTextCue = TryAddSpecialPageKeywordEvidence(
            evidenceReferences,
            "page",
            "displayName",
            page.DisplayName,
            [["customer", "segment", "segmentation", "cohort", "account", "buyer"]],
            out _);
        hasCustomerTextCue |= TryAddSpecialPageKeywordEvidence(
            evidenceReferences,
            "page",
            "strictVisibleTitle",
            GetStrictVisibleTitleText(page),
            [["customer", "segment", "segmentation", "cohort", "account", "buyer"]],
            out _);
        if (hasCustomerTextCue)
        {
            score += 2;
        }

        var customerSemanticHints = page.PageFilters
            .SelectMany(filter => filter.FieldHints)
            .Concat(page.Visuals
                .Where(visual => !visual.IsHidden)
                .SelectMany(visual => visual.FieldRoles.CategoryHints
                    .Concat(visual.FieldRoles.SeriesHints)
                    .Concat(visual.FieldRoles.MeasureHints)
                    .Concat(visual.FieldRoles.ValueHints)))
            .Where(IsCustomerSegmentationCue)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (customerSemanticHints.Count > 0)
        {
            evidenceReferences.Add(new StorySpecialPageEvidenceReference
            {
                SourceType = "fieldHint",
                ReferenceId = StorySpecialPageType.CustomerSegmentationDiagnostic.ToString(),
                Summary = $"Customer or segmentation semantic cues detected: {string.Join(", ", customerSemanticHints.OrderBy(hint => hint, StringComparer.OrdinalIgnoreCase))}.",
            });
            score += customerSemanticHints.Count >= 2 ? 2 : 1;
        }

        var visibleAnalyticalVisuals = page.Visuals
            .Where(visual => !visual.IsHidden && !visual.IsDecorative && !visual.IsNavigationElement && !visual.IsKpiCard)
            .ToList();
        bool hasDiagnosticBreakdownStructure =
            visibleAnalyticalVisuals.Count >= 2 &&
            (visibleAnalyticalVisuals.Count(visual => visual.IsComparison || visual.Type is "tableEx" or "table" or "matrix") >= 2 ||
             visibleAnalyticalVisuals.Any(visual => visual.Type is "tableEx" or "table" or "matrix"));
        if (hasDiagnosticBreakdownStructure)
        {
            evidenceReferences.Add(new StorySpecialPageEvidenceReference
            {
                SourceType = "layout",
                ReferenceId = StorySpecialPageType.CustomerSegmentationDiagnostic.ToString(),
                Summary = "Multiple analytical breakdown visuals suggest a diagnostic or segmentation page rather than a KPI monitor.",
            });
            score += 1;
        }

        if (!hasCustomerTextCue || customerSemanticHints.Count == 0 || !hasDiagnosticBreakdownStructure || score < 4)
        {
            return null;
        }

        return new StorySpecialPageAssessment
        {
            PageType = StorySpecialPageType.CustomerSegmentationDiagnostic,
            Confidence = score >= 5 ? StorySpecialPageConfidence.High : StorySpecialPageConfidence.Medium,
            EvidenceReferences = evidenceReferences,
            Reason = "Customer and segmentation cues indicate a diagnostic breakdown page, so generic performance-monitor promotion should be downgraded.",
            PromotionState = StoryAssessmentPromotionState.Internal,
            SurfaceScope = StoryAssessmentSurfaceScope.PbirSpecific,
            TreatAsPrimaryNarrativePage = true,
            SuppressNormalStoryGaps = false,
            SuppressGenericArchetypePromotion = true,
        };
    }

    private static bool IsCustomerSegmentationCue(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
        {
            return false;
        }

        return TextContainsPhrase(hint, "customer") ||
               TextContainsPhrase(hint, "segment") ||
               TextContainsPhrase(hint, "segmentation") ||
               TextContainsPhrase(hint, "cohort") ||
               TextContainsPhrase(hint, "account") ||
               TextContainsPhrase(hint, "buyer");
    }

    private static bool RequiresAdditionalKeyInfluencerSupport(
        IReadOnlyList<string> matchedPhrases,
        IReadOnlyList<StorySpecialPageEvidenceReference> evidenceReferences)
    {
        var normalizedMatches = matchedPhrases
            .Select(NormalizeSpecialPageCue)
            .Where(match => !string.IsNullOrWhiteSpace(match))
            .ToList();
        bool hasCompactAlias = normalizedMatches.Any(match => match is
            "retkeyinf" or
            "keyinf" or
            "keyinfluence" or
            "keyinfluencer" or
            "influencer" or
            "driver" or
            "drivers");
        bool hasStrongPhrase = normalizedMatches.Any(match => match == "keyinfluencers");
        if (!hasCompactAlias || hasStrongPhrase)
        {
            return false;
        }

        return !evidenceReferences.Any(reference =>
            string.Equals(reference.SourceType, "visualType", StringComparison.Ordinal) ||
            string.Equals(reference.SourceType, "fieldHint", StringComparison.Ordinal));
    }

    private static string NormalizeSpecialPageCue(string text)
    {
        return Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9]+", string.Empty, RegexOptions.CultureInvariant);
    }

    private static int GetSpecialPageConfidenceRank(StorySpecialPageConfidence confidence)
    {
        return confidence switch
        {
            StorySpecialPageConfidence.High => 3,
            StorySpecialPageConfidence.Medium => 2,
            _ => 1,
        };
    }

    private static void AddIfDetected(
        ICollection<StorySpecialPageAssessment> candidates,
        StorySpecialPageAssessment? assessment)
    {
        if (assessment is not null)
        {
            candidates.Add(assessment);
        }
    }

    private static StoryAssessmentArchetypeClassification? BuildStoryAssessmentArchetypeClassification(
        StorySignalRegistry? registry,
        StoryFilterTopologyAssessment? topologyAssessment = null,
        StorySpecialPageAssessment? specialPageAssessment = null)
    {
        if (registry?.Entries is null || registry.Entries.Count == 0)
        {
            return null;
        }

        var entriesById = registry.Entries
            .GroupBy(entry => entry.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);

        var candidateResults = new[]
        {
            EvaluateArchetype(entriesById, StoryArchetypeId.PerformanceMonitor, BuildPerformanceMonitorExpectations()),
            EvaluateArchetype(entriesById, StoryArchetypeId.TrendException, BuildTrendExceptionExpectations()),
            EvaluateArchetype(entriesById, StoryArchetypeId.Ranking, BuildRankingExpectations()),
            EvaluateArchetype(entriesById, StoryArchetypeId.Comparison, BuildComparisonExpectations()),
            EvaluateArchetype(entriesById, StoryArchetypeId.Decomposition, BuildDecompositionExpectations()),
            EvaluateArchetype(entriesById, StoryArchetypeId.NarrativeWalkthrough, BuildNarrativeWalkthroughExpectations()),
        }
        .Select(result => ApplyTopologyReinforcement(result, topologyAssessment))
        .Select(result => ApplySpecialPageArchetypeGuardrails(result, specialPageAssessment))
        .OrderByDescending(result => result.MatchScore)
        .ThenBy(result => result.ArchetypeId.ToString(), StringComparer.Ordinal)
        .ToList();

        if (candidateResults.Count == 0)
        {
            return null;
        }

        var bestScore = candidateResults[0].MatchScore;
        var secondBestScore = candidateResults.Count > 1 ? candidateResults[1].MatchScore : 0d;
        bool suppressedBySpecialPageType = specialPageAssessment is not null &&
                                           specialPageAssessment.PageType != StorySpecialPageType.Unknown &&
                                           specialPageAssessment.SuppressGenericArchetypePromotion;

        var finalizedResults = candidateResults
            .Select((result, index) => FinalizeArchetypeResult(
                result,
                marginToNearestCompetitor: index == 0 ? bestScore - secondBestScore : bestScore - result.MatchScore,
                isBestFit: index == 0))
            .ToList();

        var bestFit = finalizedResults[0];
        return new StoryAssessmentArchetypeClassification
        {
            BestFitArchetypeId = bestFit.ArchetypeId,
            SurfaceScope = DetermineArchetypeClassificationSurfaceScope(finalizedResults),
            PromotionState = DetermineCanonicalPromotionState(finalizedResults.Select(result => result.PromotionState)),
            SuppressedBySpecialPageType = suppressedBySpecialPageType,
            ArchetypePromotionDisposition = DetermineArchetypePromotionDisposition(specialPageAssessment, suppressedBySpecialPageType),
            SpecialPageReason = suppressedBySpecialPageType ? specialPageAssessment?.Reason : null,
            ArchetypeResults = finalizedResults,
            Level1ValidationHarness = new StoryAssessmentLevel1ValidationHarness
            {
                ReviewerChoice = null,
                SystemChoice = bestFit.ArchetypeId.ToString(),
                DisagreementReason = null,
                AccuracyRating = StoryAssessmentValidationRating.NotAssessed,
                ConsistencyRating = StoryAssessmentValidationRating.NotAssessed,
                ExplainabilityRating = StoryAssessmentValidationRating.NotAssessed,
                ActionabilityRating = StoryAssessmentValidationRating.NotAssessed,
            },
            PromotionGateDefinition = new StoryAssessmentPromotionGateDefinition
            {
                MinimumClassificationAccuracy = 0.85d,
                MinimumExplanationQuality = StoryAssessmentValidationRating.Strong,
                MinimumGapUsefulnessPotential = StoryAssessmentValidationRating.Mixed,
                MaximumFalsePositiveRate = 0.10d,
                ReviewerAgreementThresholdPlaceholder = 0.80d,
            },
        };
    }

    private static StorySemanticCoherenceAssessment BuildStorySemanticCoherenceAssessment(
        PageData page,
        StorySpecialPageAssessment? specialPageAssessment = null)
    {
        var tuningDetails = BuildSemanticCoherenceTuningDetails(page, specialPageAssessment);
        var extractedTerms = ExtractSemanticCoherenceTerms(page, specialPageAssessment);
        var scoringMode = specialPageAssessment is not null &&
                          specialPageAssessment.PageType != StorySpecialPageType.Unknown &&
                          !specialPageAssessment.TreatAsPrimaryNarrativePage
            ? "DiagnosticSpecialPage"
            : "PrimaryNarrative";
        if (extractedTerms.Count == 0)
        {
            return CreateSparseSemanticCoherenceAssessment(scoringMode, tuningDetails);
        }

        var termClusters = BuildSemanticTermClusters(extractedTerms);
        if (termClusters.Count == 0)
        {
            return CreateSparseSemanticCoherenceAssessment(scoringMode, tuningDetails, extractedTerms);
        }

        var topCluster = termClusters[0];
        var secondCluster = termClusters.Count > 1 ? termClusters[1] : null;
        var totalClusterWeight = termClusters.Sum(cluster => cluster.Weight);
        var totalEvidenceCount = extractedTerms.Count;
        bool diagnosticMode = string.Equals(scoringMode, "DiagnosticSpecialPage", StringComparison.Ordinal);
        bool isSparse = diagnosticMode
            ? totalEvidenceCount < 3 || totalClusterWeight < 2.5d
            : totalEvidenceCount < 4 || totalClusterWeight < 3.5d || topCluster.SupportCount < 2;
        var dominantShare = topCluster.Weight / Math.Max(totalClusterWeight, 1d);
        double coherenceScore = isSparse
            ? Math.Round(dominantShare * 35d, 2)
            : Math.Round(Math.Min(100d, dominantShare * 72d + Math.Min(28d, topCluster.SupportCount * 5d)), 2);

        bool strongCompetingStory = !isSparse &&
            secondCluster is not null &&
            MeetsStrongCompetingStoryThreshold(topCluster, secondCluster, extractedTerms);
        bool weakDisagreement = !isSparse &&
            secondCluster is not null &&
            !strongCompetingStory &&
            MeetsWeakDisagreementThreshold(topCluster, secondCluster, extractedTerms);

        var coherenceClassification = isSparse
            ? StorySemanticCoherenceClassification.Sparse
            : strongCompetingStory || coherenceScore < 55d
                ? StorySemanticCoherenceClassification.Split
                : StorySemanticCoherenceClassification.Focused;
        var competingStoryStatus = strongCompetingStory
            ? StoryCompetingStoryStatus.StrongCandidatePromotionDelayed
            : weakDisagreement || (!isSparse && secondCluster is not null && coherenceClassification == StorySemanticCoherenceClassification.Split)
                ? StoryCompetingStoryStatus.WeakDiagnosticOnly
                : StoryCompetingStoryStatus.None;
        var confidence = DetermineSemanticCoherenceConfidence(
            coherenceScore,
            totalEvidenceCount,
            topCluster,
            secondCluster,
            isSparse,
            weakDisagreement);
        var validationStatus = competingStoryStatus == StoryCompetingStoryStatus.StrongCandidatePromotionDelayed
            ? StorySemanticCoherenceValidationStatus.PromotionDelayedRequiresStrongerValidation
            : !isSparse && confidence != StorySemanticCoherenceConfidence.Low
                ? StorySemanticCoherenceValidationStatus.Level1Candidate
                : StorySemanticCoherenceValidationStatus.Internal;
        var dominantConcept = isSparse ? null : topCluster.ClusterId;
        var weakDisagreementSignals = (weakDisagreement ||
                                       (!isSparse && secondCluster is not null && coherenceClassification == StorySemanticCoherenceClassification.Split)) &&
                                      secondCluster is not null
            ? new[]
            {
                $"Secondary cluster '{secondCluster.ClusterId}' remains distinct but below competing-story confidence thresholds.",
                $"Top clusters differ by {Math.Round(Math.Abs(topCluster.Weight - secondCluster.Weight), 2).ToString(CultureInfo.InvariantCulture)} weighted support.",
            }
            : Array.Empty<string>();
        var explanationHooks = BuildSemanticCoherenceExplanationHooks(
            topCluster,
            secondCluster,
            coherenceClassification,
            competingStoryStatus,
            extractedTerms.Count,
            weakDisagreementSignals);

        return new StorySemanticCoherenceAssessment
        {
            CoherenceScore = coherenceScore,
            SurfaceScope = StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
            ScoringMode = scoringMode,
            CoherenceClassification = coherenceClassification,
            DominantConcept = dominantConcept,
            ExtractedTerms = extractedTerms,
            TermClusters = termClusters,
            CompetingStoryStatus = competingStoryStatus,
            WeakDisagreementSignals = weakDisagreementSignals,
            ExplanationHooks = explanationHooks,
            Confidence = confidence,
            ValidationStatus = validationStatus,
            PromotionState = DetermineSemanticCoherencePromotionState(competingStoryStatus, isSparse),
            TuningDetails = tuningDetails,
            Level1ValidationHarness = new StorySemanticCoherenceLevel1ValidationHarness
            {
                ReviewerCoherenceChoice = null,
                SystemCoherenceChoice = coherenceClassification.ToString(),
                ReviewerDominantConcept = null,
                SystemDominantConcept = dominantConcept ?? string.Empty,
                DisagreementReason = null,
                AccuracyRating = StoryAssessmentValidationRating.NotAssessed,
                ConsistencyRating = StoryAssessmentValidationRating.NotAssessed,
                ExplainabilityRating = StoryAssessmentValidationRating.NotAssessed,
                ActionabilityRating = StoryAssessmentValidationRating.NotAssessed,
            },
        };
    }

    private static StorySemanticCoherenceAssessment CreateSparseSemanticCoherenceAssessment(
        string scoringMode,
        IReadOnlyList<string> tuningDetails,
        IReadOnlyList<StorySemanticTermEvidence>? extractedTerms = null)
    {
        return new StorySemanticCoherenceAssessment
        {
            CoherenceScore = 0d,
            SurfaceScope = StoryAssessmentSurfaceScope.CrossSurfaceCandidate,
            ScoringMode = scoringMode,
            CoherenceClassification = StorySemanticCoherenceClassification.Sparse,
            DominantConcept = null,
            ExtractedTerms = extractedTerms ?? Array.Empty<StorySemanticTermEvidence>(),
            TermClusters = Array.Empty<StorySemanticTermCluster>(),
            CompetingStoryStatus = StoryCompetingStoryStatus.None,
            WeakDisagreementSignals = Array.Empty<string>(),
            ExplanationHooks =
            [
                "Sparse semantic metadata prevented a reliable coherence judgment.",
            ],
            Confidence = StorySemanticCoherenceConfidence.Low,
            ValidationStatus = StorySemanticCoherenceValidationStatus.Internal,
            PromotionState = StoryAssessmentPromotionState.Internal,
            TuningDetails = tuningDetails,
            Level1ValidationHarness = new StorySemanticCoherenceLevel1ValidationHarness
            {
                ReviewerCoherenceChoice = null,
                SystemCoherenceChoice = StorySemanticCoherenceClassification.Sparse.ToString(),
                ReviewerDominantConcept = null,
                SystemDominantConcept = string.Empty,
                DisagreementReason = null,
                AccuracyRating = StoryAssessmentValidationRating.NotAssessed,
                ConsistencyRating = StoryAssessmentValidationRating.NotAssessed,
                ExplainabilityRating = StoryAssessmentValidationRating.NotAssessed,
                ActionabilityRating = StoryAssessmentValidationRating.NotAssessed,
            },
        };
    }

    private static StoryGapAssessment BuildStoryGapAssessment(
        StorySignalRegistry? registry,
        StoryAssessmentArchetypeClassification? archetypeClassification,
        StorySemanticCoherenceAssessment? semanticCoherenceAssessment,
        StoryFilterTopologyAssessment? topologyAssessment,
        StorySpecialPageAssessment? specialPageAssessment)
    {
        var gaps = new List<StoryGapRecord>();

        if (registry?.Entries is { Count: > 0 })
        {
            foreach (var entry in registry.Entries
                         .Where(entry =>
                             !entry.Fired &&
                             entry.RequirementRole != StorySignalRequirementRole.Optional &&
                             entry.Remediability != StorySignalRemediability.NotDirectlyRemediable &&
                             entry.ActionabilityType != StoryAssessmentActionabilityType.DiagnosticOnly)
                         .OrderBy(entry => entry.Id, StringComparer.Ordinal))
            {
                var confidence = DetermineSignalGapConfidence(entry);
                gaps.Add(new StoryGapRecord
                {
                    GapId = $"gap.missing.{entry.Id}",
                    Description = BuildMissingSignalGapDescription(entry),
                    EvidenceReferences =
                    [
                        new StoryGapEvidenceReference
                        {
                            SourceType = "signalRegistry",
                            ReferenceId = entry.Id,
                            Summary = $"Signal did not fire: {entry.ExplanationHook}",
                        },
                    ],
                    RemediationLayer = MapGapRemediationLayer(entry.Remediability, entry.Category),
                    ActionabilityAssessment = DowngradeGapActionability(
                        MapGapActionability(entry.ActionabilityType),
                        confidence),
                    ArchetypeRelevance = DetermineGapArchetypeRelevance(entry.Id, archetypeClassification),
                    PromotionState = StoryAssessmentPromotionState.Internal,
                    Confidence = confidence,
                    IsFutureContractCandidate = IsFutureContractCandidateGapId($"gap.missing.{entry.Id}"),
                });
            }
        }

        if (semanticCoherenceAssessment is not null)
        {
            var semanticConfidence = MapGapConfidence(semanticCoherenceAssessment.Confidence);
            if (semanticCoherenceAssessment.CoherenceClassification == StorySemanticCoherenceClassification.Sparse)
            {
                gaps.Add(new StoryGapRecord
                {
                    GapId = "gap.semantic.sparseMetadata",
                    Description = "Strengthen semantic model names, descriptions, or visible titles so the page resolves to a clearer business concept.",
                    EvidenceReferences = BuildSemanticGapEvidenceReferences(
                        "semantic.sparseMetadata",
                        semanticCoherenceAssessment.ExplanationHooks),
                    RemediationLayer = StoryGapRemediationLayer.Model,
                    ActionabilityAssessment = DowngradeGapActionability(
                        StoryGapActionabilityAssessment.PartlyActionable,
                        semanticConfidence),
                    ArchetypeRelevance = StoryGapArchetypeRelevance.Low,
                    PromotionState = StoryAssessmentPromotionState.Internal,
                    Confidence = semanticConfidence,
                    IsFutureContractCandidate = IsFutureContractCandidateGapId("gap.semantic.sparseMetadata"),
                });
            }

            if (semanticCoherenceAssessment.CoherenceClassification == StorySemanticCoherenceClassification.Split)
            {
                gaps.Add(new StoryGapRecord
                {
                    GapId = "gap.semantic.competingStoryMetadata",
                    Description = "Align measure names, field labels, and visible wording around one dominant concept so the story is easier to explain consistently.",
                    EvidenceReferences = BuildSemanticGapEvidenceReferences(
                        "semantic.competingStoryMetadata",
                        semanticCoherenceAssessment.ExplanationHooks),
                    RemediationLayer = StoryGapRemediationLayer.Model,
                    ActionabilityAssessment = DowngradeGapActionability(
                        StoryGapActionabilityAssessment.PartlyActionable,
                        semanticConfidence),
                    ArchetypeRelevance = StoryGapArchetypeRelevance.Primary,
                    PromotionState = StoryAssessmentPromotionState.Internal,
                    Confidence = semanticConfidence,
                    IsFutureContractCandidate = IsFutureContractCandidateGapId("gap.semantic.competingStoryMetadata"),
                });

                gaps.Add(new StoryGapRecord
                {
                    GapId = "gap.semantic.competingStoryRestructure",
                    Description = "Separate competing narratives into clearer sections, visuals, or pages so users do not have to reconcile multiple stories at once.",
                    EvidenceReferences = BuildSemanticGapEvidenceReferences(
                        "semantic.competingStoryRestructure",
                        semanticCoherenceAssessment.ExplanationHooks),
                    RemediationLayer = StoryGapRemediationLayer.Restructure,
                    ActionabilityAssessment = DowngradeGapActionability(
                        StoryGapActionabilityAssessment.Actionable,
                        semanticConfidence),
                    ArchetypeRelevance = StoryGapArchetypeRelevance.Primary,
                    PromotionState = StoryAssessmentPromotionState.Internal,
                    Confidence = semanticConfidence,
                    IsFutureContractCandidate = IsFutureContractCandidateGapId("gap.semantic.competingStoryRestructure"),
                });
            }
        }

        if (topologyAssessment is not null &&
            topologyAssessment.Signals.Any(signal =>
                signal.Id == "topology.scatteredGenericFilters" &&
                signal.Fired))
        {
            gaps.Add(new StoryGapRecord
            {
                GapId = "gap.topology.scatteredFilters",
                Description = "Group or simplify filter controls so the story keeps one consistent exploration entry point instead of scattered filter affordances.",
                EvidenceReferences = BuildTopologyGapEvidenceReferences(topologyAssessment),
                RemediationLayer = StoryGapRemediationLayer.Restructure,
                ActionabilityAssessment = StoryGapActionabilityAssessment.PartlyActionable,
                ArchetypeRelevance = StoryGapArchetypeRelevance.Supporting,
                PromotionState = StoryAssessmentPromotionState.Internal,
                Confidence = StoryGapConfidence.Medium,
                IsFutureContractCandidate = IsFutureContractCandidateGapId("gap.topology.scatteredFilters"),
            });
        }

        var filteredGaps = FilterStoryGapsForValidation(gaps, specialPageAssessment);

        return new StoryGapAssessment
        {
            SurfaceScope = DetermineStoryGapSurfaceScope(filteredGaps),
            PromotionState = StoryAssessmentPromotionState.Internal,
            Gaps = filteredGaps
                .GroupBy(gap => gap.GapId, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(gap => gap.GapId, StringComparer.Ordinal)
                .ToList(),
        };
    }

    private static StoryConfidenceBreakdownAssessment BuildStoryConfidenceBreakdownAssessment(
        StorySignalRegistry? registry,
        StoryAssessmentArchetypeClassification? archetypeClassification,
        StorySemanticCoherenceAssessment? semanticCoherenceAssessment,
        StoryFilterTopologyAssessment? topologyAssessment,
        StoryGapAssessment? gapAssessment)
    {
        var records = new List<StoryConfidenceDimensionRecord>();
        var lowConfidenceCauses = DetermineConfidenceLowCauses(
            registry,
            archetypeClassification,
            semanticCoherenceAssessment);

        records.Add(BuildAccuracyConfidenceDimension(
            registry,
            archetypeClassification,
            semanticCoherenceAssessment,
            lowConfidenceCauses));
        records.Add(BuildConsistencyConfidenceDimension(
            registry,
            archetypeClassification,
            semanticCoherenceAssessment,
            lowConfidenceCauses));
        records.Add(BuildExplainabilityConfidenceDimension(
            registry,
            semanticCoherenceAssessment,
            topologyAssessment,
            lowConfidenceCauses));
        records.Add(BuildActionabilityConfidenceDimension(
            registry,
            topologyAssessment,
            gapAssessment,
            lowConfidenceCauses));

        var strongestDimensions = BuildConfidenceDimensionLabelsByRating(records, descending: true);
        var weakestDimensions = BuildConfidenceDimensionLabelsByRating(records, descending: false);

        return new StoryConfidenceBreakdownAssessment
        {
            SurfaceScope = DetermineConfidenceBreakdownSurfaceScope(records),
            PromotionState = StoryAssessmentPromotionState.Internal,
            Dimensions = records,
            StrongestDimensions = strongestDimensions,
            WeakestDimensions = weakestDimensions,
            LowConfidenceCauses = lowConfidenceCauses,
        };
    }

    private static StoryArchetypeMatchResult ApplySpecialPageArchetypeGuardrails(
        StoryArchetypeMatchResult result,
        StorySpecialPageAssessment? specialPageAssessment)
    {
        if (specialPageAssessment is null ||
            specialPageAssessment.PageType == StorySpecialPageType.Unknown ||
            !specialPageAssessment.SuppressGenericArchetypePromotion)
        {
            return result;
        }

        var penalty = GetSpecialPageArchetypePenalty(specialPageAssessment.PageType, result.ArchetypeId);
        if (penalty <= 0d)
        {
            return result;
        }

        var hooks = result.ExplanationHooks.ToList();
        hooks.Add($"Special page type '{specialPageAssessment.PageType}' reduced generic archetype promotion for {result.ArchetypeId}.");

        return new StoryArchetypeMatchResult
        {
            ArchetypeId = result.ArchetypeId,
            SurfaceScope = result.SurfaceScope,
            MatchScore = Math.Round(Math.Max(0d, result.MatchScore - penalty), 2),
            MatchConfidence = result.MatchConfidence,
            MatchedSignals = result.MatchedSignals,
            MissedSignals = result.MissedSignals,
            ExplanationHooks = hooks
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            ValidationStatus = result.ValidationStatus,
            PromotionEligibilityState = result.PromotionEligibilityState,
            PromotionState = result.PromotionState,
        };
    }

    private static double GetSpecialPageArchetypePenalty(StorySpecialPageType pageType, StoryArchetypeId archetypeId)
    {
        return pageType switch
        {
            StorySpecialPageType.Tooltip or StorySpecialPageType.Qna or StorySpecialPageType.ReferenceLegal or StorySpecialPageType.ValidationSandbox => archetypeId switch
            {
                StoryArchetypeId.PerformanceMonitor => 0.32d,
                StoryArchetypeId.Comparison => 0.28d,
                StoryArchetypeId.Ranking => 0.24d,
                StoryArchetypeId.TrendException => 0.22d,
                StoryArchetypeId.Decomposition => 0.18d,
                StoryArchetypeId.NarrativeWalkthrough => 0.10d,
                _ => 0d,
            },
            StorySpecialPageType.WhatIf => archetypeId switch
            {
                StoryArchetypeId.PerformanceMonitor => 0.20d,
                StoryArchetypeId.Comparison => 0.18d,
                StoryArchetypeId.Ranking => 0.14d,
                StoryArchetypeId.TrendException => 0.10d,
                _ => 0d,
            },
            StorySpecialPageType.KeyInfluencers => archetypeId switch
            {
                StoryArchetypeId.PerformanceMonitor => 0.22d,
                StoryArchetypeId.Comparison => 0.20d,
                StoryArchetypeId.Ranking => 0.18d,
                StoryArchetypeId.TrendException => 0.12d,
                StoryArchetypeId.Decomposition => 0.08d,
                _ => 0d,
            },
            StorySpecialPageType.CustomerSegmentationDiagnostic => archetypeId switch
            {
                StoryArchetypeId.PerformanceMonitor => 0.28d,
                _ => 0d,
            },
            StorySpecialPageType.MarketBasket => archetypeId switch
            {
                StoryArchetypeId.PerformanceMonitor => 0.24d,
                StoryArchetypeId.Comparison => 0.22d,
                StoryArchetypeId.Ranking => 0.18d,
                StoryArchetypeId.TrendException => 0.12d,
                StoryArchetypeId.NarrativeWalkthrough => 0.06d,
                _ => 0d,
            },
            _ => 0d,
        };
    }

    private static string DetermineArchetypePromotionDisposition(
        StorySpecialPageAssessment? specialPageAssessment,
        bool suppressedBySpecialPageType)
    {
        if (!suppressedBySpecialPageType)
        {
            return "Normal";
        }

        return specialPageAssessment?.TreatAsPrimaryNarrativePage == true
            ? "DowngradedBySpecialPageType"
            : "SecondaryToSpecialPageType";
    }

    private static IReadOnlyList<string> BuildSemanticCoherenceTuningDetails(
        PageData page,
        StorySpecialPageAssessment? specialPageAssessment)
    {
        var details = new List<string>
        {
            "Weighted page display name and strict visible title ahead of secondary visual text.",
            "Weighted the primary narrative visual title and measure hints above supporting visuals.",
            "Applied narrow deterministic phrase folding for Q&A, what-if, key influencers, and market basket terms.",
        };

        if (specialPageAssessment is not null && specialPageAssessment.PageType != StorySpecialPageType.Unknown)
        {
            details.Add(
                specialPageAssessment.TreatAsPrimaryNarrativePage
                    ? $"Special-page cue '{specialPageAssessment.PageType}' adjusted weighting without disabling review."
                    : $"Special-page cue '{specialPageAssessment.PageType}' enabled diagnostic coherence mode.");
        }

        if (!string.IsNullOrWhiteSpace(GetStrictVisibleTitleText(page)))
        {
            details.Add("Strict visible page title contributed additional deterministic weighting.");
        }

        return details
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static List<StorySemanticTermEvidence> ExtractSemanticCoherenceTerms(
        PageData page,
        StorySpecialPageAssessment? specialPageAssessment = null)
    {
        var analysis = AnalyzeNarrativePage(page);
        var primaryVisual = SelectPrimaryNarrativeVisual(page);
        var sourceEntries = new List<(string Source, string RawText, double Weight)>();

        void AddSource(string source, string? rawText, double weight)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return;
            }

            sourceEntries.Add((source, rawText!, weight));
        }

        AddSource("page.displayName", page.DisplayName, 1.35d);
        AddSource("page.visibleTitle", analysis.VisibleTitle, 1.50d);
        AddSource("page.strictVisibleTitle", GetStrictVisibleTitleText(page), 1.60d);
        if (specialPageAssessment is not null && specialPageAssessment.PageType != StorySpecialPageType.Unknown)
        {
            AddSource("page.specialType", specialPageAssessment.PageType.ToString(), 1.60d);
        }

        foreach (var visual in page.Visuals
                     .Where(visual => !visual.IsHidden)
                     .OrderBy(visual => visual.Y)
                     .ThenBy(visual => visual.X)
                     .ThenBy(visual => visual.Id, StringComparer.Ordinal))
        {
            var titleWeight = visual.IsSlicer
                ? 0.55d
                : primaryVisual?.Id == visual.Id ? 1.55d : 1.15d;
            var measureWeight = visual.IsSlicer
                ? 0.35d
                : primaryVisual?.Id == visual.Id ? 1.20d : 1.00d;
            var categoryWeight = visual.IsSlicer
                ? 0.45d
                : primaryVisual?.Id == visual.Id ? 1.15d : 1.00d;
            AddSource($"visual.{visual.Id}.title", visual.Text.VisibleTitleText, titleWeight);
            AddSource($"visual.{visual.Id}.subtitle", visual.Text.VisibleSubtitleText, 1.00d);
            AddSource($"visual.{visual.Id}.textbox", visual.Text.TextBoxText, 1.20d);
            if (!string.Equals(visual.BestVisibleText, visual.Text.VisibleTitleText, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(visual.BestVisibleText, visual.Text.TextBoxText, StringComparison.OrdinalIgnoreCase))
            {
                AddSource($"visual.{visual.Id}.visibleText", visual.BestVisibleText, 1.00d);
            }

            foreach (var hint in visual.FieldRoles.CategoryHints)
            {
                AddSource($"visual.{visual.Id}.category", hint, categoryWeight);
            }

            foreach (var hint in visual.FieldRoles.SeriesHints)
            {
                AddSource($"visual.{visual.Id}.series", hint, 1.00d);
            }

            foreach (var hint in visual.FieldRoles.MeasureHints)
            {
                AddSource($"visual.{visual.Id}.measure", hint, measureWeight);
            }

            foreach (var hint in visual.FieldRoles.ValueHints)
            {
                AddSource($"visual.{visual.Id}.value", hint, 0.90d);
            }
        }

        return sourceEntries
            .Select(entry => new StorySemanticTermEvidence
            {
                CanonicalTerm = NormalizeSemanticCoherenceTerm(entry.RawText) ?? string.Empty,
                RawText = entry.RawText,
                Source = entry.Source,
                Weight = entry.Weight,
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.CanonicalTerm))
            .OrderBy(entry => entry.CanonicalTerm, StringComparer.Ordinal)
            .ThenBy(entry => entry.Source, StringComparer.Ordinal)
            .ThenBy(entry => entry.RawText, StringComparer.Ordinal)
            .ToList();
    }

    private static VisualData? SelectPrimaryNarrativeVisual(PageData page)
    {
        return page.Visuals
            .Where(visual => !visual.IsHidden && !visual.IsDecorative && !visual.IsNavigationElement)
            .OrderByDescending(visual => visual.W * visual.H)
            .ThenBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .FirstOrDefault();
    }

    private static string? NormalizeSemanticCoherenceTerm(string? rawText)
    {
        var normalized = NormalizeSemanticHint(rawText);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        normalized = ApplySemanticPhraseNormalization(normalized);
        var tokenBuffer = Regex.Replace(normalized.ToLowerInvariant(), @"[^a-z0-9]+", " ", RegexOptions.CultureInvariant);
        var tokens = tokenBuffer
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeSemanticCoherenceToken)
            .Where(token => !string.IsNullOrWhiteSpace(token) && !IsSemanticCoherenceStopWord(token))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return tokens.Count == 0 ? null : string.Join(' ', tokens);
    }

    private static string ApplySemanticPhraseNormalization(string normalized)
    {
        return normalized
            .Replace("Q&A", "Qna", StringComparison.OrdinalIgnoreCase)
            .Replace("Q & A", "Qna", StringComparison.OrdinalIgnoreCase)
            .Replace("What If", "WhatIf", StringComparison.OrdinalIgnoreCase)
            .Replace("What-If", "WhatIf", StringComparison.OrdinalIgnoreCase)
            .Replace("Key Influencers", "KeyInfluencers", StringComparison.OrdinalIgnoreCase)
            .Replace("Market Basket", "MarketBasket", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSemanticCoherenceToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        var normalized = token.Trim();
        if (normalized.Length > 4 && normalized.EndsWith("ies", StringComparison.Ordinal))
        {
            normalized = normalized[..^3] + "y";
        }
        else if (normalized.Length > 4 && normalized.EndsWith("es", StringComparison.Ordinal) &&
                 (normalized.EndsWith("ches", StringComparison.Ordinal) ||
                  normalized.EndsWith("shes", StringComparison.Ordinal) ||
                  normalized.EndsWith("sses", StringComparison.Ordinal) ||
                  normalized.EndsWith("xes", StringComparison.Ordinal) ||
                  normalized.EndsWith("zes", StringComparison.Ordinal)))
        {
            normalized = normalized[..^2];
        }
        else if (normalized.Length > 3 && normalized.EndsWith('s') && !normalized.EndsWith("ss", StringComparison.Ordinal))
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }

    private static bool IsSemanticCoherenceStopWord(string token) =>
        token.Length < 3 ||
        token is "the" or "and" or "for" or "with" or "from" or "into" or "onto" or "over" or "under" or
            "page" or "chart" or "visual" or "dashboard" or "report" or "review" or "summary" or "detail" or
            "details" or "analysis" or "view" or "metric" or "measure" or "value" or "count" or "total";

    private static List<StorySemanticTermCluster> BuildSemanticTermClusters(
        IReadOnlyList<StorySemanticTermEvidence> extractedTerms)
    {
        var clusterBuckets = new Dictionary<string, SemanticCoherenceClusterAccumulator>(StringComparer.Ordinal);

        foreach (var term in extractedTerms)
        {
            var tokens = term.CanonicalTerm
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (tokens.Count == 0)
            {
                continue;
            }

            var tokenWeight = term.Weight / tokens.Count;
            foreach (var token in tokens)
            {
                if (!clusterBuckets.TryGetValue(token, out var bucket))
                {
                    bucket = new SemanticCoherenceClusterAccumulator();
                    clusterBuckets[token] = bucket;
                }

                bucket.Weight += tokenWeight;
                bucket.Terms.Add(term.CanonicalTerm);
                bucket.RawTexts.Add(term.RawText);
            }
        }

        return clusterBuckets
            .Select(pair => new StorySemanticTermCluster
            {
                ClusterId = pair.Key,
                Weight = Math.Round(pair.Value.Weight, 2),
                SupportCount = pair.Value.Terms.Count,
                Terms = pair.Value.Terms
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(term => term, StringComparer.Ordinal)
                    .ToList(),
                ExplanationHook = $"Cluster '{pair.Key}' is supported by {pair.Value.Terms.Count} extracted term(s).",
            })
            .Where(cluster => cluster.Weight > 0d)
            .OrderByDescending(cluster => cluster.Weight)
            .ThenByDescending(cluster => cluster.SupportCount)
            .ThenBy(cluster => cluster.ClusterId, StringComparer.Ordinal)
            .ToList();
    }

    private static bool MeetsStrongCompetingStoryThreshold(
        StorySemanticTermCluster topCluster,
        StorySemanticTermCluster secondCluster,
        IReadOnlyList<StorySemanticTermEvidence> extractedTerms)
    {
        if (topCluster.SupportCount < 3 || secondCluster.SupportCount < 3)
        {
            return false;
        }

        if (topCluster.Weight < 2.0d || secondCluster.Weight < 2.0d)
        {
            return false;
        }

        var ratio = topCluster.Weight / Math.Max(secondCluster.Weight, 0.01d);
        if (ratio > 1.20d || ratio < 0.83d)
        {
            return false;
        }

        if (!AreDistinctSemanticClusters(topCluster.ClusterId, secondCluster.ClusterId))
        {
            return false;
        }

        return CountExclusiveSupportingTerms(topCluster.ClusterId, secondCluster.ClusterId, extractedTerms) >= 2 &&
               CountExclusiveSupportingTerms(secondCluster.ClusterId, topCluster.ClusterId, extractedTerms) >= 2;
    }

    private static bool MeetsWeakDisagreementThreshold(
        StorySemanticTermCluster topCluster,
        StorySemanticTermCluster secondCluster,
        IReadOnlyList<StorySemanticTermEvidence> extractedTerms)
    {
        if (secondCluster.Weight < 0.3d || secondCluster.SupportCount < 1)
        {
            return false;
        }

        if (!AreDistinctSemanticClusters(topCluster.ClusterId, secondCluster.ClusterId))
        {
            return false;
        }

        var ratio = topCluster.Weight / Math.Max(secondCluster.Weight, 0.01d);
        return ratio <= 3.50d && CountExclusiveSupportingTerms(secondCluster.ClusterId, topCluster.ClusterId, extractedTerms) >= 1;
    }

    private static int CountExclusiveSupportingTerms(
        string clusterId,
        string otherClusterId,
        IReadOnlyList<StorySemanticTermEvidence> extractedTerms)
    {
        return extractedTerms.Count(term =>
            ContainsSemanticCoherenceToken(term.CanonicalTerm, clusterId) &&
            !ContainsSemanticCoherenceToken(term.CanonicalTerm, otherClusterId));
    }

    private static bool AreDistinctSemanticClusters(string firstClusterId, string secondClusterId) =>
        !string.Equals(firstClusterId, secondClusterId, StringComparison.Ordinal);

    private static bool ContainsSemanticCoherenceToken(string canonicalTerm, string token) =>
        canonicalTerm
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(token, StringComparer.Ordinal);

    private static StorySemanticCoherenceConfidence DetermineSemanticCoherenceConfidence(
        double coherenceScore,
        int totalEvidenceCount,
        StorySemanticTermCluster topCluster,
        StorySemanticTermCluster? secondCluster,
        bool isSparse,
        bool weakDisagreement)
    {
        if (isSparse)
        {
            return StorySemanticCoherenceConfidence.Low;
        }

        if (!weakDisagreement &&
            coherenceScore >= 60d &&
            totalEvidenceCount >= 4 &&
            topCluster.SupportCount >= 3)
        {
            return StorySemanticCoherenceConfidence.High;
        }

        if (coherenceScore >= 40d && totalEvidenceCount >= 4)
        {
            return StorySemanticCoherenceConfidence.Medium;
        }

        return StorySemanticCoherenceConfidence.Low;
    }

    private static List<string> BuildSemanticCoherenceExplanationHooks(
        StorySemanticTermCluster topCluster,
        StorySemanticTermCluster? secondCluster,
        StorySemanticCoherenceClassification coherenceClassification,
        StoryCompetingStoryStatus competingStoryStatus,
        int evidenceCount,
        IReadOnlyList<string> weakDisagreementSignals)
    {
        var hooks = new List<string>
        {
            $"Dominant cluster '{topCluster.ClusterId}' has {topCluster.Weight.ToString("0.##", CultureInfo.InvariantCulture)} weighted support across {topCluster.SupportCount} term(s).",
            $"Coherence classified as {coherenceClassification}.",
            $"Semantic evidence count: {evidenceCount}.",
        };

        if (secondCluster is not null)
        {
            hooks.Add($"Secondary cluster '{secondCluster.ClusterId}' has {secondCluster.Weight.ToString("0.##", CultureInfo.InvariantCulture)} weighted support across {secondCluster.SupportCount} term(s).");
        }

        if (competingStoryStatus == StoryCompetingStoryStatus.StrongCandidatePromotionDelayed)
        {
            hooks.Add("Competing-story detection fired only after both leading clusters cleared the strong-support and near-equal thresholds.");
        }
        else if (weakDisagreementSignals.Count > 0)
        {
            hooks.AddRange(weakDisagreementSignals);
        }

        return hooks
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static List<StoryConfidenceLowCause> DetermineConfidenceLowCauses(
        StorySignalRegistry? registry,
        StoryAssessmentArchetypeClassification? archetypeClassification,
        StorySemanticCoherenceAssessment? semanticCoherenceAssessment)
    {
        var causes = new List<StoryConfidenceLowCause>();
        var entries = registry?.Entries ?? Array.Empty<StorySignalRegistryEntry>();

        int firedSignals = entries.Count(entry => entry.Fired);
        if (firedSignals < 4 ||
            semanticCoherenceAssessment?.CoherenceClassification == StorySemanticCoherenceClassification.Sparse)
        {
            causes.Add(StoryConfidenceLowCause.SparseEvidence);
        }

        if (semanticCoherenceAssessment?.CompetingStoryStatus is StoryCompetingStoryStatus.StrongCandidatePromotionDelayed or StoryCompetingStoryStatus.WeakDiagnosticOnly)
        {
            causes.Add(StoryConfidenceLowCause.ConflictingEvidence);
        }

        var bestMatch = archetypeClassification?.ArchetypeResults.FirstOrDefault();
        if (bestMatch?.MatchConfidence == StoryArchetypeMatchConfidence.Low)
        {
            causes.Add(StoryConfidenceLowCause.WeakArchetypeMatch);
        }

        if (semanticCoherenceAssessment?.Confidence == StorySemanticCoherenceConfidence.Low ||
            semanticCoherenceAssessment?.CoherenceClassification == StorySemanticCoherenceClassification.Split)
        {
            causes.Add(StoryConfidenceLowCause.LowSemanticCoherence);
        }

        if (entries.Any(entry => !entry.Fired &&
                                 entry.Category == StorySignalCategory.Context &&
                                 entry.RequirementRole != StorySignalRequirementRole.Optional))
        {
            causes.Add(StoryConfidenceLowCause.MissingContext);
        }

        return causes
            .Distinct()
            .OrderBy(cause => cause)
            .ToList();
    }

    private static StoryConfidenceDimensionRecord BuildAccuracyConfidenceDimension(
        StorySignalRegistry? registry,
        StoryAssessmentArchetypeClassification? archetypeClassification,
        StorySemanticCoherenceAssessment? semanticCoherenceAssessment,
        IReadOnlyList<StoryConfidenceLowCause> lowConfidenceCauses)
    {
        var entries = registry?.Entries ?? Array.Empty<StorySignalRegistryEntry>();
        var bestMatch = archetypeClassification?.ArchetypeResults.FirstOrDefault();
        var drivers = new List<string>();
        var reducers = new List<string>();
        var missingSignals = entries
            .Where(entry => !entry.Fired && entry.Category == StorySignalCategory.Context)
            .Select(entry => entry.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var evidenceReferences = new List<StoryGapEvidenceReference>();

        if (bestMatch is not null)
        {
            drivers.Add($"Best-fit archetype '{bestMatch.ArchetypeId}' matched at {bestMatch.MatchConfidence} confidence.");
            evidenceReferences.Add(new StoryGapEvidenceReference
            {
                SourceType = "archetypeClassification",
                ReferenceId = bestMatch.ArchetypeId.ToString(),
                Summary = $"Best-fit archetype matched with {bestMatch.MatchConfidence} confidence at score {bestMatch.MatchScore.ToString("0.##", CultureInfo.InvariantCulture)}.",
            });

            if (bestMatch.MatchConfidence == StoryArchetypeMatchConfidence.Low)
            {
                reducers.Add("Weak archetype match reduces confidence that the current story interpretation is the right one.");
            }
        }

        if (entries.Any(entry => entry.Fired &&
                                 entry.EvidenceRole == StorySignalEvidenceRole.DirectEvidence &&
                                 entry.ExplanationType == StoryAssessmentExplanationType.DirectEvidence))
        {
            drivers.Add("Direct evidence signals fired for the current narrative interpretation.");
        }

        if (missingSignals.Count > 0)
        {
            reducers.Add("Missing context signals reduce confidence in narrative accuracy.");
        }

        evidenceReferences.AddRange(BuildSignalRegistryEvidenceReferences(
            entries.Where(entry => entry.Fired && entry.EvidenceRole == StorySignalEvidenceRole.DirectEvidence).Take(2),
            "accuracy"));

        var rating = StoryAssessmentValidationRating.Mixed;
        if (bestMatch?.MatchConfidence == StoryArchetypeMatchConfidence.High &&
            !lowConfidenceCauses.Contains(StoryConfidenceLowCause.MissingContext) &&
            semanticCoherenceAssessment?.Confidence != StorySemanticCoherenceConfidence.Low)
        {
            rating = StoryAssessmentValidationRating.Strong;
        }
        else if (bestMatch?.MatchConfidence == StoryArchetypeMatchConfidence.Low ||
                 lowConfidenceCauses.Contains(StoryConfidenceLowCause.MissingContext))
        {
            rating = StoryAssessmentValidationRating.Weak;
        }

        return new StoryConfidenceDimensionRecord
        {
            DimensionId = StoryConfidenceBreakdownDimension.Accuracy,
            DimensionLabel = "Accuracy",
            Rating = rating,
            ConfidenceDrivers = drivers.Distinct(StringComparer.Ordinal).ToList(),
            ConfidenceReducers = reducers.Distinct(StringComparer.Ordinal).ToList(),
            MissingSignals = missingSignals,
            EvidenceReferences = evidenceReferences
                .GroupBy(reference => $"{reference.SourceType}:{reference.ReferenceId}", StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList(),
            Explanation = rating == StoryAssessmentValidationRating.Strong
                ? "Accuracy confidence is strengthened by aligned archetype and direct-evidence signals."
                : rating == StoryAssessmentValidationRating.Weak
                    ? "Accuracy confidence is limited by missing context or a weak archetype match."
                    : "Accuracy confidence is mixed because the current evidence is partially aligned but incomplete.",
            Actionability = missingSignals.Count > 0
                ? StoryGapActionabilityAssessment.Actionable
                : StoryGapActionabilityAssessment.PartlyActionable,
            PromotionState = StoryAssessmentPromotionState.Internal,
            SurfaceScope = DetermineConfidenceSurfaceScope(entries.Select(entry => entry.SurfaceScope)),
        };
    }

    private static StoryConfidenceDimensionRecord BuildConsistencyConfidenceDimension(
        StorySignalRegistry? registry,
        StoryAssessmentArchetypeClassification? archetypeClassification,
        StorySemanticCoherenceAssessment? semanticCoherenceAssessment,
        IReadOnlyList<StoryConfidenceLowCause> lowConfidenceCauses)
    {
        var entries = registry?.Entries ?? Array.Empty<StorySignalRegistryEntry>();
        var bestMatch = archetypeClassification?.ArchetypeResults.FirstOrDefault();
        var drivers = new List<string>();
        var reducers = new List<string>();
        var missingSignals = new List<string>();
        var evidenceReferences = new List<StoryGapEvidenceReference>();

        if (bestMatch?.MatchConfidence is StoryArchetypeMatchConfidence.High or StoryArchetypeMatchConfidence.Medium)
        {
            drivers.Add("Archetype matching stayed above the low-confidence threshold.");
            evidenceReferences.Add(new StoryGapEvidenceReference
            {
                SourceType = "archetypeClassification",
                ReferenceId = $"{bestMatch.ArchetypeId}.consistency",
                Summary = $"Archetype consistency anchored by {bestMatch.MatchConfidence} match confidence.",
            });
        }

        if (semanticCoherenceAssessment?.CompetingStoryStatus == StoryCompetingStoryStatus.None)
        {
            drivers.Add("Semantic coherence did not detect a competing story.");
        }

        if (lowConfidenceCauses.Contains(StoryConfidenceLowCause.ConflictingEvidence))
        {
            reducers.Add("Conflicting or competing semantic evidence reduces consistency confidence.");
        }

        if (lowConfidenceCauses.Contains(StoryConfidenceLowCause.SparseEvidence))
        {
            reducers.Add("Sparse evidence reduces repeatability confidence across similar pages.");
        }

        if (semanticCoherenceAssessment is not null)
        {
            evidenceReferences.AddRange(BuildSemanticGapEvidenceReferences(
                "consistency.semantic",
                semanticCoherenceAssessment.ExplanationHooks));
        }

        var rating = StoryAssessmentValidationRating.Mixed;
        if (reducers.Count == 0 &&
            bestMatch?.MatchConfidence != StoryArchetypeMatchConfidence.Low &&
            semanticCoherenceAssessment?.CompetingStoryStatus == StoryCompetingStoryStatus.None)
        {
            rating = StoryAssessmentValidationRating.Strong;
        }
        else if (reducers.Count > 0)
        {
            rating = StoryAssessmentValidationRating.Weak;
        }

        return new StoryConfidenceDimensionRecord
        {
            DimensionId = StoryConfidenceBreakdownDimension.Consistency,
            DimensionLabel = "Consistency",
            Rating = rating,
            ConfidenceDrivers = drivers.Distinct(StringComparer.Ordinal).ToList(),
            ConfidenceReducers = reducers.Distinct(StringComparer.Ordinal).ToList(),
            MissingSignals = missingSignals,
            EvidenceReferences = evidenceReferences
                .GroupBy(reference => $"{reference.SourceType}:{reference.ReferenceId}", StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList(),
            Explanation = rating == StoryAssessmentValidationRating.Strong
                ? "Consistency confidence is supported by stable archetype and coherence signals."
                : rating == StoryAssessmentValidationRating.Weak
                    ? "Consistency confidence is reduced by sparse or conflicting evidence."
                    : "Consistency confidence remains mixed because the evidence is only partially stable.",
            Actionability = reducers.Count > 0
                ? StoryGapActionabilityAssessment.PartlyActionable
                : StoryGapActionabilityAssessment.NotActionable,
            PromotionState = StoryAssessmentPromotionState.Internal,
            SurfaceScope = DetermineConfidenceSurfaceScope(entries.Select(entry => entry.SurfaceScope)),
        };
    }

    private static StoryConfidenceDimensionRecord BuildExplainabilityConfidenceDimension(
        StorySignalRegistry? registry,
        StorySemanticCoherenceAssessment? semanticCoherenceAssessment,
        StoryFilterTopologyAssessment? topologyAssessment,
        IReadOnlyList<StoryConfidenceLowCause> lowConfidenceCauses)
    {
        var entries = registry?.Entries ?? Array.Empty<StorySignalRegistryEntry>();
        int directEvidenceCount = entries.Count(entry =>
            entry.Fired &&
            entry.ExplanationType == StoryAssessmentExplanationType.DirectEvidence);
        var drivers = new List<string>();
        var reducers = new List<string>();
        var missingSignals = new List<string>();
        var evidenceReferences = new List<StoryGapEvidenceReference>();

        if (directEvidenceCount >= 2)
        {
            drivers.Add("Multiple direct-evidence signals support explainable reasoning.");
        }

        if (semanticCoherenceAssessment?.ExplanationHooks.Count > 0)
        {
            drivers.Add("Semantic coherence exposes explanation hooks for the dominant concept and evidence count.");
            evidenceReferences.AddRange(BuildSemanticGapEvidenceReferences(
                "explainability.semantic",
                semanticCoherenceAssessment.ExplanationHooks));
        }

        if (topologyAssessment?.Signals.Any(signal => signal.Fired && signal.Classification == StoryFilterTopologySignalClassification.DiagnosticOnly) == true)
        {
            reducers.Add("Diagnostic-only topology signals reduce explainability clarity.");
            evidenceReferences.AddRange(BuildTopologyGapEvidenceReferences(topologyAssessment));
        }

        if (lowConfidenceCauses.Contains(StoryConfidenceLowCause.SparseEvidence))
        {
            reducers.Add("Sparse evidence limits how clearly the confidence rationale can be defended.");
        }

        evidenceReferences.AddRange(BuildSignalRegistryEvidenceReferences(
            entries.Where(entry => entry.Fired).Take(2),
            "explainability"));

        var rating = StoryAssessmentValidationRating.Mixed;
        if (directEvidenceCount >= 2 && reducers.Count == 0)
        {
            rating = StoryAssessmentValidationRating.Strong;
        }
        else if (reducers.Count > 0 && directEvidenceCount == 0)
        {
            rating = StoryAssessmentValidationRating.Weak;
        }

        return new StoryConfidenceDimensionRecord
        {
            DimensionId = StoryConfidenceBreakdownDimension.Explainability,
            DimensionLabel = "Explainability",
            Rating = rating,
            ConfidenceDrivers = drivers.Distinct(StringComparer.Ordinal).ToList(),
            ConfidenceReducers = reducers.Distinct(StringComparer.Ordinal).ToList(),
            MissingSignals = missingSignals,
            EvidenceReferences = evidenceReferences
                .GroupBy(reference => $"{reference.SourceType}:{reference.ReferenceId}", StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList(),
            Explanation = rating == StoryAssessmentValidationRating.Strong
                ? "Explainability confidence is supported by direct evidence and explicit explanation hooks."
                : rating == StoryAssessmentValidationRating.Weak
                    ? "Explainability confidence is limited because too much of the rationale depends on sparse or diagnostic-only evidence."
                    : "Explainability confidence remains mixed because some evidence is explicit but not complete.",
            Actionability = StoryGapActionabilityAssessment.PartlyActionable,
            PromotionState = StoryAssessmentPromotionState.Internal,
            SurfaceScope = DetermineConfidenceSurfaceScope(entries.Select(entry => entry.SurfaceScope)),
        };
    }

    private static StoryConfidenceDimensionRecord BuildActionabilityConfidenceDimension(
        StorySignalRegistry? registry,
        StoryFilterTopologyAssessment? topologyAssessment,
        StoryGapAssessment? gapAssessment,
        IReadOnlyList<StoryConfidenceLowCause> lowConfidenceCauses)
    {
        var entries = registry?.Entries ?? Array.Empty<StorySignalRegistryEntry>();
        var gaps = gapAssessment?.Gaps ?? Array.Empty<StoryGapRecord>();
        int actionableGapCount = gaps.Count(gap => gap.ActionabilityAssessment == StoryGapActionabilityAssessment.Actionable);
        var drivers = new List<string>();
        var reducers = new List<string>();
        var missingSignals = entries
            .Where(entry => !entry.Fired &&
                            entry.ActionabilityType == StoryAssessmentActionabilityType.DirectRemediation)
            .Select(entry => entry.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var evidenceReferences = new List<StoryGapEvidenceReference>();

        if (actionableGapCount > 0)
        {
            drivers.Add("Actionable story gaps provide a concrete path to strengthen confidence.");
            evidenceReferences.AddRange(gaps
                .Where(gap => gap.ActionabilityAssessment == StoryGapActionabilityAssessment.Actionable)
                .Take(2)
                .Select(gap => new StoryGapEvidenceReference
                {
                    SourceType = "storyGap",
                    ReferenceId = gap.GapId,
                    Summary = gap.Description,
                }));
        }

        if (topologyAssessment?.ActionabilityContribution == StoryAssessmentValidationRating.Strong)
        {
            drivers.Add("Filter topology contributes clear, author-facing remediation guidance.");
            evidenceReferences.AddRange(BuildTopologyGapEvidenceReferences(topologyAssessment));
        }

        if (gaps.Any(gap => gap.ActionabilityAssessment == StoryGapActionabilityAssessment.NotActionable))
        {
            reducers.Add("Low-confidence or non-actionable gaps reduce confidence that the current rationale leads directly to remediation.");
        }

        if (lowConfidenceCauses.Contains(StoryConfidenceLowCause.MissingContext))
        {
            reducers.Add("Missing context limits direct author actionability until narrative anchors are added.");
        }

        evidenceReferences.AddRange(BuildSignalRegistryEvidenceReferences(
            entries.Where(entry => !entry.Fired &&
                                   entry.ActionabilityType == StoryAssessmentActionabilityType.DirectRemediation)
                .Take(2),
            "actionability"));

        var rating = StoryAssessmentValidationRating.Mixed;
        if (actionableGapCount > 0 && reducers.Count == 0)
        {
            rating = StoryAssessmentValidationRating.Strong;
        }
        else if (actionableGapCount == 0 || reducers.Count > 1)
        {
            rating = StoryAssessmentValidationRating.Weak;
        }

        return new StoryConfidenceDimensionRecord
        {
            DimensionId = StoryConfidenceBreakdownDimension.Actionability,
            DimensionLabel = "Actionability",
            Rating = rating,
            ConfidenceDrivers = drivers.Distinct(StringComparer.Ordinal).ToList(),
            ConfidenceReducers = reducers.Distinct(StringComparer.Ordinal).ToList(),
            MissingSignals = missingSignals,
            EvidenceReferences = evidenceReferences
                .GroupBy(reference => $"{reference.SourceType}:{reference.ReferenceId}", StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList(),
            Explanation = rating == StoryAssessmentValidationRating.Strong
                ? "Actionability confidence is high because the assessment points to concrete author actions."
                : rating == StoryAssessmentValidationRating.Weak
                    ? "Actionability confidence is limited because too much of the rationale remains indirect or downgraded."
                    : "Actionability confidence is mixed because some remediation is clear but not all confidence limits are easily resolved.",
            Actionability = actionableGapCount > 0
                ? StoryGapActionabilityAssessment.Actionable
                : StoryGapActionabilityAssessment.PartlyActionable,
            PromotionState = StoryAssessmentPromotionState.Internal,
            SurfaceScope = DetermineConfidenceSurfaceScope(entries.Select(entry => entry.SurfaceScope)),
        };
    }

    private static List<string> BuildConfidenceDimensionLabelsByRating(
        IReadOnlyList<StoryConfidenceDimensionRecord> records,
        bool descending)
    {
        if (records.Count == 0)
        {
            return [];
        }

        var ordered = descending
            ? records.OrderByDescending(record => GetValidationRatingRank(record.Rating))
            : records.OrderBy(record => GetValidationRatingRank(record.Rating));
        int targetRank = GetValidationRatingRank(ordered.First().Rating);

        return ordered
            .Where(record => GetValidationRatingRank(record.Rating) == targetRank)
            .Select(record => record.DimensionLabel)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(label => label, StringComparer.Ordinal)
            .ToList();
    }

    private static int GetValidationRatingRank(StoryAssessmentValidationRating rating)
    {
        return rating switch
        {
            StoryAssessmentValidationRating.Strong => 3,
            StoryAssessmentValidationRating.Mixed => 2,
            StoryAssessmentValidationRating.Weak => 1,
            _ => 0,
        };
    }

    private static StoryAssessmentSurfaceScope DetermineConfidenceSurfaceScope(
        IEnumerable<StoryAssessmentSurfaceScope> surfaceScopes)
    {
        var scopes = surfaceScopes.ToList();
        return scopes.Contains(StoryAssessmentSurfaceScope.CrossSurfaceCandidate)
            ? StoryAssessmentSurfaceScope.CrossSurfaceCandidate
            : StoryAssessmentSurfaceScope.PbirSpecific;
    }

    private static StoryAssessmentSurfaceScope DetermineConfidenceBreakdownSurfaceScope(
        IReadOnlyList<StoryConfidenceDimensionRecord> records)
    {
        return DetermineConfidenceSurfaceScope(records.Select(record => record.SurfaceScope));
    }

    private static List<StoryGapEvidenceReference> BuildSignalRegistryEvidenceReferences(
        IEnumerable<StorySignalRegistryEntry> entries,
        string referencePrefix)
    {
        return entries
            .Select((entry, index) => new StoryGapEvidenceReference
            {
                SourceType = "signalRegistry",
                ReferenceId = $"{referencePrefix}.{entry.Id}.{index + 1}",
                Summary = entry.Fired
                    ? $"Signal fired: {entry.ExplanationHook}"
                    : $"Signal missing: {entry.ExplanationHook}",
            })
            .ToList();
    }

    private static StoryGapConfidence DetermineSignalGapConfidence(StorySignalRegistryEntry entry)
    {
        if (entry.EvidenceRole == StorySignalEvidenceRole.DirectEvidence &&
            entry.ExplanationType == StoryAssessmentExplanationType.DirectEvidence)
        {
            return StoryGapConfidence.High;
        }

        return entry.EvidenceRole == StorySignalEvidenceRole.DirectEvidence
            ? StoryGapConfidence.Medium
            : StoryGapConfidence.Low;
    }

    private static StoryGapRemediationLayer MapGapRemediationLayer(
        StorySignalRemediability remediability,
        StorySignalCategory category)
    {
        return remediability switch
        {
            StorySignalRemediability.ReportLayer => StoryGapRemediationLayer.Report,
            StorySignalRemediability.SemanticModel => StoryGapRemediationLayer.Model,
            StorySignalRemediability.Mixed when category == StorySignalCategory.Semantic => StoryGapRemediationLayer.Model,
            StorySignalRemediability.Mixed => StoryGapRemediationLayer.Report,
            _ => StoryGapRemediationLayer.Restructure,
        };
    }

    private static StoryGapActionabilityAssessment MapGapActionability(
        StoryAssessmentActionabilityType actionabilityType)
    {
        return actionabilityType switch
        {
            StoryAssessmentActionabilityType.DirectRemediation => StoryGapActionabilityAssessment.Actionable,
            StoryAssessmentActionabilityType.IndirectGuidance => StoryGapActionabilityAssessment.PartlyActionable,
            _ => StoryGapActionabilityAssessment.NotActionable,
        };
    }

    private static StoryGapActionabilityAssessment DowngradeGapActionability(
        StoryGapActionabilityAssessment actionabilityAssessment,
        StoryGapConfidence confidence)
    {
        if (confidence != StoryGapConfidence.Low)
        {
            return actionabilityAssessment;
        }

        return actionabilityAssessment switch
        {
            StoryGapActionabilityAssessment.Actionable => StoryGapActionabilityAssessment.PartlyActionable,
            StoryGapActionabilityAssessment.PartlyActionable => StoryGapActionabilityAssessment.NotActionable,
            _ => StoryGapActionabilityAssessment.NotActionable,
        };
    }

    private static StoryGapArchetypeRelevance DetermineGapArchetypeRelevance(
        string signalId,
        StoryAssessmentArchetypeClassification? archetypeClassification)
    {
        var bestFit = archetypeClassification?.ArchetypeResults.FirstOrDefault();
        if (bestFit is null)
        {
            return StoryGapArchetypeRelevance.Low;
        }

        if (bestFit.MissedSignals.Any(signal => signal.Contains(signalId, StringComparison.Ordinal)))
        {
            return StoryGapArchetypeRelevance.Primary;
        }

        if (bestFit.MatchedSignals.Any(signal => signal.Contains(signalId, StringComparison.Ordinal)))
        {
            return StoryGapArchetypeRelevance.Supporting;
        }

        return StoryGapArchetypeRelevance.Low;
    }

    private static StoryGapConfidence MapGapConfidence(
        StorySemanticCoherenceConfidence confidence)
    {
        return confidence switch
        {
            StorySemanticCoherenceConfidence.High => StoryGapConfidence.High,
            StorySemanticCoherenceConfidence.Medium => StoryGapConfidence.Medium,
            _ => StoryGapConfidence.Low,
        };
    }

    private static List<StoryGapEvidenceReference> BuildSemanticGapEvidenceReferences(
        string referencePrefix,
        IReadOnlyList<string> explanationHooks)
    {
        return explanationHooks
            .Where(hook => !string.IsNullOrWhiteSpace(hook))
            .Take(3)
            .Select((hook, index) => new StoryGapEvidenceReference
            {
                SourceType = "semanticCoherence",
                ReferenceId = $"{referencePrefix}.{index + 1}",
                Summary = hook,
            })
            .ToList();
    }

    private static List<StoryGapEvidenceReference> BuildTopologyGapEvidenceReferences(
        StoryFilterTopologyAssessment topologyAssessment)
    {
        var references = new List<StoryGapEvidenceReference>();
        var firedDiagnosticSignal = topologyAssessment.Signals.FirstOrDefault(signal =>
            signal.Id == "topology.scatteredGenericFilters" &&
            signal.Fired);
        if (firedDiagnosticSignal is not null)
        {
            references.Add(new StoryGapEvidenceReference
            {
                SourceType = "filterTopology",
                ReferenceId = firedDiagnosticSignal.Id,
                Summary = "Filter topology retained a scattered or generic control pattern as diagnostic-only evidence.",
            });
        }

        foreach (var note in topologyAssessment.DiagnosticNotes.Take(2))
        {
            references.Add(new StoryGapEvidenceReference
            {
                SourceType = "filterTopology",
                ReferenceId = "diagnosticNote",
                Summary = note,
            });
        }

        return references;
    }

    private static StoryAssessmentSurfaceScope DetermineStoryGapSurfaceScope(
        IReadOnlyList<StoryGapRecord> gaps)
    {
        if (gaps.Count == 0)
        {
            return StoryAssessmentSurfaceScope.PbirSpecific;
        }

        return gaps.Any(gap => gap.RemediationLayer == StoryGapRemediationLayer.Restructure ||
                               gap.RemediationLayer == StoryGapRemediationLayer.Model)
            ? StoryAssessmentSurfaceScope.CrossSurfaceCandidate
            : StoryAssessmentSurfaceScope.PbirSpecific;
    }

    private static List<StoryGapRecord> FilterStoryGapsForValidation(
        IReadOnlyList<StoryGapRecord> gaps,
        StorySpecialPageAssessment? specialPageAssessment)
    {
        if (specialPageAssessment?.SuppressNormalStoryGaps == true)
        {
            return [];
        }

        IEnumerable<StoryGapRecord> filtered = gaps;
        if (specialPageAssessment?.PageType == StorySpecialPageType.Tooltip)
        {
            filtered = filtered.Where(gap =>
                gap.GapId.Contains("layout", StringComparison.Ordinal) &&
                gap.RemediationLayer == StoryGapRemediationLayer.Report);
        }
        else
        {
            filtered = filtered.Where(gap =>
                gap.IsFutureContractCandidate ||
                gap.GapId is "gap.semantic.competingStoryMetadata" or "gap.semantic.competingStoryRestructure" ||
                !(gap.RemediationLayer == StoryGapRemediationLayer.Model &&
                  gap.Confidence == StoryGapConfidence.Low) &&
                gap.ActionabilityAssessment != StoryGapActionabilityAssessment.NotActionable);
        }

        return filtered.ToList();
    }

    private static bool IsFutureContractCandidateGapId(string gapId)
    {
        return gapId is
            "gap.missing.layout.meaningfulVisibleTitle" or
            "gap.missing.context.targetBenchmarkPresent" or
            "gap.missing.context.priorPeriodContext" or
            "gap.missing.semantic.primaryMetric" or
            "gap.missing.semantic.primaryDimension" or
            "gap.topology.scatteredFilters";
    }

    private static GuidedStoryImprovements BuildGuidedStoryImprovements(
        StoryGapAssessment? storyGapAssessment,
        StorySpecialPageAssessment? specialPageAssessment)
    {
        if (specialPageAssessment is not null &&
            (specialPageAssessment.SuppressNormalStoryGaps ||
             specialPageAssessment.PageType != StorySpecialPageType.Unknown))
        {
            return new GuidedStoryImprovements();
        }

        var mapped = (storyGapAssessment?.Gaps ?? Array.Empty<StoryGapRecord>())
            .Select(gap => MapGuidedStoryImprovement(gap, storyGapAssessment))
            .Where(improvement => improvement is not null)
            .Cast<GuidedStoryImprovement>()
            .GroupBy(improvement => improvement.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(improvement => GetGuidedStoryImprovementPriorityRank(improvement.Priority))
            .ThenBy(improvement => GetGuidedStoryImprovementSequenceRank(improvement.Id))
            .ThenBy(improvement => improvement.Title, StringComparer.Ordinal)
            .ToList();

        return new GuidedStoryImprovements
        {
            HighPriorityImprovements = mapped
                .Where(improvement => string.Equals(improvement.Priority, "high", StringComparison.Ordinal))
                .ToList(),
            MediumPriorityImprovements = mapped
                .Where(improvement => string.Equals(improvement.Priority, "medium", StringComparison.Ordinal))
                .ToList(),
            StoryImprovementRationale = BuildGuidedStoryImprovementRationale(mapped),
        };
    }

    private static GuidedStoryImprovement? MapGuidedStoryImprovement(
        StoryGapRecord gap,
        StoryGapAssessment? storyGapAssessment)
    {
        var priority = DetermineGuidedStoryImprovementPriority(gap.GapId, storyGapAssessment);

        return gap.GapId switch
        {
            "gap.missing.layout.meaningfulVisibleTitle" => new GuidedStoryImprovement
            {
                Id = "missing-title-question-anchor",
                Title = "Add a clearer page question or title",
                Summary = "The page does not establish its main question or decision early enough.",
                Rationale = "A clear title or question anchor helps readers understand what the page is trying to explain before they interpret the visuals.",
                ExpectedImpact = "Stronger narrative scan path and faster comprehension.",
                Priority = priority,
                RelatedImpactArea = "storytelling",
            },
            "gap.missing.context.targetBenchmarkPresent" => new GuidedStoryImprovement
            {
                Id = "missing-benchmark-target",
                Title = "Add a benchmark or target",
                Summary = "The current result appears without a visible target, budget, or benchmark for comparison.",
                Rationale = "Readers need an explicit reference point to judge whether the current result is strong, weak, or off track.",
                ExpectedImpact = "Clearer decision context around the headline numbers.",
                Priority = priority,
                RelatedImpactArea = "benchmark",
            },
            "gap.missing.context.priorPeriodContext" => new GuidedStoryImprovement
            {
                Id = "missing-prior-period-context",
                Title = "Add prior-period context",
                Summary = "The page shows the current result, but not enough context about movement over time.",
                Rationale = "Prior-period context helps readers judge change, momentum, and whether the current result is improving or slipping.",
                ExpectedImpact = "Stronger trend interpretation and decision context.",
                Priority = priority,
                RelatedImpactArea = "benchmark",
            },
            "gap.missing.semantic.primaryMetric" => new GuidedStoryImprovement
            {
                Id = "missing-primary-metric",
                Title = "Make the primary metric more explicit",
                Summary = "The page lacks one clearly stated metric that anchors the story.",
                Rationale = "A primary metric gives the page a stable headline measure and makes the rest of the evidence easier to interpret.",
                ExpectedImpact = "Clearer KPI framing and stronger narrative focus.",
                Priority = priority,
                RelatedImpactArea = "kpiEffectiveness",
            },
            "gap.missing.semantic.primaryDimension" => new GuidedStoryImprovement
            {
                Id = "missing-primary-dimension",
                Title = "Clarify the primary comparison dimension",
                Summary = "The page does not make the main comparison group explicit enough for quick reading.",
                Rationale = "A clear primary dimension tells readers how to segment the result and where to look for the key comparison.",
                ExpectedImpact = "Cleaner comparison logic and faster interpretation.",
                Priority = priority,
                RelatedImpactArea = "storytelling",
            },
            "gap.topology.scatteredFilters" => new GuidedStoryImprovement
            {
                Id = "scattered-filters",
                Title = "Consolidate scattered filters",
                Summary = "Filter controls are spread across the page instead of creating one clear exploration entry point.",
                Rationale = "Consolidated filters reduce visual noise and help the story start with the evidence rather than the controls.",
                ExpectedImpact = "Cleaner reading flow and a more focused exploration path.",
                Priority = priority,
                RelatedImpactArea = "storytelling",
            },
            _ => null,
        };
    }

    private static string DetermineGuidedStoryImprovementPriority(
        string gapId,
        StoryGapAssessment? storyGapAssessment)
    {
        var defaultPriority = gapId switch
        {
            "gap.missing.layout.meaningfulVisibleTitle" => "high",
            "gap.missing.context.targetBenchmarkPresent" => "high",
            "gap.missing.semantic.primaryMetric" => "high",
            "gap.missing.context.priorPeriodContext" => "medium",
            "gap.missing.semantic.primaryDimension" => "medium",
            "gap.topology.scatteredFilters" => "medium",
            _ => "medium",
        };

        if (defaultPriority == "high")
        {
            return defaultPriority;
        }

        var validatedGapCount = (storyGapAssessment?.Gaps ?? Array.Empty<StoryGapRecord>())
            .Count(gap => IsFutureContractCandidateGapId(gap.GapId));

        return gapId == "gap.missing.semantic.primaryDimension" && validatedGapCount >= 4
            ? "high"
            : defaultPriority;
    }

    private static int GetGuidedStoryImprovementPriorityRank(string priority)
    {
        return priority switch
        {
            "high" => 0,
            "medium" => 1,
            _ => 2,
        };
    }

    private static int GetGuidedStoryImprovementSequenceRank(string improvementId)
    {
        return improvementId switch
        {
            "missing-title-question-anchor" => 0,
            "missing-benchmark-target" => 1,
            "missing-primary-metric" => 2,
            "missing-primary-dimension" => 3,
            "missing-prior-period-context" => 4,
            "scattered-filters" => 5,
            _ => 6,
        };
    }

    private static string BuildGuidedStoryImprovementRationale(
        IReadOnlyList<GuidedStoryImprovement> improvements)
    {
        if (improvements.Count == 0)
        {
            return string.Empty;
        }

        var ids = improvements
            .Select(improvement => improvement.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (ids.Contains("missing-benchmark-target") && ids.Contains("missing-prior-period-context"))
        {
            return "The page clearly communicates current performance, but readers cannot tell whether results are good, bad, improving, or declining because benchmark and trend context are missing.";
        }

        if (ids.Contains("missing-primary-metric") && ids.Contains("scattered-filters"))
        {
            return "The page has useful content, but the headline metric is not obvious enough and the filter path competes with the story instead of reinforcing it.";
        }

        if (ids.Contains("missing-title-question-anchor"))
        {
            return "The page has a recognizable story, but the headline question is still too implicit for readers to understand the point quickly.";
        }

        if (ids.Contains("missing-benchmark-target"))
        {
            return "The page surfaces a current result, but readers cannot judge whether it is on track because no visible benchmark or target anchors the interpretation.";
        }

        if (ids.Contains("missing-prior-period-context"))
        {
            return "The page shows current performance, but readers cannot tell whether it is improving or declining because prior-period context is missing.";
        }

        if (ids.Contains("missing-primary-metric"))
        {
            return "The page includes relevant evidence, but the headline metric is still too implicit for readers to identify the main takeaway quickly.";
        }

        if (ids.Contains("missing-primary-dimension"))
        {
            return "The page includes relevant evidence, but the main comparison group is not explicit enough for readers to see where the key comparison lives.";
        }

        if (ids.Contains("scattered-filters"))
        {
            return "The page story is understandable, but the filter path is fragmented enough to compete with the main reading flow.";
        }

        return "The page has a recognizable story, but a few missing anchors still make the reading path harder to interpret quickly.";
    }

    private static string BuildMissingSignalGapDescription(StorySignalRegistryEntry entry)
    {
        return entry.Id switch
        {
            "layout.meaningfulVisibleTitle" => "Add a visible page title or question anchor so the report states its story intent in the scan path.",
            "context.targetBenchmarkPresent" => "Add a visible target, budget, or benchmark so the current result has explicit decision context.",
            "semantic.primaryMetric" => "Clarify the primary business metric in semantic metadata or visible labeling so the page has a stable quantitative anchor.",
            "semantic.primaryDimension" => "Clarify the primary comparison dimension so the grouping logic is explicit to both the model and the reader.",
            "layout.leadVisualType" => "Promote one clear lead visual so the page has a recognizable narrative starting point.",
            _ => $"Strengthen the missing story signal '{entry.Id}' so the page provides clearer narrative evidence.",
        };
    }

    private static StoryArchetypeMatchResult EvaluateArchetype(
        IReadOnlyDictionary<string, StorySignalRegistryEntry> entriesById,
        StoryArchetypeId archetypeId,
        IReadOnlyList<ArchetypeExpectation> expectations)
    {
        double totalWeight = expectations.Sum(expectation => expectation.Weight);
        double matchedWeight = 0d;
        var matchedSignals = new List<string>();
        var missedSignals = new List<string>();
        var explanationHooks = new List<string>();

        foreach (var expectation in expectations)
        {
            bool matched = expectation.IsMatched(entriesById);
            if (matched)
            {
                matchedWeight += expectation.Weight;
                matchedSignals.Add(expectation.DescribeMatched(entriesById));
                explanationHooks.Add(expectation.GetExplanationHook(entriesById));
            }
            else
            {
                missedSignals.Add(expectation.DescribeMissed(entriesById));
                explanationHooks.Add(expectation.GetExplanationHook(entriesById));
            }
        }

        return new StoryArchetypeMatchResult
        {
            ArchetypeId = archetypeId,
            SurfaceScope = StoryAssessmentSurfaceScope.PbirSpecific,
            MatchScore = totalWeight <= 0d ? 0d : Math.Round(matchedWeight / totalWeight, 2),
            MatchConfidence = StoryArchetypeMatchConfidence.Low,
            MatchedSignals = matchedSignals,
            MissedSignals = missedSignals,
            ExplanationHooks = explanationHooks
                .Where(hook => !string.IsNullOrWhiteSpace(hook))
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            ValidationStatus = StoryArchetypeValidationStatus.NeedsLevel1Review,
            PromotionEligibilityState = StoryAssessmentPromotionEligibilityState.NotEligible,
            PromotionState = StoryAssessmentPromotionState.Internal,
        };
    }

    private static StoryArchetypeMatchResult ApplyTopologyReinforcement(
        StoryArchetypeMatchResult result,
        StoryFilterTopologyAssessment? topologyAssessment)
    {
        if (topologyAssessment is null || topologyAssessment.Signals.Count == 0)
        {
            return result;
        }

        var bonusMap = BuildTopologyReinforcementBonusMap(topologyAssessment.Signals);
        if (!bonusMap.TryGetValue(result.ArchetypeId, out var bonus) || bonus <= 0d)
        {
            return result;
        }

        var boundedBonus = result.MatchScore >= 0.45d
            ? Math.Min(0.12d, bonus)
            : Math.Min(0.05d, bonus);
        if (boundedBonus <= 0d)
        {
            return result;
        }

        var hooks = result.ExplanationHooks.ToList();
        hooks.Add($"Filter topology reinforcement contributed {boundedBonus:0.##} to {result.ArchetypeId} without acting as primary narrative evidence.");

        return new StoryArchetypeMatchResult
        {
            ArchetypeId = result.ArchetypeId,
            SurfaceScope = result.SurfaceScope,
            MatchScore = Math.Round(Math.Min(1d, result.MatchScore + boundedBonus), 2),
            MatchConfidence = result.MatchConfidence,
            MatchedSignals = result.MatchedSignals,
            MissedSignals = result.MissedSignals,
            ExplanationHooks = hooks
                .Where(hook => !string.IsNullOrWhiteSpace(hook))
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            ValidationStatus = result.ValidationStatus,
            PromotionEligibilityState = result.PromotionEligibilityState,
            PromotionState = result.PromotionState,
        };
    }

    private static StoryArchetypeMatchResult FinalizeArchetypeResult(
        StoryArchetypeMatchResult result,
        double marginToNearestCompetitor,
        bool isBestFit)
    {
        var confidence = DetermineArchetypeMatchConfidence(result.MatchScore, marginToNearestCompetitor, result.MatchedSignals.Count, isBestFit);
        var validationStatus = confidence switch
        {
            StoryArchetypeMatchConfidence.High when isBestFit => StoryArchetypeValidationStatus.ReadyForPromotionReview,
            StoryArchetypeMatchConfidence.Low when isBestFit => StoryArchetypeValidationStatus.AmbiguousNeedsReview,
            _ => StoryArchetypeValidationStatus.NeedsLevel1Review,
        };
        var promotionEligibilityState = confidence switch
        {
            StoryArchetypeMatchConfidence.High when isBestFit => StoryAssessmentPromotionEligibilityState.ReadyForPromotionReview,
            StoryArchetypeMatchConfidence.Medium when isBestFit => StoryAssessmentPromotionEligibilityState.Level1ReviewCandidate,
            _ => StoryAssessmentPromotionEligibilityState.NotEligible,
        };

        return new StoryArchetypeMatchResult
        {
            ArchetypeId = result.ArchetypeId,
            SurfaceScope = result.SurfaceScope,
            MatchScore = result.MatchScore,
            MatchConfidence = confidence,
            MatchedSignals = result.MatchedSignals,
            MissedSignals = result.MissedSignals,
            ExplanationHooks = result.ExplanationHooks,
            ValidationStatus = validationStatus,
            PromotionEligibilityState = promotionEligibilityState,
            PromotionState = DetermineArchetypePromotionState(validationStatus, promotionEligibilityState),
        };
    }

    private static StoryArchetypeMatchConfidence DetermineArchetypeMatchConfidence(
        double matchScore,
        double marginToNearestCompetitor,
        int matchedSignalCount,
        bool isBestFit)
    {
        if (isBestFit && matchScore >= 0.75d && marginToNearestCompetitor >= 0.15d && matchedSignalCount >= 4)
        {
            return StoryArchetypeMatchConfidence.High;
        }

        if (isBestFit && matchScore >= 0.65d && marginToNearestCompetitor >= 0.12d && matchedSignalCount >= 3)
        {
            return StoryArchetypeMatchConfidence.Medium;
        }

        if (!isBestFit && matchScore >= 0.60d)
        {
            return StoryArchetypeMatchConfidence.Medium;
        }

        return StoryArchetypeMatchConfidence.Low;
    }

    private static StoryAssessmentPromotionState DetermineArchetypePromotionState(
        StoryArchetypeValidationStatus validationStatus,
        StoryAssessmentPromotionEligibilityState promotionEligibilityState)
    {
        return validationStatus == StoryArchetypeValidationStatus.ReadyForPromotionReview &&
               promotionEligibilityState == StoryAssessmentPromotionEligibilityState.ReadyForPromotionReview
            ? StoryAssessmentPromotionState.Internal
            : StoryAssessmentPromotionState.Internal;
    }

    private static StoryAssessmentSurfaceScope DetermineArchetypeClassificationSurfaceScope(
        IReadOnlyList<StoryArchetypeMatchResult> results)
    {
        return results.Any(result => result.SurfaceScope == StoryAssessmentSurfaceScope.CrossSurfaceCandidate)
            ? StoryAssessmentSurfaceScope.CrossSurfaceCandidate
            : StoryAssessmentSurfaceScope.PbirSpecific;
    }

    private static StoryAssessmentPromotionState DetermineCanonicalPromotionState(
        IEnumerable<StoryAssessmentPromotionState> promotionStates)
    {
        return promotionStates.Any(state => state != StoryAssessmentPromotionState.Internal)
            ? promotionStates.First(state => state != StoryAssessmentPromotionState.Internal)
            : StoryAssessmentPromotionState.Internal;
    }

    private static StoryAssessmentPromotionState DetermineSemanticCoherencePromotionState(
        StoryCompetingStoryStatus competingStoryStatus,
        bool isSparse)
    {
        return StoryAssessmentPromotionState.Internal;
    }

    private static StoryAssessmentSurfaceScope DetermineTopologySurfaceScope(
        IReadOnlyList<StoryFilterTopologySignal> signals)
    {
        return signals.Any(signal => signal.SurfaceScope == StoryAssessmentSurfaceScope.CrossSurfaceCandidate)
            ? StoryAssessmentSurfaceScope.CrossSurfaceCandidate
            : StoryAssessmentSurfaceScope.PbirSpecific;
    }

    private static IReadOnlyList<ArchetypeExpectation> BuildPerformanceMonitorExpectations() =>
    [
        ExpectSignal("layout.leadIntent", 0.20d, entry => EntryRawValueEquals(entry, "comparison"), "lead intent comparison"),
        ExpectSignal("layout.topScanKpiCount", 0.18d, entry => EntryRawIntAtLeast(entry, 1), "KPI layer present"),
        ExpectSignal("context.targetBenchmarkPresent", 0.22d, entry => entry.Fired, "target or benchmark context present"),
        ExpectSignal("layout.meaningfulVisibleTitle", 0.18d, entry => TitleContainsAny(entry?.RawValue, "performance", "monitor", "kpi", "scorecard"), "monitor-oriented title"),
        ExpectSignal("layout.supportingEvidenceFlow", 0.12d, entry => entry.Fired, "supporting evidence flow present"),
        ExpectSignal("semantic.primaryMetric", 0.10d, entry => entry.Fired, "primary metric inferred"),
    ];

    private static IReadOnlyList<ArchetypeExpectation> BuildTrendExceptionExpectations() =>
    [
        ExpectSignal("layout.leadIntent", 0.28d, entry => EntryRawValueEquals(entry, "trend"), "lead intent trend"),
        ExpectSignal("context.priorPeriodContext", 0.18d, entry => entry.Fired, "prior-period context present"),
        ExpectSignal("context.targetBenchmarkPresent", 0.16d, entry => entry.Fired, "target or benchmark context present"),
        ExpectSignal("layout.meaningfulVisibleTitle", 0.18d, entry => TitleContainsAny(entry?.RawValue, "trend", "exception", "variance", "risk", "alert", "monitor"), "trend/exception title cue"),
        ExpectSignal("layout.supportingEvidenceFlow", 0.10d, entry => entry.Fired, "supporting evidence flow present"),
        ExpectSignal("semantic.primaryMetric", 0.10d, entry => entry.Fired, "primary metric inferred"),
    ];

    private static IReadOnlyList<ArchetypeExpectation> BuildRankingExpectations() =>
    [
        ExpectSignal("layout.leadVisualType", 0.24d, entry => RawValueContainsAny(entry?.RawValue, "bar", "column"), "bar/column lead visual"),
        ExpectSignal("layout.leadIntent", 0.16d, entry => EntryRawValueEquals(entry, "comparison"), "lead intent comparison"),
        ExpectSignal("semantic.primaryDimension", 0.18d, entry => entry.Fired, "ranking dimension inferred"),
        ExpectSignal("layout.meaningfulVisibleTitle", 0.26d, entry => TitleContainsAny(entry?.RawValue, "top", "rank", "bottom", "highest", "lowest"), "ranking title cue"),
        ExpectSignal("semantic.primaryMetric", 0.08d, entry => entry.Fired, "primary metric inferred"),
        ExpectSignal("layout.supportingEvidenceFlow", 0.08d, entry => entry.Fired, "supporting evidence flow present"),
    ];

    private static IReadOnlyList<ArchetypeExpectation> BuildComparisonExpectations() =>
    [
        ExpectSignal("layout.leadIntent", 0.24d, entry => EntryRawValueEquals(entry, "comparison"), "lead intent comparison"),
        ExpectSignal("semantic.primaryDimension", 0.18d, entry => entry.Fired, "comparison dimension inferred"),
        ExpectSignal("layout.meaningfulVisibleTitle", 0.24d, entry => TitleContainsAny(entry?.RawValue, "vs", "versus", "compare", "comparison", "by"), "comparison title cue"),
        ExpectSignal("context.targetBenchmarkPresent", 0.12d, entry => entry.Fired, "target/budget benchmark context present"),
        ExpectSignal("layout.supportingEvidenceFlow", 0.12d, entry => entry.Fired, "supporting evidence flow present"),
        ExpectSignal("semantic.primaryMetric", 0.10d, entry => entry.Fired, "primary metric inferred"),
    ];

    private static IReadOnlyList<ArchetypeExpectation> BuildDecompositionExpectations() =>
    [
        ExpectSignal("layout.leadIntent", 0.30d, entry => EntryRawValueEquals(entry, "composition"), "lead intent composition"),
        ExpectSignal("layout.leadVisualType", 0.20d, entry => RawValueContainsAny(entry?.RawValue, "stacked", "pie", "donut", "funnel"), "composition-oriented visual"),
        ExpectSignal("semantic.primaryDimension", 0.15d, entry => entry.Fired, "decomposition dimension inferred"),
        ExpectSignal("layout.meaningfulVisibleTitle", 0.20d, entry => TitleContainsAny(entry?.RawValue, "share", "mix", "composition", "contribution", "breakdown"), "decomposition title cue"),
        ExpectSignal("semantic.primaryMetric", 0.08d, entry => entry.Fired, "primary metric inferred"),
        ExpectSignal("layout.supportingEvidenceFlow", 0.07d, entry => entry.Fired, "supporting evidence flow present"),
    ];

    private static IReadOnlyList<ArchetypeExpectation> BuildNarrativeWalkthroughExpectations() =>
    [
        ExpectSignal("layout.meaningfulVisibleTitle", 0.18d, entry => entry?.Fired == true, "meaningful narrative title"),
        ExpectSignal("layout.supportingEvidenceFlow", 0.24d, entry => entry.Fired, "supporting evidence flow present"),
        ExpectSignal("semantic.primaryMetric", 0.14d, entry => entry.Fired, "primary metric inferred"),
        ExpectSignal("semantic.primaryDimension", 0.10d, entry => entry.Fired, "supporting dimension inferred"),
        ExpectSignal("semantic.richMetadataSupport", 0.10d, entry => entry.Fired, "rich metadata support present"),
        ExpectSignal("layout.meaningfulVisibleTitle", 0.18d, entry => TitleContainsAny(entry?.RawValue, "why", "story", "journey", "walkthrough", "explained"), "storytelling title cue"),
        ExpectSignal("layout.leadIntent", 0.06d, entry => entry?.Fired == true, "lead analytical intent inferred"),
    ];

    private static ArchetypeExpectation ExpectSignal(
        string signalId,
        double weight,
        Func<StorySignalRegistryEntry?, bool> predicate,
        string narrativeLabel)
    {
        return new ArchetypeExpectation(
            signalId,
            weight,
            entriesById => predicate(GetSignal(entriesById, signalId)),
            entriesById => DescribeSignalState(GetSignal(entriesById, signalId), signalId, narrativeLabel, matched: true),
            entriesById => DescribeSignalState(GetSignal(entriesById, signalId), signalId, narrativeLabel, matched: false),
            entriesById => GetSignal(entriesById, signalId)?.ExplanationHook ?? narrativeLabel);
    }

    private static StorySignalRegistryEntry? GetSignal(
        IReadOnlyDictionary<string, StorySignalRegistryEntry> entriesById,
        string signalId) =>
        entriesById.TryGetValue(signalId, out var entry) ? entry : null;

    private static string DescribeSignalState(
        StorySignalRegistryEntry? entry,
        string signalId,
        string narrativeLabel,
        bool matched)
    {
        if (entry is null)
        {
            return $"{signalId}: {(matched ? "missing match target" : "missing")} ({narrativeLabel})";
        }

        var rawSuffix = string.IsNullOrWhiteSpace(entry.RawValue)
            ? string.Empty
            : $" [{entry.RawValue}]";
        return $"{signalId}: {(matched ? "matched" : "missed")} {narrativeLabel}{rawSuffix}";
    }

    private static bool EntryRawValueEquals(StorySignalRegistryEntry? entry, string expectedValue) =>
        entry is not null &&
        string.Equals(entry.RawValue, expectedValue, StringComparison.OrdinalIgnoreCase);

    private static bool EntryRawIntAtLeast(StorySignalRegistryEntry? entry, int minimum) =>
        entry is not null &&
        int.TryParse(entry.RawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue) &&
        parsedValue >= minimum;

    private static IReadOnlyDictionary<StoryArchetypeId, double> BuildTopologyReinforcementBonusMap(
        IReadOnlyList<StoryFilterTopologySignal> signals)
    {
        var bonuses = Enum.GetValues<StoryArchetypeId>()
            .ToDictionary(archetype => archetype, _ => 0d);

        foreach (var signal in signals.Where(signal => signal.Fired && signal.SupportsArchetypeReinforcement))
        {
            switch (signal.Id)
            {
                case "topology.hierarchicalTimeFilter":
                    bonuses[StoryArchetypeId.TrendException] += 0.10d;
                    bonuses[StoryArchetypeId.PerformanceMonitor] += 0.04d;
                    break;
                case "topology.scopedFilterContext":
                    bonuses[StoryArchetypeId.TrendException] += 0.04d;
                    bonuses[StoryArchetypeId.Comparison] += 0.06d;
                    bonuses[StoryArchetypeId.Ranking] += 0.04d;
                    break;
                case "topology.reportLevelContext":
                    bonuses[StoryArchetypeId.TrendException] += 0.03d;
                    bonuses[StoryArchetypeId.Comparison] += 0.02d;
                    break;
                case "topology.consistentControlBand":
                    bonuses[StoryArchetypeId.Comparison] += 0.04d;
                    bonuses[StoryArchetypeId.NarrativeWalkthrough] += 0.04d;
                    break;
            }
        }

        return bonuses;
    }

    private static StoryAssessmentValidationRating DetermineAggregateTopologyRating(
        IEnumerable<StoryAssessmentValidationRating> ratings)
    {
        var distinctRatings = ratings
            .Where(rating => rating != StoryAssessmentValidationRating.NotAssessed)
            .Distinct()
            .ToList();

        if (distinctRatings.Count == 0)
        {
            return StoryAssessmentValidationRating.NotAssessed;
        }

        if (distinctRatings.Contains(StoryAssessmentValidationRating.Strong))
        {
            return StoryAssessmentValidationRating.Strong;
        }

        if (distinctRatings.Contains(StoryAssessmentValidationRating.Mixed))
        {
            return StoryAssessmentValidationRating.Mixed;
        }

        return StoryAssessmentValidationRating.Weak;
    }

    private static bool TitleContainsAny(string? rawTitle, params string[] keywords) =>
        keywords.Any(keyword => TextContainsPhrase(rawTitle ?? string.Empty, keyword));

    private static bool RawValueContainsAny(string? rawValue, params string[] snippets) =>
        snippets.Any(snippet => rawValue?.Contains(snippet, StringComparison.OrdinalIgnoreCase) == true);

    private static StorySignalRegistryEntry CreateStorySignalEntry(
        string id,
        StorySignalCategory category,
        string? rawValue,
        bool fired,
        StorySignalContributionIntent contributionIntent,
        StorySignalRemediability remediability,
        string explanationHook,
        StoryAssessmentSurfaceScope surfaceScope,
        StorySignalRequirementRole requirementRole,
        StorySignalEvidenceRole evidenceRole,
        StoryAssessmentExplanationType explanationType,
        StoryAssessmentActionabilityType actionabilityType)
    {
        return new StorySignalRegistryEntry
        {
            Id = id,
            Category = category,
            RawValue = rawValue,
            Fired = fired,
            ContributionIntent = contributionIntent,
            Remediability = remediability,
            ExplanationHook = explanationHook,
            ReliabilityState = StorySignalReliabilityState.Candidate,
            SurfaceScope = surfaceScope,
            RequirementRole = requirementRole,
            EvidenceRole = evidenceRole,
            ExplanationType = explanationType,
            ActionabilityType = actionabilityType,
            PromotionState = StoryAssessmentPromotionState.Internal,
            Evaluations = Array.Empty<StoryAssessmentDimensionEvaluation>(),
        };
    }

    private sealed record ArchetypeExpectation(
        string SignalId,
        double Weight,
        Func<IReadOnlyDictionary<string, StorySignalRegistryEntry>, bool> IsMatched,
        Func<IReadOnlyDictionary<string, StorySignalRegistryEntry>, string> DescribeMatched,
        Func<IReadOnlyDictionary<string, StorySignalRegistryEntry>, string> DescribeMissed,
        Func<IReadOnlyDictionary<string, StorySignalRegistryEntry>, string> GetExplanationHook);

    private sealed class SemanticCoherenceClusterAccumulator
    {
        public double Weight { get; set; }

        public HashSet<string> Terms { get; } = new(StringComparer.Ordinal);

        public HashSet<string> RawTexts { get; } = new(StringComparer.Ordinal);
    }

    private static string CleanMetricLabel(string? label)
    {
        var normalized = NormalizeSemanticHint(label);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "performance";
        }

        normalized = Regex.Replace(normalized, @"^(ytd|mtd|qtd|fy|cy|py)\s+", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\s+(ytd|mtd|qtd|fy|cy|py)$", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return normalized.Trim();
    }

    private static SemanticNarrativeSignals AnalyzeSemanticNarrativeSignals(
        NarrativePageAnalysis analysis,
        VisualData? leadVisual)
    {
        var leadMeasureCandidates = SelectSemanticHintCandidates(leadVisual?.FieldRoles.MeasureHints);
        var pageMeasureCandidates = SelectSemanticHintCandidates(analysis.VisibleDataVisuals.SelectMany(visual => visual.FieldRoles.MeasureHints));
        var leadCategoryCandidates = SelectSemanticHintCandidates(leadVisual?.FieldRoles.CategoryHints);
        var pageCategoryCandidates = SelectSemanticHintCandidates(analysis.VisibleDataVisuals.SelectMany(visual => visual.FieldRoles.CategoryHints));

        var primaryMetric = ExtractPrimaryStoryMetric(analysis, leadVisual);
        var primaryDimension = ExtractPrimaryStoryDimension(analysis, leadVisual);

        int confidenceBonus = 0;
        if (!IsGenericStoryMetric(primaryMetric) && !string.IsNullOrWhiteSpace(primaryDimension))
        {
            confidenceBonus += 1;
        }

        if (HasRichSemanticSupport(leadMeasureCandidates, leadVisual?.FieldRoles.MeasureHints) ||
            HasRichSemanticSupport(leadCategoryCandidates, leadVisual?.FieldRoles.CategoryHints))
        {
            confidenceBonus += 1;
        }

        if (HasSemanticTextAlignment(analysis.VisibleTitle, primaryMetric, primaryDimension) ||
            HasSemanticTextAlignment(leadVisual?.BestVisibleText, primaryMetric, primaryDimension))
        {
            confidenceBonus += 1;
        }

        confidenceBonus = Math.Min(confidenceBonus, 3);

        var evidence = new List<string>();
        if (!IsGenericStoryMetric(primaryMetric) || !string.IsNullOrWhiteSpace(primaryDimension))
        {
            var semanticSummary = string.IsNullOrWhiteSpace(primaryDimension)
                ? $"Lead visual semantic metadata emphasizes {primaryMetric}"
                : $"Lead visual semantic metadata emphasizes {primaryMetric} by {primaryDimension}";
            evidence.Add(semanticSummary);
        }

        if (HasRichSemanticSupport(leadMeasureCandidates, leadVisual?.FieldRoles.MeasureHints) ||
            HasRichSemanticSupport(leadCategoryCandidates, leadVisual?.FieldRoles.CategoryHints))
        {
            evidence.Add("Additional semantic metadata aliases and descriptions reinforce the same business concept");
        }

        return new SemanticNarrativeSignals(
            primaryMetric,
            primaryDimension,
            confidenceBonus,
            evidence,
            HasRichSemanticSupport(leadMeasureCandidates, leadVisual?.FieldRoles.MeasureHints) ||
                HasRichSemanticSupport(leadCategoryCandidates, leadVisual?.FieldRoles.CategoryHints),
            HasSemanticTextAlignment(analysis.VisibleTitle, primaryMetric, primaryDimension) ||
                HasSemanticTextAlignment(leadVisual?.BestVisibleText, primaryMetric, primaryDimension));
    }

    private static string BuildExecutiveHeadlinePhrase(string metricLabel)
    {
        if (string.IsNullOrWhiteSpace(metricLabel) || IsGenericStoryMetric(metricLabel))
        {
            return "headline performance";
        }

        return ContainsPerformanceLikeWord(metricLabel)
            ? $"headline {metricLabel}"
            : $"headline {metricLabel} performance";
    }

    private static string BuildMetricPerformancePhrase(string metricLabel)
    {
        if (string.IsNullOrWhiteSpace(metricLabel) || IsGenericStoryMetric(metricLabel))
        {
            return "performance";
        }

        return ContainsPerformanceLikeWord(metricLabel)
            ? metricLabel
            : $"{metricLabel} performance";
    }

    private static bool ContainsPerformanceLikeWord(string metricLabel)
    {
        var normalized = NormalizeSemanticHint(metricLabel);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return Regex.IsMatch(
            normalized,
            @"\b(performance|result|results|variance|gap|forecast|revenue|sales|margin|profit|cost|volume|inventory|pipeline|attainment)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static List<string> SelectSemanticHintCandidates(IEnumerable<string>? hints, int maxCount = 3)
    {
        if (hints is null)
        {
            return [];
        }

        return hints
            .Select(NormalizeSemanticHint)
            .Where(hint => !string.IsNullOrWhiteSpace(hint))
            .Select((hint, index) => new { Hint = hint!, Index = index })
            .GroupBy(item => item.Hint, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(item => ScoreSemanticHint(item.Hint))
            .ThenBy(item => item.Index)
            .Take(maxCount)
            .Select(item => item.Hint)
            .ToList();
    }

    private static bool HasRichSemanticSupport(
        IReadOnlyCollection<string> preferredCandidates,
        IEnumerable<string>? rawHints)
    {
        if (preferredCandidates.Count > 1)
        {
            return true;
        }

        if (rawHints is null)
        {
            return false;
        }

        return rawHints
            .Select(NormalizeSemanticHint)
            .Where(hint => !string.IsNullOrWhiteSpace(hint))
            .Select(hint => hint!)
            .Any(hint => hint.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 5);
    }

    private static bool HasSemanticTextAlignment(
        string? text,
        params string?[] semanticHints)
    {
        var normalizedText = NormalizeSemanticHint(text);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return false;
        }

        var textKeywords = ExtractSemanticKeywords(normalizedText);
        if (textKeywords.Count == 0)
        {
            return false;
        }

        return semanticHints
            .Where(hint => !string.IsNullOrWhiteSpace(hint))
            .SelectMany(hint => ExtractSemanticKeywords(hint!))
            .Any(textKeywords.Contains);
    }

    private static HashSet<string> ExtractSemanticKeywords(string text)
    {
        return text
            .Split([' ', '/', '-', '&'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim().ToLowerInvariant())
            .Where(token => token.Length >= 4)
            .Where(token => token is not "with" and not "from" and not "into" and not "using" and not "over" and not "view")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsGenericStoryMetric(string? metricLabel)
    {
        var normalized = NormalizeSemanticHint(metricLabel);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        return normalized.Equals("performance", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("metric", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("value", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("amount", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("measure", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("count", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("total", StringComparison.OrdinalIgnoreCase);
    }

    private static string? SelectPreferredSemanticHint(IEnumerable<string>? hints)
    {
        if (hints is null)
        {
            return null;
        }

        return hints
            .Select(NormalizeSemanticHint)
            .Where(hint => !string.IsNullOrWhiteSpace(hint))
            .Select(hint => new { Hint = hint!, Score = ScoreSemanticHint(hint!) })
            .OrderByDescending(item => item.Score)
            .Select(item => item.Hint)
            .FirstOrDefault();
    }

    private static int ScoreSemanticHint(string hint)
    {
        var score = 0;

        if (Regex.IsMatch(hint, @"^[A-Za-z][A-Za-z0-9&/\-\s]{2,60}$", RegexOptions.CultureInvariant))
        {
            score += 4;
        }

        if (hint.Contains(' '))
        {
            score += 2;
        }

        var wordCount = hint.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount is >= 1 and <= 4)
        {
            score += 3;
        }
        else if (wordCount <= 8)
        {
            score += 1;
        }
        else
        {
            score -= 4;
        }

        if (hint.Contains('[') || hint.Contains(']') || hint.Contains('.') || hint.Contains('_'))
        {
            score -= 3;
        }

        if (hint.StartsWith("Sum of ", StringComparison.OrdinalIgnoreCase))
        {
            score -= 2;
        }

        return score;
    }

    private static string? NormalizeSemanticHint(string? hint)
    {
        var normalized = NormalizeText(hint);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var bracketMatch = Regex.Match(normalized, @"(?:.+)?\[(?<label>[^\]]+)\]$", RegexOptions.CultureInvariant);
        if (bracketMatch.Success)
        {
            normalized = bracketMatch.Groups["label"].Value;
        }
        else if (normalized.Contains('.', StringComparison.Ordinal))
        {
            normalized = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries).Last();
        }

        normalized = normalized.Trim('\'', '"');
        normalized = normalized.Replace('_', ' ');
        normalized = Regex.Replace(normalized, @"\s{2,}", " ", RegexOptions.CultureInvariant);
        return normalized.Trim();
    }

    private static bool IsTableLikeVisual(VisualData visual) =>
        visual.Type is "table" or "matrix";

    private static bool ContainsOperationalMonitoringCue(string? title) =>
        ContainsTextKeyword(title, "monitor") ||
        ContainsTextKeyword(title, "operations") ||
        ContainsTextKeyword(title, "daily") ||
        ContainsTextKeyword(title, "weekly") ||
        ContainsTextKeyword(title, "performance");

    private static string ClassifyAnalyticalTask(
        string visualType,
        IReadOnlyList<string> categoryHints,
        IReadOnlyList<string> seriesHints,
        IReadOnlyList<string> measureHints,
        string? title)
    {
        var normalizedVisualType = visualType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedVisualType is "table" or "matrix")
        {
            return "table-reference";
        }

        if (normalizedVisualType is "scatterchart" or "bubblechart")
        {
            return "relationship";
        }

        if (normalizedVisualType is "piechart" or "donutchart" or "stackedcolumnchart" or "stackedbarchart" or "stackedareachart" or "funnel" or "funnelchart")
        {
            return "composition";
        }

        if (normalizedVisualType is "linechart" or "areachart" or "lineandstackedcolumnchart" or "lineandclusteredcolumnchart")
        {
            var evidenceTexts = categoryHints
                .Concat(seriesHints)
                .Concat(measureHints)
                .Append(title ?? string.Empty);
            return evidenceTexts.Any(ContainsSequentialKeywords)
                ? "trend"
                : "comparison";
        }

        if (normalizedVisualType.Contains("bar", StringComparison.Ordinal) ||
            normalizedVisualType.Contains("column", StringComparison.Ordinal) ||
            normalizedVisualType is "barchart" or "columnchart" or "waterfallchart" or "card" or "kpivisual" or "multirowcard")
        {
            return "comparison";
        }

        return "comparison";
    }

    private static List<string> InferChartIntentEvidence(VisualData visual, PageData page)
    {
        var evidence = new List<string> { visual.Type };
        evidence.AddRange(visual.FieldRoles.CategoryHints.Take(1));
        evidence.AddRange(visual.FieldRoles.SeriesHints.Take(1));
        evidence.AddRange(visual.FieldRoles.MeasureHints.Take(1));
        if (!string.IsNullOrWhiteSpace(visual.BestVisibleText))
        {
            evidence.Add(visual.BestVisibleText!);
        }
        else if (!string.IsNullOrWhiteSpace(GetPageVisibleTitle(page)))
        {
            evidence.Add(GetPageVisibleTitle(page)!);
        }

        return evidence
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string InferChartIntentConfidence(VisualData visual, IReadOnlyCollection<string> evidence)
    {
        if (visual.FieldRoles.CategoryHints.Count > 0 || visual.FieldRoles.MeasureHints.Count > 0)
        {
            return "high";
        }

        return evidence.Count >= 2 ? "medium" : "low";
    }

    private static double AddSemanticColorConsistencyFeedback(
        List<FrameworkFeedbackItem> feedback,
        List<string> recs,
        List<PageData> pages)
    {
        var conflicts = BuildSemanticColorConflicts(pages);
        if (conflicts.Count == 0)
        {
            if (HasRepeatedConsistentSemanticMappings(pages))
            {
                feedback.Add(FeedbackItem(
                    true,
                    "Semantic color consistency: Repeated semantic color mappings remain consistent across the report.",
                    FindingTypes.StrongHeuristic));
            }

            return 0.0;
        }

        var issue = conflicts
            .OrderByDescending(conflict => conflict.Assignments.Count)
            .ThenBy(conflict => conflict.PageName, StringComparer.OrdinalIgnoreCase)
            .First();
        recs.Add("[High] Semantic Color: Keep the same category or status meaning on the same color across visuals and pages.");
        feedback.Add(FeedbackItem(
            false,
            $"Semantic color consistency: {issue.DisplayLabel} uses multiple colors on {issue.PageName} ({string.Join(", ", issue.Colors)}). Keep repeated meanings visually stable.",
            FindingTypes.StrongHeuristic,
            BuildAffectedVisuals(pages, issue.Assignments)));

        return Math.Min(12.0, conflicts.Count * 6.0);
    }

    private static List<SemanticColorConflict> BuildSemanticColorConflicts(List<PageData> pages)
    {
        return pages
            .SelectMany(ExtractSemanticColorAssignments)
            .GroupBy(assignment => new { assignment.SourcePageName, assignment.SemanticKey })
            .Select(group => new
            {
                group.Key.SourcePageName,
                group.Key.SemanticKey,
                DisplayLabel = group.Select(assignment => assignment.DisplayLabel)
                    .FirstOrDefault(label => !string.IsNullOrWhiteSpace(label)) ?? group.Key.SemanticKey,
                Colors = group.Select(assignment => assignment.Color)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(color => color, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Assignments = group.ToList(),
            })
            .Where(group => group.Colors.Count > 1)
            .Select(group => new SemanticColorConflict(group.SourcePageName, group.SemanticKey, group.DisplayLabel, group.Colors, group.Assignments))
            .ToList();
    }

    private static bool HasRepeatedConsistentSemanticMappings(List<PageData> pages) =>
        pages.SelectMany(ExtractSemanticColorAssignments)
            .GroupBy(assignment => assignment.SemanticKey, StringComparer.OrdinalIgnoreCase)
            .Any(group =>
                group.Count() > 1 &&
                group.Select(assignment => assignment.Color).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1);

    private static double AddSemanticStatusAccessibilityFeedback(
        List<FrameworkFeedbackItem> feedback,
        List<string> recs,
        List<PageData> pages)
    {
        var issues = BuildStatusColorConflicts(pages);
        if (issues.Count == 0)
        {
            return 0.0;
        }

        var issue = issues
            .OrderByDescending(item => item.Assignments.Count)
            .ThenBy(item => item.PageName, StringComparer.OrdinalIgnoreCase)
            .First();
        recs.Add("[Medium] Semantic Color: Reserve red/green for consistent bad/good status semantics only.");
        feedback.Add(FeedbackItem(
            false,
            $"Status color semantics: {issue.SemanticKey} uses {issue.Color} on {issue.PageName}. Reserve red/green for consistent bad/good status semantics only.",
            FindingTypes.StrongHeuristic,
            BuildAffectedVisuals(pages, issues.SelectMany(item => item.Assignments))));

        return Math.Min(12.0, issues.Count * 6.0);
    }

    private static List<StatusSemanticIssue> BuildStatusColorConflicts(List<PageData> pages)
    {
        return pages
            .SelectMany(ExtractSemanticColorAssignments)
            .Where(assignment => assignment.SemanticKey.StartsWith("status:", StringComparison.OrdinalIgnoreCase))
            .Where(assignment =>
                (IsNegativeStatusSemanticKey(assignment.SemanticKey) && IsGreenDominant(assignment.Color)) ||
                (IsPositiveStatusSemanticKey(assignment.SemanticKey) && IsRedDominant(assignment.Color)))
            .GroupBy(assignment => new { assignment.SourcePageName, assignment.SemanticKey, assignment.Color })
            .Select(group => new StatusSemanticIssue(group.Key.SourcePageName, group.Key.SemanticKey, group.Key.Color, group.ToList()))
            .ToList();
    }

    private static bool IsNegativeStatusSemanticKey(string semanticKey) =>
        semanticKey is "status:at-risk" or "status:off-track" or "status:bad" or "status:critical";

    private static bool IsPositiveStatusSemanticKey(string semanticKey) =>
        semanticKey is "status:on-track" or "status:good";

    private static List<AffectedVisualReference> BuildAffectedVisuals(
        List<PageData> pages,
        IEnumerable<SemanticColorAssignment> assignments)
    {
        var visualTypesByRef = pages
            .SelectMany(page => page.Visuals.Select(visual => new
            {
                PageName = page.DisplayName,
                VisualId = visual.Id,
                VisualType = visual.Type,
            }))
            .ToDictionary(
                item => $"{item.PageName}::{item.VisualId}",
                item => item.VisualType,
                StringComparer.OrdinalIgnoreCase);

        return assignments
            .Select(assignment =>
            {
                visualTypesByRef.TryGetValue($"{assignment.SourcePageName}::{assignment.SourceVisualId}", out var visualType);
                return new AffectedVisualReference(
                    assignment.SourcePageName,
                    assignment.SourceVisualId,
                    visualType ?? "visual");
            })
            .Distinct()
            .ToList();
    }

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
        var analysis = AnalyzeMetricLabelGovernance(pages);
        return analysis is null
            ? null
            : new ConsistencyIssue(analysis.Value.Message, analysis.Value.Visuals);
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

    private static ConsistencyIssue? AnalyzeTitleAlignmentConsistency(IEnumerable<PageData> pages)
    {
        var titleProfiles = pages
            .Select(BuildLayoutConventionProfile)
            .Where(profile => !string.IsNullOrWhiteSpace(profile.TitleAlignment) && profile.TitleVisual is not null)
            .ToList();
        if (titleProfiles.Count < 2)
        {
            return null;
        }

        var distinctTitleAlignments = titleProfiles
            .Select(profile => profile.TitleAlignment)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (distinctTitleAlignments.Count < 2)
        {
            return null;
        }

        var first = titleProfiles[0];
        var second = titleProfiles.First(profile =>
            !string.Equals(profile.TitleAlignment, first.TitleAlignment, StringComparison.Ordinal));
        return new ConsistencyIssue(
            $"title anchors shift between {first.TitleAlignment} and {second.TitleAlignment} alignment",
            DeduplicateAffectedVisualTuples(
            [
                (first.Page.DisplayName, first.TitleVisual!),
                (second.Page.DisplayName, second.TitleVisual!),
            ]));
    }

    private static ConsistencyIssue? AnalyzeFilterConventionConsistency(IEnumerable<PageData> pages)
    {
        var filterProfiles = pages
            .Select(BuildLayoutConventionProfile)
            .Where(profile => !string.IsNullOrWhiteSpace(profile.FilterConvention) && profile.FilterVisuals.Count > 0)
            .ToList();
        if (filterProfiles.Count < 2)
        {
            return null;
        }

        var distinctFilterConventions = filterProfiles
            .Select(profile => profile.FilterConvention)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (distinctFilterConventions.Count < 2)
        {
            return null;
        }

        var first = filterProfiles[0];
        var second = filterProfiles.First(profile =>
            !string.Equals(profile.FilterConvention, first.FilterConvention, StringComparison.Ordinal));
        return new ConsistencyIssue(
            $"filter conventions shift between a {first.FilterConvention} and a {second.FilterConvention}",
            DeduplicateAffectedVisualTuples(
                first.FilterVisuals.Take(2).Select(visual => (first.Page.DisplayName, visual))
                    .Concat(second.FilterVisuals.Take(2).Select(visual => (second.Page.DisplayName, visual)))));
    }

    private static ConsistencyIssue? AnalyzeCrossPageKpiBandConsistency(IEnumerable<PageData> pages)
    {
        var profiles = pages
            .Select(BuildKpiBandProfile)
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Convention) && profile.KpiVisuals.Count > 0)
            .ToList();
        if (profiles.Count < 2)
        {
            return null;
        }

        var distinctConventions = profiles
            .Select(profile => profile.Convention)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (distinctConventions.Count < 2)
        {
            return null;
        }

        var first = profiles[0];
        var second = profiles.First(profile =>
            !string.Equals(profile.Convention, first.Convention, StringComparison.Ordinal));
        return new ConsistencyIssue(
            $"top-row KPI cards shift between a {first.Convention} pattern on '{first.Page.DisplayName}' and a {second.Convention} pattern on '{second.Page.DisplayName}'",
            DeduplicateAffectedVisualTuples(
                first.KpiVisuals.Take(2).Select(visual => (first.Page.DisplayName, visual))
                    .Concat(second.KpiVisuals.Take(2).Select(visual => (second.Page.DisplayName, visual)))));
    }

    private static ConsistencyIssue? AnalyzeDominantLayoutPattern(IEnumerable<PageData> pages)
    {
        var profiles = pages
            .Select(page =>
            {
                var layout = BuildLayoutConventionProfile(page);
                var kpi = BuildKpiBandProfile(page);
                return new
                {
                    Page = page,
                    Layout = layout,
                    Kpi = kpi,
                    Signature = $"{layout.TitleAlignment ?? "none"}|{layout.FilterConvention ?? "none"}|{kpi.Convention ?? "none"}",
                };
            })
            .ToList();
        if (profiles.Count < 3)
        {
            return null;
        }

        var dominant = profiles
            .GroupBy(profile => profile.Signature, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .FirstOrDefault();
        if (dominant is null || dominant.Count() < 2 || dominant.Count() == profiles.Count)
        {
            return null;
        }

        var outliers = profiles
            .Where(profile => !string.Equals(profile.Signature, dominant.Key, StringComparison.Ordinal))
            .ToList();
        if (outliers.Count == 0)
        {
            return null;
        }

        var visuals = outliers
            .SelectMany(profile =>
            {
                var tuples = new List<(string PageName, VisualData Visual)>();
                if (profile.Layout.TitleVisual is not null)
                {
                    tuples.Add((profile.Page.DisplayName, profile.Layout.TitleVisual));
                }

                tuples.AddRange(profile.Layout.FilterVisuals.Take(1).Select(visual => (profile.Page.DisplayName, visual)));
                tuples.AddRange(profile.Kpi.KpiVisuals.Take(1).Select(visual => (profile.Page.DisplayName, visual)));
                return tuples;
            })
            .ToList();

        return new ConsistencyIssue(
            $"{string.Join(", ", outliers.Select(profile => $"'{profile.Page.DisplayName}'"))} break from the dominant layout pattern used on {dominant.Count()} page(s)",
            DeduplicateAffectedVisualTuples(visuals));
    }

    private static ReportConsistencyIssueContext? AnalyzeCrossPageNavigationConsistency(IEnumerable<PageData> pages)
    {
        var profiles = pages
            .Select(BuildNavigationProfile)
            .ToList();
        var pagesWithNavigation = profiles.Where(profile => profile.Controls.Count > 0).ToList();
        if (pagesWithNavigation.Count < 2)
        {
            return null;
        }

        var notes = new List<string>();
        var affectedPages = new HashSet<string>(StringComparer.Ordinal);

        var zones = pagesWithNavigation
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Zone))
            .Select(profile => profile.Zone!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (zones.Count >= 2)
        {
            var dominantZone = pagesWithNavigation
                .Where(profile => !string.IsNullOrWhiteSpace(profile.Zone))
                .GroupBy(profile => profile.Zone!, StringComparer.Ordinal)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .First();
            var outlierPages = pagesWithNavigation
                .Where(profile => !string.Equals(profile.Zone, dominantZone.Key, StringComparison.Ordinal))
                .Select(profile => profile.Page.DisplayName)
                .ToList();
            if (outlierPages.Count > 0)
            {
                notes.Add($"navigation controls shift away from the dominant {dominantZone.Key} zone on {string.Join(", ", outlierPages)}");
                foreach (var pageName in outlierPages)
                {
                    affectedPages.Add(pageName);
                }
            }
        }

        var pagesWithoutNavigation = profiles
            .Where(profile => profile.Controls.Count == 0)
            .Select(profile => profile.Page.DisplayName)
            .ToList();
        if (pagesWithoutNavigation.Count > 0)
        {
            notes.Add($"navigation is missing on {string.Join(", ", pagesWithoutNavigation)} even though peer pages expose report controls");
            foreach (var pageName in pagesWithoutNavigation)
            {
                affectedPages.Add(pageName);
            }
        }

        bool hasPartialMetadata = pagesWithNavigation.Any(profile => profile.HasPartialMetadata);
        if (hasPartialMetadata)
        {
            notes.Add("navigation evidence is partially detectable from PBIR metadata because some controls are unlabeled shapes or images");
            foreach (var pageName in pagesWithNavigation.Where(profile => profile.HasPartialMetadata).Select(profile => profile.Page.DisplayName))
            {
                affectedPages.Add(pageName);
            }
        }

        if (notes.Count == 0)
        {
            return null;
        }

        return new ReportConsistencyIssueContext(
            "navigation",
            "navigationPattern",
            $"Navigation patterns differ across the report: {string.Join("; ", notes)}. Detection is partially detectable from PBIR metadata.",
            affectedPages.OrderBy(name => name, StringComparer.Ordinal).ToList(),
            "medium",
            "medium",
            "Keep navigation buttons, back/reset controls, and report-flow affordances in one predictable zone across related pages.",
            "Report consistency: Navigation controls differ from the broader report flow.");
    }

    private static ReportConsistencyContext? BuildReportConsistencyContext(List<PageData> pages)
    {
        if (pages.Count < 2)
        {
            return null;
        }

        var issues = BuildReportConsistencyIssues(pages);
        bool consistentTitleAnchors = !issues.Any(issue => issue.IssueCategory == "titleHeader");
        bool consistentFilterBand = !issues.Any(issue => issue.IssueCategory == "filterPlacement");
        bool consistentMetricLabels = !issues.Any(issue => issue.Category == "metricGovernance");
        bool consistentSemanticColors = !issues.Any(issue => issue.Category == "semanticColors");
        var affectedPages = issues
            .SelectMany(issue => issue.AffectedPages)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(pageName => pageName, StringComparer.Ordinal)
            .ToList();
        var categories = issues
            .Select(issue => issue.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ReportConsistencyContext(
            new ReportConsistencySummary
            {
                ConsistentTitleAnchors = consistentTitleAnchors,
                ConsistentFilterBand = consistentFilterBand,
                ConsistentMetricLabels = consistentMetricLabels,
                ConsistentSemanticColors = consistentSemanticColors,
                OverallFinding = issues.Count == 0
                    ? "The report follows a stable cross-page pattern."
                    : $"{issues.Count} cross-page consistency issue(s) detected across {string.Join(", ", categories)}.",
                AffectedPages = affectedPages,
                IssueCount = issues.Count,
                Issues = issues
                    .Select(issue => new ReportConsistencyFinding
                    {
                        Category = issue.Category,
                        IssueCategory = issue.IssueCategory,
                        OverallFinding = issue.OverallFinding,
                        AffectedPages = issue.AffectedPages,
                        Severity = issue.Severity,
                        Confidence = issue.Confidence,
                        RecommendedRemediation = issue.RecommendedRemediation,
                    })
                    .ToList(),
                Findings = issues.Select(issue => issue.OverallFinding).ToList(),
            },
            issues);
    }

    private static List<string> BuildReportConsistencyNotes(
        string pageName,
        ReportConsistencyContext? context)
    {
        if (context is null)
        {
            return [];
        }

        return context.Issues
            .Where(issue => issue.AffectedPages.Contains(pageName, StringComparer.Ordinal))
            .Select(issue => issue.PageNote)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static List<ReportConsistencyIssueContext> BuildReportConsistencyIssues(List<PageData> pages)
    {
        var issues = new List<ReportConsistencyIssueContext>();

        if (AnalyzeTitleAlignmentConsistency(pages) is { } titleIssue)
        {
            issues.Add(new ReportConsistencyIssueContext(
                "layout",
                "titleHeader",
                $"Title and header zones shift across pages: {titleIssue.Message}.",
                titleIssue.Visuals.Select(entry => entry.PageName).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList(),
                "medium",
                "high",
                "Keep page titles in the same header zone and alignment so the report reads like a single product.",
                "Report consistency: Title anchor placement differs from other pages."));
        }

        if (AnalyzeFilterConventionConsistency(pages) is { } filterIssue)
        {
            issues.Add(new ReportConsistencyIssueContext(
                "layout",
                "filterPlacement",
                $"Slicer and filter placement shifts across pages: {filterIssue.Message}.",
                filterIssue.Visuals.Select(entry => entry.PageName).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList(),
                "medium",
                "high",
                "Keep slicers in one stable band or rail unless a page intentionally changes interaction mode.",
                "Report consistency: Filter-band placement differs from other pages."));
        }

        if (AnalyzeCrossPageKpiBandConsistency(pages) is { } kpiBandIssue)
        {
            issues.Add(new ReportConsistencyIssueContext(
                "layout",
                "kpiPlacement",
                $"KPI card placement shifts across comparable pages: {kpiBandIssue.Message}.",
                kpiBandIssue.Visuals.Select(entry => entry.PageName).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList(),
                "medium",
                "high",
                "Keep overview KPI cards in a stable top band so repeated pages share the same entry point.",
                "Report consistency: KPI placement differs from the report pattern."));
        }

        if (AnalyzeDominantLayoutPattern(pages) is { } layoutPatternIssue)
        {
            issues.Add(new ReportConsistencyIssueContext(
                "layout",
                "layoutPattern",
                layoutPatternIssue.Message,
                layoutPatternIssue.Visuals.Select(entry => entry.PageName).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList(),
                "medium",
                "high",
                "Keep repeated pages on the dominant layout pattern or template unless the different layout signals a deliberate mode change.",
                "Report consistency: This page breaks from the dominant layout pattern."));
        }

        var navigationIssue = AnalyzeCrossPageNavigationConsistency(pages);
        if (navigationIssue is not null)
        {
            issues.Add(navigationIssue);
        }

        var metricLabelIssue = AnalyzeMetricLabelGovernance(pages);
        if (metricLabelIssue is not null)
        {
            issues.Add(new ReportConsistencyIssueContext(
                "metricGovernance",
                "metricLabels",
                $"KPI label naming drifts across pages: {metricLabelIssue.Value.Message}.",
                metricLabelIssue.Value.Visuals.Select(entry => entry.PageName).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList(),
                "low",
                metricLabelIssue.Value.Confidence,
                string.IsNullOrWhiteSpace(metricLabelIssue.Value.SuggestedCanonicalLabel)
                    ? "Choose one canonical KPI naming convention and reuse it across pages."
                    : $"Choose one canonical KPI naming convention and reuse a label such as '{metricLabelIssue.Value.SuggestedCanonicalLabel}' across pages.",
                "Report consistency: Metric label naming differs from other pages."));
        }

        var semanticColorIssue = AnalyzeCrossPageSemanticColorConsistency(pages);
        if (semanticColorIssue is not null)
        {
            var affectedPages = semanticColorIssue.Assignments
                .Select(assignment => assignment.SourcePageName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            var isCategoryIdentity = semanticColorIssue.SemanticKey.StartsWith("region:", StringComparison.OrdinalIgnoreCase) ||
                semanticColorIssue.SemanticKey.StartsWith("segment:", StringComparison.OrdinalIgnoreCase) ||
                semanticColorIssue.SemanticKey.StartsWith("category:", StringComparison.OrdinalIgnoreCase);
            issues.Add(new ReportConsistencyIssueContext(
                "semanticColors",
                "semanticColorDrift",
                $"Semantic color consistency: {semanticColorIssue.Message}.",
                affectedPages,
                isCategoryIdentity ? "low" : "medium",
                isCategoryIdentity ? "medium" : "high",
                isCategoryIdentity
                    ? "Review whether category identity colors are meant to stay stable across pages. If so, keep the same categories on the same colors."
                    : "Keep the same semantic roles on the same colors across pages so users can scan the report without relearning the legend.",
                isCategoryIdentity
                    ? "Report consistency: Category colors may differ from the broader report palette."
                    : "Report consistency: Semantic color meaning differs from other pages."));
        }

        return issues;
    }

    private static CrossPageSemanticColorIssue? AnalyzeCrossPageSemanticColorConsistency(List<PageData> pages)
    {
        var conflicts = pages
            .SelectMany(ExtractSemanticColorAssignments)
            .GroupBy(assignment => assignment.SemanticKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                SemanticKey = group.Key,
                DisplayLabel = group.Select(assignment => assignment.DisplayLabel)
                    .FirstOrDefault(label => !string.IsNullOrWhiteSpace(label)) ?? group.Key,
                Colors = group.Select(assignment => assignment.Color)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(color => color, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Pages = group.Select(assignment => assignment.SourcePageName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList(),
                Assignments = group.ToList(),
            })
            .Where(group => group.Pages.Count > 1 && group.Colors.Count > 1)
            .OrderByDescending(group => group.Assignments.Count)
            .ThenBy(group => group.SemanticKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (conflicts.Count == 0)
        {
            return null;
        }

        var primary = conflicts[0];
        var message = string.Join("; ", conflicts.Select(conflict =>
            $"{conflict.DisplayLabel} uses multiple colors across pages ({string.Join(", ", conflict.Colors)}) on {string.Join(", ", conflict.Pages)}"));
        return new CrossPageSemanticColorIssue(
            primary.SemanticKey,
            primary.DisplayLabel,
            message,
            conflicts.SelectMany(conflict => conflict.Assignments).ToList());
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

    private static KpiBandProfile BuildKpiBandProfile(PageData page)
    {
        double canvasWidth = GetCanvasWidth(page);
        double canvasHeight = GetCanvasHeight(page);
        var kpiVisuals = page.Visuals
            .Where(visual => !visual.IsHidden && !visual.IsNavigationElement && visual.IsKpiCard)
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ToList();
        string? convention = kpiVisuals.Count == 0
            ? null
            : ClassifyKpiBandConvention(kpiVisuals, canvasWidth, canvasHeight);
        return new KpiBandProfile(page, convention, kpiVisuals);
    }

    private static NavigationProfile BuildNavigationProfile(PageData page)
    {
        double canvasWidth = GetCanvasWidth(page);
        double canvasHeight = GetCanvasHeight(page);
        var controls = page.Visuals
            .Where(visual => !visual.IsHidden && IsPotentialNavigationControl(visual))
            .OrderBy(visual => visual.Y)
            .ThenBy(visual => visual.X)
            .ToList();
        string? zone = controls.Count == 0
            ? null
            : ClassifyNavigationZone(controls, canvasWidth, canvasHeight);
        bool hasPartialMetadata = controls.Any(visual =>
            string.IsNullOrWhiteSpace(visual.BestVisibleText) &&
            (visual.Type.Equals("shape", StringComparison.OrdinalIgnoreCase) ||
             visual.Type.Equals("basicShape", StringComparison.OrdinalIgnoreCase) ||
             visual.Type.Equals("image", StringComparison.OrdinalIgnoreCase)));
        return new NavigationProfile(page, zone, controls, hasPartialMetadata);
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

    private static string ClassifyKpiBandConvention(IReadOnlyList<VisualData> kpiVisuals, double canvasWidth, double canvasHeight)
    {
        var topBand = kpiVisuals.Where(visual => visual.Y <= canvasHeight * 0.22).ToList();
        if (topBand.Count == 0)
        {
            return "lower band";
        }

        double minX = topBand.Min(visual => visual.X);
        if (minX <= canvasWidth * 0.2)
        {
            return "top-left band";
        }

        if (minX <= canvasWidth * 0.45)
        {
            return "top-center band";
        }

        return "top-right band";
    }

    private static string ClassifyNavigationZone(IReadOnlyList<VisualData> controls, double canvasWidth, double canvasHeight)
    {
        double avgX = controls.Average(visual => visual.X + visual.W / 2.0);
        double avgY = controls.Average(visual => visual.Y + visual.H / 2.0);
        bool top = avgY <= canvasHeight * 0.25;
        bool bottom = avgY >= canvasHeight * 0.75;
        bool left = avgX <= canvasWidth * 0.28;
        bool right = avgX >= canvasWidth * 0.72;

        if (top && right)
        {
            return "top-right";
        }

        if (top && left)
        {
            return "top-left";
        }

        if (bottom && left)
        {
            return "bottom-left";
        }

        if (bottom && right)
        {
            return "bottom-right";
        }

        if (left)
        {
            return "left rail";
        }

        if (right)
        {
            return "right rail";
        }

        return "mixed";
    }

    private static bool IsPotentialNavigationControl(VisualData visual)
    {
        if (!visual.IsNavigationElement || visual.IsSlicer)
        {
            return false;
        }

        var normalizedType = visual.Type?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedType is "actionbutton" or "navigationbutton" or "qnavisual")
        {
            return true;
        }

        var text = visual.BestVisibleText;
        return !string.IsNullOrWhiteSpace(text) && (
            ContainsTextKeyword(text, "home") ||
            ContainsTextKeyword(text, "back") ||
            ContainsTextKeyword(text, "next") ||
            ContainsTextKeyword(text, "previous") ||
            ContainsTextKeyword(text, "reset") ||
            ContainsTextKeyword(text, "clear") ||
            ContainsTextKeyword(text, "filter"));
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

    private static MetricLabelGovernanceAnalysis? AnalyzeMetricLabelGovernance(IEnumerable<PageData> pages)
    {
        var metricLabels = pages
            .SelectMany(page => page.Visuals
                .Where(visual => !visual.IsHidden && !visual.IsNavigationElement && visual.IsKpiCard && !string.IsNullOrWhiteSpace(visual.BestVisibleText))
                .Select(visual =>
                {
                    var label = visual.BestVisibleText!;
                    var canonical = BuildMetricCanonicalKey(label);
                    return new MetricLabelEntry(
                        page.DisplayName,
                        visual,
                        label,
                        ClassifyMetricLabelPattern(label),
                        canonical.CanonicalKey,
                        canonical.UsedSemanticAlias);
                }))
            .ToList();
        if (metricLabels.Count < 2)
        {
            return null;
        }

        bool hasPrefix = metricLabels.Any(entry => entry.Pattern == MetricLabelPattern.PrefixModifier);
        bool hasSuffix = metricLabels.Any(entry => entry.Pattern == MetricLabelPattern.SuffixModifier);
        bool hasGeneric = metricLabels.Any(entry => entry.Pattern == MetricLabelPattern.GenericAggregate);

        var messageParts = new List<string>();
        var affectedVisuals = new List<(string PageName, VisualData Visual)>();
        string confidence = "high";
        string? suggestedCanonicalLabel = null;

        if (hasPrefix && hasSuffix)
        {
            var prefixExample = metricLabels.First(entry => entry.Pattern == MetricLabelPattern.PrefixModifier).Label;
            var suffixExample = metricLabels.First(entry => entry.Pattern == MetricLabelPattern.SuffixModifier).Label;
            messageParts.Add($"metric labels mix prefix modifiers such as '{prefixExample}' and suffix modifiers such as '{suffixExample}'");
            affectedVisuals.AddRange(metricLabels
                .Where(entry => entry.Pattern is MetricLabelPattern.PrefixModifier or MetricLabelPattern.SuffixModifier)
                .Select(entry => (entry.PageName, entry.Visual)));
        }

        if (hasGeneric)
        {
            var genericExample = metricLabels.First(entry => entry.Pattern == MetricLabelPattern.GenericAggregate).Label;
            messageParts.Add($"generic labels such as '{genericExample}' remain in the KPI layer");
            affectedVisuals.AddRange(metricLabels
                .Where(entry => entry.Pattern == MetricLabelPattern.GenericAggregate)
                .Select(entry => (entry.PageName, entry.Visual)));
        }

        var fuzzyGroups = metricLabels
            .Where(entry => !string.IsNullOrWhiteSpace(entry.CanonicalKey))
            .GroupBy(entry => entry.CanonicalKey, StringComparer.Ordinal)
            .Select(group => new
            {
                CanonicalKey = group.Key,
                Entries = group.ToList(),
                DistinctLabels = group.Select(entry => entry.Label)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                UsedSemanticAlias = group.Any(entry => entry.UsedSemanticAlias),
            })
            .Where(group => group.DistinctLabels.Count > 1)
            .OrderByDescending(group => group.Entries.Count)
            .ThenByDescending(group => group.DistinctLabels.Max(label => label.Length))
            .ToList();

        if (fuzzyGroups.Count > 0)
        {
            var group = fuzzyGroups[0];
            var examples = group.DistinctLabels.Take(2).ToList();
            suggestedCanonicalLabel = SelectCanonicalMetricLabel(group.Entries);
            messageParts.Add(
                $"equivalent KPI labels vary between '{examples[0]}' and '{examples[1]}'" +
                (string.IsNullOrWhiteSpace(suggestedCanonicalLabel) ? string.Empty : $"; use one canonical label such as '{suggestedCanonicalLabel}'"));
            affectedVisuals.AddRange(group.Entries.Select(entry => (entry.PageName, entry.Visual)));
            confidence = group.UsedSemanticAlias ? "medium" : confidence;
        }

        if (messageParts.Count == 0)
        {
            return null;
        }

        return new MetricLabelGovernanceAnalysis(
            string.Join("; ", messageParts),
            DeduplicateAffectedVisualTuples(affectedVisuals),
            suggestedCanonicalLabel,
            confidence);
    }

    private static (string CanonicalKey, bool UsedSemanticAlias) BuildMetricCanonicalKey(string label)
    {
        var normalized = NormalizeNarrativeText(label);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return (string.Empty, false);
        }

        normalized = normalized.Replace("%", " percent ", StringComparison.Ordinal);
        normalized = Regex.Replace(normalized, @"\bcy\b", "current year", RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\bpy\b", "prior year", RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\bgross\s+margin\b", "margin", RegexOptions.CultureInvariant);
        normalized = Regex.Replace(normalized, @"\s+", " ", RegexOptions.CultureInvariant).Trim();

        bool usedSemanticAlias = false;
        var canonicalTokens = new List<string>();
        foreach (var token in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var canonical = token switch
            {
                "sales" => "salesrevenue",
                "revenue" => "salesrevenue",
                _ => token,
            };

            if (!string.Equals(canonical, token, StringComparison.Ordinal))
            {
                usedSemanticAlias = true;
            }

            canonicalTokens.Add(canonical);
        }

        var modifierOrder = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["current"] = 0,
            ["year"] = 1,
            ["prior"] = 2,
            ["ytd"] = 3,
            ["mtd"] = 4,
            ["qtd"] = 5,
            ["fy"] = 6,
            ["rolling"] = 7,
        };

        var ordered = canonicalTokens
            .OrderBy(token => modifierOrder.TryGetValue(token, out var priority) ? priority : 50)
            .ThenBy(token => token, StringComparer.Ordinal)
            .ToList();

        return (string.Join(' ', ordered), usedSemanticAlias);
    }

    private static string? SelectCanonicalMetricLabel(IEnumerable<MetricLabelEntry> entries) =>
        entries
            .Select(entry => entry.Label.Trim())
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .OrderByDescending(label => label.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
            .ThenByDescending(label => label.Length)
            .FirstOrDefault();

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
            var direct = ReadFirstString(obj, ["displayName", "friendlyName", "name", "value", "field", "label", "queryRef"]);
            if (!string.IsNullOrWhiteSpace(direct))
            {
                values.Add(direct);
            }

            var description = ReadFirstString(obj, ["description"]);
            if (!string.IsNullOrWhiteSpace(description))
            {
                values.Add(description);
            }

            if (TryGetPropertyCaseInsensitive(obj, "synonyms", out var synonymsNode))
            {
                values.AddRange(ReadStringValues(synonymsNode));
            }

            if (TryGetPropertyCaseInsensitive(obj, "aliases", out var aliasesNode))
            {
                values.AddRange(ReadStringValues(aliasesNode));
            }

            if (TryGetPropertyCaseInsensitive(obj, "alias", out var aliasNode))
            {
                values.AddRange(ReadStringValues(aliasNode));
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

    private static int InferHierarchyDepth(
        IReadOnlyList<string> fieldHints,
        string? hierarchyPattern)
    {
        if (!string.IsNullOrWhiteSpace(hierarchyPattern))
        {
            return hierarchyPattern.Split('>', StringSplitOptions.RemoveEmptyEntries).Length;
        }

        var normalized = string.Join(" ", fieldHints)
            .ToLowerInvariant();
        int depth = 0;
        foreach (var token in new[] { "year", "quarter", "month", "week", "day" })
        {
            if (normalized.Contains(token, StringComparison.Ordinal))
            {
                depth++;
            }
        }

        return depth >= 2 ? depth : 0;
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

    private readonly record struct KpiBandProfile(
        PageData Page,
        string? Convention,
        List<VisualData> KpiVisuals);

    private readonly record struct NavigationProfile(
        PageData Page,
        string? Zone,
        List<VisualData> Controls,
        bool HasPartialMetadata);

    private readonly record struct MetricLabelEntry(
        string PageName,
        VisualData Visual,
        string Label,
        MetricLabelPattern Pattern,
        string CanonicalKey,
        bool UsedSemanticAlias);

    private readonly record struct MetricLabelGovernanceAnalysis(
        string Message,
        List<(string PageName, VisualData Visual)> Visuals,
        string? SuggestedCanonicalLabel,
        string Confidence);

    private sealed record SemanticColorConflict(
        string PageName,
        string SemanticKey,
        string DisplayLabel,
        List<string> Colors,
        List<SemanticColorAssignment> Assignments);

    private sealed record CrossPageSemanticColorIssue(
        string SemanticKey,
        string DisplayLabel,
        string Message,
        List<SemanticColorAssignment> Assignments);

    private sealed record StatusSemanticIssue(
        string PageName,
        string SemanticKey,
        string Color,
        List<SemanticColorAssignment> Assignments);

    private sealed record ReportConsistencyIssueContext(
        string Category,
        string IssueCategory,
        string OverallFinding,
        List<string> AffectedPages,
        string Severity,
        string Confidence,
        string RecommendedRemediation,
        string PageNote);

    private sealed record ReportConsistencyContext(
        ReportConsistencySummary Summary,
        List<ReportConsistencyIssueContext> Issues);

    private enum MetricLabelPattern
    {
        Plain,
        PrefixModifier,
        SuffixModifier,
        GenericAggregate,
    }

    private sealed record PageData
    {
        public required string Name { get; init; }
        public required string DisplayName { get; init; }
        public List<VisualData> Visuals { get; init; } = [];
        public CanvasMetadata? Canvas { get; init; }
        public List<FilterDefinitionData> PageFilters { get; init; } = [];
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
        public FilterTopologyMetadata Filter { get; init; } = FilterTopologyMetadata.Empty;

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

    private sealed record FilterTopologyMetadata(
        IReadOnlyList<string> FieldHints,
        string? HierarchyPattern,
        int HierarchyDepth,
        string? FilterType)
    {
        public static FilterTopologyMetadata Empty { get; } = new([], null, 0, null);
    }

    private sealed record FilterDefinitionData(
        string SourceId,
        StoryFilterScope Scope,
        string DisplayLabel,
        IReadOnlyList<string> FieldHints,
        string? HierarchyPattern,
        int HierarchyDepth,
        string? FilterType,
        string? PlacementZone,
        bool IsMalformed);

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

    private sealed record SemanticNarrativeSignals(
        string PrimaryMetric,
        string? PrimaryDimension,
        int ConfidenceBonus,
        List<string> Evidence,
        bool HasRichMetadataSupport,
        bool HasSemanticTextAlignment);
}
