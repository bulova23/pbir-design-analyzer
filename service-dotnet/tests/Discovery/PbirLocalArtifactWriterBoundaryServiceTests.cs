using System.Reflection;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirLocalArtifactWriterBoundaryServiceTests
{
    [Fact(DisplayName = "PBIR local writer boundary creates deterministic dry-run write manifests with planned paths and hashes")]
    public void CreateWriteManifest_ValidDryRunRequest_CreatesDeterministicManifest()
    {
        var inputs = CreateReadyWriterInputs();
        var request = PbirLocalWriteRequest.LocalDryRun(
            requestId: "pbirLocalWriteRequest:executive-summary",
            sourceIrRef: inputs.IrState.Ir!.Metadata.IrId,
            sourceIrSchemaVersion: inputs.IrState.Ir.Metadata.SchemaVersion,
            sourceIrContentHash: inputs.IrState.Ir.Hashes.ContentHash,
            sourcePreviewManifestRef: inputs.PreviewState.Manifest!.Metadata.ManifestId,
            sourcePreviewManifestSchemaVersion: inputs.PreviewState.Manifest.SchemaVersion,
            sourcePreviewManifestHash: inputs.PreviewState.Manifest.Hashes.ManifestHash,
            targetOutputRoot: "local-writer-output",
            requestedArtifactTypes:
            [
                PbirLocalWriteArtifactType.PreviewMarkdown,
                PbirLocalWriteArtifactType.PreviewJson,
                PbirLocalWriteArtifactType.IrJson,
                PbirLocalWriteArtifactType.ManifestJson,
                PbirLocalWriteArtifactType.DiagnosticsMarkdown
            ]);
        var generatedUtc = DateTimeOffset.Parse("2026-06-26T16:00:00+00:00");
        var service = new PbirLocalArtifactWriterBoundaryService();

        var first = service.CreateWriteManifest(
            inputs.PreviewState.Manifest,
            inputs.IrState,
            request,
            existingLocalRelativePaths: [],
            generatedUtc);
        var second = service.CreateWriteManifest(
            inputs.PreviewState.Manifest,
            inputs.IrState,
            request,
            existingLocalRelativePaths: [],
            generatedUtc);

        Assert.Equal(PbirLocalArtifactWriterReadinessState.Planned, first.Readiness);
        Assert.True(first.Safety.IsAllowed);
        Assert.NotNull(first.Manifest);
        Assert.Equal(Serialize(first.Manifest), Serialize(second.Manifest));
        Assert.Equal(PbirLocalWriteManifestContract.SchemaVersionV1, first.Manifest!.SchemaVersion);
        Assert.Equal(PbirLocalArtifactWriterContract.SchemaVersionV1, first.Manifest.Writer.SchemaVersion);
        Assert.Equal(request.RequestId, first.Manifest.SourceLineage.WriteRequestRef);
        Assert.Equal(inputs.IrState.Ir.Metadata.IrId, first.Manifest.SourceLineage.PbirIrRef);
        Assert.Equal(inputs.PreviewState.Manifest.Metadata.ManifestId, first.Manifest.SourceLineage.PreviewManifestRef);
        Assert.Empty(first.Manifest.RejectedArtifacts);
        Assert.Empty(first.Manifest.OverwriteRisk.RiskPaths);
        Assert.False(first.Manifest.OverwriteRisk.HasRisk);

        var relativePaths = first.Manifest.PlannedFiles.Select(file => file.RelativePath).ToArray();
        Assert.Equal(relativePaths.OrderBy(path => path, StringComparer.Ordinal), relativePaths);
        Assert.Contains("pbir-local-writer/v1/preview/report-preview.md", relativePaths);
        Assert.Contains("pbir-local-writer/v1/preview/report-preview.json", relativePaths);
        Assert.Contains("pbir-local-writer/v1/ir/canonical-pbir-ir.json", relativePaths);
        Assert.Contains("pbir-local-writer/v1/manifests/pbir-preview-manifest.json", relativePaths);
        Assert.Contains("pbir-local-writer/v1/diagnostics/local-write-diagnostics.md", relativePaths);
        Assert.All(first.Manifest.PlannedFiles, file =>
        {
            Assert.StartsWith("local-writer-output/", file.IntendedPath, StringComparison.Ordinal);
            Assert.Equal(64, file.HashSha256.Length);
            Assert.True(file.ByteLength > 0);
            Assert.False(file.WillWrite);
        });
        Assert.Contains(inputs.IrState.Ir.Metadata.IrId, first.Manifest.SourceLineage.ImmutableLineage);
        Assert.Contains(inputs.PreviewState.Manifest.Metadata.ManifestId, first.Manifest.SourceLineage.ImmutableLineage);
        Assert.Contains(first.Manifest.Metadata.ManifestId, first.Manifest.SourceLineage.ImmutableLineage);
        Assert.Equal(64, first.Manifest.Hashes.InputHash.Length);
        Assert.Equal(64, first.Manifest.Hashes.FileSetHash.Length);
        Assert.Equal(64, first.Manifest.Hashes.ManifestHash.Length);
        Assert.Contains("No files will be written by this boundary.", first.Manifest.Warnings);
        Assert.Equal(PbirLocalRollbackActionKind.NoOpDryRun, first.Manifest.RollbackPlan.Actions.Single(action => action.RelativePath == "pbir-local-writer/v1/preview/report-preview.md").ActionKind);
    }

    [Fact(DisplayName = "PBIR local writer boundary detects overwrite risk and generates rollback plan without writing files")]
    public void CreateWriteManifest_ExistingPlannedPath_DetectsOverwriteRiskAndRollbackPlan()
    {
        var inputs = CreateReadyWriterInputs();
        var request = PbirLocalWriteRequest.LocalDryRun(
            requestId: "pbirLocalWriteRequest:overwrite-risk",
            sourceIrRef: inputs.IrState.Ir!.Metadata.IrId,
            sourceIrSchemaVersion: inputs.IrState.Ir.Metadata.SchemaVersion,
            sourceIrContentHash: inputs.IrState.Ir.Hashes.ContentHash,
            sourcePreviewManifestRef: inputs.PreviewState.Manifest!.Metadata.ManifestId,
            sourcePreviewManifestSchemaVersion: inputs.PreviewState.Manifest.SchemaVersion,
            sourcePreviewManifestHash: inputs.PreviewState.Manifest.Hashes.ManifestHash,
            targetOutputRoot: "local-writer-output",
            requestedArtifactTypes: [PbirLocalWriteArtifactType.PreviewMarkdown, PbirLocalWriteArtifactType.PreviewJson]);
        var existingPath = "local-writer-output/pbir-local-writer/v1/preview/report-preview.md";

        var state = new PbirLocalArtifactWriterBoundaryService().CreateWriteManifest(
            inputs.PreviewState.Manifest,
            inputs.IrState,
            request,
            existingLocalRelativePaths: [existingPath],
            DateTimeOffset.Parse("2026-06-26T16:00:00+00:00"));

        Assert.Equal(PbirLocalArtifactWriterReadinessState.PlannedWithOverwriteRisk, state.Readiness);
        Assert.NotNull(state.Manifest);
        Assert.True(state.Manifest!.OverwriteRisk.HasRisk);
        Assert.Contains(existingPath, state.Manifest.OverwriteRisk.RiskPaths);
        Assert.Contains("Overwrite risk detected for planned local artifacts.", state.Manifest.Warnings);
        var plannedRiskFile = Assert.Single(state.Manifest.PlannedFiles, file => file.IntendedPath == existingPath);
        Assert.True(plannedRiskFile.OverwriteRisk);
        Assert.Equal(PbirLocalRollbackActionKind.RestoreExistingLocalFile, state.Manifest.RollbackPlan.Actions.Single(action => action.IntendedPath == existingPath).ActionKind);
        Assert.Contains(existingPath, state.Manifest.RollbackPlan.ProtectedExistingPaths);
    }

    [Theory(DisplayName = "PBIR local writer safety gate rejects deployable, non-local, missing dry-run, unsafe overwrite, execution, provider, CLI, API, and deployment requests")]
    [MemberData(nameof(UnsafeWriteRequests))]
    public void CreateWriteManifest_UnsafeRequest_IsRejected(object requestObject, string expectedReason)
    {
        var inputs = CreateReadyWriterInputs();
        var request = Assert.IsType<PbirLocalWriteRequest>(requestObject);

        var state = new PbirLocalArtifactWriterBoundaryService().CreateWriteManifest(
            inputs.PreviewState.Manifest!,
            inputs.IrState,
            request,
            existingLocalRelativePaths: [],
            DateTimeOffset.Parse("2026-06-26T16:00:00+00:00"));

        Assert.Equal(PbirLocalArtifactWriterReadinessState.Rejected, state.Readiness);
        Assert.Null(state.Manifest);
        Assert.False(state.Safety.IsAllowed);
        Assert.Contains(expectedReason, state.Safety.Reasons);
        Assert.Contains(expectedReason, state.Diagnostics.SafetyRejections);
    }

    [Fact(DisplayName = "PBIR local writer boundary rejects source IR and preview manifest mismatches")]
    public void CreateWriteManifest_SourceReferenceMismatch_IsRejected()
    {
        var inputs = CreateReadyWriterInputs();
        var request = PbirLocalWriteRequest.LocalDryRun(
            requestId: "pbirLocalWriteRequest:mismatch",
            sourceIrRef: inputs.IrState.Ir!.Metadata.IrId,
            sourceIrSchemaVersion: inputs.IrState.Ir.Metadata.SchemaVersion,
            sourceIrContentHash: "not-the-ir-content-hash",
            sourcePreviewManifestRef: inputs.PreviewState.Manifest!.Metadata.ManifestId,
            sourcePreviewManifestSchemaVersion: inputs.PreviewState.Manifest.SchemaVersion,
            sourcePreviewManifestHash: inputs.PreviewState.Manifest.Hashes.ManifestHash,
            targetOutputRoot: "local-writer-output",
            requestedArtifactTypes: [PbirLocalWriteArtifactType.PreviewMarkdown]);

        var state = new PbirLocalArtifactWriterBoundaryService().CreateWriteManifest(
            inputs.PreviewState.Manifest,
            inputs.IrState,
            request,
            existingLocalRelativePaths: [],
            DateTimeOffset.Parse("2026-06-26T16:00:00+00:00"));

        Assert.Equal(PbirLocalArtifactWriterReadinessState.Rejected, state.Readiness);
        Assert.Null(state.Manifest);
        Assert.Contains("write request PBIR IR content hash must match the IR content hash.", state.Safety.Reasons);
    }

    [Fact(DisplayName = "PBIR local writer boundary writes no files and exposes no deployable PBIR or execution surface")]
    public void PbirLocalWriterBoundary_RemainsDryRunOnlyAndNonExecuting()
    {
        var targetRoot = "pbir-local-writer-test-output-should-not-exist";
        Assert.False(Directory.Exists(targetRoot));
        var inputs = CreateReadyWriterInputs();
        var request = PbirLocalWriteRequest.LocalDryRun(
            requestId: "pbirLocalWriteRequest:no-files",
            sourceIrRef: inputs.IrState.Ir!.Metadata.IrId,
            sourceIrSchemaVersion: inputs.IrState.Ir.Metadata.SchemaVersion,
            sourceIrContentHash: inputs.IrState.Ir.Hashes.ContentHash,
            sourcePreviewManifestRef: inputs.PreviewState.Manifest!.Metadata.ManifestId,
            sourcePreviewManifestSchemaVersion: inputs.PreviewState.Manifest.SchemaVersion,
            sourcePreviewManifestHash: inputs.PreviewState.Manifest.Hashes.ManifestHash,
            targetOutputRoot: targetRoot,
            requestedArtifactTypes:
            [
                PbirLocalWriteArtifactType.PreviewMarkdown,
                PbirLocalWriteArtifactType.PreviewJson,
                PbirLocalWriteArtifactType.IrJson,
                PbirLocalWriteArtifactType.ManifestJson,
                PbirLocalWriteArtifactType.DiagnosticsMarkdown
            ]);

        var state = new PbirLocalArtifactWriterBoundaryService().CreateWriteManifest(
            inputs.PreviewState.Manifest,
            inputs.IrState,
            request,
            existingLocalRelativePaths: [],
            DateTimeOffset.Parse("2026-06-26T16:00:00+00:00"));

        Assert.NotNull(state.Manifest);
        Assert.False(Directory.Exists(targetRoot));
        Assert.DoesNotContain(state.Manifest!.PlannedFiles, file =>
            file.RelativePath.EndsWith("report.json", StringComparison.OrdinalIgnoreCase) ||
            file.RelativePath.EndsWith("definition.pbir", StringComparison.OrdinalIgnoreCase) ||
            file.RelativePath.EndsWith(".pbir", StringComparison.OrdinalIgnoreCase) ||
            file.RelativePath.EndsWith(".bim", StringComparison.OrdinalIgnoreCase) ||
            file.RelativePath.EndsWith(".tmdl", StringComparison.OrdinalIgnoreCase) ||
            file.RelativePath.EndsWith(".pbip", StringComparison.OrdinalIgnoreCase));

        var forbiddenTokens = new[]
        {
            "GeneratePbir",
            "SerializePbir",
            "InvokeProvider",
            "InvokeMicrosoftApi",
            "InvokeApi",
            "InvokeCli",
            "Deploy",
            "RunSkill",
            "Publish",
            "Execute"
        };
        Type[] types =
        [
            typeof(PbirLocalArtifactWriterBoundaryService),
            typeof(PbirLocalArtifactWriterSafetyGate),
            typeof(PbirLocalWriteRequest),
            typeof(PbirLocalWriteManifest),
            typeof(PbirLocalArtifactWriterState)
        ];

        foreach (var type in types)
        {
            Assert.DoesNotContain(forbiddenTokens, token => type.Name.Contains(token, StringComparison.OrdinalIgnoreCase));

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName)
                {
                    continue;
                }

                Assert.DoesNotContain(forbiddenTokens, token => method.Name.Contains(token, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    public static IEnumerable<object[]> UnsafeWriteRequests()
    {
        var inputs = CreateReadyWriterInputs();
        var valid = PbirLocalWriteRequest.LocalDryRun(
            requestId: "pbirLocalWriteRequest:unsafe",
            sourceIrRef: inputs.IrState.Ir!.Metadata.IrId,
            sourceIrSchemaVersion: inputs.IrState.Ir.Metadata.SchemaVersion,
            sourceIrContentHash: inputs.IrState.Ir.Hashes.ContentHash,
            sourcePreviewManifestRef: inputs.PreviewState.Manifest!.Metadata.ManifestId,
            sourcePreviewManifestSchemaVersion: inputs.PreviewState.Manifest.SchemaVersion,
            sourcePreviewManifestHash: inputs.PreviewState.Manifest.Hashes.ManifestHash,
            targetOutputRoot: "local-writer-output",
            requestedArtifactTypes: [PbirLocalWriteArtifactType.PreviewMarkdown]);

        yield return [valid with { RequestedArtifactTypes = [PbirLocalWriteArtifactType.ReportJson] }, "deployable PBIR artifact requests are not allowed: reportJson."];
        yield return [valid with { RequestedArtifactTypes = [PbirLocalWriteArtifactType.DefinitionPbir] }, "deployable PBIR artifact requests are not allowed: definitionPbir."];
        yield return [valid with { RequestedArtifactTypes = [PbirLocalWriteArtifactType.ModelBim] }, "deployable PBIR artifact requests are not allowed: modelBim."];
        yield return [valid with { RequestedArtifactTypes = [PbirLocalWriteArtifactType.Tmdl] }, "deployable PBIR artifact requests are not allowed: tmdl."];
        yield return [valid with { RequestedArtifactTypes = [PbirLocalWriteArtifactType.PbipProject] }, "deployable PBIR artifact requests are not allowed: pbipProject."];
        yield return [valid with { RequestedArtifactTypes = [PbirLocalWriteArtifactType.DeployableReport] }, "deployable PBIR artifact requests are not allowed: deployableReport."];
        yield return [valid with { TargetOutputRoot = "/tmp/local-writer-output" }, "target output root must be a local relative path."];
        yield return [valid with { DryRun = null }, "dry-run flag must be present and true."];
        yield return [valid with { DryRun = false }, "dry-run flag must be present and true."];
        yield return [valid with { OverwritePolicy = PbirLocalOverwritePolicy.OverwriteExisting }, "overwrite policy must not allow replacing existing files."];
        yield return [valid with { ProviderInvocationRequested = true }, "provider invocation requests are not allowed."];
        yield return [valid with { MicrosoftApiRequested = true }, "Microsoft API requests are not allowed."];
        yield return [valid with { CliRequested = true }, "CLI requests are not allowed."];
        yield return [valid with { MicrosoftSkillsExecutionRequested = true }, "Microsoft Skills execution requests are not allowed."];
        yield return [valid with { DeploymentRequested = true }, "deployment requests are not allowed."];
    }

    private static (PbirIntermediateRepresentationState IrState, PbirPreviewSerializerState PreviewState) CreateReadyWriterInputs()
    {
        var irInputs = PbirIntermediateRepresentationServiceTests.CreateReadyIrInputs();
        var irService = new PbirIntermediateRepresentationService();
        var irState = irService.CreateIntermediateRepresentation(
            irInputs.ManifestState,
            irInputs.SpecificationState,
            DateTimeOffset.Parse("2026-06-26T14:00:00+00:00"));
        var serializerRequest = irService.CreateSerializerRequest(irState);
        var previewOptions = PbirPreviewSerializerOptions.LocalPreview(
            outputRoot: "preview-artifacts",
            outputTypes:
            [
                PbirPreviewOutputType.Markdown,
                PbirPreviewOutputType.Json,
                PbirPreviewOutputType.VisualLayoutSummary,
                PbirPreviewOutputType.SemanticBindingSummary,
                PbirPreviewOutputType.NavigationSummary
            ]);
        var previewState = new PbirPreviewSerializerService().CreatePreviewArtifacts(
            irState,
            serializerRequest,
            previewOptions,
            DateTimeOffset.Parse("2026-06-26T15:00:00+00:00"));

        return (irState, previewState);
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
