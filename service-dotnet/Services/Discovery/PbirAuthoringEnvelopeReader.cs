using System.Security.Cryptography;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirAuthoringEnvelopeReader
{
    internal PbirAuthoringEnvelope Read(
        string sourceDirectory,
        IReadOnlyDictionary<string, string> fileHashes,
        List<LocalPbirMutationDiagnostic> diagnostics)
    {
        var items = new List<PbirAuthoringEnvelopeItem>();
        foreach (var relativePath in fileHashes.Keys.OrderBy(path => path, StringComparer.Ordinal))
        {
            if (!TryGetOwner(relativePath, out var ownerKind, out var ownerId, out var importedIdentity))
            {
                continue;
            }

            var path = Path.Combine(sourceDirectory, "definition", relativePath);
            try
            {
                var sourceContent = File.ReadAllText(path);
                using var document = JsonDocument.Parse(sourceContent);
                var schemaUrl = document.RootElement.TryGetProperty("$schema", out var schema)
                    ? schema.GetString()
                    : null;
                if (!IsSupportedSchema(ownerKind, schemaUrl))
                {
                    diagnostics.Add(new("PBIR43-IMPORT-001", relativePath, "The document schema is outside the pinned authoring envelope."));
                    continue;
                }

                items.Add(new(
                    ownerKind,
                    ownerId,
                    relativePath,
                    schemaUrl!,
                    SchemaVersion(ownerKind),
                    ownerKind is PbirAuthoringOwnerKind.Page or PbirAuthoringOwnerKind.Visual
                        ? PbirAuthoringPreservationClass.TypedSupported
                    : PbirAuthoringPreservationClass.OpaquePreserved,
                    document.RootElement.Clone(),
                    sourceContent,
                    fileHashes[relativePath],
                    document.RootElement.EnumerateObject().Select(property => property.Name).ToArray(),
                    importedIdentity is null ? null : new(importedIdentity, null, null)));
            }
            catch (JsonException)
            {
                diagnostics.Add(new("PBIR43-IMPORT-002", relativePath, "The authoring envelope document is not valid JSON."));
            }
        }

        var definitionHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            string.Join("\n", items.Select(item => $"{item.OwnedRelativePath}:{item.SourceHash}"))))).ToLowerInvariant();
        return new(PbirAuthoringEnvelopeContract.SchemaVersionV1, items, definitionHash);
    }

    private static bool TryGetOwner(
        string relativePath,
        out PbirAuthoringOwnerKind ownerKind,
        out string ownerId,
        out string? importedIdentity)
    {
        ownerKind = default;
        ownerId = string.Empty;
        importedIdentity = null;
        var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (relativePath == "report.json")
        {
            ownerKind = PbirAuthoringOwnerKind.Report;
            ownerId = "report";
            return true;
        }
        if (relativePath == "pages/pages.json")
        {
            ownerKind = PbirAuthoringOwnerKind.PagesMetadata;
            ownerId = "pages";
            return true;
        }
        if (parts.Length == 3 && parts[0] == "pages" && parts[2] == "page.json")
        {
            ownerKind = PbirAuthoringOwnerKind.Page;
            ownerId = parts[1];
            importedIdentity = parts[1];
            return true;
        }
        if (parts.Length == 5 && parts[0] == "pages" && parts[2] == "visuals" && parts[4] == "visual.json")
        {
            ownerKind = PbirAuthoringOwnerKind.Visual;
            ownerId = parts[3];
            importedIdentity = parts[3];
            return true;
        }
        return false;
    }

    private static bool IsSupportedSchema(PbirAuthoringOwnerKind ownerKind, string? schemaUrl) =>
        ownerKind switch
        {
            PbirAuthoringOwnerKind.Report => schemaUrl == PbirDeployableSchemaLock.ReportSchemaUrl,
            PbirAuthoringOwnerKind.PagesMetadata => schemaUrl == PbirDeployableSchemaLock.PagesMetadataSchemaUrl,
            PbirAuthoringOwnerKind.Page => schemaUrl == PbirDeployableSchemaLock.PageSchemaUrl,
            PbirAuthoringOwnerKind.Visual => schemaUrl == PbirDeployableSchemaLock.VisualContainerSchemaUrl,
            _ => false
        };

    private static string SchemaVersion(PbirAuthoringOwnerKind ownerKind) =>
        ownerKind switch
        {
            PbirAuthoringOwnerKind.Report => PbirDeployableSchemaLock.DefinitionSchemaVersion,
            PbirAuthoringOwnerKind.PagesMetadata => PbirDeployableSchemaLock.DefinitionSchemaVersion,
            PbirAuthoringOwnerKind.Page => PbirDeployableSchemaLock.DefinitionSchemaVersion,
            PbirAuthoringOwnerKind.Visual => PbirDeployableSchemaLock.DefinitionSchemaVersion,
            _ => PbirDeployableSchemaLock.DefinitionSchemaVersion
        };
}
