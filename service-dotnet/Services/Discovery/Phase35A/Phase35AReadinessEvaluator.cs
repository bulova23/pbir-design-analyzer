namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35AReadinessEvaluator
{
    internal Phase35AReadinessResult Evaluate(Phase35ARequest request, Phase35AProviderProfile profile, Phase35AAuthorization authorization, Phase35AExecutionPolicy policy)
    {
        var reasons = new List<string> { "No runtime generation provider is available." };
        if (profile.ExecutionClass != Phase35AExecutionClass.Executable) reasons.Add("provider is not explicitly registered as executable");
        if (request.ProviderId != profile.ProviderId) reasons.Add("provider identity mismatch");
        if (authorization.Status != Phase35AAuthorizationStatus.Approved) reasons.Add("authorization is not approved");
        if (!policy.ExecutionPermitted) reasons.Add("execution policy denies execution");
        if (!profile.Capabilities.Contains(Phase35ACapability.PbirGeneration)) reasons.Add("provider does not declare PBIR generation capability");
        return new(Phase35AContracts.ReadinessV1, Phase35AReadiness.Unavailable, reasons);
    }
}

