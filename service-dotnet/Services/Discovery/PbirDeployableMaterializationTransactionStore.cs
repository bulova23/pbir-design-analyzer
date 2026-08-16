using System.Text.RegularExpressions;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableMaterializationTransactionReuseException(string message) : Exception(message);

internal sealed class PbirDeployableMaterializationTransactionStore
{
    private static readonly Regex TransactionIdPattern = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant);
    private readonly IPbirDeployableMaterializationFileSystem _fileSystem;
    private readonly PbirDeployableMaterializationCanonicalJson _canonicalJson;

    internal PbirDeployableMaterializationTransactionStore(
        IPbirDeployableMaterializationFileSystem fileSystem,
        PbirDeployableMaterializationCanonicalJson canonicalJson)
    {
        _fileSystem = fileSystem;
        _canonicalJson = canonicalJson;
    }

    internal void EnsureControlRoot(PbirDeployableMaterializationPathResult paths)
    {
        var ownerRoot = Path.Combine(paths.CanonicalOutputBasePath, ".pbir-design-analyzer");
        var markerPath = Path.Combine(paths.ControlRootPath, "control-root.json");
        if (!_fileSystem.DirectoryExists(paths.ControlRootPath))
        {
            if (_fileSystem.DirectoryExists(ownerRoot))
            {
                throw new InvalidDataException("Existing private control directory has no Phase 30 ownership marker.");
            }
            _fileSystem.CreateDirectory(paths.ControlRootPath);
            var baseHash = _canonicalJson.ComputeSha256(paths.CanonicalOutputBasePath);
            var provisional = new PbirDeployableMaterializationControlRoot(
                PbirDeployableMaterializationControlRootContract.SchemaVersionV1,
                "pbir-design-analyzer", "deployablePbirMaterialization", baseHash, string.Empty);
            var marker = provisional with { ControlRootHash = _canonicalJson.Hash(provisional) };
            _fileSystem.WriteAllBytesCreateNew(markerPath, _canonicalJson.Serialize(marker));
        }

        if (!_fileSystem.FileExists(markerPath))
        {
            throw new InvalidDataException("Phase 30 control-root marker is missing.");
        }
        var existing = _canonicalJson.Deserialize<PbirDeployableMaterializationControlRoot>(_fileSystem.ReadAllBytes(markerPath));
        var expectedHash = _canonicalJson.Hash(existing with { ControlRootHash = string.Empty });
        if (existing.SchemaVersion != PbirDeployableMaterializationControlRootContract.SchemaVersionV1 ||
            existing.Owner != "pbir-design-analyzer" || existing.Purpose != "deployablePbirMaterialization" ||
            existing.CanonicalOutputBaseHash != _canonicalJson.ComputeSha256(paths.CanonicalOutputBasePath) ||
            existing.ControlRootHash != expectedHash)
        {
            throw new InvalidDataException("Phase 30 control-root marker is invalid or belongs to another base.");
        }
        _fileSystem.CreateDirectory(Path.Combine(paths.ControlRootPath, "targets"));
        _fileSystem.CreateDirectory(paths.TargetControlPath);
        _fileSystem.CreateDirectory(Path.Combine(paths.TargetControlPath, "transactions"));
    }

    internal void ValidateControlRootIfPresent(PbirDeployableMaterializationPathResult paths)
    {
        var ownerRoot = Path.Combine(paths.CanonicalOutputBasePath, ".pbir-design-analyzer");
        if (!_fileSystem.DirectoryExists(ownerRoot)) return;
        var markerPath = Path.Combine(paths.ControlRootPath, "control-root.json");
        if (!_fileSystem.DirectoryExists(paths.ControlRootPath) || !_fileSystem.FileExists(markerPath))
            throw new InvalidDataException("Existing private control directory is not owned by Phase 30.");
        var existing = _canonicalJson.Deserialize<PbirDeployableMaterializationControlRoot>(_fileSystem.ReadAllBytes(markerPath));
        if (existing.SchemaVersion != PbirDeployableMaterializationControlRootContract.SchemaVersionV1 ||
            existing.Owner != "pbir-design-analyzer" || existing.Purpose != "deployablePbirMaterialization" ||
            existing.CanonicalOutputBaseHash != _canonicalJson.ComputeSha256(paths.CanonicalOutputBasePath) ||
            existing.ControlRootHash != _canonicalJson.Hash(existing with { ControlRootHash = string.Empty }))
            throw new InvalidDataException("Existing Phase 30 ownership marker is invalid.");
    }

    internal IDisposable AcquireLock(PbirDeployableMaterializationPathResult paths) =>
        _fileSystem.OpenExclusiveLock(Path.Combine(paths.TargetControlPath, "materialization.lock"));

    internal string TransactionPath(PbirDeployableMaterializationPathResult paths, string transactionId)
    {
        if (!TransactionIdPattern.IsMatch(transactionId))
        {
            throw new InvalidDataException("Transaction id is unsafe.");
        }
        return Path.Combine(paths.TargetControlPath, "transactions", transactionId);
    }

    internal PbirDeployableMaterializationTransaction Begin(
        PbirDeployableMaterializationPathResult paths,
        PbirDeployableMaterializationPreview preview,
        PbirDeployableMaterializationApplyRequest request,
        string? previousReceiptHash)
    {
        var transactionPath = TransactionPath(paths, request.TransactionId);
        if (_fileSystem.DirectoryExists(transactionPath))
        {
            throw new PbirDeployableMaterializationTransactionReuseException("Transaction id has already been used.");
        }
        _fileSystem.CreateDirectory(transactionPath);
        var transaction = Rehash(new PbirDeployableMaterializationTransaction(
            PbirDeployableMaterializationTransactionContract.SchemaVersionV1,
            request.TransactionId, "apply", paths.TargetKey, paths.CanonicalTargetPath,
            preview.PreviewId, preview.Hashes.SelfHash, preview.ArtifactRef, preview.ArtifactHash,
            preview.ManifestRef, preview.ManifestHash, preview.TargetInventory.TargetState,
            preview.Hashes.TargetStateHash, previousReceiptHash, PbirDeployableMaterializationJournalPhase.Initialized,
            [new("initialized", preview.Hashes.TargetStateHash)], null, null, null, preview.Lineage, string.Empty));
        _fileSystem.WriteAllBytesCreateNew(Path.Combine(transactionPath, "journal.json"), _canonicalJson.Serialize(transaction));
        return transaction;
    }

    internal PbirDeployableMaterializationTransaction Advance(
        PbirDeployableMaterializationPathResult paths,
        PbirDeployableMaterializationTransaction transaction,
        PbirDeployableMaterializationJournalPhase phase,
        string stateHash,
        string? stagingHash = null,
        string? backupHash = null,
        string? committedHash = null)
    {
        if (!IsAllowedTransition(transaction.Phase, phase))
            throw new InvalidDataException($"Journal transition {transaction.Phase} -> {phase} is not allowed.");
        var next = Rehash(transaction with
        {
            Phase = phase,
            Events = transaction.Events.Concat([new(phase.ToString().ToLowerInvariant(), stateHash)]).ToArray(),
            StagingInventoryHash = stagingHash ?? transaction.StagingInventoryHash,
            BackupInventoryHash = backupHash ?? transaction.BackupInventoryHash,
            CommittedTargetStateHash = committedHash ?? transaction.CommittedTargetStateHash,
            TransactionHash = string.Empty
        });
        ReplaceAtomically(
            Path.Combine(TransactionPath(paths, transaction.TransactionId), "journal.json"),
            _canonicalJson.Serialize(next),
            phase.ToString().ToLowerInvariant());
        return next;
    }

    internal PbirDeployableMaterializationTransaction ReadTransaction(PbirDeployableMaterializationPathResult paths, string transactionId)
    {
        var transaction = _canonicalJson.Deserialize<PbirDeployableMaterializationTransaction>(
            _fileSystem.ReadAllBytes(Path.Combine(TransactionPath(paths, transactionId), "journal.json")));
        if (transaction.TransactionHash != Rehash(transaction with { TransactionHash = string.Empty }).TransactionHash)
        {
            throw new InvalidDataException("Transaction journal hash is invalid.");
        }
        return transaction;
    }

    internal PbirDeployableMaterializationReceipt? ReadCurrentReceipt(PbirDeployableMaterializationPathResult paths)
    {
        var path = Path.Combine(paths.TargetControlPath, "current-receipt.json");
        if (!_fileSystem.FileExists(path)) return null;
        var receipt = _canonicalJson.Deserialize<PbirDeployableMaterializationReceipt>(_fileSystem.ReadAllBytes(path));
        if (receipt.ReceiptHash != Rehash(receipt with { ReceiptHash = string.Empty }).ReceiptHash)
        {
            throw new InvalidDataException("Current receipt hash is invalid.");
        }
        return receipt;
    }

    internal string? FindActiveTransaction(PbirDeployableMaterializationPathResult paths)
    {
        var transactionsPath = Path.Combine(paths.TargetControlPath, "transactions");
        if (!_fileSystem.DirectoryExists(transactionsPath)) return null;
        var active = new List<string>();
        foreach (var entry in _fileSystem.EnumerateEntries(transactionsPath))
        {
            if ((_fileSystem.GetAttributes(entry) & FileAttributes.Directory) == 0) throw new InvalidDataException("Unexpected transaction control entry.");
            var transactionId = Path.GetFileName(entry);
            var transaction = ReadTransaction(paths, transactionId);
            if (transaction.Phase is not (PbirDeployableMaterializationJournalPhase.Aborted or
                PbirDeployableMaterializationJournalPhase.Completed or
                PbirDeployableMaterializationJournalPhase.Restored))
            {
                active.Add(transactionId);
            }
        }
        if (active.Count > 1) throw new InvalidDataException("Multiple active transactions require recovery.");
        return active.SingleOrDefault();
    }

    internal PbirDeployableMaterializationReceipt Rehash(PbirDeployableMaterializationReceipt receipt) =>
        receipt with { ReceiptHash = _canonicalJson.Hash(receipt with { ReceiptHash = string.Empty }) };

    internal void SaveCurrentReceipt(PbirDeployableMaterializationPathResult paths, PbirDeployableMaterializationReceipt receipt)
    {
        var current = Path.Combine(paths.TargetControlPath, "current-receipt.json");
        ReplaceAtomically(current, _canonicalJson.Serialize(receipt), receipt.TransactionId);
    }

    internal void SavePreviousReceipt(PbirDeployableMaterializationPathResult paths, string transactionId, byte[] bytes) =>
        _fileSystem.WriteAllBytesCreateNew(Path.Combine(TransactionPath(paths, transactionId), "previous-receipt.json"), bytes);

    internal byte[]? ReadCurrentReceiptBytes(PbirDeployableMaterializationPathResult paths)
    {
        var path = Path.Combine(paths.TargetControlPath, "current-receipt.json");
        return _fileSystem.FileExists(path) ? _fileSystem.ReadAllBytes(path) : null;
    }

    internal void RemoveCurrentReceipt(PbirDeployableMaterializationPathResult paths) =>
        _fileSystem.DeleteFile(Path.Combine(paths.TargetControlPath, "current-receipt.json"));

    internal void RestoreCurrentReceipt(PbirDeployableMaterializationPathResult paths, byte[] bytes, string transactionId) =>
        ReplaceAtomically(Path.Combine(paths.TargetControlPath, "current-receipt.json"), bytes, $"restore-{transactionId}");

    private PbirDeployableMaterializationTransaction Rehash(PbirDeployableMaterializationTransaction transaction) =>
        transaction with { TransactionHash = _canonicalJson.Hash(transaction with { TransactionHash = string.Empty }) };

    private void ReplaceAtomically(string path, byte[] content, string token)
    {
        var temporaryPath = $"{path}.{token}.next";
        _fileSystem.WriteAllBytesCreateNew(temporaryPath, content);
        _fileSystem.MoveFile(temporaryPath, path, overwrite: true);
    }

    private static bool IsAllowedTransition(
        PbirDeployableMaterializationJournalPhase from,
        PbirDeployableMaterializationJournalPhase to) => (from, to) switch
        {
            (PbirDeployableMaterializationJournalPhase.Initialized, PbirDeployableMaterializationJournalPhase.StagingWritten or PbirDeployableMaterializationJournalPhase.Aborted or PbirDeployableMaterializationJournalPhase.Restoring) => true,
            (PbirDeployableMaterializationJournalPhase.StagingWritten, PbirDeployableMaterializationJournalPhase.StagingVerified or PbirDeployableMaterializationJournalPhase.Aborted or PbirDeployableMaterializationJournalPhase.Restoring) => true,
            (PbirDeployableMaterializationJournalPhase.StagingVerified, PbirDeployableMaterializationJournalPhase.BackupMoved or PbirDeployableMaterializationJournalPhase.TargetPromoted or PbirDeployableMaterializationJournalPhase.Aborted or PbirDeployableMaterializationJournalPhase.Restoring) => true,
            (PbirDeployableMaterializationJournalPhase.BackupMoved, PbirDeployableMaterializationJournalPhase.TargetPromoted or PbirDeployableMaterializationJournalPhase.Restoring) => true,
            (PbirDeployableMaterializationJournalPhase.TargetPromoted, PbirDeployableMaterializationJournalPhase.TargetVerified or PbirDeployableMaterializationJournalPhase.Restoring) => true,
            (PbirDeployableMaterializationJournalPhase.TargetVerified, PbirDeployableMaterializationJournalPhase.ReceiptCommitted or PbirDeployableMaterializationJournalPhase.Restoring) => true,
            (PbirDeployableMaterializationJournalPhase.ReceiptCommitted, PbirDeployableMaterializationJournalPhase.Completed or PbirDeployableMaterializationJournalPhase.Restoring) => true,
            (PbirDeployableMaterializationJournalPhase.Completed, PbirDeployableMaterializationJournalPhase.Restoring) => true,
            (PbirDeployableMaterializationJournalPhase.RecoveryRequired, PbirDeployableMaterializationJournalPhase.Restoring) => true,
            (PbirDeployableMaterializationJournalPhase.Restoring, PbirDeployableMaterializationJournalPhase.Restored or PbirDeployableMaterializationJournalPhase.RecoveryRequired) => true,
            _ => false
        };
}
