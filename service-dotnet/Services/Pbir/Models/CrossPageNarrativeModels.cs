namespace PowerBIModelingService.Services.Pbir.Models;

internal enum CrossPageNarrativeRoleId
{
    Overview,
    ExecutiveSummary,
    OperationalMonitor,
    ComparativeAnalysis,
    DiagnosticInvestigation,
    DetailDrill,
    ScenarioExploration,
    ExceptionAnalysis,
    SupportingContext,
    ReferenceLegal,
    Tooltip,
    Qna,
    ValidationSandbox,
}

internal enum CrossPageNarrativeRoleConfidence
{
    Low,
    Medium,
    High,
}

internal sealed class CrossPageNarrativeRoleAssignment
{
    public CrossPageNarrativeRoleId PrimaryRole { get; init; }

    public CrossPageNarrativeRoleConfidence Confidence { get; init; }

    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> SecondaryHints { get; init; } = Array.Empty<string>();
}

internal enum CrossPageNarrativeEdgeType
{
    OrderedNext,
    OrderedPrevious,
    Drillthrough,
    ReverseDrillSupport,
    TopicContinuity,
    RoleCompatibleTransition,
    SegmentMembership,
    SummaryToDetail,
    SupportingContext,
}

internal enum CrossPageNarrativeEdgeObservationKind
{
    Observed,
    Inferred,
}

internal sealed class CrossPageNarrativeEdge
{
    public required string SourcePageId { get; init; }

    public required string TargetPageId { get; init; }

    public CrossPageNarrativeEdgeType EdgeType { get; init; }

    public CrossPageNarrativeEdgeObservationKind ObservationKind { get; init; }

    public double Strength { get; init; }

    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
}

internal sealed class CrossPageNarrativeGraph
{
    public IReadOnlyList<string> PageIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<CrossPageNarrativeEdge> Edges { get; init; } = Array.Empty<CrossPageNarrativeEdge>();

    public IReadOnlyList<IReadOnlyList<string>> Segments { get; init; } = Array.Empty<IReadOnlyList<string>>();

    public IReadOnlyList<string> MainNarrativePath { get; init; } = Array.Empty<string>();
}

internal enum CrossPageNarrativeAssessmentConfidence
{
    Low,
    Medium,
    High,
}

internal enum CrossPageNarrativeDimensionId
{
    Flow,
    Consistency,
    Navigation,
    Continuity,
    Actionability,
}

internal sealed class CrossPageNarrativeDimensionScore
{
    public CrossPageNarrativeDimensionId DimensionId { get; init; }

    public double Score { get; init; }

    public CrossPageNarrativeAssessmentConfidence Confidence { get; init; }

    public IReadOnlyList<string> StrongestEvidence { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> WeakeningEvidence { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> MissingEvidence { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AffectedPageIds { get; init; } = Array.Empty<string>();
}

internal sealed class CrossPageNarrativeScoreSummary
{
    public double CompositeScore { get; init; }

    public CrossPageNarrativeAssessmentConfidence Confidence { get; init; }

    public StoryAssessmentPromotionState PromotionState { get; init; }

    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public IReadOnlyList<CrossPageNarrativeDimensionScore> Dimensions { get; init; } =
        Array.Empty<CrossPageNarrativeDimensionScore>();

    public string DominantNarrativeSummary { get; init; } = string.Empty;
}

internal enum CrossPageNarrativeGapId
{
    MissingExecutiveEntryPoint,
    MissingNarrativeBridge,
    MissingDrillPath,
    BrokenDrillAlignment,
    InconsistentKpiHierarchy,
    InconsistentNamingLayer,
    DisconnectedAnalysisPage,
    OrphanDetailPage,
    UnsignaledContextShift,
    FragmentedReportSegmentation,
}

internal sealed class CrossPageNarrativeGap
{
    public CrossPageNarrativeGapId GapId { get; init; }

    public required string StableId { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required string WhyItMatters { get; init; }

    public required string ExpectedImpact { get; init; }

    public IReadOnlyList<string> AffectedPageIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<StoryGapEvidenceReference> EvidenceReferences { get; init; } =
        Array.Empty<StoryGapEvidenceReference>();

    public StoryGapConfidence Confidence { get; init; }

    public StoryGapActionabilityAssessment ActionabilityAssessment { get; init; }

    public StoryGapRemediationLayer RemediationLayer { get; init; }
}

internal enum CrossPageNarrativeOrphanState
{
    Connected,
    AdvisoryDisconnectedSpecialPage,
    OrphanedPage,
    UnreachablePage,
    UnusedDrillTarget,
    IsolatedAnalysisIsland,
}

internal sealed class CrossPageNarrativePageAssessment
{
    public required string PageId { get; init; }

    public required string PageName { get; init; }

    public required CrossPageNarrativeRoleAssignment RoleAssignment { get; init; }

    public CrossPageNarrativeOrphanState OrphanState { get; init; }

    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RelatedPageIds { get; init; } = Array.Empty<string>();
}

internal sealed class CrossPageNarrativeAssessment
{
    public string DominantReportObjective { get; init; } = string.Empty;

    public StoryAssessmentPromotionState PromotionState { get; init; }

    public StoryAssessmentSurfaceScope SurfaceScope { get; init; }

    public required CrossPageNarrativeGraph Graph { get; init; }

    public IReadOnlyList<CrossPageNarrativePageAssessment> Pages { get; init; } =
        Array.Empty<CrossPageNarrativePageAssessment>();

    public required CrossPageNarrativeScoreSummary ScoreSummary { get; init; }

    public IReadOnlyList<CrossPageNarrativeGap> Gaps { get; init; } =
        Array.Empty<CrossPageNarrativeGap>();
}
