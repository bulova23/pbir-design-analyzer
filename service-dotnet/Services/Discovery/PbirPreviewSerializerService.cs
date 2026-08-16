using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirPreviewSerializerService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly PbirPreviewSerializerSafetyGate _safetyGate;
    private readonly PbirPreviewSerializerValidator _validator;

    internal PbirPreviewSerializerService()
        : this(new PbirPreviewSerializerSafetyGate(), new PbirPreviewSerializerValidator())
    {
    }

    internal PbirPreviewSerializerService(
        PbirPreviewSerializerSafetyGate safetyGate,
        PbirPreviewSerializerValidator validator)
    {
        _safetyGate = safetyGate;
        _validator = validator;
    }

    internal PbirPreviewSerializerState CreatePreviewArtifacts(
        PbirIntermediateRepresentationState irState,
        PbirSerializerRequest serializerRequest,
        PbirPreviewSerializerOptions options,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(irState);
        ArgumentNullException.ThrowIfNull(serializerRequest);
        ArgumentNullException.ThrowIfNull(options);

        var safety = _safetyGate.Validate(irState, serializerRequest, options);
        if (!safety.IsAllowed || irState.Ir is null)
        {
            return new PbirPreviewSerializerState(
                Output: null,
                Manifest: null,
                Safety: safety,
                Validation: new PbirPreviewSerializerValidationResult(
                    new PbirPreviewSerializerValidationDiagnostics(
                        MissingRequiredSections: irState.Ir is null ? ["pbirIr"] : [],
                        InvalidReferences: [],
                        UnsupportedOutputTypes: [],
                        LineageViolations: [],
                        HashViolations: [],
                        BoundaryViolations: safety.Reasons)),
                Diagnostics: new PbirPreviewSerializerDiagnostics(
                    SafetyRejections: safety.Reasons,
                    BoundaryViolations: safety.Reasons),
                Readiness: PbirPreviewSerializerReadinessState.Rejected);
        }

        var ir = irState.Ir;
        var sourceReferences = new PbirPreviewSourceReferences(
            PbirIrRef: ir.Metadata.IrId,
            PbirIrSchemaVersion: ir.Metadata.SchemaVersion,
            PbirIrContentHash: ir.Hashes.ContentHash,
            SerializerRequestRef: serializerRequest.RequestId);
        var generatedFiles = CreateGeneratedFiles(ir);
        var inputHash = ComputeSha256(Serialize(new
        {
            ir,
            serializerRequest = serializerRequest with
            {
                SerializerImplementationAvailable = false
            },
            outputTypes = options.OutputTypes.OrderBy(outputType => outputType.ToString(), StringComparer.Ordinal).ToArray()
        }));
        var fileSetHash = PbirPreviewSerializerValidator.ComputeFileSetHash(generatedFiles);
        var artifactId = $"pbirPreviewArtifact:{ir.Metadata.IrId}";
        var manifestId = $"pbirPreviewManifest:{ir.Metadata.IrId}";
        var outputHash = ComputeSha256(Serialize(new
        {
            schemaVersion = PbirPreviewArtifactContract.SchemaVersionV1,
            artifactId,
            sourceReferences,
            generatedFiles = generatedFiles.Select(file => new
            {
                file.RelativePath,
                file.ContentType,
                file.Purpose,
                file.OutputType,
                file.ByteLength,
                file.HashSha256
            }).ToArray(),
            inputHash,
            fileSetHash
        }));
        var output = new PbirPreviewArtifact(
            SchemaVersion: PbirPreviewArtifactContract.SchemaVersionV1,
            Metadata: new PbirPreviewArtifactMetadata(
                ArtifactId: artifactId,
                GeneratedUtc: generatedUtc.UtcDateTime,
                OutputRoot: options.OutputRoot,
                LocalOutputOnly: true),
            SourceReferences: sourceReferences,
            GeneratedFiles: generatedFiles,
            Hashes: new PbirPreviewArtifactHashes(
                InputHash: inputHash,
                FileSetHash: fileSetHash,
                OutputHash: outputHash));
        var manifestReferences = generatedFiles
            .Select(file => new PbirPreviewGeneratedFileReference(
                RelativePath: file.RelativePath,
                ContentType: file.ContentType,
                Purpose: file.Purpose,
                OutputType: file.OutputType,
                ByteLength: file.ByteLength,
                HashSha256: file.HashSha256))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
        var manifestLineage = new PbirPreviewLineage(
            UpstreamLineage: ir.Lineage.UpstreamLineage
                .OrderBy(entry => entry.Stage, StringComparer.Ordinal)
                .ThenBy(entry => entry.ReferenceId, StringComparer.Ordinal)
                .ThenBy(entry => entry.Label, StringComparer.Ordinal)
                .ToArray(),
            ImmutableLineage: ir.Lineage.ImmutableLineage
                .Append(serializerRequest.RequestId)
                .Append(artifactId)
                .Append(manifestId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reference => reference, StringComparer.Ordinal)
                .ToArray());
        var manifestWithoutHash = new PbirPreviewManifest(
            SchemaVersion: PbirPreviewManifestContract.SchemaVersionV1,
            Metadata: new PbirPreviewManifestMetadata(
                ManifestId: manifestId,
                GeneratedUtc: generatedUtc.UtcDateTime),
            SourceReferences: sourceReferences,
            GeneratedFiles: manifestReferences,
            Lineage: manifestLineage,
            Warnings: ["Preview artifacts are local human-review artifacts only."],
            UnsupportedSections:
            [
                "Deployable PBIR serialization remains unsupported.",
                "report.json output remains unsupported.",
                "definition.pbir output remains unsupported.",
                "Microsoft Skills execution remains unsupported.",
                "Provider, API, CLI, and deployment execution remain unsupported."
            ],
            Hashes: new PbirPreviewManifestHashes(
                InputHash: inputHash,
                FileSetHash: fileSetHash,
                ManifestHash: string.Empty));
        var manifest = manifestWithoutHash with
        {
            Hashes = manifestWithoutHash.Hashes with
            {
                ManifestHash = PbirPreviewSerializerValidator.ComputeManifestHash(manifestWithoutHash)
            }
        };
        var validation = _validator.Validate(output, manifest, irState, serializerRequest);

        return new PbirPreviewSerializerState(
            Output: output,
            Manifest: manifest,
            Safety: safety,
            Validation: validation,
            Diagnostics: validation.IsValid
                ? PbirPreviewSerializerDiagnostics.Empty
                : new PbirPreviewSerializerDiagnostics(
                    SafetyRejections: [],
                    BoundaryViolations: validation.Diagnostics.BoundaryViolations),
            Readiness: validation.IsValid
                ? PbirPreviewSerializerReadinessState.Generated
                : PbirPreviewSerializerReadinessState.Rejected);
    }

    private static IReadOnlyList<PbirPreviewGeneratedFile> CreateGeneratedFiles(PbirIntermediateRepresentation ir)
    {
        var files = new[]
        {
            CreateFile(
                "pbir-preview-artifact/v1/report-preview.md",
                "text/markdown",
                "Human-readable PBIR IR preview",
                PbirPreviewOutputType.Markdown,
                CreateMarkdownPreview(ir)),
            CreateFile(
                "pbir-preview-artifact/v1/report-preview.json",
                "application/json",
                "Structured PBIR IR preview",
                PbirPreviewOutputType.Json,
                CreateJsonPreview(ir))
        };

        return files
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static PbirPreviewGeneratedFile CreateFile(
        string relativePath,
        string contentType,
        string purpose,
        PbirPreviewOutputType outputType,
        string content)
    {
        return new PbirPreviewGeneratedFile(
            RelativePath: relativePath,
            ContentType: contentType,
            Purpose: purpose,
            OutputType: outputType,
            Content: content,
            ByteLength: Encoding.UTF8.GetByteCount(content),
            HashSha256: ComputeSha256(content));
    }

    private static string CreateMarkdownPreview(PbirIntermediateRepresentation ir)
    {
        var lines = new List<string>
        {
            "# PBIR Preview",
            string.Empty,
            $"PBIR IR: {ir.Metadata.IrId}",
            $"Schema: {ir.Metadata.SchemaVersion}",
            $"Content hash: {ir.Hashes.ContentHash}",
            string.Empty,
            "## Pages"
        };

        lines.AddRange(ir.Pages
            .OrderBy(page => page.Order)
            .ThenBy(page => page.PageId, StringComparer.Ordinal)
            .Select(page => $"- {page.Order}. {page.PageId}: {page.IntendedPurpose} ({page.NavigationBehavior})"));
        lines.Add(string.Empty);
        lines.Add("## Visual Layout Summary");
        lines.AddRange(ir.Layout.Containers
            .OrderBy(container => container.ContainerId, StringComparer.Ordinal)
            .Select(container =>
            {
                var visualRefs = string.Join(", ", container.VisualRefs.OrderBy(visualRef => visualRef, StringComparer.Ordinal));

                return $"- {container.PageId}: {container.Purpose}; visuals: {visualRefs}";
            }));
        lines.AddRange(ir.Visuals
            .OrderBy(visual => visual.PageId, StringComparer.Ordinal)
            .ThenBy(visual => visual.VisualId, StringComparer.Ordinal)
            .Select(visual => $"  - {visual.VisualId}: {visual.VisualType} at {visual.Placement}; intent: {visual.SemanticIntent}"));
        lines.Add(string.Empty);
        lines.Add("## Semantic Binding Summary");
        lines.AddRange(ir.Semantics
            .OrderBy(semantic => semantic.SemanticId, StringComparer.Ordinal)
            .Select(semantic =>
            {
                var measures = string.Join(", ", semantic.Measures.OrderBy(measure => measure, StringComparer.Ordinal));
                var dimensions = string.Join(", ", semantic.Dimensions.OrderBy(dimension => dimension, StringComparer.Ordinal));
                var filters = string.Join(", ", semantic.Filters.OrderBy(filter => filter, StringComparer.Ordinal));

                return $"- {semantic.SemanticId}: measures [{measures}], dimensions [{dimensions}], filters [{filters}], drill {semantic.DrillBehavior}";
            }));
        lines.Add(string.Empty);
        lines.Add("## Navigation Summary");
        lines.Add($"Landing page: {ir.Navigation.LandingPage}");
        lines.AddRange(ir.Navigation.PageTransitions
            .OrderBy(transition => transition.FromPageId, StringComparer.Ordinal)
            .ThenBy(transition => transition.ToPageId, StringComparer.Ordinal)
            .Select(transition => $"- {transition.FromPageId} -> {transition.ToPageId}: {transition.Transition}"));
        lines.Add(string.Empty);
        lines.Add("## Unsupported Sections");
        lines.Add("- Deployable PBIR serialization remains unsupported.");
        lines.Add("- report.json and definition.pbir output remain unsupported.");

        return string.Join("\n", lines) + "\n";
    }

    private static string CreateJsonPreview(PbirIntermediateRepresentation ir)
    {
        return Serialize(new
        {
            schemaVersion = PbirPreviewArtifactContract.SchemaVersionV1,
            pbirIrRef = ir.Metadata.IrId,
            pbirIrContentHash = ir.Hashes.ContentHash,
            pages = ir.Pages
                .OrderBy(page => page.Order)
                .ThenBy(page => page.PageId, StringComparer.Ordinal)
                .Select(page => new
                {
                    page.PageId,
                    page.PageIdentity,
                    page.NavigationBehavior,
                    page.IntendedPurpose,
                    page.Order
                })
                .ToArray(),
            visualLayoutSummary = ir.Visuals
                .OrderBy(visual => visual.PageId, StringComparer.Ordinal)
                .ThenBy(visual => visual.VisualId, StringComparer.Ordinal)
                .Select(visual => new
                {
                    visual.VisualId,
                    visual.PageId,
                    visual.VisualType,
                    visual.Placement,
                    visual.SemanticIntent,
                    visual.InteractionModel,
                    visual.Order
                })
                .ToArray(),
            semanticBindingSummary = ir.Semantics
                .OrderBy(semantic => semantic.SemanticId, StringComparer.Ordinal)
                .Select(semantic => new
                {
                    semantic.SemanticId,
                    semantic.PageId,
                    semantic.Measures,
                    semantic.Dimensions,
                    semantic.Kpis,
                    semantic.Filters,
                    semantic.DrillBehavior,
                    semantic.Relationships
                })
                .ToArray(),
            navigationSummary = new
            {
                ir.Navigation.LandingPage,
                pageTransitions = ir.Navigation.PageTransitions
                    .OrderBy(transition => transition.FromPageId, StringComparer.Ordinal)
                    .ThenBy(transition => transition.ToPageId, StringComparer.Ordinal)
                    .ThenBy(transition => transition.Transition, StringComparer.Ordinal)
                    .ToArray(),
                bookmarks = ir.Navigation.Bookmarks
                    .OrderBy(bookmark => bookmark, StringComparer.Ordinal)
                    .ToArray(),
                drillPaths = ir.Navigation.DrillPaths.ToArray()
            },
            unsupportedSections = new[]
            {
                "Deployable PBIR serialization remains unsupported.",
                "report.json output remains unsupported.",
                "definition.pbir output remains unsupported."
            }
        });
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
