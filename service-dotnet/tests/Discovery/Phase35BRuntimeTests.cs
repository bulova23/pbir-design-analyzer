using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35BRuntimeTests
{
    [Fact]
    public void ProductionCatalog_RemainsUnavailable()
    {
        Assert.NotEmpty(Phase35BProductionCatalog.Registrations);
        Assert.All(Phase35BProductionCatalog.Registrations, registration =>
        {
            Assert.Null(registration.Adapter);
            Assert.NotEqual(Phase35AExecutionClass.Executable, registration.Profile.ExecutionClass);
        });
    }

    [Fact]
    public void Resolution_RequiresOneExactMatchingProvider()
    {
        var profile = CreateProfile("fake.provider");
        var adapter = new FakeAdapter("fake.provider", [Phase35ACapability.PbirGeneration]);
        var registry = new Phase35BProviderRegistry([
            new Phase35BProviderRegistration(profile, adapter)
        ]);
        var request = CreateRequest(profile.ProviderId, Phase35ACapability.PbirGeneration);
        var authorization = Approved(request, Phase35AExecutionPolicyFor(profile, Phase35ACapability.PbirGeneration));
        var policy = Phase35AExecutionPolicyFor(profile, Phase35ACapability.PbirGeneration);
        var readiness = Ready();

        var result = new Phase35BProviderResolutionService().Resolve(request, authorization, policy, readiness, registry);

        Assert.True(result.IsSuccess);
        Assert.Same(adapter, result.Adapter);
    }

    [Fact]
    public void Resolution_FailsClosedForAmbiguousRegistrations()
    {
        var profile = CreateProfile("fake.provider");
        var registrations = new[]
        {
            new Phase35BProviderRegistration(profile, new FakeAdapter(profile.ProviderId, [Phase35ACapability.PbirGeneration])),
            new Phase35BProviderRegistration(profile, new FakeAdapter(profile.ProviderId, [Phase35ACapability.PbirGeneration]))
        };
        var request = CreateRequest(profile.ProviderId, Phase35ACapability.PbirGeneration);
        var policy = Phase35AExecutionPolicyFor(profile, Phase35ACapability.PbirGeneration);

        var result = new Phase35BProviderResolutionService().Resolve(
            request, Approved(request, policy), policy, Ready(), new Phase35BProviderRegistry(registrations));

        Assert.False(result.IsSuccess);
        Assert.Contains("ambiguous", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolution_RejectsAdapterRequestValidationFailure()
    {
        var profile = CreateProfile("fake.provider");
        var request = CreateRequest(profile.ProviderId, Phase35ACapability.PbirGeneration);
        var policy = Phase35AExecutionPolicyFor(profile, Phase35ACapability.PbirGeneration);
        var adapter = new FakeAdapter(profile.ProviderId, [Phase35ACapability.PbirGeneration]) { ValidationErrors = ["adapter rejected request"] };

        var result = new Phase35BProviderResolutionService().Resolve(
            request, Approved(request, policy), policy, Ready(),
            new Phase35BProviderRegistry([new Phase35BProviderRegistration(profile, adapter)]));

        Assert.False(result.IsSuccess);
        Assert.Contains("adapter rejected request", result.Reason);
    }

    [Fact]
    public void AuthorizationGate_DeniesMismatchedScope()
    {
        var profile = CreateProfile("fake.provider");
        var request = CreateRequest(profile.ProviderId, Phase35ACapability.PbirGeneration);
        var policy = Phase35AExecutionPolicyFor(profile, Phase35ACapability.PbirGeneration);
        var authorization = new Phase35AAuthorization(
            Phase35AContracts.AuthorizationV1,
            Phase35AAuthorizationStatus.Approved,
            request.RequestId,
            "different.provider",
            request.RequiredCapabilities,
            request.ArtifactKind,
            request.PolicyHash);

        var result = new Phase35BAuthorizationGate().Validate(request, profile, authorization, policy);

        Assert.False(result.IsAllowed);
        Assert.Contains(result.Reasons, reason => reason.Contains("provider", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LifecycleCoordinator_RejectsIllegalTransitions()
    {
        var coordinator = new Phase35BLifecycleCoordinator();

        Assert.Throws<Phase35BContractException>(() =>
            coordinator.Transition(Phase35BRuntimeState.Created, Phase35BRuntimeEvent.Completed));
        Assert.Equal(
            Phase35BRuntimeState.Validated,
            coordinator.Transition(Phase35BRuntimeState.Created, Phase35BRuntimeEvent.Validated));
    }

    [Fact]
    public void SessionFactory_CreatesReplacementRecordsWithoutMutatingOriginal()
    {
        var profile = CreateProfile("fake.provider");
        var request = CreateRequest(profile.ProviderId, Phase35ACapability.PbirGeneration);
        var policy = Phase35AExecutionPolicyFor(profile, Phase35ACapability.PbirGeneration);
        var session = new Phase35BSessionFactory(() => new DateTimeOffset(2026, 8, 12, 13, 0, 0, TimeSpan.Zero))
            .Create(request, profile, Approved(request, policy), Ready(), policy, new Phase35BTimeoutPolicy(TimeSpan.FromSeconds(5)));

        var next = session.Advance(Phase35BRuntimeState.Validated, Phase35BRuntimeEvent.Validated);

        Assert.Equal(Phase35BRuntimeState.Created, session.State);
        Assert.Equal(Phase35BRuntimeState.Validated, next.State);
        Assert.NotEqual(session.SessionId, string.Empty);
        Assert.Equal(session.RequestHash, next.RequestHash);
        Assert.Equal(request.PolicyHash, session.PolicyHash);
    }

    [Fact]
    public async Task Orchestrator_CompletesOnlyThroughOfflineFakeAdapter()
    {
        var profile = CreateProfile("fake.provider");
        var policy = Phase35AExecutionPolicyFor(profile, Phase35ACapability.PbirGeneration);
        var request = CreateRequest(profile.ProviderId, Phase35ACapability.PbirGeneration);
        var adapter = new FakeAdapter(profile.ProviderId, [Phase35ACapability.PbirGeneration]);
        var registry = new Phase35BProviderRegistry([new Phase35BProviderRegistration(profile, adapter)]);
        var orchestrator = Phase35BOrchestrator.CreateForTests(registry, () => new DateTimeOffset(2026, 8, 12, 13, 0, 0, TimeSpan.Zero));

        var outcome = await orchestrator.ExecuteAsync(new Phase35BExecutionInput(
            request, Approved(request, policy), policy, Ready(), new Phase35BTimeoutPolicy(TimeSpan.FromSeconds(5))));

        Assert.Equal(Phase35BOutcomeStatus.Completed, outcome.Status);
        Assert.Equal(Phase35BRuntimeState.Completed, outcome.Session!.State);
        Assert.Equal(Phase35BArtifactDisposition.Accepted, outcome.Artifact!.Disposition);
        Assert.NotNull(outcome.Audit);
        Assert.Contains(outcome.Audit!.Lifecycle, item => item.State == Phase35BRuntimeState.Executing);
        Assert.True(adapter.OfflineExecutionCalled);
    }

    [Fact]
    public async Task Orchestrator_DoesNotInvokeAdapterWhenReadinessFails()
    {
        var profile = CreateProfile("fake.provider");
        var policy = Phase35AExecutionPolicyFor(profile, Phase35ACapability.PbirGeneration);
        var request = CreateRequest(profile.ProviderId, Phase35ACapability.PbirGeneration);
        var adapter = new FakeAdapter(profile.ProviderId, [Phase35ACapability.PbirGeneration]);
        var orchestrator = Phase35BOrchestrator.CreateForTests(
            new Phase35BProviderRegistry([new Phase35BProviderRegistration(profile, adapter)]),
            () => new DateTimeOffset(2026, 8, 12, 13, 0, 0, TimeSpan.Zero));

        var outcome = await orchestrator.ExecuteAsync(new Phase35BExecutionInput(
            request, Approved(request, policy), policy, new Phase35AReadinessResult(
                Phase35AContracts.ReadinessV1, Phase35AReadiness.Blocked, ["test readiness failure"]),
            new Phase35BTimeoutPolicy(TimeSpan.FromSeconds(5))));

        Assert.Equal(Phase35BOutcomeStatus.Rejected, outcome.Status);
        Assert.False(adapter.OfflineExecutionCalled);
    }

    [Fact]
    public async Task Orchestrator_ClassifiesCallerCancellationSeparately()
    {
        var profile = CreateProfile("fake.provider");
        var policy = Phase35AExecutionPolicyFor(profile, Phase35ACapability.PbirGeneration);
        var request = CreateRequest(profile.ProviderId, Phase35ACapability.PbirGeneration);
        using var cancellation = new CancellationTokenSource();
        var adapter = new FakeAdapter(profile.ProviderId, [Phase35ACapability.PbirGeneration]) { Delay = TimeSpan.FromSeconds(1) };
        var orchestrator = Phase35BOrchestrator.CreateForTests(
            new Phase35BProviderRegistry([new Phase35BProviderRegistration(profile, adapter)]),
            () => DateTimeOffset.UtcNow);
        cancellation.Cancel();

        var outcome = await orchestrator.ExecuteAsync(new Phase35BExecutionInput(
            request, Approved(request, policy), policy, Ready(), new Phase35BTimeoutPolicy(TimeSpan.FromSeconds(5))), cancellation.Token);

        Assert.Equal(Phase35BOutcomeStatus.Cancelled, outcome.Status);
    }

    [Fact]
    public async Task Orchestrator_ClassifiesPolicyTimeoutSeparately()
    {
        var profile = CreateProfile("fake.provider");
        var policy = Phase35AExecutionPolicyFor(profile, Phase35ACapability.PbirGeneration);
        var request = CreateRequest(profile.ProviderId, Phase35ACapability.PbirGeneration);
        var adapter = new FakeAdapter(profile.ProviderId, [Phase35ACapability.PbirGeneration]) { Delay = TimeSpan.FromMilliseconds(100) };
        var orchestrator = Phase35BOrchestrator.CreateForTests(
            new Phase35BProviderRegistry([new Phase35BProviderRegistration(profile, adapter)]),
            () => DateTimeOffset.UtcNow);

        var outcome = await orchestrator.ExecuteAsync(new Phase35BExecutionInput(
            request, Approved(request, policy), policy, Ready(), new Phase35BTimeoutPolicy(TimeSpan.FromMilliseconds(1))));

        Assert.Equal(Phase35BOutcomeStatus.TimedOut, outcome.Status);
        Assert.Equal("execution-timeout", outcome.Audit!.FailureCode);
    }

    [Fact]
    public void ArtifactIntake_QuarantinesUnsafeArtifact()
    {
        var request = CreateRequest("fake.provider", Phase35ACapability.PbirGeneration);
        var result = new Phase35AResult(Phase35AContracts.ResultV1, "result-1", request.RequestId, Phase35AResultStatus.Accepted, []);
        var artifact = new Phase35AArtifact(
            Phase35AContracts.ArtifactV1, "artifact-1", Phase35AArtifactKind.PbirReport, request.RequestId, request.ProviderId,
            Phase35AContracts.ContractVersion, new string('a', 64), result.ResultId, Phase35ALineage.From(request, result),
            Phase35AValidationStatus.Valid,
            new Phase35AQuarantine(Phase35AQuarantineReason.UnsafeContent, "test", false), Phase35ARedaction.None);

        var disposition = new Phase35BArtifactIntakeService().Review(artifact, result);

        Assert.Equal(Phase35BArtifactDisposition.Quarantined, disposition.Disposition);
    }

    [Fact]
    public void ArtifactIntake_RejectsBrokenLineage()
    {
        var request = CreateRequest("fake.provider", Phase35ACapability.PbirGeneration);
        var result = new Phase35AResult(Phase35AContracts.ResultV1, "result-1", request.RequestId, Phase35AResultStatus.Accepted, []);
        var artifact = new Phase35AArtifact(
            Phase35AContracts.ArtifactV1, "artifact-1", Phase35AArtifactKind.PbirReport, request.RequestId, request.ProviderId,
            Phase35AContracts.ContractVersion, new string('a', 64), "different-result", Phase35ALineage.From(request, result),
            Phase35AValidationStatus.Valid, Phase35AQuarantine.None, Phase35ARedaction.None);

        var disposition = new Phase35BArtifactIntakeService().Review(artifact, result);

        Assert.Equal(Phase35BArtifactDisposition.Rejected, disposition.Disposition);
    }

    private static Phase35AProviderProfile CreateProfile(string providerId) => new(
        Phase35AContracts.ProviderProfileV1, providerId, providerId, Phase35AProviderCategory.OfflineTest,
        Phase35AExecutionClass.Executable, Phase35ATrustClassification.TrustedContract,
        [Phase35ACapability.PbirGeneration], [Phase35AArtifactKind.PbirReport], []);

    private static Phase35ARequest CreateRequest(string providerId, Phase35ACapability capability) => new(
        Phase35AContracts.RequestV1, "request-1", "intent-1", ["input-1"], [capability], Phase35AArtifactKind.PbirReport,
        providerId, new string('b', 64), new string('c', 64));

    private static Phase35AExecutionPolicy Phase35AExecutionPolicyFor(Phase35AProviderProfile profile, Phase35ACapability capability) => new(
        Phase35AContracts.PolicyV1, profile.ProviderId, [capability], [Phase35AArtifactKind.PbirReport], true, false,
        new Phase35ARetryPolicy(Phase35AContracts.RetryV1, 1, []), false, true, true);

    private static Phase35AAuthorization Approved(Phase35ARequest request, Phase35AExecutionPolicy policy) => new(
        Phase35AContracts.AuthorizationV1, Phase35AAuthorizationStatus.Approved, request.RequestId, request.ProviderId,
        request.RequiredCapabilities, request.ArtifactKind, request.PolicyHash);

    private static Phase35AReadinessResult Ready() => new(Phase35AContracts.ReadinessV1, Phase35AReadiness.ReadyForExecution, []);

    private sealed class FakeAdapter(string providerId, IReadOnlyList<Phase35ACapability> capabilities) : IPhase35BProviderAdapter
    {
        public string ProviderId { get; } = providerId;
        public string AdapterVersion => "fake/v1";
        public IReadOnlyList<Phase35ACapability> Capabilities { get; } = capabilities;
        public TimeSpan Delay { get; init; }
        public bool OfflineExecutionCalled { get; private set; }
        public IReadOnlyList<string> ValidationErrors { get; init; } = [];

        public Phase35BAdapterValidation ValidateRequest(Phase35ARequest request) =>
            new(request.ProviderId == ProviderId ? ValidationErrors : ["provider identity mismatch"], []);

        public Phase35AReadinessResult DeclareReadiness(Phase35ARequest request) => Ready();

        public Phase35BExecutionPlan DescribeExecutionPlan(Phase35ARequest request) =>
            new("offline-fake", ["returns deterministic contract records only"]);

        public async Task<Phase35BOfflineExecutionResult> ExecuteOfflineAsync(Phase35BExecutionContext context, CancellationToken cancellationToken)
        {
            OfflineExecutionCalled = true;
            if (Delay > TimeSpan.Zero) await Task.Delay(Delay, cancellationToken);
            var result = new Phase35AResult(Phase35AContracts.ResultV1, "result-1", context.Session.RequestId, Phase35AResultStatus.Accepted, []);
            var artifact = new Phase35AArtifact(
                Phase35AContracts.ArtifactV1, "artifact-1", Phase35AArtifactKind.PbirReport, context.Session.RequestId,
                ProviderId, Phase35AContracts.ContractVersion, new string('a', 64), result.ResultId, Phase35ALineage.From(context.Request, result),
                Phase35AValidationStatus.Valid, Phase35AQuarantine.None, Phase35ARedaction.None);
            return new(result, artifact);
        }
    }
}
