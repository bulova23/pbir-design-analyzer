using System.Security.Cryptography;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35DSignedAttestationVerifier
{
    private readonly Phase35ACanonicalJson _canonical = new();

    internal Phase35DAttestationVerification Verify(Phase35DSignedAttestation? attestation, Phase35DCertificationCandidate expected, string publicKeyBase64, DateTimeOffset now, Phase35DCertificationProfile profile)
    {
        if (attestation is null) return new(false, Phase35DAttestationReason.Missing, string.Empty, null);
        if (attestation.SchemaVersion != Phase35DContracts.AttestationV1 || string.IsNullOrWhiteSpace(attestation.SignedPayload) || string.IsNullOrWhiteSpace(attestation.SignatureBase64)) return new(false, Phase35DAttestationReason.Malformed, attestation.SignerIdentity, attestation.SignerNotAfter);
        if (attestation.SignatureAlgorithm != Phase35DSignatureAlgorithm.RsaSha256) return new(false, Phase35DAttestationReason.UnsupportedAlgorithm, attestation.SignerIdentity, attestation.SignerNotAfter);
        if (attestation.SignerIdentity != expected.ExpectedSignerIdentity) return new(false, Phase35DAttestationReason.SignerMismatch, attestation.SignerIdentity, attestation.SignerNotAfter);
        if (profile.RevokedSignerIdentities.Contains(attestation.SignerIdentity, StringComparer.Ordinal)) return new(false, Phase35DAttestationReason.SignerRevoked, attestation.SignerIdentity, attestation.SignerNotAfter);
        if (!profile.ApprovedSignerIdentities.Contains(attestation.SignerIdentity, StringComparer.Ordinal)) return new(false, Phase35DAttestationReason.SignerUnapproved, attestation.SignerIdentity, attestation.SignerNotAfter);
        if (attestation.SignerNotBefore > now || attestation.SignerNotAfter <= now) return new(false, Phase35DAttestationReason.SignerExpired, attestation.SignerIdentity, attestation.SignerNotAfter);
        if (attestation.Candidate.Package.Metadata.PackageSha256 != expected.Package.Metadata.PackageSha256) return new(false, Phase35DAttestationReason.PackageHashMismatch, attestation.SignerIdentity, attestation.SignerNotAfter);
        if (attestation.Candidate.ProviderId != expected.ProviderId || attestation.Candidate.ProviderVersion != expected.ProviderVersion || attestation.Candidate.ImplementationId != expected.ImplementationId || attestation.Candidate.Package.IdentityHash != expected.Package.IdentityHash) return new(false, Phase35DAttestationReason.IdentityMismatch, attestation.SignerIdentity, attestation.SignerNotAfter);
        if (!string.Equals(attestation.SignedPayload, _canonical.Hash(expected), StringComparison.Ordinal)) return new(false, Phase35DAttestationReason.IdentityMismatch, attestation.SignerIdentity, attestation.SignerNotAfter);
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);
            var payload = System.Text.Encoding.UTF8.GetBytes(attestation.SignedPayload);
            var signature = Convert.FromBase64String(attestation.SignatureBase64);
            if (!rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) return new(false, Phase35DAttestationReason.InvalidSignature, attestation.SignerIdentity, attestation.SignerNotAfter);
            return new(true, Phase35DAttestationReason.None, attestation.SignerIdentity, attestation.SignerNotAfter);
        }
        catch (FormatException) { return new(false, Phase35DAttestationReason.Malformed, attestation.SignerIdentity, attestation.SignerNotAfter); }
        catch (CryptographicException) { return new(false, Phase35DAttestationReason.Malformed, attestation.SignerIdentity, attestation.SignerNotAfter); }
    }
}
