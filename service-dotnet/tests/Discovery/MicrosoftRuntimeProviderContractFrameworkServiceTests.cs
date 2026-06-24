using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class MicrosoftRuntimeProviderContractFrameworkServiceTests
{
    [Fact(DisplayName = "Microsoft runtime provider contract accepts valid PBIR Microsoft runtime requests and becomes ready for a future Microsoft runtime provider")]
    public void CreateMicrosoftRuntimeState_ValidPbirCandidate_BecomesReadyForMicrosoftRuntimeProvider()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var registry = new RuntimeProviderRegistry();
        var service = new MicrosoftRuntimeProviderContractFrameworkService(registry);
        var definition = service.CreateDefaultProviderDefinition();
        var registration = service.CreateDefaultRegistration(definition, planning);

        registry.Register(registration);

        var first = service.CreateMicrosoftRuntimeState(planning, registration.ProviderId);
        var second = service.CreateMicrosoftRuntimeState(planning, registration.ProviderId);

        Assert.NotNull(first.Request);
        Assert.NotNull(first.Context);
        Assert.Equal(MicrosoftRuntimeProviderContract.SchemaVersionV1, first.Definition!.SchemaVersion);
        Assert.Equal(MicrosoftRuntimeRequestContract.SchemaVersionV1, first.Request!.SchemaVersion);
        Assert.Equal(MicrosoftRuntimeContextContract.SchemaVersionV1, first.Context!.SchemaVersion);
        Assert.Equal(MicrosoftRuntimeReadinessState.ReadyForMicrosoftRuntimeProvider, first.Readiness);
        Assert.True(first.AcceptsExecutionCandidate);
        Assert.Equal(first.Request.RequestId, first.Context.PlanningLineage.RuntimeRequestRef);
        Assert.Equal(SerializeState(first), SerializeState(second));
    }

    [Fact(DisplayName = "Microsoft runtime provider contract preserves planned-only Fabric Data App handling without becoming executable")]
    public void CreateMicrosoftRuntimeState_FabricDataApp_RemainsPlannedOnly()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage() with
        {
            ExperienceDefinition = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage().ExperienceDefinition with
            {
                ExperienceType = OpportunityExperienceType.FabricDataApp
            }
        });
        var registry = new RuntimeProviderRegistry();
        var service = new MicrosoftRuntimeProviderContractFrameworkService(registry);
        var definition = service.CreateDefaultProviderDefinition();
        var registration = service.CreateDefaultRegistration(definition, planning);

        registry.Register(registration);

        var state = service.CreateMicrosoftRuntimeState(planning, registration.ProviderId);

        Assert.Equal(MicrosoftRuntimeReadinessState.PlannedOnly, state.Readiness);
        Assert.False(state.AcceptsExecutionCandidate);
        Assert.Contains(GenerationRequestContract.FabricDataAppDefaultProfile, state.Validation.Diagnostics.PlannedTargetProfiles);
    }

    [Fact(DisplayName = "Microsoft runtime provider contract rejects unsupported Fabric App and invalid capability mappings")]
    public void ValidateRequest_UnsupportedTargetAndInvalidCapabilityMappings_FailClosed()
    {
        var service = new MicrosoftRuntimeProviderContractFrameworkService(new RuntimeProviderRegistry());
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var definition = service.CreateDefaultProviderDefinition();
        var validRequest = service.BuildRequest(planning, definition);
        var validContext = service.CreateContext(planning, definition, validRequest);

        var unsupportedRequest = validRequest with
        {
            TargetProfile = validRequest.TargetProfile with
            {
                TargetProfileId = GenerationRequestContract.FabricAppDefaultProfile,
                ArtifactType = "fabricApp"
            }
        };
        var invalidCapabilityRequest = validRequest with
        {
            CapabilityRequirements = validRequest.CapabilityRequirements with
            {
                RequiredCapabilities = ["deploymentSupport"]
            }
        };

        var unsupportedValidation = service.ValidateRequest(planning, definition, unsupportedRequest, validContext);
        var invalidCapabilityValidation = service.ValidateRequest(planning, definition, invalidCapabilityRequest, validContext);

        Assert.False(unsupportedValidation.IsValid);
        Assert.Contains(GenerationRequestContract.FabricAppDefaultProfile, unsupportedValidation.Diagnostics.UnsupportedTargetProfiles);
        Assert.False(invalidCapabilityValidation.IsValid);
        Assert.Contains("deploymentSupport", invalidCapabilityValidation.Diagnostics.IncompatibleCapabilities);
    }

    [Fact(DisplayName = "Microsoft runtime readiness evaluates invalid, unsupported, planned-only, blocked, candidate, and ready states correctly")]
    public void MicrosoftRuntimeReadinessService_EvaluatesEveryStateCorrectly()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var registry = new RuntimeProviderRegistry();
        var service = new MicrosoftRuntimeProviderContractFrameworkService(registry);
        var definition = service.CreateDefaultProviderDefinition();
        var registration = service.CreateDefaultRegistration(definition, planning);

        registry.Register(registration);

        var ready = service.CreateMicrosoftRuntimeState(planning, registration.ProviderId);
        var candidate = service.EvaluateReadiness(
            planning,
            definition,
            registration,
            new MicrosoftRuntimeProviderValidationResult(MicrosoftRuntimeValidationDiagnostics.Empty),
            ready.Request! with
            {
                ReviewRequirements = ready.Request!.ReviewRequirements with
                {
                    GenerationApproved = false
                }
            },
            ready.Context!);
        var invalid = service.EvaluateReadiness(
            planning,
            definition,
            registration,
            new MicrosoftRuntimeProviderValidationResult(
                new MicrosoftRuntimeValidationDiagnostics(
                    MissingRequiredSections: ["microsoftRuntimeRequest.provenance"],
                    MissingRequiredFields: [],
                    UnsupportedTargetProfiles: [],
                    PlannedTargetProfiles: [],
                    IncompatibleCapabilities: [],
                    ApprovalFailures: [],
                    ProvenanceFailures: [],
                    VersionMismatches: [],
                    BlockingFailures: [])),
            ready.Request!,
            ready.Context!);
        var unsupported = service.CreateMicrosoftRuntimeState(planning, "runtime-provider:microsoft-runtime-provider:missing");
        var planned = service.CreateMicrosoftRuntimeState(new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage() with
        {
            ExperienceDefinition = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage().ExperienceDefinition with
            {
                ExperienceType = OpportunityExperienceType.FabricDataApp
            }
        }), registration.ProviderId);
        var blocked = service.CreateMicrosoftRuntimeState(new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage() with
        {
            ExperienceDefinition = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage().ExperienceDefinition with
            {
                ExperienceType = OpportunityExperienceType.FabricApp
            }
        }), registration.ProviderId);

        Assert.Equal(MicrosoftRuntimeReadinessState.ReadyForMicrosoftRuntimeProvider, ready.Readiness);
        Assert.Equal(MicrosoftRuntimeReadinessState.Candidate, candidate);
        Assert.Equal(MicrosoftRuntimeReadinessState.Invalid, invalid);
        Assert.Equal(MicrosoftRuntimeReadinessState.Unsupported, unsupported.Readiness);
        Assert.Equal(MicrosoftRuntimeReadinessState.PlannedOnly, planned.Readiness);
        Assert.Equal(MicrosoftRuntimeReadinessState.Blocked, blocked.Readiness);
    }

    [Fact(DisplayName = "Microsoft runtime provider registration works through the runtime provider registry and supports discovery plus capability lookup")]
    public void MicrosoftRuntimeProviderRegistration_RegistersDiscoversAndLooksUpCapabilities()
    {
        var registry = new RuntimeProviderRegistry();
        var service = new MicrosoftRuntimeProviderContractFrameworkService(registry);
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var definition = service.CreateDefaultProviderDefinition();
        var registration = service.CreateDefaultRegistration(definition, planning);

        registry.Register(registration);

        Assert.True(registry.TryGetProvider(registration.ProviderId, out var resolved));
        Assert.NotNull(resolved);
        Assert.Equal(definition.ProviderId, resolved!.ProviderId);
        Assert.Single(registry.DiscoverByCategory(MicrosoftAdapterSpecificationContract.ProviderCategory, GenerationRequestContract.PbirReportDefaultProfile));
        Assert.Contains(
            registry.FindProvidersByCapability("layoutGeneration"),
            provider => provider.ProviderId == registration.ProviderId);
    }

    [Fact(DisplayName = "Microsoft runtime provider contract remains pre-execution with no Microsoft Skills execution, API invocation, CLI invocation, artifact generation, or deployment surface")]
    public void MicrosoftRuntimeProviderBoundary_RemainsContractOnly()
    {
        var forbiddenTokens = new[] { "Execute", "Invoke", "Api", "Cli", "GenerateArtifact", "Deploy", "AnalyzerRunner", "RunSkill" };
        Type[] types =
        [
            typeof(MicrosoftRuntimeProviderContractFrameworkService),
            typeof(MicrosoftRuntimeProviderValidator),
            typeof(MicrosoftRuntimeReadinessService),
            typeof(MicrosoftRuntimeProviderDefinition),
            typeof(MicrosoftRuntimeRequest),
            typeof(MicrosoftRuntimeContext)
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

    [Fact(DisplayName = "Microsoft runtime provider contracts inventory the required field paths for provider definition, request, and context")]
    public void MicrosoftRuntimeProviderContracts_InventoryCoversRequiredFieldPaths()
    {
        var providerInventoryPaths = MicrosoftRuntimeProviderContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var providerModelPaths = EnumerateFieldPaths(typeof(MicrosoftRuntimeProviderDefinition), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var requestInventoryPaths = MicrosoftRuntimeRequestContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var requestModelPaths = EnumerateFieldPaths(typeof(MicrosoftRuntimeRequest), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var contextInventoryPaths = MicrosoftRuntimeContextContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var contextModelPaths = EnumerateFieldPaths(typeof(MicrosoftRuntimeContext), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(providerModelPaths.ToHashSet(StringComparer.Ordinal), providerInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(requestModelPaths.ToHashSet(StringComparer.Ordinal), requestInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(contextModelPaths.ToHashSet(StringComparer.Ordinal), contextInventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static string SerializeState(MicrosoftRuntimeProviderFrameworkState state)
    {
        return string.Join("|",
            state.Definition!.SchemaVersion,
            state.Request!.RequestId,
            state.Context!.ContextId,
            state.Readiness,
            state.AcceptsExecutionCandidate,
            string.Join(",", state.Validation.Diagnostics.UnsupportedTargetProfiles),
            string.Join(",", state.Validation.Diagnostics.PlannedTargetProfiles));
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

        return type.Namespace == typeof(MicrosoftRuntimeProviderDefinition).Namespace ? type : null;
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
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsEnum ||
            type == typeof(string) ||
            type == typeof(bool) ||
            type == typeof(int) ||
            type == typeof(long);
    }
}
