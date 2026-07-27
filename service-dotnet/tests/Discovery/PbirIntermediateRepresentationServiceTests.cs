using System.Reflection;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirIntermediateRepresentationServiceTests
{
    [Fact(DisplayName = "PBIR IR service creates deterministic canonical IR with stable hashes, ordering, immutable identifiers, and preserved intent")]
    public void CreateIntermediateRepresentation_ReadyInputs_CreatesDeterministicCanonicalIr()
    {
        var inputs = CreateReadyIrInputs();
        var generatedUtc = DateTimeOffset.Parse("2026-06-26T14:00:00+00:00");
        var service = new PbirIntermediateRepresentationService();

        var first = service.CreateIntermediateRepresentation(
            inputs.ManifestState,
            inputs.SpecificationState,
            generatedUtc);
        var second = service.CreateIntermediateRepresentation(
            inputs.ManifestState,
            inputs.SpecificationState,
            generatedUtc);
        var later = service.CreateIntermediateRepresentation(
            inputs.ManifestState,
            inputs.SpecificationState,
            generatedUtc.AddHours(1));

        Assert.NotNull(first.Ir);
        Assert.Equal(PbirIntermediateRepresentationReadinessState.ReadyForSerializer, first.Readiness);
        Assert.True(first.Validation.IsValid);
        Assert.Equal(PbirIntermediateRepresentationContract.SchemaVersionV1, first.Ir!.Metadata.SchemaVersion);
        Assert.Equal("pbirIr:generationManifest:planningOutcome:designPackage:executive-summary", first.Ir.Metadata.IrId);
        Assert.Equal(generatedUtc.UtcDateTime, first.Ir.Metadata.GeneratedUtc);
        Assert.Equal(first.Ir.Metadata.IrId, later.Ir!.Metadata.IrId);
        Assert.Equal(inputs.ManifestState.Manifest!.Metadata.ManifestId, first.Ir.References.GenerationManifestRef);
        Assert.Equal(inputs.SpecificationState.Specification!.SpecificationId, first.Ir.References.PbirGenerationSpecificationRef);

        Assert.NotEmpty(first.Ir.Pages);
        Assert.Equal(Enumerable.Range(1, first.Ir.Pages.Count), first.Ir.Pages.Select(page => page.Order));
        Assert.Equal(first.Ir.Pages.OrderBy(page => page.PageId, StringComparer.Ordinal).Select(page => page.PageId), first.Ir.Pages.Select(page => page.PageId));
        Assert.All(first.Ir.Pages, page =>
        {
            Assert.False(string.IsNullOrWhiteSpace(page.PageId));
            Assert.False(string.IsNullOrWhiteSpace(page.IntendedPurpose));
            Assert.False(string.IsNullOrWhiteSpace(page.NavigationBehavior));
        });

        Assert.NotEmpty(first.Ir.Visuals);
        Assert.Equal(first.Ir.Visuals.OrderBy(visual => visual.VisualId, StringComparer.Ordinal).Select(visual => visual.VisualId), first.Ir.Visuals.Select(visual => visual.VisualId));
        Assert.All(first.Ir.Visuals, visual =>
        {
            Assert.Contains(first.Ir.Pages, page => page.PageId == visual.PageId);
            Assert.False(string.IsNullOrWhiteSpace(visual.VisualType));
            Assert.False(string.IsNullOrWhiteSpace(visual.Placement));
            Assert.False(string.IsNullOrWhiteSpace(visual.SemanticIntent));
            Assert.NotEmpty(visual.InteractionModel);
        });

        Assert.NotEmpty(first.Ir.Semantics);
        Assert.All(first.Ir.Semantics, semantic =>
        {
            Assert.Contains(first.Ir.Pages, page => page.PageId == semantic.PageId);
            Assert.NotEmpty(semantic.Measures);
            Assert.NotEmpty(semantic.Dimensions);
            Assert.NotEmpty(semantic.Kpis);
            Assert.NotEmpty(semantic.Filters);
            Assert.False(string.IsNullOrWhiteSpace(semantic.DrillBehavior));
            Assert.NotEmpty(semantic.Relationships);
        });

        Assert.Equal(first.Ir.Navigation.LandingPage, first.Ir.Pages.First().PageId);
        Assert.NotEmpty(first.Ir.Navigation.PageTransitions);
        Assert.NotEmpty(first.Ir.Navigation.DrillPaths);
        Assert.Contains(first.Ir.Navigation.Bookmarks, bookmark => bookmark == $"landing:{first.Ir.Navigation.LandingPage}");

        Assert.NotEmpty(first.Ir.Layout.Containers);
        Assert.All(first.Ir.Layout.Containers, container =>
        {
            Assert.Contains(first.Ir.Pages, page => page.PageId == container.PageId);
            Assert.NotEmpty(container.VisualRefs);
        });
        Assert.NotEmpty(first.Ir.Layout.Spacing);
        Assert.NotEmpty(first.Ir.Layout.Alignment);
        Assert.NotEmpty(first.Ir.Layout.ResponsiveHints);

        var sourceSuccessCriteria = inputs.SpecificationState.Specification.ArtifactSpecifications.Single().SuccessCriteria;
        Assert.Equal(sourceSuccessCriteria.BusinessSuccessCriteria, first.Ir.SuccessCriteria.BusinessIntent);
        Assert.Equal(sourceSuccessCriteria.AnalyticalSuccessCriteria, first.Ir.SuccessCriteria.AnalyticalFlow);
        Assert.Equal(sourceSuccessCriteria.PlanningOutcomeRequirements, first.Ir.SuccessCriteria.SuccessCriteria);

        Assert.All(inputs.ManifestState.Manifest.Lineage.ImmutableUpstreamLineage, reference => Assert.Contains(reference, first.Ir.Lineage.ImmutableLineage));
        Assert.Contains(first.Ir.Metadata.IrId, first.Ir.Lineage.ImmutableLineage);
        Assert.Equal(64, first.Ir.Hashes.InputHash.Length);
        Assert.Equal(64, first.Ir.Hashes.ContentHash.Length);
        Assert.Equal(64, first.Ir.Hashes.LineageHash.Length);
        Assert.Equal(Serialize(first.Ir), Serialize(second.Ir));
    }

    [Fact(DisplayName = "PBIR IR validator fails closed for invalid layout, semantic, navigation, and incomplete IR")]
    public void Validate_InvalidIr_FailsClosed()
    {
        var inputs = CreateReadyIrInputs();
        var baseline = new PbirIntermediateRepresentationService().CreateIntermediateRepresentation(
            inputs.ManifestState,
            inputs.SpecificationState,
            DateTimeOffset.Parse("2026-06-26T14:00:00+00:00"));
        var validator = new PbirIntermediateRepresentationValidator();

        var invalidLayout = baseline.Ir! with
        {
            Layout = baseline.Ir.Layout with
            {
                Containers = []
            }
        };
        var invalidSemantics = baseline.Ir with
        {
            Semantics =
            [
                baseline.Ir.Semantics[0] with
                {
                    PageId = "missing-page",
                    Measures = []
                }
            ]
        };
        var invalidNavigation = baseline.Ir with
        {
            Navigation = baseline.Ir.Navigation with
            {
                LandingPage = "missing-page",
                PageTransitions =
                [
                    new PbirIntermediateRepresentationPageTransition(
                        FromPageId: baseline.Ir.Pages[0].PageId,
                        ToPageId: "missing-page",
                        Transition: "invalid")
                ]
            }
        };
        var incomplete = baseline.Ir with
        {
            Metadata = baseline.Ir.Metadata with
            {
                SchemaVersion = "pbir-ir/v2"
            },
            Pages = []
        };

        var layoutValidation = validator.Validate(invalidLayout);
        var semanticValidation = validator.Validate(invalidSemantics);
        var navigationValidation = validator.Validate(invalidNavigation);
        var incompleteValidation = validator.Validate(incomplete);

        Assert.False(layoutValidation.IsValid);
        Assert.Contains("layout.containers must include every declared page.", layoutValidation.Diagnostics.InvalidLayoutDefinitions);
        Assert.False(semanticValidation.IsValid);
        Assert.Contains("semantic.pageId must match a declared page.", semanticValidation.Diagnostics.InvalidSemanticDefinitions);
        Assert.Contains("semantic.measures must not be empty.", semanticValidation.Diagnostics.InvalidSemanticDefinitions);
        Assert.False(navigationValidation.IsValid);
        Assert.Contains("navigation.landingPage must match a declared page.", navigationValidation.Diagnostics.InvalidNavigationDefinitions);
        Assert.Contains("navigation.pageTransitions must reference declared pages.", navigationValidation.Diagnostics.InvalidNavigationDefinitions);
        Assert.False(incompleteValidation.IsValid);
        Assert.Contains("pbir-ir/v2", incompleteValidation.Diagnostics.UnsupportedSchemaVersions);
        Assert.Contains("pages", incompleteValidation.Diagnostics.MissingRequiredSections);
    }

    [Fact(DisplayName = "PBIR IR readiness distinguishes incomplete, blocked, canonical, and readyForSerializer states")]
    public void ReadinessService_EvaluatesEveryStateCorrectly()
    {
        var readiness = new PbirIntermediateRepresentationReadinessService();
        var incompleteValidation = new PbirIntermediateRepresentationValidationResult(
            new PbirIntermediateRepresentationValidationDiagnostics(
                MissingRequiredSections: ["pages"],
                MissingRequiredFields: [],
                InvalidReferences: [],
                InvalidNavigationDefinitions: [],
                InvalidSemanticDefinitions: [],
                InvalidLayoutDefinitions: [],
                UnsupportedSchemaVersions: [],
                BoundaryViolations: []));
        var blockedValidation = new PbirIntermediateRepresentationValidationResult(
            new PbirIntermediateRepresentationValidationDiagnostics(
                MissingRequiredSections: [],
                MissingRequiredFields: [],
                InvalidReferences: [],
                InvalidNavigationDefinitions: ["navigation.landingPage must match a declared page."],
                InvalidSemanticDefinitions: [],
                InvalidLayoutDefinitions: [],
                UnsupportedSchemaVersions: [],
                BoundaryViolations: []));
        var canonicalValidation = new PbirIntermediateRepresentationValidationResult(PbirIntermediateRepresentationValidationDiagnostics.Empty);

        Assert.Equal(PbirIntermediateRepresentationReadinessState.Incomplete, readiness.Evaluate(incompleteValidation, prepareForSerializer: false));
        Assert.Equal(PbirIntermediateRepresentationReadinessState.Blocked, readiness.Evaluate(blockedValidation, prepareForSerializer: false));
        Assert.Equal(PbirIntermediateRepresentationReadinessState.Canonical, readiness.Evaluate(canonicalValidation, prepareForSerializer: false));
        Assert.Equal(PbirIntermediateRepresentationReadinessState.ReadyForSerializer, readiness.Evaluate(canonicalValidation, prepareForSerializer: true));
    }

    [Fact(DisplayName = "PBIR serializer request is a contract boundary only and does not serialize PBIR")]
    public void CreateSerializerRequest_ReadyIr_CreatesRequestContractOnly()
    {
        var inputs = CreateReadyIrInputs();
        var irState = new PbirIntermediateRepresentationService().CreateIntermediateRepresentation(
            inputs.ManifestState,
            inputs.SpecificationState,
            DateTimeOffset.Parse("2026-06-26T14:00:00+00:00"));

        var request = new PbirIntermediateRepresentationService().CreateSerializerRequest(irState);

        Assert.Equal(PbirSerializerRequestContract.SchemaVersionV1, request.SchemaVersion);
        Assert.Equal($"pbirSerializerRequest:{irState.Ir!.Metadata.IrId}", request.RequestId);
        Assert.Equal(irState.Ir.Metadata.IrId, request.PbirIrRef);
        Assert.Equal(irState.Ir.Hashes.ContentHash, request.PbirIrContentHash);
        Assert.True(request.SerializerImplementationAvailable);
        Assert.False(request.ProviderInvocationAllowed);
        Assert.False(request.DeploymentAllowed);
        Assert.False(request.MicrosoftSkillsExecutionAllowed);
    }

    [Fact(DisplayName = "PBIR IR layer exposes no PBIR serialization, Microsoft Skills execution, provider invocation, deployment, CLI, or API surface")]
    public void PbirIntermediateRepresentationBoundary_RemainsNonExecuting()
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
            "Publish",
            "Execute"
        };
        Type[] types =
        [
            typeof(PbirIntermediateRepresentationService),
            typeof(PbirIntermediateRepresentationValidator),
            typeof(PbirIntermediateRepresentationReadinessService),
            typeof(PbirIntermediateRepresentation),
            typeof(PbirIntermediateRepresentationState),
            typeof(PbirSerializerRequest)
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

    internal static (GenerationManifestState ManifestState, PbirGenerationSpecificationState SpecificationState) CreateReadyIrInputs()
    {
        var generationInputs = GenerationManifestServiceTests.CreateReadyInputs();
        var manifestState = new GenerationManifestService().CreateManifestState(
            generationInputs.Planning,
            generationInputs.SpecificationState,
            generationInputs.ProviderState,
            generationInputs.ExecutionPlanningState,
            generationInputs.RuntimeProviderState,
            generationInputs.MicrosoftRuntimeState,
            DateTimeOffset.Parse("2026-06-26T13:45:00+00:00"));

        return (manifestState, generationInputs.SpecificationState);
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
