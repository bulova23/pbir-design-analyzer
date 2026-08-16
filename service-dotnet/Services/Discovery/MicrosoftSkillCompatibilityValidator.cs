using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftSkillCompatibilityValidator
{
    internal MicrosoftSkillCompatibilityValidationResult Validate(
        MicrosoftSkillsCatalog catalog,
        string targetProfileId,
        IReadOnlyCollection<string> requiredCapabilities,
        IReadOnlyCollection<string> optionalCapabilities,
        MicrosoftSkillResolutionResult? resolution)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProfileId);
        ArgumentNullException.ThrowIfNull(requiredCapabilities);
        ArgumentNullException.ThrowIfNull(optionalCapabilities);

        var document = catalog.Document;
        var missingSections = new List<string>();
        var missingFields = new List<string>();
        var duplicateSkillIds = new List<string>();
        var unsupportedTargetProfiles = new List<string>();
        var unsupportedCapabilities = new List<string>();
        var unsatisfiedPrerequisites = new List<string>();
        var versionMismatches = new List<string>();
        var integrityFailures = new List<string>();

        if (!string.Equals(document.SchemaVersion, MicrosoftSkillsCatalogContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            versionMismatches.Add(document.SchemaVersion);
        }

        ValidateNotBlank(document.CatalogId, "catalogId", missingFields);
        ValidateNotBlank(document.CatalogVersion, "catalogVersion", missingFields);
        ValidateNotBlank(document.ProviderCategory, "providerCategory", missingFields);

        var skills = document.Skills ?? [];

        if (skills.Count == 0)
        {
            missingSections.Add("skills");
        }

        var duplicateIds = skills
            .GroupBy(skill => skill.SkillId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        duplicateSkillIds.AddRange(duplicateIds);

        var allProvidedCapabilities = skills
            .SelectMany(skill => skill.ProvidedCapabilities ?? [])
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var supportedProfiles = skills
            .SelectMany(skill => skill.SupportedTargetProfiles ?? [])
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (!supportedProfiles.Contains(targetProfileId))
        {
            unsupportedTargetProfiles.Add(targetProfileId);
        }

        for (var index = 0; index < skills.Count; index++)
        {
            var skill = skills[index];
            var prefix = $"skills[{index}]";

            if (!string.Equals(skill.SchemaVersion, MicrosoftSkillDefinitionContract.SchemaVersionV1, StringComparison.Ordinal))
            {
                versionMismatches.Add(skill.SchemaVersion);
            }

            ValidateNotBlank(skill.SkillId, $"{prefix}.skillId", missingFields);
            ValidateNotBlank(skill.SkillName, $"{prefix}.skillName", missingFields);
            ValidateNotBlank(skill.SkillVersion, $"{prefix}.skillVersion", missingFields);
            ValidateNotBlank(skill.SkillCategory, $"{prefix}.skillCategory", missingFields);

            if (skill.ProvidedCapabilities is null || skill.ProvidedCapabilities.Count == 0)
            {
                missingSections.Add($"{prefix}.providedCapabilities");
            }

            if (skill.SupportedTargetProfiles is null || skill.SupportedTargetProfiles.Count == 0)
            {
                missingSections.Add($"{prefix}.supportedTargetProfiles");
            }

            if (skill.SupportedExecutionModes is null || skill.SupportedExecutionModes.Count == 0)
            {
                missingSections.Add($"{prefix}.supportedExecutionModes");
            }

            foreach (var prerequisite in skill.PrerequisiteCapabilities ?? [])
            {
                if (!allProvidedCapabilities.Contains(prerequisite))
                {
                    unsatisfiedPrerequisites.Add(prerequisite);
                }
            }
        }

        var candidateSkills = resolution?.CandidateSkillSet
            .Select(candidate => candidate.SkillId)
            .ToHashSet(StringComparer.Ordinal) ?? [];
        var candidateCapabilities = skills
            .Where(skill => candidateSkills.Contains(skill.SkillId))
            .SelectMany(skill => skill.ProvidedCapabilities)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var capability in requiredCapabilities.Distinct(StringComparer.Ordinal))
        {
            var anySupportingSkill = skills.Any(skill =>
                Supports(skill, targetProfileId, capability));
            if (!anySupportingSkill)
            {
                unsupportedCapabilities.Add(capability);
            }

            if (resolution is not null &&
                !resolution.CapabilityCoverage.RequiredCapabilitiesCovered.Contains(capability, StringComparer.Ordinal))
            {
                integrityFailures.Add($"required capability {capability} is not covered by the selected skills.");
            }
        }

        foreach (var capability in optionalCapabilities.Distinct(StringComparer.Ordinal))
        {
            var anySupportingSkill = skills.Any(skill =>
                Supports(skill, targetProfileId, capability));
            if (!anySupportingSkill)
            {
                unsupportedCapabilities.Add(capability);
            }
        }

        if (resolution is not null)
        {
            foreach (var selectedSkillId in resolution.RequiredSkills.Concat(resolution.OptionalSkills).Select(skill => skill.SkillId).Distinct(StringComparer.Ordinal))
            {
                if (!catalog.TryGetSkill(selectedSkillId, out var skill) || skill is null)
                {
                    integrityFailures.Add($"selected skill {selectedSkillId} is missing from the catalog.");
                    continue;
                }

                if (!skill.SupportedTargetProfiles.Contains(targetProfileId, StringComparer.Ordinal) ||
                    skill.UnsupportedProfiles.Contains(targetProfileId, StringComparer.Ordinal))
                {
                    unsupportedTargetProfiles.Add(targetProfileId);
                }

                foreach (var prerequisite in skill.PrerequisiteCapabilities)
                {
                    if (!candidateCapabilities.Contains(prerequisite))
                    {
                        unsatisfiedPrerequisites.Add(prerequisite);
                    }
                }
            }
        }

        return new MicrosoftSkillCompatibilityValidationResult(
            new MicrosoftSkillCompatibilityDiagnostics(
                MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
                MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
                DuplicateSkillIds: duplicateSkillIds.Distinct(StringComparer.Ordinal).ToArray(),
                UnsupportedTargetProfiles: unsupportedTargetProfiles.Distinct(StringComparer.Ordinal).ToArray(),
                UnsupportedCapabilities: unsupportedCapabilities.Distinct(StringComparer.Ordinal).ToArray(),
                UnsatisfiedPrerequisites: unsatisfiedPrerequisites.Distinct(StringComparer.Ordinal).ToArray(),
                VersionMismatches: versionMismatches.Distinct(StringComparer.Ordinal).ToArray(),
                IntegrityFailures: integrityFailures.Distinct(StringComparer.Ordinal).ToArray()));
    }

    private static bool Supports(MicrosoftSkillDefinition skill, string targetProfileId, string capability)
    {
        return skill.Status != MicrosoftSkillAvailabilityStatus.Unsupported &&
            skill.SupportedTargetProfiles.Contains(targetProfileId, StringComparer.Ordinal) &&
            !skill.UnsupportedProfiles.Contains(targetProfileId, StringComparer.Ordinal) &&
            skill.ProvidedCapabilities.Contains(capability, StringComparer.Ordinal) &&
            !skill.UnsupportedCapabilities.Contains(capability, StringComparer.Ordinal);
    }

    private static void ValidateNotBlank(string? value, string fieldName, ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldName);
        }
    }
}
