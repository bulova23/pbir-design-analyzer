using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class MicrosoftAdapterSpecificationServiceTests
{
    [Fact(DisplayName = "Microsoft Adapter Specification loads a valid microsoft-adapter-specification/v1 contract")]
    public void CreateDefaultSpecification_ValidDefaults_LoadsSpecification()
    {
        var service = new MicrosoftAdapterSpecificationService();

        var specification = service.CreateDefaultSpecification();
        var validation = service.ValidateSpecification(specification);

        Assert.Equal(MicrosoftAdapterSpecificationContract.SchemaVersionV1, specification.SchemaMetadata.SchemaVersion);
        Assert.Equal("microsoft-adapter-specification", specification.SchemaMetadata.SpecificationId);
        Assert.Equal("microsoftPowerBi", specification.ProviderIdentity.ProviderId);
        Assert.True(validation.IsValid);
        Assert.Empty(validation.Diagnostics.UnsupportedSchemaVersions);
        Assert.Contains(
            specification.SupportedTargetProfiles,
            profile => profile.TargetProfileId == GenerationRequestContract.PbirReportDefaultProfile &&
                profile.SupportStatus == MicrosoftAdapterSupportStatus.Supported);
    }

    [Fact(DisplayName = "Microsoft Adapter Specification validation fails closed for missing sections, missing fields, and unsupported schema versions")]
    public void ValidateSpecification_InvalidSpecification_FailsClosed()
    {
        var service = new MicrosoftAdapterSpecificationService();
        var defaults = service.CreateDefaultSpecification();
        var specification = defaults with
        {
            SchemaMetadata = defaults.SchemaMetadata with
            {
                SchemaVersion = "microsoft-adapter-specification/v2",
                SpecificationVersion = string.Empty
            },
            ProviderIdentity = defaults.ProviderIdentity with
            {
                ProviderDisplayName = string.Empty
            },
            CapabilityMappings = [],
            CompatibilityCatalog = null!,
            ReviewRequirementsCatalog = null!
        };

        var validation = service.ValidateSpecification(specification);

        Assert.False(validation.IsValid);
        Assert.Contains("schemaMetadata.specificationVersion", validation.Diagnostics.MissingRequiredFields);
        Assert.Contains("providerIdentity.providerDisplayName", validation.Diagnostics.MissingRequiredFields);
        Assert.Contains("capabilityMappings", validation.Diagnostics.MissingRequiredSections);
        Assert.Contains("compatibilityCatalog", validation.Diagnostics.MissingRequiredSections);
        Assert.Contains("reviewRequirementsCatalog", validation.Diagnostics.MissingRequiredSections);
        Assert.Contains("microsoft-adapter-specification/v2", validation.Diagnostics.UnsupportedSchemaVersions);
    }

    [Fact(DisplayName = "Microsoft Adapter Specification translates PBIR provider planning into supported Microsoft capability requirements deterministically")]
    public void EvaluatePlanning_PbirReport_AssignsSupportedReadinessAndDeterministicTranslation()
    {
        var service = new MicrosoftAdapterSpecificationService();
        var specification = service.CreateDefaultSpecification();
        var adapterRequest = CreateValidProviderAdapterRequest();
        var executionPlan = CreateValidExecutionPlan();

        var first = service.EvaluatePlanning(specification, adapterRequest, executionPlan);
        var second = service.EvaluatePlanning(specification, adapterRequest, executionPlan);

        Assert.Equal(MicrosoftAdapterPlanningReadinessState.Supported, first.Readiness);
        Assert.Equal(first.Translation!.TargetProfileId, second.Translation!.TargetProfileId);
        Assert.Equal(first.Translation.SourceCapabilityRequirements, second.Translation.SourceCapabilityRequirements);
        Assert.Equal(first.Translation.ResolvedCapabilityRequirements, second.Translation.ResolvedCapabilityRequirements);
        Assert.Equal(first.Translation.RequiredCapabilities, second.Translation.RequiredCapabilities);
        Assert.Equal(first.Translation.MissingCapabilities, second.Translation.MissingCapabilities);
        Assert.Equal(first.Translation.PlanningRequirements, second.Translation.PlanningRequirements);
        Assert.Equal(
            ["layoutGeneration", "navigationGeneration", "pageGeneration", "semanticGeneration"],
            first.Translation!.ResolvedCapabilityRequirements);
        Assert.Equal(
            ["Preserve PBIR report-definition structure.", "Preserve page-level navigation intent.", "Preserve semantic-model KPI and filter bindings."],
            first.Translation.PlanningRequirements);
        Assert.Empty(first.Diagnostics.UnsupportedTargetProfiles);
        Assert.Empty(first.Diagnostics.UnsupportedCapabilityRequirements);
        Assert.Empty(first.Diagnostics.FutureCapabilityRequirements);
    }

    [Fact(DisplayName = "Microsoft Adapter Specification resolves planned Fabric Data App combinations as descriptive-only partial support")]
    public void EvaluatePlanning_FabricDataApp_AssignsPartialSupportForFutureCombination()
    {
        var service = new MicrosoftAdapterSpecificationService();
        var specification = service.CreateDefaultSpecification();
        var generationRequest = CreateValidGenerationRequest() with
        {
            RequestId = "genreq:fabricDataApp:designPackage:executive-summary",
            TargetArtifactProfile = new GenerationRequestTargetArtifactProfile(
                ArtifactType: GenerationRequestArtifactType.FabricDataApp,
                ProfileId: GenerationRequestContract.FabricDataAppDefaultProfile,
                SourceExperienceType: OpportunityExperienceType.FabricDataApp)
        };
        var executionPlan = new ExecutionPlanFrameworkService().CreateDraft(generationRequest).Plan!;
        var adapterRequest = new ProviderAdapterFrameworkService(new ProviderAdapterRegistry(), new ProviderAdapterCompatibilityService())
            .BuildAdapterRequest(generationRequest, executionPlan)
            .Request!;

        var state = service.EvaluatePlanning(specification, adapterRequest, executionPlan);

        Assert.Equal(MicrosoftAdapterPlanningReadinessState.PartiallySupported, state.Readiness);
        Assert.Contains(GenerationRequestContract.FabricDataAppDefaultProfile, state.Diagnostics.FutureTargetProfiles);
        Assert.Contains("deploymentSupport", state.Diagnostics.FutureCapabilityRequirements);
        Assert.Equal(MicrosoftAdapterCombinationStatus.Future, state.CompatibilityStatus);
    }

    [Fact(DisplayName = "Microsoft Adapter Specification rejects unsupported Fabric App targets and unsupported capability combinations")]
    public void EvaluatePlanning_UnsupportedTarget_FailsClosed()
    {
        var service = new MicrosoftAdapterSpecificationService();
        var specification = service.CreateDefaultSpecification();
        var generationRequest = CreateValidGenerationRequest() with
        {
            RequestId = "genreq:fabricApp:designPackage:executive-summary",
            TargetArtifactProfile = new GenerationRequestTargetArtifactProfile(
                ArtifactType: GenerationRequestArtifactType.FabricApp,
                ProfileId: GenerationRequestContract.FabricAppDefaultProfile,
                SourceExperienceType: OpportunityExperienceType.FabricApp)
        };
        var executionPlan = CreateValidExecutionPlan() with
        {
            ExecutionPlanId = "execplan:fabricApp:genreq:fabricApp:designPackage:executive-summary",
            SourceReferences = new ExecutionPlanSourceReferences(
                GenerationRequestRef: generationRequest.RequestId,
                SourceDesignPackageRef: generationRequest.SourceDesignPackageRef),
            TargetDefinition = new ExecutionPlanTargetDefinition(
                TargetArtifactProfile: generationRequest.TargetArtifactProfile,
                ExperienceType: OpportunityExperienceType.FabricApp),
            ProviderPlanningMetadata = new ExecutionPlanProviderPlanningMetadata(
                ProviderCategory: ExecutionPlanContract.ProviderNeutralPlanningCategory,
                CapabilityModel: new ExecutionPlanProviderCapabilityModel(
                    SupportsLayoutGeneration: true,
                    SupportsSemanticGeneration: true,
                    SupportsArtifactGeneration: true,
                    SupportsValidation: false),
                SupportedCapabilities: ["layoutGeneration", "semanticGeneration", "artifactGeneration"],
                UnsupportedCapabilities: ["validation"])
        };
        var adapterRequest = new ProviderAdapterRequest(
            SchemaVersion: ProviderAdapterContract.SchemaVersionV1,
            ExecutionPlanRef: executionPlan.ExecutionPlanId,
            GenerationRequestRef: generationRequest.RequestId,
            SourceContractVersions: new ProviderAdapterSourceContractVersions(
                GenerationRequestSchemaVersion: generationRequest.SchemaVersion,
                ExecutionPlanSchemaVersion: executionPlan.SchemaVersion),
            TargetArtifactProfile: generationRequest.TargetArtifactProfile,
            CapabilityRequirements: executionPlan.ProviderPlanningMetadata.SupportedCapabilities,
            Constraints: new ProviderAdapterConstraintSet(
                UnsupportedTargets: executionPlan.PlanningConstraints.UnsupportedTargets,
                UnsupportedCapabilities: executionPlan.PlanningConstraints.UnsupportedCapabilities,
                ReviewRequirements: executionPlan.PlanningConstraints.ReviewRequirements,
                ValidationRequirements: executionPlan.PlanningConstraints.ValidationRequirements),
            ReviewRequirements: executionPlan.ReviewRequirements,
            SuccessContract: generationRequest.SuccessContract);

        var state = service.EvaluatePlanning(specification, adapterRequest, executionPlan);

        Assert.Equal(MicrosoftAdapterPlanningReadinessState.Unsupported, state.Readiness);
        Assert.Equal(MicrosoftAdapterCombinationStatus.Unsupported, state.CompatibilityStatus);
        Assert.Contains(GenerationRequestContract.FabricAppDefaultProfile, state.Diagnostics.UnsupportedTargetProfiles);
        Assert.Contains("deploymentSupport", state.Diagnostics.UnsupportedCapabilityRequirements);
    }

    [Fact(DisplayName = "Microsoft Adapter Specification only becomes ready for a future Microsoft adapter after supported evaluation")]
    public void PrepareForMicrosoftAdapter_SupportedPlanning_TransitionsReadinessOnlyForSupportedState()
    {
        var service = new MicrosoftAdapterSpecificationService();
        var specification = service.CreateDefaultSpecification();
        var supported = service.EvaluatePlanning(specification, CreateValidProviderAdapterRequest(), CreateValidExecutionPlan());
        var futureInputs = CreateFutureFabricDataAppInputs();
        var partial = service.EvaluatePlanning(
            specification,
            futureInputs.AdapterRequest,
            futureInputs.ExecutionPlan);

        var ready = service.PrepareForMicrosoftAdapter(supported);
        var partialReady = service.PrepareForMicrosoftAdapter(partial);

        Assert.Equal(MicrosoftAdapterPlanningReadinessState.ReadyForMicrosoftAdapter, ready.Readiness);
        Assert.Equal(MicrosoftAdapterPlanningReadinessState.PartiallySupported, partialReady.Readiness);
    }

    [Fact(DisplayName = "Microsoft Adapter Specification remains descriptive-only and contains no Microsoft execution, API invocation, artifact generation, deployment, or analyzer automation surface")]
    public void MicrosoftAdapterSpecificationBoundary_RemainsDescriptiveOnly()
    {
        var forbiddenTokens = new[] { "Execute", "Invoke", "Api", "Cli", "GenerateArtifact", "Deploy", "AnalyzerRunner", "ValidateArtifact" };
        Type[] types =
        [
            typeof(MicrosoftAdapterSpecificationService),
            typeof(MicrosoftProviderPlanningTranslator),
            typeof(MicrosoftAdapterCompatibilityCatalog),
            typeof(MicrosoftAdapterSpecification),
            typeof(MicrosoftProviderPlanningTranslation)
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

    [Fact(DisplayName = "Microsoft Adapter Specification contract inventory covers the required field paths for schema, identity, target-profile mapping, compatibility, constraints, and review catalogs")]
    public void MicrosoftAdapterSpecificationInventory_CoversEveryFieldPath()
    {
        var inventoryPaths = MicrosoftAdapterSpecificationContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var modelPaths = EnumerateFieldPaths(typeof(MicrosoftAdapterSpecification), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(modelPaths.ToHashSet(StringComparer.Ordinal), inventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static GenerationRequest CreateValidGenerationRequest()
    {
        return new GenerationRequestFrameworkService()
            .CreateDraft(new DesignPackageConsumptionService().Consume(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage()))
            .Request!;
    }

    private static ExecutionPlan CreateValidExecutionPlan()
    {
        return new ExecutionPlanFrameworkService()
            .CreateDraft(CreateValidGenerationRequest())
            .Plan!;
    }

    private static ProviderAdapterRequest CreateValidProviderAdapterRequest()
    {
        return new ProviderAdapterFrameworkService(new ProviderAdapterRegistry(), new ProviderAdapterCompatibilityService())
            .BuildAdapterRequest(CreateValidGenerationRequest(), CreateValidExecutionPlan())
            .Request!;
    }

    private static (GenerationRequest GenerationRequest, ExecutionPlan ExecutionPlan, ProviderAdapterRequest AdapterRequest) CreateFutureFabricDataAppInputs()
    {
        var generationRequest = CreateValidGenerationRequest() with
        {
            RequestId = "genreq:fabricDataApp:designPackage:executive-summary",
            TargetArtifactProfile = new GenerationRequestTargetArtifactProfile(
                ArtifactType: GenerationRequestArtifactType.FabricDataApp,
                ProfileId: GenerationRequestContract.FabricDataAppDefaultProfile,
                SourceExperienceType: OpportunityExperienceType.FabricDataApp)
        };
        var executionPlan = new ExecutionPlanFrameworkService().CreateDraft(generationRequest).Plan!;
        var adapterRequest = new ProviderAdapterFrameworkService(new ProviderAdapterRegistry(), new ProviderAdapterCompatibilityService())
            .BuildAdapterRequest(generationRequest, executionPlan)
            .Request!;

        return (generationRequest, executionPlan, adapterRequest);
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

        return type.Namespace == typeof(MicrosoftAdapterSpecification).Namespace ? type : null;
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
