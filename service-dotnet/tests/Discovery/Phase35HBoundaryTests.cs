using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35HBoundaryTests
{
    [Fact]
    public void Phase35H_SourcesExposeOnlyDomainOperationsAndNoGenericExecutionBridge()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "../../../../Services/Discovery/Phase35H");
        var source = string.Join("\n", Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        foreach (var forbidden in new[] { "RunCommand", "RunProcess", "ExecuteShell", "ExecuteScript", "UploadExecutable", "InvokeTool", "InvokeMcp", "Process.Start", "ProcessStartInfo", "HttpClient", "cmd.exe", "/bin/sh", "Assembly.Load" })
            Assert.DoesNotContain(forbidden, source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Phase35H_WorkerSourceDoesNotContainProviderOrMutationAuthority()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "../../../../Services/Discovery/Phase35H");
        var source = string.Join("\n", Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        foreach (var forbidden in new[] { "Power BI Desktop", "PBIR", "MCP", "Skills", "Publish", "Fabric", "connectionString", "bearerToken", "password" })
            Assert.DoesNotContain(forbidden, source, StringComparison.OrdinalIgnoreCase);
    }
}
