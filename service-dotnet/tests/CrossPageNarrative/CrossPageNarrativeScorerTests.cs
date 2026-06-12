using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.CrossPageNarrative;

public sealed class CrossPageNarrativeScorerTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ScorerTypeName = "PowerBIModelingService.Services.Pbir.CrossPageNarrative.CrossPageNarrativeScorer";

    [Fact(DisplayName = "Scorer applies the approved report-level dimension weighting")]
    public void Score_AppliesWeightedComposite()
    {
        var dimensions = new[]
        {
            CreateDimension("Flow", 80, "High"),
            CreateDimension("Consistency", 70, "High"),
            CreateDimension("Navigation", 60, "Medium"),
            CreateDimension("Continuity", 90, "High"),
            CreateDimension("Actionability", 50, "Medium"),
        };

        var summary = Score(dimensions, dominantNarrativeSummary: "Executive revenue review");

        Assert.Equal(71.5d, (double)(GetPropertyValue(summary, "CompositeScore") ?? 0d), precision: 1);
        Assert.Equal("Medium", GetStringProperty(summary, "Confidence"));
        Assert.Equal("Internal", GetStringProperty(summary, "PromotionState"));
        Assert.Equal("CrossSurfaceCandidate", GetStringProperty(summary, "SurfaceScope"));
    }

    private static object Score(IEnumerable<object> dimensions, string dominantNarrativeSummary)
    {
        var scorerType = CoreAssembly.GetType(ScorerTypeName, throwOnError: false);
        Assert.NotNull(scorerType);
        var dimensionType = RequireType("CrossPageNarrativeDimensionScore");

        var method = scorerType!.GetMethod("Score", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var typedDimensions = CreateTypedList(dimensionType, dimensions);
        var result = method!.Invoke(null, [typedDimensions, dominantNarrativeSummary]);
        Assert.NotNull(result);
        return result!;
    }

    private static object CreateDimension(string dimensionId, double score, string confidence)
    {
        var type = RequireType("CrossPageNarrativeDimensionScore");
        var dimensionEnum = RequireType("CrossPageNarrativeDimensionId");
        var confidenceEnum = RequireType("CrossPageNarrativeAssessmentConfidence");
        var dimension = Activator.CreateInstance(type);
        Assert.NotNull(dimension);

        SetProperty(dimension!, "DimensionId", Enum.Parse(dimensionEnum, dimensionId));
        SetProperty(dimension!, "Score", score);
        SetProperty(dimension!, "Confidence", Enum.Parse(confidenceEnum, confidence));
        SetProperty(dimension!, "StrongestEvidence", new[] { "test" });
        SetProperty(dimension!, "WeakeningEvidence", Array.Empty<string>());
        SetProperty(dimension!, "MissingEvidence", Array.Empty<string>());
        SetProperty(dimension!, "AffectedPageIds", Array.Empty<string>());
        return dimension!;
    }

    private static Type RequireType(string typeName)
    {
        var type = CoreAssembly.GetType($"PowerBIModelingService.Services.Pbir.Models.{typeName}", throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }

    private static object CreateTypedList(Type itemType, IEnumerable<object> items)
    {
        var listType = typeof(List<>).MakeGenericType(itemType);
        var list = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add");
        Assert.NotNull(addMethod);

        foreach (var item in items)
        {
            addMethod!.Invoke(list, [item]);
        }

        return list;
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        return property!.GetValue(target);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        return GetPropertyValue(target, propertyName)?.ToString() ?? string.Empty;
    }
}
