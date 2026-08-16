namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35CCredentialBoundaryPolicy
{
    internal Phase35CCredentialEvaluation Evaluate(Phase35CCredentialGrant grant, string providerId, Phase35ACapability capability, string scope, DateTimeOffset now)
    {
        if (grant.Classification == Phase35CCredentialClass.SecretMaterial) return new(false, Phase35CCredentialReason.RawSecretMaterial);
        if (grant.ExpiresAt <= now) return new(false, Phase35CCredentialReason.Expired);
        if (grant.ProviderId != providerId) return new(false, Phase35CCredentialReason.ProviderMismatch);
        if (grant.Capability != capability) return new(false, Phase35CCredentialReason.CapabilityMismatch);
        if (grant.Scope != scope) return new(false, Phase35CCredentialReason.ScopeMismatch);
        return new(true, Phase35CCredentialReason.None);
    }

    internal Phase35CCredentialEvaluation ValidateSerializedAuditValue(string value) =>
        value.Contains("secret", StringComparison.OrdinalIgnoreCase) || value.Contains("password", StringComparison.OrdinalIgnoreCase)
            ? new(false, Phase35CCredentialReason.RawSecretMaterial)
            : new(true, Phase35CCredentialReason.None);
}
