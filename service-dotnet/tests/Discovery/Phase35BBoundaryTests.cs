using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35BBoundaryTests
{
    [Fact]
    public void Phase35BAssembly_DoesNotReferenceForbiddenExecutionAssemblies()
    {
        var assembly = typeof(Phase35BOrchestrator).Assembly;
        var names = assembly.GetReferencedAssemblies().Select(item => item.Name).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("System.Net.Http", names);
        Assert.DoesNotContain("System.Diagnostics.Process", names);
    }

    [Fact]
    public void OfflineAdapterContract_ContainsNoEscapeHatchMethods()
    {
        var methods = typeof(IPhase35BProviderAdapter).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        Assert.DoesNotContain(methods, method => method.Name.Contains("Command", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, method => method.Name.Contains("Path", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, method => method.Name.Contains("Url", StringComparison.OrdinalIgnoreCase));
    }
}
