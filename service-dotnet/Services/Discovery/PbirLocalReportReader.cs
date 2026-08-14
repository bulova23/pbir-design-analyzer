using System.Security.Cryptography;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirLocalReportReader
{
    private static readonly HashSet<string> SupportedVisualTypes = ["card", "table", "clusteredColumnChart", "lineChart", "barChart", "pieChart", "slicer"];

    internal PbirLocalReportImportSnapshot Import(string sourceDirectory)
    {
        var diagnostics = new List<LocalPbirMutationDiagnostic>();
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
        {
            diagnostics.Add(new("PBIR42-IMPORT-001", "sourceDirectory", "Source report directory does not exist."));
            return Empty(sourceDirectory, files, diagnostics);
        }
        var definition = Path.Combine(sourceDirectory, "definition");
        foreach (var filePath in Directory.Exists(definition) ? Directory.GetFiles(definition, "*.json", SearchOption.AllDirectories) : [])
        {
            var relative = filePath[(definition.Length + 1)..];
            files[relative] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(filePath))).ToLowerInvariant();
        }
        var pagesMetadataPath = Path.Combine(definition, "pages", "pages.json");
        if (!File.Exists(pagesMetadataPath)) diagnostics.Add(new("PBIR42-IMPORT-002", "definition/pages/pages.json", "Pages metadata is required."));
        if (diagnostics.Count > 0) return Empty(sourceDirectory, files, diagnostics);
        var pageOrder = ReadJson(pagesMetadataPath, files, diagnostics, "pagesMetadata");
        if (pageOrder is null) return Empty(sourceDirectory, files, diagnostics);
        var pages = new List<PbirIntermediateRepresentationPage>();
        var visuals = new List<PbirIntermediateRepresentationVisual>();
        var semantics = new List<PbirIntermediateRepresentationSemantic>();
        var pageIdentities = new Dictionary<string, string>(StringComparer.Ordinal);
        var visualIdentities = new Dictionary<string, string>(StringComparer.Ordinal);
        var order = 0;
        foreach (var pageIdentity in pageOrder.RootElement.GetProperty("pageOrder").EnumerateArray().Select(x => x.GetString() ?? string.Empty))
        {
            var pagePath = Path.Combine(definition, "pages", pageIdentity, "page.json");
            var page = ReadJson(pagePath, files, diagnostics, $"pages/{pageIdentity}/page.json");
            if (page is null) continue;
            if (!HasSchema(page.RootElement, PbirDeployableSchemaLock.PageSchemaUrl)) { diagnostics.Add(new("PBIR42-IMPORT-003", pagePath, "Unsupported page schema.")); continue; }
            var pageId = pageIdentity;
            var displayName = page.RootElement.TryGetProperty("displayName", out var name) ? name.GetString() ?? pageId : pageId;
            pageIdentities[pageId] = pageIdentity;
            pages.Add(new(pageId, pageIdentity, "", "", order++, displayName));
            var visualDirectory = Path.Combine(definition, "pages", pageIdentity, "visuals");
            if (!Directory.Exists(visualDirectory)) continue;
            var visualOrder = 0;
            foreach (var visualPath in Directory.GetFiles(visualDirectory, "visual.json", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
            {
                var visualIdentity = new DirectoryInfo(Path.GetDirectoryName(visualPath)!).Name;
                var visual = ReadJson(visualPath, files, diagnostics, visualPath[(definition.Length + 1)..]);
                if (visual is null) continue;
                if (!HasSchema(visual.RootElement, PbirDeployableSchemaLock.VisualContainerSchemaUrl)) { diagnostics.Add(new("PBIR42-IMPORT-004", visualPath, "Unsupported visual schema.")); continue; }
                var visualNode = visual.RootElement.GetProperty("visual");
                var visualType = visualNode.GetProperty("visualType").GetString() ?? string.Empty;
                if (!SupportedVisualTypes.Contains(visualType)) { diagnostics.Add(new("PBIR42-IMPORT-005", visualPath, "Visual type is outside the closed Phase 42 catalog.")); continue; }
                var layoutNode = visual.RootElement.GetProperty("position");
                var layout = new PbirIntermediateRepresentationVisualLayout(layoutNode.GetProperty("x").GetInt32(), layoutNode.GetProperty("y").GetInt32(), layoutNode.GetProperty("width").GetInt32(), layoutNode.GetProperty("height").GetInt32());
                var bindings = ReadBindings(visualNode, visualIdentity, diagnostics);
                var semanticIntent = visualIdentity;
                visuals.Add(new(visualIdentity, pageId, visualType, $"page:{pageId}/slot:{visualOrder + 1}", semanticIntent, ["none"], visualOrder++, layout, bindings));
                visualIdentities[visualIdentity] = visualIdentity;
                semantics.Add(new($"semantic:{visualIdentity}", pageId, bindings.Where(x => x.Kind == PbirIntermediateRepresentationBindingKind.Measure).Select(x => x.Token).ToArray(), bindings.Where(x => x.Kind == PbirIntermediateRepresentationBindingKind.Dimension).Select(x => x.Token).ToArray(), [semanticIntent], [], "none", [$"visual:[{visualIdentity}]->semantic:[{semanticIntent}]"]));
            }
        }
        var ir = new PbirIntermediateRepresentation(
            new($"import:{Path.GetFileName(Path.TrimEndingDirectorySeparator(sourceDirectory))}", PbirIntermediateRepresentationContract.SchemaVersionV1, DateTime.UnixEpoch),
            new("import", "import"), pages, visuals, semantics,
            new(pages.FirstOrDefault()?.PageId ?? string.Empty, [], [], []), new([], [], [], []), new([], [], []), new([], []), new(string.Empty, string.Empty, string.Empty));
        var envelope = new PbirAuthoringEnvelopeReader().Read(sourceDirectory, files, diagnostics);
        ir = ir with { AuthoringEnvelope = envelope };
        var contentHash = PbirIntermediateRepresentationIntegrity.ComputeContentHash(ir);
        ir = ir with { Hashes = new PbirIntermediateRepresentationHashes("import", contentHash, string.Empty) };
        var state = new PbirIntermediateRepresentationState(ir, new PbirIntermediateRepresentationValidationResult(PbirIntermediateRepresentationValidationDiagnostics.Empty), PbirIntermediateRepresentationReadinessState.ReadyForSerializer);
        return new(PbirLocalReportImportContract.SchemaVersionV1, sourceDirectory, state, pageIdentities, visualIdentities, files, diagnostics);
    }

    private static IReadOnlyList<PbirIntermediateRepresentationBinding> ReadBindings(JsonElement visual, string visualId, List<LocalPbirMutationDiagnostic> diagnostics)
    {
        var result = new List<PbirIntermediateRepresentationBinding>();
        if (!visual.TryGetProperty("query", out var query) || !query.TryGetProperty("queryState", out var state)) return result;
        foreach (var role in state.EnumerateObject())
        {
            if (!role.Value.TryGetProperty("projections", out var projections)) continue;
            foreach (var projection in projections.EnumerateArray())
            {
                if (!projection.TryGetProperty("field", out var field)) { diagnostics.Add(new("PBIR42-IMPORT-006", visualId, "Projection field is missing.")); continue; }
                var kindNode = field.TryGetProperty("Measure", out var measure) ? measure : field.GetProperty("Column");
                var kind = field.TryGetProperty("Measure", out _) ? PbirIntermediateRepresentationBindingKind.Measure : PbirIntermediateRepresentationBindingKind.Dimension;
                var expression = kindNode.GetProperty("Expression");
                var entity = expression.GetProperty("SourceRef").GetProperty("Entity").GetString() ?? string.Empty;
                var property = kindNode.GetProperty("Property").GetString() ?? string.Empty;
                var token = projection.TryGetProperty("nativeQueryRef", out var nativeQueryRef)
                    ? nativeQueryRef.GetString() ?? property
                    : projection.TryGetProperty("queryRef", out var queryRef)
                        ? queryRef.GetString() ?? $"{entity}.{property}"
                        : $"{entity}.{property}";
                result.Add(new($"{visualId}:{result.Count}", Enum.TryParse<PbirIntermediateRepresentationBindingRole>(role.Name, true, out var parsed) ? parsed : PbirIntermediateRepresentationBindingRole.Value, kind, token, entity, property, result.Count));
            }
        }
        return result;
    }

    private static JsonDocument? ReadJson(string path, Dictionary<string, string> hashes, List<LocalPbirMutationDiagnostic> diagnostics, string field)
    {
        if (!File.Exists(path)) { diagnostics.Add(new("PBIR42-IMPORT-007", field, "Required PBIR file is missing.")); return null; }
        try { var bytes = File.ReadAllBytes(path); hashes[field] = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(); return JsonDocument.Parse(bytes); }
        catch (JsonException) { diagnostics.Add(new("PBIR42-IMPORT-008", field, "PBIR file is not valid JSON.")); return null; }
    }

    private static bool HasSchema(JsonElement element, string expected) => element.TryGetProperty("$schema", out var schema) && schema.GetString() == expected;
    private static PbirLocalReportImportSnapshot Empty(string source, IReadOnlyDictionary<string, string> files, IReadOnlyList<LocalPbirMutationDiagnostic> diagnostics) => new(PbirLocalReportImportContract.SchemaVersionV1, source, new(null, new(PbirIntermediateRepresentationValidationDiagnostics.Empty), PbirIntermediateRepresentationReadinessState.Blocked), new Dictionary<string, string>(), new Dictionary<string, string>(), files, diagnostics);
}
