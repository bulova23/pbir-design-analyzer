using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35AContracts
{
    internal const string ContractVersion = "phase35a-generation-provider/v1";
    internal const string ProviderProfileV1 = "phase35a-provider-profile/v1";
    internal const string RequestV1 = "phase35a-generation-request/v1";
    internal const string AuthorizationV1 = "phase35a-authorization/v1";
    internal const string PolicyV1 = "phase35a-execution-policy/v1";
    internal const string ReadinessV1 = "phase35a-provider-readiness/v1";
    internal const string ReceiptV1 = "phase35a-execution-receipt/v1";
    internal const string ResultV1 = "phase35a-provider-result/v1";
    internal const string ArtifactV1 = "phase35a-artifact/v1";
    internal const string FailureV1 = "phase35a-failure/v1";
    internal const string LineageV1 = "phase35a-lineage/v1";
    internal const string RetryV1 = "phase35a-retry-policy/v1";
    internal const string RedactionV1 = "phase35a-redaction/v1";
    internal const string QuarantineV1 = "phase35a-quarantine/v1";
}

internal enum Phase35AProviderCategory { LocalInspection, LaterVerification, SemanticModelOnly, MetadataOnly, OfflineTest }
internal enum Phase35AExecutionClass { NonExecutable, DeferredRuntime, Executable }
internal enum Phase35ATrustClassification { TrustedContract, LocalReadOnly, ExternalUntrusted, Untrusted }
internal enum Phase35AArtifactKind { PbirReport, SemanticModel, VerificationRecord, OfflineFixture }
internal enum Phase35ACapability { PbirValidation, PbirMetadataInspection, SemanticModelInspection, DesktopVerification, PbirGeneration }
internal enum Phase35AReadinessRequirement { ExplicitExecutableRegistration, Authorization, PolicyApproval, OutputValidation, LineageIntegrity }
internal enum Phase35AReadiness { Unavailable, Blocked, ReadyForExecution }
internal enum Phase35AAuthorizationStatus { Denied, Approved }
internal enum Phase35ALifecycleState { Requested, Authorized, Accepted, Running, Completed, Failed, Quarantined, Rejected }
internal enum Phase35ALifecycleEvent { AuthorizationApproved, RequestAccepted, ExecutionStarted, ExecutionCompleted, ExecutionFailed, OutputQuarantined, RequestRejected }
internal enum Phase35AResultStatus { Accepted, Rejected, Failed, Quarantined }
internal enum Phase35AValidationStatus { NotValidated, Valid, Invalid }
internal enum Phase35AFailureClass { Validation, Authorization, Readiness, PolicyViolation, ProviderFailure, ArtifactValidation, LineageProvenance, Quarantine }
internal enum Phase35ARetryDisposition { NonRetryable, Retryable }
internal enum Phase35ARedactionStatus { NotRequired, Required, Applied }
internal enum Phase35AQuarantineReason { None, ValidationFailure, PolicyViolation, UnsafeContent, LineageFailure, UnknownOutput }

internal sealed record Phase35AProviderProfile(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("category")] Phase35AProviderCategory Category,
    [property: JsonPropertyName("executionClass")] Phase35AExecutionClass ExecutionClass,
    [property: JsonPropertyName("trust")] Phase35ATrustClassification Trust,
    [property: JsonPropertyName("capabilities")] IReadOnlyList<Phase35ACapability> Capabilities,
    [property: JsonPropertyName("artifactKinds")] IReadOnlyList<Phase35AArtifactKind> ArtifactKinds,
    [property: JsonPropertyName("readinessRequirements")] IReadOnlyList<Phase35AReadinessRequirement> ReadinessRequirements);

internal sealed record Phase35ARequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("intentReference")] string IntentReference,
    [property: JsonPropertyName("authoritativeInputReferences")] IReadOnlyList<string> AuthoritativeInputReferences,
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<Phase35ACapability> RequiredCapabilities,
    [property: JsonPropertyName("artifactKind")] Phase35AArtifactKind ArtifactKind,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("authoritativeInputHash")] string AuthoritativeInputHash,
    [property: JsonPropertyName("policyHash")] string PolicyHash);

internal sealed record Phase35AAuthorization(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("status")] Phase35AAuthorizationStatus Status,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("capabilities")] IReadOnlyList<Phase35ACapability> Capabilities,
    [property: JsonPropertyName("artifactKind")] Phase35AArtifactKind ArtifactKind,
    [property: JsonPropertyName("policyHash")] string PolicyHash)
{
    internal static Phase35AAuthorization Denied { get; } = new(Phase35AContracts.AuthorizationV1, Phase35AAuthorizationStatus.Denied, string.Empty, string.Empty, [], Phase35AArtifactKind.PbirReport, string.Empty);
}

internal sealed record Phase35ARetryPolicy(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("maxAttempts")] int MaxAttempts,
    [property: JsonPropertyName("retryableFailures")] IReadOnlyList<Phase35AFailureClass> RetryableFailures)
{
    internal static Phase35ARetryPolicy None { get; } = new(Phase35AContracts.RetryV1, 1, []);
    internal bool IsRetryable(Phase35AFailureClass failureClass) => MaxAttempts > 1 && RetryableFailures.Contains(failureClass);
}

internal sealed record Phase35AExecutionPolicy(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("allowedProviderId")] string AllowedProviderId,
    [property: JsonPropertyName("allowedCapabilities")] IReadOnlyList<Phase35ACapability> AllowedCapabilities,
    [property: JsonPropertyName("allowedArtifactKinds")] IReadOnlyList<Phase35AArtifactKind> AllowedArtifactKinds,
    [property: JsonPropertyName("executionPermitted")] bool ExecutionPermitted,
    [property: JsonPropertyName("mutationPermitted")] bool MutationPermitted,
    [property: JsonPropertyName("retryPolicy")] Phase35ARetryPolicy RetryPolicy,
    [property: JsonPropertyName("redactionRequired")] bool RedactionRequired,
    [property: JsonPropertyName("quarantineRequired")] bool QuarantineRequired,
    [property: JsonPropertyName("lineageRequired")] bool LineageRequired)
{
    internal static Phase35AExecutionPolicy Denied { get; } = new(Phase35AContracts.PolicyV1, string.Empty, [], [], false, false, Phase35ARetryPolicy.None, true, true, true);
}

internal sealed record Phase35AReadinessResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("state")] Phase35AReadiness State,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record Phase35AExecutionReceipt(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("receiptId")] string ReceiptId,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("authorizationHash")] string AuthorizationHash,
    [property: JsonPropertyName("policyHash")] string PolicyHash,
    [property: JsonPropertyName("resultId")] string ResultId,
    [property: JsonPropertyName("lifecycleState")] Phase35ALifecycleState LifecycleState,
    [property: JsonPropertyName("lineage")] Phase35ALineage Lineage,
    [property: JsonPropertyName("receiptHash")] string ReceiptHash);

internal sealed record Phase35AFailure(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("class")] Phase35AFailureClass Class,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("retryable")] bool Retryable);

internal sealed record Phase35AResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("resultId")] string ResultId,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("status")] Phase35AResultStatus Status,
    [property: JsonPropertyName("failures")] IReadOnlyList<Phase35AFailure> Failures);

internal sealed record Phase35ARedaction(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("status")] Phase35ARedactionStatus Status,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("originalHashReference")] string OriginalHashReference)
{
    internal static Phase35ARedaction None { get; } = new(Phase35AContracts.RedactionV1, Phase35ARedactionStatus.NotRequired, string.Empty, string.Empty);
}

internal sealed record Phase35AQuarantine(
    [property: JsonPropertyName("reason")] Phase35AQuarantineReason Reason,
    [property: JsonPropertyName("reference")] string Reference,
    [property: JsonPropertyName("releaseEligible")] bool ReleaseEligible)
{
    internal static Phase35AQuarantine None { get; } = new(Phase35AQuarantineReason.None, string.Empty, true);
}

internal sealed record Phase35AArtifact(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("artifactId")] string ArtifactId,
    [property: JsonPropertyName("kind")] Phase35AArtifactKind Kind,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("contractVersion")] string ContractVersion,
    [property: JsonPropertyName("contentHash")] string ContentHash,
    [property: JsonPropertyName("resultId")] string ResultId,
    [property: JsonPropertyName("lineage")] Phase35ALineage Lineage,
    [property: JsonPropertyName("validation")] Phase35AValidationStatus Validation,
    [property: JsonPropertyName("quarantine")] Phase35AQuarantine Quarantine,
    [property: JsonPropertyName("redaction")] Phase35ARedaction Redaction);

internal sealed record Phase35ALineage(
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("resultId")] string ResultId,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("requestHash")] string RequestHash,
    [property: JsonPropertyName("resultHash")] string ResultHash)
{
    internal static Phase35ALineage From(Phase35ARequest request, Phase35AResult result) => new(
        request.RequestId, result.ResultId, request.ProviderId, new Phase35ACanonicalJson().Hash(request), new Phase35ACanonicalJson().Hash(result));
}

internal sealed record Phase35AValidation(
    IReadOnlyList<string> UnsupportedSchemaVersions,
    IReadOnlyList<string> UnsupportedCapabilities,
    IReadOnlyList<string> InvalidReferences,
    IReadOnlyList<string> PolicyViolations,
    IReadOnlyList<string> InvalidValues)
{
    internal bool IsValid => UnsupportedSchemaVersions.Count == 0 && UnsupportedCapabilities.Count == 0 && InvalidReferences.Count == 0 && PolicyViolations.Count == 0 && InvalidValues.Count == 0;
}

internal sealed class Phase35AContractException(string message) : InvalidOperationException(message);
