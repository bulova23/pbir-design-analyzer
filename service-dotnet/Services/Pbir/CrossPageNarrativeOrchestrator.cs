using PowerBIModelingService.Services.Pbir.CrossPageNarrative;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

internal sealed class CrossPageNarrativeOrchestrator
{
    public CrossPageNarrativeAssessment? Build(IReadOnlyList<PageScore>? pageScores)
    {
        return CrossPageNarrativeAssessmentBuilder.Build(pageScores);
    }
}
