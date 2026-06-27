using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirPreviewPackageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    internal PbirPreviewPackageState CreatePackage(
        PbirLocalPreviewWriteResult previewWriteResult,
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentationState irState,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(previewWriteResult);
        ArgumentNullException.ThrowIfNull(previewManifest);
        ArgumentNullException.ThrowIfNull(irState);

        var safety = Validate(previewWriteResult, previewManifest, irState);
        if (!safety.IsAllowed || irState.Ir is null)
        {
            return new PbirPreviewPackageState(
                Package: null,
                Safety: safety,
                Diagnostics: new PbirPreviewPackageDiagnostics(
                    SafetyRejections: safety.Reasons,
                    BoundaryViolations: safety.Reasons),
                Readiness: PbirPreviewPackageReadinessState.Rejected);
        }

        var ir = irState.Ir;
        var packageId = $"pbirPreviewPackage:{previewWriteResult.Metadata.ResultId}";
        var fileInventory = previewWriteResult.WrittenFiles
            .Select(file => new PbirPreviewPackageFileInventoryItem(
                ArtifactType: file.ArtifactType,
                RelativePath: NormalizePath(file.RelativePath),
                IntendedPath: NormalizePath(file.IntendedPath),
                PhysicalPath: NormalizePath(file.PhysicalPath),
                ContentType: file.ContentType,
                SourceHash: file.SourceHash,
                HashSha256: file.HashSha256,
                ByteLength: file.ByteLength))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
        var hashInventory = new PbirPreviewPackageHashInventory(
            Entries: CreateHashInventory(previewWriteResult, previewManifest, ir, fileInventory));
        var lineage = new PbirPreviewPackageLineage(
            WriteRequestRef: previewWriteResult.SourceLineage.WriteRequestRef,
            SourceWriteManifestRef: previewWriteResult.SourceLineage.SourceWriteManifestRef,
            SourceWriteManifestHash: previewWriteResult.SourceLineage.SourceWriteManifestHash,
            GenerationManifestRef: ir.References.GenerationManifestRef,
            PbirIrRef: previewWriteResult.SourceLineage.PbirIrRef,
            PbirIrSchemaVersion: previewWriteResult.SourceLineage.PbirIrSchemaVersion,
            PbirIrContentHash: previewWriteResult.SourceLineage.PbirIrContentHash,
            PreviewManifestRef: previewWriteResult.SourceLineage.PreviewManifestRef,
            PreviewManifestSchemaVersion: previewWriteResult.SourceLineage.PreviewManifestSchemaVersion,
            PreviewManifestHash: previewWriteResult.SourceLineage.PreviewManifestHash,
            ImmutableLineage: previewWriteResult.SourceLineage.ImmutableLineage
                .Concat(ir.Lineage.ImmutableLineage)
                .Append(packageId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reference => reference, StringComparer.Ordinal)
                .ToArray());
        var warnings = previewWriteResult.Warnings
            .Append("PBIR preview package contains metadata and references only.")
            .Append("No zip file or deployable PBIR artifact was created.")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(warning => warning, StringComparer.Ordinal)
            .ToArray();
        var rejectedArtifacts = previewWriteResult.RejectedFiles
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
        var inputHash = ComputeSha256(Serialize(new
        {
            previewWriteResult,
            previewManifest,
            ir
        }));
        var inventoryHash = ComputeSha256(Serialize(new
        {
            fileInventory,
            hashInventory
        }));
        var packageWithoutHash = new PbirPreviewPackage(
            SchemaVersion: PbirPreviewPackageContract.SchemaVersionV1,
            PackageDescriptor: new PbirPreviewPackageDescriptor(
                SchemaVersion: PbirPreviewPackageContract.SchemaVersionV1,
                MetadataOnly: true,
                LocalOnly: true,
                ContainsPhysicalFileContent: false,
                ZipCreated: false,
                DeployableArtifactsAllowed: false),
            Metadata: new PbirPreviewPackageMetadata(
                PackageId: packageId,
                GeneratedUtc: generatedUtc.UtcDateTime,
                SourcePreviewWriteResultRef: previewWriteResult.Metadata.ResultId),
            FileInventory: fileInventory,
            HashInventory: hashInventory,
            Lineage: lineage,
            RollbackPlanReference: previewWriteResult.RollbackPlanReference,
            Warnings: warnings,
            RejectedArtifacts: rejectedArtifacts,
            Hashes: new PbirPreviewPackageHashes(
                InputHash: inputHash,
                InventoryHash: inventoryHash,
                PackageHash: string.Empty));
        var package = packageWithoutHash with
        {
            Hashes = packageWithoutHash.Hashes with
            {
                PackageHash = ComputePackageHash(packageWithoutHash)
            }
        };

        return new PbirPreviewPackageState(
            Package: package,
            Safety: safety,
            Diagnostics: PbirPreviewPackageDiagnostics.Empty,
            Readiness: PbirPreviewPackageReadinessState.Packaged);
    }

    private static PbirPreviewPackageSafetyGateResult Validate(
        PbirLocalPreviewWriteResult previewWriteResult,
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentationState irState)
    {
        var reasons = new List<string>();
        var ir = irState.Ir;
        if (ir is null)
        {
            reasons.Add("complete PBIR IR must be provided.");
        }

        if (!string.Equals(previewWriteResult.SchemaVersion, PbirLocalPreviewWriteResultContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            reasons.Add("source preview write result schema version must be pbir-local-preview-write-result/v1.");
        }

        if (!previewWriteResult.Writer.PreviewOnly ||
            !previewWriteResult.Writer.LocalOnly ||
            previewWriteResult.Writer.DeployableArtifactsAllowed)
        {
            reasons.Add("source preview write result must be local preview-only metadata.");
        }

        if (!string.Equals(previewManifest.SchemaVersion, PbirPreviewManifestContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            reasons.Add("source preview manifest schema version must be pbir-preview-manifest/v1.");
        }

        if (ir is not null &&
            (!string.Equals(previewWriteResult.SourceLineage.PbirIrRef, ir.Metadata.IrId, StringComparison.Ordinal) ||
            !string.Equals(previewWriteResult.SourceLineage.PbirIrContentHash, ir.Hashes.ContentHash, StringComparison.Ordinal) ||
            !string.Equals(previewWriteResult.SourceLineage.PreviewManifestRef, previewManifest.Metadata.ManifestId, StringComparison.Ordinal) ||
            !string.Equals(previewWriteResult.SourceLineage.PreviewManifestHash, previewManifest.Hashes.ManifestHash, StringComparison.Ordinal)))
        {
            reasons.Add("preview write result lineage must match the PBIR IR and preview manifest.");
        }

        if (previewWriteResult.WrittenFiles.Count == 0)
        {
            reasons.Add("preview package requires at least one written preview file reference.");
        }

        if (!IsHash(previewWriteResult.Hashes.InputHash) ||
            !IsHash(previewWriteResult.Hashes.FileSetHash) ||
            !IsHash(previewWriteResult.Hashes.ResultHash) ||
            !IsHash(previewManifest.Hashes.ManifestHash) ||
            ir is not null && !IsHash(ir.Hashes.ContentHash))
        {
            reasons.Add("preview package hash inputs must include complete SHA-256 hashes.");
        }

        if (string.IsNullOrWhiteSpace(previewWriteResult.SourceLineage.SourceWriteManifestRef) ||
            string.IsNullOrWhiteSpace(previewWriteResult.SourceLineage.PbirIrRef) ||
            string.IsNullOrWhiteSpace(previewWriteResult.SourceLineage.PreviewManifestRef) ||
            previewWriteResult.SourceLineage.ImmutableLineage.Count == 0)
        {
            reasons.Add("preview package lineage must be complete.");
        }

        foreach (var file in previewWriteResult.WrittenFiles)
        {
            var forbiddenName = GetForbiddenArtifactName(file.ArtifactType) ??
                GetForbiddenPathName(file.RelativePath) ??
                GetForbiddenPathName(file.IntendedPath);
            if (forbiddenName is not null)
            {
                reasons.Add($"preview package references forbidden deployable artifacts: {FormatForbiddenArtifactName(forbiddenName)}.");
            }

            if (!IsHash(file.HashSha256) || file.ByteLength <= 0)
            {
                reasons.Add("preview package file inventory must include complete SHA-256 hashes.");
            }
        }

        if (previewWriteResult.RollbackPlanReference.ActionCount <= 0 ||
            !IsHash(previewWriteResult.RollbackPlanReference.RollbackPlanHash))
        {
            reasons.Add("preview package requires rollback metadata reference.");
        }

        return new PbirPreviewPackageSafetyGateResult(
            IsAllowed: reasons.Count == 0,
            Reasons: reasons
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray());
    }

    private static IReadOnlyList<PbirPreviewPackageHashInventoryEntry> CreateHashInventory(
        PbirLocalPreviewWriteResult previewWriteResult,
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentation ir,
        IReadOnlyList<PbirPreviewPackageFileInventoryItem> files)
    {
        var entries = files
            .Select(file => new PbirPreviewPackageHashInventoryEntry(
                HashKind: "file",
                ReferenceId: file.RelativePath,
                HashSha256: file.HashSha256,
                Description: "Hash of a written local preview file reference."))
            .Concat(
            [
                new PbirPreviewPackageHashInventoryEntry(
                    HashKind: "previewWriteResult",
                    ReferenceId: previewWriteResult.Hashes.ResultHash,
                    HashSha256: previewWriteResult.Hashes.ResultHash,
                    Description: "Hash of pbir-local-preview-write-result/v1."),
                new PbirPreviewPackageHashInventoryEntry(
                    HashKind: "previewManifest",
                    ReferenceId: previewManifest.Hashes.ManifestHash,
                    HashSha256: previewManifest.Hashes.ManifestHash,
                    Description: "Hash of pbir-preview-manifest/v1."),
                new PbirPreviewPackageHashInventoryEntry(
                    HashKind: "pbirIr",
                    ReferenceId: ir.Hashes.ContentHash,
                    HashSha256: ir.Hashes.ContentHash,
                    Description: "Canonical PBIR IR content hash."),
                new PbirPreviewPackageHashInventoryEntry(
                    HashKind: "rollbackPlan",
                    ReferenceId: previewWriteResult.RollbackPlanReference.RollbackPlanHash,
                    HashSha256: previewWriteResult.RollbackPlanReference.RollbackPlanHash,
                    Description: "Rollback plan reference hash.")
            ])
            .OrderBy(entry => entry.HashKind, StringComparer.Ordinal)
            .ThenBy(entry => entry.ReferenceId, StringComparer.Ordinal)
            .ToArray();

        return entries;
    }

    private static string ComputePackageHash(PbirPreviewPackage package)
    {
        return ComputeSha256(Serialize(new
        {
            package.SchemaVersion,
            package.PackageDescriptor,
            package.Metadata,
            package.FileInventory,
            package.HashInventory,
            package.Lineage,
            package.RollbackPlanReference,
            package.Warnings,
            package.RejectedArtifacts,
            package.Hashes.InputHash,
            package.Hashes.InventoryHash
        }));
    }

    internal static bool IsHash(string value)
    {
        return value.Length == 64 && value.All(Uri.IsHexDigit);
    }

    internal static string? GetForbiddenArtifactName(PbirLocalWriteArtifactType artifactType)
    {
        return artifactType switch
        {
            PbirLocalWriteArtifactType.ReportJson => "report.json",
            PbirLocalWriteArtifactType.DefinitionPbir => "definition.pbir",
            PbirLocalWriteArtifactType.ModelBim => "model.bim",
            PbirLocalWriteArtifactType.Tmdl => "TMDL",
            PbirLocalWriteArtifactType.PbipProject => "PBIP project",
            PbirLocalWriteArtifactType.DeployableReport => "deployable report",
            _ => null
        };
    }

    internal static string? GetForbiddenPathName(string path)
    {
        var normalized = NormalizePath(path);
        var fileName = Path.GetFileName(normalized);
        if (fileName.EndsWith(".pbip", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains(".Report/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains(".SemanticModel/", StringComparison.OrdinalIgnoreCase))
        {
            return "PBIP project";
        }

        if (string.Equals(fileName, "report.json", StringComparison.OrdinalIgnoreCase))
        {
            return "report.json";
        }

        if (string.Equals(fileName, "definition.pbir", StringComparison.OrdinalIgnoreCase))
        {
            return "definition.pbir";
        }

        if (string.Equals(fileName, "model.bim", StringComparison.OrdinalIgnoreCase))
        {
            return "model.bim";
        }

        if (fileName.EndsWith(".tmdl", StringComparison.OrdinalIgnoreCase))
        {
            return "TMDL";
        }

        return null;
    }

    internal static string FormatForbiddenArtifactName(string artifactName)
    {
        return string.Equals(artifactName, "TMDL", StringComparison.OrdinalIgnoreCase)
            ? "TMDL"
            : artifactName;
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
}
