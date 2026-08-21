using Xunit;

namespace ServiceDotnet.Tests.Architecture;

public sealed class AccessibilityColorMathBoundaryTests
{
    [Fact]
    public void AccessibilityColorMath_IsPureAndLayerIndependent()
    {
        var sourcePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "Services", "Pbir", "AccessibilityColorMath.cs"));
        Assert.True(File.Exists(sourcePath), $"Missing architecture source: {sourcePath}");
        var source = File.ReadAllText(sourcePath);

        foreach (var forbiddenDependency in new[]
        {
            "Provider", "Mutation", "Authoring", "Rpc", "Transport", "VSCode", "VisualStudio",
            "File.", "Directory.", "ILogger", "ReportDiscovery", "PbirProjectService"
        })
        {
            Assert.DoesNotContain(forbiddenDependency, source, StringComparison.OrdinalIgnoreCase);
        }
    }
}
