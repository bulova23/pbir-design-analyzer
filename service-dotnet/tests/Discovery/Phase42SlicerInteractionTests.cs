using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase42SlicerInteractionTests
{
    [Fact]
    public void V7Contract_UsesPinnedSchemaVersionAndTypedRule()
    {
        Assert.Equal("local-pbir-generation-request/v7", LocalPbirGenerationRequestContract.SchemaVersionV7);

        var rule = new LocalPbirGenerationSlicerInteractionRule(
            "region-to-chart",
            "region",
            ["chart"],
            LocalPbirGenerationSlicerInteractionMode.DataFilter);

        Assert.Equal("region", rule.SourceVisualId);
        Assert.Equal(LocalPbirGenerationSlicerInteractionMode.DataFilter, rule.Mode);
    }

    [Fact]
    public void Validation_AcceptsSamePageSlicerTargetsAndNormalizesDeterministically()
    {
        var visuals = new[]
        {
            Slicer("region", "summary"),
            Visual("chart", "summary", "lineChart"),
            Visual("table", "summary", "table")
        };
        var rules = new[]
        {
            new LocalPbirGenerationSlicerInteractionRule("region-to-table", "region", ["table"], LocalPbirGenerationSlicerInteractionMode.HighlightFilter),
            new LocalPbirGenerationSlicerInteractionRule("region-to-chart", "region", ["chart"], LocalPbirGenerationSlicerInteractionMode.DataFilter)
        };

        var result = Phase42InteractionValidation.Validate("summary", visuals, rules);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { "region-to-chart", "region-to-table" }, result.Rules.Select(rule => rule.InteractionId).ToArray());
    }

    [Theory]
    [InlineData("missing-source")]
    [InlineData("missing-target")]
    [InlineData("cross-page")]
    [InlineData("self-reference")]
    public void Validation_RejectsInvalidReferences(string scenario)
    {
        var visuals = new[] { Slicer("region", "summary"), Visual("chart", "summary", "lineChart") };
        var rule = scenario switch
        {
            "missing-source" => new LocalPbirGenerationSlicerInteractionRule("r", "missing", ["chart"], LocalPbirGenerationSlicerInteractionMode.DataFilter),
            "missing-target" => new LocalPbirGenerationSlicerInteractionRule("r", "region", ["missing"], LocalPbirGenerationSlicerInteractionMode.DataFilter),
            "cross-page" => new LocalPbirGenerationSlicerInteractionRule("r", "region", ["other-page-chart"], LocalPbirGenerationSlicerInteractionMode.DataFilter),
            _ => new LocalPbirGenerationSlicerInteractionRule("r", "region", ["region"], LocalPbirGenerationSlicerInteractionMode.DataFilter)
        };

        var result = Phase42InteractionValidation.Validate("summary", visuals, [rule], ["other-page-chart"]);

        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Validation_RejectsDuplicateRulesAndUnsupportedSource()
    {
        var visuals = new[] { Slicer("region", "summary"), Visual("chart", "summary", "lineChart") };
        var rules = new[]
        {
            new LocalPbirGenerationSlicerInteractionRule("same", "chart", ["region"], LocalPbirGenerationSlicerInteractionMode.DataFilter),
            new LocalPbirGenerationSlicerInteractionRule("same", "region", ["chart"], LocalPbirGenerationSlicerInteractionMode.DataFilter)
        };

        var result = Phase42InteractionValidation.Validate("summary", visuals, rules);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "PBIR42-INTERACTION-DUPLICATE-ID-001");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "PBIR42-INTERACTION-SOURCE-001");
    }

    [Fact]
    public void Provider_GeneratesPinnedVisualInteractionsDeterministically()
    {
        var request = CreateRequest();
        var first = new LocalPbirGenerationProviderService().Generate(request);
        var second = new LocalPbirGenerationProviderService().Generate(request);

        Assert.True(first.Readiness == LocalPbirGenerationReadinessState.Generated,
            string.Join(" | ", first.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Field}:{diagnostic.Message}")));
        var page = first.Artifact!.Files.Single(file => file.RelativePath.EndsWith("/page.json", StringComparison.Ordinal)).Content;
        Assert.Contains("\"source\": \"region\"", page, StringComparison.Ordinal);
        Assert.Contains("\"target\": \"chart\"", page, StringComparison.Ordinal);
        Assert.Contains("\"type\": \"DataFilter\"", page, StringComparison.Ordinal);
        Assert.Equal(first.Artifact.Files.Select(file => (file.RelativePath, file.Content)), second.Artifact!.Files.Select(file => (file.RelativePath, file.Content)));
        Assert.Equal(first.Manifest!.Hashes.ArtifactHash, second.Manifest!.Hashes.ArtifactHash);
    }

    [Fact]
    public async Task Provider_RoundTripsExplicitInteractionsThroughAnalyzer()
    {
        var output = Directory.CreateTempSubdirectory("pbir-phase42-");
        try
        {
            var result = await new LocalPbirGenerationProviderService().GenerateAndVerifyAsync(CreateRequest(output.FullName));

            Assert.True(result.Readiness == LocalPbirGenerationReadinessState.RoundTripVerified,
                string.Join(" | ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Field}:{diagnostic.Message}")));
            Assert.Equal(1, result.RoundTrip!.PageCount);
            Assert.Equal(2, result.RoundTrip.VisualCount);
        }
        finally
        {
            output.Delete(recursive: true);
        }
    }

    private static LocalPbirGenerationVisual Slicer(string id, string pageId) => new(id, pageId, "slicer", 0, null,
        [new("binding", "Region", LocalPbirGenerationBindingKind.Dimension, LocalPbirGenerationBindingRole.Category, "Sales", "Region")]);

    private static LocalPbirGenerationVisual Visual(string id, string pageId, string type) => new(id, pageId, type, 1, null,
        [new("binding", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")]);

    private static LocalPbirGenerationRequestV7 CreateRequest(string? outputBase = null) => new(
        LocalPbirGenerationRequestContract.SchemaVersionV7,
        "phase42-interactions",
        "Phase42",
        "Sales.SemanticModel",
        new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc),
        outputBase ?? Path.GetTempPath(),
        "phase42-output",
        [new("summary", "Summary", 0)],
        [
            Slicer("region", "summary"),
            new("chart", "summary", "lineChart", 1, null, [
                new("category", "Region", LocalPbirGenerationBindingKind.Dimension, LocalPbirGenerationBindingRole.Category, "Sales", "Region"),
                new("value", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")])
        ],
        Compositions: [new(
            "overview",
            [new("PrimaryChart", "chart"), new("Filter1", "region")],
            null,
            null,
            "summary",
            [new("region-to-chart", "region", ["chart"], LocalPbirGenerationSlicerInteractionMode.DataFilter)])]);
}
