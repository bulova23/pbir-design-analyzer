using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableSerializerValidator
{
    private static readonly HashSet<string> SupportedVisualTypes =
        new(StringComparer.Ordinal)
        {
            "card",
            "table",
            "clusteredColumnChart",
            "lineChart"
        };

    internal PbirDeployableDiagnostics ValidateInput(
        PbirIntermediateRepresentation ir,
        PbirDeployableSerializerRequest request)
    {
        ArgumentNullException.ThrowIfNull(ir);
        ArgumentNullException.ThrowIfNull(request);

        var unsupportedVisualTypes = new List<PbirDeployableDiagnostic>();
        var incompleteSemanticBindings = new List<PbirDeployableDiagnostic>();
        var invalidModelReferences = new List<PbirDeployableDiagnostic>();
        var duplicateIdentities = new List<PbirDeployableDiagnostic>();
        var invalidLayoutDefinitions = new List<PbirDeployableDiagnostic>();
        var invalidNavigationDefinitions = new List<PbirDeployableDiagnostic>();

        ValidatePages(ir, duplicateIdentities, invalidNavigationDefinitions);
        ValidateVisuals(
            ir,
            request,
            unsupportedVisualTypes,
            incompleteSemanticBindings,
            invalidModelReferences,
            duplicateIdentities,
            invalidLayoutDefinitions);

        return new PbirDeployableDiagnostics(
            SchemaVersion: PbirDeployableDiagnosticsContract.SchemaVersionV1,
            MissingRequiredFields: [],
            UnsupportedSchemaVersions: [],
            UnsupportedVisualTypes: Order(unsupportedVisualTypes),
            IncompleteSemanticBindings: Order(incompleteSemanticBindings),
            InvalidModelReferences: Order(invalidModelReferences),
            InvalidPaths: [],
            DuplicateIdentities: Order(duplicateIdentities),
            InvalidLayoutDefinitions: Order(invalidLayoutDefinitions),
            InvalidNavigationDefinitions: Order(invalidNavigationDefinitions),
            SchemaIncompatibilities: [],
            HashViolations: [],
            LineageViolations: [],
            BoundaryViolations: [],
            Warnings: [],
            UnsupportedSections: ir.Pages
                .OrderBy(page => page.Order)
                .Select(page => Diagnostic(
                    "PBIRDEPLOY-UNSUPPORTED-001",
                    $"pages.{page.PageId}.intendedPurpose",
                    "IntendedPurpose is preserved as diagnostic context and is not emitted."))
                .ToArray());
    }

    internal PbirDeployableValidation ValidateOutput(
        PbirDeployableArtifact artifact,
        PbirDeployableManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(manifest);

        var schemaResults = new List<PbirDeployableDiagnostic>();
        var structuralResults = new List<PbirDeployableDiagnostic>();
        var crossReferenceResults = new List<PbirDeployableDiagnostic>();
        var hashResults = new List<PbirDeployableDiagnostic>();
        var expectedSchemas = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["definition.pbir"] = PbirDeployableSchemaLock.DefinitionPropertiesSchemaUrl,
            ["definition/version.json"] = PbirDeployableSchemaLock.VersionMetadataSchemaUrl,
            ["definition/report.json"] = PbirDeployableSchemaLock.ReportSchemaUrl,
            ["definition/pages/pages.json"] = PbirDeployableSchemaLock.PagesMetadataSchemaUrl,
        };

        if (artifact.Files.Count == 0 ||
            artifact.Files.Select(file => file.RelativePath).Distinct(StringComparer.Ordinal).Count() != artifact.Files.Count ||
            !artifact.Files.Select(file => file.RelativePath)
                .SequenceEqual(
                    artifact.Files.Select(file => file.RelativePath).OrderBy(value => value, StringComparer.Ordinal),
                    StringComparer.Ordinal) ||
            artifact.Files.Any(file => file.RelativePath == "report.json"))
        {
            structuralResults.Add(Diagnostic(
                "PBIRDEPLOY-STRUCTURE-001",
                "artifact.files",
                "Artifact inventory must be nonempty, path-unique, and contain no root report.json."));
        }

        foreach (var file in artifact.Files)
        {
            try
            {
                using var document = JsonDocument.Parse(file.Content);
                if (!document.RootElement.TryGetProperty("$schema", out var schemaElement) ||
                    schemaElement.GetString() != file.SchemaUrl)
                {
                    schemaResults.Add(Diagnostic(
                        "PBIRDEPLOY-SCHEMA-OUTPUT-001",
                        file.RelativePath,
                        "Emitted document schema must match its locked inventory schema."));
                }

                ValidateSupportedTemplate(file, document.RootElement, schemaResults);
            }
            catch (JsonException)
            {
                structuralResults.Add(Diagnostic(
                    "PBIRDEPLOY-JSON-001",
                    file.RelativePath,
                    "Emitted document must be valid JSON."));
            }
            catch (Exception exception) when (
                exception is InvalidOperationException or KeyNotFoundException)
            {
                schemaResults.Add(Diagnostic(
                    "PBIRDEPLOY-SCHEMA-OUTPUT-003",
                    file.RelativePath,
                    "Document properties and types must match the exact supported Phase 29 template."));
            }

            if (Encoding.UTF8.GetByteCount(file.Content) != file.ByteLength ||
                !string.Equals(
                    ComputeSha256(file.Content),
                    file.HashSha256,
                    StringComparison.Ordinal))
            {
                hashResults.Add(Diagnostic(
                    "PBIRDEPLOY-HASH-OUTPUT-001",
                    file.RelativePath,
                    "Emitted file byte length and hash must match exact UTF-8 content."));
            }

            var expectedSchema = GetExpectedSchema(file.RelativePath);
            var expectedSchemaVersion = file.RelativePath == "definition.pbir"
                ? PbirDeployableSchemaLock.DefinitionPropertiesSchemaVersion
                : PbirDeployableSchemaLock.DefinitionSchemaVersion;
            if (expectedSchema is null ||
                file.SchemaUrl != expectedSchema ||
                file.SchemaVersion != expectedSchemaVersion)
            {
                schemaResults.Add(Diagnostic(
                    "PBIRDEPLOY-SCHEMA-OUTPUT-002",
                    file.RelativePath,
                    "Required document uses an incompatible schema."));
            }
        }

        ValidateDocumentCrossReferences(artifact.Files, crossReferenceResults, structuralResults);

        foreach (var requiredPath in expectedSchemas.Keys)
        {
            if (!artifact.Files.Any(file => file.RelativePath == requiredPath))
            {
                structuralResults.Add(Diagnostic(
                    "PBIRDEPLOY-STRUCTURE-002",
                    requiredPath,
                    "Required modern PBIR document is missing."));
            }
        }

        var expectedManifestFiles = artifact.Files
            .Select(file => new PbirDeployableGeneratedFileReference(
                file.RelativePath,
                file.ContentType,
                file.ByteLength,
                file.HashSha256,
                file.SchemaUrl,
                file.SchemaVersion))
            .ToArray();
        if (!expectedManifestFiles.SequenceEqual(manifest.Files))
        {
            crossReferenceResults.Add(Diagnostic(
                "PBIRDEPLOY-XREF-001",
                "manifest.files",
                "Manifest file inventory must match the artifact inventory exactly."));
        }

        ValidateAggregateHashes(artifact, manifest, hashResults);

        var isValid =
            schemaResults.Count == 0 &&
            structuralResults.Count == 0 &&
            crossReferenceResults.Count == 0 &&
            hashResults.Count == 0;

        return new PbirDeployableValidation(
            SchemaVersion: PbirDeployableValidationContract.SchemaVersionV1,
            IsValid: isValid,
            ValidatedFileCount: artifact.Files.Count,
            SchemaContractResults: Order(schemaResults),
            StructuralValidationResults: Order(structuralResults),
            CrossReferenceValidationResults: Order(crossReferenceResults),
            HashValidationResults: Order(hashResults));
    }

    private static string? GetExpectedSchema(string relativePath)
    {
        if (relativePath == "definition.pbir")
        {
            return PbirDeployableSchemaLock.DefinitionPropertiesSchemaUrl;
        }

        if (relativePath == "definition/version.json")
        {
            return PbirDeployableSchemaLock.VersionMetadataSchemaUrl;
        }

        if (relativePath == "definition/report.json")
        {
            return PbirDeployableSchemaLock.ReportSchemaUrl;
        }

        if (relativePath == "definition/pages/pages.json")
        {
            return PbirDeployableSchemaLock.PagesMetadataSchemaUrl;
        }

        if (relativePath.StartsWith("definition/pages/", StringComparison.Ordinal) &&
            relativePath.EndsWith("/page.json", StringComparison.Ordinal))
        {
            return PbirDeployableSchemaLock.PageSchemaUrl;
        }

        if (relativePath.StartsWith("definition/pages/", StringComparison.Ordinal) &&
            relativePath.EndsWith("/visual.json", StringComparison.Ordinal))
        {
            return PbirDeployableSchemaLock.VisualContainerSchemaUrl;
        }

        return null;
    }

    private static void ValidateSupportedTemplate(
        PbirDeployableGeneratedFile file,
        JsonElement root,
        List<PbirDeployableDiagnostic> schemaResults)
    {
        var valid = file.RelativePath switch
        {
            "definition.pbir" =>
                HasExactProperties(root, "$schema", "version", "datasetReference") &&
                root.GetProperty("$schema").ValueKind == JsonValueKind.String &&
                root.GetProperty("version").ValueKind == JsonValueKind.String &&
                HasExactProperties(root.GetProperty("datasetReference"), "byPath") &&
                HasExactProperties(root.GetProperty("datasetReference").GetProperty("byPath"), "path") &&
                root.GetProperty("datasetReference").GetProperty("byPath").GetProperty("path").ValueKind ==
                JsonValueKind.String,
            "definition/version.json" =>
                HasExactProperties(root, "$schema", "version") &&
                root.GetProperty("version").ValueKind == JsonValueKind.String,
            "definition/report.json" =>
                HasExactProperties(root, "$schema", "layoutOptimization", "themeCollection") &&
                root.GetProperty("layoutOptimization").ValueKind == JsonValueKind.String &&
                root.GetProperty("themeCollection").ValueKind == JsonValueKind.Object &&
                !root.GetProperty("themeCollection").EnumerateObject().Any(),
            "definition/pages/pages.json" =>
                HasExactProperties(root, "$schema", "pageOrder", "activePageName") &&
                root.GetProperty("pageOrder").ValueKind == JsonValueKind.Array &&
                root.GetProperty("pageOrder").EnumerateArray()
                    .All(value => value.ValueKind == JsonValueKind.String) &&
                root.GetProperty("activePageName").ValueKind == JsonValueKind.String,
            _ when file.RelativePath.EndsWith("/page.json", StringComparison.Ordinal) =>
                HasExactProperties(root, "$schema", "name", "displayName", "displayOption", "height", "width") &&
                root.GetProperty("name").ValueKind == JsonValueKind.String &&
                root.GetProperty("displayName").ValueKind == JsonValueKind.String &&
                root.GetProperty("displayOption").GetString() == "FitToPage" &&
                root.GetProperty("height").TryGetInt32(out var height) && height == 720 &&
                root.GetProperty("width").TryGetInt32(out var width) && width == 1280,
            _ when file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal) =>
                HasExactProperties(root, "$schema", "name", "position", "visual") &&
                HasExactProperties(root.GetProperty("position"), "x", "y", "z", "height", "width", "tabOrder") &&
                HasExactProperties(root.GetProperty("visual"), "visualType", "query") &&
                HasExactProperties(root.GetProperty("visual").GetProperty("query"), "queryState"),
            _ => false
        };

        if (!valid)
        {
            schemaResults.Add(Diagnostic(
                "PBIRDEPLOY-SCHEMA-OUTPUT-003",
                file.RelativePath,
                "Document properties and types must match the exact supported Phase 29 template."));
        }
    }

    private static bool HasExactProperties(JsonElement element, params string[] expected)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return element.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(value => value, StringComparer.Ordinal)
            .SequenceEqual(expected.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static void ValidateAggregateHashes(
        PbirDeployableArtifact artifact,
        PbirDeployableManifest manifest,
        List<PbirDeployableDiagnostic> hashResults)
    {
        var fileSetContent = string.Join(
            "\n",
            artifact.Files.Select(file => $"{file.RelativePath}\n{file.ByteLength}\n{file.HashSha256}"));
        var expectedFileSetHash = ComputeSha256(fileSetContent);
        var expectedLineageHash = ComputeSha256(JsonSerializer.Serialize(new
        {
            pbirIrRef = artifact.Lineage.PbirIrRef,
            pbirIrContentHash = artifact.Lineage.PbirIrContentHash,
            serializerRequestRef = artifact.Lineage.SerializerRequestRef,
            deployableSerializerRequestRef = artifact.Lineage.DeployableSerializerRequestRef,
            semanticModelInventoryRef = artifact.Lineage.SemanticModelInventoryRef,
            semanticModelInventoryContentHash = artifact.Lineage.SemanticModelInventoryContentHash,
            upstreamLineage = artifact.Lineage.UpstreamLineage,
            immutableLineage = artifact.Lineage.ImmutableLineage
        }));
        var canonicalJson = new PbirDeployableSerializerCanonicalJson();
        var expectedArtifactHash = canonicalJson.ComputeArtifactHash(
            artifact.SchemaVersion,
            artifact.ArtifactId,
            artifact.TargetFormat,
            artifact.Files,
            artifact.Lineage,
            artifact.Hashes.InputHash,
            expectedFileSetHash,
            expectedLineageHash);
        var expectedManifestHash = canonicalJson.ComputeManifestHash(
            manifest.SchemaVersion,
            manifest.ManifestId,
            manifest.ArtifactRef,
            manifest.SchemaLock,
            manifest.Files,
            manifest.SupportedFeatures,
            manifest.Warnings,
            manifest.UnsupportedSections,
            manifest.Lineage,
            expectedArtifactHash,
            manifest.Hashes.InputHash,
            expectedFileSetHash,
            expectedLineageHash);
        var hashesMatch =
            artifact.Hashes.InputHash == manifest.Hashes.InputHash &&
            artifact.Hashes.FileSetHash == expectedFileSetHash &&
            manifest.Hashes.FileSetHash == expectedFileSetHash &&
            artifact.Hashes.ArtifactHash == expectedArtifactHash &&
            manifest.Hashes.ArtifactHash == expectedArtifactHash &&
            artifact.Hashes.ManifestHash == expectedManifestHash &&
            manifest.Hashes.ManifestHash == expectedManifestHash &&
            artifact.Hashes.LineageHash == expectedLineageHash &&
            manifest.Hashes.LineageHash == expectedLineageHash &&
            artifact.Lineage.LineageHash == expectedLineageHash &&
            manifest.Lineage.LineageHash == expectedLineageHash &&
            artifact.Lineage == manifest.Lineage;

        if (!hashesMatch)
        {
            hashResults.Add(Diagnostic(
                "PBIRDEPLOY-HASH-OUTPUT-002",
                "artifact",
                "File-set, artifact, manifest, and immutable lineage hashes must match canonical content."));
        }
    }

    private static void ValidateDocumentCrossReferences(
        IReadOnlyList<PbirDeployableGeneratedFile> files,
        List<PbirDeployableDiagnostic> crossReferenceResults,
        List<PbirDeployableDiagnostic> structuralResults)
    {
        try
        {
            var pagesMetadataFile = files.Single(file => file.RelativePath == "definition/pages/pages.json");
            using var pagesMetadata = JsonDocument.Parse(pagesMetadataFile.Content);
            var pageOrder = pagesMetadata.RootElement.GetProperty("pageOrder")
                .EnumerateArray()
                .Select(value => value.GetString()!)
                .ToArray();
            var activePageName = pagesMetadata.RootElement.GetProperty("activePageName").GetString();
            var pageFiles = files
                .Where(file => file.RelativePath.EndsWith("/page.json", StringComparison.Ordinal))
                .OrderBy(file =>
                {
                    using var document = JsonDocument.Parse(file.Content);
                    var name = document.RootElement.GetProperty("name").GetString();
                    return Array.IndexOf(pageOrder, name);
                })
                .ToArray();
            var pageNames = new List<string>();

            foreach (var pageFile in pageFiles)
            {
                var segments = pageFile.RelativePath.Split('/');
                using var pageDocument = JsonDocument.Parse(pageFile.Content);
                var name = pageDocument.RootElement.GetProperty("name").GetString();
                if (segments.Length != 4 || name != segments[2])
                {
                    crossReferenceResults.Add(Diagnostic(
                        "PBIRDEPLOY-XREF-002",
                        pageFile.RelativePath,
                        "Page document name must equal its containing page folder."));
                }

                pageNames.Add(name!);
            }

            if (!pageOrder.SequenceEqual(pageNames, StringComparer.Ordinal) ||
                activePageName is null ||
                !pageOrder.Contains(activePageName, StringComparer.Ordinal))
            {
                crossReferenceResults.Add(Diagnostic(
                    "PBIRDEPLOY-XREF-003",
                    pagesMetadataFile.RelativePath,
                    "Page order, active page, and page definition inventory must match exactly."));
            }

            var canonicalJson = new PbirDeployableSerializerCanonicalJson();
            var allowedSlots = Enumerable.Range(1, 6)
                .Select(canonicalJson.GetLayoutSlot)
                .ToArray();
            foreach (var visualFile in files.Where(file =>
                         file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal)))
            {
                var segments = visualFile.RelativePath.Split('/');
                using var visualDocument = JsonDocument.Parse(visualFile.Content);
                var root = visualDocument.RootElement;
                var name = root.GetProperty("name").GetString();
                if (segments.Length != 6 ||
                    !pageOrder.Contains(segments[2], StringComparer.Ordinal) ||
                    name != segments[4])
                {
                    crossReferenceResults.Add(Diagnostic(
                        "PBIRDEPLOY-XREF-004",
                        visualFile.RelativePath,
                        "Visual page and name must match its containing folders."));
                }

                var position = root.GetProperty("position");
                var positionMatches = allowedSlots.Count(slot =>
                    position.GetProperty("x").GetInt32() == slot.X &&
                    position.GetProperty("y").GetInt32() == slot.Y &&
                    position.GetProperty("z").GetInt32() == slot.Z &&
                    position.GetProperty("height").GetInt32() == slot.Height &&
                    position.GetProperty("width").GetInt32() == slot.Width &&
                    position.GetProperty("tabOrder").GetInt32() == slot.TabOrder);
                var x = position.GetProperty("x").GetInt32();
                var y = position.GetProperty("y").GetInt32();
                var width = position.GetProperty("width").GetInt32();
                var height = position.GetProperty("height").GetInt32();
                var boundedPosition = x >= 0 && y >= 0 && width > 0 && height > 0 &&
                    x + width <= 1280 && y + height <= 720;
                if (positionMatches != 1 && !boundedPosition)
                {
                    structuralResults.Add(Diagnostic(
                        "PBIRDEPLOY-STRUCTURE-002",
                        visualFile.RelativePath,
                        "Visual position must match one exact modern-grid-1280x720/v1 slot."));
                }

                ValidateVisualDocumentRoles(root, visualFile.RelativePath, structuralResults);
            }
        }
        catch (InvalidOperationException)
        {
            structuralResults.Add(Diagnostic(
                "PBIRDEPLOY-STRUCTURE-003",
                "artifact.files",
                "Required page metadata and page documents must exist exactly once."));
        }
        catch (KeyNotFoundException)
        {
            structuralResults.Add(Diagnostic(
                "PBIRDEPLOY-STRUCTURE-004",
                "artifact.files",
                "Page and visual documents must contain every required runtime property."));
        }
        catch (JsonException)
        {
            structuralResults.Add(Diagnostic(
                "PBIRDEPLOY-STRUCTURE-006",
                "artifact.files",
                "Page and visual cross-reference validation requires valid JSON documents."));
        }
    }

    private static void ValidateVisualDocumentRoles(
        JsonElement root,
        string relativePath,
        List<PbirDeployableDiagnostic> structuralResults)
    {
        var visual = root.GetProperty("visual");
        var visualType = visual.GetProperty("visualType").GetString();
        var queryState = visual.GetProperty("query").GetProperty("queryState");
        var roles = queryState.EnumerateObject().ToArray();
        var valid = visualType switch
        {
            "card" => roles.Length == 1 && roles[0].Name == "Fields" &&
                      roles[0].Value.GetProperty("projections").GetArrayLength() == 1,
            "table" => roles.Length == 1 && roles[0].Name == "Values" &&
                       roles[0].Value.GetProperty("projections").GetArrayLength() > 0,
            "clusteredColumnChart" or "lineChart" =>
                roles.Length == 2 &&
                roles[0].Name == "Category" &&
                roles[0].Value.GetProperty("projections").GetArrayLength() == 1 &&
                roles[1].Name == "Y" &&
                roles[1].Value.GetProperty("projections").GetArrayLength() > 0,
            _ => false
        };

        if (!valid)
        {
            structuralResults.Add(Diagnostic(
                "PBIRDEPLOY-STRUCTURE-005",
                relativePath,
                "Visual type and query-state roles must match the locked Phase 29 templates."));
        }
    }

    private static void ValidatePages(
        PbirIntermediateRepresentation ir,
        List<PbirDeployableDiagnostic> duplicateIdentities,
        List<PbirDeployableDiagnostic> invalidNavigationDefinitions)
    {
        if (HasDuplicates(ir.Pages.Select(page => page.PageId)) ||
            HasDuplicates(ir.Pages.Select(page => page.PageIdentity)) ||
            ir.Pages.Select(page => page.Order).Distinct().Count() != ir.Pages.Count ||
            !ir.Pages.OrderBy(page => page.Order).Select(page => page.Order)
                .SequenceEqual(Enumerable.Range(1, ir.Pages.Count)) ||
            ir.Pages.Any(page =>
                string.IsNullOrWhiteSpace(page.PageId) ||
                string.IsNullOrWhiteSpace(page.PageIdentity) ||
                page.NavigationBehavior != "pageTab" ||
                string.IsNullOrWhiteSpace(page.IntendedPurpose)))
        {
            duplicateIdentities.Add(Diagnostic(
                "PBIRDEPLOY-IDENTITY-002",
                "pages",
                "Page ids, identities, and contiguous order values must be complete and unique."));
        }

        var orderedPages = ir.Pages.OrderBy(page => page.Order).ToArray();
        var expectedTransitions = orderedPages
            .Zip(orderedPages.Skip(1))
            .Select(pair => new PbirIntermediateRepresentationPageTransition(
                pair.First.PageId,
                pair.Second.PageId,
                $"{pair.First.PageId}->{pair.Second.PageId}"))
            .ToArray();
        var expectedBookmarks = orderedPages
            .Select(page => $"page:{page.PageId}")
            .Append($"landing:{ir.Navigation.LandingPage}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        if (!orderedPages.Any(page => page.PageId == ir.Navigation.LandingPage) ||
            !ir.Navigation.PageTransitions.SequenceEqual(expectedTransitions) ||
            !ir.Navigation.Bookmarks.OrderBy(value => value, StringComparer.Ordinal)
                .SequenceEqual(expectedBookmarks) ||
            ir.Navigation.DrillPaths.Count > 0)
        {
            invalidNavigationDefinitions.Add(Diagnostic(
                "PBIRDEPLOY-NAV-001",
                "navigation",
                "Navigation must contain the exact landing page, sequential transitions, canonical page markers, and no drill paths."));
        }
    }

    private static void ValidateVisuals(
        PbirIntermediateRepresentation ir,
        PbirDeployableSerializerRequest request,
        List<PbirDeployableDiagnostic> unsupportedVisualTypes,
        List<PbirDeployableDiagnostic> incompleteSemanticBindings,
        List<PbirDeployableDiagnostic> invalidModelReferences,
        List<PbirDeployableDiagnostic> duplicateIdentities,
        List<PbirDeployableDiagnostic> invalidLayoutDefinitions)
    {
        if (HasDuplicates(ir.Visuals.Select(visual => visual.VisualId)) ||
            HasDuplicates(ir.Layout.Containers.Select(container => container.ContainerId)) ||
            HasDuplicates(request.VisualBindings.Select(binding => binding.VisualId)))
        {
            duplicateIdentities.Add(Diagnostic(
                "PBIRDEPLOY-IDENTITY-003",
                "visuals",
                "Visual, container, and binding identities must be unique."));
        }

        if (request.VisualBindings.Select(binding => binding.VisualId).OrderBy(value => value, StringComparer.Ordinal)
            .SequenceEqual(ir.Visuals.Select(visual => visual.VisualId).OrderBy(value => value, StringComparer.Ordinal)) is false)
        {
            incompleteSemanticBindings.Add(Diagnostic(
                "PBIRDEPLOY-BINDING-001",
                "visualBindings",
                "Every visual must have exactly one binding and extra bindings are invalid."));
        }

        foreach (var page in ir.Pages)
        {
            var pageVisuals = ir.Visuals
                .Where(visual => visual.PageId == page.PageId)
                .OrderBy(visual => visual.Order)
                .ToArray();
            var containers = ir.Layout.Containers
                .Where(container => container.PageId == page.PageId)
                .ToArray();
            var slots = pageVisuals
                .Select(visual => ParseSlot(visual, invalidLayoutDefinitions))
                .ToArray();

            if (containers.Length != 1 ||
                pageVisuals.Length > 6 ||
                slots.Any(slot => slot is null) ||
                slots.Where(slot => slot is not null).Distinct().Count() != pageVisuals.Length ||
                !slots.Where(slot => slot is not null).Select(slot => slot!.Value)
                    .SequenceEqual(slots.Where(slot => slot is not null).Select(slot => slot!.Value).Order()) ||
                (containers.Length == 1 &&
                 !containers[0].VisualRefs.OrderBy(value => value, StringComparer.Ordinal)
                     .SequenceEqual(pageVisuals.Select(visual => visual.VisualId).OrderBy(value => value, StringComparer.Ordinal))))
            {
                invalidLayoutDefinitions.Add(Diagnostic(
                    "PBIRDEPLOY-LAYOUT-001",
                    $"pages.{page.PageId}",
                    "Page layout must use one complete container and unique ordered slots 1 through 6."));
            }
        }

        if (!ir.Layout.Spacing.SequenceEqual(["standard-8px-grid"]) ||
            !ir.Layout.Alignment.SequenceEqual(["deterministic-grid", "visual-placement-preserved"]) ||
            !ir.Layout.ResponsiveHints.SequenceEqual(
                [
                    "preserve-page-order",
                    "preserve-visual-intent",
                    "allow-future-serializer-layout-adaptation"
                ]))
        {
            invalidLayoutDefinitions.Add(Diagnostic(
                "PBIRDEPLOY-LAYOUT-002",
                "layout",
                "Layout profile markers must match modern-grid-1280x720/v1 exactly."));
        }

        foreach (var visual in ir.Visuals)
        {
            if (!SupportedVisualTypes.Contains(visual.VisualType))
            {
                unsupportedVisualTypes.Add(Diagnostic(
                    "PBIRDEPLOY-VISUAL-001",
                    $"visuals.{visual.VisualId}.visualType",
                    "Visual type is outside the Phase 29 allowlist."));
                continue;
            }

            var semanticMatches = ir.Semantics
                .Where(semantic =>
                    semantic.PageId == visual.PageId &&
                    semantic.Kpis.Count(value => value == visual.SemanticIntent) == 1)
                .ToArray();
            var bindingMatches = request.VisualBindings
                .Where(binding => binding.VisualId == visual.VisualId)
                .ToArray();

            if (semanticMatches.Length != 1 ||
                bindingMatches.Length != 1 ||
                !visual.InteractionModel.SequenceEqual(["none"]))
            {
                incompleteSemanticBindings.Add(Diagnostic(
                    "PBIRDEPLOY-BINDING-002",
                    $"visuals.{visual.VisualId}",
                    "Visual must resolve one semantic record, one binding, and interaction none."));
                continue;
            }

            ValidateBinding(
                visual,
                semanticMatches[0],
                bindingMatches[0],
                request.SemanticModelInventory,
                incompleteSemanticBindings,
                invalidModelReferences);
        }

        ValidateExactSemanticCoverage(
            ir,
            request,
            incompleteSemanticBindings,
            invalidModelReferences);
    }

    private static void ValidateExactSemanticCoverage(
        PbirIntermediateRepresentation ir,
        PbirDeployableSerializerRequest request,
        List<PbirDeployableDiagnostic> incompleteSemanticBindings,
        List<PbirDeployableDiagnostic> invalidModelReferences)
    {
        var projectedEntryRefs = request.VisualBindings
            .SelectMany(binding => binding.Projections)
            .Select(projection => projection.SemanticModelEntryRef)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var inventoryEntryRefs = request.SemanticModelInventory.Entries
            .Select(entry => entry.EntryId)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (!projectedEntryRefs.SequenceEqual(inventoryEntryRefs, StringComparer.Ordinal))
        {
            invalidModelReferences.Add(Diagnostic(
                "PBIRDEPLOY-MODEL-002",
                "semanticModelInventory.entries",
                "Every inventory entry must be referenced by at least one explicit visual projection and no extra references are allowed."));
        }

        foreach (var semantic in ir.Semantics)
        {
            var semanticVisuals = ir.Visuals
                .Where(visual =>
                    visual.PageId == semantic.PageId &&
                    semantic.Kpis.Contains(visual.SemanticIntent, StringComparer.Ordinal))
                .ToArray();
            var expectedRelationships = semanticVisuals
                .Select(visual => $"visual:[{visual.VisualId}]->semantic:[{visual.SemanticIntent}]")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (!semantic.Relationships.OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(expectedRelationships, StringComparer.Ordinal))
            {
                incompleteSemanticBindings.Add(Diagnostic(
                    "PBIRDEPLOY-BINDING-005",
                    $"semantics.{semantic.SemanticId}.relationships",
                    "Semantic relationships must exactly cover the declared visuals and semantic intents."));
            }

            var projectedTokens = request.VisualBindings
                .Where(binding => semanticVisuals.Any(visual => visual.VisualId == binding.VisualId))
                .SelectMany(binding => binding.Projections)
                .Select(projection => projection.SourceSemanticToken)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var semanticTokens = semantic.Measures
                .Concat(semantic.Dimensions)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (!projectedTokens.SequenceEqual(semanticTokens, StringComparer.Ordinal))
            {
                incompleteSemanticBindings.Add(Diagnostic(
                    "PBIRDEPLOY-BINDING-006",
                    $"semantics.{semantic.SemanticId}",
                    "Semantic measures and dimensions must exactly match projected source tokens."));
            }
        }
    }

    private static void ValidateBinding(
        PbirIntermediateRepresentationVisual visual,
        PbirIntermediateRepresentationSemantic semantic,
        PbirVisualBinding binding,
        PbirSemanticModelInventory inventory,
        List<PbirDeployableDiagnostic> incompleteSemanticBindings,
        List<PbirDeployableDiagnostic> invalidModelReferences)
    {
        var expectedRelationship = $"visual:[{visual.VisualId}]->semantic:[{visual.SemanticIntent}]";
        if (!semantic.Relationships.Contains(expectedRelationship, StringComparer.Ordinal) ||
            semantic.Filters.Count > 0 ||
            semantic.DrillBehavior != "none" ||
            semantic.Relationships.Any(value => !value.StartsWith("visual:[", StringComparison.Ordinal)))
        {
            incompleteSemanticBindings.Add(Diagnostic(
                "PBIRDEPLOY-BINDING-003",
                $"semantics.{semantic.SemanticId}",
                "Semantic relationship, filters, and drill behavior are outside the supported subset."));
        }

        var roleGroups = binding.Projections
            .GroupBy(projection => projection.Role, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.OrderBy(value => value.ProjectionOrder).ToArray(), StringComparer.Ordinal);
        var roleContractValid = visual.VisualType switch
        {
            "card" =>
                roleGroups.Count == 1 &&
                roleGroups.TryGetValue("Fields", out var fields) &&
                fields.Length == 1,
            "table" =>
                roleGroups.Count == 1 &&
                roleGroups.TryGetValue("Values", out var values) &&
                values.Length >= 1,
            "clusteredColumnChart" or "lineChart" =>
                roleGroups.Count == 2 &&
                roleGroups.TryGetValue("Category", out var category) &&
                category.Length == 1 &&
                roleGroups.TryGetValue("Y", out var y) &&
                y.Length >= 1,
            _ => false
        };

        if (!roleContractValid ||
            roleGroups.Values.Any(group =>
                !group.Select(value => value.ProjectionOrder).SequenceEqual(Enumerable.Range(1, group.Length))) ||
            binding.Projections.Any(projection =>
                projection.Aggregation != "none" ||
                projection.DisplayName is not null ||
                projection.Format is not null ||
                !IsNfcNonempty(projection.Role) ||
                !IsNfcNonempty(projection.SourceSemanticToken) ||
                !IsNfcNonempty(projection.SemanticModelEntryRef) ||
                !IsNfcNonempty(projection.QueryRef) ||
                !IsNfcNonempty(projection.NativeQueryRef)) ||
            HasDuplicates(binding.Projections.Select(projection => projection.QueryRef)) ||
            HasDuplicates(binding.Projections.Select(projection => projection.NativeQueryRef)))
        {
            incompleteSemanticBindings.Add(Diagnostic(
                "PBIRDEPLOY-BINDING-004",
                $"visualBindings.{visual.VisualId}",
                "Roles, projection order, references, aggregation, display name, and format must be explicit and supported."));
        }

        foreach (var projection in binding.Projections)
        {
            var entries = inventory.Entries
                .Where(entry =>
                    entry.EntryId == projection.SemanticModelEntryRef &&
                    entry.Token == projection.SourceSemanticToken)
                .ToArray();
            var tokenIsMeasure = semantic.Measures.Count(value => value == projection.SourceSemanticToken) == 1;
            var tokenIsDimension = semantic.Dimensions.Count(value => value == projection.SourceSemanticToken) == 1;
            var kindIsValid = entries.Length == 1 &&
                              ((tokenIsMeasure && entries[0].Kind == PbirSemanticModelEntryKind.Measure) ||
                               (tokenIsDimension && entries[0].Kind == PbirSemanticModelEntryKind.Column));
            var roleKindValid = entries.Length == 1 && visual.VisualType switch
            {
                "card" => projection.Role == "Fields" && entries[0].Kind == PbirSemanticModelEntryKind.Measure,
                "table" => projection.Role == "Values",
                "clusteredColumnChart" or "lineChart" =>
                    projection.Role == "Category"
                        ? entries[0].Kind == PbirSemanticModelEntryKind.Column
                        : projection.Role == "Y" && entries[0].Kind == PbirSemanticModelEntryKind.Measure,
                _ => false
            };

            if (!kindIsValid || !roleKindValid)
            {
                invalidModelReferences.Add(Diagnostic(
                    "PBIRDEPLOY-MODEL-001",
                    $"visualBindings.{visual.VisualId}.{projection.Role}",
                    "Projection token, model entry, role, and semantic kind must resolve exactly."));
            }
        }
    }

    private static int? ParseSlot(
        PbirIntermediateRepresentationVisual visual,
        List<PbirDeployableDiagnostic> invalidLayoutDefinitions)
    {
        var prefix = $"page:{visual.PageId}/slot:";
        if (!visual.Placement.StartsWith(prefix, StringComparison.Ordinal) ||
            !int.TryParse(visual.Placement[prefix.Length..], out var slot) ||
            slot is < 1 or > 6)
        {
            invalidLayoutDefinitions.Add(Diagnostic(
                "PBIRDEPLOY-LAYOUT-001",
                $"visuals.{visual.VisualId}.placement",
                "Visual placement must match page:[PageId]/slot:[1..6]."));
            return null;
        }

        return slot;
    }

    private static bool HasDuplicates(IEnumerable<string> values)
    {
        var array = values.ToArray();
        return array.Distinct(StringComparer.Ordinal).Count() != array.Length;
    }

    private static bool IsNfcNonempty(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.IsNormalized(NormalizationForm.FormC);
    }

    private static string ComputeSha256(string value)
    {
        return Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant();
    }

    private static PbirDeployableDiagnostic Diagnostic(string code, string path, string message)
    {
        return new PbirDeployableDiagnostic(code, path, message);
    }

    private static IReadOnlyList<PbirDeployableDiagnostic> Order(
        IEnumerable<PbirDeployableDiagnostic> diagnostics)
    {
        return diagnostics
            .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }
}
