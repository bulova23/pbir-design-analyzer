using System.IO;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirLocalPreviewFileWriterSafetyGate
{
    private readonly PbirLocalArtifactWriterSafetyGate _writerBoundarySafetyGate;

    internal PbirLocalPreviewFileWriterSafetyGate()
        : this(new PbirLocalArtifactWriterSafetyGate())
    {
    }

    internal PbirLocalPreviewFileWriterSafetyGate(PbirLocalArtifactWriterSafetyGate writerBoundarySafetyGate)
    {
        _writerBoundarySafetyGate = writerBoundarySafetyGate;
    }

    internal PbirLocalPreviewFileWriterSafetyGateResult Validate(
        PbirPreviewArtifact previewArtifact,
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentationState irState,
        PbirLocalWriteRequest request,
        PbirLocalWriteManifest writeManifest,
        string outputBaseDirectory)
    {
        ArgumentNullException.ThrowIfNull(previewArtifact);
        ArgumentNullException.ThrowIfNull(previewManifest);
        ArgumentNullException.ThrowIfNull(irState);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(writeManifest);

        var reasons = new List<string>();
        var boundarySafety = _writerBoundarySafetyGate.Validate(previewManifest, irState, request);
        reasons.AddRange(boundarySafety.Reasons);

        if (!IsLocalFilesystemPath(outputBaseDirectory))
        {
            reasons.Add("output base directory must be a local filesystem path.");
        }

        if (request.OverwritePolicy == PbirLocalOverwritePolicy.OverwriteExisting)
        {
            reasons.Add("blind overwrite is not supported.");
        }

        if (request.OverwritePolicy != PbirLocalOverwritePolicy.FailIfExists &&
            request.OverwritePolicy != PbirLocalOverwritePolicy.AllowOverwriteOnlyWhenHashMatches)
        {
            reasons.Add("local preview writer overwrite policy must be failIfExists or allowOverwriteOnlyWhenHashMatches.");
        }

        if (request.RollbackPolicy != PbirLocalRollbackPolicy.PlanOnly ||
            writeManifest.RollbackPlan.Actions.Count == 0)
        {
            reasons.Add("rollback metadata is required before local preview writes.");
        }

        ValidateManifest(previewArtifact, previewManifest, irState, request, writeManifest, reasons);

        return new PbirLocalPreviewFileWriterSafetyGateResult(
            IsAllowed: reasons.Count == 0,
            Reasons: reasons
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray());
    }

    private static void ValidateManifest(
        PbirPreviewArtifact previewArtifact,
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentationState irState,
        PbirLocalWriteRequest request,
        PbirLocalWriteManifest writeManifest,
        ICollection<string> reasons)
    {
        var ir = irState.Ir;
        if (ir is null)
        {
            reasons.Add("complete PBIR IR must be provided.");
            return;
        }

        if (!string.Equals(writeManifest.SchemaVersion, PbirLocalWriteManifestContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            reasons.Add("source write manifest schema version must be pbir-local-write-manifest/v1.");
        }

        if (!string.Equals(writeManifest.Writer.SchemaVersion, PbirLocalArtifactWriterContract.SchemaVersionV1, StringComparison.Ordinal) ||
            !writeManifest.Writer.LocalOnly ||
            !writeManifest.Writer.DryRunOnly)
        {
            reasons.Add("source write manifest must come from the dry-run local writer boundary.");
        }

        if (!string.Equals(writeManifest.SourceLineage.WriteRequestRef, request.RequestId, StringComparison.Ordinal) ||
            !string.Equals(writeManifest.SourceLineage.PbirIrRef, ir.Metadata.IrId, StringComparison.Ordinal) ||
            !string.Equals(writeManifest.SourceLineage.PbirIrContentHash, ir.Hashes.ContentHash, StringComparison.Ordinal) ||
            !string.Equals(writeManifest.SourceLineage.PreviewManifestRef, previewManifest.Metadata.ManifestId, StringComparison.Ordinal) ||
            !string.Equals(writeManifest.SourceLineage.PreviewManifestHash, previewManifest.Hashes.ManifestHash, StringComparison.Ordinal))
        {
            reasons.Add("source write manifest lineage must match the approved write request, PBIR IR, and preview manifest.");
        }

        if (!string.Equals(previewArtifact.SchemaVersion, PbirPreviewArtifactContract.SchemaVersionV1, StringComparison.Ordinal) ||
            !string.Equals(previewArtifact.SourceReferences.PbirIrRef, ir.Metadata.IrId, StringComparison.Ordinal) ||
            !string.Equals(previewArtifact.SourceReferences.PbirIrContentHash, ir.Hashes.ContentHash, StringComparison.Ordinal))
        {
            reasons.Add("preview artifact must reference the supplied PBIR IR.");
        }

        var rollbackPaths = writeManifest.RollbackPlan.Actions
            .Select(action => NormalizePath(action.RelativePath))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var plannedFile in writeManifest.PlannedFiles)
        {
            ValidatePlannedFile(previewArtifact, previewManifest, ir, request, writeManifest, plannedFile, rollbackPaths, reasons);
        }
    }

    private static void ValidatePlannedFile(
        PbirPreviewArtifact previewArtifact,
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentation ir,
        PbirLocalWriteRequest request,
        PbirLocalWriteManifest writeManifest,
        PbirLocalPlannedWriteFile plannedFile,
        IReadOnlySet<string> rollbackPaths,
        ICollection<string> reasons)
    {
        var relativePath = NormalizePath(plannedFile.RelativePath);
        var intendedPath = NormalizePath(plannedFile.IntendedPath);

        if (!PbirLocalArtifactWriterSafetyGate.IsAllowedArtifactType(plannedFile.ArtifactType))
        {
            var forbiddenName = PbirLocalArtifactWriterSafetyGate.GetForbiddenArtifactName(plannedFile.ArtifactType) ?? plannedFile.ArtifactType.ToString();
            reasons.Add($"deployable PBIR artifact paths are not allowed: {FormatForbiddenArtifactName(forbiddenName)}.");
        }

        var forbiddenPathReason = GetForbiddenPathReason(relativePath) ?? GetForbiddenPathReason(intendedPath);
        if (forbiddenPathReason is not null)
        {
            reasons.Add(forbiddenPathReason);
        }

        if (!request.RequestedArtifactTypes.Contains(plannedFile.ArtifactType))
        {
            reasons.Add("planned file artifact type must be approved by the local write request.");
        }

        if (!IsSafeRelativePath(relativePath) || !IsSafeRelativePath(intendedPath))
        {
            reasons.Add("planned file paths must be safe local relative paths.");
        }

        var expectedIntendedPath = JoinLocalPath(writeManifest.Metadata.TargetOutputRoot, relativePath);
        if (!string.Equals(intendedPath, expectedIntendedPath, StringComparison.Ordinal))
        {
            reasons.Add("planned file intended path must match target output root and relative path.");
        }

        if (plannedFile.WillWrite)
        {
            reasons.Add("source write manifest entries must remain approved dry-run entries.");
        }

        if (!rollbackPaths.Contains(relativePath))
        {
            reasons.Add("rollback plan must cover every planned file.");
        }

        var content = PbirLocalPreviewFileContentFactory.TryCreateContent(
            previewArtifact,
            previewManifest,
            ir,
            request,
            plannedFile);
        if (content is null)
        {
            reasons.Add("deterministic writer content must exist for every planned file.");
            return;
        }

        if (!string.Equals(content.HashSha256, plannedFile.HashSha256, StringComparison.Ordinal) ||
            content.ByteLength != plannedFile.ByteLength)
        {
            reasons.Add("planned file hash must match deterministic writer content.");
        }
    }

    private static bool IsLocalFilesystemPath(string path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
            !path.StartsWith("~", StringComparison.Ordinal) &&
            !path.Contains("://", StringComparison.Ordinal);
    }

    private static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            Path.IsPathRooted(path) ||
            path.StartsWith("~", StringComparison.Ordinal) ||
            path.Contains("://", StringComparison.Ordinal))
        {
            return false;
        }

        return !path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => string.Equals(segment, "..", StringComparison.Ordinal));
    }

    private static string? GetForbiddenPathReason(string path)
    {
        var fileName = Path.GetFileName(path.Replace('\\', '/'));
        if (fileName.EndsWith(".pbip", StringComparison.OrdinalIgnoreCase) ||
            path.Contains(".Report/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains(".SemanticModel/", StringComparison.OrdinalIgnoreCase))
        {
            return "PBIP project structure paths are not allowed.";
        }

        if (string.Equals(fileName, "report.json", StringComparison.OrdinalIgnoreCase))
        {
            return "deployable PBIR artifact paths are not allowed: report.json.";
        }

        if (string.Equals(fileName, "definition.pbir", StringComparison.OrdinalIgnoreCase))
        {
            return "deployable PBIR artifact paths are not allowed: definition.pbir.";
        }

        if (string.Equals(fileName, "model.bim", StringComparison.OrdinalIgnoreCase))
        {
            return "deployable PBIR artifact paths are not allowed: model.bim.";
        }

        if (fileName.EndsWith(".tmdl", StringComparison.OrdinalIgnoreCase))
        {
            return "deployable PBIR artifact paths are not allowed: TMDL.";
        }

        return null;
    }

    private static string FormatForbiddenArtifactName(string artifactName)
    {
        return string.Equals(artifactName, "tmdl", StringComparison.OrdinalIgnoreCase)
            ? "TMDL"
            : artifactName;
    }

    private static string JoinLocalPath(string root, string relativePath)
    {
        return $"{NormalizePath(root).TrimEnd('/')}/{NormalizePath(relativePath).TrimStart('/')}";
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim();
    }
}
