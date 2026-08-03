using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirDeployableMaterializationPreviewRequestContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-preview-request/v1"; }
internal static class PbirDeployableMaterializationPreviewContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-preview/v1"; }
internal static class PbirDeployableMaterializationControlRootContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-control-root/v1"; }
internal static class PbirDeployableMaterializationApplyRequestContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-apply-request/v1"; }
internal static class PbirDeployableMaterializationTransactionContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-transaction/v1"; }
internal static class PbirDeployableMaterializationApplyResultContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-apply-result/v1"; }
internal static class PbirDeployableMaterializationReceiptContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-receipt/v1"; }
internal static class PbirDeployableMaterializationRollbackRequestContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-rollback-request/v1"; }
internal static class PbirDeployableMaterializationRollbackResultContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-rollback-result/v1"; }
internal static class PbirDeployableMaterializationDiagnosticsContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-diagnostics/v1"; }
internal static class PbirDeployableMaterializationReadinessContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-readiness/v1"; }
internal static class PbirDeployableMaterializationLineageContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-lineage/v1"; }
internal static class PbirDeployableMaterializationHashesContract { internal const string SchemaVersionV1 = "pbir-deployable-materialization-hashes/v1"; }
internal static class PbirDeployableTargetInventoryContract { internal const string SchemaVersionV1 = "pbir-deployable-target-inventory/v1"; }

internal enum PbirDeployableTargetState { Absent, EmptyDirectory, Files }
internal enum PbirDeployableMaterializationDisposition { Create, ReplaceManaged, NoChanges, BlockedConflict, RecoveryRequired }
internal enum PbirDeployableMaterializationReadinessState { Incomplete, Blocked, ReadyToCreate, ReadyToReplaceManaged, NoChanges, Applying, Applied, RecoveryRequired, RollingBack, RolledBack }
internal enum PbirDeployableMaterializationJournalPhase { Initialized, StagingWritten, StagingVerified, Aborted, BackupMoved, TargetPromoted, TargetVerified, ReceiptCommitted, Completed, Restoring, Restored, RecoveryRequired }
internal enum PbirDeployableMaterializationRecoveryDisposition { RolledBackCommittedApply, RecoveredInterruptedApply }

internal sealed record PbirDeployableTargetInventoryFile(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("byteLength")] long ByteLength,
    [property: JsonPropertyName("hashSha256")] string HashSha256);

internal sealed record PbirDeployableTargetInventory(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("targetState")] PbirDeployableTargetState TargetState,
    [property: JsonPropertyName("files")] IReadOnlyList<PbirDeployableTargetInventoryFile> Files);

internal sealed record PbirDeployableMaterializationExecutionPolicy(
    [property: JsonPropertyName("filesystemMutationAllowed")] bool FilesystemMutationAllowed,
    [property: JsonPropertyName("providerInvocationAllowed")] bool ProviderInvocationAllowed,
    [property: JsonPropertyName("microsoftSkillsExecutionAllowed")] bool MicrosoftSkillsExecutionAllowed,
    [property: JsonPropertyName("apiInvocationAllowed")] bool ApiInvocationAllowed,
    [property: JsonPropertyName("cliInvocationAllowed")] bool CliInvocationAllowed,
    [property: JsonPropertyName("deploymentAllowed")] bool DeploymentAllowed,
    [property: JsonPropertyName("publishingAllowed")] bool PublishingAllowed,
    [property: JsonPropertyName("desktopAutomationAllowed")] bool DesktopAutomationAllowed,
    [property: JsonPropertyName("analyzerAutomationAllowed")] bool AnalyzerAutomationAllowed)
{
    internal static PbirDeployableMaterializationExecutionPolicy PreviewOnly { get; } = new(false, false, false, false, false, false, false, false, false);
    internal static PbirDeployableMaterializationExecutionPolicy LocalMutationOnly { get; } = new(true, false, false, false, false, false, false, false, false);
    internal bool HasExternalAuthority => ProviderInvocationAllowed || MicrosoftSkillsExecutionAllowed || ApiInvocationAllowed || CliInvocationAllowed || DeploymentAllowed || PublishingAllowed || DesktopAutomationAllowed || AnalyzerAutomationAllowed;
}

internal sealed record PbirDeployableMaterializationDiagnostic(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("message")] string Message);

internal sealed record PbirDeployableMaterializationDiagnostics(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("items")] IReadOnlyList<PbirDeployableMaterializationDiagnostic> Items)
{
    internal static PbirDeployableMaterializationDiagnostics Empty { get; } = new(PbirDeployableMaterializationDiagnosticsContract.SchemaVersionV1, []);
    internal bool HasFailures => Items.Count > 0;
}

internal sealed record PbirDeployableMaterializationLineage(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("phase29Lineage")] PbirDeployableLineage Phase29Lineage,
    [property: JsonPropertyName("immutableLineage")] IReadOnlyList<string> ImmutableLineage,
    [property: JsonPropertyName("lineageHash")] string LineageHash);

internal sealed record PbirDeployableMaterializationHashes(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("fileSetHash")] string FileSetHash,
    [property: JsonPropertyName("targetStateHash")] string TargetStateHash,
    [property: JsonPropertyName("lineageHash")] string LineageHash,
    [property: JsonPropertyName("selfHash")] string SelfHash);

internal sealed record PbirDeployableMaterializationPreviewRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("artifactRef")] string ArtifactRef,
    [property: JsonPropertyName("artifactHash")] string ArtifactHash,
    [property: JsonPropertyName("manifestRef")] string ManifestRef,
    [property: JsonPropertyName("manifestHash")] string ManifestHash,
    [property: JsonPropertyName("targetDirectoryName")] string TargetDirectoryName,
    [property: JsonPropertyName("requestedOperation")] string RequestedOperation,
    [property: JsonPropertyName("executionPolicy")] PbirDeployableMaterializationExecutionPolicy ExecutionPolicy);

internal sealed record PbirDeployableMaterializationPreview(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("previewId")] string PreviewId,
    [property: JsonPropertyName("requestRef")] string RequestRef,
    [property: JsonPropertyName("artifactRef")] string ArtifactRef,
    [property: JsonPropertyName("artifactHash")] string ArtifactHash,
    [property: JsonPropertyName("manifestRef")] string ManifestRef,
    [property: JsonPropertyName("manifestHash")] string ManifestHash,
    [property: JsonPropertyName("canonicalOutputBasePath")] string CanonicalOutputBasePath,
    [property: JsonPropertyName("canonicalTargetPath")] string CanonicalTargetPath,
    [property: JsonPropertyName("targetKey")] string TargetKey,
    [property: JsonPropertyName("targetInventory")] PbirDeployableTargetInventory TargetInventory,
    [property: JsonPropertyName("disposition")] PbirDeployableMaterializationDisposition Disposition,
    [property: JsonPropertyName("plannedFiles")] IReadOnlyList<PbirDeployableGeneratedFileReference> PlannedFiles,
    [property: JsonPropertyName("rollbackAvailable")] bool RollbackAvailable,
    [property: JsonPropertyName("activeTransactionRef")] string? ActiveTransactionRef,
    [property: JsonPropertyName("lineage")] PbirDeployableMaterializationLineage Lineage,
    [property: JsonPropertyName("hashes")] PbirDeployableMaterializationHashes Hashes);

internal sealed record PbirDeployableMaterializationPreviewState(
    PbirDeployableMaterializationPreview? Preview,
    PbirDeployableMaterializationReadinessState Readiness,
    PbirDeployableMaterializationDiagnostics Diagnostics);

internal sealed record PbirDeployableMaterializationApplyRequest(
    string SchemaVersion, string RequestId, string TransactionId, string PreviewRef, string PreviewHash,
    string ArtifactRef, string ArtifactHash, string ManifestRef, string ManifestHash,
    string ExpectedTargetStateHash, bool ApplyApproved, bool RollbackRequired,
    PbirDeployableMaterializationExecutionPolicy ExecutionPolicy);

internal sealed record PbirDeployableMaterializationReceipt(
    string SchemaVersion, string ReceiptId, string TransactionId, string ApplyRequestRef, string ApplyRequestHash,
    string PreviewRef, string PreviewHash, string ArtifactRef, string ArtifactHash, string ManifestRef, string ManifestHash,
    string TargetKey, string CanonicalTargetPath, string CommittedTargetStateHash, string? PreviousReceiptHash,
    string RollbackTransactionRef, PbirDeployableMaterializationLineage Lineage, string ReceiptHash);

internal sealed record PbirDeployableMaterializationJournalEvent(string Phase, string StateHash);

internal sealed record PbirDeployableMaterializationTransaction(
    string SchemaVersion, string TransactionId, string Operation, string TargetKey, string CanonicalTargetPath,
    string PreviewRef, string PreviewHash, string ArtifactRef, string ArtifactHash, string ManifestRef, string ManifestHash,
    PbirDeployableTargetState ExpectedPreState, string ExpectedPreStateHash, string? PreviousReceiptHash,
    PbirDeployableMaterializationJournalPhase Phase, IReadOnlyList<PbirDeployableMaterializationJournalEvent> Events,
    string? StagingInventoryHash, string? BackupInventoryHash, string? CommittedTargetStateHash,
    PbirDeployableMaterializationLineage Lineage, string TransactionHash);

internal sealed record PbirDeployableMaterializationApplyResult(
    string SchemaVersion, string ResultId, string RequestRef, string TransactionId, string TransactionHash,
    string PreviewRef, string PreviewHash, string CanonicalTargetPath, IReadOnlyList<PbirDeployableTargetInventoryFile> WrittenFiles,
    PbirDeployableTargetState PreviousTargetState, string PreviousTargetStateHash, string CommittedTargetStateHash,
    bool RollbackAvailable, string CurrentReceiptHash, PbirDeployableMaterializationLineage Lineage,
    IReadOnlyList<string> Warnings, PbirDeployableMaterializationDiagnostics Diagnostics, PbirDeployableMaterializationHashes Hashes);

internal sealed record PbirDeployableMaterializationApplyState(
    PbirDeployableMaterializationApplyResult? Result,
    PbirDeployableMaterializationReadinessState Readiness,
    PbirDeployableMaterializationDiagnostics Diagnostics);

internal sealed record PbirDeployableMaterializationRollbackRequest(
    string SchemaVersion, string RequestId, string TransactionId, string TargetDirectoryName, string TargetKey,
    string ExpectedTransactionHash, string? ExpectedCurrentReceiptHash, string ExpectedCurrentTargetStateHash,
    bool RollbackApproved, PbirDeployableMaterializationExecutionPolicy ExecutionPolicy);

internal sealed record PbirDeployableMaterializationRollbackResult(
    string SchemaVersion, string ResultId, string RequestRef, string TransactionId, string TransactionHash,
    PbirDeployableTargetState RestoredTargetState, string RestoredTargetStateHash, string? QuarantinedAppliedStateHash,
    string? RestoredReceiptHash, PbirDeployableMaterializationRecoveryDisposition RecoveryDisposition,
    PbirDeployableMaterializationLineage Lineage, PbirDeployableMaterializationDiagnostics Diagnostics,
    PbirDeployableMaterializationHashes Hashes);

internal sealed record PbirDeployableMaterializationRollbackState(
    PbirDeployableMaterializationRollbackResult? Result,
    PbirDeployableMaterializationReadinessState Readiness,
    PbirDeployableMaterializationDiagnostics Diagnostics);

internal sealed record PbirDeployableMaterializationControlRoot(
    string SchemaVersion, string Owner, string Purpose, string CanonicalOutputBaseHash, string ControlRootHash);
