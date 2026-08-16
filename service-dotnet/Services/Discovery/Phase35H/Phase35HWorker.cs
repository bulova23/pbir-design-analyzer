using System.Text.Json;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35HWorker
{
    private readonly Phase35HWorkerIdentity _worker;
    private readonly Phase35HClientIdentity _client;
    private readonly Phase35HCertificationBinding _certification;
    private readonly string _root;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Phase35HInertRunner _runner = new();
    private readonly Phase35CDurableAuditStore _audit;
    private readonly Dictionary<string, Phase35HStoredExecution> _executions;

    internal Phase35HWorker(Phase35HWorkerIdentity worker, Phase35HClientIdentity client, Phase35HCertificationBinding certification, string root, Func<DateTimeOffset> clock)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("A worker session root is required.", nameof(root));
        _worker = worker with { Key = worker.Key ?? System.Security.Cryptography.RSA.Create(2048) };
        _client = client;
        _certification = certification;
        _root = Path.GetFullPath(root);
        _clock = clock;
        Directory.CreateDirectory(_root);
        var state = Load();
        _executions = state.Executions.ToDictionary(item => item.Request.ExecutionId, StringComparer.Ordinal);
        _audit = new Phase35CDurableAuditStore(clock);
        foreach (var item in state.Audit) _audit.Append(new Phase35CAuditEvent(item.SessionId, item.ProviderId, item.Name, item.RequestHash, item.Outcome));
        foreach (var item in _executions.Values.Where(item => item.State is Phase35HLifecycleState.Received or Phase35HLifecycleState.Validated or Phase35HLifecycleState.Authorized or Phase35HLifecycleState.Accepted or Phase35HLifecycleState.Running)) item.State = Phase35HLifecycleState.Uncertain;
    }

    internal string WorkerId => _worker.WorkerId;
    internal IReadOnlyList<Phase35CAuditRecord> AuditRecords => _audit.Records;

    internal Phase35HResponse Handle(Phase35HEnvelope envelope)
    {
        if (envelope.ClientId != _client.ClientId || !VerifyClient(envelope)) return Failure(envelope.Operation, Phase35HFailureCode.AuthenticationFailed, "client identity verification failed");
        if (envelope.Request.SchemaVersion != Phase35HContracts.Version) return Failure(envelope.Operation, Phase35HFailureCode.ProtocolVersionUnsupported, "protocol major version is unsupported");
        if (Phase35HAuthentication.Hash(envelope.Request) != envelope.RequestHash) return Failure(envelope.Operation, Phase35HFailureCode.AuthenticationFailed, "request integrity verification failed");
        return envelope.Operation switch
        {
            Phase35HOperation.SubmitExecution => Submit(envelope.Request),
            Phase35HOperation.GetExecutionStatus => Status(envelope.Request),
            Phase35HOperation.CancelExecution => Cancel(envelope.Request),
            Phase35HOperation.FetchArtifactManifest => Manifest(envelope.Request),
            Phase35HOperation.FetchArtifact => Artifact(envelope.Request, envelope.Request.CorrelationId),
            _ => Failure(envelope.Operation, Phase35HFailureCode.RequestInvalid, "operation is not supported")
        };
    }

    internal void SeedUncertain(Phase35HRequest request)
    {
        _executions[request.ExecutionId] = new(request, Phase35HLifecycleState.Running, 0, null, null, null);
        Save();
    }

    private Phase35HResponse Submit(Phase35HRequest request)
    {
        var validation = Validate(request);
        if (validation is not null) return Failure(Phase35HOperation.SubmitExecution, validation.Value.Code, validation.Value.Message);
        if (_executions.TryGetValue(request.ExecutionId, out var existing))
        {
            if (Phase35HAuthentication.Hash(existing.Request) != Phase35HAuthentication.Hash(request)) return Failure(Phase35HOperation.SubmitExecution, Phase35HFailureCode.ReplayRejected, "execution identity is bound to a different request");
            return Signed(new(Phase35HOperation.SubmitExecution, _worker.WorkerId, string.Empty, string.Empty, ToStatus(existing)));
        }
        var record = new Phase35HStoredExecution(request, Phase35HLifecycleState.Authorized, 0, null, null, null);
        record.StartedAt = _clock();
        _executions.Add(request.ExecutionId, record);
        Audit(request, "received", "accepted");
        if (request.Workload is Phase35HWorkloadType.WaitUntilCancelled or Phase35HWorkloadType.WaitUntilTimeout)
        {
            record.State = Phase35HLifecycleState.Running;
            record.WorkloadStarts = 1;
        }
        else
        {
            record.State = Phase35HLifecycleState.ValidatingResult;
            record.WorkloadStarts = 1;
            var output = _runner.Run(request.Workload, request);
            record.State = output.FailureCode is null ? output.ArtifactBytes is null ? Phase35HLifecycleState.Completed : Phase35HLifecycleState.Quarantined : Phase35HLifecycleState.Failed;
            record.Failure = output.FailureCode is null ? null : new(Phase35HFailureCode.ExecutionFailed, "inert workload returned a structured failure");
            if (output.ArtifactBytes is not null)
            {
                record.Manifest = CreateManifest(request, output.ArtifactBytes);
                record.ArtifactBytes = output.ArtifactBytes;
            }
        }
        Save();
        Audit(request, "completed", record.State.ToString());
        return Signed(new(Phase35HOperation.SubmitExecution, _worker.WorkerId, string.Empty, string.Empty, ToStatus(record)));
    }

    private Phase35HResponse Status(Phase35HRequest request)
    {
        if (!_executions.TryGetValue(request.ExecutionId, out var record) || record.Request.SessionId != request.SessionId) return Failure(Phase35HOperation.GetExecutionStatus, Phase35HFailureCode.AuthorizationDenied, "execution ownership failed");
        ApplyClock(record);
        return Signed(new(Phase35HOperation.GetExecutionStatus, _worker.WorkerId, string.Empty, string.Empty, ToStatus(record)));
    }

    private Phase35HResponse Cancel(Phase35HRequest request)
    {
        if (!_executions.TryGetValue(request.ExecutionId, out var record) || record.Request.SessionId != request.SessionId) return Failure(Phase35HOperation.CancelExecution, Phase35HFailureCode.AuthorizationDenied, "execution ownership failed");
        ApplyClock(record);
        if (record.State is Phase35HLifecycleState.Completed or Phase35HLifecycleState.Failed or Phase35HLifecycleState.Cancelled or Phase35HLifecycleState.TimedOut or Phase35HLifecycleState.Quarantined) return Failure(Phase35HOperation.CancelExecution, Phase35HFailureCode.RequestInvalid, "execution is already terminal");
        record.State = Phase35HLifecycleState.Cancelled;
        record.Failure = new(Phase35HFailureCode.Cancelled, "execution cancellation was accepted");
        Save();
        Audit(record.Request, "cancelled", "cancelled");
        return Signed(new(Phase35HOperation.CancelExecution, _worker.WorkerId, string.Empty, string.Empty, ToStatus(record), Failure: record.Failure));
    }

    private Phase35HResponse Manifest(Phase35HRequest request) => _executions.TryGetValue(request.ExecutionId, out var record) && record.Request.SessionId == request.SessionId && record.Manifest is not null
        ? Signed(new(Phase35HOperation.FetchArtifactManifest, _worker.WorkerId, string.Empty, string.Empty, null, record.Manifest))
        : Failure(Phase35HOperation.FetchArtifactManifest, Phase35HFailureCode.ArtifactInvalid, "artifact manifest was not found");

    private Phase35HResponse Artifact(Phase35HRequest request, string artifactId) => _executions.TryGetValue(request.ExecutionId, out var record) && record.Request.SessionId == request.SessionId && record.Manifest?.ArtifactId == artifactId && record.ArtifactBytes is not null
        ? Signed(new(Phase35HOperation.FetchArtifact, _worker.WorkerId, string.Empty, string.Empty, null, null, new(artifactId, record.ArtifactBytes, record.Manifest.ContentHash, Phase35HArtifactDisposition.Quarantined)))
        : Failure(Phase35HOperation.FetchArtifact, Phase35HFailureCode.ArtifactInvalid, "artifact identity or session ownership failed");

    private (Phase35HFailureCode Code, string Message)? Validate(Phase35HRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId) || string.IsNullOrWhiteSpace(request.ExecutionId) || string.IsNullOrWhiteSpace(request.SessionId) || string.IsNullOrWhiteSpace(request.CorrelationId)) return (Phase35HFailureCode.RequestInvalid, "required request identity is missing");
        if (request.Workload is not Phase35HWorkloadType.ReturnSuccess and not Phase35HWorkloadType.ReturnDeterministicHash and not Phase35HWorkloadType.CreateBoundedArtifact and not Phase35HWorkloadType.WaitUntilCancelled and not Phase35HWorkloadType.WaitUntilTimeout and not Phase35HWorkloadType.ReturnStructuredFailure) return (Phase35HFailureCode.RequestInvalid, "workload is not in the closed inert set");
        if (request.ProviderId != request.Certification.ProviderId || request.ProviderVersion != request.Certification.ProviderVersion || request.ImplementationId != request.Certification.ImplementationId || request.Certification.EvidenceHash.Length != 64) return (Phase35HFailureCode.CertificationInvalid, "certification identity is not exact");
        if (request.Certification.CertificationId != Phase35HContracts.CertificationId) return (Phase35HFailureCode.AuthorizationDenied, "certification record is not authorized");
        if (request.Certification != _certification) return (Phase35HFailureCode.AuthorizationDenied, "certification or policy binding is not authorized");
        if (request.WorkerProfile != _worker.Profile || request.Certification.WorkerProfile != _worker.Profile) return (Phase35HFailureCode.WorkerProfileMismatch, "worker profile is not certified");
        if (!new Phase35CResourcePolicyEvaluator().Evaluate(request.ResourcePolicy).IsAllowed) return (Phase35HFailureCode.ResourcePolicyInvalid, "resource policy is invalid or unbounded");
        if (request.CredentialGrant is { ForbiddenValue: not null }) return (Phase35HFailureCode.RequestInvalid, "credential grant contains forbidden material");
        return null;
    }

    private void ApplyClock(Phase35HStoredExecution record)
    {
        if (record.State == Phase35HLifecycleState.Running && record.Request.Workload == Phase35HWorkloadType.WaitUntilTimeout && _clock() - record.StartedAt >= record.Request.ResourcePolicy.MaxDuration)
        {
            record.State = Phase35HLifecycleState.TimedOut;
            record.Failure = new(Phase35HFailureCode.TimedOut, "worker-side duration limit elapsed");
            Save();
        }
    }

    private Phase35HArtifactManifest CreateManifest(Phase35HRequest request, byte[] bytes)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
        var raw = new Phase35HArtifactManifest("artifact:" + request.ExecutionId, "bounded-fixture", hash, bytes.LongLength, request.RequestId, request.SessionId, request.Certification.CertificationId, _worker.WorkerId, Phase35HArtifactState.Candidate, string.Empty);
        return raw with { ManifestHash = new Phase35ACanonicalJson().Hash(raw) };
    }

    private Phase35HStatus ToStatus(Phase35HStoredExecution record) => new(record.Request.ExecutionId, record.State, record.Request.Workload, record.WorkloadStarts, record.Failure, record.Manifest is null ? null : Phase35HArtifactDisposition.Quarantined, _audit.Records.LastOrDefault()?.CurrentHash);
    private void Audit(Phase35HRequest request, string name, string outcome) => _audit.Append(new Phase35CAuditEvent(request.SessionId, request.ProviderId, name, Phase35HAuthentication.Hash(request), outcome));
    private bool VerifyClient(Phase35HEnvelope envelope) { try { return Phase35HAuthentication.Verify(envelope.Request, envelope.Signature, _client.Key); } catch (FormatException) { return false; } catch (System.Security.Cryptography.CryptographicException) { return false; } }
    private Phase35HResponse Failure(Phase35HOperation operation, Phase35HFailureCode code, string message) => Signed(new(operation, _worker.WorkerId, string.Empty, string.Empty, null, null, null, new(code, message)));
    private Phase35HResponse Signed(Phase35HResponse response) => response with { ResponseHash = new Phase35ACanonicalJson().Hash(response with { ResponseHash = string.Empty, Signature = string.Empty }), Signature = Phase35HAuthentication.SignResponse(response with { ResponseHash = new Phase35ACanonicalJson().Hash(response with { ResponseHash = string.Empty, Signature = string.Empty }) }, _worker.Key!) };
    private string StatePath => Path.Combine(_root, "worker-state.json");
    private Phase35HStoredState Load() { if (!File.Exists(StatePath)) return new([], []); return JsonSerializer.Deserialize<Phase35HStoredState>(File.ReadAllBytes(StatePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new([], []); }
    private void Save() => File.WriteAllBytes(StatePath, JsonSerializer.SerializeToUtf8Bytes(new Phase35HStoredState(_executions.Values.ToArray(), _audit.Records.Select(item => new Phase35HAuditEntry(item.Event.SessionId, item.Event.ProviderId, item.Event.Name, item.Event.RequestHash, item.Event.Outcome)).ToArray())));
}

internal sealed class Phase35HStoredExecution
{
    public Phase35HStoredExecution(Phase35HRequest request, Phase35HLifecycleState state, int workloadStarts, Phase35HFailure? failure, Phase35HArtifactManifest? manifest, byte[]? artifactBytes) { Request = request; State = state; WorkloadStarts = workloadStarts; Failure = failure; Manifest = manifest; ArtifactBytes = artifactBytes; StartedAt = DateTimeOffset.UtcNow; }
    public Phase35HRequest Request { get; set; }
    public Phase35HLifecycleState State { get; set; }
    public int WorkloadStarts { get; set; }
    public Phase35HFailure? Failure { get; set; }
    public Phase35HArtifactManifest? Manifest { get; set; }
    public byte[]? ArtifactBytes { get; set; }
    public DateTimeOffset StartedAt { get; set; }
}
internal sealed record Phase35HAuditEntry(string SessionId, string ProviderId, string Name, string RequestHash, string Outcome);
internal sealed record Phase35HStoredState(IReadOnlyList<Phase35HStoredExecution> Executions, IReadOnlyList<Phase35HAuditEntry> Audit);
