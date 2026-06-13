namespace PowerBIModelingService.Services.DesignStudio.Models;

internal enum DesignArtifactLifecycleState
{
    Draft,
    Proposed,
    Reviewed,
    Approved,
    Materialized,
    Analyzed,
    Superseded,
    Archived,
}

internal enum DesignArtifactApprovalState
{
    NotSubmitted,
    PendingApproval,
    Approved,
    Rejected,
}

internal enum DesignArtifactApprovalKind
{
    DesignApproval,
    RefinementApproval,
    ValidationApproval,
    MaterializationApproval,
}

internal enum MaterializationMode
{
    ConceptToStructurePreview,
    DraftToSurfaceCandidate,
    RefinementProposalToCandidateComparison,
}

internal enum MaterializationSourceRole
{
    Primary,
    Supporting,
    ComparisonBase,
    ComparisonProposal,
}

internal enum DesignArtifactAuthorSource
{
    User,
    Provider,
    System,
}

internal sealed record DesignArtifactAttribution(
    string ArtifactId,
    string ArtifactKind);

internal sealed record DesignArtifactProvenance(
    string Source,
    string? ProviderId = null,
    string? ProviderDisplayName = null,
    Providers.DesignProviderCapabilityKind? ProviderCapabilityKind = null,
    string? ProviderCapabilityId = null,
    string? RequestId = null,
    string? ProposalId = null,
    string? ModelOrEngineName = null,
    string? ModelOrEngineVersion = null,
    DateTimeOffset? Timestamp = null,
    DesignArtifactAttribution? ArtifactAttribution = null,
    IReadOnlyList<string>? Notes = null);

internal sealed record DesignArtifactValidationLink(
    string? AnalyzerRunId = null,
    string? ResultReference = null,
    string? ComparedIterationId = null);

internal sealed record DesignArtifactMetadata(
    string Id,
    string ThreadId,
    string Kind,
    int Version,
    DesignArtifactLifecycleState LifecycleState,
    DesignArtifactApprovalState ApprovalState,
    DesignArtifactApprovalKind ApprovalKind,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DesignArtifactAuthorSource AuthorSource,
    DesignArtifactProvenance Provenance,
    DesignArtifactValidationLink? ValidationLinkage = null);

internal sealed record DesignBrief(
    DesignArtifactMetadata Metadata,
    string Audience,
    string BusinessObjective,
    IReadOnlyList<string> KeyDecisions,
    IReadOnlyList<string> PrimaryKpis,
    IReadOnlyList<string> Dimensions,
    string IntendedStory,
    IReadOnlyList<string> SuccessCriteria,
    string ReportType,
    string NavigationExpectations,
    string? ConsumptionContext,
    string? DecisionCadence,
    IReadOnlyList<string>? NarrativeRisksOrConstraints,
    IReadOnlyList<string>? RequiredEvidenceDomains,
    string? TargetAnalyzableSurfaceFamily);

internal sealed record ReportConcept(
    DesignArtifactMetadata Metadata,
    string BriefId,
    string SourceBriefId,
    string SourceBriefVersionId,
    string Summary,
    ReportChapterMapConcept ChapterMap,
    IReadOnlyList<PageRecommendationConcept> PageRecommendations,
    IReadOnlyList<PageConcept> PageConcepts,
    KpiHierarchyConcept KpiHierarchy,
    NavigationConcept NavigationStructure,
    AnalyticalFlowConcept AnalyticalFlow,
    IReadOnlyList<AlternateReportConcept> AlternateConcepts,
    string? PreferredBaselineConceptId,
    string? ApprovedBaselineConceptId,
    AlternateConceptComparison? Comparison);

internal sealed record ChapterConcept(
    string Id,
    string Title,
    string Objective,
    IReadOnlyList<string> PageRecommendationIds);

internal sealed record ReportChapterMapConcept(
    IReadOnlyList<ChapterConcept> Chapters);

internal sealed record PageRecommendationConcept(
    string Id,
    string Title,
    string Objective,
    string ChapterId,
    IReadOnlyList<string> RecommendedKpis);

internal sealed record KpiHierarchyNodeConcept(
    string Id,
    string Label,
    string Level,
    IReadOnlyList<string> ChildNodeIds);

internal sealed record PageConcept(
    DesignArtifactMetadata Metadata,
    string ReportConceptId,
    string SourceBriefVersionId,
    string SourceReportConceptVersionId,
    string Title,
    string IntendedPurpose,
    string TargetAudienceOrRole,
    IReadOnlyList<string> PrimaryKpis,
    IReadOnlyList<string> SupportingDimensions,
    string IntendedStoryQuestion,
    string NavigationRole,
    string RelatedChapterId);

internal sealed record NavigationConcept(
    DesignArtifactMetadata Metadata,
    string ReportConceptId,
    string SourceBriefVersionId,
    string SourceReportConceptVersionId,
    string Pattern,
    string Rationale,
    IReadOnlyList<NavigationSectionConcept> Sections);

internal sealed record KpiHierarchyConcept(
    DesignArtifactMetadata Metadata,
    string ReportConceptId,
    string SourceBriefVersionId,
    string SourceReportConceptVersionId,
    IReadOnlyList<KpiHierarchyNodeConcept> Nodes,
    IReadOnlyList<string> SupportingDimensions);

internal sealed record NavigationSectionConcept(
    string Id,
    string Label,
    IReadOnlyList<string> PageRecommendationIds);

internal sealed record AnalyticalFlowStepConcept(
    string Id,
    string Label,
    string Objective,
    string PageRecommendationId);

internal sealed record AnalyticalFlowConcept(
    IReadOnlyList<AnalyticalFlowStepConcept> Steps);

internal sealed record AlternateReportConcept(
    string Id,
    string Label,
    string Summary,
    ReportChapterMapConcept ChapterMap,
    IReadOnlyList<PageRecommendationConcept> PageRecommendations,
    IReadOnlyList<KpiHierarchyNodeConcept> KpiHierarchyNodes,
    IReadOnlyList<string> SupportingDimensions,
    string NavigationPattern,
    string NavigationRationale,
    IReadOnlyList<NavigationSectionConcept> NavigationSections,
    AnalyticalFlowConcept AnalyticalFlow);

internal sealed record AlternateConceptDecision(
    string ConceptId,
    string Label,
    string Disposition);

internal sealed record AlternateConceptComparison(
    string PreferredConceptId,
    string Summary,
    IReadOnlyList<AlternateConceptDecision> Decisions);

internal sealed record DraftReportArtifact(
    DesignArtifactMetadata Metadata,
    string BriefId,
    string? ConceptId,
    string SourceBriefVersionId,
    string? SourceConceptVersionId,
    string? SourceNavigationConceptVersionId,
    IReadOnlyList<string> PageArtifactIds,
    IReadOnlyList<string> LayoutArtifactIds,
    IReadOnlyList<string> NavigationArtifactIds,
    string Summary,
    DraftArtifactStatus DraftStatus);

internal sealed record DraftPageArtifact(
    DesignArtifactMetadata Metadata,
    string DraftReportArtifactId,
    string? PageConceptId,
    string SourceBriefVersionId,
    string? SourceConceptVersionId,
    string? SourcePageConceptVersionId,
    string StructureSummary,
    IReadOnlyList<string> RecommendedVisualRoles,
    DraftArtifactStatus DraftStatus);

internal sealed record DraftLayoutArtifact(
    DesignArtifactMetadata Metadata,
    string DraftPageArtifactId,
    string? PageConceptId,
    string SourceBriefVersionId,
    string? SourceConceptVersionId,
    string? SourcePageConceptVersionId,
    string LayoutType,
    string Title,
    IReadOnlyList<string> KpiBindings,
    IReadOnlyList<string> Zones,
    DraftArtifactStatus DraftStatus);

internal sealed record DraftNavigationSectionArtifact(
    string Id,
    string Label,
    string PageArtifactId,
    string? PageConceptId);

internal sealed record DraftNavigationArtifact(
    DesignArtifactMetadata Metadata,
    string DraftReportArtifactId,
    string? NavigationConceptId,
    string SourceBriefVersionId,
    string? SourceConceptVersionId,
    string? SourceNavigationConceptVersionId,
    string FrameworkType,
    IReadOnlyList<DraftNavigationSectionArtifact> Sections,
    DraftArtifactStatus DraftStatus);

internal sealed record DraftArtifactStatus(
    string Isolation,
    string Reviewability,
    string ProductionState);

internal sealed record CrossPageNarrativeGapSummary(
    string Id,
    string Title,
    string Summary,
    IReadOnlyList<string> AffectedPageNames);

internal sealed record CrossPageNarrativeAnalyzerOutput(
    int Score,
    string Confidence,
    string DominantObjective,
    IReadOnlyList<CrossPageNarrativeGapSummary> Gaps,
    IReadOnlyList<string> NarrativePath,
    string Summary);

internal sealed record SourceArtifactLineageEntry(
    string ArtifactId,
    string ArtifactKind,
    string ArtifactVersionId,
    MaterializationSourceRole SourceRole,
    DesignArtifactApprovalState ApprovalState,
    DateTimeOffset ApprovalTimestamp);

internal sealed record MaterializationProvenanceEntry(
    string ArtifactId,
    string ArtifactKind,
    string ArtifactVersionId,
    MaterializationSourceRole SourceRole,
    DesignArtifactApprovalState ApprovalState,
    DateTimeOffset ApprovalTimestamp,
    DateTimeOffset CapturedAt);

internal sealed record RefinementSourceAnalyzerOutput(
    string AnalyzerSource,
    string AnalyzerRunId,
    string ResultReference,
    string ReportPath,
    DateTimeOffset ScoredAt,
    IReadOnlyList<string> SourceArtifactVersionIds,
    string PayloadJson);

internal sealed record RefinementNoMutationGuarantee(
    bool DirectReportMutation,
    bool MaterializationTriggered,
    bool AnalyzerHandoffTriggered,
    bool PbirAssetGenerationTriggered,
    bool AnalyzableSurfaceCreated,
    bool AutoApplied);

internal sealed record DesignArtifactBacklinkRecord(
    string AnalyzerSource,
    string AnalyzerReferenceId,
    string ArtifactId,
    string ArtifactKind,
    string ArtifactVersionId,
    SourceArtifactStableBacklinkIdentity StableIdentity,
    string? PageName,
    string Reason,
    IReadOnlyList<string> LinkedFindingIds);

internal sealed record SourceArtifactStableBacklinkIdentity(
    string DesignArtifactId,
    string DesignArtifactVersionId,
    string DraftArtifactId,
    string DraftArtifactVersionId);

internal sealed record RefinementProposal(
    DesignArtifactMetadata Metadata,
    string SourceArtifactId,
    IReadOnlyList<SourceArtifactLineageEntry> SourceLineage,
    RefinementSourceAnalyzerOutput SourceAnalyzerOutput,
    IReadOnlyList<string> AffectedArtifactIds,
    IReadOnlyList<string> AffectedArtifactVersionIds,
    string SuggestedDesignChange,
    string Rationale,
    string ExpectedImpact,
    IReadOnlyList<string> LinkedFindingIds,
    RefinementNoMutationGuarantee NoMutationGuarantee);

internal sealed record MaterializationRequest(
    DesignArtifactMetadata Metadata,
    MaterializationMode MaterializationMode,
    IReadOnlyList<string> SourceArtifactIds,
    IReadOnlyList<SourceArtifactLineageEntry> SourceLineage,
    string TargetSurfaceType,
    string TargetAnalyzer,
    string TargetAnalyzerProfile);

internal sealed record MaterializationAnalyzerHandoffMetadata(
    string Target,
    string RequestId,
    string CandidateId,
    string TargetSurfaceType,
    string TargetAnalyzer,
    string TargetAnalyzerProfile,
    string ExecutionState);

internal sealed record MaterializedSurfaceCandidate(
    DesignArtifactMetadata Metadata,
    MaterializationMode MaterializationMode,
    IReadOnlyList<string> SourceArtifactIds,
    IReadOnlyList<SourceArtifactLineageEntry> SourceLineage,
    string TargetSurfaceType,
    string DerivedSurfaceReference,
    IReadOnlyList<string> MaterializationDiagnostics,
    IReadOnlyList<MaterializationProvenanceEntry> ProvenanceTrace,
    MaterializationAnalyzerHandoffMetadata AnalyzerHandoff);

internal sealed record DesignIterationRecord(
    DesignArtifactMetadata Metadata,
    IReadOnlyList<string> SourceArtifactVersionIds,
    string? MaterializedCandidateId,
    IReadOnlyList<string> RefinementProposalIds,
    string ComparisonSummary);
