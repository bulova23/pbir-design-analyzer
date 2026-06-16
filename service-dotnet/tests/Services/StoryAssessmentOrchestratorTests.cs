using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class StoryAssessmentOrchestratorTests
{
    [Fact]
    public void Assess_DetectsTooltipAndSuppressesPrimaryNarrativePosture()
    {
        var orchestrator = new StoryAssessmentOrchestrator();
        var page = new PageData
        {
            Name = "Page1",
            DisplayName = "Net Sales Tooltip",
            Visuals =
            [
                CreateTextVisual("t1", "Tooltip details for Net Sales"),
                CreateCardVisual("v1", "Tooltip Revenue"),
                CreateChartVisual(
                    "v2",
                    "barChart",
                    "Tooltip breakdown",
                    categoryHints: ["Region"],
                    measureHints: ["Revenue Tooltip"]),
            ],
        };

        var assessment = orchestrator.Assess(page, []);

        Assert.Equal(StorySpecialPageType.Tooltip, assessment.SpecialPageAssessment.PageType);
        Assert.False(assessment.SpecialPageAssessment.TreatAsPrimaryNarrativePage);
        Assert.True(assessment.SpecialPageAssessment.SuppressGenericArchetypePromotion);
        Assert.NotNull(assessment.ArchetypeClassification);
        Assert.True(assessment.ArchetypeClassification!.SuppressedBySpecialPageType);
        Assert.NotEmpty(assessment.SpecialPageAssessment.EvidenceReferences);
    }

    [Fact]
    public void Assess_GeneratesConfidenceBreakdownFromAlignedSignals()
    {
        var orchestrator = new StoryAssessmentOrchestrator();
        var page = new PageData
        {
            Name = "Page1",
            DisplayName = "Revenue Performance Overview",
            PageFilters =
            [
                new FilterDefinitionData(
                    "page-filter-1",
                    StoryFilterScope.Page,
                    "Business Unit",
                    ["Business Unit"],
                    null,
                    0,
                    "categorical",
                    null,
                    false),
            ],
            Visuals =
            [
                CreateTextVisual("t1", "Revenue Performance Overview"),
                CreateSlicerVisual("s1", "Date", categoryHints: ["Date Hierarchy"]),
                CreateChartVisual(
                    "v1",
                    "lineChart",
                    "Revenue Trend vs Target",
                    categoryHints: ["Revenue Month"],
                    measureHints: ["Revenue"]),
                CreateChartVisual(
                    "v2",
                    "barChart",
                    "Revenue by Region",
                    categoryHints: ["Revenue Region"],
                    measureHints: ["Revenue"]),
            ],
        };
        var reportFilters = new List<FilterDefinitionData>
        {
            new(
                "report-filter-1",
                StoryFilterScope.Report,
                "Scenario",
                ["Scenario"],
                null,
                0,
                "categorical",
                null,
                false),
        };

        var assessment = orchestrator.Assess(page, reportFilters);
        var accuracy = Assert.Single(
            assessment.ConfidenceBreakdownAssessment.Dimensions
                .Where(dimension => dimension.DimensionId == StoryConfidenceBreakdownDimension.Accuracy));

        Assert.Equal(4, assessment.ConfidenceBreakdownAssessment.Dimensions.Count);
        Assert.Equal(1, assessment.FilterTopologyAssessment.PageFilterCount);
        Assert.Equal(1, assessment.FilterTopologyAssessment.ReportFilterCount);
        Assert.NotEmpty(accuracy.ConfidenceDrivers);
        Assert.Contains(
            assessment.ConfidenceBreakdownAssessment.Dimensions,
            dimension => dimension.DimensionId == StoryConfidenceBreakdownDimension.Explainability &&
                         dimension.EvidenceReferences.Count > 0);
    }

    private static VisualData CreateTextVisual(string id, string text)
    {
        return new VisualData
        {
            Id = id,
            Type = "textbox",
            X = 0,
            Y = 0,
            W = 420,
            H = 40,
            Text = new VisualTextMetadata(null, null, text),
        };
    }

    private static VisualData CreateCardVisual(string id, string title)
    {
        return new VisualData
        {
            Id = id,
            Type = "card",
            X = 0,
            Y = 80,
            W = 220,
            H = 120,
            Text = new VisualTextMetadata(title, null, null),
        };
    }

    private static VisualData CreateSlicerVisual(string id, string title, IReadOnlyList<string> categoryHints)
    {
        return new VisualData
        {
            Id = id,
            Type = "slicer",
            X = 0,
            Y = 60,
            W = 220,
            H = 110,
            Text = new VisualTextMetadata(title, null, null),
            FieldRoles = new VisualFieldRoleMetadata(categoryHints, [], [], []),
            Filter = new FilterTopologyMetadata(categoryHints, "Year > Quarter > Month", 3, "categorical"),
        };
    }

    private static VisualData CreateChartVisual(
        string id,
        string type,
        string title,
        IReadOnlyList<string> categoryHints,
        IReadOnlyList<string> measureHints)
    {
        return new VisualData
        {
            Id = id,
            Type = type,
            X = 0,
            Y = 120,
            W = 520,
            H = 220,
            Text = new VisualTextMetadata(title, null, null),
            FieldRoles = new VisualFieldRoleMetadata(categoryHints, [], [], measureHints),
        };
    }
}
