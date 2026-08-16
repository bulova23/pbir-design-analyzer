using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;
using System.Collections;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PlanningOrchestrationServiceTests
{
    [Fact(DisplayName = "Planning Orchestration composes the end-to-end planning stack deterministically and produces a planning outcome approved for a future execution provider")]
    public void Orchestrate_ValidInputs_ProducesDeterministicPlanningOutcome()
    {
        var service = new PlanningOrchestrationService();
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage();

        var first = service.Orchestrate(package);
        var second = service.Orchestrate(package);

        Assert.NotNull(first.Outcome);
        Assert.Equal(PlanningOutcomeContract.SchemaVersionV1, first.Outcome.Metadata.SchemaVersion);
        Assert.Equal("planningOutcome:designPackage:executive-summary", first.Outcome.Metadata.OutcomeId);
        Assert.Equal(PlanningOutcomeStatus.ApprovedForExecutionProvider, first.Outcome.Status);
        Assert.Equal(PlanningReadinessStatus.ApprovedForExecutionProvider, first.Outcome.ReadinessSummary.Status);
        Assert.Equal("designPackage:executive-summary", first.Outcome.References.DesignPackageRef);
        Assert.Equal("genreq:pbirReport:designPackage:executive-summary", first.Outcome.References.GenerationRequestRef);
        Assert.Equal("execplan:pbirReport:genreq:pbirReport:designPackage:executive-summary", first.Outcome.References.ExecutionPlanRef);
        Assert.Equal("capneg:pbirReport/default:execplan:pbirReport:genreq:pbirReport:designPackage:executive-summary", first.Outcome.References.NegotiationRef);
        Assert.Equal("execprov:microsoft.contract.execution-provider:execplan:pbirReport:genreq:pbirReport:designPackage:executive-summary", first.Outcome.References.ExecutionProviderRef);
        Assert.Equal(ExecutionProviderReadinessState.ApprovedForExecutionProvider, first.Outcome.ReadinessSummary.ExecutionProviderReadiness);
        Assert.NotNull(first.MicrosoftSkillProviderState);
        Assert.True(first.Outcome.ReadinessSummary.ApprovalStatus.DesignApproved);
        Assert.True(first.Outcome.ReadinessSummary.ApprovalStatus.GenerationApproved);
        Assert.Contains(first.Outcome.Lineage.UpstreamLineage, entry => entry.ReferenceId == "designPackage:executive-summary");
        Assert.Contains(first.Outcome.Lineage.PlanningLineage, entry => entry.Stage == "microsoftSkillProviderSelection");
        Assert.Contains(first.Outcome.Lineage.PlanningLineage, entry => entry.ReferenceId == first.Outcome.References.ExecutionProviderRef);
        Assert.Empty(first.Outcome.Failures);
        Assert.Equal(SerializeOutcome(first.Outcome), SerializeOutcome(second.Outcome));
        Assert.Equal(SerializeOrchestration(first.OrchestrationState), SerializeOrchestration(second.OrchestrationState));
    }

    [Fact(DisplayName = "Planning Orchestration blocks unsupported Fabric App targets without introducing execution behavior")]
    public void Orchestrate_UnsupportedTarget_ProducesBlockedPlanningOutcome()
    {
        var service = new PlanningOrchestrationService();
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage() with
        {
            ExperienceDefinition = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage().ExperienceDefinition with
            {
                ExperienceType = OpportunityExperienceType.FabricApp
            }
        };

        var result = service.Orchestrate(package);

        Assert.Equal(PlanningOutcomeStatus.PlanningBlocked, result.Outcome.Status);
        Assert.Equal(PlanningReadinessStatus.Blocked, result.Outcome.ReadinessSummary.Status);
        Assert.Contains(result.Outcome.Failures, failure => failure.FailureType == PlanningFailureType.UnsupportedTarget);
        Assert.Contains("FabricApp", result.Outcome.ReadinessSummary.BlockingIssues);
    }

    [Fact(DisplayName = "Planning Orchestration blocks capability gaps for future-facing Fabric Data App planning deterministically")]
    public void Orchestrate_BlockedCapability_ProducesBlockedPlanningOutcome()
    {
        var service = new PlanningOrchestrationService();
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage() with
        {
            ExperienceDefinition = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage().ExperienceDefinition with
            {
                ExperienceType = OpportunityExperienceType.FabricDataApp
            }
        };

        var result = service.Orchestrate(package);

        Assert.Equal(PlanningOutcomeStatus.PlanningBlocked, result.Outcome.Status);
        Assert.Contains(result.Outcome.Failures, failure => failure.FailureType == PlanningFailureType.BlockedCapability);
        Assert.Contains("deploymentSupport", result.Outcome.ReadinessSummary.UnresolvedRequirements);
    }

    [Fact(DisplayName = "Planning Orchestration transition validation rejects invalid stage jumps and missing predecessor outputs")]
    public void ValidateTransition_InvalidStageJump_FailsClosed()
    {
        var service = new PlanningOrchestrationService();
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage();
        var generationRequest = new GenerationRequestFrameworkService()
            .PrepareForProviderPlanning(
                new GenerationRequestFrameworkService().CreateDraft(
                    new DesignPackageConsumptionService().Consume(package)))
            .Request!;

        var validation = service.ValidateTransition(
            PlanningStage.GenerationRequest,
            PlanningStage.CapabilityNegotiation,
            new PlanningTransitionContext(
                DesignPackage: package,
                GenerationRequest: generationRequest,
                ExecutionPlan: null,
                ProviderAdapterState: null,
                MicrosoftPlanningState: null,
                CapabilityNegotiationResult: null,
                MicrosoftSkillState: null,
                MicrosoftSkillProviderState: null,
                ExecutionProviderState: null));

        Assert.False(validation.IsValid);
        Assert.Contains("generationRequest -> capabilityNegotiation", validation.InvalidTransitions);
        Assert.Contains("executionPlan", validation.MissingDependencies);
    }

    [Fact(DisplayName = "Planning Orchestration transition validation fails on reference integrity mismatches and invalid contract versions")]
    public void ValidateTransition_InvalidReferencesAndVersions_FailClosed()
    {
        var service = new PlanningOrchestrationService();
        var package = GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage();
        var generationRequest = new GenerationRequestFrameworkService()
            .PrepareForProviderPlanning(
                new GenerationRequestFrameworkService().CreateDraft(
                    new DesignPackageConsumptionService().Consume(package)))
            .Request!;
        var executionPlan = new ExecutionPlanFrameworkService()
            .PrepareForProviderAdapter(new ExecutionPlanFrameworkService().CreateDraft(generationRequest))
            .Plan! with
            {
                SourceReferences = new ExecutionPlanSourceReferences(
                    GenerationRequestRef: "genreq:different",
                    SourceDesignPackageRef: package.PackageId)
            };
        generationRequest = generationRequest with
        {
            SchemaVersion = "generation-request/v2"
        };
        executionPlan = executionPlan with
        {
                SourceReferences = new ExecutionPlanSourceReferences(
                    GenerationRequestRef: "genreq:different",
                    SourceDesignPackageRef: package.PackageId)
        };

        var validation = service.ValidateTransition(
            PlanningStage.GenerationRequest,
            PlanningStage.ExecutionPlan,
            new PlanningTransitionContext(
                DesignPackage: package,
                GenerationRequest: generationRequest,
                ExecutionPlan: executionPlan,
                ProviderAdapterState: null,
                MicrosoftPlanningState: null,
                CapabilityNegotiationResult: null,
                MicrosoftSkillState: null,
                MicrosoftSkillProviderState: null,
                ExecutionProviderState: null));

        Assert.False(validation.IsValid);
        Assert.Contains("generation-request/v2", validation.VersionMismatches);
        Assert.Contains("executionPlan.sourceReferences.generationRequestRef must match generationRequest.requestId.", validation.InvalidReferences);
    }

    [Fact(DisplayName = "Planning Orchestration remains planning-only and exposes no execution, provider invocation, artifact generation, deployment, or analyzer automation surface")]
    public void PlanningOrchestrationBoundary_RemainsPlanningOnly()
    {
        var forbiddenTokens = new[] { "Execute", "Invoke", "Api", "Cli", "GenerateArtifact", "Deploy", "AnalyzerRunner", "MicrosoftSkill" };
        Type[] types =
        [
            typeof(PlanningOrchestrationService),
            typeof(PlanningReadinessAggregator),
            typeof(PlanningOutcome),
            typeof(PlanningOrchestrationState),
            typeof(PlanningTransitionValidationResult)
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

    [Fact(DisplayName = "Planning Orchestration and Planning Outcome contract inventories cover the required field paths for lifecycle, references, readiness, and lineage")]
    public void PlanningContracts_InventoryCoversRequiredFieldPaths()
    {
        var outcomeInventoryPaths = PlanningOutcomeContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var outcomeModelPaths = EnumerateFieldPaths(typeof(PlanningOutcome), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var orchestrationInventoryPaths = PlanningOrchestrationContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var orchestrationModelPaths = EnumerateFieldPaths(typeof(PlanningOrchestrationState), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(outcomeModelPaths.ToHashSet(StringComparer.Ordinal), outcomeInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(orchestrationModelPaths.ToHashSet(StringComparer.Ordinal), orchestrationInventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static string SerializeOutcome(PlanningOutcome outcome)
    {
        return string.Join("|",
            outcome.Metadata.SchemaVersion,
            outcome.Metadata.OutcomeId,
            outcome.References.DesignPackageRef,
            outcome.References.GenerationRequestRef,
            outcome.References.ExecutionPlanRef,
            outcome.References.NegotiationRef,
            outcome.References.ExecutionProviderRef,
            outcome.Status,
            outcome.ReadinessSummary.Status,
            string.Join(",", outcome.ReadinessSummary.BlockingIssues),
            string.Join(",", outcome.ReadinessSummary.UnresolvedRequirements),
            string.Join(",", outcome.ReadinessSummary.CapabilitySummary.RequiredCapabilities),
            string.Join(",", outcome.ReadinessSummary.CapabilitySummary.ResolvedCapabilities),
            string.Join(",", outcome.ReadinessSummary.CapabilitySummary.UnresolvedCapabilities),
            outcome.ReadinessSummary.ApprovalStatus.DesignApproved,
            outcome.ReadinessSummary.ApprovalStatus.GenerationApproved,
            outcome.ReadinessSummary.ExecutionProviderReadiness,
            string.Join(",", outcome.Failures.Select(failure => $"{failure.FailureType}:{failure.Stage}:{failure.Message}")));
    }

    private static string SerializeOrchestration(PlanningOrchestrationState state)
    {
        return string.Join("|",
            state.SchemaVersion,
            state.OrchestrationId,
            state.CurrentStage,
            string.Join(",", state.StageHistory.Select(history => $"{history.Stage}:{history.Status}:{history.ReferenceId}")),
            string.Join(",", state.TransitionHistory.Select(transition => $"{transition.FromStage}>{transition.ToStage}:{transition.RuleVersion}")));
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

        return type.Namespace == typeof(PlanningOutcome).Namespace ? type : null;
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
