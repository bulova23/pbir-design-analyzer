using System.Text.RegularExpressions;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirMaterializationOrchestrationService
{
    private static readonly Regex SafeIdentifier = new("^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$", RegexOptions.CultureInvariant);
    private static readonly Regex SafeTransactionIdentifier = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant);
    private readonly PbirDeployableSerializerService _serializer;
    private readonly PbirDeployableMaterializationPreviewService _previewService;
    private readonly PbirDeployableMaterializationApplyService _applyService;
    private readonly PbirDeployableMaterializationRollbackService _rollbackService;

    internal PbirMaterializationOrchestrationService()
        : this(
            new PbirDeployableSerializerService(),
            new PbirDeployableMaterializationPreviewService(),
            new PbirDeployableMaterializationApplyService(),
            new PbirDeployableMaterializationRollbackService())
    {
    }

    internal PbirMaterializationOrchestrationService(
        PbirDeployableSerializerService serializer,
        PbirDeployableMaterializationPreviewService previewService,
        PbirDeployableMaterializationApplyService applyService,
        PbirDeployableMaterializationRollbackService? rollbackService = null)
    {
        _serializer = serializer;
        _previewService = previewService;
        _applyService = applyService;
        _rollbackService = rollbackService ?? new PbirDeployableMaterializationRollbackService();
    }

    internal PbirDeployableMaterializationRollbackState Rollback(
        PbirDeployableMaterializationRollbackRequest request,
        string outputBaseDirectory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _rollbackService.Rollback(request, outputBaseDirectory);
    }

    internal PbirMaterializationOrchestrationResult Preview(
        PbirMaterializationOrchestrationPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Failed(string.Empty, PbirMaterializationOrchestrationOutcome.InvalidRequest, "PBIR31-REQUEST-001", "request", "The preview request is invalid.");
        }

        if (request.SchemaVersion != PbirMaterializationOrchestrationPreviewRequestContract.SchemaVersionV1 ||
            !SafeIdentifier.IsMatch(request.RequestId ?? string.Empty) || request.Input is null)
        {
            return Failed(request.RequestId, PbirMaterializationOrchestrationOutcome.InvalidRequest, "PBIR31-REQUEST-001", "request", "The preview request is invalid.");
        }
        var requestId = request.RequestId!;
        if (request.RequestedOperation != "preview")
        {
            return Failed(requestId, PbirMaterializationOrchestrationOutcome.UnsupportedOperation, "PBIR31-OPERATION-001", "requestedOperation", "The requested operation is not supported.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var serialized = Serialize(request.Input, cancellationToken);
            if (serialized.Artifact is null || serialized.Manifest is null ||
                serialized.Readiness != PbirDeployableSerializerReadinessState.Serialized)
            {
                return Failed(requestId, ClassifySerializerFailure(serialized), "PBIR31-SERIALIZER-001", "artifact", "Canonical PBIR serialization did not produce a valid artifact.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var phase30 = _previewService.CreatePreview(
                serialized.Artifact,
                serialized.Manifest,
                CreatePhase30PreviewRequest(requestId, request.Input.TargetDirectoryName, serialized.Artifact, serialized.Manifest),
                request.Input.OutputBaseDirectory);
            cancellationToken.ThrowIfCancellationRequested();
            if (phase30.Preview is null)
            {
                return FailedFromPhase30(requestId, phase30.Diagnostics);
            }

            return FromPreview(requestId, phase30.Preview);
        }
        catch (OperationCanceledException)
        {
            return Failed(requestId, PbirMaterializationOrchestrationOutcome.Cancelled, "PBIR31-CANCELLED-001", "request", "The operation was cancelled safely.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            return Failed(requestId, PbirMaterializationOrchestrationOutcome.Failure, "PBIR31-FAILURE-001", "destination", "The local PBIR operation failed safely.");
        }
    }

    internal PbirMaterializationOrchestrationResult Apply(
        PbirMaterializationOrchestrationApplyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Failed(string.Empty, PbirMaterializationOrchestrationOutcome.InvalidRequest, "PBIR31-REQUEST-002", "request", "The apply request is invalid.");
        }
        if (request.SchemaVersion != PbirMaterializationOrchestrationApplyRequestContract.SchemaVersionV1 ||
            !SafeIdentifier.IsMatch(request.RequestId ?? string.Empty) || request.Input is null ||
            request.ValidatedPreview is null || !request.ApplyApproved)
        {
            return Failed(request.RequestId, PbirMaterializationOrchestrationOutcome.InvalidRequest, "PBIR31-REQUEST-002", "request", "The apply request is invalid.");
        }
        var requestId = request.RequestId!;
        var transactionId = request.TransactionId!;
        var previewRequestId = request.ValidatedPreview.PreviewRequestId!;
        if (request.RequestedOperation != "apply")
        {
            return Failed(requestId, PbirMaterializationOrchestrationOutcome.UnsupportedOperation, "PBIR31-OPERATION-001", "requestedOperation", "The requested operation is not supported.");
        }
        if (!SafeTransactionIdentifier.IsMatch(request.TransactionId ?? string.Empty))
        {
            return Failed(requestId, PbirMaterializationOrchestrationOutcome.InvalidRequest, "PBIR31-TRANSACTION-001", "transactionId", "A fresh safe transaction ID is required.");
        }
        if (request.ValidatedPreview.SchemaVersion != PbirMaterializationOrchestrationPreviewIdentityContract.SchemaVersionV1 ||
            !SafeIdentifier.IsMatch(request.ValidatedPreview.PreviewRequestId ?? string.Empty))
        {
            return Failed(requestId, PbirMaterializationOrchestrationOutcome.InvalidRequest, "PBIR31-PREVIEW-001", "validatedPreview", "The validated preview identity is invalid.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var serialized = Serialize(request.Input, cancellationToken);
            if (serialized.Artifact is null || serialized.Manifest is null ||
                serialized.Readiness != PbirDeployableSerializerReadinessState.Serialized)
            {
                return Failed(requestId, ClassifySerializerFailure(serialized), "PBIR31-SERIALIZER-001", "artifact", "Canonical PBIR serialization did not produce a valid artifact.");
            }

            var currentState = _previewService.CreatePreview(
                serialized.Artifact,
                serialized.Manifest,
                CreatePhase30PreviewRequest(previewRequestId, request.Input.TargetDirectoryName, serialized.Artifact, serialized.Manifest),
                request.Input.OutputBaseDirectory);
            cancellationToken.ThrowIfCancellationRequested();
            if (currentState.Preview is null)
            {
                return FailedFromPhase30(requestId, currentState.Diagnostics);
            }

            var currentIdentity = CreateIdentity(currentState.Preview);
            if (currentIdentity != request.ValidatedPreview)
            {
                return Failed(requestId, PbirMaterializationOrchestrationOutcome.StalePreview, "PBIR31-PREVIEW-002", "validatedPreview", "The validated preview is stale.");
            }
            if (currentState.Preview.Disposition == PbirDeployableMaterializationDisposition.BlockedConflict)
            {
                return FromPreview(requestId, currentState.Preview);
            }
            if (currentState.Preview.Disposition == PbirDeployableMaterializationDisposition.RecoveryRequired)
            {
                return FromPreview(requestId, currentState.Preview);
            }
            if (currentState.Preview.Disposition == PbirDeployableMaterializationDisposition.NoChanges)
            {
                return FromPreview(requestId, currentState.Preview);
            }

            var applyRequest = new PbirDeployableMaterializationApplyRequest(
                PbirDeployableMaterializationApplyRequestContract.SchemaVersionV1,
                requestId,
                transactionId,
                currentState.Preview.PreviewId,
                currentState.Preview.Hashes.SelfHash,
                serialized.Artifact.ArtifactId,
                serialized.Artifact.Hashes.ArtifactHash,
                serialized.Manifest.ManifestId,
                serialized.Manifest.Hashes.ManifestHash,
                currentState.Preview.Hashes.TargetStateHash,
                ApplyApproved: true,
                RollbackRequired: true,
                PbirDeployableMaterializationExecutionPolicy.LocalMutationOnly);
            var applied = _applyService.Apply(
                serialized.Artifact,
                serialized.Manifest,
                currentState.Preview,
                applyRequest,
                request.Input.OutputBaseDirectory,
                cancellationToken);
            if (applied.Result is null)
            {
                return FailedFromPhase30(requestId, applied.Diagnostics);
            }

            return new(
                PbirMaterializationOrchestrationResultContract.SchemaVersionV1,
                requestId,
                PbirMaterializationOrchestrationOutcome.Applied,
                currentIdentity,
                applied.Result.TransactionId,
                null,
                applied.Result.RollbackAvailable,
                applied.Result.WrittenFiles,
                applied.Result.Lineage,
                applied.Result.CommittedTargetStateHash,
                applied.Result.Hashes.SelfHash,
                PbirMaterializationOrchestrationDiagnostics.Empty,
                applied.Result.TransactionHash,
                applied.Result.CurrentReceiptHash,
                currentState.Preview.TargetKey);
        }
        catch (OperationCanceledException)
        {
            return Failed(requestId, PbirMaterializationOrchestrationOutcome.Cancelled, "PBIR31-CANCELLED-001", "request", "The operation was cancelled safely.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            return Failed(requestId, PbirMaterializationOrchestrationOutcome.Failure, "PBIR31-FAILURE-001", "destination", "The local PBIR operation failed safely.");
        }
    }

    internal PbirMaterializationOrchestrationResult InspectRecovery(
        PbirMaterializationOrchestrationRecoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null ||
            request.SchemaVersion != PbirMaterializationOrchestrationRecoveryRequestContract.SchemaVersionV1 ||
            !SafeIdentifier.IsMatch(request.RequestId ?? string.Empty) || request.Input is null ||
            !SafeIdentifier.IsMatch(request.PreviewRequestId ?? string.Empty))
        {
            return Failed(request?.RequestId ?? string.Empty, PbirMaterializationOrchestrationOutcome.InvalidRequest, "PBIR31-REQUEST-003", "request", "The recovery inspection request is invalid.");
        }
        if (request.RequestedOperation != "inspectRecovery")
        {
            return Failed(request.RequestId, PbirMaterializationOrchestrationOutcome.UnsupportedOperation, "PBIR31-OPERATION-001", "requestedOperation", "The requested operation is not supported.");
        }
        var requestId = request.RequestId!;
        var previewRequestId = request.PreviewRequestId!;

        var preview = Preview(new(
            PbirMaterializationOrchestrationPreviewRequestContract.SchemaVersionV1,
            previewRequestId,
            "preview",
            request.Input), cancellationToken);
        return preview with { RequestId = requestId };
    }

    private PbirDeployableSerializerState Serialize(PbirMaterializationOrchestrationInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var serialized = _serializer.CreateArtifacts(input.IrState, input.SerializerRequest, input.DeployableSerializerRequest);
        cancellationToken.ThrowIfCancellationRequested();
        return serialized;
    }

    private static PbirDeployableMaterializationPreviewRequest CreatePhase30PreviewRequest(
        string requestId,
        string targetDirectoryName,
        PbirDeployableArtifact artifact,
        PbirDeployableManifest manifest) =>
        new(
            PbirDeployableMaterializationPreviewRequestContract.SchemaVersionV1,
            requestId,
            artifact.ArtifactId,
            artifact.Hashes.ArtifactHash,
            manifest.ManifestId,
            manifest.Hashes.ManifestHash,
            targetDirectoryName,
            "preview",
            PbirDeployableMaterializationExecutionPolicy.PreviewOnly);

    private static PbirMaterializationOrchestrationResult FromPreview(
        string requestId,
        PbirDeployableMaterializationPreview preview)
    {
        var outcome = preview.Disposition switch
        {
            PbirDeployableMaterializationDisposition.Create when preview.TargetInventory.TargetState == PbirDeployableTargetState.Absent => PbirMaterializationOrchestrationOutcome.Absent,
            PbirDeployableMaterializationDisposition.Create => PbirMaterializationOrchestrationOutcome.Empty,
            PbirDeployableMaterializationDisposition.NoChanges => PbirMaterializationOrchestrationOutcome.ExactMatch,
            PbirDeployableMaterializationDisposition.ReplaceManaged => PbirMaterializationOrchestrationOutcome.ManagedReplacement,
            PbirDeployableMaterializationDisposition.BlockedConflict => PbirMaterializationOrchestrationOutcome.Conflict,
            PbirDeployableMaterializationDisposition.RecoveryRequired => PbirMaterializationOrchestrationOutcome.RecoveryRequired,
            _ => PbirMaterializationOrchestrationOutcome.Failure
        };
        return new(
            PbirMaterializationOrchestrationResultContract.SchemaVersionV1,
            requestId,
            outcome,
            CreateIdentity(preview),
            null,
            preview.ActiveTransactionRef,
            preview.RollbackAvailable,
            [],
            preview.Lineage,
            preview.Hashes.TargetStateHash,
            preview.Hashes.SelfHash,
            PbirMaterializationOrchestrationDiagnostics.Empty);
    }

    private static PbirMaterializationOrchestrationPreviewIdentity CreateIdentity(PbirDeployableMaterializationPreview preview) =>
        new(
            PbirMaterializationOrchestrationPreviewIdentityContract.SchemaVersionV1,
            preview.RequestRef,
            preview.PreviewId,
            preview.Hashes.SelfHash,
            preview.Hashes.TargetStateHash,
            preview.ArtifactRef,
            preview.ArtifactHash,
            preview.ManifestRef,
            preview.ManifestHash);

    private static PbirMaterializationOrchestrationOutcome ClassifySerializerFailure(PbirDeployableSerializerState state) =>
        state.Validation.SchemaContractResults.Count > 0
            ? PbirMaterializationOrchestrationOutcome.SchemaFailure
            : PbirMaterializationOrchestrationOutcome.InvalidRequest;

    private static PbirMaterializationOrchestrationResult FailedFromPhase30(
        string requestId,
        PbirDeployableMaterializationDiagnostics diagnostics)
    {
        var outcome = diagnostics.Items.Any(item => item.Code.StartsWith("PBIRMAT-PATH-", StringComparison.Ordinal))
            ? PbirMaterializationOrchestrationOutcome.UnsafeDestination
            : diagnostics.Items.Any(item => item.Code.StartsWith("PBIRMAT-SCHEMA-", StringComparison.Ordinal))
                ? PbirMaterializationOrchestrationOutcome.SchemaFailure
                : diagnostics.Items.Any(item => item.Code == "PBIRMAT-TRANSACTION-002")
                    ? PbirMaterializationOrchestrationOutcome.TransactionReused
                    : PbirMaterializationOrchestrationOutcome.Failure;
        var failure = Failed(requestId, outcome, "PBIR31-PHASE30-001", "destination", "The local PBIR operation was rejected safely.");
        return failure with
        {
            Diagnostics = new PbirMaterializationOrchestrationDiagnostics(
                PbirMaterializationOrchestrationDiagnosticsContract.SchemaVersionV1,
                diagnostics.Items
                    .Select(item => new PbirMaterializationOrchestrationDiagnostic(item.Code, item.Path, item.Message))
                    .ToArray())
        };
    }

    private static PbirMaterializationOrchestrationResult Failed(
        string? requestId,
        PbirMaterializationOrchestrationOutcome outcome,
        string code,
        string field,
        string message) =>
        new(
            PbirMaterializationOrchestrationResultContract.SchemaVersionV1,
            requestId ?? string.Empty,
            outcome,
            null,
            null,
            null,
            false,
            [],
            null,
            null,
            null,
            new(PbirMaterializationOrchestrationDiagnosticsContract.SchemaVersionV1, [new(code, field, message)]));
}
