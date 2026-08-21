using System.Text.Json;
using System.Text.Json.Serialization;
using PowerBIModelingService.Services.Discovery.Models;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.PbirAuthoringRpc;

internal static class PbirAuthoringRpcContract
{
    internal const string SchemaVersionV1 = "pbir-authoring-rpc/v1";
    internal const string SnapshotHandleSchemaVersionV1 = "pbir-authoring-rpc-snapshot/v1";
    internal const string ArtifactHandleSchemaVersionV1 = "pbir-authoring-rpc-artifact/v1";
}

internal enum PbirAuthoringRpcOperation
{
    Generate,
    Import,
    Mutate,
    Validate,
    Analyze
}

internal enum PbirAuthoringMutationMode
{
    Preview,
    Execute
}

internal static class PbirAuthoringMutationModeCatalog
{
    internal static IReadOnlyList<PbirAuthoringMutationMode> All { get; } =
        [PbirAuthoringMutationMode.Preview, PbirAuthoringMutationMode.Execute];
}

internal static class PbirAuthoringRpcOperationCatalog
{
    internal static IReadOnlyList<PbirAuthoringRpcOperation> All { get; } =
    [
        PbirAuthoringRpcOperation.Generate,
        PbirAuthoringRpcOperation.Import,
        PbirAuthoringRpcOperation.Mutate,
        PbirAuthoringRpcOperation.Validate,
        PbirAuthoringRpcOperation.Analyze
    ];
}

internal enum PbirAuthoringRpcErrorCategory
{
    InvalidRequest,
    ImportFailed,
    UnsupportedAuthoring,
    MutationConflict,
    ValidationFailed,
    AnalyzerFailed,
    ExecutionFailed,
    InternalFailure
}

internal static class PbirAuthoringRpcErrorCategoryCatalog
{
    internal static IReadOnlyList<PbirAuthoringRpcErrorCategory> All { get; } =
    [
        PbirAuthoringRpcErrorCategory.InvalidRequest,
        PbirAuthoringRpcErrorCategory.ImportFailed,
        PbirAuthoringRpcErrorCategory.UnsupportedAuthoring,
        PbirAuthoringRpcErrorCategory.MutationConflict,
        PbirAuthoringRpcErrorCategory.ValidationFailed,
        PbirAuthoringRpcErrorCategory.AnalyzerFailed,
        PbirAuthoringRpcErrorCategory.ExecutionFailed,
        PbirAuthoringRpcErrorCategory.InternalFailure
    ];
}

internal enum PbirAuthoringGenerationRequestKind
{
    V1,
    V2,
    V3,
    V4,
    V5,
    V6,
    V7
}

internal static class PbirAuthoringGenerationRequestKindCatalog
{
    internal static IReadOnlyList<PbirAuthoringGenerationRequestKind> All { get; } =
        Enum.GetValues<PbirAuthoringGenerationRequestKind>();
}

internal sealed record PbirAuthoringGenerationRequest
{
    internal PbirAuthoringGenerationRequest(LocalPbirGenerationRequest request) => V1 = request;
    internal PbirAuthoringGenerationRequest(LocalPbirGenerationRequestV2 request) => V2 = request;
    internal PbirAuthoringGenerationRequest(LocalPbirGenerationRequestV3 request) => V3 = request;
    internal PbirAuthoringGenerationRequest(LocalPbirGenerationRequestV4 request) => V4 = request;
    internal PbirAuthoringGenerationRequest(LocalPbirGenerationRequestV5 request) => V5 = request;
    internal PbirAuthoringGenerationRequest(LocalPbirGenerationRequestV6 request) => V6 = request;
    internal PbirAuthoringGenerationRequest(LocalPbirGenerationRequestV7 request) => V7 = request;

    [JsonPropertyName("v1")] internal LocalPbirGenerationRequest? V1 { get; }
    [JsonPropertyName("v2")] internal LocalPbirGenerationRequestV2? V2 { get; }
    [JsonPropertyName("v3")] internal LocalPbirGenerationRequestV3? V3 { get; }
    [JsonPropertyName("v4")] internal LocalPbirGenerationRequestV4? V4 { get; }
    [JsonPropertyName("v5")] internal LocalPbirGenerationRequestV5? V5 { get; }
    [JsonPropertyName("v6")] internal LocalPbirGenerationRequestV6? V6 { get; }
    [JsonPropertyName("v7")] internal LocalPbirGenerationRequestV7? V7 { get; }

    internal PbirAuthoringGenerationRequestKind Kind =>
        V1 is not null ? PbirAuthoringGenerationRequestKind.V1 :
        V2 is not null ? PbirAuthoringGenerationRequestKind.V2 :
        V3 is not null ? PbirAuthoringGenerationRequestKind.V3 :
        V4 is not null ? PbirAuthoringGenerationRequestKind.V4 :
        V5 is not null ? PbirAuthoringGenerationRequestKind.V5 :
        V6 is not null ? PbirAuthoringGenerationRequestKind.V6 :
        PbirAuthoringGenerationRequestKind.V7;

    internal string KindSchemaVersion =>
        V1?.SchemaVersion ?? V2?.SchemaVersion ?? V3?.SchemaVersion ?? V4?.SchemaVersion ??
        V5?.SchemaVersion ?? V6?.SchemaVersion ?? V7?.SchemaVersion ?? string.Empty;
}

internal sealed record PbirAuthoringRpcRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("operation")] PbirAuthoringRpcOperation Operation,
    [property: JsonPropertyName("generate")] PbirAuthoringGenerateRequest? Generate = null,
    [property: JsonPropertyName("import")] PbirAuthoringImportRequest? Import = null,
    [property: JsonPropertyName("mutate")] PbirAuthoringMutateRequest? Mutate = null,
    [property: JsonPropertyName("validate")] PbirAuthoringValidateRequest? Validate = null,
    [property: JsonPropertyName("analyze")] PbirAuthoringAnalyzeRequest? Analyze = null);

internal sealed record PbirAuthoringGenerateRequest(
    [property: JsonPropertyName("request")] PbirAuthoringGenerationRequest Request);

internal sealed record PbirAuthoringImportRequest(
    [property: JsonPropertyName("sourceDirectory")] string SourceDirectory);

internal sealed record PbirAuthoringSnapshotHandle(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("snapshotId")] string SnapshotId,
    [property: JsonPropertyName("sourceIdentity")] PbirAuthoringSourceIdentity SourceIdentity);

internal sealed record PbirAuthoringSourceIdentity(
    [property: JsonPropertyName("sourceDirectoryName")] string SourceDirectoryName,
    [property: JsonPropertyName("contentHash")] string ContentHash,
    [property: JsonPropertyName("fileCount")] int FileCount);

internal sealed record PbirAuthoringMutateRequest(
    [property: JsonPropertyName("snapshot")] PbirAuthoringSnapshotHandle Snapshot,
    [property: JsonPropertyName("request")] LocalPbirMutationRequest Request,
    [property: JsonPropertyName("mode")] PbirAuthoringMutationMode Mode = PbirAuthoringMutationMode.Execute);

internal sealed record PbirAuthoringArtifactHandle(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("artifactId")] string ArtifactId,
    [property: JsonPropertyName("artifactHash")] string ArtifactHash,
    [property: JsonPropertyName("manifestId")] string ManifestId,
    [property: JsonPropertyName("manifestHash")] string ManifestHash);

internal sealed record PbirAuthoringValidateRequest(
    [property: JsonPropertyName("artifact")] PbirAuthoringArtifactHandle Artifact);

internal sealed record PbirAuthoringAnalyzeRequest(
    [property: JsonPropertyName("reportDirectory")] string? ReportDirectory = null,
    [property: JsonPropertyName("artifact")] PbirAuthoringArtifactHandle? Artifact = null,
    [property: JsonPropertyName("snapshot")] PbirAuthoringSnapshotHandle? Snapshot = null,
    [property: JsonPropertyName("config")] JsonElement? Config = null,
    [property: JsonPropertyName("pageName")] string? PageName = null);

internal sealed record PbirAuthoringArtifactIdentity(
    [property: JsonPropertyName("artifactId")] string ArtifactId,
    [property: JsonPropertyName("artifactHash")] string ArtifactHash,
    [property: JsonPropertyName("manifestId")] string ManifestId,
    [property: JsonPropertyName("manifestHash")] string ManifestHash);

internal sealed record PbirAuthoringFidelity(
    [property: JsonPropertyName("classification")] PbirFidelityClassification Classification,
    [property: JsonPropertyName("preservedPathCount")] int PreservedPathCount,
    [property: JsonPropertyName("changedPathCount")] int ChangedPathCount,
    [property: JsonPropertyName("unexpectedPathCount")] int UnexpectedPathCount);

internal sealed record PbirAuthoringTiming(
    [property: JsonPropertyName("dispatchMilliseconds")] long DispatchMilliseconds,
    [property: JsonPropertyName("orchestrationMilliseconds")] long OrchestrationMilliseconds,
    [property: JsonPropertyName("serializationMilliseconds")] long SerializationMilliseconds,
    [property: JsonPropertyName("analyzerMilliseconds")] long AnalyzerMilliseconds,
    [property: JsonPropertyName("planningMilliseconds")] long PlanningMilliseconds = 0,
    [property: JsonPropertyName("previewMilliseconds")] long PreviewMilliseconds = 0,
    [property: JsonPropertyName("analyzerBeforeMilliseconds")] long AnalyzerBeforeMilliseconds = 0);

internal enum PbirAuthoringDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

internal sealed record PbirAuthoringDiagnostic(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("severity")] PbirAuthoringDiagnosticSeverity Severity,
    [property: JsonPropertyName("summary")] string Summary);

internal sealed record PbirAuthoringRpcError(
    [property: JsonPropertyName("category")] PbirAuthoringRpcErrorCategory Category,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("summary")] string Summary);

internal sealed record PbirAuthoringAnalyzerSummary(
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("pageCount")] int PageCount,
    [property: JsonPropertyName("visualCount")] int VisualCount,
    [property: JsonPropertyName("result")] ScoreResult? Result = null);

internal sealed record PbirAuthoringRpcResponse(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("operation")] PbirAuthoringRpcOperation Operation,
    [property: JsonPropertyName("succeeded")] bool Succeeded,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<PbirAuthoringDiagnostic> Diagnostics,
    [property: JsonPropertyName("error")] PbirAuthoringRpcError? Error,
    [property: JsonPropertyName("artifactIdentity")] PbirAuthoringArtifactIdentity? ArtifactIdentity,
    [property: JsonPropertyName("fidelity")] PbirAuthoringFidelity? Fidelity,
    [property: JsonPropertyName("analyzer")] PbirAuthoringAnalyzerSummary? Analyzer,
    [property: JsonPropertyName("timing")] PbirAuthoringTiming Timing,
    [property: JsonPropertyName("generateResult")] PbirAuthoringGenerateResult? GenerateResult = null,
    [property: JsonPropertyName("importResult")] PbirAuthoringImportResult? ImportResult = null,
    [property: JsonPropertyName("mutateResult")] PbirAuthoringMutateResult? MutateResult = null,
    [property: JsonPropertyName("validateResult")] PbirAuthoringValidateResult? ValidateResult = null,
    [property: JsonPropertyName("analyzeResult")] PbirAuthoringAnalyzeResult? AnalyzeResult = null);

internal sealed record PbirAuthoringGenerateResult(
    [property: JsonPropertyName("requestVersion")] string RequestVersion,
    [property: JsonPropertyName("artifact")] PbirAuthoringArtifactHandle? Artifact);

internal sealed record PbirAuthoringImportResult(
    [property: JsonPropertyName("snapshot")] PbirAuthoringSnapshotHandle Snapshot,
    [property: JsonPropertyName("pages")] IReadOnlyList<PbirAuthoringPageMetadata> Pages,
    [property: JsonPropertyName("visuals")] IReadOnlyList<PbirAuthoringVisualMetadata> Visuals);

internal sealed record PbirAuthoringPageMetadata(
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("displayName")] string DisplayName);

internal sealed record PbirAuthoringVisualMetadata(
    [property: JsonPropertyName("visualId")] string VisualId,
    [property: JsonPropertyName("pageId")] string PageId,
    [property: JsonPropertyName("visualType")] string VisualType,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("layout")] LocalPbirGenerationVisualLayout? Layout);

internal sealed record PbirAuthoringMutationPreview(
    [property: JsonPropertyName("previewId")] string PreviewId,
    [property: JsonPropertyName("mutationKind")] LocalPbirMutationOperationKind MutationKind,
    [property: JsonPropertyName("targetPageId")] string TargetPageId,
    [property: JsonPropertyName("currentDisplayName")] string CurrentDisplayName,
    [property: JsonPropertyName("proposedDisplayName")] string ProposedDisplayName,
    [property: JsonPropertyName("affectedPageIds")] IReadOnlyList<string> AffectedPageIds,
    [property: JsonPropertyName("affectedVisualIds")] IReadOnlyList<string> AffectedVisualIds,
    [property: JsonPropertyName("preservedPageIds")] IReadOnlyList<string> PreservedPageIds,
    [property: JsonPropertyName("preservedVisualIds")] IReadOnlyList<string> PreservedVisualIds,
    [property: JsonPropertyName("affectedObjectCount")] int AffectedObjectCount,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<PbirAuthoringDiagnostic> Diagnostics,
    [property: JsonPropertyName("executionAdmissible")] bool ExecutionAdmissible,
    [property: JsonPropertyName("isNoOp")] bool IsNoOp,
    [property: JsonPropertyName("payload")] PbirAuthoringMutationPreviewPayload? Payload = null,
    [property: JsonPropertyName("diffs")] IReadOnlyList<PbirAuthoringSemanticDiff>? Diffs = null);

internal sealed record PbirAuthoringMutationPreviewPayload(
    [property: JsonPropertyName("kind")] LocalPbirMutationOperationKind Kind,
    [property: JsonPropertyName("page")] PbirAuthoringPageMutationPreview? Page = null,
    [property: JsonPropertyName("visual")] PbirAuthoringVisualMutationPreview? Visual = null);

internal sealed record PbirAuthoringPageMutationPreview(
    [property: JsonPropertyName("currentDisplayName")] string? CurrentDisplayName,
    [property: JsonPropertyName("proposedDisplayName")] string? ProposedDisplayName,
    [property: JsonPropertyName("currentPosition")] int? CurrentPosition,
    [property: JsonPropertyName("proposedPosition")] int? ProposedPosition,
    [property: JsonPropertyName("deterministicPageId")] string? DeterministicPageId,
    [property: JsonPropertyName("navigationAffectedPageIds")] IReadOnlyList<string> NavigationAffectedPageIds);

internal sealed record PbirAuthoringVisualMutationPreview(
    [property: JsonPropertyName("currentPageId")] string? CurrentPageId,
    [property: JsonPropertyName("proposedPageId")] string? ProposedPageId,
    [property: JsonPropertyName("currentOrder")] int? CurrentOrder,
    [property: JsonPropertyName("proposedOrder")] int? ProposedOrder,
    [property: JsonPropertyName("currentLayout")] LocalPbirGenerationVisualLayout? CurrentLayout,
    [property: JsonPropertyName("proposedLayout")] LocalPbirGenerationVisualLayout? ProposedLayout);

internal sealed record PbirAuthoringSemanticDiff(
    [property: JsonPropertyName("kind")] LocalPbirMutationSemanticDiffKind Kind,
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("beforePageId")] string? BeforePageId,
    [property: JsonPropertyName("afterPageId")] string? AfterPageId,
    [property: JsonPropertyName("beforeDisplayName")] string? BeforeDisplayName,
    [property: JsonPropertyName("afterDisplayName")] string? AfterDisplayName,
    [property: JsonPropertyName("beforeOrder")] int? BeforeOrder,
    [property: JsonPropertyName("afterOrder")] int? AfterOrder,
    [property: JsonPropertyName("beforeLayout")] LocalPbirGenerationVisualLayout? BeforeLayout,
    [property: JsonPropertyName("afterLayout")] LocalPbirGenerationVisualLayout? AfterLayout);

internal sealed record PbirAuthoringAnalyzerComparison(
    [property: JsonPropertyName("before")] PbirAuthoringAnalyzerSummary Before,
    [property: JsonPropertyName("after")] PbirAuthoringAnalyzerSummary After,
    [property: JsonPropertyName("scoreDelta")] double ScoreDelta,
    [property: JsonPropertyName("preservedPageIds")] IReadOnlyList<string> PreservedPageIds,
    [property: JsonPropertyName("preservedVisualIds")] IReadOnlyList<string> PreservedVisualIds);

internal sealed record PbirAuthoringMutateResult(
    [property: JsonPropertyName("artifact")] PbirAuthoringArtifactHandle? Artifact,
    [property: JsonPropertyName("changedPageCount")] int ChangedPageCount,
    [property: JsonPropertyName("changedVisualCount")] int ChangedVisualCount,
    [property: JsonPropertyName("preview")] PbirAuthoringMutationPreview? Preview = null,
    [property: JsonPropertyName("comparison")] PbirAuthoringAnalyzerComparison? Comparison = null,
    [property: JsonPropertyName("materialization")] PbirAuthoringMaterializationHandle? Materialization = null);

internal sealed record PbirAuthoringMaterializationHandle(
    [property: JsonPropertyName("outputBaseDirectory")] string OutputBaseDirectory,
    [property: JsonPropertyName("targetDirectoryName")] string TargetDirectoryName,
    [property: JsonPropertyName("targetKey")] string TargetKey,
    [property: JsonPropertyName("transactionId")] string TransactionId,
    [property: JsonPropertyName("transactionHash")] string TransactionHash,
    [property: JsonPropertyName("currentReceiptHash")] string CurrentReceiptHash,
    [property: JsonPropertyName("currentTargetStateHash")] string CurrentTargetStateHash);

internal sealed record PbirAuthoringValidateResult(
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("validatedFileCount")] int ValidatedFileCount);

internal sealed record PbirAuthoringAnalyzeResult(
    [property: JsonPropertyName("summary")] PbirAuthoringAnalyzerSummary Summary);
