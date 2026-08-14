using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableSerializerService
{
    private readonly PbirDeployableSerializerSafetyGate _safetyGate;
    private readonly PbirDeployableSerializerValidator _validator;
    private readonly PbirDeployableSerializerCanonicalJson _canonicalJson;

    internal PbirDeployableSerializerService()
        : this(
            new PbirDeployableSerializerSafetyGate(),
            new PbirDeployableSerializerValidator(),
            new PbirDeployableSerializerCanonicalJson())
    {
    }

    internal PbirDeployableSerializerService(
        PbirDeployableSerializerSafetyGate safetyGate,
        PbirDeployableSerializerValidator validator,
        PbirDeployableSerializerCanonicalJson canonicalJson)
    {
        _safetyGate = safetyGate;
        _validator = validator;
        _canonicalJson = canonicalJson;
    }

    internal PbirDeployableSerializerState CreateArtifacts(
        PbirIntermediateRepresentationState irState,
        PbirSerializerRequest serializerRequest,
        PbirDeployableSerializerRequest request)
    {
        ArgumentNullException.ThrowIfNull(irState);
        ArgumentNullException.ThrowIfNull(serializerRequest);
        ArgumentNullException.ThrowIfNull(request);

        var safety = _safetyGate.Validate(irState, serializerRequest, request);
        if (!safety.IsValid || irState.Ir is null)
        {
            return Rejected(safety.Readiness, safety.Diagnostics);
        }

        var inputDiagnostics = _validator.ValidateInput(irState.Ir, request);
        if (inputDiagnostics.HasFailures)
        {
            return Rejected(PbirDeployableSerializerReadinessState.Blocked, inputDiagnostics);
        }

        var candidate = CreateCandidate(irState.Ir, serializerRequest, request, inputDiagnostics);
        var validation = _validator.ValidateOutput(candidate.Artifact, candidate.Manifest);
        if (!validation.IsValid)
        {
            return new PbirDeployableSerializerState(
                Artifact: null,
                Manifest: null,
                Validation: validation,
                Readiness: PbirDeployableSerializerReadinessState.Blocked,
                Diagnostics: inputDiagnostics with
                {
                    SchemaIncompatibilities =
                    [
                        .. validation.SchemaContractResults,
                        .. validation.StructuralValidationResults,
                        .. validation.CrossReferenceValidationResults
                    ],
                    HashViolations = validation.HashValidationResults
                });
        }

        return new PbirDeployableSerializerState(
            Artifact: candidate.Artifact,
            Manifest: candidate.Manifest,
            Validation: validation,
            Readiness: PbirDeployableSerializerReadinessState.Serialized,
            Diagnostics: inputDiagnostics);
    }

    private (PbirDeployableArtifact Artifact, PbirDeployableManifest Manifest) CreateCandidate(
        PbirIntermediateRepresentation ir,
        PbirSerializerRequest serializerRequest,
        PbirDeployableSerializerRequest request,
        PbirDeployableDiagnostics diagnostics)
    {
        var pages = ir.Pages.OrderBy(page => page.Order).ToArray();
        var pageIdentities = pages.ToDictionary(
            page => page.PageId,
            page => _canonicalJson.CreatePageIdentity(ir.Metadata.IrId, page.PageIdentity),
            StringComparer.Ordinal);
        var visualIdentities = ir.Visuals.ToDictionary(
            visual => visual.VisualId,
            visual =>
            {
                var page = pages.Single(value => value.PageId == visual.PageId);
                return _canonicalJson.CreateVisualIdentity(
                    ir.Metadata.IrId,
                    page.PageIdentity,
                    visual.VisualId);
            },
            StringComparer.Ordinal);

        if (pageIdentities.Values.Distinct(StringComparer.Ordinal).Count() != pageIdentities.Count ||
            visualIdentities.Values.Distinct(StringComparer.Ordinal).Count() != visualIdentities.Count)
        {
            throw new InvalidOperationException("Generated PBIR identity collision.");
        }

        var files = new List<PbirDeployableGeneratedFile>
        {
            CreateFile(
                "definition.pbir",
                PbirDeployableSchemaLock.DefinitionPropertiesSchemaUrl,
                PbirDeployableSchemaLock.DefinitionPropertiesSchemaVersion,
                [ir.Metadata.IrId, request.RequestId],
                writer =>
                {
                    writer.WriteStartObject();
                    writer.WriteString("$schema", PbirDeployableSchemaLock.DefinitionPropertiesSchemaUrl);
                    writer.WriteString("version", PbirDeployableSchemaLock.PbirFileFormatVersion);
                    writer.WritePropertyName("datasetReference");
                    writer.WriteStartObject();
                    writer.WritePropertyName("byPath");
                    writer.WriteStartObject();
                    writer.WriteString("path", request.DatasetReference.ByPath.Path);
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }),
            CreateFile(
                "definition/version.json",
                PbirDeployableSchemaLock.VersionMetadataSchemaUrl,
                PbirDeployableSchemaLock.DefinitionSchemaVersion,
                [ir.Metadata.IrId],
                writer =>
                {
                    writer.WriteStartObject();
                    writer.WriteString("$schema", PbirDeployableSchemaLock.VersionMetadataSchemaUrl);
                    writer.WriteString("version", PbirDeployableSchemaLock.ReportDefinitionVersion);
                    writer.WriteEndObject();
                }),
            CreateFile(
                "definition/report.json",
                PbirDeployableSchemaLock.ReportSchemaUrl,
                PbirDeployableSchemaLock.DefinitionSchemaVersion,
                [ir.Metadata.IrId],
                writer =>
                {
                    WriteReport(writer, request);
                }),
            CreateFile(
                "definition/pages/pages.json",
                PbirDeployableSchemaLock.PagesMetadataSchemaUrl,
                PbirDeployableSchemaLock.DefinitionSchemaVersion,
                [ir.Metadata.IrId],
                writer =>
                {
                    writer.WriteStartObject();
                    writer.WriteString("$schema", PbirDeployableSchemaLock.PagesMetadataSchemaUrl);
                    writer.WritePropertyName("pageOrder");
                    writer.WriteStartArray();
                    foreach (var page in pages)
                    {
                        writer.WriteStringValue(pageIdentities[page.PageId]);
                    }

                    writer.WriteEndArray();
                    writer.WriteString("activePageName", pageIdentities[ir.Navigation.LandingPage]);
                    writer.WriteEndObject();
                })
        };

        foreach (var page in pages)
        {
            var pageIdentity = pageIdentities[page.PageId];
            files.Add(CreateFile(
                $"definition/pages/{pageIdentity}/page.json",
                PbirDeployableSchemaLock.PageSchemaUrl,
                PbirDeployableSchemaLock.DefinitionSchemaVersion,
                [ir.Metadata.IrId, page.PageIdentity],
                writer => WritePage(writer, pageIdentity, page, request.Authoring)));

            foreach (var visual in ir.Visuals
                         .Where(value => value.PageId == page.PageId)
                         .OrderBy(value => value.Order))
            {
                var visualIdentity = visualIdentities[visual.VisualId];
                var slot = visual.Layout is null
                    ? _canonicalJson.GetLayoutSlot(ParseSlot(visual))
                    : new PbirDeployableLayoutSlot(
                        ParseSlot(visual),
                        visual.Layout.X,
                        visual.Layout.Y,
                        visual.Layout.Width,
                        visual.Layout.Height,
                        Math.Max(0, visual.Order - 1) * 1000,
                        Math.Max(0, visual.Order - 1) * 1000);
                var binding = request.VisualBindings.Single(value => value.VisualId == visual.VisualId);
                var semantic = ir.Semantics.Single(value =>
                    value.PageId == visual.PageId &&
                    value.Kpis.Contains(visual.SemanticIntent, StringComparer.Ordinal));
                files.Add(CreateFile(
                    $"definition/pages/{pageIdentity}/visuals/{visualIdentity}/visual.json",
                    PbirDeployableSchemaLock.VisualContainerSchemaUrl,
                    PbirDeployableSchemaLock.DefinitionSchemaVersion,
                    [ir.Metadata.IrId, page.PageIdentity, visual.VisualId, semantic.SemanticId],
                    writer => WriteVisual(writer, visual, visualIdentity, slot, binding, request.SemanticModelInventory, request.Authoring)));
            }
        }

        var orderedFiles = files.OrderBy(file => file.RelativePath, StringComparer.Ordinal).ToArray();
        var inputHash = _canonicalJson.ComputeSha256(JsonSerializer.Serialize(new
        {
            irContentHash = ir.Hashes.ContentHash,
            request
        }));
        var fileSetHash = ComputeFileSetHash(orderedFiles);
        var artifactId = $"pbirDeployableArtifact:{ir.Metadata.IrId}";
        var manifestId = $"pbirDeployableManifest:{ir.Metadata.IrId}";
        var immutableLineage = ir.Lineage.ImmutableLineage
            .Append(serializerRequest.RequestId)
            .Append(request.RequestId)
            .Append(request.SemanticModelInventoryRef)
            .Append(artifactId)
            .Append(manifestId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var upstreamLineage = ir.Lineage.UpstreamLineage
            .OrderBy(value => value.Stage, StringComparer.Ordinal)
            .ThenBy(value => value.ReferenceId, StringComparer.Ordinal)
            .ThenBy(value => value.Label, StringComparer.Ordinal)
            .ToArray();
        var lineageWithoutHash = new
        {
            pbirIrRef = ir.Metadata.IrId,
            pbirIrContentHash = ir.Hashes.ContentHash,
            serializerRequestRef = serializerRequest.RequestId,
            deployableSerializerRequestRef = request.RequestId,
            semanticModelInventoryRef = request.SemanticModelInventoryRef,
            semanticModelInventoryContentHash = request.SemanticModelInventoryContentHash,
            upstreamLineage,
            immutableLineage
        };
        var lineageHash = _canonicalJson.ComputeSha256(JsonSerializer.Serialize(lineageWithoutHash));
        var lineage = new PbirDeployableLineage(
            SchemaVersion: PbirDeployableLineageContract.SchemaVersionV1,
            PbirIrRef: ir.Metadata.IrId,
            PbirIrContentHash: ir.Hashes.ContentHash,
            SerializerRequestRef: serializerRequest.RequestId,
            DeployableSerializerRequestRef: request.RequestId,
            SemanticModelInventoryRef: request.SemanticModelInventoryRef,
            SemanticModelInventoryContentHash: request.SemanticModelInventoryContentHash,
            UpstreamLineage: upstreamLineage,
            ImmutableLineage: immutableLineage,
            LineageHash: lineageHash);
        var manifestFiles = orderedFiles.Select(file => new PbirDeployableGeneratedFileReference(
                file.RelativePath,
                file.ContentType,
                file.ByteLength,
                file.HashSha256,
                file.SchemaUrl,
                file.SchemaVersion))
            .ToArray();
        IReadOnlyList<string> schemaLock =
        [
            PbirDeployableSchemaLock.DefinitionPropertiesSchemaUrl,
            PbirDeployableSchemaLock.VersionMetadataSchemaUrl,
            PbirDeployableSchemaLock.ReportSchemaUrl,
            PbirDeployableSchemaLock.PagesMetadataSchemaUrl,
            PbirDeployableSchemaLock.PageSchemaUrl,
            PbirDeployableSchemaLock.VisualContainerSchemaUrl
        ];
        IReadOnlyList<string> supportedFeatures =
        [
            "modernPbir",
            "pageTabNavigation",
            "modern-grid-1280x720/v1",
            "card",
            "table",
            "clusteredColumnChart",
            "lineChart",
            "directColumnBinding",
            "directMeasureBinding"
        ];
        IReadOnlyList<PbirDeployableDiagnostic> warnings =
        [
            new(
                "PBIRDEPLOY-WARNING-001",
                "artifact",
                "Artifact inventory is in memory only and carries no materialization authority.")
        ];
        var artifactHash = _canonicalJson.ComputeArtifactHash(
            PbirDeployableArtifactContract.SchemaVersionV1,
            artifactId,
            request.TargetFormat,
            orderedFiles,
            lineage,
            inputHash,
            fileSetHash,
            lineageHash);
        var manifestHash = _canonicalJson.ComputeManifestHash(
            PbirDeployableManifestContract.SchemaVersionV1,
            manifestId,
            artifactId,
            schemaLock,
            manifestFiles,
            supportedFeatures,
            warnings,
            diagnostics.UnsupportedSections,
            lineage,
            artifactHash,
            inputHash,
            fileSetHash,
            lineageHash);
        var hashes = new PbirDeployableHashes(
            SchemaVersion: PbirDeployableHashesContract.SchemaVersionV1,
            InputHash: inputHash,
            FileSetHash: fileSetHash,
            ArtifactHash: artifactHash,
            ManifestHash: manifestHash,
            LineageHash: lineageHash);
        var artifact = new PbirDeployableArtifact(
            SchemaVersion: PbirDeployableArtifactContract.SchemaVersionV1,
            ArtifactId: artifactId,
            TargetFormat: request.TargetFormat,
            Files: orderedFiles,
            Lineage: lineage,
            Hashes: hashes);
        var manifest = new PbirDeployableManifest(
            SchemaVersion: PbirDeployableManifestContract.SchemaVersionV1,
            ManifestId: manifestId,
            ArtifactRef: artifactId,
            SchemaLock: schemaLock,
            Files: manifestFiles,
            SupportedFeatures: supportedFeatures,
            Warnings: warnings,
            UnsupportedSections: diagnostics.UnsupportedSections,
            Lineage: lineage,
            Hashes: hashes);

        return (artifact, manifest);
    }

    private PbirDeployableGeneratedFile CreateFile(
        string relativePath,
        string schemaUrl,
        string schemaVersion,
        IReadOnlyList<string> sourceIrReferences,
        Action<Utf8JsonWriter> writeDocument)
    {
        var content = _canonicalJson.SerializeDocument(writeDocument);
        return new PbirDeployableGeneratedFile(
            RelativePath: relativePath,
            ContentType: "application/json",
            Content: content,
            ByteLength: Encoding.UTF8.GetByteCount(content),
            HashSha256: _canonicalJson.ComputeSha256(content),
            SchemaUrl: schemaUrl,
            SchemaVersion: schemaVersion,
            SourceIrReferences: sourceIrReferences
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
    }

    private static void WriteVisual(
        Utf8JsonWriter writer,
        PbirIntermediateRepresentationVisual visual,
        string visualIdentity,
        PbirDeployableLayoutSlot slot,
        PbirVisualBinding binding,
        PbirSemanticModelInventory inventory,
        LocalPbirGenerationRequestV3? authoring)
    {
        writer.WriteStartObject();
        writer.WriteString("$schema", PbirDeployableSchemaLock.VisualContainerSchemaUrl);
        writer.WriteString("name", visualIdentity);
        writer.WritePropertyName("position");
        writer.WriteStartObject();
        writer.WriteNumber("x", slot.X);
        writer.WriteNumber("y", slot.Y);
        writer.WriteNumber("z", slot.Z);
        writer.WriteNumber("height", slot.Height);
        writer.WriteNumber("width", slot.Width);
        writer.WriteNumber("tabOrder", slot.TabOrder);
        writer.WriteEndObject();
        writer.WritePropertyName("visual");
        writer.WriteStartObject();
        writer.WriteString("visualType", visual.VisualType);
        var visualAuthoring = authoring?.Visuals
            .FirstOrDefault(value => visual.VisualId == value.VisualId || visual.VisualId.EndsWith($":{value.VisualId}", StringComparison.Ordinal))
            ?.Authoring;
        writer.WritePropertyName("query");
        writer.WriteStartObject();
        writer.WritePropertyName("queryState");
        writer.WriteStartObject();

        foreach (var role in GetRoleOrder(visual.VisualType, visualAuthoring))
        {
            writer.WritePropertyName(role);
            writer.WriteStartObject();
            writer.WritePropertyName("projections");
            writer.WriteStartArray();
            foreach (var projection in binding.Projections
                         .Where(value => value.Role == role)
                         .OrderBy(value => value.ProjectionOrder))
            {
                var entry = inventory.Entries.Single(value =>
                    value.EntryId == projection.SemanticModelEntryRef &&
                    value.Token == projection.SourceSemanticToken);
                WriteProjection(writer, projection, entry);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
        WriteVisualAuthoring(writer, visual, visualAuthoring);
        writer.WriteEndObject();
        WriteFilterConfig(writer, visualAuthoring?.Filters);
        writer.WriteEndObject();
    }

    private static void WriteReport(Utf8JsonWriter writer, PbirDeployableSerializerRequest request)
    {
        writer.WriteStartObject();
        writer.WriteString("$schema", PbirDeployableSchemaLock.ReportSchemaUrl);
        writer.WriteString("layoutOptimization", "None");
        writer.WritePropertyName("themeCollection");
        writer.WriteStartObject();
        if (request.Authoring?.Theme is not null)
        {
            writer.WritePropertyName("baseTheme");
            WriteThemeMetadata(writer, "Base", "SharedResources");
            writer.WritePropertyName("customTheme");
            WriteThemeMetadata(writer, request.Authoring.Theme.Name, "RegisteredResources");
        }
        writer.WriteEndObject();
        WriteFilterConfig(writer, request.Authoring?.ReportFilters);
        var palette = request.Authoring?.Theme?.Palette;
        if (request.Authoring?.Metadata is not null || palette is { Count: > 0 })
        {
            writer.WritePropertyName("annotations");
            writer.WriteStartArray();
            WriteAnnotation(writer, "author", request.Authoring?.Metadata?.Author);
            WriteAnnotation(writer, "description", request.Authoring?.Metadata?.Description);
            WriteAnnotation(writer, "displayName", request.Authoring?.Metadata?.DisplayName);
            if (palette is { Count: > 0 })
            {
                WriteAnnotation(writer, "themePalette", string.Join(",", palette.OrderBy(value => value.Hex, StringComparer.OrdinalIgnoreCase).Select(value => value.Hex)));
            }
            writer.WriteEndArray();
        }
        if (request.Authoring?.Interaction is { } interaction)
        {
            writer.WritePropertyName("settings");
            writer.WriteStartObject();
            writer.WriteBoolean("defaultFilterActionIsDataFilter", interaction.Enabled && interaction.Mode == LocalPbirGenerationInteractionMode.CrossFilter);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    private static void WritePage(Utf8JsonWriter writer, string pageIdentity, PbirIntermediateRepresentationPage page, LocalPbirGenerationRequestV3? authoring)
    {
        var pageAuthoring = authoring?.Pages.FirstOrDefault(value => value.PageId == page.PageId)?.Authoring;
        writer.WriteStartObject();
        writer.WriteString("$schema", PbirDeployableSchemaLock.PageSchemaUrl);
        writer.WriteString("name", pageIdentity);
        writer.WriteString("displayName", page.DisplayName ?? page.PageId);
        writer.WriteString("displayOption", "FitToPage");
        writer.WriteNumber("height", 720);
        writer.WriteNumber("width", 1280);
        WriteFilterConfig(writer, pageAuthoring?.Filters);
        if (pageAuthoring?.Background is not null)
        {
            writer.WritePropertyName("objects");
            writer.WriteStartObject();
            writer.WritePropertyName("background");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            writer.WriteString("color", pageAuthoring.Background.Hex);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        if (authoring?.Interaction is { } interaction)
        {
            writer.WritePropertyName("visualInteractions");
            writer.WriteStartArray();
            var visualIds = authoring.Visuals.Where(value => value.PageId == page.PageId).OrderBy(value => value.Order).ThenBy(value => value.VisualId, StringComparer.Ordinal).Select(value => value.VisualId).ToArray();
            foreach (var source in visualIds)
            foreach (var target in visualIds.Where(value => value != source))
            {
                writer.WriteStartObject();
                writer.WriteString("source", source);
                writer.WriteString("target", target);
                writer.WriteString("type", interaction.Enabled ? interaction.Mode switch
                {
                    LocalPbirGenerationInteractionMode.CrossFilter => "DataFilter",
                    LocalPbirGenerationInteractionMode.CrossHighlight => "HighlightFilter",
                    _ => "NoFilter"
                } : "NoFilter");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    private static void WriteThemeMetadata(Utf8JsonWriter writer, string name, string type)
    {
        writer.WriteStartObject();
        writer.WriteString("name", name);
        writer.WriteString("reportVersionAtImport", "1.0.0");
        writer.WriteString("type", type);
        writer.WriteEndObject();
    }

    private static void WriteAnnotation(Utf8JsonWriter writer, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        writer.WriteStartObject();
        writer.WriteString("name", name);
        writer.WriteString("value", value);
        writer.WriteEndObject();
    }

    private static void WriteFilterConfig(Utf8JsonWriter writer, IReadOnlyList<LocalPbirGenerationEqualityFilter>? filters)
    {
        if (filters is not { Count: > 0 }) return;
        writer.WritePropertyName("filterConfig");
        writer.WriteStartObject();
        writer.WritePropertyName("filters");
        writer.WriteStartArray();
        foreach (var filter in filters.OrderBy(value => value.FilterId, StringComparer.Ordinal)) WriteFilter(writer, filter);
        writer.WriteEndArray();
        writer.WriteString("filterSortOrder", "Custom");
        writer.WriteEndObject();
    }

    private static void WriteFilter(Utf8JsonWriter writer, LocalPbirGenerationEqualityFilter filter)
    {
        writer.WriteStartObject();
        writer.WriteString("name", filter.FilterId);
        writer.WriteString("displayName", filter.DisplayName ?? filter.Property);
        writer.WriteNumber("ordinal", 0);
        writer.WritePropertyName("field");
        WriteField(writer, filter);
        writer.WriteString("type", "Categorical");
        writer.WritePropertyName("filter");
        writer.WriteStartObject();
        writer.WriteNumber("Version", 2);
        writer.WritePropertyName("From");
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WriteString("Name", filter.Entity);
        writer.WriteString("Entity", filter.Entity);
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WritePropertyName("Where");
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("Condition");
        writer.WriteStartObject();
        writer.WritePropertyName("In");
        writer.WriteStartObject();
        writer.WritePropertyName("Expressions");
        writer.WriteStartArray();
        WriteField(writer, filter);
        writer.WriteEndArray();
        writer.WritePropertyName("Values");
        writer.WriteStartArray();
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("Literal");
        writer.WriteStartObject();
        writer.WriteString("Value", $"'{filter.Value.Replace("'", "''", StringComparison.Ordinal)}'");
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteString("howCreated", "User");
        writer.WriteEndObject();
    }

    private static void WriteField(Utf8JsonWriter writer, LocalPbirGenerationEqualityFilter filter)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(filter.Kind == LocalPbirGenerationBindingKind.Measure ? "Measure" : "Column");
        writer.WriteStartObject();
        writer.WritePropertyName("Expression");
        writer.WriteStartObject();
        writer.WritePropertyName("SourceRef");
        writer.WriteStartObject();
        writer.WriteString("Entity", filter.Entity);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteString("Property", filter.Property);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteVisualAuthoring(Utf8JsonWriter writer, PbirIntermediateRepresentationVisual visual, LocalPbirGenerationVisualAuthoring? authoring)
    {
        if (authoring is null) return;
        writer.WritePropertyName("objects");
        writer.WriteStartObject();
        if (visual.VisualType == "card" && authoring.Card is { } card)
        {
            WriteTitleObject(writer, card.Title, card.Label, "labels", card.Alignment);
            WriteNumberFormatObject(writer, "values", card.NumberFormat);
            WriteBoxObjects(writer, card.Box);
        }
        if (visual.VisualType == "table" && authoring.Table is { } table)
        {
            WriteTitleObject(writer, table.Title, table.Header, "columnHeaders", null);
            WriteTitleObject(writer, table.Subtitle, table.Row, "values", null, table.NumberFormat);
            if (table.AlternateRowColor is not null) WriteColorObject(writer, "alternateRows", table.AlternateRowColor.Hex);
        }
        if (visual.VisualType is "clusteredColumnChart" or "lineChart" or "barChart" or "pieChart" && authoring.Chart is { } chart)
        {
            WriteTitleObject(writer, chart.Title ?? authoring.Axis?.Title, null, "title", null);
            var axisLabels = chart.AxisLabels ?? authoring.Axis?.Visible;
            var legendVisible = chart.LegendVisible ?? authoring.Legend?.Visible;
            if (axisLabels is not null) WriteBooleanObject(writer, "categoryAxisLabels", axisLabels.Value);
            if (legendVisible is not null) WriteBooleanObject(writer, "legend", legendVisible.Value);
            if (chart.Background is not null) WriteColorObject(writer, "background", chart.Background.Hex);
            var colors = chart.Colors is { Count: > 0 }
                ? chart.Colors
                : authoring.ConditionalFormatting is { } conditional
                    ? [conditional.Color]
                    : null;
            if (colors is { Count: > 0 }) WriteColorObjects(writer, "dataColors", colors);
        }
        if (visual.VisualType == "slicer" && authoring.Slicer is { } slicer)
        {
            WriteTitleObject(writer, slicer.Title, slicer.Label, "title", null);
        }
        // V5 axis, legend, tooltip, template, and conditional-formatting data are
        // contract-level authoring inputs. Only the existing schema-safe objects
        // above are emitted into PBIR; arbitrary custom visual-container objects
        // are intentionally not introduced.
        writer.WriteEndObject();
    }

    private static void WriteTitleObject(Utf8JsonWriter writer, string? text, LocalPbirGenerationTextStyle? style, string objectName, LocalPbirGenerationTextAlignment? alignment, string? numberFormat = null)
    {
        if (text is null && style is null && alignment is null && numberFormat is null) return;
        writer.WritePropertyName(objectName);
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("properties");
        writer.WriteStartObject();
        if (text is not null) writer.WriteString("text", text);
        WriteTextStyle(writer, style, alignment);
        if (numberFormat is not null) writer.WriteString("formatString", numberFormat);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndArray();
    }

    private static void WriteTextStyle(Utf8JsonWriter writer, LocalPbirGenerationTextStyle? style, LocalPbirGenerationTextAlignment? alignment)
    {
        if (style is null && alignment is null) return;
        if (style?.FontFamily is not null) writer.WriteString("fontFamily", style.FontFamily);
        if (style?.FontSize is not null) writer.WriteNumber("fontSize", style.FontSize.Value);
        if (style?.FontWeight is not null) writer.WriteBoolean("bold", style.FontWeight == LocalPbirGenerationFontWeight.Bold);
        if (style?.Color is not null) writer.WriteString("fontColor", style.Color.Hex);
        if (style?.Alignment is not null) writer.WriteString("alignment", style.Alignment.Value.ToString());
        if (alignment is not null) writer.WriteString("alignment", alignment.Value.ToString());
    }

    private static void WriteNumberFormatObject(Utf8JsonWriter writer, string objectName, string? format)
    {
        if (format is null) return;
        writer.WritePropertyName(objectName);
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("properties");
        writer.WriteStartObject();
        writer.WriteString("formatString", format);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndArray();
    }

    private static void WriteColorObject(Utf8JsonWriter writer, string objectName, string color)
    {
        writer.WritePropertyName(objectName);
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("properties");
        writer.WriteStartObject();
        writer.WriteString("color", color);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndArray();
    }

    private static void WriteColorObjects(Utf8JsonWriter writer, string objectName, IReadOnlyList<LocalPbirGenerationColor> colors)
    {
        writer.WritePropertyName(objectName);
        writer.WriteStartArray();
        foreach (var color in colors)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            writer.WriteString("color", color.Hex);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteBooleanObject(Utf8JsonWriter writer, string objectName, bool value)
    {
        writer.WritePropertyName(objectName);
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("properties");
        writer.WriteStartObject();
        writer.WriteBoolean("show", value);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndArray();
    }

    private static void WriteBoxObjects(Utf8JsonWriter writer, LocalPbirGenerationBoxStyle? box)
    {
        if (box?.Background is not null) WriteColorObject(writer, "background", box.Background.Hex);
        if (box?.BorderColor is not null) WriteColorObject(writer, "border", box.BorderColor.Hex);
        if (box?.Padding is not null)
        {
            writer.WritePropertyName("padding");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            writer.WriteNumber("top", box.Padding.Top);
            writer.WriteNumber("right", box.Padding.Right);
            writer.WriteNumber("bottom", box.Padding.Bottom);
            writer.WriteNumber("left", box.Padding.Left);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndArray();
        }
    }

    private static void WriteProjection(
        Utf8JsonWriter writer,
        PbirRoleProjectionBinding projection,
        PbirSemanticModelInventoryEntry entry)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("field");
        writer.WriteStartObject();
        writer.WritePropertyName(entry.Kind == PbirSemanticModelEntryKind.Measure ? "Measure" : "Column");
        writer.WriteStartObject();
        writer.WritePropertyName("Expression");
        writer.WriteStartObject();
        writer.WritePropertyName("SourceRef");
        writer.WriteStartObject();
        writer.WriteString("Entity", entry.Entity);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteString("Property", entry.Property);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteString("queryRef", projection.QueryRef);
        writer.WriteString("nativeQueryRef", projection.NativeQueryRef);
        writer.WriteEndObject();
    }

    private static IReadOnlyList<string> GetRoleOrder(string visualType, LocalPbirGenerationVisualAuthoring? authoring)
    {
        IReadOnlyList<string> roles = visualType switch
        {
            "card" => ["Fields"],
            "table" => ["Values"],
            "clusteredColumnChart" or "barChart" or "pieChart" => ["Category", "Y"],
            "lineChart" => ["Category", "Y", "Series"],
            "slicer" => ["Category"],
            _ => throw new InvalidOperationException($"Unsupported visual type: {visualType}")
        };
        return roles;
    }

    private static int ParseSlot(PbirIntermediateRepresentationVisual visual)
    {
        var prefix = $"page:{visual.PageId}/slot:";
        return int.Parse(visual.Placement[prefix.Length..]);
    }

    private string ComputeFileSetHash(IReadOnlyList<PbirDeployableGeneratedFile> files)
    {
        var content = string.Join(
            "\n",
            files.Select(file => $"{file.RelativePath}\n{file.ByteLength}\n{file.HashSha256}"));
        return _canonicalJson.ComputeSha256(content);
    }

    private static PbirDeployableSerializerState Rejected(
        PbirDeployableSerializerReadinessState readiness,
        PbirDeployableDiagnostics diagnostics)
    {
        return new PbirDeployableSerializerState(
            Artifact: null,
            Manifest: null,
            Validation: new PbirDeployableValidation(
                SchemaVersion: PbirDeployableValidationContract.SchemaVersionV1,
                IsValid: false,
                ValidatedFileCount: 0,
                SchemaContractResults: [],
                StructuralValidationResults: [],
                CrossReferenceValidationResults: [],
                HashValidationResults: []),
            Readiness: readiness,
            Diagnostics: diagnostics);
    }
}
