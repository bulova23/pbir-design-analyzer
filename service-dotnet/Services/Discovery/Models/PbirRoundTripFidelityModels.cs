using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal enum PbirFidelityClassification
{
    ByteIdentical,
    SemanticallyIdentical,
    ExpectedNormalizedDifference,
    UnexpectedDifference,
    MissingOutput,
    Unsupported
}

internal sealed record PbirRoundTripFileFidelity(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("classification")] PbirFidelityClassification Classification,
    [property: JsonPropertyName("sourceHash")] string? SourceHash,
    [property: JsonPropertyName("outputHash")] string? OutputHash);

internal sealed record PbirRoundTripFidelityResult(
    [property: JsonPropertyName("files")] IReadOnlyList<PbirRoundTripFileFidelity> Files,
    [property: JsonPropertyName("preservedPaths")] IReadOnlyList<string> PreservedPaths,
    [property: JsonPropertyName("changedPaths")] IReadOnlyList<string> ChangedPaths,
    [property: JsonPropertyName("unexpectedPaths")] IReadOnlyList<string> UnexpectedPaths)
{
    internal bool IsFidelityReady => UnexpectedPaths.Count == 0;
}
