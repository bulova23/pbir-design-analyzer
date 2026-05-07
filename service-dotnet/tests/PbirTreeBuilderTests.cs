using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using Xunit;

namespace PowerBIModelingService.Tests;

public sealed class PbirTreeBuilderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _projectRoot;

    public PbirTreeBuilderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pbir-tree-" + Guid.NewGuid().ToString("N"));
        _projectRoot = Path.Combine(_tempDir, "workspace", "PBITesting");
        var reportRoot = Path.Combine(_projectRoot, "Sales & Production.Report");
        var definitionRoot = Path.Combine(reportRoot, "definition");
        var pageRoot = Path.Combine(definitionRoot, "pages", "OverviewPage");
        var visualRoot = Path.Combine(pageRoot, "visuals", "Visual1");
        var themeRoot = Path.Combine(definitionRoot, "themes");

        Directory.CreateDirectory(visualRoot);
        Directory.CreateDirectory(themeRoot);

        File.WriteAllText(Path.Combine(reportRoot, "definition.pbir"), "{}");
        File.WriteAllText(
            Path.Combine(definitionRoot, "report.json"),
            """
            {
              "name": "Sales & Production",
              "theme": {
                "name": "Corporate Theme",
                "href": "themes/corporate.json"
              }
            }
            """);
        File.WriteAllText(
            Path.Combine(definitionRoot, "pages", "pages.json"),
            """
            {
              "pageOrder": ["OverviewPage"]
            }
            """);
        File.WriteAllText(
            Path.Combine(pageRoot, "page.json"),
            """
            {
              "name": "OverviewPage",
              "displayName": "Overview"
            }
            """);
        File.WriteAllText(
            Path.Combine(visualRoot, "visual.json"),
            """
            {
              "name": "Sales by Region",
              "visual": {
                "visualType": "barChart"
              }
            }
            """);
        File.WriteAllText(Path.Combine(themeRoot, "corporate.json"), "{}");
    }

    [Fact]
    public void BuildTree_ReportDefinition_ReturnsPagesAndVisuals()
    {
        var projectService = new PbirProjectService(NullLogger<PbirProjectService>.Instance);
        var location = projectService.TryGetReportLocation(_projectRoot);
        Assert.NotNull(location);

        var treeBuilder = new PbirTreeBuilder(NullLogger<PbirTreeBuilder>.Instance);
        var tree = treeBuilder.BuildTree(location!);

        Assert.NotNull(tree);
        Assert.True(tree.ContainsKey("pages"));

        var pagesJson = JsonSerializer.Serialize(tree["pages"]);
        var pages = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(pagesJson);
        Assert.NotNull(pages);
        Assert.Single(pages);

        var firstPage = pages[0];
        Assert.True(firstPage.ContainsKey("visuals"));

        var visualsJson = firstPage["visuals"].GetRawText();
        var visuals = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(visualsJson);
        Assert.NotNull(visuals);
        Assert.Single(visuals);
    }

    [Fact]
    public void BuildTree_ReportDefinition_UsesWorkspaceRelativePaths()
    {
        var projectService = new PbirProjectService(NullLogger<PbirProjectService>.Instance);
        var location = projectService.TryGetReportLocation(_projectRoot);
        Assert.NotNull(location);

        var treeBuilder = new PbirTreeBuilder(NullLogger<PbirTreeBuilder>.Instance);
        var tree = treeBuilder.BuildTree(location!);

        var reportPath = tree["path"].ToString() ?? string.Empty;
        Assert.False(Path.IsPathRooted(reportPath));
        Assert.StartsWith("Sales & Production.Report/", reportPath.Replace(Path.DirectorySeparatorChar, '/'));

        var pagesJson = JsonSerializer.Serialize(tree["pages"]);
        var pages = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(pagesJson);
        Assert.NotNull(pages);
        Assert.Single(pages);

        var pagePath = pages[0]["path"].GetString() ?? string.Empty;
        Assert.False(Path.IsPathRooted(pagePath));
        Assert.StartsWith("Sales & Production.Report/", pagePath.Replace(Path.DirectorySeparatorChar, '/'));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for temporary fixtures.
        }
    }
}
