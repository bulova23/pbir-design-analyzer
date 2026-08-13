using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35IContracts
{
    internal const string ContractVersion = "phase35i-windows-containment/v1";
    internal const string ContainmentProfileVersion = "phase35i-job-restricted-token/v1";
}

internal enum Phase35IWorkloadMode
{
    ReturnSuccess,
    ReturnDeterministicHash,
    CreateBoundedArtifact,
    WaitUntilCancelled,
    WaitUntilTimeout,
    AttemptChild,
    AttemptNestedChild,
    BoundedDiagnostics,
    RestrictedFileAccessCheck,
    ReturnStructuredFailure
}

internal enum Phase35IFailureCode
{
    None,
    PlatformUnsupported,
    WorkerProfileMismatch,
    RunnerIdentityMismatch,
    ExecutableIdentityMismatch,
    ExecutablePathInvalid,
    WorkloadNotAllowed,
    ResourcePolicyInvalid,
    AuditCorrelationMissing,
    RestrictedTokenCreationFailed,
    JobCreationFailed,
    JobConfigurationFailed,
    SuspendedLaunchFailed,
    JobAssignmentFailed,
    ResumeFailed,
    TimedOut,
    Cancelled,
    ProcessLimitExceeded,
    MemoryLimitExceeded,
    EnvironmentDenied,
    FileAccessDenied,
    CleanupFailed,
    NativeFailure,
    ResultInvalid
}

internal enum Phase35ILifecycleResult { Admitted, Started, Completed, Failed, TimedOut, Cancelled, Rejected }
internal enum Phase35IProofStatus { ProvenForInertWorkload, PartiallyProven, NotProven }
internal enum Phase35IEnforcementKind { JobObject, WorkerRuntime, NotProven }

internal sealed record Phase35IWorkerProfile(
    [property: JsonPropertyName("profileId")] string ProfileId,
    [property: JsonPropertyName("osFamily")] string OsFamily,
    [property: JsonPropertyName("osVersionConstraint")] string OsVersionConstraint,
    [property: JsonPropertyName("architecture")] string Architecture,
    [property: JsonPropertyName("runtime")] string Runtime,
    [property: JsonPropertyName("runtimeVersion")] string RuntimeVersion,
    [property: JsonPropertyName("runnerId")] string RunnerId,
    [property: JsonPropertyName("runnerVersion")] string RunnerVersion,
    [property: JsonPropertyName("runnerPackageHash")] string RunnerPackageHash,
    [property: JsonPropertyName("runnerExecutableHash")] string RunnerExecutableHash,
    [property: JsonPropertyName("containmentProfileVersion")] string ContainmentProfileVersion,
    [property: JsonPropertyName("supportedWorkloads")] IReadOnlyList<Phase35HWorkloadType> SupportedWorkloads,
    [property: JsonPropertyName("supportsMemoryLimit")] bool SupportsMemoryLimit,
    [property: JsonPropertyName("supportsProcessLimit")] bool SupportsProcessLimit,
    [property: JsonPropertyName("supportsTimeoutLimit")] bool SupportsTimeoutLimit);

internal sealed record Phase35IRunnerIdentity(
    [property: JsonPropertyName("runnerId")] string RunnerId,
    [property: JsonPropertyName("runnerVersion")] string RunnerVersion,
    [property: JsonPropertyName("packageHash")] string PackageHash,
    [property: JsonPropertyName("executableRelativePath")] string ExecutableRelativePath,
    [property: JsonPropertyName("executableSha256")] string ExecutableSha256,
    [property: JsonPropertyName("architecture")] string Architecture,
    [property: JsonPropertyName("certificationEvidenceId")] string CertificationEvidenceId);

internal sealed record Phase35IContainmentProfile(
    string Version,
    bool KillOnJobClose,
    bool NoBreakaway,
    bool RequireProcessAssignment,
    bool RequireExplicitEnvironment,
    bool RequireExplicitHandles);

internal sealed record Phase35IAdmissionRequest(
    Phase35HRequest RemoteRequest,
    Phase35IWorkerProfile WorkerProfile,
    Phase35IRunnerIdentity CertifiedRunner,
    bool AuditAvailable,
    bool IsWindowsHost);

internal sealed record Phase35IAdmissionDecision(bool IsAdmitted, IReadOnlyList<Phase35IFailureCode> Failures, Phase35IResourceProjectionResult? ResourceProjection);

internal sealed record Phase35IJobLimits(int ExecutionTimeoutSeconds, int ActiveProcessLimit, long MemoryBytes, bool KillOnJobClose, bool NoBreakaway);
internal sealed record Phase35IWorkerLimits(long ResultBytes, int ArtifactCount, long ArtifactBytes, int ConcurrencyLimit);
internal sealed record Phase35IResourceProjectionResult(Phase35IJobLimits JobLimits, Phase35IWorkerLimits WorkerLimits, string EnforcementSummary);

internal sealed record Phase35IContainmentResult(
    string ExecutionId,
    string SessionId,
    Phase35ILifecycleResult Result,
    Phase35IFailureCode? Failure,
    bool JobAssigned,
    bool CleanupSucceeded,
    string Phase35HAuditCorrelationHash,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt);

internal sealed record Phase35ITokenEvidence(string RestrictedPrivileges, string AdministrativeGroupHandling, string RestrictedSids, string IntegrityAssumption);
internal sealed record Phase35IIsolationEvidence(string EnvironmentPolicyHash, string HandlePolicy);
internal sealed record Phase35IJobEvidence(string LimitsSummary);

internal sealed record Phase35IEvidence(
    string CanonicalPayload,
    string EvidenceHash,
    string ExecutionId,
    string SessionId,
    string RequestHash,
    Phase35IWorkerProfile WorkerProfile,
    Phase35IRunnerIdentity Runner,
    string ContainmentProfileVersion,
    Phase35ITokenEvidence RestrictedToken,
    Phase35IJobEvidence Job,
    Phase35IIsolationEvidence Isolation,
    string Phase35HAuditCorrelationHash,
    Phase35IProofStatus ProofStatus);
