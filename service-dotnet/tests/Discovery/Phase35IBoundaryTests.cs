using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35IBoundaryTests
{
    [Fact, Trait("Category", "Boundary")]
    public void WindowsNativeApisExistOnlyInTheDedicatedRuntime()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var sourceRoot = Path.Combine(root, "Services", "Discovery");
        var files = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories).Where(path => !path.Contains("Phase35I.Runtime", StringComparison.Ordinal));
        var forbidden = new[] { "CreateRestrictedToken", "CreateProcessAsUser", "CreateJobObject", "AssignProcessToJobObject", "ResumeThread", "TerminateJobObject" };

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain(forbidden, token => text.Contains(token, StringComparison.Ordinal));
        }
    }

    [Fact, Trait("Category", "Boundary")]
    public void PortablePhase35IHasNoShellOrCallerLaunchSurface()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var directory = Path.Combine(root, "Services", "Discovery", "Phase35I");
        var text = string.Join("\n", Directory.EnumerateFiles(directory, "*.cs").Select(File.ReadAllText));
        foreach (var forbidden in new[] { "cmd.exe", "powershell", "Process.Start", "WorkingDirectory", "IProvider", "CredentialManager", "Mcp", "Skills" }) Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
    }
}
