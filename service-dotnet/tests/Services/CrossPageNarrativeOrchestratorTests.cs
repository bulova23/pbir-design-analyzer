using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class CrossPageNarrativeOrchestratorTests
{
    [Fact]
    public void Build_ReturnsNull_ForSinglePage()
    {
        var orchestrator = new CrossPageNarrativeOrchestrator();

        var assessment = orchestrator.Build(
            [
                new PageScore
                {
                    PageId = "overview",
                    PageName = "Executive Overview",
                },
            ]);

        Assert.Null(assessment);
    }

    [Fact]
    public void Build_AssessesOverviewToDetailFlowAndSpecialPages()
    {
        var orchestrator = new CrossPageNarrativeOrchestrator();
        var assessment = orchestrator.Build(
            [
                CreateOverviewPageScore(),
                CreateDetailPageScore(),
                CreateTooltipPageScore(),
            ]);

        Assert.NotNull(assessment);
        Assert.Equal("executive performance review", assessment!.DominantReportObjective);
        Assert.Contains(
            assessment.Pages,
            page => page.PageId == "overview" && page.RoleAssignment.PrimaryRole == CrossPageNarrativeRoleId.Overview);
        Assert.Contains(
            assessment.Pages,
            page => page.PageId == "detail" && page.RoleAssignment.PrimaryRole == CrossPageNarrativeRoleId.DetailDrill);
        Assert.Contains(
            assessment.Pages,
            page => page.PageId == "tooltip" && page.RoleAssignment.PrimaryRole == CrossPageNarrativeRoleId.Tooltip);
        Assert.Contains(
            assessment.Graph.Edges,
            edge => edge.SourcePageId == "overview" &&
                    edge.TargetPageId == "detail" &&
                    edge.EdgeType == CrossPageNarrativeEdgeType.OrderedNext);
    }

    private static PageScore CreateOverviewPageScore()
    {
        return new PageScore
        {
            PageId = "overview",
            PageName = "Executive Overview",
            DataVisualCount = 3,
            VisualMetadata = new PageVisualMetadataSummary
            {
                PageName = "Executive Overview",
                VisiblePageTitle = "Executive Overview",
                Visuals =
                [
                    new VisualMetadataItem { VisualId = "k1", VisualType = "card" },
                    new VisualMetadataItem { VisualId = "v1", VisualType = "barChart" },
                ],
            },
            InferredStorySummary = new PageStorySummary
            {
                IntentProfile = "executive review",
                StoryArchetype = "NarrativeWalkthrough",
                InferredStory = "Executive revenue review",
                Confidence = "High",
            },
            PageIntentProfile = new PageIntentProfileSummary
            {
                InferredProfile = "executive review",
                ActionabilityExpectation = "high",
            },
        };
    }

    private static PageScore CreateDetailPageScore()
    {
        return new PageScore
        {
            PageId = "detail",
            PageName = "Regional Detail",
            DataVisualCount = 2,
            VisualMetadata = new PageVisualMetadataSummary
            {
                PageName = "Regional Detail",
                VisiblePageTitle = "Regional Detail",
                Visuals =
                [
                    new VisualMetadataItem { VisualId = "t1", VisualType = "tableEx" },
                ],
            },
            InferredStorySummary = new PageStorySummary
            {
                IntentProfile = "detail analysis",
                StoryArchetype = "Comparison",
                InferredStory = "Regional revenue detail investigation",
                Confidence = "High",
            },
            PageIntentProfile = new PageIntentProfileSummary
            {
                InferredProfile = "detail analysis",
                ActionabilityExpectation = "high",
            },
            ActionabilityBreakdown = new ActionabilityBreakdown
            {
                DrillPathPresent = true,
                ExpectationLevel = "high",
                Summary = "Detailed drill path is available.",
            },
        };
    }

    private static PageScore CreateTooltipPageScore()
    {
        return new PageScore
        {
            PageId = "tooltip",
            PageName = "Revenue Tooltip",
            DataVisualCount = 1,
            InternalStorySpecialPageAssessment = new StorySpecialPageAssessment
            {
                PageType = StorySpecialPageType.Tooltip,
                Confidence = StorySpecialPageConfidence.High,
                Reason = "Tooltip cue detected.",
                PromotionState = StoryAssessmentPromotionState.Internal,
                SurfaceScope = StoryAssessmentSurfaceScope.PbirSpecific,
                TreatAsPrimaryNarrativePage = false,
                SuppressNormalStoryGaps = false,
                SuppressGenericArchetypePromotion = true,
                EvidenceReferences =
                [
                    new StorySpecialPageEvidenceReference
                    {
                        SourceType = "pageName",
                        ReferenceId = "Revenue Tooltip",
                        Summary = "Tooltip page naming cue",
                    },
                ],
            },
        };
    }
}
