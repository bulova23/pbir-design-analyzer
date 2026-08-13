using System.Diagnostics;
using System.Runtime.Versioning;

namespace PowerBIModelingService.Services.Discovery;

[SupportedOSPlatform("windows")]
internal sealed class Phase35IWindowsRuntime
{
    internal Phase35IContainmentResult Execute(string workerRoot, Phase35IWorkerProfile profile, Phase35IRunnerIdentity runner, Phase35HRequest request, Phase35IResourceProjectionResult limits, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) return new(request.ExecutionId, request.SessionId, Phase35ILifecycleResult.Rejected, Phase35IFailureCode.PlatformUnsupported, false, true, request.CorrelationId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var started = DateTimeOffset.UtcNow;
        var binder = new Phase35IPathBinder();
        var executablePath = binder.Bind(workerRoot, request.SessionId, runner);
        var sessionRoot = binder.BindSessionRoot(workerRoot, request.SessionId);
        if (!File.Exists(executablePath) || !string.Equals(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(executablePath))).ToLowerInvariant(), runner.ExecutableSha256, StringComparison.OrdinalIgnoreCase)) return Failed(request, started, Phase35IFailureCode.ExecutableIdentityMismatch);
        Directory.CreateDirectory(sessionRoot);
        nint token = 0, restrictedToken = 0, job = 0;
        nint environment = 0;
        Phase35IWindowsNative.ProcessInformation process = default;
        try
        {
            if (!Phase35IWindowsNative.OpenProcessToken(Process.GetCurrentProcess().Handle, Phase35IWindowsNative.TokenQuery | Phase35IWindowsNative.TokenDuplicate | Phase35IWindowsNative.TokenAssignPrimary | Phase35IWindowsNative.TokenAdjustDefault, out token)) return Failed(request, started, Phase35IFailureCode.RestrictedTokenCreationFailed);
            if (!Phase35IWindowsNative.CreateRestrictedToken(token, Phase35IWindowsNative.DisableMaxPrivilege, 0, 0, 0, 0, 0, 0, out restrictedToken)) return Failed(request, started, Phase35IFailureCode.RestrictedTokenCreationFailed);
            job = Phase35IWindowsNative.CreateJobObject(0, null);
            if (job == 0) return Failed(request, started, Phase35IFailureCode.JobCreationFailed);
            var flags = Phase35IWindowsNative.JobObjectLimitKillOnJobClose | Phase35IWindowsNative.JobObjectLimitActiveProcess | Phase35IWindowsNative.JobObjectLimitDieOnUnhandledException;
            if (limits.JobLimits.MemoryBytes > 0) flags |= Phase35IWindowsNative.JobObjectLimitProcessMemory;
            var jobInfo = new Phase35IWindowsNative.ExtendedLimitInformation { BasicLimitInformation = new() { LimitFlags = flags, ActiveProcessLimit = (uint)limits.JobLimits.ActiveProcessLimit }, JobMemoryLimit = (nuint)limits.JobLimits.MemoryBytes };
            if (!Phase35IWindowsNative.SetInformationJobObject(job, Phase35IWindowsNative.JobObjectExtendedLimitInformation, ref jobInfo, (uint)System.Runtime.InteropServices.Marshal.SizeOf<Phase35IWindowsNative.ExtendedLimitInformation>())) return Failed(request, started, Phase35IFailureCode.JobConfigurationFailed);
            var startup = new Phase35IWindowsNative.StartupInfo { Cb = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Phase35IWindowsNative.StartupInfo>() };
            var commandLine = $"\"{executablePath}\" --workload={request.Workload}";
            environment = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUni("\0\0");
            if (!Phase35IWindowsNative.CreateProcessAsUser(restrictedToken, executablePath, commandLine, 0, 0, false, Phase35IWindowsNative.CreateSuspended | Phase35IWindowsNative.CreateUnicodeEnvironment, environment, sessionRoot, ref startup, out process)) return Failed(request, started, Phase35IFailureCode.SuspendedLaunchFailed);
            if (!Phase35IWindowsNative.AssignProcessToJobObject(job, process.Process) || !Phase35IWindowsNative.IsProcessInJob(process.Process, job, out var assigned) || !assigned) return Failed(request, started, Phase35IFailureCode.JobAssignmentFailed);
            if (Phase35IWindowsNative.ResumeThread(process.Thread) == uint.MaxValue) return Failed(request, started, Phase35IFailureCode.ResumeFailed);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(limits.JobLimits.ExecutionTimeoutSeconds));
            while (!timeout.IsCancellationRequested && !Process.GetProcessById((int)process.ProcessId).HasExited) Thread.Sleep(25);
            var failure = cancellationToken.IsCancellationRequested ? Phase35IFailureCode.Cancelled : timeout.IsCancellationRequested ? Phase35IFailureCode.TimedOut : (Phase35IFailureCode?)null;
            if (failure is not null) Phase35IWindowsNative.TerminateJobObject(job, 1);
            var cleanup = CleanupSessionRoot(sessionRoot);
            return new(request.ExecutionId, request.SessionId, failure == Phase35IFailureCode.Cancelled ? Phase35ILifecycleResult.Cancelled : failure == Phase35IFailureCode.TimedOut ? Phase35ILifecycleResult.TimedOut : Phase35ILifecycleResult.Completed, failure, true, cleanup, request.CorrelationId, started, DateTimeOffset.UtcNow);
        }
        finally
        {
            if (process.Process != 0 && job != 0) Phase35IWindowsNative.TerminateJobObject(job, 1);
            if (process.Thread != 0) Phase35IWindowsNative.CloseHandle(process.Thread);
            if (process.Process != 0) Phase35IWindowsNative.CloseHandle(process.Process);
            if (restrictedToken != 0) Phase35IWindowsNative.CloseHandle(restrictedToken);
            if (token != 0) Phase35IWindowsNative.CloseHandle(token);
            if (job != 0) Phase35IWindowsNative.CloseHandle(job);
            if (environment != 0) System.Runtime.InteropServices.Marshal.FreeCoTaskMem(environment);
        }
    }

    private static Phase35IContainmentResult Failed(Phase35HRequest request, DateTimeOffset started, Phase35IFailureCode failure) => new(request.ExecutionId, request.SessionId, Phase35ILifecycleResult.Failed, failure, false, true, request.CorrelationId, started, DateTimeOffset.UtcNow);

    private static bool CleanupSessionRoot(string sessionRoot)
    {
        try
        {
            if (!Directory.Exists(sessionRoot) || Directory.EnumerateFileSystemEntries(sessionRoot, "*", SearchOption.AllDirectories).Take(129).Count() > 128) return false;
            Directory.Delete(sessionRoot, true);
            return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}
