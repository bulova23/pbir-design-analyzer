using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35CRuntimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TrustEvaluator_ApprovesMatchingFreshAttestation()
    {
        var profile = Profile("future.fake", Phase35AExecutionClass.Executable);
        var identity = new Phase35CProviderIdentity(profile.ProviderId, "1.0.0", "impl:fake", [Phase35ACapability.PbirGeneration], Phase35AExecutionClass.Executable);
        var versions = Versions();
        var attestation = Attestation(identity, versions, Now.AddHours(1));

        var result = new Phase35CProviderTrustEvaluator(() => Now).Evaluate(profile, identity, attestation, versions);

        Assert.True(result.IsTrusted, result.Reason.ToString());
        Assert.Equal(Phase35CTrustReason.None, result.Reason);
    }

    [Fact]
    public void TrustEvaluator_RejectsMissingOrStaleAttestation()
    {
        var profile = Profile("future.fake", Phase35AExecutionClass.Executable);
        var identity = new Phase35CProviderIdentity(profile.ProviderId, "1.0.0", "impl:fake", [Phase35ACapability.PbirGeneration], Phase35AExecutionClass.Executable);
        var versions = Versions();

        Assert.Equal(Phase35CTrustReason.AttestationMissing,
            new Phase35CProviderTrustEvaluator(() => Now).Evaluate(profile, identity, null, versions).Reason);
        Assert.Equal(Phase35CTrustReason.TrustExpired,
            new Phase35CProviderTrustEvaluator(() => Now).Evaluate(profile, identity, Attestation(identity, versions, Now.AddMinutes(-1)), versions).Reason);
    }

    [Fact]
    public void SandboxPolicyEvaluator_RequiresExplicitContainment()
    {
        var approved = new Phase35CSandboxPolicy("sandbox/v1", Phase35CProcessModel.Isolated, Phase35CNetworkPolicy.Denied,
            Phase35CFilesystemPolicy.DedicatedOutputOnly, Phase35CEnvironmentPolicy.Allowlisted, Phase35CCredentialAccessPolicy.GrantOnly,
            TimeSpan.FromMinutes(5), 256, 1, 1, 1024, false, []);
        var result = new Phase35CSandboxPolicyEvaluator().Evaluate(approved, approved);

        Assert.True(result.IsAllowed);
        Assert.False(new Phase35CSandboxPolicyEvaluator().Evaluate(approved with { Network = Phase35CNetworkPolicy.Unrestricted }, approved).IsAllowed);
    }

    [Fact]
    public void CredentialBoundary_RejectsRawSecretAndMismatchedGrant()
    {
        var boundary = new Phase35CCredentialBoundaryPolicy();
        var grant = new Phase35CCredentialGrant("grant:1", Phase35CCredentialClass.OpaqueReference, "provider", Phase35ACapability.PbirGeneration, "scope:report", Now.AddHours(1));

        Assert.True(boundary.Evaluate(grant, "provider", Phase35ACapability.PbirGeneration, "scope:report", Now).IsAllowed);
        Assert.Equal(Phase35CCredentialReason.RawSecretMaterial, boundary.ValidateSerializedAuditValue("secret-value").Reason);
        Assert.Equal(Phase35CCredentialReason.ScopeMismatch, boundary.Evaluate(grant, "provider", Phase35ACapability.PbirGeneration, "scope:other", Now).Reason);
    }

    [Fact]
    public void ReplayProtection_DistinguishesDuplicateFromAuthorizedRetry()
    {
        var replay = new Phase35CReplayProtectionService();
        var first = new Phase35CExecutionIdentity("execution:1", "session:1", "request-hash", "nonce:1");

        Assert.True(replay.Accept(first, false).IsAccepted);
        Assert.Equal(Phase35CReplayReason.DuplicateExecution, replay.Accept(first, false).Reason);
        Assert.True(replay.Accept(first, true).IsAccepted);
        Assert.Equal(Phase35CReplayReason.ModifiedRequest, replay.Accept(first with { RequestHash = "different" }, false).Reason);
    }

    [Fact]
    public void DurableAuditStore_DetectsMutationAndSequenceGap()
    {
        var store = new Phase35CDurableAuditStore(() => Now);
        store.Append(new Phase35CAuditEvent("session:1", "provider", "started", "request-hash", "outcome:started"));
        store.Append(new Phase35CAuditEvent("session:1", "provider", "completed", "request-hash", "outcome:completed"));

        Assert.True(store.ValidateChain().IsValid);
        var tampered = store.Records.Select(record => record.Sequence == 2 ? record with { Event = record.Event with { Outcome = "changed" } } : record).ToArray();
        Assert.False(store.ValidateChain(tampered).IsValid);
        Assert.False(store.ValidateChain(store.Records.Skip(1).ToArray()).IsValid);
    }

    [Fact]
    public void ArtifactSafetyPipeline_UnknownScannerFailsClosedAndSuspiciousIsQuarantined()
    {
        var descriptor = new Phase35CArtifactDescriptor("artifact:1", "request:1", "provider", Phase35AArtifactKind.PbirReport, "hash:1", 10, true, true);
        var pipeline = new Phase35CArtifactSafetyPipeline(new Phase35CFakeArtifactScanner(Phase35CScannerClassification.Unknown));

        Assert.Equal(Phase35CArtifactDisposition.Rejected, pipeline.Evaluate(descriptor, new Phase35CArtifactSafetyPolicy(1, 100, [Phase35AArtifactKind.PbirReport])).Disposition);
        Assert.Equal(Phase35CArtifactDisposition.Quarantined,
            new Phase35CArtifactSafetyPipeline(new Phase35CFakeArtifactScanner(Phase35CScannerClassification.Suspicious))
                .Evaluate(descriptor, new Phase35CArtifactSafetyPolicy(1, 100, [Phase35AArtifactKind.PbirReport])).Disposition);
        Assert.Equal(Phase35CArtifactDisposition.Accepted,
            new Phase35CArtifactSafetyPipeline(new Phase35CFakeArtifactScanner(Phase35CScannerClassification.Clean))
                .Evaluate(descriptor, new Phase35CArtifactSafetyPolicy(1, 100, [Phase35AArtifactKind.PbirReport])).Disposition);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ArtifactSafetyPipeline_RejectsUnsafeScannerOutcomes(int classificationValue)
    {
        var artifact = new Phase35CArtifactDescriptor("artifact:1", "request:1", "provider", Phase35AArtifactKind.PbirReport, "hash:1", 10, true, true);

        var result = new Phase35CArtifactSafetyPipeline(new Phase35CFakeArtifactScanner((Phase35CScannerClassification)classificationValue))
            .Evaluate(artifact, new Phase35CArtifactSafetyPolicy(1, 100, [Phase35AArtifactKind.PbirReport]));

        Assert.Equal(Phase35CArtifactDisposition.Rejected, result.Disposition);
    }

    [Fact]
    public void ResourcePolicyEvaluator_RejectsUnboundedPolicy()
    {
        var result = new Phase35CResourcePolicyEvaluator().Evaluate(new Phase35CResourcePolicy(TimeSpan.Zero, 0, 0, 0, 0, 0));

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void OutputCorpus_EvaluatesRequiredAndForbiddenProperties()
    {
        var fixture = new Phase35COutputCorpusFixture("clean", "corpus/v1", ["kind", "hash"], ["secret"], Phase35CExpectedValidationOutcome.Valid, ["future.fake"]);

        Assert.True(new Phase35COutputValidationEvaluator().Evaluate(fixture, ["kind", "hash"]).IsValid);
        Assert.False(new Phase35COutputValidationEvaluator().Evaluate(fixture, ["kind", "secret"]).IsValid);
    }

    [Fact]
    public void ActivationGate_DeniesProductionCatalogAndMissingEveryRequiredGate()
    {
        var production = new Phase35CActivationGate(() => Now).EvaluateProduction(Phase35BProductionCatalog.Registrations);

        Assert.False(production.IsEligible);
        Assert.Contains(Phase35CActivationDenialReason.ProviderUnavailable, production.DenialReasons);
        Assert.Contains(Phase35CActivationDenialReason.TrustMissing, production.DenialReasons);
    }

    [Fact]
    public void ActivationGate_AllowsOnlyAFullyAssuredSyntheticProvider()
    {
        var versions = Versions();
        var decision = new Phase35CActivationGate(() => Now).Evaluate(new Phase35CActivationInput(
            Profile("future.fake", Phase35AExecutionClass.Executable),
            new Phase35AAuthorization(Phase35AContracts.AuthorizationV1, Phase35AAuthorizationStatus.Approved, "request:1", "future.fake", [Phase35ACapability.PbirGeneration], Phase35AArtifactKind.PbirReport, "policy:1"),
            new Phase35AReadinessResult(Phase35AContracts.ReadinessV1, Phase35AReadiness.ReadyForExecution, []),
            new(true, Phase35CTrustReason.None, Now, Now.AddHours(1)),
            new(true, []), new(true, Phase35CCredentialReason.None), new(true, []), new(true, []), new(true, []), true, true, true, versions));

        Assert.True(decision.IsEligible);
        Assert.Empty(decision.DenialReasons);
    }

    [Fact]
    public void ConformanceEvaluator_DetectsIdentityAndCapabilityViolations()
    {
        var request = new Phase35ARequest(Phase35AContracts.RequestV1, "request:1", "intent", ["input:1"], [Phase35ACapability.PbirGeneration], Phase35AArtifactKind.PbirReport, "future.fake", "hash:input", "hash:policy");
        var result = new Phase35CConformanceEvaluator().Evaluate(new ConformanceAdapter(), new Phase35CProviderIdentity("future.fake", "1.0", "impl", [Phase35ACapability.PbirGeneration], Phase35AExecutionClass.Executable), request);

        Assert.True(result.IsConformant);
        var broken = new Phase35CConformanceEvaluator().Evaluate(new ConformanceAdapter("wrong", []), new Phase35CProviderIdentity("future.fake", "1.0", "impl", [Phase35ACapability.PbirGeneration], Phase35AExecutionClass.Executable), request);
        Assert.False(broken.IsConformant);
        Assert.Contains(Phase35CConformanceViolation.IdentityUnstable, broken.Violations);
        Assert.Contains(Phase35CConformanceViolation.CapabilityMismatch, broken.Violations);
    }

    [Fact]
    public void ConformanceEvaluator_DetectsRuntimeAndSafetyViolations()
    {
        var request = new Phase35ARequest(Phase35AContracts.RequestV1, "request:1", "intent", ["input:1"], [Phase35ACapability.PbirGeneration], Phase35AArtifactKind.PbirReport, "future.fake", "hash:input", "hash:policy");
        var evidence = new Phase35CConformanceEvidence(false, false, false, false, false, false);

        var result = new Phase35CConformanceEvaluator().Evaluate(new ConformanceAdapter(), new Phase35CProviderIdentity("future.fake", "1.0", "impl", [Phase35ACapability.PbirGeneration], Phase35AExecutionClass.Executable), request, evidence);

        Assert.False(result.IsConformant);
        Assert.Contains(Phase35CConformanceViolation.CancellationIgnored, result.Violations);
        Assert.Contains(Phase35CConformanceViolation.FailureMappingInvalid, result.Violations);
        Assert.Contains(Phase35CConformanceViolation.ArtifactLineageInvalid, result.Violations);
        Assert.Contains(Phase35CConformanceViolation.ArtifactUnclassified, result.Violations);
        Assert.Contains(Phase35CConformanceViolation.AuditNotEmitted, result.Violations);
        Assert.Contains(Phase35CConformanceViolation.SecretLeakage, result.Violations);
    }

    [Fact]
    public void PolicyVersions_AreRecordedAndChangingPolicyInvalidatesAttestation()
    {
        var profile = Profile("future.fake", Phase35AExecutionClass.Executable);
        var identity = new Phase35CProviderIdentity(profile.ProviderId, "1.0.0", "impl:fake", [Phase35ACapability.PbirGeneration], Phase35AExecutionClass.Executable);
        var versions = Versions();
        var result = new Phase35CProviderTrustEvaluator(() => Now).Evaluate(profile, identity, Attestation(identity, versions, Now.AddHours(1)), versions with { Sandbox = "sandbox/v2" });

        Assert.Equal(Phase35CTrustReason.PolicyBindingMismatch, result.Reason);
    }

    [Fact]
    public void SecretMaterial_CannotBeSerializedIntoAuditRecords()
    {
        var eventRecord = new Phase35CAuditEvent("session:1", "provider", "decision", "request-hash", "secret-value");
        var json = JsonSerializer.Serialize(eventRecord);

        Assert.DoesNotContain("secret-value", json, StringComparison.Ordinal);
    }

    private static Phase35CPolicyVersions Versions() => new("execution/v1", "sandbox/v1", "credential/v1", "artifact/v1", "conformance/v1", "corpus/v1");

    private static Phase35CProviderAttestation Attestation(Phase35CProviderIdentity identity, Phase35CPolicyVersions versions, DateTimeOffset expires) =>
        new(Phase35CContracts.TrustV1, identity.ProviderId, identity.Version, identity.ImplementationIdentity, identity.Capabilities, identity.ExecutionClass, "sandbox/v1", versions, Now, expires);

    private static Phase35AProviderProfile Profile(string id, Phase35AExecutionClass executionClass) =>
        new(Phase35AContracts.ProviderProfileV1, id, id, Phase35AProviderCategory.OfflineTest, executionClass, Phase35ATrustClassification.Untrusted,
            [Phase35ACapability.PbirGeneration], [Phase35AArtifactKind.PbirReport], [Phase35AReadinessRequirement.ExplicitExecutableRegistration]);

    private sealed class ConformanceAdapter(string providerId = "future.fake", IReadOnlyList<Phase35ACapability>? capabilities = null) : IPhase35BProviderAdapter
    {
        public string ProviderId { get; } = providerId;
        public string AdapterVersion => "fake/v1";
        public IReadOnlyList<Phase35ACapability> Capabilities { get; } = capabilities ?? [Phase35ACapability.PbirGeneration];
        public Phase35BAdapterValidation ValidateRequest(Phase35ARequest request) => new([], []);
        public Phase35AReadinessResult DeclareReadiness(Phase35ARequest request) => new(Phase35AContracts.ReadinessV1, Phase35AReadiness.ReadyForExecution, []);
        public Phase35BExecutionPlan DescribeExecutionPlan(Phase35ARequest request) => new("plan:fake", ["offline"]);
        public Task<Phase35BOfflineExecutionResult> ExecuteOfflineAsync(Phase35BExecutionContext context, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
