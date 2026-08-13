using System.Text.Json;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35HClient(IPhase35HTransport transport, Phase35HClientIdentity client, Phase35HWorkerIdentity worker, Func<DateTimeOffset> clock)
{
    private readonly Phase35CDurableAuditStore _audit = new(clock);
    internal IReadOnlyList<Phase35CAuditRecord> AuditRecords => _audit.Records;
    internal Phase35HResponse Submit(Phase35HRequest request) => Send(Phase35HOperation.SubmitExecution, request);
    internal Phase35HClientValue<Phase35HStatus> GetStatus(string executionId)
    {
        var response = Send(Phase35HOperation.GetExecutionStatus, Phase35HTestRequest(executionId));
        return new(response.Status, response.Failure);
    }
    internal Phase35HResponse Cancel(string executionId) => Send(Phase35HOperation.CancelExecution, Phase35HTestRequest(executionId));
    internal Phase35HClientValue<Phase35HArtifactManifest> FetchArtifactManifest(string executionId)
    {
        var response = Send(Phase35HOperation.FetchArtifactManifest, Phase35HTestRequest(executionId));
        return new(response.Manifest, response.Failure);
    }

    internal Phase35HClientValue<Phase35HArtifactBytes> FetchArtifact(string executionId, string artifactId)
    {
        var response = Send(Phase35HOperation.FetchArtifact, Phase35HTestRequest(executionId, artifactId));
        var artifact = response.Artifact;
        if (artifact is null) return new(null, response.Failure);
        if (Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(artifact.Bytes)).ToLowerInvariant() != artifact.ContentHash) return new(null, new(Phase35HFailureCode.ArtifactInvalid, "artifact hash validation failed"));
        var descriptor = new Phase35CArtifactDescriptor(artifact.ArtifactId, executionId, "phase35h.inert-fixture", Phase35AArtifactKind.OfflineFixture, artifact.ContentHash, artifact.Bytes.LongLength, true, true);
        var safety = new Phase35CArtifactSafetyPipeline(new Phase35CFakeArtifactScanner(Phase35CScannerClassification.Clean)).Evaluate(descriptor, new Phase35CArtifactSafetyPolicy(1, 1024, [Phase35AArtifactKind.OfflineFixture]));
        return new(artifact with { LocalDisposition = safety.Disposition switch { Phase35CArtifactDisposition.Accepted => Phase35HArtifactDisposition.Accepted, Phase35CArtifactDisposition.Quarantined => Phase35HArtifactDisposition.Quarantined, _ => Phase35HArtifactDisposition.Rejected } }, response.Failure);
    }
    internal Phase35HEnvelope CreateEnvelope(Phase35HOperation operation, Phase35HRequest request) => new(operation, request, Phase35HAuthentication.Hash(request), client.ClientId, Phase35HAuthentication.Sign(request, client.Key));

    private Phase35HResponse Send(Phase35HOperation operation, Phase35HRequest request)
    {
        Phase35HResponse response;
        try
        {
            response = transport.Send(CreateEnvelope(operation, request));
        }
        catch (JsonException)
        {
            return new(operation, worker.WorkerId, string.Empty, string.Empty, Failure: new(Phase35HFailureCode.RequestInvalid, "request contains a value outside the closed contract"));
        }
        if (response.WorkerId != worker.WorkerId || !Verify(response)) return response with { Failure = new(Phase35HFailureCode.AuthenticationFailed, "worker identity verification failed"), Status = null, Manifest = null, Artifact = null };
        _audit.Append(new Phase35CAuditEvent(request.SessionId, request.ProviderId, "remote-response", Phase35HAuthentication.Hash(request), response.Failure?.Code.ToString() ?? response.ResponseHash));
        return response;
    }

    private bool Verify(Phase35HResponse response) { try { return Phase35HAuthentication.VerifyResponse(response, worker.Key!); } catch (FormatException) { return false; } catch (System.Security.Cryptography.CryptographicException) { return false; } }
    private static Phase35HRequest Phase35HTestRequest(string executionId, string? correlationId = null) => new(Phase35HContracts.Version, "request:lookup", executionId, "session:1", "phase35h.inert-fixture", "1.0.0", "phase35h-inert-runner", new("phase35h.inert-fixture", "1.0.0", "phase35h-inert-runner", Phase35HContracts.CertificationId, new string('a', 64), "execution/v1", "sandbox/v1", "artifact/v1", Phase35HContracts.WorkerProfile), new(TimeSpan.FromMinutes(5), 1, 1, 1024, 4096, 1), null, Phase35HWorkloadType.ReturnSuccess, "result/v1", correlationId ?? executionId, Phase35HContracts.WorkerProfile);
}

internal sealed record Phase35HClientValue<T>(T? Value, Phase35HFailure? Failure);
