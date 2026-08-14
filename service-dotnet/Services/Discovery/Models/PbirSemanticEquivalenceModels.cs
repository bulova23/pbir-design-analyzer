using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal sealed record PbirSemanticEquivalenceResult(
    [property: JsonPropertyName("isEquivalent")] bool IsEquivalent,
    [property: JsonPropertyName("unchangedPaths")] IReadOnlyList<string> UnchangedPaths,
    [property: JsonPropertyName("expectedChanges")] IReadOnlyList<string> ExpectedChanges,
    [property: JsonPropertyName("unexpectedChanges")] IReadOnlyList<string> UnexpectedChanges);
