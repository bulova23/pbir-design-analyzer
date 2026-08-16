using PowerBIModelingService.Services.Discovery.Models;
using System.Text.RegularExpressions;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableMaterializationRollbackService
{
    private readonly IPbirDeployableMaterializationFileSystem _fileSystem;
    private readonly PbirDeployableMaterializationCanonicalJson _canonicalJson;
    private readonly PbirDeployableMaterializationPathPolicy _pathPolicy;
    private readonly PbirDeployableMaterializationTransactionStore _transactionStore;

    internal PbirDeployableMaterializationRollbackService()
        : this(new PbirDeployableMaterializationFileSystem())
    {
    }

    internal PbirDeployableMaterializationRollbackService(IPbirDeployableMaterializationFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _canonicalJson = new PbirDeployableMaterializationCanonicalJson();
        _pathPolicy = new PbirDeployableMaterializationPathPolicy(fileSystem);
        _transactionStore = new PbirDeployableMaterializationTransactionStore(fileSystem, _canonicalJson);
    }

    internal PbirDeployableMaterializationRollbackState Rollback(
        PbirDeployableMaterializationRollbackRequest request,
        string outputBaseDirectory)
    {
        var diagnostics = new List<PbirDeployableMaterializationDiagnostic>();
        if (request.SchemaVersion != PbirDeployableMaterializationRollbackRequestContract.SchemaVersionV1 ||
            !request.RollbackApproved || !request.ExecutionPolicy.FilesystemMutationAllowed || request.ExecutionPolicy.HasExternalAuthority)
        {
            diagnostics.Add(new("PBIRMAT-ROLLBACK-BOUNDARY-001", "request", "Rollback authority is incomplete or exceeds local filesystem mutation."));
            return Failed(diagnostics);
        }
        if (string.IsNullOrEmpty(request.TransactionId) ||
            !Regex.IsMatch(request.TransactionId, "^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant))
        {
            diagnostics.Add(new("PBIRMAT-TRANSACTION-001", "transactionId", "Transaction id is unsafe."));
            return Failed(diagnostics);
        }

        var paths = _pathPolicy.Resolve(outputBaseDirectory, request.TargetDirectoryName, []);
        diagnostics.AddRange(paths.Diagnostics);
        if (!paths.IsValid || paths.TargetKey != request.TargetKey) return Failed(diagnostics.Append(new("PBIRMAT-ROLLBACK-PATH-001", "targetKey", "Rollback target key does not match the authorized target.")));

        try
        {
            _transactionStore.ValidateControlRootIfPresent(paths);
            if (!_fileSystem.DirectoryExists(paths.TargetControlPath))
                throw new InvalidDataException("No Phase 30 transaction state exists for this target.");
            using var materializationLock = _transactionStore.AcquireLock(paths);
            var transaction = _transactionStore.ReadTransaction(paths, request.TransactionId);
            var receipt = _transactionStore.ReadCurrentReceipt(paths);
            var isCommittedRollback = transaction.Phase == PbirDeployableMaterializationJournalPhase.Completed;
            var isRecoverableInterrupted = transaction.Phase is
                PbirDeployableMaterializationJournalPhase.Initialized or
                PbirDeployableMaterializationJournalPhase.StagingWritten or
                PbirDeployableMaterializationJournalPhase.StagingVerified or
                PbirDeployableMaterializationJournalPhase.BackupMoved or
                PbirDeployableMaterializationJournalPhase.TargetPromoted or
                PbirDeployableMaterializationJournalPhase.TargetVerified or
                PbirDeployableMaterializationJournalPhase.ReceiptCommitted or
                PbirDeployableMaterializationJournalPhase.RecoveryRequired;
            if (transaction.TransactionHash != request.ExpectedTransactionHash || transaction.TargetKey != paths.TargetKey ||
                transaction.CanonicalTargetPath != paths.CanonicalTargetPath || (!isCommittedRollback && !isRecoverableInterrupted))
            {
                throw new InvalidDataException("Rollback does not identify the current transaction.");
            }
            if (isCommittedRollback && (receipt is null || receipt.TransactionId != request.TransactionId || receipt.ReceiptHash != request.ExpectedCurrentReceiptHash))
            {
                throw new InvalidDataException("Committed rollback requires the exact current receipt.");
            }
            if (!isCommittedRollback && request.ExpectedCurrentReceiptHash is not null && receipt?.ReceiptHash != request.ExpectedCurrentReceiptHash)
            {
                throw new InvalidDataException("Interrupted recovery receipt does not match.");
            }

            var inventoryService = new PbirDeployableMaterializationPreviewService(
                new PbirDeployableMaterializationSafetyGate(), _canonicalJson, _fileSystem);
            var current = inventoryService.Inventory(paths.CanonicalTargetPath);
            var currentHash = _canonicalJson.ComputeSha256(_canonicalJson.SerializeTargetInventory(current));
            if (currentHash != request.ExpectedCurrentTargetStateHash ||
                (isCommittedRollback && currentHash != receipt!.CommittedTargetStateHash))
            {
                throw new InvalidDataException("Current target changed after apply; rollback is blocked.");
            }

            var originalPhase = transaction.Phase;
            transaction = _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.Restoring, currentHash);
            var transactionPath = _transactionStore.TransactionPath(paths, request.TransactionId);
            var quarantine = Path.Combine(transactionPath, "quarantine");
            if (_fileSystem.DirectoryExists(quarantine)) throw new InvalidDataException("Rollback quarantine already exists.");
            var phaseHadPromotedTarget = isCommittedRollback || transaction.Events.Any(value =>
                value.Phase is "targetpromoted" or "targetverified" or "receiptcommitted" or "completed");
            if (phaseHadPromotedTarget && _fileSystem.DirectoryExists(paths.CanonicalTargetPath))
            {
                _fileSystem.MoveDirectory(paths.CanonicalTargetPath, quarantine);
            }

            var backup = Path.Combine(transactionPath, "backup");
            var preStateWasMoved = isCommittedRollback || originalPhase is
                PbirDeployableMaterializationJournalPhase.BackupMoved or
                PbirDeployableMaterializationJournalPhase.TargetPromoted or
                PbirDeployableMaterializationJournalPhase.TargetVerified or
                PbirDeployableMaterializationJournalPhase.ReceiptCommitted;
            if (preStateWasMoved &&
                transaction.ExpectedPreState is (PbirDeployableTargetState.EmptyDirectory or PbirDeployableTargetState.Files))
            {
                if (!_fileSystem.DirectoryExists(backup)) throw new InvalidDataException("Required rollback backup is missing.");
                _fileSystem.MoveDirectory(backup, paths.CanonicalTargetPath);
            }

            var previousReceiptPath = Path.Combine(transactionPath, "previous-receipt.json");
            string? restoredReceiptHash = receipt?.ReceiptHash;
            if (preStateWasMoved && _fileSystem.FileExists(previousReceiptPath))
            {
                var previousBytes = _fileSystem.ReadAllBytes(previousReceiptPath);
                var previous = _canonicalJson.Deserialize<PbirDeployableMaterializationReceipt>(previousBytes);
                if (previous.ReceiptHash != transaction.PreviousReceiptHash ||
                    previous.ReceiptHash != _transactionStore.Rehash(previous with { ReceiptHash = string.Empty }).ReceiptHash)
                    throw new InvalidDataException("Previous receipt is invalid.");
                _transactionStore.RestoreCurrentReceipt(paths, previousBytes, request.TransactionId);
                restoredReceiptHash = previous.ReceiptHash;
            }
            else if (preStateWasMoved && _fileSystem.FileExists(Path.Combine(paths.TargetControlPath, "current-receipt.json")))
            {
                _transactionStore.RemoveCurrentReceipt(paths);
                restoredReceiptHash = null;
            }

            var restored = inventoryService.Inventory(paths.CanonicalTargetPath);
            var restoredHash = _canonicalJson.ComputeSha256(_canonicalJson.SerializeTargetInventory(restored));
            if (restoredHash != transaction.ExpectedPreStateHash) throw new InvalidDataException("Restored target does not match the journaled pre-state.");
            transaction = _transactionStore.Advance(paths, transaction, PbirDeployableMaterializationJournalPhase.Restored, restoredHash);

            var sourceLineage = receipt?.Lineage ?? transaction.Lineage;
            var immutableLineage = sourceLineage.ImmutableLineage.Concat([request.RequestId]).ToArray();
            var lineageHash = _canonicalJson.Hash(new { sourceLineage, immutableLineage });
            var lineage = new PbirDeployableMaterializationLineage(PbirDeployableMaterializationLineageContract.SchemaVersionV1, sourceLineage.Phase29Lineage, immutableLineage, lineageHash);
            var inputHash = _canonicalJson.Hash(new { request, transaction.TransactionHash });
            var provisionalHashes = new PbirDeployableMaterializationHashes(PbirDeployableMaterializationHashesContract.SchemaVersionV1, inputHash, string.Empty, restoredHash, lineageHash, string.Empty);
            var provisional = new PbirDeployableMaterializationRollbackResult(
                PbirDeployableMaterializationRollbackResultContract.SchemaVersionV1, $"rollbackResult:{request.TransactionId}", request.RequestId,
                request.TransactionId, transaction.TransactionHash, restored.TargetState, restoredHash, currentHash, restoredReceiptHash,
                isCommittedRollback
                    ? PbirDeployableMaterializationRecoveryDisposition.RolledBackCommittedApply
                    : PbirDeployableMaterializationRecoveryDisposition.RecoveredInterruptedApply,
                lineage,
                PbirDeployableMaterializationDiagnostics.Empty, provisionalHashes);
            var result = provisional with { Hashes = provisionalHashes with { SelfHash = _canonicalJson.Hash(provisional) } };
            return new(result, PbirDeployableMaterializationReadinessState.RolledBack, PbirDeployableMaterializationDiagnostics.Empty);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            diagnostics.Add(new("PBIRMAT-ROLLBACK-001", paths.CanonicalTargetPath, exception.Message));
            return Failed(diagnostics);
        }
    }

    private static PbirDeployableMaterializationRollbackState Failed(IEnumerable<PbirDeployableMaterializationDiagnostic> diagnostics) =>
        new(null, PbirDeployableMaterializationReadinessState.Blocked,
            new(PbirDeployableMaterializationDiagnosticsContract.SchemaVersionV1,
                PbirDeployableMaterializationSafetyGate.Order(diagnostics)));
}
