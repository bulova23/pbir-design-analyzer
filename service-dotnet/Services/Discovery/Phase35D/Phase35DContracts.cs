using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35DContracts
{
    internal const string ContractVersion = "phase35d-provider-certification/v1";
    internal const string CandidateV1 = "phase35d-certification-candidate/v1";
    internal const string PackageIdentityV1 = "phase35d-package-identity/v1";
    internal const string AttestationV1 = "phase35d-signed-attestation/v1";
    internal const string ProfileV1 = "phase35d-certification-profile/v1";
    internal const string EvidenceV1 = "phase35d-certification-evidence/v1";
    internal const string RecordV1 = "phase35d-certification-record/v1";
    internal const string PersistenceV1 = "phase35d-protected-persistence/v1";
}

internal enum Phase35DSignatureAlgorithm { RsaSha256 }
internal enum Phase35DSignerStatus { Approved, Expired, Revoked, Unapproved }
internal enum Phase35DAttestationReason { None, Missing, Malformed, UnsupportedAlgorithm, InvalidSignature, PackageHashMismatch, IdentityMismatch, SignerMismatch, SignerExpired, SignerRevoked, SignerUnapproved }
internal enum Phase35DCertificationState { Candidate, EvidenceCollected, Verified, Certified, Rejected, Expired, Revoked, Superseded, Invalidated }
internal enum Phase35DAdmission { Certified, PreProductionEligible, ProductionEligible }
internal enum Phase35DCertificationDenialReason { PackageIdentityMismatch, SignatureInvalid, SignerUnapproved, SignerExpired, SignerRevoked, TrustFailed, SandboxPolicyFailed, CredentialPolicyFailed, ConformanceFailed, OutputCorpusFailed, AuditUnavailable, ReplayProtectionUnavailable, ResourcePolicyInvalid, CertificationStale, PolicyVersionMismatch, EvidenceIncomplete, ProductionNotAuthorized }

internal sealed record Phase35DPackageMetadata(
    [property: JsonPropertyName("packageVersion")] string PackageVersion,
    [property: JsonPropertyName("packageSha256")] string PackageSha256,
    [property: JsonPropertyName("manifestSha256")] string ManifestSha256,
    [property: JsonPropertyName("implementationSha256")] string ImplementationSha256,
    [property: JsonPropertyName("signerIdentity")] string SignerIdentity,
    [property: JsonPropertyName("signatureAlgorithm")] Phase35DSignatureAlgorithm SignatureAlgorithm,
    [property: JsonPropertyName("certificateOrKeyId")] string CertificateOrKeyId,
    [property: JsonPropertyName("buildProvenance")] string BuildProvenance);

internal sealed record Phase35DPackageIdentity(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("implementationId")] string ImplementationId,
    [property: JsonPropertyName("packageId")] string PackageId,
    [property: JsonPropertyName("metadata")] Phase35DPackageMetadata Metadata,
    [property: JsonPropertyName("identityHash")] string IdentityHash);

internal sealed record Phase35DCertificationCandidate(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("implementationId")] string ImplementationId,
    [property: JsonPropertyName("package")] Phase35DPackageIdentity Package,
    [property: JsonPropertyName("expectedSignerIdentity")] string ExpectedSignerIdentity,
    [property: JsonPropertyName("capabilities")] IReadOnlyList<Phase35ACapability> Capabilities,
    [property: JsonPropertyName("executionClass")] Phase35AExecutionClass ExecutionClass,
    [property: JsonPropertyName("sandboxPolicy")] Phase35CSandboxPolicy SandboxPolicy,
    [property: JsonPropertyName("policyVersions")] Phase35CPolicyVersions PolicyVersions,
    [property: JsonPropertyName("outputCorpusVersion")] string OutputCorpusVersion,
    [property: JsonPropertyName("conformanceProfileVersion")] string ConformanceProfileVersion);

internal sealed record Phase35DSignedAttestation(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("candidate")] Phase35DCertificationCandidate Candidate,
    [property: JsonPropertyName("signerIdentity")] string SignerIdentity,
    [property: JsonPropertyName("signerNotBefore")] DateTimeOffset SignerNotBefore,
    [property: JsonPropertyName("signerNotAfter")] DateTimeOffset SignerNotAfter,
    [property: JsonPropertyName("signatureAlgorithm")] Phase35DSignatureAlgorithm SignatureAlgorithm,
    [property: JsonPropertyName("signedPayload")] string SignedPayload,
    [property: JsonPropertyName("signature")] string SignatureBase64);

internal sealed record Phase35DCertificationProfile(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("profileId")] string ProfileId,
    [property: JsonPropertyName("approvedSignerIdentities")] IReadOnlyList<string> ApprovedSignerIdentities,
    [property: JsonPropertyName("revokedSignerIdentities")] IReadOnlyList<string> RevokedSignerIdentities,
    [property: JsonPropertyName("requiredExecutionClass")] Phase35AExecutionClass RequiredExecutionClass,
    [property: JsonPropertyName("requiredSandboxPolicyVersion")] string RequiredSandboxPolicyVersion,
    [property: JsonPropertyName("requiredOutputCorpusVersion")] string RequiredOutputCorpusVersion,
    [property: JsonPropertyName("requiredConformanceProfileVersion")] string RequiredConformanceProfileVersion,
    [property: JsonPropertyName("validFor")] TimeSpan ValidFor);

internal sealed record Phase35DAttestationVerification(bool IsValid, Phase35DAttestationReason Reason, string SignerIdentity, DateTimeOffset? ExpiresAt);
internal sealed record Phase35DProviderConformanceResult(bool IsConformant, Phase35CConformanceResult Runtime, Phase35COutputValidationResult Output, IReadOnlyList<string> Reasons);

internal sealed record Phase35DCertificationInput(
    Phase35DCertificationCandidate Candidate,
    Phase35DCertificationProfile Profile,
    Phase35DSignedAttestation? SignedAttestation,
    string SignerPublicKeyBase64,
    Phase35CProviderAttestation? TrustAttestation,
    Phase35AProviderProfile ProviderProfile,
    Phase35ARequest Request,
    IPhase35BProviderAdapter Adapter,
    Phase35CConformanceEvidence ConformanceEvidence,
    Phase35COutputCorpusFixture OutputFixture,
    IReadOnlyList<string> OutputProperties,
    Phase35CSandboxPolicy ApprovedSandboxPolicy,
    Phase35CCredentialEvaluation Credential,
    Phase35CResourcePolicy ResourcePolicy,
    bool AuditAvailable,
    bool ReplayProtectionAvailable,
    bool ArtifactSafetyAvailable);

internal sealed record Phase35DCertificationEvidence(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("candidateIdentity")] Phase35DPackageIdentity CandidateIdentity,
    [property: JsonPropertyName("attestation")] Phase35DAttestationVerification Attestation,
    [property: JsonPropertyName("trust")] Phase35CTrustEvaluation Trust,
    [property: JsonPropertyName("sandbox")] Phase35CSandboxEvaluation Sandbox,
    [property: JsonPropertyName("credential")] Phase35CCredentialEvaluation Credential,
    [property: JsonPropertyName("resource")] Phase35CResourceEvaluation Resource,
    [property: JsonPropertyName("conformance")] Phase35DProviderConformanceResult Conformance,
    [property: JsonPropertyName("auditAvailable")] bool AuditAvailable,
    [property: JsonPropertyName("replayProtectionAvailable")] bool ReplayProtectionAvailable,
    [property: JsonPropertyName("artifactSafetyAvailable")] bool ArtifactSafetyAvailable,
    [property: JsonPropertyName("policyVersions")] Phase35CPolicyVersions PolicyVersions,
    [property: JsonPropertyName("collectedAt")] DateTimeOffset CollectedAt,
    [property: JsonPropertyName("evidenceHash")] string EvidenceHash);

internal sealed record Phase35DCertificationDecision(bool IsCertified, bool IsPreProductionEligible, IReadOnlyList<Phase35DCertificationDenialReason> Reasons, Phase35DCertificationEvidence Evidence);

internal sealed record Phase35DCertificationRecord(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("certificationId")] string CertificationId,
    [property: JsonPropertyName("candidate")] Phase35DCertificationCandidate Candidate,
    [property: JsonPropertyName("profileId")] string ProfileId,
    [property: JsonPropertyName("evidenceHash")] string EvidenceHash,
    [property: JsonPropertyName("issuedAt")] DateTimeOffset IssuedAt,
    [property: JsonPropertyName("expiresAt")] DateTimeOffset ExpiresAt,
    [property: JsonPropertyName("state")] Phase35DCertificationState State,
    [property: JsonPropertyName("revocationReason")] string? RevocationReason,
    [property: JsonPropertyName("supersedes")] string? Supersedes,
    [property: JsonPropertyName("supersededBy")] string? SupersededBy,
    [property: JsonPropertyName("policyVersions")] Phase35CPolicyVersions PolicyVersions);

internal sealed record Phase35DActivationBindingDecision(bool IsEligible, Phase35DAdmission Admission, IReadOnlyList<Phase35DCertificationDenialReason> Reasons);

internal sealed record Phase35DPersistedAuditRecord(
    [property: JsonPropertyName("sequence")] long Sequence,
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("requestHash")] string RequestHash,
    [property: JsonPropertyName("outcomeHash")] string OutcomeHash,
    [property: JsonPropertyName("previousHash")] string PreviousHash,
    [property: JsonPropertyName("currentHash")] string CurrentHash,
    [property: JsonPropertyName("at")] DateTimeOffset At);

internal sealed record Phase35DPersistedState(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("auditRecords")] IReadOnlyList<Phase35DPersistedAuditRecord> AuditRecords,
    [property: JsonPropertyName("replayIdentities")] IReadOnlyList<Phase35CExecutionIdentity> ReplayIdentities,
    [property: JsonPropertyName("stateHash")] string StateHash);
