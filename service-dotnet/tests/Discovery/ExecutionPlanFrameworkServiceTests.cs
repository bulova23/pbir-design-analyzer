using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class ExecutionPlanFrameworkServiceTests
{
    [Fact(DisplayName = "Execution Plan Framework creates execution-plan/v1 from a valid Generation Request and preserves provenance, success contract, and review policy")]
    public void CreateDraft_ValidGenerationRequest_BuildsDraftExecutionPlan()
    {
        var framework = new ExecutionPlanFrameworkService();

        var draft = framework.CreateDraft(CreateValidRequest());

        Assert.NotNull(draft.Plan);
        Assert.Equal(ExecutionPlanReadinessState.Draft, draft.Readiness);
        Assert.Equal(ExecutionPlanContract.SchemaVersionV1, draft.Plan!.SchemaVersion);
        Assert.Equal("execplan:pbirReport:genreq:pbirReport:designPackage:executive-summary", draft.Plan.ExecutionPlanId);
        Assert.Equal("genreq:pbirReport:designPackage:executive-summary", draft.Plan.SourceReferences.GenerationRequestRef);
        Assert.Equal("designPackage:executive-summary", draft.Plan.SourceReferences.SourceDesignPackageRef);
        Assert.Equal(GenerationRequestContract.PbirReportDefaultProfile, draft.Plan.TargetDefinition.TargetArtifactProfile.ProfileId);
        Assert.Equal(OpportunityExperienceType.ExecutiveDashboard, draft.Plan.TargetDefinition.ExperienceType);
        Assert.Equal(ExecutionPlanContract.ProviderNeutralPlanningCategory, draft.Plan.ProviderPlanningMetadata.ProviderCategory);
        Assert.Equal(["layoutGeneration", "semanticGeneration"], draft.Plan.ProviderPlanningMetadata.SupportedCapabilities);
        Assert.Equal(["artifactGeneration", "validation"], draft.Plan.ProviderPlanningMetadata.UnsupportedCapabilities);
        Assert.Equal(draft.Request!.SuccessContract, draft.Plan.SuccessContract);
        Assert.True(draft.Plan.ReviewRequirements.DesignApprovalRequired);
        Assert.True(draft.Plan.ReviewRequirements.GenerationApprovalRequired);
        Assert.True(draft.Plan.ReviewRequirements.AnalyzerReviewRequired);
        Assert.Equal(
            new[] { "schema-analysis", "artifact-design", "layout-planning", "semantic-planning", "validation-planning" },
            draft.Plan.PlannedWorkUnits.Select(unit => unit.WorkUnitId).ToArray());
    }

    [Fact(DisplayName = "Execution Plan Framework marks only valid plans as ready for provider adapters and preserves deterministic output")]
    public void PrepareForProviderAdapter_ValidDraft_AssignsReadinessAndProducesDeterministicPlan()
    {
        var framework = new ExecutionPlanFrameworkService();
        var draft = framework.CreateDraft(CreateValidRequest());

        var validated = framework.Validate(draft);
        var first = framework.PrepareForProviderAdapter(validated);
        var second = framework.PrepareForProviderAdapter(validated);

        Assert.Equal(ExecutionPlanReadinessState.Valid, validated.Readiness);
        Assert.Equal(ExecutionPlanReadinessState.ReadyForProviderAdapter, first.Readiness);
        Assert.Equal(first.Plan, second.Plan);
    }

    [Fact(DisplayName = "Execution Plan validation fails closed for missing sections and unsupported schema versions")]
    public void Validate_MissingSectionsAndSchemaVersionFailures_BlockReadiness()
    {
        var framework = new ExecutionPlanFrameworkService();
        var draft = framework.CreateDraft(CreateValidRequest());
        var invalidPlan = draft.Plan! with
        {
            SchemaVersion = "execution-plan/v2",
            PlannedWorkUnits = [],
            DependencyGraph = null!
        };

        var result = framework.Validate(new ExecutionPlanFrameworkState(
            Request: draft.Request,
            Plan: invalidPlan,
            Readiness: ExecutionPlanReadinessState.Draft,
            Diagnostics: ExecutionPlanValidationDiagnostics.Empty));

        Assert.Equal(ExecutionPlanReadinessState.Blocked, result.Readiness);
        Assert.Contains("execution-plan/v2", result.Diagnostics.UnsupportedSchemaVersions);
        Assert.Contains("plannedWorkUnits", result.Diagnostics.MissingRequiredSections);
        Assert.Contains("dependencyGraph", result.Diagnostics.MissingRequiredSections);
    }

    [Fact(DisplayName = "Execution Plan validation fails closed for dependency failures, capability inconsistencies, review-policy drift, and unsupported targets")]
    public void Validate_DependencyCapabilityTargetAndReviewFailures_BlockReadiness()
    {
        var framework = new ExecutionPlanFrameworkService();
        var draft = framework.CreateDraft(CreateValidRequest());
        var invalidPlan = draft.Plan! with
        {
            TargetDefinition = draft.Plan.TargetDefinition with
            {
                TargetArtifactProfile = draft.Plan.TargetDefinition.TargetArtifactProfile with
                {
                    ArtifactType = GenerationRequestArtifactType.FabricApp,
                    ProfileId = GenerationRequestContract.FabricAppDefaultProfile
                }
            },
            ProviderPlanningMetadata = draft.Plan.ProviderPlanningMetadata with
            {
                CapabilityModel = draft.Plan.ProviderPlanningMetadata.CapabilityModel with
                {
                    SupportsArtifactGeneration = true
                }
            },
            DependencyGraph = draft.Plan.DependencyGraph with
            {
                Dependencies =
                [
                    new ExecutionPlanDependency("validation-planning", ["missing-work-unit"])
                ]
            },
            ReviewRequirements = draft.Plan.ReviewRequirements with
            {
                AnalyzerReviewRequired = false
            }
        };

        var result = framework.Validate(new ExecutionPlanFrameworkState(
            Request: draft.Request,
            Plan: invalidPlan,
            Readiness: ExecutionPlanReadinessState.Draft,
            Diagnostics: ExecutionPlanValidationDiagnostics.Empty));

        Assert.Equal(ExecutionPlanReadinessState.Blocked, result.Readiness);
        Assert.Contains(GenerationRequestContract.FabricAppDefaultProfile, result.Diagnostics.UnsupportedTargetProfiles);
        Assert.Contains("dependencyGraph.dependencies references unknown work unit missing-work-unit.", result.Diagnostics.DependencyFailures);
        Assert.Contains("providerPlanningMetadata capability declarations must match supportedCapabilities and unsupportedCapabilities.", result.Diagnostics.CapabilityInconsistencies);
        Assert.Contains("reviewRequirements.analyzerReviewRequired must stay true.", result.Diagnostics.ReviewRequirementFailures);
    }

    [Fact(DisplayName = "Blocked execution plans cannot become ready for provider adapters")]
    public void PrepareForProviderAdapter_BlockedPlan_RemainsBlocked()
    {
        var framework = new ExecutionPlanFrameworkService();
        var blocked = new ExecutionPlanFrameworkState(
            Request: CreateValidRequest(),
            Plan: null,
            Readiness: ExecutionPlanReadinessState.Blocked,
            Diagnostics: new ExecutionPlanValidationDiagnostics(
                MissingRequiredSections: ["plannedWorkUnits"],
                MissingRequiredFields: [],
                UnsupportedTargetProfiles: [],
                UnsupportedSchemaVersions: [],
                DependencyFailures: [],
                CapabilityInconsistencies: [],
                TargetCompatibilityFailures: [],
                ReviewRequirementFailures: []));

        var prepared = framework.PrepareForProviderAdapter(blocked);

        Assert.Equal(ExecutionPlanReadinessState.Blocked, prepared.Readiness);
        Assert.Null(prepared.Plan);
    }

    [Fact(DisplayName = "Execution Plan Framework remains provider-neutral and contains no Microsoft execution, CLI execution, artifact generation, or analyzer automation surface")]
    public void ExecutionPlanFrameworkBoundary_RemainsPlanningOnly()
    {
        var forbiddenTokens = new[] { "Microsoft", "PowerBi", "Cli", "Execute", "GenerateArtifact", "AnalyzerRunner", "Deploy" };
        Type[] types =
        [
            typeof(ExecutionPlanFrameworkService),
            typeof(ExecutionPlanBuilder),
            typeof(ExecutionPlanValidator),
            typeof(ExecutionPlan),
            typeof(ExecutionPlanFrameworkState)
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

    [Fact(DisplayName = "Execution Plan contract inventory covers every required field path after provider-planning additions")]
    public void ExecutionPlanInventory_CoversEveryFieldPath()
    {
        var inventoryPaths = ExecutionPlanContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var modelPaths = EnumerateFieldPaths(typeof(ExecutionPlan), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(modelPaths.ToHashSet(StringComparer.Ordinal), inventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static GenerationRequest CreateValidRequest()
    {
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage();
        var request = new GenerationRequestFrameworkService()
            .CreateDraft(new DesignPackageConsumptionService().Consume(package))
            .Request;

        Assert.NotNull(request);
        return request!;
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

        return type.Namespace == typeof(ExecutionPlan).Namespace ? type : null;
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

internal static class GenerationRequestFrameworkServiceTestsAccessor
{
    internal static DesignPackage CreateValidPackage()
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
}
