using System.IO;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirLocalPreviewFileWriterService
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly PbirLocalPreviewFileWriterSafetyGate _safetyGate;

    internal PbirLocalPreviewFileWriterService()
        : this(new PbirLocalPreviewFileWriterSafetyGate())
    {
    }

    internal PbirLocalPreviewFileWriterService(PbirLocalPreviewFileWriterSafetyGate safetyGate)
    {
        _safetyGate = safetyGate;
    }

    internal PbirLocalPreviewFileWriterState WritePreviewFiles(
        PbirPreviewArtifact previewArtifact,
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentationState irState,
        PbirLocalWriteRequest request,
        PbirLocalWriteManifest writeManifest,
        string outputBaseDirectory,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(previewArtifact);
        ArgumentNullException.ThrowIfNull(previewManifest);
        ArgumentNullException.ThrowIfNull(irState);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(writeManifest);

        var safety = _safetyGate.Validate(
            previewArtifact,
            previewManifest,
            irState,
            request,
            writeManifest,
            outputBaseDirectory);
        if (!safety.IsAllowed || irState.Ir is null)
        {
            return new PbirLocalPreviewFileWriterState(
                Result: null,
                Safety: safety,
                Diagnostics: new PbirLocalPreviewFileWriterDiagnostics(
                    SafetyRejections: safety.Reasons,
                    BoundaryViolations: safety.Reasons),
                Readiness: PbirLocalPreviewFileWriterReadinessState.Rejected);
        }

        var ir = irState.Ir;
        var baseDirectory = Path.GetFullPath(outputBaseDirectory);
        var contentByPath = writeManifest.PlannedFiles
            .Select(file => PbirLocalPreviewFileContentFactory.TryCreateContent(previewArtifact, previewManifest, ir, request, file))
            .OfType<PbirLocalPreviewFileContent>()
            .ToDictionary(content => content.PlannedFile.RelativePath, StringComparer.Ordinal);
        var overwriteRejection = FindOverwriteRejection(writeManifest.PlannedFiles, contentByPath, baseDirectory, request.OverwritePolicy);
        if (overwriteRejection is not null)
        {
            return new PbirLocalPreviewFileWriterState(
                Result: null,
                Safety: new PbirLocalPreviewFileWriterSafetyGateResult(
                    IsAllowed: false,
                    Reasons: [overwriteRejection]),
                Diagnostics: new PbirLocalPreviewFileWriterDiagnostics(
                    SafetyRejections: [overwriteRejection],
                    BoundaryViolations: [overwriteRejection]),
                Readiness: PbirLocalPreviewFileWriterReadinessState.Rejected);
        }

        var writtenFiles = new List<PbirLocalPreviewWrittenFile>();
        foreach (var plannedFile in writeManifest.PlannedFiles.OrderBy(file => file.RelativePath, StringComparer.Ordinal))
        {
            var content = contentByPath[plannedFile.RelativePath];
            var physicalPath = GetPhysicalPath(baseDirectory, plannedFile.IntendedPath);
            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
            File.WriteAllText(physicalPath, content.Content, Utf8NoBom);
            writtenFiles.Add(new PbirLocalPreviewWrittenFile(
                ArtifactType: plannedFile.ArtifactType,
                RelativePath: plannedFile.RelativePath,
                IntendedPath: plannedFile.IntendedPath,
                PhysicalPath: physicalPath,
                ContentType: plannedFile.ContentType,
                SourceHash: plannedFile.SourceHash,
                HashSha256: plannedFile.HashSha256,
                ByteLength: plannedFile.ByteLength));
        }

        var resultId = $"pbirLocalPreviewWriteResult:{writeManifest.Metadata.ManifestId}";
        var sourceLineage = CreateSourceLineage(ir, previewManifest, request, writeManifest, resultId);
        var rollbackReference = new PbirLocalPreviewRollbackPlanReference(
            SourceWriteManifestRef: writeManifest.Metadata.ManifestId,
            Policy: writeManifest.RollbackPlan.Policy,
            ActionCount: writeManifest.RollbackPlan.Actions.Count,
            RollbackPlanHash: PbirLocalPreviewFileContentFactory.ComputeSha256(Serialize(writeManifest.RollbackPlan)));
        var inputHash = PbirLocalPreviewFileContentFactory.ComputeSha256(Serialize(new
        {
            previewArtifact,
            previewManifest,
            ir,
            request,
            writeManifest
        }));
        var fileSetHash = ComputeFileSetHash(writtenFiles);
        var resultWithoutHash = new PbirLocalPreviewWriteResult(
            SchemaVersion: PbirLocalPreviewWriteResultContract.SchemaVersionV1,
            Writer: new PbirLocalPreviewFileWriterDescriptor(
                SchemaVersion: PbirLocalPreviewFileWriterContract.SchemaVersionV1,
                LocalOnly: true,
                PreviewOnly: true,
                DeployableArtifactsAllowed: false),
            Metadata: new PbirLocalPreviewWriteResultMetadata(
                ResultId: resultId,
                GeneratedUtc: generatedUtc.UtcDateTime,
                OutputBaseDirectory: baseDirectory,
                TargetOutputRoot: NormalizePath(writeManifest.Metadata.TargetOutputRoot)),
            SourceLineage: sourceLineage,
            WrittenFiles: writtenFiles.OrderBy(file => file.RelativePath, StringComparer.Ordinal).ToArray(),
            RollbackPlanReference: rollbackReference,
            SkippedFiles: [],
            RejectedFiles: [],
            Warnings:
            [
                "Only non-deployable local preview files were written.",
                "Rollback metadata was recorded; automatic rollback is not implemented.",
                "Deployable PBIR artifacts remain forbidden."
            ],
            Hashes: new PbirLocalPreviewWriteResultHashes(
                InputHash: inputHash,
                FileSetHash: fileSetHash,
                ResultHash: string.Empty));
        var result = resultWithoutHash with
        {
            Hashes = resultWithoutHash.Hashes with
            {
                ResultHash = ComputeResultHash(resultWithoutHash)
            }
        };

        return new PbirLocalPreviewFileWriterState(
            Result: result,
            Safety: safety,
            Diagnostics: PbirLocalPreviewFileWriterDiagnostics.Empty,
            Readiness: result.SkippedFiles.Count == 0
                ? PbirLocalPreviewFileWriterReadinessState.Written
                : PbirLocalPreviewFileWriterReadinessState.WrittenWithSkippedFiles);
    }

    private static string? FindOverwriteRejection(
        IReadOnlyList<PbirLocalPlannedWriteFile> plannedFiles,
        IReadOnlyDictionary<string, PbirLocalPreviewFileContent> contentByPath,
        string baseDirectory,
        PbirLocalOverwritePolicy overwritePolicy)
    {
        foreach (var plannedFile in plannedFiles.OrderBy(file => file.RelativePath, StringComparer.Ordinal))
        {
            var physicalPath = GetPhysicalPath(baseDirectory, plannedFile.IntendedPath);
            if (!File.Exists(physicalPath))
            {
                continue;
            }

            if (overwritePolicy == PbirLocalOverwritePolicy.FailIfExists)
            {
                return "existing files are not allowed with failIfExists overwrite policy.";
            }

            var existingHash = PbirLocalPreviewFileContentFactory.ComputeSha256(File.ReadAllText(physicalPath, Encoding.UTF8));
            var plannedHash = contentByPath[plannedFile.RelativePath].HashSha256;
            if (!string.Equals(existingHash, plannedHash, StringComparison.Ordinal))
            {
                return "existing file hash must match the approved manifest hash before overwrite.";
            }
        }

        return null;
    }

    private static PbirLocalPreviewWriteSourceLineage CreateSourceLineage(
        PbirIntermediateRepresentation ir,
        PbirPreviewManifest previewManifest,
        PbirLocalWriteRequest request,
        PbirLocalWriteManifest writeManifest,
        string resultId)
    {
        return new PbirLocalPreviewWriteSourceLineage(
            WriteRequestRef: request.RequestId,
            SourceWriteManifestRef: writeManifest.Metadata.ManifestId,
            SourceWriteManifestSchemaVersion: writeManifest.SchemaVersion,
            SourceWriteManifestHash: writeManifest.Hashes.ManifestHash,
            PbirIrRef: ir.Metadata.IrId,
            PbirIrSchemaVersion: ir.Metadata.SchemaVersion,
            PbirIrContentHash: ir.Hashes.ContentHash,
            PreviewManifestRef: previewManifest.Metadata.ManifestId,
            PreviewManifestSchemaVersion: previewManifest.SchemaVersion,
            PreviewManifestHash: previewManifest.Hashes.ManifestHash,
            ImmutableLineage: writeManifest.SourceLineage.ImmutableLineage
                .Append(resultId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reference => reference, StringComparer.Ordinal)
                .ToArray());
    }

    private static string ComputeFileSetHash(IReadOnlyList<PbirLocalPreviewWrittenFile> files)
    {
        var material = string.Join(
            "\n",
            files
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .Select(file => $"{file.RelativePath}|{file.IntendedPath}|{file.ContentType}|{file.ByteLength}|{file.HashSha256}"));

        return PbirLocalPreviewFileContentFactory.ComputeSha256(material);
    }

    private static string ComputeResultHash(PbirLocalPreviewWriteResult result)
    {
        return PbirLocalPreviewFileContentFactory.ComputeSha256(Serialize(new
        {
            result.SchemaVersion,
            result.Writer,
            result.Metadata,
            result.SourceLineage,
            result.WrittenFiles,
            result.RollbackPlanReference,
            result.SkippedFiles,
            result.RejectedFiles,
            result.Warnings,
            result.Hashes.InputHash,
            result.Hashes.FileSetHash
        }));
    }

    private static string GetPhysicalPath(string baseDirectory, string intendedPath)
    {
        var physicalPath = Path.GetFullPath(Path.Combine(baseDirectory, NormalizePath(intendedPath)));
        var normalizedBase = baseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!physicalPath.StartsWith(normalizedBase, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Planned file path escaped the output base directory.");
        }

        return physicalPath;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim();
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
