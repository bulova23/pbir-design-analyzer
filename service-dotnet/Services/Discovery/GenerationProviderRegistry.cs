using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationProviderRegistry
{
    private readonly Dictionary<string, GenerationProviderDefinition> _providers = new(StringComparer.Ordinal);

    internal void Register(GenerationProviderDefinition provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _providers[provider.ProviderId] = provider;
    }

    internal IReadOnlyList<GenerationProviderDefinition> Discover()
    {
        return _providers.Values
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }

    internal bool TryGetProvider(string providerId, out GenerationProviderDefinition? provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        var found = _providers.TryGetValue(providerId, out var resolved);
        provider = resolved;
        return found;
    }

    internal IReadOnlyList<GenerationProviderDefinition> FindProvidersByCapability(string capabilityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

        return _providers.Values
            .Where(provider => provider.SupportedCapabilities.Contains(capabilityId, StringComparer.Ordinal))
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<GenerationProviderDefinition> FindProvidersByArtifactType(GenerationProviderArtifactType artifactType)
    {
        return _providers.Values
            .Where(provider => provider.SupportedArtifactTypes.Contains(artifactType))
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<GenerationProviderDefinition> FindProvidersByTargetProfile(string targetProfileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProfileId);

        return _providers.Values
            .Where(provider => provider.SupportedTargetProfiles.Contains(targetProfileId, StringComparer.Ordinal))
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }
}
