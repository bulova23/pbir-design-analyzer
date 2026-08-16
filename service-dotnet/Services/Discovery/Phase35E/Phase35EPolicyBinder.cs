namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35EPolicyBinder
{
    internal Phase35EPolicyBinding Bind(Phase35ESandboxPolicy policy, Phase35EPlatformCapabilities capabilities)
    {
        var enforced = new List<string>();
        var unsupported = new List<string>();
        Add("network", policy.RequireNetworkDenial, capabilities.NetworkIsolation, enforced, unsupported);
        Add("filesystem", policy.RequireFilesystemIsolation, capabilities.FilesystemIsolation, enforced, unsupported);
        Add("environment", policy.RequireEnvironmentIsolation, capabilities.EnvironmentIsolation, enforced, unsupported);
        Add("child-process", policy.RequireChildProcessDenial, capabilities.ProcessIsolation, enforced, unsupported);
        Add("memory-limit", policy.RequireMemoryLimit, capabilities.MemoryLimit, enforced, unsupported);
        Add("cpu-limit", policy.RequireCpuLimit, capabilities.CpuLimit, enforced, unsupported);
        Add("process-count-limit", policy.RequireProcessCountLimit, capabilities.ProcessCountLimit, enforced, unsupported);
        if (policy.MaxDuration <= TimeSpan.Zero || policy.MaxDuration > TimeSpan.FromHours(1) || policy.MaxStdoutBytes <= 0 || policy.MaxStderrBytes <= 0 || policy.MaxArtifactCount <= 0) unsupported.Add("finite-resources"); else enforced.Add("finite-resources");
        return new(policy.Version, enforced, [], unsupported, Phase35EHashing.Hash(new { }), Phase35EHashing.Hash(new { }), policy.RequireNetworkDenial ? "denied" : "not-requested", $"timeout={policy.MaxDuration};stdout={policy.MaxStdoutBytes};stderr={policy.MaxStderrBytes};artifacts={policy.MaxArtifactCount}");
    }

    private static void Add(string name, bool required, bool supported, List<string> enforced, List<string> unsupported)
    {
        if (!required) return;
        if (supported) enforced.Add(name); else unsupported.Add(name);
    }
}
