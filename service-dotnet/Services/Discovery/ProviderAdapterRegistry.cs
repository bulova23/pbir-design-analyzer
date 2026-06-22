using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ProviderAdapterRegistry
{
    private readonly List<ProviderAdapterDefinition> _adapters = [];

    internal void Register(ProviderAdapterDefinition adapterDefinition)
    {
        ArgumentNullException.ThrowIfNull(adapterDefinition);

        _adapters.RemoveAll(adapter => string.Equals(adapter.AdapterId, adapterDefinition.AdapterId, StringComparison.Ordinal));
        _adapters.Add(adapterDefinition);
    }

    internal ProviderAdapterDefinition? Discover(string adapterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);

        return _adapters.FirstOrDefault(adapter => string.Equals(adapter.AdapterId, adapterId, StringComparison.Ordinal));
    }

    internal IReadOnlyList<ProviderAdapterDefinition> DiscoverAll()
    {
        return _adapters.ToArray();
    }

    internal IReadOnlyList<ProviderAdapterDefinition> FindByCapability(string capability)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capability);

        return _adapters
            .Where(adapter => adapter.SupportedCapabilities.Contains(capability, StringComparer.Ordinal))
            .ToArray();
    }

    internal IReadOnlyList<ProviderAdapterDefinition> FindByTargetProfile(string targetProfile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProfile);

        return _adapters
            .Where(adapter => adapter.SupportedTargetProfiles.Contains(targetProfile, StringComparer.Ordinal))
            .ToArray();
    }

    internal ProviderAdapterCompatibilityEvaluation EvaluateCompatibility(
        string adapterId,
        ProviderAdapterRequest request,
        ExecutionPlan executionPlan,
        GenerationRequest generationRequest,
        ProviderAdapterCompatibilityService compatibilityService)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionPlan);
        ArgumentNullException.ThrowIfNull(generationRequest);
        ArgumentNullException.ThrowIfNull(compatibilityService);

        var adapter = Discover(adapterId);
        return adapter is null
            ? new ProviderAdapterCompatibilityEvaluation(
                ProviderAdapterCompatibilityStatus.Incompatible,
                new ProviderAdapterCompatibilityDiagnostics(
                    MissingRequiredSections: [],
                    MissingRequiredFields: [],
                    TargetCompatibilityFailures: [],
                    CapabilityCompatibilityFailures: [],
                    ExecutionPlanCompatibilityFailures: [$"adapterRegistry does not contain adapter {adapterId}."],
                    VersionCompatibilityFailures: []))
            : compatibilityService.Evaluate(adapter, request, executionPlan, generationRequest);
    }
}
