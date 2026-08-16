using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class DiscoveryProfileBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.Discovery.Models";

    [Fact(DisplayName = "Discovery Profile substrate types exist as backend-internal models")]
    public void DiscoveryProfile_InternalTypesExist()
    {
        string[] expectedTypeNames =
        [
            "DiscoveryConfidenceLevel",
            "DiscoveryDateIntelligenceReadiness",
            "DiscoveryMeasureProfile",
            "DiscoveryDimensionProfile",
            "DiscoveryHierarchyProfile",
            "DiscoveryDateIntelligenceProfile",
            "DiscoveryRelationshipProfile",
            "DiscoveryDomainSignal",
            "DiscoveryKpiCluster",
            "DiscoveryAudienceSignal",
            "DiscoveryProfile",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Discovery Profile models do not widen ScoreResult or PageScore public contracts")]
    public void DiscoveryProfile_PublicContracts_DoNotExposeDiscoveryTypes()
    {
        var publicResultPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var publicPagePropertyNames = typeof(PageScore)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("DiscoveryProfile", publicResultPropertyNames);
        Assert.DoesNotContain("SemanticModelDiscovery", publicResultPropertyNames);
        Assert.DoesNotContain("DiscoveryProfile", publicPagePropertyNames);
        Assert.DoesNotContain("SemanticModelDiscovery", publicPagePropertyNames);
    }
}
