using System.Security.Cryptography;
using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35ERuntimeTests
{
    [Fact]
    public void Admission_RejectsUnsupportedPlatformAndUnboundIdentityBeforeLaunch()
    {
        var policy = Policy();
        var identity = Identity();
        var decision = new Phase35ESandboxAdmission().Evaluate(
            new Phase35ESandboxAdmissionInput(identity, identity with { ExecutableSha256 = "wrong" }, policy, Capabilities(), true));

        Assert.False(decision.IsAllowed);
        Assert.Contains(Phase35EFailureCode.ExecutableIdentityMismatch, decision.Failures);
    }

    [Fact]
    public void PolicyBinder_FailsClosedForUnsupportedRequiredControls()
    {
        var policy = Policy() with { RequireMemoryLimit = true };
        var binding = new Phase35EPolicyBinder().Bind(policy, Capabilities());

        Assert.False(binding.IsEnforceable);
        Assert.Contains("memory-limit", binding.UnsupportedControls);
    }

    [Fact]
    public void EnvironmentBuilder_ExposesOnlyApprovedValues()
    {
        var environment = new Phase35ESandboxEnvironmentBuilder().Build(
            new Dictionary<string, string?> { ["SAFE_INPUT"] = "ok", ["API_KEY"] = "secret" }, ["SAFE_INPUT"]);

        Assert.Equal(new Dictionary<string, string?> { ["SAFE_INPUT"] = "ok" }, environment);
    }

    [Fact]
    public void Evidence_IsDeterministicAndDoesNotContainRawOutput()
    {
        var evidence = new Phase35ESandboxEvidenceCollector().Collect(
            new Phase35ESandboxResult("session:1", "future.fake", "impl:fake", "package:1", "cert:1", "sandbox/v1", "darwin-arm64", Phase35EExitClassification.Completed, null, 3, 4, [], true),
            new Phase35EPolicyBinding("sandbox/v1", ["network", "filesystem"], [], ["memory"], "env-hash", "filesystem-hash", "denied", "timeout=00:01:00"));

        Assert.Equal(evidence, new Phase35ESandboxEvidenceCollector().Collect(
            new Phase35ESandboxResult("session:1", "future.fake", "impl:fake", "package:1", "cert:1", "sandbox/v1", "darwin-arm64", Phase35EExitClassification.Completed, null, 3, 4, [], true),
            new Phase35EPolicyBinding("sandbox/v1", ["network", "filesystem"], [], ["memory"], "env-hash", "filesystem-hash", "denied", "timeout=00:01:00")));
        Assert.DoesNotContain("secret", evidence.CanonicalPayload, StringComparison.Ordinal);
        Assert.NotEmpty(evidence.EvidenceHash);
    }

    [Fact]
    public async Task Runner_BoundsTimeoutAndReportsOwnedTermination()
    {
        var runner = new Phase35ESandboxedProcessRunner(new Phase35EProcessBoundaryForTests());
        var result = await runner.RunAsync(Spec("timeout") with { Timeout = TimeSpan.FromMilliseconds(100) }, new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);

        Assert.Equal(Phase35EExitClassification.TimedOut, result.ExitClassification);
        Assert.Equal(Phase35EFailureCode.TimedOut, result.Failure);
        Assert.True(result.CleanupSucceeded);
    }

    [Fact]
    public void MacBoundary_ReportsCustomSeatbeltAsUnsupportedWhenTheRuntimeCannotProveIt()
    {
        var capabilities = new Phase35EMacSandboxAdapter().GetCapabilities();
        Assert.False(capabilities.ProcessIsolation);
        Assert.False(new Phase35ESandboxAdmission().Evaluate(new Phase35ESandboxAdmissionInput(Identity(), Identity(), Policy() with { RequireNetworkDenial = true }, capabilities, true)).IsAllowed);
    }

    [Fact]
    public void AuditProjection_EmitsBoundedLifecycleEvents()
    {
        var audit = new Phase35CDurableAuditStore(() => new DateTimeOffset(2026, 8, 12, 15, 0, 0, TimeSpan.Zero));
        new Phase35EAuditProjector(audit).Append("session:1", "future.fake", "request:1", "sandbox-admission", "package:1");
        new Phase35EAuditProjector(audit).Append("session:1", "future.fake", "request:1", "sandbox-cleanup", "clean");

        Assert.True(audit.ValidateChain().IsValid);
        Assert.Equal(["sandbox-admission", "sandbox-cleanup"], audit.Records.Select(item => item.Event.Name));
    }

    private static Phase35ESandboxPolicy Policy() => new("sandbox/v1", false, false, false, TimeSpan.FromSeconds(1), 1024, 1024, 1, false, false, false, false);
    private static Phase35EPlatformCapabilities Capabilities() => new("darwin-arm64", true, true, true, true, false, false, false, true);
    private static Phase35EExecutableIdentity Identity() => new("future.fake", "1.0.0", "impl:fake", "package:1", "cert:1", "/usr/bin/true", Convert.ToHexString(SHA256.HashData(File.ReadAllBytes("/usr/bin/true"))).ToLowerInvariant());
    private static Phase35ESandboxExecutionSpec Spec(string mode) => new(Identity(), Policy(), "session:1", "request:1", "/tmp", "/tmp", "/tmp", [mode], new Dictionary<string, string?>(), TimeSpan.FromMilliseconds(10), 128);

    private sealed class Phase35EProcessBoundaryForTests : IPhase35EProcessBoundary
    {
        public async Task<Phase35EProcessCapture> StartAsync(Phase35ESandboxExecutionSpec spec, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new Phase35EProcessCapture(0, "", "", true);
        }
        public Task TerminateAsync(Phase35EProcessCapture process, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
