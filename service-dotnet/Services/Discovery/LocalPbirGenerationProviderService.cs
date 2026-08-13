using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services.Discovery.Models;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class LocalPbirGenerationProviderService
{
    private static readonly string[] SupportedDatasetExtensions = [".SemanticModel"];
    private readonly PbirDeployableSerializerService _serializer;

    internal LocalPbirGenerationProviderService()
        : this(new PbirDeployableSerializerService())
    {
    }

    internal LocalPbirGenerationProviderService(PbirDeployableSerializerService serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    internal LocalPbirGenerationResult Generate(LocalPbirGenerationRequest? request)
    {
        var diagnostics = Validate(request);
        if (request is null || diagnostics.Count > 0)
        {
            return Rejected(request?.RequestId, diagnostics);
        }

        try
        {
            var inputs = CreateInputs(request);
            var serialized = _serializer.CreateArtifacts(
                inputs.IrState,
                inputs.SerializerRequest,
                inputs.DeployableSerializerRequest);
            if (serialized.Artifact is null || serialized.Manifest is null ||
                serialized.Readiness != PbirDeployableSerializerReadinessState.Serialized ||
                !serialized.Validation.IsValid || serialized.Diagnostics.HasFailures)
            {
                return Rejected(
                    request.RequestId,
                    ConvertDiagnostics(serialized.Diagnostics),
                    serialized);
            }

            return new LocalPbirGenerationResult(
                LocalPbirGenerationResultContract.SchemaVersionV1,
                request.RequestId,
                LocalPbirGenerationReadinessState.Generated,
                serialized.Artifact,
                serialized.Manifest,
                serialized.Validation,
                null,
                null,
                []);
        }
        catch (ArgumentException exception)
        {
            return Rejected(
                request.RequestId,
                [new("PBIR36-REQUEST-001", "request", exception.Message)]);
        }
        catch (InvalidOperationException exception)
        {
            return Rejected(
                request.RequestId,
                [new("PBIR36-GENERATION-001", "artifact", exception.Message)]);
        }
    }

    internal async Task<LocalPbirGenerationResult> GenerateAndVerifyAsync(
        LocalPbirGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var generated = Generate(request);
        if (generated.Artifact is null || generated.Manifest is null ||
            generated.Readiness != LocalPbirGenerationReadinessState.Generated)
        {
            return generated;
        }

        try
        {
            var inputs = CreateInputs(request);
            var orchestration = new PbirMaterializationOrchestrationService();
            var orchestrationInput = new PbirMaterializationOrchestrationInput(
                inputs.IrState,
                inputs.SerializerRequest,
                inputs.DeployableSerializerRequest,
                request.OutputBaseDirectory,
                request.TargetDirectoryName);
            var preview = orchestration.Preview(
                new PbirMaterializationOrchestrationPreviewRequest(
                    PbirMaterializationOrchestrationPreviewRequestContract.SchemaVersionV1,
                    $"{request.RequestId}-preview",
                    "preview",
                    orchestrationInput),
                cancellationToken);
            if (preview.ValidatedPreview is null ||
                preview.Outcome is not (PbirMaterializationOrchestrationOutcome.Absent or
                    PbirMaterializationOrchestrationOutcome.Empty or
                    PbirMaterializationOrchestrationOutcome.ExactMatch))
            {
                return WithMaterializationFailure(generated, preview);
            }

            var transactionId = $"phase36-{ComputeSha256(request.RequestId)[..24]}";
            var materialization = orchestration.Apply(
                new PbirMaterializationOrchestrationApplyRequest(
                    PbirMaterializationOrchestrationApplyRequestContract.SchemaVersionV1,
                    request.RequestId,
                    "apply",
                    orchestrationInput,
                    preview.ValidatedPreview,
                    transactionId,
                    true),
                cancellationToken);
            if (materialization.Outcome is not (PbirMaterializationOrchestrationOutcome.Applied or
                PbirMaterializationOrchestrationOutcome.ExactMatch))
            {
                return WithMaterializationFailure(generated, materialization);
            }

            var targetPath = Path.Combine(request.OutputBaseDirectory, request.TargetDirectoryName);
            var projectService = new PbirProjectService(NullLogger<PbirProjectService>.Instance);
            var location = projectService.TryGetReportLocation(targetPath);
            if (location is null)
            {
                return RoundTripFailure(
                    generated,
                    new("PBIR36-ROUNDTRIP-001", "artifact", "The generated PBIR report could not be resolved after materialization."));
            }

            var scoring = new PbirScoringService(
                projectService,
                NullLogger<PbirScoringService>.Instance);
            var score = await scoring.ScoreAsync(location.ProjectRootPath).ConfigureAwait(false);
            var pageCount = score.PageScores?.Count ?? 0;
            var visualCount = generated.Artifact.Files.Count(file =>
                file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal));
            if (pageCount != 1 || visualCount != 1)
            {
                return RoundTripFailure(
                    generated,
                    new("PBIR36-ROUNDTRIP-002", "roundTrip", "The analyzer did not observe exactly one generated page and visual."),
                    materialization);
            }

            return generated with
            {
                Readiness = LocalPbirGenerationReadinessState.RoundTripVerified,
                Materialization = materialization,
                RoundTrip = new LocalPbirGenerationRoundTrip(score, pageCount, visualCount)
            };
        }
        catch (OperationCanceledException)
        {
            return RoundTripFailure(
                generated,
                new("PBIR36-ROUNDTRIP-CANCELLED-001", "request", "The round-trip was cancelled safely."));
        }
        catch (IOException)
        {
            return RoundTripFailure(
                generated,
                new("PBIR36-ROUNDTRIP-IO-001", "destination", "The local round-trip failed safely."));
        }
    }

    private static IReadOnlyList<LocalPbirGenerationDiagnostic> Validate(LocalPbirGenerationRequest? request)
    {
        if (request is null)
        {
            return [new("PBIR36-REQUEST-001", "request", "The generation request is required.")];
        }

        var diagnostics = new List<LocalPbirGenerationDiagnostic>();
        if (request.SchemaVersion != LocalPbirGenerationRequestContract.SchemaVersionV1)
        {
            diagnostics.Add(new("PBIR36-REQUEST-SCHEMA-001", "schemaVersion", "The request schema version is unsupported."));
        }

        ValidateIdentifier(request.RequestId, "requestId", diagnostics);
        ValidateIdentifier(request.ReportName, "reportName", diagnostics);
        ValidateIdentifier(request.PageId, "pageId", diagnostics);
        ValidateIdentifier(request.VisualId, "visualId", diagnostics);
        ValidateIdentifier(request.MeasureToken, "measureToken", diagnostics);
        ValidateIdentifier(request.MeasureEntity, "measureEntity", diagnostics);
        ValidateIdentifier(request.MeasureProperty, "measureProperty", diagnostics);

        if (request.VisualType != LocalPbirGenerationProviderContract.SupportedVisualType)
        {
            diagnostics.Add(new("PBIR36-REQUEST-VISUAL-001", "visualType", "Only the card visual type is supported."));
        }

        if (!IsSafeDatasetPath(request.DatasetPath))
        {
            diagnostics.Add(new("PBIR36-REQUEST-PATH-001", "datasetPath", "The dataset path must be a safe relative SemanticModel path."));
        }

        if (!Path.IsPathFullyQualified(request.OutputBaseDirectory) ||
            !Directory.Exists(request.OutputBaseDirectory))
        {
            diagnostics.Add(new("PBIR36-REQUEST-OUTPUT-001", "outputBaseDirectory", "The output base must be an existing absolute directory."));
        }

        if (string.IsNullOrWhiteSpace(request.TargetDirectoryName) ||
            request.TargetDirectoryName is "." or ".." ||
            request.TargetDirectoryName.Contains('/') ||
            request.TargetDirectoryName.Contains('\\') ||
            request.TargetDirectoryName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            diagnostics.Add(new("PBIR36-REQUEST-TARGET-001", "targetDirectoryName", "The target directory name must be one safe path segment."));
        }

        return diagnostics;
    }

    private static void ValidateIdentifier(
        string? value,
        string field,
        ICollection<LocalPbirGenerationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64 ||
            value.Any(character => !(char.IsLetterOrDigit(character) || character is '.' or '_' or '-' or ':')))
        {
            diagnostics.Add(new("PBIR36-REQUEST-ID-001", field, "The identifier is missing or unsafe."));
        }
    }

    private static bool IsSafeDatasetPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathFullyQualified(path) ||
            (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':') ||
            path.Contains('\\') || path.Contains("//", StringComparison.Ordinal) ||
            path.Split('/').Any(segment => segment is "." or ".."))
        {
            return false;
        }

        return SupportedDatasetExtensions.Any(extension => path.EndsWith(extension, StringComparison.Ordinal));
    }

    private static GenerationInputs CreateInputs(LocalPbirGenerationRequest request)
    {
        var irId = $"pbirIr:{request.RequestId}";
        var manifestRef = $"localPbirGenerationRequest:{request.RequestId}";
        var specificationRef = $"localPbirGenerationSpecification:{request.RequestId}";
        var visualId = $"visual:{request.RequestId}:{request.VisualId}";
        var entryId = $"measure:{request.MeasureEntity}.{request.MeasureProperty}";
        var inventory = new PbirSemanticModelInventory(
            PbirSemanticModelInventoryContract.SchemaVersionV1,
            $"modelInventory:{request.RequestId}",
            [new(
                entryId,
                request.MeasureToken,
                request.MeasureEntity,
                request.MeasureProperty,
                PbirSemanticModelEntryKind.Measure)]);
        var page = new PbirIntermediateRepresentationPage(
            request.PageId,
            $"page:{request.PageId}",
            "pageTab",
            request.ReportName,
            1);
        var visual = new PbirIntermediateRepresentationVisual(
            visualId,
            request.PageId,
            request.VisualType,
            $"page:{request.PageId}/slot:1",
            request.MeasureToken,
            ["none"],
            1);
        var semantic = new PbirIntermediateRepresentationSemantic(
            $"semantic:{request.RequestId}",
            request.PageId,
            [request.MeasureToken],
            [],
            [request.MeasureToken],
            [],
            "none",
            [$"visual:[{visualId}]->semantic:[{request.MeasureToken}]"]);
        var navigation = new PbirIntermediateRepresentationNavigation(
            request.PageId,
            [],
            [$"page:{request.PageId}", $"landing:{request.PageId}"],
            []);
        var layout = new PbirIntermediateRepresentationLayout(
            [new(
                $"container:{request.RequestId}",
                request.PageId,
                "deterministic card layout",
                [visualId])],
            ["standard-8px-grid"],
            ["deterministic-grid", "visual-placement-preserved"],
            ["preserve-page-order", "preserve-visual-intent", "allow-future-serializer-layout-adaptation"]);
        var successCriteria = new PbirIntermediateRepresentationSuccessCriteria(
            [$"{request.MeasureToken} is visible."],
            [request.PageDisplayName],
            [$"{request.MeasureToken} is bound explicitly."]);
        var lineage = new PbirIntermediateRepresentationLineage(
            [new("localPbirGeneration", manifestRef, "Phase 36 local generation request")],
            [manifestRef, specificationRef, irId]);
        var metadata = new PbirIntermediateRepresentationMetadata(
            irId,
            PbirIntermediateRepresentationContract.SchemaVersionV1,
            request.GeneratedUtc);
        var references = new PbirIntermediateRepresentationReferences(manifestRef, specificationRef);
        var contentHash = PbirIntermediateRepresentationIntegrity.ComputeContentHash(
            metadata,
            references,
            [page],
            [visual],
            [semantic],
            navigation,
            layout,
            successCriteria,
            lineage);
        var ir = new PbirIntermediateRepresentation(
            metadata,
            references,
            [page],
            [visual],
            [semantic],
            navigation,
            layout,
            successCriteria,
            lineage,
            new(
                ComputeSha256(JsonSerializer.Serialize(request)),
                contentHash,
                ComputeSha256(JsonSerializer.Serialize(lineage.ImmutableLineage))));
        var irState = new PbirIntermediateRepresentationState(
            ir,
            new PbirIntermediateRepresentationValidationResult(
                PbirIntermediateRepresentationValidationDiagnostics.Empty),
            PbirIntermediateRepresentationReadinessState.ReadyForSerializer);
        var serializerRequest = new PbirSerializerRequest(
            PbirSerializerRequestContract.SchemaVersionV1,
            $"pbirSerializerRequest:{irId}",
            irId,
            ir.Metadata.SchemaVersion,
            contentHash,
            true,
            false,
            false,
            false);
        var inventoryHash = new PbirDeployableSerializerCanonicalJson().ComputeSha256(
            new PbirDeployableSerializerCanonicalJson().SerializeSemanticModelInventory(inventory));
        var deployableRequest = new PbirDeployableSerializerRequest(
            PbirDeployableSerializerRequestContract.SchemaVersionV1,
            $"pbirDeployableSerializerRequest:{request.RequestId}",
            serializerRequest.RequestId,
            serializerRequest.SchemaVersion,
            irId,
            ir.Metadata.SchemaVersion,
            contentHash,
            "modernPbir",
            PbirDeployableSchemaLock.DefinitionPropertiesSchemaVersion,
            PbirDeployableSchemaLock.DefinitionSchemaVersion,
            new(new(request.DatasetPath)),
            "modern-grid-1280x720/v1",
            inventory,
            inventory.InventoryRef,
            inventoryHash,
            [new(
                visualId,
                [new(
                    "Fields",
                    1,
                    request.MeasureToken,
                    entryId,
                    $"{request.MeasureEntity}.{request.MeasureProperty}",
                    request.MeasureProperty,
                    "none",
                    null,
                    null)])],
            PbirDeployableExecutionPolicy.NoAuthority);
        return new GenerationInputs(irState, serializerRequest, deployableRequest);
    }

    private static string ComputeSha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static IReadOnlyList<LocalPbirGenerationDiagnostic> ConvertDiagnostics(PbirDeployableDiagnostics diagnostics)
    {
        var converted = new List<LocalPbirGenerationDiagnostic>();
        Add(converted, diagnostics.MissingRequiredFields);
        Add(converted, diagnostics.UnsupportedSchemaVersions);
        Add(converted, diagnostics.UnsupportedVisualTypes);
        Add(converted, diagnostics.IncompleteSemanticBindings);
        Add(converted, diagnostics.InvalidModelReferences);
        Add(converted, diagnostics.InvalidPaths);
        Add(converted, diagnostics.DuplicateIdentities);
        Add(converted, diagnostics.InvalidLayoutDefinitions);
        Add(converted, diagnostics.InvalidNavigationDefinitions);
        Add(converted, diagnostics.SchemaIncompatibilities);
        Add(converted, diagnostics.HashViolations);
        Add(converted, diagnostics.LineageViolations);
        Add(converted, diagnostics.BoundaryViolations);
        Add(converted, diagnostics.Warnings);
        Add(converted, diagnostics.UnsupportedSections);
        return converted;

        static void Add(
            ICollection<LocalPbirGenerationDiagnostic> target,
            IEnumerable<PbirDeployableDiagnostic> source)
        {
            foreach (var diagnostic in source)
            {
                target.Add(new(diagnostic.Code, diagnostic.Path, diagnostic.Message));
            }
        }
    }

    private static LocalPbirGenerationResult Rejected(
        string? requestId,
        IReadOnlyList<LocalPbirGenerationDiagnostic> diagnostics,
        PbirDeployableSerializerState? serialized = null) =>
        new(
            LocalPbirGenerationResultContract.SchemaVersionV1,
            requestId ?? string.Empty,
            LocalPbirGenerationReadinessState.Rejected,
            serialized?.Artifact,
            serialized?.Manifest,
            serialized?.Validation,
            null,
            null,
            diagnostics);

    private static LocalPbirGenerationResult WithMaterializationFailure(
        LocalPbirGenerationResult generated,
        PbirMaterializationOrchestrationResult materialization) =>
        generated with
        {
            Readiness = LocalPbirGenerationReadinessState.Rejected,
            Materialization = materialization,
            Diagnostics =
            [new(
                "PBIR36-MATERIALIZATION-001",
                "destination",
                "Phase 31 did not materialize the generated artifact.")]
        };

    private static LocalPbirGenerationResult RoundTripFailure(
        LocalPbirGenerationResult generated,
        LocalPbirGenerationDiagnostic diagnostic,
        PbirMaterializationOrchestrationResult? materialization = null) =>
        generated with
        {
            Readiness = LocalPbirGenerationReadinessState.Rejected,
            Materialization = materialization ?? generated.Materialization,
            Diagnostics = [diagnostic]
        };

    private sealed record GenerationInputs(
        PbirIntermediateRepresentationState IrState,
        PbirSerializerRequest SerializerRequest,
        PbirDeployableSerializerRequest DeployableSerializerRequest);
}
