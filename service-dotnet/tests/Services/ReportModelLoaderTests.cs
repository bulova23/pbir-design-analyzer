using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class ReportModelLoaderTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    [Fact]
    public void LoadReportModel_LoadsPagesInMetadataOrder_UsesDirectoryVisualFallback_AndExtractsFilters()
    {
        var projectRoot = CreateTempProject();
        var location = ResolveLocation(projectRoot);
        var service = CreateLoaderService();
        var method = service.GetType().GetMethod("LoadReportModel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var model = method!.Invoke(service, [location]);
        Assert.NotNull(model);

        var pages = ReadObjectList(model!, "Pages");
        Assert.Equal(2, pages.Count);
        Assert.Equal("Summary", ReadString(pages[0], "Name"));
        Assert.Equal("Detail", ReadString(pages[1], "Name"));

        var summaryVisuals = ReadObjectList(pages[0], "Visuals");
        Assert.Single(summaryVisuals);
        Assert.Equal("card", ReadString(summaryVisuals[0], "Type"));

        var summaryPageFilters = ReadObjectList(pages[0], "PageFilters");
        Assert.Single(summaryPageFilters);
        Assert.Equal("Region", ReadString(summaryPageFilters[0], "DisplayLabel"));

        var reportFilters = ReadObjectList(model!, "ReportFilters");
        Assert.Single(reportFilters);
        Assert.Equal("Fiscal Year", ReadString(reportFilters[0], "DisplayLabel"));
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
            }
        }
    }

    private object CreateLoaderService()
    {
        var assembly = typeof(PbirScoringService).Assembly;
        var type = assembly.GetType("PowerBIModelingService.Services.Pbir.ReportModelLoader");
        Assert.NotNull(type);

        var logger = NullLogger.Instance;
        return Activator.CreateInstance(type!, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [logger], null)!;
    }

    private static PbirReportLocation ResolveLocation(string projectRoot)
    {
        var projectService = new PbirProjectService(NullLogger<PbirProjectService>.Instance);
        return Assert.IsType<PbirReportLocation>(projectService.TryGetReportLocation(projectRoot));
    }

    private string CreateTempProject()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "pbir-model-loader-" + Guid.NewGuid().ToString("N"));
        var definitionRoot = Path.Combine(tempRoot, "TestReport.Report", "definition");
        var pagesRoot = Path.Combine(definitionRoot, "pages");
        Directory.CreateDirectory(pagesRoot);
        _tempDirs.Add(tempRoot);

        File.WriteAllText(
            Path.Combine(definitionRoot, "report.json"),
            """
            {
              "id": "test",
              "name": "TestReport",
              "theme": { "name": "CY24SU10" },
              "reportFilters": [
                { "field": ["Fiscal Year"], "label": "Fiscal Year" }
              ]
            }
            """);

        File.WriteAllText(
            Path.Combine(pagesRoot, "pages.json"),
            """
            {
              "pageOrder": ["Summary", "Detail"]
            }
            """);

        var summaryDir = Path.Combine(pagesRoot, "Summary");
        Directory.CreateDirectory(summaryDir);
        File.WriteAllText(
            Path.Combine(summaryDir, "page.json"),
            """
            {
              "name": "Summary",
              "displayName": "Summary",
              "pageFilters": [
                { "field": ["Region"], "label": "Region" }
              ]
            }
            """);

        var summaryVisualDir = Path.Combine(summaryDir, "visuals", "summary-card");
        Directory.CreateDirectory(summaryVisualDir);
        File.WriteAllText(
            Path.Combine(summaryVisualDir, "visual.json"),
            """
            {
              "name": "summary-card",
              "position": { "x": 0, "y": 0, "width": 160, "height": 80 },
              "visual": { "visualType": "card" }
            }
            """);

        var detailDir = Path.Combine(pagesRoot, "Detail");
        Directory.CreateDirectory(detailDir);
        File.WriteAllText(
            Path.Combine(detailDir, "page.json"),
            """
            {
              "name": "Detail",
              "displayName": "Detail",
              "visuals": [
                { "id": "detail-chart", "type": "barChart", "x": 0, "y": 0, "width": 320, "height": 220 }
              ]
            }
            """);

        return tempRoot;
    }

    private static List<object> ReadObjectList(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(target);
        Assert.IsAssignableFrom<IEnumerable>(value);
        return ((IEnumerable)value!).Cast<object>().ToList();
    }

    private static string ReadString(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(target)?.ToString() ?? string.Empty;
    }
}
