using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed record PbirDeployableLayoutSlot(
    int Slot,
    int X,
    int Y,
    int Width,
    int Height,
    int Z,
    int TabOrder);

internal sealed class PbirDeployableSerializerCanonicalJson
{
    private static readonly IReadOnlyList<PbirDeployableLayoutSlot> LayoutSlots =
    [
        new(1, 24, 24, 400, 328, 0, 0),
        new(2, 440, 24, 400, 328, 1000, 1000),
        new(3, 856, 24, 400, 328, 2000, 2000),
        new(4, 24, 368, 400, 328, 3000, 3000),
        new(5, 440, 368, 400, 328, 4000, 4000),
        new(6, 856, 368, 400, 328, 5000, 5000),
    ];

    internal byte[] SerializeSemanticModelInventory(PbirSemanticModelInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        using var stream = new MemoryStream();
        using (var writer = CreateWriter(stream, indented: false))
        {
            writer.WriteStartObject();
            writer.WriteString("schemaVersion", inventory.SchemaVersion);
            writer.WriteString("inventoryRef", inventory.InventoryRef);
            writer.WritePropertyName("entries");
            writer.WriteStartArray();
            foreach (var entry in inventory.Entries
                         .OrderBy(value => value.EntryId, StringComparer.Ordinal)
                         .ThenBy(value => value.Token, StringComparer.Ordinal)
                         .ThenBy(value => value.Entity, StringComparer.Ordinal)
                         .ThenBy(value => value.Property, StringComparer.Ordinal)
                         .ThenBy(
                             value => value.Kind == PbirSemanticModelEntryKind.Column ? "column" : "measure",
                             StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("entryId", entry.EntryId);
                writer.WriteString("token", entry.Token);
                writer.WriteString("entity", entry.Entity);
                writer.WriteString("property", entry.Property);
                writer.WriteString(
                    "kind",
                    entry.Kind == PbirSemanticModelEntryKind.Column ? "column" : "measure");
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    internal string SerializeDocument(Action<Utf8JsonWriter> writeDocument)
    {
        ArgumentNullException.ThrowIfNull(writeDocument);

        using var stream = new MemoryStream();
        using (var writer = CreateWriter(stream, indented: true))
        {
            writeDocument(writer);
        }

        return $"{Encoding.UTF8.GetString(stream.ToArray())}\n";
    }

    internal string CreatePageIdentity(string irId, string pageIdentity)
    {
        return CreateIdentity($"page|{irId}|{pageIdentity}");
    }

    internal string CreateVisualIdentity(string irId, string pageIdentity, string visualId)
    {
        return CreateIdentity($"visual|{irId}|{pageIdentity}|{visualId}");
    }

    internal PbirDeployableLayoutSlot GetLayoutSlot(int slot)
    {
        if (slot < 1 || slot > LayoutSlots.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "Layout slot must be between 1 and 6.");
        }

        return LayoutSlots[slot - 1];
    }

    internal string ComputeSha256(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return ComputeSha256(Encoding.UTF8.GetBytes(content));
    }

    internal string ComputeSha256(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    internal string ComputeArtifactHash(
        string schemaVersion,
        string artifactId,
        string targetFormat,
        IReadOnlyList<PbirDeployableGeneratedFile> files,
        PbirDeployableLineage lineage,
        string inputHash,
        string fileSetHash,
        string lineageHash)
    {
        return ComputeSha256(JsonSerializer.Serialize(new
        {
            schemaVersion,
            artifactId,
            targetFormat,
            files,
            lineage,
            hashes = new
            {
                schemaVersion = PbirDeployableHashesContract.SchemaVersionV1,
                inputHash,
                fileSetHash,
                lineageHash
            }
        }));
    }

    internal string ComputeManifestHash(
        string schemaVersion,
        string manifestId,
        string artifactRef,
        IReadOnlyList<string> schemaLock,
        IReadOnlyList<PbirDeployableGeneratedFileReference> files,
        IReadOnlyList<string> supportedFeatures,
        IReadOnlyList<PbirDeployableDiagnostic> warnings,
        IReadOnlyList<PbirDeployableDiagnostic> unsupportedSections,
        PbirDeployableLineage lineage,
        string artifactHash,
        string inputHash,
        string fileSetHash,
        string lineageHash)
    {
        return ComputeSha256(JsonSerializer.Serialize(new
        {
            schemaVersion,
            manifestId,
            artifactRef,
            schemaLock,
            files,
            supportedFeatures,
            warnings,
            unsupportedSections,
            lineage,
            hashes = new
            {
                schemaVersion = PbirDeployableHashesContract.SchemaVersionV1,
                inputHash,
                fileSetHash,
                artifactHash,
                lineageHash
            }
        }));
    }

    private static Utf8JsonWriter CreateWriter(Stream stream, bool indented)
    {
        return new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = indented,
            SkipValidation = false
        });
    }

    private string CreateIdentity(string value)
    {
        return ComputeSha256(value)[..20];
    }
}
