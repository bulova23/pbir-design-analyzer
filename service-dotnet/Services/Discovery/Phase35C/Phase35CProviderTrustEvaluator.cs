namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35CProviderTrustEvaluator(Func<DateTimeOffset>? clock = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);

    internal Phase35CTrustEvaluation Evaluate(Phase35AProviderProfile profile, Phase35CProviderIdentity identity, Phase35CProviderAttestation? attestation, Phase35CPolicyVersions policyVersions)
    {
        var now = _clock();
        if (attestation is null) return new(false, Phase35CTrustReason.AttestationMissing, now, null);
        if (profile.ProviderId != identity.ProviderId || attestation.ProviderId != profile.ProviderId) return new(false, Phase35CTrustReason.ProviderMismatch, now, attestation.ExpiresAt);
        if (attestation.ProviderVersion != identity.Version) return new(false, Phase35CTrustReason.VersionMismatch, now, attestation.ExpiresAt);
        if (attestation.ImplementationIdentity != identity.ImplementationIdentity) return new(false, Phase35CTrustReason.ImplementationMismatch, now, attestation.ExpiresAt);
        if (!identity.Capabilities.Order().SequenceEqual(attestation.Capabilities.Order())) return new(false, Phase35CTrustReason.CapabilityMismatch, now, attestation.ExpiresAt);
        if (attestation.ExecutionClass != identity.ExecutionClass) return new(false, Phase35CTrustReason.ExecutionModeMismatch, now, attestation.ExpiresAt);
        if (attestation.SandboxPolicyVersion != policyVersions.Sandbox || attestation.PolicyVersions != policyVersions) return new(false, Phase35CTrustReason.PolicyBindingMismatch, now, attestation.ExpiresAt);
        if (attestation.ExpiresAt <= now || attestation.EvaluatedAt > now) return new(false, Phase35CTrustReason.TrustExpired, now, attestation.ExpiresAt);
        if (attestation.SchemaVersion != Phase35CContracts.TrustV1 || attestation.ExpiresAt <= attestation.EvaluatedAt) return new(false, Phase35CTrustReason.InvalidAttestation, now, attestation.ExpiresAt);
        return new(true, Phase35CTrustReason.None, now, attestation.ExpiresAt);
    }
}
