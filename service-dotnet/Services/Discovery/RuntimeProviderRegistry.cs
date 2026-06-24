using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class RuntimeProviderRegistry
{
    private readonly Dictionary<string, RuntimeProviderRegistration> _providers = new(StringComparer.Ordinal);

    internal void Register(RuntimeProviderRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        _providers[registration.ProviderId] = registration;
    }

    internal bool TryGetProvider(string providerId, out RuntimeProviderRegistration? registration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        return _providers.TryGetValue(providerId, out registration);
    }

    internal IReadOnlyList<RuntimeProviderRegistration> DiscoverByCategory(string providerCategory, string? targetProfileId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerCategory);

        return _providers.Values
            .Where(provider =>
                string.Equals(provider.ProviderCategory, providerCategory, StringComparison.Ordinal) &&
                (string.IsNullOrWhiteSpace(targetProfileId) ||
                    provider.SupportedTargetProfiles.Contains(targetProfileId, StringComparer.Ordinal)))
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<RuntimeProviderRegistration> FindProvidersByCapability(string capabilityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

        return _providers.Values
            .Where(provider => provider.SupportedCapabilities.Contains(capabilityId, StringComparer.Ordinal))
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }
}
