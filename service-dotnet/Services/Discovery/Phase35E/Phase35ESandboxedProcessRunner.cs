using System.Diagnostics;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35ESandboxedProcessRunner(IPhase35EProcessBoundary boundary)
{
    internal async Task<Phase35ESandboxResult> RunAsync(Phase35ESandboxExecutionSpec spec, CancellationToken cancellationToken = default)
    {
        Phase35EProcessCapture? process = null;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(spec.Timeout);
            process = await boundary.StartAsync(spec, timeout.Token);
            if (!process.Started) return Failed(spec, Phase35EExitClassification.ProcessCreationFailed, Phase35EFailureCode.ProcessCreationFailed, process);
            var classification = process.ExitCode == 0 ? Phase35EExitClassification.Completed : Phase35EExitClassification.NonZeroExit;
            if (process.Stdout.Length > spec.MaxOutputBytes || process.Stderr.Length > spec.MaxOutputBytes) classification = Phase35EExitClassification.OutputLimitExceeded;
            return new(spec.SessionId, spec.Identity.ProviderId, spec.Identity.ImplementationId, spec.Identity.PackageId, spec.Identity.CertificationId, spec.Policy.Version, OperatingSystem.IsMacOS() ? "darwin-arm64" : Environment.OSVersion.Platform.ToString(), classification, classification == Phase35EExitClassification.Completed ? null : Phase35EFailureCode.ResultInvalid, process.Stdout.Length, process.Stderr.Length, [], true);
        }
        catch (OperationCanceledException)
        {
            if (process is not null) await boundary.TerminateAsync(process, CancellationToken.None);
            var failure = cancellationToken.IsCancellationRequested ? Phase35EFailureCode.Cancelled : Phase35EFailureCode.TimedOut;
            return Failed(spec, failure == Phase35EFailureCode.TimedOut ? Phase35EExitClassification.TimedOut : Phase35EExitClassification.Completed, failure, process);
        }
        finally
        {
            if (process is not null) await boundary.TerminateAsync(process, CancellationToken.None);
        }
    }

    private static Phase35ESandboxResult Failed(Phase35ESandboxExecutionSpec spec, Phase35EExitClassification classification, Phase35EFailureCode failure, Phase35EProcessCapture? process) => new(spec.SessionId, spec.Identity.ProviderId, spec.Identity.ImplementationId, spec.Identity.PackageId, spec.Identity.CertificationId, spec.Policy.Version, OperatingSystem.IsMacOS() ? "darwin-arm64" : Environment.OSVersion.Platform.ToString(), classification, failure, process?.Stdout.Length ?? 0, process?.Stderr.Length ?? 0, [], true);
}
