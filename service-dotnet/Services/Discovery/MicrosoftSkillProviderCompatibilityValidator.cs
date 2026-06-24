using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftSkillProviderCompatibilityValidator
{
    internal MicrosoftSkillProviderCompatibilityValidationResult Validate(
        MicrosoftSkillPlanningState skillState,
        MicrosoftSkillProviderRegistry registry,
        SkillProviderSelection? selection)
    {
        ArgumentNullException.ThrowIfNull(skillState);
        ArgumentNullException.ThrowIfNull(registry);

        var missingSections = new List<string>();
        var missingFields = new List<string>();
        var duplicateProviderIds = new List<string>();
        var unsupportedTargetProfiles = new List<string>();
        var unsupportedSkills = new List<string>();
        var unsupportedCapabilities = new List<string>();
        var unsatisfiedPrerequisites = new List<string>();
        var versionMismatches = new List<string>();
        var integrityFailures = new List<string>();

        if (selection is null)
        {
            missingSections.Add("skillProviderSelection");
        }

        if (!string.Equals(skillState.Catalog.SchemaVersion, MicrosoftSkillsCatalogContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            versionMismatches.Add(skillState.Catalog.SchemaVersion);
        }

        if (selection is not null && !string.Equals(selection.SchemaVersion, SkillProviderSelectionContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            versionMismatches.Add(selection.SchemaVersion);
        }

        var providers = registry.Registrations;
        if (providers.Count == 0)
        {
            missingSections.Add("providers");
        }

        duplicateProviderIds.AddRange(providers
            .GroupBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key));

        foreach (var provider in providers)
        {
            if (!string.Equals(provider.SchemaVersion, MicrosoftSkillProviderContract.SchemaVersionV1, StringComparison.Ordinal))
            {
                versionMismatches.Add(provider.SchemaVersion);
            }

            ValidateNotBlank(provider.ProviderId, "providerId", missingFields);
            ValidateNotBlank(provider.ProviderName, $"{provider.ProviderId}.providerName", missingFields);
            ValidateNotBlank(provider.ProviderVersion, $"{provider.ProviderId}.providerVersion", missingFields);
            ValidateNotBlank(provider.ProviderCategory, $"{provider.ProviderId}.providerCategory", missingFields);

            if (provider.SupportedExecutionModes.Count == 0)
            {
                missingSections.Add($"{provider.ProviderId}.supportedExecutionModes");
            }

            if (provider.SupportedSkills.Count == 0)
            {
                missingSections.Add($"{provider.ProviderId}.supportedSkills");
            }

            if (provider.SupportedCapabilities.Count == 0)
            {
                missingSections.Add($"{provider.ProviderId}.supportedCapabilities");
            }

            if (provider.SupportedTargetProfiles.Count == 0)
            {
                missingSections.Add($"{provider.ProviderId}.supportedTargetProfiles");
            }
        }

        if (selection is not null)
        {
            var targetProfileId = selection.TargetProfileId;
            var selectedProviders = selection.SelectedProviderCandidates
                .Select(candidate =>
                {
                    if (string.IsNullOrWhiteSpace(candidate.ProviderId))
                    {
                        integrityFailures.Add("selected provider id is required.");
                        return null;
                    }

                    return registry.TryGetProvider(candidate.ProviderId, out var provider) ? provider : null;
                })
                .ToArray();

            foreach (var provider in selectedProviders)
            {
                if (provider is null)
                {
                    integrityFailures.Add("selected provider is missing from the registry.");
                    continue;
                }

                if (!provider.SupportedTargetProfiles.Contains(targetProfileId, StringComparer.Ordinal))
                {
                    unsupportedTargetProfiles.Add(targetProfileId);
                }

                foreach (var skillId in provider.SupportedSkills)
                {
                    if (!TryGetSkill(skillState, skillId, out _))
                    {
                        integrityFailures.Add($"provider {provider.ProviderId} references unknown skill {skillId}.");
                    }
                }
            }

            var coveredSkills = selectedProviders
                .Where(provider => provider is not null)
                .SelectMany(provider => provider!.SupportedSkills)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);
            var coveredCapabilities = selectedProviders
                .Where(provider => provider is not null)
                .SelectMany(provider => provider!.SupportedCapabilities)
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var requiredSkillId in selection.RequiredSkills.Distinct(StringComparer.Ordinal))
            {
                if (!coveredSkills.Contains(requiredSkillId))
                {
                    unsupportedSkills.Add(requiredSkillId);
                }
            }

            foreach (var requiredCapability in selection.CoverageSummary.RequiredCapabilitiesRequested.Distinct(StringComparer.Ordinal))
            {
                if (!coveredCapabilities.Contains(requiredCapability))
                {
                    unsupportedCapabilities.Add(requiredCapability);
                }
            }

            foreach (var selectedProvider in selectedProviders.Where(provider => provider is not null))
            {
                foreach (var skillId in selectedProvider!.SupportedSkills.Distinct(StringComparer.Ordinal))
                {
                    if (!TryGetSkill(skillState, skillId, out var skill))
                    {
                        continue;
                    }

                    foreach (var prerequisite in skill!.PrerequisiteCapabilities)
                    {
                        if (!selectedProvider.SupportedCapabilities.Contains(prerequisite, StringComparer.Ordinal))
                        {
                            unsatisfiedPrerequisites.Add(prerequisite);
                        }
                    }
                }
            }
        }

        return new MicrosoftSkillProviderCompatibilityValidationResult(
            new MicrosoftSkillProviderCompatibilityDiagnostics(
                MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
                MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
                DuplicateProviderIds: duplicateProviderIds.Distinct(StringComparer.Ordinal).ToArray(),
                UnsupportedTargetProfiles: unsupportedTargetProfiles.Distinct(StringComparer.Ordinal).ToArray(),
                UnsupportedSkills: unsupportedSkills.Distinct(StringComparer.Ordinal).ToArray(),
                UnsupportedCapabilities: unsupportedCapabilities.Distinct(StringComparer.Ordinal).ToArray(),
                UnsatisfiedPrerequisites: unsatisfiedPrerequisites.Distinct(StringComparer.Ordinal).ToArray(),
                VersionMismatches: versionMismatches.Distinct(StringComparer.Ordinal).ToArray(),
                IntegrityFailures: integrityFailures.Distinct(StringComparer.Ordinal).ToArray()));
    }

    private static bool TryGetSkill(MicrosoftSkillPlanningState skillState, string skillId, out MicrosoftSkillDefinition? skill)
    {
        skill = skillState.Catalog.Skills.FirstOrDefault(candidate => string.Equals(candidate.SkillId, skillId, StringComparison.Ordinal));
        return skill is not null;
    }

    private static void ValidateNotBlank(string? value, string fieldName, ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldName);
        }
    }
}
