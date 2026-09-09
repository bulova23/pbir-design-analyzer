namespace PowerBIModelingService.Services.Pbir.Scoring;

internal readonly record struct ScoringStageId(string Value)
{
    public static ScoringStageId Accessibility { get; } = new("accessibility");
    public static ScoringStageId VisualBestPractices { get; } = new("visualBestPractices");
    public static ScoringStageId ReportConsistency { get; } = new("reportConsistency");
    public static ScoringStageId StoryAssessment { get; } = new("storyAssessment");

    public override string ToString() => Value;
}
