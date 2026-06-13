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

internal sealed record MaterializationAnalyzerHandoffMetadata(
    string Target,
    string RequestId,
    string CandidateId,
    string TargetSurfaceType,
    string TargetAnalyzer,
    string TargetAnalyzerProfile,
    string ExecutionState);

internal sealed record MaterializationSideEffectState(
    bool AnalyzerHandoffExecuted,
    bool PbirFilesCreated,
    bool ReportMutationOccurred,
    bool DeliveryTriggered,
    bool ProviderExecutionTriggered);

internal sealed record MaterializationGatewayOutcome(
    bool Succeeded,
    IReadOnlyList<string> Diagnostics,
    MaterializationAnalyzerHandoffMetadata AnalyzerHandoff,
    MaterializationSideEffectState SideEffects);
