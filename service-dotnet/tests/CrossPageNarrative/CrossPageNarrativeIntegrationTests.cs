using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.CrossPageNarrative;

public sealed class CrossPageNarrativeIntegrationTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    [Fact(DisplayName = "Report scoring generates internal Cross-Page Narrative assessment without widening the public payload")]
    public async Task ScoreAsync_ReportMode_GeneratesInternalCrossPageNarrativeAssessment()
    {
        var reportRoot = CreateTempPbirReport(
            ("overview", "Overview",
                """
                {"name":"overview","displayName":"Overview","visuals":[
                  {"id":"t1","type":"textbox","x":0,"y":0,"width":520,"height":40,
                   "textbox":{"visible":true,"text":"Revenue Overview"}},
                  {"id":"v1","type":"barChart","x":0,"y":140,"width":520,"height":260,
                   "title":{"visible":true,"text":"Revenue by Region"},
                   "fieldRoles":{"category":["Region"],"measure":["Revenue"]}}
                ]}
                """),
            ("detail", "Region Detail",
                """
                {"name":"detail","displayName":"Region Detail","visuals":[
                  {"id":"v1","type":"tableEx","x":0,"y":120,"width":520,"height":260,
                   "title":{"visible":true,"text":"Region Revenue Detail"},
                   "fieldRoles":{"category":["Region"],"measure":["Revenue"]}}
                ]}
                """));
        var service = BuildScoringService();

        var result = await service.ScoreAsync(reportRoot);
        var publicPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var internalAssessment = typeof(ScoreResult).GetProperty(
            "InternalCrossPageNarrativeAssessment",
            BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(result);

        Assert.DoesNotContain("InternalCrossPageNarrativeAssessment", publicPropertyNames);
        Assert.NotNull(internalAssessment);
        Assert.Equal("CrossPageNarrativeAssessment", internalAssessment!.GetType().Name);
    }

    [Fact(DisplayName = "Single-page scoring skips Cross-Page Narrative generation")]
    public async Task ScoreAsync_SinglePageMode_SkipsCrossPageNarrativeAssessment()
    {
        var reportRoot = CreateTempPbirReport(
            ("overview", "Overview",
                """
                {"name":"overview","displayName":"Overview","visuals":[
                  {"id":"v1","type":"barChart","x":0,"y":120,"width":520,"height":260,
                   "title":{"visible":true,"text":"Revenue by Region"},
                   "fieldRoles":{"category":["Region"],"measure":["Revenue"]}}
                ]}
                """));
        var service = BuildScoringService();

        var result = await service.ScoreAsync(reportRoot, pageName: "overview");
        var internalAssessment = typeof(ScoreResult).GetProperty(
            "InternalCrossPageNarrativeAssessment",
            BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(result);

        Assert.Null(internalAssessment);
    }

    private PbirScoringService BuildScoringService()
    {
        return new PbirScoringService(
            new PbirProjectService(NullLogger<PbirProjectService>.Instance),
            NullLogger<PbirScoringService>.Instance);
    }

    private string CreateTempPbirReport(params (string PageId, string DisplayName, string PageJson)[] pages)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "pbir-cross-page-" + Guid.NewGuid().ToString("N"));
        var reportRoot = Path.Combine(tmp, "TestReport.Report");
        var definitionDir = Path.Combine(reportRoot, "definition");
        var pagesRoot = Path.Combine(definitionDir, "pages");
        Directory.CreateDirectory(pagesRoot);
        _tempDirs.Add(tmp);

        var pageOrder = string.Join(",", pages.Select(page => $"\"{page.PageId}\""));
        File.WriteAllText(
            Path.Combine(definitionDir, "report.json"),
            """{"id":"test","name":"TestReport","theme":{"name":"CY24SU10"}}""");
        File.WriteAllText(
            Path.Combine(pagesRoot, "pages.json"),
            $$"""{"pageOrder":[{{pageOrder}}]}""");

        foreach (var page in pages)
        {
            var pageDir = Path.Combine(pagesRoot, page.PageId);
            Directory.CreateDirectory(pageDir);
            File.WriteAllText(Path.Combine(pageDir, "page.json"), page.PageJson);
        }

        return tmp;
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }
}
