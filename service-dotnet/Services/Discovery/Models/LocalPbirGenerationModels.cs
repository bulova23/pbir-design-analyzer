using System.Text.Json.Serialization;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class LocalPbirGenerationRequestContract
{
    internal const string SchemaVersionV1 = "local-pbir-generation-request/v1";
}

internal static class LocalPbirGenerationProviderContract
{
    internal const string SchemaVersionV1 = "local-pbir-generation-provider/v1";
    internal const string SupportedVisualType = "card";
}

internal static class LocalPbirGenerationResultContract
{
    internal const string SchemaVersionV1 = "local-pbir-generation-result/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "schemaVersion",
        "requestId",
        "artifact",
        "manifest",
        "validation",
        "materialization",
        "roundTrip.score",
        "diagnostics"
    ];
}

internal enum LocalPbirGenerationReadinessState
{
    Rejected,
    Generated,
    RoundTripVerified
}

internal sealed record LocalPbirGenerationRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("reportName")] string ReportName,
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("pageDisplayName")] string PageDisplayName,
    [property: JsonPropertyName("visualId")] string VisualId,
    [property: JsonPropertyName("visualType")] string VisualType,
    [property: JsonPropertyName("datasetPath")] string DatasetPath,
    [property: JsonPropertyName("measureToken")] string MeasureToken,
    [property: JsonPropertyName("measureEntity")] string MeasureEntity,
    [property: JsonPropertyName("measureProperty")] string MeasureProperty,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc,
    [property: JsonPropertyName("outputBaseDirectory")] string OutputBaseDirectory,
    [property: JsonPropertyName("targetDirectoryName")] string TargetDirectoryName);

internal sealed record LocalPbirGenerationDiagnostic(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("message")] string Message);

internal sealed record LocalPbirGenerationRoundTrip(
    [property: JsonPropertyName("score")] ScoreResult Score,
    [property: JsonPropertyName("pageCount")] int PageCount,
    [property: JsonPropertyName("visualCount")] int VisualCount);

internal sealed record LocalPbirGenerationResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("readiness")] LocalPbirGenerationReadinessState Readiness,
    [property: JsonPropertyName("artifact")] PbirDeployableArtifact? Artifact,
    [property: JsonPropertyName("manifest")] PbirDeployableManifest? Manifest,
    [property: JsonPropertyName("validation")] PbirDeployableValidation? Validation,
    [property: JsonPropertyName("materialization")] PbirMaterializationOrchestrationResult? Materialization,
    [property: JsonPropertyName("roundTrip")] LocalPbirGenerationRoundTrip? RoundTrip,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<LocalPbirGenerationDiagnostic> Diagnostics);
