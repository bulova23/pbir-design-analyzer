using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirLocalReportImportContract
{
    internal const string SchemaVersionV1 = "pbir-local-report-import/v1";
}

internal sealed record PbirLocalReportImportSnapshot(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("sourceDirectory")] string SourceDirectory,
    [property: JsonPropertyName("irState")] PbirIntermediateRepresentationState IrState,
    [property: JsonPropertyName("pageIdentities")] IReadOnlyDictionary<string, string> PageIdentities,
    [property: JsonPropertyName("visualIdentities")] IReadOnlyDictionary<string, string> VisualIdentities,
    [property: JsonPropertyName("fileHashes")] IReadOnlyDictionary<string, string> FileHashes,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<LocalPbirMutationDiagnostic> Diagnostics,
    [property: JsonPropertyName("performance")] PbirLocalReportImportPerformance? Performance = null);

internal sealed record PbirLocalReportImportPerformance(
    [property: JsonPropertyName("readerMilliseconds")] long ReaderMilliseconds,
    [property: JsonPropertyName("semanticProjectionMilliseconds")] long SemanticProjectionMilliseconds);
