using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class OpportunityCatalogBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.Discovery.Models";

    [Fact(DisplayName = "Opportunity Catalog substrate types exist as backend-internal models")]
    public void OpportunityCatalog_InternalTypesExist()
    {
        string[] expectedTypeNames =
        [
            "OpportunityCategory",
            "OpportunityExperienceType",
            "OpportunitySemanticSignal",
            "OpportunityCandidate",
            "OpportunityCatalog",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Opportunity Catalog models do not widen ScoreResult or PageScore public contracts")]
    public void OpportunityCatalog_PublicContracts_DoNotExposeOpportunityTypes()
    {
        var publicResultPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var publicPagePropertyNames = typeof(PageScore)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("OpportunityCatalog", publicResultPropertyNames);
        Assert.DoesNotContain("OpportunityCandidate", publicResultPropertyNames);
        Assert.DoesNotContain("OpportunityCatalog", publicPagePropertyNames);
        Assert.DoesNotContain("OpportunityCandidate", publicPagePropertyNames);
    }
}
