using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class GenerationProviderExecutionPlanningServiceTests
{
    [Fact(DisplayName = "Generation Provider Execution Planning creates deterministic provider-neutral execution plans from generation-provider requests")]
    public void CreatePlanState_ValidInputs_BuildsDeterministicExecutionPlan()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var specificationState = new PbirGenerationSpecificationService().PrepareForGenerationProvider(
            new PbirGenerationSpecificationService().CreateSpecification(planning));
        var providerState = new GenerationProviderFrameworkService().CreateProviderState(specificationState);
        var service = new GenerationProviderExecutionPlanningService();

        var first = service.CreatePlanState(providerState.Request!, providerState.Provider!, specificationState, planning.Outcome);
        var second = service.CreatePlanState(providerState.Request!, providerState.Provider!, specificationState, planning.Outcome);

        Assert.NotNull(first.Plan);
        Assert.Equal(GenerationProviderExecutionPlanReadinessState.ReadyForExecutionProvider, first.Readiness);
        Assert.Equal(GenerationProviderExecutionPlanContract.SchemaVersionV1, first.Plan!.Metadata.SchemaVersion);
        Assert.Equal(
            "generationProviderExecutionPlan:generationProviderRequest:pbirGenerationSpecification:planningOutcome:designPackage:executive-summary",
            first.Plan.Metadata.ExecutionPlanId);
        Assert.Equal(providerState.Request!.Metadata.RequestId, first.Plan.References.GenerationProviderRequestRef);
        Assert.Equal(specificationState.Specification!.SpecificationId, first.Plan.References.PbirGenerationSpecificationRef);
        Assert.Equal(planning.Outcome.Metadata.OutcomeId, first.Plan.References.PlanningOutcomeRef);
        Assert.Equal(
            new[]
            {
                "specificationValidation",
                "providerCapabilityValidation",
                "executionPreparation",
                "providerHandoffPreparation",
            },
            first.Plan.ExecutionStages.Select(stage => stage.StageId).ToArray());
        Assert.True(first.Plan.ExecutionConstraints.DryRunOnly);
        Assert.True(first.Plan.ExecutionConstraints.MockExecutionPermitted);
        Assert.True(first.Plan.ExecutionConstraints.ProviderInvocationProhibited);
        Assert.True(first.Plan.ExecutionConstraints.DeploymentProhibited);
        Assert.Equal(GenerationProviderReadinessState.ReadyForGenerationProvider, first.Plan.ExecutionDependencies.ProviderReadiness.CurrentReadiness);
        Assert.Equal(PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider, first.Plan.ExecutionDependencies.SpecificationCompleteness.CurrentReadiness);
        Assert.Equal(PlanningReadinessStatus.ApprovedForExecutionProvider, first.Plan.ExecutionDependencies.RuntimeReadiness.CurrentReadiness);
        Assert.Equal(SerializeState(first), SerializeState(second));
    }

    [Fact(DisplayName = "Generation Provider Execution Plan validation fails closed for invalid references, invalid stage ordering, incompatible providers, and incomplete specifications")]
    public void Validate_InvalidPlans_FailClosed()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var specificationState = new PbirGenerationSpecificationService().PrepareForGenerationProvider(
            new PbirGenerationSpecificationService().CreateSpecification(planning));
        var providerState = new GenerationProviderFrameworkService().CreateProviderState(specificationState);
        var service = new GenerationProviderExecutionPlanningService();
        var validator = new GenerationProviderExecutionPlanValidator();

        var baseline = service.CreatePlanState(providerState.Request!, providerState.Provider!, specificationState, planning.Outcome);
        var invalidPlan = baseline.Plan! with
        {
            Metadata = baseline.Plan!.Metadata with
            {
                SchemaVersion = "generation-provider-execution-plan/v2"
            },
            References = baseline.Plan.References with
            {
                PlanningOutcomeRef = "planningOutcome:different"
            },
            ExecutionStages =
            [
                baseline.Plan.ExecutionStages[2],
                baseline.Plan.ExecutionStages[0],
                baseline.Plan.ExecutionStages[1],
                baseline.Plan.ExecutionStages[3]
            ]
        };
        var incompleteSpecification = specificationState with
        {
            Specification = specificationState.Specification! with
            {
                ArtifactSpecifications =
                [
                    specificationState.Specification!.ArtifactSpecifications[0] with
                    {
                        PageSpecifications = []
                    }
                ]
            }
        };
        var incompatibleProvider = providerState.Provider! with
        {
            SupportedCapabilities = ["pageGeneration"],
            Status = GenerationProviderStatus.Unsupported
        };

        var validation = validator.Validate(
            invalidPlan,
            providerState.Request!,
            incompatibleProvider,
            incompleteSpecification,
            planning.Outcome);

        Assert.False(validation.IsValid);
        Assert.Contains("generation-provider-execution-plan/v2", validation.Diagnostics.UnsupportedSchemaVersions);
        Assert.Contains("references.planningOutcomeRef must match planningOutcome.metadata.outcomeId.", validation.Diagnostics.InvalidReferences);
        Assert.Contains("executionStages must remain in deterministic provider-neutral order.", validation.Diagnostics.StageOrderingFailures);
        Assert.Contains("provider.status must remain compatible with execution planning.", validation.Diagnostics.ProviderCompatibilityFailures);
        Assert.Contains("artifactSpecifications.pageSpecifications", validation.Diagnostics.ReadinessCompatibilityFailures);
    }

    [Fact(DisplayName = "Generation Provider Execution readiness distinguishes blocked, partiallyPrepared, prepared, and readyForExecutionProvider states deterministically")]
    public void ReadinessService_EvaluatesEveryStateCorrectly()
    {
        var readiness = new GenerationProviderExecutionReadinessService();
        var blockedValidation = new GenerationProviderExecutionPlanValidationResult(
            new GenerationProviderExecutionPlanValidationDiagnostics(
                MissingRequiredSections: ["executionStages"],
                MissingRequiredFields: [],
                InvalidReferences: [],
                StageOrderingFailures: [],
                ReadinessCompatibilityFailures: [],
                ProviderCompatibilityFailures: [],
                UnsupportedSchemaVersions: [],
                BoundaryViolations: []));
        var partialValidation = new GenerationProviderExecutionPlanValidationResult(
            new GenerationProviderExecutionPlanValidationDiagnostics(
                MissingRequiredSections: [],
                MissingRequiredFields: [],
                InvalidReferences: [],
                StageOrderingFailures: [],
                ReadinessCompatibilityFailures: ["specification is not ready for generation provider planning."],
                ProviderCompatibilityFailures: [],
                UnsupportedSchemaVersions: [],
                BoundaryViolations: []));
        var preparedValidation = new GenerationProviderExecutionPlanValidationResult(
            GenerationProviderExecutionPlanValidationDiagnostics.Empty);
        var plan = CreateSyntheticPlan(
            providerReadiness: GenerationProviderReadinessState.ReadyForGenerationProvider,
            specificationReadiness: PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider,
            runtimeReadiness: PlanningReadinessStatus.ApprovedForExecutionProvider,
            approvalsSatisfied: true);

        Assert.Equal(GenerationProviderExecutionPlanReadinessState.Blocked, readiness.Evaluate(blockedValidation));
        Assert.Equal(GenerationProviderExecutionPlanReadinessState.PartiallyPrepared, readiness.Evaluate(partialValidation));
        Assert.Equal(GenerationProviderExecutionPlanReadinessState.Prepared, readiness.Evaluate(preparedValidation));
        Assert.Equal(
            GenerationProviderExecutionPlanReadinessState.ReadyForExecutionProvider,
            readiness.PrepareForExecutionProvider(GenerationProviderExecutionPlanReadinessState.Prepared, plan));
    }

    [Fact(DisplayName = "Generation Provider Execution Planning remains provider-neutral with no PBIR generation, provider invocation, API invocation, CLI invocation, or deployment surface")]
    public void ExecutionPlanningBoundary_RemainsPlanningOnly()
    {
        var forbiddenTokens = new[] { "GeneratePbir", "InvokeProvider", "InvokeApi", "InvokeCli", "Deploy", "RunSkill", "Execute", "Publish" };
        Type[] types =
        [
            typeof(GenerationProviderExecutionPlanningService),
            typeof(GenerationProviderExecutionPlanValidator),
            typeof(GenerationProviderExecutionReadinessService),
            typeof(GenerationProviderExecutionPlan),
            typeof(GenerationProviderExecutionPlanningState)
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

    [Fact(DisplayName = "Generation Provider Execution Plan contract inventory covers the required field paths for metadata, references, stages, constraints, and dependencies")]
    public void ExecutionPlanContracts_InventoryCoversRequiredFieldPaths()
    {
        var inventoryPaths = GenerationProviderExecutionPlanContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var modelPaths = EnumerateFieldPaths(typeof(GenerationProviderExecutionPlan), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(modelPaths.ToHashSet(StringComparer.Ordinal), inventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static GenerationProviderExecutionPlan CreateSyntheticPlan(
        GenerationProviderReadinessState providerReadiness,
        PbirGenerationSpecificationReadinessState specificationReadiness,
        PlanningReadinessStatus runtimeReadiness,
        bool approvalsSatisfied)
    {
        return new GenerationProviderExecutionPlan(
            Metadata: new GenerationProviderExecutionPlanMetadata(
                ExecutionPlanId: "generationProviderExecutionPlan:test",
                SchemaVersion: GenerationProviderExecutionPlanContract.SchemaVersionV1),
            References: new GenerationProviderExecutionPlanReferences(
                GenerationProviderRequestRef: "generationProviderRequest:test",
                PbirGenerationSpecificationRef: "pbirGenerationSpecification:test",
                PlanningOutcomeRef: "planningOutcome:test"),
            ExecutionStages:
            [
                new GenerationProviderExecutionStage("specificationValidation", "Specification Validation", 1, ["specificationCompleteness"]),
                new GenerationProviderExecutionStage("providerCapabilityValidation", "Provider Capability Validation", 2, ["providerReadiness"]),
                new GenerationProviderExecutionStage("executionPreparation", "Execution Preparation", 3, ["runtimeReadiness"]),
                new GenerationProviderExecutionStage("providerHandoffPreparation", "Provider Handoff Preparation", 4, ["requiredApprovals"])
            ],
            ExecutionConstraints: new GenerationProviderExecutionConstraints(
                DryRunOnly: true,
                MockExecutionPermitted: true,
                DeploymentProhibited: true,
                ProviderInvocationProhibited: true,
                ApiInvocationProhibited: true,
                CliInvocationProhibited: true,
                ReportMutationProhibited: true),
            ExecutionDependencies: new GenerationProviderExecutionDependencies(
                RequiredApprovals: new GenerationProviderExecutionApprovalDependencies(
                    DesignApprovalRequired: true,
                    GenerationApprovalRequired: true,
                    AnalyzerReviewRequired: true,
                    DesignApproved: approvalsSatisfied,
                    GenerationApproved: approvalsSatisfied),
                ProviderReadiness: new GenerationProviderExecutionProviderDependency(
                    CurrentReadiness: providerReadiness,
                    RequiredReadiness: GenerationProviderReadinessState.ReadyForGenerationProvider),
                RuntimeReadiness: new GenerationProviderExecutionRuntimeDependency(
                    CurrentReadiness: runtimeReadiness,
                    RequiredReadiness: PlanningReadinessStatus.ApprovedForExecutionProvider),
                SpecificationCompleteness: new GenerationProviderExecutionSpecificationDependency(
                    CurrentReadiness: specificationReadiness,
                    RequiredReadiness: PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider)));
    }

    private static string SerializeState(GenerationProviderExecutionPlanningState state)
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
