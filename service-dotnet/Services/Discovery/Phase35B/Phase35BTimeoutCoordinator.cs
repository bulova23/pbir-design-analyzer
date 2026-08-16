namespace PowerBIModelingService.Services.Discovery;

internal enum Phase35BTimeoutStatus { Completed, Cancelled, TimedOut, Failed }

internal sealed record Phase35BTimeoutResult<T>(Phase35BTimeoutStatus Status, T? Value, Exception? Error);

internal sealed class Phase35BTimeoutCoordinator(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    internal async Task<Phase35BTimeoutResult<T>> RunAsync<T>(Func<CancellationToken, Task<T>> operation, Phase35BTimeoutPolicy policy, CancellationToken callerCancellation)
    {
        if (!policy.IsValid) return new(Phase35BTimeoutStatus.Failed, default, new Phase35BContractException("Timeout policy is invalid."));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(callerCancellation);
        var task = operation(linked.Token);
        var timeout = Task.Delay(policy.Timeout, _timeProvider, CancellationToken.None);
        var completed = await Task.WhenAny(task, timeout);
        if (completed == timeout)
        {
            linked.Cancel();
            return new(Phase35BTimeoutStatus.TimedOut, default, null);
        }
        try
        {
            return new(Phase35BTimeoutStatus.Completed, await task, null);
        }
        catch (OperationCanceledException) when (callerCancellation.IsCancellationRequested)
        {
            return new(Phase35BTimeoutStatus.Cancelled, default, null);
        }
        catch (Exception exception)
        {
            return new(Phase35BTimeoutStatus.Failed, default, exception);
        }
    }
}
