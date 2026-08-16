using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class ExperienceBlueprintBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.Discovery.Models";

    [Fact(DisplayName = "Experience Blueprint substrate types exist as backend-internal models")]
    public void ExperienceBlueprint_InternalTypesExist()
    {
        string[] expectedTypeNames =
        [
            "ExperienceBlueprint",
            "ExperienceBlueprintPage",
            "ExperienceBlueprintAnalyticalFlow",
            "ExperienceBlueprintNavigationIntent",
            "ExperienceBlueprintProvenance",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Experience Blueprint models do not widen ScoreResult or PageScore public contracts")]
    public void ExperienceBlueprint_PublicContracts_DoNotExposeBlueprintTypes()
    {
        var publicResultPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var publicPagePropertyNames = typeof(PageScore)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("ExperienceBlueprint", publicResultPropertyNames);
        Assert.DoesNotContain("ExperienceBlueprint", publicPagePropertyNames);
    }
}
