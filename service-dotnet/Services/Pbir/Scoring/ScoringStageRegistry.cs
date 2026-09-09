namespace PowerBIModelingService.Services.Pbir.Scoring;

internal sealed class ScoringStageRegistry
{
    private readonly IReadOnlyList<IScoringStage> _stages;

    public ScoringStageRegistry(IReadOnlyList<IScoringStage> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);
        var copied = stages.ToArray();
        if (copied.Any(stage => stage is null))
        {
            throw new ArgumentException("Scoring stages cannot contain null entries.", nameof(stages));
        }

        var duplicate = copied
            .GroupBy(stage => stage.Id)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate scoring stage id '{duplicate.Key}'.", nameof(stages));
        }

        _stages = Array.AsReadOnly(copied);
    }

    public IReadOnlyList<IScoringStage> Stages => _stages;
}
