using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.CrossPageNarrative;

public sealed class CrossPageNarrativeModelBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.Pbir.Models";

    [Fact(DisplayName = "Cross-Page Narrative substrate types exist as backend-internal models")]
    public void CrossPageNarrative_InternalTypesExist()
    {
        string[] expectedTypeNames =
        [
            "CrossPageNarrativeRoleId",
            "CrossPageNarrativeRoleConfidence",
            "CrossPageNarrativeRoleAssignment",
            "CrossPageNarrativeEdgeType",
            "CrossPageNarrativeEdgeObservationKind",
            "CrossPageNarrativeEdge",
            "CrossPageNarrativeGraph",
            "CrossPageNarrativeAssessmentConfidence",
            "CrossPageNarrativeDimensionId",
            "CrossPageNarrativeDimensionScore",
            "CrossPageNarrativeScoreSummary",
            "CrossPageNarrativeGapId",
            "CrossPageNarrativeGap",
            "CrossPageNarrativeOrphanState",
            "CrossPageNarrativePageAssessment",
            "CrossPageNarrativeAssessment",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Cross-Page Narrative assessment stays internal on ScoreResult and does not leak on public contracts")]
    public void CrossPageNarrative_PublicContracts_DoNotExposeAssessment()
    {
        var publicResultPropertyNames = typeof(ScoreResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var publicPagePropertyNames = typeof(PageScore)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("InternalCrossPageNarrativeAssessment", publicResultPropertyNames);
        Assert.DoesNotContain("CrossPageNarrativeAssessment", publicResultPropertyNames);
        Assert.DoesNotContain("CrossPageNarrativeRole", publicPagePropertyNames);
        Assert.DoesNotContain("CrossPageNarrativeAssessment", publicPagePropertyNames);

        var internalProperty = typeof(ScoreResult).GetProperty(
            "InternalCrossPageNarrativeAssessment",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(internalProperty);
        Assert.Equal("CrossPageNarrativeAssessment", internalProperty!.PropertyType.Name);
    }

    [Fact(DisplayName = "Cross-Page Narrative score summary reuses existing internal promotion and surface-scope vocabularies")]
    public void CrossPageNarrativeScoreSummary_ReusesCanonicalPromotionAndSurfaceScope()
    {
        var scoreSummaryType = RequireType("CrossPageNarrativeScoreSummary");
        var properties = scoreSummaryType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType.Name);

        Assert.Equal("StoryAssessmentPromotionState", properties["PromotionState"]);
        Assert.Equal("StoryAssessmentSurfaceScope", properties["SurfaceScope"]);
    }

    private static Type RequireType(string typeName)
    {
        var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }
}
