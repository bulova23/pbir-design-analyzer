using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerBIModelingService.RpcHost;

internal readonly record struct RpcRequestId(
    JsonValueKind Kind,
    string CanonicalKey,
    JsonElement JsonValue);

internal sealed record ParsedJsonRpcRequest(
    string ProtocolVersion,
    RpcRequestId? Id,
    string Method,
    JsonElement? Params,
    ReadOnlyMemory<byte>? ParamsUtf8);

internal sealed record RpcProtocolError(
    int Code,
    string Message,
    RpcRequestId? ResponseId = null);

internal sealed record JsonRpcParseResult(
    ParsedJsonRpcRequest? Request,
    RpcProtocolError? Error)
{
    internal bool IsSuccess => Request is not null && Error is null;

    internal static JsonRpcParseResult Success(ParsedJsonRpcRequest request) => new(request, null);

    internal static JsonRpcParseResult Failure(
        int code,
        string message,
        RpcRequestId? responseId = null) => new(null, new RpcProtocolError(code, message, responseId));
}

internal static class JsonRpcRequestParser
{
    internal const int ParseErrorCode = -32700;
    internal const int InvalidRequestCode = -32600;
    internal const string ParseErrorMessage = "Parse error.";
    internal const string InvalidRequestMessage = "Invalid Request.";

    internal static JsonRpcParseResult Parse(ReadOnlySpan<byte> utf8Json, RpcTransportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (utf8Json.Length == 0 || utf8Json.Length > options.MaxRequestBytes)
        {
            return JsonRpcParseResult.Failure(InvalidRequestCode, InvalidRequestMessage);
        }

        try
        {
            var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = options.MaxJsonDepth,
            });

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                return JsonRpcParseResult.Failure(InvalidRequestCode, InvalidRequestMessage);
            }

            var seenJsonrpc = false;
            var seenId = false;
            var seenMethod = false;
            var seenParams = false;
            string? protocolVersion = null;
            string? method = null;
            RpcRequestId? id = null;
            JsonElement? parameters = null;
            byte[]? parametersUtf8 = null;
            var payloadBytes = 0L;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    return Invalid(id);
                }

                var isJsonrpc = reader.ValueTextEquals("jsonrpc"u8);
                var isId = reader.ValueTextEquals("id"u8);
                var isMethod = reader.ValueTextEquals("method"u8);
                var isParams = reader.ValueTextEquals("params"u8);
                if (!reader.Read())
                {
                    return ParseFailure();
                }

                if (isJsonrpc && !seenJsonrpc)
                {
                    seenJsonrpc = true;
                    if (reader.TokenType != JsonTokenType.String || !reader.ValueTextEquals("2.0"u8))
                    {
                        return Invalid(id);
                    }

                    protocolVersion = "2.0";
                }
                else if (isId && !seenId)
                {
                    seenId = true;
                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        continue;
                    }

                    if (!TryReadId(ref reader, options, out var parsedId))
                    {
                        return Invalid();
                    }

                    id = parsedId;
                }
                else if (isMethod && !seenMethod)
                {
                    seenMethod = true;
                    if (reader.TokenType != JsonTokenType.String ||
                        reader.ValueSpan.Length == 0 ||
                        reader.ValueSpan.Length > options.MaxMethodBytes)
                    {
                        return Invalid(id);
                    }

                    method = reader.GetString();
                    if (string.IsNullOrWhiteSpace(method) || method.Any(char.IsControl))
                    {
                        return Invalid(id);
                    }
                }
                else if (isParams && !seenParams)
                {
                    seenParams = true;
                    if (reader.TokenType is not JsonTokenType.StartObject and not JsonTokenType.StartArray)
                    {
                        return Invalid(id);
                    }

                    var payloadStart = checked((int)reader.TokenStartIndex);
                    if (!reader.TrySkip())
                    {
                        return ParseFailure();
                    }

                    var payloadEnd = checked((int)reader.BytesConsumed);
                    payloadBytes = payloadEnd - payloadStart;
                    if (payloadBytes > options.MaxPayloadBytes)
                    {
                        return Invalid(id);
                    }

                    parametersUtf8 = utf8Json[payloadStart..payloadEnd].ToArray();
                    using var document = JsonDocument.Parse(parametersUtf8);
                    parameters = document.RootElement.Clone();
                }
                else
                {
                    return Invalid(id);
                }
            }

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                return ParseFailure();
            }

            if (reader.Read())
            {
                return ParseFailure();
            }

            if (!seenJsonrpc || protocolVersion is null || !seenMethod || method is null)
            {
                return Invalid(id);
            }

            if (utf8Json.Length - payloadBytes > options.MaxEnvelopeBytes)
            {
                return Invalid(id);
            }

            return JsonRpcParseResult.Success(new ParsedJsonRpcRequest(
                protocolVersion,
                id,
                method,
                parameters,
                parametersUtf8));
        }
        catch (JsonException)
        {
            return ParseFailure();
        }
        catch (InvalidOperationException exception) when (exception.InnerException is DecoderFallbackException)
        {
            return ParseFailure();
        }
        catch (OverflowException)
        {
            return Invalid();
        }
    }

    internal static bool TryParseCancellationId(
        ReadOnlyMemory<byte>? parametersUtf8,
        RpcTransportOptions options,
        out RpcRequestId requestId)
    {
        requestId = default;
        if (!parametersUtf8.HasValue)
        {
            return false;
        }

        try
        {
            var reader = new Utf8JsonReader(parametersUtf8.Value.Span, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = options.MaxJsonDepth,
            });
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject ||
                !reader.Read() || reader.TokenType != JsonTokenType.PropertyName ||
                !reader.ValueTextEquals("id"u8) ||
                !reader.Read() ||
                !TryReadId(ref reader, options, out requestId) ||
                !reader.Read() || reader.TokenType != JsonTokenType.EndObject ||
                reader.Read())
            {
                requestId = default;
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            requestId = default;
            return false;
        }
    }

    private static bool TryReadId(
        ref Utf8JsonReader reader,
        RpcTransportOptions options,
        out RpcRequestId requestId)
    {
        requestId = default;
        var rawValue = reader.ValueSpan;

        if (reader.TokenType == JsonTokenType.String)
        {
            if (rawValue.Length == 0 || rawValue.Length > options.MaxRequestIdBytes)
            {
                return false;
            }

            var value = reader.GetString();
            if (string.IsNullOrEmpty(value) || value.Any(char.IsControl))
            {
                return false;
            }

            requestId = new RpcRequestId(
                JsonValueKind.String,
                $"s:{value}",
                CloneValue(rawValue, JsonValueKind.String));
            return true;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (rawValue.IndexOfAny((byte)'.', (byte)'e', (byte)'E') >= 0 ||
                !reader.TryGetInt64(out var value))
            {
                return false;
            }

            requestId = new RpcRequestId(
                JsonValueKind.Number,
                $"n:{value.ToString(CultureInfo.InvariantCulture)}",
                CloneValue(rawValue, JsonValueKind.Number));
            return true;
        }

        return false;
    }

    private static JsonElement CloneValue(ReadOnlySpan<byte> rawValue, JsonValueKind kind)
    {
        byte[] documentBytes;
        if (kind == JsonValueKind.String)
        {
            documentBytes = new byte[rawValue.Length + 2];
            documentBytes[0] = (byte)'"';
            rawValue.CopyTo(documentBytes.AsSpan(1));
            documentBytes[^1] = (byte)'"';
        }
        else
        {
            documentBytes = rawValue.ToArray();
        }

        using var document = JsonDocument.Parse(documentBytes);
        return document.RootElement.Clone();
    }

    private static JsonRpcParseResult ParseFailure() =>
        JsonRpcParseResult.Failure(ParseErrorCode, ParseErrorMessage);

    private static JsonRpcParseResult Invalid(RpcRequestId? id = null) =>
        JsonRpcParseResult.Failure(InvalidRequestCode, InvalidRequestMessage, id);
}

internal sealed class JsonRpcRequest
{
    public string Jsonrpc { get; set; } = "2.0";

    public JsonElement? Id { get; set; }

    public string Method { get; set; } = string.Empty;

    public JsonElement? Params { get; set; }
}

internal sealed class JsonRpcSuccessResponse
{
    public string Jsonrpc { get; set; } = "2.0";

    public JsonElement? Id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public object? Result { get; set; }
}

internal sealed class JsonRpcErrorEnvelope
{
    public string Jsonrpc { get; set; } = "2.0";

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public JsonElement? Id { get; set; }

    public JsonRpcErrorPayload Error { get; set; } = new();
}

internal sealed class JsonRpcErrorPayload
{
    public int Code { get; set; }

    public string Message { get; set; } = string.Empty;
}
