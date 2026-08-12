namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35CReplayProtectionService
{
    private readonly Dictionary<string, Phase35CExecutionIdentity> _identities = new(StringComparer.Ordinal);

    internal Phase35CReplayEvaluation Accept(Phase35CExecutionIdentity identity, bool authorizedRetry)
    {
        if (string.IsNullOrWhiteSpace(identity.ExecutionId) || string.IsNullOrWhiteSpace(identity.SessionId) || string.IsNullOrWhiteSpace(identity.RequestHash) || string.IsNullOrWhiteSpace(identity.Nonce)) return new(false, Phase35CReplayReason.InvalidIdentity);
        if (!_identities.TryGetValue(identity.ExecutionId, out var existing))
        {
            _identities.Add(identity.ExecutionId, identity);
            return new(true, Phase35CReplayReason.None);
        }
        if (existing.RequestHash != identity.RequestHash) return new(false, Phase35CReplayReason.ModifiedRequest);
        return authorizedRetry ? new(true, Phase35CReplayReason.None) : new(false, Phase35CReplayReason.DuplicateExecution);
    }
}
