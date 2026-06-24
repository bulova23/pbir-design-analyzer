using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftSkillProviderResolutionService
{
    internal SkillProviderSelection Resolve(
        MicrosoftSkillPlanningState skillState,
        MicrosoftSkillProviderRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(skillState);
        ArgumentNullException.ThrowIfNull(registry);

        var resolution = skillState.Resolution ?? throw new ArgumentException("Microsoft skill resolution is required.", nameof(skillState));
        var requiredSkills = resolution.RequiredSkills
            .Select(skill => skill.SkillId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(skillId => skillId, StringComparer.Ordinal)
            .ToArray();
        var optionalSkills = resolution.OptionalSkills
            .Select(skill => skill.SkillId)
            .Except(requiredSkills, StringComparer.Ordinal)
            .OrderBy(skillId => skillId, StringComparer.Ordinal)
            .ToArray();
        var requiredCapabilities = resolution.CapabilityCoverage.RequiredCapabilitiesRequested
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var optionalCapabilities = resolution.CapabilityCoverage.OptionalCapabilitiesRequested
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var targetProfileId = resolution.TargetProfileId;

        var candidates = registry.FindProvidersByTargetProfile(targetProfileId)
            .Select(provider => ToCandidate(provider, requiredSkills, optionalSkills, requiredCapabilities, optionalCapabilities, targetProfileId))
            .Where(candidate =>
                candidate.MatchedSkills.Count > 0 ||
                candidate.MatchedCapabilities.Count > 0)
            .OrderBy(candidate => GetStatusRank(candidate.ProviderStatus))
            .ThenByDescending(candidate => candidate.MatchedSkills.Count)
            .ThenByDescending(candidate => candidate.MatchedCapabilities.Count)
            .ThenBy(candidate => candidate.ProviderId, StringComparer.Ordinal)
            .ToArray();

        var selected = SelectProviders(candidates, requiredSkills, requiredCapabilities);
        var coveredSkills = selected
            .SelectMany(candidate => candidate.MatchedSkills)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(skillId => skillId, StringComparer.Ordinal)
            .ToArray();
        var coveredOptionalSkills = selected
            .SelectMany(candidate => candidate.MatchedSkills)
            .Intersect(optionalSkills, StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(skillId => skillId, StringComparer.Ordinal)
            .ToArray();
        var coveredCapabilities = selected
            .SelectMany(candidate => candidate.MatchedCapabilities)
            .Intersect(requiredCapabilities, StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var coveredOptionalCapabilities = selected
            .SelectMany(candidate => candidate.MatchedCapabilities)
            .Intersect(optionalCapabilities, StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var unsupportedSkills = requiredSkills
            .Except(coveredSkills, StringComparer.Ordinal)
            .OrderBy(skillId => skillId, StringComparer.Ordinal)
            .ToArray();
        var unresolvedRequiredCapabilities = requiredCapabilities
            .Except(coveredCapabilities, StringComparer.Ordinal)
            .Union(resolution.UnresolvedCapabilities.RequiredCapabilities, StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var unresolvedOptionalCapabilities = optionalCapabilities
            .Except(coveredOptionalCapabilities, StringComparer.Ordinal)
            .Union(resolution.UnresolvedCapabilities.OptionalCapabilities, StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var blockingIssues = unsupportedSkills
            .Concat(unresolvedRequiredCapabilities)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(issue => issue, StringComparer.Ordinal)
            .ToArray();
        var knownProviderIds = candidates
            .Select(candidate => candidate.ProviderId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(providerId => providerId, StringComparer.Ordinal)
            .ToArray();

        return new SkillProviderSelection(
            SchemaVersion: SkillProviderSelectionContract.SchemaVersionV1,
            SelectionId: $"skillProviderSelection:{targetProfileId}",
            TargetProfileId: targetProfileId,
            RequiredSkills: requiredSkills,
            CandidateProviders: candidates,
            SelectedProviderCandidates: selected,
            UnsupportedSkills: unsupportedSkills,
            CoverageSummary: new MicrosoftSkillProviderCoverageSummary(
                RequiredSkillsRequested: requiredSkills,
                RequiredSkillsCovered: coveredSkills.Intersect(requiredSkills, StringComparer.Ordinal).ToArray(),
                OptionalSkillsRequested: optionalSkills,
                OptionalSkillsCovered: coveredOptionalSkills,
                RequiredCapabilitiesRequested: requiredCapabilities,
                RequiredCapabilitiesCovered: coveredCapabilities,
                OptionalCapabilitiesRequested: optionalCapabilities,
                OptionalCapabilitiesCovered: coveredOptionalCapabilities,
                UnresolvedRequiredCapabilities: unresolvedRequiredCapabilities,
                UnresolvedOptionalCapabilities: unresolvedOptionalCapabilities,
                SupportedTargetProfiles: candidates
                    .SelectMany(candidate => candidate.MatchedTargetProfiles)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(profile => profile, StringComparer.Ordinal)
                    .ToArray()),
            ReadinessSummary: new MicrosoftSkillProviderSelectionReadinessSummary(
                Readiness: blockingIssues.Length == 0
                    ? MicrosoftSkillProviderReadinessState.Satisfied
                    : MicrosoftSkillProviderReadinessState.PartiallySatisfied,
                KnownProviderIds: knownProviderIds,
                BlockingIssues: blockingIssues,
                UnresolvedSkills: unsupportedSkills,
                UnresolvedCapabilities: unresolvedRequiredCapabilities));
    }

    private static MicrosoftSkillProviderCandidate ToCandidate(
        MicrosoftSkillProviderDefinition provider,
        IReadOnlyCollection<string> requiredSkills,
        IReadOnlyCollection<string> optionalSkills,
        IReadOnlyCollection<string> requiredCapabilities,
        IReadOnlyCollection<string> optionalCapabilities,
        string targetProfileId)
    {
        return new MicrosoftSkillProviderCandidate(
            ProviderId: provider.ProviderId,
            ProviderVersion: provider.ProviderVersion,
            ProviderStatus: provider.ProviderStatus,
            MatchedSkills: provider.SupportedSkills
                .Intersect(requiredSkills.Concat(optionalSkills), StringComparer.Ordinal)
                .OrderBy(skillId => skillId, StringComparer.Ordinal)
                .ToArray(),
            MatchedCapabilities: provider.SupportedCapabilities
                .Intersect(requiredCapabilities.Concat(optionalCapabilities), StringComparer.Ordinal)
                .OrderBy(capability => capability, StringComparer.Ordinal)
                .ToArray(),
            MatchedTargetProfiles: provider.SupportedTargetProfiles
                .Where(profile => string.Equals(profile, targetProfileId, StringComparison.Ordinal))
                .OrderBy(profile => profile, StringComparer.Ordinal)
                .ToArray());
    }

    private static IReadOnlyList<MicrosoftSkillProviderCandidate> SelectProviders(
        IReadOnlyList<MicrosoftSkillProviderCandidate> candidates,
        IReadOnlyCollection<string> requiredSkills,
        IReadOnlyCollection<string> requiredCapabilities)
    {
        var selected = new List<MicrosoftSkillProviderCandidate>();
        var remainingSkills = requiredSkills.ToHashSet(StringComparer.Ordinal);
        var remainingCapabilities = requiredCapabilities.ToHashSet(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            var contributesSkills = candidate.MatchedSkills.Any(skillId => remainingSkills.Contains(skillId));
            var contributesCapabilities = candidate.MatchedCapabilities.Any(capability => remainingCapabilities.Contains(capability));
            if (!contributesSkills && !contributesCapabilities)
            {
                continue;
            }

            selected.Add(candidate);
            foreach (var skillId in candidate.MatchedSkills)
            {
                remainingSkills.Remove(skillId);
            }

            foreach (var capability in candidate.MatchedCapabilities)
            {
                remainingCapabilities.Remove(capability);
            }

            if (remainingSkills.Count == 0 && remainingCapabilities.Count == 0)
            {
                break;
            }
        }

        return selected
            .OrderBy(candidate => candidate.ProviderId, StringComparer.Ordinal)
            .ToArray();
    }

    private static int GetStatusRank(MicrosoftSkillProviderStatus status)
    {
        return status switch
        {
            MicrosoftSkillProviderStatus.Available => 0,
            MicrosoftSkillProviderStatus.Planned => 1,
            MicrosoftSkillProviderStatus.Deprecated => 2,
            _ => 3,
        };
    }
}
