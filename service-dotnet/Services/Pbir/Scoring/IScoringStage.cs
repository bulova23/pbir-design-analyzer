using System.Collections.ObjectModel;
using System.Text.Json;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir.Scoring;

internal interface IScoringStage
{
    ScoringStageId Id { get; }

    ScoringStageResult Analyze(ReportAnalysisContext context, ScoringStageInput input);
}

internal sealed record ScoringStageInput(
    IReadOnlyList<PageData> Pages,
    IReadOnlyList<string> ThemeColors,
    JsonElement? Configuration,
    IReadOnlyList<string> Recommendations);

internal sealed record ScoringStageResult(
    IReadOnlyDictionary<string, IReadOnlyList<FrameworkFeedbackItem>> Feedback,
    IReadOnlyList<string> Recommendations)
{
    public static ScoringStageResult Empty { get; } = new(
        new ReadOnlyDictionary<string, IReadOnlyList<FrameworkFeedbackItem>>(
            new Dictionary<string, IReadOnlyList<FrameworkFeedbackItem>>()),
        Array.Empty<string>());
}
