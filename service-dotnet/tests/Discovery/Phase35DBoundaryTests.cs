using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35DBoundaryTests
{
    [Fact]
    public void Phase35DAssembly_DoesNotReferenceExecutionOrNetworkAssemblies()
    {
        var names = typeof(Phase35DContracts).Assembly.GetReferencedAssemblies().Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("System.Net.Http", names);
        Assert.DoesNotContain("System.Diagnostics.Process", names);
    }

    [Fact]
    public void Phase35DContractSurface_HasNoExecutionEscapeHatches()
    {
        var types = typeof(Phase35DContracts).Assembly.GetTypes().Where(type => type.Namespace?.Contains("Phase35D", StringComparison.Ordinal) == true);
        var names = types.SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)).Select(property => property.Name).ToArray();
        Assert.DoesNotContain(names, name => name.Contains("Command", StringComparison.OrdinalIgnoreCase) || name.Contains("Url", StringComparison.OrdinalIgnoreCase) || name.Contains("Process", StringComparison.OrdinalIgnoreCase) || name.Contains("Secret", StringComparison.OrdinalIgnoreCase));
    }
}
