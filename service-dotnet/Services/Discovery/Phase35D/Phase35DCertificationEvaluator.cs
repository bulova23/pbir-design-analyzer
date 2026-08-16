namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35DCertificationEvaluator(Func<DateTimeOffset>? clock = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);
    private readonly Phase35ACanonicalJson _canonical = new();

    internal Phase35DCertificationDecision Evaluate(Phase35DCertificationInput input)
    {
        var now = _clock();
        var identity = new Phase35CProviderIdentity(input.Candidate.ProviderId, input.Candidate.ProviderVersion, input.Candidate.ImplementationId, input.Candidate.Capabilities, input.Candidate.ExecutionClass);
        var attestation = new Phase35DSignedAttestationVerifier().Verify(input.SignedAttestation, input.Candidate, input.SignerPublicKeyBase64, now, input.Profile);
        var trust = new Phase35CProviderTrustEvaluator(() => now).Evaluate(input.ProviderProfile, identity, input.TrustAttestation, input.Candidate.PolicyVersions);
        var sandbox = new Phase35CSandboxPolicyEvaluator().Evaluate(input.Candidate.SandboxPolicy, input.ApprovedSandboxPolicy);
        var resource = new Phase35CResourcePolicyEvaluator().Evaluate(input.ResourcePolicy);
        var conformance = new Phase35DConformanceRunner().Run(input.Adapter, identity, input.Request, input.ConformanceEvidence, input.OutputFixture, input.OutputProperties);
        var reasons = new List<Phase35DCertificationDenialReason>();
        if (!attestation.IsValid) reasons.Add(attestation.Reason switch { Phase35DAttestationReason.SignerUnapproved => Phase35DCertificationDenialReason.SignerUnapproved, Phase35DAttestationReason.SignerExpired => Phase35DCertificationDenialReason.SignerExpired, Phase35DAttestationReason.SignerRevoked => Phase35DCertificationDenialReason.SignerRevoked, Phase35DAttestationReason.IdentityMismatch or Phase35DAttestationReason.PackageHashMismatch => Phase35DCertificationDenialReason.PackageIdentityMismatch, _ => Phase35DCertificationDenialReason.SignatureInvalid });
        if (!trust.IsTrusted) reasons.Add(Phase35DCertificationDenialReason.TrustFailed);
        if (!sandbox.IsAllowed) reasons.Add(Phase35DCertificationDenialReason.SandboxPolicyFailed);
        if (!input.Credential.IsAllowed) reasons.Add(Phase35DCertificationDenialReason.CredentialPolicyFailed);
        if (!resource.IsAllowed) reasons.Add(Phase35DCertificationDenialReason.ResourcePolicyInvalid);
        if (!conformance.IsConformant) reasons.Add(conformance.Output.IsValid ? Phase35DCertificationDenialReason.ConformanceFailed : Phase35DCertificationDenialReason.OutputCorpusFailed);
        if (!input.AuditAvailable) reasons.Add(Phase35DCertificationDenialReason.AuditUnavailable);
        if (!input.ReplayProtectionAvailable) reasons.Add(Phase35DCertificationDenialReason.ReplayProtectionUnavailable);
        if (!input.ArtifactSafetyAvailable) reasons.Add(Phase35DCertificationDenialReason.EvidenceIncomplete);
        if (input.Candidate.ExecutionClass != input.Profile.RequiredExecutionClass || input.Candidate.SandboxPolicy.Version != input.Profile.RequiredSandboxPolicyVersion || input.Candidate.OutputCorpusVersion != input.Profile.RequiredOutputCorpusVersion || input.Candidate.ConformanceProfileVersion != input.Profile.RequiredConformanceProfileVersion || input.Candidate.PolicyVersions != new Phase35CPolicyVersions(input.Candidate.PolicyVersions.Execution, input.Candidate.SandboxPolicy.Version, input.Candidate.PolicyVersions.Credential, input.Candidate.PolicyVersions.ArtifactSafety, input.Candidate.PolicyVersions.Conformance, input.Candidate.PolicyVersions.OutputCorpus)) reasons.Add(Phase35DCertificationDenialReason.PolicyVersionMismatch);
        var evidenceWithoutHash = new Phase35DCertificationEvidence(Phase35DContracts.EvidenceV1, input.Candidate.Package, attestation, trust, sandbox, input.Credential, resource, conformance, input.AuditAvailable, input.ReplayProtectionAvailable, input.ArtifactSafetyAvailable, input.Candidate.PolicyVersions, now, string.Empty);
        var evidence = evidenceWithoutHash with { EvidenceHash = _canonical.Hash(evidenceWithoutHash) };
        return new(reasons.Count == 0, reasons.Count == 0, reasons.Distinct().ToArray(), evidence);
    }
}
