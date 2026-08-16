using System.Reflection;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class ArchitectureCertificationServiceTests
{
    [Fact(DisplayName = "Architecture validation proves every Phase 1-19 framework participates in the planning-only architecture")]
    public void Validate_CompletePlanningArchitecture_VerifiesEveryFrameworkAndBoundary()
    {
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage();
        var createdUtc = DateTimeOffset.Parse("2026-06-26T12:30:00+00:00");
        var service = new ArchitectureValidationService();

        var state = service.Validate(package, createdUtc);

        Assert.True(state.IsValid);
        Assert.NotNull(state.Validation);
        Assert.Equal(ArchitectureValidationContract.SchemaVersionV1, state.Validation!.SchemaVersion);
        Assert.Equal("architectureValidation:designPackage:executive-summary", state.Validation.ValidationId);
        Assert.Equal(
            new[]
            {
                "designPackageConsumption",
                "generationRequest",
                "executionPlan",
                "providerAdapter",
                "microsoftAdapterSpecification",
                "capabilityNegotiation",
                "executionProviderContract",
                "planningOrchestration",
                "runtimeProviderAbstraction",
                "microsoftRuntimeProviderContract",
                "microsoftSkillsCapabilityCatalog",
                "microsoftSkillProviderAdapter",
                "pbirExecutionPrototypeBoundary",
                "pbirGenerationSpecification",
                "generationProvider",
                "generationProviderExecutionPlanning",
                "generationManifest",
                "generationPipelineVerification"
            },
            state.Validation.FrameworkParticipation.Select(framework => framework.FrameworkId).ToArray());
        Assert.All(state.Validation.FrameworkParticipation, framework => Assert.True(framework.Participates));
        Assert.All(state.Validation.TrustBoundaryVerification, boundary => Assert.True(boundary.Verified));
        Assert.All(state.Validation.OwnershipVerification, boundary => Assert.True(boundary.Verified));
        Assert.All(state.Validation.ProviderNeutralityVerification, boundary => Assert.True(boundary.Verified));
        Assert.Empty(state.Diagnostics.LayerSeparationViolations);
        Assert.Empty(state.Diagnostics.TrustBoundaryViolations);
        Assert.Empty(state.Diagnostics.OwnershipBoundaryViolations);
        Assert.Empty(state.Diagnostics.ProviderNeutralityViolations);
        Assert.Empty(state.Diagnostics.DeterminismViolations);
        Assert.Empty(state.Diagnostics.LineageViolations);
        Assert.Empty(state.Diagnostics.SchemaVersionViolations);
        Assert.Empty(state.Diagnostics.ReadinessTransitionViolations);
        Assert.Empty(state.Diagnostics.ApprovalTransitionViolations);
    }

    [Fact(DisplayName = "Architecture certification, readiness report, and gap analysis are deterministic v1 contracts")]
    public void Certify_ValidArchitecture_CreatesDeterministicCertificationReadinessAndGapAnalysis()
    {
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage();
        var createdUtc = DateTimeOffset.Parse("2026-06-26T12:30:00+00:00");
        var validation = new ArchitectureValidationService().Validate(package, createdUtc);
        var service = new ArchitectureReadinessCertificationService();

        var first = service.Certify(validation);
        var second = service.Certify(validation);

        Assert.True(first.IsCertified);
        Assert.NotNull(first.Certification);
        Assert.NotNull(first.ReadinessReport);
        Assert.NotNull(first.GapAnalysis);
        Assert.Equal(ArchitectureCertificationContract.SchemaVersionV1, first.Certification!.SchemaVersion);
        Assert.Equal(ArchitectureReadinessReportContract.SchemaVersionV1, first.ReadinessReport!.SchemaVersion);
        Assert.Equal(ArchitectureGapAnalysisContract.SchemaVersionV1, first.GapAnalysis!.SchemaVersion);
        Assert.Equal(Enumerable.Range(1, 19).ToArray(), first.Certification.ArchitectureCoverage.CompletedPhases);
        Assert.Contains("generation-manifest/v1", first.Certification.ArchitectureCoverage.ImplementedSchemas);
        Assert.Contains("generation-pipeline-verification/v1", first.Certification.ArchitectureCoverage.ImplementedSchemas);
        Assert.Contains("architecture-certification/v1", first.Certification.ArchitectureCoverage.ImplementedSchemas);
        Assert.Contains("architecture-readiness-report/v1", first.Certification.ArchitectureCoverage.ImplementedSchemas);
        Assert.Contains("architecture-gap-analysis/v1", first.Certification.ArchitectureCoverage.ImplementedSchemas);
        Assert.Equal(ArchitectureReadinessState.ReadyForExecutionImplementation, first.ReadinessReport.Readiness);
        Assert.False(first.ReadinessReport.ExecutionCapabilityExists);
        Assert.Empty(first.GapAnalysis.ArchitecturalGaps);
        Assert.Equal(
            new[]
            {
                "artifactGeneration",
                "deployment",
                "executionImplementation",
                "microsoftSkillsImplementation",
                "productUxIntegration",
                "providerImplementation"
            },
            first.GapAnalysis.RemainingWork.Select(gap => gap.Category).OrderBy(category => category, StringComparer.Ordinal).ToArray());
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
    }

    [Fact(DisplayName = "Architecture validation fails closed when trust boundaries or schema versions drift")]
    public void Validate_InvalidContext_FailsClosedForBoundaryAndSchemaDrift()
    {
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage();
        var createdUtc = DateTimeOffset.Parse("2026-06-26T12:30:00+00:00");
        var service = new ArchitectureValidationService();
        var context = service.CreateContext(package, createdUtc);
        var invalidManifest = context.ManifestState.Manifest! with
        {
            Metadata = context.ManifestState.Manifest.Metadata with
            {
                SchemaVersion = "generation-manifest/v2"
            },
            ExecutionConstraints = context.ManifestState.Manifest.ExecutionConstraints with
            {
                DeploymentAllowed = true,
                ProviderInvocationAllowed = true,
                ApiInvocationAllowed = true,
                CliInvocationAllowed = true
            }
        };
        var invalidContext = context with
        {
            ManifestState = context.ManifestState with
            {
                Manifest = invalidManifest
            }
        };

        var state = service.Validate(invalidContext);

        Assert.False(state.IsValid);
        Assert.Contains("generationManifest", state.Diagnostics.SchemaVersionViolations);
        Assert.Contains("noDeployment", state.Diagnostics.TrustBoundaryViolations);
        Assert.Contains("noProviderInvocation", state.Diagnostics.TrustBoundaryViolations);
        Assert.Contains("noMicrosoftApiInvocation", state.Diagnostics.TrustBoundaryViolations);
        Assert.Contains("noCliInvocation", state.Diagnostics.TrustBoundaryViolations);
    }

    [Fact(DisplayName = "Architecture readiness certification distinguishes incomplete, conditional, architecturally complete, and ready-for-execution-implementation states")]
    public void ReadinessCertificationService_EvaluatesEveryReadinessState()
    {
        var service = new ArchitectureReadinessCertificationService();

        Assert.Equal(ArchitectureReadinessState.Incomplete, service.EvaluateReadiness(CreateDiagnostics(layer: ["generationRequest"])));
        Assert.Equal(ArchitectureReadinessState.ConditionallyReady, service.EvaluateReadiness(CreateDiagnostics(boundary: ["noProviderInvocation"])));
        Assert.Equal(ArchitectureReadinessState.ArchitecturallyComplete, service.EvaluateReadiness(CreateDiagnostics(gaps: ["uxIntegrationDeferred"])));
        Assert.Equal(ArchitectureReadinessState.ReadyForExecutionImplementation, service.EvaluateReadiness(ArchitectureValidationDiagnostics.Empty));
    }

    [Fact(DisplayName = "Architecture certification remains certification-only with no PBIR generation, provider invocation, Microsoft API, CLI, deployment, or Analyzer Workspace automation surface")]
    public void ArchitectureCertificationBoundary_RemainsCertificationOnly()
    {
        var forbiddenTokens = new[]
        {
            "GeneratePbir",
            "InvokeProvider",
            "InvokeMicrosoftApi",
            "InvokeApi",
            "InvokeCli",
            "Deploy",
            "RunSkill",
            "PublishArtifact",
            "AutomateAnalyzerWorkspace"
        };
        Type[] types =
        [
            typeof(ArchitectureValidationService),
            typeof(ArchitectureReadinessCertificationService),
            typeof(ArchitectureValidation),
            typeof(ArchitectureCertification),
            typeof(ArchitectureReadinessReport),
            typeof(ArchitectureGapAnalysis),
            typeof(ArchitectureCertificationState)
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

    private static ArchitectureValidationDiagnostics CreateDiagnostics(
        IReadOnlyList<string>? layer = null,
        IReadOnlyList<string>? boundary = null,
        IReadOnlyList<string>? gaps = null)
    {
        return new ArchitectureValidationDiagnostics(
            LayerSeparationViolations: layer ?? [],
            TrustBoundaryViolations: boundary ?? [],
            OwnershipBoundaryViolations: [],
            ProviderNeutralityViolations: [],
            DeterminismViolations: [],
            LineageViolations: [],
            SchemaVersionViolations: [],
            ReadinessTransitionViolations: [],
            ApprovalTransitionViolations: [],
            DeferredArchitectureGaps: gaps ?? []);
    }
}
