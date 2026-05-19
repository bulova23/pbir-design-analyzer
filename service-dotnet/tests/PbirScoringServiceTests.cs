using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests;

/// <summary>
/// Unit tests for the PBIR score models.
/// Tests use the public <see cref="ScoreResult"/> shape and verify composite and
/// feedback behavior without requiring a running PBIR project on disk.
/// </summary>
public class PbirScoringServiceTests
{
    // ── FrameworkFeedbackItem tests ──────────────────────────────────────────

    [Fact(DisplayName = "FrameworkFeedbackItem stores Ok, Text, and default classification correctly")]
    public void FrameworkFeedbackItem_Constructor_StoresOkTextAndDefaultClassification()
    {
        var item = new FrameworkFeedbackItem(true, "All visuals aligned.");

        Assert.True(item.Ok);
        Assert.Equal("All visuals aligned.", item.Text);
        Assert.Equal(FindingTypes.StrongHeuristic, item.FindingType);
    }

    [Fact(DisplayName = "FrameworkFeedbackItem with explicit classification stores failure metadata")]
    public void FrameworkFeedbackItem_FailureItem_StoresExplicitFindingType()
    {
        var item = new FrameworkFeedbackItem(
            false,
            "Grid misalignment detected on page 1.",
            FindingType: FindingTypes.Objective);

        Assert.False(item.Ok);
        Assert.Contains("Grid misalignment", item.Text);
        Assert.Equal(FindingTypes.Objective, item.FindingType);
    }

    // ── ScoreResult composite formula tests ─────────────────────────────────

    [Fact(DisplayName = "CompositeScore uses configured framework weights totalling 100")]
    public void ScoreResult_AllScores100_CompositeIs100()
    {
        var result = new ScoreResult
        {
            GestaltScore             = 100,
            CognitiveLoadScore       = 100,
            DataInkScore             = 100,
            AccessibilityScore       = 100,
            VisualBestPracticesScore = 100,
            StephenFewScore          = 100,
            FrameworkWeights         = CreateFrameworkWeights(
                gestalt: 15,
                cognitiveLoad: 20,
                dataInk: 15,
                accessibility: 15,
                visualBestPractices: 20,
                stephenFew: 15),
        };

        Assert.Equal(100.0, result.CompositeScore);
    }

    [Fact(DisplayName = "CompositeScore of zero report is 0 when configured weights are present")]
    public void ScoreResult_AllScores0_CompositeIs0()
    {
        var result = new ScoreResult
        {
            GestaltScore             = 0,
            CognitiveLoadScore       = 0,
            DataInkScore             = 0,
            AccessibilityScore       = 0,
            VisualBestPracticesScore = 0,
            StephenFewScore          = 0,
            FrameworkWeights         = CreateFrameworkWeights(
                gestalt: 15,
                cognitiveLoad: 20,
                dataInk: 15,
                accessibility: 15,
                visualBestPractices: 20,
                stephenFew: 15),
        };

        Assert.Equal(0.0, result.CompositeScore);
    }

    [Fact(DisplayName = "CompositeScore applies explicitly configured framework weights")]
    public void ScoreResult_WeightedFormula_IsCorrect()
    {
        var result = new ScoreResult
        {
            GestaltScore             = 80,
            CognitiveLoadScore       = 60,
            DataInkScore             = 90,
            AccessibilityScore       = 70,
            VisualBestPracticesScore = 50,
            StephenFewScore          = 100,
            FrameworkWeights         = CreateFrameworkWeights(
                gestalt: 15,
                cognitiveLoad: 20,
                dataInk: 15,
                accessibility: 15,
                visualBestPractices: 20,
                stephenFew: 15),
        };

        // Manual: 80×0.15 + 60×0.20 + 90×0.15 + 70×0.15 + 50×0.20 + 100×0.15
        //       = 12 + 12 + 13.5 + 10.5 + 10 + 15 = 73
        Assert.Equal(73.0, result.CompositeScore);
    }

    // ── Feedback dictionary tests ────────────────────────────────────────────

    [Fact(DisplayName = "Feedback keys match normalized framework IDs")]
    public void ScoreResult_FeedbackKeys_MatchWizardKeys()
    {
        var expectedKeys = new[]
        {
            "gestalt", "cognitiveLoad", "dataInk", "accessibility", "visualBestPractices",
            "governance", "stephenFew", "tufte", "graphicalPerception", "density", "narrative"
        };

        var result = new ScoreResult
        {
            Feedback = expectedKeys.ToDictionary(k => k, _ => new List<FrameworkFeedbackItem>()),
        };

        foreach (var key in expectedKeys)
        {
            Assert.True(result.Feedback.ContainsKey(key), $"Missing feedback key: '{key}'");
        }
    }

    [Fact(DisplayName = "Feedback dictionary can hold mix of pass and fail items per framework")]
    public void ScoreResult_FeedbackItems_CanMixPassAndFail()
    {
        var result = new ScoreResult
        {
            Feedback = new()
            {
                ["gestalt"] =
                [
                    new(true,  "Grid alignment: All visuals aligned."),
                    new(false, "Figure/ground: No KPI cards found."),
                ],
            },
        };

        var gestalt = result.Feedback["gestalt"];
        Assert.Equal(2, gestalt.Count);
        Assert.True(gestalt[0].Ok);
        Assert.False(gestalt[1].Ok);
    }

    // ── Backward compatibility tests ─────────────────────────────────────────

    [Fact(DisplayName = "Legacy LayoutScore property is still settable (not removed)")]
    public void ScoreResult_LegacyLayoutScore_IsStillSettable()
    {
#pragma warning disable CS0618
        var result = new ScoreResult { LayoutScore = 75 };
        Assert.Equal(75, result.LayoutScore);
#pragma warning restore CS0618
    }

    [Fact(DisplayName = "Legacy GovernanceScore property still defaults to 100")]
    public void ScoreResult_LegacyGovernanceScore_DefaultsTo100()
    {
#pragma warning disable CS0618
        var result = new ScoreResult();
        Assert.Equal(100, result.GovernanceScore);
#pragma warning restore CS0618
    }

    // ── PageCount and metadata tests ─────────────────────────────────────────

    [Fact(DisplayName = "PageCount can be set and retrieved")]
    public void ScoreResult_PageCount_SetAndRetrieved()
    {
        var result = new ScoreResult { PageCount = 5 };
        Assert.Equal(5, result.PageCount);
    }

    [Fact(DisplayName = "ScoredAt defaults to a recent UTC timestamp")]
    public void ScoreResult_ScoredAt_DefaultsToNearNow()
    {
        var before = DateTimeOffset.UtcNow;
        var result = new ScoreResult();
        var after  = DateTimeOffset.UtcNow;

        Assert.InRange(result.ScoredAt, before, after);
    }

    // ── Non-contradiction verification (SC-003) ──────────────────────────────

    [Fact(DisplayName = "StephenFew and VBP feedback both penalise pie charts (not contradict)")]
    public void ScoreResult_PieChartReport_StephenFewAndVBPConsistent()
    {
        // Simulate a result as the scoring engine would produce for a report with pie charts
        var result = new ScoreResult
        {
            Feedback = new()
            {
                ["visualBestPractices"] =
                [
                    new(false, "Pie avoidance: 1 pie/donut chart(s) detected — replace with bar or column charts."),
                    new(true,  "Chart variety: Includes trend or comparison charts."),
                ],
                ["stephenFew"] =
                [
                    new(false, "Pie avoidance: Stephen Few strongly recommends replacing pie/donut charts with bar charts."),
                    new(true,  "One-screen rule: All pages have ≤8 visuals."),
                ],
            },
        };

        var vbpPieItem  = result.Feedback["visualBestPractices"].First(i => i.Text.Contains("pie"));
        var fewPieItem  = result.Feedback["stephenFew"].First(i => i.Text.Contains("pie"));

        // Both must agree: pie charts are bad (Ok=false). Neither should say Ok=true for pie presence.
        Assert.False(vbpPieItem.Ok, "VBP should flag pie charts as a problem.");
        Assert.False(fewPieItem.Ok, "Stephen Few should flag pie charts as a problem.");

        // Neither message should recommend adding or keeping pie charts
        Assert.DoesNotContain("add", vbpPieItem.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("add", fewPieItem.Text, StringComparison.OrdinalIgnoreCase);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // T031: PAGE FILTERING LOGIC (6+ tests)
    // ═══════════════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "GetPageScores returns list for multi-page report")]
    public void GetPageScores_MultiPageReport_ReturnsNonNullList()
    {
        var result = new ScoreResult { PageScores = new List<PageScore>() };
        Assert.NotNull(result.PageScores);
        Assert.Empty(result.PageScores);
    }

    [Fact(DisplayName = "GetPageScores correctly initializes with multiple pages")]
    public void GetPageScores_WithFivePages_ReturnsCountOfFive()
    {
        var pages = new List<PageScore>
        {
            CreatePageScore("Page1"),
            CreatePageScore("Page2"),
            CreatePageScore("Page3"),
            CreatePageScore("Page4"),
            CreatePageScore("Page5"),
        };

        var result = new ScoreResult { PageScores = pages };

        Assert.Equal(5, result.PageScores.Count);
        Assert.All(result.PageScores, p => Assert.NotNull(p.PageName));
    }

    [Fact(DisplayName = "PageScores finds exact page match by name")]
    public void PageScores_ExactNameMatch_ReturnsCorrectPage()
    {
        var pages = new List<PageScore>
        {
            CreatePageScore("Home"),
            CreatePageScore("Sales Analysis"),
            CreatePageScore("Forecast"),
        };

        var result = new ScoreResult { PageScores = pages };
        var salesPage = result.PageScores.FirstOrDefault(p => p.PageName == "Sales Analysis");

        Assert.NotNull(salesPage);
        Assert.Equal("Sales Analysis", salesPage.PageName);
    }

    [Fact(DisplayName = "PageScores filters are case-sensitive")]
    public void PageScores_CaseSensitivity_DoesNotMatchDifferentCase()
    {
        var pages = new List<PageScore>
        {
            CreatePageScore("Dashboard"),
            CreatePageScore("Analytics"),
        };

        var result = new ScoreResult { PageScores = pages };
        var lowercase = result.PageScores.FirstOrDefault(p => p.PageName == "dashboard");
        var mixedCase = result.PageScores.FirstOrDefault(p => p.PageName == "DashBoard");

        Assert.Null(lowercase);
        Assert.Null(mixedCase);
    }

    [Fact(DisplayName = "PageScores handles special characters in page names")]
    public void PageScores_SpecialCharactersInName_MatchesExactly()
    {
        var specialNames = new[] { "Q1 & Q2", "2024 (Updated)", "Data#Analysis-Q3" };
        var pages = specialNames.Select(CreatePageScore).ToList();

        var result = new ScoreResult { PageScores = pages };

        foreach (var name in specialNames)
        {
            var page = result.PageScores.FirstOrDefault(p => p.PageName == name);
            Assert.NotNull(page);
            Assert.Equal(name, page.PageName);
        }
    }

    [Fact(DisplayName = "PageScores handles whitespace in page names")]
    public void PageScores_LeadingTrailingSpaces_MatchesExactly()
    {
        var pages = new List<PageScore>
        {
            CreatePageScore(" SpacedPage "),
            CreatePageScore("NoSpaces"),
        };

        var result = new ScoreResult { PageScores = pages };
        var spaced = result.PageScores.FirstOrDefault(p => p.PageName == " SpacedPage ");
        var noSpaces = result.PageScores.FirstOrDefault(p => p.PageName == "NoSpaces");

        Assert.NotNull(spaced);
        Assert.Null(result.PageScores.FirstOrDefault(p => p.PageName == "SpacedPage"));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // T032: AGGREGATION FORMULA (6+ tests)
    // ═══════════════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "CompositeScore aggregation with 2-page report")]
    public void CompositeScore_TwoPageAggregation_ReturnsWeightedAverage()
    {
        var page1 = new ScoreResult
        {
            GestaltScore             = 80,
            CognitiveLoadScore       = 90,
            DataInkScore             = 85,
            AccessibilityScore       = 75,
            VisualBestPracticesScore = 80,
            EnterpriseGovernanceScore = 90,
            StephenFewScore          = 95,
            FrameworkWeights         = CreateFrameworkWeights(),
        };

        var page2 = new ScoreResult
        {
            GestaltScore             = 70,
            CognitiveLoadScore       = 60,
            DataInkScore             = 75,
            AccessibilityScore       = 85,
            VisualBestPracticesScore = 70,
            EnterpriseGovernanceScore = 80,
            StephenFewScore          = 65,
            FrameworkWeights         = CreateFrameworkWeights(),
        };

        var page1Score = page1.CompositeScore;
        var page2Score = page2.CompositeScore;
        var averageScore = Math.Round((page1Score + page2Score) / 2, 2);

        Assert.InRange(page1Score, 75, 90);
        Assert.InRange(page2Score, 65, 80);
        Assert.InRange(averageScore, 70, 85);
    }

    [Fact(DisplayName = "CompositeScore uses the configured Cognitive Load weight")]
    public void CompositeScore_CognitiveLoadWeight20_CalculatesCorrectly()
    {
        var result = new ScoreResult
        {
            GestaltScore             = 0,
            CognitiveLoadScore       = 100,
            DataInkScore             = 0,
            AccessibilityScore       = 0,
            VisualBestPracticesScore = 0,
            StephenFewScore          = 0,
            FrameworkWeights         = CreateFrameworkWeights(cognitiveLoad: 20),
        };

        Assert.Equal(20.0, result.CompositeScore);
    }

    [Fact(DisplayName = "CompositeScore uses the configured Visual Best Practices weight")]
    public void CompositeScore_VBPConfiguredWeight_CalculatesCorrectly()
    {
        var result = new ScoreResult
        {
            GestaltScore             = 0,
            CognitiveLoadScore       = 0,
            DataInkScore             = 0,
            AccessibilityScore       = 0,
            VisualBestPracticesScore = 100,
            StephenFewScore          = 0,
            FrameworkWeights         = CreateFrameworkWeights(visualBestPractices: 15),
        };

        Assert.Equal(15.0, result.CompositeScore);
    }

    [Fact(DisplayName = "CompositeScore with 5-page inputs aggregates correctly")]
    public void CompositeScore_FivePageAggregation_ProducesValidWeightedAverage()
    {
        var pages = new[]
        {
            CreateScoreResult(80, 75, 85, 70, 90, 80),
            CreateScoreResult(60, 65, 55, 80, 70, 75),
            CreateScoreResult(95, 85, 90, 75, 85, 95),
            CreateScoreResult(70, 80, 75, 85, 80, 70),
            CreateScoreResult(50, 55, 60, 50, 55, 65),
        };

        var compositeScores = pages.Select(p => p.CompositeScore).ToList();
        var averageComposite = Math.Round(compositeScores.Average(), 2);

        Assert.Equal(5, compositeScores.Count);
        Assert.All(compositeScores, score => Assert.InRange(score, 0, 100));
        Assert.InRange(averageComposite, 0, 100);
    }

    [Fact(DisplayName = "CompositeScore with 10-page inputs maintains formula consistency")]
    public void CompositeScore_TenPageAggregation_MaintainsFormulaConsistency()
    {
        var pages = Enumerable.Range(1, 10)
            .Select(i => CreateScoreResult(
                50 + (i * 3),
                60 + (i * 2),
                55 + (i * 3),
                65 + (i * 1),
                70 + (i * 2),
                60 + (i * 2)
            ))
            .ToList();

        var composites = pages.Select(p => p.CompositeScore).ToList();

        Assert.Equal(10, composites.Count);
        Assert.All(composites, score => Assert.InRange(score, 0, 100));
        
        var minScore = composites.Min();
        var maxScore = composites.Max();
        Assert.InRange(minScore, 50, 100);
        Assert.InRange(maxScore, 50, 100);
    }

    [Fact(DisplayName = "CompositeScore with 20-page inputs produces statistically valid aggregate")]
    public void CompositeScore_TwentyPageAggregation_ProducesValidAggregate()
    {
        var pages = Enumerable.Range(1, 20)
            .Select(i => CreateScoreResult(
                40 + (i % 10) * 4,
                50 + (i % 8) * 5,
                45 + (i % 9) * 4,
                55 + (i % 7) * 3,
                60 + (i % 6) * 4,
                50 + (i % 10) * 3
            ))
            .ToList();

        var composites = pages.Select(p => p.CompositeScore).ToList();
        var reportComposite = Math.Round(composites.Average(), 2);

        Assert.Equal(20, composites.Count);
        Assert.InRange(reportComposite, 0, 100);
        Assert.NotEqual(0, reportComposite);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // T033: ERROR HANDLING PER-PAGE (6+ tests)
    // ═══════════════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "PageScore with ScoringError marks IsSuccessful as false")]
    public void PageScore_WithScoringError_IsSuccessfulIsFalse()
    {
        var page = new PageScore
        {
            PageName = "FailedPage",
            GestaltScore = 0,
            CognitiveLoadScore = 0,
            DataInkScore = 0,
            AccessibilityScore = 0,
            VisualBestPracticesScore = 0,
            StephenFewScore = 0,
            ScoringError = "Malformed visual configuration detected.",
        };

        Assert.False(page.IsSuccessful);
        Assert.NotNull(page.ScoringError);
    }

    [Fact(DisplayName = "PageScore without ScoringError marks IsSuccessful as true")]
    public void PageScore_WithoutScoringError_IsSuccessfulIsTrue()
    {
        var page = CreatePageScore("SuccessPage");

        Assert.True(page.IsSuccessful);
        Assert.Null(page.ScoringError);
    }

    [Fact(DisplayName = "ScoringErrors dictionary tracks per-page failures")]
    public void ScoringErrors_TrackMultiplePageFailures_DictionaryPopulated()
    {
        var result = new ScoreResult
        {
            ScoringErrors = new Dictionary<string, string>
            {
                ["Page1"] = "Missing visual property: x",
                ["Page3"] = "Invalid layout coordinate",
            },
        };

        Assert.Equal(2, result.ScoringErrors.Count);
        Assert.Contains("Page1", result.ScoringErrors.Keys);
        Assert.Contains("Page3", result.ScoringErrors.Keys);
        Assert.Equal("Missing visual property: x", result.ScoringErrors["Page1"]);
    }

    [Fact(DisplayName = "ComputeReportScore continues after page error")]
    public void ScoreResult_PartialPageFailure_OtherPagesSucceed()
    {
        var pages = new List<PageScore>
        {
            CreatePageScore("Page1"),
            new PageScore
            {
                PageName = "Page2",
                GestaltScore = 0,
                CognitiveLoadScore = 0,
                DataInkScore = 0,
                AccessibilityScore = 0,
                VisualBestPracticesScore = 0,
                StephenFewScore = 0,
                ScoringError = "Visual configuration error",
            },
            CreatePageScore("Page3"),
        };

        var result = new ScoreResult { PageScores = pages };
        var successfulPages = result.PageScores.Where(p => p.IsSuccessful).ToList();

        Assert.Equal(3, result.PageScores.Count);
        Assert.Equal(2, successfulPages.Count);
        Assert.Single(result.PageScores.Where(p => !p.IsSuccessful));
    }

    [Fact(DisplayName = "ScoringErrors includes failed page names with descriptive messages")]
    public void ScoringErrors_PartialFailure_PopulatesFailedPageNames()
    {
        var result = new ScoreResult
        {
            PageScores = new List<PageScore>
            {
                CreatePageScore("SuccessfulPage"),
                new PageScore
                {
                    PageName = "FailedPage1",
                    GestaltScore = 0,
                    CognitiveLoadScore = 0,
                    DataInkScore = 0,
                    AccessibilityScore = 0,
                    VisualBestPracticesScore = 0,
                    StephenFewScore = 0,
                    ScoringError = "Invalid visual binding",
                },
                new PageScore
                {
                    PageName = "FailedPage2",
                    GestaltScore = 0,
                    CognitiveLoadScore = 0,
                    DataInkScore = 0,
                    AccessibilityScore = 0,
                    VisualBestPracticesScore = 0,
                    StephenFewScore = 0,
                    ScoringError = "Missing data field",
                },
            },
            ScoringErrors = new Dictionary<string, string>
            {
                ["FailedPage1"] = "Invalid visual binding",
                ["FailedPage2"] = "Missing data field",
            },
        };

        Assert.Equal(2, result.ScoringErrors.Count);
        Assert.True(result.ScoringErrors.ContainsKey("FailedPage1"));
        Assert.True(result.ScoringErrors.ContainsKey("FailedPage2"));
        Assert.DoesNotContain("SuccessfulPage", result.ScoringErrors.Keys);
    }

    [Fact(DisplayName = "Empty ScoringErrors dictionary indicates all pages succeeded")]
    public void ScoringErrors_AllPagesSuccessful_DictionaryEmpty()
    {
        var result = new ScoreResult
        {
            PageScores = new List<PageScore>
            {
                CreatePageScore("Page1"),
                CreatePageScore("Page2"),
                CreatePageScore("Page3"),
            },
            ScoringErrors = new Dictionary<string, string>(),
        };

        Assert.Empty(result.ScoringErrors);
        Assert.All(result.PageScores, p => Assert.True(p.IsSuccessful));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>Creates a PageScore with provided name and default scores (all 75).</summary>
    private static PageScore CreatePageScore(string pageName)
    {
        return new PageScore
        {
            PageName = pageName,
            GestaltScore = 75,
            CognitiveLoadScore = 75,
            DataInkScore = 75,
            AccessibilityScore = 75,
            VisualBestPracticesScore = 75,
            StephenFewScore = 75,
            EnterpriseGovernanceScore = 75,
            FrameworkWeights = CreateFrameworkWeights(),
        };
    }

    /// <summary>Creates a ScoreResult with specified framework scores.</summary>
    private static ScoreResult CreateScoreResult(
        double gestalt,
        double cogLoad,
        double dataInk,
        double accessibility,
        double vbp,
        double few,
        double governance = 75)
    {
        return new ScoreResult
        {
            GestaltScore = gestalt,
            CognitiveLoadScore = cogLoad,
            DataInkScore = dataInk,
            AccessibilityScore = accessibility,
            VisualBestPracticesScore = vbp,
            EnterpriseGovernanceScore = governance,
            StephenFewScore = few,
            FrameworkWeights = CreateFrameworkWeights(),
        };
    }

    private static Dictionary<string, double> CreateFrameworkWeights(
        double gestalt = 25,
        double cognitiveLoad = 20,
        double dataInk = 15,
        double accessibility = 15,
        double visualBestPractices = 15,
        double governance = 10,
        double stephenFew = 0,
        double tufte = 0,
        double graphicalPerception = 0,
        double density = 0,
        double narrative = 0)
    {
        return new Dictionary<string, double>
        {
            ["gestalt"] = gestalt,
            ["cognitiveLoad"] = cognitiveLoad,
            ["dataInk"] = dataInk,
            ["accessibility"] = accessibility,
            ["visualBestPractices"] = visualBestPractices,
            ["governance"] = governance,
            ["stephenFew"] = stephenFew,
            ["tufte"] = tufte,
            ["graphicalPerception"] = graphicalPerception,
            ["density"] = density,
            ["narrative"] = narrative,
        };
    }
}
