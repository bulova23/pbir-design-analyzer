using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal sealed record PbirResolvedAuthoringDocument(
    [property: JsonPropertyName("ownerKind")] PbirAuthoringOwnerKind OwnerKind,
    [property: JsonPropertyName("ownerId")] string OwnerId,
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("sourceHash")] string SourceHash,
    bool Changed = false,
    string? ChangedPath = null);

internal sealed record PbirResolvedAuthoringRepresentation(
    IReadOnlyList<PbirResolvedAuthoringDocument> Documents,
    IReadOnlyList<string> ChangedPaths)
{
    internal static PbirResolvedAuthoringRepresentation Empty { get; } = new([], []);
}
