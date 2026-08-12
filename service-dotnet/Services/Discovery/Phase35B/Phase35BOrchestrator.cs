namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35BOrchestrator
{
    private readonly Phase35BProviderRegistry _registry;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Phase35BValidationPipeline _validation;
    private readonly Phase35BProviderResolutionService _resolution = new();
    private readonly Phase35BAuthorizationGate _authorization = new();
    private readonly Phase35BReadinessGate _readiness = new();
    private readonly Phase35BTimeoutCoordinator _timeout;

    private Phase35BOrchestrator(Phase35BProviderRegistry registry, Func<DateTimeOffset> clock, IReadOnlyList<IPhase35BValidationStage> stages)
    {
        _registry = registry;
        _clock = clock;
        _validation = new Phase35BValidationPipeline(stages);
        _timeout = new Phase35BTimeoutCoordinator();
    }

    internal static Phase35BOrchestrator CreateForTests(Phase35BProviderRegistry registry, Func<DateTimeOffset> clock) =>
        new(registry, clock, Phase35BDefaultValidationStages.Create());

    internal static Phase35BOrchestrator CreateProduction() =>
        new(new Phase35BProviderRegistry(Phase35BProductionCatalog.Registrations), () => DateTimeOffset.UtcNow, Phase35BDefaultValidationStages.Create());

    internal async Task<Phase35BExecutionOutcome> ExecuteAsync(Phase35BExecutionInput input, CancellationToken cancellationToken = default)
    {
        var profile = _registry.Registrations.FirstOrDefault(registration => registration.Profile.ProviderId == input.Request.ProviderId)?.Profile;
        if (profile is null) return Rejected(input, "provider-not-found", "no exact provider profile was registered");
        var resolution = _resolution.Resolve(input.Request, input.Authorization, input.Policy, input.Readiness, _registry);
        if (!resolution.IsSuccess || resolution.Adapter is null || resolution.Profile is null)
            return Rejected(input, "provider-resolution-failed", resolution.Reason);
        var authorization = _authorization.Validate(input.Request, resolution.Profile, input.Authorization, input.Policy);
        if (!authorization.IsAllowed) return Rejected(input, "authorization-failed", string.Join("; ", authorization.Reasons));
        var readiness = _readiness.Validate(input.Request, resolution.Profile, input.Authorization, input.Policy, input.Readiness);
        if (!readiness.IsReady) return Rejected(input, "readiness-failed", string.Join("; ", readiness.Reasons));
        if (cancellationToken.IsCancellationRequested)
            return new(Phase35BOutcomeStatus.Cancelled, null, null, null, null, new Phase35AFailure(Phase35AContracts.FailureV1, Phase35AFailureClass.ProviderFailure, "cancelled-before-start", "caller cancellation was already requested", false));

        var session = new Phase35BSessionFactory(_clock).Create(input.Request, resolution.Profile, input.Authorization, input.Readiness, input.Policy, input.TimeoutPolicy);
        session = session.Advance(Phase35BRuntimeState.Validated, Phase35BRuntimeEvent.Validated)
            .Advance(Phase35BRuntimeState.Authorized, Phase35BRuntimeEvent.AuthorizationApproved)
            .Advance(Phase35BRuntimeState.Ready, Phase35BRuntimeEvent.ReadinessApproved)
            .Advance(Phase35BRuntimeState.ProviderResolved, Phase35BRuntimeEvent.ProviderResolved)
            .Advance(Phase35BRuntimeState.Executing, Phase35BRuntimeEvent.ExecutionStarted);
        var context = new Phase35BExecutionContext(input.Request, session);
        var execution = await _timeout.RunAsync(token => resolution.Adapter.ExecuteOfflineAsync(context, token), input.TimeoutPolicy, cancellationToken);
        if (execution.Status == Phase35BTimeoutStatus.Cancelled)
            return Terminal(session.Advance(Phase35BRuntimeState.Cancelled, Phase35BRuntimeEvent.Cancelled), null, null, Phase35BOutcomeStatus.Cancelled, "caller-cancelled");
        if (execution.Status == Phase35BTimeoutStatus.TimedOut)
            return Terminal(session.Advance(Phase35BRuntimeState.TimedOut, Phase35BRuntimeEvent.TimedOut), null, null, Phase35BOutcomeStatus.TimedOut, "execution-timeout");
        if (execution.Status != Phase35BTimeoutStatus.Completed || execution.Value is null)
            return Terminal(session.Advance(Phase35BRuntimeState.Failed, Phase35BRuntimeEvent.Failed), null, null, Phase35BOutcomeStatus.Failed, "offline-adapter-failure");

        var offline = execution.Value;
        session = session.Advance(Phase35BRuntimeState.ValidatingResult, Phase35BRuntimeEvent.ResultValidationStarted);
        var validation = _validation.Validate(new Phase35BExecutionContext(input.Request, session), offline);
        if (!validation.IsValid)
            return CompleteWithAudit(session.Advance(Phase35BRuntimeState.Failed, Phase35BRuntimeEvent.Failed), offline.Result, null, validation, Phase35BOutcomeStatus.Failed, "validation-failed");
        session = session.Advance(Phase35BRuntimeState.ReviewingArtifact, Phase35BRuntimeEvent.ArtifactReviewStarted);
        var artifact = new Phase35BArtifactIntakeService().Review(offline.Artifact, offline.Result);
        if (artifact.Disposition == Phase35BArtifactDisposition.Quarantined)
            return CompleteWithAudit(session.Advance(Phase35BRuntimeState.Quarantined, Phase35BRuntimeEvent.Quarantined), offline.Result, artifact, validation, Phase35BOutcomeStatus.Quarantined, "artifact-quarantined");
        if (artifact.Disposition == Phase35BArtifactDisposition.Rejected)
            return CompleteWithAudit(session.Advance(Phase35BRuntimeState.Failed, Phase35BRuntimeEvent.Failed), offline.Result, artifact, validation, Phase35BOutcomeStatus.Failed, "artifact-rejected");
        return CompleteWithAudit(session.Advance(Phase35BRuntimeState.Completed, Phase35BRuntimeEvent.Completed), offline.Result, artifact, validation, Phase35BOutcomeStatus.Completed, null);
    }

    private Phase35BExecutionOutcome Rejected(Phase35BExecutionInput input, string code, string message) =>
        new(Phase35BOutcomeStatus.Rejected, null, null, null, null, new Phase35AFailure(Phase35AContracts.FailureV1, Phase35AFailureClass.Authorization, code, message, false));

    private Phase35BExecutionOutcome Terminal(Phase35BSession session, Phase35AResult? result, Phase35BArtifactReview? artifact, Phase35BOutcomeStatus status, string code) =>
        CompleteWithAudit(session, result, artifact, new Phase35BValidationResult([]), status, code);

    private Phase35BExecutionOutcome CompleteWithAudit(Phase35BSession session, Phase35AResult? result, Phase35BArtifactReview? artifact, Phase35BValidationResult validation, Phase35BOutcomeStatus status, string? code)
    {
        var audit = new Phase35BAuditProjectionService(_clock).Create(session, validation, artifact, status, code);
        var failure = code is null ? null : new Phase35AFailure(Phase35AContracts.FailureV1, status == Phase35BOutcomeStatus.Quarantined ? Phase35AFailureClass.Quarantine : Phase35AFailureClass.ProviderFailure, code, code, false);
        return new(status, session, result, artifact, audit, failure);
    }
}
