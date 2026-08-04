using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerBIModelingService.RpcHost;

internal enum RpcResponseWriteStatus
{
    Written,
    WrittenFallbackError,
    Suppressed,
    OutputFault,
}

internal sealed class RpcResponseWriter : IAsyncDisposable
{
    private const int InternalErrorCode = -32603;
    private const string InternalErrorMessage = "Internal error.";

    private readonly Stream _output;
    private readonly int _maxResponseBytes;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private int _closed;
    private int _disposed;

    internal RpcResponseWriter(Stream output, RpcTransportOptions options)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        ArgumentNullException.ThrowIfNull(options);
        _maxResponseBytes = options.MaxResponseBytes;
    }

    internal bool IsWritable => Volatile.Read(ref _closed) == 0;

    internal void DisableWrites() => Interlocked.Exchange(ref _closed, 1);

    internal Task<RpcResponseWriteStatus> WriteResultAsync(
        RpcRequestId? id,
        object? result,
        CancellationToken cancellationToken) =>
        WritePayloadAsync(
            new JsonRpcSuccessResponse
            {
                Id = id?.JsonValue,
                Result = result,
            },
            id,
            cancellationToken);

    internal Task<RpcResponseWriteStatus> WriteErrorAsync(
        RpcRequestId? id,
        int code,
        string message,
        CancellationToken cancellationToken) =>
        WritePayloadAsync(
            new JsonRpcErrorEnvelope
            {
                Id = id?.JsonValue,
                Error = new JsonRpcErrorPayload
                {
                    Code = code,
                    Message = message,
                },
            },
            id,
            cancellationToken);

    internal async Task CloseAsync()
    {
        DisableWrites();
        await _writeLock.WaitAsync().ConfigureAwait(false);
        _writeLock.Release();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await CloseAsync().ConfigureAwait(false);
        _writeLock.Dispose();
    }

    private async Task<RpcResponseWriteStatus> WritePayloadAsync(
        object payload,
        RpcRequestId? id,
        CancellationToken cancellationToken)
    {
        if (!IsWritable)
        {
            return RpcResponseWriteStatus.Suppressed;
        }

        byte[] body;
        var usedFallback = false;
        try
        {
            body = SerializeBounded(payload);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            usedFallback = true;
            try
            {
                body = SerializeBounded(new JsonRpcErrorEnvelope
                {
                    Id = id?.JsonValue,
                    Error = new JsonRpcErrorPayload
                    {
                        Code = InternalErrorCode,
                        Message = InternalErrorMessage,
                    },
                });
            }
            catch (Exception fallbackException) when (fallbackException is not OperationCanceledException)
            {
                return RpcResponseWriteStatus.Suppressed;
            }
        }

        var header = Encoding.ASCII.GetBytes($"Content-Length: {body.Length}\r\n\r\n");
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!IsWritable)
            {
                return RpcResponseWriteStatus.Suppressed;
            }

            try
            {
                await _output.WriteAsync(header, cancellationToken).ConfigureAwait(false);
                await _output.WriteAsync(body, cancellationToken).ConfigureAwait(false);
                await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
                return usedFallback
                    ? RpcResponseWriteStatus.WrittenFallbackError
                    : RpcResponseWriteStatus.Written;
            }
            catch (IOException)
            {
                Interlocked.Exchange(ref _closed, 1);
                return RpcResponseWriteStatus.OutputFault;
            }
            catch (ObjectDisposedException)
            {
                Interlocked.Exchange(ref _closed, 1);
                return RpcResponseWriteStatus.OutputFault;
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private byte[] SerializeBounded(object payload)
    {
        using var stream = new BoundedWriteStream(_maxResponseBytes);
        JsonSerializer.Serialize(stream, payload, payload.GetType(), _jsonOptions);
        return stream.ToArray();
    }

    private sealed class BoundedWriteStream : Stream
    {
        private readonly int _limit;
        private readonly MemoryStream _inner;

        internal BoundedWriteStream(int limit)
        {
            _limit = limit;
            _inner = new MemoryStream(Math.Min(256, limit));
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        internal byte[] ToArray() => _inner.ToArray();

        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureWithinLimit(count);
            _inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureWithinLimit(buffer.Length);
            _inner.Write(buffer);
        }

        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            EnsureWithinLimit(buffer.Length);
            return _inner.WriteAsync(buffer, cancellationToken);
        }

        private void EnsureWithinLimit(int count)
        {
            if (count < 0 || _inner.Length > _limit - count)
            {
                throw new RpcResponseLimitException();
            }
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    private sealed class RpcResponseLimitException : Exception
    {
    }
}
