using System.Reflection;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class GenerationPipelineVerificationServiceTests
{
    [Fact(DisplayName = "Generation pipeline verification proves the complete deterministic planning pipeline from Design Package through Generation Manifest")]
    public void VerifyPipeline_ValidInputs_SucceedsDeterministically()
    {
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage();
        var createdUtc = DateTimeOffset.Parse("2026-06-25T11:45:00+00:00");
        var service = new GenerationPipelineVerificationService();

        var first = service.VerifyPipeline(package, createdUtc);
        var second = service.VerifyPipeline(package, createdUtc);

        Assert.True(first.IsVerified);
        Assert.Equal(GenerationPipelineVerificationContract.SchemaVersionV1, first.Verification!.SchemaVersion);
        Assert.Equal("generationPipelineVerification:designPackage:executive-summary", first.Verification.VerificationId);
        Assert.Equal(10, first.Verification.StageResults.Count);
        Assert.Equal(
            new[]
            {
                "designPackage",
                "generationRequest",
                "executionPlan",
                "planningOutcome",
                "runtimeProvider",
                "microsoftRuntimeProvider",
                "skillResolution",
                "generationProvider",
                "generationProviderExecutionPlan",
                "generationManifest"
            },
            first.Verification.StageResults.Select(stage => stage.StageId).ToArray());
        Assert.All(first.Verification.StageResults, stage => Assert.True(stage.Completed));
        Assert.Equal(first.Verification.ManifestRef, first.Verification.StageResults.Last().ReferenceId);
        Assert.Contains(first.Verification.PreservedReferences, reference => reference == "designPackage:executive-summary");
        Assert.Contains(first.Verification.PreservedReferences, reference => reference.StartsWith("runtimeRequest:", StringComparison.Ordinal));
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
    }

    [Fact(DisplayName = "Generation pipeline verification fails for incomplete pipeline sections, missing references, invalid readiness transitions, and incompatible providers")]
    public void VerifyPipeline_InvalidStates_FailsClosed()
    {
        var createdUtc = DateTimeOffset.Parse("2026-06-25T11:45:00+00:00");
        var inputs = GenerationManifestServiceTests.CreateReadyInputs();
        var manifestService = new GenerationManifestService();
        var manifestState = manifestService.CreateManifestState(
            inputs.Planning,
            inputs.SpecificationState,
            inputs.ProviderState,
            inputs.ExecutionPlanningState,
            inputs.RuntimeProviderState,
            inputs.MicrosoftRuntimeState,
            createdUtc);
        var service = new GenerationPipelineVerificationService();

        var invalidManifest = manifestState.Manifest! with
        {
            SourceReferences = manifestState.Manifest!.SourceReferences with
            {
                GenerationRequestRef = string.Empty
            },
            ReadinessSummary = manifestState.Manifest.ReadinessSummary with
            {
                RuntimeReadiness = RuntimeProviderReadinessState.Blocked
            }
        };
        var invalidManifestState = manifestState with
        {
            Manifest = invalidManifest,
            Validation = new GenerationManifestValidator().Validate(
                invalidManifest,
                inputs.Planning,
                inputs.SpecificationState,
                inputs.ProviderState,
                inputs.ExecutionPlanningState,
                inputs.RuntimeProviderState,
                inputs.MicrosoftRuntimeState),
            Readiness = GenerationManifestReadinessState.Blocked
        };
        var invalidProviderState = inputs.ProviderState with
        {
            Provider = inputs.ProviderState.Provider! with
            {
                SupportedCapabilities = ["deploymentSupport"]
            }
        };

        var result = service.VerifyPipeline(
            inputs.Planning,
            inputs.SpecificationState,
            invalidProviderState,
            inputs.ExecutionPlanningState,
            inputs.RuntimeProviderState,
            inputs.MicrosoftRuntimeState,
            invalidManifestState);

        Assert.False(result.IsVerified);
        Assert.Contains("generationRequest", result.Diagnostics.MissingReferences);
        Assert.Contains("runtimeProvider", result.Diagnostics.InvalidReadinessTransitions);
        Assert.Contains("generationProvider", result.Diagnostics.IncompatibleProviders);
    }

    [Fact(DisplayName = "Generation pipeline verification remains planning-only with no generation, provider invocation, deployment, Microsoft API, or CLI execution surface")]
    public void GenerationPipelineVerificationBoundary_RemainsPlanningOnly()
    {
        var forbiddenTokens = new[] { "GeneratePbir", "InvokeProvider", "InvokeApi", "InvokeCli", "Deploy", "RunSkill", "Execute", "Publish" };
        Type[] types =
        [
            typeof(GenerationPipelineVerificationService),
            typeof(GenerationPipelineVerification),
            typeof(GenerationPipelineVerificationState),
            typeof(GenerationPipelineVerificationDiagnostics)
        ];

        foreach (var type in types)
        {
            Assert.DoesNotContain(forbiddenTokens, token => type.Name.Contains(token, StringComparison.OrdinalIgnoreCase));

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.DoesNotContain(forbiddenTokens, token => method.Name.Contains(token, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
