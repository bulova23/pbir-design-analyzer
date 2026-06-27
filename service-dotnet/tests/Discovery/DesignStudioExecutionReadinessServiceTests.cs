using System.Reflection;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class DesignStudioExecutionReadinessServiceTests
{
    [Fact(DisplayName = "Design Studio execution readiness aggregates the complete planning pipeline into deterministic dashboard sections")]
    public void CreateDashboard_CompletePipeline_AggregatesDeterministicReadiness()
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateReadyInputs(output.Path, PbirReviewTarget.AnalyzerWorkspace);
        var context = inputs.Context with
        {
            PreviewReviewStatus = DesignStudioExecutionPreviewReviewStatus.AnalyzerCandidateMetadataPrepared
        };
        var service = new DesignStudioExecutionReadinessService();

        var first = service.CreateDashboard(
            context,
            DesignStudioExecutionReadinessBoundaryRequests.None,
            DateTimeOffset.Parse("2026-06-27T14:00:00+00:00"));
        var second = service.CreateDashboard(
            context,
            DesignStudioExecutionReadinessBoundaryRequests.None,
            DateTimeOffset.Parse("2026-06-27T14:00:00+00:00"));

        Assert.True(first.Safety.IsAllowed);
        Assert.NotNull(first.Dashboard);
        Assert.Equal(DesignStudioExecutionReadinessContract.SchemaVersionV1, first.Dashboard!.SchemaVersion);
        Assert.Equal(DesignStudioExecutionReadinessSummary.ReadyForAnalyzerReview, first.Dashboard.ReadinessSummary);
        Assert.Equal(
            new[]
            {
                "architecture",
                "planning",
                "generation",
                "runtime",
                "skills",
                "review"
            },
            first.Dashboard.StageSummaries.Select(stage => stage.StageId).ToArray());
        Assert.Equal(
            new[]
            {
                "Architecture",
                "Planning",
                "Generation",
                "Runtime",
                "Skills",
                "Review"
            },
            first.Dashboard.StageSummaries.Select(stage => stage.Section).ToArray());
        Assert.All(first.Dashboard.StageSummaries, stage => Assert.False(string.IsNullOrWhiteSpace(stage.Status)));
        Assert.Contains(first.Dashboard.StageSummaries, stage =>
            stage.StageId == "generation" &&
            stage.Status == "ready" &&
            stage.Items.Any(item => item.Label == "PBIR IR readiness" && item.Value == "ReadyForSerializer"));
        Assert.Contains(first.Dashboard.StageSummaries, stage =>
            stage.StageId == "skills" &&
            stage.Items.Any(item => item.Label == "Selected provider" && item.Value == inputs.Manifest.CapabilitySummary.SelectedGenerationProvider.ProviderId) &&
            stage.Items.Any(item => item.Label == "Capability coverage summary"));
        Assert.Contains("Prepare Analyzer candidate metadata", first.Dashboard.ReviewerActionsAvailable);
        Assert.Equal(inputs.Certification.Certification!.CertificationId, first.Dashboard.ArchitectureCertificationReference.CertificationId);
        Assert.Contains(first.Dashboard.LineageReferences, reference => reference.ReferenceId == inputs.Manifest.Metadata.ManifestId);
        Assert.Equal(JsonSerializer.Serialize(first.Dashboard), JsonSerializer.Serialize(second.Dashboard));
    }

    [Fact(DisplayName = "Design Studio execution readiness warning summaries aggregate blockers, approvals, unsupported capabilities, and architecture gaps deterministically")]
    public void CreateDashboard_IncompleteReview_AggregatesWarningSummariesDeterministically()
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateReadyInputs(output.Path, PbirReviewTarget.DesignStudio);
        var manifest = inputs.Manifest with
        {
            ApprovalSummary = inputs.Manifest.ApprovalSummary with
            {
                DesignApproval = inputs.Manifest.ApprovalSummary.DesignApproval with
                {
                    DesignApproved = false,
                    GenerationApproved = false
                }
            }
        };
        var context = inputs.Context with
        {
            GenerationManifestState = inputs.Context.GenerationManifestState with { Manifest = manifest },
            PreviewReviewStatus = DesignStudioExecutionPreviewReviewStatus.Pending
        };

        var state = new DesignStudioExecutionReadinessService().CreateDashboard(
            context,
            DesignStudioExecutionReadinessBoundaryRequests.None,
            DateTimeOffset.Parse("2026-06-27T14:00:00+00:00"));

        Assert.NotNull(state.Dashboard);
        Assert.Equal(DesignStudioExecutionReadinessSummary.ReadyForDesignReview, state.Dashboard!.ReadinessSummary);
        Assert.Equal(
            state.Dashboard.WarningSummaries
                .OrderBy(warning => warning.Category, StringComparer.Ordinal)
                .ThenBy(warning => warning.Message, StringComparer.Ordinal)
                .Select(warning => warning.Message),
            state.Dashboard.WarningSummaries.Select(warning => warning.Message));
        Assert.Contains(state.Dashboard.WarningSummaries, warning =>
            warning.Category == "missingApproval" &&
            warning.Message == "Design approval has not been recorded.");
        Assert.Contains(state.Dashboard.WarningSummaries, warning =>
            warning.Category == "missingApproval" &&
            warning.Message == "Generation approval has not been recorded.");
        Assert.Contains(state.Dashboard.WarningSummaries, warning =>
            warning.Category == "unsupportedCapability" &&
            warning.Message == "Microsoft Skills execution is not implemented.");
        Assert.Contains(state.Dashboard.WarningSummaries, warning =>
            warning.Category == "remainingArchitectureGap" &&
            warning.Message.Contains("Execution providers can now be designed", StringComparison.Ordinal));
    }

    [Theory(DisplayName = "Design Studio execution readiness safety gate rejects execution, provider invocation, deployment, Analyzer automation, and malformed payloads")]
    [MemberData(nameof(UnsafeDashboardInputs))]
    public void CreateDashboard_UnsafeInputs_AreRejected(
        object mutationObject,
        string expectedReason)
    {
        using var output = TestOutputDirectory.Create();
        var inputs = CreateReadyInputs(output.Path, PbirReviewTarget.DesignStudio);
        var mutate = Assert.IsType<Func<DesignStudioExecutionReadinessContext, (DesignStudioExecutionReadinessContext Context, DesignStudioExecutionReadinessBoundaryRequests BoundaryRequests)>>(mutationObject);
        var unsafeInput = mutate(inputs.Context);

        var state = new DesignStudioExecutionReadinessService().CreateDashboard(
            unsafeInput.Context,
            unsafeInput.BoundaryRequests,
            DateTimeOffset.Parse("2026-06-27T14:00:00+00:00"));

        Assert.Equal(DesignStudioExecutionReadinessSummary.Blocked, state.ReadinessSummary);
        Assert.Null(state.Dashboard);
        Assert.False(state.Safety.IsAllowed);
        Assert.Contains(expectedReason, state.Safety.Reasons);
    }

    [Fact(DisplayName = "Design Studio execution readiness remains informational with no generation, Microsoft Skills execution, provider invocation, deployment, or Analyzer automation surface")]
    public void DesignStudioExecutionReadinessBoundary_RemainsInformationalOnly()
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
            "Deploy",
            "Publish",
            "LaunchAnalyzer",
            "AutomateAnalyzer"
        };
        Type[] types =
        [
            typeof(DesignStudioExecutionReadinessService),
            typeof(DesignStudioExecutionReadinessSafetyGate),
            typeof(DesignStudioExecutionReadinessDashboard),
            typeof(DesignStudioExecutionReadinessState)
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

    public static IEnumerable<object[]> UnsafeDashboardInputs()
    {
        yield return
        [
            Mutate((context) => (context, DesignStudioExecutionReadinessBoundaryRequests.None with { ExecutionRequested = true })),
            "execution requests are not allowed from Design Studio execution readiness."
        ];
        yield return
        [
            Mutate((context) => (context, DesignStudioExecutionReadinessBoundaryRequests.None with { ProviderInvocationRequested = true })),
            "provider invocation requests are not allowed from Design Studio execution readiness."
        ];
        yield return
        [
            Mutate((context) => (context, DesignStudioExecutionReadinessBoundaryRequests.None with { DeploymentRequested = true })),
            "deployment requests are not allowed from Design Studio execution readiness."
        ];
        yield return
        [
            Mutate((context) => (context, DesignStudioExecutionReadinessBoundaryRequests.None with { AutomaticAnalyzerLaunchRequested = true })),
            "automatic Analyzer launch requests are not allowed from Design Studio execution readiness."
        ];
        yield return
        [
            Mutate((context) => (context with { PreviewReviewSchemaVersion = "design-studio-execution-readiness/v2" }, DesignStudioExecutionReadinessBoundaryRequests.None)),
            "readiness dashboard payload must use design-studio-execution-readiness/v1."
        ];
        yield return
        [
            Mutate((context) => (context with { GenerationManifestState = context.GenerationManifestState with { Manifest = null } }, DesignStudioExecutionReadinessBoundaryRequests.None)),
            "generation manifest is required for execution readiness aggregation."
        ];
    }

    private static Func<DesignStudioExecutionReadinessContext, (DesignStudioExecutionReadinessContext Context, DesignStudioExecutionReadinessBoundaryRequests BoundaryRequests)> Mutate(
        Func<DesignStudioExecutionReadinessContext, (DesignStudioExecutionReadinessContext Context, DesignStudioExecutionReadinessBoundaryRequests BoundaryRequests)> mutation)
    {
        return mutation;
    }

    private static ReadyInputs CreateReadyInputs(string outputPath, PbirReviewTarget reviewTarget)
    {
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage();
        var architectureValidation = new ArchitectureValidationService().Validate(package, DateTimeOffset.Parse("2026-06-26T12:30:00+00:00"));
        var certification = new ArchitectureReadinessCertificationService().Certify(architectureValidation);
        var manifestInputs = GenerationManifestServiceTests.CreateReadyInputs();
        var manifestState = new GenerationManifestService().CreateManifestState(
            manifestInputs.Planning,
            manifestInputs.SpecificationState,
            manifestInputs.ProviderState,
            manifestInputs.ExecutionPlanningState,
            manifestInputs.RuntimeProviderState,
            manifestInputs.MicrosoftRuntimeState,
            DateTimeOffset.Parse("2026-06-25T10:30:00+00:00"));
        var pipelineVerificationState = new GenerationPipelineVerificationService().VerifyPipeline(
            manifestInputs.Planning,
            manifestInputs.SpecificationState,
            manifestInputs.ProviderState,
            manifestInputs.ExecutionPlanningState,
            manifestInputs.RuntimeProviderState,
            manifestInputs.MicrosoftRuntimeState,
            manifestState);
        var irState = new PbirIntermediateRepresentationService().CreateIntermediateRepresentation(
            manifestState,
            manifestInputs.SpecificationState,
            DateTimeOffset.Parse("2026-06-26T14:00:00+00:00"));
        var serializerRequest = new PbirIntermediateRepresentationService().CreateSerializerRequest(irState);
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
            requestId: "pbirLocalWriteRequest:phase28-execution-readiness",
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
        var previewWriteState = new PbirLocalPreviewFileWriterService().WritePreviewFiles(
            previewState.Output!,
            previewState.Manifest,
            irState,
            request,
            writeManifestState.Manifest!,
            outputPath,
            DateTimeOffset.Parse("2026-06-26T17:00:00+00:00"));
        var previewPackageState = new PbirPreviewPackageService().CreatePackage(
            previewWriteState.Result!,
            previewState.Manifest,
            irState,
            DateTimeOffset.Parse("2026-06-27T10:00:00+00:00"));
        var handoffState = new PbirReviewHandoffService().CreateReviewHandoff(
            previewPackageState.Package!,
            manifestState,
            PbirReviewHandoffRequest.ForReview(
                handoffId: $"pbirReviewHandoff:phase28:{reviewTarget}",
                reviewTarget: reviewTarget,
                requiredReviewerAction: "Review execution readiness dashboard before future downstream work."),
            DateTimeOffset.Parse("2026-06-27T10:15:00+00:00"));
        var context = new DesignStudioExecutionReadinessContext(
            PreviewReviewSchemaVersion: DesignStudioExecutionReadinessContract.SchemaVersionV1,
            ArchitectureCertificationState: certification,
            GenerationManifestState: manifestState,
            PipelineVerificationState: pipelineVerificationState,
            PbirGenerationSpecificationState: manifestInputs.SpecificationState,
            PbirIntermediateRepresentationState: irState,
            PreviewPackageState: previewPackageState,
            ReviewHandoffState: handoffState,
            PreviewReviewStatus: DesignStudioExecutionPreviewReviewStatus.Pending);

        return new ReadyInputs(
            Certification: certification,
            Manifest: manifestState.Manifest!,
            Context: context);
    }

    private sealed record ReadyInputs(
        ArchitectureCertificationState Certification,
        GenerationManifest Manifest,
        DesignStudioExecutionReadinessContext Context);

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
                "design-studio-execution-readiness-tests",
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
