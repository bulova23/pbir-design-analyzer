using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class RuntimeProviderAbstractionFrameworkServiceTests
{
    [Fact(DisplayName = "Runtime Provider Abstraction Framework creates a deterministic pre-execution candidate that is only ready for a future runtime provider")]
    public void CreateRuntimeCandidate_ValidPlanningOutcome_BuildsDeterministicReadyCandidate()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var registry = new RuntimeProviderRegistry();
        var service = new RuntimeProviderAbstractionFrameworkService(registry);
        var registration = service.CreateDefaultRegistration(planning.ExecutionProviderState!.ProviderDefinition!, planning.ExecutionProviderState.ProviderRequest!);

        registry.Register(registration);

        var first = service.CreateRuntimeCandidate(planning, registration.ProviderId);
        var second = service.CreateRuntimeCandidate(planning, registration.ProviderId);

        Assert.NotNull(first.Request);
        Assert.NotNull(first.Context);
        Assert.NotNull(first.Result);
        Assert.NotNull(first.ExecutionCandidate);
        Assert.Equal(RuntimeProviderRequestContract.SchemaVersionV1, first.Request!.SchemaVersion);
        Assert.Equal(RuntimeProviderContextContract.SchemaVersionV1, first.Context!.SchemaVersion);
        Assert.Equal(RuntimeProviderResultContract.SchemaVersionV1, first.Result!.SchemaVersion);
        Assert.Equal(RuntimeProviderContract.SchemaVersionV1, first.ExecutionCandidate!.SchemaVersion);
        Assert.Equal(RuntimeProviderReadinessState.ReadyForRuntimeProvider, first.Readiness);
        Assert.Equal(RuntimeProviderResultStatus.Accepted, first.Result.Status);
        Assert.Equal(planning.Outcome.Metadata.OutcomeId, first.Request.PlanningOutcomeRef);
        Assert.Equal(planning.Outcome.References.ExecutionProviderRef, first.Request.ExecutionProviderRef);
        Assert.Equal(planning.Outcome.References.ExecutionPlanRef, first.Request.ExecutionPlanRef);
        Assert.Equal(planning.Outcome.References.NegotiationRef, first.Request.CapabilityResolutionRef);
        Assert.Equal(planning.GenerationRequestState.Request!.TargetArtifactProfile.ProfileId, first.Context.TargetProfileId);
        Assert.Equal(planning.ExecutionProviderState.ProviderDefinition!.ProviderCategory, first.Context.ProviderCategory);
        Assert.Equal(first.Request.RequestId, first.Context.ExecutionLineage.RequestRef);
        Assert.Equal(first.Request.RequestId, first.ExecutionCandidate.RequestRef);
        Assert.Equal(first.Context.ContextId, first.ExecutionCandidate.ContextRef);
        Assert.Equal(first.Result.ResultId, first.ExecutionCandidate.ResultRef);
        Assert.Equal(SerializeState(first), SerializeState(second));
    }

    [Fact(DisplayName = "Runtime Provider Abstraction Framework preserves blocked planning outcomes and does not create execution candidates from blocked runtime states")]
    public void CreateRuntimeCandidate_BlockedPlanningOutcome_RemainsBlocked()
    {
        var orchestrationService = new PlanningOrchestrationService();
        var validPackage = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage();
        var blockedPlanning = orchestrationService.Orchestrate(validPackage with
        {
            ExperienceDefinition = validPackage.ExperienceDefinition with
            {
                ExperienceType = OpportunityExperienceType.FabricApp
            }
        });
        var registry = new RuntimeProviderRegistry();
        var service = new RuntimeProviderAbstractionFrameworkService(registry);
        var readyPlanning = orchestrationService.Orchestrate(validPackage);
        var registration = service.CreateDefaultRegistration(readyPlanning.ExecutionProviderState!.ProviderDefinition!, readyPlanning.ExecutionProviderState.ProviderRequest!);

        registry.Register(registration);

        var blockedState = service.CreateRuntimeCandidate(blockedPlanning, registration.ProviderId);

        Assert.Equal(RuntimeProviderReadinessState.Blocked, blockedState.Readiness);
        Assert.Equal(RuntimeProviderResultStatus.Blocked, blockedState.Result!.Status);
        Assert.Null(blockedState.ExecutionCandidate);
        Assert.Contains("FabricApp", string.Join("|", blockedState.Result.Reasons));
    }

    [Fact(DisplayName = "Runtime Provider validation succeeds for valid requests and fails closed for invalid lineage, references, capability resolution, and version mismatches")]
    public void ValidateRequest_ValidAndInvalidRequests_ProduceExpectedOutcomes()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var registry = new RuntimeProviderRegistry();
        var service = new RuntimeProviderAbstractionFrameworkService(registry);
        var registration = service.CreateDefaultRegistration(planning.ExecutionProviderState!.ProviderDefinition!, planning.ExecutionProviderState.ProviderRequest!);
        registry.Register(registration);

        var validState = service.CreateRuntimeCandidate(planning, registration.ProviderId);
        var invalidRequest = validState.Request! with
        {
            PlanningOutcomeRef = "planningOutcome:different",
            ExecutionPlanRef = "execplan:different",
            CapabilityResolutionRef = "capneg:different",
            SchemaVersion = "runtime-provider-request/v2",
            SourceContractVersions = validState.Request!.SourceContractVersions with
            {
                PlanningOutcomeSchemaVersion = "planning-outcome/v2"
            },
            ExecutionConstraints = validState.Request.ExecutionConstraints with
            {
                RequiredCapabilities = ["unsupportedCapability"]
            }
        };
        var invalidContext = validState.Context! with
        {
            ExecutionLineage = validState.Context!.ExecutionLineage with
            {
                PlanningOutcomeRef = "planningOutcome:other"
            }
        };

        var validValidation = service.ValidateRequest(planning, registration.ProviderId, validState.Request!, validState.Context!);
        var invalidValidation = service.ValidateRequest(planning, registration.ProviderId, invalidRequest, invalidContext);

        Assert.True(validValidation.IsValid);
        Assert.False(invalidValidation.IsValid);
        Assert.Contains("runtime-provider-request/v2", invalidValidation.Diagnostics.VersionMismatches);
        Assert.Contains("planning-outcome/v2", invalidValidation.Diagnostics.VersionMismatches);
        Assert.Contains("runtimeProviderRequest.planningOutcomeRef must match planningOutcome.metadata.outcomeId.", invalidValidation.Diagnostics.InvalidReferences);
        Assert.Contains("runtimeProviderRequest.executionPlanRef must match planningOutcome.references.executionPlanRef.", invalidValidation.Diagnostics.InvalidReferences);
        Assert.Contains("runtimeProviderRequest.capabilityResolutionRef must match planningOutcome.references.negotiationRef.", invalidValidation.Diagnostics.InvalidReferences);
        Assert.Contains("runtimeProviderContext.executionLineage.planningOutcomeRef must match runtimeProviderRequest.planningOutcomeRef.", invalidValidation.Diagnostics.InvalidLineage);
        Assert.Contains("unsupportedCapability", invalidValidation.Diagnostics.CapabilityResolutionFailures);
    }

    [Fact(DisplayName = "Runtime Provider readiness evaluates invalid, blocked, unsupported, candidate, and ready states correctly")]
    public void RuntimeReadinessService_EvaluatesEveryStateCorrectly()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var registry = new RuntimeProviderRegistry();
        var service = new RuntimeProviderAbstractionFrameworkService(registry);
        var registration = service.CreateDefaultRegistration(planning.ExecutionProviderState!.ProviderDefinition!, planning.ExecutionProviderState.ProviderRequest!);
        registry.Register(registration);

        var ready = service.CreateRuntimeCandidate(planning, registration.ProviderId);
        var candidate = service.EvaluateReadiness(
            planning,
            registration,
            new RuntimeProviderValidationResult(RuntimeProviderValidationDiagnostics.Empty),
            ready.Request! with
            {
                ApprovalState = ready.Request!.ApprovalState with
                {
                    GenerationApproved = false
                }
            },
            ready.Context!);
        var unsupported = service.CreateRuntimeCandidate(planning, "runtime-provider:missing");
        var invalid = service.EvaluateReadiness(
            planning,
            null,
            new RuntimeProviderValidationResult(
                new RuntimeProviderValidationDiagnostics(
                    MissingRequiredSections: ["runtimeProviderRequest.executionConstraints"],
                    MissingRequiredFields: [],
                    InvalidReferences: [],
                    InvalidLineage: [],
                    InvalidApprovalState: [],
                    CapabilityResolutionFailures: [],
                    ExecutionConstraintFailures: [],
                    VersionMismatches: [])),
            ready.Request!,
            ready.Context!);
        var blocked = service.CreateRuntimeCandidate(new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage() with
        {
            ExperienceDefinition = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage().ExperienceDefinition with
            {
                ExperienceType = OpportunityExperienceType.FabricApp
            }
        }), registration.ProviderId);

        Assert.Equal(RuntimeProviderReadinessState.ReadyForRuntimeProvider, ready.Readiness);
        Assert.Equal(RuntimeProviderReadinessState.Candidate, candidate.ReadinessStatus);
        Assert.Equal(RuntimeProviderReadinessState.Unsupported, unsupported.Readiness);
        Assert.Equal(RuntimeProviderReadinessState.Invalid, invalid.ReadinessStatus);
        Assert.Equal(RuntimeProviderReadinessState.Blocked, blocked.Readiness);
    }

    [Fact(DisplayName = "Runtime Provider Registry registers providers, discovers them, and looks up capabilities without loading or invoking providers")]
    public void RuntimeProviderRegistry_RegistersDiscoversAndLooksUpCapabilities()
    {
        var registry = new RuntimeProviderRegistry();
        var first = new RuntimeProviderRegistration(
            ProviderId: "runtime-provider:microsoft-contract",
            ProviderName: "Microsoft Contract Runtime Provider",
            ProviderVersion: "1.0.0",
            ProviderCategory: MicrosoftAdapterSpecificationContract.ProviderCategory,
            ExecutionProviderRef: "execprov:microsoft.contract.execution-provider:execplan:test",
            SupportedRequestSchemaVersions: [RuntimeProviderRequestContract.SchemaVersionV1],
            SupportedContextSchemaVersions: [RuntimeProviderContextContract.SchemaVersionV1],
            SupportedResultSchemaVersions: [RuntimeProviderResultContract.SchemaVersionV1],
            SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile],
            SupportedCapabilities: ["layoutGeneration", "semanticGeneration"]);
        var second = first with
        {
            ProviderId = "runtime-provider:fabric-contract",
            ExecutionProviderRef = "execprov:fabric.contract.execution-provider:execplan:test",
            SupportedTargetProfiles = [GenerationRequestContract.FabricDataAppDefaultProfile],
            SupportedCapabilities = ["layoutGeneration", "deploymentPlanning"]
        };

        registry.Register(first);
        registry.Register(second);

        Assert.True(registry.TryGetProvider("runtime-provider:microsoft-contract", out var resolved));
        Assert.NotNull(resolved);
        Assert.Equal(first.ExecutionProviderRef, resolved!.ExecutionProviderRef);
        Assert.Single(registry.DiscoverByCategory(MicrosoftAdapterSpecificationContract.ProviderCategory, GenerationRequestContract.PbirReportDefaultProfile));
        Assert.Equal(
            new[] { "runtime-provider:fabric-contract", "runtime-provider:microsoft-contract" },
            registry.FindProvidersByCapability("layoutGeneration").Select(provider => provider.ProviderId).OrderBy(id => id, StringComparer.Ordinal).ToArray());
    }

    [Fact(DisplayName = "Runtime Provider contracts expose the runtime interface and remain contract-only with no execution, provider invocation, artifact generation, or deployment surface")]
    public void RuntimeProviderBoundary_RemainsContractOnly()
    {
        var providerInterface = typeof(IRuntimeProvider);
        var methods = providerInterface.GetMethods().Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        Assert.Equal(
            new[]
            {
                "CanAcceptRequest",
                "CreateExecutionContext",
                "EvaluateExecutionReadiness",
                "ValidateRequest"
            },
            methods);

        var forbiddenTokens = new[] { "Execute", "Invoke", "Api", "Cli", "GenerateArtifact", "Deploy", "AnalyzerRunner", "MicrosoftSkill" };
        Type[] types =
        [
            typeof(IRuntimeProvider),
            typeof(RuntimeProviderAbstractionFrameworkService),
            typeof(RuntimeProviderValidator),
            typeof(RuntimeReadinessService),
            typeof(RuntimeProviderRegistry),
            typeof(RuntimeExecutionCandidate)
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

    [Fact(DisplayName = "Runtime Provider contract inventories cover request, context, result, and execution candidate field paths")]
    public void RuntimeProviderContracts_InventoryCoversRequiredFieldPaths()
    {
        var runtimeInventoryPaths = RuntimeProviderContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var runtimeModelPaths = EnumerateFieldPaths(typeof(RuntimeExecutionCandidate), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var requestInventoryPaths = RuntimeProviderRequestContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var requestModelPaths = EnumerateFieldPaths(typeof(RuntimeProviderRequest), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var contextInventoryPaths = RuntimeProviderContextContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var contextModelPaths = EnumerateFieldPaths(typeof(RuntimeProviderContext), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var resultInventoryPaths = RuntimeProviderResultContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var resultModelPaths = EnumerateFieldPaths(typeof(RuntimeProviderResult), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(runtimeModelPaths.ToHashSet(StringComparer.Ordinal), runtimeInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(requestModelPaths.ToHashSet(StringComparer.Ordinal), requestInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(contextModelPaths.ToHashSet(StringComparer.Ordinal), contextInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(resultModelPaths.ToHashSet(StringComparer.Ordinal), resultInventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static string SerializeState(RuntimeProviderFrameworkState state)
    {
        return string.Join("|",
            state.Request!.SchemaVersion,
            state.Request.RequestId,
            state.Context!.ContextId,
            state.Result!.ResultId,
            state.Result.Status,
            state.Readiness,
            state.ExecutionCandidate!.CandidateId,
            string.Join(",", state.Result.Reasons));
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

        return type.IsClass || type.IsValueType ? type : null;
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
