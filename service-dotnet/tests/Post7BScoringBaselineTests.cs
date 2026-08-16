using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;
using StoryAssessmentValidationExport;
using Xunit;

namespace PowerBIModelingService.Tests;

public sealed class Post7BScoringBaselineTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    [Fact]
    public async Task RealCorpus_Post7BBaselines_MatchNormalizedProjection_WhenFixturesAreAvailable()
    {
        var availableReports = GetRepresentativeReports()
            .Where(report => File.Exists(report.ReportPath))
            .ToList();
        if (availableReports.Count == 0)
        {
            return;
        }

        var scoringService = new PbirScoringService(
            new PbirProjectService(NullLogger<PbirProjectService>.Instance),
            NullLogger<PbirScoringService>.Instance);
        var exportService = new StoryAssessmentValidationExportService();

        foreach (var report in availableReports)
        {
            var score = await scoringService.ScoreAsync(report.ReportPath);
            var export = await exportService.CreateReportAsync(report.ReportPath);
            var actual = JsonSerializer.Serialize(BuildProjection(report.ReportFileName, score, export), SerializerOptions) + Environment.NewLine;
            var expected = await File.ReadAllTextAsync(report.BaselinePath);

            Assert.Equal(expected, actual);
        }
    }

    private static object BuildProjection(
        string reportFileName,
        ScoreResult score,
        StoryAssessmentValidationExportReport export)
    {
        return new
        {
            reportName = Path.GetFileNameWithoutExtension(reportFileName),
            fixture = reportFileName,
            scoreSummary = new
            {
                score.PageCount,
                score.CompositeScore,
                score.GestaltScore,
                score.CognitiveLoadScore,
                score.DataInkScore,
                score.AccessibilityScore,
                score.VisualBestPracticesScore,
                score.EnterpriseGovernanceScore,
                score.GraphicalPerceptionScore,
                score.StephenFewScore,
                score.TufteScore,
                score.DensityScore,
                score.NarrativeScore,
                score.DataVisualCount,
                score.NavigationVisualCount,
                score.HiddenVisualCount,
            },
            recommendations = score.Recommendations,
            topLevelGuidedStoryImprovements = new
            {
                highPriorityIds = score.GuidedStoryImprovements.HighPriorityImprovements.Select(improvement => improvement.Id).ToList(),
                mediumPriorityIds = score.GuidedStoryImprovements.MediumPriorityImprovements.Select(improvement => improvement.Id).ToList(),
                rationale = score.GuidedStoryImprovements.StoryImprovementRationale,
            },
            reportConsistency = score.ReportConsistencySummary is null
                ? null
                : new
                {
                    score.ReportConsistencySummary.IssueCount,
                    score.ReportConsistencySummary.OverallFinding,
                    affectedPages = score.ReportConsistencySummary.AffectedPages,
                    issues = score.ReportConsistencySummary.Issues.Select(issue => new
                    {
                        issue.Category,
                        issue.IssueCategory,
                        issue.Severity,
                        issue.Confidence,
                        issue.AffectedPages,
                    }).ToList(),
                },
            pageSummaries = (score.PageScores ?? []).Select(page => new
            {
                page.PageName,
                page.CompositeScore,
                detectedStory = page.InferredStorySummary?.InferredStory,
                storyArchetype = page.InferredStorySummary?.StoryArchetype,
                highPriorityGuidedStoryImprovementIds = page.GuidedStoryImprovements.HighPriorityImprovements.Select(improvement => improvement.Id).ToList(),
                mediumPriorityGuidedStoryImprovementIds = page.GuidedStoryImprovements.MediumPriorityImprovements.Select(improvement => improvement.Id).ToList(),
                recommendationCount = page.Recommendations.Count,
                benchmarkArchetype = page.BenchmarkComparison?.Archetype,
                actionabilityScore = page.ActionabilityBreakdown?.Score,
            }).ToList(),
            validationExport = new
            {
                pages = export.Pages.Select(page => new
                {
                    page.PageName,
                    page.DetectedStory,
                    page.SpecialPageResult,
                    page.ArchetypeClassification,
                    page.ArchetypeSuppressionStatus,
                    page.SemanticCoherenceResult,
                    page.CompetingStoryStatus,
                    page.FilterTopologyResult,
                    storyGapIds = page.StoryGaps.Select(gap => gap.GapId).ToList(),
                    futureContractCandidateGapIds = page.StoryGaps
                        .Where(gap => gap.IsFutureContractCandidate)
                        .Select(gap => gap.GapId)
                        .ToList(),
                    confidenceRatings = page.ConfidenceBreakdown
                        .Select(dimension => new { dimension.DimensionId, dimension.Rating })
                        .ToList(),
                    page.PromotionStates,
                    page.SurfaceScopes,
                }).ToList(),
                crossPageNarrative = export.CrossPageNarrative is null
                    ? null
                    : new
                    {
                        export.CrossPageNarrative.DominantReportObjective,
                        export.CrossPageNarrative.MainNarrativePath,
                        pageRoles = export.CrossPageNarrative.PageRoles
                            .Select(role => new { role.PageName, role.Role, role.Confidence })
                            .ToList(),
                        orphanDecisions = export.CrossPageNarrative.OrphanDecisions
                            .Select(decision => new { decision.PageName, decision.OrphanState })
                            .ToList(),
                        dimensionScores = export.CrossPageNarrative.DimensionScores
                            .Select(dimension => new { dimension.DimensionId, dimension.Score, dimension.Confidence })
                            .ToList(),
                        reportLevelGapStableIds = export.CrossPageNarrative.ReportLevelGaps
                            .Select(gap => gap.StableId)
                            .ToList(),
                    },
            },
        };
    }

    private static IReadOnlyList<RepresentativeReport> GetRepresentativeReports()
    {
        var baselinesRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "Baselines",
            "Post7BScoring"));

        return
        [
            new RepresentativeReport(
                "/Users/bcrowell/Documents/GitHub/PBITesting/Sales & Production.pbip",
                "Sales & Production.pbip",
                Path.Combine(baselinesRoot, "sales-and-production.baseline.json")),
            new RepresentativeReport(
                "/Users/bcrowell/Documents/GitHub/PBITest2/Sales Analysis.pbip",
                "Sales Analysis.pbip",
                Path.Combine(baselinesRoot, "sales-analysis.baseline.json")),
            new RepresentativeReport(
                "/Users/bcrowell/Documents/GitHub/PBITest3/Running Record Dataverse.pbip",
                "Running Record Dataverse.pbip",
                Path.Combine(baselinesRoot, "running-record-dataverse.baseline.json")),
            new RepresentativeReport(
                "/Users/bcrowell/Documents/GitHub/PBITest4/Sales AWF.pbip",
                "Sales AWF.pbip",
                Path.Combine(baselinesRoot, "sales-awf.baseline.json")),
        ];
    }

    private sealed record RepresentativeReport(string ReportPath, string ReportFileName, string BaselinePath);
}
