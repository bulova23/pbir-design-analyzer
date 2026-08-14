using System.Text.Json.Serialization;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class LocalPbirGenerationRequestContract
{
    internal const string SchemaVersionV1 = "local-pbir-generation-request/v1";
    internal const string SchemaVersionV2 = "local-pbir-generation-request/v2";
    internal const string SchemaVersionV3 = "local-pbir-generation-request/v3";
}

internal static class LocalPbirGenerationProviderContract
{
    internal const string SchemaVersionV1 = "local-pbir-generation-provider/v1";
    internal const string SupportedVisualType = "card";
    internal static IReadOnlyList<string> SupportedVisualTypes { get; } = ["card", "table"];
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

internal enum LocalPbirGenerationBindingKind
{
    Measure,
    Dimension
}

internal sealed record LocalPbirGenerationPage(
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("authoring")] LocalPbirGenerationPageAuthoring? Authoring = null);

internal sealed record LocalPbirGenerationLayout(
    [property: JsonPropertyName("x")] int? X,
    [property: JsonPropertyName("y")] int? Y,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height);

internal sealed record LocalPbirGenerationVisualLayout(int X, int Y, int Width, int Height);

internal sealed record LocalPbirGenerationBinding(
    [property: JsonPropertyName("bindingId")] string BindingId,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("kind")] LocalPbirGenerationBindingKind Kind,
    [property: JsonPropertyName("entity")] string Entity,
    [property: JsonPropertyName("property")] string Property);

internal sealed record LocalPbirGenerationVisual(
    [property: JsonPropertyName("visualId")] string VisualId,
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("visualType")] string VisualType,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("layout")] LocalPbirGenerationLayout? Layout,
    [property: JsonPropertyName("bindings")] IReadOnlyList<LocalPbirGenerationBinding> Bindings,
    [property: JsonPropertyName("authoring")] LocalPbirGenerationVisualAuthoring? Authoring = null);

internal sealed record LocalPbirGenerationRequestV2(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("reportName")] string ReportName,
    [property: JsonPropertyName("datasetPath")] string DatasetPath,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc,
    [property: JsonPropertyName("outputBaseDirectory")] string OutputBaseDirectory,
    [property: JsonPropertyName("targetDirectoryName")] string TargetDirectoryName,
    [property: JsonPropertyName("pages")] IReadOnlyList<LocalPbirGenerationPage> Pages,
    [property: JsonPropertyName("visuals")] IReadOnlyList<LocalPbirGenerationVisual> Visuals);

internal sealed record LocalPbirGenerationRequestV3(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("reportName")] string ReportName,
    [property: JsonPropertyName("datasetPath")] string DatasetPath,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc,
    [property: JsonPropertyName("outputBaseDirectory")] string OutputBaseDirectory,
    [property: JsonPropertyName("targetDirectoryName")] string TargetDirectoryName,
    [property: JsonPropertyName("pages")] IReadOnlyList<LocalPbirGenerationPage> Pages,
    [property: JsonPropertyName("visuals")] IReadOnlyList<LocalPbirGenerationVisual> Visuals,
    [property: JsonPropertyName("theme")] LocalPbirGenerationTheme? Theme = null,
    [property: JsonPropertyName("reportFilters")] IReadOnlyList<LocalPbirGenerationEqualityFilter>? ReportFilters = null,
    [property: JsonPropertyName("metadata")] LocalPbirGenerationReportMetadata? Metadata = null,
    [property: JsonPropertyName("interaction")] LocalPbirGenerationInteractionSettings? Interaction = null,
    [property: JsonPropertyName("layout")] LocalPbirGenerationLayoutSettings? Layout = null);

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

internal sealed record LocalPbirGenerationPerformance(
    [property: JsonPropertyName("generationMilliseconds")] long GenerationMilliseconds,
    [property: JsonPropertyName("materializationMilliseconds")] long MaterializationMilliseconds,
    [property: JsonPropertyName("analyzerMilliseconds")] long AnalyzerMilliseconds);

internal sealed record LocalPbirGenerationResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("readiness")] LocalPbirGenerationReadinessState Readiness,
    [property: JsonPropertyName("artifact")] PbirDeployableArtifact? Artifact,
    [property: JsonPropertyName("manifest")] PbirDeployableManifest? Manifest,
    [property: JsonPropertyName("validation")] PbirDeployableValidation? Validation,
    [property: JsonPropertyName("materialization")] PbirMaterializationOrchestrationResult? Materialization,
    [property: JsonPropertyName("roundTrip")] LocalPbirGenerationRoundTrip? RoundTrip,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<LocalPbirGenerationDiagnostic> Diagnostics)
{
    internal int GeneratedPageCount { get; init; }
    internal int GeneratedVisualCount { get; init; }
    internal LocalPbirGenerationPerformance? Performance { get; init; }
}
