using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirMaterializationOrchestrationPreviewRequestContract { internal const string SchemaVersionV1 = "pbir-materialization-orchestration-preview-request/v1"; }
internal static class PbirMaterializationOrchestrationApplyRequestContract { internal const string SchemaVersionV1 = "pbir-materialization-orchestration-apply-request/v1"; }
internal static class PbirMaterializationOrchestrationRecoveryRequestContract { internal const string SchemaVersionV1 = "pbir-materialization-orchestration-recovery-request/v1"; }
internal static class PbirMaterializationOrchestrationPreviewIdentityContract { internal const string SchemaVersionV1 = "pbir-materialization-orchestration-preview-identity/v1"; }
internal static class PbirMaterializationOrchestrationResultContract { internal const string SchemaVersionV1 = "pbir-materialization-orchestration-result/v1"; }
internal static class PbirMaterializationOrchestrationDiagnosticsContract { internal const string SchemaVersionV1 = "pbir-materialization-orchestration-diagnostics/v1"; }

internal enum PbirMaterializationOrchestrationOutcome
{
    Absent,
    Empty,
    ExactMatch,
    ManagedReplacement,
    Conflict,
    RecoveryRequired,
    Applied,
    StalePreview,
    InvalidRequest,
    UnsafeDestination,
    UnsupportedOperation,
    SchemaFailure,
    TransactionReused,
    Cancelled,
    Failure
}

internal sealed record PbirMaterializationOrchestrationInput(
    PbirIntermediateRepresentationState IrState,
    PbirSerializerRequest SerializerRequest,
    PbirDeployableSerializerRequest DeployableSerializerRequest,
    string OutputBaseDirectory,
    string TargetDirectoryName);

internal sealed record PbirMaterializationOrchestrationPreviewRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("requestedOperation")] string RequestedOperation,
    [property: JsonPropertyName("input")] PbirMaterializationOrchestrationInput Input);

internal sealed record PbirMaterializationOrchestrationPreviewIdentity(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("previewRequestId")] string PreviewRequestId,
    [property: JsonPropertyName("previewId")] string PreviewId,
    [property: JsonPropertyName("previewHash")] string PreviewHash,
    [property: JsonPropertyName("targetStateHash")] string TargetStateHash,
    [property: JsonPropertyName("artifactRef")] string ArtifactRef,
    [property: JsonPropertyName("artifactHash")] string ArtifactHash,
    [property: JsonPropertyName("manifestRef")] string ManifestRef,
    [property: JsonPropertyName("manifestHash")] string ManifestHash);

internal sealed record PbirMaterializationOrchestrationApplyRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("requestedOperation")] string RequestedOperation,
    [property: JsonPropertyName("input")] PbirMaterializationOrchestrationInput Input,
    [property: JsonPropertyName("validatedPreview")] PbirMaterializationOrchestrationPreviewIdentity ValidatedPreview,
    [property: JsonPropertyName("transactionId")] string TransactionId,
    [property: JsonPropertyName("applyApproved")] bool ApplyApproved);

internal sealed record PbirMaterializationOrchestrationRecoveryRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("requestedOperation")] string RequestedOperation,
    [property: JsonPropertyName("input")] PbirMaterializationOrchestrationInput Input,
    [property: JsonPropertyName("previewRequestId")] string PreviewRequestId);

internal sealed record PbirMaterializationOrchestrationDiagnostic(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("message")] string Message);

internal sealed record PbirMaterializationOrchestrationDiagnostics(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("items")] IReadOnlyList<PbirMaterializationOrchestrationDiagnostic> Items)
{
    internal static PbirMaterializationOrchestrationDiagnostics Empty { get; } =
        new(PbirMaterializationOrchestrationDiagnosticsContract.SchemaVersionV1, []);
}

internal sealed record PbirMaterializationOrchestrationResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("outcome")] PbirMaterializationOrchestrationOutcome Outcome,
    [property: JsonPropertyName("validatedPreview")] PbirMaterializationOrchestrationPreviewIdentity? ValidatedPreview,
    [property: JsonPropertyName("transactionId")] string? TransactionId,
    [property: JsonPropertyName("activeTransactionRef")] string? ActiveTransactionRef,
    [property: JsonPropertyName("rollbackAvailable")] bool RollbackAvailable,
    [property: JsonPropertyName("writtenFiles")] IReadOnlyList<PbirDeployableTargetInventoryFile> WrittenFiles,
    [property: JsonPropertyName("lineage")] PbirDeployableMaterializationLineage? Lineage,
    [property: JsonPropertyName("targetStateHash")] string? TargetStateHash,
    [property: JsonPropertyName("resultHash")] string? ResultHash,
    [property: JsonPropertyName("diagnostics")] PbirMaterializationOrchestrationDiagnostics Diagnostics);
