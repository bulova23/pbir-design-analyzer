using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35HContracts
{
    internal const string Version = "remote-execution/v1";
    internal const string CertificationId = "phase35h-certification/v1";
    internal const string WorkerProfile = "windows-worker-proof/v1";
}

internal enum Phase35HOperation { SubmitExecution, GetExecutionStatus, CancelExecution, FetchArtifactManifest, FetchArtifact }
internal enum Phase35HWorkloadType { ReturnSuccess, ReturnDeterministicHash, CreateBoundedArtifact, WaitUntilCancelled, WaitUntilTimeout, ReturnStructuredFailure }
internal enum Phase35HLifecycleState { Received, Validated, Authorized, Accepted, Running, ValidatingResult, Quarantined, Completed, Failed, Cancelled, TimedOut, Rejected, Uncertain }
internal enum Phase35HFailureCode { AuthenticationFailed, ProtocolVersionUnsupported, RequestInvalid, CertificationInvalid, AuthorizationDenied, ReplayRejected, WorkerProfileMismatch, ResourcePolicyInvalid, ExecutionFailed, TimedOut, Cancelled, ResultInvalid, ArtifactInvalid, AuditFailure }
internal enum Phase35HArtifactState { Candidate, Rejected, Quarantined }

internal sealed record Phase35HClientIdentity(string ClientId, System.Security.Cryptography.RSA Key);
internal sealed record Phase35HWorkerIdentity(string WorkerId, string Profile, string BuildIdentity, string RunnerIdentity, string RuntimeIdentity, System.Security.Cryptography.RSA? Key = null);
internal sealed record Phase35HCertificationBinding(string ProviderId, string ProviderVersion, string ImplementationId, string CertificationId, string EvidenceHash, string ExecutionPolicyVersion, string ContainmentPolicyVersion, string ArtifactPolicyVersion, string WorkerProfile);
internal sealed record Phase35HCredentialGrantReference(string GrantId, string GrantType, string Scope, DateTimeOffset ExpiresAt, string? ForbiddenValue = null);
internal sealed record Phase35HRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("executionId")] string ExecutionId,
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("implementationId")] string ImplementationId,
    [property: JsonPropertyName("certification")] Phase35HCertificationBinding Certification,
    [property: JsonPropertyName("resourcePolicy")] Phase35CResourcePolicy ResourcePolicy,
    [property: JsonPropertyName("credentialGrant")] Phase35HCredentialGrantReference? CredentialGrant,
    [property: JsonPropertyName("workload")] Phase35HWorkloadType Workload,
    [property: JsonPropertyName("expectedOutputContract")] string ExpectedOutputContract,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("workerProfile")] string WorkerProfile);
internal sealed record Phase35HEnvelope(Phase35HOperation Operation, Phase35HRequest Request, string RequestHash, string ClientId, string Signature);
internal sealed record Phase35HFailure(Phase35HFailureCode Code, string SafeMessage);
internal sealed record Phase35HResult(string Outcome, string Workload, string WorkerId, DateTimeOffset StartedAt, DateTimeOffset FinishedAt, string? FailureCode);
internal sealed record Phase35HArtifactManifest(string ArtifactId, string Kind, string ContentHash, long SizeBytes, string RequestId, string SessionId, string CertificationId, string WorkerId, Phase35HArtifactState State, string ManifestHash);
internal sealed record Phase35HArtifactBytes(string ArtifactId, byte[] Bytes, string ContentHash, Phase35HArtifactDisposition LocalDisposition);
internal enum Phase35HArtifactDisposition { Accepted, Rejected, Quarantined }
internal sealed record Phase35HStatus(string ExecutionId, Phase35HLifecycleState State, Phase35HWorkloadType Workload, int WorkloadStarts, Phase35HFailure? Failure, Phase35HArtifactDisposition? ArtifactDisposition, string? RemoteAuditEvidenceHash);
internal sealed record Phase35HResponse(Phase35HOperation Operation, string WorkerId, string ResponseHash, string Signature, Phase35HStatus? Status = null, Phase35HArtifactManifest? Manifest = null, Phase35HArtifactBytes? Artifact = null, Phase35HFailure? Failure = null)
{
    internal bool IsSuccess => Failure is null;
}
internal sealed record Phase35HAuditSnapshot(IReadOnlyList<Phase35CAuditRecord> Records, string EvidenceHash);
