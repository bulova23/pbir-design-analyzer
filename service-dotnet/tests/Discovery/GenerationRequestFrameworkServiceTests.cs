using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class GenerationRequestFrameworkServiceTests
{
    [Fact(DisplayName = "Generation Request Framework creates a draft request from consumed Design Package input and preserves contract lineage")]
    public void CreateDraft_ValidConsumptionResult_BuildsDraftRequest()
    {
        var consumptionService = new DesignPackageConsumptionService();
        var framework = new GenerationRequestFrameworkService();

        var consumptionResult = consumptionService.Consume(CreateValidPackage());
        var draft = framework.CreateDraft(consumptionResult);

        Assert.NotNull(draft.Request);
        Assert.Equal(GenerationRequestReadinessState.Draft, draft.Readiness);
        Assert.Equal(GenerationRequestContract.SchemaVersionV1, draft.Request!.SchemaVersion);
        Assert.Equal("genreq:pbirReport:designPackage:executive-summary", draft.Request.RequestId);
        Assert.Equal("designPackage:executive-summary", draft.Request.SourceDesignPackageRef);
        Assert.Equal("pbirReport/default", draft.Request.TargetArtifactProfile.ProfileId);
        Assert.Equal(OpportunityExperienceType.ExecutiveDashboard, draft.Request.TargetArtifactProfile.SourceExperienceType);
        Assert.Equal("Executive", draft.Request.DesignIntent.PrimaryAudience);
        Assert.Equal("Track revenue trends.", draft.Request.DesignIntent.BusinessOutcome);
        Assert.Equal("designPackage:executive-summary", draft.Request.Provenance.SourceDesignPackageRef);
        Assert.True(draft.Request.ReviewPolicy.DesignApprovalRequired);
        Assert.True(draft.Request.ReviewPolicy.GenerationApprovalRequired);
        Assert.True(draft.Request.ReviewPolicy.AnalyzerReviewRequired);
    }

    [Fact(DisplayName = "Generation Request Framework marks valid requests as ready for provider planning only after validation and prompt derivation")]
    public void PrepareForProviderPlanning_ValidRequest_AssignsReadinessAndDerivesPromptSegments()
    {
        var framework = new GenerationRequestFrameworkService();
        var draft = framework.CreateDraft(new DesignPackageConsumptionService().Consume(CreateValidPackage()));

        var validated = framework.Validate(draft);
        var prepared = framework.PrepareForProviderPlanning(validated);

        Assert.Equal(GenerationRequestReadinessState.Valid, validated.Readiness);
        Assert.Equal(GenerationRequestReadinessState.ReadyForProviderPlanning, prepared.Readiness);
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
            prepared.PromptSegments.Select(segment => segment.Title).ToArray());
    }

    [Fact(DisplayName = "Generation Request Framework blocks invalid requests instead of implying approval or execution readiness")]
    public void PrepareForProviderPlanning_InvalidRequest_RemainsBlocked()
    {
        var framework = new GenerationRequestFrameworkService();
        var draft = framework.CreateDraft(new DesignPackageConsumptionService().Consume(CreateInvalidPackage()));

        Assert.Equal(GenerationRequestReadinessState.Blocked, draft.Readiness);
        Assert.Null(draft.Request);
        Assert.Contains("designPackage.Kpis", draft.Diagnostics.MissingInputs);

        var prepared = framework.PrepareForProviderPlanning(draft);

        Assert.Equal(GenerationRequestReadinessState.Blocked, prepared.Readiness);
        Assert.Empty(prepared.PromptSegments);
    }

    [Fact(DisplayName = "Generation Request Framework fails closed for unsupported schema versions, target profiles, and incompatible target profile mappings")]
    public void Validate_RequestCompatibilityFailures_BlockReadiness()
    {
        var framework = new GenerationRequestFrameworkService();
        var draft = framework.CreateDraft(new DesignPackageConsumptionService().Consume(CreateValidPackage()));
        var request = draft.Request! with
        {
            SchemaVersion = "generation-request/v2",
            TargetArtifactProfile = draft.Request!.TargetArtifactProfile with
            {
                ProfileId = "unsupported/default",
                ArtifactType = GenerationRequestArtifactType.FabricDataApp
            }
        };

        var result = framework.Validate(new GenerationRequestFrameworkState(
            Request: request,
            Readiness: GenerationRequestReadinessState.Draft,
            Diagnostics: GenerationRequestValidationDiagnostics.Empty,
            PromptSegments: []));

        Assert.Equal(GenerationRequestReadinessState.Blocked, result.Readiness);
        Assert.Contains("generation-request/v2", result.Diagnostics.UnsupportedSchemaVersions);
        Assert.Contains("unsupported/default", result.Diagnostics.UnsupportedTargetProfiles);
        Assert.Contains(
            "targetArtifactProfile.sourceExperienceType is incompatible with the requested artifact profile.",
            result.Diagnostics.CompatibilityFailures);
    }

    [Fact(DisplayName = "Prompt segment orchestration stays deterministic and depends only on the Generation Request contract")]
    public void PrepareForProviderPlanning_RepeatedCallsProduceIdenticalPromptSegments()
    {
        var framework = new GenerationRequestFrameworkService();
        var validated = framework.Validate(framework.CreateDraft(new DesignPackageConsumptionService().Consume(CreateValidPackage())));

        var first = framework.PrepareForProviderPlanning(validated);
        var second = framework.PrepareForProviderPlanning(validated);

        Assert.Equal(first.PromptSegments, second.PromptSegments);
    }

    [Fact(DisplayName = "Generation Request Framework remains provider-neutral and contains no execution or analyzer invocation surface")]
    public void GenerationRequestFrameworkBoundary_RemainsProviderNeutralAndNonExecuting()
    {
        var forbiddenTokens = new[] { "Microsoft", "PowerBi", "Cli", "PbirGenerator", "AnalyzerRunner", "Execute" };
        Type[] types =
        [
            typeof(GenerationRequestFrameworkService),
            typeof(GenerationRequestBuilder),
            typeof(GenerationRequestValidator),
            typeof(GenerationRequestPromptSegmentOrchestrator),
            typeof(GenerationRequestFrameworkState)
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

    [Fact(DisplayName = "Generation Request contract inventory covers every required field path after the framework additions")]
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

    private static DesignPackage CreateInvalidPackage()
    {
        return CreateValidPackage() with
        {
            Kpis = [],
            SuccessCriteria = new DesignPackageSuccessCriteria([], [])
        };
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
