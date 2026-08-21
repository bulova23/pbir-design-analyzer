using System.Security.Cryptography;
using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35IContainmentTests
{
    [Fact]
    public void AdmissionAcceptsOnlyExactCertifiedWorkerAndRunnerIdentity()
    {
        var request = Phase35ITestData.Request();
        var profile = Phase35ITestData.Profile();
        var identity = Phase35ITestData.Runner();

        var decision = new Phase35IAdmission().Evaluate(new(request, profile, identity, true, OperatingSystem.IsWindows()));

        Assert.True(decision.IsAdmitted);
        Assert.Empty(decision.Failures);
    }

    [Fact]
    public void AdmissionRejectsMismatchedProfileAndRunnerIdentity()
    {
        var request = Phase35ITestData.Request();
        var profile = Phase35ITestData.Profile() with { RunnerVersion = "2.0.0" };
        var identity = Phase35ITestData.Runner() with { ExecutableSha256 = new string('b', 64) };

        var decision = new Phase35IAdmission().Evaluate(new(request, profile, identity, true, true));

        Assert.False(decision.IsAdmitted);
        Assert.Contains(Phase35IFailureCode.WorkerProfileMismatch, decision.Failures);
        Assert.Contains(Phase35IFailureCode.ExecutableIdentityMismatch, decision.Failures);
    }

    [Fact]
    public void ResourceProjectionSeparatesJobEnforcedAndWorkerEnforcedLimits()
    {
        var projection = new Phase35IResourceProjection().Project(new(TimeSpan.FromSeconds(3), 1, 2, 4096, 8192, 1));

        Assert.Equal(3, projection.JobLimits.ExecutionTimeoutSeconds);
        Assert.Equal(2, projection.JobLimits.ActiveProcessLimit);
        Assert.Equal(0, projection.JobLimits.MemoryBytes);
        Assert.Equal(8192, projection.WorkerLimits.ResultBytes);
        Assert.Equal(2, projection.WorkerLimits.ArtifactCount);
        Assert.Contains("memory=not-configured", projection.EnforcementSummary);
        Assert.Contains("result-bytes=worker", projection.EnforcementSummary);
    }

    [Fact]
    public void EvidenceUsesCanonicalHashAndCorrelatesPhase35HAudit()
    {
        var result = new Phase35IContainmentResult("exec:1", "session:1", Phase35ILifecycleResult.Completed, null, false, true, "audit:hash", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddSeconds(1));
        var evidence = new Phase35IEvidenceBuilder().Build(result, Phase35ITestData.Request(), Phase35ITestData.Profile(), Phase35ITestData.Runner(), new("env", "handles"), new("limits"));

        Assert.Equal(Phase35IProofStatus.PartiallyProven, evidence.ProofStatus);
        Assert.Equal("audit:hash", evidence.Phase35HAuditCorrelationHash);
        Assert.Equal(64, evidence.EvidenceHash.Length);
        Assert.DoesNotContain("PHASE35I_TEST_SECRET", evidence.CanonicalPayload);
    }

    [Fact]
    public void AdmissionRejectsUnknownWorkloadAndNonFinitePolicy()
    {
        var request = Phase35ITestData.Request() with { Workload = (Phase35HWorkloadType)999 };
        var profile = Phase35ITestData.Profile();
        var identity = Phase35ITestData.Runner();
        var invalidPolicy = request.ResourcePolicy with { MaxResultBytes = 0 };

        var decision = new Phase35IAdmission().Evaluate(new(request with { ResourcePolicy = invalidPolicy }, profile, identity, true, true));

        Assert.False(decision.IsAdmitted);
        Assert.Contains(Phase35IFailureCode.WorkloadNotAllowed, decision.Failures);
        Assert.Contains(Phase35IFailureCode.ResourcePolicyInvalid, decision.Failures);
    }

    [Fact]
    public void WorkerOwnedPathBindingRejectsTraversalAndDerivesSessionRoot()
    {
        var binder = new Phase35IPathBinder();
        var root = Path.Combine(Path.GetTempPath(), "phase35i-worker");

        Assert.Throws<InvalidOperationException>(() => binder.Bind(root, "session:1", Phase35ITestData.Runner() with { ExecutableRelativePath = "../outside.exe" }));
        Assert.EndsWith(Path.Combine("sessions", "session%3A1"), binder.BindSessionRoot(root, "session:1"), StringComparison.Ordinal);
    }
}

internal static class Phase35ITestData
{
    internal static Phase35HRequest Request() => new(
        Phase35HContracts.Version, "request:1", "exec:1", "session:1", "provider", "1.0.0", "implementation",
        new("provider", "1.0.0", "implementation", Phase35HContracts.CertificationId, new string('a', 64), "execution/v1", "containment/v1", "artifact/v1", "windows-worker-proof/v1"),
        new(TimeSpan.FromSeconds(3), 1, 2, 4096, 8192, 1), null, Phase35HWorkloadType.ReturnSuccess, "result/v1", "audit:1", "windows-worker-proof/v1");

    internal static Phase35IWorkerProfile Profile() => new("windows-worker-proof/v1", "Windows", "10.0.20348", "X64", "net8.0", "8.0.0", "phase35i-inert-runner", "1.0.0", new string('a', 64), new string('a', 64), Phase35IContracts.ContainmentProfileVersion, [Phase35HWorkloadType.ReturnSuccess], true, true, true);

    internal static Phase35IRunnerIdentity Runner() => new("phase35i-inert-runner", "1.0.0", new string('a', 64), "runner/Phase35I.InertRunner.exe", new string('a', 64), "X64", "cert:1");
}
