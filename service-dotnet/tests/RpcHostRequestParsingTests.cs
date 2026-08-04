extern alias RpcHost;
using System.Text;
using Xunit;
using JsonRpcRequestParser = RpcHost::PowerBIModelingService.RpcHost.JsonRpcRequestParser;
using JsonRpcFraming = RpcHost::PowerBIModelingService.RpcHost.JsonRpcFraming;
using RpcFrameStatus = RpcHost::PowerBIModelingService.RpcHost.RpcFrameStatus;
using RpcTransportOptions = RpcHost::PowerBIModelingService.RpcHost.RpcTransportOptions;

namespace ServiceDotnet.Tests;

public sealed class RpcHostRequestParsingTests
{
    [Fact]
    public void ProductionOptions_AreFiniteAndInternallyConsistent()
    {
        var options = RpcTransportOptions.Production;

        Assert.Equal(8 * 1024, options.MaxHeaderBytes);
        Assert.Equal(4 * 1024, options.MaxHeaderLineBytes);
        Assert.Equal(16, options.MaxHeaderCount);
        Assert.Equal(8 * 1024 * 1024, options.MaxRequestBytes);
        Assert.Equal(7 * 1024 * 1024, options.MaxPayloadBytes);
        Assert.Equal(64 * 1024, options.MaxEnvelopeBytes);
        Assert.Equal(64, options.MaxJsonDepth);
        Assert.Equal(256, options.MaxMethodBytes);
        Assert.Equal(128, options.MaxRequestIdBytes);
        Assert.Equal(16 * 1024 * 1024, options.MaxResponseBytes);
        Assert.Equal(8, options.MaxConcurrentRequests);
        Assert.Equal(64, options.MaxRegisteredRequests);
    }

    [Fact]
    public void Options_RejectNonPositiveAndInconsistentLimits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateOptions(maxRequestBytes: 0));
        Assert.Throws<ArgumentException>(() => CreateOptions(maxPayloadBytes: 101, maxRequestBytes: 100));
        Assert.Throws<ArgumentException>(() => CreateOptions(maxEnvelopeBytes: 101, maxRequestBytes: 100));
        Assert.Throws<ArgumentException>(() => CreateOptions(maxConcurrentRequests: 3, maxRegisteredRequests: 2));
    }

    [Theory]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{}}", "n:1")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"request-1\",\"method\":\"model/ping\",\"params\":{}}", "s:request-1")]
    public void Parse_AcceptsExistingRequestShapes(string json, string canonicalId)
    {
        var result = JsonRpcRequestParser.Parse(Encoding.UTF8.GetBytes(json), RpcTransportOptions.Production);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Request);
        Assert.Equal(canonicalId, result.Request!.Id?.CanonicalKey);
        Assert.Equal("2.0", result.Request.ProtocolVersion);
    }

    [Theory]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"initialized\",\"params\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":null,\"method\":\"initialized\",\"params\":{}}")]
    public void Parse_AcceptsOmittedOrNullNotificationId(string json)
    {
        var result = Parse(json);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Request!.Id);
    }

    [Fact]
    public void Parse_PreservesMultibyteUtf8Params()
    {
        var result = Parse("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\",\"params\":{\"label\":\"café 🚀\"}}");

        Assert.True(result.IsSuccess);
        Assert.Equal("café 🚀", result.Request!.Params?.GetProperty("label").GetString());
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\"} trailing")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\"}{\"x\":1}")]
    public void Parse_RejectsMalformedTruncatedOrTrailingJson(string json)
    {
        var result = Parse(json);

        Assert.False(result.IsSuccess);
        Assert.Equal(-32700, result.Error!.Code);
        Assert.Null(result.Error.ResponseId);
    }

    [Theory]
    [InlineData("{\"id\":1,\"method\":\"model/ping\"}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"1.0\",\"id\":1,\"method\":\"model/ping\"}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\"}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"id\":2,\"method\":\"model/ping\"}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\",\"method\":\"initialize\"}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\",\"params\":{},\"params\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\",\"unknown\":true}")]
    [InlineData("{\"Jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\"}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"Method\":\"model/ping\"}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"\"}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\",\"params\":true}")]
    public void Parse_RejectsInvalidEnvelopes(string json)
    {
        var result = Parse(json);

        Assert.False(result.IsSuccess);
        Assert.Equal(-32600, result.Error!.Code);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("1.5")]
    [InlineData("1e2")]
    [InlineData("9223372036854775808")]
    [InlineData("\"\"")]
    [InlineData("\"bad\\u0001id\"")]
    public void Parse_RejectsInvalidIdentifiers(string idJson)
    {
        var result = Parse($"{{\"jsonrpc\":\"2.0\",\"id\":{idJson},\"method\":\"model/ping\"}}");

        Assert.False(result.IsSuccess);
        Assert.Equal(-32600, result.Error!.Code);
        Assert.Null(result.Error.ResponseId);
    }

    [Fact]
    public void Parse_DistinguishesStringAndNumericIdentifiers()
    {
        var numeric = Parse("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\"}");
        var text = Parse("{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"model/ping\"}");

        Assert.NotEqual(numeric.Request!.Id!.Value.CanonicalKey, text.Request!.Id!.Value.CanonicalKey);
    }

    [Fact]
    public void Parse_AcceptsConfiguredMethodAndIdBoundaries()
    {
        var options = CreateOptions(maxMethodBytes: 4, maxRequestIdBytes: 4);
        var result = JsonRpcRequestParser.Parse(
            Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"id\":\"1234\",\"method\":\"ping\",\"params\":{}}"),
            options);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"12345\",\"method\":\"ping\",\"params\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":\"1234\",\"method\":\"pings\",\"params\":{}}")]
    public void Parse_RejectsOneByteOverMethodOrIdLimit(string json)
    {
        var result = JsonRpcRequestParser.Parse(
            Encoding.UTF8.GetBytes(json),
            CreateOptions(maxMethodBytes: 4, maxRequestIdBytes: 4));

        Assert.False(result.IsSuccess);
        Assert.Equal(-32600, result.Error!.Code);
    }

    [Fact]
    public void Parse_EnforcesPayloadBoundaryWithoutDispatch()
    {
        const string prefix = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"ping\",\"params\":";
        const string payload = "{\"v\":\"1234\"}";
        const string suffix = "}";
        var payloadBytes = Encoding.UTF8.GetByteCount(payload);

        var accepted = JsonRpcRequestParser.Parse(
            Encoding.UTF8.GetBytes(prefix + payload + suffix),
            CreateOptions(maxPayloadBytes: payloadBytes));
        var rejected = JsonRpcRequestParser.Parse(
            Encoding.UTF8.GetBytes(prefix + payload + suffix),
            CreateOptions(maxPayloadBytes: payloadBytes - 1));

        Assert.True(accepted.IsSuccess);
        Assert.False(rejected.IsSuccess);
        Assert.Equal(-32600, rejected.Error!.Code);
    }

    [Fact]
    public void Parse_EnforcesEnvelopeBoundaryWithoutDispatch()
    {
        const string json = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"ping\",\"params\":{}}";
        const string payload = "{}";
        var body = Encoding.UTF8.GetBytes(json);
        var envelopeBytes = body.Length - Encoding.UTF8.GetByteCount(payload);

        var accepted = JsonRpcRequestParser.Parse(body, CreateOptions(maxEnvelopeBytes: envelopeBytes));
        var rejected = JsonRpcRequestParser.Parse(body, CreateOptions(maxEnvelopeBytes: envelopeBytes - 1));

        Assert.True(accepted.IsSuccess);
        Assert.False(rejected.IsSuccess);
        Assert.Equal(-32600, rejected.Error!.Code);
    }

    [Fact]
    public void Parse_RejectsInvalidUtf8()
    {
        var prefix = Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"");
        var suffix = Encoding.UTF8.GetBytes("\"}");
        var bytes = prefix.Concat(new byte[] { 0xc3, 0x28 }).Concat(suffix).ToArray();

        var result = JsonRpcRequestParser.Parse(bytes, RpcTransportOptions.Production);

        Assert.False(result.IsSuccess);
        Assert.Equal(-32700, result.Error!.Code);
    }

    [Fact]
    public void Parse_EnforcesJsonDepth()
    {
        const string json = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"ping\",\"params\":{\"a\":{\"b\":{}}}}";

        var accepted = JsonRpcRequestParser.Parse(Encoding.UTF8.GetBytes(json), CreateOptions(maxJsonDepth: 4));
        var rejected = JsonRpcRequestParser.Parse(Encoding.UTF8.GetBytes(json), CreateOptions(maxJsonDepth: 3));

        Assert.True(accepted.IsSuccess);
        Assert.False(rejected.IsSuccess);
        Assert.Equal(-32700, rejected.Error!.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(64)]
    public async Task Framing_ReadsExactAsciiBodyAcrossChunkSizes(int chunkSize)
    {
        var payload = Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{}}");
        await using var input = new ChunkedReadStream(Frame(payload), chunkSize);

        var result = await JsonRpcFraming.ReadFrameAsync(input, RpcTransportOptions.Production, CancellationToken.None);

        Assert.Equal(RpcFrameStatus.Frame, result.Status);
        Assert.Equal(payload, result.Payload);
    }

    [Fact]
    public async Task Framing_UsesUtf8ByteLengthAndAcceptsSupportedContentType()
    {
        var payload = Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"model/ping\",\"params\":{\"label\":\"café 🚀\"}}");
        var headers = Encoding.ASCII.GetBytes(
            $"Content-Length: {payload.Length}\r\nContent-Type: application/vscode-jsonrpc; charset=utf-8\r\n\r\n");
        await using var input = new MemoryStream(headers.Concat(payload).ToArray());

        var result = await JsonRpcFraming.ReadFrameAsync(input, RpcTransportOptions.Production, CancellationToken.None);

        Assert.Equal(RpcFrameStatus.Frame, result.Status);
        Assert.Equal(payload, result.Payload);
    }

    [Fact]
    public async Task Framing_ReturnsEndOfStreamOnlyBeforeAnyHeaderBytes()
    {
        await using var input = new MemoryStream();

        var result = await JsonRpcFraming.ReadFrameAsync(input, RpcTransportOptions.Production, CancellationToken.None);

        Assert.Equal(RpcFrameStatus.EndOfStream, result.Status);
        Assert.Null(result.Payload);
    }

    [Theory]
    [InlineData("Content-Length: nope\r\n\r\n")]
    [InlineData("Content-Length: 0\r\n\r\n")]
    [InlineData("Content-Length: 2\r\nContent-Length: 2\r\n\r\n{}")]
    [InlineData("Content-Length: 2\r\nX-Unknown: value\r\n\r\n{}")]
    [InlineData("Content-Length: 2\n\n{}")]
    [InlineData("Content-Type: application/json; charset=utf-8\r\n\r\n{}")]
    [InlineData("Malformed\r\nContent-Length: 2\r\n\r\n{}")]
    public async Task Framing_RejectsMalformedOrAmbiguousHeaders(string frame)
    {
        await using var input = new MemoryStream(Encoding.UTF8.GetBytes(frame));

        var result = await JsonRpcFraming.ReadFrameAsync(input, RpcTransportOptions.Production, CancellationToken.None);

        Assert.Equal(RpcFrameStatus.TerminalFault, result.Status);
        Assert.Null(result.Payload);
    }

    [Fact]
    public async Task Framing_RejectsOversizedDeclarationBeforeReadingBody()
    {
        var options = CreateOptions(maxRequestBytes: 4, maxPayloadBytes: 4, maxEnvelopeBytes: 4);
        await using var input = new CountingReadStream(Encoding.ASCII.GetBytes("Content-Length: 5\r\n\r\nabcde"));

        var result = await JsonRpcFraming.ReadFrameAsync(input, options, CancellationToken.None);

        Assert.Equal(RpcFrameStatus.TerminalFault, result.Status);
        Assert.True(input.BytesRead < input.Length);
    }

    [Fact]
    public async Task Framing_AcceptsExactRequestLimitAndRejectsOneByteOver()
    {
        var options = CreateOptions(maxRequestBytes: 4, maxPayloadBytes: 4, maxEnvelopeBytes: 4);
        await using var exactInput = new MemoryStream(Frame(Encoding.ASCII.GetBytes("1234")));
        await using var overInput = new MemoryStream(Frame(Encoding.ASCII.GetBytes("12345")));

        var exact = await JsonRpcFraming.ReadFrameAsync(exactInput, options, CancellationToken.None);
        var over = await JsonRpcFraming.ReadFrameAsync(overInput, options, CancellationToken.None);

        Assert.Equal(RpcFrameStatus.Frame, exact.Status);
        Assert.Equal(RpcFrameStatus.TerminalFault, over.Status);
    }

    [Fact]
    public async Task Framing_RejectsTruncatedBody()
    {
        await using var input = new MemoryStream(Encoding.ASCII.GetBytes("Content-Length: 4\r\n\r\n12"));

        var result = await JsonRpcFraming.ReadFrameAsync(input, RpcTransportOptions.Production, CancellationToken.None);

        Assert.Equal(RpcFrameStatus.TerminalFault, result.Status);
        Assert.Null(result.Payload);
    }

    [Fact]
    public async Task Framing_EnforcesHeaderLineBoundary()
    {
        var bytes = Encoding.ASCII.GetBytes("Content-Length: 2\r\n\r\n{}");
        await using var exactInput = new MemoryStream(bytes);
        await using var overInput = new MemoryStream(bytes);

        var exact = await JsonRpcFraming.ReadFrameAsync(
            exactInput,
            CreateOptions(maxHeaderBytes: 64, maxHeaderLineBytes: 17),
            CancellationToken.None);
        var over = await JsonRpcFraming.ReadFrameAsync(
            overInput,
            CreateOptions(maxHeaderBytes: 64, maxHeaderLineBytes: 16),
            CancellationToken.None);

        Assert.Equal(RpcFrameStatus.Frame, exact.Status);
        Assert.Equal(RpcFrameStatus.TerminalFault, over.Status);
    }

    [Fact]
    public async Task Framing_EnforcesTotalHeaderBoundary()
    {
        var bytes = Encoding.ASCII.GetBytes("Content-Length: 2\r\n\r\n{}");
        await using var exactInput = new MemoryStream(bytes);
        await using var overInput = new MemoryStream(bytes);

        var exact = await JsonRpcFraming.ReadFrameAsync(
            exactInput,
            CreateOptions(maxHeaderBytes: 21, maxHeaderLineBytes: 17),
            CancellationToken.None);
        var over = await JsonRpcFraming.ReadFrameAsync(
            overInput,
            CreateOptions(maxHeaderBytes: 20, maxHeaderLineBytes: 17),
            CancellationToken.None);

        Assert.Equal(RpcFrameStatus.Frame, exact.Status);
        Assert.Equal(RpcFrameStatus.TerminalFault, over.Status);
    }

    [Fact]
    public async Task Framing_EnforcesHeaderCountBoundary()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "Content-Length: 2\r\nContent-Type: application/json; charset=utf-8\r\n\r\n{}");
        await using var exactInput = new MemoryStream(bytes);
        await using var overInput = new MemoryStream(bytes);

        var exact = await JsonRpcFraming.ReadFrameAsync(
            exactInput,
            CreateOptions(maxHeaderBytes: 128, maxHeaderLineBytes: 64, maxHeaderCount: 2),
            CancellationToken.None);
        var over = await JsonRpcFraming.ReadFrameAsync(
            overInput,
            CreateOptions(maxHeaderBytes: 128, maxHeaderLineBytes: 64, maxHeaderCount: 1),
            CancellationToken.None);

        Assert.Equal(RpcFrameStatus.Frame, exact.Status);
        Assert.Equal(RpcFrameStatus.TerminalFault, over.Status);
    }

    private static RpcHost::PowerBIModelingService.RpcHost.JsonRpcParseResult Parse(string json) =>
        JsonRpcRequestParser.Parse(Encoding.UTF8.GetBytes(json), RpcTransportOptions.Production);

    private static RpcTransportOptions CreateOptions(
        int maxHeaderBytes = 256,
        int maxHeaderLineBytes = 128,
        int maxHeaderCount = 4,
        int maxRequestBytes = 1024,
        int maxPayloadBytes = 512,
        int maxEnvelopeBytes = 512,
        int maxJsonDepth = 16,
        int maxMethodBytes = 64,
        int maxRequestIdBytes = 32,
        int maxConcurrentRequests = 2,
        int maxRegisteredRequests = 4) => new(
            maxHeaderBytes,
            maxHeaderLineBytes,
            maxHeaderCount,
            maxRequestBytes,
            maxPayloadBytes,
            maxEnvelopeBytes,
            maxJsonDepth,
            maxMethodBytes,
            maxRequestIdBytes,
            maxResponseBytes: 2048,
            maxConcurrentRequests,
            maxRegisteredRequests);

    private static byte[] Frame(byte[] payload) =>
        Encoding.ASCII.GetBytes($"Content-Length: {payload.Length}\r\n\r\n")
            .Concat(payload)
            .ToArray();

    private sealed class ChunkedReadStream(byte[] bytes, int chunkSize) : MemoryStream(bytes)
    {
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            base.ReadAsync(buffer, offset, Math.Min(count, chunkSize), cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, chunkSize)], cancellationToken);
    }

    private sealed class CountingReadStream(byte[] bytes) : MemoryStream(bytes)
    {
        internal long BytesRead { get; private set; }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return CountAsync(base.ReadAsync(buffer, cancellationToken));
        }

        private async ValueTask<int> CountAsync(ValueTask<int> readTask)
        {
            var read = await readTask.ConfigureAwait(false);
            BytesRead += read;
            return read;
        }
    }
}
