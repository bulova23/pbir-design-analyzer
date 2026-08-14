using System.Text.Json;
using System.Text.Json.Nodes;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirAuthoringMergeService
{
    internal PbirResolvedAuthoringRepresentation Resolve(PbirIntermediateRepresentation ir)
    {
        ArgumentNullException.ThrowIfNull(ir);
        if (ir.AuthoringEnvelope is null) return PbirResolvedAuthoringRepresentation.Empty;

        var documents = ir.AuthoringEnvelope.Items
            .Where(item => item.Classification is not PbirAuthoringPreservationClass.Unsupported)
            .OrderBy(item => item.OwnedRelativePath, StringComparer.Ordinal)
            .Select(item => ResolveDocument(ir, item))
            .ToArray();
        return new(documents, documents.Where(document => document.Changed).Select(document => document.ChangedPath!).ToArray());
    }

    private static PbirResolvedAuthoringDocument ResolveDocument(
        PbirIntermediateRepresentation ir,
        PbirAuthoringEnvelopeItem item)
    {
        var content = item.SourceContent ?? JsonSerializer.Serialize(item.SourceDocument);
        if (item.OwnerKind == PbirAuthoringOwnerKind.Visual)
        {
            var visual = ir.Visuals.FirstOrDefault(value => string.Equals(value.VisualId, item.OwnerId, StringComparison.Ordinal));
            if (visual?.Layout is not null && TryMergeLayout(content, visual.Layout, out var merged))
            {
                return new(item.OwnerKind, item.OwnerId, item.OwnedRelativePath, merged,
                    item.SourceHash, true, $"{item.OwnedRelativePath}/position");
            }
        }
        return new(item.OwnerKind, item.OwnerId, item.OwnedRelativePath, content, item.SourceHash, false, null);
    }

    private static bool TryMergeLayout(
        string content,
        PbirIntermediateRepresentationVisualLayout layout,
        out string merged)
    {
        var node = JsonNode.Parse(content)?.AsObject();
        var position = node?["position"]?.AsObject();
        if (node is null || position is null) { merged = content; return false; }
        var changed = position["x"]?.GetValue<int>() != layout.X ||
                      position["y"]?.GetValue<int>() != layout.Y ||
                      position["width"]?.GetValue<int>() != layout.Width ||
                      position["height"]?.GetValue<int>() != layout.Height;
        if (!changed) { merged = content; return false; }
        position["x"] = layout.X;
        position["y"] = layout.Y;
        position["width"] = layout.Width;
        position["height"] = layout.Height;
        merged = node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        return true;
    }
}
