using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35EBoundaryTests
{
    [Fact]
    public void Phase35E_ProductionSources_ContainNoShellOrNetworkBridge()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "../../../../Services/Discovery/Phase35E");
        var source = string.Join("\n", Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.EndsWith("Phase35ESandboxedProcessRunner.cs", StringComparison.Ordinal) && !file.EndsWith("Phase35EMacSandboxAdapter.cs", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        Assert.DoesNotContain("cmd.exe", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/bin/sh", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/bin/bash", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Process.Start", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Phase35F_SelectionSourcesContainNoProcessCreationFallback()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "../../../../Services/Discovery/Phase35F");
        var source = string.Join("\n", Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.DoesNotContain("Process.Start", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ProcessStartInfo", source, StringComparison.Ordinal);
    }
}
