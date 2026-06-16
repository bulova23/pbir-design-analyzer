using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class ThemeResolutionServiceTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    [Fact]
    public void ResolveThemeColors_LocalThemeFile_ReturnsOnlyValidHexColors()
    {
        var projectRoot = CreateTempProject();
        var location = ResolveLocation(projectRoot);
        var service = CreateThemeResolutionService();
        var method = service.GetType().GetMethod("ResolveThemeColors", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var reportJson = JsonNode.Parse(File.ReadAllText(location.ReportJsonPath))!.AsObject();
        var colors = method!.Invoke(service, [reportJson, location]);

        var palette = Assert.IsAssignableFrom<IEnumerable<string>>(colors);
        Assert.Equal(["#112233", "#445566", "#778899"], palette.ToList());
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

    private object CreateThemeResolutionService()
    {
        var assembly = typeof(PbirScoringService).Assembly;
        var type = assembly.GetType("PowerBIModelingService.Services.Pbir.ThemeResolutionService");
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
        var tempRoot = Path.Combine(Path.GetTempPath(), "pbir-theme-resolution-" + Guid.NewGuid().ToString("N"));
        var definitionRoot = Path.Combine(tempRoot, "TestReport.Report", "definition");
        var themeRoot = Path.Combine(tempRoot, "themes");
        Directory.CreateDirectory(definitionRoot);
        Directory.CreateDirectory(themeRoot);
        _tempDirs.Add(tempRoot);

        File.WriteAllText(
            Path.Combine(definitionRoot, "report.json"),
            """
            {
              "id": "test",
              "name": "TestReport",
              "theme": { "href": "themes/customTheme.json" }
            }
            """);

        File.WriteAllText(
            Path.Combine(themeRoot, "customTheme.json"),
            """
            {
              "dataColors": ["#112233", "#445566", "invalid", "#778899"]
            }
            """);

        return tempRoot;
    }
}
