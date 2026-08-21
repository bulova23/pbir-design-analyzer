using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Characterization;

/// <summary>
/// Golden characterization tests freeze representative scoring outputs before structural refactoring.
/// Golden changes require an intentional review; this test never rewrites expected results.
/// </summary>
public sealed class ScoringCharacterizationTests
{
    [Fact]
    public async Task MinimalReport_MatchesGoldenBehavior()
    {
        var fixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Characterization", "MinimalReport.Report");
        var goldenPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Characterization", "characterization-minimal.golden.json");
        var service = new PbirScoringService(
            new PbirProjectService(NullLogger<PbirProjectService>.Instance),
            NullLogger<PbirScoringService>.Instance);

        var result = await service.ScoreAsync(fixtureRoot);
        var actual = CreateSnapshot(result);
        var expected = await File.ReadAllTextAsync(goldenPath);

        var actualJson = JsonSerializer.Serialize(actual, new JsonSerializerOptions { WriteIndented = true });
        Assert.Equal(JsonDocument.Parse(expected).RootElement.GetRawText(), actualJson);
    }

    private static object CreateSnapshot(ScoreResult result)
    {
        var scores = new SortedDictionary<string, double>
        {
            ["accessibility"] = result.AccessibilityScore,
            ["cognitiveLoad"] = result.CognitiveLoadScore,
            ["composite"] = result.CompositeScore,
            ["dataInk"] = result.DataInkScore,
            ["density"] = result.DensityScore,
            ["enterpriseGovernance"] = result.EnterpriseGovernanceScore,
            ["gestalt"] = result.GestaltScore,
            ["graphicalPerception"] = result.GraphicalPerceptionScore,
            ["narrative"] = result.NarrativeScore,
            ["stephenFew"] = result.StephenFewScore,
            ["tufte"] = result.TufteScore,
            ["visualBestPractices"] = result.VisualBestPracticesScore,
        };

        var feedback = result.Feedback
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SelectMany(pair => pair.Value.Select(item => new
            {
                framework = pair.Key,
                ok = item.Ok,
                text = item.Text,
                findingType = item.FindingType,
                affectedVisuals = item.AffectedVisuals,
                evidence = item.Text,
            }))
            .ToArray();

        var snapshotWithoutFingerprint = new
        {
            fixtureId = "characterization-minimal",
            schemaVersion = "score-characterization/v1",
            pageCount = result.PageCount,
            dataVisualCount = result.DataVisualCount,
            navigationVisualCount = result.NavigationVisualCount,
            hiddenVisualCount = result.HiddenVisualCount,
            compositeScore = result.CompositeScore,
            scores,
            feedback,
            recommendations = result.Recommendations,
            diagnostics = result.ScoringErrors ?? new Dictionary<string, string>(),
            evidence = result.InferredStorySummary?.Evidence ?? [],
            readiness = result.ActionabilityBreakdown?.Score,
        };
        var canonical = JsonSerializer.Serialize(snapshotWithoutFingerprint);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();

        return new
        {
            snapshotWithoutFingerprint.fixtureId,
            snapshotWithoutFingerprint.schemaVersion,
            snapshotWithoutFingerprint.pageCount,
            snapshotWithoutFingerprint.dataVisualCount,
            snapshotWithoutFingerprint.navigationVisualCount,
            snapshotWithoutFingerprint.hiddenVisualCount,
            snapshotWithoutFingerprint.compositeScore,
            snapshotWithoutFingerprint.scores,
            snapshotWithoutFingerprint.feedback,
            snapshotWithoutFingerprint.recommendations,
            snapshotWithoutFingerprint.diagnostics,
            snapshotWithoutFingerprint.evidence,
            snapshotWithoutFingerprint.readiness,
            deterministicFingerprint = fingerprint,
        };
    }
}
