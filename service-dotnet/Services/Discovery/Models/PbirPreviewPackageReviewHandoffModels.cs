using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirPreviewPackageContract
{
    internal const string SchemaVersionV1 = "pbir-preview-package/v1";
}

internal static class PbirReviewHandoffContract
{
    internal const string SchemaVersionV1 = "pbir-review-handoff/v1";
}

internal enum PbirPreviewPackageReadinessState
{
    Rejected,
    Packaged,
}

internal enum PbirReviewHandoffReadinessState
{
    Incomplete,
    ReadyForDesignReview,
    ReadyForAnalyzerReview,
    Blocked,
}

internal enum PbirReviewTarget
{
    DesignStudio,
    AnalyzerWorkspace,
}

internal sealed record PbirPreviewPackageSafetyGateResult(
    [property: JsonPropertyName("isAllowed")] bool IsAllowed,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record PbirPreviewPackageDescriptor(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("metadataOnly")] bool MetadataOnly,
    [property: JsonPropertyName("localOnly")] bool LocalOnly,
    [property: JsonPropertyName("containsPhysicalFileContent")] bool ContainsPhysicalFileContent,
    [property: JsonPropertyName("zipCreated")] bool ZipCreated,
    [property: JsonPropertyName("deployableArtifactsAllowed")] bool DeployableArtifactsAllowed);

internal sealed record PbirPreviewPackageMetadata(
    [property: JsonPropertyName("packageId")] string PackageId,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc,
    [property: JsonPropertyName("sourcePreviewWriteResultRef")] string SourcePreviewWriteResultRef);

internal sealed record PbirPreviewPackageFileInventoryItem(
    [property: JsonPropertyName("artifactType")] PbirLocalWriteArtifactType ArtifactType,
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("intendedPath")] string IntendedPath,
    [property: JsonPropertyName("physicalPath")] string PhysicalPath,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("sourceHash")] string SourceHash,
    [property: JsonPropertyName("hashSha256")] string HashSha256,
    [property: JsonPropertyName("byteLength")] int ByteLength);

internal sealed record PbirPreviewPackageHashInventoryEntry(
    [property: JsonPropertyName("hashKind")] string HashKind,
    [property: JsonPropertyName("referenceId")] string ReferenceId,
    [property: JsonPropertyName("hashSha256")] string HashSha256,
    [property: JsonPropertyName("description")] string Description);

internal sealed record PbirPreviewPackageHashInventory(
    [property: JsonPropertyName("entries")] IReadOnlyList<PbirPreviewPackageHashInventoryEntry> Entries);

internal sealed record PbirPreviewPackageLineage(
    [property: JsonPropertyName("writeRequestRef")] string WriteRequestRef,
    [property: JsonPropertyName("sourceWriteManifestRef")] string SourceWriteManifestRef,
    [property: JsonPropertyName("sourceWriteManifestHash")] string SourceWriteManifestHash,
    [property: JsonPropertyName("generationManifestRef")] string GenerationManifestRef,
    [property: JsonPropertyName("pbirIrRef")] string PbirIrRef,
    [property: JsonPropertyName("pbirIrSchemaVersion")] string PbirIrSchemaVersion,
    [property: JsonPropertyName("pbirIrContentHash")] string PbirIrContentHash,
    [property: JsonPropertyName("previewManifestRef")] string PreviewManifestRef,
    [property: JsonPropertyName("previewManifestSchemaVersion")] string PreviewManifestSchemaVersion,
    [property: JsonPropertyName("previewManifestHash")] string PreviewManifestHash,
    [property: JsonPropertyName("immutableLineage")] IReadOnlyList<string> ImmutableLineage);

internal sealed record PbirPreviewPackageHashes(
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("inventoryHash")] string InventoryHash,
    [property: JsonPropertyName("packageHash")] string PackageHash);

internal sealed record PbirPreviewPackage(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("packageDescriptor")] PbirPreviewPackageDescriptor PackageDescriptor,
    [property: JsonPropertyName("metadata")] PbirPreviewPackageMetadata Metadata,
    [property: JsonPropertyName("fileInventory")] IReadOnlyList<PbirPreviewPackageFileInventoryItem> FileInventory,
    [property: JsonPropertyName("hashInventory")] PbirPreviewPackageHashInventory HashInventory,
    [property: JsonPropertyName("lineage")] PbirPreviewPackageLineage Lineage,
    [property: JsonPropertyName("rollbackPlanReference")] PbirLocalPreviewRollbackPlanReference RollbackPlanReference,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("rejectedArtifacts")] IReadOnlyList<PbirLocalPreviewRejectedFile> RejectedArtifacts,
    [property: JsonPropertyName("hashes")] PbirPreviewPackageHashes Hashes);

internal sealed record PbirPreviewPackageDiagnostics(
    IReadOnlyList<string> SafetyRejections,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static PbirPreviewPackageDiagnostics Empty { get; } = new([], []);
}

internal sealed record PbirPreviewPackageState(
    PbirPreviewPackage? Package,
    PbirPreviewPackageSafetyGateResult Safety,
    PbirPreviewPackageDiagnostics Diagnostics,
    PbirPreviewPackageReadinessState Readiness);

internal sealed record PbirReviewHandoffRequest(
    [property: JsonPropertyName("handoffId")] string HandoffId,
    [property: JsonPropertyName("reviewTarget")] PbirReviewTarget ReviewTarget,
    [property: JsonPropertyName("requiredReviewerAction")] string RequiredReviewerAction,
    [property: JsonPropertyName("automaticAnalyzerValidationRequested")] bool AutomaticAnalyzerValidationRequested,
    [property: JsonPropertyName("workspaceLaunchRequested")] bool WorkspaceLaunchRequested,
    [property: JsonPropertyName("deploymentRequested")] bool DeploymentRequested)
{
    internal static PbirReviewHandoffRequest ForReview(
        string handoffId,
        PbirReviewTarget reviewTarget,
        string requiredReviewerAction)
    {
        return new PbirReviewHandoffRequest(
            HandoffId: handoffId,
            ReviewTarget: reviewTarget,
            RequiredReviewerAction: requiredReviewerAction,
            AutomaticAnalyzerValidationRequested: false,
            WorkspaceLaunchRequested: false,
            DeploymentRequested: false);
    }
}

internal sealed record PbirReviewHandoffSafetyGateResult(
    [property: JsonPropertyName("isAllowed")] bool IsAllowed,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record PbirReviewHandoffPreviewPackageReference(
    [property: JsonPropertyName("packageId")] string PackageId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("packageHash")] string PackageHash);

internal sealed record PbirReviewHandoffDesignPackageReference(
    [property: JsonPropertyName("designPackageRef")] string DesignPackageRef);

internal sealed record PbirReviewHandoffGenerationManifestReference(
    [property: JsonPropertyName("manifestId")] string ManifestId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion);

internal sealed record PbirReviewHandoffPbirIrReference(
    [property: JsonPropertyName("irId")] string IrId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("contentHash")] string ContentHash);

internal sealed record PbirReviewTargetDescriptor(
    [property: JsonPropertyName("target")] PbirReviewTarget Target,
    [property: JsonPropertyName("reviewOnly")] bool ReviewOnly);

internal sealed record PbirAnalyzerWorkspaceBoundary(
    [property: JsonPropertyName("validationOccurred")] bool ValidationOccurred,
    [property: JsonPropertyName("automaticValidationRequested")] bool AutomaticValidationRequested,
    [property: JsonPropertyName("automaticValidationAllowed")] bool AutomaticValidationAllowed,
    [property: JsonPropertyName("workspaceLaunchRequested")] bool WorkspaceLaunchRequested,
    [property: JsonPropertyName("validationStatus")] string ValidationStatus);

internal sealed record PbirDeploymentBoundary(
    [property: JsonPropertyName("deploymentRequested")] bool DeploymentRequested,
    [property: JsonPropertyName("deploymentAllowed")] bool DeploymentAllowed);

internal sealed record PbirReviewHandoffLineage(
    [property: JsonPropertyName("previewPackageRef")] string PreviewPackageRef,
    [property: JsonPropertyName("designPackageRef")] string DesignPackageRef,
    [property: JsonPropertyName("generationManifestRef")] string GenerationManifestRef,
    [property: JsonPropertyName("pbirIrRef")] string PbirIrRef,
    [property: JsonPropertyName("sourceWriteManifestRef")] string SourceWriteManifestRef,
    [property: JsonPropertyName("immutableLineage")] IReadOnlyList<string> ImmutableLineage);

internal sealed record PbirReviewHandoffHashes(
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("handoffHash")] string HandoffHash);

internal sealed record PbirReviewHandoff(
    [property: JsonPropertyName("handoffId")] string HandoffId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("generatedUtc")] DateTime GeneratedUtc,
    [property: JsonPropertyName("previewPackageReference")] PbirReviewHandoffPreviewPackageReference PreviewPackageReference,
    [property: JsonPropertyName("designPackageReference")] PbirReviewHandoffDesignPackageReference DesignPackageReference,
    [property: JsonPropertyName("generationManifestReference")] PbirReviewHandoffGenerationManifestReference GenerationManifestReference,
    [property: JsonPropertyName("pbirIrReference")] PbirReviewHandoffPbirIrReference PbirIrReference,
    [property: JsonPropertyName("reviewTarget")] PbirReviewTargetDescriptor ReviewTarget,
    [property: JsonPropertyName("reviewReadiness")] PbirReviewHandoffReadinessState ReviewReadiness,
    [property: JsonPropertyName("requiredReviewerAction")] string RequiredReviewerAction,
    [property: JsonPropertyName("designStudioApprovalContext")] PlanningApprovalStatus DesignStudioApprovalContext,
    [property: JsonPropertyName("analyzerWorkspaceBoundary")] PbirAnalyzerWorkspaceBoundary AnalyzerWorkspaceBoundary,
    [property: JsonPropertyName("deploymentBoundary")] PbirDeploymentBoundary DeploymentBoundary,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("lineage")] PbirReviewHandoffLineage Lineage,
    [property: JsonPropertyName("hashes")] PbirReviewHandoffHashes Hashes);

internal sealed record PbirReviewHandoffDiagnostics(
    IReadOnlyList<string> SafetyRejections,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static PbirReviewHandoffDiagnostics Empty { get; } = new([], []);
}

internal sealed record PbirReviewHandoffState(
    PbirReviewHandoff? Handoff,
    PbirReviewHandoffSafetyGateResult Safety,
    PbirReviewHandoffDiagnostics Diagnostics,
    PbirReviewHandoffReadinessState Readiness);
