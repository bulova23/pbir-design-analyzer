using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirGenerationSpecificationReadinessService
{
    internal PbirGenerationSpecificationReadinessState Evaluate(PbirGenerationSpecificationValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);

        if (validation.Diagnostics.MissingRequiredSections.Count > 0 ||
            validation.Diagnostics.MissingRequiredFields.Count > 0 ||
            validation.Diagnostics.UnsupportedSchemaVersions.Count > 0 ||
            validation.Diagnostics.BoundaryViolations.Count > 0)
        {
            return PbirGenerationSpecificationReadinessState.Incomplete;
        }

        if (validation.Diagnostics.MissingDesignIntent.Count > 0 ||
            validation.Diagnostics.InvalidPageDefinitions.Count > 0 ||
            validation.Diagnostics.InvalidVisualDefinitions.Count > 0 ||
            validation.Diagnostics.InvalidSemanticDefinitions.Count > 0 ||
            validation.Diagnostics.InvalidNavigationDefinitions.Count > 0 ||
            validation.Diagnostics.IncompleteSuccessCriteria.Count > 0)
        {
            return PbirGenerationSpecificationReadinessState.PartiallySpecified;
        }

        return PbirGenerationSpecificationReadinessState.Specified;
    }

    internal PbirGenerationSpecificationReadinessState PrepareForGenerationProvider(
        PbirGenerationSpecificationReadinessState readiness,
        bool hasArtifacts)
    {
        return readiness == PbirGenerationSpecificationReadinessState.Specified && hasArtifacts
            ? PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider
            : readiness;
    }
}
