using System.Text.Json.Nodes;
using PowerBIModelingService;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir.Scoring;
using Xunit;

namespace ServiceDotnet.Tests.Services.Scoring;

public sealed class ReportAnalysisContextTests
{
    [Fact]
    public void ContextCopiesCollectionsAndJsonInput()
    {
        var location = new PbirReportLocation("/project", "/project/report.Report", "/project/report.Report/definition", "/project/report.Report/definition/report.json", "/project");
        var reportJson = new JsonObject { ["name"] = "before" };
        var pages = new List<PageData> { CreatePage("Page A") };
        var filters = new List<FilterDefinitionData>();
        var colors = new List<string> { "#FFFFFF" };
        var weights = new Dictionary<string, double> { ["accessibility"] = 15 };

        var context = new ReportAnalysisContext(location, reportJson, pages, filters, colors, weights, new NavigationScoringSettings(true, 25, 8, 5));
        reportJson["name"] = "after";
        pages.Add(CreatePage("Page B"));
        colors.Add("#000000");
        weights["accessibility"] = 99;

        Assert.Single(context.Pages);
        Assert.Single(context.ThemeColors);
        Assert.Equal(15, context.FrameworkWeights["accessibility"]);
        Assert.Equal("before", context.CreateReportJsonSnapshot()["name"]?.GetValue<string>());
    }

    [Fact]
    public void ContextPreservesSourcePageOrder()
    {
        var location = new PbirReportLocation("/project", "/project/report.Report", "/project/report.Report/definition", "/project/report.Report/definition/report.json", "/project");
        var pages = new[] { CreatePage("Second"), CreatePage("First") };

        var context = new ReportAnalysisContext(location, new JsonObject(), pages, [], [], new Dictionary<string, double>(), new NavigationScoringSettings(true, 25, 8, 5));

        Assert.Equal(["Second", "First"], context.Pages.Select(page => page.Name));
    }

    private static PageData CreatePage(string name) => new() { Name = name, DisplayName = name };
}
