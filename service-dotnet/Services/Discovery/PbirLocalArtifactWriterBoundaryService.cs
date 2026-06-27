using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirLocalArtifactWriterBoundaryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly PbirLocalArtifactWriterSafetyGate _safetyGate;

    internal PbirLocalArtifactWriterBoundaryService()
        : this(new PbirLocalArtifactWriterSafetyGate())
    {
    }

    internal PbirLocalArtifactWriterBoundaryService(PbirLocalArtifactWriterSafetyGate safetyGate)
    {
        _safetyGate = safetyGate;
    }

    internal PbirLocalArtifactWriterState CreateWriteManifest(
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentationState irState,
        PbirLocalWriteRequest request,
        IReadOnlyList<string> existingLocalRelativePaths,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(previewManifest);
        ArgumentNullException.ThrowIfNull(irState);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(existingLocalRelativePaths);

        var safety = _safetyGate.Validate(previewManifest, irState, request);
        if (!safety.IsAllowed || irState.Ir is null)
        {
            return new PbirLocalArtifactWriterState(
                Manifest: null,
                Safety: safety,
                Diagnostics: new PbirLocalArtifactWriterDiagnostics(
                    SafetyRejections: safety.Reasons,
                    BoundaryViolations: safety.Reasons),
                Readiness: PbirLocalArtifactWriterReadinessState.Rejected);
        }

        var ir = irState.Ir;
        var existingPaths = existingLocalRelativePaths
            .Select(NormalizePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.Ordinal);
        var plannedFiles = CreatePlannedFiles(previewManifest, ir, request, existingPaths);
        var riskPaths = plannedFiles
            .Where(file => file.OverwriteRisk)
            .Select(file => file.IntendedPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var overwriteRisk = new PbirLocalOverwriteRisk(
            HasRisk: riskPaths.Length > 0,
            Policy: request.OverwritePolicy,
            RiskPaths: riskPaths);
        var manifestId = $"pbirLocalWriteManifest:{request.RequestId}";
        var sourceLineage = CreateSourceLineage(previewManifest, ir, request, manifestId);
        var rollbackPlan = CreateRollbackPlan(request.RollbackPolicy, plannedFiles, riskPaths);
        var warnings = CreateWarnings(overwriteRisk);
        var rejectedArtifacts = CreateRejectedArtifacts(request);
        var inputHash = ComputeSha256(Serialize(new
        {
            previewManifest,
            ir,
            request
        }));
        var fileSetHash = ComputeFileSetHash(plannedFiles);
        var manifestWithoutHash = new PbirLocalWriteManifest(
            SchemaVersion: PbirLocalWriteManifestContract.SchemaVersionV1,
            Writer: new PbirLocalArtifactWriterDescriptor(
                SchemaVersion: PbirLocalArtifactWriterContract.SchemaVersionV1,
                LocalOnly: true,
                DryRunOnly: true),
            Metadata: new PbirLocalWriteManifestMetadata(
                ManifestId: manifestId,
                GeneratedUtc: generatedUtc.UtcDateTime,
                TargetOutputRoot: NormalizePath(request.TargetOutputRoot)),
            SourceLineage: sourceLineage,
            PlannedFiles: plannedFiles,
            OverwriteRisk: overwriteRisk,
            RollbackPlan: rollbackPlan,
            Warnings: warnings,
            RejectedArtifacts: rejectedArtifacts,
            Hashes: new PbirLocalWriteManifestHashes(
                InputHash: inputHash,
                FileSetHash: fileSetHash,
                ManifestHash: string.Empty));
        var manifest = manifestWithoutHash with
        {
            Hashes = manifestWithoutHash.Hashes with
            {
                ManifestHash = ComputeManifestHash(manifestWithoutHash)
            }
        };

        return new PbirLocalArtifactWriterState(
            Manifest: manifest,
            Safety: safety,
            Diagnostics: PbirLocalArtifactWriterDiagnostics.Empty,
            Readiness: overwriteRisk.HasRisk
                ? PbirLocalArtifactWriterReadinessState.PlannedWithOverwriteRisk
                : PbirLocalArtifactWriterReadinessState.Planned);
    }

    private static IReadOnlyList<PbirLocalPlannedWriteFile> CreatePlannedFiles(
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentation ir,
        PbirLocalWriteRequest request,
        IReadOnlySet<string> existingPaths)
    {
        return request.RequestedArtifactTypes
            .Where(PbirLocalArtifactWriterSafetyGate.IsAllowedArtifactType)
            .Distinct()
            .Select(artifactType => CreatePlannedFile(previewManifest, ir, request, existingPaths, artifactType))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static PbirLocalPlannedWriteFile CreatePlannedFile(
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentation ir,
        PbirLocalWriteRequest request,
        IReadOnlySet<string> existingPaths,
        PbirLocalWriteArtifactType artifactType)
    {
        var descriptor = CreatePlannedFileDescriptor(previewManifest, ir, request, artifactType);
        var relativePath = NormalizePath(descriptor.RelativePath);
        var intendedPath = JoinLocalPath(request.TargetOutputRoot, relativePath);
        var overwriteRisk = existingPaths.Contains(intendedPath) || existingPaths.Contains(relativePath);

        return new PbirLocalPlannedWriteFile(
            ArtifactType: artifactType,
            RelativePath: relativePath,
            IntendedPath: intendedPath,
            ContentType: descriptor.ContentType,
            Purpose: descriptor.Purpose,
            SourceHash: descriptor.SourceHash,
            HashSha256: descriptor.HashSha256,
            ByteLength: descriptor.ByteLength,
            OverwriteRisk: overwriteRisk,
            WillWrite: false);
    }

    private static PlannedFileDescriptor CreatePlannedFileDescriptor(
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentation ir,
        PbirLocalWriteRequest request,
        PbirLocalWriteArtifactType artifactType)
    {
        return artifactType switch
        {
            PbirLocalWriteArtifactType.PreviewMarkdown => FromPreviewManifest(
                previewManifest,
                PbirPreviewOutputType.Markdown,
                "pbir-local-writer/v1/preview/report-preview.md",
                "text/markdown",
                "Planned local copy of PBIR preview Markdown."),
            PbirLocalWriteArtifactType.PreviewJson => FromPreviewManifest(
                previewManifest,
                PbirPreviewOutputType.Json,
                "pbir-local-writer/v1/preview/report-preview.json",
                "application/json",
                "Planned local copy of PBIR preview JSON."),
            PbirLocalWriteArtifactType.IrJson => FromContent(
                "pbir-local-writer/v1/ir/canonical-pbir-ir.json",
                "application/json",
                "Planned local copy of canonical PBIR IR JSON.",
                ir.Hashes.ContentHash,
                Serialize(ir)),
            PbirLocalWriteArtifactType.ManifestJson => FromContent(
                "pbir-local-writer/v1/manifests/pbir-preview-manifest.json",
                "application/json",
                "Planned local copy of PBIR preview manifest JSON.",
                previewManifest.Hashes.ManifestHash,
                Serialize(previewManifest)),
            PbirLocalWriteArtifactType.DiagnosticsMarkdown => FromContent(
                "pbir-local-writer/v1/diagnostics/local-write-diagnostics.md",
                "text/markdown",
                "Planned local write diagnostics Markdown.",
                request.RequestId,
                CreateDiagnosticsMarkdown(previewManifest, ir, request)),
            _ => throw new InvalidOperationException("Unsupported local write artifact type.")
        };
    }

    private static PlannedFileDescriptor FromPreviewManifest(
        PbirPreviewManifest previewManifest,
        PbirPreviewOutputType outputType,
        string relativePath,
        string contentType,
        string purpose)
    {
        var sourceFile = previewManifest.GeneratedFiles.Single(file => file.OutputType == outputType);

        return new PlannedFileDescriptor(
            RelativePath: relativePath,
            ContentType: contentType,
            Purpose: purpose,
            SourceHash: sourceFile.HashSha256,
            HashSha256: sourceFile.HashSha256,
            ByteLength: sourceFile.ByteLength);
    }

    private static PlannedFileDescriptor FromContent(
        string relativePath,
        string contentType,
        string purpose,
        string sourceHash,
        string content)
    {
        return new PlannedFileDescriptor(
            RelativePath: relativePath,
            ContentType: contentType,
            Purpose: purpose,
            SourceHash: sourceHash,
            HashSha256: ComputeSha256(content),
            ByteLength: Encoding.UTF8.GetByteCount(content));
    }

    private static PbirLocalWriteSourceLineage CreateSourceLineage(
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentation ir,
        PbirLocalWriteRequest request,
        string manifestId)
    {
        return new PbirLocalWriteSourceLineage(
            WriteRequestRef: request.RequestId,
            PbirIrRef: ir.Metadata.IrId,
            PbirIrSchemaVersion: ir.Metadata.SchemaVersion,
            PbirIrContentHash: ir.Hashes.ContentHash,
            PreviewManifestRef: previewManifest.Metadata.ManifestId,
            PreviewManifestSchemaVersion: previewManifest.SchemaVersion,
            PreviewManifestHash: previewManifest.Hashes.ManifestHash,
            UpstreamLineage: previewManifest.Lineage.UpstreamLineage
                .OrderBy(entry => entry.Stage, StringComparer.Ordinal)
                .ThenBy(entry => entry.ReferenceId, StringComparer.Ordinal)
                .ThenBy(entry => entry.Label, StringComparer.Ordinal)
                .ToArray(),
            ImmutableLineage: previewManifest.Lineage.ImmutableLineage
                .Concat(ir.Lineage.ImmutableLineage)
                .Append(request.RequestId)
                .Append(manifestId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reference => reference, StringComparer.Ordinal)
                .ToArray());
    }

    private static PbirLocalRollbackPlan CreateRollbackPlan(
        PbirLocalRollbackPolicy policy,
        IReadOnlyList<PbirLocalPlannedWriteFile> plannedFiles,
        IReadOnlyList<string> riskPaths)
    {
        return new PbirLocalRollbackPlan(
            Policy: policy,
            DryRunOnly: true,
            ProtectedExistingPaths: riskPaths,
            Actions: plannedFiles
                .Select(file => new PbirLocalRollbackAction(
                    RelativePath: file.RelativePath,
                    IntendedPath: file.IntendedPath,
                    ActionKind: file.OverwriteRisk
                        ? PbirLocalRollbackActionKind.RestoreExistingLocalFile
                        : PbirLocalRollbackActionKind.NoOpDryRun,
                    Reason: file.OverwriteRisk
                        ? "Existing local path would be protected by rollback planning."
                        : "Dry-run boundary does not create a file, so no rollback action is required."))
                .OrderBy(action => action.RelativePath, StringComparer.Ordinal)
                .ToArray());
    }

    private static IReadOnlyList<string> CreateWarnings(PbirLocalOverwriteRisk overwriteRisk)
    {
        var warnings = new List<string>
        {
            "No files will be written by this boundary.",
            "Deployable PBIR artifacts remain forbidden.",
            "Actual local artifact writing remains unimplemented."
        };

        if (overwriteRisk.HasRisk)
        {
            warnings.Add("Overwrite risk detected for planned local artifacts.");
        }

        return warnings
            .OrderBy(warning => warning, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<PbirLocalRejectedArtifact> CreateRejectedArtifacts(PbirLocalWriteRequest request)
    {
        return request.RequestedArtifactTypes
            .Select(artifactType => new
            {
                ArtifactType = artifactType,
                ForbiddenName = PbirLocalArtifactWriterSafetyGate.GetForbiddenArtifactName(artifactType)
            })
            .Where(item => item.ForbiddenName is not null)
            .Select(item => new PbirLocalRejectedArtifact(
                ArtifactType: item.ArtifactType,
                Reason: $"deployable PBIR artifact requests are not allowed: {item.ForbiddenName}."))
            .OrderBy(item => item.ArtifactType.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    private static string CreateDiagnosticsMarkdown(
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentation ir,
        PbirLocalWriteRequest request)
    {
        var requestedArtifacts = string.Join(
            ", ",
            request.RequestedArtifactTypes
                .Select(artifactType => artifactType.ToString())
                .OrderBy(value => value, StringComparer.Ordinal));

        return string.Join(
            "\n",
            [
                "# PBIR Local Write Diagnostics",
                string.Empty,
                $"Request: {request.RequestId}",
                $"PBIR IR: {ir.Metadata.IrId}",
                $"Preview manifest: {previewManifest.Metadata.ManifestId}",
                $"Target output root: {NormalizePath(request.TargetOutputRoot)}",
                $"Requested artifacts: {requestedArtifacts}",
                "Dry-run only: true",
                "No files will be written by this boundary.",
                "Deployable PBIR artifacts remain forbidden.",
                string.Empty
            ]);
    }

    private static string ComputeFileSetHash(IReadOnlyList<PbirLocalPlannedWriteFile> files)
    {
        var material = string.Join(
            "\n",
            files
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .Select(file => $"{file.RelativePath}|{file.ContentType}|{file.ArtifactType}|{file.ByteLength}|{file.HashSha256}|{file.OverwriteRisk}"));

        return ComputeSha256(material);
    }

    private static string ComputeManifestHash(PbirLocalWriteManifest manifest)
    {
        return ComputeSha256(Serialize(new
        {
            manifest.SchemaVersion,
            manifest.Writer,
            manifest.Metadata,
            manifest.SourceLineage,
            manifest.PlannedFiles,
            manifest.OverwriteRisk,
            manifest.RollbackPlan,
            manifest.Warnings,
            manifest.RejectedArtifacts,
            manifest.Hashes.InputHash,
            manifest.Hashes.FileSetHash
        }));
    }

    private static string JoinLocalPath(string root, string relativePath)
    {
        return $"{NormalizePath(root).TrimEnd('/')}/{NormalizePath(relativePath).TrimStart('/')}";
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim();
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private sealed record PlannedFileDescriptor(
        string RelativePath,
        string ContentType,
        string Purpose,
        string SourceHash,
        string HashSha256,
        int ByteLength);
}
