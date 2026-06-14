namespace PowerBIModelingService.Services.DesignStudio.Materialization;

internal enum MaterializationMode
{
    ConceptToStructurePreview,
    DraftToSurfaceCandidate,
    RefinementProposalToCandidateComparison,
}

internal sealed record MaterializationProvenanceEntry(
    string ArtifactId,
    string ArtifactKind,
    string ArtifactVersionId,
    string SourceRole,
    string ApprovalState,
    DateTimeOffset ApprovalTimestamp,
    DateTimeOffset CapturedAt);

internal sealed record MaterializationSnapshotReference(
    string SnapshotId,
    string RootPath,
    string SourceLocation);

internal sealed record MaterializationHandoffContext(
    string? RepositoryBackedPath,
    MaterializationSnapshotReference? SnapshotReference,
    IReadOnlyList<string> DegradedMappings,
    IReadOnlyList<string> OmittedEvidence);

internal enum MaterializationHandoffEligibility
{
    Executable,
    NonExecutablePreview,
    Unsupported,
}

internal sealed record MaterializationAnalyzerHandoffReference(
    string ReferenceKind,
    string? RepositoryPath,
    string? SnapshotId,
    string? RootPath,
    string? SourceLocation,
    string? Reason);

internal sealed record MaterializationAnalyzerHandoffMetadata(
    string Target,
    string RequestId,
    string CandidateId,
    string TargetSurfaceType,
    string TargetAnalyzer,
    string TargetAnalyzerProfile,
    MaterializationHandoffEligibility ExecutableEligibility,
    string ExecutionState,
    string WorkspaceOpenState);

internal sealed record MaterializationAnalyzerHandoffContract(
    MaterializationAnalyzerHandoffMetadata Metadata,
    MaterializationAnalyzerHandoffReference Reference,
    IReadOnlyList<string> Diagnostics);

internal sealed record MaterializationSideEffectState(
    bool AnalyzerHandoffExecuted,
    bool AnalyzerWorkspaceOpened,
    bool PbirFilesCreated,
    bool ReportMutationOccurred,
    bool DeliveryTriggered,
    bool ProviderExecutionTriggered);

internal sealed record MaterializationGatewayOutcome(
    bool Succeeded,
    IReadOnlyList<string> Diagnostics,
    MaterializationAnalyzerHandoffContract AnalyzerHandoff,
    MaterializationSideEffectState SideEffects);
