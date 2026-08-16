using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

internal sealed class RecommendationAssemblyService
{
    public List<string> CreateBuffer()
    {
        return [];
    }

    public void AddBookmarkAwareScoringRecommendation(List<string> recommendations, int layoutStateCount)
    {
        recommendations.Add(
            $"[Info] Bookmark-aware scoring active: page scored across {layoutStateCount} layout states (Default + {layoutStateCount - 1} bookmark state{(layoutStateCount == 2 ? string.Empty : "s")}).");
    }
}

internal sealed class ScoreCompatibilityAdapter
{
    public void PopulateLegacyScores(ScoreResult result)
    {
#pragma warning disable CS0618
        result.LayoutScore = result.GestaltScore;
        result.ThemeScore = result.VisualBestPracticesScore;
        result.GovernanceScore = result.EnterpriseGovernanceScore;
#pragma warning restore CS0618
    }
}

internal sealed class ScoreResultAssemblyService
{
    private readonly ScoreCompatibilityAdapter _scoreCompatibilityAdapter;

    public ScoreResultAssemblyService(ScoreCompatibilityAdapter scoreCompatibilityAdapter)
    {
        _scoreCompatibilityAdapter = scoreCompatibilityAdapter;
    }

    public ScoreResult CreateSinglePageResult(ScoreResultAssemblyInput input)
    {
        var result = CreateBaseResult(input);
        result.ScoredPageId = input.ScoredPageId;
        result.ScoredPageName = input.ScoredPageName;
        result.PerStateScores = input.PerStateScores;
        return result;
    }

    public ScoreResult CreateReportResult(ScoreResultAssemblyInput input)
    {
        var result = CreateBaseResult(input);
        result.PageScores = input.PageScores;
        result.ScoringErrors = input.ScoringErrors ?? [];
        result.ReportConsistencySummary = input.ReportConsistencySummary;
        return result;
    }

    public PageScore CreatePageScore(PageScoreAssemblyInput input)
    {
        return new PageScore
        {
            PageId = input.PageId,
            PageName = input.PageName,
            GestaltScore = input.Frameworks.GestaltScore,
            CognitiveLoadScore = input.Frameworks.CognitiveLoadScore,
            DataInkScore = input.Frameworks.DataInkScore,
            AccessibilityScore = input.Frameworks.AccessibilityScore,
            VisualBestPracticesScore = input.Frameworks.VisualBestPracticesScore,
            StephenFewScore = input.Frameworks.StephenFewScore,
            EnterpriseGovernanceScore = input.Frameworks.EnterpriseGovernanceScore,
            TufteScore = input.Frameworks.TufteScore,
            GraphicalPerceptionScore = input.Frameworks.GraphicalPerceptionScore,
            DensityScore = input.Frameworks.DensityScore,
            NarrativeScore = input.Frameworks.NarrativeScore,
            Feedback = input.Frameworks.Feedback,
            Recommendations = input.Recommendations,
            FrameworkWeights = input.FrameworkWeights,
            DataVisualCount = input.DataVisualCount,
            NavigationVisualCount = input.NavigationVisualCount,
            HiddenVisualCount = input.HiddenVisualCount,
            VisualMetadata = input.VisualMetadata,
            ReportConsistencyNotes = input.ReportConsistencyNotes,
            InferredStorySummary = input.InferredStorySummary,
            PageIntentProfile = input.PageIntentProfile,
            ActionabilityBreakdown = input.ActionabilityBreakdown,
            BenchmarkComparison = input.BenchmarkComparison,
            GuidedStoryImprovements = input.StoryAssessment?.GuidedStoryImprovements ?? new GuidedStoryImprovements(),
            InternalStorySignalRegistry = input.StoryAssessment?.SignalRegistry,
            InternalStoryAssessmentArchetypeClassification = input.StoryAssessment?.ArchetypeClassification,
            InternalStorySpecialPageAssessment = input.StoryAssessment?.SpecialPageAssessment,
            InternalStorySemanticCoherenceAssessment = input.StoryAssessment?.SemanticCoherenceAssessment,
            InternalStoryFilterTopologyAssessment = input.StoryAssessment?.FilterTopologyAssessment,
            InternalStoryGapAssessment = input.StoryAssessment?.GapAssessment,
            InternalStoryConfidenceBreakdownAssessment = input.StoryAssessment?.ConfidenceBreakdownAssessment,
            PerStateScores = input.PerStateScores,
            ReportConsistency = input.ReportConsistency,
        };
    }

    private ScoreResult CreateBaseResult(ScoreResultAssemblyInput input)
    {
        var result = new ScoreResult
        {
            GestaltScore = input.Frameworks.GestaltScore,
            CognitiveLoadScore = input.Frameworks.CognitiveLoadScore,
            DataInkScore = input.Frameworks.DataInkScore,
            AccessibilityScore = input.Frameworks.AccessibilityScore,
            VisualBestPracticesScore = input.Frameworks.VisualBestPracticesScore,
            StephenFewScore = input.Frameworks.StephenFewScore,
            EnterpriseGovernanceScore = input.Frameworks.EnterpriseGovernanceScore,
            TufteScore = input.Frameworks.TufteScore,
            GraphicalPerceptionScore = input.Frameworks.GraphicalPerceptionScore,
            DensityScore = input.Frameworks.DensityScore,
            NarrativeScore = input.Frameworks.NarrativeScore,
            Feedback = input.Frameworks.Feedback,
            PageCount = input.PageCount,
            Recommendations = input.Recommendations,
            ReportPath = input.ReportPath,
            ScoredAt = input.ScoredAt,
            FrameworkWeights = input.FrameworkWeights,
            DataVisualCount = input.DataVisualCount,
            NavigationVisualCount = input.NavigationVisualCount,
            HiddenVisualCount = input.HiddenVisualCount,
            VisualMetadata = input.VisualMetadata,
            InferredStorySummary = input.InferredStorySummary,
            PageIntentProfile = input.PageIntentProfile,
            ActionabilityBreakdown = input.ActionabilityBreakdown,
            BenchmarkComparison = input.BenchmarkComparison,
            GuidedStoryImprovements = input.StoryAssessment?.GuidedStoryImprovements ?? new GuidedStoryImprovements(),
            InternalStorySignalRegistry = input.StoryAssessment?.SignalRegistry,
            InternalStoryAssessmentArchetypeClassification = input.StoryAssessment?.ArchetypeClassification,
            InternalStorySpecialPageAssessment = input.StoryAssessment?.SpecialPageAssessment,
            InternalStorySemanticCoherenceAssessment = input.StoryAssessment?.SemanticCoherenceAssessment,
            InternalStoryFilterTopologyAssessment = input.StoryAssessment?.FilterTopologyAssessment,
            InternalStoryGapAssessment = input.StoryAssessment?.GapAssessment,
            InternalStoryConfidenceBreakdownAssessment = input.StoryAssessment?.ConfidenceBreakdownAssessment,
        };

        _scoreCompatibilityAdapter.PopulateLegacyScores(result);
        return result;
    }
}

internal sealed class ScoreFrameworkSet
{
    public double GestaltScore { get; init; }
    public double CognitiveLoadScore { get; init; }
    public double DataInkScore { get; init; }
    public double AccessibilityScore { get; init; }
    public double VisualBestPracticesScore { get; init; }
    public double StephenFewScore { get; init; }
    public double EnterpriseGovernanceScore { get; init; }
    public double TufteScore { get; init; }
    public double GraphicalPerceptionScore { get; init; }
    public double DensityScore { get; init; }
    public double NarrativeScore { get; init; }
    public required Dictionary<string, List<FrameworkFeedbackItem>> Feedback { get; init; }
}

internal sealed class ScoreResultAssemblyInput
{
    public required ScoreFrameworkSet Frameworks { get; init; }
    public required List<string> Recommendations { get; init; }
    public string? ReportPath { get; init; }
    public int PageCount { get; init; }
    public string? ScoredPageId { get; init; }
    public string? ScoredPageName { get; init; }
    public DateTimeOffset ScoredAt { get; init; }
    public Dictionary<string, double>? FrameworkWeights { get; init; }
    public int DataVisualCount { get; init; }
    public int NavigationVisualCount { get; init; }
    public int HiddenVisualCount { get; init; }
    public PageVisualMetadataSummary? VisualMetadata { get; init; }
    public PageStorySummary? InferredStorySummary { get; init; }
    public PageIntentProfileSummary? PageIntentProfile { get; init; }
    public ActionabilityBreakdown? ActionabilityBreakdown { get; init; }
    public BenchmarkComparisonSummary? BenchmarkComparison { get; init; }
    public StoryAssessmentArtifacts? StoryAssessment { get; init; }
    public ReportConsistencySummary? ReportConsistencySummary { get; init; }
    public List<PageScore>? PageScores { get; init; }
    public Dictionary<string, string>? ScoringErrors { get; init; }
    public Dictionary<string, double>? PerStateScores { get; init; }
}

internal sealed class PageScoreAssemblyInput
{
    public string? PageId { get; init; }
    public required string PageName { get; init; }
    public required ScoreFrameworkSet Frameworks { get; init; }
    public required List<string> Recommendations { get; init; }
    public Dictionary<string, double>? FrameworkWeights { get; init; }
    public int DataVisualCount { get; init; }
    public int NavigationVisualCount { get; init; }
    public int HiddenVisualCount { get; init; }
    public PageVisualMetadataSummary? VisualMetadata { get; init; }
    public List<string> ReportConsistencyNotes { get; init; } = [];
    public PageStorySummary? InferredStorySummary { get; init; }
    public PageIntentProfileSummary? PageIntentProfile { get; init; }
    public ActionabilityBreakdown? ActionabilityBreakdown { get; init; }
    public BenchmarkComparisonSummary? BenchmarkComparison { get; init; }
    public StoryAssessmentArtifacts? StoryAssessment { get; init; }
    public Dictionary<string, double>? PerStateScores { get; init; }
    public ReportConsistencySummary? ReportConsistency { get; init; }
}
