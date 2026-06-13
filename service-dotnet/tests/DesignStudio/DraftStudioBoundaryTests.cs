using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.DesignStudio;

public sealed class DraftStudioBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ModelsNamespace = "PowerBIModelingService.Services.DesignStudio.Models";

    [Fact(DisplayName = "Draft Studio internal artifact types exist and remain backend-internal")]
    public void DraftStudio_InternalDraftTypesExist()
    {
        string[] expectedTypeNames =
        [
            "DraftReportArtifact",
            "DraftPageArtifact",
            "DraftLayoutArtifact",
            "DraftNavigationArtifact",
            "DraftNavigationSectionArtifact",
            "DraftArtifactStatus",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Draft Studio models do not introduce analyzable surfaces, materialization authority, or report mutation methods")]
    public void DraftStudio_Models_DoNotExposeMaterializationOrMutationAuthority()
    {
        string[] draftTypeNames =
        [
            "DraftReportArtifact",
            "DraftPageArtifact",
            "DraftLayoutArtifact",
            "DraftNavigationArtifact",
            "DraftNavigationSectionArtifact",
            "DraftArtifactStatus",
        ];

        var methods = draftTypeNames
            .Select(typeName => CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false))
            .Where(type => type is not null)
            .SelectMany(type => type!.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(method => method.Name)
            .ToArray();

        Assert.DoesNotContain(methods, method => method.Contains("Materialize", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("CreateSurface", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("GeneratePbir", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Deploy", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Apply", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Mutate", StringComparison.Ordinal));
    }
}
