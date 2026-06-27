using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal static class PbirLocalPreviewFileContentFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    internal static PbirLocalPreviewFileContent? TryCreateContent(
        PbirPreviewArtifact previewArtifact,
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentation ir,
        PbirLocalWriteRequest request,
        PbirLocalPlannedWriteFile plannedFile)
    {
        return plannedFile.ArtifactType switch
        {
            PbirLocalWriteArtifactType.PreviewMarkdown => FromPreviewArtifact(
                previewArtifact,
                plannedFile,
                PbirPreviewOutputType.Markdown),
            PbirLocalWriteArtifactType.PreviewJson => FromPreviewArtifact(
                previewArtifact,
                plannedFile,
                PbirPreviewOutputType.Json),
            PbirLocalWriteArtifactType.IrJson => FromContent(
                plannedFile,
                Serialize(ir)),
            PbirLocalWriteArtifactType.ManifestJson => FromContent(
                plannedFile,
                Serialize(previewManifest)),
            PbirLocalWriteArtifactType.DiagnosticsMarkdown => FromContent(
                plannedFile,
                CreateDiagnosticsMarkdown(previewManifest, ir, request)),
            _ => null
        };
    }

    internal static string CreateDiagnosticsMarkdown(
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

    internal static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    internal static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static PbirLocalPreviewFileContent? FromPreviewArtifact(
        PbirPreviewArtifact previewArtifact,
        PbirLocalPlannedWriteFile plannedFile,
        PbirPreviewOutputType outputType)
    {
        var sourceFile = previewArtifact.GeneratedFiles.SingleOrDefault(file => file.OutputType == outputType);
        if (sourceFile is null)
        {
            return null;
        }

        return FromContent(plannedFile, sourceFile.Content);
    }

    private static PbirLocalPreviewFileContent FromContent(PbirLocalPlannedWriteFile plannedFile, string content)
    {
        return new PbirLocalPreviewFileContent(
            plannedFile,
            content,
            Encoding.UTF8.GetByteCount(content),
            ComputeSha256(content));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim();
    }
}

internal sealed record PbirLocalPreviewFileContent(
    PbirLocalPlannedWriteFile PlannedFile,
    string Content,
    int ByteLength,
    string HashSha256);
