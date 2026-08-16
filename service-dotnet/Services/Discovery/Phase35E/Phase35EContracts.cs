using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35EContracts
{
    internal const string SandboxVersion = "phase35e-os-sandbox/v1";
}

internal enum Phase35EControlState { Enforced, Verified, Unsupported, NotApplicable }
internal enum Phase35EFailureCode { None, SandboxNotSupported, PolicyNotEnforceable, ExecutableIdentityMismatch, SandboxAdmissionDenied, ProcessCreationFailed, NetworkIsolationFailed, FilesystemIsolationFailed, EnvironmentIsolationFailed, ResourceLimitExceeded, TimedOut, Cancelled, SandboxViolation, CleanupFailed, ResultInvalid }
internal enum Phase35EExitClassification { Completed, NonZeroExit, TimedOut, Cancelled, PolicyViolation, OutputLimitExceeded, InvalidResult, ProcessCreationFailed }

internal sealed record Phase35EExecutableIdentity(string ProviderId, string ProviderVersion, string ImplementationId, string PackageId, string CertificationId, string ExecutablePath, string ExecutableSha256);
internal sealed record Phase35ESandboxPolicy(string Version, bool RequireNetworkDenial, bool RequireFilesystemIsolation, bool RequireEnvironmentIsolation, TimeSpan MaxDuration, long MaxStdoutBytes, long MaxStderrBytes, int MaxArtifactCount, bool RequireChildProcessDenial, bool RequireMemoryLimit, bool RequireCpuLimit, bool RequireProcessCountLimit);
internal sealed record Phase35EPlatformCapabilities(string Platform, bool ProcessIsolation, bool FilesystemIsolation, bool NetworkIsolation, bool EnvironmentIsolation, bool MemoryLimit, bool CpuLimit, bool ProcessCountLimit, bool SecureTermination);
internal sealed record Phase35EControl(string Name, Phase35EControlState State);
internal sealed record Phase35EPolicyBinding(string Version, IReadOnlyList<string> EnforcedControls, IReadOnlyList<string> VerifiedControls, IReadOnlyList<string> UnsupportedControls, string EnvironmentAllowlistHash, string FilesystemPolicyHash, string NetworkPolicy, string ResourceSummary)
{
    internal bool IsEnforceable => UnsupportedControls.Count == 0;
}
internal sealed record Phase35ESandboxAdmissionInput(Phase35EExecutableIdentity CertifiedIdentity, Phase35EExecutableIdentity RequestedIdentity, Phase35ESandboxPolicy Policy, Phase35EPlatformCapabilities Capabilities, bool AuditAvailable);
internal sealed record Phase35ESandboxAdmissionDecision(bool IsAllowed, IReadOnlyList<Phase35EFailureCode> Failures, Phase35EPolicyBinding? Binding);
internal sealed record Phase35ESandboxExecutionSpec(Phase35EExecutableIdentity Identity, Phase35ESandboxPolicy Policy, string SessionId, string RequestHash, string WorkingDirectory, string InputDirectory, string OutputDirectory, IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string?> Environment, TimeSpan Timeout, long MaxOutputBytes);
internal sealed record Phase35EProcessCapture(int ExitCode, string Stdout, string Stderr, bool Started);
internal sealed record Phase35ESandboxResult(string SessionId, string ProviderId, string ImplementationId, string PackageId, string CertificationId, string SandboxVersion, string Platform, Phase35EExitClassification ExitClassification, Phase35EFailureCode? Failure, long StdoutBytes, long StderrBytes, IReadOnlyList<Phase35EFailureCode> Violations, bool CleanupSucceeded);
internal sealed record Phase35ESandboxEvidence(string CanonicalPayload, string EvidenceHash);
internal interface IPhase35EProcessBoundary
{
    Task<Phase35EProcessCapture> StartAsync(Phase35ESandboxExecutionSpec spec, CancellationToken cancellationToken);
    Task TerminateAsync(Phase35EProcessCapture process, CancellationToken cancellationToken);
}

internal static class Phase35EHashing
{
    internal static string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)))).ToLowerInvariant();
}
