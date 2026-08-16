using System.Text.Json;
using System.Text.Json.Serialization;
using PowerBIModelingService.PbirAuthoringRpc;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.RpcHost;

/// <summary>
/// Thin JSON-RPC adapter for the transport-independent Phase 45 authoring dispatcher.
/// It owns only wire validation and serialization; authoring behavior remains in Core.
/// </summary>
internal sealed class PbirAuthoringRpcAdapter
{
    private readonly PbirAuthoringRpcDispatcher _dispatcher;
    private readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();

    internal PbirAuthoringRpcAdapter(PbirAuthoringRpcDispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher ?? new PbirAuthoringRpcDispatcher();
    }

    internal async Task<JsonElement> HandleAsync(
        JsonElement? parameters,
        ReadOnlyMemory<byte>? parametersUtf8,
        CancellationToken cancellationToken)
    {
        if (!parameters.HasValue || parameters.Value.ValueKind != JsonValueKind.Object ||
            (parametersUtf8.HasValue && parametersUtf8.Value.Length > PbirAuthoringRpcHostContract.MaxRequestPayloadBytes))
        {
            return Failure("invalidRequest", "PBIR-RPC-REQUEST-001", "The authoring request must be a bounded JSON object.");
        }

        if (!parameters.Value.TryGetProperty("operation", out var operation) ||
            operation.ValueKind != JsonValueKind.String ||
            !IsExposedOperation(operation.GetString()))
        {
            return Failure("invalidRequest", "PBIR-RPC-REQUEST-002", "Only Generate, Import, Analyze, and the curated mutation catalog are exposed through VS Code.");
        }

        if (!parameters.Value.TryGetProperty("schemaVersion", out var schemaVersion) ||
            schemaVersion.ValueKind != JsonValueKind.String ||
            !string.Equals(schemaVersion.GetString(), PbirAuthoringRpcContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            return Failure("invalidRequest", "PBIR-RPC-REQUEST-003", "The authoring request schema version is unsupported.");
        }

        try
        {
            var request = JsonSerializer.Deserialize<PbirAuthoringRpcRequest>(parameters.Value.GetRawText(), _jsonOptions);
            if (request is null)
                return Failure("invalidRequest", "PBIR-RPC-REQUEST-003", "The authoring request could not be deserialized.");

            var publicOperations = request.Mutate?.Request?.Operations;
            if (request.Operation == PbirAuthoringRpcOperation.Mutate &&
                (publicOperations is null || publicOperations.Count != 1))
            {
                return Failure("unsupportedAuthoring", "PBIR-RPC-MUTATE-009", "Exactly one curated mutation operation is required.");
            }

            if (request.Operation == PbirAuthoringRpcOperation.Mutate &&
                !IsCuratedMutation(publicOperations![0].Kind))
            {
                return Failure("unsupportedAuthoring", "PBIR-RPC-MUTATE-008", "The requested mutation is backend-only and is not exposed through VS Code.");
            }

            var response = await _dispatcher.DispatchAsync(request, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.SerializeToElement(response, _jsonOptions);
        }
        catch (JsonException)
        {
            return Failure("invalidRequest", "PBIR-RPC-REQUEST-004", "The authoring request has an unsupported typed payload.");
        }
        catch (NotSupportedException)
        {
            return Failure("invalidRequest", "PBIR-RPC-REQUEST-004", "The authoring request has an unsupported typed payload.");
        }
    }

    private static bool IsExposedOperation(string? operation) =>
        operation is not null &&
        (operation.Equals("generate", StringComparison.OrdinalIgnoreCase) ||
         operation.Equals("import", StringComparison.OrdinalIgnoreCase) ||
         operation.Equals("mutate", StringComparison.OrdinalIgnoreCase) ||
         operation.Equals("analyze", StringComparison.OrdinalIgnoreCase));

    private static bool IsCuratedMutation(LocalPbirMutationOperationKind kind) =>
        kind is LocalPbirMutationOperationKind.RenamePage or
            LocalPbirMutationOperationKind.AddPage or
            LocalPbirMutationOperationKind.RemovePage or
            LocalPbirMutationOperationKind.MovePage or
            LocalPbirMutationOperationKind.MoveVisual or
            LocalPbirMutationOperationKind.ResizeVisual;

    private static JsonElement Failure(string category, string code, string summary) =>
        JsonSerializer.SerializeToElement(new
        {
            schemaVersion = "pbir-authoring-rpc/v1",
            operation = "unknown",
            succeeded = false,
            diagnostics = Array.Empty<object>(),
            error = new { category, code, summary },
            timing = new { dispatchMilliseconds = 0, orchestrationMilliseconds = 0, serializationMilliseconds = 0, analyzerMilliseconds = 0 },
        });

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new PbirAuthoringGenerationRequestConverter());
        return options;
    }
}

internal sealed class PbirAuthoringGenerationRequestConverter : JsonConverter<PbirAuthoringGenerationRequest>
{
    public override PbirAuthoringGenerationRequest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var value = document.RootElement;
        var flags = new[] { "v1", "v2", "v3", "v4", "v5", "v6", "v7" };
        var present = flags.Where(flag => value.TryGetProperty(flag, out _)).ToArray();
        if (present.Length != 1)
            throw new JsonException("Exactly one generation request version is required.");

        return present[0] switch
        {
            "v1" => new(JsonSerializer.Deserialize<LocalPbirGenerationRequest>(value.GetProperty("v1"), options)!),
            "v2" => new(JsonSerializer.Deserialize<LocalPbirGenerationRequestV2>(value.GetProperty("v2"), options)!),
            "v3" => new(JsonSerializer.Deserialize<LocalPbirGenerationRequestV3>(value.GetProperty("v3"), options)!),
            "v4" => new(JsonSerializer.Deserialize<LocalPbirGenerationRequestV4>(value.GetProperty("v4"), options)!),
            "v5" => new(JsonSerializer.Deserialize<LocalPbirGenerationRequestV5>(value.GetProperty("v5"), options)!),
            "v6" => new(JsonSerializer.Deserialize<LocalPbirGenerationRequestV6>(value.GetProperty("v6"), options)!),
            "v7" => new(JsonSerializer.Deserialize<LocalPbirGenerationRequestV7>(value.GetProperty("v7"), options)!),
            _ => throw new JsonException("Unsupported generation request version.")
        };
    }

    public override void Write(Utf8JsonWriter writer, PbirAuthoringGenerationRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (value.Kind)
        {
            case PbirAuthoringGenerationRequestKind.V1:
                writer.WritePropertyName("v1");
                JsonSerializer.Serialize(writer, value.V1, options);
                break;
            case PbirAuthoringGenerationRequestKind.V2:
                writer.WritePropertyName("v2");
                JsonSerializer.Serialize(writer, value.V2, options);
                break;
            case PbirAuthoringGenerationRequestKind.V3:
                writer.WritePropertyName("v3");
                JsonSerializer.Serialize(writer, value.V3, options);
                break;
            case PbirAuthoringGenerationRequestKind.V4:
                writer.WritePropertyName("v4");
                JsonSerializer.Serialize(writer, value.V4, options);
                break;
            case PbirAuthoringGenerationRequestKind.V5:
                writer.WritePropertyName("v5");
                JsonSerializer.Serialize(writer, value.V5, options);
                break;
            case PbirAuthoringGenerationRequestKind.V6:
                writer.WritePropertyName("v6");
                JsonSerializer.Serialize(writer, value.V6, options);
                break;
            case PbirAuthoringGenerationRequestKind.V7:
                writer.WritePropertyName("v7");
                JsonSerializer.Serialize(writer, value.V7, options);
                break;
            default:
                throw new JsonException("Unsupported generation request version.");
        }
        writer.WriteEndObject();
    }
}
