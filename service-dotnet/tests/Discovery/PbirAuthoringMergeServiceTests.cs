using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirAuthoringMergeServiceTests
{
    [Fact]
    public void Resolve_UnchangedEnvelope_UsesSourceContentAndPreservesUnmodeledProperties()
    {
        using var source = JsonDocument.Parse("{\"$schema\":\"schema\",\"objects\":{\"title\":[]},\"futureProperty\":{\"enabled\":true}}");
        var item = new PbirAuthoringEnvelopeItem(
            PbirAuthoringOwnerKind.Visual, "visual-1", "visual.json", "schema", "1.0.0",
            PbirAuthoringPreservationClass.OpaquePreserved, source.RootElement.Clone(),
            "{\n  \"$schema\": \"schema\",\n  \"objects\": {\"title\": []},\n  \"futureProperty\": {\"enabled\": true}\n}", "original",
            ["$schema", "objects", "futureProperty"]);
        var ir = CreateIr(new(PbirAuthoringEnvelopeContract.SchemaVersionV1, [item], "definition"));

        var result = new PbirAuthoringMergeService().Resolve(ir);

        var resolved = Assert.Single(result.Documents);
        Assert.Equal(item.SourceContent, resolved.Content);
        Assert.Contains("futureProperty", resolved.Content, StringComparison.Ordinal);
        Assert.Empty(result.ChangedPaths);
    }

    [Fact]
    public void Resolve_ChangedTypedLayout_OverridesOnlyPositionAndKeepsOpaqueVisualProperties()
    {
        using var source = JsonDocument.Parse("{\"$schema\":\"schema\",\"name\":\"visual-1\",\"position\":{\"x\":1,\"y\":2,\"width\":10,\"height\":20,\"z\":7},\"visual\":{\"visualType\":\"card\",\"futureProperty\":{\"keep\":true}}}");
        var item = new PbirAuthoringEnvelopeItem(
            PbirAuthoringOwnerKind.Visual, "visual-1", "pages/page-1/visuals/visual-1/visual.json", "schema", "1.0.0",
            PbirAuthoringPreservationClass.TypedSupported, source.RootElement.Clone(), source.RootElement.GetRawText(), "original",
            ["$schema", "name", "position", "visual"], new("visual-1", null, null));
        var visual = new PbirIntermediateRepresentationVisual("visual-1", "page-1", "card", "page:page-1/slot:1", "visual-1", [], 0, new(100, 110, 120, 130), []);
        var ir = CreateIr(new(PbirAuthoringEnvelopeContract.SchemaVersionV1, [item], "definition")) with { Visuals = [visual] };

        var result = new PbirAuthoringMergeService().Resolve(ir);

        var resolved = Assert.Single(result.Documents);
        using var document = JsonDocument.Parse(resolved.Content);
        var position = document.RootElement.GetProperty("position");
        Assert.Equal(100, position.GetProperty("x").GetInt32());
        Assert.Equal(7, position.GetProperty("z").GetInt32());
        Assert.True(document.RootElement.GetProperty("visual").GetProperty("futureProperty").GetProperty("keep").GetBoolean());
        Assert.Contains("pages/page-1/visuals/visual-1/visual.json/position", result.ChangedPaths);
    }

    private static PbirIntermediateRepresentation CreateIr(PbirAuthoringEnvelope envelope) => new(
        new("ir", PbirIntermediateRepresentationContract.SchemaVersionV1, DateTime.UnixEpoch), new("manifest", "spec"),
        [], [], [], new("", [], [], []), new([], [], [], []), new([], [], []), new([], []), new("", "", ""), null, envelope);
}
