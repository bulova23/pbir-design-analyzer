using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal interface IReferenceGenerationProvider
{
    ReferenceGenerationState CreateReferenceOutput(
        GenerationManifestState manifestState,
        ArchitectureCertificationState certificationState,
        PbirGenerationSpecificationState specificationState,
        ReferenceGenerationOptions options,
        DateTimeOffset generatedUtc);
}

internal sealed class ReferencePbirGenerationService : IReferenceGenerationProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly ReferenceGenerationSafetyGate _safetyGate;
    private readonly PbirIntermediateRepresentationService _irService;

    internal ReferencePbirGenerationService()
        : this(new ReferenceGenerationSafetyGate(), new PbirIntermediateRepresentationService())
    {
    }

    internal ReferencePbirGenerationService(
        ReferenceGenerationSafetyGate safetyGate,
        PbirIntermediateRepresentationService irService)
    {
        _safetyGate = safetyGate;
        _irService = irService;
    }

    public ReferenceGenerationState CreateReferenceOutput(
        GenerationManifestState manifestState,
        ArchitectureCertificationState certificationState,
        PbirGenerationSpecificationState specificationState,
        ReferenceGenerationOptions options,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(manifestState);
        ArgumentNullException.ThrowIfNull(certificationState);
        ArgumentNullException.ThrowIfNull(specificationState);
        ArgumentNullException.ThrowIfNull(options);

        var safety = _safetyGate.Validate(manifestState, certificationState, specificationState, options);
        if (!safety.IsAllowed)
        {
            return new ReferenceGenerationState(
                Output: null,
                Safety: safety,
                Diagnostics: new ReferenceGenerationDiagnostics(
                    SafetyRejections: safety.Reasons,
                    BoundaryViolations: safety.Reasons),
                Readiness: ReferenceGenerationReadinessState.Rejected);
        }

        var manifest = manifestState.Manifest!;
        var specification = specificationState.Specification!;
        var certification = certificationState.Certification!;
        var irState = _irService.CreateIntermediateRepresentation(manifestState, specificationState, generatedUtc);
        if (irState.Ir is null || irState.Readiness != PbirIntermediateRepresentationReadinessState.ReadyForSerializer)
        {
            var reasons = irState.Validation.Diagnostics.MissingRequiredSections
                .Concat(irState.Validation.Diagnostics.MissingRequiredFields)
                .Concat(irState.Validation.Diagnostics.InvalidReferences)
                .Concat(irState.Validation.Diagnostics.InvalidNavigationDefinitions)
                .Concat(irState.Validation.Diagnostics.InvalidSemanticDefinitions)
                .Concat(irState.Validation.Diagnostics.InvalidLayoutDefinitions)
                .Concat(irState.Validation.Diagnostics.UnsupportedSchemaVersions)
                .Concat(irState.Validation.Diagnostics.BoundaryViolations)
                .DefaultIfEmpty("canonical PBIR IR must be readyForSerializer.")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray();

            return new ReferenceGenerationState(
                Output: null,
                Safety: new ReferenceGenerationSafetyGateResult(IsAllowed: false, Reasons: reasons),
                Diagnostics: new ReferenceGenerationDiagnostics(
                    SafetyRejections: reasons,
                    BoundaryViolations: reasons),
                Readiness: ReferenceGenerationReadinessState.Rejected);
        }

        var ir = irState.Ir;
        var outputId = $"referenceGenerationOutput:{manifest.Metadata.ManifestId}";
        var sourceReferences = new ReferenceGenerationSourceReferences(
            GenerationManifestRef: manifest.Metadata.ManifestId,
            PbirGenerationSpecificationRef: specification.SpecificationId,
            ArchitectureCertificationRef: certification.CertificationId);
        var metadata = new ReferenceGenerationMetadata(
            OutputId: outputId,
            GeneratorSchemaVersion: ReferencePbirGeneratorContract.SchemaVersionV1,
            GenerationMode: "localDeterministicReference",
            GeneratedUtc: generatedUtc.UtcDateTime,
            DryRun: options.DryRun,
            LocalOutputOnly: options.LocalOutputOnly,
            DeploymentEnabled: false,
            ProviderInvocationEnabled: false,
            MicrosoftApiInvocationEnabled: false,
            CliInvocationEnabled: false);
        var canonicalIr = new ReferenceGenerationCanonicalIrSummary(
            PbirIrRef: ir.Metadata.IrId,
            SchemaVersion: ir.Metadata.SchemaVersion,
            InputHash: ir.Hashes.InputHash,
            ContentHash: ir.Hashes.ContentHash,
            LineageHash: ir.Hashes.LineageHash,
            ImmutableIrLineage: ir.Lineage.ImmutableLineage);
        var generatedFiles = CreateGeneratedFiles(manifest, specification, ir);
        var lineage = new ReferenceGenerationLineage(
            UpstreamLineage: manifest.Lineage.UpstreamLineage
                .OrderBy(entry => entry.Stage, StringComparer.Ordinal)
                .ThenBy(entry => entry.ReferenceId, StringComparer.Ordinal)
                .ThenBy(entry => entry.Label, StringComparer.Ordinal)
                .ToArray(),
            ImmutableLineage: CreateImmutableLineage(manifest, specification, certification, ir, outputId));
        var inputHash = ComputeInputHash(manifest, specification);
        var fileSetHash = ComputeFileSetHash(generatedFiles);
        var outputHash = ComputeSha256(Serialize(new
        {
            schemaVersion = ReferenceGenerationOutputContract.SchemaVersionV1,
            sourceReferences,
            canonicalIr,
            immutableLineage = lineage.ImmutableLineage,
            inputHash,
            fileSetHash
        }));
        var output = new ReferenceGenerationOutput(
            SchemaVersion: ReferenceGenerationOutputContract.SchemaVersionV1,
            Metadata: metadata,
            SourceReferences: sourceReferences,
            CanonicalIr: canonicalIr,
            GeneratedFiles: generatedFiles,
            Lineage: lineage,
            Hashes: new ReferenceGenerationHashes(
                InputHash: inputHash,
                FileSetHash: fileSetHash,
                OutputHash: outputHash));

        return new ReferenceGenerationState(
            Output: output,
            Safety: safety,
            Diagnostics: ReferenceGenerationDiagnostics.Empty,
            Readiness: ReferenceGenerationReadinessState.Generated);
    }

    private static IReadOnlyList<ReferenceGeneratedFile> CreateGeneratedFiles(
        GenerationManifest manifest,
        PbirGenerationSpecification specification,
        PbirIntermediateRepresentation ir)
    {
        var files = new[]
        {
            CreateFile(
                "reference-pbir-generator/v1/manifest-summary.json",
                "application/json",
                "Deterministic generation manifest summary",
                Serialize(new
                {
                    manifest.Metadata.ManifestId,
                    manifest.Metadata.SchemaVersion,
                    manifest.SourceReferences,
                    manifest.CapabilitySummary,
                    manifest.ReadinessSummary,
                    manifest.ApprovalSummary,
                    manifest.ExecutionConstraints
                })),
            CreateFile(
                "reference-pbir-generator/v1/canonical-pbir-ir.json",
                "application/json",
                "Canonical PBIR intermediate representation",
                Serialize(ir)),
            CreateFile(
                "reference-pbir-generator/v1/lineage.md",
                "text/markdown",
                "Immutable reference generation lineage",
                CreateLineageMarkdown(manifest, specification))
        };

        return files
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static ReferenceGeneratedFile CreateFile(
        string relativePath,
        string contentType,
        string purpose,
        string content)
    {
        return new ReferenceGeneratedFile(
            RelativePath: relativePath,
            ContentType: contentType,
            Purpose: purpose,
            Content: content,
            ByteLength: Encoding.UTF8.GetByteCount(content),
            HashSha256: ComputeSha256(content));
    }

    private static string CreateLineageMarkdown(
        GenerationManifest manifest,
        PbirGenerationSpecification specification)
    {
        var lines = new List<string>
        {
            "# Reference Generation Lineage",
            string.Empty,
            $"Generation manifest: {manifest.Metadata.ManifestId}",
            $"PBIR generation specification: {specification.SpecificationId}",
            $"Canonical PBIR IR: pbirIr:{manifest.Metadata.ManifestId}",
            string.Empty,
            "## Immutable References"
        };
        lines.AddRange(manifest.Lineage.ImmutableUpstreamLineage
            .Append(specification.SpecificationId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .Select(reference => $"- {reference}"));

        return string.Join("\n", lines) + "\n";
    }

    private static IReadOnlyList<string> CreateImmutableLineage(
        GenerationManifest manifest,
        PbirGenerationSpecification specification,
        ArchitectureCertification certification,
        PbirIntermediateRepresentation ir,
        string outputId)
    {
        return manifest.Lineage.ImmutableUpstreamLineage
            .Append(manifest.Metadata.ManifestId)
            .Append(specification.SpecificationId)
            .Append(certification.CertificationId)
            .Append(ir.Metadata.IrId)
            .Append(outputId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ComputeInputHash(GenerationManifest manifest, PbirGenerationSpecification specification)
    {
        return ComputeSha256(Serialize(new
        {
            manifest,
            specification
        }));
    }

    private static string ComputeFileSetHash(IReadOnlyList<ReferenceGeneratedFile> files)
    {
        var material = string.Join(
            "\n",
            files
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .Select(file => $"{file.RelativePath}:{file.HashSha256}:{file.ByteLength}"));

        return ComputeSha256(material);
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
