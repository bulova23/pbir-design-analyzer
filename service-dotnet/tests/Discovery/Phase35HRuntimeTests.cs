using System.Security.Cryptography;
using PowerBIModelingService.Services.Discovery;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35HRuntimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TypedTransport_AuthenticatesBothSidesAndRejectsTampering()
    {
        using var identities = Phase35HTestFixture.Identities();
        var worker = Phase35HTestFixture.Worker(identities, Now);
        var client = new Phase35HClient(new Phase35HTransport(worker), identities.Client, identities.Worker, () => Now);

        var envelope = client.CreateEnvelope(Phase35HOperation.SubmitExecution, Phase35HTestFixture.Request());
        Assert.True(Phase35HAuthentication.Verify(envelope.Request, envelope.Signature, identities.Client.Key));
        var raw = worker.Handle(envelope);
        Assert.Null(raw.Failure);
        Assert.True(Phase35HAuthentication.VerifyResponse(raw, identities.Worker.Key!));
        var response = client.Submit(Phase35HTestFixture.Request("execution:second"));
        Assert.True(response.IsSuccess);
        Assert.Equal(Phase35HLifecycleState.Completed, response.Status!.State);
        Assert.True(new Phase35CDurableAuditStore().ValidateChain(worker.AuditRecords).IsValid);
        Assert.NotEmpty(client.AuditRecords);

        var tampered = client.CreateEnvelope(Phase35HOperation.GetExecutionStatus, Phase35HTestFixture.Request() with { ExecutionId = response.Status.ExecutionId });
        tampered = tampered with { RequestHash = tampered.RequestHash + "x" };
        var rejected = worker.Handle(tampered);
        Assert.Equal(Phase35HFailureCode.AuthenticationFailed, rejected.Failure!.Code);

        using var unknown = Phase35HTestFixture.Identities("unknown-client");
        var unknownClient = new Phase35HClient(new Phase35HTransport(worker), unknown.Client, identities.Worker, () => Now);
        var unknownResponse = unknownClient.Submit(Phase35HTestFixture.Request("execution:unknown"));
        Assert.Equal(Phase35HFailureCode.AuthenticationFailed, unknownResponse.Failure!.Code);

        using var wrongWorkerKey = RSA.Create(2048);
        var wrongWorkerClient = new Phase35HClient(new Phase35HTransport(worker), identities.Client, identities.Worker with { Key = wrongWorkerKey }, () => Now);
        Assert.Equal(Phase35HFailureCode.AuthenticationFailed, wrongWorkerClient.Submit(Phase35HTestFixture.Request("execution:wrong-worker")).Failure!.Code);
    }

    [Fact]
    public void Worker_RevalidatesCertificationPolicyProfileWorkloadAndResources()
    {
        using var identities = Phase35HTestFixture.Identities();
        var worker = Phase35HTestFixture.Worker(identities, Now);
        var client = new Phase35HClient(new Phase35HTransport(worker), identities.Client, identities.Worker, () => Now);

        Assert.Equal(Phase35HFailureCode.CertificationInvalid, client.Submit(Phase35HTestFixture.Request() with { Certification = Phase35HTestFixture.Certification with { EvidenceHash = "bad" }, ExecutionId = "execution:bad-cert" }).Failure!.Code);
        Assert.Equal(Phase35HFailureCode.WorkerProfileMismatch, client.Submit(Phase35HTestFixture.Request("execution:bad-profile") with { WorkerProfile = "linux-worker/v1" }).Failure!.Code);
        Assert.Equal(Phase35HFailureCode.ResourcePolicyInvalid, client.Submit(Phase35HTestFixture.Request("execution:bad-policy") with { ResourcePolicy = new Phase35CResourcePolicy(TimeSpan.Zero, 0, 0, 0, 0, 0) }).Failure!.Code);
        Assert.Equal(Phase35HFailureCode.RequestInvalid, client.Submit(Phase35HTestFixture.Request("execution:bad-workload") with { Workload = (Phase35HWorkloadType)99 }).Failure!.Code);
        Assert.Equal(Phase35HFailureCode.AuthorizationDenied, client.Submit(Phase35HTestFixture.Request("execution:bad-cert-id") with { Certification = Phase35HTestFixture.Certification with { CertificationId = "other" } }).Failure!.Code);
        Assert.Equal(Phase35HFailureCode.ProtocolVersionUnsupported, client.Submit(Phase35HTestFixture.Request("execution:bad-version") with { SchemaVersion = "remote-execution/v2" }).Failure!.Code);
        var statusWithWrongSession = client.CreateEnvelope(Phase35HOperation.GetExecutionStatus, Phase35HTestFixture.Request("execution:missing") with { SessionId = "session:other" });
        Assert.Equal(Phase35HFailureCode.AuthorizationDenied, worker.Handle(statusWithWrongSession).Failure!.Code);
    }

    [Fact]
    public void Submit_IsIdempotentAndModifiedRetryCannotStartAnotherWorkload()
    {
        using var identities = Phase35HTestFixture.Identities();
        var worker = Phase35HTestFixture.Worker(identities, Now);
        var client = new Phase35HClient(new Phase35HTransport(worker), identities.Client, identities.Worker, () => Now);
        var request = Phase35HTestFixture.Request();

        var first = client.Submit(request);
        var retry = client.Submit(request);
        Assert.Equal(first.Status!.ExecutionId, retry.Status!.ExecutionId);
        Assert.Equal(first.Status.WorkloadStarts, retry.Status.WorkloadStarts);
        Assert.Equal(Phase35HFailureCode.ReplayRejected, client.Submit(request with { ExpectedOutputContract = "changed" }).Failure!.Code);
    }

    [Fact]
    public void InertRunner_ReturnsBoundedResultAndQuarantinedArtifactPassesLocalSafetyPipeline()
    {
        using var identities = Phase35HTestFixture.Identities();
        var worker = Phase35HTestFixture.Worker(identities, Now);
        var client = new Phase35HClient(new Phase35HTransport(worker), identities.Client, identities.Worker, () => Now);

        var response = client.Submit(Phase35HTestFixture.Request() with { ExecutionId = "execution:artifact", Workload = Phase35HWorkloadType.CreateBoundedArtifact });
        Assert.Equal(Phase35HArtifactDisposition.Quarantined, response.Status!.ArtifactDisposition);
        var manifest = Assert.IsType<Phase35HArtifactManifest>(client.FetchArtifactManifest(response.Status.ExecutionId).Value);
        Assert.Equal(Phase35HArtifactState.Candidate, manifest.State);
        var artifact = client.FetchArtifact(response.Status.ExecutionId, manifest.ArtifactId);
        Assert.Equal(Phase35HArtifactDisposition.Accepted, artifact.Value!.LocalDisposition);
        Assert.Equal(manifest.ContentHash, artifact.Value.ContentHash);
    }

    [Fact]
    public void WorkerSideTimeoutAndTypedCancellationAreIndependentOfClientTimeout()
    {
        using var identities = Phase35HTestFixture.Identities();
        var clock = new Phase35HTestClock(Now);
        var worker = Phase35HTestFixture.Worker(identities, clock);
        var client = new Phase35HClient(new Phase35HTransport(worker), identities.Client, identities.Worker, () => clock.Now);

        var cancelled = client.Submit(Phase35HTestFixture.Request("execution:cancel") with { Workload = Phase35HWorkloadType.WaitUntilCancelled });
        Assert.Equal(Phase35HFailureCode.Cancelled, client.Cancel(cancelled.Status!.ExecutionId).Failure!.Code);
        var timed = client.Submit(Phase35HTestFixture.Request("execution:timeout") with { Workload = Phase35HWorkloadType.WaitUntilTimeout });
        clock.Now = clock.Now.AddMinutes(6);
        Assert.Equal(Phase35HFailureCode.TimedOut, client.GetStatus(timed.Status!.ExecutionId).Value!.Failure!.Code);
    }

    [Fact]
    public void Restart_ReconcilesTerminalStateAndMarksIncompleteWorkUncertainWithoutReplay()
    {
        var root = Path.Combine(Path.GetTempPath(), "phase35h-tests", Guid.NewGuid().ToString("N"));
        try
        {
            using var identities = Phase35HTestFixture.Identities();
            var first = Phase35HTestFixture.Worker(identities, Now, root);
            var client = new Phase35HClient(new Phase35HTransport(first), identities.Client, identities.Worker, () => Now);
            var completed = client.Submit(Phase35HTestFixture.Request("execution:completed"));
            first.SeedUncertain(Phase35HTestFixture.Request("execution:uncertain"));

            var restarted = Phase35HTestFixture.Worker(identities, Now, root);
            var restartedClient = new Phase35HClient(new Phase35HTransport(restarted), identities.Client, identities.Worker, () => Now);
            Assert.Equal(Phase35HLifecycleState.Completed, restartedClient.GetStatus(completed.Status!.ExecutionId).Value!.State);
            Assert.Equal(Phase35HLifecycleState.Uncertain, restartedClient.GetStatus("execution:uncertain").Value!.State);
            Assert.Equal(0, restartedClient.GetStatus("execution:uncertain").Value!.WorkloadStarts);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void CredentialReference_RejectsSecretMaterialAndNeverCarriesSecretValue()
    {
        using var identities = Phase35HTestFixture.Identities();
        var worker = Phase35HTestFixture.Worker(identities, Now);
        var client = new Phase35HClient(new Phase35HTransport(worker), identities.Client, identities.Worker, () => Now);
        var response = client.Submit(Phase35HTestFixture.Request("execution:secret") with { CredentialGrant = new Phase35HCredentialGrantReference("grant:1", "opaque", "scope:1", Now.AddHours(1), "password=bad") });
        Assert.Equal(Phase35HFailureCode.RequestInvalid, response.Failure!.Code);
    }

}

internal static class Phase35HTestFixture
{
    internal static Phase35HTestIdentities Identities(string clientId = "client:test") => new(clientId, "worker:test", RSA.Create(2048), RSA.Create(2048));
    internal static Phase35HWorker Worker(Phase35HTestIdentities identities, DateTimeOffset now, string? root = null) => Worker(identities, new Phase35HTestClock(now), root);
    internal static Phase35HWorker Worker(Phase35HTestIdentities identities, Phase35HTestClock clock, string? root = null) => new(identities.Worker, identities.Client, Certification, root ?? Path.Combine(Path.GetTempPath(), "phase35h-tests", Guid.NewGuid().ToString("N")), () => clock.Now);
    internal static Phase35HCertificationBinding Certification => new("phase35h.inert-fixture", "1.0.0", "phase35h-inert-runner", "phase35h-certification/v1", new string('a', 64), "execution/v1", "sandbox/v1", "artifact/v1", "windows-worker-proof/v1");
    internal static Phase35HRequest Request(string executionId = "execution:1") => new("remote-execution/v1", "request:1", executionId, "session:1", "phase35h.inert-fixture", "1.0.0", "phase35h-inert-runner", Certification, new Phase35CResourcePolicy(TimeSpan.FromMinutes(5), 1, 1, 1024, 4096, 1), null, Phase35HWorkloadType.ReturnSuccess, "result/v1", "audit:1", "windows-worker-proof/v1");
}

internal sealed class Phase35HTestIdentities(string clientId, string workerId, RSA clientKey, RSA workerKey) : IDisposable
{
    internal string ClientId { get; } = clientId;
    internal string WorkerId { get; } = workerId;
    internal Phase35HClientIdentity Client { get; } = new(clientId, clientKey);
    internal Phase35HWorkerIdentity Worker { get; } = new(workerId, "windows-worker-proof/v1", "worker-build:1", "phase35h-inert-runner/v1", "runtime:net8", workerKey);
    public void Dispose() { ClientKey.Dispose(); WorkerKey.Dispose(); }
    private RSA ClientKey => Client.Key;
    private RSA WorkerKey => Worker.Key;
}

internal sealed class Phase35HTestClock(DateTimeOffset now)
{
    public DateTimeOffset Now { get; set; } = now;
}
