using System.Diagnostics;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35EMacSandboxAdapter
{
    internal Phase35EPlatformCapabilities GetCapabilities() => new("darwin-arm64", false, false, false, false, false, false, false, false);

    internal string BuildProfile(Phase35ESandboxExecutionSpec spec)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException("Phase35E macOS Seatbelt enforcement is unavailable");
        var roots = string.Join(" ", new[] { spec.InputDirectory, spec.WorkingDirectory, spec.OutputDirectory }.Select(root => $"(subpath \"{Escape(root)}\")"));
        return $"(version 1) (deny default) (allow process-exec (literal \"{Escape(spec.Identity.ExecutablePath)}\")) (allow file-read* {roots}) (allow file-write* (subpath \"{Escape(spec.WorkingDirectory)}\") (subpath \"{Escape(spec.OutputDirectory)}\")) (deny network*) (deny process-fork)";
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}

internal sealed class Phase35EMacSandboxProcessBoundary : IPhase35EProcessBoundary
{
    private readonly Phase35EMacSandboxAdapter _adapter = new();

    public async Task<Phase35EProcessCapture> StartAsync(Phase35ESandboxExecutionSpec spec, CancellationToken cancellationToken)
    {
        var profilePath = Path.Combine(Path.GetTempPath(), "phase35e-" + Guid.NewGuid().ToString("N") + ".sb");
        await File.WriteAllTextAsync(profilePath, _adapter.BuildProfile(spec), cancellationToken);
        try
        {
            var startInfo = new ProcessStartInfo("/usr/bin/sandbox-exec") { WorkingDirectory = spec.WorkingDirectory, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = false, CreateNoWindow = true };
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add(profilePath);
            startInfo.ArgumentList.Add(spec.Identity.ExecutablePath);
            foreach (var argument in spec.Arguments) startInfo.ArgumentList.Add(argument);
            startInfo.Environment.Clear();
            foreach (var item in spec.Environment) startInfo.Environment[item.Key] = item.Value;
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("sandbox process creation returned no handle");
            try
            {
                var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                return new(process.ExitCode, stdout, stderr, true);
            }
            catch
            {
                if (!process.HasExited) process.Kill(true);
                throw;
            }
        }
        finally
        {
            File.Delete(profilePath);
        }
    }

    public Task TerminateAsync(Phase35EProcessCapture process, CancellationToken cancellationToken) => Task.CompletedTask;
}
