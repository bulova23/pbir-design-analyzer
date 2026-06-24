using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirGenerationSpecificationServiceTests
{
    [Fact(DisplayName = "PBIR generation specification maps pages, visuals, KPIs, navigation, and success criteria from Design Studio intent into artifact specifications")]
    public void CreateSpecification_ValidPlanningOutcome_MapsAuthoritativeArtifactSpecification()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var service = new PbirGenerationSpecificationService();

        var first = service.CreateSpecification(planning);
        var second = service.CreateSpecification(planning);

        Assert.NotNull(first.Specification);
        Assert.Equal(PbirGenerationSpecificationReadinessState.Specified, first.Readiness);
        Assert.Equal(PbirGenerationSpecificationContract.SchemaVersionV1, first.Specification!.SchemaVersion);
        Assert.Equal("pbirGenerationSpecification:planningOutcome:designPackage:executive-summary", first.Specification.SpecificationId);
        Assert.Equal("designPackage:executive-summary", first.Specification.DesignReferences.DesignPackageReference);
        Assert.Equal("genreq:pbirReport:designPackage:executive-summary", first.Specification.DesignReferences.GenerationRequestReference);
        Assert.Equal("planningOutcome:designPackage:executive-summary", first.Specification.DesignReferences.PlanningOutcomeReference);

        var artifact = Assert.Single(first.Specification.ArtifactSpecifications);
        Assert.Equal(PbirArtifactSpecificationContract.SchemaVersionV1, artifact.SchemaVersion);
        Assert.Equal(GenerationRequestContract.PbirReportDefaultProfile, artifact.TargetProfileId);
        Assert.Equal(new[] { "Executive Summary", "Regional Detail" }, artifact.PageSpecifications.Select(page => page.PageId).ToArray());
        Assert.Equal("Executive", artifact.PageSpecifications[0].Audience);
        Assert.Equal("Entry page.", artifact.PageSpecifications[0].NavigationBehavior);
        Assert.Equal(new[] { "Executive Summary:Card", "Regional Detail:BarChart" }, artifact.VisualSpecifications.Select(visual => $"{visual.PageId}:{visual.VisualType}").ToArray());
        Assert.Equal(new[] { "Revenue", "Gross Margin" }, artifact.SemanticSpecifications.Select(semantic => semantic.KpiBinding).ToArray());
        Assert.Equal(new[] { "summary", "investigate", "decide" }, artifact.NavigationSpecifications.DrillPaths.ToArray());
        Assert.Equal(new[] { "Executives can review revenue confidently." }, artifact.SuccessCriteria.BusinessSuccessCriteria.ToArray());
        Assert.Equal(SerializeState(first), SerializeState(second));
    }

    [Fact(DisplayName = "PBIR generation specification validation fails incomplete or invalid specifications and missing design intent fails closed")]
    public void Validate_IncompleteOrInvalidSpecifications_FailClosed()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var service = new PbirGenerationSpecificationService();
        var validator = new PbirGenerationSpecificationValidator();

        var created = service.CreateSpecification(planning);
        var incompleteSpecification = created.Specification! with
        {
            ArtifactSpecifications =
            [
                created.Specification!.ArtifactSpecifications[0] with
                {
                    PageSpecifications = [],
                    VisualSpecifications = [],
                    SemanticSpecifications = [],
                    NavigationSpecifications = created.Specification.ArtifactSpecifications[0].NavigationSpecifications with
                    {
                        LandingPage = string.Empty,
                        PageTransitions = [],
                        DrillPaths = []
                    },
                    SuccessCriteria = new PbirArtifactSuccessCriteria([], [], [])
                }
            ]
        };
        var missingDesignIntent = created.Specification! with
        {
            DesignReferences = created.Specification!.DesignReferences with
            {
                DesignPackageReference = string.Empty
            }
        };

        var incompleteValidation = validator.Validate(incompleteSpecification);
        var missingIntentValidation = validator.Validate(missingDesignIntent);

        Assert.False(incompleteValidation.IsValid);
        Assert.Contains("artifactSpecifications.pageSpecifications", incompleteValidation.Diagnostics.MissingRequiredSections);
        Assert.Contains("artifactSpecifications.visualSpecifications", incompleteValidation.Diagnostics.MissingRequiredSections);
        Assert.Contains("artifactSpecifications.semanticSpecifications", incompleteValidation.Diagnostics.MissingRequiredSections);
        Assert.Contains("artifactSpecifications.navigationSpecifications", incompleteValidation.Diagnostics.MissingRequiredSections);
        Assert.Contains("artifactSpecifications.successCriteria", incompleteValidation.Diagnostics.MissingRequiredSections);

        Assert.False(missingIntentValidation.IsValid);
        Assert.Contains("designReferences.designPackageReference", missingIntentValidation.Diagnostics.MissingRequiredFields);
    }

    [Fact(DisplayName = "PBIR generation specification readiness distinguishes incomplete, partiallySpecified, specified, and readyForGenerationProvider states")]
    public void ReadinessService_EvaluatesEveryStateCorrectly()
    {
        var readiness = new PbirGenerationSpecificationReadinessService();
        var validValidation = new PbirGenerationSpecificationValidationResult(PbirGenerationSpecificationValidationDiagnostics.Empty);
        var incompleteValidation = new PbirGenerationSpecificationValidationResult(
            new PbirGenerationSpecificationValidationDiagnostics(
                MissingRequiredSections: ["artifactSpecifications.pageSpecifications"],
                MissingRequiredFields: [],
                MissingDesignIntent: [],
                InvalidPageDefinitions: [],
                InvalidVisualDefinitions: [],
                InvalidSemanticDefinitions: [],
                InvalidNavigationDefinitions: [],
                IncompleteSuccessCriteria: [],
                UnsupportedSchemaVersions: [],
                BoundaryViolations: []));
        var partialValidation = new PbirGenerationSpecificationValidationResult(
            new PbirGenerationSpecificationValidationDiagnostics(
                MissingRequiredSections: [],
                MissingRequiredFields: [],
                MissingDesignIntent: [],
                InvalidPageDefinitions: [],
                InvalidVisualDefinitions: ["visual.pageId must match a declared page."],
                InvalidSemanticDefinitions: [],
                InvalidNavigationDefinitions: [],
                IncompleteSuccessCriteria: [],
                UnsupportedSchemaVersions: [],
                BoundaryViolations: []));

        Assert.Equal(PbirGenerationSpecificationReadinessState.Incomplete, readiness.Evaluate(incompleteValidation));
        Assert.Equal(PbirGenerationSpecificationReadinessState.PartiallySpecified, readiness.Evaluate(partialValidation));
        Assert.Equal(PbirGenerationSpecificationReadinessState.Specified, readiness.Evaluate(validValidation));
        Assert.Equal(
            PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider,
            readiness.PrepareForGenerationProvider(PbirGenerationSpecificationReadinessState.Specified, hasArtifacts: true));
    }

    [Fact(DisplayName = "PBIR generation specification remains specification-only with no PBIR generation, Microsoft API invocation, CLI invocation, or deployment surface")]
    public void PbirGenerationSpecificationBoundary_RemainsSpecificationOnly()
    {
        var forbiddenTokens = new[] { "GeneratePbir", "InvokeApi", "Cli", "Deploy", "RunSkill", "Execute", "Publish" };
        Type[] types =
        [
            typeof(PbirGenerationSpecificationService),
            typeof(PbirGenerationSpecificationValidator),
            typeof(PbirGenerationSpecificationReadinessService),
            typeof(PbirGenerationSpecification),
            typeof(PbirArtifactSpecification)
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

    [Fact(DisplayName = "PBIR generation specification contracts inventory the required field paths for generation and artifact specification models")]
    public void PbirGenerationSpecificationContracts_InventoryCoversRequiredFieldPaths()
    {
        var generationInventoryPaths = PbirGenerationSpecificationContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var generationModelPaths = EnumerateFieldPaths(typeof(PbirGenerationSpecification), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var artifactInventoryPaths = PbirArtifactSpecificationContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var artifactModelPaths = EnumerateFieldPaths(typeof(PbirArtifactSpecification), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(generationModelPaths.ToHashSet(StringComparer.Ordinal), generationInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(artifactModelPaths.ToHashSet(StringComparer.Ordinal), artifactInventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static string SerializeState(PbirGenerationSpecificationState state)
    {
        return System.Text.Json.JsonSerializer.Serialize(state);
    }

    private static IReadOnlyList<string> EnumerateFieldPaths(Type type, string? prefix)
    {
        var fieldPaths = new List<string>();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var path = string.IsNullOrWhiteSpace(prefix)
                ? property.Name
                : $"{prefix}.{property.Name}";
            fieldPaths.Add(path);

            if (IsScalar(property.PropertyType))
            {
                continue;
            }

            if (TryGetEnumerableElementType(property.PropertyType, out var elementType))
            {
                if (!IsScalar(elementType))
                {
                    foreach (var childPath in EnumerateFieldPaths(elementType, path))
                    {
                        fieldPaths.Add(childPath);
                    }
                }

                continue;
            }

            foreach (var childPath in EnumerateFieldPaths(property.PropertyType, path))
            {
                fieldPaths.Add(childPath);
            }
        }

        return fieldPaths
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    private static bool IsScalar(Type type)
    {
        return type.IsEnum ||
            type == typeof(string) ||
            type == typeof(bool) ||
            type == typeof(int) ||
            type == typeof(double);
    }
}
