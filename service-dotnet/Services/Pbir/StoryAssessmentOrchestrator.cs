using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

internal sealed class StoryAssessmentOrchestrator
{
    private readonly StorySignalRegistryService _storySignalRegistryService;
    private readonly SpecialPageAssessmentService _specialPageAssessmentService;

    public StoryAssessmentOrchestrator()
        : this(new StorySignalRegistryService(), new SpecialPageAssessmentService())
    {
    }

    public StoryAssessmentOrchestrator(
        StorySignalRegistryService storySignalRegistryService,
        SpecialPageAssessmentService specialPageAssessmentService)
    {
        _storySignalRegistryService = storySignalRegistryService;
        _specialPageAssessmentService = specialPageAssessmentService;
    }

    public StoryAssessmentArtifacts Assess(
        PageData page,
        IReadOnlyList<FilterDefinitionData> reportFilters,
        IReadOnlyList<string>? reportConsistencyNotes = null)
    {
        var signalRegistry = _storySignalRegistryService.Build(page, reportConsistencyNotes);
        var filterTopologyAssessment = PbirScoringService.BuildStoryFilterTopologyAssessment(page, reportFilters);
        var specialPageAssessment = _specialPageAssessmentService.Build(page);
        var archetypeClassification = PbirScoringService.BuildStoryAssessmentArchetypeClassification(
            signalRegistry,
            filterTopologyAssessment,
            specialPageAssessment);
        var semanticCoherenceAssessment = PbirScoringService.BuildStorySemanticCoherenceAssessment(page, specialPageAssessment);
        var gapAssessment = PbirScoringService.BuildStoryGapAssessment(
            signalRegistry,
            archetypeClassification,
            semanticCoherenceAssessment,
            filterTopologyAssessment,
            specialPageAssessment);
        var confidenceBreakdownAssessment = PbirScoringService.BuildStoryConfidenceBreakdownAssessment(
            signalRegistry,
            archetypeClassification,
            semanticCoherenceAssessment,
            filterTopologyAssessment,
            gapAssessment);
        var guidedStoryImprovements = PbirScoringService.BuildGuidedStoryImprovements(
            gapAssessment,
            specialPageAssessment);

        return new StoryAssessmentArtifacts(
            signalRegistry,
            filterTopologyAssessment,
            specialPageAssessment,
            archetypeClassification,
            semanticCoherenceAssessment,
            gapAssessment,
            confidenceBreakdownAssessment,
            guidedStoryImprovements);
    }
}

internal sealed record StoryAssessmentArtifacts(
    StorySignalRegistry SignalRegistry,
    StoryFilterTopologyAssessment FilterTopologyAssessment,
    StorySpecialPageAssessment SpecialPageAssessment,
    StoryAssessmentArchetypeClassification? ArchetypeClassification,
    StorySemanticCoherenceAssessment SemanticCoherenceAssessment,
    StoryGapAssessment GapAssessment,
    StoryConfidenceBreakdownAssessment ConfidenceBreakdownAssessment,
    GuidedStoryImprovements GuidedStoryImprovements);
