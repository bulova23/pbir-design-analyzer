using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.DesignStudio;

public sealed class DesignStudioProviderBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string ProvidersNamespace = "PowerBIModelingService.Services.DesignStudio.Providers";

    [Fact(DisplayName = "Design Studio provider registry types exist and remain backend-internal")]
    public void ProviderRegistry_InternalTypesExist()
    {
        string[] expectedTypeNames =
        [
            "IDesignStudioProvider",
            "DesignProviderCapabilityKind",
            "DesignProviderCapability",
            "DesignProviderWorkflowConstraints",
            "DesignProviderFailureBehavior",
            "DesignProviderProvenanceRequirements",
            "DesignProviderTrustPosture",
        ];

        foreach (var typeName in expectedTypeNames)
        {
            var type = CoreAssembly.GetType($"{ProvidersNamespace}.{typeName}", throwOnError: false);
            Assert.NotNull(type);
            Assert.True(type!.IsNotPublic, $"{typeName} should remain backend-internal.");
        }
    }

    [Fact(DisplayName = "Design Studio provider models do not expose report mutation, PBIR generation, or materialization authority")]
    public void ProviderRegistry_DoesNotExposeMutationOrMaterializationAuthority()
    {
        string[] providerTypeNames =
        [
            "IDesignStudioProvider",
            "DesignProviderCapability",
            "DesignProviderWorkflowConstraints",
        ];

        var methods = providerTypeNames
            .Select(typeName => CoreAssembly.GetType($"{ProvidersNamespace}.{typeName}", throwOnError: false))
            .Where(type => type is not null)
            .SelectMany(type => type!.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(method => method.Name)
            .ToArray();

        Assert.DoesNotContain(methods, method => method.Contains("Materialize", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("GeneratePbir", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("CreateSurface", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Mutate", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Apply", StringComparison.Ordinal));
        Assert.DoesNotContain(methods, method => method.Contains("Deploy", StringComparison.Ordinal));
    }
}
