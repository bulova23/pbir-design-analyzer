using System.Collections;
using System.Reflection;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class GenerationManifestServiceTests
{
    [Fact(DisplayName = "Generation Manifest creates deterministic immutable provider-neutral handoff documents from upstream planning artifacts")]
    public void CreateManifestState_ValidInputs_BuildsDeterministicManifest()
    {
        var inputs = CreateReadyInputs();
        var createdUtc = DateTimeOffset.Parse("2026-06-25T10:30:00+00:00");
        var service = new GenerationManifestService();

        var first = service.CreateManifestState(
            inputs.Planning,
            inputs.SpecificationState,
            inputs.ProviderState,
            inputs.ExecutionPlanningState,
            inputs.RuntimeState,
            createdUtc);
        var second = service.CreateManifestState(
            inputs.Planning,
            inputs.SpecificationState,
            inputs.ProviderState,
            inputs.ExecutionPlanningState,
            inputs.RuntimeState,
            createdUtc);

        Assert.NotNull(first.Manifest);
        Assert.Equal(GenerationManifestReadinessState.ReadyForGenerator, first.Readiness);
        Assert.Equal(GenerationManifestContract.SchemaVersionV1, first.Manifest!.Metadata.SchemaVersion);
        Assert.Equal("generationManifest:planningOutcome:designPackage:executive-summary", first.Manifest.Metadata.ManifestId);
        Assert.Equal(createdUtc.UtcDateTime, first.Manifest.Metadata.CreatedUtc);
        Assert.Equal(inputs.Planning.Outcome.References.DesignPackageRef, first.Manifest.References.DesignPackageRef);
        Assert.Equal(inputs.Planning.Outcome.References.GenerationRequestRef, first.Manifest.References.GenerationRequestRef);
        Assert.Equal(inputs.Planning.Outcome.References.ExecutionPlanRef, first.Manifest.References.ExecutionPlanRef);
        Assert.Equal(inputs.Planning.Outcome.Metadata.OutcomeId, first.Manifest.References.PlanningOutcomeRef);
        Assert.Equal(inputs.RuntimeState.Request!.RequestId, first.Manifest.References.RuntimeProviderRef);
        Assert.Equal(inputs.ProviderState.Request!.Metadata.RequestId, first.Manifest.References.GenerationProviderRequestRef);
        Assert.Equal(inputs.ExecutionPlanningState.Plan!.Metadata.ExecutionPlanId, first.Manifest.References.GenerationProviderExecutionPlanRef);
        Assert.Equal(inputs.SpecificationState.Specification!.SpecificationId, first.Manifest.GenerationSpecification.PbirGenerationSpecificationRef);
        Assert.Equal(inputs.Planning.Outcome.ReadinessSummary.CapabilitySummary.RequiredCapabilities, first.Manifest.CapabilitySummary.NegotiatedCapabilities);
        Assert.Equal(inputs.ProviderState.Provider!.SupportedCapabilities, first.Manifest.CapabilitySummary.ProviderCapabilities);
        Assert.Equal(inputs.ProviderState.Provider!.ProviderId, first.Manifest.CapabilitySummary.SelectedProvider.ProviderId);
        Assert.Equal(inputs.RuntimeState.Context!.MicrosoftSkillSummary.RequiredSkillIds, first.Manifest.CapabilitySummary.SelectedSkills);
        Assert.True(first.Manifest.ExecutionConstraints.DryRunOnly);
        Assert.False(first.Manifest.ExecutionConstraints.DeploymentAllowed);
        Assert.False(first.Manifest.ExecutionConstraints.ProviderInvocationAllowed);
        Assert.False(first.Manifest.ExecutionConstraints.ApiInvocationAllowed);
        Assert.False(first.Manifest.ExecutionConstraints.CliInvocationAllowed);
        Assert.Contains(first.Manifest.Lineage.ImmutableReferences, reference => reference == first.Manifest.References.DesignPackageRef);
        Assert.Contains(first.Manifest.Lineage.ImmutableReferences, reference => reference == first.Manifest.References.GenerationProviderExecutionPlanRef);
        Assert.Equal(Serialize(first), Serialize(second));
    }

    [Fact(DisplayName = "Generation Manifest validation fails for missing references, invalid readiness consistency, schema mismatches, and lineage integrity drift")]
    public void Validate_InvalidManifest_FailsClosed()
    {
        var inputs = CreateReadyInputs();
        var createdUtc = DateTimeOffset.Parse("2026-06-25T10:30:00+00:00");
        var service = new GenerationManifestService();
        var validator = new GenerationManifestValidator();
        var baseline = service.CreateManifestState(
            inputs.Planning,
            inputs.SpecificationState,
            inputs.ProviderState,
            inputs.ExecutionPlanningState,
            inputs.RuntimeState,
            createdUtc);

        var invalidManifest = baseline.Manifest! with
        {
            Metadata = baseline.Manifest!.Metadata with
            {
                SchemaVersion = "generation-manifest/v2"
            },
            References = baseline.Manifest.References with
            {
                RuntimeProviderRef = string.Empty,
                PlanningOutcomeRef = "planningOutcome:different"
            },
            ApprovalSummary = baseline.Manifest.ApprovalSummary with
            {
                RuntimeReadiness = MicrosoftRuntimeReadinessState.Blocked,
                GenerationReadiness = GenerationProviderExecutionPlanReadinessState.Blocked
            },
            Lineage = baseline.Manifest.Lineage with
            {
                ImmutableReferences = baseline.Manifest.Lineage.ImmutableReferences
                    .Where(reference => !string.Equals(reference, baseline.Manifest.References.PlanningOutcomeRef, StringComparison.Ordinal))
                    .ToArray()
            }
        };

        var validation = validator.Validate(
            invalidManifest,
            inputs.Planning,
            inputs.SpecificationState,
            inputs.ProviderState,
            inputs.ExecutionPlanningState,
            inputs.RuntimeState);

        Assert.False(validation.IsValid);
        Assert.Contains("references.runtimeProviderRef", validation.Diagnostics.MissingRequiredFields);
        Assert.Contains("generation-manifest/v2", validation.Diagnostics.UnsupportedSchemaVersions);
        Assert.Contains("references.planningOutcomeRef must match planningOutcome.metadata.outcomeId.", validation.Diagnostics.InvalidReferences);
        Assert.Contains("approvalSummary.runtimeReadiness must match microsoftRuntimeProvider.readiness.", validation.Diagnostics.ReadinessConsistencyFailures);
        Assert.Contains("lineage.immutableReferences must contain every required upstream reference.", validation.Diagnostics.LineageIntegrityFailures);
    }

    [Fact(DisplayName = "Generation Manifest readiness distinguishes incomplete, blocked, and readyForGenerator states deterministically")]
    public void ReadinessService_EvaluatesEveryStateCorrectly()
    {
        var readiness = new GenerationManifestReadinessService();
        var incompleteValidation = new GenerationManifestValidationResult(
            new GenerationManifestValidationDiagnostics(
                MissingRequiredSections: ["references"],
                MissingRequiredFields: [],
                InvalidReferences: [],
                UnsupportedSchemaVersions: [],
                LineageIntegrityFailures: [],
                ReadinessConsistencyFailures: [],
                ProviderCompatibilityFailures: [],
                BoundaryViolations: []));
        var blockedValidation = new GenerationManifestValidationResult(
            new GenerationManifestValidationDiagnostics(
                MissingRequiredSections: [],
                MissingRequiredFields: [],
                InvalidReferences: [],
                UnsupportedSchemaVersions: [],
                LineageIntegrityFailures: [],
                ReadinessConsistencyFailures: ["approvalSummary.generationReadiness must match generationProviderExecutionPlan.readiness."],
                ProviderCompatibilityFailures: [],
                BoundaryViolations: []));
        var readyValidation = new GenerationManifestValidationResult(GenerationManifestValidationDiagnostics.Empty);

        Assert.Equal(GenerationManifestReadinessState.Incomplete, readiness.Evaluate(incompleteValidation));
        Assert.Equal(GenerationManifestReadinessState.Blocked, readiness.Evaluate(blockedValidation));
        Assert.Equal(GenerationManifestReadinessState.ReadyForGenerator, readiness.Evaluate(readyValidation));
    }

    [Fact(DisplayName = "Generation Manifest remains metadata-only with no generation, provider invocation, API invocation, CLI invocation, or deployment surface")]
    public void GenerationManifestBoundary_RemainsMetadataOnly()
    {
        var forbiddenTokens = new[] { "GeneratePbir", "InvokeProvider", "InvokeApi", "InvokeCli", "Deploy", "RunSkill", "Execute", "Publish" };
        Type[] types =
        [
            typeof(GenerationManifestService),
            typeof(GenerationManifestValidator),
            typeof(GenerationManifestReadinessService),
            typeof(GenerationManifest),
            typeof(GenerationManifestState)
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

    [Fact(DisplayName = "Generation Manifest contract inventory covers required metadata, references, specification, capability, approval, constraints, and lineage field paths")]
    public void GenerationManifestContracts_InventoryCoversRequiredFieldPaths()
    {
        var inventoryPaths = GenerationManifestContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var modelPaths = EnumerateFieldPaths(typeof(GenerationManifest), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(modelPaths.ToHashSet(StringComparer.Ordinal), inventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static string Serialize(GenerationManifestState state)
    {
        return JsonSerializer.Serialize(state);
    }

    private static (
        PlanningOrchestrationResult Planning,
        PbirGenerationSpecificationState SpecificationState,
        GenerationProviderFrameworkState ProviderState,
        GenerationProviderExecutionPlanningState ExecutionPlanningState,
        MicrosoftRuntimeProviderFrameworkState RuntimeState) CreateReadyInputs()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var specificationState = new PbirGenerationSpecificationService().PrepareForGenerationProvider(
            new PbirGenerationSpecificationService().CreateSpecification(planning));
        var providerState = new GenerationProviderFrameworkService().CreateProviderState(specificationState);
        var executionPlanningState = new GenerationProviderExecutionPlanningService().CreatePlanState(
            providerState.Request!,
            providerState.Provider!,
            specificationState,
            planning.Outcome);
        var runtimeRegistry = new RuntimeProviderRegistry();
        var runtimeService = new MicrosoftRuntimeProviderContractFrameworkService(runtimeRegistry);
        var runtimeDefinition = runtimeService.CreateDefaultProviderDefinition();
        runtimeRegistry.Register(runtimeService.CreateDefaultRegistration(runtimeDefinition, planning));
        var runtimeState = runtimeService.CreateMicrosoftRuntimeState(planning, runtimeDefinition.ProviderId);

        return (planning, specificationState, providerState, executionPlanningState, runtimeState);
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
            type == typeof(double) ||
            type == typeof(DateTime);
    }
}
