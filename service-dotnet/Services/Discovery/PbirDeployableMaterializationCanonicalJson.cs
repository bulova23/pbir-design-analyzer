using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableMaterializationCanonicalJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    internal byte[] SerializeTargetInventory(PbirDeployableTargetInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var ordered = inventory.Files.OrderBy(file => file.RelativePath, StringComparer.Ordinal).ToArray();
        if (ordered.Select(file => file.RelativePath).Distinct(StringComparer.Ordinal).Count() != ordered.Length ||
            ordered.Any(file => !IsHash(file.HashSha256)))
        {
            throw new InvalidDataException("Target inventory paths and hashes must be canonical and unique.");
        }

        return Serialize(new PbirDeployableTargetInventory(inventory.SchemaVersion, inventory.TargetState, ordered));
    }

    internal byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

    internal T Deserialize<T>(byte[] bytes) where T : class =>
        JsonSerializer.Deserialize<T>(bytes, Options) ?? throw new InvalidDataException("Canonical materialization JSON is null.");

    internal string ComputeSha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    internal string ComputeSha256(string value) => ComputeSha256(Encoding.UTF8.GetBytes(value));
    internal string Hash<T>(T value) => ComputeSha256(Serialize(value));

    internal static bool IsHash(string? value) =>
        value is { Length: 64 } && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}
