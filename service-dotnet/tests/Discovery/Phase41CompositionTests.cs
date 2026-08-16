using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase41CompositionTests
{
    [Theory]
    [InlineData("executiveSummary")]
    [InlineData("overview")]
    [InlineData("detail")]
    [InlineData("comparison")]
    public void PageTemplateCatalog_ContainsApprovedTemplate(string templateName)
    {
        var template = Phase41PageTemplateCatalog.Get(templateName);

        Assert.Equal(templateName, template.Name);
        Assert.NotEmpty(template.Sections);
        Assert.All(template.Sections, section => Assert.NotEmpty(section.Slots));
    }

    [Fact]
    public void VisualDescriptorCatalog_ContainsSlicer()
    {
        var descriptor = Phase41VisualDescriptorCatalog.Get("slicer");

        Assert.Equal("slicer", descriptor.VisualType);
        Assert.Contains(LocalPbirGenerationBindingRole.Category, descriptor.SupportedBindingRoles);
        Assert.False(descriptor.SupportsTooltip);
    }

    [Fact]
    public void CompositionProjection_AssignsTemplateSlotsDeterministically()
    {
        var page = new LocalPbirGenerationPage("summary", "Executive Summary", 0,
            new LocalPbirGenerationPageAuthoring());
        var visuals = new[]
        {
            new LocalPbirGenerationVisual("revenue", "summary", "card", 0, null, [
                new("revenue-binding", "Revenue", LocalPbirGenerationBindingKind.Measure,
                    LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")]),
            new LocalPbirGenerationVisual("region", "summary", "slicer", 1, null, [
                new("region-binding", "Region", LocalPbirGenerationBindingKind.Dimension,
                    LocalPbirGenerationBindingRole.Category, "Sales", "Region")]),
            new LocalPbirGenerationVisual("analysis", "summary", "lineChart", 2, null, [
                new("analysis-category", "Region", LocalPbirGenerationBindingKind.Dimension,
                    LocalPbirGenerationBindingRole.Category, "Sales", "Region"),
                new("analysis-value", "Revenue", LocalPbirGenerationBindingKind.Measure,
                    LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")])
        };
        var composition = new LocalPbirGenerationPageComposition(
            "executiveSummary",
            [new("Kpi1", "revenue"), new("RegionSlicer", "region"), new("PrimaryChart", "analysis")],
            null,
            null);

        var result = Phase41CompositionProjection.Resolve(page, visuals, composition);

        Assert.Equal(new LocalPbirGenerationVisualLayout(48, 96, 336, 160), result.VisualLayouts["revenue"]);
        Assert.Equal(new LocalPbirGenerationVisualLayout(1032, 96, 192, 160), result.VisualLayouts["region"]);
    }

    [Fact]
    public void CompositionValidation_RejectsDuplicateSlotAssignments()
    {
        var page = new LocalPbirGenerationPage("summary", "Summary", 0);
        var visuals = new[]
        {
            new LocalPbirGenerationVisual("one", "summary", "card", 0, null, [
                new("binding-one", "Revenue", LocalPbirGenerationBindingKind.Measure,
                    LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")]),
            new LocalPbirGenerationVisual("two", "summary", "card", 1, null, [
                new("binding-two", "Margin", LocalPbirGenerationBindingKind.Measure,
                    LocalPbirGenerationBindingRole.Value, "Sales", "Margin")])
        };
        var composition = new LocalPbirGenerationPageComposition(
            "executiveSummary",
            [new("Kpi1", "one"), new("Kpi1", "two")],
            null,
            null);

        var diagnostics = Phase41CompositionValidation.Validate(page, visuals, composition);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "PBIR41-COMPOSITION-DUPLICATE-SLOT-001");
    }

    [Fact]
    public void CompositionValidation_RejectsNavigationToUnknownPage()
    {
        var page = new LocalPbirGenerationPage("summary", "Summary", 0);
        var composition = new LocalPbirGenerationPageComposition(
            "overview",
            [],
            new LocalPbirGenerationNavigationDefinition("main", [
                new("detail", LocalPbirGenerationNavigationTargetKind.Page, "missing")]),
            null);

        var diagnostics = Phase41CompositionValidation.Validate(page, [], composition, [page]);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "PBIR41-NAVIGATION-TARGET-001");
    }

    [Fact]
    public void CompositionValidation_RejectsSlicerWithMeasureBinding()
    {
        var page = new LocalPbirGenerationPage("summary", "Summary", 0);
        var visuals = new[]
        {
            new LocalPbirGenerationVisual("filter", "summary", "slicer", 0, null, [
                new("binding", "Revenue", LocalPbirGenerationBindingKind.Measure,
                    LocalPbirGenerationBindingRole.Category, "Sales", "Revenue")])
        };
        var composition = new LocalPbirGenerationPageComposition("overview", [], null, null);

        var diagnostics = Phase41CompositionValidation.Validate(page, visuals, composition);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "PBIR41-SLICER-BINDING-001");
    }

    [Fact]
    public void CompositionValidation_RejectsSlicerInteractionToUnknownVisual()
    {
        var page = new LocalPbirGenerationPage("summary", "Summary", 0);
        var visuals = new[]
        {
            new LocalPbirGenerationVisual("filter", "summary", "slicer", 0, null, [
                new("binding", "Region", LocalPbirGenerationBindingKind.Dimension,
                    LocalPbirGenerationBindingRole.Category, "Sales", "Region")]),
            new LocalPbirGenerationVisual("chart", "summary", "lineChart", 1, null, [
                new("category", "Region", LocalPbirGenerationBindingKind.Dimension,
                    LocalPbirGenerationBindingRole.Category, "Sales", "Region"),
                new("value", "Revenue", LocalPbirGenerationBindingKind.Measure,
                    LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")])
        };
        var composition = new LocalPbirGenerationPageComposition(
            "overview",
            [new("PrimaryChart", "chart")],
            null,
            new("filter", LocalPbirGenerationAxisOrientation.Vertical, Interaction: new(["missing-visual"])));

        var diagnostics = Phase41CompositionValidation.Validate(page, visuals, composition);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "PBIR41-SLICER-INTERACTION-001");
    }
}
