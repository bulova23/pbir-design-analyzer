using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class CapabilityNegotiationServiceTests
{
    [Fact(DisplayName = "Capability Negotiation resolves required capabilities, omits optional unsupported capabilities, and becomes ready for a future execution provider")]
    public void Negotiate_PbirReport_ResolvesRequiredOptionalAndSubstitutedCapabilitiesDeterministically()
    {
        var service = new CapabilityNegotiationService();
        var specification = new MicrosoftAdapterSpecificationService().CreateDefaultSpecification();
        var inputs = CreateValidPbirInputs();

        var first = service.Negotiate(
            inputs.GenerationRequest,
            inputs.ExecutionPlan,
            inputs.AdapterRequest,
            inputs.AdapterDefinition,
            specification);
        var second = service.Negotiate(
            inputs.GenerationRequest,
            inputs.ExecutionPlan,
            inputs.AdapterRequest,
            inputs.AdapterDefinition,
            specification);
        var prepared = service.PrepareForExecutionProvider(first);

        Assert.NotNull(first.Result);
        Assert.Equal(CapabilityNegotiationReadinessState.PartiallyResolved, first.Readiness);
        Assert.Equal(CapabilityNegotiationReadinessState.ReadyForExecutionProvider, prepared.Readiness);
        Assert.Equal(first.Result.SchemaVersion, second.Result!.SchemaVersion);
        Assert.Equal(first.Result.NegotiationId, second.Result.NegotiationId);
        Assert.Equal(first.Result.TargetProfileId, second.Result.TargetProfileId);
        Assert.Equal(first.Result.ProviderCategory, second.Result.ProviderCategory);
        Assert.Equal(
            first.Result.Requirements.Select(SerializeRequirement),
            second.Result.Requirements.Select(SerializeRequirement));
        Assert.Equal(
            first.Result.Resolutions.Select(SerializeResolution),
            second.Result.Resolutions.Select(SerializeResolution));
        Assert.Equal(
            first.Result.Substitutions.Select(SerializeSubstitution),
            second.Result.Substitutions.Select(SerializeSubstitution));
        Assert.Equal(first.Result.ResolutionSummary, second.Result.ResolutionSummary);
        Assert.Equal(first.Diagnostics.MissingRequiredSections, second.Diagnostics.MissingRequiredSections);
        Assert.Equal(first.Diagnostics.MissingRequiredFields, second.Diagnostics.MissingRequiredFields);
        Assert.Equal(first.Diagnostics.MissingCapabilityDefinitions, second.Diagnostics.MissingCapabilityDefinitions);
        Assert.Equal(first.Diagnostics.InvalidSubstitutions, second.Diagnostics.InvalidSubstitutions);
        Assert.Equal(first.Diagnostics.CircularSubstitutions, second.Diagnostics.CircularSubstitutions);
        Assert.Equal(first.Diagnostics.UnsupportedRequiredCapabilities, second.Diagnostics.UnsupportedRequiredCapabilities);
        Assert.Equal(first.Diagnostics.VersionMismatches, second.Diagnostics.VersionMismatches);
        Assert.Equal(first.Diagnostics.CompatibilityFailures, second.Diagnostics.CompatibilityFailures);
        Assert.Equal("capneg:pbirReport/default:execplan:pbirReport:genreq:pbirReport:designPackage:executive-summary", first.Result!.NegotiationId);
        Assert.Equal(GenerationRequestContract.PbirReportDefaultProfile, first.Result.TargetProfileId);
        Assert.Equal(MicrosoftAdapterSpecificationContract.ProviderCategory, first.Result.ProviderCategory);
        Assert.True(first.Result.ResolutionSummary.AllRequiredCapabilitiesSatisfied);
        Assert.Equal(2, first.Result.ResolutionSummary.SatisfiedCount);
        Assert.Equal(2, first.Result.ResolutionSummary.SubstitutedCount);
        Assert.Equal(1, first.Result.ResolutionSummary.OmittedCount);
        Assert.Contains(first.Result.Requirements, requirement =>
            requirement.CapabilityId == "layoutGeneration" &&
            requirement.RequirementLevel == CapabilityRequirementLevel.Required);
        Assert.Contains(first.Result.Requirements, requirement =>
            requirement.CapabilityId == "validationSupport" &&
            requirement.RequirementLevel == CapabilityRequirementLevel.Optional);
        Assert.Contains(first.Result.Resolutions, resolution =>
            resolution.CapabilityId == "layoutGeneration" &&
            resolution.Resolution == CapabilityResolutionStatus.Satisfied &&
            resolution.ResolvedCapabilityId == "layoutGeneration");
        Assert.Contains(first.Result.Resolutions, resolution =>
            resolution.CapabilityId == "navigationGeneration" &&
            resolution.Resolution == CapabilityResolutionStatus.Substituted &&
            resolution.ResolvedCapabilityId == "layoutGeneration");
        Assert.Contains(first.Result.Resolutions, resolution =>
            resolution.CapabilityId == "pageGeneration" &&
            resolution.Resolution == CapabilityResolutionStatus.Substituted &&
            resolution.ResolvedCapabilityId == "layoutGeneration");
        Assert.Contains(first.Result.Resolutions, resolution =>
            resolution.CapabilityId == "validationSupport" &&
            resolution.Resolution == CapabilityResolutionStatus.Omitted);
        Assert.Contains(first.Result.Substitutions, substitution =>
            substitution.OriginalCapabilityId == "navigationGeneration" &&
            substitution.SubstituteCapabilityId == "layoutGeneration");
        Assert.Empty(first.Diagnostics.VersionMismatches);
        Assert.Empty(first.Diagnostics.UnsupportedRequiredCapabilities);
    }

    [Fact(DisplayName = "Capability Negotiation can classify preferred capabilities and resolve them through explicit deterministic substitution rules")]
    public void Negotiate_PreferredCapability_ResolvesWithExplicitAlternateCapability()
    {
        var service = new CapabilityNegotiationService();
        var baseSpecification = new MicrosoftAdapterSpecificationService().CreateDefaultSpecification();
        var specification = baseSpecification with
        {
            CapabilityMappings = baseSpecification.CapabilityMappings
                .Concat(
                [
                    new MicrosoftAdapterCapabilityMapping(
                        CapabilityId: "presentationAssembly",
                        ProviderCapabilityRequirements: ["layoutGeneration"],
                        SupportStatus: MicrosoftAdapterSupportStatus.Supported,
                        Notes: "Presentation assembly remains an enrichment-only capability.")
                ])
                .ToArray()
        };
        var substitutionCatalog = new CapabilityNegotiationSubstitutionCatalog(
            SchemaVersion: CapabilityNegotiationContract.SubstitutionCatalogSchemaVersionV1,
            CatalogId: CapabilityNegotiationContract.DefaultSubstitutionCatalogId,
            CatalogVersion: CapabilityNegotiationContract.DefaultSubstitutionCatalogVersionV1,
            Rules:
            [
                new CapabilityNegotiationSubstitutionRule(
                    RuleId: "preferred-presentation-from-layout",
                    OriginalCapabilityId: "presentationAssembly",
                    SubstituteCapabilityId: "layoutGeneration",
                    AppliesToTargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
                    SubstitutionReason: "Preferred presentation assembly can reuse layout generation outputs.")
            ]);
        var inputs = CreateValidPbirInputs();

        var state = service.Negotiate(
            inputs.GenerationRequest,
            inputs.ExecutionPlan,
            inputs.AdapterRequest,
            inputs.AdapterDefinition,
            specification,
            substitutionCatalog);

        Assert.NotNull(state.Result);
        Assert.Contains(state.Result!.Requirements, requirement =>
            requirement.CapabilityId == "presentationAssembly" &&
            requirement.RequirementLevel == CapabilityRequirementLevel.Preferred);
        Assert.Contains(state.Result.Resolutions, resolution =>
            resolution.CapabilityId == "presentationAssembly" &&
            resolution.RequirementLevel == CapabilityRequirementLevel.Preferred &&
            resolution.Resolution == CapabilityResolutionStatus.Substituted &&
            resolution.ResolvedCapabilityId == "layoutGeneration");
        Assert.Contains(state.Result.Substitutions, substitution =>
            substitution.OriginalCapabilityId == "presentationAssembly" &&
            substitution.SubstituteCapabilityId == "layoutGeneration");
    }

    [Fact(DisplayName = "Capability Negotiation blocks when required capabilities remain unsupported after compatibility and substitution evaluation")]
    public void Negotiate_UnsupportedRequiredCapability_BlocksAppropriately()
    {
        var service = new CapabilityNegotiationService();
        var specification = new MicrosoftAdapterSpecificationService().CreateDefaultSpecification();
        var inputs = CreateValidFabricDataAppInputs();

        var state = service.Negotiate(
            inputs.GenerationRequest,
            inputs.ExecutionPlan,
            inputs.AdapterRequest,
            inputs.AdapterDefinition,
            specification);

        Assert.NotNull(state.Result);
        Assert.Equal(CapabilityNegotiationReadinessState.Blocked, state.Readiness);
        Assert.False(state.Result!.ResolutionSummary.AllRequiredCapabilitiesSatisfied);
        Assert.Contains("deploymentSupport", state.Diagnostics.UnsupportedRequiredCapabilities);
        Assert.Contains(state.Result.Resolutions, resolution =>
            resolution.CapabilityId == "deploymentSupport" &&
            resolution.RequirementLevel == CapabilityRequirementLevel.Required &&
            resolution.Resolution == CapabilityResolutionStatus.Blocked);
    }

    [Fact(DisplayName = "Capability Negotiation validation fails for missing capability definitions, invalid substitutions, circular substitutions, and version mismatches")]
    public void Negotiate_InvalidDefinitionsAndVersions_FailClosed()
    {
        var service = new CapabilityNegotiationService();
        var baseSpecification = new MicrosoftAdapterSpecificationService().CreateDefaultSpecification();
        var specification = baseSpecification with
        {
            SchemaMetadata = baseSpecification.SchemaMetadata with
            {
                SchemaVersion = "microsoft-adapter-specification/v2"
            },
            TargetProfileMappings =
            [
                baseSpecification.TargetProfileMappings.First(mapping =>
                    mapping.TargetProfileId == GenerationRequestContract.PbirReportDefaultProfile) with
                {
                    RequiredCapabilities = ["layoutGeneration", "unknownCapability"]
                }
            ]
        };
        var substitutionCatalog = new CapabilityNegotiationSubstitutionCatalog(
            SchemaVersion: CapabilityNegotiationContract.SubstitutionCatalogSchemaVersionV1,
            CatalogId: CapabilityNegotiationContract.DefaultSubstitutionCatalogId,
            CatalogVersion: CapabilityNegotiationContract.DefaultSubstitutionCatalogVersionV1,
            Rules:
            [
                new CapabilityNegotiationSubstitutionRule(
                    RuleId: "invalid-missing-substitute",
                    OriginalCapabilityId: "unknownCapability",
                    SubstituteCapabilityId: "missingSubstitute",
                    AppliesToTargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
                    SubstitutionReason: "Invalid substitution for validation coverage."),
                new CapabilityNegotiationSubstitutionRule(
                    RuleId: "circular-a",
                    OriginalCapabilityId: "circularA",
                    SubstituteCapabilityId: "circularB",
                    AppliesToTargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
                    SubstitutionReason: "Validation coverage."),
                new CapabilityNegotiationSubstitutionRule(
                    RuleId: "circular-b",
                    OriginalCapabilityId: "circularB",
                    SubstituteCapabilityId: "circularA",
                    AppliesToTargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
                    SubstitutionReason: "Validation coverage.")
            ]);
        var inputs = CreateValidPbirInputs();
        inputs.GenerationRequest = inputs.GenerationRequest with { SchemaVersion = "generation-request/v2" };

        var state = service.Negotiate(
            inputs.GenerationRequest,
            inputs.ExecutionPlan,
            inputs.AdapterRequest,
            inputs.AdapterDefinition,
            specification,
            substitutionCatalog);

        Assert.Equal(CapabilityNegotiationReadinessState.Blocked, state.Readiness);
        Assert.Null(state.Result);
        Assert.Contains("unknownCapability", state.Diagnostics.MissingCapabilityDefinitions);
        Assert.Contains("missingSubstitute", state.Diagnostics.InvalidSubstitutions);
        Assert.Contains("circularA -> circularB -> circularA", state.Diagnostics.CircularSubstitutions);
        Assert.Contains("generation-request/v2", state.Diagnostics.VersionMismatches);
        Assert.Contains("microsoft-adapter-specification/v2", state.Diagnostics.VersionMismatches);
    }

    [Fact(DisplayName = "Capability Negotiation contract inventory covers the required field paths for requirement, resolution, substitution, and result contracts")]
    public void CapabilityNegotiationInventory_CoversEveryFieldPath()
    {
        var inventoryPaths = CapabilityNegotiationContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var modelPaths = EnumerateFieldPaths(typeof(CapabilityNegotiationResult), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(modelPaths.ToHashSet(StringComparer.Ordinal), inventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    [Fact(DisplayName = "Capability Negotiation remains planning-only and exposes no Microsoft execution, provider invocation, CLI execution, artifact generation, deployment, or analyzer automation surface")]
    public void CapabilityNegotiationBoundary_RemainsPlanningOnly()
    {
        var forbiddenTokens = new[] { "Execute", "Invoke", "Api", "Cli", "GenerateArtifact", "Deploy", "AnalyzerRunner", "MicrosoftSkill" };
        Type[] types =
        [
            typeof(CapabilityNegotiationService),
            typeof(CapabilityNegotiationValidator),
            typeof(CapabilityNegotiationResult),
            typeof(CapabilityNegotiationSubstitutionCatalog),
            typeof(CapabilityResolution)
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

    private static (GenerationRequest GenerationRequest, ExecutionPlan ExecutionPlan, ProviderAdapterRequest AdapterRequest, ProviderAdapterDefinition AdapterDefinition) CreateValidPbirInputs()
    {
        var generationRequest = new GenerationRequestFrameworkService()
            .CreateDraft(new DesignPackageConsumptionService().Consume(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage()))
            .Request!;
        var executionPlan = new ExecutionPlanFrameworkService()
            .CreateDraft(generationRequest)
            .Plan!;
        var adapterRequest = new ProviderAdapterFrameworkService(new ProviderAdapterRegistry(), new ProviderAdapterCompatibilityService())
            .BuildAdapterRequest(generationRequest, executionPlan)
            .Request!;
        var adapterDefinition = new ProviderAdapterDefinition(
            AdapterId: "provider-neutral/layout",
            AdapterName: "Provider Neutral Layout Adapter",
            AdapterVersion: "1.0.0",
            ProviderCategory: ProviderAdapterContract.ProviderNeutralCategory,
            SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile, GenerationRequestContract.FabricDataAppDefaultProfile],
            SupportedCapabilities: ["layoutGeneration", "semanticGeneration"],
            UnsupportedCapabilities: ["artifactGeneration", "validation"],
            SupportedGenerationRequestSchemaVersions: [GenerationRequestContract.SchemaVersionV1],
            SupportedExecutionPlanSchemaVersions: [ExecutionPlanContract.SchemaVersionV1]);

        return (generationRequest, executionPlan, adapterRequest, adapterDefinition);
    }

    private static (GenerationRequest GenerationRequest, ExecutionPlan ExecutionPlan, ProviderAdapterRequest AdapterRequest, ProviderAdapterDefinition AdapterDefinition) CreateValidFabricDataAppInputs()
    {
        var generationRequest = CreateValidPbirInputs().GenerationRequest with
        {
            RequestId = "genreq:fabricDataApp:designPackage:executive-summary",
            TargetArtifactProfile = new GenerationRequestTargetArtifactProfile(
                ArtifactType: GenerationRequestArtifactType.FabricDataApp,
                ProfileId: GenerationRequestContract.FabricDataAppDefaultProfile,
                SourceExperienceType: OpportunityExperienceType.FabricDataApp)
        };
        var executionPlan = new ExecutionPlanFrameworkService()
            .CreateDraft(generationRequest)
            .Plan!;
        var adapterRequest = new ProviderAdapterFrameworkService(new ProviderAdapterRegistry(), new ProviderAdapterCompatibilityService())
            .BuildAdapterRequest(generationRequest, executionPlan)
            .Request!;
        var adapterDefinition = CreateValidPbirInputs().AdapterDefinition;

        return (generationRequest, executionPlan, adapterRequest, adapterDefinition);
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

        return type.Namespace == typeof(CapabilityNegotiationResult).Namespace ? type : null;
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

    private static string SerializeRequirement(CapabilityRequirement requirement)
    {
        return $"{requirement.CapabilityId}|{requirement.CapabilityCategory}|{requirement.RequirementLevel}|{string.Join(",", requirement.ProviderCapabilityRequirements)}";
    }

    private static string SerializeResolution(CapabilityResolution resolution)
    {
        return $"{resolution.CapabilityId}|{resolution.RequirementLevel}|{resolution.Resolution}|{resolution.ResolvedCapabilityId}|{resolution.ResolutionReason}";
    }

    private static string SerializeSubstitution(CapabilitySubstitution substitution)
    {
        return $"{substitution.RuleId}|{substitution.OriginalCapabilityId}|{substitution.SubstituteCapabilityId}|{substitution.AppliesToTargetProfileId}";
    }
}
