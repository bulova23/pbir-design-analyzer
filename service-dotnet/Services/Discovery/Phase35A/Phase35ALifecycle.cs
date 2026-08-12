namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35ALifecycle
{
    internal static Phase35ALifecycleState Transition(Phase35ALifecycleState state, Phase35ALifecycleEvent lifecycleEvent) => (state, lifecycleEvent) switch
    {
        (Phase35ALifecycleState.Requested, Phase35ALifecycleEvent.AuthorizationApproved) => Phase35ALifecycleState.Authorized,
        (Phase35ALifecycleState.Authorized, Phase35ALifecycleEvent.RequestAccepted) => Phase35ALifecycleState.Accepted,
        (Phase35ALifecycleState.Accepted, Phase35ALifecycleEvent.ExecutionStarted) => Phase35ALifecycleState.Running,
        (Phase35ALifecycleState.Running, Phase35ALifecycleEvent.ExecutionCompleted) => Phase35ALifecycleState.Completed,
        (Phase35ALifecycleState.Running, Phase35ALifecycleEvent.ExecutionFailed) => Phase35ALifecycleState.Failed,
        (Phase35ALifecycleState.Running, Phase35ALifecycleEvent.OutputQuarantined) => Phase35ALifecycleState.Quarantined,
        (Phase35ALifecycleState.Requested, Phase35ALifecycleEvent.RequestRejected) => Phase35ALifecycleState.Rejected,
        _ => throw new Phase35AContractException($"Invalid Phase 35A lifecycle transition: {state} + {lifecycleEvent}.")
    };
}

