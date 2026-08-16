using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35ARequestProjector
{
    internal Phase35ARequest Project(GenerationProviderFrameworkState authoritativeState)
    {
        ArgumentNullException.ThrowIfNull(authoritativeState);
        var request = authoritativeState.Request ?? throw new Phase35AContractException("Authoritative generation provider request is required.");
        var profile = authoritativeState.Provider ?? throw new Phase35AContractException("Authoritative provider profile is required.");
        var canonical = new Phase35ACanonicalJson();
        return new(
            Phase35AContracts.RequestV1,
            request.Metadata.RequestId,
            $"intent:{request.References.PlanningOutcomeReference.OutcomeId}",
            [request.References.PlanningOutcomeReference.OutcomeId, request.References.PbirSpecificationReference.SpecificationId],
            request.Requirements.CapabilityRequirements.RequiredCapabilities.Select(ParseCapability).ToArray(),
            ParseArtifact(request.Requirements.CapabilityRequirements.ArtifactType),
            profile.ProviderId,
            canonical.Hash(request),
            canonical.Hash(Phase35AExecutionPolicy.Denied));
    }

    private static Phase35ACapability ParseCapability(string value) => value switch
    {
        "pageGeneration" or "visualGeneration" or "semanticGeneration" or "navigationGeneration" or "successCriteriaPreservation" => Phase35ACapability.PbirGeneration,
        _ => throw new Phase35AContractException($"Unsupported authoritative capability: {value}.")
    };

    private static Phase35AArtifactKind ParseArtifact(GenerationProviderArtifactType value) => value switch
    {
        GenerationProviderArtifactType.PbirReport => Phase35AArtifactKind.PbirReport,
        _ => throw new Phase35AContractException($"Unsupported authoritative artifact: {value}.")
    };
}
