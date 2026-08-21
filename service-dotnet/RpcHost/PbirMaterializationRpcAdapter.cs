using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.RpcHost;

internal sealed class PbirMaterializationRpcAdapter
{
    private readonly PbirMaterializationOrchestrationService _orchestration;

    internal PbirMaterializationRpcAdapter(PbirMaterializationOrchestrationService orchestration)
    {
        _orchestration = orchestration ?? throw new ArgumentNullException(nameof(orchestration));
    }

    internal static PbirMaterializationRpcAdapter CreateForTests() =>
        new(new PbirMaterializationOrchestrationService());

    internal PbirMaterializationRpcResponse ValidateForTests(JsonElement payload) =>
        TryRead(payload, PbirMaterializationRpcContract.PreviewOperation, CancellationToken.None, out _, out var response)
            ? response!
            : response!;

    internal PbirMaterializationRpcResponse MapForTests(
        string operation,
        PbirMaterializationOrchestrationResult result) => Map(operation, result);

    internal Task<PbirMaterializationRpcResponse> HandleAsync(
        string operation,
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        if (operation == PbirMaterializationRpcContract.RollbackOperation)
        {
            return Task.FromResult(HandleRollback(payload, cancellationToken));
        }

        if (!TryRead(payload, operation, cancellationToken, out var request, out var invalid))
        {
            return Task.FromResult(invalid!);
        }

        try
        {
            var result = request switch
            {
                PbirMaterializationOrchestrationPreviewRequest preview => _orchestration.Preview(preview, cancellationToken),
                PbirMaterializationOrchestrationApplyRequest apply => _orchestration.Apply(apply, cancellationToken),
                PbirMaterializationOrchestrationRecoveryRequest recovery => _orchestration.InspectRecovery(recovery, cancellationToken),
                _ => throw new InvalidOperationException("Unsupported adapter request.")
            };
            return Task.FromResult(Map(operation, result));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            var requestId = ReadString(payload, "requestId") ?? string.Empty;
            return Task.FromResult(PbirMaterializationRpcValidation.Fault(requestId, operation));
        }
    }

    private PbirMaterializationRpcResponse HandleRollback(JsonElement payload, CancellationToken cancellationToken)
    {
        var requestId = ReadString(payload, "requestId") ?? string.Empty;
        var allowed = new[] { "schemaVersion", "requestId", "operation", "outputBaseDirectory", "targetDirectoryName", "targetKey", "transactionId", "expectedTransactionHash", "expectedCurrentReceiptHash", "expectedCurrentTargetStateHash", "rollbackApproved", "executionPolicy" };
        if (payload.ValueKind != JsonValueKind.Object ||
            PbirMaterializationRpcValidation.HasDuplicateProperties(payload) ||
            payload.EnumerateObject().Any(property => !allowed.Contains(property.Name, StringComparer.Ordinal)))
        {
            return PbirMaterializationRpcValidation.Invalid(requestId, PbirMaterializationRpcContract.RollbackOperation, "PBIR-RPC-ROLLBACK-001", "request");
        }

        try
        {
            var request = JsonSerializer.Deserialize<PbirMaterializationRpcRollbackRequest>(payload.GetRawText(), PbirMaterializationRpcValidation.SerializerOptions);
            if (request is null || request.SchemaVersion != PbirMaterializationRpcContract.RollbackRequestSchemaVersion ||
                request.Operation != PbirMaterializationRpcContract.RollbackOperation ||
                !PbirMaterializationRpcValidation.IsSafeIdentifier(request.RequestId) ||
                !PbirMaterializationRpcValidation.IsSafeTransactionIdentifier(request.TransactionId) ||
                !request.RollbackApproved || request.ExecutionPolicy.HasExternalAuthority ||
                !request.ExecutionPolicy.FilesystemMutationAllowed || string.IsNullOrWhiteSpace(request.OutputBaseDirectory) ||
                !Path.IsPathFullyQualified(request.OutputBaseDirectory) || string.IsNullOrWhiteSpace(request.TargetDirectoryName) ||
                string.IsNullOrWhiteSpace(request.TargetKey) || string.IsNullOrWhiteSpace(request.ExpectedTransactionHash) ||
                string.IsNullOrWhiteSpace(request.ExpectedCurrentTargetStateHash))
            {
                return PbirMaterializationRpcValidation.Invalid(requestId, PbirMaterializationRpcContract.RollbackOperation, "PBIR-RPC-ROLLBACK-002", "request");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var state = _orchestration.Rollback(new(
                PbirDeployableMaterializationRollbackRequestContract.SchemaVersionV1,
                request.RequestId, request.TransactionId, request.TargetDirectoryName, request.TargetKey,
                request.ExpectedTransactionHash, request.ExpectedCurrentReceiptHash,
                request.ExpectedCurrentTargetStateHash, request.RollbackApproved, request.ExecutionPolicy),
                request.OutputBaseDirectory);
            var result = state.Result;
            return new(
                PbirMaterializationRpcContract.ResponseSchemaVersion,
                request.RequestId,
                PbirMaterializationRpcContract.RollbackOperation,
                state.Readiness == PbirDeployableMaterializationReadinessState.RolledBack ? "rolled-back" : "failure",
                null, result?.TransactionId, null, false, [], null,
                result?.RestoredTargetStateHash, result?.Hashes.SelfHash,
                state.Diagnostics.Items.Select(item => new PbirMaterializationRpcDiagnostic(item.Code, item.Path, item.Message)).ToArray());
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return PbirMaterializationRpcValidation.Fault(requestId, PbirMaterializationRpcContract.RollbackOperation); }
    }

    private static bool TryRead(
        JsonElement payload,
        string operation,
        CancellationToken cancellationToken,
        out object? request,
        out PbirMaterializationRpcResponse? invalid)
    {
        request = null;
        invalid = null;
        var requestId = ReadString(payload, "requestId") ?? string.Empty;
        if (payload.ValueKind != JsonValueKind.Object ||
            !PbirMaterializationRpcContract.SupportedOperations.Contains(operation) ||
            PbirMaterializationRpcValidation.HasDuplicateProperties(payload))
        {
            invalid = PbirMaterializationRpcValidation.Invalid(requestId, operation, "PBIR-RPC-REQUEST-001", "request");
            return false;
        }

        var allowed = operation == PbirMaterializationRpcContract.ApplyOperation
            ? new[] { "schemaVersion", "requestId", "operation", "input", "validatedPreview", "transactionId", "applyApproved" }
            : new[] { "schemaVersion", "requestId", "operation", "input", "previewRequestId" };
        if (payload.EnumerateObject().Any(property => !allowed.Contains(property.Name, StringComparer.Ordinal)))
        {
            invalid = PbirMaterializationRpcValidation.Invalid(requestId, operation, "PBIR-RPC-REQUEST-002", "request");
            return false;
        }

        var expectedVersion = operation switch
        {
            PbirMaterializationRpcContract.PreviewOperation => PbirMaterializationRpcContract.PreviewRequestSchemaVersion,
            PbirMaterializationRpcContract.ApplyOperation => PbirMaterializationRpcContract.ApplyRequestSchemaVersion,
            _ => PbirMaterializationRpcContract.RecoveryRequestSchemaVersion
        };
        if (!TryGetString(payload, "schemaVersion", out var schemaVersion) || schemaVersion != expectedVersion ||
            !PbirMaterializationRpcValidation.IsSafeIdentifier(requestId) ||
            !TryGetString(payload, "operation", out var requestedOperation) || requestedOperation != operation ||
            !payload.TryGetProperty("input", out var inputElement) || inputElement.ValueKind != JsonValueKind.Object)
        {
            invalid = PbirMaterializationRpcValidation.Invalid(requestId, operation, "PBIR-RPC-REQUEST-003", "request");
            return false;
        }

        try
        {
            var input = JsonSerializer.Deserialize<PbirMaterializationOrchestrationInput>(
                inputElement.GetRawText(), PbirMaterializationRpcValidation.SerializerOptions);
            if (input is null || !PbirMaterializationRpcValidation.IsSafeDestination(input) ||
                !PbirMaterializationRpcValidation.IsSupportedMaterializationInput(input))
            {
                invalid = PbirMaterializationRpcValidation.Invalid(requestId, operation, "PBIR-RPC-PATH-001", "input");
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (operation == PbirMaterializationRpcContract.PreviewOperation)
            {
                if (!PbirMaterializationRpcValidation.IsReadOnlyInput(input))
                {
                    invalid = PbirMaterializationRpcValidation.Invalid(requestId, operation, "PBIR-RPC-POLICY-001", "input");
                    return false;
                }
                request = new PbirMaterializationOrchestrationPreviewRequest(
                    PbirMaterializationOrchestrationPreviewRequestContract.SchemaVersionV1,
                    requestId, "preview", input);
            }
            else if (operation == PbirMaterializationRpcContract.ApplyOperation)
            {
                var hasPreview = payload.TryGetProperty("validatedPreview", out var previewElement);
                var hasTransaction = payload.TryGetProperty("transactionId", out var transactionElement);
                var hasApproval = payload.TryGetProperty("applyApproved", out var approvedElement);
                var approved = hasApproval && approvedElement.ValueKind == JsonValueKind.True;
                var transactionId = hasTransaction && transactionElement.ValueKind == JsonValueKind.String
                    ? transactionElement.GetString()
                    : null;
                var preview = hasPreview && TryDeserialize(
                    previewElement, out PbirMaterializationOrchestrationPreviewIdentity? parsedPreview)
                    ? parsedPreview
                    : null;
                if (!hasPreview || !hasTransaction || !hasApproval || !approved ||
                    !PbirMaterializationRpcValidation.IsSafeTransactionIdentifier(transactionId) ||
                    !PbirMaterializationRpcValidation.IsReadOnlyInput(input) || preview is null)
                {
                    invalid = PbirMaterializationRpcValidation.Invalid(requestId, operation, "PBIR-RPC-APPLY-001", "request");
                    return false;
                }
                request = new PbirMaterializationOrchestrationApplyRequest(
                    PbirMaterializationOrchestrationApplyRequestContract.SchemaVersionV1,
                    requestId, "apply", input, preview, transactionId!, true);
            }
            else
            {
                var previewRequestId = payload.TryGetProperty("previewRequestId", out var previewRequestElement) &&
                    previewRequestElement.ValueKind == JsonValueKind.String
                    ? previewRequestElement.GetString()
                    : null;
                if (!payload.TryGetProperty("previewRequestId", out _) ||
                    previewRequestElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(previewRequestElement.GetString()) ||
                    !PbirMaterializationRpcValidation.IsSafeIdentifier(previewRequestId) ||
                    !PbirMaterializationRpcValidation.IsReadOnlyInput(input))
                {
                    invalid = PbirMaterializationRpcValidation.Invalid(requestId, operation, "PBIR-RPC-RECOVERY-001", "previewRequestId");
                    return false;
                }
                request = new PbirMaterializationOrchestrationRecoveryRequest(
                    PbirMaterializationOrchestrationRecoveryRequestContract.SchemaVersionV1,
                    requestId, "inspectRecovery", input, previewRequestId!);
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException)
        {
            invalid = PbirMaterializationRpcValidation.Invalid(requestId, operation, "PBIR-RPC-REQUEST-004", "input");
            return false;
        }
        catch (NotSupportedException)
        {
            invalid = PbirMaterializationRpcValidation.Invalid(requestId, operation, "PBIR-RPC-REQUEST-005", "input");
            return false;
        }
        catch (Exception)
        {
            invalid = PbirMaterializationRpcValidation.Invalid(requestId, operation, "PBIR-RPC-REQUEST-006", "input");
            return false;
        }
    }

    private static bool TryDeserialize<T>(JsonElement value, out T? result)
    {
        try
        {
            result = JsonSerializer.Deserialize<T>(value.GetRawText(), PbirMaterializationRpcValidation.SerializerOptions);
            return result is not null;
        }
        catch (JsonException)
        {
            result = default;
            return false;
        }
    }

    private static PbirMaterializationRpcResponse Map(
        string operation,
        PbirMaterializationOrchestrationResult result)
    {
        var diagnostics = result.Diagnostics.Items
            .Select(item => new PbirMaterializationRpcDiagnostic(item.Code, item.Field, item.Message))
            .ToArray();
        return new(
            PbirMaterializationRpcContract.ResponseSchemaVersion,
            result.RequestId,
            operation,
            result.Outcome switch
            {
                PbirMaterializationOrchestrationOutcome.Absent => "absent-destination",
                PbirMaterializationOrchestrationOutcome.Empty => "empty-destination",
                PbirMaterializationOrchestrationOutcome.ExactMatch => "exact-match",
                PbirMaterializationOrchestrationOutcome.ManagedReplacement => "managed-replacement",
                PbirMaterializationOrchestrationOutcome.Conflict => "conflict",
                PbirMaterializationOrchestrationOutcome.RecoveryRequired => "recovery-required",
                PbirMaterializationOrchestrationOutcome.Applied => "applied",
                PbirMaterializationOrchestrationOutcome.StalePreview => "stale-preview",
                PbirMaterializationOrchestrationOutcome.InvalidRequest => "invalid-request",
                PbirMaterializationOrchestrationOutcome.UnsafeDestination => "unsafe-destination",
                PbirMaterializationOrchestrationOutcome.UnsupportedOperation => "unsupported-operation",
                PbirMaterializationOrchestrationOutcome.SchemaFailure => "schema-failure",
                PbirMaterializationOrchestrationOutcome.TransactionReused => "transaction-reused",
                PbirMaterializationOrchestrationOutcome.Cancelled => "cancelled",
                PbirMaterializationOrchestrationOutcome.Failure => "failure",
                _ => "failure"
            },
            result.ValidatedPreview,
            result.Outcome == PbirMaterializationOrchestrationOutcome.Applied ? result.TransactionId : null,
            result.ActiveTransactionRef,
            result.RollbackAvailable,
            result.WrittenFiles.Select(file => new PbirDeployableTargetInventoryFile(
                file.RelativePath, file.ByteLength, file.HashSha256)).ToArray(),
            result.Lineage,
            result.TargetStateHash,
            result.ResultHash,
            diagnostics,
            result.TransactionHash,
            result.CurrentReceiptHash,
            result.TargetKey);
    }

    private static string? ReadString(JsonElement value, string propertyName) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String ? property.GetString() : null;

    private static bool TryGetString(JsonElement value, string propertyName, out string? result)
    {
        result = ReadString(value, propertyName);
        return result is not null;
    }
}
