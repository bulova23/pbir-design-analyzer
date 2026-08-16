using System.Text;
using System.Text.RegularExpressions;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableMaterializationApplyService
{
    private readonly IPbirDeployableMaterializationFileSystem _fileSystem;
    private readonly PbirDeployableMaterializationCanonicalJson _canonicalJson;
    private readonly PbirDeployableMaterializationPathPolicy _pathPolicy;
    private readonly PbirDeployableMaterializationTransactionStore _transactionStore;
    private readonly PbirDeployableMaterializationSchemaValidator _schemaValidator;

    internal PbirDeployableMaterializationApplyService()
        : this(new PbirDeployableMaterializationFileSystem())
    {
    }

    internal PbirDeployableMaterializationApplyService(IPbirDeployableMaterializationFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _canonicalJson = new PbirDeployableMaterializationCanonicalJson();
        _pathPolicy = new PbirDeployableMaterializationPathPolicy(fileSystem);
        _transactionStore = new PbirDeployableMaterializationTransactionStore(fileSystem, _canonicalJson);
        _schemaValidator = new PbirDeployableMaterializationSchemaValidator();
    }

    internal PbirDeployableMaterializationApplyState Apply(
        PbirDeployableArtifact artifact,
        PbirDeployableManifest manifest,
        PbirDeployableMaterializationPreview preview,
        PbirDeployableMaterializationApplyRequest request,
        string outputBaseDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(request);
        var diagnostics = Validate(artifact, manifest, preview, request);
        var paths = _pathPolicy.Resolve(outputBaseDirectory, Path.GetFileName(preview.CanonicalTargetPath), artifact.Files.Select(file => file.RelativePath).ToArray());
        diagnostics.AddRange(paths.Diagnostics);
        if (paths.IsValid &&
            (paths.CanonicalOutputBasePath != preview.CanonicalOutputBasePath ||
             paths.CanonicalTargetPath != preview.CanonicalTargetPath ||
             paths.TargetKey != preview.TargetKey))
        {
            diagnostics.Add(new("PBIRMAT-PATH-007", outputBaseDirectory, "Apply output base does not match the approved preview."));
        }
        if (diagnostics.Count > 0) return Failed(diagnostics);

        PbirDeployableMaterializationTransaction? transaction = null;
        var promoted = false;
        var backupMoved = false;
        string? transactionPath = null;
        byte[]? previousReceiptBytes = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _transactionStore.EnsureControlRoot(paths);
            using var materializationLock = _transactionStore.AcquireLock(paths);
            cancellationToken.ThrowIfCancellationRequested();
            var inventoryService = CreateInventoryService();
            var current = inventoryService.Inventory(paths.CanonicalTargetPath);
            var currentHash = _canonicalJson.ComputeSha256(_canonicalJson.SerializeTargetInventory(current));
            if (currentHash != request.ExpectedTargetStateHash || currentHash != preview.Hashes.TargetStateHash)
            {
                throw new InvalidDataException("Target changed after preview.");
            }
            if (preview.Disposition is not (PbirDeployableMaterializationDisposition.Create or PbirDeployableMaterializationDisposition.ReplaceManaged))
            {
                throw new InvalidDataException("Preview does not authorize apply.");
            }

            var previousReceipt = _transactionStore.ReadCurrentReceipt(paths);
            previousReceiptBytes = _transactionStore.ReadCurrentReceiptBytes(paths);
            transaction = _transactionStore.Begin(paths, preview, request, previousReceipt?.ReceiptHash);
            transactionPath = _transactionStore.TransactionPath(paths, request.TransactionId);
            if (previousReceiptBytes is not null)
            {
                _transactionStore.SavePreviousReceipt(paths, request.TransactionId, previousReceiptBytes);
            }

            var staging = Path.Combine(transactionPath, "staging");
            _fileSystem.CreateDirectory(staging);
            foreach (var file in artifact.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var destination = Path.Combine(staging, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                _fileSystem.CreateDirectory(Path.GetDirectoryName(destination)!);
                _fileSystem.WriteAllBytesCreateNew(destination, Encoding.UTF8.GetBytes(file.Content));
            }
            var staged = inventoryService.Inventory(staging);
            var stagedHash = _canonicalJson.ComputeSha256(_canonicalJson.SerializeTargetInventory(staged));
            var expected = new PbirDeployableTargetInventory(PbirDeployableTargetInventoryContract.SchemaVersionV1, PbirDeployableTargetState.Files,
                artifact.Files.Select(file => new PbirDeployableTargetInventoryFile(file.RelativePath, file.ByteLength, file.HashSha256)).ToArray());
            var expectedHash = _canonicalJson.ComputeSha256(_canonicalJson.SerializeTargetInventory(expected));
            if (stagedHash != expectedHash) throw new InvalidDataException("Staging inventory is incomplete or changed.");
            transaction = _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.StagingWritten, stagedHash, stagingHash: stagedHash);
            transaction = _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.StagingVerified, stagedHash);
            cancellationToken.ThrowIfCancellationRequested();

            var backup = Path.Combine(transactionPath, "backup");
            if (_fileSystem.DirectoryExists(paths.CanonicalTargetPath))
            {
                _fileSystem.MoveDirectory(paths.CanonicalTargetPath, backup);
                backupMoved = true;
                transaction = _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.BackupMoved, currentHash, backupHash: currentHash);
            }
            _fileSystem.MoveDirectory(staging, paths.CanonicalTargetPath);
            promoted = true;
            transaction = _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.TargetPromoted, expectedHash, committedHash: expectedHash);
            var committed = inventoryService.Inventory(paths.CanonicalTargetPath);
            var committedHash = _canonicalJson.ComputeSha256(_canonicalJson.SerializeTargetInventory(committed));
            if (committedHash != expectedHash) throw new InvalidDataException("Published target does not match the validated artifact.");
            transaction = _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.TargetVerified, committedHash);

            var immutableLineage = preview.Lineage.ImmutableLineage.Concat([request.RequestId, request.TransactionId]).ToArray();
            var lineageHash = _canonicalJson.Hash(new { preview.Lineage, immutableLineage });
            var lineage = new PbirDeployableMaterializationLineage(PbirDeployableMaterializationLineageContract.SchemaVersionV1, artifact.Lineage, immutableLineage, lineageHash);
            var provisionalReceipt = new PbirDeployableMaterializationReceipt(
                PbirDeployableMaterializationReceiptContract.SchemaVersionV1, $"receipt:{request.TransactionId}", request.TransactionId,
                request.RequestId, _canonicalJson.Hash(request), preview.PreviewId, preview.Hashes.SelfHash,
                artifact.ArtifactId, artifact.Hashes.ArtifactHash, manifest.ManifestId, manifest.Hashes.ManifestHash,
                paths.TargetKey, paths.CanonicalTargetPath, committedHash, previousReceipt?.ReceiptHash,
                $"rollback:{request.TransactionId}", lineage, string.Empty);
            var receipt = _transactionStore.Rehash(provisionalReceipt);
            _transactionStore.SaveCurrentReceipt(paths, receipt);
            transaction = _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.ReceiptCommitted, receipt.ReceiptHash);
            transaction = _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.Completed, committedHash);

            var inputHash = _canonicalJson.Hash(new { request, preview.Hashes.SelfHash, transaction.TransactionHash });
            var provisionalHashes = new PbirDeployableMaterializationHashes(PbirDeployableMaterializationHashesContract.SchemaVersionV1, inputHash, artifact.Hashes.FileSetHash, committedHash, lineageHash, string.Empty);
            var provisionalResult = new PbirDeployableMaterializationApplyResult(
                PbirDeployableMaterializationApplyResultContract.SchemaVersionV1, $"applyResult:{request.TransactionId}", request.RequestId,
                request.TransactionId, transaction.TransactionHash, preview.PreviewId, preview.Hashes.SelfHash, paths.CanonicalTargetPath,
                committed.Files, current.TargetState, currentHash, committedHash, true, receipt.ReceiptHash, lineage, [],
                PbirDeployableMaterializationDiagnostics.Empty, provisionalHashes);
            var result = provisionalResult with { Hashes = provisionalHashes with { SelfHash = _canonicalJson.Hash(provisionalResult) } };
            return new(result, PbirDeployableMaterializationReadinessState.Applied, PbirDeployableMaterializationDiagnostics.Empty);
        }
        catch (OperationCanceledException)
        {
            if (!TryRestore(paths, preview, request, transaction, transactionPath, promoted, backupMoved, previousReceiptBytes, diagnostics))
            {
                throw new InvalidDataException("Cancellation left transaction state requiring explicit recovery.");
            }
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or PbirDeployableMaterializationTransactionReuseException)
        {
            if (!TryRestore(paths, preview, request, transaction, transactionPath, promoted, backupMoved, previousReceiptBytes, diagnostics))
            {
                return Failed(diagnostics, PbirDeployableMaterializationReadinessState.RecoveryRequired);
            }
            diagnostics.Add(new(
                exception is PbirDeployableMaterializationTransactionReuseException ? "PBIRMAT-TRANSACTION-002" : "PBIRMAT-APPLY-001",
                paths.CanonicalTargetPath,
                exception.Message));
            return Failed(diagnostics);
        }
    }

    private bool TryRestore(
        PbirDeployableMaterializationPathResult paths,
        PbirDeployableMaterializationPreview preview,
        PbirDeployableMaterializationApplyRequest request,
        PbirDeployableMaterializationTransaction? transaction,
        string? transactionPath,
        bool promoted,
        bool backupMoved,
        byte[]? previousReceiptBytes,
        List<PbirDeployableMaterializationDiagnostic> diagnostics)
    {
        try
        {
            if (transactionPath is not null && promoted && _fileSystem.DirectoryExists(paths.CanonicalTargetPath))
            {
                _fileSystem.MoveDirectory(paths.CanonicalTargetPath, Path.Combine(transactionPath, "quarantine"));
            }
            if (transactionPath is not null && backupMoved)
            {
                _fileSystem.MoveDirectory(Path.Combine(transactionPath, "backup"), paths.CanonicalTargetPath);
            }
            var currentReceiptPath = Path.Combine(paths.TargetControlPath, "current-receipt.json");
            if (previousReceiptBytes is not null)
            {
                _transactionStore.RestoreCurrentReceipt(paths, previousReceiptBytes, request.TransactionId);
            }
            else if (_fileSystem.FileExists(currentReceiptPath))
            {
                _transactionStore.RemoveCurrentReceipt(paths);
            }
            if (transaction is not null)
            {
                if (promoted || backupMoved)
                {
                    transaction = _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.Restoring, preview.Hashes.TargetStateHash);
                    _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.Restored, preview.Hashes.TargetStateHash);
                }
                else
                {
                    _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.Aborted, preview.Hashes.TargetStateHash);
                }
            }
            return true;
        }
        catch
        {
            diagnostics.Add(new("PBIRMAT-RECOVERY-001", paths.CanonicalTargetPath, "Automatic restoration could not be proven; explicit recovery is required."));
            return false;
        }
    }

    private List<PbirDeployableMaterializationDiagnostic> Validate(
        PbirDeployableArtifact artifact, PbirDeployableManifest manifest,
        PbirDeployableMaterializationPreview preview, PbirDeployableMaterializationApplyRequest request)
    {
        var diagnostics = new List<PbirDeployableMaterializationDiagnostic>();
        var phase29 = new PbirDeployableSerializerValidator().ValidateOutput(artifact, manifest);
        if (!phase29.IsValid) diagnostics.Add(new("PBIRMAT-PHASE29-001", "artifact", "Phase 29 postflight validation failed."));
        diagnostics.AddRange(_schemaValidator.Validate(artifact));
        if (request.SchemaVersion != PbirDeployableMaterializationApplyRequestContract.SchemaVersionV1 || !request.ApplyApproved || !request.RollbackRequired ||
            !request.ExecutionPolicy.FilesystemMutationAllowed || request.ExecutionPolicy.HasExternalAuthority)
            diagnostics.Add(new("PBIRMAT-BOUNDARY-002", "request", "Apply authority is incomplete or exceeds local filesystem mutation."));
        if (string.IsNullOrEmpty(request.TransactionId) ||
            !Regex.IsMatch(request.TransactionId, "^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant))
            diagnostics.Add(new("PBIRMAT-TRANSACTION-001", "transactionId", "Transaction id is unsafe."));
        var previewHash = _canonicalJson.Hash(preview with { Hashes = preview.Hashes with { SelfHash = string.Empty } });
        if (preview.Hashes.SelfHash != previewHash || request.PreviewRef != preview.PreviewId || request.PreviewHash != preview.Hashes.SelfHash ||
            request.ArtifactRef != artifact.ArtifactId || request.ArtifactHash != artifact.Hashes.ArtifactHash ||
            request.ManifestRef != manifest.ManifestId || request.ManifestHash != manifest.Hashes.ManifestHash)
            diagnostics.Add(new("PBIRMAT-REFERENCE-002", "request", "Apply references or hashes do not match the unchanged preview and Phase 29 output."));
        return diagnostics;
    }

    private PbirDeployableMaterializationPreviewService CreateInventoryService() =>
        new(new PbirDeployableMaterializationSafetyGate(), _canonicalJson, _fileSystem);

    private static PbirDeployableMaterializationApplyState Failed(
        IEnumerable<PbirDeployableMaterializationDiagnostic> diagnostics,
        PbirDeployableMaterializationReadinessState readiness = PbirDeployableMaterializationReadinessState.Blocked) =>
        new(null, readiness, new(PbirDeployableMaterializationDiagnosticsContract.SchemaVersionV1,
            PbirDeployableMaterializationSafetyGate.Order(diagnostics)));
}
