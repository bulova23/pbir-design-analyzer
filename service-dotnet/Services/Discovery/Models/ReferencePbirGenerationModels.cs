using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class ReferencePbirGeneratorContract
{
    internal const string SchemaVersionV1 = "reference-pbir-generator/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "GeneratorSchemaVersion",
        "GenerationManifestRef",
        "PbirGenerationSpecificationRef",
        "ArchitectureCertificationRef",
        "DryRun",
        "LocalOutputOnly",
    ];
}

internal static class ReferenceGenerationOutputContract
{
    internal const string SchemaVersionV1 = "reference-generation-output/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "Metadata",
        "Metadata.OutputId",
        "Metadata.GeneratorSchemaVersion",
        "Metadata.GenerationMode",
        "Metadata.GeneratedUtc",
        "Metadata.DryRun",
        "Metadata.LocalOutputOnly",
        "Metadata.DeploymentEnabled",
        "Metadata.ProviderInvocationEnabled",
        "Metadata.MicrosoftApiInvocationEnabled",
        "Metadata.CliInvocationEnabled",
        "SourceReferences",
        "SourceReferences.GenerationManifestRef",
        "SourceReferences.PbirGenerationSpecificationRef",
        "SourceReferences.ArchitectureCertificationRef",
        "CanonicalIr",
        "CanonicalIr.PbirIrRef",
        "CanonicalIr.SchemaVersion",
        "CanonicalIr.InputHash",
        "CanonicalIr.ContentHash",
        "CanonicalIr.LineageHash",
        "CanonicalIr.ImmutableIrLineage",
        "GeneratedFiles",
        "GeneratedFiles.RelativePath",
        "GeneratedFiles.ContentType",
        "GeneratedFiles.Purpose",
        "GeneratedFiles.Content",
        "GeneratedFiles.ByteLength",
        "GeneratedFiles.HashSha256",
        "Lineage",
        "Lineage.UpstreamLineage",
        "Lineage.ImmutableLineage",
        "Hashes",
        "Hashes.InputHash",
        "Hashes.FileSetHash",
        "Hashes.OutputHash",
    ];
}

internal enum ReferenceGenerationReadinessState
{
    Rejected,
    Generated,
}

internal sealed record ReferenceGenerationOptions(
    bool DryRun,
    bool LocalOutputOnly,
    bool DeploymentRequested,
    bool ProviderInvocationRequested,
    bool MicrosoftApiRequested,
    bool CliRequested,
    bool NetworkAccessRequested)
{
    internal static ReferenceGenerationOptions Default { get; } =
        new(
            DryRun: true,
            LocalOutputOnly: true,
            DeploymentRequested: false,
            ProviderInvocationRequested: false,
            MicrosoftApiRequested: false,
            CliRequested: false,
            NetworkAccessRequested: false);
}

internal sealed record ReferenceGenerationSafetyGateResult(
    [property: JsonPropertyName("isAllowed")] bool IsAllowed,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record ReferenceGenerationMetadata(
    [property: JsonPropertyName("outputId")] string OutputId,
    [property: JsonPropertyName("generatorSchemaVersion")] string GeneratorSchemaVersion,
    [property: JsonPropertyName("generationMode")] string GenerationMode,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc,
    [property: JsonPropertyName("dryRun")] bool DryRun,
    [property: JsonPropertyName("localOutputOnly")] bool LocalOutputOnly,
    [property: JsonPropertyName("deploymentEnabled")] bool DeploymentEnabled,
    [property: JsonPropertyName("providerInvocationEnabled")] bool ProviderInvocationEnabled,
    [property: JsonPropertyName("microsoftApiInvocationEnabled")] bool MicrosoftApiInvocationEnabled,
    [property: JsonPropertyName("cliInvocationEnabled")] bool CliInvocationEnabled);

internal sealed record ReferenceGenerationSourceReferences(
    [property: JsonPropertyName("generationManifestRef")] string GenerationManifestRef,
    [property: JsonPropertyName("pbirGenerationSpecificationRef")] string PbirGenerationSpecificationRef,
    [property: JsonPropertyName("architectureCertificationRef")] string ArchitectureCertificationRef);

internal sealed record ReferenceGenerationCanonicalIrSummary(
    [property: JsonPropertyName("pbirIrRef")] string PbirIrRef,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("contentHash")] string ContentHash,
    [property: JsonPropertyName("lineageHash")] string LineageHash,
    [property: JsonPropertyName("immutableIrLineage")] IReadOnlyList<string> ImmutableIrLineage);

internal sealed record ReferenceGeneratedFile(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("byteLength")] int ByteLength,
    [property: JsonPropertyName("hashSha256")] string HashSha256);

internal sealed record ReferenceGenerationLineage(
    [property: JsonPropertyName("upstreamLineage")] IReadOnlyList<PlanningLineageEntry> UpstreamLineage,
    [property: JsonPropertyName("immutableLineage")] IReadOnlyList<string> ImmutableLineage);

internal sealed record ReferenceGenerationHashes(
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("fileSetHash")] string FileSetHash,
    [property: JsonPropertyName("outputHash")] string OutputHash);

internal sealed record ReferenceGenerationOutput(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("metadata")] ReferenceGenerationMetadata Metadata,
    [property: JsonPropertyName("sourceReferences")] ReferenceGenerationSourceReferences SourceReferences,
    [property: JsonPropertyName("canonicalIr")] ReferenceGenerationCanonicalIrSummary CanonicalIr,
    [property: JsonPropertyName("generatedFiles")] IReadOnlyList<ReferenceGeneratedFile> GeneratedFiles,
    [property: JsonPropertyName("lineage")] ReferenceGenerationLineage Lineage,
    [property: JsonPropertyName("hashes")] ReferenceGenerationHashes Hashes);

internal sealed record ReferenceGenerationDiagnostics(
    IReadOnlyList<string> SafetyRejections,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static ReferenceGenerationDiagnostics Empty { get; } = new([], []);
}

internal sealed record ReferenceGenerationState(
    ReferenceGenerationOutput? Output,
    ReferenceGenerationSafetyGateResult Safety,
    ReferenceGenerationDiagnostics Diagnostics,
    ReferenceGenerationReadinessState Readiness);
