using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35ABoundaryTests
{
    [Fact]
    public void Phase35A_HasNoExecutableProviderSurface()
    {
        var assembly = typeof(Phase35AProviderCatalog).Assembly;
        var types = assembly.GetTypes().Where(type => type.Namespace?.Contains("Phase35A", StringComparison.Ordinal) == true);

        Assert.DoesNotContain(types.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)),
            method => method.Name.Contains("Execute", StringComparison.OrdinalIgnoreCase) ||
                method.Name.Contains("Invoke", StringComparison.OrdinalIgnoreCase) ||
                method.Name.Contains("Spawn", StringComparison.OrdinalIgnoreCase));
    }
}

