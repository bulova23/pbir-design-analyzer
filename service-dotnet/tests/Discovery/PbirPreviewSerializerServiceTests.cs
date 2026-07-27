using System.Reflection;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirPreviewSerializerServiceTests
{
    [Fact(DisplayName = "PBIR preview serializer emits deterministic Markdown and JSON preview artifacts with stable hashes and complete summaries")]
    public void CreatePreviewArtifacts_ReadyIrAndSafeRequest_EmitsDeterministicPreviewArtifacts()
    {
        var inputs = CreateReadyPreviewInputs();
        var options = PbirPreviewSerializerOptions.LocalPreview(
            outputRoot: "preview-artifacts",
            outputTypes:
            [
                PbirPreviewOutputType.Markdown,
                PbirPreviewOutputType.Json,
                PbirPreviewOutputType.VisualLayoutSummary,
                PbirPreviewOutputType.SemanticBindingSummary,
                PbirPreviewOutputType.NavigationSummary
            ]);
        var generatedUtc = DateTimeOffset.Parse("2026-06-26T15:00:00+00:00");
        var service = new PbirPreviewSerializerService();

        var first = service.CreatePreviewArtifacts(inputs.IrState, inputs.SerializerRequest, options, generatedUtc);
        var second = service.CreatePreviewArtifacts(inputs.IrState, inputs.SerializerRequest, options, generatedUtc);

        Assert.Equal(PbirPreviewSerializerReadinessState.Generated, first.Readiness);
        Assert.True(first.Safety.IsAllowed);
        Assert.True(first.Validation.IsValid);
        Assert.NotNull(first.Output);
        Assert.NotNull(first.Manifest);
        Assert.Equal(Serialize(first.Output), Serialize(second.Output));
        Assert.Equal(Serialize(first.Manifest), Serialize(second.Manifest));

        Assert.Equal(PbirPreviewArtifactContract.SchemaVersionV1, first.Output!.SchemaVersion);
        Assert.Equal(PbirPreviewManifestContract.SchemaVersionV1, first.Manifest!.SchemaVersion);
        Assert.Equal(inputs.IrState.Ir!.Metadata.IrId, first.Manifest.SourceReferences.PbirIrRef);
        Assert.Equal(inputs.IrState.Ir.Hashes.ContentHash, first.Manifest.SourceReferences.PbirIrContentHash);
        Assert.Equal(inputs.SerializerRequest.RequestId, first.Manifest.SourceReferences.SerializerRequestRef);
        Assert.Equal(inputs.IrState.Ir.Metadata.IrId, first.Output.SourceReferences.PbirIrRef);
        Assert.Equal(inputs.SerializerRequest.RequestId, first.Output.SourceReferences.SerializerRequestRef);

        var markdown = Assert.Single(first.Output.GeneratedFiles, file => file.RelativePath == "pbir-preview-artifact/v1/report-preview.md");
        var json = Assert.Single(first.Output.GeneratedFiles, file => file.RelativePath == "pbir-preview-artifact/v1/report-preview.json");

        Assert.Equal("text/markdown", markdown.ContentType);
        Assert.Equal("application/json", json.ContentType);
        Assert.Equal(64, markdown.HashSha256.Length);
        Assert.Equal(64, json.HashSha256.Length);
        Assert.Equal(markdown.HashSha256, first.Manifest.GeneratedFiles.Single(file => file.RelativePath == markdown.RelativePath).HashSha256);
        Assert.Equal(json.HashSha256, first.Manifest.GeneratedFiles.Single(file => file.RelativePath == json.RelativePath).HashSha256);

        Assert.Contains("# PBIR Preview", markdown.Content, StringComparison.Ordinal);
        Assert.Contains("## Pages", markdown.Content, StringComparison.Ordinal);
        Assert.Contains("## Visual Layout Summary", markdown.Content, StringComparison.Ordinal);
        Assert.Contains("## Semantic Binding Summary", markdown.Content, StringComparison.Ordinal);
        Assert.Contains("## Navigation Summary", markdown.Content, StringComparison.Ordinal);
        Assert.Contains(inputs.IrState.Ir.Pages[0].PageId, markdown.Content, StringComparison.Ordinal);
        Assert.Contains(inputs.IrState.Ir.Visuals[0].VisualId, markdown.Content, StringComparison.Ordinal);
        Assert.Contains(inputs.IrState.Ir.Semantics[0].SemanticId, markdown.Content, StringComparison.Ordinal);
        Assert.Contains(inputs.IrState.Ir.Navigation.LandingPage, markdown.Content, StringComparison.Ordinal);

        using var previewJson = JsonDocument.Parse(json.Content);
        Assert.Equal(PbirPreviewArtifactContract.SchemaVersionV1, previewJson.RootElement.GetProperty("schemaVersion").GetString());
        Assert.True(previewJson.RootElement.GetProperty("pages").GetArrayLength() > 0);
        Assert.True(previewJson.RootElement.GetProperty("visualLayoutSummary").GetArrayLength() > 0);
        Assert.True(previewJson.RootElement.GetProperty("semanticBindingSummary").GetArrayLength() > 0);
        Assert.True(previewJson.RootElement.GetProperty("navigationSummary").GetProperty("pageTransitions").GetArrayLength() > 0);

        Assert.Equal(64, first.Output.Hashes.InputHash.Length);
        Assert.Equal(64, first.Output.Hashes.FileSetHash.Length);
        Assert.Equal(64, first.Output.Hashes.OutputHash.Length);
        Assert.Equal(64, first.Manifest.Hashes.ManifestHash.Length);
        Assert.Equal(first.Output.Hashes.FileSetHash, first.Manifest.Hashes.FileSetHash);
        Assert.Contains(inputs.IrState.Ir.Metadata.IrId, first.Manifest.Lineage.ImmutableLineage);
        Assert.Contains(inputs.SerializerRequest.RequestId, first.Manifest.Lineage.ImmutableLineage);
        Assert.Contains(first.Manifest.Metadata.ManifestId, first.Manifest.Lineage.ImmutableLineage);
        Assert.Contains("Deployable PBIR serialization remains unsupported.", first.Manifest.UnsupportedSections);
        Assert.Contains("Preview artifacts are local human-review artifacts only.", first.Manifest.Warnings);
    }

    [Theory(DisplayName = "PBIR preview serializer safety gate rejects deployable, execution, provider, CLI, API, deployment, non-local, and incomplete requests")]
    [MemberData(nameof(UnsafePreviewOptions))]
    public void CreatePreviewArtifacts_UnsafeRequest_IsRejected(object optionsObject, string expectedReason)
    {
        var inputs = CreateReadyPreviewInputs();
        var options = Assert.IsType<PbirPreviewSerializerOptions>(optionsObject);

        var state = new PbirPreviewSerializerService().CreatePreviewArtifacts(
            inputs.IrState,
            inputs.SerializerRequest,
            options,
            DateTimeOffset.Parse("2026-06-26T15:00:00+00:00"));

        Assert.Equal(PbirPreviewSerializerReadinessState.Rejected, state.Readiness);
        Assert.Null(state.Output);
        Assert.Null(state.Manifest);
        Assert.False(state.Safety.IsAllowed);
        Assert.Contains(expectedReason, state.Safety.Reasons);
        Assert.Contains(expectedReason, state.Diagnostics.SafetyRejections);
    }

    [Fact(DisplayName = "PBIR preview serializer rejects incomplete IR and serializer request mismatches")]
    public void CreatePreviewArtifacts_IncompleteIrOrMismatchedRequest_IsRejected()
    {
        var inputs = CreateReadyPreviewInputs();
        var options = PbirPreviewSerializerOptions.LocalPreview(
            outputRoot: "preview-artifacts",
            outputTypes: [PbirPreviewOutputType.Markdown, PbirPreviewOutputType.Json]);
        var mismatchedRequest = inputs.SerializerRequest with
        {
            PbirIrContentHash = "not-the-ir-content-hash"
        };
        var incompleteState = inputs.IrState with
        {
            Ir = null
        };

        var mismatch = new PbirPreviewSerializerService().CreatePreviewArtifacts(
            inputs.IrState,
            mismatchedRequest,
            options,
            DateTimeOffset.Parse("2026-06-26T15:00:00+00:00"));
        var incomplete = new PbirPreviewSerializerService().CreatePreviewArtifacts(
            incompleteState,
            inputs.SerializerRequest,
            options,
            DateTimeOffset.Parse("2026-06-26T15:00:00+00:00"));

        Assert.Equal(PbirPreviewSerializerReadinessState.Rejected, mismatch.Readiness);
        Assert.Null(mismatch.Output);
        Assert.Contains("serializer request PBIR IR content hash must match the IR content hash.", mismatch.Safety.Reasons);
        Assert.Equal(PbirPreviewSerializerReadinessState.Rejected, incomplete.Readiness);
        Assert.Null(incomplete.Output);
        Assert.Contains("complete PBIR IR must be provided.", incomplete.Safety.Reasons);
    }

    [Fact(DisplayName = "PBIR preview serializer validator rejects unsupported preview output types, lineage gaps, and hash instability")]
    public void Validator_InvalidPreviewOutput_FailsClosed()
    {
        var inputs = CreateReadyPreviewInputs();
        var options = PbirPreviewSerializerOptions.LocalPreview(
            outputRoot: "preview-artifacts",
            outputTypes: [PbirPreviewOutputType.Markdown, PbirPreviewOutputType.Json]);
        var state = new PbirPreviewSerializerService().CreatePreviewArtifacts(
            inputs.IrState,
            inputs.SerializerRequest,
            options,
            DateTimeOffset.Parse("2026-06-26T15:00:00+00:00"));
        var validator = new PbirPreviewSerializerValidator();

        var invalidLineage = state.Manifest! with
        {
            Lineage = state.Manifest.Lineage with
            {
                ImmutableLineage = []
            }
        };
        var invalidHash = state.Manifest with
        {
            Hashes = state.Manifest.Hashes with
            {
                FileSetHash = "invalid"
            }
        };
        var unsupportedFile = state.Output!.GeneratedFiles[0] with
        {
            OutputType = (PbirPreviewOutputType)999
        };
        var unsupportedOutput = state.Output with
        {
            GeneratedFiles = [unsupportedFile]
        };

        var lineageResult = validator.Validate(state.Output, invalidLineage, inputs.IrState, inputs.SerializerRequest);
        var hashResult = validator.Validate(state.Output, invalidHash, inputs.IrState, inputs.SerializerRequest);
        var unsupportedResult = validator.Validate(unsupportedOutput, state.Manifest, inputs.IrState, inputs.SerializerRequest);

        Assert.False(lineageResult.IsValid);
        Assert.Contains("preview manifest lineage must include PBIR IR, serializer request, and preview manifest references.", lineageResult.Diagnostics.LineageViolations);
        Assert.False(hashResult.IsValid);
        Assert.Contains("preview manifest file-set hash must match generated preview files.", hashResult.Diagnostics.HashViolations);
        Assert.False(unsupportedResult.IsValid);
        Assert.Contains("preview output type is unsupported.", unsupportedResult.Diagnostics.UnsupportedOutputTypes);
    }

    [Fact(DisplayName = "PBIR preview serializer exposes no deployable PBIR, Microsoft Skills execution, provider invocation, API invocation, CLI invocation, or deployment surface")]
    public void PbirPreviewSerializerBoundary_RemainsLocalPreviewOnly()
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
            typeof(PbirPreviewSerializerService),
            typeof(PbirPreviewSerializerSafetyGate),
            typeof(PbirPreviewSerializerValidator),
            typeof(PbirPreviewArtifact),
            typeof(PbirPreviewManifest),
            typeof(PbirPreviewSerializerState)
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

    [Fact(DisplayName = "PBIR preview serializer remains byte-identical and preview-only when deployable serializer availability becomes true")]
    public void CreatePreviewArtifacts_DeployableSerializerAvailable_PreservesPreviewBehavior()
    {
        var inputs = CreateReadyPreviewInputs();
        var availableRequest = inputs.SerializerRequest with
        {
            SerializerImplementationAvailable = true
        };
        var unavailableRequest = availableRequest with
        {
            SerializerImplementationAvailable = false
        };
        var options = PbirPreviewSerializerOptions.LocalPreview(
            "preview-artifacts",
            [PbirPreviewOutputType.Markdown, PbirPreviewOutputType.Json]);
        var generatedUtc = DateTimeOffset.Parse("2026-06-26T15:00:00+00:00");
        var service = new PbirPreviewSerializerService();

        var available = service.CreatePreviewArtifacts(
            inputs.IrState,
            availableRequest,
            options,
            generatedUtc);
        var unavailable = service.CreatePreviewArtifacts(
            inputs.IrState,
            unavailableRequest,
            options,
            generatedUtc);

        Assert.Equal(PbirPreviewSerializerReadinessState.Generated, available.Readiness);
        Assert.Equal(Serialize(unavailable.Output), Serialize(available.Output));
        Assert.Equal(Serialize(unavailable.Manifest), Serialize(available.Manifest));
        Assert.False(availableRequest.ProviderInvocationAllowed);
        Assert.False(availableRequest.DeploymentAllowed);
        Assert.False(availableRequest.MicrosoftSkillsExecutionAllowed);
        Assert.All(
            available.Output!.GeneratedFiles,
            file => Assert.StartsWith("pbir-preview-artifact/v1/", file.RelativePath, StringComparison.Ordinal));
        Assert.DoesNotContain(
            available.Output.GeneratedFiles,
            file =>
                file.RelativePath is "definition.pbir" or "definition/report.json" or "report.json" ||
                file.RelativePath.Contains("/visuals/", StringComparison.Ordinal));

        var dependencyTypes = typeof(PbirPreviewSerializerService)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(field => field.FieldType)
            .Concat(
                typeof(PbirPreviewSerializerService)
                    .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .SelectMany(constructor => constructor.GetParameters())
                    .Select(parameter => parameter.ParameterType))
            .ToArray();

        Assert.DoesNotContain(typeof(PbirDeployableSerializerService), dependencyTypes);
        Assert.DoesNotContain(
            typeof(PbirPreviewSerializerService)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
            method => method.ReturnType == typeof(PbirDeployableSerializerState));
    }

    public static IEnumerable<object[]> UnsafePreviewOptions()
    {
        yield return
        [
            PbirPreviewSerializerOptions.LocalPreview("preview-artifacts", [PbirPreviewOutputType.Markdown]) with
            {
                DeployableOutputRequested = true
            },
            "deployable output requests are not allowed."
        ];
        yield return
        [
            PbirPreviewSerializerOptions.LocalPreview("preview-artifacts", [PbirPreviewOutputType.Markdown]) with
            {
                RequestedOutputFiles = ["report.json"]
            },
            "deployable PBIR file output is not allowed: report.json."
        ];
        yield return
        [
            PbirPreviewSerializerOptions.LocalPreview("preview-artifacts", [PbirPreviewOutputType.Markdown]) with
            {
                RequestedOutputFiles = ["definition.pbir"]
            },
            "deployable PBIR file output is not allowed: definition.pbir."
        ];
        yield return
        [
            PbirPreviewSerializerOptions.LocalPreview("preview-artifacts", [PbirPreviewOutputType.Markdown]) with
            {
                ProviderInvocationRequested = true
            },
            "provider invocation requests are not allowed."
        ];
        yield return
        [
            PbirPreviewSerializerOptions.LocalPreview("preview-artifacts", [PbirPreviewOutputType.Markdown]) with
            {
                MicrosoftApiRequested = true
            },
            "Microsoft API requests are not allowed."
        ];
        yield return
        [
            PbirPreviewSerializerOptions.LocalPreview("preview-artifacts", [PbirPreviewOutputType.Markdown]) with
            {
                CliRequested = true
            },
            "CLI requests are not allowed."
        ];
        yield return
        [
            PbirPreviewSerializerOptions.LocalPreview("preview-artifacts", [PbirPreviewOutputType.Markdown]) with
            {
                MicrosoftSkillsExecutionRequested = true
            },
            "Microsoft Skills execution requests are not allowed."
        ];
        yield return
        [
            PbirPreviewSerializerOptions.LocalPreview("preview-artifacts", [PbirPreviewOutputType.Markdown]) with
            {
                DeploymentRequested = true
            },
            "deployment requests are not allowed."
        ];
        yield return
        [
            PbirPreviewSerializerOptions.LocalPreview("/tmp/preview-artifacts", [PbirPreviewOutputType.Markdown]),
            "preview output path must be a local relative path."
        ];
    }

    private static (PbirIntermediateRepresentationState IrState, PbirSerializerRequest SerializerRequest) CreateReadyPreviewInputs()
    {
        var inputs = PbirIntermediateRepresentationServiceTests.CreateReadyIrInputs();
        var irService = new PbirIntermediateRepresentationService();
        var irState = irService.CreateIntermediateRepresentation(
            inputs.ManifestState,
            inputs.SpecificationState,
            DateTimeOffset.Parse("2026-06-26T14:00:00+00:00"));
        var serializerRequest = irService.CreateSerializerRequest(irState);

        return (irState, serializerRequest);
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
