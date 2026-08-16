using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationProviderValidator
{
    internal GenerationProviderValidationResult Validate(
        PbirGenerationSpecificationState specificationState,
        GenerationProviderRequest request,
        GenerationProviderDefinition provider)
    {
        ArgumentNullException.ThrowIfNull(specificationState);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(provider);

        var missingRequiredSections = new List<string>();
        var missingRequiredFields = new List<string>();
        var unsupportedSchemaVersions = new List<string>();
        var unsupportedArtifactTypes = new List<string>();
        var unsupportedTargetProfiles = new List<string>();
        var unsupportedGenerationModes = new List<string>();
        var providerCompatibilityFailures = new List<string>();
        var specificationCompletenessFailures = new List<string>();
        var boundaryViolations = new List<string>();

        ValidateSchemaVersion(request.SchemaVersion, GenerationProviderRequestContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(provider.SchemaVersion, GenerationProviderDefinitionContract.SchemaVersionV1, unsupportedSchemaVersions);

        ValidateNotBlank(request.Metadata.RequestId, "metadata.requestId", missingRequiredFields);
        ValidateNotBlank(request.References.PlanningOutcomeReference.OutcomeId, "references.planningOutcomeReference.outcomeId", missingRequiredFields);
        ValidateNotBlank(request.References.ExecutionCandidateReference.CandidateId, "references.executionCandidateReference.candidateId", missingRequiredFields);
        ValidateNotBlank(request.References.PbirSpecificationReference.SpecificationId, "references.pbirSpecificationReference.specificationId", missingRequiredFields);
        ValidateNotBlank(request.Requirements.CapabilityRequirements.TargetProfileId, "requirements.capabilityRequirements.targetProfileId", missingRequiredFields);

        if (request.Requirements.CapabilityRequirements.RequiredCapabilities.Count == 0)
        {
            missingRequiredSections.Add("requirements.capabilityRequirements");
        }

        if (specificationState.Specification is null)
        {
            specificationCompletenessFailures.Add("pbirGenerationSpecification.specification");
        }
        else
        {
            var specificationValidation = new PbirGenerationSpecificationValidator().Validate(specificationState.Specification);
            specificationCompletenessFailures.AddRange(specificationValidation.Diagnostics.MissingRequiredSections);
            specificationCompletenessFailures.AddRange(specificationValidation.Diagnostics.MissingRequiredFields);
            specificationCompletenessFailures.AddRange(specificationValidation.Diagnostics.MissingDesignIntent);
            specificationCompletenessFailures.AddRange(specificationValidation.Diagnostics.InvalidPageDefinitions);
            specificationCompletenessFailures.AddRange(specificationValidation.Diagnostics.InvalidVisualDefinitions);
            specificationCompletenessFailures.AddRange(specificationValidation.Diagnostics.InvalidSemanticDefinitions);
            specificationCompletenessFailures.AddRange(specificationValidation.Diagnostics.InvalidNavigationDefinitions);
            specificationCompletenessFailures.AddRange(specificationValidation.Diagnostics.IncompleteSuccessCriteria);
            unsupportedSchemaVersions.AddRange(specificationValidation.Diagnostics.UnsupportedSchemaVersions);
            boundaryViolations.AddRange(specificationValidation.Diagnostics.BoundaryViolations);
        }

        if (!provider.SupportedArtifactTypes.Contains(request.Requirements.CapabilityRequirements.ArtifactType))
        {
            unsupportedArtifactTypes.Add(request.Requirements.CapabilityRequirements.ArtifactType.ToString());
        }

        if (!provider.SupportedTargetProfiles.Contains(request.Requirements.CapabilityRequirements.TargetProfileId, StringComparer.Ordinal))
        {
            unsupportedTargetProfiles.Add(request.Requirements.CapabilityRequirements.TargetProfileId);
        }

        foreach (var mode in request.Requirements.ProviderRequirements.RequiredGenerationModes)
        {
            if (!provider.SupportedGenerationModes.Contains(mode))
            {
                unsupportedGenerationModes.Add(mode.ToString());
            }
        }

        foreach (var capability in request.Requirements.CapabilityRequirements.RequiredCapabilities)
        {
            if (!provider.SupportedCapabilities.Contains(capability, StringComparer.Ordinal))
            {
                providerCompatibilityFailures.Add($"provider.capability missing:{capability}");
            }
        }

        if (!request.Requirements.ProviderRequirements.AllowedStatuses.Contains(provider.Status))
        {
            providerCompatibilityFailures.Add($"provider.status must be one of: {string.Join(", ", request.Requirements.ProviderRequirements.AllowedStatuses)}.");
        }

        if (provider.Status == GenerationProviderStatus.Unsupported)
        {
            providerCompatibilityFailures.Add("provider.status must not be unsupported.");
        }

        if (request.Requirements.Constraints.AllowApiInvocation ||
            request.Requirements.Constraints.AllowCliInvocation ||
            request.Requirements.Constraints.AllowDeployment ||
            request.Requirements.Constraints.AllowReportMutation)
        {
            boundaryViolations.Add("generationProviderRequest.constraints must remain metadata-only in Phase 16.");
        }

        return new GenerationProviderValidationResult(
            new GenerationProviderValidationDiagnostics(
                MissingRequiredSections: missingRequiredSections.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                MissingRequiredFields: missingRequiredFields.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                UnsupportedSchemaVersions: unsupportedSchemaVersions.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                UnsupportedArtifactTypes: unsupportedArtifactTypes.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                UnsupportedTargetProfiles: unsupportedTargetProfiles.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                UnsupportedGenerationModes: unsupportedGenerationModes.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                ProviderCompatibilityFailures: providerCompatibilityFailures.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                SpecificationCompletenessFailures: specificationCompletenessFailures.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                BoundaryViolations: boundaryViolations.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray()));
    }

    private static void ValidateSchemaVersion(string actual, string expected, ICollection<string> unsupportedSchemaVersions)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            unsupportedSchemaVersions.Add(actual);
        }
    }

    private static void ValidateNotBlank(string value, string fieldPath, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add(fieldPath);
        }
    }
}
