using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.DesignStudio;

public sealed class DesignStudioProviderBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string DesignStudioNamespacePrefix = "PowerBIModelingService.Services.DesignStudio";
    private const string ModelsNamespace = "PowerBIModelingService.Services.DesignStudio.Models";

    [Fact(DisplayName = "Design Studio backend no longer exposes speculative provider registry runtime types")]
    public void ProviderRegistry_SpeculativeRuntimeTypesAreRemoved()
    {
        string[] speculativeTypeNames =
        [
            "IDesignStudioProvider",
            "DesignProviderCapability",
            "DesignProviderWorkflowConstraints",
            "DesignProviderFailureBehavior",
            "DesignProviderProvenanceRequirements",
            "DesignProviderTrustPosture",
        ];

        foreach (var typeName in speculativeTypeNames)
        {
            var matchingType = CoreAssembly
                .GetTypes()
                .SingleOrDefault(type => type.Name == typeName && type.Namespace?.StartsWith(DesignStudioNamespacePrefix, StringComparison.Ordinal) == true);

            Assert.Null(matchingType);
        }
    }

    [Fact(DisplayName = "Design Studio keeps only provider provenance vocabulary inside the contract mirror")]
    public void ProviderRegistry_OnlyContractMirrorVocabularyRemains()
    {
        var capabilityKindType = CoreAssembly.GetType($"{ModelsNamespace}.DesignProviderCapabilityKind", throwOnError: false);
        Assert.NotNull(capabilityKindType);
        Assert.True(capabilityKindType!.IsNotPublic, "DesignProviderCapabilityKind should remain backend-internal.");

        var provenanceType = CoreAssembly.GetType($"{ModelsNamespace}.DesignArtifactProvenance", throwOnError: false);
        Assert.NotNull(provenanceType);

        var providerCapabilityProperty = provenanceType!
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(property => property.Name == "ProviderCapabilityKind");

        Assert.Equal("Nullable`1", providerCapabilityProperty.PropertyType.Name);
        Assert.Equal("DesignProviderCapabilityKind", Nullable.GetUnderlyingType(providerCapabilityProperty.PropertyType)?.Name);
    }
}
