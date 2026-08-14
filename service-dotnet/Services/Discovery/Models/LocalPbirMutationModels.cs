using System.Text.Json.Serialization;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class LocalPbirMutationRequestContract
{
    internal const string SchemaVersionV1 = "local-pbir-mutation-request/v1";
}

internal static class LocalPbirMutationResultContract
{
    internal const string SchemaVersionV1 = "local-pbir-mutation-result/v1";
}

internal enum LocalPbirMutationReadiness
{
    Rejected,
    Planned,
    NoChanges,
    Applied,
    RoundTripVerified
}

internal enum LocalPbirMutationOperationKind
{
    AddPage,
    RemovePage,
    RenamePage,
    MovePage,
    AddVisual,
    RemoveVisual,
    ReplaceVisual,
    MoveVisual,
    ResizeVisual,
    UpdateBinding,
    UpdateFormatting,
    UpdateTheme,
    UpdateFilter,
    UpdateNavigation,
    UpdateSlicer
}

internal static class LocalPbirMutationOperationKindCatalog
{
    internal static IReadOnlyList<LocalPbirMutationOperationKind> All { get; } =
        Enum.GetValues<LocalPbirMutationOperationKind>();
}

internal sealed record LocalPbirMutationTarget(
    [property: JsonPropertyName("pageId")] string? PageId = null,
    [property: JsonPropertyName("visualId")] string? VisualId = null,
    [property: JsonPropertyName("section")] string? Section = null,
    [property: JsonPropertyName("slotId")] string? SlotId = null,
    [property: JsonPropertyName("navigationId")] string? NavigationId = null,
    [property: JsonPropertyName("slicerId")] string? SlicerId = null);

internal sealed record LocalPbirMutationOperation(
    [property: JsonPropertyName("kind")] LocalPbirMutationOperationKind Kind,
    [property: JsonPropertyName("target")] LocalPbirMutationTarget? Target = null,
    [property: JsonPropertyName("page")] LocalPbirGenerationPage? Page = null,
    [property: JsonPropertyName("visual")] LocalPbirGenerationVisual? Visual = null,
    [property: JsonPropertyName("replacement")] LocalPbirGenerationVisual? Replacement = null,
    [property: JsonPropertyName("displayName")] string? DisplayName = null,
    [property: JsonPropertyName("order")] int? Order = null,
    [property: JsonPropertyName("layout")] LocalPbirGenerationLayout? Layout = null,
    [property: JsonPropertyName("binding")] LocalPbirGenerationBinding? Binding = null,
    [property: JsonPropertyName("authoring")] LocalPbirGenerationVisualAuthoring? Authoring = null,
    [property: JsonPropertyName("theme")] LocalPbirGenerationTheme? Theme = null,
    [property: JsonPropertyName("filter")] LocalPbirGenerationEqualityFilter? Filter = null,
    [property: JsonPropertyName("navigation")] LocalPbirGenerationNavigationDefinition? Navigation = null,
    [property: JsonPropertyName("slicer")] LocalPbirGenerationSlicerDefinition? Slicer = null);

internal sealed record LocalPbirMutationRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("mutationId")] string MutationId,
    [property: JsonPropertyName("sourceDirectory")] string SourceDirectory,
    [property: JsonPropertyName("outputBaseDirectory")] string OutputBaseDirectory,
    [property: JsonPropertyName("targetDirectoryName")] string TargetDirectoryName,
    [property: JsonPropertyName("operations")] IReadOnlyList<LocalPbirMutationOperation> Operations,
    [property: JsonPropertyName("datasetPath")] string? DatasetPath = null,
    [property: JsonPropertyName("requestId")] string? RequestId = null);

internal sealed record LocalPbirMutationDiagnostic(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("message")] string Message);

internal sealed record LocalPbirMutationIdentityEvidence(
    [property: JsonPropertyName("pageIds")] IReadOnlyList<string> PageIds,
    [property: JsonPropertyName("visualIds")] IReadOnlyList<string> VisualIds,
    [property: JsonPropertyName("changedPageIds")] IReadOnlyList<string> ChangedPageIds,
    [property: JsonPropertyName("changedVisualIds")] IReadOnlyList<string> ChangedVisualIds);

internal sealed record LocalPbirMutationEvidence(
    [property: JsonPropertyName("mutationId")] string MutationId,
    [property: JsonPropertyName("operations")] IReadOnlyList<LocalPbirMutationOperationKind> Operations,
    [property: JsonPropertyName("affectedPages")] IReadOnlyList<string> AffectedPages,
    [property: JsonPropertyName("affectedVisuals")] IReadOnlyList<string> AffectedVisuals,
    [property: JsonPropertyName("identities")] LocalPbirMutationIdentityEvidence Identities,
    [property: JsonPropertyName("sourceFileHashes")] IReadOnlyDictionary<string, string> SourceFileHashes,
    [property: JsonPropertyName("resultFileHashes")] IReadOnlyDictionary<string, string> ResultFileHashes,
    [property: JsonPropertyName("idempotentNoChange")] bool IdempotentNoChange,
    [property: JsonPropertyName("analyzerResult")] ScoreResult? AnalyzerResult,
    [property: JsonPropertyName("lineage")] PbirDeployableLineage? Lineage);

internal sealed record LocalPbirMutationPerformance(
    [property: JsonPropertyName("importMilliseconds")] long ImportMilliseconds,
    [property: JsonPropertyName("planningMilliseconds")] long PlanningMilliseconds,
    [property: JsonPropertyName("executionMilliseconds")] long ExecutionMilliseconds,
    [property: JsonPropertyName("serializationMilliseconds")] long SerializationMilliseconds,
    [property: JsonPropertyName("materializationMilliseconds")] long MaterializationMilliseconds,
    [property: JsonPropertyName("analyzerMilliseconds")] long AnalyzerMilliseconds);

internal sealed record LocalPbirMutationResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("mutationId")] string MutationId,
    [property: JsonPropertyName("readiness")] LocalPbirMutationReadiness Readiness,
    [property: JsonPropertyName("artifact")] PbirDeployableArtifact? Artifact,
    [property: JsonPropertyName("manifest")] PbirDeployableManifest? Manifest,
    [property: JsonPropertyName("validation")] PbirDeployableValidation? Validation,
    [property: JsonPropertyName("materialization")] PbirMaterializationOrchestrationResult? Materialization,
    [property: JsonPropertyName("evidence")] LocalPbirMutationEvidence? Evidence,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<LocalPbirMutationDiagnostic> Diagnostics)
{
    internal LocalPbirMutationPerformance? Performance { get; init; }
}
