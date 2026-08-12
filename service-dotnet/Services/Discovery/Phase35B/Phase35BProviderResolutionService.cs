namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35BProviderResolutionService
{
    internal Phase35BProviderResolution Resolve(Phase35ARequest request, Phase35AAuthorization authorization, Phase35AExecutionPolicy policy, Phase35AReadinessResult readiness, Phase35BProviderRegistry registry)
    {
        var candidates = registry.Registrations.Where(registration => registration.Profile.ProviderId == request.ProviderId).ToArray();
        if (candidates.Length == 0) return new(false, "no exact provider match", null, null);
        if (candidates.Length != 1) return new(false, "ambiguous exact provider match", null, null);
        var candidate = candidates[0];
        if (candidate.Adapter is null) return new(false, "provider has no registered adapter", null, candidate.Profile);
        if (candidate.Adapter.ProviderId != candidate.Profile.ProviderId) return new(false, "adapter identity does not match profile", null, candidate.Profile);
        if (request.RequiredCapabilities.Any(capability => !candidate.Profile.Capabilities.Contains(capability) || !candidate.Adapter.Capabilities.Contains(capability))) return new(false, "provider capability mismatch", null, candidate.Profile);
        var adapterValidation = candidate.Adapter.ValidateRequest(request);
        if (!adapterValidation.IsValid) return new(false, string.Join("; ", adapterValidation.Errors), null, candidate.Profile);
        var declaredReadiness = candidate.Adapter.DeclareReadiness(request);
        if (declaredReadiness.State != readiness.State) return new(false, "adapter readiness does not match governed readiness snapshot", null, candidate.Profile);
        var authorizationDecision = new Phase35BAuthorizationGate().Validate(request, candidate.Profile, authorization, policy);
        if (!authorizationDecision.IsAllowed) return new(false, string.Join("; ", authorizationDecision.Reasons), null, candidate.Profile);
        var readinessDecision = new Phase35BReadinessGate().Validate(request, candidate.Profile, authorization, policy, readiness);
        return readinessDecision.IsReady
            ? new(true, string.Empty, candidate.Adapter, candidate.Profile)
            : new(false, string.Join("; ", readinessDecision.Reasons), null, candidate.Profile);
    }
}
