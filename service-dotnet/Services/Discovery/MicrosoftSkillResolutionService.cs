using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftSkillResolutionService
{
    internal MicrosoftSkillResolutionResult Resolve(
        MicrosoftSkillsCatalog catalog,
        string targetProfileId,
        IReadOnlyCollection<string> requiredCapabilities,
        IReadOnlyCollection<string> optionalCapabilities,
        IReadOnlyCollection<ExecutionProviderMode>? preferredExecutionModes = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetProfileId);
        ArgumentNullException.ThrowIfNull(requiredCapabilities);
        ArgumentNullException.ThrowIfNull(optionalCapabilities);

        preferredExecutionModes ??= [ExecutionProviderMode.Assisted];

        var requiredSet = requiredCapabilities
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var optionalSet = optionalCapabilities
            .Distinct(StringComparer.Ordinal)
            .Except(requiredSet, StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var selectedRequired = new List<MicrosoftSkillDefinition>();
        var selectedOptional = new List<MicrosoftSkillDefinition>();
        var coveredRequired = new HashSet<string>(StringComparer.Ordinal);
        var coveredOptional = new HashSet<string>(StringComparer.Ordinal);
        var candidates = DiscoverCandidates(catalog, targetProfileId, requiredSet.Concat(optionalSet).ToArray(), preferredExecutionModes);
        var remainingRequired = new HashSet<string>(requiredSet, StringComparer.Ordinal);

        while (remainingRequired.Count > 0)
        {
            var nextSkill = SelectNextSkill(candidates, remainingRequired, preferredExecutionModes);
            if (nextSkill is null)
            {
                break;
            }

            if (!selectedRequired.Contains(nextSkill))
            {
                selectedRequired.Add(nextSkill);
            }

            foreach (var capability in nextSkill.ProvidedCapabilities)
            {
                if (requiredSet.Contains(capability))
                {
                    coveredRequired.Add(capability);
                    remainingRequired.Remove(capability);
                }

                if (optionalSet.Contains(capability))
                {
                    coveredOptional.Add(capability);
                }
            }

            foreach (var prerequisite in nextSkill.PrerequisiteCapabilities)
            {
                if (!coveredRequired.Contains(prerequisite))
                {
                    remainingRequired.Add(prerequisite);
                    requiredSet.Add(prerequisite);
                }
            }

            candidates.Remove(nextSkill);
        }

        foreach (var capability in optionalSet.Except(coveredOptional, StringComparer.Ordinal))
        {
            var skill = DiscoverCandidates(catalog, targetProfileId, [capability], preferredExecutionModes)
                .FirstOrDefault();
            if (skill is null)
            {
                continue;
            }

            if (!selectedRequired.Contains(skill) && !selectedOptional.Contains(skill))
            {
                selectedOptional.Add(skill);
            }

            foreach (var provided in skill.ProvidedCapabilities)
            {
                if (optionalSet.Contains(provided))
                {
                    coveredOptional.Add(provided);
                }
            }
        }

        var candidateSkillSet = selectedRequired
            .Concat(selectedOptional)
            .Distinct()
            .Select(skill => ToCandidate(skill, requiredSet.Concat(optionalSet).ToArray()))
            .OrderBy(candidate => candidate.SkillId, StringComparer.Ordinal)
            .ToArray();
        var unresolvedRequired = requiredSet
            .Except(coveredRequired, StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var unresolvedOptional = optionalSet
            .Except(coveredOptional, StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();

        return new MicrosoftSkillResolutionResult(
            ResolutionId: $"microsoftSkillResolution:{targetProfileId}",
            TargetProfileId: targetProfileId,
            CandidateSkillSet: candidateSkillSet,
            RequiredSkills: selectedRequired
                .Distinct()
                .Select(skill => ToCandidate(skill, requiredSet.ToArray()))
                .OrderBy(candidate => candidate.SkillId, StringComparer.Ordinal)
                .ToArray(),
            OptionalSkills: selectedOptional
                .Distinct()
                .Select(skill => ToCandidate(skill, optionalSet.ToArray()))
                .OrderBy(candidate => candidate.SkillId, StringComparer.Ordinal)
                .ToArray(),
            CapabilityCoverage: new MicrosoftSkillCapabilityCoverageSummary(
                RequiredCapabilitiesRequested: requiredSet.OrderBy(capability => capability, StringComparer.Ordinal).ToArray(),
                RequiredCapabilitiesCovered: coveredRequired.OrderBy(capability => capability, StringComparer.Ordinal).ToArray(),
                OptionalCapabilitiesRequested: optionalSet.OrderBy(capability => capability, StringComparer.Ordinal).ToArray(),
                OptionalCapabilitiesCovered: coveredOptional.OrderBy(capability => capability, StringComparer.Ordinal).ToArray()),
            UnresolvedCapabilities: new MicrosoftSkillUnresolvedCapabilitySummary(
                RequiredCapabilities: unresolvedRequired,
                OptionalCapabilities: unresolvedOptional,
                UnsupportedCapabilities: unresolvedRequired.Concat(unresolvedOptional)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(capability => capability, StringComparer.Ordinal)
                    .ToArray()));
    }

    private static List<MicrosoftSkillDefinition> DiscoverCandidates(
        MicrosoftSkillsCatalog catalog,
        string targetProfileId,
        IReadOnlyCollection<string> capabilities,
        IReadOnlyCollection<ExecutionProviderMode> preferredExecutionModes)
    {
        return capabilities
            .SelectMany(capability => catalog.DiscoverByCapability(capability))
            .Where(skill =>
                skill.SupportedTargetProfiles.Contains(targetProfileId, StringComparer.Ordinal) &&
                !skill.UnsupportedProfiles.Contains(targetProfileId, StringComparer.Ordinal) &&
                skill.SupportedExecutionModes.Any(mode => preferredExecutionModes.Contains(mode)))
            .Distinct()
            .OrderBy(skill => GetStatusRank(skill.Status))
            .ThenByDescending(skill => skill.ProvidedCapabilities.Intersect(capabilities, StringComparer.Ordinal).Count())
            .ThenBy(skill => skill.SkillId, StringComparer.Ordinal)
            .ToList();
    }

    private static MicrosoftSkillDefinition? SelectNextSkill(
        IReadOnlyList<MicrosoftSkillDefinition> candidates,
        IReadOnlyCollection<string> remainingRequired,
        IReadOnlyCollection<ExecutionProviderMode> preferredExecutionModes)
    {
        return candidates
            .Where(skill => skill.SupportedExecutionModes.Any(mode => preferredExecutionModes.Contains(mode)))
            .OrderBy(skill => GetStatusRank(skill.Status))
            .ThenByDescending(skill => skill.ProvidedCapabilities.Intersect(remainingRequired, StringComparer.Ordinal).Count())
            .ThenByDescending(skill => skill.PrerequisiteCapabilities.Count)
            .ThenBy(skill => skill.SkillId, StringComparer.Ordinal)
            .FirstOrDefault(skill => skill.ProvidedCapabilities.Any(capability => remainingRequired.Contains(capability, StringComparer.Ordinal)));
    }

    private static MicrosoftSkillCandidate ToCandidate(MicrosoftSkillDefinition skill, IReadOnlyCollection<string> relevantCapabilities)
    {
        return new MicrosoftSkillCandidate(
            SkillId: skill.SkillId,
            SkillVersion: skill.SkillVersion,
            SkillStatus: skill.Status,
            MatchedCapabilities: skill.ProvidedCapabilities
                .Intersect(relevantCapabilities, StringComparer.Ordinal)
                .OrderBy(capability => capability, StringComparer.Ordinal)
                .ToArray());
    }

    private static int GetStatusRank(MicrosoftSkillAvailabilityStatus status)
    {
        return status switch
        {
            MicrosoftSkillAvailabilityStatus.Available => 0,
            MicrosoftSkillAvailabilityStatus.Planned => 1,
            MicrosoftSkillAvailabilityStatus.Deprecated => 2,
            _ => 3,
        };
    }
}
