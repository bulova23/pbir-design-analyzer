using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using PowerBIModelingService.Services.Pbir;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.PbirAuthoringRpc;

internal sealed class PbirAuthoringRpcDispatcher
{
    private readonly LocalPbirGenerationProviderService _generation;
    private readonly LocalPbirMutationProviderService _mutation;
    private readonly PbirProjectService _projectService;
    private readonly PbirScoringService _scoringService;
    private readonly Dictionary<string, PbirLocalReportImportSnapshot> _snapshots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (PbirDeployableArtifact Artifact, PbirDeployableManifest Manifest, string ReportDirectory)> _artifacts = new(StringComparer.Ordinal);

    internal PbirAuthoringRpcDispatcher()
        : this(
            new PbirProjectService(NullLogger<PbirProjectService>.Instance),
            null,
            new LocalPbirGenerationProviderService(),
            new LocalPbirMutationProviderService())
    {
    }

    internal PbirAuthoringRpcDispatcher(
        PbirProjectService projectService,
        PbirScoringService? scoringService)
        : this(
            projectService,
            scoringService,
            new LocalPbirGenerationProviderService(),
            new LocalPbirMutationProviderService())
    {
    }

    internal PbirAuthoringRpcDispatcher(
        LocalPbirGenerationProviderService generation,
        LocalPbirMutationProviderService mutation)
        : this(
            new PbirProjectService(NullLogger<PbirProjectService>.Instance),
            null,
            generation,
            mutation)
    {
    }

    private PbirAuthoringRpcDispatcher(
        PbirProjectService projectService,
        PbirScoringService? scoringService,
        LocalPbirGenerationProviderService generation,
        LocalPbirMutationProviderService mutation)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _scoringService = scoringService ?? new PbirScoringService(_projectService, NullLogger<PbirScoringService>.Instance);
        _generation = generation;
        _mutation = mutation;
    }

    internal Task<PbirAuthoringRpcResponse> DispatchAsync(
        PbirAuthoringRpcRequest? request,
        CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        var operation = request?.Operation ?? PbirAuthoringRpcOperation.Generate;
        var response = ValidateRequest(request, operation);
        timer.Stop();
        if (response is not null)
            return Task.FromResult(response with { Timing = new(timer.ElapsedMilliseconds, 0, 0, 0) });

        cancellationToken.ThrowIfCancellationRequested();
        return operation switch
        {
            PbirAuthoringRpcOperation.Generate => GenerateAsync(request!, timer, cancellationToken),
            PbirAuthoringRpcOperation.Import => Task.FromResult(Import(request!, timer)),
            PbirAuthoringRpcOperation.Mutate => MutateAsync(request!, timer, cancellationToken),
            PbirAuthoringRpcOperation.Validate => Task.FromResult(Validate(request!, timer)),
            PbirAuthoringRpcOperation.Analyze => AnalyzeAsync(request!, timer, cancellationToken),
            _ => Task.FromResult(Failure(operation, PbirAuthoringRpcErrorCategory.InternalFailure,
                "PBIR-RPC-INTERNAL-001", "The requested authoring operation is not implemented."))
        };
    }

    private async Task<PbirAuthoringRpcResponse> GenerateAsync(
        PbirAuthoringRpcRequest request,
        Stopwatch dispatchTimer,
        CancellationToken cancellationToken)
    {
        var operationTimer = Stopwatch.StartNew();
        LocalPbirGenerationResult result;
        try
        {
            result = request.Generate!.Request.Kind switch
            {
                PbirAuthoringGenerationRequestKind.V1 => await _generation.GenerateAndVerifyAsync(request.Generate.Request.V1!, cancellationToken),
                PbirAuthoringGenerationRequestKind.V2 => await _generation.GenerateAndVerifyAsync(request.Generate.Request.V2!, cancellationToken),
                PbirAuthoringGenerationRequestKind.V3 => await _generation.GenerateAndVerifyAsync(request.Generate.Request.V3!, cancellationToken),
                PbirAuthoringGenerationRequestKind.V4 => await _generation.GenerateAndVerifyAsync(request.Generate.Request.V4!, cancellationToken),
                PbirAuthoringGenerationRequestKind.V5 => await _generation.GenerateAndVerifyAsync(request.Generate.Request.V5!, cancellationToken),
                PbirAuthoringGenerationRequestKind.V6 => await _generation.GenerateAndVerifyAsync(request.Generate.Request.V6!, cancellationToken),
                PbirAuthoringGenerationRequestKind.V7 => await _generation.GenerateAndVerifyAsync(request.Generate.Request.V7!, cancellationToken),
                _ => throw new InvalidOperationException()
            };
        }
        catch (OperationCanceledException)
        {
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.InvalidRequest, "PBIR-RPC-CANCELLED-001", "The authoring operation was cancelled.");
        }
        catch (Exception)
        {
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.InternalFailure, "PBIR-RPC-INTERNAL-002", "The authoring operation failed safely.");
        }

        operationTimer.Stop();
        var artifact = result.Artifact is null || result.Manifest is null ? null : CreateArtifactHandle(result.Artifact, result.Manifest);
        if (result.Artifact is not null && result.Manifest is not null)
            _artifacts[artifact!.ArtifactHash] = (result.Artifact, result.Manifest, GetGeneratedReportDirectory(request.Generate.Request));
        var analyzer = result.RoundTrip is null ? null : CreateAnalyzerSummary(result.RoundTrip.Score, result.RoundTrip.PageCount, result.RoundTrip.VisualCount);
        var response = new PbirAuthoringRpcResponse(
            PbirAuthoringRpcContract.SchemaVersionV1,
            request.Operation,
            result.Readiness is LocalPbirGenerationReadinessState.Generated or LocalPbirGenerationReadinessState.RoundTripVerified,
            result.Diagnostics.Select(ToDiagnostic).ToArray(),
            result.Readiness == LocalPbirGenerationReadinessState.Rejected
                ? new(ErrorCategoryFor(result.Diagnostics), "PBIR-RPC-GENERATE-001", "Generation was rejected by the existing authoring provider.")
                : null,
            artifact is null ? null : ToArtifactIdentity(artifact),
            null,
            analyzer,
            new(0, operationTimer.ElapsedMilliseconds, result.Performance?.MaterializationMilliseconds ?? 0, result.Performance?.AnalyzerMilliseconds ?? 0),
            new(result.SchemaVersion, artifact),
            null, null, null, null);
        dispatchTimer.Stop();
        return response with { Timing = response.Timing with { DispatchMilliseconds = dispatchTimer.ElapsedMilliseconds } };
    }

    private PbirAuthoringRpcResponse Import(PbirAuthoringRpcRequest request, Stopwatch dispatchTimer)
    {
        var operationTimer = Stopwatch.StartNew();
        PbirLocalReportImportSnapshot snapshot;
        try
        {
            snapshot = _mutation.Import(request.Import!.SourceDirectory);
        }
        catch (Exception)
        {
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.ImportFailed, "PBIR-RPC-IMPORT-001", "The PBIR report could not be imported.");
        }

        operationTimer.Stop();
        var contentHash = Hash(string.Join("|", snapshot.FileHashes.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}:{x.Value}")));
        var handle = new PbirAuthoringSnapshotHandle(
            PbirAuthoringRpcContract.SnapshotHandleSchemaVersionV1,
            $"snapshot:{contentHash[..24]}",
            new(Path.GetFileName(Path.TrimEndingDirectorySeparator(snapshot.SourceDirectory)), contentHash, snapshot.FileHashes.Count));
        _snapshots[handle.SnapshotId] = snapshot;
        var invalidDiagnostics = snapshot.Diagnostics.Where(diagnostic => diagnostic.ProjectionStatus == LocalPbirSemanticProjectionStatus.Invalid).ToArray();
        dispatchTimer.Stop();
        return new(
            PbirAuthoringRpcContract.SchemaVersionV1,
            request.Operation,
            snapshot.IrState.Ir is not null && invalidDiagnostics.Length == 0,
            snapshot.Diagnostics.Select(ToDiagnostic).ToArray(),
            snapshot.IrState.Ir is null
                ? new(PbirAuthoringRpcErrorCategory.ImportFailed, "PBIR-RPC-IMPORT-002", "The PBIR report did not satisfy the supported import boundary.")
                : invalidDiagnostics.Length > 0
                    ? new(PbirAuthoringRpcErrorCategory.ImportFailed, "PBIR-RPC-IMPORT-003", $"The PBIR report import found {invalidDiagnostics.Length} invalid semantic binding(s): {invalidDiagnostics[0].Message}")
                    : null,
            null, null, null,
            new(dispatchTimer.ElapsedMilliseconds, operationTimer.ElapsedMilliseconds, 0, 0),
            null,
            new(handle, snapshot.IrState.Ir?.Pages
                .OrderBy(page => page.Order)
                .ThenBy(page => page.PageId, StringComparer.Ordinal)
                .Select(page => new PbirAuthoringPageMetadata(page.PageId, page.DisplayName ?? page.PageId))
                .ToArray() ?? [],
                snapshot.IrState.Ir?.Visuals
                    .OrderBy(visual => visual.PageId, StringComparer.Ordinal)
                    .ThenBy(visual => visual.Order)
                    .ThenBy(visual => visual.VisualId, StringComparer.Ordinal)
                    .Select(visual => new PbirAuthoringVisualMetadata(visual.VisualId, visual.PageId, visual.VisualType, visual.Order, ToLayout(visual.Layout)))
                    .ToArray() ?? []), null, null, null);
    }

    private async Task<PbirAuthoringRpcResponse> AnalyzeAsync(
        PbirAuthoringRpcRequest request,
        Stopwatch dispatchTimer,
        CancellationToken cancellationToken)
    {
        var operationTimer = Stopwatch.StartNew();
        try
        {
            var reportDirectory = ResolveAnalyzeDirectory(request.Analyze!);
            if (reportDirectory is null)
                return Failure(request.Operation, PbirAuthoringRpcErrorCategory.AnalyzerFailed, "PBIR-RPC-ANALYZE-001", "The report handle or directory could not be resolved for analysis.");

            var location = _projectService.TryGetReportLocation(reportDirectory);
            if (location is null)
                return Failure(request.Operation, PbirAuthoringRpcErrorCategory.AnalyzerFailed, "PBIR-RPC-ANALYZE-001", "The report could not be resolved for analysis.");
            var analyzerTimer = Stopwatch.StartNew();
            var score = await _scoringService
                .ScoreAsync(location.ProjectRootPath, request.Analyze!.Config, request.Analyze.PageName);
            analyzerTimer.Stop();
            operationTimer.Stop();
            dispatchTimer.Stop();
            var summary = CreateAnalyzerSummary(score, score.PageScores?.Count ?? 0, score.DataVisualCount);
            return new(PbirAuthoringRpcContract.SchemaVersionV1, request.Operation, true, [], null, null, null, summary,
                new(dispatchTimer.ElapsedMilliseconds, operationTimer.ElapsedMilliseconds, 0, analyzerTimer.ElapsedMilliseconds),
                null, null, null, null, new(summary));
        }
        catch (OperationCanceledException)
        {
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.AnalyzerFailed, "PBIR-RPC-ANALYZE-002", "Analysis was cancelled safely.");
        }
        catch (Exception)
        {
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.AnalyzerFailed, "PBIR-RPC-ANALYZE-003", "The report could not be analyzed safely.");
        }
    }

    private PbirAuthoringRpcResponse Validate(PbirAuthoringRpcRequest request, Stopwatch dispatchTimer)
    {
        var operationTimer = Stopwatch.StartNew();
        if (!_artifacts.TryGetValue(request.Validate!.Artifact.ArtifactHash, out var stored) ||
            stored.Artifact.ArtifactId != request.Validate.Artifact.ArtifactId ||
            stored.Manifest.Hashes.ManifestHash != request.Validate.Artifact.ManifestHash)
        {
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.InvalidRequest, "PBIR-RPC-VALIDATE-001", "The generated artifact handle is unknown or stale.");
        }

        var validation = new PbirDeployableSerializerValidator().ValidateOutput(stored.Artifact, stored.Manifest);
        operationTimer.Stop();
        dispatchTimer.Stop();
        var diagnostics = validation.SchemaContractResults.Concat(validation.StructuralValidationResults)
            .Concat(validation.CrossReferenceValidationResults).Concat(validation.HashValidationResults)
            .Select(ToDiagnostic).ToArray();
        return new(
            PbirAuthoringRpcContract.SchemaVersionV1,
            request.Operation,
            validation.IsValid,
            diagnostics,
            validation.IsValid ? null : new(PbirAuthoringRpcErrorCategory.ValidationFailed, "PBIR-RPC-VALIDATE-002", "Schema validation rejected the artifact."),
            new(stored.Artifact.ArtifactId, stored.Artifact.Hashes.ArtifactHash, stored.Manifest.ManifestId, stored.Manifest.Hashes.ManifestHash),
            null, null,
            new(dispatchTimer.ElapsedMilliseconds, operationTimer.ElapsedMilliseconds, operationTimer.ElapsedMilliseconds, 0),
            null, null, null,
            new(validation.IsValid, validation.ValidatedFileCount), null);
    }

    private async Task<PbirAuthoringRpcResponse> MutateAsync(
        PbirAuthoringRpcRequest request,
        Stopwatch dispatchTimer,
        CancellationToken cancellationToken)
    {
        var operationTimer = Stopwatch.StartNew();
        if (request.Mutate!.Request.Operations is null || request.Mutate.Request.Operations.Count == 0)
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.InvalidRequest, "PBIR-RPC-MUTATE-002", "At least one mutation operation is required.");
        if (request.Mutate.Request.Operations.Count != 1)
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.UnsupportedAuthoring, "PBIR-RPC-MUTATE-009", "Exactly one mutation operation is supported by the public authoring workflow.");
        if (!_snapshots.TryGetValue(request.Mutate!.Snapshot.SnapshotId, out var snapshot) ||
            (!string.IsNullOrWhiteSpace(request.Mutate.Request.SourceDirectory) &&
             snapshot.SourceDirectory != request.Mutate.Request.SourceDirectory))
        {
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.InvalidRequest, "PBIR-RPC-MUTATE-001", "The imported snapshot handle is unknown or stale.");
        }

        var effectiveRequest = request.Mutate.Request with
        {
            SourceDirectory = snapshot.SourceDirectory,
            OutputBaseDirectory = string.IsNullOrWhiteSpace(request.Mutate.Request.OutputBaseDirectory)
                ? Path.Combine(Path.GetTempPath(), "pbir-authoring", request.Mutate.Request.MutationId)
                : request.Mutate.Request.OutputBaseDirectory,
            TargetDirectoryName = string.IsNullOrWhiteSpace(request.Mutate.Request.TargetDirectoryName)
                ? "mutated"
                : request.Mutate.Request.TargetDirectoryName,
        };
        var planningTimer = Stopwatch.StartNew();
        var plan = _mutation.Plan(snapshot, effectiveRequest);
        planningTimer.Stop();
        if (!plan.IsValid)
        {
            var category = plan.Diagnostics.Any(diagnostic => diagnostic.Code.Contains("CONFLICT", StringComparison.OrdinalIgnoreCase))
                ? PbirAuthoringRpcErrorCategory.MutationConflict
                : plan.Diagnostics.Any(diagnostic => diagnostic.Code.Contains("UNSUPPORTED", StringComparison.OrdinalIgnoreCase))
                    ? PbirAuthoringRpcErrorCategory.UnsupportedAuthoring
                    : PbirAuthoringRpcErrorCategory.InvalidRequest;
            return Failure(request.Operation, category, "PBIR-RPC-MUTATE-002", "The mutation request was rejected by the existing planner.", plan.Diagnostics.Select(ToDiagnostic).ToArray());
        }

        var previewTimer = Stopwatch.StartNew();
        var semanticPreview = CreateMutationPreview(plan, effectiveRequest.Operations.Single());
        previewTimer.Stop();
        if (request.Mutate.Mode == PbirAuthoringMutationMode.Preview)
        {
            dispatchTimer.Stop();
            operationTimer.Stop();
            return new(
                PbirAuthoringRpcContract.SchemaVersionV1,
                request.Operation,
                true,
                semanticPreview.Diagnostics,
                null,
                null,
                null,
                null,
                new(dispatchTimer.ElapsedMilliseconds, operationTimer.ElapsedMilliseconds, 0, 0, planningTimer.ElapsedMilliseconds, previewTimer.ElapsedMilliseconds),
                null,
                null,
                new(null, 0, 0, semanticPreview),
                null,
                null);
        }

        if (plan.IsNoOp)
        {
            dispatchTimer.Stop();
            operationTimer.Stop();
            return new(
                PbirAuthoringRpcContract.SchemaVersionV1,
                request.Operation,
                true,
                semanticPreview.Diagnostics,
                null,
                null,
                null,
                null,
                new(dispatchTimer.ElapsedMilliseconds, operationTimer.ElapsedMilliseconds, 0, 0, planningTimer.ElapsedMilliseconds, previewTimer.ElapsedMilliseconds),
                null,
                null,
                new(null, 0, 0, semanticPreview),
                null,
                null);
        }

        var execution = _mutation.Execute(plan);
        if (execution.IrState.Ir is null)
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.MutationConflict, "PBIR-RPC-MUTATE-003", "The mutation could not produce a valid shared authoring state.");

        var updatedIr = execution.IrState.Ir with
        {
            Hashes = execution.IrState.Ir.Hashes with
            {
                ContentHash = PbirIntermediateRepresentationIntegrity.ComputeContentHash(execution.IrState.Ir)
            }
        };
        var normalizedIr = NormalizeImportedIr(updatedIr);
        normalizedIr = normalizedIr with
        {
            Hashes = normalizedIr.Hashes with
            {
                ContentHash = PbirIntermediateRepresentationIntegrity.ComputeContentHash(normalizedIr)
            }
        };
        var irState = execution.IrState with { Ir = normalizedIr };
        var resolved = new PbirAuthoringMergeService().Resolve(normalizedIr);
        var irService = new PbirIntermediateRepresentationService();
        var serializerRequest = irService.CreateSerializerRequest(irState);
        var deployableRequest = CreateImportedDeployableRequest(irState, serializerRequest, effectiveRequest);
        var serializerTimer = Stopwatch.StartNew();
        var serialized = new PbirDeployableSerializerService().CreateArtifacts(irState, serializerRequest, deployableRequest);
        serializerTimer.Stop();
        if (serialized.Artifact is null || serialized.Manifest is null)
        {
            var serializerDiagnostics = serialized.Diagnostics.UnsupportedVisualTypes
                .Concat(serialized.Diagnostics.IncompleteSemanticBindings)
                .Concat(serialized.Diagnostics.InvalidModelReferences)
                .Concat(serialized.Diagnostics.DuplicateIdentities)
                .Concat(serialized.Diagnostics.InvalidLayoutDefinitions)
                .Concat(serialized.Diagnostics.SchemaIncompatibilities)
                .Concat(serialized.Validation.SchemaContractResults)
                .Concat(serialized.Validation.StructuralValidationResults)
                .Concat(serialized.Validation.CrossReferenceValidationResults)
                .Concat(serialized.Validation.HashValidationResults)
                .Select(ToDiagnostic).ToArray();
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.ValidationFailed, "PBIR-RPC-MUTATE-004", "The mutated report failed existing serializer validation.", serializerDiagnostics);
        }

        var input = new PbirMaterializationOrchestrationInput(
            irState, serializerRequest, deployableRequest,
            effectiveRequest.OutputBaseDirectory,
            effectiveRequest.TargetDirectoryName);
        var orchestration = new PbirMaterializationOrchestrationService();
        var materializationPreview = orchestration.Preview(new(
            PbirMaterializationOrchestrationPreviewRequestContract.SchemaVersionV1,
            $"{effectiveRequest.MutationId}-preview",
            "preview", input), cancellationToken);
        if (materializationPreview.ValidatedPreview is null || materializationPreview.Outcome is not (PbirMaterializationOrchestrationOutcome.Absent or PbirMaterializationOrchestrationOutcome.Empty or PbirMaterializationOrchestrationOutcome.ExactMatch))
        {
            var previewDiagnostics = string.Join(",", materializationPreview.Diagnostics.Items.Select(item => item.Code).Take(4));
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.MutationConflict, "PBIR-RPC-MUTATE-005", $"The mutation destination has a materialization conflict ({materializationPreview.Outcome};{previewDiagnostics}).");
        }
        var applied = orchestration.Apply(new(
            PbirMaterializationOrchestrationApplyRequestContract.SchemaVersionV1,
            effectiveRequest.MutationId,
            "apply", input, materializationPreview.ValidatedPreview,
            "phase47-" + Hash(effectiveRequest.MutationId)[..24], true), cancellationToken);
        if (applied.Outcome is not (PbirMaterializationOrchestrationOutcome.Applied or PbirMaterializationOrchestrationOutcome.ExactMatch))
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.ExecutionFailed, "PBIR-RPC-MUTATE-006", "The mutation could not be materialized safely.");

        var outputDirectory = Path.Combine(effectiveRequest.OutputBaseDirectory, effectiveRequest.TargetDirectoryName);
        var beforeLocation = _projectService.TryGetReportLocation(snapshot.SourceDirectory);
        if (beforeLocation is null)
            return Failure(request.Operation, PbirAuthoringRpcErrorCategory.AnalyzerFailed, "PBIR-RPC-MUTATE-007", "The source report could not be analyzed before mutation.");
        var analyzerBeforeTimer = Stopwatch.StartNew();
        var beforeScore = await _scoringService.ScoreAsync(beforeLocation.ProjectRootPath);
        analyzerBeforeTimer.Stop();
        var analyzerTimer = Stopwatch.StartNew();
        var score = await _scoringService.ScoreAsync(outputDirectory);
        analyzerTimer.Stop();
        operationTimer.Stop();
        dispatchTimer.Stop();
        var artifact = CreateArtifactHandle(serialized.Artifact, serialized.Manifest);
        _artifacts[artifact.ArtifactHash] = (serialized.Artifact, serialized.Manifest, outputDirectory);
        var fidelity = CreateFidelity(snapshot, serialized.Artifact, plan.AffectedPages.Count + plan.AffectedVisuals.Count > 0);
        var response = new PbirAuthoringRpcResponse(
            PbirAuthoringRpcContract.SchemaVersionV1, request.Operation, true,
            plan.Diagnostics.Select(ToDiagnostic).ToArray(), null, ToArtifactIdentity(artifact), fidelity,
            CreateAnalyzerSummary(score, score.PageScores?.Count ?? 0, score.DataVisualCount),
            new(dispatchTimer.ElapsedMilliseconds, operationTimer.ElapsedMilliseconds, serializerTimer.ElapsedMilliseconds, analyzerTimer.ElapsedMilliseconds, planningTimer.ElapsedMilliseconds, previewTimer.ElapsedMilliseconds, analyzerBeforeTimer.ElapsedMilliseconds),
            null, null,
            new(
                artifact,
                plan.AffectedPages.Count,
                plan.AffectedVisuals.Count,
                semanticPreview,
                new(
                    CreateAnalyzerSummary(beforeScore, beforeScore.PageScores?.Count ?? 0, beforeScore.DataVisualCount),
                    CreateAnalyzerSummary(score, score.PageScores?.Count ?? 0, score.DataVisualCount),
                    score.CompositeScore - beforeScore.CompositeScore,
                    snapshot.IrState.Ir!.Pages.Select(page => page.PageId).Except(plan.AffectedPages, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                    snapshot.IrState.Ir.Visuals.Select(visual => visual.VisualId).Except(plan.AffectedVisuals, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray()),
                new PbirAuthoringMaterializationHandle(
                    effectiveRequest.OutputBaseDirectory,
                    effectiveRequest.TargetDirectoryName,
                    applied.TargetKey ?? string.Empty,
                    applied.TransactionId ?? string.Empty,
                    applied.TransactionHash ?? string.Empty,
                    applied.CurrentReceiptHash ?? string.Empty,
                    applied.TargetStateHash ?? string.Empty)));
        _ = resolved;
        return response;
    }

    private static PbirAuthoringRpcResponse? ValidateRequest(
        PbirAuthoringRpcRequest? request,
        PbirAuthoringRpcOperation operation)
    {
        if (request is null || request.SchemaVersion != PbirAuthoringRpcContract.SchemaVersionV1)
            return Failure(operation, PbirAuthoringRpcErrorCategory.InvalidRequest, "PBIR-RPC-REQUEST-001", "The RPC request is invalid.");

        if (!PbirAuthoringRpcOperationCatalog.All.Contains(operation))
            return Failure(operation, PbirAuthoringRpcErrorCategory.InvalidRequest, "PBIR-RPC-REQUEST-002", "The RPC operation is not supported.");

        var payloadCount = new[]
        {
            request.Generate is not null,
            request.Import is not null,
            request.Mutate is not null,
            request.Validate is not null,
            request.Analyze is not null
        }.Count(value => value);
        if (payloadCount != 1 || !HasExpectedPayload(request, operation) ||
            (operation == PbirAuthoringRpcOperation.Generate && !HasExactlyOneGenerationCase(request.Generate!.Request)))
            return Failure(operation, PbirAuthoringRpcErrorCategory.InvalidRequest, "PBIR-RPC-REQUEST-003", "Exactly one payload matching the operation is required.");

        return null;
    }

    private static bool HasExpectedPayload(PbirAuthoringRpcRequest request, PbirAuthoringRpcOperation operation) =>
        operation switch
        {
            PbirAuthoringRpcOperation.Generate => request.Generate is not null && request.Generate.Request is not null,
            PbirAuthoringRpcOperation.Import => request.Import is not null && !string.IsNullOrWhiteSpace(request.Import.SourceDirectory),
            PbirAuthoringRpcOperation.Mutate => request.Mutate is not null && request.Mutate.Snapshot is not null && request.Mutate.Request is not null,
            PbirAuthoringRpcOperation.Validate => request.Validate is not null && request.Validate.Artifact is not null,
            PbirAuthoringRpcOperation.Analyze => request.Analyze is not null &&
                ((!string.IsNullOrWhiteSpace(request.Analyze.ReportDirectory) ? 1 : 0) +
                 (request.Analyze.Artifact is not null ? 1 : 0) +
                 (request.Analyze.Snapshot is not null ? 1 : 0) == 1),
            _ => false
        };

    private static bool HasExactlyOneGenerationCase(PbirAuthoringGenerationRequest request) =>
        new[] { request.V1 is not null, request.V2 is not null, request.V3 is not null, request.V4 is not null,
            request.V5 is not null, request.V6 is not null, request.V7 is not null }.Count(value => value) == 1;

    private static PbirAuthoringArtifactHandle CreateArtifactHandle(PbirDeployableArtifact artifact, PbirDeployableManifest manifest) =>
        new(PbirAuthoringRpcContract.ArtifactHandleSchemaVersionV1, artifact.ArtifactId, artifact.Hashes.ArtifactHash, manifest.ManifestId, manifest.Hashes.ManifestHash);

    private static PbirAuthoringArtifactIdentity ToArtifactIdentity(PbirAuthoringArtifactHandle artifact) =>
        new(artifact.ArtifactId, artifact.ArtifactHash, artifact.ManifestId, artifact.ManifestHash);

    private static PbirAuthoringAnalyzerSummary CreateAnalyzerSummary(ScoreResult score, int pageCount, int visualCount) =>
        new(score.CompositeScore, pageCount, visualCount, score);

    private string? ResolveAnalyzeDirectory(PbirAuthoringAnalyzeRequest request)
    {
        if (request.Artifact is not null)
        {
            return _artifacts.TryGetValue(request.Artifact.ArtifactHash, out var stored) &&
                stored.Artifact.ArtifactId == request.Artifact.ArtifactId &&
                stored.Manifest.Hashes.ManifestHash == request.Artifact.ManifestHash
                ? stored.ReportDirectory
                : null;
        }

        if (request.Snapshot is not null)
        {
            if (!_snapshots.TryGetValue(request.Snapshot.SnapshotId, out var snapshot))
                return null;

            var contentHash = Hash(string.Join("|", snapshot.FileHashes.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}:{x.Value}")));
            return Path.GetFileName(Path.TrimEndingDirectorySeparator(snapshot.SourceDirectory)) == request.Snapshot.SourceIdentity.SourceDirectoryName &&
                contentHash == request.Snapshot.SourceIdentity.ContentHash &&
                snapshot.FileHashes.Count == request.Snapshot.SourceIdentity.FileCount
                ? snapshot.SourceDirectory
                : null;
        }

        return request.ReportDirectory;
    }

    private static string GetGeneratedReportDirectory(PbirAuthoringGenerationRequest request) => request.Kind switch
    {
        PbirAuthoringGenerationRequestKind.V1 => Path.Combine(request.V1!.OutputBaseDirectory, request.V1.TargetDirectoryName),
        PbirAuthoringGenerationRequestKind.V2 => Path.Combine(request.V2!.OutputBaseDirectory, request.V2.TargetDirectoryName),
        PbirAuthoringGenerationRequestKind.V3 => Path.Combine(request.V3!.OutputBaseDirectory, request.V3.TargetDirectoryName),
        PbirAuthoringGenerationRequestKind.V4 => Path.Combine(request.V4!.OutputBaseDirectory, request.V4.TargetDirectoryName),
        PbirAuthoringGenerationRequestKind.V5 => Path.Combine(request.V5!.OutputBaseDirectory, request.V5.TargetDirectoryName),
        PbirAuthoringGenerationRequestKind.V6 => Path.Combine(request.V6!.OutputBaseDirectory, request.V6.TargetDirectoryName),
        PbirAuthoringGenerationRequestKind.V7 => Path.Combine(request.V7!.OutputBaseDirectory, request.V7.TargetDirectoryName),
        _ => throw new InvalidOperationException("Unsupported generation request version.")
    };

    private static PbirAuthoringDiagnostic ToDiagnostic(LocalPbirMutationDiagnostic diagnostic) =>
        new(diagnostic.Code, diagnostic.Field, diagnostic.ProjectionStatus == LocalPbirSemanticProjectionStatus.Invalid
            ? PbirAuthoringDiagnosticSeverity.Error
            : PbirAuthoringDiagnosticSeverity.Warning, diagnostic.Message);

    private static PbirAuthoringMutationPreview CreateMutationPreview(PbirMutationPlan plan, LocalPbirMutationOperation requestedOperation)
    {
        var operation = plan.Operations.SingleOrDefault() ?? requestedOperation;
        var pageId = operation.Target?.PageId ?? operation.Page?.PageId ?? string.Empty;
        var page = plan.Snapshot.IrState.Ir?.Pages.SingleOrDefault(page => page.PageId == pageId);
        var visual = operation.Target?.VisualId is null ? null : plan.Snapshot.IrState.Ir?.Visuals.SingleOrDefault(item => item.VisualId == operation.Target.VisualId);
        var diagnostics = plan.Diagnostics.Select(ToDiagnostic).ToArray();
        var payload = new PbirAuthoringMutationPreviewPayload(
            requestedOperation.Kind,
            requestedOperation.Kind is LocalPbirMutationOperationKind.AddPage or LocalPbirMutationOperationKind.RemovePage or LocalPbirMutationOperationKind.RenamePage or LocalPbirMutationOperationKind.MovePage
                ? new(
                    page?.DisplayName,
                    requestedOperation.DisplayName,
                    page?.Order,
                    requestedOperation.Page?.Order ?? requestedOperation.Order,
                    requestedOperation.Page?.PageId ?? page?.PageId,
                    requestedOperation.Kind == LocalPbirMutationOperationKind.RemovePage ? plan.Snapshot.IrState.Ir?.Pages.Where(item => item.PageId != pageId).Select(item => item.PageId).ToArray() ?? [] : [])
                : null,
            requestedOperation.Kind is LocalPbirMutationOperationKind.MoveVisual or LocalPbirMutationOperationKind.ResizeVisual
                ? new(
                    visual?.PageId,
                    requestedOperation.Target?.PageId ?? requestedOperation.Visual?.PageId ?? visual?.PageId,
                    visual?.Order,
                    requestedOperation.Order,
                    ToLayout(visual?.Layout),
                    ToLayout(requestedOperation.Layout, visual?.Layout))
                : null);
        return new(
            $"preview:{Hash($"{plan.MutationId}:{plan.Fingerprint}")[..24]}",
            requestedOperation.Kind,
            pageId,
            page?.DisplayName ?? string.Empty,
            requestedOperation.DisplayName ?? string.Empty,
            plan.AffectedPages,
            plan.AffectedVisuals,
            plan.Snapshot.IrState.Ir?.Pages.Select(item => item.PageId).Except(plan.AffectedPages, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray() ?? [],
            plan.Snapshot.IrState.Ir?.Visuals.Select(item => item.VisualId).Except(plan.AffectedVisuals, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray() ?? [],
            plan.AffectedPages.Count + plan.AffectedVisuals.Count,
            diagnostics,
            plan.IsValid && !plan.IsNoOp,
            plan.IsNoOp,
            payload,
            plan.Diffs.Select(diff => new PbirAuthoringSemanticDiff(
                diff.Kind, diff.ObjectId, diff.BeforePageId, diff.AfterPageId, diff.BeforeDisplayName, diff.AfterDisplayName,
                diff.BeforeOrder, diff.AfterOrder, diff.BeforeLayout, diff.AfterLayout)).ToArray());
    }

    private static LocalPbirGenerationVisualLayout? ToLayout(PbirIntermediateRepresentationVisualLayout? layout) =>
        layout is null ? null : new(layout.X, layout.Y, layout.Width, layout.Height);

    private static LocalPbirGenerationVisualLayout? ToLayout(LocalPbirGenerationLayout? proposed, PbirIntermediateRepresentationVisualLayout? current)
    {
        if (proposed is null && current is null) return null;
        return new(
            proposed?.X ?? current?.X ?? 0,
            proposed?.Y ?? current?.Y ?? 0,
            proposed?.Width ?? current?.Width ?? 1,
            proposed?.Height ?? current?.Height ?? 1);
    }

    private static PbirAuthoringDiagnostic ToDiagnostic(LocalPbirGenerationDiagnostic diagnostic) =>
        new(diagnostic.Code, diagnostic.Field, PbirAuthoringDiagnosticSeverity.Error, "The generation provider reported a bounded diagnostic.");

    private static PbirAuthoringDiagnostic ToDiagnostic(PbirDeployableDiagnostic diagnostic) =>
        new(diagnostic.Code, diagnostic.Path, PbirAuthoringDiagnosticSeverity.Error, "The schema validator reported a bounded diagnostic.");

    private static PbirDeployableSerializerRequest CreateImportedDeployableRequest(
        PbirIntermediateRepresentationState state,
        PbirSerializerRequest serializerRequest,
        LocalPbirMutationRequest request)
    {
        var ir = state.Ir!;
        var bindings = ir.Visuals.SelectMany(visual => (visual.Bindings ?? []).Select(binding => (visual, binding))).ToArray();
        var entries = bindings
            .Select(item => new PbirSemanticModelInventoryEntry(
                $"{item.binding.Kind.ToString().ToLowerInvariant()}:{item.binding.Entity}.{item.binding.Property}",
                item.binding.Token, item.binding.Entity, item.binding.Property,
                item.binding.Kind == PbirIntermediateRepresentationBindingKind.Measure ? PbirSemanticModelEntryKind.Measure : PbirSemanticModelEntryKind.Column))
            .GroupBy(entry => entry.EntryId, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(entry => entry.EntryId, StringComparer.Ordinal)
            .ToArray();
        var inventory = new PbirSemanticModelInventory(PbirSemanticModelInventoryContract.SchemaVersionV1, $"modelInventory:{ir.Metadata.IrId}", entries);
        var canonical = new PbirDeployableSerializerCanonicalJson();
        var inventoryHash = canonical.ComputeSha256(canonical.SerializeSemanticModelInventory(inventory));
        var visualBindings = ir.Visuals.OrderBy(visual => visual.VisualId, StringComparer.Ordinal).Select(visual => new PbirVisualBinding(
            visual.VisualId,
            (visual.Bindings ?? []).Select((binding, index) => new PbirRoleProjectionBinding(
                SerializerRole(visual.VisualType, binding), index + 1, binding.Token,
                $"{binding.Kind.ToString().ToLowerInvariant()}:{binding.Entity}.{binding.Property}",
                $"{binding.Entity}.{binding.Property}", $"{binding.Entity}.{binding.Property}", "none", null, null)).ToArray())).ToArray();
        return new(
            PbirDeployableSerializerRequestContract.SchemaVersionV1,
            $"pbirDeployableSerializerRequest:{request.MutationId}",
            serializerRequest.RequestId, serializerRequest.SchemaVersion, ir.Metadata.IrId,
            ir.Metadata.SchemaVersion, ir.Hashes.ContentHash, "modernPbir",
            PbirDeployableSchemaLock.DefinitionPropertiesSchemaVersion,
            PbirDeployableSchemaLock.DefinitionSchemaVersion,
            new(new(request.DatasetPath ?? "Imported.SemanticModel")),
            "modern-grid-1280x720/v1", inventory, inventory.InventoryRef, inventoryHash,
            visualBindings, PbirDeployableExecutionPolicy.NoAuthority);
    }

    private static PbirIntermediateRepresentation NormalizeImportedIr(PbirIntermediateRepresentation ir)
    {
        var orderedPages = ir.Pages.OrderBy(page => page.Order).ThenBy(page => page.PageId, StringComparer.Ordinal)
            .Select((page, index) => page with
            {
                NavigationBehavior = "pageTab",
                IntendedPurpose = string.IsNullOrWhiteSpace(page.IntendedPurpose) ? page.DisplayName ?? page.PageId : page.IntendedPurpose,
                Order = index + 1
            }).ToArray();
        var pageOrder = orderedPages.Select(page => page.PageId).ToDictionary(page => page, StringComparer.Ordinal);
        var visuals = ir.Visuals
            .GroupBy(visual => visual.PageId, StringComparer.Ordinal)
            .SelectMany(group => group.OrderBy(visual => visual.Order).ThenBy(visual => visual.VisualId, StringComparer.Ordinal)
                .Select((visual, index) => visual with
                {
                    Order = index + 1,
                    Placement = $"page:{visual.PageId}/slot:{index + 1}"
                }))
            .OrderBy(visual => pageOrder[visual.PageId]).ThenBy(visual => visual.Order).ThenBy(visual => visual.VisualId, StringComparer.Ordinal)
            .ToArray();
        var navigation = new PbirIntermediateRepresentationNavigation(
            orderedPages[0].PageId,
            orderedPages.Zip(orderedPages.Skip(1), (from, to) => new PbirIntermediateRepresentationPageTransition(from.PageId, to.PageId, $"{from.PageId}->{to.PageId}")).ToArray(),
            orderedPages.Select(page => $"page:{page.PageId}").Append($"landing:{orderedPages[0].PageId}").OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            []);
        var layout = new PbirIntermediateRepresentationLayout(
            orderedPages.Select(page => new PbirIntermediateRepresentationLayoutContainer(
                $"container:{page.PageId}", page.PageId, "imported report layout",
                visuals.Where(visual => visual.PageId == page.PageId).Select(visual => visual.VisualId).ToArray())).ToArray(),
            ["standard-8px-grid"],
            ["deterministic-grid", "visual-placement-preserved"],
            ["preserve-page-order", "preserve-visual-intent", "allow-future-serializer-layout-adaptation"]);
        return ir with { Pages = orderedPages, Visuals = visuals, Navigation = navigation, Layout = layout };
    }

    private static string SerializerRole(
        string visualType,
        PbirIntermediateRepresentationBinding binding) =>
        visualType switch
        {
            "card" => "Fields",
            "table" => "Values",
            "clusteredColumnChart" or "barChart" or "pieChart" or "lineChart" when binding.Role == PbirIntermediateRepresentationBindingRole.Value => "Y",
            "clusteredColumnChart" or "barChart" or "pieChart" or "lineChart" => binding.Role.ToString(),
            _ => binding.Role.ToString()
        };

    private static PbirAuthoringFidelity CreateFidelity(
        PbirLocalReportImportSnapshot snapshot,
        PbirDeployableArtifact artifact,
        bool mutationApplied)
    {
        var source = ReadFiles(snapshot.SourceDirectory);
        var output = artifact.Files.ToDictionary(file => file.RelativePath, file => file.Content, StringComparer.Ordinal);
        var comparison = new PbirRoundTripFidelityService().Compare(source, output);
        var classification = comparison.UnexpectedPaths.Count > 0
            ? PbirFidelityClassification.UnexpectedDifference
            : mutationApplied
                ? PbirFidelityClassification.ExpectedNormalizedDifference
                : PbirFidelityClassification.ByteIdentical;
        return new(classification, comparison.PreservedPaths.Count, comparison.ChangedPaths.Count, comparison.UnexpectedPaths.Count);
    }

    private static IReadOnlyDictionary<string, string> ReadFiles(string sourceDirectory)
    {
        var definition = Path.Combine(sourceDirectory, "definition");
        if (!Directory.Exists(definition)) return new Dictionary<string, string>(StringComparer.Ordinal);
        return Directory.GetFiles(definition, "*.json", SearchOption.AllDirectories)
            .ToDictionary(path => path[(definition.Length + 1)..], File.ReadAllText, StringComparer.Ordinal);
    }

    private static PbirAuthoringRpcErrorCategory ErrorCategoryFor(IReadOnlyList<LocalPbirGenerationDiagnostic> diagnostics) =>
        diagnostics.Any(diagnostic => diagnostic.Code.Contains("UNSUPPORTED", StringComparison.OrdinalIgnoreCase))
            ? PbirAuthoringRpcErrorCategory.UnsupportedAuthoring
            : PbirAuthoringRpcErrorCategory.ValidationFailed;

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static PbirAuthoringRpcResponse Failure(
        PbirAuthoringRpcOperation operation,
        PbirAuthoringRpcErrorCategory category,
        string code,
        string summary) =>
        new(
            PbirAuthoringRpcContract.SchemaVersionV1,
            operation,
            false,
            [],
            new(category, code, summary),
            null,
            null,
            null,
            new(0, 0, 0, 0));

    private static PbirAuthoringRpcResponse Failure(
        PbirAuthoringRpcOperation operation,
        PbirAuthoringRpcErrorCategory category,
        string code,
        string summary,
        IReadOnlyList<PbirAuthoringDiagnostic> diagnostics) =>
        Failure(operation, category, code, summary) with { Diagnostics = diagnostics };
}
