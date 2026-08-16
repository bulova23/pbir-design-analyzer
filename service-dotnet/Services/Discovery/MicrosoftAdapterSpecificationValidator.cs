using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftAdapterSpecificationValidator
{
    internal MicrosoftAdapterSpecificationValidationResult Validate(MicrosoftAdapterSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var missingSections = new List<string>();
        var missingFields = new List<string>();
        var unsupportedSchemaVersions = new List<string>();
        var reviewRequirementFailures = new List<string>();

        if (specification.SchemaMetadata is null)
        {
            missingSections.Add("schemaMetadata");
        }
        else
        {
            ValidateNotBlank(specification.SchemaMetadata.SchemaVersion, "schemaMetadata.schemaVersion", missingFields);
            ValidateNotBlank(specification.SchemaMetadata.SpecificationId, "schemaMetadata.specificationId", missingFields);
            ValidateNotBlank(specification.SchemaMetadata.SpecificationVersion, "schemaMetadata.specificationVersion", missingFields);

            if (!string.Equals(specification.SchemaMetadata.SchemaVersion, MicrosoftAdapterSpecificationContract.SchemaVersionV1, StringComparison.Ordinal))
            {
                unsupportedSchemaVersions.Add(specification.SchemaMetadata.SchemaVersion);
            }
        }

        if (specification.ProviderIdentity is null)
        {
            missingSections.Add("providerIdentity");
        }
        else
        {
            ValidateNotBlank(specification.ProviderIdentity.ProviderId, "providerIdentity.providerId", missingFields);
            ValidateNotBlank(specification.ProviderIdentity.ProviderCategory, "providerIdentity.providerCategory", missingFields);
            ValidateNotBlank(specification.ProviderIdentity.ProviderDisplayName, "providerIdentity.providerDisplayName", missingFields);
        }

        if (specification.SupportedTargetProfiles is null || specification.SupportedTargetProfiles.Count == 0)
        {
            missingSections.Add("supportedTargetProfiles");
        }

        if (specification.CapabilityMappings is null || specification.CapabilityMappings.Count == 0)
        {
            missingSections.Add("capabilityMappings");
        }

        if (specification.TargetProfileMappings is null || specification.TargetProfileMappings.Count == 0)
        {
            missingSections.Add("targetProfileMappings");
        }

        if (specification.CompatibilityCatalog is null)
        {
            missingSections.Add("compatibilityCatalog");
        }

        if (specification.ConstraintCatalog is null)
        {
            missingSections.Add("constraintCatalog");
        }

        if (specification.ReviewRequirementsCatalog is null)
        {
            missingSections.Add("reviewRequirementsCatalog");
        }
        else
        {
            if (!specification.ReviewRequirementsCatalog.DesignApprovalRequired)
            {
                reviewRequirementFailures.Add("reviewRequirementsCatalog.designApprovalRequired must stay true.");
            }

            if (!specification.ReviewRequirementsCatalog.GenerationApprovalRequired)
            {
                reviewRequirementFailures.Add("reviewRequirementsCatalog.generationApprovalRequired must stay true.");
            }

            if (!specification.ReviewRequirementsCatalog.AnalyzerValidationRequired)
            {
                reviewRequirementFailures.Add("reviewRequirementsCatalog.analyzerValidationRequired must stay true.");
            }
        }

        return new MicrosoftAdapterSpecificationValidationResult(new MicrosoftAdapterSpecificationDiagnostics(
            MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
            MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedSchemaVersions: unsupportedSchemaVersions.Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedTargetProfiles: [],
            UnsupportedCapabilityRequirements: [],
            FutureTargetProfiles: [],
            FutureCapabilityRequirements: [],
            ConstraintFailures: [],
            ReviewRequirementFailures: reviewRequirementFailures.Distinct(StringComparer.Ordinal).ToArray()));
    }

    private static void ValidateNotBlank(string? value, string fieldName, ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldName);
        }
    }
}
