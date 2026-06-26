using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirPreviewSerializerValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    internal PbirPreviewSerializerValidationResult Validate(
        PbirPreviewArtifact output,
        PbirPreviewManifest manifest,
        PbirIntermediateRepresentationState irState,
        PbirSerializerRequest serializerRequest)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(irState);
        ArgumentNullException.ThrowIfNull(serializerRequest);

        var missingRequiredSections = new List<string>();
        var invalidReferences = new List<string>();
        var unsupportedOutputTypes = new List<string>();
        var lineageViolations = new List<string>();
        var hashViolations = new List<string>();
        var boundaryViolations = new List<string>();

        var ir = irState.Ir;
        if (ir is null)
        {
            missingRequiredSections.Add("pbirIr");
        }

        if (!string.Equals(output.SchemaVersion, PbirPreviewArtifactContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            invalidReferences.Add("preview artifact schema version must be pbir-preview-artifact/v1.");
        }

        if (!string.Equals(manifest.SchemaVersion, PbirPreviewManifestContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            invalidReferences.Add("preview manifest schema version must be pbir-preview-manifest/v1.");
        }

        if (output.GeneratedFiles.Count == 0)
        {
            missingRequiredSections.Add("generatedFiles");
        }

        ValidateSourceReferences(output, manifest, ir, serializerRequest, invalidReferences);
        ValidateGeneratedFiles(output, unsupportedOutputTypes, hashViolations, boundaryViolations);
        ValidateManifest(manifest, output, ir, serializerRequest, lineageViolations, hashViolations, boundaryViolations);

        return new PbirPreviewSerializerValidationResult(
            new PbirPreviewSerializerValidationDiagnostics(
                MissingRequiredSections: DistinctAndOrder(missingRequiredSections),
                InvalidReferences: DistinctAndOrder(invalidReferences),
                UnsupportedOutputTypes: DistinctAndOrder(unsupportedOutputTypes),
                LineageViolations: DistinctAndOrder(lineageViolations),
                HashViolations: DistinctAndOrder(hashViolations),
                BoundaryViolations: DistinctAndOrder(boundaryViolations)));
    }

    internal static string ComputeFileSetHash(IReadOnlyList<PbirPreviewGeneratedFile> files)
    {
        var material = string.Join(
            "\n",
            files
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .Select(file => $"{file.RelativePath}|{file.ContentType}|{file.OutputType}|{file.ByteLength}|{file.HashSha256}"));

        return ComputeSha256(material);
    }

    internal static string ComputeManifestHash(PbirPreviewManifest manifest)
    {
        return ComputeSha256(Serialize(new
        {
            manifest.SchemaVersion,
            manifest.Metadata,
            manifest.SourceReferences,
            manifest.GeneratedFiles,
            manifest.Lineage,
            manifest.Warnings,
            manifest.UnsupportedSections,
            manifest.Hashes.InputHash,
            manifest.Hashes.FileSetHash
        }));
    }

    private static void ValidateSourceReferences(
        PbirPreviewArtifact output,
        PbirPreviewManifest manifest,
        PbirIntermediateRepresentation? ir,
        PbirSerializerRequest serializerRequest,
        ICollection<string> invalidReferences)
    {
        if (!string.Equals(output.SourceReferences.SerializerRequestRef, serializerRequest.RequestId, StringComparison.Ordinal) ||
            !string.Equals(manifest.SourceReferences.SerializerRequestRef, serializerRequest.RequestId, StringComparison.Ordinal))
        {
            invalidReferences.Add("preview source references must include the serializer request id.");
        }

        if (ir is null)
        {
            return;
        }

        if (!string.Equals(output.SourceReferences.PbirIrRef, ir.Metadata.IrId, StringComparison.Ordinal) ||
            !string.Equals(manifest.SourceReferences.PbirIrRef, ir.Metadata.IrId, StringComparison.Ordinal))
        {
            invalidReferences.Add("preview source references must include the PBIR IR id.");
        }

        if (!string.Equals(output.SourceReferences.PbirIrContentHash, ir.Hashes.ContentHash, StringComparison.Ordinal) ||
            !string.Equals(manifest.SourceReferences.PbirIrContentHash, ir.Hashes.ContentHash, StringComparison.Ordinal))
        {
            invalidReferences.Add("preview source references must include the PBIR IR content hash.");
        }
    }

    private static void ValidateGeneratedFiles(
        PbirPreviewArtifact output,
        ICollection<string> unsupportedOutputTypes,
        ICollection<string> hashViolations,
        ICollection<string> boundaryViolations)
    {
        foreach (var file in output.GeneratedFiles)
        {
            if (!Enum.IsDefined(file.OutputType))
            {
                unsupportedOutputTypes.Add("preview output type is unsupported.");
            }

            if (file.RelativePath.EndsWith("report.json", StringComparison.OrdinalIgnoreCase) ||
                file.RelativePath.EndsWith("definition.pbir", StringComparison.OrdinalIgnoreCase) ||
                file.RelativePath.EndsWith(".pbir", StringComparison.OrdinalIgnoreCase) ||
                file.RelativePath.EndsWith(".bim", StringComparison.OrdinalIgnoreCase) ||
                file.RelativePath.EndsWith(".tmdl", StringComparison.OrdinalIgnoreCase) ||
                file.RelativePath.EndsWith(".pbip", StringComparison.OrdinalIgnoreCase))
            {
                boundaryViolations.Add("preview artifacts must not include deployable PBIR files.");
            }

            if (file.ByteLength != Encoding.UTF8.GetByteCount(file.Content) ||
                !string.Equals(file.HashSha256, ComputeSha256(file.Content), StringComparison.Ordinal))
            {
                hashViolations.Add("preview generated file hash or byte length is unstable.");
            }
        }
    }

    private static void ValidateManifest(
        PbirPreviewManifest manifest,
        PbirPreviewArtifact output,
        PbirIntermediateRepresentation? ir,
        PbirSerializerRequest serializerRequest,
        ICollection<string> lineageViolations,
        ICollection<string> hashViolations,
        ICollection<string> boundaryViolations)
    {
        var fileSetHash = ComputeFileSetHash(output.GeneratedFiles);
        if (!string.Equals(output.Hashes.FileSetHash, fileSetHash, StringComparison.Ordinal) ||
            !string.Equals(manifest.Hashes.FileSetHash, fileSetHash, StringComparison.Ordinal))
        {
            hashViolations.Add("preview manifest file-set hash must match generated preview files.");
        }

        var manifestHash = ComputeManifestHash(manifest);
        if (!string.Equals(manifest.Hashes.ManifestHash, manifestHash, StringComparison.Ordinal))
        {
            hashViolations.Add("preview manifest hash must be stable.");
        }

        if (ir is not null &&
            (!manifest.Lineage.ImmutableLineage.Contains(ir.Metadata.IrId, StringComparer.Ordinal) ||
            !manifest.Lineage.ImmutableLineage.Contains(serializerRequest.RequestId, StringComparer.Ordinal) ||
            !manifest.Lineage.ImmutableLineage.Contains(manifest.Metadata.ManifestId, StringComparer.Ordinal)))
        {
            lineageViolations.Add("preview manifest lineage must include PBIR IR, serializer request, and preview manifest references.");
        }

        if (manifest.GeneratedFiles.Any(file =>
            file.RelativePath.EndsWith("report.json", StringComparison.OrdinalIgnoreCase) ||
            file.RelativePath.EndsWith("definition.pbir", StringComparison.OrdinalIgnoreCase)))
        {
            boundaryViolations.Add("preview manifest must not reference deployable PBIR files.");
        }
    }

    private static IReadOnlyList<string> DistinctAndOrder(IEnumerable<string> values)
    {
        return values
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
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
