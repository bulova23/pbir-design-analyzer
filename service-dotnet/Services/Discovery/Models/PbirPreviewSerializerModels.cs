using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirPreviewArtifactContract
{
    internal const string SchemaVersionV1 = "pbir-preview-artifact/v1";
}

internal static class PbirPreviewManifestContract
{
    internal const string SchemaVersionV1 = "pbir-preview-manifest/v1";
}

internal enum PbirPreviewOutputType
{
    Markdown,
    Json,
    VisualLayoutSummary,
    SemanticBindingSummary,
    NavigationSummary,
}

internal enum PbirPreviewSerializerReadinessState
{
    Rejected,
    Generated,
}

internal sealed record PbirPreviewSerializerOptions(
    [property: JsonPropertyName("outputRoot")] string OutputRoot,
    [property: JsonPropertyName("outputTypes")] IReadOnlyList<PbirPreviewOutputType> OutputTypes,
    [property: JsonPropertyName("requestedOutputFiles")] IReadOnlyList<string> RequestedOutputFiles,
    [property: JsonPropertyName("localOutputOnly")] bool LocalOutputOnly,
    [property: JsonPropertyName("deployableOutputRequested")] bool DeployableOutputRequested,
    [property: JsonPropertyName("deploymentRequested")] bool DeploymentRequested,
    [property: JsonPropertyName("providerInvocationRequested")] bool ProviderInvocationRequested,
    [property: JsonPropertyName("microsoftApiRequested")] bool MicrosoftApiRequested,
    [property: JsonPropertyName("cliRequested")] bool CliRequested,
    [property: JsonPropertyName("microsoftSkillsExecutionRequested")] bool MicrosoftSkillsExecutionRequested)
{
    internal static PbirPreviewSerializerOptions LocalPreview(
        string outputRoot,
        IReadOnlyList<PbirPreviewOutputType> outputTypes)
    {
        return new PbirPreviewSerializerOptions(
            OutputRoot: outputRoot,
            OutputTypes: outputTypes,
            RequestedOutputFiles: [],
            LocalOutputOnly: true,
            DeployableOutputRequested: false,
            DeploymentRequested: false,
            ProviderInvocationRequested: false,
            MicrosoftApiRequested: false,
            CliRequested: false,
            MicrosoftSkillsExecutionRequested: false);
    }
}

internal sealed record PbirPreviewSerializerSafetyGateResult(
    [property: JsonPropertyName("isAllowed")] bool IsAllowed,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record PbirPreviewSourceReferences(
    [property: JsonPropertyName("pbirIrRef")] string PbirIrRef,
    [property: JsonPropertyName("pbirIrSchemaVersion")] string PbirIrSchemaVersion,
    [property: JsonPropertyName("pbirIrContentHash")] string PbirIrContentHash,
    [property: JsonPropertyName("serializerRequestRef")] string SerializerRequestRef);

internal sealed record PbirPreviewArtifactMetadata(
    [property: JsonPropertyName("artifactId")] string ArtifactId,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc,
    [property: JsonPropertyName("outputRoot")] string OutputRoot,
    [property: JsonPropertyName("localOutputOnly")] bool LocalOutputOnly);

internal sealed record PbirPreviewGeneratedFile(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("outputType")] PbirPreviewOutputType OutputType,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("byteLength")] int ByteLength,
    [property: JsonPropertyName("hashSha256")] string HashSha256);

internal sealed record PbirPreviewGeneratedFileReference(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("outputType")] PbirPreviewOutputType OutputType,
    [property: JsonPropertyName("byteLength")] int ByteLength,
    [property: JsonPropertyName("hashSha256")] string HashSha256);

internal sealed record PbirPreviewLineage(
    [property: JsonPropertyName("upstreamLineage")] IReadOnlyList<PlanningLineageEntry> UpstreamLineage,
    [property: JsonPropertyName("immutableLineage")] IReadOnlyList<string> ImmutableLineage);

internal sealed record PbirPreviewArtifactHashes(
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("fileSetHash")] string FileSetHash,
    [property: JsonPropertyName("outputHash")] string OutputHash);

internal sealed record PbirPreviewManifestMetadata(
    [property: JsonPropertyName("manifestId")] string ManifestId,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc);

internal sealed record PbirPreviewManifestHashes(
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("fileSetHash")] string FileSetHash,
    [property: JsonPropertyName("manifestHash")] string ManifestHash);

internal sealed record PbirPreviewArtifact(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("metadata")] PbirPreviewArtifactMetadata Metadata,
    [property: JsonPropertyName("sourceReferences")] PbirPreviewSourceReferences SourceReferences,
    [property: JsonPropertyName("generatedFiles")] IReadOnlyList<PbirPreviewGeneratedFile> GeneratedFiles,
    [property: JsonPropertyName("hashes")] PbirPreviewArtifactHashes Hashes);

internal sealed record PbirPreviewManifest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("metadata")] PbirPreviewManifestMetadata Metadata,
    [property: JsonPropertyName("sourceReferences")] PbirPreviewSourceReferences SourceReferences,
    [property: JsonPropertyName("generatedFiles")] IReadOnlyList<PbirPreviewGeneratedFileReference> GeneratedFiles,
    [property: JsonPropertyName("lineage")] PbirPreviewLineage Lineage,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("unsupportedSections")] IReadOnlyList<string> UnsupportedSections,
    [property: JsonPropertyName("hashes")] PbirPreviewManifestHashes Hashes);

internal sealed record PbirPreviewSerializerValidationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> InvalidReferences,
    IReadOnlyList<string> UnsupportedOutputTypes,
    IReadOnlyList<string> LineageViolations,
    IReadOnlyList<string> HashViolations,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static PbirPreviewSerializerValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], []);
}

internal sealed record PbirPreviewSerializerValidationResult(
    PbirPreviewSerializerValidationDiagnostics Diagnostics)
{
    internal bool IsValid =>
        Diagnostics.MissingRequiredSections.Count == 0 &&
        Diagnostics.InvalidReferences.Count == 0 &&
        Diagnostics.UnsupportedOutputTypes.Count == 0 &&
        Diagnostics.LineageViolations.Count == 0 &&
        Diagnostics.HashViolations.Count == 0 &&
        Diagnostics.BoundaryViolations.Count == 0;
}

internal sealed record PbirPreviewSerializerDiagnostics(
    IReadOnlyList<string> SafetyRejections,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static PbirPreviewSerializerDiagnostics Empty { get; } = new([], []);
}

internal sealed record PbirPreviewSerializerState(
    PbirPreviewArtifact? Output,
    PbirPreviewManifest? Manifest,
    PbirPreviewSerializerSafetyGateResult Safety,
    PbirPreviewSerializerValidationResult Validation,
    PbirPreviewSerializerDiagnostics Diagnostics,
    PbirPreviewSerializerReadinessState Readiness);
