extern alias RpcHost;
using System.Text;
using System.Text.Json;
using Xunit;
using JsonRpcRequestParser = RpcHost::PowerBIModelingService.RpcHost.JsonRpcRequestParser;
using RpcResponseWriteStatus = RpcHost::PowerBIModelingService.RpcHost.RpcResponseWriteStatus;
using RpcResponseWriter = RpcHost::PowerBIModelingService.RpcHost.RpcResponseWriter;
using RpcTransportOptions = RpcHost::PowerBIModelingService.RpcHost.RpcTransportOptions;

namespace ServiceDotnet.Tests;

public sealed class RpcHostResponseWriterTests
{
    [Fact]
    public async Task WriteResultAsync_EmitsExplicitNullResult()
    {
        await using var output = new MemoryStream();
        await using var writer = new RpcResponseWriter(output, CreateOptions());

        var status = await writer.WriteResultAsync(Id("shutdown"), null, CancellationToken.None);
        var frame = Assert.Single(ParseFrames(output.ToArray()));

        Assert.Equal(RpcResponseWriteStatus.Written, status);
        Assert.Equal("2.0", frame.GetProperty("jsonrpc").GetString());
        Assert.Equal("shutdown", frame.GetProperty("id").GetString());
        Assert.Equal(JsonValueKind.Null, frame.GetProperty("result").ValueKind);
    }

    [Fact]
    public async Task WriteErrorAsync_EmitsBoundedStandardError()
    {
        await using var output = new MemoryStream();
        await using var writer = new RpcResponseWriter(output, CreateOptions());

        var status = await writer.WriteErrorAsync(Id(7), -32600, "Invalid Request.", CancellationToken.None);
        var frame = Assert.Single(ParseFrames(output.ToArray()));

        Assert.Equal(RpcResponseWriteStatus.Written, status);
        Assert.Equal(7, frame.GetProperty("id").GetInt32());
        Assert.Equal(-32600, frame.GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal("Invalid Request.", frame.GetProperty("error").GetProperty("message").GetString());
    }

    [Fact]
    public async Task ConcurrentWrites_ProduceCompleteNonInterleavedFrames()
    {
        await using var output = new YieldingWriteStream();
        await using var writer = new RpcResponseWriter(output, CreateOptions());

        await Task.WhenAll(
            writer.WriteResultAsync(Id("first"), new { value = new string('a', 200) }, CancellationToken.None),
            writer.WriteResultAsync(Id("second"), new { value = new string('b', 200) }, CancellationToken.None),
            writer.WriteResultAsync(Id("third"), new { value = new string('c', 200) }, CancellationToken.None));

        var frames = ParseFrames(output.ToArray());
        Assert.Equal(3, frames.Count);
        Assert.Equal(new[] { "first", "second", "third" },
            frames.Select(frame => frame.GetProperty("id").GetString()).OrderBy(value => value));
    }

    [Fact]
    public async Task ResponseLimit_AcceptsExactSerializedBodyLength()
    {
        var result = new { value = "boundary" };
        var bodyLength = JsonSerializer.SerializeToUtf8Bytes(new
        {
            jsonrpc = "2.0",
            id = "request",
            result,
        }).Length;
        await using var output = new MemoryStream();
        await using var writer = new RpcResponseWriter(output, CreateOptions(maxResponseBytes: bodyLength));

        var status = await writer.WriteResultAsync(Id("request"), result, CancellationToken.None);

        Assert.Equal(RpcResponseWriteStatus.Written, status);
        Assert.Single(ParseFrames(output.ToArray()));
    }

    [Fact]
    public async Task OversizedResult_IsReplacedWithFixedInternalError()
    {
        await using var output = new MemoryStream();
        await using var writer = new RpcResponseWriter(output, CreateOptions(maxResponseBytes: 128));

        var status = await writer.WriteResultAsync(
            Id("request"),
            new { value = new string('x', 1024) },
            CancellationToken.None);
        var frame = Assert.Single(ParseFrames(output.ToArray()));

        Assert.Equal(RpcResponseWriteStatus.WrittenFallbackError, status);
        Assert.Equal(-32603, frame.GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal("Internal error.", frame.GetProperty("error").GetProperty("message").GetString());
        Assert.DoesNotContain(new string('x', 32), Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task ResultSerializationFault_IsReplacedWithFixedInternalError()
    {
        await using var output = new MemoryStream();
        await using var writer = new RpcResponseWriter(output, CreateOptions());

        var status = await writer.WriteResultAsync(
            Id("request"),
            new ThrowingResult(),
            CancellationToken.None);
        var frame = Assert.Single(ParseFrames(output.ToArray()));

        Assert.Equal(RpcResponseWriteStatus.WrittenFallbackError, status);
        Assert.Equal(-32603, frame.GetProperty("error").GetProperty("code").GetInt32());
        Assert.DoesNotContain("sensitive serialization failure", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task OutputFailure_ClosesWriterAndSuppressesLaterWrites()
    {
        await using var output = new ThrowingWriteStream();
        await using var writer = new RpcResponseWriter(output, CreateOptions());

        var failed = await writer.WriteResultAsync(Id(1), new { ok = true }, CancellationToken.None);
        var suppressed = await writer.WriteResultAsync(Id(2), new { ok = true }, CancellationToken.None);

        Assert.Equal(RpcResponseWriteStatus.OutputFault, failed);
        Assert.Equal(RpcResponseWriteStatus.Suppressed, suppressed);
        Assert.False(writer.IsWritable);
    }

    [Fact]
    public async Task CloseAsync_IsIdempotentAndSuppressesWrites()
    {
        await using var output = new MemoryStream();
        await using var writer = new RpcResponseWriter(output, CreateOptions());

        await writer.CloseAsync();
        await writer.CloseAsync();
        var status = await writer.WriteResultAsync(Id(1), new { ok = true }, CancellationToken.None);

        Assert.Equal(RpcResponseWriteStatus.Suppressed, status);
        Assert.Empty(output.ToArray());
    }

    private static RpcHost::PowerBIModelingService.RpcHost.RpcRequestId Id(string value) =>
        JsonRpcRequestParser.Parse(
            Encoding.UTF8.GetBytes($"{{\"jsonrpc\":\"2.0\",\"id\":{JsonSerializer.Serialize(value)},\"method\":\"test\"}}"),
            CreateOptions()).Request!.Id!.Value;

    private static RpcHost::PowerBIModelingService.RpcHost.RpcRequestId Id(long value) =>
        JsonRpcRequestParser.Parse(
            Encoding.UTF8.GetBytes($"{{\"jsonrpc\":\"2.0\",\"id\":{value},\"method\":\"test\"}}"),
            CreateOptions()).Request!.Id!.Value;

    private static RpcTransportOptions CreateOptions(int maxResponseBytes = 4096) => new(
        maxHeaderBytes: 256,
        maxHeaderLineBytes: 128,
        maxHeaderCount: 4,
        maxRequestBytes: 2048,
        maxPayloadBytes: 1024,
        maxEnvelopeBytes: 1024,
        maxJsonDepth: 16,
        maxMethodBytes: 64,
        maxRequestIdBytes: 32,
        maxResponseBytes,
        maxConcurrentRequests: 2,
        maxRegisteredRequests: 4);

    private static IReadOnlyList<JsonElement> ParseFrames(byte[] bytes)
    {
        var frames = new List<JsonElement>();
        var offset = 0;
        while (offset < bytes.Length)
        {
            var separator = FindSequence(bytes, offset, "\r\n\r\n"u8);
            Assert.True(separator >= offset);
            var header = Encoding.ASCII.GetString(bytes, offset, separator - offset);
            Assert.StartsWith("Content-Length: ", header);
            var length = int.Parse(header["Content-Length: ".Length..]);
            var bodyStart = separator + 4;
            Assert.True(bodyStart + length <= bytes.Length);
            using var document = JsonDocument.Parse(bytes.AsMemory(bodyStart, length));
            frames.Add(document.RootElement.Clone());
            offset = bodyStart + length;
        }

        return frames;
    }

    private static int FindSequence(byte[] bytes, int start, ReadOnlySpan<byte> sequence)
    {
        for (var index = start; index <= bytes.Length - sequence.Length; index++)
        {
            if (bytes.AsSpan(index, sequence.Length).SequenceEqual(sequence))
            {
                return index;
            }
        }

        return -1;
    }

    private sealed class YieldingWriteStream : Stream
    {
        private readonly MemoryStream _inner = new();

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

        internal byte[] ToArray() => _inner.ToArray();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            foreach (var value in buffer.Span.ToArray())
            {
                await Task.Yield();
                _inner.WriteByte(value);
            }
        }

        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
    }

    private sealed class ThrowingWriteStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() => throw new IOException("sensitive output failure");
        public override Task FlushAsync(CancellationToken cancellationToken) => throw new IOException("sensitive output failure");
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("sensitive output failure");
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException(new IOException("sensitive output failure"));
    }

    private sealed class ThrowingResult
    {
        public string Value => throw new InvalidOperationException("sensitive serialization failure");
    }
}
