namespace StoryAssessmentValidationExport;

public sealed class StoryAssessmentValidationExportReport
{
    public required string Title { get; init; }

    public required string ContractNotice { get; init; }

    public required string ReportPath { get; init; }

    public required string GeneratedAtUtc { get; init; }

    public IReadOnlyList<StoryAssessmentValidationExportPage> Pages { get; init; } =
        Array.Empty<StoryAssessmentValidationExportPage>();

    public StoryAssessmentValidationExportCrossPageNarrative? CrossPageNarrative { get; init; }
}

public sealed class StoryAssessmentValidationExportCrossPageNarrative
{
    public required string DominantReportObjective { get; init; }

    public IReadOnlyList<string> MainNarrativePath { get; init; } = Array.Empty<string>();

    public IReadOnlyList<StoryAssessmentValidationExportPageRole> PageRoles { get; init; } =
        Array.Empty<StoryAssessmentValidationExportPageRole>();

    public IReadOnlyList<StoryAssessmentValidationExportOrphanDecision> OrphanDecisions { get; init; } =
        Array.Empty<StoryAssessmentValidationExportOrphanDecision>();

    public IReadOnlyList<StoryAssessmentValidationExportNarrativeDimension> DimensionScores { get; init; } =
        Array.Empty<StoryAssessmentValidationExportNarrativeDimension>();

    public IReadOnlyList<StoryAssessmentValidationExportNarrativeGap> ReportLevelGaps { get; init; } =
        Array.Empty<StoryAssessmentValidationExportNarrativeGap>();
}

public sealed class StoryAssessmentValidationExportPageRole
{
    public required string PageName { get; init; }

    public required string Role { get; init; }

    public required string Confidence { get; init; }
}

public sealed class StoryAssessmentValidationExportOrphanDecision
{
    public required string PageName { get; init; }

    public required string OrphanState { get; init; }
}

public sealed class StoryAssessmentValidationExportNarrativeDimension
{
    public required string DimensionId { get; init; }

    public double Score { get; init; }

    public required string Confidence { get; init; }
}

public sealed class StoryAssessmentValidationExportNarrativeGap
{
    public required string GapId { get; init; }

    public required string StableId { get; init; }

    public required string Summary { get; init; }

    public required string Confidence { get; init; }
}

public sealed class StoryAssessmentValidationExportPage
{
    public required string PageName { get; init; }

    public required string DetectedStory { get; init; }

    public IReadOnlyList<string> SignalRegistrySummary { get; init; } = Array.Empty<string>();

    public required string SpecialPageResult { get; init; }

    public required string ArchetypeClassification { get; init; }

    public required string ArchetypeSuppressionStatus { get; init; }

    public required string SemanticCoherenceResult { get; init; }

    public IReadOnlyList<string> CoherenceTuningDetails { get; init; } = Array.Empty<string>();

    public required string CompetingStoryStatus { get; init; }

    public required string FilterTopologyResult { get; init; }

    public IReadOnlyList<StoryAssessmentValidationExportGap> StoryGaps { get; init; } = Array.Empty<StoryAssessmentValidationExportGap>();

    public IReadOnlyList<StoryAssessmentValidationExportConfidenceDimension> ConfidenceBreakdown { get; init; } =
        Array.Empty<StoryAssessmentValidationExportConfidenceDimension>();

    public IReadOnlyList<string> PromotionStates { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> SurfaceScopes { get; init; } = Array.Empty<string>();
}

public sealed class StoryAssessmentValidationExportGap
{
    public required string GapId { get; init; }

    public required string Description { get; init; }

    public required string RemediationLayer { get; init; }

    public required string Confidence { get; init; }

    public bool IsFutureContractCandidate { get; init; }
}

public sealed class StoryAssessmentValidationExportConfidenceDimension
{
    public required string DimensionId { get; init; }

    public required string DimensionLabel { get; init; }

    public required string Rating { get; init; }

    public IReadOnlyList<string> ConfidenceDrivers { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ConfidenceReducers { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> MissingSignals { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> EvidenceReferences { get; init; } = Array.Empty<string>();

    public required string Explanation { get; init; }

    public required string Actionability { get; init; }

    public required string PromotionState { get; init; }

    public required string SurfaceScope { get; init; }
}
