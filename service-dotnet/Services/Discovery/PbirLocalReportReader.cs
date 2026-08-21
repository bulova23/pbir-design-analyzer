using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirLocalReportReader
{
    private static readonly HashSet<string> SupportedVisualTypes = ["card", "table", "clusteredColumnChart", "lineChart", "barChart", "pieChart", "slicer"];

    internal PbirLocalReportImportSnapshot Import(string sourceDirectory)
    {
        var readerTimer = Stopwatch.StartNew();
        var projectionTimer = new Stopwatch();
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
            var relative = NormalizeRelativePath(filePath[(definition.Length + 1)..]);
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
                var visual = ReadJson(visualPath, files, diagnostics, NormalizeRelativePath(visualPath[(definition.Length + 1)..]));
                if (visual is null) continue;
                if (!HasSchema(visual.RootElement, PbirDeployableSchemaLock.VisualContainerSchemaUrl)) { diagnostics.Add(new("PBIR42-IMPORT-004", visualPath, "Unsupported visual schema.")); continue; }
                var visualNode = visual.RootElement.GetProperty("visual");
                var visualType = visualNode.GetProperty("visualType").GetString() ?? string.Empty;
                if (!SupportedVisualTypes.Contains(visualType)) { diagnostics.Add(new("PBIR42-IMPORT-005", visualPath, "Visual type is outside the closed Phase 42 catalog.")); continue; }
                var layoutNode = visual.RootElement.GetProperty("position");
                var layout = new PbirIntermediateRepresentationVisualLayout(layoutNode.GetProperty("x").GetInt32(), layoutNode.GetProperty("y").GetInt32(), layoutNode.GetProperty("width").GetInt32(), layoutNode.GetProperty("height").GetInt32());
                projectionTimer.Start();
                var bindings = ReadBindings(visualType, visualNode, visualIdentity, diagnostics);
                projectionTimer.Stop();
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
        var readiness = diagnostics.Any(diagnostic => diagnostic.ProjectionStatus == LocalPbirSemanticProjectionStatus.Invalid)
            ? PbirIntermediateRepresentationReadinessState.Blocked
            : PbirIntermediateRepresentationReadinessState.ReadyForSerializer;
        var state = new PbirIntermediateRepresentationState(ir, new PbirIntermediateRepresentationValidationResult(PbirIntermediateRepresentationValidationDiagnostics.Empty), readiness);
        readerTimer.Stop();
        return new(PbirLocalReportImportContract.SchemaVersionV1, sourceDirectory, state, pageIdentities, visualIdentities, files, diagnostics,
            new PbirLocalReportImportPerformance(readerTimer.ElapsedMilliseconds, projectionTimer.ElapsedMilliseconds));
    }

    private static IReadOnlyList<PbirIntermediateRepresentationBinding> ReadBindings(string visualType, JsonElement visual, string visualId, List<LocalPbirMutationDiagnostic> diagnostics)
    {
        var result = new List<PbirIntermediateRepresentationBinding>();
        if (!visual.TryGetProperty("query", out var query) || !query.TryGetProperty("queryState", out var state)) return result;
        foreach (var role in state.EnumerateObject())
        {
            if (!role.Value.TryGetProperty("projections", out var projections)) continue;
            var roleMappings = Phase40VisualDescriptorCatalog.ResolveImportedRoles(visualType, role.Name);
            if (roleMappings.Count == 0)
            {
                diagnostics.Add(new LocalPbirMutationDiagnostic("PBIR44-IMPORT-ROLE-001", $"{visualId}.query.queryState.{role.Name}", $"Query-state role '{role.Name}' is not represented by the {visualType} descriptor; its schema-supported source is preserved but remains untyped.") with { ProjectionStatus = LocalPbirSemanticProjectionStatus.PreservedButUntyped });
                continue;
            }

            if (roleMappings.Count > 1)
            {
                diagnostics.Add(new LocalPbirMutationDiagnostic("PBIR44-IMPORT-ROLE-003", $"{visualId}.query.queryState.{role.Name}", $"Query-state role '{role.Name}' maps ambiguously through the {visualType} descriptor.") with { ProjectionStatus = LocalPbirSemanticProjectionStatus.Invalid });
                continue;
            }

            var mapping = roleMappings[0];
            foreach (var projection in projections.EnumerateArray())
            {
                if (!projection.TryGetProperty("field", out var field))
                {
                    diagnostics.Add(new LocalPbirMutationDiagnostic("PBIR44-IMPORT-BINDING-001", visualId, "Projection field is missing.") with { ProjectionStatus = LocalPbirSemanticProjectionStatus.Invalid });
                    continue;
                }

                var hasMeasure = field.TryGetProperty("Measure", out var measure);
                var hasColumn = field.TryGetProperty("Column", out var column);
                if (hasMeasure == hasColumn)
                {
                    diagnostics.Add(new LocalPbirMutationDiagnostic("PBIR44-IMPORT-BINDING-001", visualId, "Projection field must contain exactly one Measure or Column expression.") with { ProjectionStatus = LocalPbirSemanticProjectionStatus.Invalid });
                    continue;
                }

                var kindNode = hasMeasure ? measure : column;
                var kind = hasMeasure ? PbirIntermediateRepresentationBindingKind.Measure : PbirIntermediateRepresentationBindingKind.Dimension;
                if (mapping.Kind is not null && (mapping.Kind == LocalPbirGenerationBindingKind.Measure) != hasMeasure)
                {
                    diagnostics.Add(new LocalPbirMutationDiagnostic("PBIR44-IMPORT-BINDING-002", $"{visualId}.query.queryState.{role.Name}", $"Imported role '{role.Name}' has field kind '{kind}' but the descriptor requires '{mapping.Kind}'.") with { ProjectionStatus = LocalPbirSemanticProjectionStatus.Invalid });
                    continue;
                }

                if (!kindNode.TryGetProperty("Expression", out var expression) ||
                    !expression.TryGetProperty("SourceRef", out var sourceRef) ||
                    !sourceRef.TryGetProperty("Entity", out var entityNode) ||
                    !kindNode.TryGetProperty("Property", out var propertyNode))
                {
                    diagnostics.Add(new LocalPbirMutationDiagnostic("PBIR44-IMPORT-BINDING-001", visualId, "Projection field expression must contain SourceRef.Entity and Property.") with { ProjectionStatus = LocalPbirSemanticProjectionStatus.Invalid });
                    continue;
                }

                var entity = entityNode.GetString() ?? string.Empty;
                var property = propertyNode.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(property))
                {
                    diagnostics.Add(new LocalPbirMutationDiagnostic("PBIR44-IMPORT-BINDING-001", visualId, "Projection field entity and property must not be empty.") with { ProjectionStatus = LocalPbirSemanticProjectionStatus.Invalid });
                    continue;
                }
                var token = projection.TryGetProperty("nativeQueryRef", out var nativeQueryRef)
                    ? nativeQueryRef.GetString() ?? property
                    : projection.TryGetProperty("queryRef", out var queryRef)
                        ? queryRef.GetString() ?? $"{entity}.{property}"
                        : $"{entity}.{property}";
                result.Add(new($"{visualId}:{result.Count}", Enum.Parse<PbirIntermediateRepresentationBindingRole>(mapping.BindingRole.ToString()), kind, token, entity, property, result.Count));
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

    private static bool HasSchema(JsonElement element, string expected) =>
        element.TryGetProperty("$schema", out var schema) &&
        schema.ValueKind == JsonValueKind.String &&
        schema.GetString() == expected;

    private static string NormalizeRelativePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');

    private static PbirLocalReportImportSnapshot Empty(string source, IReadOnlyDictionary<string, string> files, IReadOnlyList<LocalPbirMutationDiagnostic> diagnostics) => new(PbirLocalReportImportContract.SchemaVersionV1, source, new(null, new(PbirIntermediateRepresentationValidationDiagnostics.Empty), PbirIntermediateRepresentationReadinessState.Blocked), new Dictionary<string, string>(), new Dictionary<string, string>(), files, diagnostics);
}
