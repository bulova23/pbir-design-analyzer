using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class ProviderAdapterFrameworkServiceTests
{
    [Fact(DisplayName = "Provider Adapter Framework builds provider-adapter/v1 input from execution planning contracts")]
    public void BuildAdapterRequest_ValidInputs_BuildsPlanningOnlyProviderRequest()
    {
        var framework = new ProviderAdapterFrameworkService(new ProviderAdapterRegistry(), new ProviderAdapterCompatibilityService());

        var result = framework.BuildAdapterRequest(CreateValidGenerationRequest(), CreateValidExecutionPlan());

        Assert.NotNull(result.Request);
        Assert.Equal(ProviderAdapterContract.SchemaVersionV1, result.Request!.SchemaVersion);
        Assert.Equal("execplan:pbirReport:genreq:pbirReport:designPackage:executive-summary", result.Request.ExecutionPlanRef);
        Assert.Equal("genreq:pbirReport:designPackage:executive-summary", result.Request.GenerationRequestRef);
        Assert.Equal(GenerationRequestContract.PbirReportDefaultProfile, result.Request.TargetArtifactProfile.ProfileId);
        Assert.Equal(["layoutGeneration", "semanticGeneration"], result.Request.CapabilityRequirements);
        Assert.Equal(["artifactGeneration", "validation"], result.Request.Constraints.UnsupportedCapabilities);
        Assert.True(result.Request.ReviewRequirements.DesignApprovalRequired);
        Assert.True(result.Request.ReviewRequirements.GenerationApprovalRequired);
        Assert.True(result.Request.ReviewRequirements.AnalyzerReviewRequired);
        Assert.Equal(
            CreateValidGenerationRequest().SuccessContract.BusinessSuccessCriteria,
            result.Request.SuccessContract.BusinessSuccessCriteria);
    }

    [Fact(DisplayName = "Provider Adapter Registry registers adapters, discovers them, and supports capability and target-profile lookup")]
    public void Registry_RegisterAndLookup_WorksAcrossMultipleFutureProviders()
    {
        var registry = new ProviderAdapterRegistry();
        var first = CreateAdapterDefinition(
            adapterId: "provider-neutral/layout",
            supportedCapabilities: ["layoutGeneration", "semanticGeneration"],
            unsupportedCapabilities: ["artifactGeneration", "validation"],
            supportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile]);
        var second = CreateAdapterDefinition(
            adapterId: "provider-neutral/data-app",
            supportedCapabilities: ["layoutGeneration", "artifactGeneration"],
            unsupportedCapabilities: ["semanticGeneration", "validation"],
            supportedTargetProfiles: [GenerationRequestContract.FabricDataAppDefaultProfile]);

        registry.Register(first);
        registry.Register(second);

        Assert.Equal(first, registry.Discover("provider-neutral/layout"));
        Assert.Equal(
            new[] { "provider-neutral/layout", "provider-neutral/data-app" },
            registry.DiscoverAll().Select(adapter => adapter.AdapterId).ToArray());
        Assert.Equal(
            new[] { "provider-neutral/layout", "provider-neutral/data-app" },
            registry.FindByCapability("layoutGeneration").Select(adapter => adapter.AdapterId).ToArray());
        Assert.Equal(
            new[] { "provider-neutral/data-app" },
            registry.FindByTargetProfile(GenerationRequestContract.FabricDataAppDefaultProfile).Select(adapter => adapter.AdapterId).ToArray());
    }

    [Fact(DisplayName = "Provider Adapter Framework accepts compatible adapters and marks them ready for a future execution provider")]
    public void EvaluateAdapter_CompatibleAdapter_AssignsAcceptedPlanningResponseAndReadiness()
    {
        var registry = new ProviderAdapterRegistry();
        registry.Register(CreateAdapterDefinition(
            adapterId: "provider-neutral/layout",
            supportedCapabilities: ["layoutGeneration", "semanticGeneration"],
            unsupportedCapabilities: ["artifactGeneration", "validation"],
            supportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile]));

        var framework = new ProviderAdapterFrameworkService(registry, new ProviderAdapterCompatibilityService());
        var result = framework.EvaluateAdapter(
            "provider-neutral/layout",
            CreateValidGenerationRequest(),
            CreateValidExecutionPlan());

        Assert.Equal(ProviderAdapterPlanningReadinessState.ReadyForExecutionProvider, result.Readiness);
        Assert.Equal(ProviderAdapterPlanningResponseStatus.Accepted, result.PlanningResponse!.Status);
        Assert.Equal(ProviderAdapterCompatibilityStatus.Compatible, result.PlanningResponse.Compatibility.Status);
        Assert.Empty(result.Diagnostics.VersionCompatibilityFailures);
        Assert.Empty(result.Diagnostics.TargetCompatibilityFailures);
        Assert.Empty(result.Diagnostics.CapabilityCompatibilityFailures);
    }

    [Fact(DisplayName = "Provider Adapter Framework rejects unsupported targets and incompatible capability or version combinations")]
    public void EvaluateAdapter_UnsupportedAndIncompatibleInputs_FailClosed()
    {
        var registry = new ProviderAdapterRegistry();
        registry.Register(CreateAdapterDefinition(
            adapterId: "provider-neutral/layout",
            supportedCapabilities: ["layoutGeneration"],
            unsupportedCapabilities: ["semanticGeneration", "artifactGeneration", "validation"],
            supportedTargetProfiles: [GenerationRequestContract.FabricDataAppDefaultProfile],
            supportedExecutionPlanSchemaVersions: [ExecutionPlanContract.SchemaVersionV1],
            supportedGenerationRequestSchemaVersions: [GenerationRequestContract.SchemaVersionV1]));

        var framework = new ProviderAdapterFrameworkService(registry, new ProviderAdapterCompatibilityService());
        var unsupportedTarget = framework.EvaluateAdapter(
            "provider-neutral/layout",
            CreateValidGenerationRequest(),
            CreateValidExecutionPlan());

        Assert.Equal(ProviderAdapterPlanningReadinessState.Unsupported, unsupportedTarget.Readiness);
        Assert.Equal(ProviderAdapterPlanningResponseStatus.Unsupported, unsupportedTarget.PlanningResponse!.Status);
        Assert.Contains(GenerationRequestContract.PbirReportDefaultProfile, unsupportedTarget.Diagnostics.TargetCompatibilityFailures);

        var incompatibleVersion = framework.EvaluateAdapter(
            "provider-neutral/layout",
            CreateValidGenerationRequest() with { SchemaVersion = "generation-request/v2" },
            CreateValidExecutionPlan());

        Assert.Equal(ProviderAdapterPlanningReadinessState.Incompatible, incompatibleVersion.Readiness);
        Assert.Equal(ProviderAdapterPlanningResponseStatus.Incompatible, incompatibleVersion.PlanningResponse!.Status);
        Assert.Contains("generation-request/v2", incompatibleVersion.Diagnostics.VersionCompatibilityFailures);
    }

    [Fact(DisplayName = "Provider Adapter Framework remains provider-neutral and contains no Microsoft execution, CLI execution, artifact generation, deployment, or analyzer automation surface")]
    public void ProviderAdapterFrameworkBoundary_RemainsPlanningOnly()
    {
        var forbiddenTokens = new[] { "Microsoft", "PowerBi", "Cli", "Execute", "GenerateArtifact", "Deploy", "AnalyzerRunner" };
        Type[] types =
        [
            typeof(ProviderAdapterFrameworkService),
            typeof(ProviderAdapterRegistry),
            typeof(ProviderAdapterCompatibilityService),
            typeof(ProviderAdapterDefinition),
            typeof(ProviderAdapterRequest),
            typeof(ProviderAdapterPlanningResponse)
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

    [Fact(DisplayName = "Provider Adapter contract inventory covers the required field paths for definition and request contracts")]
    public void ProviderAdapterInventory_CoversEveryFieldPath()
    {
        var inventoryPaths = ProviderAdapterContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var modelPaths = EnumerateFieldPaths(typeof(ProviderAdapterDefinition), prefix: "Definition")
            .Concat(EnumerateFieldPaths(typeof(ProviderAdapterRequest), prefix: "Request"))
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

    private static ProviderAdapterDefinition CreateAdapterDefinition(
        string adapterId,
        IReadOnlyList<string> supportedCapabilities,
        IReadOnlyList<string> unsupportedCapabilities,
        IReadOnlyList<string> supportedTargetProfiles,
        IReadOnlyList<string>? supportedExecutionPlanSchemaVersions = null,
        IReadOnlyList<string>? supportedGenerationRequestSchemaVersions = null)
    {
        return new ProviderAdapterDefinition(
            AdapterId: adapterId,
            AdapterName: "Provider Neutral Adapter",
            AdapterVersion: "1.0.0",
            ProviderCategory: "providerNeutral",
            SupportedTargetProfiles: supportedTargetProfiles,
            SupportedCapabilities: supportedCapabilities,
            UnsupportedCapabilities: unsupportedCapabilities,
            SupportedGenerationRequestSchemaVersions: supportedGenerationRequestSchemaVersions ?? [GenerationRequestContract.SchemaVersionV1],
            SupportedExecutionPlanSchemaVersions: supportedExecutionPlanSchemaVersions ?? [ExecutionPlanContract.SchemaVersionV1]);
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

        return type.Namespace == typeof(ProviderAdapterDefinition).Namespace ? type : null;
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
