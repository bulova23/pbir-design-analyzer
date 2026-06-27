using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirLocalPreviewFileWriterContract
{
    internal const string SchemaVersionV1 = "pbir-local-preview-writer/v1";
}

internal static class PbirLocalPreviewWriteResultContract
{
    internal const string SchemaVersionV1 = "pbir-local-preview-write-result/v1";
}

internal enum PbirLocalPreviewFileWriterReadinessState
{
    Rejected,
    Written,
    WrittenWithSkippedFiles,
}

internal sealed record PbirLocalPreviewFileWriterSafetyGateResult(
    [property: JsonPropertyName("isAllowed")] bool IsAllowed,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record PbirLocalPreviewFileWriterDescriptor(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("localOnly")] bool LocalOnly,
    [property: JsonPropertyName("previewOnly")] bool PreviewOnly,
    [property: JsonPropertyName("deployableArtifactsAllowed")] bool DeployableArtifactsAllowed);

internal sealed record PbirLocalPreviewWriteResultMetadata(
    [property: JsonPropertyName("resultId")] string ResultId,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc,
    [property: JsonPropertyName("outputBaseDirectory")] string OutputBaseDirectory,
    [property: JsonPropertyName("targetOutputRoot")] string TargetOutputRoot);

internal sealed record PbirLocalPreviewWriteSourceLineage(
    [property: JsonPropertyName("writeRequestRef")] string WriteRequestRef,
    [property: JsonPropertyName("sourceWriteManifestRef")] string SourceWriteManifestRef,
    [property: JsonPropertyName("sourceWriteManifestSchemaVersion")] string SourceWriteManifestSchemaVersion,
    [property: JsonPropertyName("sourceWriteManifestHash")] string SourceWriteManifestHash,
    [property: JsonPropertyName("pbirIrRef")] string PbirIrRef,
    [property: JsonPropertyName("pbirIrSchemaVersion")] string PbirIrSchemaVersion,
    [property: JsonPropertyName("pbirIrContentHash")] string PbirIrContentHash,
    [property: JsonPropertyName("previewManifestRef")] string PreviewManifestRef,
    [property: JsonPropertyName("previewManifestSchemaVersion")] string PreviewManifestSchemaVersion,
    [property: JsonPropertyName("previewManifestHash")] string PreviewManifestHash,
    [property: JsonPropertyName("immutableLineage")] IReadOnlyList<string> ImmutableLineage);

internal sealed record PbirLocalPreviewWrittenFile(
    [property: JsonPropertyName("artifactType")] PbirLocalWriteArtifactType ArtifactType,
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("intendedPath")] string IntendedPath,
    [property: JsonPropertyName("physicalPath")] string PhysicalPath,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("sourceHash")] string SourceHash,
    [property: JsonPropertyName("hashSha256")] string HashSha256,
    [property: JsonPropertyName("byteLength")] int ByteLength);

internal sealed record PbirLocalPreviewSkippedFile(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("intendedPath")] string IntendedPath,
    [property: JsonPropertyName("reason")] string Reason);

internal sealed record PbirLocalPreviewRejectedFile(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("intendedPath")] string IntendedPath,
    [property: JsonPropertyName("reason")] string Reason);

internal sealed record PbirLocalPreviewRollbackPlanReference(
    [property: JsonPropertyName("sourceWriteManifestRef")] string SourceWriteManifestRef,
    [property: JsonPropertyName("policy")] PbirLocalRollbackPolicy Policy,
    [property: JsonPropertyName("actionCount")] int ActionCount,
    [property: JsonPropertyName("rollbackPlanHash")] string RollbackPlanHash);

internal sealed record PbirLocalPreviewWriteResultHashes(
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("fileSetHash")] string FileSetHash,
    [property: JsonPropertyName("resultHash")] string ResultHash);

internal sealed record PbirLocalPreviewWriteResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("writer")] PbirLocalPreviewFileWriterDescriptor Writer,
    [property: JsonPropertyName("metadata")] PbirLocalPreviewWriteResultMetadata Metadata,
    [property: JsonPropertyName("sourceLineage")] PbirLocalPreviewWriteSourceLineage SourceLineage,
    [property: JsonPropertyName("writtenFiles")] IReadOnlyList<PbirLocalPreviewWrittenFile> WrittenFiles,
    [property: JsonPropertyName("rollbackPlanReference")] PbirLocalPreviewRollbackPlanReference RollbackPlanReference,
    [property: JsonPropertyName("skippedFiles")] IReadOnlyList<PbirLocalPreviewSkippedFile> SkippedFiles,
    [property: JsonPropertyName("rejectedFiles")] IReadOnlyList<PbirLocalPreviewRejectedFile> RejectedFiles,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("hashes")] PbirLocalPreviewWriteResultHashes Hashes);

internal sealed record PbirLocalPreviewFileWriterDiagnostics(
    IReadOnlyList<string> SafetyRejections,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static PbirLocalPreviewFileWriterDiagnostics Empty { get; } = new([], []);
}

internal sealed record PbirLocalPreviewFileWriterState(
    PbirLocalPreviewWriteResult? Result,
    PbirLocalPreviewFileWriterSafetyGateResult Safety,
    PbirLocalPreviewFileWriterDiagnostics Diagnostics,
    PbirLocalPreviewFileWriterReadinessState Readiness);
