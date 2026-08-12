using System.Security.Cryptography;
using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35DRuntimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PackageIdentity_IsStableAndChangesWhenApprovedMetadataChanges()
    {
        var resolver = new Phase35DPackageIdentityResolver();
        var first = resolver.Resolve("future.fake", "1.0.0", "impl:fake", Metadata());
        var second = resolver.Resolve("future.fake", "1.0.0", "impl:fake", Metadata());

        Assert.Equal(first, second);
        Assert.NotEqual(first.IdentityHash, resolver.Resolve("future.fake", "1.0.0", "impl:fake", Metadata() with { BuildProvenance = "build:2" }).IdentityHash);
        Assert.Equal(first.PackageId, first.Metadata.PackageSha256 == Metadata().PackageSha256 ? first.PackageId : string.Empty);
    }

    [Fact]
    public void SignedAttestation_VerifiesAndRejectsSignatureIdentityAndSignerFailures()
    {
        using var rsa = RSA.Create(2048);
        var candidate = Candidate(new Phase35DPackageIdentityResolver());
        var profile = Profile();
        var valid = Attestation(candidate, rsa, "signer:approved", Now.AddHours(1));
        var verifier = new Phase35DSignedAttestationVerifier();

        Assert.True(verifier.Verify(valid, candidate, Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), Now, profile).IsValid);
        Assert.Equal(Phase35DAttestationReason.InvalidSignature, verifier.Verify(valid with { SignatureBase64 = Convert.ToBase64String([1, 2, 3]) }, candidate, Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), Now, profile).Reason);
        Assert.Equal(Phase35DAttestationReason.SignerMismatch, verifier.Verify(valid with { SignerIdentity = "signer:other" }, candidate, Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), Now, profile).Reason);
        Assert.Equal(Phase35DAttestationReason.SignerExpired, verifier.Verify(valid with { SignerNotAfter = Now.AddMinutes(-1) }, candidate, Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), Now, profile).Reason);
        Assert.Equal(Phase35DAttestationReason.SignerRevoked, verifier.Verify(valid, candidate, Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), Now, profile with { RevokedSignerIdentities = ["signer:approved"] }).Reason);
        var changedPackage = candidate with { Package = candidate.Package with { Metadata = candidate.Package.Metadata with { PackageSha256 = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd" } } };
        Assert.Equal(Phase35DAttestationReason.PackageHashMismatch, verifier.Verify(valid with { Candidate = changedPackage }, candidate, Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), Now, profile).Reason);
        Assert.Equal(Phase35DAttestationReason.UnsupportedAlgorithm, verifier.Verify(valid with { SignatureAlgorithm = (Phase35DSignatureAlgorithm)99 }, candidate, Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), Now, profile).Reason);
        Assert.Equal(Phase35DAttestationReason.Missing, verifier.Verify(null, candidate, Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), Now, profile).Reason);
    }

    [Fact]
    public void Certification_ApprovesPreProductionAndRejectsBrokenConformance()
    {
        using var rsa = RSA.Create(2048);
        var candidate = Candidate(new Phase35DPackageIdentityResolver());
        var profile = Profile();
        var input = Input(candidate, profile, rsa, new Phase35CConformanceEvidence(true, true, true, true, true, true));
        var decision = new Phase35DCertificationEvaluator(() => Now).Evaluate(input);

        Assert.True(decision.IsCertified);
        Assert.True(decision.IsPreProductionEligible);
        Assert.Empty(decision.Reasons);
        Console.WriteLine($"PHASE35D_EVIDENCE_HASH={decision.Evidence.EvidenceHash}");
        var broken = new Phase35DCertificationEvaluator(() => Now).Evaluate(input with { ConformanceEvidence = new Phase35CConformanceEvidence(false, true, true, true, true, true) });
        Assert.False(broken.IsCertified);
        Assert.Contains(Phase35DCertificationDenialReason.ConformanceFailed, broken.Reasons);
    }

    [Fact]
    public void Lifecycle_ExpiresRevokesAndSupersedesImmutableRecords()
    {
        using var rsa = RSA.Create(2048);
        var candidate = Candidate(new Phase35DPackageIdentityResolver());
        var profile = Profile();
        var decision = new Phase35DCertificationEvaluator(() => Now).Evaluate(Input(candidate, profile, rsa, new Phase35CConformanceEvidence(true, true, true, true, true, true)));
        var lifecycle = new Phase35DCertificationLifecycle(() => Now);
        var record = lifecycle.Issue(decision, candidate, profile, "cert:1");

        Assert.True(lifecycle.IsLive(record, Now));
        Assert.False(lifecycle.IsLive(record, Now.AddDays(2)));
        Assert.Equal(Phase35DCertificationState.Revoked, lifecycle.Revoke(record, "key-compromise").State);
        Assert.Equal("cert:2", lifecycle.Supersede(record, "cert:2").SupersededBy);
        Assert.Throws<InvalidOperationException>(() => lifecycle.Transition(Phase35DCertificationState.Candidate, Phase35DCertificationState.Certified));
        Assert.Equal(Phase35DCertificationState.Expired, lifecycle.Expire(record, Now.AddDays(2)).State);
        Assert.Equal(Phase35DCertificationState.Certified, record.State);
    }

    [Fact]
    public void ProtectedPersistence_ReloadsAndFailsClosedOnTamperingAndDuplicateReplay()
    {
        var directory = Path.Combine(Path.GetTempPath(), "phase35d-tests", Guid.NewGuid().ToString("N"));
        var file = Path.Combine(directory, "state.json");
        try
        {
            var store = new Phase35DProtectedAuditReplayStore(file);
            var audit = new Phase35CDurableAuditStore(() => Now);
            var record = audit.Append(new Phase35CAuditEvent("session:1", "provider", "started", "request", "started"));
            store.AppendAudit(record);
            store.AddReplay(new Phase35CExecutionIdentity("execution:1", "session:1", "request", "nonce"));
            Assert.Single(new Phase35DProtectedAuditReplayStore(file).Load().AuditRecords);
            Assert.Throws<InvalidDataException>(() => store.AddReplay(new Phase35CExecutionIdentity("execution:1", "session:1", "request", "nonce")));
            File.WriteAllText(file, File.ReadAllText(file).Replace("started", "changed", StringComparison.Ordinal));
            Assert.Throws<InvalidDataException>(() => store.Load());
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void ActivationBinding_RequiresExactLiveCertificationAndNeverMakesProductionEligible()
    {
        using var rsa = RSA.Create(2048);
        var candidate = Candidate(new Phase35DPackageIdentityResolver());
        var profile = Profile();
        var input = Input(candidate, profile, rsa, new Phase35CConformanceEvidence(true, true, true, true, true, true));
        var decision = new Phase35DCertificationEvaluator(() => Now).Evaluate(input);
        var record = new Phase35DCertificationLifecycle(() => Now).Issue(decision, candidate, profile, "cert:1");
        var binding = new Phase35DCertificationActivationBinding(() => Now).Evaluate(candidate, profile, record, decision.Evidence, ActivationInput());

        Assert.True(binding.IsEligible);
        Assert.Equal(Phase35DAdmission.PreProductionEligible, binding.Admission);
        Assert.False(new Phase35CActivationGate(() => Now).EvaluateProduction(Phase35BProductionCatalog.Registrations).IsEligible);
        Assert.False(new Phase35DCertificationActivationBinding(() => Now).Evaluate(candidate with { ProviderVersion = "2.0.0" }, profile, record, decision.Evidence, ActivationInput()).IsEligible);
        Assert.False(new Phase35DCertificationActivationBinding(() => Now.AddDays(2)).Evaluate(candidate, profile, record, decision.Evidence, ActivationInput()).IsEligible);
        Assert.False(new Phase35DCertificationActivationBinding(() => Now).Evaluate(candidate, profile, record, decision.Evidence, ActivationInput() with { Profile = ProviderProfile() with { ProviderId = "other.provider" } }).IsEligible);
    }

    private static Phase35DPackageMetadata Metadata() => new("1.0.0", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc", "signer:approved", Phase35DSignatureAlgorithm.RsaSha256, "key:1", "build:1");
    private static Phase35CPolicyVersions Versions() => new("execution/v1", "sandbox/v1", "credential/v1", "artifact/v1", "conformance/v1", "corpus/v1");
    private static Phase35DCertificationCandidate Candidate(Phase35DPackageIdentityResolver resolver) => new(Phase35DContracts.CandidateV1, "future.fake", "1.0.0", "impl:fake", resolver.Resolve("future.fake", "1.0.0", "impl:fake", Metadata()), "signer:approved", [Phase35ACapability.PbirGeneration], Phase35AExecutionClass.Executable, Sandbox(), Versions(), "corpus/v1", "conformance/v1");
    private static Phase35DCertificationProfile Profile() => new(Phase35DContracts.ProfileV1, "preprod/v1", ["signer:approved"], [], Phase35AExecutionClass.Executable, "sandbox/v1", "corpus/v1", "conformance/v1", TimeSpan.FromDays(1));
    private static Phase35CSandboxPolicy Sandbox() => new("sandbox/v1", Phase35CProcessModel.Isolated, Phase35CNetworkPolicy.Denied, Phase35CFilesystemPolicy.DedicatedOutputOnly, Phase35CEnvironmentPolicy.Allowlisted, Phase35CCredentialAccessPolicy.GrantOnly, TimeSpan.FromMinutes(5), 256, 1, 1, 1024, false, []);
    private static Phase35DSignedAttestation Attestation(Phase35DCertificationCandidate candidate, RSA rsa, string signer, DateTimeOffset expires)
    {
        var payload = new Phase35ACanonicalJson().Hash(candidate);
        return new(Phase35DContracts.AttestationV1, candidate, signer, Now.AddHours(-1), expires, Phase35DSignatureAlgorithm.RsaSha256, payload, Convert.ToBase64String(rsa.SignData(System.Text.Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)));
    }
    private static Phase35DCertificationInput Input(Phase35DCertificationCandidate candidate, Phase35DCertificationProfile profile, RSA rsa, Phase35CConformanceEvidence evidence) => new(candidate, profile, Attestation(candidate, rsa, "signer:approved", Now.AddHours(1)), Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), new(Phase35CContracts.TrustV1, candidate.ProviderId, candidate.ProviderVersion, candidate.ImplementationId, candidate.Capabilities, candidate.ExecutionClass, "sandbox/v1", candidate.PolicyVersions, Now.AddHours(-1), Now.AddHours(1)), ProviderProfile(), Request(), new Adapter(), evidence, new("clean", "corpus/v1", ["kind", "hash"], ["secret"], Phase35CExpectedValidationOutcome.Valid, [candidate.ProviderId]), ["kind", "hash"], Sandbox(), new(true, Phase35CCredentialReason.None), new(TimeSpan.FromMinutes(5), 1, 1, 1024, 1024, 1), true, true, true);
    private static Phase35AProviderProfile ProviderProfile() => new(Phase35AContracts.ProviderProfileV1, "future.fake", "Future Fake", Phase35AProviderCategory.OfflineTest, Phase35AExecutionClass.Executable, Phase35ATrustClassification.Untrusted, [Phase35ACapability.PbirGeneration], [Phase35AArtifactKind.PbirReport], [Phase35AReadinessRequirement.ExplicitExecutableRegistration]);
    private static Phase35ARequest Request() => new(Phase35AContracts.RequestV1, "request:1", "intent", ["input:1"], [Phase35ACapability.PbirGeneration], Phase35AArtifactKind.PbirReport, "future.fake", "hash:input", "hash:policy");
    private static Phase35CActivationInput ActivationInput() => new(ProviderProfile(), new(Phase35AContracts.AuthorizationV1, Phase35AAuthorizationStatus.Approved, "request:1", "future.fake", [Phase35ACapability.PbirGeneration], Phase35AArtifactKind.PbirReport, "policy:1"), new(Phase35AContracts.ReadinessV1, Phase35AReadiness.ReadyForExecution, []), new(true, Phase35CTrustReason.None, Now, Now.AddHours(1)), new(true, []), new(true, Phase35CCredentialReason.None), new(true, []), new(true, []), new(true, []), true, true, true, Versions());

    private sealed class Adapter : IPhase35BProviderAdapter
    {
        public string ProviderId => "future.fake";
        public string AdapterVersion => "fake/v1";
        public IReadOnlyList<Phase35ACapability> Capabilities => [Phase35ACapability.PbirGeneration];
        public Phase35BAdapterValidation ValidateRequest(Phase35ARequest request) => new([], []);
        public Phase35AReadinessResult DeclareReadiness(Phase35ARequest request) => new(Phase35AContracts.ReadinessV1, Phase35AReadiness.ReadyForExecution, []);
        public Phase35BExecutionPlan DescribeExecutionPlan(Phase35ARequest request) => new("plan:fake", ["offline"]);
        public Task<Phase35BOfflineExecutionResult> ExecuteOfflineAsync(Phase35BExecutionContext context, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
