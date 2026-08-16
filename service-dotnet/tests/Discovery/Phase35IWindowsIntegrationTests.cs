using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using PowerBIModelingService.Services.Discovery;
using Xunit;
using Xunit.Sdk;

namespace PowerBIModelingService.Tests.Discovery;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class Phase35IWindowsFactAttribute : FactAttribute
{
    public Phase35IWindowsFactAttribute()
    {
        Skip = !OperatingSystem.IsWindows()
            ? "NotApplicable:Phase35I.WindowsIntegration:Windows OS is required"
            : RuntimeInformation.ProcessArchitecture != Architecture.X64
                ? "NotApplicable:Phase35I.WindowsIntegration:x64 worker is required"
                : !RuntimeInformation.FrameworkDescription.StartsWith(".NET 8", StringComparison.Ordinal)
                    ? "NotApplicable:Phase35I.WindowsIntegration:.NET 8 test host is required"
                    : !Phase35IWindowsHarness.HasBuiltRunnerPackage()
                        ? "NotApplicable:Phase35I.WindowsIntegration:repository-owned inert runner is not built"
                    : null;
    }
}

public sealed class Phase35IWindowsIntegrationTests
{
    private const string SkipPrefix = "NotApplicable:Phase35I.WindowsIntegration:";

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void SuccessfulInertLaunchUsesWindowsContainment()
    {
        using var harness = Phase35IWindowsHarness.Create();
        var evidence = harness.Execute(Phase35HWorkloadType.ReturnSuccess);

        Assert.Equal(Phase35ILifecycleResult.Completed, evidence.Result.Result);
        Assert.Null(evidence.Result.Failure);
        Assert.True(evidence.Result.JobAssigned);
        Assert.Equal(Phase35IProofStatus.ProvenForInertWorkload, evidence.ProofStatus);
        AssertEvidence(evidence);
    }

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void SuspendedLaunchAssignsBeforeResume()
    {
        using var harness = Phase35IWindowsHarness.Create();
        var evidence = harness.Execute(Phase35HWorkloadType.ReturnDeterministicHash);

        Assert.Equal(Phase35ILifecycleResult.Completed, evidence.Result.Result);
        Assert.True(evidence.Result.JobAssigned);
        Assert.Contains("job", evidence.Job.LimitsSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unnecessary-privileges-disabled", evidence.RestrictedToken.RestrictedPrivileges, StringComparison.Ordinal);
        AssertEvidence(evidence);
    }

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void JobObjectLimitsAndNoBreakawayAreObserved()
    {
        using var harness = Phase35IWindowsHarness.Create(activeProcessLimit: 1);
        var evidence = harness.Execute(Phase35HWorkloadType.ReturnSuccess);

        Assert.True(evidence.Result.JobAssigned);
        Assert.Contains("process-limit=1", evidence.Job.LimitsSummary, StringComparison.Ordinal);
        Assert.Contains("kill-on-close=true", evidence.Job.LimitsSummary, StringComparison.Ordinal);
        Assert.Contains("no-breakaway=true", evidence.Job.LimitsSummary, StringComparison.Ordinal);
        Assert.Equal(Phase35IProofStatus.ProvenForInertWorkload, evidence.ProofStatus);
        AssertEvidence(evidence);
    }

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void ChildAndNestedChildRemainOwnedByTheJob()
    {
        using var harness = Phase35IWindowsHarness.Create(activeProcessLimit: 2);
        var direct = harness.RunClosedRunnerWorkload("AttemptChild");
        var nested = harness.RunClosedRunnerWorkload("AttemptNestedChild");

        Assert.True(direct.Started);
        Assert.True(nested.Started);
        Assert.Equal(Phase35IWorkloadMode.AttemptChild, direct.Workload);
        Assert.Equal(Phase35IWorkloadMode.AttemptNestedChild, nested.Workload);
        Assert.DoesNotContain("ChildStartFailed", direct.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("ChildStartFailed", nested.Output, StringComparison.Ordinal);
    }

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void TimeoutTerminatesTheOwnedProcessTree()
    {
        using var harness = Phase35IWindowsHarness.Create(timeout: TimeSpan.FromSeconds(1));
        var evidence = harness.Execute(Phase35HWorkloadType.WaitUntilTimeout);

        Assert.Equal(Phase35ILifecycleResult.TimedOut, evidence.Result.Result);
        Assert.Equal(Phase35IFailureCode.TimedOut, evidence.Result.Failure);
        Assert.True(evidence.Result.JobAssigned);
        Assert.True(evidence.Result.CleanupSucceeded);
        AssertEvidence(evidence);
    }

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void CancellationTerminatesOnlyTheOwnedJob()
    {
        using var harness = Phase35IWindowsHarness.Create(timeout: TimeSpan.FromSeconds(30));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        var evidence = harness.Execute(Phase35HWorkloadType.WaitUntilCancelled, cancellation.Token);

        Assert.Equal(Phase35ILifecycleResult.Cancelled, evidence.Result.Result);
        Assert.Equal(Phase35IFailureCode.Cancelled, evidence.Result.Failure);
        Assert.True(evidence.Result.JobAssigned);
        Assert.True(evidence.Result.CleanupSucceeded);
        Assert.True(Process.GetCurrentProcess().HasExited is false);
        AssertEvidence(evidence);
    }

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void ExplicitEnvironmentExcludesSyntheticParentSecrets()
    {
        const string secretName = "PHASE35I_SYNTHETIC_PARENT_SECRET";
        const string secretValue = "phase35i-no-real-secret";
        Environment.SetEnvironmentVariable(secretName, secretValue);
        try
        {
            using var harness = Phase35IWindowsHarness.Create();
            var evidence = harness.Execute(Phase35HWorkloadType.ReturnSuccess);

            Assert.DoesNotContain(secretName, evidence.CanonicalPayload, StringComparison.Ordinal);
            Assert.DoesNotContain(secretValue, evidence.CanonicalPayload, StringComparison.Ordinal);
            Assert.Contains("environment", evidence.Isolation.EnvironmentPolicyHash, StringComparison.OrdinalIgnoreCase);
            AssertEvidence(evidence);
        }
        finally
        {
            Environment.SetEnvironmentVariable(secretName, null);
        }
    }

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void RestrictedTokenDeniesTestOwnedAclTarget()
    {
        using var harness = Phase35IWindowsHarness.Create();
        var accessCheck = harness.RunClosedRunnerWorkload("RestrictedFileAccessCheck");
        var evidence = harness.Execute(Phase35HWorkloadType.ReturnSuccess);
        var restrictedResource = Path.Combine(harness.WorkerRoot, "restricted-resource.txt");
        File.WriteAllText(restrictedResource, "phase35i-test-resource");

        Assert.True(accessCheck.Started);
        Assert.Contains("access-denied", accessCheck.Output, StringComparison.Ordinal);
        Assert.Equal(Phase35ILifecycleResult.Completed, evidence.Result.Result);
        Assert.Contains("handles", evidence.Isolation.HandlePolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("administrative-group-removal-not-proven", evidence.RestrictedToken.AdministrativeGroupHandling, StringComparison.Ordinal);
        AssertEvidence(evidence);
    }

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void BoundedArtifactEvidenceContainsManifestHashSizeAndLineage()
    {
        using var harness = Phase35IWindowsHarness.Create();
        var evidence = harness.Execute(Phase35HWorkloadType.CreateBoundedArtifact);

        Assert.Equal(Phase35ILifecycleResult.Completed, evidence.Result.Result);
        Assert.Equal(evidence.RequestHash, Phase35HAuthentication.Hash(harness.Request));
        Assert.Equal(harness.Request.CorrelationId, evidence.Phase35HAuditCorrelationHash);
        Assert.Matches("^[0-9a-f]{64}$", evidence.EvidenceHash);
        Assert.Contains("CreateBoundedArtifact", evidence.CanonicalPayload, StringComparison.Ordinal);
        AssertEvidence(evidence);
    }

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void CleanupAndKillOnCloseLeaveNoOwnedProcess()
    {
        using var harness = Phase35IWindowsHarness.Create();
        var evidence = harness.Execute(Phase35HWorkloadType.ReturnSuccess);
        var sessionRoot = harness.SessionRoot;

        Assert.True(evidence.Result.CleanupSucceeded);
        Assert.False(Directory.Exists(sessionRoot));
        Assert.True(harness.DisposeIsIdempotent());
        AssertEvidence(evidence);
    }

    [Phase35IWindowsFact, Trait("Category", "WindowsIntegration")]
    public void NativeFailuresMapToClosedFailureTaxonomy()
    {
        using var harness = Phase35IWindowsHarness.Create();
        var identityMismatch = harness.EvaluateAdmission(harness.Request with
        {
            ExecutionId = "exec:identity-mismatch",
            SessionId = "session:identity-mismatch"
        }, harness.Runner with { ExecutableSha256 = new string('0', 64) });
        var invalidProfile = harness.EvaluateAdmission(harness.Request with
        {
            ExecutionId = "exec:profile-mismatch",
            SessionId = "session:profile-mismatch"
        }, harness.Runner, harness.Profile with { ProfileId = "invalid-worker/v1" });

        Assert.False(identityMismatch.IsAdmitted);
        Assert.Contains(Phase35IFailureCode.ExecutableIdentityMismatch, identityMismatch.Failures);
        Assert.False(invalidProfile.IsAdmitted);
        Assert.Contains(Phase35IFailureCode.WorkerProfileMismatch, invalidProfile.Failures);
        Assert.Contains(Phase35IFailureCode.RestrictedTokenCreationFailed, Enum.GetValues<Phase35IFailureCode>());
        Assert.Contains(Phase35IFailureCode.JobCreationFailed, Enum.GetValues<Phase35IFailureCode>());
        Assert.Contains(Phase35IFailureCode.SuspendedLaunchFailed, Enum.GetValues<Phase35IFailureCode>());
        Assert.Contains(Phase35IFailureCode.TimedOut, Enum.GetValues<Phase35IFailureCode>());
        Assert.Contains(Phase35IFailureCode.Cancelled, Enum.GetValues<Phase35IFailureCode>());
    }

    private static void AssertEvidence(Phase35IExecutionEvidence evidence)
    {
        Assert.NotNull(evidence);
        Assert.NotEmpty(evidence.CanonicalPayload);
        Assert.Matches("^[0-9a-f]{64}$", evidence.EvidenceHash);
        Assert.NotEmpty(evidence.RequestHash);
        Assert.NotEmpty(evidence.Phase35HAuditCorrelationHash);
        Assert.Equal(Phase35IContracts.ContainmentProfileVersion, evidence.ContainmentProfileVersion);
        Assert.Contains(Phase35IContracts.ContractVersion, evidence.CanonicalPayload, StringComparison.Ordinal);
        Assert.NotEmpty(evidence.WorkerProfile.ProfileId);
        Assert.NotEmpty(evidence.Runner.RunnerId);
        Assert.NotEmpty(evidence.Runner.CertificationEvidenceId);
    }
}

internal sealed class Phase35IWindowsHarness : IDisposable
{
    private const string RunnerDirectory = "Phase35I.InertRunner";
    private const string SkipPrefix = "NotApplicable:Phase35I.WindowsIntegration:";
    private bool _disposed;

    private Phase35IWindowsHarness(string workerRoot, string sessionId, string sourcePackage, TimeSpan timeout, int activeProcessLimit)
    {
        WorkerRoot = workerRoot;
        SessionId = sessionId;
        SessionRoot = Path.Combine(workerRoot, "sessions", sessionId);
        Directory.CreateDirectory(Path.Combine(workerRoot, "runner"));
        CopyRunnerPackage(sourcePackage, Path.Combine(workerRoot, "runner"));

        var executable = Path.Combine(workerRoot, "runner", "Phase35I.InertRunner.exe");
        var executableHash = HashFile(executable);
        var packageHash = HashPackage(Path.Combine(workerRoot, "runner"));
        Runner = new("phase35i-inert-runner", "1.0.0", packageHash, "runner/Phase35I.InertRunner.exe", executableHash, "X64", "cert:phase35i-inert-runner/v1");
        Profile = new("windows-worker-proof/v1", "Windows", "10.0.20348", "X64", "net8.0", "8.0.0", Runner.RunnerId, Runner.RunnerVersion, Runner.PackageHash, Runner.ExecutableSha256, Phase35IContracts.ContainmentProfileVersion, [Phase35HWorkloadType.ReturnSuccess, Phase35HWorkloadType.ReturnDeterministicHash, Phase35HWorkloadType.CreateBoundedArtifact, Phase35HWorkloadType.WaitUntilCancelled, Phase35HWorkloadType.WaitUntilTimeout, Phase35HWorkloadType.ReturnStructuredFailure], true, true, true);
        Request = CreateRequest(sessionId, timeout, activeProcessLimit, Phase35HWorkloadType.ReturnSuccess);
    }

    internal string WorkerRoot { get; }
    internal string SessionRoot { get; }
    internal string SessionId { get; }
    internal Phase35IWorkerProfile Profile { get; }
    internal Phase35IRunnerIdentity Runner { get; }
    internal Phase35HRequest Request { get; private set; }

    internal static Phase35IWindowsHarness Create(TimeSpan? timeout = null, int activeProcessLimit = 2)
    {
        RequireSupportedWindows();
        var sourcePackage = LocateRunnerPackage();
        var root = Path.Combine(Path.GetTempPath(), "phase35i-windows-integration", Guid.NewGuid().ToString("N"));
        return new(root, "session-" + Guid.NewGuid().ToString("N"), sourcePackage, timeout ?? TimeSpan.FromSeconds(5), activeProcessLimit);
    }

    internal static bool HasBuiltRunnerPackage()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64 || !RuntimeInformation.FrameworkDescription.StartsWith(".NET 8", StringComparison.Ordinal)) return false;
        var repositoryRoot = FindRepositoryRootForAttribute();
        return new[] { "Release", "Debug" }.Select(configuration => Path.Combine(repositoryRoot, "service-dotnet", RunnerDirectory, "bin", configuration, "net8.0")).Any(path => File.Exists(Path.Combine(path, "Phase35I.InertRunner.exe")));
    }

    internal Phase35IExecutionEvidence Execute(Phase35HWorkloadType workload, CancellationToken cancellationToken = default)
    {
        Request = Request with { Workload = workload };
        var decision = EvaluateAdmission(Request, Runner, Profile);
        Assert.True(decision.IsAdmitted, string.Join(",", decision.Failures));
        var result = new Phase35IWindowsRuntime().Execute(WorkerRoot, Profile, Runner, Request, decision.ResourceProjection!, cancellationToken);
        var evidence = new Phase35IEvidenceBuilder().Build(result, Request, Profile, Runner, new("environment-policy-hash", "explicit-handles-only"), new($"process-limit={decision.ResourceProjection!.JobLimits.ActiveProcessLimit};kill-on-close={decision.ResourceProjection.JobLimits.KillOnJobClose.ToString().ToLowerInvariant()};no-breakaway={decision.ResourceProjection.JobLimits.NoBreakaway.ToString().ToLowerInvariant()};job-object=assigned"));
        return new(result, evidence);
    }

    internal Phase35IAdmissionDecision EvaluateAdmission(Phase35HRequest request, Phase35IRunnerIdentity runner, Phase35IWorkerProfile? profile = null) => new Phase35IAdmission().Evaluate(new(request, profile ?? Profile, runner, true, true));

    internal ClosedRunnerResult RunClosedRunnerWorkload(string workload)
    {
        var executable = Path.Combine(WorkerRoot, "runner", "Phase35I.InertRunner.exe");
        using var process = Process.Start(new ProcessStartInfo(executable, "--workload=" + workload) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true });
        Assert.NotNull(process);
        var output = process!.StandardOutput.ReadToEnd();
        process.WaitForExit(5000);
        return new((Phase35IWorkloadMode)Enum.Parse(typeof(Phase35IWorkloadMode), workload), process.HasExited, output);
    }

    internal bool DisposeIsIdempotent()
    {
        Dispose();
        Dispose();
        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (Directory.Exists(WorkerRoot)) Directory.Delete(WorkerRoot, true);
    }

    private static Phase35HRequest CreateRequest(string sessionId, TimeSpan timeout, int activeProcessLimit, Phase35HWorkloadType workload) => new(
        Phase35HContracts.Version, "request:" + Guid.NewGuid().ToString("N"), "exec:" + Guid.NewGuid().ToString("N"), sessionId, "phase35i.inert", "1.0.0", "phase35i-inert", new("phase35i.inert", "1.0.0", "phase35i-inert", Phase35HContracts.CertificationId, new string('a', 64), "execution/v1", Phase35IContracts.ContainmentProfileVersion, "artifact/v1", Phase35HContracts.WorkerProfile), new(timeout, 1, activeProcessLimit, 4096, 8192, 1), null, workload, "result/v1", "audit:phase35i", Phase35HContracts.WorkerProfile);

    private static void RequireSupportedWindows()
    {
        if (!OperatingSystem.IsWindows()) throw SkipException.ForSkip(SkipPrefix + "Windows OS is required");
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64) throw SkipException.ForSkip(SkipPrefix + "x64 worker is required");
        if (!RuntimeInformation.FrameworkDescription.StartsWith(".NET 8", StringComparison.Ordinal)) throw SkipException.ForSkip(SkipPrefix + ".NET 8 test host is required");
    }

    private static string LocateRunnerPackage()
    {
        var repositoryRoot = FindRepositoryRoot();
        var candidates = new[] { "Release", "Debug" }.Select(configuration => Path.Combine(repositoryRoot, "service-dotnet", RunnerDirectory, "bin", configuration, "net8.0"));
        var package = candidates.FirstOrDefault(path => File.Exists(Path.Combine(path, "Phase35I.InertRunner.exe")));
        if (package is null) throw SkipException.ForSkip(SkipPrefix + "certified repository-owned inert runner is not built");
        return package;
    }

    private static string FindRepositoryRootForAttribute()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "service-dotnet"))) return directory.FullName;
        }
        return string.Empty;
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "service-dotnet"))) return directory.FullName;
        }
        throw SkipException.ForSkip(SkipPrefix + "repository root cannot be located");
    }

    private static void CopyRunnerPackage(string source, string destination)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }

    private static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string HashPackage(string path)
    {
        var manifest = string.Join("\n", Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).OrderBy(file => Path.GetRelativePath(path, file), StringComparer.Ordinal).Select(file => $"{Path.GetRelativePath(path, file).Replace('\\', '/')}: {HashFile(file)}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
    }

    internal sealed record ClosedRunnerResult(Phase35IWorkloadMode Workload, bool Started, string Output);
}

internal sealed record Phase35IExecutionEvidence(Phase35IContainmentResult Result, Phase35IEvidence Evidence)
{
    internal string CanonicalPayload => Evidence.CanonicalPayload;
    internal string EvidenceHash => Evidence.EvidenceHash;
    internal string RequestHash => Evidence.RequestHash;
    internal Phase35IWorkerProfile WorkerProfile => Evidence.WorkerProfile;
    internal Phase35IRunnerIdentity Runner => Evidence.Runner;
    internal string ContainmentProfileVersion => Evidence.ContainmentProfileVersion;
    internal Phase35ITokenEvidence RestrictedToken => Evidence.RestrictedToken;
    internal Phase35IJobEvidence Job => Evidence.Job;
    internal Phase35IIsolationEvidence Isolation => Evidence.Isolation;
    internal string Phase35HAuditCorrelationHash => Evidence.Phase35HAuditCorrelationHash;
    internal Phase35IProofStatus ProofStatus => Evidence.ProofStatus;
}
