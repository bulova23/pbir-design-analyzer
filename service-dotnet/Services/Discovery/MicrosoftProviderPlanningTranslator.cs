using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftProviderPlanningTranslator
{
    internal MicrosoftProviderPlanningTranslation Translate(
        MicrosoftAdapterSpecification specification,
        ProviderAdapterRequest adapterRequest,
        ExecutionPlan executionPlan)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(adapterRequest);
        ArgumentNullException.ThrowIfNull(executionPlan);

        var sourceCapabilities = (adapterRequest.CapabilityRequirements ?? executionPlan.ProviderPlanningMetadata.SupportedCapabilities ?? [])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var resolvedCapabilities = specification.CapabilityMappings
            .Where(mapping => mapping.ProviderCapabilityRequirements.All(requirement => sourceCapabilities.Contains(requirement, StringComparer.Ordinal)))
            .Select(mapping => mapping.CapabilityId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var targetMapping = specification.TargetProfileMappings
            .FirstOrDefault(mapping => string.Equals(mapping.TargetProfileId, adapterRequest.TargetArtifactProfile.ProfileId, StringComparison.Ordinal))
            ?? new MicrosoftAdapterTargetProfileMapping(
                TargetProfileId: adapterRequest.TargetArtifactProfile.ProfileId,
                RequiredCapabilities: [],
                OptionalCapabilities: [],
                UnsupportedCapabilities: [],
                PlanningRequirements: []);
        var missingCapabilities = targetMapping.RequiredCapabilities
            .Except(resolvedCapabilities, StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();

        return new MicrosoftProviderPlanningTranslation(
            TargetProfileId: adapterRequest.TargetArtifactProfile.ProfileId,
            SourceCapabilityRequirements: sourceCapabilities,
            ResolvedCapabilityRequirements: resolvedCapabilities,
            RequiredCapabilities: targetMapping.RequiredCapabilities,
            MissingCapabilities: missingCapabilities,
            PlanningRequirements: targetMapping.PlanningRequirements);
    }
}
