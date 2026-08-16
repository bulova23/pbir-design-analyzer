using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35ACanonicalJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    internal byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);
    internal T Deserialize<T>(byte[] bytes) => JsonSerializer.Deserialize<T>(bytes, Options) ?? throw new JsonException("Phase 35A contract JSON was null.");
    internal string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(Serialize(value))).ToLowerInvariant();
    internal static bool IsHash(string? value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}

