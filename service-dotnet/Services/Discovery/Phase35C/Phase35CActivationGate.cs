namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35CActivationGate(Func<DateTimeOffset>? clock = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);

    internal Phase35CActivationDecision Evaluate(Phase35CActivationInput input)
    {
        var reasons = new List<Phase35CActivationDenialReason>();
        if (input.Profile.ExecutionClass != Phase35AExecutionClass.Executable) reasons.Add(Phase35CActivationDenialReason.ProviderUnavailable);
        if (input.Authorization.Status != Phase35AAuthorizationStatus.Approved) reasons.Add(Phase35CActivationDenialReason.AuthorizationDenied);
        if (input.Readiness.State != Phase35AReadiness.ReadyForExecution) reasons.Add(Phase35CActivationDenialReason.ReadinessFailed);
        if (!input.Trust.IsTrusted) reasons.Add(input.Trust.Reason == Phase35CTrustReason.TrustExpired ? Phase35CActivationDenialReason.TrustExpired : Phase35CActivationDenialReason.TrustMissing);
        if (!input.Sandbox.IsAllowed) reasons.Add(Phase35CActivationDenialReason.SandboxPolicyFailed);
        if (!input.Credential.IsAllowed) reasons.Add(Phase35CActivationDenialReason.CredentialGrantMissing);
        if (!input.Resource.IsAllowed) reasons.Add(Phase35CActivationDenialReason.ResourcePolicyInvalid);
        if (!input.Conformance.IsConformant) reasons.Add(Phase35CActivationDenialReason.ConformanceFailed);
        if (!input.OutputValidation.IsValid) reasons.Add(Phase35CActivationDenialReason.OutputCorpusNotApproved);
        if (!input.AuditAvailable) reasons.Add(Phase35CActivationDenialReason.AuditUnavailable);
        if (!input.ArtifactScannerAvailable) reasons.Add(Phase35CActivationDenialReason.ArtifactScannerUnavailable);
        if (!input.ReplayProtectionAvailable) reasons.Add(Phase35CActivationDenialReason.ReplayProtectionUnavailable);
        _ = _clock();
        return new(reasons.Count == 0, reasons.Distinct().ToArray(), input.PolicyVersions);
    }

    internal Phase35CActivationDecision EvaluateProduction(IReadOnlyList<Phase35BProviderRegistration> registrations)
    {
        var versions = new Phase35CPolicyVersions("execution/v1", "sandbox/v1", "credential/v1", "artifact/v1", "conformance/v1", "corpus/v1");
        var reasons = new List<Phase35CActivationDenialReason>();
        if (registrations.Count == 0 || registrations.All(registration => registration.Adapter is null || registration.Profile.ExecutionClass != Phase35AExecutionClass.Executable)) reasons.Add(Phase35CActivationDenialReason.ProviderUnavailable);
        if (registrations.All(registration => registration.Adapter is null)) reasons.Add(Phase35CActivationDenialReason.TrustMissing);
        reasons.AddRange([
            Phase35CActivationDenialReason.SandboxPolicyFailed,
            Phase35CActivationDenialReason.CredentialGrantMissing,
            Phase35CActivationDenialReason.AuditUnavailable,
            Phase35CActivationDenialReason.ConformanceFailed,
            Phase35CActivationDenialReason.OutputCorpusNotApproved,
            Phase35CActivationDenialReason.ArtifactScannerUnavailable,
            Phase35CActivationDenialReason.ReplayProtectionUnavailable,
            Phase35CActivationDenialReason.ResourcePolicyInvalid]);
        _ = _clock();
        return new(false, reasons.Distinct().ToArray(), versions);
    }
}
