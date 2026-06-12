namespace PowerBIModelingService.Services.Pbir.Models;

/// <summary>
/// Canonical feedback classification values used by the scorer and score panel.
/// </summary>
public static class FindingTypes
{
    public const string Objective = "objective";
    public const string StrongHeuristic = "strongHeuristic";
    public const string StylePreference = "stylePreference";
}

/// <summary>
/// A single pass/fail evaluation result within a scoring framework.
/// </summary>
/// <param name="Ok"><see langword="true"/> if the criterion passed; <see langword="false"/> if it needs attention.</param>
/// <param name="Text">Plain-language explanation of the result and (for failures) how to fix it.</param>
/// <param name="AffectedVisuals">Optional visual references that help users locate the contributing PBIR visuals in the explorer.</param>
/// <param name="EarnedPoints">Optional earned points for this criterion so the UI can summarize score breakdowns.</param>
/// <param name="PossiblePoints">Optional maximum points for this criterion so the UI can summarize score breakdowns.</param>
/// <param name="FindingType">One of <c>objective</c>, <c>strongHeuristic</c>, or <c>stylePreference</c>.</param>
public sealed record FrameworkFeedbackItem(
    bool Ok,
    string Text,
    List<AffectedVisualReference>? AffectedVisuals = null,
    double? EarnedPoints = null,
    double? PossiblePoints = null,
    string FindingType = FindingTypes.StrongHeuristic);

/// <summary>
/// Identifies a visual mentioned in score feedback so the UI can link findings back to the PBIR sidecar tree.
/// </summary>
/// <param name="PageName">Display name of the page that contains the visual.</param>
/// <param name="VisualId">Stable PBIR visual identifier.</param>
/// <param name="VisualType">Power BI visual type for user-facing display.</param>
public sealed record AffectedVisualReference(string PageName, string VisualId, string VisualType);

/// <summary>
/// Structured cross-page consistency finding shown in the score panel.
/// </summary>
public sealed class ReportConsistencyFinding
{
    /// <summary>Gets or sets the high-level grouping category such as layout, navigation, or semanticColors.</summary>
    public required string Category { get; init; }

    /// <summary>Gets or sets the concrete issue category within the grouping.</summary>
    public required string IssueCategory { get; init; }

    /// <summary>Gets or sets the human-readable description of the report-level inconsistency.</summary>
    public required string OverallFinding { get; init; }

    /// <summary>Gets or sets the affected report pages.</summary>
    public List<string> AffectedPages { get; init; } = [];

    /// <summary>Gets or sets the relative severity for the finding.</summary>
    public required string Severity { get; init; }

    /// <summary>Gets or sets the scorer confidence for the finding.</summary>
    public required string Confidence { get; init; }

    /// <summary>Gets or sets the recommended remediation text.</summary>
    public required string RecommendedRemediation { get; init; }
}

/// <summary>
/// Deterministic story hypothesis inferred for a page from its visible structure and chart mix.
/// </summary>
public sealed class PageStorySummary
{
    /// <summary>Gets or sets the inferred page-intent profile.</summary>
    public required string IntentProfile { get; init; }

    /// <summary>Gets or sets the inferred story archetype.</summary>
    public required string StoryArchetype { get; init; }

    /// <summary>Gets or sets the natural-language story hypothesis shown to users.</summary>
    public required string InferredStory { get; init; }

    /// <summary>Gets or sets the confidence for the hypothesis.</summary>
    public required string Confidence { get; init; }

    /// <summary>Gets or sets the evidence used to infer the story.</summary>
    public List<string> Evidence { get; init; } = [];
}

/// <summary>
/// Deterministic page-intent profile used to adjust review expectations.
/// </summary>
public sealed class PageIntentProfileSummary
{
    /// <summary>Gets or sets the inferred profile for the page.</summary>
    public required string InferredProfile { get; init; }

    /// <summary>Gets or sets how demanding actionability expectations should be for this profile.</summary>
    public required string ActionabilityExpectation { get; init; }

    /// <summary>Gets or sets concrete reviewer guidance tied to this profile.</summary>
    public List<string> ReviewGuidance { get; init; } = [];

    /// <summary>Gets or sets the evidence used to infer the profile.</summary>
    public List<string> Evidence { get; init; } = [];
}

/// <summary>
/// Explains whether a page supports action and decision-making.
/// </summary>
public sealed class ActionabilityBreakdown
{
    /// <summary>Gets or sets the 0-100 actionability score.</summary>
    public double Score { get; init; }

    /// <summary>Gets or sets whether the page exposes a target, budget, benchmark, or similar anchor.</summary>
    public bool TargetBenchmarkPresent { get; init; }

    /// <summary>Gets or sets whether the page surfaces exceptions or outliers clearly.</summary>
    public bool ExceptionVisibility { get; init; }

    /// <summary>Gets or sets whether urgency or recency cues are visible.</summary>
    public bool UrgencySignaling { get; init; }

    /// <summary>Gets or sets whether prior-period or delta context is visible.</summary>
    public bool PriorPeriodContext { get; init; }

    /// <summary>Gets or sets whether the page offers a drill or supporting-evidence path.</summary>
    public bool DrillPathPresent { get; init; }

    /// <summary>Gets or sets the expectation level implied by the page profile.</summary>
    public required string ExpectationLevel { get; init; }

    /// <summary>Gets or sets actionability strengths that the page already demonstrates.</summary>
    public List<string> Strengths { get; init; } = [];

    /// <summary>Gets or sets the highest-priority decision-support gaps.</summary>
    public List<string> Gaps { get; init; } = [];

    /// <summary>Gets or sets the concise narrative summary shown in the UI.</summary>
    public required string Summary { get; init; }
}

/// <summary>
/// Compares the page against a recognizable dashboard archetype and benchmark expectation.
/// </summary>
public sealed class BenchmarkComparisonSummary
{
    /// <summary>Gets or sets the nearest matching dashboard archetype.</summary>
    public required string Archetype { get; init; }

    /// <summary>Gets or sets the benchmark label used for comparison messaging.</summary>
    public required string BenchmarkLabel { get; init; }

    /// <summary>Gets or sets the broad comparison outcome relative to the benchmark.</summary>
    public required string ComparativePosition { get; init; }

    /// <summary>Gets or sets whether the page looks polished but still fails to support decisions.</summary>
    public bool BeautifulButUseless { get; init; }

    /// <summary>Gets or sets the primary comparative insight for the reviewer.</summary>
    public required string Insight { get; init; }

    /// <summary>Gets or sets the notable comparative strengths.</summary>
    public List<string> Strengths { get; init; } = [];

    /// <summary>Gets or sets the notable comparative gaps.</summary>
    public List<string> Gaps { get; init; } = [];
}

/// <summary>
/// Safe public recommendation promoted from validated Story Assessment gaps.
/// </summary>
public sealed class GuidedStoryImprovement
{
    /// <summary>Gets or sets the stable user-facing recommendation identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Gets or sets the short user-facing recommendation title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets or sets the concise explanation of what is weaker today.</summary>
    public required string Summary { get; init; }

    /// <summary>Gets or sets the consultant-friendly explanation of why the change matters.</summary>
    public required string Rationale { get; init; }

    /// <summary>Gets or sets the expected user-facing impact after the improvement is made.</summary>
    public required string ExpectedImpact { get; init; }

    /// <summary>Gets or sets the public priority label for the recommendation.</summary>
    public required string Priority { get; init; }

    /// <summary>Gets or sets the related issue impact area used by downstream presentation layers.</summary>
    public required string RelatedImpactArea { get; init; }
}

/// <summary>
/// Safe public container for Guided Story Improvements.
/// </summary>
public sealed class GuidedStoryImprovements
{
    /// <summary>Gets or sets the highest-priority validated story improvements.</summary>
    public List<GuidedStoryImprovement> HighPriorityImprovements { get; init; } = [];

    /// <summary>Gets or sets the medium-priority validated story improvements.</summary>
    public List<GuidedStoryImprovement> MediumPriorityImprovements { get; init; } = [];

    /// <summary>Gets or sets the compact rationale shown below the recommendation lists.</summary>
    public string StoryImprovementRationale { get; init; } = string.Empty;
}

/// <summary>
/// Summarizes cross-page consistency findings for a scored report.
/// </summary>
public sealed class ReportConsistencySummary
{
    /// <summary>Gets or sets a value indicating whether title anchors are consistent across pages.</summary>
    public bool ConsistentTitleAnchors { get; init; }

    /// <summary>Gets or sets a value indicating whether filter bands are consistently positioned across pages.</summary>
    public bool ConsistentFilterBand { get; init; }

    /// <summary>Gets or sets a value indicating whether metric labels remain consistent across pages.</summary>
    public bool ConsistentMetricLabels { get; init; }

    /// <summary>Gets or sets a value indicating whether semantic color mappings remain consistent across pages.</summary>
    public bool ConsistentSemanticColors { get; init; }

    /// <summary>Gets or sets an overall report-level summary of the cross-page findings.</summary>
    public string? OverallFinding { get; init; }

    /// <summary>Gets or sets the unique pages affected by one or more cross-page findings.</summary>
    public List<string> AffectedPages { get; init; } = [];

    /// <summary>Gets or sets the count of structured cross-page findings.</summary>
    public int IssueCount { get; init; }

    /// <summary>Gets or sets the structured cross-page findings for UI grouping and remediation.</summary>
    public List<ReportConsistencyFinding> Issues { get; init; } = [];

    /// <summary>Gets or sets the human-readable consistency findings for the report.</summary>
    public List<string> Findings { get; init; } = [];
}

/// <summary>
/// Holds the scoring dimensions and final composite score produced by <see cref="PbirScoringService"/>.
/// All sub-scores are clamped to [0, 100]. The composite score is a weighted sum of the
/// enabled frameworks using the normalized weights supplied in <see cref="FrameworkWeights"/>.
/// </summary>
public sealed class ScoreResult
{
    // ── Framework sub-scores (each [0, 100]) ────────────────────────────────

    /// <summary>Gestalt Principles score.</summary>
    public double GestaltScore { get; set; }

    /// <summary>Cognitive Load Theory score.</summary>
    public double CognitiveLoadScore { get; set; }

    /// <summary>Data-Ink Ratio / Tufte score.</summary>
    public double DataInkScore { get; set; }

    /// <summary>Accessibility / WCAG 2.1 score.</summary>
    public double AccessibilityScore { get; set; }

    /// <summary>Visual Best Practices score.</summary>
    public double VisualBestPracticesScore { get; set; }

    /// <summary>Stephen Few score (weight 0% by default).</summary>
    public double StephenFewScore { get; set; }

    /// <summary>Enterprise Governance score (weight 0% by default). Decision 6: Governance is a scoring framework.</summary>
    public double EnterpriseGovernanceScore { get; set; }

    /// <summary>Graphical Perception score (weight 0% by default).</summary>
    public double GraphicalPerceptionScore { get; set; }

    /// <summary>Tufte Minimalism score (weight 0% by default).</summary>
    public double TufteScore { get; set; }

    /// <summary>Dashboard Density score (weight 0% by default).</summary>
    public double DensityScore { get; set; }

    /// <summary>Narrative Design score (weight 0% by default).</summary>
    public double NarrativeScore { get; set; }

    // ── Legacy sub-scores (retained for backward compatibility) ─────────────

    /// <summary>Layout / Gestalt principles score.</summary>
    [Obsolete("Use GestaltScore. LayoutScore is retained for backward compatibility.")]
    public double LayoutScore { get; set; }

    /// <summary>Theme / Visual Best Practices score.</summary>
    [Obsolete("Use VisualBestPracticesScore. ThemeScore is retained for backward compatibility.")]
    public double ThemeScore { get; set; }

    /// <summary>Governance / Enterprise Standard score (legacy).</summary>
    [Obsolete("Use EnterpriseGovernanceScore. GovernanceScore is retained for backward compatibility.")]
    public double GovernanceScore { get; set; } = 100;

    // ── Composite ───────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the normalized framework weights for composite score calculation.
    /// Keys mirror <c>PbirScoringService.NormalizeFrameworkId</c>, e.g.
    /// gestalt, cognitiveLoad, dataInk, accessibility, visualBestPractices,
    /// governance, graphicalPerception, stephenFew, tufte, density, narrative.
    /// </summary>
    public Dictionary<string, double>? FrameworkWeights { get; set; }

    /// <summary>
    /// Gets the composite weighted score using the configured framework weights.
    /// Disabled frameworks (weight = 0) do not contribute to the composite.
    /// </summary>
    public double CompositeScore
    {
        get
        {
            double gestaltWeight = 0.0;
            double cogLoadWeight = 0.0;
            double dataInkWeight = 0.0;
            double a11yWeight = 0.0;
            double vbpWeight = 0.0;
            double governanceWeight = 0.00;
            double graphicalWeight = 0.00;
            double fewWeight = 0.00;
            double tufteWeight = 0.00;
            double densityWeight = 0.00;
            double narrativeWeight = 0.00;

            if (FrameworkWeights != null && FrameworkWeights.Count > 0)
            {
                FrameworkWeights.TryGetValue("gestalt", out var gw);
                FrameworkWeights.TryGetValue("cognitiveLoad", out var cw);
                FrameworkWeights.TryGetValue("dataInk", out var dw);
                FrameworkWeights.TryGetValue("accessibility", out var aw);
                FrameworkWeights.TryGetValue("visualBestPractices", out var vw);
                FrameworkWeights.TryGetValue("governance", out var governanceW);
                FrameworkWeights.TryGetValue("graphicalPerception", out var graphW);
                FrameworkWeights.TryGetValue("stephenFew", out var fw);
                FrameworkWeights.TryGetValue("tufte", out var tw);
                FrameworkWeights.TryGetValue("density", out var densityW);
                FrameworkWeights.TryGetValue("narrative", out var nw);

                gestaltWeight = gw / 100.0;
                cogLoadWeight = cw / 100.0;
                dataInkWeight = dw / 100.0;
                a11yWeight = aw / 100.0;
                vbpWeight = vw / 100.0;
                governanceWeight = governanceW / 100.0;
                graphicalWeight = graphW / 100.0;
                fewWeight = fw / 100.0;
                tufteWeight = tw / 100.0;
                densityWeight = densityW / 100.0;
                narrativeWeight = nw / 100.0;
            }

            return Math.Round(
                GestaltScore             * gestaltWeight +
                CognitiveLoadScore       * cogLoadWeight +
                DataInkScore             * dataInkWeight +
                AccessibilityScore       * a11yWeight +
                VisualBestPracticesScore * vbpWeight +
                EnterpriseGovernanceScore * governanceWeight +
                GraphicalPerceptionScore  * graphicalWeight +
                StephenFewScore           * fewWeight +
                TufteScore                * tufteWeight +
                DensityScore              * densityWeight +
                NarrativeScore            * narrativeWeight,
                2);
        }
    }

    // ── Per-framework feedback ───────────────────────────────────────────────

    /// <summary>
    /// Per-framework feedback items keyed by normalized framework ID.
    /// </summary>
    public Dictionary<string, List<FrameworkFeedbackItem>> Feedback { get; set; } = [];

    // ── Metadata ─────────────────────────────────────────────────────────────

    /// <summary>Gets or sets the count of pages that contain at least one visual.</summary>
    public int PageCount { get; set; }

    /// <summary>Gets or sets the list of actionable improvement recommendations (prefixed [High]/[Medium]/[Low]).</summary>
    public List<string> Recommendations { get; set; } = [];

    /// <summary>Gets or sets the report path this score was computed for.</summary>
    public string? ReportPath { get; set; }

    /// <summary>Gets or sets the timestamp when the score was computed (UTC).</summary>
    public DateTimeOffset ScoredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the count of visible data visuals across the scored scope.</summary>
    public int DataVisualCount { get; set; }

    /// <summary>Gets or sets the count of visible navigation or UI control visuals across the scored scope.</summary>
    public int NavigationVisualCount { get; set; }

    /// <summary>Gets or sets the count of hidden visuals across the scored scope.</summary>
    public int HiddenVisualCount { get; set; }

    /// <summary>
    /// Gets or sets the structured visual metadata summary for the scored page when single-page scoring is used.
    /// <c>null</c> for full-report scoring where page-level metadata is carried on <see cref="PageScores"/>.
    /// </summary>
    public PageVisualMetadataSummary? VisualMetadata { get; set; }

    /// <summary>Gets or sets the inferred page story summary for single-page scoring.</summary>
    public PageStorySummary? InferredStorySummary { get; set; }

    /// <summary>Gets or sets the deterministic page-intent profile for single-page scoring.</summary>
    public PageIntentProfileSummary? PageIntentProfile { get; set; }

    /// <summary>Gets or sets the actionability breakdown for single-page scoring.</summary>
    public ActionabilityBreakdown? ActionabilityBreakdown { get; set; }

    /// <summary>Gets or sets the archetype and benchmark comparison for single-page scoring.</summary>
    public BenchmarkComparisonSummary? BenchmarkComparison { get; set; }

    /// <summary>Gets or sets the safe public Guided Story Improvements promoted from validated Story Assessment gaps.</summary>
    public GuidedStoryImprovements GuidedStoryImprovements { get; set; } = new();

    /// <summary>Gets or sets the report-level cross-page consistency summary when full-report scoring is used.</summary>
    public ReportConsistencySummary? ReportConsistencySummary { get; set; }

    /// <summary>
    /// Gets or sets the internal-only Story Assessment signal registry captured during scoring.
    /// This remains out of the public score-panel contract until validation promotion occurs.
    /// </summary>
    internal StorySignalRegistry? InternalStorySignalRegistry { get; set; }

    /// <summary>
    /// Gets or sets the internal-only Story Assessment archetype classification captured during scoring.
    /// This remains out of the public score-panel contract until validation promotion occurs.
    /// </summary>
    internal StoryAssessmentArchetypeClassification? InternalStoryAssessmentArchetypeClassification { get; set; }

    /// <summary>
    /// Gets or sets the internal-only Story Assessment special-page assessment captured during scoring.
    /// This remains out of the public score-panel contract until validation promotion occurs.
    /// </summary>
    internal StorySpecialPageAssessment? InternalStorySpecialPageAssessment { get; set; }

    /// <summary>
    /// Gets or sets the internal-only Story Assessment semantic coherence assessment captured during scoring.
    /// This remains out of the public score-panel contract until validation promotion occurs.
    /// </summary>
    internal StorySemanticCoherenceAssessment? InternalStorySemanticCoherenceAssessment { get; set; }

    /// <summary>
    /// Gets or sets the internal-only Story Assessment filter topology assessment captured during scoring.
    /// This remains out of the public score-panel contract until validation promotion occurs.
    /// </summary>
    internal StoryFilterTopologyAssessment? InternalStoryFilterTopologyAssessment { get; set; }

    /// <summary>
    /// Gets or sets the internal-only Story Assessment story gap assessment captured during scoring.
    /// This remains out of the public score-panel contract until validation promotion occurs.
    /// </summary>
    internal StoryGapAssessment? InternalStoryGapAssessment { get; set; }

    /// <summary>
    /// Gets or sets the internal-only Story Assessment confidence breakdown captured during scoring.
    /// This remains out of the public score-panel contract until validation promotion occurs.
    /// </summary>
    internal StoryConfidenceBreakdownAssessment? InternalStoryConfidenceBreakdownAssessment { get; set; }

    // ── Per-Page Scores (Feature 003: Per-Page Scoring) ───────────────────────

    /// <summary>
    /// Gets or sets the list of per-page scores when a full report is scored.
    /// Populated only when <c>ScoreAsync(reportPath, pageName: null)</c> is called.
    /// <c>null</c> when a single page is scored directly (use primary scores instead).
    /// This list enables per-page breakdown display (tabbed UI) and page-level iteration.
    /// </summary>
    public List<PageScore>? PageScores { get; set; }

    /// <summary>
    /// Gets or sets the stable PBIR page identifier of the specific page scored (if single-page mode).
    /// <c>null</c> when scoring the entire report.
    /// </summary>
    public string? ScoredPageId { get; set; }

    /// <summary>
    /// Gets or sets the name of the specific page scored (if single-page mode).
    /// <c>null</c> when scoring the entire report.
    /// Set in single-page mode to identify which page produced the top-level scores.
    /// </summary>
    public string? ScoredPageName { get; set; }

    // ── Per-State Scores (Feature 004: Bookmark-Aware Scoring) ────────────────

    /// <summary>
    /// Gets or sets the per-state scores when bookmarks are detected on a page.
    /// Keys: state name (e.g., "Default", "Sales Filter", "Region Filter")
    /// Values: composite score for that layout state (0-100)
    /// Populated only when bookmarks are detected during page/report scoring.
    /// If null or empty, no bookmarks were found.
    /// </summary>
    public Dictionary<string, double>? PerStateScores { get; set; }

    /// <summary>
    /// Gets or sets the name of the current bookmark state being scored (for per-state breakdown).
    /// <c>null</c> for full report scores or when no bookmarks exist.
    /// Used internally during multi-state scoring.
    /// </summary>
    public string? CurrentLayoutState { get; set; }

    // ── Error Handling (Feature 003: Per-Page Scoring) ───────────────────────

    /// <summary>
    /// Gets or sets a dictionary of errors encountered during per-page scoring.
    /// Keys are page names (or indices), values are error messages.
    /// Used to support partial failure mode where some pages fail but others succeed.
    /// Empty if all pages scored successfully.
    /// </summary>
    public Dictionary<string, string> ScoringErrors { get; set; } = [];
}
