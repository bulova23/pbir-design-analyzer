using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftSkillReadinessService
{
    internal MicrosoftSkillReadinessState Evaluate(
        MicrosoftSkillCompatibilityValidationResult validation,
        MicrosoftSkillResolutionResult resolution)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(resolution);

        if (validation.Diagnostics.UnsupportedTargetProfiles.Count > 0)
        {
            return MicrosoftSkillReadinessState.Unsupported;
        }

        if (validation.Diagnostics.MissingRequiredSections.Count > 0 ||
            validation.Diagnostics.MissingRequiredFields.Count > 0 ||
            validation.Diagnostics.DuplicateSkillIds.Count > 0 ||
            validation.Diagnostics.UnsupportedCapabilities.Count > 0 ||
            validation.Diagnostics.UnsatisfiedPrerequisites.Count > 0 ||
            validation.Diagnostics.VersionMismatches.Count > 0 ||
            validation.Diagnostics.IntegrityFailures.Count > 0)
        {
            return MicrosoftSkillReadinessState.PartiallySatisfied;
        }

        if (resolution.UnresolvedCapabilities.RequiredCapabilities.Count > 0)
        {
            return MicrosoftSkillReadinessState.PartiallySatisfied;
        }

        return MicrosoftSkillReadinessState.Satisfied;
    }

    internal MicrosoftSkillReadinessState PrepareForSkillProvider(
        MicrosoftSkillReadinessState readiness,
        MicrosoftSkillResolutionResult resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        return readiness == MicrosoftSkillReadinessState.Satisfied &&
            resolution.RequiredSkills.Count > 0 &&
            resolution.UnresolvedCapabilities.RequiredCapabilities.Count == 0
            ? MicrosoftSkillReadinessState.ReadyForSkillProvider
            : readiness;
    }
}
