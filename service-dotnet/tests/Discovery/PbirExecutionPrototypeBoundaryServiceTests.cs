using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirExecutionPrototypeBoundaryServiceTests
{
    [Fact(DisplayName = "PBIR execution prototype boundary creates a deterministic dry-run execution request envelope for ready PBIR candidates")]
    public void CreatePrototypeBoundary_ValidPbirDryRun_CreatesDeterministicEnvelope()
    {
        var (planning, runtime) = CreateReadyPbirRuntimeState();
        var service = new PbirExecutionPrototypeBoundaryService();
        var options = PbirExecutionPrototypeOptions.DryRunDefault;

        var first = service.CreatePrototypeBoundary(planning, runtime, options);
        var second = service.CreatePrototypeBoundary(planning, runtime, options);

        Assert.True(first.SafetyGate.IsAllowed);
        Assert.True(first.AcceptsExecutionPrototype);
        Assert.NotNull(first.Request);
        Assert.NotNull(first.DryRunSummary);
        Assert.Null(first.MockResult);
        Assert.Equal(PbirExecutionPrototypeContract.SchemaVersionV1, first.SchemaVersion);
        Assert.Equal(PbirExecutionRequestContract.SchemaVersionV1, first.Request!.SchemaVersion);
        Assert.Equal(PbirExecutionMode.DryRun, first.Request.ExecutionMode);
        Assert.True(first.Request.DryRun);
        Assert.Equal(GenerationRequestContract.PbirReportDefaultProfile, first.Request.TargetProfile.TargetProfileId);
        Assert.Equal(MicrosoftRuntimeReadinessState.ReadyForMicrosoftRuntimeProvider, first.Request.MicrosoftRuntimeContextReference.RuntimeReadiness);
        Assert.Equal("dryRun", first.DryRunSummary!.SummaryKind);
        Assert.Equal(SerializeState(first), SerializeState(second));
    }

    [Fact(DisplayName = "PBIR execution safety gate rejects non-PBIR targets including Fabric App and Fabric Data App")]
    public void CreatePrototypeBoundary_NonPbirTargets_AreRejected()
    {
        var (planning, runtime) = CreateReadyPbirRuntimeState();
        var service = new PbirExecutionPrototypeBoundaryService();
        var fabricApp = runtime with
        {
            Request = runtime.Request! with
            {
                TargetProfile = runtime.Request!.TargetProfile with
                {
                    TargetProfileId = GenerationRequestContract.FabricAppDefaultProfile,
                    ArtifactType = "fabricApp"
                }
            }
        };
        var fabricDataApp = runtime with
        {
            Request = runtime.Request! with
            {
                TargetProfile = runtime.Request!.TargetProfile with
                {
                    TargetProfileId = GenerationRequestContract.FabricDataAppDefaultProfile,
                    ArtifactType = "fabricDataApp"
                }
            }
        };

        var fabricAppResult = service.CreatePrototypeBoundary(planning, fabricApp, PbirExecutionPrototypeOptions.DryRunDefault);
        var fabricDataAppResult = service.CreatePrototypeBoundary(planning, fabricDataApp, PbirExecutionPrototypeOptions.DryRunDefault);

        Assert.False(fabricAppResult.SafetyGate.IsAllowed);
        Assert.Contains(GenerationRequestContract.FabricAppDefaultProfile, fabricAppResult.SafetyGate.Reasons);
        Assert.False(fabricDataAppResult.SafetyGate.IsAllowed);
        Assert.Contains(GenerationRequestContract.FabricDataAppDefaultProfile, fabricDataAppResult.SafetyGate.Reasons);
    }

    [Fact(DisplayName = "PBIR execution safety gate rejects missing approvals and unsupported Microsoft runtime readiness")]
    public void CreatePrototypeBoundary_MissingApprovalsOrUnsupportedReadiness_AreRejected()
    {
        var (planning, runtime) = CreateReadyPbirRuntimeState();
        var service = new PbirExecutionPrototypeBoundaryService();
        var missingApproval = runtime with
        {
            Request = runtime.Request! with
            {
                ReviewRequirements = runtime.Request!.ReviewRequirements with
                {
                    GenerationApproved = false
                }
            }
        };
        var unsupportedReadiness = runtime with
        {
            Readiness = MicrosoftRuntimeReadinessState.Unsupported,
            AcceptsExecutionCandidate = false
        };

        var missingApprovalResult = service.CreatePrototypeBoundary(planning, missingApproval, PbirExecutionPrototypeOptions.DryRunDefault);
        var unsupportedReadinessResult = service.CreatePrototypeBoundary(planning, unsupportedReadiness, PbirExecutionPrototypeOptions.DryRunDefault);

        Assert.False(missingApprovalResult.SafetyGate.IsAllowed);
        Assert.Contains("generation approval", string.Join("|", missingApprovalResult.SafetyGate.Reasons), StringComparison.OrdinalIgnoreCase);
        Assert.False(unsupportedReadinessResult.SafetyGate.IsAllowed);
        Assert.Contains("readyForMicrosoftRuntimeProvider", string.Join("|", unsupportedReadinessResult.SafetyGate.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "PBIR dry-run summaries are deterministic across repeated evaluations")]
    public void CreatePrototypeBoundary_RepeatedDryRuns_ProduceIdenticalSummaries()
    {
        var (planning, runtime) = CreateReadyPbirRuntimeState();
        var service = new PbirExecutionPrototypeBoundaryService();

        var first = service.CreatePrototypeBoundary(planning, runtime, PbirExecutionPrototypeOptions.DryRunDefault);
        var second = service.CreatePrototypeBoundary(planning, runtime, PbirExecutionPrototypeOptions.DryRunDefault);

        Assert.NotNull(first.DryRunSummary);
        Assert.NotNull(second.DryRunSummary);
        Assert.Equal(first.DryRunSummary!.SummaryKind, second.DryRunSummary!.SummaryKind);
        Assert.Equal(first.DryRunSummary.PlannedPages, second.DryRunSummary.PlannedPages);
        Assert.Equal(first.DryRunSummary.PlannedVisuals, second.DryRunSummary.PlannedVisuals);
        Assert.Equal(first.DryRunSummary.PlannedSemanticBindings, second.DryRunSummary.PlannedSemanticBindings);
        Assert.Equal(first.DryRunSummary.Constraints, second.DryRunSummary.Constraints);
        Assert.Equal(first.DryRunSummary.Warnings, second.DryRunSummary.Warnings);
    }

    [Fact(DisplayName = "PBIR mocked execution is allowed from deterministic fixtures and generated artifact refs stay empty unless explicit fixture output paths are supplied")]
    public void CreatePrototypeBoundary_MockedExecution_PreservesArtifactSafety()
    {
        var (planning, runtime) = CreateReadyPbirRuntimeState();
        var service = new PbirExecutionPrototypeBoundaryService();

        var withoutOutputPaths = service.CreatePrototypeBoundary(
            planning,
            runtime,
            new PbirExecutionPrototypeOptions(
                ExecutionMode: PbirExecutionMode.MockedExecution,
                DryRun: false,
                AllowLiveProviderInvocation: false,
                AllowDeployment: false,
                MockFixtureId: "fixtures/pbir/mock-execution/basic",
                MockOutputPaths: []));
        var withOutputPaths = service.CreatePrototypeBoundary(
            planning,
            runtime,
            new PbirExecutionPrototypeOptions(
                ExecutionMode: PbirExecutionMode.MockedExecution,
                DryRun: false,
                AllowLiveProviderInvocation: false,
                AllowDeployment: false,
                MockFixtureId: "fixtures/pbir/mock-execution/basic",
                MockOutputPaths: ["/tmp/mock/report/report.json"]));

        Assert.True(withoutOutputPaths.SafetyGate.IsAllowed);
        Assert.NotNull(withoutOutputPaths.MockResult);
        Assert.Empty(withoutOutputPaths.MockResult!.GeneratedArtifactRefs);
        Assert.True(withOutputPaths.SafetyGate.IsAllowed);
        Assert.Single(withOutputPaths.MockResult!.GeneratedArtifactRefs);
        Assert.Equal("/tmp/mock/report/report.json", withOutputPaths.MockResult.GeneratedArtifactRefs[0]);
    }

    [Fact(DisplayName = "PBIR execution safety gate rejects live execution, deployment, unsupported providers, and non-dry-run requests that are not mocked execution")]
    public void CreatePrototypeBoundary_ProtectedModes_AreRejected()
    {
        var (planning, runtime) = CreateReadyPbirRuntimeState();
        var service = new PbirExecutionPrototypeBoundaryService();
        var unsupportedProvider = runtime with
        {
            Definition = runtime.Definition! with
            {
                ProviderCategory = "unsupported"
            }
        };

        var liveExecutionResult = service.CreatePrototypeBoundary(
            planning,
            runtime,
            new PbirExecutionPrototypeOptions(
                ExecutionMode: PbirExecutionMode.DryRun,
                DryRun: true,
                AllowLiveProviderInvocation: true,
                AllowDeployment: false,
                MockFixtureId: null,
                MockOutputPaths: []));
        var deploymentResult = service.CreatePrototypeBoundary(
            planning,
            runtime,
            new PbirExecutionPrototypeOptions(
                ExecutionMode: PbirExecutionMode.DryRun,
                DryRun: true,
                AllowLiveProviderInvocation: false,
                AllowDeployment: true,
                MockFixtureId: null,
                MockOutputPaths: []));
        var unsupportedProviderResult = service.CreatePrototypeBoundary(planning, unsupportedProvider, PbirExecutionPrototypeOptions.DryRunDefault);
        var invalidNonDryRunResult = service.CreatePrototypeBoundary(
            planning,
            runtime,
            new PbirExecutionPrototypeOptions(
                ExecutionMode: PbirExecutionMode.DryRun,
                DryRun: false,
                AllowLiveProviderInvocation: false,
                AllowDeployment: false,
                MockFixtureId: null,
                MockOutputPaths: []));

        Assert.False(liveExecutionResult.SafetyGate.IsAllowed);
        Assert.Contains("live provider invocation", string.Join("|", liveExecutionResult.SafetyGate.Reasons), StringComparison.OrdinalIgnoreCase);
        Assert.False(deploymentResult.SafetyGate.IsAllowed);
        Assert.Contains("deployment", string.Join("|", deploymentResult.SafetyGate.Reasons), StringComparison.OrdinalIgnoreCase);
        Assert.False(unsupportedProviderResult.SafetyGate.IsAllowed);
        Assert.Contains("unsupported provider", string.Join("|", unsupportedProviderResult.SafetyGate.Reasons), StringComparison.OrdinalIgnoreCase);
        Assert.False(invalidNonDryRunResult.SafetyGate.IsAllowed);
        Assert.Contains("mockedExecution", string.Join("|", invalidNonDryRunResult.SafetyGate.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "PBIR execution prototype boundary remains dry-run and mock only with no Microsoft API invocation, CLI invocation, live execution, or deployment surface")]
    public void PbirExecutionPrototypeBoundary_RemainsProtectedAndNonInvoking()
    {
        var forbiddenTokens = new[] { "Api", "Cli", "Invoke", "ExecuteLive", "Deploy", "RunSkill", "GenerateArtifact" };
        Type[] types =
        [
            typeof(PbirExecutionPrototypeBoundaryService),
            typeof(PbirExecutionSafetyGate),
            typeof(PbirExecutionPrototypeState),
            typeof(PbirExecutionRequestEnvelope),
            typeof(PbirMockExecutionResult)
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

    [Fact(DisplayName = "PBIR execution prototype contracts inventory the required field paths for prototype state, request envelope, and mock result")]
    public void PbirExecutionPrototypeContracts_InventoryCoversRequiredFieldPaths()
    {
        var prototypeInventoryPaths = PbirExecutionPrototypeContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var prototypeModelPaths = EnumerateFieldPaths(typeof(PbirExecutionPrototypeState), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var requestInventoryPaths = PbirExecutionRequestContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var requestModelPaths = EnumerateFieldPaths(typeof(PbirExecutionRequestEnvelope), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var mockInventoryPaths = PbirMockExecutionResultContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var mockModelPaths = EnumerateFieldPaths(typeof(PbirMockExecutionResult), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(prototypeModelPaths.ToHashSet(StringComparer.Ordinal), prototypeInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(requestModelPaths.ToHashSet(StringComparer.Ordinal), requestInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(mockModelPaths.ToHashSet(StringComparer.Ordinal), mockInventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static (PlanningOrchestrationResult Planning, MicrosoftRuntimeProviderFrameworkState Runtime) CreateReadyPbirRuntimeState()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var registry = new RuntimeProviderRegistry();
        var runtimeService = new MicrosoftRuntimeProviderContractFrameworkService(registry);
        var definition = runtimeService.CreateDefaultProviderDefinition();
        var registration = runtimeService.CreateDefaultRegistration(definition, planning);

        registry.Register(registration);

        return (planning, runtimeService.CreateMicrosoftRuntimeState(planning, registration.ProviderId));
    }

    private static string SerializeState(PbirExecutionPrototypeState state)
    {
        return string.Join("|",
            state.SchemaVersion,
            state.Request!.RequestId,
            state.SafetyGate.IsAllowed,
            state.Request.ExecutionMode,
            state.Request.DryRun,
            state.DryRunSummary!.SummaryKind,
            state.MockResult is null ? "no-mock" : string.Join(",", state.MockResult.GeneratedArtifactRefs));
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

        return type.Namespace == typeof(PbirExecutionPrototypeState).Namespace ? type : null;
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
