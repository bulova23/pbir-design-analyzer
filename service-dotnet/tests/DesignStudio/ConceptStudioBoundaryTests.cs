using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.DesignStudio;

public sealed class ConceptStudioBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.DesignStudio.Models";

    [Fact(DisplayName = "Concept Studio internal concept artifact types exist and remain backend-internal")]
    public void ConceptStudio_InternalConceptTypesExist()
    {
        string[] expectedTypeNames =
        [
            "ReportChapterMapConcept",
            "ChapterConcept",
            "PageRecommendationConcept",
            "PageConcept",
            "AnalyticalFlowConcept",
            "AnalyticalFlowStepConcept",
            "AlternateConceptComparison",
            "AlternateConceptDecision",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Concept Studio internal concept models do not introduce analyzable surfaces or materialization authority")]
    public void ConceptStudio_Models_DoNotExposeAnalyzerSurfaceOrMaterializationAuthority()
    {
        string[] conceptTypeNames =
        [
            "ReportChapterMapConcept",
            "ChapterConcept",
            "PageRecommendationConcept",
            "PageConcept",
            "AnalyticalFlowConcept",
            "AnalyticalFlowStepConcept",
            "AlternateConceptComparison",
            "AlternateConceptDecision",
            "AlternateReportConcept",
        ];

        var methods = conceptTypeNames
            .Select(typeName => CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false))
            .Where(type => type is not null)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(method => method.Name)
            .ToArray();

        Assert.DoesNotContain(methods, method => method.Contains("Materialize", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("CreateSurface", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("GeneratePbir", StringComparison.Ordinal));
    }
}
