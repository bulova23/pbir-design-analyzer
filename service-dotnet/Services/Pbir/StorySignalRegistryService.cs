using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

internal sealed class StorySignalRegistryService
{
    public StorySignalRegistry Build(PageData page, IReadOnlyList<string>? reportConsistencyNotes = null)
    {
        return PbirScoringService.BuildStorySignalRegistry(page, reportConsistencyNotes);
    }
}
