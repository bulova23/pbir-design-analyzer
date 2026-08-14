using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal enum LocalPbirGenerationSlicerInteractionMode
{
    Default,
    DataFilter,
    HighlightFilter,
    NoFilter
}

internal sealed record LocalPbirGenerationSlicerInteractionRule(
    [property: JsonPropertyName("interactionId")] string InteractionId,
    [property: JsonPropertyName("sourceVisualId")] string SourceVisualId,
    [property: JsonPropertyName("targetVisualIds")] IReadOnlyList<string> TargetVisualIds,
    [property: JsonPropertyName("mode")] LocalPbirGenerationSlicerInteractionMode Mode,
    [property: JsonPropertyName("enabled")] bool Enabled = true);

internal sealed record Phase42InteractionValidationResult(
    IReadOnlyList<LocalPbirGenerationSlicerInteractionRule> Rules,
    IReadOnlyList<LocalPbirGenerationDiagnostic> Diagnostics);

internal sealed record PbirIntermediateRepresentationVisualInteraction(
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("sourceVisualId")] string SourceVisualId,
    [property: JsonPropertyName("targetVisualId")] string TargetVisualId,
    [property: JsonPropertyName("type")] string Type);
