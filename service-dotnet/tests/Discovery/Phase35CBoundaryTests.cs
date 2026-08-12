using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35CBoundaryTests
{
    [Fact]
    public void Phase35CAssembly_DoesNotReferenceProviderExecutionAssemblies()
    {
        var names = typeof(Phase35CActivationGate).Assembly.GetReferencedAssemblies().Select(item => item.Name).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("System.Net.Http", names);
        Assert.DoesNotContain("System.Diagnostics.Process", names);
    }

    [Fact]
    public void Phase35CContracts_DoNotExposeSecretOrExecutionEscapeHatches()
    {
        var types = typeof(Phase35CActivationGate).Assembly.GetTypes().Where(type => type.Namespace?.Contains("Phase35C", StringComparison.Ordinal) == true);
        var publicNames = types.SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)).Select(property => property.Name).ToArray();

        Assert.DoesNotContain(publicNames, name => name.Contains("Secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicNames, name => name.Contains("Command", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicNames, name => name.Contains("Url", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicNames, name => name.Contains("Path", StringComparison.OrdinalIgnoreCase));
    }
}
