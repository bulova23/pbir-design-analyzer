using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftSkillProviderReadinessService
{
    internal MicrosoftSkillProviderReadinessState Evaluate(
        MicrosoftSkillProviderCompatibilityValidationResult validation,
        SkillProviderSelection selection)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(selection);

        if (validation.Diagnostics.UnsupportedTargetProfiles.Count > 0)
        {
            return MicrosoftSkillProviderReadinessState.Unsupported;
        }

        if (validation.Diagnostics.MissingRequiredSections.Count > 0 ||
            validation.Diagnostics.MissingRequiredFields.Count > 0 ||
            validation.Diagnostics.DuplicateProviderIds.Count > 0 ||
            validation.Diagnostics.UnsupportedSkills.Count > 0 ||
            validation.Diagnostics.UnsupportedCapabilities.Count > 0 ||
            validation.Diagnostics.UnsatisfiedPrerequisites.Count > 0 ||
            validation.Diagnostics.VersionMismatches.Count > 0 ||
            validation.Diagnostics.IntegrityFailures.Count > 0)
        {
            return MicrosoftSkillProviderReadinessState.PartiallySatisfied;
        }

        if (selection.UnsupportedSkills.Count > 0 ||
            selection.CoverageSummary.UnresolvedRequiredCapabilities.Count > 0)
        {
            return MicrosoftSkillProviderReadinessState.PartiallySatisfied;
        }

        return MicrosoftSkillProviderReadinessState.Satisfied;
    }

    internal MicrosoftSkillProviderReadinessState PrepareForSkillProviderAdapter(
        MicrosoftSkillProviderReadinessState readiness,
        SkillProviderSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return readiness == MicrosoftSkillProviderReadinessState.Satisfied &&
            selection.RequiredSkills.Count > 0 &&
            selection.SelectedProviderCandidates.Count > 0 &&
            selection.UnsupportedSkills.Count == 0
            ? MicrosoftSkillProviderReadinessState.ReadyForSkillProviderAdapter
            : readiness;
    }
}
