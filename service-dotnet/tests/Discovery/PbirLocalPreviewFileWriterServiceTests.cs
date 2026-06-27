using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirLocalPreviewFileWriterServiceTests
{
    [Fact(DisplayName = "PBIR local preview writer writes allowed preview artifacts with manifest hashes preserved")]
    public void WritePreviewFiles_AllowedArtifacts_WritesFilesAndPreservesHashes()
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateApprovedWriteInputs(
            requestedArtifactTypes:
            [
                PbirLocalWriteArtifactType.PreviewMarkdown,
                PbirLocalWriteArtifactType.PreviewJson,
                PbirLocalWriteArtifactType.IrJson,
                PbirLocalWriteArtifactType.ManifestJson,
                PbirLocalWriteArtifactType.DiagnosticsMarkdown
            ]);
        var service = new PbirLocalPreviewFileWriterService();

        var state = service.WritePreviewFiles(
            inputs.PreviewState.Output!,
            inputs.PreviewState.Manifest!,
            inputs.IrState,
            inputs.Request,
            inputs.WriteManifest,
            output.Path,
            DateTimeOffset.Parse("2026-06-26T17:00:00+00:00"));

        Assert.Equal(PbirLocalPreviewFileWriterReadinessState.Written, state.Readiness);
        Assert.True(state.Safety.IsAllowed);
        Assert.NotNull(state.Result);
        Assert.Equal(PbirLocalPreviewWriteResultContract.SchemaVersionV1, state.Result!.SchemaVersion);
        Assert.Equal(PbirLocalPreviewFileWriterContract.SchemaVersionV1, state.Result.Writer.SchemaVersion);
        Assert.Equal(inputs.WriteManifest.Metadata.ManifestId, state.Result.SourceLineage.SourceWriteManifestRef);
        Assert.Equal(inputs.WriteManifest.Hashes.ManifestHash, state.Result.SourceLineage.SourceWriteManifestHash);
        Assert.Equal(inputs.WriteManifest.Metadata.ManifestId, state.Result.RollbackPlanReference.SourceWriteManifestRef);
        Assert.Empty(state.Result.SkippedFiles);
        Assert.Empty(state.Result.RejectedFiles);
        Assert.Equal(5, state.Result.WrittenFiles.Count);

        foreach (var writtenFile in state.Result.WrittenFiles)
        {
            var plannedFile = inputs.WriteManifest.PlannedFiles.Single(file => file.RelativePath == writtenFile.RelativePath);
            var physicalPath = Path.Combine(output.Path, plannedFile.IntendedPath);

            Assert.True(File.Exists(physicalPath), $"Expected {physicalPath} to exist.");
            Assert.Equal(physicalPath, writtenFile.PhysicalPath);
            Assert.Equal(plannedFile.IntendedPath, writtenFile.IntendedPath);
            Assert.Equal(plannedFile.ContentType, writtenFile.ContentType);
            Assert.Equal(plannedFile.ByteLength, writtenFile.ByteLength);
            Assert.Equal(plannedFile.HashSha256, writtenFile.HashSha256);
            Assert.Equal(plannedFile.HashSha256, ComputeSha256(File.ReadAllText(physicalPath, Encoding.UTF8)));
        }

        var writtenPaths = Directory.EnumerateFiles(output.Path, "*", SearchOption.AllDirectories)
            .Select(path => path.Replace('\\', '/'))
            .ToArray();
        Assert.Contains(writtenPaths, path => path.EndsWith("pbir-local-writer/v1/preview/report-preview.md", StringComparison.Ordinal));
        Assert.Contains(writtenPaths, path => path.EndsWith("pbir-local-writer/v1/preview/report-preview.json", StringComparison.Ordinal));
        Assert.Contains(writtenPaths, path => path.EndsWith("pbir-local-writer/v1/ir/canonical-pbir-ir.json", StringComparison.Ordinal));
        Assert.Contains(writtenPaths, path => path.EndsWith("pbir-local-writer/v1/manifests/pbir-preview-manifest.json", StringComparison.Ordinal));
        Assert.Contains(writtenPaths, path => path.EndsWith("pbir-local-writer/v1/diagnostics/local-write-diagnostics.md", StringComparison.Ordinal));
        Assert.DoesNotContain(writtenPaths, IsForbiddenDeployablePath);
        Assert.Equal(64, state.Result.Hashes.InputHash.Length);
        Assert.Equal(64, state.Result.Hashes.FileSetHash.Length);
        Assert.Equal(64, state.Result.Hashes.ResultHash.Length);
    }

    [Fact(DisplayName = "PBIR local preview writer allows overwrite only when the existing file hash matches the approved manifest")]
    public void WritePreviewFiles_ExistingFileHashMatches_AllowsHashMatchedOverwrite()
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateApprovedWriteInputs(
            requestedArtifactTypes: [PbirLocalWriteArtifactType.PreviewMarkdown],
            overwritePolicy: PbirLocalOverwritePolicy.AllowOverwriteOnlyWhenHashMatches);
        var plannedFile = inputs.WriteManifest.PlannedFiles.Single();
        var physicalPath = Path.Combine(output.Path, plannedFile.IntendedPath);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
        var sourceFile = inputs.PreviewState.Output!.GeneratedFiles.Single(file => file.OutputType == PbirPreviewOutputType.Markdown);
        File.WriteAllText(physicalPath, sourceFile.Content, Encoding.UTF8);

        var state = new PbirLocalPreviewFileWriterService().WritePreviewFiles(
            inputs.PreviewState.Output,
            inputs.PreviewState.Manifest!,
            inputs.IrState,
            inputs.Request,
            inputs.WriteManifest,
            output.Path,
            DateTimeOffset.Parse("2026-06-26T17:00:00+00:00"));

        Assert.Equal(PbirLocalPreviewFileWriterReadinessState.Written, state.Readiness);
        Assert.NotNull(state.Result);
        Assert.Single(state.Result!.WrittenFiles);
        Assert.Empty(state.Result.RejectedFiles);
        Assert.Equal(plannedFile.HashSha256, ComputeSha256(File.ReadAllText(physicalPath, Encoding.UTF8)));
    }

    [Fact(DisplayName = "PBIR local preview writer rejects existing file overwrite when hashes do not match")]
    public void WritePreviewFiles_ExistingFileHashMismatch_RejectsWithoutOverwriting()
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateApprovedWriteInputs(
            requestedArtifactTypes: [PbirLocalWriteArtifactType.PreviewMarkdown],
            overwritePolicy: PbirLocalOverwritePolicy.AllowOverwriteOnlyWhenHashMatches);
        var plannedFile = inputs.WriteManifest.PlannedFiles.Single();
        var physicalPath = Path.Combine(output.Path, plannedFile.IntendedPath);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
        File.WriteAllText(physicalPath, "existing different content", Encoding.UTF8);

        var state = new PbirLocalPreviewFileWriterService().WritePreviewFiles(
            inputs.PreviewState.Output!,
            inputs.PreviewState.Manifest!,
            inputs.IrState,
            inputs.Request,
            inputs.WriteManifest,
            output.Path,
            DateTimeOffset.Parse("2026-06-26T17:00:00+00:00"));

        Assert.Equal(PbirLocalPreviewFileWriterReadinessState.Rejected, state.Readiness);
        Assert.Null(state.Result);
        Assert.False(state.Safety.IsAllowed);
        Assert.Contains("existing file hash must match the approved manifest hash before overwrite.", state.Safety.Reasons);
        Assert.Equal("existing different content", File.ReadAllText(physicalPath, Encoding.UTF8));
    }

    [Theory(DisplayName = "PBIR local preview writer safety gate rejects deployable paths, non-local output, blind overwrite, missing rollback, and unapproved manifests")]
    [MemberData(nameof(UnsafeWriterInputs))]
    public void WritePreviewFiles_UnsafeInputs_AreRejected(object mutationObject, string expectedReason)
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateApprovedWriteInputs(requestedArtifactTypes: [PbirLocalWriteArtifactType.PreviewMarkdown]);
        var mutation = Assert.IsType<Func<ApprovedWriteInputs, string, UnsafeWriterCall>>(mutationObject);
        var unsafeCall = mutation(inputs, output.Path);

        var state = new PbirLocalPreviewFileWriterService().WritePreviewFiles(
            unsafeCall.PreviewArtifact,
            unsafeCall.PreviewManifest,
            unsafeCall.IrState,
            unsafeCall.Request,
            unsafeCall.WriteManifest,
            unsafeCall.OutputBaseDirectory,
            DateTimeOffset.Parse("2026-06-26T17:00:00+00:00"));

        Assert.Equal(PbirLocalPreviewFileWriterReadinessState.Rejected, state.Readiness);
        Assert.Null(state.Result);
        Assert.False(state.Safety.IsAllowed);
        Assert.Contains(expectedReason, state.Safety.Reasons);
        Assert.False(Directory.EnumerateFiles(output.Path, "*", SearchOption.AllDirectories).Any());
    }

    [Fact(DisplayName = "PBIR local preview writer exposes no deployable PBIR or external execution surface")]
    public void PbirLocalPreviewFileWriter_RemainsPreviewOnlyAndNonExecuting()
    {
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
            typeof(PbirLocalPreviewFileWriterService),
            typeof(PbirLocalPreviewFileWriterSafetyGate),
            typeof(PbirLocalPreviewWriteResult),
            typeof(PbirLocalPreviewFileWriterState)
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

    public static IEnumerable<object[]> UnsafeWriterInputs()
    {
        yield return [
            Mutate((inputs, outputPath) => new UnsafeWriterCall(
                inputs.PreviewState.Output!,
                inputs.PreviewState.Manifest!,
                inputs.IrState,
                inputs.Request,
                ReplacePlannedFile(inputs.WriteManifest, inputs.WriteManifest.PlannedFiles.Single() with
                {
                    ArtifactType = PbirLocalWriteArtifactType.ReportJson,
                    RelativePath = "pbir-local-writer/v1/report/report.json",
                    IntendedPath = "local-preview-output/pbir-local-writer/v1/report/report.json"
                }),
                outputPath)),
            "deployable PBIR artifact paths are not allowed: report.json."
        ];
        yield return [
            Mutate((inputs, outputPath) => new UnsafeWriterCall(
                inputs.PreviewState.Output!,
                inputs.PreviewState.Manifest!,
                inputs.IrState,
                inputs.Request,
                ReplacePlannedFile(inputs.WriteManifest, inputs.WriteManifest.PlannedFiles.Single() with
                {
                    ArtifactType = PbirLocalWriteArtifactType.DefinitionPbir,
                    RelativePath = "pbir-local-writer/v1/definition.pbir",
                    IntendedPath = "local-preview-output/pbir-local-writer/v1/definition.pbir"
                }),
                outputPath)),
            "deployable PBIR artifact paths are not allowed: definition.pbir."
        ];
        yield return [
            Mutate((inputs, outputPath) => new UnsafeWriterCall(
                inputs.PreviewState.Output!,
                inputs.PreviewState.Manifest!,
                inputs.IrState,
                inputs.Request,
                ReplacePlannedFile(inputs.WriteManifest, inputs.WriteManifest.PlannedFiles.Single() with
                {
                    ArtifactType = PbirLocalWriteArtifactType.ModelBim,
                    RelativePath = "pbir-local-writer/v1/model.bim",
                    IntendedPath = "local-preview-output/pbir-local-writer/v1/model.bim"
                }),
                outputPath)),
            "deployable PBIR artifact paths are not allowed: model.bim."
        ];
        yield return [
            Mutate((inputs, outputPath) => new UnsafeWriterCall(
                inputs.PreviewState.Output!,
                inputs.PreviewState.Manifest!,
                inputs.IrState,
                inputs.Request,
                ReplacePlannedFile(inputs.WriteManifest, inputs.WriteManifest.PlannedFiles.Single() with
                {
                    ArtifactType = PbirLocalWriteArtifactType.Tmdl,
                    RelativePath = "pbir-local-writer/v1/model/tables/table.tmdl",
                    IntendedPath = "local-preview-output/pbir-local-writer/v1/model/tables/table.tmdl"
                }),
                outputPath)),
            "deployable PBIR artifact paths are not allowed: TMDL."
        ];
        yield return [
            Mutate((inputs, outputPath) => new UnsafeWriterCall(
                inputs.PreviewState.Output!,
                inputs.PreviewState.Manifest!,
                inputs.IrState,
                inputs.Request,
                ReplacePlannedFile(inputs.WriteManifest, inputs.WriteManifest.PlannedFiles.Single() with
                {
                    ArtifactType = PbirLocalWriteArtifactType.PbipProject,
                    RelativePath = "Sales.Report/report.json",
                    IntendedPath = "local-preview-output/Sales.Report/report.json"
                }),
                outputPath)),
            "PBIP project structure paths are not allowed."
        ];
        yield return [
            Mutate((inputs, _) => new UnsafeWriterCall(
                inputs.PreviewState.Output!,
                inputs.PreviewState.Manifest!,
                inputs.IrState,
                inputs.Request,
                inputs.WriteManifest,
                "https://contoso.example/output")),
            "output base directory must be a local filesystem path."
        ];
        yield return [
            Mutate((inputs, outputPath) => new UnsafeWriterCall(
                inputs.PreviewState.Output!,
                inputs.PreviewState.Manifest!,
                inputs.IrState,
                inputs.Request with { OverwritePolicy = PbirLocalOverwritePolicy.OverwriteExisting },
                inputs.WriteManifest,
                outputPath)),
            "blind overwrite is not supported."
        ];
        yield return [
            Mutate((inputs, outputPath) => new UnsafeWriterCall(
                inputs.PreviewState.Output!,
                inputs.PreviewState.Manifest!,
                inputs.IrState,
                inputs.Request with { RollbackPolicy = PbirLocalRollbackPolicy.None },
                inputs.WriteManifest,
                outputPath)),
            "rollback metadata is required before local preview writes."
        ];
        yield return [
            Mutate((inputs, outputPath) => new UnsafeWriterCall(
                inputs.PreviewState.Output!,
                inputs.PreviewState.Manifest!,
                inputs.IrState,
                inputs.Request,
                ReplacePlannedFile(inputs.WriteManifest, inputs.WriteManifest.PlannedFiles.Single() with
                {
                    HashSha256 = new string('0', 64)
                }),
                outputPath)),
            "planned file hash must match deterministic writer content."
        ];
        yield return [
            Mutate((inputs, outputPath) => new UnsafeWriterCall(
                inputs.PreviewState.Output!,
                inputs.PreviewState.Manifest!,
                inputs.IrState,
                inputs.Request,
                inputs.WriteManifest with { RollbackPlan = inputs.WriteManifest.RollbackPlan with { Actions = [] } },
                outputPath)),
            "rollback plan must cover every planned file."
        ];
    }

    private static Func<ApprovedWriteInputs, string, UnsafeWriterCall> Mutate(Func<ApprovedWriteInputs, string, UnsafeWriterCall> mutation)
    {
        return mutation;
    }

    private static ApprovedWriteInputs CreateApprovedWriteInputs(
        IReadOnlyList<PbirLocalWriteArtifactType> requestedArtifactTypes,
        PbirLocalOverwritePolicy overwritePolicy = PbirLocalOverwritePolicy.FailIfExists)
    {
        var irInputs = PbirIntermediateRepresentationServiceTests.CreateReadyIrInputs();
        var irService = new PbirIntermediateRepresentationService();
        var irState = irService.CreateIntermediateRepresentation(
            irInputs.ManifestState,
            irInputs.SpecificationState,
            DateTimeOffset.Parse("2026-06-26T14:00:00+00:00"));
        var serializerRequest = irService.CreateSerializerRequest(irState);
        var previewState = new PbirPreviewSerializerService().CreatePreviewArtifacts(
            irState,
            serializerRequest,
            PbirPreviewSerializerOptions.LocalPreview(
                outputRoot: "preview-artifacts",
                outputTypes:
                [
                    PbirPreviewOutputType.Markdown,
                    PbirPreviewOutputType.Json,
                    PbirPreviewOutputType.VisualLayoutSummary,
                    PbirPreviewOutputType.SemanticBindingSummary,
                    PbirPreviewOutputType.NavigationSummary
                ]),
            DateTimeOffset.Parse("2026-06-26T15:00:00+00:00"));
        var request = PbirLocalWriteRequest.LocalDryRun(
            requestId: "pbirLocalWriteRequest:phase25-preview-writer",
            sourceIrRef: irState.Ir!.Metadata.IrId,
            sourceIrSchemaVersion: irState.Ir.Metadata.SchemaVersion,
            sourceIrContentHash: irState.Ir.Hashes.ContentHash,
            sourcePreviewManifestRef: previewState.Manifest!.Metadata.ManifestId,
            sourcePreviewManifestSchemaVersion: previewState.Manifest.SchemaVersion,
            sourcePreviewManifestHash: previewState.Manifest.Hashes.ManifestHash,
            targetOutputRoot: "local-preview-output",
            requestedArtifactTypes: requestedArtifactTypes) with
        {
            OverwritePolicy = overwritePolicy
        };
        var writeManifestState = new PbirLocalArtifactWriterBoundaryService().CreateWriteManifest(
            previewState.Manifest,
            irState,
            request,
            existingLocalRelativePaths: [],
            DateTimeOffset.Parse("2026-06-26T16:00:00+00:00"));

        return new ApprovedWriteInputs(
            irState,
            previewState,
            request,
            writeManifestState.Manifest!);
    }

    private static PbirLocalWriteManifest ReplacePlannedFile(PbirLocalWriteManifest manifest, PbirLocalPlannedWriteFile replacement)
    {
        return manifest with
        {
            PlannedFiles = [replacement]
        };
    }

    private static bool IsForbiddenDeployablePath(string path)
    {
        var normalized = path.Replace('\\', '/');

        return normalized.EndsWith("report.json", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith("definition.pbir", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith("model.bim", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".tmdl", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".pbip", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains(".Report/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains(".SemanticModel/", StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed record ApprovedWriteInputs(
        PbirIntermediateRepresentationState IrState,
        PbirPreviewSerializerState PreviewState,
        PbirLocalWriteRequest Request,
        PbirLocalWriteManifest WriteManifest);

    private sealed record UnsafeWriterCall(
        PbirPreviewArtifact PreviewArtifact,
        PbirPreviewManifest PreviewManifest,
        PbirIntermediateRepresentationState IrState,
        PbirLocalWriteRequest Request,
        PbirLocalWriteManifest WriteManifest,
        string OutputBaseDirectory);

    private sealed class TestOutputDirectory : IDisposable
    {
        private TestOutputDirectory(string path)
        {
            Path = path;
        }

        internal string Path { get; }

        internal static TestOutputDirectory Create()
        {
            var path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "pbir-local-preview-writer-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            return new TestOutputDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
