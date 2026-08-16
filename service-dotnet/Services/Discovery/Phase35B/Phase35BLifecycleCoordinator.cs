namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35BLifecycleCoordinator
{
    internal Phase35BRuntimeState Transition(Phase35BRuntimeState state, Phase35BRuntimeEvent lifecycleEvent) => (state, lifecycleEvent) switch
    {
        (Phase35BRuntimeState.Created, Phase35BRuntimeEvent.Created) => Phase35BRuntimeState.Created,
        (Phase35BRuntimeState.Created, Phase35BRuntimeEvent.Validated) => Phase35BRuntimeState.Validated,
        (Phase35BRuntimeState.Validated, Phase35BRuntimeEvent.AuthorizationApproved) => Phase35BRuntimeState.Authorized,
        (Phase35BRuntimeState.Authorized, Phase35BRuntimeEvent.ReadinessApproved) => Phase35BRuntimeState.Ready,
        (Phase35BRuntimeState.Ready, Phase35BRuntimeEvent.ProviderResolved) => Phase35BRuntimeState.ProviderResolved,
        (Phase35BRuntimeState.ProviderResolved, Phase35BRuntimeEvent.ExecutionStarted) => Phase35BRuntimeState.Executing,
        (Phase35BRuntimeState.Executing, Phase35BRuntimeEvent.ResultValidationStarted) => Phase35BRuntimeState.ValidatingResult,
        (Phase35BRuntimeState.ValidatingResult, Phase35BRuntimeEvent.ArtifactReviewStarted) => Phase35BRuntimeState.ReviewingArtifact,
        (Phase35BRuntimeState.ReviewingArtifact, Phase35BRuntimeEvent.Completed) => Phase35BRuntimeState.Completed,
        (Phase35BRuntimeState.Created or Phase35BRuntimeState.Validated or Phase35BRuntimeState.Authorized or Phase35BRuntimeState.Ready or Phase35BRuntimeState.ProviderResolved or Phase35BRuntimeState.Executing or Phase35BRuntimeState.ValidatingResult or Phase35BRuntimeState.ReviewingArtifact, Phase35BRuntimeEvent.Rejected) => Phase35BRuntimeState.Rejected,
        (Phase35BRuntimeState.Created or Phase35BRuntimeState.Validated or Phase35BRuntimeState.Authorized or Phase35BRuntimeState.Ready or Phase35BRuntimeState.ProviderResolved or Phase35BRuntimeState.Executing or Phase35BRuntimeState.ValidatingResult or Phase35BRuntimeState.ReviewingArtifact, Phase35BRuntimeEvent.Failed) => Phase35BRuntimeState.Failed,
        (Phase35BRuntimeState.Executing, Phase35BRuntimeEvent.Cancelled) => Phase35BRuntimeState.Cancelled,
        (Phase35BRuntimeState.Executing, Phase35BRuntimeEvent.TimedOut) => Phase35BRuntimeState.TimedOut,
        (Phase35BRuntimeState.ReviewingArtifact, Phase35BRuntimeEvent.Quarantined) => Phase35BRuntimeState.Quarantined,
        _ => throw new Phase35BContractException($"Invalid Phase 35B lifecycle transition: {state} + {lifecycleEvent}.")
    };
}
