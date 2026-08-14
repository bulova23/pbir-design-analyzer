using System.Diagnostics;
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
                [])
            {
                GeneratedPageCount = 1,
                GeneratedVisualCount = 1
            };
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

    internal LocalPbirGenerationResult Generate(LocalPbirGenerationRequestV2? request)
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
                return Rejected(request.RequestId, ConvertDiagnostics(serialized.Diagnostics), serialized);
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
                [])
            {
                GeneratedPageCount = request.Pages.Count,
                GeneratedVisualCount = request.Visuals.Count
            };
        }
        catch (ArgumentException exception)
        {
            return Rejected(request.RequestId, [new("PBIR37-GENERATION-001", "request", exception.Message)]);
        }
        catch (InvalidOperationException exception)
        {
            return Rejected(request.RequestId, [new("PBIR37-GENERATION-002", "artifact", exception.Message)]);
        }
    }

    internal LocalPbirGenerationResult Generate(LocalPbirGenerationRequestV3? request)
    {
        var diagnostics = Validate(request);
        if (request is null || diagnostics.Count > 0)
        {
            return Rejected(request?.RequestId, diagnostics);
        }

        try
        {
            var inputs = CreateInputs(request);
            var serialized = _serializer.CreateArtifacts(inputs.IrState, inputs.SerializerRequest, inputs.DeployableSerializerRequest);
            if (serialized.Artifact is null || serialized.Manifest is null ||
                serialized.Readiness != PbirDeployableSerializerReadinessState.Serialized ||
                !serialized.Validation.IsValid || serialized.Diagnostics.HasFailures)
            {
                return Rejected(request.RequestId, ConvertDiagnostics(serialized.Diagnostics), serialized);
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
                [])
            {
                GeneratedPageCount = request.Pages.Count,
                GeneratedVisualCount = request.Visuals.Count
            };
        }
        catch (ArgumentException exception)
        {
            return Rejected(request.RequestId, [new("PBIR38-GENERATION-001", "request", exception.Message)]);
        }
        catch (InvalidOperationException exception)
        {
            return Rejected(request.RequestId, [new("PBIR38-GENERATION-002", "artifact", exception.Message)]);
        }
    }

    internal LocalPbirGenerationResult Generate(LocalPbirGenerationRequestV4? request)
    {
        var diagnostics = Validate(request);
        if (request is null || diagnostics.Count > 0)
        {
            return Rejected(request?.RequestId, diagnostics);
        }

        return Generate(ToV3(request));
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

    internal async Task<LocalPbirGenerationResult> GenerateAndVerifyAsync(
        LocalPbirGenerationRequestV2 request,
        CancellationToken cancellationToken = default)
    {
        var generationTimer = Stopwatch.StartNew();
        var generated = Generate(request);
        generationTimer.Stop();
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
            var materializationTimer = Stopwatch.StartNew();
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

            var transactionId = $"phase37-{ComputeSha256(request.RequestId)[..24]}";
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
            materializationTimer.Stop();

            var targetPath = Path.Combine(request.OutputBaseDirectory, request.TargetDirectoryName);
            var projectService = new PbirProjectService(NullLogger<PbirProjectService>.Instance);
            var location = projectService.TryGetReportLocation(targetPath);
            if (location is null)
            {
                return RoundTripFailure(generated, new("PBIR37-ROUNDTRIP-001", "artifact", "The generated PBIR report could not be resolved after materialization."));
            }

            var analyzerTimer = Stopwatch.StartNew();
            var scoring = new PbirScoringService(projectService, NullLogger<PbirScoringService>.Instance);
            var score = await scoring.ScoreAsync(location.ProjectRootPath).ConfigureAwait(false);
            analyzerTimer.Stop();
            var pageCount = score.PageScores?.Count ?? 0;
            if (pageCount != request.Pages.Count || generated.GeneratedVisualCount != request.Visuals.Count)
            {
                return RoundTripFailure(generated, new("PBIR37-ROUNDTRIP-002", "roundTrip", "The analyzer did not observe the requested generated page and visual counts."), materialization);
            }

            return generated with
            {
                Readiness = LocalPbirGenerationReadinessState.RoundTripVerified,
                Materialization = materialization,
                RoundTrip = new LocalPbirGenerationRoundTrip(score, pageCount, request.Visuals.Count),
                Performance = new LocalPbirGenerationPerformance(
                    generationTimer.ElapsedMilliseconds,
                    materializationTimer.ElapsedMilliseconds,
                    analyzerTimer.ElapsedMilliseconds)
            };
        }
        catch (OperationCanceledException)
        {
            return RoundTripFailure(generated, new("PBIR37-ROUNDTRIP-CANCELLED-001", "request", "The round-trip was cancelled safely."));
        }
        catch (IOException)
        {
            return RoundTripFailure(generated, new("PBIR37-ROUNDTRIP-IO-001", "destination", "The local round-trip failed safely."));
        }
    }

    internal async Task<LocalPbirGenerationResult> GenerateAndVerifyAsync(
        LocalPbirGenerationRequestV3 request,
        CancellationToken cancellationToken = default)
    {
        var generationTimer = Stopwatch.StartNew();
        var generated = Generate(request);
        generationTimer.Stop();
        if (generated.Artifact is null || generated.Manifest is null || generated.Readiness != LocalPbirGenerationReadinessState.Generated)
        {
            return generated;
        }

        try
        {
            var inputs = CreateInputs(request);
            var orchestration = new PbirMaterializationOrchestrationService();
            var orchestrationInput = new PbirMaterializationOrchestrationInput(
                inputs.IrState, inputs.SerializerRequest, inputs.DeployableSerializerRequest,
                request.OutputBaseDirectory, request.TargetDirectoryName);
            var materializationTimer = Stopwatch.StartNew();
            var preview = orchestration.Preview(
                new PbirMaterializationOrchestrationPreviewRequest(
                    PbirMaterializationOrchestrationPreviewRequestContract.SchemaVersionV1,
                    $"{request.RequestId}-preview", "preview", orchestrationInput), cancellationToken);
            if (preview.ValidatedPreview is null || preview.Outcome is not (PbirMaterializationOrchestrationOutcome.Absent or PbirMaterializationOrchestrationOutcome.Empty or PbirMaterializationOrchestrationOutcome.ExactMatch))
            {
                return WithMaterializationFailure(generated, preview);
            }

            var transactionId = $"phase38-{ComputeSha256(request.RequestId)[..24]}";
            var materialization = orchestration.Apply(
                new PbirMaterializationOrchestrationApplyRequest(
                    PbirMaterializationOrchestrationApplyRequestContract.SchemaVersionV1,
                    request.RequestId, "apply", orchestrationInput, preview.ValidatedPreview, transactionId, true),
                cancellationToken);
            if (materialization.Outcome is not (PbirMaterializationOrchestrationOutcome.Applied or PbirMaterializationOrchestrationOutcome.ExactMatch))
            {
                return WithMaterializationFailure(generated, materialization);
            }
            materializationTimer.Stop();

            var targetPath = Path.Combine(request.OutputBaseDirectory, request.TargetDirectoryName);
            var projectService = new PbirProjectService(NullLogger<PbirProjectService>.Instance);
            var location = projectService.TryGetReportLocation(targetPath);
            if (location is null)
            {
                return RoundTripFailure(generated, new("PBIR38-ROUNDTRIP-001", "artifact", "The generated PBIR report could not be resolved after materialization."));
            }

            var analyzerTimer = Stopwatch.StartNew();
            var scoring = new PbirScoringService(projectService, NullLogger<PbirScoringService>.Instance);
            var score = await scoring.ScoreAsync(location.ProjectRootPath).ConfigureAwait(false);
            analyzerTimer.Stop();
            var pageCount = score.PageScores?.Count ?? 0;
            if (pageCount != request.Pages.Count || generated.GeneratedVisualCount != request.Visuals.Count)
            {
                return RoundTripFailure(generated, new("PBIR38-ROUNDTRIP-002", "roundTrip", "The analyzer did not observe the requested generated page and visual counts."), materialization);
            }

            return generated with
            {
                Readiness = LocalPbirGenerationReadinessState.RoundTripVerified,
                Materialization = materialization,
                RoundTrip = new LocalPbirGenerationRoundTrip(score, pageCount, request.Visuals.Count),
                Performance = new LocalPbirGenerationPerformance(generationTimer.ElapsedMilliseconds, materializationTimer.ElapsedMilliseconds, analyzerTimer.ElapsedMilliseconds)
            };
        }
        catch (OperationCanceledException)
        {
            return RoundTripFailure(generated, new("PBIR38-ROUNDTRIP-CANCELLED-001", "request", "The round-trip was cancelled safely."));
        }
        catch (IOException)
        {
            return RoundTripFailure(generated, new("PBIR38-ROUNDTRIP-IO-001", "destination", "The local round-trip failed safely."));
        }
    }

    internal Task<LocalPbirGenerationResult> GenerateAndVerifyAsync(
        LocalPbirGenerationRequestV4 request,
        CancellationToken cancellationToken = default) =>
        GenerateAndVerifyAsync(ToV3(request), cancellationToken);

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

    private static IReadOnlyList<LocalPbirGenerationDiagnostic> Validate(LocalPbirGenerationRequestV2? request)
    {
        if (request is null)
        {
            return [new("PBIR37-REQUEST-001", "request", "The generation request is required.")];
        }

        var diagnostics = new List<LocalPbirGenerationDiagnostic>();
        if (request.SchemaVersion != LocalPbirGenerationRequestContract.SchemaVersionV2)
        {
            diagnostics.Add(new("PBIR37-REQUEST-SCHEMA-001", "schemaVersion", "The request schema version is unsupported."));
        }

        ValidateIdentifier(request.RequestId, "requestId", diagnostics, "PBIR37-REQUEST-ID-001");
        ValidateIdentifier(request.ReportName, "reportName", diagnostics, "PBIR37-REQUEST-ID-001");
        if (!IsSafeDatasetPath(request.DatasetPath))
        {
            diagnostics.Add(new("PBIR37-REQUEST-PATH-001", "datasetPath", "The dataset path must be a safe relative SemanticModel path."));
        }

        if (!Path.IsPathFullyQualified(request.OutputBaseDirectory) || !Directory.Exists(request.OutputBaseDirectory))
        {
            diagnostics.Add(new("PBIR37-REQUEST-OUTPUT-001", "outputBaseDirectory", "The output base must be an existing absolute directory."));
        }

        if (string.IsNullOrWhiteSpace(request.TargetDirectoryName) ||
            request.TargetDirectoryName is "." or ".." ||
            request.TargetDirectoryName.Contains('/') || request.TargetDirectoryName.Contains('\\') ||
            request.TargetDirectoryName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            diagnostics.Add(new("PBIR37-REQUEST-TARGET-001", "targetDirectoryName", "The target directory name must be one safe path segment."));
        }

        ValidateUniqueIdentifiers(request.Pages.Select(page => page.PageId), "pages", diagnostics);
        ValidateUniqueIdentifiers(request.Visuals.Select(visual => visual.VisualId), "visuals", diagnostics);
        var pageIds = request.Pages.Select(page => page.PageId).ToHashSet(StringComparer.Ordinal);
        foreach (var page in request.Pages)
        {
            ValidateIdentifier(page.PageId, $"pages[{page.PageId}].pageId", diagnostics, "PBIR37-REQUEST-ID-001");
            ValidateIdentifier(page.DisplayName, $"pages[{page.PageId}].displayName", diagnostics, "PBIR37-REQUEST-ID-001");
            if (page.Order < 0)
            {
                diagnostics.Add(new("PBIR37-REQUEST-PAGE-ORDER-001", $"pages[{page.PageId}].order", "Page order must be non-negative."));
            }
        }

        foreach (var visual in request.Visuals)
        {
            var bindingIds = new HashSet<string>(StringComparer.Ordinal);
            ValidateIdentifier(visual.VisualId, $"visuals[{visual.VisualId}].visualId", diagnostics, "PBIR37-REQUEST-ID-001");
            if (!pageIds.Contains(visual.PageId))
            {
                diagnostics.Add(new("PBIR37-REQUEST-REFERENCE-001", $"visuals[{visual.VisualId}].pageId", "Visual page reference must identify a requested page."));
            }

            if (!LocalPbirGenerationProviderContract.SupportedVisualTypes.Contains(visual.VisualType, StringComparer.Ordinal))
            {
                diagnostics.Add(new("PBIR37-REQUEST-VISUAL-001", $"visuals[{visual.VisualId}].visualType", "Only card and table visual types are supported."));
            }

            if (visual.Order < 0)
            {
                diagnostics.Add(new("PBIR37-REQUEST-VISUAL-ORDER-001", $"visuals[{visual.VisualId}].order", "Visual order must be non-negative."));
            }

            if (visual.Bindings.Count == 0 ||
                (visual.VisualType == "card" && visual.Bindings.Any(binding => binding.Kind != LocalPbirGenerationBindingKind.Measure)) ||
                (visual.VisualType == "table" && visual.Bindings.Count == 0))
            {
                diagnostics.Add(new("PBIR37-REQUEST-BINDING-001", $"visuals[{visual.VisualId}].bindings", "Card visuals require measures and table visuals require at least one direct field binding."));
            }

            foreach (var binding in visual.Bindings)
            {
                ValidateIdentifier(binding.BindingId, $"bindings[{binding.BindingId}].bindingId", diagnostics, "PBIR37-REQUEST-ID-001");
                if (!bindingIds.Add(binding.BindingId))
                {
                    diagnostics.Add(new("PBIR37-REQUEST-DUPLICATE-ID-001", $"bindings[{binding.BindingId}].bindingId", "Binding identifiers must be unique."));
                }

                ValidateIdentifier(binding.Token, $"bindings[{binding.BindingId}].token", diagnostics, "PBIR37-REQUEST-ID-001");
                ValidateIdentifier(binding.Entity, $"bindings[{binding.BindingId}].entity", diagnostics, "PBIR37-REQUEST-ID-001");
                ValidateIdentifier(binding.Property, $"bindings[{binding.BindingId}].property", diagnostics, "PBIR37-REQUEST-ID-001");
            }
        }

        ValidateLayout(request, diagnostics);
        return diagnostics;
    }

    private static IReadOnlyList<LocalPbirGenerationDiagnostic> Validate(LocalPbirGenerationRequestV3? request)
    {
        if (request is null)
        {
            return [new("PBIR38-REQUEST-001", "request", "The generation request is required.")];
        }

        var compatible = new LocalPbirGenerationRequestV2(
            LocalPbirGenerationRequestContract.SchemaVersionV2, request.RequestId, request.ReportName,
            request.DatasetPath, request.GeneratedUtc, request.OutputBaseDirectory, request.TargetDirectoryName,
            request.Pages, request.Visuals);
        var diagnostics = Validate(compatible).ToList();
        if (request.SchemaVersion != LocalPbirGenerationRequestContract.SchemaVersionV3)
        {
            diagnostics.Add(new("PBIR38-REQUEST-SCHEMA-001", "schemaVersion", "The request schema version is unsupported."));
        }

        ValidateFilters(request.ReportFilters ?? [], "reportFilters", diagnostics);
        ValidateTheme(request.Theme, diagnostics);
        ValidateInteraction(request.Interaction, "interaction", diagnostics);
        ValidateLayoutSettings(request.Layout, diagnostics);
        foreach (var page in request.Pages)
        {
            ValidateFilters(page.Authoring?.Filters ?? [], $"pages[{page.PageId}].authoring.filters", diagnostics);
        }

        foreach (var visual in request.Visuals)
        {
            ValidateFilters(visual.Authoring?.Filters ?? [], $"visuals[{visual.VisualId}].authoring.filters", diagnostics);
            ValidateInteraction(visual.Authoring?.Interaction, $"visuals[{visual.VisualId}].authoring.interaction", diagnostics);
            ValidateFormatting(visual, diagnostics);
        }

        return diagnostics;
    }

    private static IReadOnlyList<LocalPbirGenerationDiagnostic> Validate(LocalPbirGenerationRequestV4? request)
    {
        if (request is null)
        {
            return [new("PBIR39-REQUEST-001", "request", "The generation request is required.")];
        }

        var compatible = ToV3(request);
        var diagnostics = Validate(compatible).ToList();
        if (request.SchemaVersion != LocalPbirGenerationRequestContract.SchemaVersionV4)
        {
            diagnostics.Add(new("PBIR39-REQUEST-SCHEMA-001", "schemaVersion", "The request schema version is unsupported."));
        }

        foreach (var visual in request.Visuals)
        {
            if (visual.VisualType is not "card" and not "table" and not "clusteredColumnChart") continue;
            var roles = visual.Bindings
                .GroupBy(binding => binding.Role)
                .ToDictionary(group => group.Key, group => group.ToArray());

            if (roles.Values.Any(bindings => bindings.Length > 1 && visual.VisualType == "card"))
            {
                diagnostics.Add(new("PBIR39-BINDING-ROLE-001", $"visuals[{visual.VisualId}].bindings", "Card bindings may contain only one Value role."));
            }

            if (visual.VisualType == "clusteredColumnChart")
            {
                if (!roles.TryGetValue(LocalPbirGenerationBindingRole.Category, out var categories) || categories.Length != 1 ||
                    !roles.TryGetValue(LocalPbirGenerationBindingRole.Value, out var values) || values.Length < 1)
                {
                    diagnostics.Add(new("PBIR39-BINDING-ROLE-001", $"visuals[{visual.VisualId}].bindings", "Clustered column charts require exactly one Category and at least one Value binding."));
                }

                if (visual.Bindings.Any(binding => binding.Role is not LocalPbirGenerationBindingRole.Category and not LocalPbirGenerationBindingRole.Value))
                {
                    diagnostics.Add(new("PBIR39-BINDING-ROLE-002", $"visuals[{visual.VisualId}].bindings", "Clustered column charts support only Category and Value roles in Phase 39."));
                }
            }

            foreach (var binding in visual.Bindings)
            {
                var expectedKind = binding.Role == LocalPbirGenerationBindingRole.Category
                    ? LocalPbirGenerationBindingKind.Dimension
                    : LocalPbirGenerationBindingKind.Measure;
                if (binding.Kind != expectedKind)
                {
                    diagnostics.Add(new("PBIR39-BINDING-KIND-001", $"visuals[{visual.VisualId}].bindings[{binding.BindingId}]", "Category bindings must be dimensions and Value bindings must be measures."));
                }
            }
        }

        return diagnostics;
    }

    private static LocalPbirGenerationRequestV3 ToV3(LocalPbirGenerationRequestV4 request) =>
        new(
            LocalPbirGenerationRequestContract.SchemaVersionV3,
            request.RequestId,
            request.ReportName,
            request.DatasetPath,
            request.GeneratedUtc,
            request.OutputBaseDirectory,
            request.TargetDirectoryName,
            request.Pages,
            request.Visuals,
            request.Theme,
            request.ReportFilters,
            request.Metadata,
            request.Interaction,
            request.Layout);

    private static void ValidateFilters(
        IReadOnlyList<LocalPbirGenerationEqualityFilter> filters,
        string field,
        ICollection<LocalPbirGenerationDiagnostic> diagnostics)
    {
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var filter in filters)
        {
            ValidateIdentifier(filter.FilterId, $"{field}[{filter.FilterId}].filterId", diagnostics, "PBIR38-FILTER-ID-001");
            ValidateIdentifier(filter.Entity, $"{field}[{filter.FilterId}].entity", diagnostics, "PBIR38-FILTER-REFERENCE-001");
            ValidateIdentifier(filter.Property, $"{field}[{filter.FilterId}].property", diagnostics, "PBIR38-FILTER-REFERENCE-001");
            if (string.IsNullOrWhiteSpace(filter.Value))
            {
                diagnostics.Add(new("PBIR38-FILTER-VALUE-001", $"{field}[{filter.FilterId}].value", "Equality filter values must be non-empty."));
            }
            if (!identities.Add($"{filter.Entity}.{filter.Property}"))
            {
                diagnostics.Add(new("PBIR38-FILTER-DUPLICATE-001", field, "A filter scope cannot declare the same field more than once."));
            }
        }
    }

    private static void ValidateTheme(LocalPbirGenerationTheme? theme, ICollection<LocalPbirGenerationDiagnostic> diagnostics)
    {
        if (theme is null) return;
        if (string.IsNullOrWhiteSpace(theme.Name) || theme.Name.Length > 128)
        {
            diagnostics.Add(new("PBIR38-THEME-001", "theme.name", "Theme name must be non-empty and at most 128 characters."));
        }
        ValidateColor(theme.BackgroundColor, "theme.backgroundColor", diagnostics);
        ValidateColor(theme.AccentColor, "theme.accentColor", diagnostics);
        if (theme.FontSize is <= 0 or > 96)
        {
            diagnostics.Add(new("PBIR38-THEME-FONT-001", "theme.fontSize", "Theme font size must be between 1 and 96."));
        }
        if (theme.Palette is not null)
        {
            if (theme.Palette.Count != theme.Palette.Select(color => color.Hex).Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                diagnostics.Add(new("PBIR38-THEME-DUPLICATE-001", "theme.palette", "Theme palette colors must be unique."));
            }
            foreach (var color in theme.Palette) ValidateColor(color, "theme.palette", diagnostics);
        }
    }

    private static void ValidateInteraction(LocalPbirGenerationInteractionSettings? interaction, string field, ICollection<LocalPbirGenerationDiagnostic> diagnostics)
    {
        if (interaction is not null && !interaction.Enabled && interaction.Mode != LocalPbirGenerationInteractionMode.Disabled)
        {
            diagnostics.Add(new("PBIR38-INTERACTION-001", field, "Disabled interactions must use the Disabled mode."));
        }
    }

    private static void ValidateLayoutSettings(LocalPbirGenerationLayoutSettings? layout, ICollection<LocalPbirGenerationDiagnostic> diagnostics)
    {
        if (layout is null) return;
        if (layout.Margin < 0 || layout.Spacing < 0 || layout.VisualPadding < 0)
        {
            diagnostics.Add(new("PBIR38-LAYOUT-001", "layout", "Margins, spacing, and visual padding must be non-negative."));
        }
    }

    private static void ValidateFormatting(LocalPbirGenerationVisual visual, ICollection<LocalPbirGenerationDiagnostic> diagnostics)
    {
        var card = visual.Authoring?.Card;
        var table = visual.Authoring?.Table;
        var chart = visual.Authoring?.Chart;
        if (visual.VisualType == "card" && (table is not null || chart is not null) ||
            visual.VisualType == "table" && (card is not null || chart is not null) ||
            visual.VisualType == "clusteredColumnChart" && (card is not null || table is not null))
        {
            diagnostics.Add(new("PBIR38-FORMAT-UNSUPPORTED-001", $"visuals[{visual.VisualId}].authoring", "Formatting must match the visual type."));
        }
        var styles = new[] { card?.Label, table?.Header, table?.Row };
        foreach (var style in styles)
        {
            if (style is null) continue;
            ValidateColor(style.Color, $"visuals[{visual.VisualId}].authoring", diagnostics);
            if (style.FontSize is <= 0 or > 96)
            {
                diagnostics.Add(new("PBIR38-FORMAT-FONT-001", $"visuals[{visual.VisualId}].authoring", "Font size must be between 1 and 96."));
            }
        }
        ValidateBox(card?.Box, visual.VisualId, diagnostics);
        ValidateBox(table?.Box, visual.VisualId, diagnostics);
        if (chart is not null)
        {
            ValidateColor(chart.Background, $"visuals[{visual.VisualId}].authoring", diagnostics);
            foreach (var color in chart.Colors ?? [])
            {
                ValidateColor(color, $"visuals[{visual.VisualId}].authoring", diagnostics);
            }
        }
    }

    private static void ValidateBox(LocalPbirGenerationBoxStyle? box, string visualId, ICollection<LocalPbirGenerationDiagnostic> diagnostics)
    {
        if (box is null) return;
        ValidateColor(box.Background, $"visuals[{visualId}].authoring", diagnostics);
        ValidateColor(box.BorderColor, $"visuals[{visualId}].authoring", diagnostics);
        if (box.BorderWidth is < 0 or > 20) diagnostics.Add(new("PBIR38-FORMAT-BORDER-001", $"visuals[{visualId}].authoring", "Border width must be between 0 and 20."));
        var padding = box.Padding;
        if (padding is not null && new[] { padding.Top, padding.Right, padding.Bottom, padding.Left }.Any(value => value < 0 || value > 100))
        {
            diagnostics.Add(new("PBIR38-FORMAT-PADDING-001", $"visuals[{visualId}].authoring", "Padding must be between 0 and 100."));
        }
    }

    private static void ValidateColor(LocalPbirGenerationColor? color, string field, ICollection<LocalPbirGenerationDiagnostic> diagnostics)
    {
        if (color is null) return;
        if (color.Hex.Length != 7 || color.Hex[0] != '#' || color.Hex.Skip(1).Any(character => !Uri.IsHexDigit(character)))
        {
            diagnostics.Add(new("PBIR38-FORMAT-COLOR-001", field, "Colors must use #RRGGBB format."));
        }
    }

    private static void ValidateUniqueIdentifiers(
        IEnumerable<string> values,
        string field,
        ICollection<LocalPbirGenerationDiagnostic> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!seen.Add(value))
            {
                diagnostics.Add(new("PBIR37-REQUEST-DUPLICATE-ID-001", field, "Identifiers must be unique."));
            }
        }
    }

    private static void ValidateLayout(
        LocalPbirGenerationRequestV2 request,
        ICollection<LocalPbirGenerationDiagnostic> diagnostics)
    {
        foreach (var group in request.Visuals.GroupBy(visual => visual.PageId, StringComparer.Ordinal))
        {
            var placed = new List<(LocalPbirGenerationVisual Visual, LocalPbirGenerationVisualLayout Layout)>();
            var autoIndex = 0;
            foreach (var visual in group.OrderBy(value => value.Order).ThenBy(value => value.VisualId, StringComparer.Ordinal))
            {
                var layout = visual.Layout ?? new LocalPbirGenerationLayout(
                    24 + (autoIndex % 3) * 416,
                    24 + (autoIndex / 3) * 344,
                    400,
                    328);
                autoIndex++;
                if (autoIndex > 6)
                {
                    diagnostics.Add(new("PBIR37-LAYOUT-CAPACITY-001", $"pages[{group.Key}].visuals", "A page supports at most six visuals in the Phase 37 layout profile."));
                }
                if (layout.X is null || layout.Y is null || layout.Width is null || layout.Height is null ||
                    layout.X < 0 || layout.Y < 0 || layout.Width <= 0 || layout.Height <= 0 ||
                    layout.X + layout.Width > 1280 || layout.Y + layout.Height > 720)
                {
                    diagnostics.Add(new("PBIR37-LAYOUT-BOUNDS-001", $"visuals[{visual.VisualId}].layout", "Visual layout must be positive and fit within the 1280x720 page canvas."));
                    continue;
                }

                var concrete = new LocalPbirGenerationVisualLayout(layout.X.Value, layout.Y.Value, layout.Width.Value, layout.Height.Value);
                if (placed.Any(existing => Overlaps(existing.Layout, concrete)))
                {
                    diagnostics.Add(new("PBIR37-LAYOUT-OVERLAP-001", $"visuals[{visual.VisualId}].layout", "Visuals on the same page must not overlap."));
                }
                placed.Add((visual, concrete));
            }
        }
    }

    private static bool Overlaps(LocalPbirGenerationVisualLayout left, LocalPbirGenerationVisualLayout right) =>
        left.X < right.X + right.Width && left.X + left.Width > right.X &&
        left.Y < right.Y + right.Height && left.Y + left.Height > right.Y;

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

    private static void ValidateIdentifier(
        string? value,
        string field,
        ICollection<LocalPbirGenerationDiagnostic> diagnostics,
        string code)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64 ||
            value.Any(character => !(char.IsLetterOrDigit(character) || character is '.' or '_' or '-' or ':')))
        {
            diagnostics.Add(new(code, field, "The identifier is missing or unsafe."));
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

    private static GenerationInputs CreateInputs(LocalPbirGenerationRequestV2 request)
    {
        var irId = $"pbirIr:{request.RequestId}";
        var manifestRef = $"localPbirGenerationRequest:{request.RequestId}";
        var specificationRef = $"localPbirGenerationSpecification:{request.RequestId}";
        var orderedPages = request.Pages.OrderBy(page => page.Order).ThenBy(page => page.PageId, StringComparer.Ordinal).ToArray();
        var orderedVisuals = request.Visuals
            .OrderBy(visual => Array.FindIndex(orderedPages, page => page.PageId == visual.PageId))
            .ThenBy(visual => visual.Order)
            .ThenBy(visual => visual.VisualId, StringComparer.Ordinal)
            .ToArray();
        var pages = orderedPages.Select(page => new PbirIntermediateRepresentationPage(
            page.PageId,
            $"page:{request.RequestId}:{page.PageId}",
            "pageTab",
            page.DisplayName,
            page.Order + 1,
            page.DisplayName)).ToArray();
        var visuals = orderedVisuals.Select(visual => new PbirIntermediateRepresentationVisual(
            $"visual:{request.RequestId}:{visual.VisualId}",
            visual.PageId,
            visual.VisualType,
            $"page:{visual.PageId}/slot:{visual.Order + 1}",
            $"intent:{visual.VisualId}",
            ["none"],
            visual.Order + 1,
            ResolveLayout(visual),
            visual.Bindings.Select((binding, index) => new PbirIntermediateRepresentationBinding(
                binding.BindingId,
                (PbirIntermediateRepresentationBindingRole)binding.Role,
                (PbirIntermediateRepresentationBindingKind)binding.Kind,
                binding.Token,
                binding.Entity,
                binding.Property,
                index + 1)).ToArray())).ToArray();
        var inventoryEntries = orderedVisuals
            .SelectMany(visual => visual.Bindings)
            .Select(binding => new
            {
                EntryId = $"{(binding.Kind == LocalPbirGenerationBindingKind.Measure ? "measure" : "column")}:{binding.Entity}.{binding.Property}",
                binding.Token,
                binding.Entity,
                binding.Property,
                Kind = binding.Kind == LocalPbirGenerationBindingKind.Measure
                    ? PbirSemanticModelEntryKind.Measure
                    : PbirSemanticModelEntryKind.Column
            })
            .GroupBy(entry => entry.EntryId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(entry => entry.EntryId, StringComparer.Ordinal)
            .Select(entry => new PbirSemanticModelInventoryEntry(entry.EntryId, entry.Token, entry.Entity, entry.Property, entry.Kind))
            .ToArray();
        var inventory = new PbirSemanticModelInventory(PbirSemanticModelInventoryContract.SchemaVersionV1, $"modelInventory:{request.RequestId}", inventoryEntries);
        var semantics = orderedPages.Select(page =>
        {
            var pageVisuals = orderedVisuals.Where(visual => visual.PageId == page.PageId).ToArray();
            var bindings = pageVisuals.SelectMany(visual => visual.Bindings).ToArray();
            return new PbirIntermediateRepresentationSemantic(
                $"semantic:{request.RequestId}:{page.PageId}",
                page.PageId,
                bindings.Where(binding => binding.Kind == LocalPbirGenerationBindingKind.Measure).Select(binding => binding.Token).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                bindings.Where(binding => binding.Kind == LocalPbirGenerationBindingKind.Dimension).Select(binding => binding.Token).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                pageVisuals.Select(visual => $"intent:{visual.VisualId}").ToArray(),
                [],
                "none",
                pageVisuals.Select(visual => $"visual:[visual:{request.RequestId}:{visual.VisualId}]->semantic:[intent:{visual.VisualId}]").ToArray());
        }).ToArray();
        var navigation = new PbirIntermediateRepresentationNavigation(
            orderedPages[0].PageId,
            orderedPages.Zip(orderedPages.Skip(1), (from, to) => new PbirIntermediateRepresentationPageTransition(from.PageId, to.PageId, $"{from.PageId}->{to.PageId}")).ToArray(),
            orderedPages.Select(page => $"page:{page.PageId}").Append($"landing:{orderedPages[0].PageId}").ToArray(),
            []);
        var layout = new PbirIntermediateRepresentationLayout(
            orderedPages.Select(page => new PbirIntermediateRepresentationLayoutContainer(
                $"container:{request.RequestId}:{page.PageId}",
                page.PageId,
                $"deterministic {page.DisplayName} layout",
                orderedVisuals.Where(visual => visual.PageId == page.PageId).Select(visual => $"visual:{request.RequestId}:{visual.VisualId}").ToArray())).ToArray(),
            ["standard-8px-grid"],
            ["deterministic-grid", "visual-placement-preserved"],
            ["preserve-page-order", "preserve-visual-intent", "allow-future-serializer-layout-adaptation"]);
        var successCriteria = new PbirIntermediateRepresentationSuccessCriteria(
            orderedVisuals.SelectMany(visual => visual.Bindings).Select(binding => $"{binding.Token} is visible.").Distinct(StringComparer.Ordinal).ToArray(),
            orderedPages.Select(page => page.DisplayName).ToArray(),
            orderedVisuals.SelectMany(visual => visual.Bindings).Select(binding => $"{binding.Token} is bound explicitly.").Distinct(StringComparer.Ordinal).ToArray());
        var lineage = new PbirIntermediateRepresentationLineage([new("localPbirGeneration", manifestRef, "Phase 37 local generation request")], [manifestRef, specificationRef, irId]);
        var metadata = new PbirIntermediateRepresentationMetadata(irId, PbirIntermediateRepresentationContract.SchemaVersionV1, request.GeneratedUtc);
        var references = new PbirIntermediateRepresentationReferences(manifestRef, specificationRef);
        var contentHash = PbirIntermediateRepresentationIntegrity.ComputeContentHash(metadata, references, pages, visuals, semantics, navigation, layout, successCriteria, lineage);
        var ir = new PbirIntermediateRepresentation(
            metadata,
            references,
            pages,
            visuals,
            semantics,
            navigation,
            layout,
            successCriteria,
            lineage,
            new(ComputeSha256(JsonSerializer.Serialize(request)), contentHash, ComputeSha256(JsonSerializer.Serialize(lineage.ImmutableLineage))));
        var irState = new PbirIntermediateRepresentationState(ir, new PbirIntermediateRepresentationValidationResult(PbirIntermediateRepresentationValidationDiagnostics.Empty), PbirIntermediateRepresentationReadinessState.ReadyForSerializer);
        var serializerRequest = new PbirSerializerRequest(PbirSerializerRequestContract.SchemaVersionV1, $"pbirSerializerRequest:{irId}", irId, ir.Metadata.SchemaVersion, contentHash, true, false, false, false);
        var canonicalJson = new PbirDeployableSerializerCanonicalJson();
        var inventoryHash = canonicalJson.ComputeSha256(canonicalJson.SerializeSemanticModelInventory(inventory));
        var visualBindings = orderedVisuals.Select(visual => new PbirVisualBinding(
            $"visual:{request.RequestId}:{visual.VisualId}",
            CreateProjections(visual))).ToArray();
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
            visualBindings,
            PbirDeployableExecutionPolicy.NoAuthority);
        return new GenerationInputs(irState, serializerRequest, deployableRequest);

        static IReadOnlyList<PbirRoleProjectionBinding> CreateProjections(LocalPbirGenerationVisual visual)
        {
            var roleOrders = new Dictionary<string, int>(StringComparer.Ordinal);
            return visual.Bindings.Select(binding =>
            {
                var role = GetSerializerRole(visual, binding);
                roleOrders[role] = roleOrders.TryGetValue(role, out var current) ? current + 1 : 1;
                return new PbirRoleProjectionBinding(
                    role,
                    roleOrders[role],
                    binding.Token,
                    $"{(binding.Kind == LocalPbirGenerationBindingKind.Measure ? "measure" : "column")}:{binding.Entity}.{binding.Property}",
                    $"{binding.Entity}.{binding.Property}",
                    binding.Property,
                    "none",
                    null,
                    null);
            }).ToArray();
        }

        static string GetSerializerRole(LocalPbirGenerationVisual visual, LocalPbirGenerationBinding binding)
        {
            if (visual.VisualType == "card") return "Fields";
            if (visual.VisualType == "table") return "Values";
            return binding.Role switch
            {
                LocalPbirGenerationBindingRole.Category => "Category",
                LocalPbirGenerationBindingRole.Value => "Y",
                _ => throw new InvalidOperationException($"Unsupported Phase 39 binding role: {binding.Role}")
            };
        }

        static PbirIntermediateRepresentationVisualLayout ResolveLayout(LocalPbirGenerationVisual visual)
        {
            var layout = visual.Layout ?? new LocalPbirGenerationLayout(24 + (visual.Order % 3) * 416, 24 + (visual.Order / 3) * 344, 400, 328);
            return new(layout.X!.Value, layout.Y!.Value, layout.Width!.Value, layout.Height!.Value);
        }
    }

    private static GenerationInputs CreateInputs(LocalPbirGenerationRequestV3 request)
    {
        var layout = request.Layout ?? new LocalPbirGenerationLayoutSettings();
        var visuals = request.Visuals.Select(visual => visual.Layout is not null
            ? visual
            : visual with
            {
                Layout = new LocalPbirGenerationLayout(
                    layout.Margin + (visual.Order % 3) * (400 + layout.Spacing),
                    layout.Margin + (visual.Order / 3) * (328 + layout.Spacing),
                    400,
                    328)
            }).ToArray();
        var compatible = new LocalPbirGenerationRequestV2(
            LocalPbirGenerationRequestContract.SchemaVersionV2, request.RequestId, request.ReportName,
            request.DatasetPath, request.GeneratedUtc, request.OutputBaseDirectory, request.TargetDirectoryName,
            request.Pages, visuals);
        var inputs = CreateInputs(compatible);
        return inputs with { DeployableSerializerRequest = inputs.DeployableSerializerRequest with { Authoring = request } };
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
            Diagnostics = materialization.Diagnostics.Items.Count > 0
                ? materialization.Diagnostics.Items.Select(item => new LocalPbirGenerationDiagnostic(item.Code, item.Field, item.Message)).ToArray()
                : [new(
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
