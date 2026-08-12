namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35DCertificationLifecycle(Func<DateTimeOffset>? clock = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);

    internal Phase35DCertificationRecord Issue(Phase35DCertificationDecision decision, Phase35DCertificationCandidate candidate, Phase35DCertificationProfile profile, string certificationId)
    {
        if (!decision.IsCertified) throw new InvalidOperationException("Only a passing certification decision can be issued.");
        var issued = _clock();
        if (profile.ValidFor <= TimeSpan.Zero) throw new InvalidOperationException("Certification profile validity must be positive.");
        return new(Phase35DContracts.RecordV1, certificationId, candidate, profile.ProfileId, decision.Evidence.EvidenceHash, issued, issued.Add(profile.ValidFor), Phase35DCertificationState.Certified, null, null, null, candidate.PolicyVersions);
    }

    internal Phase35DCertificationRecord Revoke(Phase35DCertificationRecord record, string reason) => record with { State = Phase35DCertificationState.Revoked, RevocationReason = reason };
    internal Phase35DCertificationRecord Supersede(Phase35DCertificationRecord record, string successorId) => record with { State = Phase35DCertificationState.Superseded, SupersededBy = successorId };
    internal Phase35DCertificationRecord Expire(Phase35DCertificationRecord record, DateTimeOffset now) => record.ExpiresAt <= now ? record with { State = Phase35DCertificationState.Expired } : throw new InvalidOperationException("A live certification cannot be expired early.");
    internal Phase35DCertificationState Transition(Phase35DCertificationState current, Phase35DCertificationState next)
    {
        var allowed = (current, next) switch
        {
            (Phase35DCertificationState.Candidate, Phase35DCertificationState.EvidenceCollected) => true,
            (Phase35DCertificationState.EvidenceCollected, Phase35DCertificationState.Verified) => true,
            (Phase35DCertificationState.Verified, Phase35DCertificationState.Certified) => true,
            (Phase35DCertificationState.Candidate, Phase35DCertificationState.Rejected) => true,
            (Phase35DCertificationState.EvidenceCollected, Phase35DCertificationState.Rejected) => true,
            (Phase35DCertificationState.Verified, Phase35DCertificationState.Rejected) => true,
            (Phase35DCertificationState.Certified, Phase35DCertificationState.Expired or Phase35DCertificationState.Revoked or Phase35DCertificationState.Superseded or Phase35DCertificationState.Invalidated) => true,
            _ => false
        };
        if (!allowed) throw new InvalidOperationException($"Illegal certification lifecycle transition: {current} -> {next}.");
        return next;
    }
    internal bool IsLive(Phase35DCertificationRecord record, DateTimeOffset now) => record.State == Phase35DCertificationState.Certified && record.ExpiresAt > now;
}
