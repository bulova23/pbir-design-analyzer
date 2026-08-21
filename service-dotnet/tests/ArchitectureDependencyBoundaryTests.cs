using Xunit;

namespace ServiceDotnet.Tests;

/// <summary>
/// Small, explicit composition-boundary guards. These are intentionally source-level until
/// the domains have enough independent lifecycle to justify separate assemblies.
/// </summary>
public sealed class ArchitectureDependencyBoundaryTests
{
    [Fact]
    public void ProductionCompositionRoot_DoesNotReachExperimentalOrAuthoringInfrastructure()
    {
        var source = ReadSource("RpcHost", "Program.cs");

        Assert.DoesNotContain("Phase35", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RuntimeProvider", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PbirAuthoring", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScoringService_DoesNotDependOnPresentationOrExperimentalInfrastructure()
    {
        var source = ReadSource("Services", "Pbir", "PbirScoringService.cs");

        Assert.DoesNotContain("DesignStudio", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webview", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Phase35", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RuntimeProvider", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadSource(params string[] segments)
    {
        var serviceRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", ".."));
        var path = Path.Combine([serviceRoot, .. segments]);
        Assert.True(File.Exists(path), $"Missing architecture source: {path}");
        return File.ReadAllText(path);
    }
}
