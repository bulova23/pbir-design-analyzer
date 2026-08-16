extern alias RpcHost;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Xunit;
using IRpcRequestHandler = RpcHost::PowerBIModelingService.RpcHost.IRpcRequestHandler;
using ParsedJsonRpcRequest = RpcHost::PowerBIModelingService.RpcHost.ParsedJsonRpcRequest;
using RpcHandlerResult = RpcHost::PowerBIModelingService.RpcHost.RpcHandlerResult;
using JsonRpcRequestParser = RpcHost::PowerBIModelingService.RpcHost.JsonRpcRequestParser;
using RpcRegistrationStatus = RpcHost::PowerBIModelingService.RpcHost.RpcRegistrationStatus;
using RpcRequestRegistry = RpcHost::PowerBIModelingService.RpcHost.RpcRequestRegistry;
using RpcTerminalOutcome = RpcHost::PowerBIModelingService.RpcHost.RpcTerminalOutcome;
using RpcTransportOptions = RpcHost::PowerBIModelingService.RpcHost.RpcTransportOptions;
using SimpleJsonRpcServer = RpcHost::PowerBIModelingService.RpcHost.SimpleJsonRpcServer;

namespace ServiceDotnet.Tests;

public sealed class RpcHostLifecycleTests
{
    [Fact]
    public void Registry_DistinguishesTypedIdentifiers()
    {
        using var registry = new RpcRequestRegistry(maxRegisteredRequests: 2, CancellationToken.None);

        var numeric = registry.TryRegister(Id(1));
        var text = registry.TryRegister(Id("1"));

        Assert.Equal(RpcRegistrationStatus.Registered, numeric.Status);
        Assert.Equal(RpcRegistrationStatus.Registered, text.Status);
        Assert.Equal(2, registry.Count);
    }

    [Fact]
    public void Registry_EnforcesCapacityAtBoundary()
    {
        using var registry = new RpcRequestRegistry(maxRegisteredRequests: 2, CancellationToken.None);

        Assert.Equal(RpcRegistrationStatus.Registered, registry.TryRegister(Id(1)).Status);
        Assert.Equal(RpcRegistrationStatus.Registered, registry.TryRegister(Id(2)).Status);
        Assert.Equal(RpcRegistrationStatus.CapacityExceeded, registry.TryRegister(Id(3)).Status);
        Assert.Equal(2, registry.Count);
    }

    [Fact]
    public void Registry_ReturnsExistingRegistrationForDuplicateActiveId()
    {
        using var registry = new RpcRequestRegistry(maxRegisteredRequests: 2, CancellationToken.None);
        var original = registry.TryRegister(Id("duplicate"));

        var duplicate = registry.TryRegister(Id("duplicate"));

        Assert.Equal(RpcRegistrationStatus.Duplicate, duplicate.Status);
        Assert.Same(original.Registration, duplicate.Registration);
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void CancellationBeforeDispatch_ClaimsOutcomeAndPreventsDispatch()
    {
        using var registry = new RpcRequestRegistry(maxRegisteredRequests: 1, CancellationToken.None);
        var registration = registry.TryRegister(Id(1)).Registration!;

        Assert.True(registration.TryCancel(RpcTerminalOutcome.Cancelled));
        Assert.True(registration.Token.IsCancellationRequested);
        Assert.False(registration.TryMarkDispatched());
        Assert.False(registration.TryClaim(RpcTerminalOutcome.Completed));
    }

    [Fact]
    public void CancellationDuringExecution_CancelsTokenAndOwnsTerminalResponse()
    {
        using var registry = new RpcRequestRegistry(maxRegisteredRequests: 1, CancellationToken.None);
        var registration = registry.TryRegister(Id(1)).Registration!;
        Assert.True(registration.TryMarkDispatched());

        Assert.True(registration.TryCancel(RpcTerminalOutcome.Cancelled));

        Assert.True(registration.Token.IsCancellationRequested);
        Assert.Equal(RpcTerminalOutcome.Cancelled, registration.Outcome);
        Assert.False(registration.TryClaim(RpcTerminalOutcome.Faulted));
    }

    [Fact]
    public void CancellationAfterCompletionAndRepeatedCancellation_AreIgnored()
    {
        using var registry = new RpcRequestRegistry(maxRegisteredRequests: 1, CancellationToken.None);
        var registration = registry.TryRegister(Id(1)).Registration!;

        Assert.True(registration.TryClaim(RpcTerminalOutcome.Completed));
        Assert.False(registration.TryCancel(RpcTerminalOutcome.Cancelled));
        Assert.False(registration.TryCancel(RpcTerminalOutcome.Cancelled));
        Assert.False(registration.Token.IsCancellationRequested);
        Assert.Equal(RpcTerminalOutcome.Completed, registration.Outcome);
    }

    [Fact]
    public async Task CompletionCancellationRace_HasExactlyOneWinner()
    {
        using var registry = new RpcRequestRegistry(maxRegisteredRequests: 1, CancellationToken.None);
        var registration = registry.TryRegister(Id(1)).Registration!;
        using var barrier = new Barrier(2);

        var completion = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return registration.TryClaim(RpcTerminalOutcome.Completed);
        });
        var cancellation = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return registration.TryCancel(RpcTerminalOutcome.Cancelled);
        });

        var results = await Task.WhenAll(completion, cancellation);

        Assert.Single(results.Where(result => result));
        Assert.Contains(registration.Outcome, new[] { RpcTerminalOutcome.Completed, RpcTerminalOutcome.Cancelled });
    }

    [Fact]
    public void RemovedIdentifier_CanBeReusedAndResourcesAreDisposed()
    {
        using var registry = new RpcRequestRegistry(maxRegisteredRequests: 1, CancellationToken.None);
        var first = registry.TryRegister(Id("reuse")).Registration!;
        Assert.True(first.TryClaim(RpcTerminalOutcome.Completed));

        Assert.True(registry.RemoveAndDispose(first));
        var second = registry.TryRegister(Id("reuse"));

        Assert.True(first.IsDisposed);
        Assert.Equal(RpcRegistrationStatus.Registered, second.Status);
        Assert.NotSame(first, second.Registration);
    }

    [Fact]
    public void CancelAll_StopsAcceptanceAndCancelsEveryActiveRegistration()
    {
        using var registry = new RpcRequestRegistry(maxRegisteredRequests: 3, CancellationToken.None);
        var first = registry.TryRegister(Id(1)).Registration!;
        var second = registry.TryRegister(Id(2)).Registration!;

        var cancelled = registry.StopAndCancelAll(RpcTerminalOutcome.ShuttingDown);

        Assert.Equal(2, cancelled.Count);
        Assert.True(first.Token.IsCancellationRequested);
        Assert.True(second.Token.IsCancellationRequested);
        Assert.Equal(RpcRegistrationStatus.ShuttingDown, registry.TryRegister(Id(3)).Status);
    }

    [Fact]
    public void Dispose_IsIdempotentAndCleansRegistrations()
    {
        var registry = new RpcRequestRegistry(maxRegisteredRequests: 2, CancellationToken.None);
        var first = registry.TryRegister(Id(1)).Registration!;
        var second = registry.TryRegister(Id(2)).Registration!;

        registry.Dispose();
        registry.Dispose();

        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
        Assert.Equal(0, registry.Count);
        Assert.True(registry.IsDisposed);
    }

    [Fact]
    public async Task Server_DispatchesIndependentRequestsConcurrentlyAndCompletesOutOfOrder()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output, maxConcurrentRequests: 2);
        var run = server.RunAsync();

        input.Push(Request(1, "first"));
        input.Push(Request(2, "second"));
        await handler.WaitStartedAsync("first");
        await handler.WaitStartedAsync("second");

        handler.Complete("second", "second-result");
        await output.WaitForFlushCountAsync(1);
        handler.Complete("first", "first-result");
        await output.WaitForFlushCountAsync(2);
        input.Push(Request(99, "shutdown"));

        await run;
        var frames = ParseFrames(output.ToArray());

        Assert.Equal(new long[] { 2, 1, 99 }, frames.Select(ReadNumericId));
        Assert.True(handler.MaximumConcurrency >= 2);
        Assert.Equal(0, server.ActiveRequestCount);
        Assert.Equal(0, server.TrackedTaskCount);
    }

    [Fact]
    public async Task Server_CancelsQueuedRequestBeforeDispatch()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output, maxConcurrentRequests: 1);
        var run = server.RunAsync();

        input.Push(Request(1, "blocking"));
        await handler.WaitStartedAsync("blocking");
        input.Push(Request(2, "queued"));
        input.Push(Cancel(2));
        await output.WaitForFlushCountAsync(1);

        Assert.False(handler.HasStarted("queued"));
        handler.Complete("blocking", "done");
        await output.WaitForFlushCountAsync(2);
        input.Push(Request(99, "shutdown"));

        await run;
        var cancellation = Assert.Single(ParseFrames(output.ToArray()).Where(frame => ReadNumericId(frame) == 2));
        Assert.Equal(-32800, cancellation.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Server_CancelsExecutingRequestAndIgnoresRepeatedCancellation()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Request(1, "blocking"));
        await handler.WaitStartedAsync("blocking");
        input.Push(Cancel(1));
        input.Push(Cancel(1));
        await output.WaitForFlushCountAsync(1);
        await handler.WaitCancelledAsync("blocking");
        input.Push(Request(99, "shutdown"));

        await run;
        var responses = ParseFrames(output.ToArray()).Where(frame => ReadNumericId(frame) == 1).ToList();
        var cancellation = Assert.Single(responses);
        Assert.Equal(-32800, cancellation.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Server_IgnoresCancellationBeforeRegistrationAndAfterCompletion()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Cancel(1));
        input.Push(Request(1, "work"));
        await handler.WaitStartedAsync("work");
        handler.Complete("work", "done");
        await output.WaitForFlushCountAsync(1);
        input.Push(Cancel(1));
        input.Push(Request(99, "shutdown"));

        await run;
        var response = Assert.Single(ParseFrames(output.ToArray()).Where(frame => ReadNumericId(frame) == 1));
        Assert.Equal("done", response.GetProperty("result").GetString());
    }

    [Fact]
    public async Task Server_DuplicateActiveIdCancelsOriginalAndWritesOneDeterministicError()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Request(1, "blocking"));
        await handler.WaitStartedAsync("blocking");
        input.Push(Request(1, "duplicate"));
        await output.WaitForFlushCountAsync(1);
        await handler.WaitCancelledAsync("blocking");
        input.Push(Request(99, "shutdown"));

        await run;
        var duplicate = Assert.Single(ParseFrames(output.ToArray()).Where(frame => ReadNumericId(frame) == 1));
        Assert.Equal(-32600, duplicate.GetProperty("error").GetProperty("code").GetInt32());
        Assert.False(handler.HasStarted("duplicate"));
    }

    [Fact]
    public async Task Server_RedactsFaultDetailsFromResponse()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        handler.Fault("fault", new InvalidOperationException("/sensitive/path transaction-31-secret"));
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Request(1, "fault"));
        await output.WaitForFlushCountAsync(1);
        input.Push(Request(99, "shutdown"));

        await run;
        var responseBytes = Encoding.UTF8.GetString(output.ToArray());
        var fault = Assert.Single(ParseFrames(output.ToArray()).Where(frame => ReadNumericId(frame) == 1));
        Assert.Equal(-32603, fault.GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal("Internal error.", fault.GetProperty("error").GetProperty("message").GetString());
        Assert.DoesNotContain("sensitive", responseBytes, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("transaction-31", responseBytes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Server_ShutdownWithMultipleInflightRequestsCancelsDrainsAndDisposesOnce()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output, maxConcurrentRequests: 2);
        var run = server.RunAsync();

        input.Push(Request(1, "first"));
        input.Push(Request(2, "second"));
        await handler.WaitStartedAsync("first");
        await handler.WaitStartedAsync("second");
        input.Push(Request(99, "shutdown"));

        await run;
        await server.ShutdownAsync();
        var frames = ParseFrames(output.ToArray());

        Assert.Contains(frames, frame => ReadNumericId(frame) == 1 && frame.GetProperty("error").GetProperty("code").GetInt32() == -32800);
        Assert.Contains(frames, frame => ReadNumericId(frame) == 2 && frame.GetProperty("error").GetProperty("code").GetInt32() == -32800);
        Assert.Contains(frames, frame => ReadNumericId(frame) == 99 && frame.GetProperty("result").ValueKind == JsonValueKind.Null);
        Assert.Equal(0, server.ActiveRequestCount);
        Assert.Equal(0, server.TrackedTaskCount);
        Assert.True(server.IsDisposed);
        Assert.Equal(1, handler.DisposeCount);
    }

    [Fact]
    public async Task Server_ShutdownWithZeroInflightRequestsIsIdempotent()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Request(99, "shutdown"));
        await run;
        await Task.WhenAll(server.ShutdownAsync(), server.ShutdownAsync());

        var shutdown = Assert.Single(ParseFrames(output.ToArray()));
        Assert.Equal(99, ReadNumericId(shutdown));
        Assert.Equal(JsonValueKind.Null, shutdown.GetProperty("result").ValueKind);
        Assert.Equal(0, server.ActiveRequestCount);
        Assert.Equal(0, server.TrackedTaskCount);
        Assert.Equal(1, handler.DisposeCount);
    }

    [Fact]
    public async Task Server_ShutdownWithOneInflightRequestCancelsAndDrainsIt()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Request(1, "blocking"));
        await handler.WaitStartedAsync("blocking");
        input.Push(Request(99, "shutdown"));

        await run;
        var frames = ParseFrames(output.ToArray());

        Assert.Contains(frames, frame => ReadNumericId(frame) == 1 && frame.GetProperty("error").GetProperty("code").GetInt32() == -32800);
        Assert.Contains(frames, frame => ReadNumericId(frame) == 99 && frame.GetProperty("result").ValueKind == JsonValueKind.Null);
        Assert.True(handler.WasCancelled("blocking"));
        Assert.Equal(0, server.ActiveRequestCount);
        Assert.Equal(0, server.TrackedTaskCount);
    }

    [Fact]
    public async Task Server_DisconnectCancelsInflightWorkAndSuppressesResponses()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Request(1, "blocking"));
        await handler.WaitStartedAsync("blocking");
        input.Complete();

        await run;
        Assert.True(handler.WasCancelled("blocking"));
        Assert.Empty(output.ToArray());
        Assert.Equal(0, server.ActiveRequestCount);
        Assert.Equal(0, server.TrackedTaskCount);
        Assert.True(server.IsDisposed);
    }

    [Fact]
    public async Task Server_DiagnosticsUseBoundedCorrelationAndRedactPeerAndExceptionData()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        var logger = new CapturingLogger<SimpleJsonRpcServer>();
        handler.Fault("fault", new InvalidOperationException("/secret/phase31/transaction/internal"));
        await using var server = CreateServer(handler, input, output, logger: logger);
        var run = server.RunAsync();

        input.Push(Frame("{\"jsonrpc\":\"2.0\",\"id\":\"secret-request-id\",\"method\":\"fault\",\"params\":{\"reportPath\":\"/secret/report.Report\",\"payload\":\"private-content\"}}"));
        await output.WaitForFlushCountAsync(1);
        input.Push(Request(99, "shutdown"));

        await run;
        var diagnostics = string.Join('\n', logger.Messages);

        Assert.Contains("handler_fault", diagnostics);
        Assert.Contains("correlation=", diagnostics);
        Assert.DoesNotContain("secret-request-id", diagnostics);
        Assert.DoesNotContain("/secret", diagnostics);
        Assert.DoesNotContain("phase31", diagnostics, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("transaction", diagnostics, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-content", diagnostics);
        Assert.All(logger.Messages, message => Assert.True(message.Length <= 256));
    }

    [Fact]
    public async Task Server_RejectsInvalidEnvelopeBeforeDispatchAndContinuesAtNextFrame()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Frame("{\"jsonrpc\":\"2.0\",\"id\":7,\"method\":\"invalid\",\"unknown\":true}"));
        await output.WaitForFlushCountAsync(1);
        input.Push(Request(1, "work"));
        await handler.WaitStartedAsync("work");
        handler.Complete("work", "done");
        await output.WaitForFlushCountAsync(2);
        input.Push(Request(99, "shutdown"));

        await run;
        var frames = ParseFrames(output.ToArray());
        Assert.Contains(frames, frame => ReadNumericId(frame) == 7 && frame.GetProperty("error").GetProperty("code").GetInt32() == -32600);
        Assert.Contains(frames, frame => ReadNumericId(frame) == 1 && frame.GetProperty("result").GetString() == "done");
        Assert.False(handler.HasStarted("invalid"));
    }

    [Fact]
    public async Task Server_RejectsMalformedJsonAndContinuesAtKnownFrameBoundary()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Frame("{"));
        await output.WaitForFlushCountAsync(1);
        input.Push(Request(1, "work"));
        await handler.WaitStartedAsync("work");
        handler.Complete("work", "done");
        await output.WaitForFlushCountAsync(2);
        input.Push(Request(99, "shutdown"));

        await run;
        var parseError = Assert.Single(ParseFrames(output.ToArray()).Where(frame =>
            frame.GetProperty("id").ValueKind == JsonValueKind.Null));
        Assert.Equal(-32700, parseError.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Server_TreatsMalformedFramingAsTerminalAndDoesNotDispatch()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Encoding.ASCII.GetBytes("Content-Length: nope\r\n\r\n"));
        await run;

        Assert.Empty(output.ToArray());
        Assert.Equal(0, server.ActiveRequestCount);
        Assert.Equal(0, server.TrackedTaskCount);
        Assert.False(handler.HasStarted("work"));
    }

    [Fact]
    public async Task Server_RejectsCancellationSentAsRequestWithoutCancellingTarget()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new ControlledHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Request(1, "work"));
        await handler.WaitStartedAsync("work");
        input.Push(Frame("{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"$/cancelRequest\",\"params\":{\"id\":1}}"));
        await output.WaitForFlushCountAsync(1);

        Assert.False(handler.WasCancelled("work"));
        handler.Complete("work", "done");
        await output.WaitForFlushCountAsync(2);
        input.Push(Request(99, "shutdown"));

        await run;
        var invalidCancellation = Assert.Single(ParseFrames(output.ToArray()).Where(frame => ReadNumericId(frame) == 2));
        Assert.Equal(-32600, invalidCancellation.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Server_HandlerDisposeFaultDoesNotPreventLifecycleCleanup()
    {
        await using var input = new PushInputStream();
        await using var output = new FlushObservedStream();
        var handler = new DisposeFaultingHandler();
        await using var server = CreateServer(handler, input, output);
        var run = server.RunAsync();

        input.Push(Request(99, "shutdown"));
        await run;

        Assert.Equal(1, handler.DisposeCount);
        Assert.Equal(0, server.ActiveRequestCount);
        Assert.Equal(0, server.TrackedTaskCount);
        Assert.True(server.IsDisposed);
    }

    private static RpcHost::PowerBIModelingService.RpcHost.RpcRequestId Id(string value) =>
        ParseId(JsonSerializer.Serialize(value));

    private static RpcHost::PowerBIModelingService.RpcHost.RpcRequestId Id(long value) =>
        ParseId(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static RpcHost::PowerBIModelingService.RpcHost.RpcRequestId ParseId(string idJson) =>
        JsonRpcRequestParser.Parse(
            Encoding.UTF8.GetBytes($"{{\"jsonrpc\":\"2.0\",\"id\":{idJson},\"method\":\"test\"}}"),
            Options()).Request!.Id!.Value;

    private static RpcTransportOptions Options() => new(
        maxHeaderBytes: 256,
        maxHeaderLineBytes: 128,
        maxHeaderCount: 4,
        maxRequestBytes: 2048,
        maxPayloadBytes: 1024,
        maxEnvelopeBytes: 1024,
        maxJsonDepth: 16,
        maxMethodBytes: 64,
        maxRequestIdBytes: 32,
        maxResponseBytes: 4096,
        maxConcurrentRequests: 2,
        maxRegisteredRequests: 4);

    private static SimpleJsonRpcServer CreateServer(
        IRpcRequestHandler handler,
        Stream input,
        Stream output,
        int maxConcurrentRequests = 2,
        ILogger<SimpleJsonRpcServer>? logger = null) => new(
            handler,
            input,
            output,
            logger ?? NullLogger<SimpleJsonRpcServer>.Instance,
            new RpcTransportOptions(
                maxHeaderBytes: 1024,
                maxHeaderLineBytes: 512,
                maxHeaderCount: 8,
                maxRequestBytes: 4096,
                maxPayloadBytes: 3072,
                maxEnvelopeBytes: 1024,
                maxJsonDepth: 32,
                maxMethodBytes: 128,
                maxRequestIdBytes: 64,
                maxResponseBytes: 4096,
                maxConcurrentRequests,
                maxRegisteredRequests: 8));

    private static byte[] Request(long id, string method) => Frame(
        $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"method\":{JsonSerializer.Serialize(method)},\"params\":{{}}}}");

    private static byte[] Cancel(long id) => Frame(
        $"{{\"jsonrpc\":\"2.0\",\"method\":\"$/cancelRequest\",\"params\":{{\"id\":{id}}}}}");

    private static byte[] Frame(string json)
    {
        var payload = Encoding.UTF8.GetBytes(json);
        return Encoding.ASCII.GetBytes($"Content-Length: {payload.Length}\r\n\r\n")
            .Concat(payload)
            .ToArray();
    }

    private static IReadOnlyList<JsonElement> ParseFrames(byte[] bytes)
    {
        var frames = new List<JsonElement>();
        var offset = 0;
        while (offset < bytes.Length)
        {
            var separator = Encoding.ASCII.GetString(bytes, offset, bytes.Length - offset)
                .IndexOf("\r\n\r\n", StringComparison.Ordinal);
            Assert.True(separator >= 0);
            separator += offset;
            var header = Encoding.ASCII.GetString(bytes, offset, separator - offset);
            var length = int.Parse(header["Content-Length: ".Length..]);
            var bodyStart = separator + 4;
            using var document = JsonDocument.Parse(bytes.AsMemory(bodyStart, length));
            frames.Add(document.RootElement.Clone());
            offset = bodyStart + length;
        }

        return frames;
    }

    private static long ReadNumericId(JsonElement frame) => frame.GetProperty("id").GetInt64();

    private sealed class PushInputStream : Stream
    {
        private readonly Channel<byte[]> _chunks = Channel.CreateUnbounded<byte[]>();
        private byte[]? _current;
        private int _offset;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        internal void Push(byte[] bytes) => Assert.True(_chunks.Writer.TryWrite(bytes));
        internal void Complete() => _chunks.Writer.TryComplete();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            while (_current is null || _offset == _current.Length)
            {
                if (!await _chunks.Reader.WaitToReadAsync(cancellationToken))
                {
                    return 0;
                }

                if (_chunks.Reader.TryRead(out _current))
                {
                    _offset = 0;
                }
            }

            var count = Math.Min(buffer.Length, _current.Length - _offset);
            _current.AsMemory(_offset, count).CopyTo(buffer);
            _offset += count;
            return count;
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class FlushObservedStream : Stream
    {
        private readonly MemoryStream _inner = new();
        private readonly object _sync = new();
        private readonly List<TaskCompletionSource> _waiters = [];
        private int _flushCount;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

        internal byte[] ToArray()
        {
            lock (_sync)
            {
                return _inner.ToArray();
            }
        }

        internal Task WaitForFlushCountAsync(int count)
        {
            lock (_sync)
            {
                if (_flushCount >= count)
                {
                    return Task.CompletedTask;
                }

                while (_waiters.Count < count)
                {
                    _waiters.Add(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
                }

                return _waiters[count - 1].Task;
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                _inner.Write(buffer.Span);
            }

            return ValueTask.CompletedTask;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                _flushCount++;
                if (_waiters.Count >= _flushCount)
                {
                    _waiters[_flushCount - 1].TrySetResult();
                }
            }

            return Task.CompletedTask;
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_sync)
            {
                _inner.Write(buffer, offset, count);
            }
        }
    }

    private sealed class ControlledHandler : IRpcRequestHandler
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _started = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _completions = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _cancelled = new();
        private int _concurrency;
        private int _maximumConcurrency;
        private int _disposeCount;

        internal int MaximumConcurrency => Volatile.Read(ref _maximumConcurrency);
        internal int DisposeCount => Volatile.Read(ref _disposeCount);

        public async Task<RpcHandlerResult> HandleAsync(
            ParsedJsonRpcRequest request,
            CancellationToken cancellationToken)
        {
            var concurrency = Interlocked.Increment(ref _concurrency);
            UpdateMaximum(concurrency);
            var started = _started.GetOrAdd(request.Method, _ => NewSignal());
            var completion = _completions.GetOrAdd(request.Method, _ => NewCompletion());
            started.TrySetResult();

            try
            {
                var result = await completion.Task.WaitAsync(cancellationToken);
                if (result is Exception exception)
                {
                    throw exception;
                }

                return RpcHandlerResult.Success(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _cancelled.GetOrAdd(request.Method, _ => NewSignal()).TrySetResult();
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _concurrency);
            }
        }

        internal Task WaitStartedAsync(string method) =>
            _started.GetOrAdd(method, _ => NewSignal()).Task;

        internal Task WaitCancelledAsync(string method) =>
            _cancelled.GetOrAdd(method, _ => NewSignal()).Task;

        internal bool HasStarted(string method) =>
            _started.TryGetValue(method, out var signal) && signal.Task.IsCompleted;

        internal bool WasCancelled(string method) =>
            _cancelled.TryGetValue(method, out var signal) && signal.Task.IsCompleted;

        internal void Complete(string method, object? result) =>
            _completions.GetOrAdd(method, _ => NewCompletion()).TrySetResult(result);

        internal void Fault(string method, Exception exception) => Complete(method, exception);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }

        private void UpdateMaximum(int concurrency)
        {
            while (true)
            {
                var current = Volatile.Read(ref _maximumConcurrency);
                if (current >= concurrency || Interlocked.CompareExchange(ref _maximumConcurrency, concurrency, current) == current)
                {
                    return;
                }
            }
        }

        private static TaskCompletionSource NewSignal() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private static TaskCompletionSource<object?> NewCompletion() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        internal List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Assert.Null(exception);
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class DisposeFaultingHandler : IRpcRequestHandler
    {
        private int _disposeCount;

        internal int DisposeCount => Volatile.Read(ref _disposeCount);

        public Task<RpcHandlerResult> HandleAsync(
            ParsedJsonRpcRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(RpcHandlerResult.Success(null));

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.FromException(new InvalidOperationException("sensitive dispose failure"));
        }
    }
}
