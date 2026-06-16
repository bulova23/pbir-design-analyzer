using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

internal sealed class SpecialPageAssessmentService
{
    public StorySpecialPageAssessment Build(PageData page)
    {
        return PbirScoringService.BuildStorySpecialPageAssessment(page);
    }
}
