using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftSkillProviderRegistry
{
    private readonly Dictionary<string, MicrosoftSkillProviderDefinition> _providers = new(StringComparer.Ordinal);

    internal IReadOnlyList<MicrosoftSkillProviderDefinition> Registrations =>
        _providers.Values
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();

    internal void Register(MicrosoftSkillProviderDefinition provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _providers[provider.ProviderId] = provider;
    }

    internal bool TryGetProvider(string providerId, out MicrosoftSkillProviderDefinition? provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        var found = _providers.TryGetValue(providerId, out var resolved);
        provider = resolved;
        return found;
    }

    internal IReadOnlyList<MicrosoftSkillProviderDefinition> DiscoverByCategory(string providerCategory, string? targetProfileId = null)
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

    internal IReadOnlyList<MicrosoftSkillProviderDefinition> FindProvidersByCapability(string capabilityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

        return _providers.Values
            .Where(provider => provider.SupportedCapabilities.Contains(capabilityId, StringComparer.Ordinal))
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<MicrosoftSkillProviderDefinition> FindProvidersBySkill(string skillId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        return _providers.Values
            .Where(provider => provider.SupportedSkills.Contains(skillId, StringComparer.Ordinal))
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<MicrosoftSkillProviderDefinition> FindProvidersByTargetProfile(string targetProfileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProfileId);

        return _providers.Values
            .Where(provider => provider.SupportedTargetProfiles.Contains(targetProfileId, StringComparer.Ordinal))
            .OrderBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }
}
