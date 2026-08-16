using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class DesignPackageConsumptionServiceTests
{
    [Fact(DisplayName = "Consumption inventory keeps required optional transformed and ignored semantics explicit")]
    public void Consume_InventoryRemainsExplicit()
    {
        var inventory = DesignPackageConsumptionService.Inventory
            .OrderBy(entry => entry.FieldPath, StringComparer.Ordinal)
            .ToArray();

        var expectedKeyEntries = new[]
        {
            ("AnalyticalFlow", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed),
            ("Audience", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct),
            ("Audience.Personas", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct),
            ("Audience.PrimaryAudience", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct),
            ("Audience.SecondaryAudiences", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct),
            ("DiscoveryContext", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct),
            ("ExperienceDefinition", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct),
            ("ExperienceDefinition.BusinessOutcome", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct),
            ("ExperienceDefinition.BusinessValue", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Ignored),
            ("ExperienceDefinition.Complexity", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Ignored),
            ("ExperienceDefinition.Confidence", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Ignored),
            ("ExperienceDefinition.ExperienceType", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed),
            ("Filters", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct),
            ("Kpis", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct),
            ("Navigation", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed),
            ("PackageId", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Direct),
            ("Pages", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed),
            ("ProviderGuidance", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct),
            ("Provenance", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed),
            ("RecommendationRationale", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Direct),
            ("RecommendationRationale.ProvenanceNotes", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Ignored),
            ("RecommendationRationale.SupportingSemanticSignals", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Ignored),
            ("SuccessCriteria", DesignPackageConsumptionRequirement.Required, DesignPackageConsumptionHandling.Transformed),
            ("VisualRecommendations", DesignPackageConsumptionRequirement.Optional, DesignPackageConsumptionHandling.Transformed),
        };

        foreach (var expectedEntry in expectedKeyEntries)
        {
            Assert.Contains(
                inventory,
                entry => entry.FieldPath == expectedEntry.Item1 &&
                    entry.Requirement == expectedEntry.Item2 &&
                    entry.Handling == expectedEntry.Item3);
        }
    }

    [Fact(DisplayName = "Consumption inventory covers every Design Package field path so contract drift fails loudly")]
    public void Consume_InventoryCoversEveryDesignPackageFieldPath()
    {
        var inventoryPaths = DesignPackageConsumptionService.Inventory
            .Select(entry => entry.FieldPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var modelPaths = EnumerateFieldPaths(typeof(DesignPackage), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(modelPaths, inventoryPaths);
    }

    [Fact(DisplayName = "Consumption validation fails clearly when required fields are missing")]
    public void Consume_MissingRequiredFieldsFailClearly()
    {
        var service = new DesignPackageConsumptionService();
        var package = CreateValidPackage() with
        {
            Audience = new DesignPackageAudience(
                PrimaryAudience: "",
                SecondaryAudiences: [],
                Personas: []),
            ExperienceDefinition = new DesignPackageExperienceDefinition(
                ExperienceType: OpportunityExperienceType.PbirReport,
                BusinessOutcome: "",
                Confidence: DiscoveryConfidenceLevel.High,
                BusinessValue: RecommendationBusinessValueLevel.High,
                Complexity: RecommendationComplexityLevel.Medium),
            Kpis = [],
            SuccessCriteria = new DesignPackageSuccessCriteria([], [])
        };

        var result = service.Consume(package);

        Assert.False(result.IsValid);
        Assert.Null(result.ConsumedPackage);
        Assert.Null(result.NormalizedGenerationInput);
        Assert.Contains("Audience.PrimaryAudience", result.Diagnostics.MissingRequiredFields);
        Assert.Contains("ExperienceDefinition.BusinessOutcome", result.Diagnostics.MissingRequiredFields);
        Assert.Contains("Kpis", result.Diagnostics.MissingRequiredFields);
        Assert.Contains("SuccessCriteria.BusinessSuccessCriteria", result.Diagnostics.MissingRequiredFields);
        Assert.Contains("SuccessCriteria.AnalyticalSuccessCriteria", result.Diagnostics.MissingRequiredFields);
    }

    [Fact(DisplayName = "Consumption preserves optional fields without making them minimum package blockers")]
    public void Consume_OptionalFieldsRemainOptional()
    {
        var service = new DesignPackageConsumptionService();
        var package = CreateValidPackage() with
        {
            Audience = new DesignPackageAudience(
                PrimaryAudience: "Executive",
                SecondaryAudiences: [],
                Personas: []),
            VisualRecommendations = [],
            RecommendationRationale = null!,
            ProviderGuidance = null!
        };

        var result = service.Consume(package);

        Assert.True(result.IsValid);
        Assert.NotNull(result.ConsumedPackage);
        Assert.NotNull(result.NormalizedGenerationInput);
        Assert.Empty(result.Diagnostics.MissingRequiredFields);
        Assert.Empty(result.NormalizedGenerationInput!.VisualHints);
        Assert.Empty(result.ConsumedPackage!.SecondaryAudiences);
        Assert.Empty(result.ConsumedPackage.Personas);
    }

    [Fact(DisplayName = "Consumption transforms report-shaped packages into provider-neutral normalized generation input")]
    public void Consume_TransformsFieldsIntoNormalizedGenerationInput()
    {
        var service = new DesignPackageConsumptionService();

        var result = service.Consume(CreateValidPackage());

        Assert.True(result.IsValid);
        Assert.NotNull(result.NormalizedGenerationInput);
        Assert.Equal(GenerationArtifactType.PbirReport, result.NormalizedGenerationInput!.TargetArtifactType);
        Assert.Equal("designPackage:executive-summary", result.NormalizedGenerationInput.SourceDesignPackageRef);
        Assert.Equal("Executive", result.NormalizedGenerationInput.PrimaryAudience);
        Assert.Equal("Track revenue trends.", result.NormalizedGenerationInput.BusinessOutcome);
        Assert.Equal(
            new[] { "Executive Summary", "Regional Detail" },
            result.NormalizedGenerationInput.PagesOrRoutes.Select(page => page.Name).ToArray());
        Assert.Equal(
            new[] { "Executive Summary", "Regional Detail" },
            result.NormalizedGenerationInput.NavigationHierarchy);
        Assert.Equal(
            new[] { "summary", "investigate", "decide" },
            result.NormalizedGenerationInput.WorkflowPath);
        Assert.Equal(
            new[] { "Revenue", "Gross Margin" },
            result.NormalizedGenerationInput.Kpis.Select(kpi => kpi.Name).ToArray());
        Assert.True(result.NormalizedGenerationInput.SuccessContract.ReviewRequired);
        Assert.True(result.NormalizedGenerationInput.SuccessContract.ValidationRequired);
    }

    [Fact(DisplayName = "Consumption rejects unsupported experience types and incompatible package states")]
    public void Consume_RejectsUnsupportedOrIncompatiblePackages()
    {
        var service = new DesignPackageConsumptionService();
        var unsupportedPackage = CreateValidPackage() with
        {
            ExperienceDefinition = new DesignPackageExperienceDefinition(
                ExperienceType: OpportunityExperienceType.FabricApp,
                BusinessOutcome: "Coordinate workflow actions.",
                Confidence: DiscoveryConfidenceLevel.High,
                BusinessValue: RecommendationBusinessValueLevel.High,
                Complexity: RecommendationComplexityLevel.High)
        };
        var incompatiblePackage = CreateValidPackage() with
        {
            Filters = new DesignPackageFilterSet(
                GlobalFilters: ["Date"],
                PageFilters:
                [
                    new DesignPackagePageFilter("Unknown Page", ["Region"])
                ])
        };

        var unsupportedResult = service.Consume(unsupportedPackage);
        var incompatibleResult = service.Consume(incompatiblePackage);

        Assert.False(unsupportedResult.IsValid);
        Assert.Contains("FabricApp", unsupportedResult.Diagnostics.UnsupportedExperienceTypes);

        Assert.False(incompatibleResult.IsValid);
        Assert.Contains(
            "Filters.PageFilters references page 'Unknown Page' that does not exist in Pages.",
            incompatibleResult.Diagnostics.IncompatiblePackageStates);
    }

    [Fact(DisplayName = "Consumption models stay provider-neutral and do not leak Microsoft-specific execution concepts")]
    public void Consume_BoundaryRemainsProviderNeutral()
    {
        var providerSpecificTokens = new[] { "Microsoft", "PowerBi", "Cli", "Prompt" };
        Type[] types =
        [
            typeof(DesignPackageConsumptionService),
            typeof(ConsumedDesignPackageView),
            typeof(NormalizedGenerationInput),
            typeof(DesignPackageConsumptionDiagnostics),
            typeof(DesignPackageFieldConsumptionMetadata)
        ];

        foreach (var type in types)
        {
            Assert.DoesNotContain(providerSpecificTokens, token => type.Name.Contains(token, StringComparison.OrdinalIgnoreCase));

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Assert.DoesNotContain(providerSpecificTokens, token => property.Name.Contains(token, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    private static DesignPackage CreateValidPackage()
    {
        return new DesignPackage(
            PackageId: "designPackage:executive-summary",
            DiscoveryContext: new DesignPackageDiscoveryContext(
                SemanticModelSource: new DesignPackageReference("semanticModel", "semanticModel:test", "Semantic model"),
                DiscoveryProfileReference: new DesignPackageReference("discoveryProfile", "discoveryProfile:test", "Discovery profile"),
                OpportunityReference: new DesignPackageReference("opportunity", "opportunity:test", "Opportunity"),
                RecommendationReference: new DesignPackageReference("recommendation", "recommendation:test", "Recommendation"),
                ExperienceBlueprintReference: new DesignPackageReference("experienceBlueprint", "experienceBlueprint:test", "Blueprint")),
            Audience: new DesignPackageAudience(
                PrimaryAudience: "Executive",
                SecondaryAudiences: ["Regional Manager"],
                Personas:
                [
                    new DesignPackagePersona("Executive", "primary", "Primary decision-maker"),
                    new DesignPackagePersona("Regional Manager", "secondary", "Regional follow-through")
                ]),
            ExperienceDefinition: new DesignPackageExperienceDefinition(
                ExperienceType: OpportunityExperienceType.ExecutiveDashboard,
                BusinessOutcome: "Track revenue trends.",
                Confidence: DiscoveryConfidenceLevel.High,
                BusinessValue: RecommendationBusinessValueLevel.High,
                Complexity: RecommendationComplexityLevel.Medium),
            Pages:
            [
                new DesignPackagePage("Executive Summary", "Show top KPIs.", "Entry page."),
                new DesignPackagePage("Regional Detail", "Investigate regional variance.", "Decision page.")
            ],
            Kpis:
            [
                new DesignPackageKpi("Revenue", "Track total revenue.", "Revenue"),
                new DesignPackageKpi("Gross Margin", "Track margin.", "Revenue")
            ],
            Filters: new DesignPackageFilterSet(
                GlobalFilters: ["Date"],
                PageFilters:
                [
                    new DesignPackagePageFilter("Executive Summary", ["Region"]),
                    new DesignPackagePageFilter("Regional Detail", ["Region", "Territory"])
                ]),
            VisualRecommendations:
            [
                new DesignPackageVisualRecommendation("Executive Summary", "Card", "Show KPI status"),
                new DesignPackageVisualRecommendation("Regional Detail", "BarChart", "Compare regions")
            ],
            Navigation: new DesignPackageNavigation(
                Hierarchy: ["Executive Summary", "Regional Detail"],
                WorkflowPath: ["summary", "investigate", "decide"]),
            AnalyticalFlow: new DesignPackageAnalyticalFlow(
                Question: "Where are revenue trends changing?",
                Investigation: "Compare regions over time.",
                Evidence: "Use revenue and gross margin trends.",
                Decision: "Focus follow-up on underperforming regions."),
            SuccessCriteria: new DesignPackageSuccessCriteria(
                BusinessSuccessCriteria: ["Executives can review revenue confidently."],
                AnalyticalSuccessCriteria: ["The flow supports summary to investigation to decision."]),
            RecommendationRationale: new DesignPackageRecommendationRationale(
                RecommendationExplanation: "This package fits executive reporting.",
                SupportingSemanticSignals: ["Revenue trend coverage"],
                LimitingFactors: ["Territory coverage is moderate"],
                AudienceRationale: "Executives need a concise review surface.",
                BusinessOutcomeRationale: "Revenue trend visibility drives leadership action.",
                ExperienceTypeRationale: "A dashboard fits recurring revenue review.",
                KpiRationale: ["Revenue anchors the business question."],
                PageRationale: ["The pages support summary then detail."],
                NavigationRationale: "Navigation preserves the decision path.",
                AnalyticalFlowRationale: "The flow moves from question to action.",
                ProvenanceNotes: ["Derived from discovery recommendation."]),
            ProviderGuidance: new DesignPackageProviderGuidance(
                WhyThisPackageExists: "Provide a clear executive review surface.",
                ExperienceToGenerate: "Executive dashboard",
                SuccessLooksLike: "Pages, KPIs, and filters remain aligned."),
            Provenance: new DesignPackageProvenance(
                PackageReference: "designPackage:executive-summary",
                Lineage:
                [
                    new DesignPackageReference("semanticModel", "semanticModel:test", "Semantic model"),
                    new DesignPackageReference("discoveryProfile", "discoveryProfile:test", "Discovery profile"),
                    new DesignPackageReference("opportunity", "opportunity:test", "Opportunity"),
                    new DesignPackageReference("recommendation", "recommendation:test", "Recommendation"),
                    new DesignPackageReference("experienceBlueprint", "experienceBlueprint:test", "Blueprint"),
                    new DesignPackageReference("designPackage", "designPackage:executive-summary", "Design package")
                ]));
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

            var nestedType = GetNestedContractType(property.PropertyType);
            if (nestedType is not null)
            {
                fieldPaths.AddRange(EnumerateFieldPaths(nestedType, path));
            }
        }

        return fieldPaths;
    }

    private static Type? GetNestedContractType(Type type)
    {
        if (IsScalar(type))
        {
            return null;
        }

        if (TryGetEnumerableElementType(type, out var elementType))
        {
            return IsScalar(elementType) ? null : elementType;
        }

        return type.Namespace == typeof(DesignPackage).Namespace ? type : null;
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

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
