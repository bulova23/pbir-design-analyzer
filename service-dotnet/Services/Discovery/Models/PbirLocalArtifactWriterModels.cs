using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirLocalArtifactWriterContract
{
    internal const string SchemaVersionV1 = "pbir-local-writer/v1";
}

internal static class PbirLocalWriteRequestContract
{
    internal const string SchemaVersionV1 = "pbir-local-write-request/v1";
}

internal static class PbirLocalWriteManifestContract
{
    internal const string SchemaVersionV1 = "pbir-local-write-manifest/v1";
}

internal enum PbirLocalWriteArtifactType
{
    PreviewMarkdown,
    PreviewJson,
    IrJson,
    ManifestJson,
    DiagnosticsMarkdown,
    ReportJson,
    DefinitionPbir,
    ModelBim,
    Tmdl,
    PbipProject,
    DeployableReport,
}

internal enum PbirLocalOverwritePolicy
{
    FailIfExists,
    SkipExisting,
    AllowOverwriteOnlyWhenHashMatches,
    OverwriteExisting,
}

internal enum PbirLocalRollbackPolicy
{
    PlanOnly,
    None,
}

internal enum PbirLocalRollbackActionKind
{
    NoOpDryRun,
    RestoreExistingLocalFile,
}

internal enum PbirLocalArtifactWriterReadinessState
{
    Rejected,
    Planned,
    PlannedWithOverwriteRisk,
}

internal sealed record PbirLocalWriteRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("sourceIrRef")] string SourceIrRef,
    [property: JsonPropertyName("sourceIrSchemaVersion")] string SourceIrSchemaVersion,
    [property: JsonPropertyName("sourceIrContentHash")] string SourceIrContentHash,
    [property: JsonPropertyName("sourcePreviewManifestRef")] string SourcePreviewManifestRef,
    [property: JsonPropertyName("sourcePreviewManifestSchemaVersion")] string SourcePreviewManifestSchemaVersion,
    [property: JsonPropertyName("sourcePreviewManifestHash")] string SourcePreviewManifestHash,
    [property: JsonPropertyName("targetOutputRoot")] string TargetOutputRoot,
    [property: JsonPropertyName("requestedArtifactTypes")] IReadOnlyList<PbirLocalWriteArtifactType> RequestedArtifactTypes,
    [property: JsonPropertyName("overwritePolicy")] PbirLocalOverwritePolicy OverwritePolicy,
    [property: JsonPropertyName("rollbackPolicy")] PbirLocalRollbackPolicy RollbackPolicy,
    [property: JsonPropertyName("dryRun")] bool? DryRun,
    [property: JsonPropertyName("deploymentRequested")] bool DeploymentRequested,
    [property: JsonPropertyName("providerInvocationRequested")] bool ProviderInvocationRequested,
    [property: JsonPropertyName("microsoftApiRequested")] bool MicrosoftApiRequested,
    [property: JsonPropertyName("cliRequested")] bool CliRequested,
    [property: JsonPropertyName("microsoftSkillsExecutionRequested")] bool MicrosoftSkillsExecutionRequested)
{
    internal static PbirLocalWriteRequest LocalDryRun(
        string requestId,
        string sourceIrRef,
        string sourceIrSchemaVersion,
        string sourceIrContentHash,
        string sourcePreviewManifestRef,
        string sourcePreviewManifestSchemaVersion,
        string sourcePreviewManifestHash,
        string targetOutputRoot,
        IReadOnlyList<PbirLocalWriteArtifactType> requestedArtifactTypes)
    {
        return new PbirLocalWriteRequest(
            SchemaVersion: PbirLocalWriteRequestContract.SchemaVersionV1,
            RequestId: requestId,
            SourceIrRef: sourceIrRef,
            SourceIrSchemaVersion: sourceIrSchemaVersion,
            SourceIrContentHash: sourceIrContentHash,
            SourcePreviewManifestRef: sourcePreviewManifestRef,
            SourcePreviewManifestSchemaVersion: sourcePreviewManifestSchemaVersion,
            SourcePreviewManifestHash: sourcePreviewManifestHash,
            TargetOutputRoot: targetOutputRoot,
            RequestedArtifactTypes: requestedArtifactTypes,
            OverwritePolicy: PbirLocalOverwritePolicy.FailIfExists,
            RollbackPolicy: PbirLocalRollbackPolicy.PlanOnly,
            DryRun: true,
            DeploymentRequested: false,
            ProviderInvocationRequested: false,
            MicrosoftApiRequested: false,
            CliRequested: false,
            MicrosoftSkillsExecutionRequested: false);
    }
}

internal sealed record PbirLocalArtifactWriterSafetyGateResult(
    [property: JsonPropertyName("isAllowed")] bool IsAllowed,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record PbirLocalArtifactWriterDescriptor(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("localOnly")] bool LocalOnly,
    [property: JsonPropertyName("dryRunOnly")] bool DryRunOnly);

internal sealed record PbirLocalWriteManifestMetadata(
    [property: JsonPropertyName("manifestId")] string ManifestId,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc,
    [property: JsonPropertyName("targetOutputRoot")] string TargetOutputRoot);

internal sealed record PbirLocalWriteSourceLineage(
    [property: JsonPropertyName("writeRequestRef")] string WriteRequestRef,
    [property: JsonPropertyName("pbirIrRef")] string PbirIrRef,
    [property: JsonPropertyName("pbirIrSchemaVersion")] string PbirIrSchemaVersion,
    [property: JsonPropertyName("pbirIrContentHash")] string PbirIrContentHash,
    [property: JsonPropertyName("previewManifestRef")] string PreviewManifestRef,
    [property: JsonPropertyName("previewManifestSchemaVersion")] string PreviewManifestSchemaVersion,
    [property: JsonPropertyName("previewManifestHash")] string PreviewManifestHash,
    [property: JsonPropertyName("upstreamLineage")] IReadOnlyList<PlanningLineageEntry> UpstreamLineage,
    [property: JsonPropertyName("immutableLineage")] IReadOnlyList<string> ImmutableLineage);

internal sealed record PbirLocalPlannedWriteFile(
    [property: JsonPropertyName("artifactType")] PbirLocalWriteArtifactType ArtifactType,
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("intendedPath")] string IntendedPath,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("sourceHash")] string SourceHash,
    [property: JsonPropertyName("hashSha256")] string HashSha256,
    [property: JsonPropertyName("byteLength")] int ByteLength,
    [property: JsonPropertyName("overwriteRisk")] bool OverwriteRisk,
    [property: JsonPropertyName("willWrite")] bool WillWrite);

internal sealed record PbirLocalOverwriteRisk(
    [property: JsonPropertyName("hasRisk")] bool HasRisk,
    [property: JsonPropertyName("policy")] PbirLocalOverwritePolicy Policy,
    [property: JsonPropertyName("riskPaths")] IReadOnlyList<string> RiskPaths);

internal sealed record PbirLocalRollbackAction(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("intendedPath")] string IntendedPath,
    [property: JsonPropertyName("actionKind")] PbirLocalRollbackActionKind ActionKind,
    [property: JsonPropertyName("reason")] string Reason);

internal sealed record PbirLocalRollbackPlan(
    [property: JsonPropertyName("policy")] PbirLocalRollbackPolicy Policy,
    [property: JsonPropertyName("dryRunOnly")] bool DryRunOnly,
    [property: JsonPropertyName("protectedExistingPaths")] IReadOnlyList<string> ProtectedExistingPaths,
    [property: JsonPropertyName("actions")] IReadOnlyList<PbirLocalRollbackAction> Actions);

internal sealed record PbirLocalRejectedArtifact(
    [property: JsonPropertyName("artifactType")] PbirLocalWriteArtifactType ArtifactType,
    [property: JsonPropertyName("reason")] string Reason);

internal sealed record PbirLocalWriteManifestHashes(
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("fileSetHash")] string FileSetHash,
    [property: JsonPropertyName("manifestHash")] string ManifestHash);

internal sealed record PbirLocalWriteManifest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("writer")] PbirLocalArtifactWriterDescriptor Writer,
    [property: JsonPropertyName("metadata")] PbirLocalWriteManifestMetadata Metadata,
    [property: JsonPropertyName("sourceLineage")] PbirLocalWriteSourceLineage SourceLineage,
    [property: JsonPropertyName("plannedFiles")] IReadOnlyList<PbirLocalPlannedWriteFile> PlannedFiles,
    [property: JsonPropertyName("overwriteRisk")] PbirLocalOverwriteRisk OverwriteRisk,
    [property: JsonPropertyName("rollbackPlan")] PbirLocalRollbackPlan RollbackPlan,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("rejectedArtifacts")] IReadOnlyList<PbirLocalRejectedArtifact> RejectedArtifacts,
    [property: JsonPropertyName("hashes")] PbirLocalWriteManifestHashes Hashes);

internal sealed record PbirLocalArtifactWriterDiagnostics(
    IReadOnlyList<string> SafetyRejections,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static PbirLocalArtifactWriterDiagnostics Empty { get; } = new([], []);
}

internal sealed record PbirLocalArtifactWriterState(
    PbirLocalWriteManifest? Manifest,
    PbirLocalArtifactWriterSafetyGateResult Safety,
    PbirLocalArtifactWriterDiagnostics Diagnostics,
    PbirLocalArtifactWriterReadinessState Readiness);
