namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35DCertificationActivationBinding(Func<DateTimeOffset>? clock = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);

    internal Phase35DActivationBindingDecision Evaluate(Phase35DCertificationCandidate candidate, Phase35DCertificationProfile profile, Phase35DCertificationRecord? record, Phase35DCertificationEvidence evidence, Phase35CActivationInput activationInput)
    {
        var reasons = new List<Phase35DCertificationDenialReason>();
        if (activationInput.Profile.ProviderId != candidate.ProviderId || activationInput.PolicyVersions != candidate.PolicyVersions) reasons.Add(Phase35DCertificationDenialReason.PolicyVersionMismatch);
        if (record is null || record.Candidate.Package.IdentityHash != candidate.Package.IdentityHash || record.Candidate.ProviderId != candidate.ProviderId || record.Candidate.ProviderVersion != candidate.ProviderVersion || record.Candidate.ImplementationId != candidate.ImplementationId || record.ProfileId != profile.ProfileId || record.EvidenceHash != evidence.EvidenceHash || record.PolicyVersions != candidate.PolicyVersions) reasons.Add(Phase35DCertificationDenialReason.PackageIdentityMismatch);
        if (record is null || record.State != Phase35DCertificationState.Certified || record.ExpiresAt <= _clock()) reasons.Add(Phase35DCertificationDenialReason.CertificationStale);
        if (!new Phase35CActivationGate(_clock).Evaluate(activationInput).IsEligible) reasons.Add(Phase35DCertificationDenialReason.TrustFailed);
        return new(reasons.Count == 0, reasons.Count == 0 ? Phase35DAdmission.PreProductionEligible : Phase35DAdmission.Certified, reasons.Distinct().ToArray());
    }
}
