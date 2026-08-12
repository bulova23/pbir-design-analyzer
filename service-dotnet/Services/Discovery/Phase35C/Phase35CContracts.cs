using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35CContracts
{
    internal const string ContractVersion = "phase35c-provider-assurance/v1";
    internal const string TrustV1 = "phase35c-provider-trust/v1";
    internal const string SandboxV1 = "phase35c-sandbox-policy/v1";
    internal const string CredentialV1 = "phase35c-credential-boundary/v1";
    internal const string AuditV1 = "phase35c-durable-audit/v1";
    internal const string ArtifactSafetyV1 = "phase35c-artifact-safety/v1";
    internal const string CorpusV1 = "phase35c-output-corpus/v1";
    internal const string ConformanceV1 = "phase35c-conformance/v1";
}

internal enum Phase35CTrustReason { None, AttestationMissing, TrustExpired, ProviderMismatch, VersionMismatch, ImplementationMismatch, CapabilityMismatch, ExecutionModeMismatch, SandboxBindingMismatch, PolicyBindingMismatch, InvalidAttestation }
internal enum Phase35CProcessModel { Isolated, Shared, HostProcess }
internal enum Phase35CNetworkPolicy { Denied, Allowlisted, Unrestricted }
internal enum Phase35CFilesystemPolicy { DedicatedOutputOnly, ReadOnlyInputs, Unrestricted }
internal enum Phase35CEnvironmentPolicy { Empty, Allowlisted, Inherited }
internal enum Phase35CCredentialAccessPolicy { None, GrantOnly, Inherited }
internal enum Phase35CCredentialClass { OpaqueReference, SecretMaterial }
internal enum Phase35CCredentialReason { None, Missing, Expired, ProviderMismatch, CapabilityMismatch, ScopeMismatch, RawSecretMaterial }
internal enum Phase35CReplayReason { None, DuplicateExecution, ModifiedRequest, StaleReplay, InvalidIdentity }
internal enum Phase35CScannerClassification { Clean, Suspicious, Malformed, Unsupported, Failure, Unknown }
internal enum Phase35CArtifactDisposition { Accepted, Rejected, Quarantined }
internal enum Phase35CExpectedValidationOutcome { Valid, Invalid }
internal enum Phase35CActivationDenialReason { ProviderUnavailable, AuthorizationDenied, ReadinessFailed, TrustMissing, TrustExpired, AttestationInvalid, SandboxPolicyFailed, CredentialGrantMissing, AuditUnavailable, ConformanceFailed, OutputCorpusNotApproved, ArtifactScannerUnavailable, ReplayProtectionUnavailable, ResourcePolicyInvalid }

internal sealed record Phase35CPolicyVersions(
    string Execution,
    string Sandbox,
    string Credential,
    string ArtifactSafety,
    string Conformance,
    string OutputCorpus);

internal sealed record Phase35CProviderIdentity(
    string ProviderId,
    string Version,
    string ImplementationIdentity,
    IReadOnlyList<Phase35ACapability> Capabilities,
    Phase35AExecutionClass ExecutionClass);

internal sealed record Phase35CProviderAttestation(
    string SchemaVersion,
    string ProviderId,
    string ProviderVersion,
    string ImplementationIdentity,
    IReadOnlyList<Phase35ACapability> Capabilities,
    Phase35AExecutionClass ExecutionClass,
    string SandboxPolicyVersion,
    Phase35CPolicyVersions PolicyVersions,
    DateTimeOffset EvaluatedAt,
    DateTimeOffset ExpiresAt);

internal sealed record Phase35CTrustEvaluation(bool IsTrusted, Phase35CTrustReason Reason, DateTimeOffset EvaluatedAt, DateTimeOffset? ExpiresAt);

internal sealed record Phase35CSandboxPolicy(
    string Version,
    Phase35CProcessModel ProcessModel,
    Phase35CNetworkPolicy Network,
    Phase35CFilesystemPolicy Filesystem,
    Phase35CEnvironmentPolicy Environment,
    Phase35CCredentialAccessPolicy CredentialAccess,
    TimeSpan MaxDuration,
    int MaxMemoryMegabytes,
    int MaxArtifactCount,
    int MaxAttempts,
    long MaxArtifactBytes,
    bool ChildProcessesAllowed,
    IReadOnlyList<string> AllowedDependencies);

internal sealed record Phase35CSandboxEvaluation(bool IsAllowed, IReadOnlyList<string> Reasons);

internal sealed record Phase35CCredentialGrant(string GrantId, Phase35CCredentialClass Classification, string ProviderId, Phase35ACapability Capability, string Scope, DateTimeOffset ExpiresAt);
internal sealed record Phase35CCredentialEvaluation(bool IsAllowed, Phase35CCredentialReason Reason);

internal sealed record Phase35CExecutionIdentity(string ExecutionId, string SessionId, string RequestHash, string Nonce);
internal sealed record Phase35CReplayEvaluation(bool IsAccepted, Phase35CReplayReason Reason);

internal sealed record Phase35CResourcePolicy(TimeSpan MaxDuration, int MaxAttempts, int MaxArtifactCount, long MaxArtifactBytes, long MaxResultBytes, int ConcurrencyLimit)
{
    internal bool IsValid => MaxDuration > TimeSpan.Zero && MaxDuration <= TimeSpan.FromHours(1) && MaxAttempts is > 0 and <= 10 && MaxArtifactCount is > 0 and <= 100 && MaxArtifactBytes is > 0 and <= 100_000_000 && MaxResultBytes is > 0 and <= 100_000_000 && ConcurrencyLimit is > 0 and <= 100;
}

internal sealed record Phase35CResourceEvaluation(bool IsAllowed, IReadOnlyList<string> Reasons);

internal sealed record Phase35CAuditEvent(string SessionId, string ProviderId, string Name, string RequestHash, [property: JsonIgnore] string Outcome)
{
    [JsonPropertyName("outcomeHash")]
    public string OutcomeHash => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Outcome))).ToLowerInvariant();
}

internal sealed record Phase35CAuditRecord(long Sequence, Phase35CAuditEvent Event, string PreviousHash, string CurrentHash, DateTimeOffset At);
internal sealed record Phase35CAuditValidation(bool IsValid, IReadOnlyList<string> Reasons);

internal sealed record Phase35CArtifactDescriptor(string ArtifactId, string RequestId, string ProviderId, Phase35AArtifactKind Kind, string ContentHash, long SizeBytes, bool IdentityValid, bool RedactionValid);
internal sealed record Phase35CArtifactSafetyPolicy(int MaxArtifactCount, long MaxArtifactBytes, IReadOnlyList<Phase35AArtifactKind> AllowedKinds);
internal sealed record Phase35CArtifactScanResult(Phase35CScannerClassification Classification, string ScannerVersion);
internal sealed record Phase35CArtifactSafetyResult(Phase35CArtifactDisposition Disposition, IReadOnlyList<string> Reasons);
internal interface IPhase35CArtifactScanner { Phase35CArtifactScanResult Scan(Phase35CArtifactDescriptor artifact); }

internal sealed record Phase35COutputCorpusFixture(string Name, string Version, IReadOnlyList<string> RequiredProperties, IReadOnlyList<string> ForbiddenProperties, Phase35CExpectedValidationOutcome ExpectedOutcome, IReadOnlyList<string> ApplicableProviders);
internal sealed record Phase35COutputValidationResult(bool IsValid, IReadOnlyList<string> Reasons);

internal sealed record Phase35CConformanceEvidence(
    bool CancellationObserved,
    bool FailureMappingDeterministic,
    bool ArtifactLineageValid,
    bool ArtifactClassified,
    bool AuditEmitted,
    bool SecretFreeDiagnostics);

internal sealed record Phase35CActivationInput(
    Phase35AProviderProfile Profile,
    Phase35AAuthorization Authorization,
    Phase35AReadinessResult Readiness,
    Phase35CTrustEvaluation Trust,
    Phase35CSandboxEvaluation Sandbox,
    Phase35CCredentialEvaluation Credential,
    Phase35CResourceEvaluation Resource,
    Phase35CConformanceResult Conformance,
    Phase35COutputValidationResult OutputValidation,
    bool AuditAvailable,
    bool ArtifactScannerAvailable,
    bool ReplayProtectionAvailable,
    Phase35CPolicyVersions PolicyVersions);

internal sealed record Phase35CActivationDecision(bool IsEligible, IReadOnlyList<Phase35CActivationDenialReason> DenialReasons, Phase35CPolicyVersions PolicyVersions);
