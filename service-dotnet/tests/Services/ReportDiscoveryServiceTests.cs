using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class ReportDiscoveryServiceTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    [Fact]
    public void ResolveRequiredReportLocation_BlankPath_ThrowsArgumentException()
    {
        var service = CreateDiscoveryService();
        var method = service.GetType().GetMethod("ResolveRequiredReportLocation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var ex = Assert.Throws<TargetInvocationException>(() => method!.Invoke(service, [" "]));
        var inner = Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Equal("Parameter 'reportPath' is required. (Parameter 'reportPath')", inner.Message);
    }

    [Fact]
    public void ResolveRequiredReportLocation_ProjectRootWithReport_ReturnsResolvedLocation()
    {
        var projectRoot = CreateTempProject();
        var service = CreateDiscoveryService();
        var method = service.GetType().GetMethod("ResolveRequiredReportLocation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var location = method!.Invoke(service, [projectRoot]);

        var typedLocation = Assert.IsType<PbirReportLocation>(location);
        Assert.Equal(projectRoot, typedLocation.ProjectRootPath);
        Assert.EndsWith("TestReport.Report", typedLocation.ReportRootPath, StringComparison.Ordinal);
        Assert.EndsWith(Path.Combine("definition", "report.json"), typedLocation.ReportJsonPath, StringComparison.Ordinal);
        Assert.Equal("TestReport", typedLocation.ReportName);
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

    private object CreateDiscoveryService()
    {
        var assembly = typeof(PbirScoringService).Assembly;
        var type = assembly.GetType("PowerBIModelingService.Services.Pbir.ReportDiscoveryService");
        Assert.NotNull(type);

        var projectService = new PbirProjectService(NullLogger<PbirProjectService>.Instance);
        var logger = NullLogger.Instance;
        return Activator.CreateInstance(type!, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [projectService, logger], null)!;
    }

    private string CreateTempProject()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "pbir-report-discovery-" + Guid.NewGuid().ToString("N"));
        var reportDefinitionDir = Path.Combine(tempRoot, "TestReport.Report", "definition");
        Directory.CreateDirectory(reportDefinitionDir);
        File.WriteAllText(
            Path.Combine(reportDefinitionDir, "report.json"),
            """{"id":"test","name":"TestReport","theme":{"name":"CY24SU10"}}""");
        _tempDirs.Add(tempRoot);
        return tempRoot;
    }
}
