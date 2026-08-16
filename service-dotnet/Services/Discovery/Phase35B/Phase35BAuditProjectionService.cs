namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35BAuditProjectionService(Func<DateTimeOffset>? clock = null)
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);

    internal Phase35BAuditRecord Create(Phase35BSession session, Phase35BValidationResult validation, Phase35BArtifactReview? artifact, Phase35BOutcomeStatus outcome, string? failureCode)
    {
        return new(
            Phase35BContracts.AuditV1, $"audit:{session.SessionId}", session.RequestId, session.RequestHash, session.ProviderId,
            session.PolicyHash, session.Authorization.Status, session.Readiness.State, session.Lifecycle,
            validation.Stages, artifact?.Disposition, outcome, failureCode, session.CreatedAt, _clock());
    }
}
