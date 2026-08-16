namespace PowerBIModelingService.Services.Discovery;

internal interface IPhase35BValidationStage
{
    string Name { get; }
    Phase35BValidationStageResult Validate(Phase35BExecutionContext context, Phase35BOfflineExecutionResult execution);
}

internal sealed class Phase35BValidationPipeline(IReadOnlyList<IPhase35BValidationStage> stages)
{
    internal Phase35BValidationResult Validate(Phase35BExecutionContext context, Phase35BOfflineExecutionResult execution)
    {
        var results = new List<Phase35BValidationStageResult>();
        foreach (var stage in stages)
        {
            var result = stage.Validate(context, execution);
            results.Add(result);
            if (!result.IsValid) break;
        }
        return new(results);
    }
}

internal sealed class Phase35BContractValidationStage(string name, Func<Phase35BExecutionContext, Phase35BOfflineExecutionResult, IReadOnlyList<string>> validator) : IPhase35BValidationStage
{
    public string Name { get; } = name;

    public Phase35BValidationStageResult Validate(Phase35BExecutionContext context, Phase35BOfflineExecutionResult execution)
    {
        var errors = validator(context, execution);
        return new(Name, errors.Count == 0, errors);
    }
}

internal static class Phase35BDefaultValidationStages
{
    internal static IReadOnlyList<IPhase35BValidationStage> Create() =>
    [
        new Phase35BContractValidationStage("request", (context, _) => context.Request.RequestId == context.Session.RequestId ? [] : ["request identity mismatch"]),
        new Phase35BContractValidationStage("policy", (_, execution) => execution.Result.SchemaVersion == Phase35AContracts.ResultV1 ? [] : ["result schema version is unsupported"]),
        new Phase35BContractValidationStage("authorization", (context, _) => context.Session.Authorization.Status == Phase35AAuthorizationStatus.Approved ? [] : ["authorization is not approved"]),
        new Phase35BContractValidationStage("readiness", (context, _) => context.Session.Readiness.State == Phase35AReadiness.ReadyForExecution ? [] : ["readiness is not ready"]),
        new Phase35BContractValidationStage("provider compatibility", (context, execution) => execution.Artifact.ProviderId == context.Session.ProviderId ? [] : ["provider identity mismatch"]),
        new Phase35BContractValidationStage("result", (_, execution) => execution.Result.Status == Phase35AResultStatus.Accepted ? [] : ["result is not accepted"]),
        new Phase35BContractValidationStage("artifact", (context, execution) => execution.Artifact.RequestId == context.Session.RequestId ? [] : ["artifact request identity mismatch"]),
        new Phase35BContractValidationStage("acceptance", (_, execution) => execution.Artifact.Quarantine.Reason == Phase35AQuarantineReason.None ? [] : ["artifact is quarantined"])
    ];
}
