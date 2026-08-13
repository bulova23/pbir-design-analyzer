namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35ESandboxAdmission
{
    internal Phase35ESandboxAdmissionDecision Evaluate(Phase35ESandboxAdmissionInput input)
    {
        var failures = new List<Phase35EFailureCode>();
        if (!OperatingSystem.IsMacOS()) failures.Add(Phase35EFailureCode.SandboxNotSupported);
        if (!new Phase35EExecutableIdentityVerifier().Verify(input.CertifiedIdentity, input.RequestedIdentity)) failures.Add(Phase35EFailureCode.ExecutableIdentityMismatch);
        var binding = new Phase35EPolicyBinder().Bind(input.Policy, input.Capabilities);
        if (!binding.IsEnforceable) failures.Add(Phase35EFailureCode.PolicyNotEnforceable);
        if (!input.AuditAvailable) failures.Add(Phase35EFailureCode.SandboxAdmissionDenied);
        return new(failures.Count == 0, failures.Distinct().ToArray(), binding);
    }
}

internal sealed class Phase35ESandboxEnvironmentBuilder
{
    internal IReadOnlyDictionary<string, string?> Build(IReadOnlyDictionary<string, string?> host, IReadOnlyList<string> allowlist) => allowlist.Distinct(StringComparer.Ordinal).ToDictionary(key => key, key => host.TryGetValue(key, out var value) ? value : null, StringComparer.Ordinal);
}
