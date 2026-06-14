using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.DesignStudio;

public sealed class DesignStudioMaterializationTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string MaterializationNamespace = "PowerBIModelingService.Services.DesignStudio.Materialization";

    [Fact(DisplayName = "Design Studio materialization gateway types exist and remain backend-internal")]
    public void MaterializationGateway_InternalTypesExist()
    {
        string[] expectedTypeNames =
        [
            "MaterializationMode",
            "MaterializationProvenanceEntry",
            "MaterializationSnapshotReference",
            "MaterializationHandoffContext",
            "MaterializationHandoffEligibility",
            "MaterializationAnalyzerHandoffReference",
            "MaterializationAnalyzerHandoffMetadata",
            "MaterializationAnalyzerHandoffContract",
            "MaterializationSideEffectState",
            "MaterializationGatewayOutcome",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{MaterializationNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Design Studio materialization models do not expose PBIR creation, analyzer execution, or report mutation authority")]
    public void MaterializationGateway_DoesNotExposeExecutionAuthority()
    {
        string[] materializationTypeNames =
        [
            "MaterializationAnalyzerHandoffContract",
            "MaterializationSideEffectState",
            "MaterializationGatewayOutcome",
        ];

        var methods = materializationTypeNames
            .Select(typeName => CoreAssembly.GetType($"{MaterializationNamespace}.{typeName}", throwOnError: false))
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
