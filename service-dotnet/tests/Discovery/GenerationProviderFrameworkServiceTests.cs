using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class GenerationProviderFrameworkServiceTests
{
    [Fact(DisplayName = "Generation provider registry supports registration, discovery, provider lookup, capability lookup, and artifact-type lookup")]
    public void GenerationProviderRegistry_RegistersDiscoversAndLooksUpProviders()
    {
        var registry = new GenerationProviderRegistry();
        var first = new GenerationProviderDefinition(
            SchemaVersion: GenerationProviderDefinitionContract.SchemaVersionV1,
            ProviderId: "microsoft.skills.generation-provider",
            ProviderName: "Microsoft Skills Generation Provider",
            ProviderVersion: "1.0.0",
            SupportedArtifactTypes: [GenerationProviderArtifactType.PbirReport],
            SupportedCapabilities: ["pageGeneration"],
            SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile],
            SupportedGenerationModes: [GenerationProviderMode.StructuredRequest],
            Status: GenerationProviderStatus.Available);
        var second = first with
        {
            ProviderId = "local.test.generation-provider",
            ProviderName = "Local Test Generation Provider",
            SupportedArtifactTypes = [GenerationProviderArtifactType.PbirReport, GenerationProviderArtifactType.FabricDataApp],
            SupportedCapabilities = ["pageGeneration"],
            SupportedTargetProfiles = [GenerationRequestContract.PbirReportDefaultProfile, GenerationRequestContract.FabricDataAppDefaultProfile],
            SupportedGenerationModes = [GenerationProviderMode.StructuredRequest, GenerationProviderMode.Mock],
            Status = GenerationProviderStatus.Planned
        };

        registry.Register(first);
        registry.Register(second);

        Assert.True(registry.TryGetProvider(first.ProviderId, out var resolved));
        Assert.Equal(first, resolved);
        Assert.Equal(new[] { second.ProviderId, first.ProviderId }, registry.Discover().Select(provider => provider.ProviderId).ToArray());
        Assert.Equal(new[] { second.ProviderId, first.ProviderId }, registry.FindProvidersByCapability("pageGeneration").Select(provider => provider.ProviderId).ToArray());
        Assert.Equal(new[] { second.ProviderId }, registry.FindProvidersByArtifactType(GenerationProviderArtifactType.FabricDataApp).Select(provider => provider.ProviderId).ToArray());
        Assert.Equal(new[] { second.ProviderId }, registry.FindProvidersByTargetProfile(GenerationRequestContract.FabricDataAppDefaultProfile).Select(provider => provider.ProviderId).ToArray());
    }

    [Fact(DisplayName = "Generation provider framework maps PBIR generation specifications into provider-neutral requests and contexts")]
    public void CreateProviderState_ValidSpecification_MapsProviderNeutralRequest()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var specificationState = new PbirGenerationSpecificationService().PrepareForGenerationProvider(
            new PbirGenerationSpecificationService().CreateSpecification(planning));
        var service = new GenerationProviderFrameworkService();

        var first = service.CreateProviderState(specificationState);
        var second = service.CreateProviderState(specificationState);

        Assert.NotNull(first.Request);
        Assert.NotNull(first.Context);
        Assert.NotNull(first.Result);
        Assert.Equal(GenerationProviderReadinessState.ReadyForGenerationProvider, first.Readiness);
        Assert.Equal(GenerationProviderContract.SchemaVersionV1, first.SchemaVersion);
        Assert.Equal(GenerationProviderRequestContract.SchemaVersionV1, first.Request!.SchemaVersion);
        Assert.Equal("generationProviderRequest:pbirGenerationSpecification:planningOutcome:designPackage:executive-summary", first.Request.Metadata.RequestId);
        Assert.Equal("planningOutcome:designPackage:executive-summary", first.Request.References.PlanningOutcomeReference.OutcomeId);
        Assert.Equal("pbirGenerationSpecification:planningOutcome:designPackage:executive-summary", first.Request.References.PbirSpecificationReference.SpecificationId);
        Assert.Equal(GenerationProviderArtifactType.PbirReport, first.Request.Requirements.CapabilityRequirements.ArtifactType);
        Assert.Equal(GenerationRequestContract.PbirReportDefaultProfile, first.Request.Requirements.CapabilityRequirements.TargetProfileId);
        Assert.Contains("pageGeneration", first.Request.Requirements.CapabilityRequirements.RequiredCapabilities);
        Assert.False(first.Request.Requirements.Constraints.AllowApiInvocation);
        Assert.False(first.Request.Requirements.Constraints.AllowCliInvocation);
        Assert.False(first.Request.Requirements.Constraints.AllowDeployment);
        Assert.False(first.Request.Requirements.Constraints.AllowReportMutation);
        Assert.Equal(GenerationProviderResultStatus.Accepted, first.Result!.Status);
        Assert.Equal(SerializeState(first), SerializeState(second));
    }

    [Fact(DisplayName = "Generation provider requests fail closed when specification completeness is missing")]
    public void CreateProviderState_IncompleteSpecification_RemainsBlocked()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var specification = new PbirGenerationSpecificationService().CreateSpecification(planning).Specification!;
        var invalidState = new PbirGenerationSpecificationState(
            specification with
            {
                ArtifactSpecifications =
                [
                    specification.ArtifactSpecifications[0] with
                    {
                        PageSpecifications = [],
                        SuccessCriteria = new PbirArtifactSuccessCriteria([], [], [])
                    }
                ]
            },
            PbirGenerationSpecificationValidationDiagnostics.Empty,
            PbirGenerationSpecificationReadinessState.Incomplete,
            AcceptsGenerationProvider: false);
        var service = new GenerationProviderFrameworkService();

        var result = service.CreateProviderState(invalidState);

        Assert.Equal(GenerationProviderReadinessState.Blocked, result.Readiness);
        Assert.NotNull(result.Request);
        Assert.Equal(GenerationProviderResultStatus.Blocked, result.Result!.Status);
        Assert.Contains("artifactSpecifications.pageSpecifications", result.Validation.Diagnostics.SpecificationCompletenessFailures);
    }

    [Fact(DisplayName = "Generation provider validator rejects incompatible providers, unsupported artifact types, unsupported target profiles, and incompatible schema versions")]
    public void GenerationProviderValidator_IncompatibleProviders_FailClosed()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var specificationState = new PbirGenerationSpecificationService().PrepareForGenerationProvider(
            new PbirGenerationSpecificationService().CreateSpecification(planning));
        var service = new GenerationProviderFrameworkService();
        var baseline = service.CreateProviderState(specificationState);
        var validator = new GenerationProviderValidator();
        var incompatibleProvider = baseline.Provider! with
        {
            SchemaVersion = "generation-provider-definition/v2",
            SupportedArtifactTypes = [GenerationProviderArtifactType.FabricDataApp],
            SupportedTargetProfiles = [GenerationRequestContract.FabricDataAppDefaultProfile],
            SupportedGenerationModes = [GenerationProviderMode.Mock],
            Status = GenerationProviderStatus.Unsupported
        };

        var validation = validator.Validate(specificationState, baseline.Request!, incompatibleProvider);

        Assert.False(validation.IsValid);
        Assert.Contains("generation-provider-definition/v2", validation.Diagnostics.UnsupportedSchemaVersions);
        Assert.Contains("PbirReport", validation.Diagnostics.UnsupportedArtifactTypes);
        Assert.Contains(GenerationRequestContract.PbirReportDefaultProfile, validation.Diagnostics.UnsupportedTargetProfiles);
        Assert.Contains("StructuredRequest", validation.Diagnostics.UnsupportedGenerationModes);
        Assert.Contains("provider.status must not be unsupported.", validation.Diagnostics.ProviderCompatibilityFailures);
    }

    [Fact(DisplayName = "Generation provider readiness distinguishes unsupported, blocked, candidate, and readyForGenerationProvider states")]
    public void GenerationProviderReadinessService_EvaluatesEveryStateCorrectly()
    {
        var readiness = new GenerationProviderReadinessService();
        var availableProvider = new GenerationProviderDefinition(
            SchemaVersion: GenerationProviderDefinitionContract.SchemaVersionV1,
            ProviderId: "microsoft.skills.generation-provider",
            ProviderName: "Microsoft Skills Generation Provider",
            ProviderVersion: "1.0.0",
            SupportedArtifactTypes: [GenerationProviderArtifactType.PbirReport],
            SupportedCapabilities: ["pageGeneration"],
            SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile],
            SupportedGenerationModes: [GenerationProviderMode.StructuredRequest],
            Status: GenerationProviderStatus.Available);
        var plannedProvider = availableProvider with
        {
            ProviderId = "copilot.generation-provider",
            Status = GenerationProviderStatus.Planned
        };
        var validValidation = new GenerationProviderValidationResult(GenerationProviderValidationDiagnostics.Empty);
        var blockedValidation = new GenerationProviderValidationResult(
            new GenerationProviderValidationDiagnostics(
                MissingRequiredSections: ["requirements.capabilityRequirements"],
                MissingRequiredFields: [],
                UnsupportedSchemaVersions: [],
                UnsupportedArtifactTypes: [],
                UnsupportedTargetProfiles: [],
                UnsupportedGenerationModes: [],
                ProviderCompatibilityFailures: [],
                SpecificationCompletenessFailures: [],
                BoundaryViolations: []));
        var unsupportedValidation = new GenerationProviderValidationResult(
            new GenerationProviderValidationDiagnostics(
                MissingRequiredSections: [],
                MissingRequiredFields: [],
                UnsupportedSchemaVersions: [],
                UnsupportedArtifactTypes: ["PbirReport"],
                UnsupportedTargetProfiles: [],
                UnsupportedGenerationModes: [],
                ProviderCompatibilityFailures: [],
                SpecificationCompletenessFailures: [],
                BoundaryViolations: []));

        Assert.Equal(GenerationProviderReadinessState.Blocked, readiness.Evaluate(blockedValidation, availableProvider));
        Assert.Equal(GenerationProviderReadinessState.Unsupported, readiness.Evaluate(unsupportedValidation, availableProvider));
        Assert.Equal(GenerationProviderReadinessState.Candidate, readiness.Evaluate(validValidation, plannedProvider));
        Assert.Equal(GenerationProviderReadinessState.ReadyForGenerationProvider, readiness.Evaluate(validValidation, availableProvider));
    }

    [Fact(DisplayName = "Generation provider boundary remains metadata-only with no PBIR generation, API invocation, CLI invocation, deployment, or live generation surface")]
    public void GenerationProviderBoundary_RemainsMetadataOnly()
    {
        var forbiddenTokens = new[] { "GeneratePbir", "InvokeApi", "Cli", "Deploy", "Execute", "Publish", "RunSkill" };
        Type[] types =
        [
            typeof(GenerationProviderFrameworkService),
            typeof(GenerationProviderRegistry),
            typeof(GenerationProviderValidator),
            typeof(GenerationProviderReadinessService),
            typeof(GenerationProviderDefinition),
            typeof(GenerationProviderRequest),
            typeof(GenerationProviderContext),
            typeof(GenerationProviderResult)
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

    [Fact(DisplayName = "Generation provider contracts inventory the required field paths for framework, definition, request, context, and result models")]
    public void GenerationProviderContracts_InventoryCoversRequiredFieldPaths()
    {
        var frameworkInventoryPaths = GenerationProviderContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var frameworkModelPaths = EnumerateFieldPaths(typeof(GenerationProviderFrameworkState), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var definitionInventoryPaths = GenerationProviderDefinitionContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var definitionModelPaths = EnumerateFieldPaths(typeof(GenerationProviderDefinition), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var requestInventoryPaths = GenerationProviderRequestContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var requestModelPaths = EnumerateFieldPaths(typeof(GenerationProviderRequest), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var contextInventoryPaths = GenerationProviderContextContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var contextModelPaths = EnumerateFieldPaths(typeof(GenerationProviderContext), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var resultInventoryPaths = GenerationProviderResultContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var resultModelPaths = EnumerateFieldPaths(typeof(GenerationProviderResult), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(frameworkModelPaths.ToHashSet(StringComparer.Ordinal), frameworkInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(definitionModelPaths.ToHashSet(StringComparer.Ordinal), definitionInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(requestModelPaths.ToHashSet(StringComparer.Ordinal), requestInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(contextModelPaths.ToHashSet(StringComparer.Ordinal), contextInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(resultModelPaths.ToHashSet(StringComparer.Ordinal), resultInventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static string SerializeState(GenerationProviderFrameworkState state)
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
