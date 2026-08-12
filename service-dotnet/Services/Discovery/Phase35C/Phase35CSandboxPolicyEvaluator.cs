namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35CSandboxPolicyEvaluator
{
    internal Phase35CSandboxEvaluation Evaluate(Phase35CSandboxPolicy requested, Phase35CSandboxPolicy approved)
    {
        var reasons = new List<string>();
        if (requested.Version != approved.Version) reasons.Add("sandbox-version-mismatch");
        if (requested.ProcessModel != Phase35CProcessModel.Isolated || requested.ProcessModel != approved.ProcessModel) reasons.Add("process-model-not-isolated");
        if (requested.Network == Phase35CNetworkPolicy.Unrestricted || requested.Network != approved.Network) reasons.Add("network-policy-not-contained");
        if (requested.Filesystem == Phase35CFilesystemPolicy.Unrestricted || requested.Filesystem != approved.Filesystem) reasons.Add("filesystem-policy-not-contained");
        if (requested.Environment == Phase35CEnvironmentPolicy.Inherited || requested.Environment != approved.Environment) reasons.Add("environment-policy-not-contained");
        if (requested.CredentialAccess != Phase35CCredentialAccessPolicy.GrantOnly || requested.CredentialAccess != approved.CredentialAccess) reasons.Add("credential-policy-not-contained");
        if (requested.ChildProcessesAllowed || requested.ChildProcessesAllowed != approved.ChildProcessesAllowed) reasons.Add("child-process-policy-not-contained");
        if (!requested.AllowedDependencies.All(item => approved.AllowedDependencies.Contains(item, StringComparer.Ordinal))) reasons.Add("dependency-policy-mismatch");
        if (requested.MaxDuration <= TimeSpan.Zero || requested.MaxDuration > approved.MaxDuration || requested.MaxMemoryMegabytes <= 0 || requested.MaxMemoryMegabytes > approved.MaxMemoryMegabytes || requested.MaxArtifactCount <= 0 || requested.MaxArtifactCount > approved.MaxArtifactCount || requested.MaxAttempts <= 0 || requested.MaxAttempts > approved.MaxAttempts || requested.MaxArtifactBytes <= 0 || requested.MaxArtifactBytes > approved.MaxArtifactBytes) reasons.Add("resource-limits-invalid");
        return new(reasons.Count == 0, reasons);
    }
}
