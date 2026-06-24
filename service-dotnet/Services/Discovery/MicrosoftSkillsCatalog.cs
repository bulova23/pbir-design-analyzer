using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftSkillsCatalog
{
    private readonly Dictionary<string, MicrosoftSkillDefinition> _skills;

    internal MicrosoftSkillsCatalog(MicrosoftSkillsCatalogDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        Document = document;
        _skills = (document.Skills ?? [])
            .GroupBy(skill => skill.SkillId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
    }

    internal MicrosoftSkillsCatalogDocument Document { get; private set; }

    internal string SchemaVersion => Document.SchemaVersion;

    internal void Register(MicrosoftSkillDefinition skill)
    {
        ArgumentNullException.ThrowIfNull(skill);

        _skills[skill.SkillId] = skill;
        Document = Document with
        {
            Skills = _skills.Values
                .OrderBy(candidate => candidate.SkillId, StringComparer.Ordinal)
                .ToArray()
        };
    }

    internal bool TryGetSkill(string skillId, out MicrosoftSkillDefinition? skill)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillId);

        var found = _skills.TryGetValue(skillId, out var resolved);
        skill = resolved;
        return found;
    }

    internal IReadOnlyList<MicrosoftSkillDefinition> DiscoverByCapability(string capabilityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityId);

        return _skills.Values
            .Where(skill =>
                skill.Status != MicrosoftSkillAvailabilityStatus.Unsupported &&
                skill.ProvidedCapabilities.Contains(capabilityId, StringComparer.Ordinal) &&
                !skill.UnsupportedCapabilities.Contains(capabilityId, StringComparer.Ordinal))
            .OrderBy(skill => skill.SkillId, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<MicrosoftSkillDefinition> DiscoverByTargetProfile(string targetProfileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProfileId);

        return _skills.Values
            .Where(skill =>
                skill.Status != MicrosoftSkillAvailabilityStatus.Unsupported &&
                skill.SupportedTargetProfiles.Contains(targetProfileId, StringComparer.Ordinal) &&
                !skill.UnsupportedProfiles.Contains(targetProfileId, StringComparer.Ordinal))
            .OrderBy(skill => skill.SkillId, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<MicrosoftSkillDefinition> FindSkillsByExecutionMode(ExecutionProviderMode executionMode)
    {
        return _skills.Values
            .Where(skill =>
                skill.Status != MicrosoftSkillAvailabilityStatus.Unsupported &&
                skill.SupportedExecutionModes.Contains(executionMode))
            .OrderBy(skill => skill.SkillId, StringComparer.Ordinal)
            .ToArray();
    }
}
