using System.Text;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableMaterializationPreviewService
{
    private readonly PbirDeployableMaterializationSafetyGate _safetyGate;
    private readonly PbirDeployableMaterializationPathPolicy _pathPolicy;
    private readonly PbirDeployableMaterializationCanonicalJson _canonicalJson;
    private readonly IPbirDeployableMaterializationFileSystem _fileSystem;

    internal PbirDeployableMaterializationPreviewService()
        : this(new PbirDeployableMaterializationSafetyGate(), new PbirDeployableMaterializationCanonicalJson(), new PbirDeployableMaterializationFileSystem())
    {
    }

    internal PbirDeployableMaterializationPreviewService(
        PbirDeployableMaterializationSafetyGate safetyGate,
        PbirDeployableMaterializationCanonicalJson canonicalJson,
        IPbirDeployableMaterializationFileSystem fileSystem)
    {
        _safetyGate = safetyGate;
        _canonicalJson = canonicalJson;
        _fileSystem = fileSystem;
        _pathPolicy = new PbirDeployableMaterializationPathPolicy(fileSystem);
    }

    internal PbirDeployableMaterializationPreviewState CreatePreview(
        PbirDeployableArtifact artifact,
        PbirDeployableManifest manifest,
        PbirDeployableMaterializationPreviewRequest request,
        string outputBaseDirectory)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = _safetyGate.ValidatePreview(artifact, manifest, request).ToList();
        var paths = _pathPolicy.Resolve(outputBaseDirectory, request.TargetDirectoryName, artifact.Files.Select(file => file.RelativePath).ToArray());
        diagnostics.AddRange(paths.Diagnostics);
        if (diagnostics.Count > 0)
        {
            return Failed(diagnostics);
        }

        PbirDeployableTargetInventory inventory;
        try
        {
            inventory = Inventory(paths.CanonicalTargetPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            diagnostics.Add(new("PBIRMAT-INVENTORY-001", paths.CanonicalTargetPath, "Target inventory could not be read safely and consistently."));
            return Failed(diagnostics);
        }

        var desired = new PbirDeployableTargetInventory(
            PbirDeployableTargetInventoryContract.SchemaVersionV1,
            PbirDeployableTargetState.Files,
            artifact.Files.Select(file => new PbirDeployableTargetInventoryFile(file.RelativePath, file.ByteLength, file.HashSha256)).ToArray());
        var targetHash = _canonicalJson.ComputeSha256(_canonicalJson.SerializeTargetInventory(inventory));
        var desiredHash = _canonicalJson.ComputeSha256(_canonicalJson.SerializeTargetInventory(desired));
        var rollbackAvailable = false;
        var managedState = false;
        var recoveryRequired = false;
        string? activeTransactionRef = null;
        var transactionStore = new PbirDeployableMaterializationTransactionStore(_fileSystem, _canonicalJson);
        try
        {
            transactionStore.ValidateControlRootIfPresent(paths);
            var receipt = transactionStore.ReadCurrentReceipt(paths);
            if (receipt is not null)
            {
                var transaction = transactionStore.ReadTransaction(paths, receipt.TransactionId);
                managedState = receipt.TargetKey == paths.TargetKey &&
                    receipt.CanonicalTargetPath == paths.CanonicalTargetPath &&
                    receipt.CommittedTargetStateHash == targetHash &&
                    transaction.TransactionId == receipt.TransactionId &&
                    transaction.Phase == PbirDeployableMaterializationJournalPhase.Completed;
                rollbackAvailable = managedState;
                recoveryRequired = !managedState;
            }
            activeTransactionRef = transactionStore.FindActiveTransaction(paths);
            recoveryRequired = recoveryRequired || activeTransactionRef is not null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            recoveryRequired = true;
        }

        var disposition = inventory.TargetState switch
        {
            _ when recoveryRequired => PbirDeployableMaterializationDisposition.RecoveryRequired,
            PbirDeployableTargetState.Absent or PbirDeployableTargetState.EmptyDirectory => PbirDeployableMaterializationDisposition.Create,
            PbirDeployableTargetState.Files when targetHash == desiredHash => PbirDeployableMaterializationDisposition.NoChanges,
            PbirDeployableTargetState.Files when managedState => PbirDeployableMaterializationDisposition.ReplaceManaged,
            _ => PbirDeployableMaterializationDisposition.BlockedConflict
        };

        var inputHash = _canonicalJson.Hash(new { request, paths.CanonicalOutputBasePath, paths.CanonicalTargetPath, targetHash });
        var immutableLineage = artifact.Lineage.ImmutableLineage.Concat([request.RequestId]).ToArray();
        var lineageHash = _canonicalJson.Hash(new { phase29 = artifact.Lineage, immutableLineage });
        var lineage = new PbirDeployableMaterializationLineage(PbirDeployableMaterializationLineageContract.SchemaVersionV1, artifact.Lineage, immutableLineage, lineageHash);
        var previewId = $"materializationPreview:{inputHash[..24]}";
        var provisionalHashes = new PbirDeployableMaterializationHashes(PbirDeployableMaterializationHashesContract.SchemaVersionV1, inputHash, artifact.Hashes.FileSetHash, targetHash, lineageHash, string.Empty);
        var provisional = new PbirDeployableMaterializationPreview(
            PbirDeployableMaterializationPreviewContract.SchemaVersionV1, previewId, request.RequestId,
            artifact.ArtifactId, artifact.Hashes.ArtifactHash, manifest.ManifestId, manifest.Hashes.ManifestHash,
            paths.CanonicalOutputBasePath, paths.CanonicalTargetPath, paths.TargetKey, inventory, disposition,
            manifest.Files.ToArray(), rollbackAvailable, activeTransactionRef, lineage, provisionalHashes);
        var previewHash = _canonicalJson.Hash(provisional);
        var preview = provisional with { Hashes = provisionalHashes with { SelfHash = previewHash } };
        var readiness = disposition switch
        {
            PbirDeployableMaterializationDisposition.Create => PbirDeployableMaterializationReadinessState.ReadyToCreate,
            PbirDeployableMaterializationDisposition.ReplaceManaged => PbirDeployableMaterializationReadinessState.ReadyToReplaceManaged,
            PbirDeployableMaterializationDisposition.NoChanges => PbirDeployableMaterializationReadinessState.NoChanges,
            PbirDeployableMaterializationDisposition.RecoveryRequired => PbirDeployableMaterializationReadinessState.RecoveryRequired,
            _ => PbirDeployableMaterializationReadinessState.Blocked
        };
        return new PbirDeployableMaterializationPreviewState(preview, readiness, PbirDeployableMaterializationDiagnostics.Empty);
    }

    internal PbirDeployableTargetInventory Inventory(string targetPath)
    {
        if (!_fileSystem.DirectoryExists(targetPath))
        {
            return new(PbirDeployableTargetInventoryContract.SchemaVersionV1, PbirDeployableTargetState.Absent, []);
        }

        var files = new List<PbirDeployableTargetInventoryFile>();
        Scan(targetPath, targetPath, files);
        if (files.Select(file => file.RelativePath).Distinct(PbirDeployableMaterializationPathPolicy.ActivePathComparer).Count() != files.Count ||
            files.Any(file => !file.RelativePath.IsNormalized(NormalizationForm.FormC)))
        {
            throw new InvalidDataException("Target inventory contains colliding or non-NFC paths.");
        }
        return new(
            PbirDeployableTargetInventoryContract.SchemaVersionV1,
            files.Count == 0 ? PbirDeployableTargetState.EmptyDirectory : PbirDeployableTargetState.Files,
            files.OrderBy(file => file.RelativePath, PbirDeployableMaterializationPathPolicy.ActivePathComparer).ToArray());
    }

    private void Scan(string root, string directory, List<PbirDeployableTargetInventoryFile> files)
    {
        foreach (var entry in _fileSystem.EnumerateEntries(directory).OrderBy(value => value, PbirDeployableMaterializationPathPolicy.ActivePathComparer))
        {
            var attributes = _fileSystem.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException("Links are not allowed in materialization targets.");
            }
            if ((attributes & FileAttributes.Directory) != 0)
            {
                Scan(root, entry, files);
                continue;
            }
            if ((attributes & (FileAttributes.Device | FileAttributes.Offline)) != 0)
            {
                throw new InvalidDataException("Special or offline files are not supported.");
            }

            var bytes = _fileSystem.ReadAllBytes(entry);
            var after = _fileSystem.GetAttributes(entry);
            if (attributes != after)
            {
                throw new InvalidDataException("Target changed during inventory.");
            }
            var relative = Path.GetRelativePath(root, entry).Replace(Path.DirectorySeparatorChar, '/');
            files.Add(new(relative, bytes.LongLength, _canonicalJson.ComputeSha256(bytes)));
        }
    }

    private static PbirDeployableMaterializationPreviewState Failed(IEnumerable<PbirDeployableMaterializationDiagnostic> diagnostics) =>
        new(null, PbirDeployableMaterializationReadinessState.Blocked,
            new(PbirDeployableMaterializationDiagnosticsContract.SchemaVersionV1, PbirDeployableMaterializationSafetyGate.Order(diagnostics)));
}
