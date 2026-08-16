using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class CapabilityNegotiationValidator
{
    internal CapabilityNegotiationValidationResult Validate(
        GenerationRequest generationRequest,
        ExecutionPlan executionPlan,
        ProviderAdapterRequest adapterRequest,
        ProviderAdapterDefinition adapterDefinition,
        MicrosoftAdapterSpecification specification,
        CapabilityNegotiationSubstitutionCatalog substitutionCatalog)
    {
        ArgumentNullException.ThrowIfNull(generationRequest);
        ArgumentNullException.ThrowIfNull(executionPlan);
        ArgumentNullException.ThrowIfNull(adapterRequest);
        ArgumentNullException.ThrowIfNull(adapterDefinition);
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(substitutionCatalog);

        var missingSections = new List<string>();
        var missingFields = new List<string>();
        var missingCapabilityDefinitions = new List<string>();
        var invalidSubstitutions = new List<string>();
        var circularSubstitutions = new List<string>();
        var unsupportedRequiredCapabilities = new List<string>();
        var versionMismatches = new List<string>();
        var compatibilityFailures = new List<string>();

        ValidateNotBlank(generationRequest.SchemaVersion, "generationRequest.schemaVersion", missingFields);
        ValidateNotBlank(executionPlan.SchemaVersion, "executionPlan.schemaVersion", missingFields);
        ValidateNotBlank(adapterRequest.SchemaVersion, "adapterRequest.schemaVersion", missingFields);
        ValidateNotBlank(specification.SchemaMetadata?.SchemaVersion, "specification.schemaMetadata.schemaVersion", missingFields);
        ValidateNotBlank(substitutionCatalog.SchemaVersion, "substitutionCatalog.schemaVersion", missingFields);

        if (generationRequest.TargetArtifactProfile is null)
        {
            missingSections.Add("generationRequest.targetArtifactProfile");
        }

        if (executionPlan.TargetDefinition is null)
        {
            missingSections.Add("executionPlan.targetDefinition");
        }

        if (adapterRequest.TargetArtifactProfile is null)
        {
            missingSections.Add("adapterRequest.targetArtifactProfile");
        }

        if (specification.CapabilityMappings is null)
        {
            missingSections.Add("specification.capabilityMappings");
        }

        if (specification.TargetProfileMappings is null)
        {
            missingSections.Add("specification.targetProfileMappings");
        }

        if (!string.Equals(generationRequest.SchemaVersion, GenerationRequestContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            versionMismatches.Add(generationRequest.SchemaVersion);
        }

        if (!string.Equals(executionPlan.SchemaVersion, ExecutionPlanContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            versionMismatches.Add(executionPlan.SchemaVersion);
        }

        if (!string.Equals(adapterRequest.SchemaVersion, ProviderAdapterContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            versionMismatches.Add(adapterRequest.SchemaVersion);
        }

        if (!string.Equals(specification.SchemaMetadata.SchemaVersion, MicrosoftAdapterSpecificationContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            versionMismatches.Add(specification.SchemaMetadata.SchemaVersion);
        }

        if (!string.Equals(substitutionCatalog.SchemaVersion, CapabilityNegotiationContract.SubstitutionCatalogSchemaVersionV1, StringComparison.Ordinal))
        {
            versionMismatches.Add(substitutionCatalog.SchemaVersion);
        }

        if (!adapterDefinition.SupportedGenerationRequestSchemaVersions.Contains(generationRequest.SchemaVersion, StringComparer.Ordinal))
        {
            versionMismatches.Add(generationRequest.SchemaVersion);
        }

        if (!adapterDefinition.SupportedExecutionPlanSchemaVersions.Contains(executionPlan.SchemaVersion, StringComparer.Ordinal))
        {
            versionMismatches.Add(executionPlan.SchemaVersion);
        }

        if (!string.Equals(adapterRequest.GenerationRequestRef, generationRequest.RequestId, StringComparison.Ordinal))
        {
            compatibilityFailures.Add("adapterRequest.generationRequestRef must match generationRequest.requestId.");
        }

        if (!string.Equals(adapterRequest.ExecutionPlanRef, executionPlan.ExecutionPlanId, StringComparison.Ordinal))
        {
            compatibilityFailures.Add("adapterRequest.executionPlanRef must match executionPlan.executionPlanId.");
        }

        if (!Equals(generationRequest.TargetArtifactProfile, executionPlan.TargetDefinition?.TargetArtifactProfile) ||
            !Equals(generationRequest.TargetArtifactProfile, adapterRequest.TargetArtifactProfile))
        {
            compatibilityFailures.Add("generationRequest, executionPlan, and adapterRequest target profiles must match.");
        }

        var targetMapping = specification.TargetProfileMappings?
            .FirstOrDefault(mapping => string.Equals(mapping.TargetProfileId, generationRequest.TargetArtifactProfile.ProfileId, StringComparison.Ordinal));
        var capabilityIds = (specification.CapabilityMappings ?? [])
            .Select(mapping => mapping.CapabilityId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var capability in targetMapping?.RequiredCapabilities ?? [])
        {
            if (!capabilityIds.Contains(capability))
            {
                missingCapabilityDefinitions.Add(capability);
            }
        }

        foreach (var capability in targetMapping?.OptionalCapabilities ?? [])
        {
            if (!capabilityIds.Contains(capability))
            {
                missingCapabilityDefinitions.Add(capability);
            }
        }

        foreach (var capability in targetMapping?.UnsupportedCapabilities ?? [])
        {
            if (!capabilityIds.Contains(capability))
            {
                missingCapabilityDefinitions.Add(capability);
            }
        }

        foreach (var rule in substitutionCatalog.Rules ?? [])
        {
            ValidateNotBlank(rule.RuleId, "substitutionCatalog.rules.ruleId", missingFields);
            ValidateNotBlank(rule.OriginalCapabilityId, "substitutionCatalog.rules.originalCapabilityId", missingFields);
            ValidateNotBlank(rule.SubstituteCapabilityId, "substitutionCatalog.rules.substituteCapabilityId", missingFields);
            ValidateNotBlank(rule.AppliesToTargetProfileId, "substitutionCatalog.rules.appliesToTargetProfileId", missingFields);
            ValidateNotBlank(rule.SubstitutionReason, "substitutionCatalog.rules.substitutionReason", missingFields);

            if (!capabilityIds.Contains(rule.OriginalCapabilityId))
            {
                invalidSubstitutions.Add(rule.OriginalCapabilityId);
            }

            if (!capabilityIds.Contains(rule.SubstituteCapabilityId))
            {
                invalidSubstitutions.Add(rule.SubstituteCapabilityId);
            }
        }

        circularSubstitutions.AddRange(DetectCircularSubstitutions(substitutionCatalog.Rules ?? []));

        return new CapabilityNegotiationValidationResult(
            new CapabilityNegotiationDiagnostics(
                MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
                MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
                MissingCapabilityDefinitions: missingCapabilityDefinitions.Distinct(StringComparer.Ordinal).ToArray(),
                InvalidSubstitutions: invalidSubstitutions.Distinct(StringComparer.Ordinal).ToArray(),
                CircularSubstitutions: circularSubstitutions.Distinct(StringComparer.Ordinal).ToArray(),
                UnsupportedRequiredCapabilities: unsupportedRequiredCapabilities.Distinct(StringComparer.Ordinal).ToArray(),
                VersionMismatches: versionMismatches.Distinct(StringComparer.Ordinal).ToArray(),
                CompatibilityFailures: compatibilityFailures.Distinct(StringComparer.Ordinal).ToArray()));
    }

    private static IReadOnlyList<string> DetectCircularSubstitutions(IReadOnlyList<CapabilityNegotiationSubstitutionRule> rules)
    {
        var circular = new List<string>();
        var ruleLookup = rules
            .GroupBy(rule => rule.OriginalCapabilityId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var rule in rules)
        {
            var path = new List<string> { rule.OriginalCapabilityId };
            var visited = new HashSet<string>(StringComparer.Ordinal) { rule.OriginalCapabilityId };
            var current = rule.SubstituteCapabilityId;

            while (ruleLookup.TryGetValue(current, out var nextRule))
            {
                path.Add(current);
                if (!visited.Add(current))
                {
                    break;
                }

                current = nextRule.SubstituteCapabilityId;
                if (string.Equals(current, rule.OriginalCapabilityId, StringComparison.Ordinal))
                {
                    path.Add(current);
                    circular.Add(string.Join(" -> ", path));
                    break;
                }
            }
        }

        return circular;
    }

    private static void ValidateNotBlank(string? value, string fieldName, ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldName);
        }
    }
}
