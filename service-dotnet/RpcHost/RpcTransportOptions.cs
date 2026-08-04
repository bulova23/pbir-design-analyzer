namespace PowerBIModelingService.RpcHost;

internal sealed class RpcTransportOptions
{
    internal RpcTransportOptions(
        int maxHeaderBytes,
        int maxHeaderLineBytes,
        int maxHeaderCount,
        int maxRequestBytes,
        int maxPayloadBytes,
        int maxEnvelopeBytes,
        int maxJsonDepth,
        int maxMethodBytes,
        int maxRequestIdBytes,
        int maxResponseBytes,
        int maxConcurrentRequests,
        int maxRegisteredRequests)
    {
        MaxHeaderBytes = RequirePositive(maxHeaderBytes, nameof(maxHeaderBytes));
        MaxHeaderLineBytes = RequirePositive(maxHeaderLineBytes, nameof(maxHeaderLineBytes));
        MaxHeaderCount = RequirePositive(maxHeaderCount, nameof(maxHeaderCount));
        MaxRequestBytes = RequirePositive(maxRequestBytes, nameof(maxRequestBytes));
        MaxPayloadBytes = RequirePositive(maxPayloadBytes, nameof(maxPayloadBytes));
        MaxEnvelopeBytes = RequirePositive(maxEnvelopeBytes, nameof(maxEnvelopeBytes));
        MaxJsonDepth = RequirePositive(maxJsonDepth, nameof(maxJsonDepth));
        MaxMethodBytes = RequirePositive(maxMethodBytes, nameof(maxMethodBytes));
        MaxRequestIdBytes = RequirePositive(maxRequestIdBytes, nameof(maxRequestIdBytes));
        MaxResponseBytes = RequirePositive(maxResponseBytes, nameof(maxResponseBytes));
        MaxConcurrentRequests = RequirePositive(maxConcurrentRequests, nameof(maxConcurrentRequests));
        MaxRegisteredRequests = RequirePositive(maxRegisteredRequests, nameof(maxRegisteredRequests));

        if (MaxHeaderLineBytes > MaxHeaderBytes)
        {
            throw new ArgumentException("The header-line limit cannot exceed the total header limit.");
        }

        if (MaxPayloadBytes > MaxRequestBytes)
        {
            throw new ArgumentException("The payload limit cannot exceed the request limit.");
        }

        if (MaxEnvelopeBytes > MaxRequestBytes)
        {
            throw new ArgumentException("The envelope limit cannot exceed the request limit.");
        }

        if (MaxConcurrentRequests > MaxRegisteredRequests)
        {
            throw new ArgumentException("The concurrent-request limit cannot exceed the registration limit.");
        }
    }

    internal static RpcTransportOptions Production { get; } = new(
        maxHeaderBytes: 8 * 1024,
        maxHeaderLineBytes: 4 * 1024,
        maxHeaderCount: 16,
        maxRequestBytes: 8 * 1024 * 1024,
        maxPayloadBytes: 7 * 1024 * 1024,
        maxEnvelopeBytes: 64 * 1024,
        maxJsonDepth: 64,
        maxMethodBytes: 256,
        maxRequestIdBytes: 128,
        maxResponseBytes: 16 * 1024 * 1024,
        maxConcurrentRequests: 8,
        maxRegisteredRequests: 64);

    internal int MaxHeaderBytes { get; }
    internal int MaxHeaderLineBytes { get; }
    internal int MaxHeaderCount { get; }
    internal int MaxRequestBytes { get; }
    internal int MaxPayloadBytes { get; }
    internal int MaxEnvelopeBytes { get; }
    internal int MaxJsonDepth { get; }
    internal int MaxMethodBytes { get; }
    internal int MaxRequestIdBytes { get; }
    internal int MaxResponseBytes { get; }
    internal int MaxConcurrentRequests { get; }
    internal int MaxRegisteredRequests { get; }

    private static int RequirePositive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Transport limits must be positive.");
        }

        return value;
    }
}
