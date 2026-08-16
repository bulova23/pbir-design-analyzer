namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35BContracts
{
    internal const string RuntimeV1 = "phase35b-runtime/v1";
    internal const string AuditV1 = "phase35b-runtime-audit/v1";
}

internal enum Phase35BRuntimeState
{
    Created,
    Validated,
    Authorized,
    Ready,
    ProviderResolved,
    Executing,
    ValidatingResult,
    ReviewingArtifact,
    Completed,
    Rejected,
    Failed,
    Cancelled,
    TimedOut,
    Quarantined
}

internal enum Phase35BRuntimeEvent
{
    Created,
    Validated,
    AuthorizationApproved,
    ReadinessApproved,
    ProviderResolved,
    ExecutionStarted,
    ResultValidationStarted,
    ArtifactReviewStarted,
    Completed,
    Rejected,
    Failed,
    Cancelled,
    TimedOut,
    Quarantined
}

internal enum Phase35BOutcomeStatus { Completed, Rejected, Failed, Cancelled, TimedOut, Quarantined }
internal enum Phase35BArtifactDisposition { Accepted, Rejected, Quarantined }

internal sealed record Phase35BTimeoutPolicy(TimeSpan Timeout)
{
    internal bool IsValid => Timeout > TimeSpan.Zero && Timeout <= TimeSpan.FromHours(1);
}

internal sealed record Phase35BProviderRegistration(
    Phase35AProviderProfile Profile,
    IPhase35BProviderAdapter? Adapter);

internal sealed record Phase35BAdapterValidation(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    internal bool IsValid => Errors.Count == 0;
}

internal sealed record Phase35BExecutionPlan(
    string PlanId,
    IReadOnlyList<string> Steps);

internal sealed record Phase35BOfflineExecutionResult(
    Phase35AResult Result,
    Phase35AArtifact Artifact);

internal sealed record Phase35BExecutionContext(
    Phase35ARequest Request,
    Phase35BSession Session);

internal sealed record Phase35BExecutionInput(
    Phase35ARequest Request,
    Phase35AAuthorization Authorization,
    Phase35AExecutionPolicy Policy,
    Phase35AReadinessResult Readiness,
    Phase35BTimeoutPolicy TimeoutPolicy);

internal sealed record Phase35BSession(
    string SessionId,
    string RequestId,
    string RequestHash,
    string ProviderId,
    string ProviderVersion,
    Phase35ACapability Capability,
    string PolicyHash,
    Phase35AAuthorization Authorization,
    Phase35AReadinessResult Readiness,
    Phase35BTimeoutPolicy TimeoutPolicy,
    DateTimeOffset CreatedAt,
    Phase35BRuntimeState State,
    IReadOnlyList<Phase35BLifecycleEntry> Lifecycle)
{
    internal Phase35BSession Advance(Phase35BRuntimeState next, Phase35BRuntimeEvent lifecycleEvent)
    {
        var actual = new Phase35BLifecycleCoordinator().Transition(State, lifecycleEvent);
        if (actual != next) throw new Phase35BContractException("Requested runtime state does not match lifecycle transition.");
        return this with { State = next, Lifecycle = Lifecycle.Append(new Phase35BLifecycleEntry(next, lifecycleEvent, CreatedAt)).ToArray() };
    }
}

internal sealed record Phase35BLifecycleEntry(
    Phase35BRuntimeState State,
    Phase35BRuntimeEvent Event,
    DateTimeOffset At);

internal sealed record Phase35BAuthorizationDecision(
    bool IsAllowed,
    IReadOnlyList<string> Reasons);

internal sealed record Phase35BReadinessDecision(
    bool IsReady,
    IReadOnlyList<string> Reasons);

internal sealed record Phase35BProviderResolution(
    bool IsSuccess,
    string Reason,
    IPhase35BProviderAdapter? Adapter,
    Phase35AProviderProfile? Profile);

internal sealed record Phase35BValidationStageResult(
    string Stage,
    bool IsValid,
    IReadOnlyList<string> Errors);

internal sealed record Phase35BValidationResult(
    IReadOnlyList<Phase35BValidationStageResult> Stages)
{
    internal bool IsValid => Stages.All(stage => stage.IsValid);
}

internal sealed record Phase35BArtifactReview(
    Phase35BArtifactDisposition Disposition,
    IReadOnlyList<string> Reasons,
    Phase35AArtifact Artifact);

internal sealed record Phase35BAuditEvent(
    string Name,
    string Outcome,
    DateTimeOffset At,
    IReadOnlyList<string> Details);

internal sealed record Phase35BAuditRecord(
    string SchemaVersion,
    string AuditId,
    string RequestId,
    string RequestHash,
    string ProviderId,
    string PolicyHash,
    Phase35AAuthorizationStatus Authorization,
    Phase35AReadiness Readiness,
    IReadOnlyList<Phase35BLifecycleEntry> Lifecycle,
    IReadOnlyList<Phase35BValidationStageResult> Validation,
    Phase35BArtifactDisposition? ArtifactDisposition,
    Phase35BOutcomeStatus Outcome,
    string? FailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset CompletedAt);

internal sealed record Phase35BDiagnosticEvent(
    string Name,
    string Outcome,
    string RequestId,
    string ProviderId,
    IReadOnlyList<string> Details,
    DateTimeOffset At);

internal sealed record Phase35BExecutionOutcome(
    Phase35BOutcomeStatus Status,
    Phase35BSession? Session,
    Phase35AResult? Result,
    Phase35BArtifactReview? Artifact,
    Phase35BAuditRecord? Audit,
    Phase35AFailure? Failure);

internal sealed class Phase35BContractException(string message) : InvalidOperationException(message);
