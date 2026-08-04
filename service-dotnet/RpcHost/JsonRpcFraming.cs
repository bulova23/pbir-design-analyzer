using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PowerBIModelingService.RpcHost;

internal enum RpcFrameStatus
{
    Frame,
    EndOfStream,
    TerminalFault,
}

internal enum RpcFrameError
{
    None,
    MalformedHeader,
    HeaderLimitExceeded,
    MissingContentLength,
    InvalidContentLength,
    RequestLimitExceeded,
    TruncatedBody,
}

internal sealed record RpcFrameReadResult(
    RpcFrameStatus Status,
    byte[]? Payload = null,
    RpcFrameError Error = RpcFrameError.None);

internal static class JsonRpcFraming
{
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/vscode-jsonrpc; charset=utf-8",
        "application/json; charset=utf-8",
    };

    internal static async Task<RpcFrameReadResult> ReadFrameAsync(
        Stream input,
        RpcTransportOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);

        var totalHeaderBytes = 0;
        var headerCount = 0;
        string? contentLengthValue = null;
        var sawContentType = false;

        while (true)
        {
            var lineResult = await ReadHeaderLineAsync(
                    input,
                    options,
                    totalHeaderBytes,
                    cancellationToken)
                .ConfigureAwait(false);
            totalHeaderBytes = lineResult.TotalBytes;

            if (lineResult.EndOfStream)
            {
                return totalHeaderBytes == 0
                    ? new RpcFrameReadResult(RpcFrameStatus.EndOfStream)
                    : Fault(RpcFrameError.MalformedHeader);
            }

            if (lineResult.Error != RpcFrameError.None)
            {
                return Fault(lineResult.Error);
            }

            if (lineResult.Line!.Length == 0)
            {
                break;
            }

            headerCount++;
            if (headerCount > options.MaxHeaderCount)
            {
                return Fault(RpcFrameError.HeaderLimitExceeded);
            }

            var separator = lineResult.Line.IndexOf(':');
            if (separator <= 0)
            {
                return Fault(RpcFrameError.MalformedHeader);
            }

            var name = lineResult.Line[..separator].Trim();
            var value = lineResult.Line[(separator + 1)..].Trim();
            if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                if (contentLengthValue is not null)
                {
                    return Fault(RpcFrameError.MalformedHeader);
                }

                contentLengthValue = value;
            }
            else if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                if (sawContentType || !SupportedContentTypes.Contains(value))
                {
                    return Fault(RpcFrameError.MalformedHeader);
                }

                sawContentType = true;
            }
            else
            {
                return Fault(RpcFrameError.MalformedHeader);
            }
        }

        if (contentLengthValue is null)
        {
            return Fault(RpcFrameError.MissingContentLength);
        }

        if (!int.TryParse(
                contentLengthValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var contentLength) ||
            contentLength <= 0)
        {
            return Fault(RpcFrameError.InvalidContentLength);
        }

        if (contentLength > options.MaxRequestBytes)
        {
            return Fault(RpcFrameError.RequestLimitExceeded);
        }

        var payload = new byte[contentLength];
        var offset = 0;
        while (offset < payload.Length)
        {
            var read = await input.ReadAsync(payload.AsMemory(offset), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return Fault(RpcFrameError.TruncatedBody);
            }

            offset += read;
        }

        return new RpcFrameReadResult(RpcFrameStatus.Frame, payload);
    }

    public static async Task<JsonRpcRequest?> ReadRequestAsync(
        Stream input,
        JsonSerializerOptions jsonOptions,
        ILogger logger)
    {
        _ = jsonOptions;
        _ = logger;
        var frame = await ReadFrameAsync(input, RpcTransportOptions.Production, CancellationToken.None)
            .ConfigureAwait(false);
        if (frame.Status == RpcFrameStatus.EndOfStream)
        {
            return null;
        }

        if (frame.Status != RpcFrameStatus.Frame)
        {
            throw new InvalidOperationException("The RPC request frame is invalid or incomplete.");
        }

        var parsed = JsonRpcRequestParser.Parse(frame.Payload!, RpcTransportOptions.Production);
        if (!parsed.IsSuccess)
        {
            throw new JsonException(parsed.Error!.Message);
        }

        return new JsonRpcRequest
        {
            Jsonrpc = parsed.Request!.ProtocolVersion,
            Id = parsed.Request.Id?.JsonValue,
            Method = parsed.Request.Method,
            Params = parsed.Request.Params,
        };
    }

    private static async Task<HeaderLineResult> ReadHeaderLineAsync(
        Stream input,
        RpcTransportOptions options,
        int initialTotalBytes,
        CancellationToken cancellationToken)
    {
        var bytes = new List<byte>(Math.Min(128, options.MaxHeaderLineBytes));
        var singleByte = new byte[1];
        var totalBytes = initialTotalBytes;

        while (true)
        {
            var read = await input.ReadAsync(singleByte.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return new HeaderLineResult(null, totalBytes, EndOfStream: true);
            }

            totalBytes++;
            if (totalBytes > options.MaxHeaderBytes)
            {
                return new HeaderLineResult(null, totalBytes, Error: RpcFrameError.HeaderLimitExceeded);
            }

            var value = singleByte[0];
            if (value == (byte)'\n')
            {
                if (bytes.Count == 0 || bytes[^1] != (byte)'\r')
                {
                    return new HeaderLineResult(null, totalBytes, Error: RpcFrameError.MalformedHeader);
                }

                bytes.RemoveAt(bytes.Count - 1);
                return new HeaderLineResult(Encoding.ASCII.GetString(bytes.ToArray()), totalBytes);
            }

            if (value < 0x20 || value > 0x7e)
            {
                if (value != (byte)'\r')
                {
                    return new HeaderLineResult(null, totalBytes, Error: RpcFrameError.MalformedHeader);
                }
            }

            bytes.Add(value);
            if (bytes.Count > options.MaxHeaderLineBytes + 1)
            {
                return new HeaderLineResult(null, totalBytes, Error: RpcFrameError.HeaderLimitExceeded);
            }

            if (value == (byte)'\r')
            {
                continue;
            }

            if (bytes.Count >= 2 && bytes[^2] == (byte)'\r')
            {
                return new HeaderLineResult(null, totalBytes, Error: RpcFrameError.MalformedHeader);
            }
        }
    }

    private static RpcFrameReadResult Fault(RpcFrameError error) =>
        new(RpcFrameStatus.TerminalFault, Error: error);

    private sealed record HeaderLineResult(
        string? Line,
        int TotalBytes,
        bool EndOfStream = false,
        RpcFrameError Error = RpcFrameError.None);
}
