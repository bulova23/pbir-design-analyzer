namespace PowerBIModelingService.Services.Pbir.Models;

/// <summary>
/// Represents the computed quality score for a single page within a PBIR report.
/// Contains per-framework scores, composite score (computed property),
/// feedback items, recommendations, and optional error information.
/// Used in Feature 003: Per-Page/Per-Tab Report Scoring.
/// </summary>
public sealed class PageScore
{
    /// <summary>Gets or sets the stable PBIR page identifier.</summary>
    public string? PageId { get; init; }

    /// <summary>Gets or sets the name of the page being scored.</summary>
    public required string PageName { get; init; }

    /// <summary>Gestalt Principles score — [0, 100].</summary>
    public double GestaltScore { get; init; }

    /// <summary>Cognitive Load Theory score — [0, 100].</summary>
    public double CognitiveLoadScore { get; init; }

    /// <summary>Data-Ink Ratio / Tufte score — [0, 100].</summary>
    public double DataInkScore { get; init; }

    /// <summary>Accessibility / WCAG 2.1 score — [0, 100].</summary>
    public double AccessibilityScore { get; init; }

    /// <summary>Visual Best Practices score — [0, 100].</summary>
    public double VisualBestPracticesScore { get; init; }

    /// <summary>Stephen Few score — [0, 100].</summary>
    public double StephenFewScore { get; init; }

    /// <summary>Enterprise Governance score — [0, 100].</summary>
    public double EnterpriseGovernanceScore { get; init; }

    /// <summary>Tufte Minimalism score — [0, 100].</summary>
    public double TufteScore { get; init; }

    /// <summary>Graphical Perception score — [0, 100].</summary>
    public double GraphicalPerceptionScore { get; init; }

    /// <summary>Dashboard Density score — [0, 100].</summary>
    public double DensityScore { get; init; }

    /// <summary>Narrative Design score — [0, 100].</summary>
    public double NarrativeScore { get; init; }

    /// <summary>Count of visible data visuals on the page.</summary>
    public int DataVisualCount { get; init; }

    /// <summary>Count of visible navigation or UI control visuals on the page.</summary>
    public int NavigationVisualCount { get; init; }

    /// <summary>Count of hidden visuals on the page.</summary>
    public int HiddenVisualCount { get; init; }

    /// <summary>Structured visual metadata extracted for this page.</summary>
    public PageVisualMetadataSummary? VisualMetadata { get; init; }

    /// <summary>
    /// Gets or sets the normalized framework weights for composite score calculation.
    /// </summary>
    public Dictionary<string, double>? FrameworkWeights { get; init; }

    /// <summary>
    /// Gets the composite weighted score for this page using the configured framework weights.
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
            double fewWeight = 0.0;
            double governanceWeight = 0.0;
            double tufteWeight = 0.00;
            double graphicalWeight = 0.00;
            double densityWeight = 0.00;
            double narrativeWeight = 0.00;

            if (FrameworkWeights != null && FrameworkWeights.Count > 0)
            {
                FrameworkWeights.TryGetValue("gestalt", out var gw);
                FrameworkWeights.TryGetValue("cognitiveLoad", out var cw);
                FrameworkWeights.TryGetValue("dataInk", out var dw);
                FrameworkWeights.TryGetValue("accessibility", out var aw);
                FrameworkWeights.TryGetValue("visualBestPractices", out var vw);
                FrameworkWeights.TryGetValue("stephenFew", out var fw);
                FrameworkWeights.TryGetValue("governance", out var governanceW);
                FrameworkWeights.TryGetValue("tufte", out var tw);
                FrameworkWeights.TryGetValue("graphicalPerception", out var gpw);
                FrameworkWeights.TryGetValue("density", out var densw);
                FrameworkWeights.TryGetValue("narrative", out var nw);

                gestaltWeight = gw / 100.0;
                cogLoadWeight = cw / 100.0;
                dataInkWeight = dw / 100.0;
                a11yWeight = aw / 100.0;
                vbpWeight = vw / 100.0;
                fewWeight = fw / 100.0;
                governanceWeight = governanceW / 100.0;
                tufteWeight = tw / 100.0;
                graphicalWeight = gpw / 100.0;
                densityWeight = densw / 100.0;
                narrativeWeight = nw / 100.0;
            }

            return Math.Round(
                GestaltScore             * gestaltWeight +
                CognitiveLoadScore       * cogLoadWeight +
                DataInkScore             * dataInkWeight +
                AccessibilityScore       * a11yWeight +
                VisualBestPracticesScore * vbpWeight +
                StephenFewScore          * fewWeight +
                EnterpriseGovernanceScore * governanceWeight +
                TufteScore               * tufteWeight +
                GraphicalPerceptionScore * graphicalWeight +
                DensityScore             * densityWeight +
                NarrativeScore           * narrativeWeight,
                2);
        }
    }

    /// <summary>
    /// Gets or sets per-framework feedback items keyed by normalized framework ID.
    /// </summary>
    public Dictionary<string, List<FrameworkFeedbackItem>> Feedback { get; init; } = [];

    /// <summary>Gets or sets actionable recommendations for improving this page's score.</summary>
    public List<string> Recommendations { get; init; } = [];

    /// <summary>Gets or sets page-level notes about cross-page consistency checks.</summary>
    public List<string> ReportConsistencyNotes { get; init; } = [];

    /// <summary>Gets or sets the inferred page story summary when deterministic evidence is sufficient.</summary>
    public PageStorySummary? InferredStorySummary { get; init; }

    /// <summary>Gets or sets the deterministic page-intent profile.</summary>
    public PageIntentProfileSummary? PageIntentProfile { get; init; }

    /// <summary>Gets or sets the actionability breakdown for the page.</summary>
    public ActionabilityBreakdown? ActionabilityBreakdown { get; init; }

    /// <summary>Gets or sets the archetype and benchmark comparison for the page.</summary>
    public BenchmarkComparisonSummary? BenchmarkComparison { get; init; }

    /// <summary>
    /// Gets or sets an error message if scoring failed for this page.
    /// <c>null</c> if page scored successfully. Non-null indicates partial failure.
    /// </summary>
    public string? ScoringError { get; init; }

    /// <summary>Gets a value indicating whether this page's scoring completed successfully.</summary>
    public bool IsSuccessful => string.IsNullOrEmpty(ScoringError);

    /// <summary>
    /// Gets the per-state composite scores when bookmarks affect this page.
    /// Keys are state display names ("Default", bookmark display name, ...) and values are the
    /// composite score (0-100) for the layout state. <c>null</c> when no bookmarks affect this page.
    /// When populated, the page's top-level framework scores are the per-state averages.
    /// </summary>
    public Dictionary<string, double>? PerStateScores { get; init; }

    /// <summary>
    /// Gets or sets the report-level consistency findings that affect this page, when available.
    /// <c>null</c> when no cross-page consistency summary has been attached.
    /// </summary>
    public ReportConsistencySummary? ReportConsistency { get; init; }

    /// <summary>
    /// Gets or sets the internal-only Story Assessment signal registry captured during scoring.
    /// This remains out of the public score payload until validation promotion occurs.
    /// </summary>
    internal StorySignalRegistry? InternalStorySignalRegistry { get; init; }

    /// <summary>
    /// Gets or sets the internal-only Story Assessment archetype classification captured during scoring.
    /// This remains out of the public score payload until validation promotion occurs.
    /// </summary>
    internal StoryAssessmentArchetypeClassification? InternalStoryAssessmentArchetypeClassification { get; init; }

    /// <summary>
    /// Gets or sets the internal-only Story Assessment semantic coherence assessment captured during scoring.
    /// This remains out of the public score payload until validation promotion occurs.
    /// </summary>
    internal StorySemanticCoherenceAssessment? InternalStorySemanticCoherenceAssessment { get; init; }
}
