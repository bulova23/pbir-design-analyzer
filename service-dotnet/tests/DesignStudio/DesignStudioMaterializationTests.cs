using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.DesignStudio;

public sealed class DesignStudioMaterializationTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string LegacyMaterializationNamespace = "PowerBIModelingService.Services.DesignStudio.Materialization";
    private const string ModelsNamespace = "PowerBIModelingService.Services.DesignStudio.Models";

    [Fact(DisplayName = "Design Studio no longer keeps a duplicate materialization gateway namespace beside the contract mirror")]
    public void MaterializationGateway_LegacyNamespaceIsRemoved()
    {
        Assert.DoesNotContain(
            CoreAssembly.GetTypes(),
            type => string.Equals(type.Namespace, LegacyMaterializationNamespace, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Design Studio materialization contracts remain passive backend mirrors without execution authority")]
    public void MaterializationContracts_DoNotExposeExecutionAuthority()
    {
        string[] materializationTypeNames =
        [
            "MaterializationRequest",
            "MaterializedSurfaceCandidate",
            "MaterializationAnalyzerHandoffContract",
            "IterationGuardrails",
        ];

        var methods = materializationTypeNames
            .Select(typeName => CoreAssembly.GetType($"{ModelsNamespace}.{typeName}", throwOnError: false))
            .Where(type => type is not null)
            .SelectMany(type => type!.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(method => method.Name)
            .ToArray();

        Assert.DoesNotContain(methods, method => method.Contains("GeneratePbir", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("ExecuteAnalyzer", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("OpenHandoff", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Apply", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Mutate", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Deploy", StringComparison.Ordinal));
    }
}
