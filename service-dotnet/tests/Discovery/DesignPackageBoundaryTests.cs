using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class DesignPackageBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.Discovery.Models";

    [Fact(DisplayName = "Design Package substrate types exist as backend-internal models")]
    public void DesignPackage_InternalTypesExist()
    {
        string[] expectedTypeNames =
        [
            "DesignPackage",
            "DesignPackageDiscoveryContext",
            "DesignPackageAudience",
            "DesignPackagePersona",
            "DesignPackageExperienceDefinition",
            "DesignPackagePage",
            "DesignPackageKpi",
            "DesignPackageFilterSet",
            "DesignPackageVisualRecommendation",
            "DesignPackageNavigation",
            "DesignPackageAnalyticalFlow",
            "DesignPackageSuccessCriteria",
            "DesignPackageRecommendationRationale",
            "DesignPackageProvenance",
            "DesignPackageReference",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Design Package models do not widen ScoreResult or PageScore public contracts")]
    public void DesignPackage_PublicContracts_DoNotExposeDesignPackageTypes()
    {
        var publicResultPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var publicPagePropertyNames = typeof(PageScore)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("DesignPackage", publicResultPropertyNames);
        Assert.DoesNotContain("DesignPackage", publicPagePropertyNames);
    }
}
