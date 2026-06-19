using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class RecommendationEngineBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.Discovery.Models";

    [Fact(DisplayName = "Recommendation Engine substrate types exist as backend-internal models")]
    public void RecommendationEngine_InternalTypesExist()
    {
        string[] expectedTypeNames =
        [
            "ConsultantDomainFramework",
            "ConsultantAudienceFit",
            "ConsultantDecisionCadence",
            "ConsultantWorkflowOrientation",
            "ConsultantConsumptionPattern",
            "ConsultantActionability",
            "ConsultantAdoptionLikelihood",
            "ConsultantMaintenanceComplexity",
            "ConsultantDecisionAssessment",
            "RecommendationBusinessValueLevel",
            "RecommendationComplexityLevel",
            "RecommendationPlacement",
            "DiscoveryRecommendation",
            "RecommendationSet",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Recommendation Engine models do not widen ScoreResult or PageScore public contracts")]
    public void RecommendationEngine_PublicContracts_DoNotExposeRecommendationTypes()
    {
        var publicResultPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var publicPagePropertyNames = typeof(PageScore)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("RecommendationSet", publicResultPropertyNames);
        Assert.DoesNotContain("DiscoveryRecommendation", publicResultPropertyNames);
        Assert.DoesNotContain("RecommendationSet", publicPagePropertyNames);
        Assert.DoesNotContain("DiscoveryRecommendation", publicPagePropertyNames);
    }
}
