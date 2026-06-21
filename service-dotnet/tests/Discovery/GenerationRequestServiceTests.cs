using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class GenerationRequestServiceTests
{
    [Fact(DisplayName = "Generation request creation produces a versioned authoritative execution contract from the normalized design package seam")]
    public void CreateGenerationRequest_ValidConsumptionResult_BuildsVersionedRequest()
    {
        var consumptionService = new DesignPackageConsumptionService();
        var generationRequestService = new GenerationRequestService();

        var consumptionResult = consumptionService.Consume(CreateValidPackage());
        var result = generationRequestService.Create(consumptionResult);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Request);
        Assert.Equal(GenerationRequestContract.SchemaVersionV1, result.Request!.SchemaVersion);
        Assert.Equal("genreq:pbirReport:designPackage:executive-summary", result.Request.RequestId);
        Assert.Equal("designPackage:executive-summary", result.Request.SourceDesignPackageRef);
        Assert.Equal(GenerationRequestArtifactType.PbirReport, result.Request.TargetArtifactProfile.ArtifactType);
        Assert.Equal("advisoryConstructionOnly", result.Request.GenerationMode.Authority);
        Assert.True(result.Request.GenerationMode.ReviewRequired);
        Assert.True(result.Request.GenerationMode.AllowPartialOutput);
        Assert.Equal("Executive", result.Request.DesignIntent.PrimaryAudience);
        Assert.Equal("Track revenue trends.", result.Request.DesignIntent.BusinessOutcome);
        Assert.Equal("Where are revenue trends changing?", result.Request.DesignIntent.AnalyticalFlow.Question);
        Assert.Equal(
            new[] { "Executive Summary", "Regional Detail" },
            result.Request.StructuralIntent.Pages.Select(page => page.Name).ToArray());
        Assert.Equal(
            new[] { "Revenue", "Gross Margin" },
            result.Request.DataIntent.Kpis.Select(kpi => kpi.Name).ToArray());
        Assert.Equal(
            new[] { "Analyzer review required.", "Validation required before downstream handoff." },
            result.Request.SuccessContract.ValidationRequirements);
        Assert.True(result.Request.ReviewPolicy.DesignApprovalRequired);
        Assert.True(result.Request.ReviewPolicy.GenerationApprovalRequired);
        Assert.True(result.Request.ReviewPolicy.AnalyzerReviewRequired);
    }

    [Fact(DisplayName = "Prompt segments are derived deterministically from the generation request and remain repeatable")]
    public void BuildPromptSegments_OutputIsDeterministicAndRepeatable()
    {
        var request = CreateValidGenerationRequest();
        var service = new GenerationRequestService();

        var first = service.BuildPromptSegments(request);
        var second = service.BuildPromptSegments(request);

        Assert.Equal(first, second);
        Assert.Equal(
            new[]
            {
                "Target Summary",
                "Audience Summary",
                "Business Outcome",
                "Structural Intent",
                "Data Intent",
                "Navigation Intent",
                "Success Criteria",
                "Constraints",
            },
            first.Select(segment => segment.Title).ToArray());
        Assert.All(first, segment => Assert.False(string.IsNullOrWhiteSpace(segment.Content)));
    }

    [Fact(DisplayName = "Generation request validation fails when required sections or required fields are missing")]
    public void ValidateGenerationRequest_MissingSectionsOrFields_FailClearly()
    {
        var request = CreateValidGenerationRequest() with
        {
            RequestId = "",
            DesignIntent = new GenerationRequestDesignIntent(
                PrimaryAudience: "",
                SecondaryAudiences: [],
                BusinessOutcome: "",
                AnalyticalFlow: new GenerationRequestAnalyticalFlow("", "", "", "")),
            StructuralIntent = new GenerationRequestStructuralIntent(
                Pages: [],
                Navigation: new GenerationRequestNavigationIntent([], []),
                VisualHints: []),
            DataIntent = new GenerationRequestDataIntent(
                Kpis: [],
                Filters: new GenerationRequestFilters([], []),
                SemanticBinding: null!)
        };

        var service = new GenerationRequestService();
        var result = service.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains("requestId", result.Diagnostics.MissingRequiredFields);
        Assert.Contains("designIntent.primaryAudience", result.Diagnostics.MissingRequiredFields);
        Assert.Contains("designIntent.businessOutcome", result.Diagnostics.MissingRequiredFields);
        Assert.Contains("designIntent.analyticalFlow.question", result.Diagnostics.MissingRequiredFields);
        Assert.Contains("structuralIntent.pages", result.Diagnostics.MissingRequiredSections);
        Assert.Contains("dataIntent.kpis", result.Diagnostics.MissingRequiredSections);
        Assert.Contains("dataIntent.semanticBinding", result.Diagnostics.MissingRequiredSections);
    }

    [Fact(DisplayName = "Generation request validation fails on unsupported target profiles and unsupported schema versions")]
    public void ValidateGenerationRequest_UnsupportedTargetOrSchema_FailsClosed()
    {
        var service = new GenerationRequestService();
        var unsupportedTargetRequest = CreateValidGenerationRequest() with
        {
            TargetArtifactProfile = new GenerationRequestTargetArtifactProfile(GenerationRequestArtifactType.FabricApp)
        };
        var unsupportedSchemaRequest = CreateValidGenerationRequest() with
        {
            SchemaVersion = "generation-request/v2"
        };

        var targetResult = service.Validate(unsupportedTargetRequest);
        var schemaResult = service.Validate(unsupportedSchemaRequest);

        Assert.False(targetResult.IsValid);
        Assert.Contains("fabricApp", targetResult.Diagnostics.UnsupportedTargetProfiles);

        Assert.False(schemaResult.IsValid);
        Assert.Contains("generation-request/v2", schemaResult.Diagnostics.UnsupportedSchemaVersions);
    }

    [Fact(DisplayName = "Generation request creation preserves the design package as the upstream contract and keeps provider specifics out of the provider-neutral core")]
    public void GenerationRequestBoundary_RemainsProviderNeutral()
    {
        var providerSpecificTokens = new[] { "Microsoft", "PowerBi", "Cli" };
        Type[] types =
        [
            typeof(GenerationRequestService),
            typeof(GenerationRequest),
            typeof(GenerationRequestTargetArtifactProfile),
            typeof(GenerationRequestPromptSegment),
            typeof(GenerationRequestValidationDiagnostics),
            typeof(GenerationRequestCreationResult)
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

    [Fact(DisplayName = "Generation request inventory covers every field path so contract drift fails loudly")]
    public void GenerationRequestInventory_CoversEveryFieldPath()
    {
        var inventoryPaths = GenerationRequestContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var modelPaths = EnumerateFieldPaths(typeof(GenerationRequest), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(modelPaths.ToHashSet(StringComparer.Ordinal), inventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static GenerationRequest CreateValidGenerationRequest()
    {
        var consumptionService = new DesignPackageConsumptionService();
        var generationRequestService = new GenerationRequestService();
        var consumptionResult = consumptionService.Consume(CreateValidPackage());
        var creationResult = generationRequestService.Create(consumptionResult);

        Assert.True(creationResult.IsValid);
        return creationResult.Request!;
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

        return type.Namespace == typeof(GenerationRequest).Namespace ? type : null;
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
