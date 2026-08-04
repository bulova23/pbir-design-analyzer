using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace PowerBIModelingService.RpcHost;

internal sealed class SimpleJsonRpcServer : IAsyncDisposable
{
    internal const int RequestCancelledCode = -32800;
    internal const int ServerBusyCode = -32000;
    internal const int InternalErrorCode = -32603;
    internal const string RequestCancelledMessage = "Request cancelled.";
    internal const string ServerBusyMessage = "Server busy.";
    internal const string InternalErrorMessage = "Internal error.";

    private readonly IRpcRequestHandler _handler;
    private readonly Stream _input;
    private readonly ILogger<SimpleJsonRpcServer> _logger;
    private readonly RpcTransportOptions _options;
    private readonly RpcResponseWriter _writer;
    private readonly RpcRequestRegistry _registry;
    private readonly SemaphoreSlim _dispatchSlots;
    private readonly SemaphoreSlim _workCapacity;
    private readonly CancellationTokenSource _stopIntake = new();
    private readonly object _trackedSync = new();
    private readonly HashSet<ScheduledOperation> _tracked = [];
    private readonly object _shutdownSync = new();
    private readonly bool _ownsHandler;

    private Task? _shutdownTask;
    private int _connectionLost;
    private int _disposed;

    internal SimpleJsonRpcServer(
        IRpcRequestHandler handler,
        Stream input,
        Stream output,
        ILogger<SimpleJsonRpcServer> logger,
        RpcTransportOptions? options = null,
        bool ownsHandler = true)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? RpcTransportOptions.Production;
        _writer = new RpcResponseWriter(output ?? throw new ArgumentNullException(nameof(output)), _options);
        _registry = new RpcRequestRegistry(_options.MaxRegisteredRequests, _stopIntake.Token);
        _dispatchSlots = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);
        _workCapacity = new SemaphoreSlim(_options.MaxRegisteredRequests, _options.MaxRegisteredRequests);
        _ownsHandler = ownsHandler;
    }

    internal int ActiveRequestCount => _registry.Count;
    internal int TrackedTaskCount
    {
        get
        {
            lock (_trackedSync)
            {
                return _tracked.Count;
            }
        }
    }

    internal bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    internal static JsonSerializerOptions CreateJsonSerializerOptions() => new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var externalCancellation = cancellationToken.Register(static state =>
        {
            try
            {
                ((CancellationTokenSource)state!).Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Shutdown may win the race with external cancellation.
            }
        }, _stopIntake);

        Log("ready", "accepting");
        RpcRequestId? shutdownResponseId = null;
        var gracefulShutdown = false;

        try
        {
            while (!_stopIntake.IsCancellationRequested)
            {
                var frame = await JsonRpcFraming.ReadFrameAsync(
                        _input,
                        _options,
                        _stopIntake.Token)
                    .ConfigureAwait(false);

                if (frame.Status != RpcFrameStatus.Frame)
                {
                    MarkConnectionLost(frame.Status == RpcFrameStatus.EndOfStream ? "input_closed" : "frame_fault");
                    break;
                }

                var parsed = JsonRpcRequestParser.Parse(frame.Payload!, _options);
                if (!parsed.IsSuccess)
                {
                    Log("request_rejected", "invalid");
                    var status = await _writer.WriteErrorAsync(
                            parsed.Error!.ResponseId,
                            parsed.Error.Code,
                            parsed.Error.Message,
                            CancellationToken.None)
                        .ConfigureAwait(false);
                    ObserveWriteStatus(status);
                    continue;
                }

                var request = parsed.Request!;
                if (request.Method == "$/cancelRequest")
                {
                    await HandleCancellationAsync(request).ConfigureAwait(false);
                    continue;
                }

                if (request.Method == "exit")
                {
                    MarkConnectionLost("exit");
                    break;
                }

                if (request.Method == "shutdown")
                {
                    if (request.Id.HasValue && await RejectDuplicateControlIdAsync(request.Id.Value).ConfigureAwait(false))
                    {
                        continue;
                    }

                    shutdownResponseId = request.Id;
                    gracefulShutdown = true;
                    break;
                }

                await AcceptApplicationRequestAsync(request).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_stopIntake.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            if (!gracefulShutdown)
            {
                MarkConnectionLost("intake_cancelled");
            }
        }
        catch
        {
            MarkConnectionLost("intake_fault");
        }

        await BeginShutdownAsync(shutdownResponseId, !gracefulShutdown || ConnectionLost).ConfigureAwait(false);
    }

    internal Task ShutdownAsync() => BeginShutdownAsync(shutdownResponseId: null, disconnected: false);

    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync().ConfigureAwait(false);
    }

    private bool ConnectionLost => Volatile.Read(ref _connectionLost) != 0;

    private async Task AcceptApplicationRequestAsync(ParsedJsonRpcRequest request)
    {
        if (!_workCapacity.Wait(0))
        {
            Log("request_rejected", "capacity");
            if (request.Id.HasValue)
            {
                ObserveWriteStatus(await _writer.WriteErrorAsync(
                        request.Id,
                        ServerBusyCode,
                        ServerBusyMessage,
                        CancellationToken.None)
                    .ConfigureAwait(false));
            }

            return;
        }

        RpcRequestRegistration? registration = null;
        if (request.Id.HasValue)
        {
            var registrationResult = _registry.TryRegister(request.Id.Value);
            switch (registrationResult.Status)
            {
                case RpcRegistrationStatus.Registered:
                    registration = registrationResult.Registration;
                    break;

                case RpcRegistrationStatus.Duplicate:
                    _workCapacity.Release();
                    await HandleDuplicateAsync(registrationResult.Registration!).ConfigureAwait(false);
                    return;

                case RpcRegistrationStatus.CapacityExceeded:
                    _workCapacity.Release();
                    ObserveWriteStatus(await _writer.WriteErrorAsync(
                            request.Id,
                            ServerBusyCode,
                            ServerBusyMessage,
                            CancellationToken.None)
                        .ConfigureAwait(false));
                    return;

                case RpcRegistrationStatus.ShuttingDown:
                    _workCapacity.Release();
                    return;
            }
        }

        Schedule(request, registration);
    }

    private async Task HandleCancellationAsync(ParsedJsonRpcRequest request)
    {
        if (request.Id.HasValue)
        {
            Log("cancellation_rejected", "request_form");
            ObserveWriteStatus(await _writer.WriteErrorAsync(
                    request.Id,
                    JsonRpcRequestParser.InvalidRequestCode,
                    JsonRpcRequestParser.InvalidRequestMessage,
                    CancellationToken.None)
                .ConfigureAwait(false));
            return;
        }

        if (!JsonRpcRequestParser.TryParseCancellationId(request.ParamsUtf8, _options, out var requestId))
        {
            Log("cancellation_rejected", "invalid");
            return;
        }

        if (!_registry.TryGet(requestId, out var registration) ||
            registration is null ||
            !registration.TryCancel(RpcTerminalOutcome.Cancelled))
        {
            Log("cancellation_ignored", "inactive");
            return;
        }

        Log("request_cancelled", "active", registration.Correlation);
        ObserveWriteStatus(await _writer.WriteErrorAsync(
                registration.Id,
                RequestCancelledCode,
                RequestCancelledMessage,
                CancellationToken.None)
            .ConfigureAwait(false));
    }

    private async Task HandleDuplicateAsync(RpcRequestRegistration registration)
    {
        if (!registration.TryCancel(RpcTerminalOutcome.DuplicateId))
        {
            Log("duplicate_ignored", "terminal", registration.Correlation);
            return;
        }

        Log("duplicate_rejected", "active", registration.Correlation);
        ObserveWriteStatus(await _writer.WriteErrorAsync(
                registration.Id,
                JsonRpcRequestParser.InvalidRequestCode,
                JsonRpcRequestParser.InvalidRequestMessage,
                CancellationToken.None)
            .ConfigureAwait(false));
    }

    private async Task<bool> RejectDuplicateControlIdAsync(RpcRequestId id)
    {
        if (!_registry.TryGet(id, out var registration) || registration is null)
        {
            return false;
        }

        await HandleDuplicateAsync(registration).ConfigureAwait(false);
        return true;
    }

    private void Schedule(ParsedJsonRpcRequest request, RpcRequestRegistration? registration)
    {
        var operation = new ScheduledOperation();
        lock (_trackedSync)
        {
            _tracked.Add(operation);
        }

        operation.Task = ExecuteScheduledAsync(operation, request, registration);
        operation.Start.TrySetResult();
    }

    private async Task ExecuteScheduledAsync(
        ScheduledOperation operation,
        ParsedJsonRpcRequest request,
        RpcRequestRegistration? registration)
    {
        var dispatchSlotAcquired = false;
        try
        {
            await operation.Start.Task.ConfigureAwait(false);
            var requestCancellation = registration?.Token ?? _stopIntake.Token;
            await _dispatchSlots.WaitAsync(requestCancellation).ConfigureAwait(false);
            dispatchSlotAcquired = true;

            if (registration is not null && !registration.TryMarkDispatched())
            {
                return;
            }

            var result = await _handler.HandleAsync(request, requestCancellation).ConfigureAwait(false);
            if (registration is null)
            {
                return;
            }

            if (!registration.TryClaim(RpcTerminalOutcome.Completed))
            {
                return;
            }

            var writeStatus = result.Kind switch
            {
                RpcHandlerResultKind.Success => await _writer.WriteResultAsync(
                        registration.Id,
                        result.Result,
                        CancellationToken.None)
                    .ConfigureAwait(false),
                RpcHandlerResultKind.Error => await _writer.WriteErrorAsync(
                        registration.Id,
                        result.ErrorCode,
                        result.ErrorMessage,
                        CancellationToken.None)
                    .ConfigureAwait(false),
                _ => RpcResponseWriteStatus.Suppressed,
            };
            ObserveWriteStatus(writeStatus);
        }
        catch (OperationCanceledException)
        {
            if (registration is not null && registration.TryCancel(RpcTerminalOutcome.Cancelled))
            {
                ObserveWriteStatus(await _writer.WriteErrorAsync(
                        registration.Id,
                        RequestCancelledCode,
                        RequestCancelledMessage,
                        CancellationToken.None)
                    .ConfigureAwait(false));
            }
        }
        catch
        {
            Log("handler_fault", "failed", registration?.Correlation);
            if (registration is not null && registration.TryClaim(RpcTerminalOutcome.Faulted))
            {
                ObserveWriteStatus(await _writer.WriteErrorAsync(
                        registration.Id,
                        InternalErrorCode,
                        InternalErrorMessage,
                        CancellationToken.None)
                    .ConfigureAwait(false));
            }
        }
        finally
        {
            if (dispatchSlotAcquired)
            {
                _dispatchSlots.Release();
            }

            if (registration is not null)
            {
                _registry.RemoveAndDispose(registration);
            }

            _workCapacity.Release();
            lock (_trackedSync)
            {
                _tracked.Remove(operation);
            }
        }
    }

    private Task BeginShutdownAsync(RpcRequestId? shutdownResponseId, bool disconnected)
    {
        lock (_shutdownSync)
        {
            if (disconnected)
            {
                Interlocked.Exchange(ref _connectionLost, 1);
            }

            _shutdownTask ??= CompleteShutdownAsync(shutdownResponseId);
            return _shutdownTask;
        }
    }

    private async Task CompleteShutdownAsync(RpcRequestId? shutdownResponseId)
    {
        Log("shutdown_started", ConnectionLost ? "disconnected" : "graceful");
        try
        {
            _stopIntake.Cancel();
            if (ConnectionLost)
            {
                await _writer.CloseAsync().ConfigureAwait(false);
            }

            var cancelled = _registry.StopAndCancelAll(RpcTerminalOutcome.ShuttingDown);
            if (!ConnectionLost)
            {
                foreach (var registration in cancelled)
                {
                    ObserveWriteStatus(await _writer.WriteErrorAsync(
                            registration.Id,
                            RequestCancelledCode,
                            RequestCancelledMessage,
                            CancellationToken.None)
                        .ConfigureAwait(false));
                }
            }

            await AwaitTrackedTasksAsync().ConfigureAwait(false);

            if (!ConnectionLost && shutdownResponseId.HasValue)
            {
                ObserveWriteStatus(await _writer.WriteResultAsync(
                        shutdownResponseId,
                        result: null,
                        CancellationToken.None)
                    .ConfigureAwait(false));
            }

            await _writer.DisposeAsync().ConfigureAwait(false);
            _registry.Dispose();
            _dispatchSlots.Dispose();
            _workCapacity.Dispose();
            _stopIntake.Dispose();
            if (_ownsHandler)
            {
                try
                {
                    await _handler.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    Log("handler_dispose_fault", "failed");
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _disposed, 1);
            Log("shutdown_completed", "stopped");
        }
    }

    private async Task AwaitTrackedTasksAsync()
    {
        while (true)
        {
            Task[] tasks;
            lock (_trackedSync)
            {
                tasks = _tracked.Select(operation => operation.Task).ToArray();
            }

            if (tasks.Length == 0)
            {
                return;
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    private void ObserveWriteStatus(RpcResponseWriteStatus status)
    {
        if (status == RpcResponseWriteStatus.OutputFault)
        {
            MarkConnectionLost("output_fault");
        }
    }

    private void MarkConnectionLost(string state)
    {
        Interlocked.Exchange(ref _connectionLost, 1);
        _writer.DisableWrites();
        Log("connection_closed", state);
        try
        {
            _stopIntake.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void Log(string eventCode, string state, string? correlation = null)
    {
        if (correlation is null)
        {
            _logger.LogInformation(
                new EventId(3200, "RpcTransport"),
                "rpc_transport event={EventCode} state={State} active={ActiveCount} tracked={TrackedCount}",
                eventCode,
                state,
                ActiveRequestCount,
                TrackedTaskCount);
            return;
        }

        _logger.LogInformation(
            new EventId(3201, "RpcTransportRequest"),
            "rpc_transport event={EventCode} state={State} correlation={Correlation} active={ActiveCount} tracked={TrackedCount}",
            eventCode,
            state,
            correlation,
            ActiveRequestCount,
            TrackedTaskCount);
    }

    private sealed class ScheduledOperation
    {
        internal TaskCompletionSource Start { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal Task Task { get; set; } = Task.CompletedTask;
    }
}
