namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35BReadinessGate
{
    internal Phase35BReadinessDecision Validate(Phase35ARequest request, Phase35AProviderProfile profile, Phase35AAuthorization authorization, Phase35AExecutionPolicy policy, Phase35AReadinessResult readiness)
    {
        var reasons = new List<string>();
        if (readiness.SchemaVersion != Phase35AContracts.ReadinessV1) reasons.Add("readiness schema version is unsupported");
        if (readiness.State != Phase35AReadiness.ReadyForExecution) reasons.AddRange(readiness.Reasons.DefaultIfEmpty("readiness is not ready"));
        if (profile.ExecutionClass != Phase35AExecutionClass.Executable) reasons.Add("provider is not executable");
        if (authorization.Status != Phase35AAuthorizationStatus.Approved) reasons.Add("authorization is not approved");
        if (!policy.ExecutionPermitted) reasons.Add("execution policy denies execution");
        if (request.RequiredCapabilities.Any(capability => !profile.Capabilities.Contains(capability))) reasons.Add("provider capability is missing");
        return new(reasons.Count == 0, reasons.Distinct(StringComparer.Ordinal).ToArray());
    }
}
