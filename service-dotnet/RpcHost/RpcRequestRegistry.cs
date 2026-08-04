using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace PowerBIModelingService.RpcHost;

internal enum RpcRegistrationStatus
{
    Registered,
    Duplicate,
    CapacityExceeded,
    ShuttingDown,
}

internal enum RpcTerminalOutcome
{
    None,
    Completed,
    Cancelled,
    Faulted,
    DuplicateId,
    ShuttingDown,
}

internal sealed record RpcRegistrationResult(
    RpcRegistrationStatus Status,
    RpcRequestRegistration? Registration = null);

internal sealed class RpcRequestRegistration : IDisposable
{
    private readonly CancellationTokenSource _cancellation;
    private int _dispatchState;
    private int _outcome;
    private int _disposed;

    internal RpcRequestRegistration(RpcRequestId id, CancellationToken connectionCancellation)
    {
        Id = id;
        Correlation = BuildCorrelation(id.CanonicalKey);
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(connectionCancellation);
    }

    internal RpcRequestId Id { get; }
    internal string Correlation { get; }
    internal CancellationToken Token => _cancellation.Token;
    internal RpcTerminalOutcome Outcome => (RpcTerminalOutcome)Volatile.Read(ref _outcome);
    internal bool IsDisposed => Volatile.Read(ref _disposed) != 0;
    internal bool WasDispatched => Volatile.Read(ref _dispatchState) != 0;

    internal bool TryMarkDispatched()
    {
        if (Outcome != RpcTerminalOutcome.None ||
            Interlocked.CompareExchange(ref _dispatchState, 1, 0) != 0)
        {
            return false;
        }

        return Outcome == RpcTerminalOutcome.None;
    }

    internal bool TryClaim(RpcTerminalOutcome outcome)
    {
        if (outcome == RpcTerminalOutcome.None)
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        return Interlocked.CompareExchange(
            ref _outcome,
            (int)outcome,
            (int)RpcTerminalOutcome.None) == (int)RpcTerminalOutcome.None;
    }

    internal bool TryCancel(RpcTerminalOutcome outcome)
    {
        if (outcome is not RpcTerminalOutcome.Cancelled and
            not RpcTerminalOutcome.DuplicateId and
            not RpcTerminalOutcome.ShuttingDown)
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        if (!TryClaim(outcome))
        {
            return false;
        }

        try
        {
            _cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        return true;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _cancellation.Dispose();
        }
    }

    private static string BuildCorrelation(string canonicalKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalKey));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}

internal sealed class RpcRequestRegistry : IDisposable
{
    private readonly ConcurrentDictionary<string, RpcRequestRegistration> _registrations = new();
    private readonly SemaphoreSlim _capacity;
    private readonly CancellationToken _connectionCancellation;
    private int _accepting = 1;
    private int _disposed;

    internal RpcRequestRegistry(int maxRegisteredRequests, CancellationToken connectionCancellation)
    {
        if (maxRegisteredRequests <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRegisteredRequests));
        }

        _capacity = new SemaphoreSlim(maxRegisteredRequests, maxRegisteredRequests);
        _connectionCancellation = connectionCancellation;
    }

    internal int Count => _registrations.Count;
    internal bool IsDisposed => Volatile.Read(ref _disposed) != 0;
    internal bool IsAccepting => Volatile.Read(ref _accepting) != 0 && !IsDisposed;

    internal RpcRegistrationResult TryRegister(RpcRequestId id)
    {
        if (!IsAccepting)
        {
            return new RpcRegistrationResult(RpcRegistrationStatus.ShuttingDown);
        }

        if (_registrations.TryGetValue(id.CanonicalKey, out var existing))
        {
            return new RpcRegistrationResult(RpcRegistrationStatus.Duplicate, existing);
        }

        if (!_capacity.Wait(0))
        {
            return new RpcRegistrationResult(RpcRegistrationStatus.CapacityExceeded);
        }

        if (!IsAccepting)
        {
            _capacity.Release();
            return new RpcRegistrationResult(RpcRegistrationStatus.ShuttingDown);
        }

        var registration = new RpcRequestRegistration(id, _connectionCancellation);
        if (_registrations.TryAdd(id.CanonicalKey, registration))
        {
            return new RpcRegistrationResult(RpcRegistrationStatus.Registered, registration);
        }

        registration.Dispose();
        _capacity.Release();
        return _registrations.TryGetValue(id.CanonicalKey, out existing)
            ? new RpcRegistrationResult(RpcRegistrationStatus.Duplicate, existing)
            : new RpcRegistrationResult(RpcRegistrationStatus.ShuttingDown);
    }

    internal bool TryGet(RpcRequestId id, out RpcRequestRegistration? registration) =>
        _registrations.TryGetValue(id.CanonicalKey, out registration);

    internal bool RemoveAndDispose(RpcRequestRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        if (!_registrations.TryGetValue(registration.Id.CanonicalKey, out var current) ||
            !ReferenceEquals(current, registration) ||
            !_registrations.TryRemove(registration.Id.CanonicalKey, out var removed))
        {
            return false;
        }

        removed.Dispose();
        _capacity.Release();
        return true;
    }

    internal IReadOnlyList<RpcRequestRegistration> StopAndCancelAll(RpcTerminalOutcome outcome)
    {
        Interlocked.Exchange(ref _accepting, 0);
        var cancelled = new List<RpcRequestRegistration>();
        foreach (var registration in _registrations.Values)
        {
            if (registration.TryCancel(outcome))
            {
                cancelled.Add(registration);
            }
        }

        return cancelled;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Interlocked.Exchange(ref _accepting, 0);
        foreach (var pair in _registrations.ToArray())
        {
            if (_registrations.TryRemove(pair.Key, out var registration))
            {
                registration.TryCancel(RpcTerminalOutcome.ShuttingDown);
                registration.Dispose();
            }
        }

        _capacity.Dispose();
    }
}
