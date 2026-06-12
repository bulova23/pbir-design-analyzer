namespace PowerBIModelingService.Services.Pbir.Models;

/// <summary>
/// Internal-only signal categories used by Story Assessment validation work.
/// These types intentionally remain out of the public score payload until signals are promoted.
/// </summary>
internal enum StorySignalCategory
{
    Layout,
    Semantic,
    Context,
    Interaction,
}

/// <summary>
/// Describes how a signal contributes to Story Assessment reasoning.
/// </summary>
internal enum StorySignalContributionIntent
{
    ClarifiesStoryIntent,
    ShapesNarrativeConfidence,
    SupportsDecisionContext,
    SupportsExplorationContext,
}

/// <summary>
/// Describes where a signal is most likely remediable when it fires negatively.
/// </summary>
internal enum StorySignalRemediability
{
    ReportLayer,
    SemanticModel,
    Mixed,
    NotDirectlyRemediable,
}

/// <summary>
/// Describes whether a signal is required or only supportive for future validation.
/// </summary>
internal enum StorySignalRequirementRole
{
    Required,
    Supportive,
    Optional,
}

/// <summary>
/// Distinguishes direct evidence from reinforcement-only signals.
/// </summary>
internal enum StorySignalEvidenceRole
{
    DirectEvidence,
    ReinforcementOnly,
}

/// <summary>
/// Describes how trustworthy a signal currently appears during validation.
/// </summary>
internal enum StorySignalReliabilityState
{
    Experimental,
    Candidate,
    Validated,
    Disputed,
    Rejected,
}

/// <summary>
/// Classifies whether a signal is PBIR-specific or a candidate for broader reuse.
/// </summary>
internal enum StoryAssessmentSurfaceScope
{
    PbirSpecific,
    CrossSurfaceCandidate,
    FutureSurfaceSpecific,
}

/// <summary>
/// Describes how clearly a signal can be explained to reviewers and users.
/// </summary>
internal enum StoryAssessmentExplanationType
{
    DirectEvidence,
    DerivedButExplainable,
    OpaqueDiagnosticOnly,
}

/// <summary>
/// Describes whether a signal leads directly to remediation or only supports diagnosis.
/// </summary>
internal enum StoryAssessmentActionabilityType
{
    DirectRemediation,
    IndirectGuidance,
    DiagnosticOnly,
}

/// <summary>
/// The four required validation dimensions for Story Assessment 2.0 signal promotion.
/// </summary>
internal enum StoryAssessmentValidationDimension
{
    Accuracy,
    Consistency,
    Explainability,
    Actionability,
}

/// <summary>
/// The current level of validation maturity applied to a signal evaluation.
/// </summary>
internal enum StoryAssessmentValidationLevel
{
    Level1ExpertReview,
    Level2FormalCorpus,
}

/// <summary>
/// A bounded deterministic rating used during validation.
/// </summary>
internal enum StoryAssessmentValidationRating
{
    NotAssessed,
    Weak,
    Mixed,
    Strong,
}

/// <summary>
/// Defines the promotion lifecycle for a Story Assessment signal.
/// </summary>
internal enum StoryAssessmentPromotionState
{
    Internal,
    Level1Validated,
    ContractEligible,
    Production,
    CrossSurfaceCandidate,
    Level2Validated,
    PlatformCritical,
}

/// <summary>
/// Records how one validation dimension was judged at a specific review level.
/// </summary>
internal sealed record StoryAssessmentDimensionEvaluation(
    StoryAssessmentValidationDimension Dimension,
    StoryAssessmentValidationLevel Level,
    StoryAssessmentValidationRating Rating,
    string Notes);

/// <summary>
/// Internal-only collection of captured story signals for a scored page.
/// </summary>
internal sealed class StorySignalRegistry
{
    public IReadOnlyList<StorySignalRegistryEntry> Entries { get; init; } =
        Array.Empty<StorySignalRegistryEntry>();
}

/// <summary>
/// Internal-only registry entry shape for future Story Assessment signal extraction work.
/// </summary>
internal sealed class StorySignalRegistryEntry
{
    public required string Id { get; init; }

    public StorySignalCategory Category { get; init; }

    public string? RawValue { get; init; }

    public bool Fired { get; init; }

    public StorySignalContributionIntent ContributionIntent { get; init; }

    public StorySignalRemediability Remediability { get; init; }

    public required string ExplanationHook { get; init; }

    public StorySignalReliabilityState ReliabilityState { get; init; }

    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public StorySignalRequirementRole RequirementRole { get; init; }

    public StorySignalEvidenceRole EvidenceRole { get; init; }

    public StoryAssessmentExplanationType ExplanationType { get; init; }

    public StoryAssessmentActionabilityType ActionabilityType { get; init; }

    public StoryAssessmentPromotionState PromotionState { get; init; }

    public IReadOnlyList<StoryAssessmentDimensionEvaluation> Evaluations { get; init; } =
        Array.Empty<StoryAssessmentDimensionEvaluation>();
}

/// <summary>
/// Supported internal-only story archetype categories for Level 1 validation.
/// </summary>
internal enum StoryArchetypeId
{
    PerformanceMonitor,
    TrendException,
    Ranking,
    Comparison,
    Decomposition,
    NarrativeWalkthrough,
}

/// <summary>
/// Bounded confidence label for an archetype match.
/// </summary>
internal enum StoryArchetypeMatchConfidence
{
    Low,
    Medium,
    High,
}

/// <summary>
/// Describes the current internal review posture of an archetype result.
/// </summary>
internal enum StoryArchetypeValidationStatus
{
    NeedsLevel1Review,
    AmbiguousNeedsReview,
    ReadyForPromotionReview,
}

/// <summary>
/// Describes whether an internal archetype result is mature enough for promotion consideration.
/// </summary>
internal enum StoryAssessmentPromotionEligibilityState
{
    NotEligible,
    Level1ReviewCandidate,
    ReadyForPromotionReview,
}

/// <summary>
/// Internal-only scored match result for a single archetype candidate.
/// </summary>
internal sealed class StoryArchetypeMatchResult
{
    public StoryArchetypeId ArchetypeId { get; init; }

    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public double MatchScore { get; init; }

    public StoryArchetypeMatchConfidence MatchConfidence { get; init; }

    public IReadOnlyList<string> MatchedSignals { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> MissedSignals { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ExplanationHooks { get; init; } = Array.Empty<string>();

    public StoryArchetypeValidationStatus ValidationStatus { get; init; }

    public StoryAssessmentPromotionEligibilityState PromotionEligibilityState { get; init; }

    /// <summary>
    /// Canonical lifecycle gate for downstream internal consumers.
    /// Specialized archetype review posture remains secondary.
    /// </summary>
    public StoryAssessmentPromotionState PromotionState { get; init; }
}

/// <summary>
/// Placeholder Level 1 review harness for expert validation against system archetype choices.
/// </summary>
internal sealed class StoryAssessmentLevel1ValidationHarness
{
    public string? ReviewerChoice { get; init; }

    public required string SystemChoice { get; init; }

    public string? DisagreementReason { get; init; }

    public StoryAssessmentValidationRating AccuracyRating { get; init; }

    public StoryAssessmentValidationRating ConsistencyRating { get; init; }

    public StoryAssessmentValidationRating ExplainabilityRating { get; init; }

    public StoryAssessmentValidationRating ActionabilityRating { get; init; }
}

/// <summary>
/// Internal-only promotion gate thresholds for archetype contract eligibility.
/// </summary>
internal sealed class StoryAssessmentPromotionGateDefinition
{
    public double MinimumClassificationAccuracy { get; init; }

    public StoryAssessmentValidationRating MinimumExplanationQuality { get; init; }

    public StoryAssessmentValidationRating MinimumGapUsefulnessPotential { get; init; }

    public double MaximumFalsePositiveRate { get; init; }

    public double ReviewerAgreementThresholdPlaceholder { get; init; }
}

/// <summary>
/// Internal-only archetype classification output built from the Story Signal Registry.
/// </summary>
internal sealed class StoryAssessmentArchetypeClassification
{
    public StoryArchetypeId BestFitArchetypeId { get; init; }

    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public StoryAssessmentPromotionState PromotionState { get; init; }

    public bool SuppressedBySpecialPageType { get; init; }

    public required string ArchetypePromotionDisposition { get; init; }

    public string? SpecialPageReason { get; init; }

    public IReadOnlyList<StoryArchetypeMatchResult> ArchetypeResults { get; init; } =
        Array.Empty<StoryArchetypeMatchResult>();

    public required StoryAssessmentLevel1ValidationHarness Level1ValidationHarness { get; init; }

    public required StoryAssessmentPromotionGateDefinition PromotionGateDefinition { get; init; }
}

/// <summary>
/// Primary internal-only classification for semantic coherence.
/// </summary>
internal enum StorySemanticCoherenceClassification
{
    Focused,
    Split,
    Sparse,
}

/// <summary>
/// Bounded confidence label for semantic coherence judgments.
/// </summary>
internal enum StorySemanticCoherenceConfidence
{
    Low,
    Medium,
    High,
}

/// <summary>
/// Distinguishes no competing-story evidence from weak diagnostics and strong delayed candidates.
/// </summary>
internal enum StoryCompetingStoryStatus
{
    None,
    WeakDiagnosticOnly,
    StrongCandidatePromotionDelayed,
}

/// <summary>
/// Validation posture for internal semantic coherence outputs.
/// </summary>
internal enum StorySemanticCoherenceValidationStatus
{
    Internal,
    Level1Candidate,
    PromotionDelayedRequiresStrongerValidation,
}

/// <summary>
/// Captured normalized term evidence used by the internal coherence scorer.
/// </summary>
internal sealed class StorySemanticTermEvidence
{
    public required string CanonicalTerm { get; init; }

    public required string RawText { get; init; }

    public required string Source { get; init; }

    public double Weight { get; init; }
}

/// <summary>
/// Deterministic token cluster used to explain coherence scoring.
/// </summary>
internal sealed class StorySemanticTermCluster
{
    public required string ClusterId { get; init; }

    public double Weight { get; init; }

    public int SupportCount { get; init; }

    public IReadOnlyList<string> Terms { get; init; } = Array.Empty<string>();

    public required string ExplanationHook { get; init; }
}

/// <summary>
/// Placeholder Level 1 review harness for semantic coherence validation.
/// </summary>
internal sealed class StorySemanticCoherenceLevel1ValidationHarness
{
    public string? ReviewerCoherenceChoice { get; init; }

    public required string SystemCoherenceChoice { get; init; }

    public string? ReviewerDominantConcept { get; init; }

    public required string SystemDominantConcept { get; init; }

    public string? DisagreementReason { get; init; }

    public StoryAssessmentValidationRating AccuracyRating { get; init; }

    public StoryAssessmentValidationRating ConsistencyRating { get; init; }

    public StoryAssessmentValidationRating ExplainabilityRating { get; init; }

    public StoryAssessmentValidationRating ActionabilityRating { get; init; }
}

/// <summary>
/// Internal-only semantic coherence assessment built from deterministic page and semantic metadata.
/// </summary>
internal sealed class StorySemanticCoherenceAssessment
{
    public double CoherenceScore { get; init; }

    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public required string ScoringMode { get; init; }

    public StorySemanticCoherenceClassification CoherenceClassification { get; init; }

    public string? DominantConcept { get; init; }

    public IReadOnlyList<StorySemanticTermEvidence> ExtractedTerms { get; init; } =
        Array.Empty<StorySemanticTermEvidence>();

    public IReadOnlyList<StorySemanticTermCluster> TermClusters { get; init; } =
        Array.Empty<StorySemanticTermCluster>();

    public StoryCompetingStoryStatus CompetingStoryStatus { get; init; }

    public IReadOnlyList<string> WeakDisagreementSignals { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<string> ExplanationHooks { get; init; } =
        Array.Empty<string>();

    public StorySemanticCoherenceConfidence Confidence { get; init; }

    public StorySemanticCoherenceValidationStatus ValidationStatus { get; init; }

    /// <summary>
    /// Canonical lifecycle gate for downstream internal consumers.
    /// Specialized coherence review posture remains secondary.
    /// </summary>
    public StoryAssessmentPromotionState PromotionState { get; init; }

    public IReadOnlyList<string> TuningDetails { get; init; } = Array.Empty<string>();

    public required StorySemanticCoherenceLevel1ValidationHarness Level1ValidationHarness { get; init; }
}

/// <summary>
/// Internal-only special page classifications that should not be forced into generic analytical page archetypes.
/// </summary>
internal enum StorySpecialPageType
{
    Unknown,
    Tooltip,
    Qna,
    WhatIf,
    KeyInfluencers,
    CustomerSegmentationDiagnostic,
    MarketBasket,
    ReferenceLegal,
    ValidationSandbox,
}

/// <summary>
/// Bounded confidence label for internal special-page detection.
/// </summary>
internal enum StorySpecialPageConfidence
{
    Low,
    Medium,
    High,
}

/// <summary>
/// Evidence reference used by the internal special-page classifier.
/// </summary>
internal sealed class StorySpecialPageEvidenceReference
{
    public required string SourceType { get; init; }

    public required string ReferenceId { get; init; }

    public required string Summary { get; init; }
}

/// <summary>
/// Internal-only special page assessment used to guard downstream Story Assessment stages.
/// </summary>
internal sealed class StorySpecialPageAssessment
{
    public StorySpecialPageType PageType { get; init; }

    public StorySpecialPageConfidence Confidence { get; init; }

    public IReadOnlyList<StorySpecialPageEvidenceReference> EvidenceReferences { get; init; } =
        Array.Empty<StorySpecialPageEvidenceReference>();

    public required string Reason { get; init; }

    public StoryAssessmentPromotionState PromotionState { get; init; }

    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public bool TreatAsPrimaryNarrativePage { get; init; }

    public bool SuppressNormalStoryGaps { get; init; }

    public bool SuppressGenericArchetypePromotion { get; init; }
}

/// <summary>
/// Distinguishes slicer visuals from page-level and report-level filter scope.
/// </summary>
internal enum StoryFilterScope
{
    Slicer,
    Page,
    Report,
}

/// <summary>
/// Classifies whether a topology signal is PBIR-specific, reusable, or diagnostic-only noise.
/// </summary>
internal enum StoryFilterTopologySignalClassification
{
    PbirSpecific,
    CrossSurfaceCandidate,
    DiagnosticOnly,
}

/// <summary>
/// Captured internal representation of one filter control or filter scope artifact.
/// </summary>
internal sealed class StoryFilterTopologyFilter
{
    public required string SourceId { get; init; }

    public StoryFilterScope Scope { get; init; }

    public required string DisplayLabel { get; init; }

    public IReadOnlyList<string> FieldHints { get; init; } = Array.Empty<string>();

    public string? HierarchyPattern { get; init; }

    public int HierarchyDepth { get; init; }

    public string? PlacementZone { get; init; }
}

/// <summary>
/// Internal-only validation result for one topology-derived signal.
/// </summary>
internal sealed class StoryFilterTopologySignal
{
    public required string Id { get; init; }

    public StoryFilterTopologySignalClassification Classification { get; init; }

    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public StoryFilterScope Scope { get; init; }

    public bool Fired { get; init; }

    public bool SupportsArchetypeReinforcement { get; init; }

    /// <summary>
    /// Canonical lifecycle gate for downstream internal consumers.
    /// Topology-specific usefulness fields remain secondary.
    /// </summary>
    public StoryAssessmentPromotionState PromotionState { get; init; }

    public StoryAssessmentValidationRating AccuracyContribution { get; init; }

    public StoryAssessmentValidationRating ExplainabilityContribution { get; init; }

    public StoryAssessmentValidationRating ActionabilityContribution { get; init; }
}

/// <summary>
/// Internal-only filter topology extraction, reinforcement, and usefulness assessment.
/// </summary>
internal sealed class StoryFilterTopologyAssessment
{
    public int SlicerCount { get; init; }

    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public StoryAssessmentPromotionState PromotionState { get; init; }

    public int PageFilterCount { get; init; }

    public int ReportFilterCount { get; init; }

    public IReadOnlyList<StoryFilterTopologyFilter> ExtractedFilters { get; init; } =
        Array.Empty<StoryFilterTopologyFilter>();

    public IReadOnlyList<string> HierarchyPatterns { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<string> TopologyCharacteristics { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<StoryFilterTopologySignal> Signals { get; init; } =
        Array.Empty<StoryFilterTopologySignal>();

    public IReadOnlyList<string> ReinforcedArchetypes { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<string> DiagnosticNotes { get; init; } =
        Array.Empty<string>();

    public StoryAssessmentValidationRating AccuracyContribution { get; init; }

    public StoryAssessmentValidationRating ExplainabilityContribution { get; init; }

    public StoryAssessmentValidationRating ActionabilityContribution { get; init; }
}

/// <summary>
/// Internal-only remediation layer classification for story gaps.
/// </summary>
internal enum StoryGapRemediationLayer
{
    Report,
    Model,
    Restructure,
}

/// <summary>
/// Internal-only actionability posture for a generated story gap.
/// </summary>
internal enum StoryGapActionabilityAssessment
{
    Actionable,
    PartlyActionable,
    NotActionable,
}

/// <summary>
/// Internal-only indicator of how directly a gap affects the best-fit archetype.
/// </summary>
internal enum StoryGapArchetypeRelevance
{
    Primary,
    Supporting,
    Low,
}

/// <summary>
/// Bounded confidence label for a generated story gap.
/// </summary>
internal enum StoryGapConfidence
{
    Low,
    Medium,
    High,
}

/// <summary>
/// Evidence reference carried by an internal-only story gap.
/// </summary>
internal sealed class StoryGapEvidenceReference
{
    public required string SourceType { get; init; }

    public required string ReferenceId { get; init; }

    public required string Summary { get; init; }
}

/// <summary>
/// Internal-only evidence-backed story gap candidate.
/// </summary>
internal sealed class StoryGapRecord
{
    public required string GapId { get; init; }

    public required string Description { get; init; }

    public IReadOnlyList<StoryGapEvidenceReference> EvidenceReferences { get; init; } =
        Array.Empty<StoryGapEvidenceReference>();

    public StoryGapRemediationLayer RemediationLayer { get; init; }

    public StoryGapActionabilityAssessment ActionabilityAssessment { get; init; }

    public StoryGapArchetypeRelevance ArchetypeRelevance { get; init; }

    public StoryAssessmentPromotionState PromotionState { get; init; }

    public StoryGapConfidence Confidence { get; init; }

    public bool IsFutureContractCandidate { get; init; }
}

/// <summary>
/// Internal-only story gap assessment built from Story Assessment validation artifacts.
/// </summary>
internal sealed class StoryGapAssessment
{
    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public StoryAssessmentPromotionState PromotionState { get; init; }

    public IReadOnlyList<StoryGapRecord> Gaps { get; init; } = Array.Empty<StoryGapRecord>();
}

/// <summary>
/// Explicit bounded causes that can limit confidence in internal Story Assessment outputs.
/// </summary>
internal enum StoryConfidenceLowCause
{
    SparseEvidence,
    ConflictingEvidence,
    WeakArchetypeMatch,
    LowSemanticCoherence,
    MissingContext,
}

/// <summary>
/// The four inspectable internal confidence-breakdown dimensions.
/// </summary>
internal enum StoryConfidenceBreakdownDimension
{
    Accuracy,
    Consistency,
    Explainability,
    Actionability,
}

/// <summary>
/// Internal-only per-dimension confidence explanation record.
/// </summary>
internal sealed class StoryConfidenceDimensionRecord
{
    public StoryConfidenceBreakdownDimension DimensionId { get; init; }

    public required string DimensionLabel { get; init; }

    public StoryAssessmentValidationRating Rating { get; init; }

    public IReadOnlyList<string> ConfidenceDrivers { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ConfidenceReducers { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> MissingSignals { get; init; } = Array.Empty<string>();

    public IReadOnlyList<StoryGapEvidenceReference> EvidenceReferences { get; init; } =
        Array.Empty<StoryGapEvidenceReference>();

    public required string Explanation { get; init; }

    public StoryGapActionabilityAssessment Actionability { get; init; }

    public StoryAssessmentPromotionState PromotionState { get; init; }

    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }
}

/// <summary>
/// Internal-only aggregate confidence explanation built from Story Assessment artifacts.
/// </summary>
internal sealed class StoryConfidenceBreakdownAssessment
{
    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public StoryAssessmentPromotionState PromotionState { get; init; }

    public IReadOnlyList<StoryConfidenceDimensionRecord> Dimensions { get; init; } =
        Array.Empty<StoryConfidenceDimensionRecord>();

    public IReadOnlyList<string> StrongestDimensions { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> WeakestDimensions { get; init; } = Array.Empty<string>();

    public IReadOnlyList<StoryConfidenceLowCause> LowConfidenceCauses { get; init; } =
        Array.Empty<StoryConfidenceLowCause>();
}
