namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35BSessionFactory(Func<DateTimeOffset>? clock = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);

    internal Phase35BSession Create(Phase35ARequest request, Phase35AProviderProfile profile, Phase35AAuthorization authorization, Phase35AReadinessResult readiness, Phase35AExecutionPolicy policy, Phase35BTimeoutPolicy timeoutPolicy)
    {
        if (!timeoutPolicy.IsValid) throw new Phase35BContractException("Timeout policy is invalid.");
        if (request.RequiredCapabilities.Count != 1) throw new Phase35BContractException("Phase 35B requires exactly one runtime capability.");
        var createdAt = _clock();
        return new(
            $"session:{request.RequestId}:{profile.ProviderId}", request.RequestId, new Phase35ACanonicalJson().Hash(request),
            profile.ProviderId, profile.SchemaVersion, request.RequiredCapabilities[0], request.PolicyHash,
            authorization, readiness, timeoutPolicy, createdAt, Phase35BRuntimeState.Created,
            [new Phase35BLifecycleEntry(Phase35BRuntimeState.Created, Phase35BRuntimeEvent.Created, createdAt)]);
    }
}
