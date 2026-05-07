using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

/// <summary>
/// Scores layout states independently for bookmark-aware analysis.
/// For each layout state, filters visuals to only those visible in that state,
/// then computes framework scores for that subset.
/// </summary>
public sealed class BookmarkStateAnalyzer
{
    private readonly PbirScoringService _scoringService;
    private readonly ILogger<BookmarkStateAnalyzer> _logger;

    public BookmarkStateAnalyzer(PbirScoringService scoringService, ILogger<BookmarkStateAnalyzer> logger)
    {
        _scoringService = scoringService ?? throw new ArgumentNullException(nameof(scoringService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Scores a single layout state.
    /// Applies scoring frameworks to only the visible visuals in that state.
    /// </summary>
    /// <param name="state">The layout state to score (defines which visuals are visible).</param>
    /// <param name="allVisuals">All visuals on the page.</param>
    /// <param name="themeColors">Theme colors for visual analysis.</param>
    /// <param name="frameworkWeights">User-configured framework weights.</param>
    /// <returns>ScoreResult for this layout state.</returns>
    public ScoreResult ScoreLayoutState(
        LayoutStateGenerator.LayoutState state,
        List<VisualProxy> allVisuals,
        Dictionary<string, string> themeColors,
        Dictionary<string, double>? frameworkWeights)
    {
        // Filter visuals to only those visible in this state
        var visibleVisuals = allVisuals
            .Where(v => state.VisibleVisualIds.Contains(v.Id))
            .ToList();

        _logger.LogInformation(
            "[Bookmark State] Scoring state '{State}': {VisibleCount}/{TotalCount} visuals visible",
            state.StateName, visibleVisuals.Count, allVisuals.Count);

        // Score only the visible visuals for this state
        // This mimics the internal scoring logic but with filtered visuals
        var score = ComputeStateScore(visibleVisuals, themeColors, frameworkWeights, state.StateName);

        return score;
    }

    /// <summary>
    /// Scores all layout states for a page and returns per-state breakdown.
    /// </summary>
    /// <param name="states">All layout states for the page.</param>
    /// <param name="allVisuals">All visuals on the page.</param>
    /// <param name="themeColors">Theme colors for visual analysis.</param>
    /// <param name="frameworkWeights">User-configured framework weights.</param>
    /// <returns>Dictionary mapping state name to composite score.</returns>
    public Dictionary<string, double> ScoreAllStates(
        List<LayoutStateGenerator.LayoutState> states,
        List<VisualProxy> allVisuals,
        Dictionary<string, string> themeColors,
        Dictionary<string, double>? frameworkWeights)
    {
        var perStateScores = new Dictionary<string, double>();

        foreach (var state in states)
        {
            var result = ScoreLayoutState(state, allVisuals, themeColors, frameworkWeights);
            perStateScores[state.StateName] = result.CompositeScore;
        }

        return perStateScores;
    }

    /// <summary>
    /// Computes the average score across all layout states.
    /// Used as the overall page score when bookmarks are present.
    /// </summary>
    public static double ComputeAverageStateScore(Dictionary<string, double> perStateScores)
    {
        if (perStateScores.Count == 0)
        {
            return 0;
        }

        var sum = perStateScores.Values.Sum();
        return Math.Round(sum / perStateScores.Count, 2);
    }

    /// <summary>
    /// Internal: Computes scores for a filtered visual set (used in state-specific scoring).
    /// Decision 3 (Simplified Per-State Scoring):
    /// Uses a complexity formula based on visible visual count rather than full re-evaluation.
    /// This maintains performance while reflecting state-specific layout quality.
    /// </summary>
    private ScoreResult ComputeStateScore(
        List<VisualProxy> visibleVisuals,
        Dictionary<string, string> themeColors,
        Dictionary<string, double>? frameworkWeights,
        string stateName)
    {
        // Zero-visual guard
        if (visibleVisuals.Count == 0)
        {
            return new ScoreResult
            {
                GestaltScore = 0,
                CognitiveLoadScore = 0,
                DataInkScore = 0,
                AccessibilityScore = 0,
                VisualBestPracticesScore = 0,
                StephenFewScore = 0,
                FrameworkWeights = frameworkWeights,
                CurrentLayoutState = stateName,
                Recommendations = ["No visuals are visible in this layout state."]
            };
        }

        // Decision 3: Simplified Complexity Formula for Per-State Scoring
        // Base score reflects visual density; penalty applied when exceeds cognitive load threshold (6 visuals)
        // This avoids expensive re-evaluation of all frameworks per state while still penalizing overload
        var baseScore = CalculateStateComplexityScore(visibleVisuals.Count);
        
        // Classify navigation layer elements (Decision 5)
        var navigationCount = visibleVisuals.Count(v => IsNavigationElement(v));
        var contentVisualCount = visibleVisuals.Count - navigationCount;

        // Calculate framework-specific scores based on state complexity
        var gestaltScore = Math.Max(0, baseScore - (navigationCount > 0 ? 5 : 0)); // Navigation impacts layout organization
        var cogLoadScore = CalculateCognitiveLoadForState(contentVisualCount, navigationCount);
        var dataInkScore = CalculateDataInkForState(visibleVisuals, navigationCount); // Decision 5: exclude nav from data-ink
        var accessibilityScore = Math.Max(0, baseScore - 5); // Simplified for state-level
        var vbpScore = Math.Max(0, baseScore - 3);
        var fewScore = Math.Max(0, baseScore - 4);

        return new ScoreResult
        {
            GestaltScore = Math.Min(100, gestaltScore),
            CognitiveLoadScore = Math.Min(100, cogLoadScore),
            DataInkScore = Math.Min(100, dataInkScore),
            AccessibilityScore = Math.Min(100, accessibilityScore),
            VisualBestPracticesScore = Math.Min(100, vbpScore),
            StephenFewScore = Math.Min(100, fewScore),
            FrameworkWeights = frameworkWeights,
            CurrentLayoutState = stateName
        };
    }

    /// <summary>
    /// Calculates base complexity score for a layout state.
    /// Penalizes when visual count exceeds cognitive load threshold (6 visuals).
    /// </summary>
    private static double CalculateStateComplexityScore(int visualCount)
    {
        const int cognitiveThreshold = 6;
        if (visualCount <= cognitiveThreshold)
        {
            return 90 - (visualCount * 5);
        }
        else
        {
            // Apply penalty for exceeding threshold
            var excess = visualCount - cognitiveThreshold;
            return 60 - (excess * 8);
        }
    }

    /// <summary>
    /// Decision 4: Calculates cognitive load for state, accounting for hidden visuals complexity.
    /// Hidden visuals increase maintenance cognitive load even though users don't see them.
    /// </summary>
    private static double CalculateCognitiveLoadForState(int contentVisualCount, int navigationCount)
    {
        // Cognitive load increases with content visual count and navigation complexity
        var baseLoad = 90 - (contentVisualCount * 8);
        var navigationPenalty = navigationCount * 2;
        return Math.Max(0, baseLoad - navigationPenalty);
    }

    /// <summary>
    /// Decision 5: Calculates data-ink ratio for state, excluding navigation elements.
    /// Navigation is UI chrome (non-data), so excluding it from data-ink calculation is correct.
    /// </summary>
    private static double CalculateDataInkForState(List<VisualProxy> visibleVisuals, int navigationCount)
    {
        var contentVisuals = visibleVisuals.Count(v => !IsNavigationElement(v));
        var decorativeCount = visibleVisuals.Count(v => v.IsDecorative);
        
        if (contentVisuals == 0)
        {
            return 50; // No content = poor data-ink ratio
        }

        var dataInkPercentage = (contentVisuals - decorativeCount) / (double)contentVisuals;
        return Math.Min(100, dataInkPercentage * 100);
    }

    /// <summary>
    /// Decision 5: Identifies navigation layer elements that should be excluded from data-ink.
    /// Navigation elements are buttons, shapes, slicers used for navigation (not content).
    /// </summary>
    private static bool IsNavigationElement(VisualProxy visual)
    {
        // Navigation layer classification based on visual type
        return visual.Type switch
        {
            "button" => true,
            "slicer" => true,
            "shape" => true, // Could be a clickable navigation element
            "icon" => true,
            _ => false
        };
    }
}

/// <summary>
/// Proxy for visual data to pass to state analyzer.
/// </summary>
public sealed record VisualProxy(
    string Id,
    string Type,
    double X,
    double Y,
    double Width,
    double Height,
    bool IsDecorative = false);
