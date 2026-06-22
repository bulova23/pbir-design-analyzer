using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ExecutionProviderValidator
{
    internal ExecutionProviderValidationResult ValidateProviderDefinition(ExecutionProviderDefinition providerDefinition)
    {
        ArgumentNullException.ThrowIfNull(providerDefinition);

        var missingSections = new List<string>();
        var missingFields = new List<string>();

        ValidateNotBlank(providerDefinition.ProviderId, "providerDefinition.providerId", missingFields);
        ValidateNotBlank(providerDefinition.ProviderName, "providerDefinition.providerName", missingFields);
        ValidateNotBlank(providerDefinition.ProviderVersion, "providerDefinition.providerVersion", missingFields);
        ValidateNotBlank(providerDefinition.ProviderCategory, "providerDefinition.providerCategory", missingFields);
        ValidateNotEmpty(providerDefinition.SupportedCapabilities, "providerDefinition.supportedCapabilities", missingSections);
        ValidateNotEmpty(providerDefinition.SupportedTargetProfiles, "providerDefinition.supportedTargetProfiles", missingSections);
        ValidateNotEmpty(providerDefinition.SupportedExecutionModes, "providerDefinition.supportedExecutionModes", missingSections);
        ValidateNotEmpty(providerDefinition.SupportedGenerationRequestSchemaVersions, "providerDefinition.supportedGenerationRequestSchemaVersions", missingSections);
        ValidateNotEmpty(providerDefinition.SupportedExecutionPlanSchemaVersions, "providerDefinition.supportedExecutionPlanSchemaVersions", missingSections);
        ValidateNotEmpty(providerDefinition.SupportedCapabilityNegotiationSchemaVersions, "providerDefinition.supportedCapabilityNegotiationSchemaVersions", missingSections);

        return new ExecutionProviderValidationResult(
            new ExecutionProviderDiagnostics(
                MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
                MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
                InvalidLineage: [],
                InvalidApprovalChains: [],
                UnsupportedProviderDefinitions: [],
                IncompatibleExecutionModes: [],
                VersionMismatches: [],
                CapabilityRequirementFailures: [],
                ReadinessRequirementFailures: [],
                ApprovalRequirementFailures: []));
    }

    private static void ValidateNotBlank(string? value, string fieldName, ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldName);
        }
    }

    private static void ValidateNotEmpty<T>(IReadOnlyCollection<T>? values, string sectionName, ICollection<string> missingSections)
    {
        if (values is null || values.Count == 0)
        {
            missingSections.Add(sectionName);
        }
    }
}
