using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirRoundTripFidelityServiceTests
{
    [Fact]
    public void Compare_ClassifiesByteSemanticExpectedAndUnexpectedDifferences()
    {
        var source = new Dictionary<string, string>
        {
            ["same.json"] = "{\"b\":2,\"a\":1}",
            ["semantic.json"] = "{\"a\":1,\"b\":2}",
            ["changed.json"] = "{\"value\":1}",
            ["unexpected.json"] = "{\"value\":1}"
        };
        var output = new Dictionary<string, string>
        {
            ["same.json"] = source["same.json"],
            ["semantic.json"] = "{\"b\":2,\"a\":1}",
            ["changed.json"] = "{\"value\":2}",
            ["unexpected.json"] = "{\"value\":2}"
        };

        var result = new PbirRoundTripFidelityService().Compare(source, output, new HashSet<string> { "changed.json" });

        Assert.Equal(PbirFidelityClassification.ByteIdentical, result.Files.Single(file => file.RelativePath == "same.json").Classification);
        Assert.Equal(PbirFidelityClassification.SemanticallyIdentical, result.Files.Single(file => file.RelativePath == "semantic.json").Classification);
        Assert.Equal(PbirFidelityClassification.ExpectedNormalizedDifference, result.Files.Single(file => file.RelativePath == "changed.json").Classification);
        Assert.Equal(PbirFidelityClassification.UnexpectedDifference, result.Files.Single(file => file.RelativePath == "unexpected.json").Classification);
        Assert.Contains("same.json", result.AuthoringIdentical);
        Assert.Contains("semantic.json", result.SemanticEquivalent);
        Assert.Contains("changed.json", result.IntentionallyChanged);
        Assert.Contains("unexpected.json", result.UnexpectedPaths);
        Assert.False(result.IsFidelityReady);
    }

    [Fact]
    public void AuthoringCompare_SeparatesNoOpPreservationFromExpectedMutation()
    {
        using var source = JsonDocument.Parse("{\"$schema\":\"schema\",\"position\":{\"x\":1},\"future\":true}");
        var item = new PbirAuthoringEnvelopeItem(
            PbirAuthoringOwnerKind.Visual, "visual-1", "visual.json", "schema", "1.0.0",
            PbirAuthoringPreservationClass.TypedSupported, source.RootElement.Clone(), source.RootElement.GetRawText(), "hash",
            ["$schema", "position", "future"]);
        var envelope = new PbirAuthoringEnvelope(PbirAuthoringEnvelopeContract.SchemaVersionV1, [item], "definition");
        var unchanged = new PbirResolvedAuthoringRepresentation(
            [new(PbirAuthoringOwnerKind.Visual, "visual-1", "visual.json", item.SourceContent!, item.SourceHash)], []);

        var noOp = new PbirAuthoringFidelityService().Compare(envelope, unchanged);

        Assert.True(noOp.IsFidelityReady);
        Assert.Contains("visual.json", noOp.AuthoringIdentical);

        var changed = new PbirResolvedAuthoringRepresentation(
            [new(PbirAuthoringOwnerKind.Visual, "visual-1", "visual.json", "{\"$schema\":\"schema\",\"position\":{\"x\":2},\"future\":true}", item.SourceHash, true, "visual.json/position")],
            ["visual.json/position"]);
        var mutation = new PbirAuthoringFidelityService().Compare(envelope, changed);

        Assert.True(mutation.IsFidelityReady);
        Assert.Contains("visual.json", mutation.IntentionallyChanged);
        Assert.DoesNotContain("future", mutation.UnexpectedPaths);
    }
}
