using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Characterization;

public sealed class RepresentativeCorpusCharacterizationTests
{
    [Fact]
    public async Task ManifestFixturesAreSafeAndDeterministic()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Characterization");
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "manifest.json")));
        var service = new PbirScoringService(new PbirProjectService(NullLogger<PbirProjectService>.Instance), NullLogger<PbirScoringService>.Instance);

        foreach (var fixture in manifest.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            var id = fixture.GetProperty("id").GetString()!;
            var fixtureRoot = Path.Combine(root, fixture.GetProperty("path").GetString()!);
            Assert.True(Directory.Exists(fixtureRoot), id);
            Assert.NotEqual("TO_BE_FILLED", fixture.GetProperty("inputHash").GetString());
            var first = await service.ScoreAsync(fixtureRoot);
            var second = await service.ScoreAsync(fixtureRoot);
            using var golden = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, fixture.GetProperty("golden").GetString()!)));
            var expected = golden.RootElement;
            Assert.Equal(expected.GetProperty("pageCount").GetInt32(), first.PageCount);
            Assert.Equal(expected.GetProperty("dataVisualCount").GetInt32(), first.DataVisualCount);
            Assert.Equal(expected.GetProperty("navigationVisualCount").GetInt32(), first.NavigationVisualCount);
            Assert.Equal(expected.GetProperty("compositeScore").GetDouble(), first.CompositeScore);
            Assert.Equal(expected.GetProperty("deterministicFingerprint").GetString(), Fingerprint(first));
            Assert.Equal(first.PageCount, second.PageCount);
            Assert.Equal(first.CompositeScore, second.CompositeScore);
            Assert.Equal(Fingerprint(first), Fingerprint(second));
        }
    }

    private static string Fingerprint(ScoreResult result) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
    {
        result.PageCount,
        result.CompositeScore,
        result.DataVisualCount,
        result.NavigationVisualCount,
        result.HiddenVisualCount,
        findings = result.Feedback.SelectMany(pair => pair.Value.Select(item => new { framework = pair.Key, item.Ok, item.FindingType, item.Text })).ToArray(),
        diagnostics = result.ScoringErrors ?? new Dictionary<string, string>(),
        readiness = result.ActionabilityBreakdown?.Score,
    })))).ToLowerInvariant();
}
