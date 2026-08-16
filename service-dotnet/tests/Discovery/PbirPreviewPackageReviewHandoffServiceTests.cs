using System.Reflection;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirPreviewPackageReviewHandoffServiceTests
{
    [Fact(DisplayName = "PBIR preview package creates deterministic metadata with complete file and hash inventory")]
    public void CreatePackage_ValidPreviewWriteResult_CreatesDeterministicPackageMetadata()
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateApprovedPreviewWriteInputs();
        var previewWriteState = WritePreviewFiles(inputs, output.Path);
        var service = new PbirPreviewPackageService();
        var generatedUtc = DateTimeOffset.Parse("2026-06-27T10:00:00+00:00");

        var first = service.CreatePackage(
            previewWriteState.Result!,
            inputs.PreviewState.Manifest!,
            inputs.IrState,
            generatedUtc);
        var second = service.CreatePackage(
            previewWriteState.Result!,
            inputs.PreviewState.Manifest!,
            inputs.IrState,
            generatedUtc);

        Assert.Equal(PbirPreviewPackageReadinessState.Packaged, first.Readiness);
        Assert.True(first.Safety.IsAllowed);
        Assert.NotNull(first.Package);
        Assert.Equal(PbirPreviewPackageContract.SchemaVersionV1, first.Package!.SchemaVersion);
        Assert.Equal("pbirPreviewPackage:pbirLocalPreviewWriteResult:pbirLocalWriteManifest:pbirLocalWriteRequest:phase26-review-handoff", first.Package.Metadata.PackageId);
        Assert.Equal(generatedUtc.UtcDateTime, first.Package.Metadata.GeneratedUtc);
        Assert.False(first.Package.PackageDescriptor.ContainsPhysicalFileContent);
        Assert.False(first.Package.PackageDescriptor.ZipCreated);
        Assert.False(first.Package.PackageDescriptor.DeployableArtifactsAllowed);
        Assert.Equal(5, first.Package.FileInventory.Count);
        Assert.Equal(first.Package.FileInventory.Count + 4, first.Package.HashInventory.Entries.Count);
        Assert.All(first.Package.FileInventory, file =>
        {
            Assert.False(IsForbiddenDeployablePath(file.RelativePath));
            Assert.False(IsForbiddenDeployablePath(file.IntendedPath));
            Assert.Equal(64, file.HashSha256.Length);
        });
        Assert.Contains(first.Package.HashInventory.Entries, entry => entry.ReferenceId == previewWriteState.Result!.Hashes.ResultHash);
        Assert.Contains(first.Package.HashInventory.Entries, entry => entry.ReferenceId == inputs.PreviewState.Manifest!.Hashes.ManifestHash);
        Assert.Equal(Serialize(first.Package), Serialize(second.Package));
    }

    [Fact(DisplayName = "PBIR preview package preserves lineage, warnings, rejected artifacts, and rollback metadata reference")]
    public void CreatePackage_ValidPreviewWriteResult_PreservesLineageAndRollbackReference()
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateApprovedPreviewWriteInputs();
        var previewWriteState = WritePreviewFiles(inputs, output.Path);

        var state = new PbirPreviewPackageService().CreatePackage(
            previewWriteState.Result!,
            inputs.PreviewState.Manifest!,
            inputs.IrState,
            DateTimeOffset.Parse("2026-06-27T10:00:00+00:00"));

        Assert.NotNull(state.Package);
        Assert.Equal(previewWriteState.Result!.RollbackPlanReference, state.Package!.RollbackPlanReference);
        Assert.Equal(previewWriteState.Result.SourceLineage.SourceWriteManifestRef, state.Package.Lineage.SourceWriteManifestRef);
        Assert.Equal(previewWriteState.Result.SourceLineage.PbirIrRef, state.Package.Lineage.PbirIrRef);
        Assert.Equal(previewWriteState.Result.SourceLineage.PreviewManifestRef, state.Package.Lineage.PreviewManifestRef);
        Assert.Contains(state.Package.Lineage.ImmutableLineage, reference => reference == state.Package.Metadata.PackageId);
        Assert.Contains(state.Package.Warnings, warning => warning == "Deployable PBIR artifacts remain forbidden.");
        Assert.Empty(state.Package.RejectedArtifacts);
    }

    [Fact(DisplayName = "PBIR review handoff creates Design Studio review records without Analyzer validation authority")]
    public void CreateReviewHandoff_DesignReviewTarget_CreatesReadyForDesignReviewRecord()
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateApprovedPreviewWriteInputs();
        var package = CreatePackage(inputs, output.Path);
        var request = PbirReviewHandoffRequest.ForReview(
            handoffId: "pbirReviewHandoff:design-review",
            reviewTarget: PbirReviewTarget.DesignStudio,
            requiredReviewerAction: "Review local preview outputs in Design Studio before any future execution planning.");

        var state = new PbirReviewHandoffService().CreateReviewHandoff(
            package,
            inputs.GenerationManifestState,
            request,
            DateTimeOffset.Parse("2026-06-27T10:15:00+00:00"));

        Assert.Equal(PbirReviewHandoffReadinessState.ReadyForDesignReview, state.Readiness);
        Assert.True(state.Safety.IsAllowed);
        Assert.NotNull(state.Handoff);
        Assert.Equal(PbirReviewHandoffContract.SchemaVersionV1, state.Handoff!.SchemaVersion);
        Assert.Equal(request.HandoffId, state.Handoff.HandoffId);
        Assert.Equal(package.Metadata.PackageId, state.Handoff.PreviewPackageReference.PackageId);
        Assert.Equal(inputs.GenerationManifestState.Manifest!.SourceReferences.DesignPackageRef, state.Handoff.DesignPackageReference.DesignPackageRef);
        Assert.Equal(inputs.GenerationManifestState.Manifest.Metadata.ManifestId, state.Handoff.GenerationManifestReference.ManifestId);
        Assert.Equal(inputs.IrState.Ir!.Metadata.IrId, state.Handoff.PbirIrReference.IrId);
        Assert.Equal(PbirReviewTarget.DesignStudio, state.Handoff.ReviewTarget.Target);
        Assert.False(state.Handoff.AnalyzerWorkspaceBoundary.ValidationOccurred);
        Assert.False(state.Handoff.AnalyzerWorkspaceBoundary.AutomaticValidationRequested);
        Assert.False(state.Handoff.AnalyzerWorkspaceBoundary.AutomaticValidationAllowed);
        Assert.False(state.Handoff.AnalyzerWorkspaceBoundary.WorkspaceLaunchRequested);
        Assert.False(state.Handoff.DeploymentBoundary.DeploymentRequested);
        Assert.False(state.Handoff.DeploymentBoundary.DeploymentAllowed);
    }

    [Fact(DisplayName = "PBIR review handoff classifies Analyzer review readiness without running Analyzer validation")]
    public void CreateReviewHandoff_AnalyzerReviewTarget_ClassifiesReadyForAnalyzerReviewWithoutValidation()
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateApprovedPreviewWriteInputs();
        var package = CreatePackage(inputs, output.Path);
        var request = PbirReviewHandoffRequest.ForReview(
            handoffId: "pbirReviewHandoff:analyzer-review",
            reviewTarget: PbirReviewTarget.AnalyzerWorkspace,
            requiredReviewerAction: "Manually import the preview package metadata as a future Analyzer candidate.");

        var state = new PbirReviewHandoffService().CreateReviewHandoff(
            package,
            inputs.GenerationManifestState,
            request,
            DateTimeOffset.Parse("2026-06-27T10:15:00+00:00"));

        Assert.Equal(PbirReviewHandoffReadinessState.ReadyForAnalyzerReview, state.Readiness);
        Assert.NotNull(state.Handoff);
        Assert.True(state.Handoff!.DesignStudioApprovalContext.DesignApproved);
        Assert.True(state.Handoff.DesignStudioApprovalContext.AnalyzerValidationRequired);
        Assert.Equal("No Analyzer Workspace validation has occurred.", state.Handoff.AnalyzerWorkspaceBoundary.ValidationStatus);
        Assert.Contains(state.Handoff.Warnings, warning => warning == "readyForAnalyzerReview does not mean Analyzer validation occurred.");
    }

    [Theory(DisplayName = "PBIR review handoff safety gate rejects deployable artifacts, missing hashes, automatic Analyzer execution, and deployment")]
    [MemberData(nameof(UnsafeHandoffInputs))]
    public void CreateReviewHandoff_UnsafeInputs_AreRejected(object mutationObject, string expectedReason)
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateApprovedPreviewWriteInputs();
        var package = CreatePackage(inputs, output.Path);
        var request = PbirReviewHandoffRequest.ForReview(
            handoffId: "pbirReviewHandoff:unsafe",
            reviewTarget: PbirReviewTarget.DesignStudio,
            requiredReviewerAction: "Review local preview outputs.");
        var mutation = Assert.IsType<Func<PbirPreviewPackage, PbirReviewHandoffRequest, UnsafeHandoffCall>>(mutationObject);
        var unsafeCall = mutation(package, request);

        var state = new PbirReviewHandoffService().CreateReviewHandoff(
            unsafeCall.Package,
            inputs.GenerationManifestState,
            unsafeCall.Request,
            DateTimeOffset.Parse("2026-06-27T10:15:00+00:00"));

        Assert.Equal(PbirReviewHandoffReadinessState.Blocked, state.Readiness);
        Assert.Null(state.Handoff);
        Assert.False(state.Safety.IsAllowed);
        Assert.Contains(expectedReason, state.Safety.Reasons);
    }

    [Fact(DisplayName = "PBIR preview package and review handoff expose no deployable PBIR or automation surface")]
    public void PbirPreviewPackageAndReviewHandoff_RemainReviewOnlyAndNonExecuting()
    {
        var forbiddenTokens = new[]
        {
            "GeneratePbir",
            "SerializePbir",
            "InvokeProvider",
            "InvokeMicrosoftApi",
            "InvokeApi",
            "InvokeCli",
            "RunSkill",
            "Publish",
            "Execute"
        };
        Type[] types =
        [
            typeof(PbirPreviewPackageService),
            typeof(PbirReviewHandoffService),
            typeof(PbirReviewHandoffSafetyGate),
            typeof(PbirPreviewPackage),
            typeof(PbirReviewHandoff)
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

    public static IEnumerable<object[]> UnsafeHandoffInputs()
    {
        yield return [
            Mutate((package, request) => new UnsafeHandoffCall(
                package with
                {
                    FileInventory =
                    [
                        package.FileInventory[0] with
                        {
                            ArtifactType = PbirLocalWriteArtifactType.ReportJson,
                            RelativePath = "pbir-local-writer/v1/report/report.json",
                            IntendedPath = "local-preview-output/pbir-local-writer/v1/report/report.json"
                        }
                    ]
                },
                request)),
            "preview package references forbidden deployable artifacts: report.json."
        ];
        yield return [
            Mutate((package, request) => new UnsafeHandoffCall(
                package with
                {
                    HashInventory = package.HashInventory with
                    {
                        Entries = [package.HashInventory.Entries[0] with { HashSha256 = string.Empty }]
                    }
                },
                request)),
            "preview package hash inventory must include complete SHA-256 hashes."
        ];
        yield return [
            Mutate((package, request) => new UnsafeHandoffCall(
                package,
                request with { AutomaticAnalyzerValidationRequested = true })),
            "automatic Analyzer Workspace validation requests are not allowed."
        ];
        yield return [
            Mutate((package, request) => new UnsafeHandoffCall(
                package,
                request with { DeploymentRequested = true })),
            "deployment requests are not allowed."
        ];
    }

    private static Func<PbirPreviewPackage, PbirReviewHandoffRequest, UnsafeHandoffCall> Mutate(
        Func<PbirPreviewPackage, PbirReviewHandoffRequest, UnsafeHandoffCall> mutation)
    {
        return mutation;
    }

    private static PbirPreviewPackage CreatePackage(ApprovedPreviewWriteInputs inputs, string outputPath)
    {
        var previewWriteState = WritePreviewFiles(inputs, outputPath);
        var packageState = new PbirPreviewPackageService().CreatePackage(
            previewWriteState.Result!,
            inputs.PreviewState.Manifest!,
            inputs.IrState,
            DateTimeOffset.Parse("2026-06-27T10:00:00+00:00"));

        Assert.NotNull(packageState.Package);
        return packageState.Package!;
    }

    private static PbirLocalPreviewFileWriterState WritePreviewFiles(ApprovedPreviewWriteInputs inputs, string outputPath)
    {
        var state = new PbirLocalPreviewFileWriterService().WritePreviewFiles(
            inputs.PreviewState.Output!,
            inputs.PreviewState.Manifest!,
            inputs.IrState,
            inputs.Request,
            inputs.WriteManifest,
            outputPath,
            DateTimeOffset.Parse("2026-06-26T17:00:00+00:00"));

        Assert.NotNull(state.Result);
        return state;
    }

    private static ApprovedPreviewWriteInputs CreateApprovedPreviewWriteInputs()
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
            requestId: "pbirLocalWriteRequest:phase26-review-handoff",
            sourceIrRef: irState.Ir!.Metadata.IrId,
            sourceIrSchemaVersion: irState.Ir.Metadata.SchemaVersion,
            sourceIrContentHash: irState.Ir.Hashes.ContentHash,
            sourcePreviewManifestRef: previewState.Manifest!.Metadata.ManifestId,
            sourcePreviewManifestSchemaVersion: previewState.Manifest.SchemaVersion,
            sourcePreviewManifestHash: previewState.Manifest.Hashes.ManifestHash,
            targetOutputRoot: "local-preview-output",
            requestedArtifactTypes:
            [
                PbirLocalWriteArtifactType.PreviewMarkdown,
                PbirLocalWriteArtifactType.PreviewJson,
                PbirLocalWriteArtifactType.IrJson,
                PbirLocalWriteArtifactType.ManifestJson,
                PbirLocalWriteArtifactType.DiagnosticsMarkdown
            ]);
        var writeManifestState = new PbirLocalArtifactWriterBoundaryService().CreateWriteManifest(
            previewState.Manifest,
            irState,
            request,
            existingLocalRelativePaths: [],
            DateTimeOffset.Parse("2026-06-26T16:00:00+00:00"));

        return new ApprovedPreviewWriteInputs(
            irInputs.ManifestState,
            irState,
            previewState,
            request,
            writeManifestState.Manifest!);
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

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private sealed record ApprovedPreviewWriteInputs(
        GenerationManifestState GenerationManifestState,
        PbirIntermediateRepresentationState IrState,
        PbirPreviewSerializerState PreviewState,
        PbirLocalWriteRequest Request,
        PbirLocalWriteManifest WriteManifest);

    private sealed record UnsafeHandoffCall(
        PbirPreviewPackage Package,
        PbirReviewHandoffRequest Request);

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
                "pbir-preview-package-handoff-tests",
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
