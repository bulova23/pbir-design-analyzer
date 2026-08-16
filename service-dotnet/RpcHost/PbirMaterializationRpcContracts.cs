using System.Text.Json.Serialization;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.RpcHost;

internal static class PbirMaterializationRpcContract
{
    internal const string PreviewOperation = "pbir/materialization/preview";
    internal const string ApplyOperation = "pbir/materialization/apply";
    internal const string RecoveryOperation = "pbir/materialization/recovery/inspect";
    internal const string PreviewRequestSchemaVersion = "pbir-local-materialization-preview-request/v1";
    internal const string ApplyRequestSchemaVersion = "pbir-local-materialization-apply-request/v1";
    internal const string RecoveryRequestSchemaVersion = "pbir-local-materialization-recovery-inspect-request/v1";
    internal const string ResponseSchemaVersion = "pbir-local-materialization-response/v1";

    internal static IReadOnlySet<string> SupportedOperations { get; } =
        new HashSet<string>(StringComparer.Ordinal) { PreviewOperation, ApplyOperation, RecoveryOperation };
}

internal sealed record PbirMaterializationRpcResponse(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("validatedPreview")] PbirMaterializationOrchestrationPreviewIdentity? ValidatedPreview,
    [property: JsonPropertyName("transactionId")] string? TransactionId,
    [property: JsonPropertyName("activeTransactionRef")] string? ActiveTransactionRef,
    [property: JsonPropertyName("rollbackAvailable")] bool RollbackAvailable,
    [property: JsonPropertyName("writtenFiles")] IReadOnlyList<PbirDeployableTargetInventoryFile> WrittenFiles,
    [property: JsonPropertyName("lineage")] PbirDeployableMaterializationLineage? Lineage,
    [property: JsonPropertyName("targetStateHash")] string? TargetStateHash,
    [property: JsonPropertyName("resultHash")] string? ResultHash,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<PbirMaterializationRpcDiagnostic> Diagnostics);

internal sealed record PbirMaterializationRpcDiagnostic(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("message")] string Message);
