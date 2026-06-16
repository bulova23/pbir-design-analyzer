using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class ScoreResultAssemblyServiceTests
{
    [Fact]
    public void CreateSinglePageResult_PopulatesCompatibilityAndStoryOutputs()
    {
        var service = new ScoreResultAssemblyService(new ScoreCompatibilityAdapter());
        var storyAssessment = BuildStoryAssessment();
        var visualMetadata = new PageVisualMetadataSummary
        {
            PageName = "Executive Summary",
            VisualCount = 2,
        };
        var scoredAt = new DateTimeOffset(2026, 6, 15, 15, 30, 0, TimeSpan.Zero);
        var frameworkWeights = new Dictionary<string, double> { ["gestalt"] = 100 };
        var recommendations = new List<string> { "[High] Layout: align the KPI band." };
        var perStateScores = new Dictionary<string, double>
        {
            ["Default"] = 80,
            ["Focus"] = 84,
        };

        var result = service.CreateSinglePageResult(new ScoreResultAssemblyInput
        {
            Frameworks = BuildFrameworks(),
            Recommendations = recommendations,
            ReportPath = "/tmp/report",
            PageCount = 1,
            ScoredPageId = "Page1",
            ScoredPageName = "Executive Summary",
            ScoredAt = scoredAt,
            FrameworkWeights = frameworkWeights,
            DataVisualCount = 2,
            NavigationVisualCount = 1,
            HiddenVisualCount = 0,
            VisualMetadata = visualMetadata,
            InferredStorySummary = new PageStorySummary
            {
                IntentProfile = "executive-overview",
                StoryArchetype = "overview",
                InferredStory = "Revenue overview.",
                Confidence = "high",
            },
            PageIntentProfile = new PageIntentProfileSummary
            {
                InferredProfile = "executive-overview",
                ActionabilityExpectation = "high",
            },
            ActionabilityBreakdown = new ActionabilityBreakdown
            {
                Score = 72,
                ExpectationLevel = "high",
                Summary = "Actionable.",
            },
            BenchmarkComparison = new BenchmarkComparisonSummary
            {
                Archetype = "executive overview",
                BenchmarkLabel = "benchmark",
                ComparativePosition = "aligned",
                Insight = "Close to target.",
            },
            StoryAssessment = storyAssessment,
            PerStateScores = perStateScores,
        });

        Assert.Equal(82, result.GestaltScore);
        Assert.Equal(recommendations, result.Recommendations);
        Assert.Equal("/tmp/report", result.ReportPath);
        Assert.Equal("Page1", result.ScoredPageId);
        Assert.Equal("Executive Summary", result.ScoredPageName);
        Assert.Equal(scoredAt, result.ScoredAt);
        Assert.Same(frameworkWeights, result.FrameworkWeights);
        Assert.Same(visualMetadata, result.VisualMetadata);
        Assert.Same(perStateScores, result.PerStateScores);
        Assert.Same(storyAssessment.GuidedStoryImprovements, result.GuidedStoryImprovements);
        Assert.Same(storyAssessment.SignalRegistry, result.InternalStorySignalRegistry);
        Assert.Same(storyAssessment.ArchetypeClassification, result.InternalStoryAssessmentArchetypeClassification);
#pragma warning disable CS0618
        Assert.Equal(result.GestaltScore, result.LayoutScore);
        Assert.Equal(result.VisualBestPracticesScore, result.ThemeScore);
        Assert.Equal(result.EnterpriseGovernanceScore, result.GovernanceScore);
#pragma warning restore CS0618
    }

    [Fact]
    public void CreateReportResult_PopulatesPageScoresErrorsAndCompatibilityFields()
    {
        var service = new ScoreResultAssemblyService(new ScoreCompatibilityAdapter());
        var pageScores = new List<PageScore>
        {
            new()
            {
                PageId = "Page1",
                PageName = "Overview",
                GestaltScore = 80,
                CognitiveLoadScore = 70,
                DataInkScore = 75,
                AccessibilityScore = 78,
                VisualBestPracticesScore = 82,
                StephenFewScore = 68,
                EnterpriseGovernanceScore = 90,
                TufteScore = 77,
                GraphicalPerceptionScore = 74,
                DensityScore = 71,
                NarrativeScore = 69,
                Feedback = new Dictionary<string, List<FrameworkFeedbackItem>>(),
                Recommendations = [],
            }
        };
        var scoringErrors = new Dictionary<string, string>
        {
            ["Detail"] = "Failed to score page 'Detail': parse error"
        };
        var reportConsistency = new ReportConsistencySummary
        {
            ConsistentTitleAnchors = false,
            ConsistentFilterBand = true,
            ConsistentMetricLabels = true,
            ConsistentSemanticColors = false,
            IssueCount = 2,
        };

        var result = service.CreateReportResult(new ScoreResultAssemblyInput
        {
            Frameworks = BuildFrameworks(),
            Recommendations = new List<string> { "[Medium] Narrative: add a clearer title." },
            ReportPath = "/tmp/report",
            PageCount = 3,
            ScoredAt = new DateTimeOffset(2026, 6, 15, 15, 35, 0, TimeSpan.Zero),
            FrameworkWeights = new Dictionary<string, double> { ["gestalt"] = 100 },
            DataVisualCount = 7,
            NavigationVisualCount = 2,
            HiddenVisualCount = 1,
            PageScores = pageScores,
            ScoringErrors = scoringErrors,
            ReportConsistencySummary = reportConsistency,
        });

        Assert.Same(pageScores, result.PageScores);
        Assert.Same(scoringErrors, result.ScoringErrors);
        Assert.Same(reportConsistency, result.ReportConsistencySummary);
#pragma warning disable CS0618
        Assert.Equal(result.GestaltScore, result.LayoutScore);
        Assert.Equal(result.VisualBestPracticesScore, result.ThemeScore);
        Assert.Equal(result.EnterpriseGovernanceScore, result.GovernanceScore);
#pragma warning restore CS0618
    }

    private static ScoreFrameworkSet BuildFrameworks()
    {
        var feedback = new Dictionary<string, List<FrameworkFeedbackItem>>
        {
            ["gestalt"] = [new FrameworkFeedbackItem(true, "Aligned.")],
            ["cognitiveLoad"] = [new FrameworkFeedbackItem(true, "Readable.")],
        };

        return new ScoreFrameworkSet
        {
            GestaltScore = 82,
            CognitiveLoadScore = 73,
            DataInkScore = 74,
            AccessibilityScore = 76,
            VisualBestPracticesScore = 79,
            StephenFewScore = 66,
            EnterpriseGovernanceScore = 91,
            TufteScore = 71,
            GraphicalPerceptionScore = 75,
            DensityScore = 68,
            NarrativeScore = 72,
            Feedback = feedback,
        };
    }

    private static StoryAssessmentArtifacts BuildStoryAssessment()
    {
        var orchestrator = new StoryAssessmentOrchestrator();
        var page = new PageData
        {
            Name = "Page1",
            DisplayName = "Executive Summary",
            Visuals =
            [
                new VisualData
                {
                    Id = "t1",
                    Type = "textbox",
                    X = 0,
                    Y = 0,
                    W = 400,
                    H = 40,
                    Text = new VisualTextMetadata(null, null, "Revenue vs Target"),
                },
                new VisualData
                {
                    Id = "v1",
                    Type = "lineChart",
                    X = 0,
                    Y = 60,
                    W = 420,
                    H = 220,
                    Text = new VisualTextMetadata("Revenue Trend", null, null),
                    FieldRoles = new VisualFieldRoleMetadata(["Month"], [], [], ["Revenue"]),
                },
            ],
        };

        return orchestrator.Assess(page, []);
    }
}
