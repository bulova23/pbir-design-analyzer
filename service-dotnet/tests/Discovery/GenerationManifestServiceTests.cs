using System.Collections;
using System.Reflection;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class GenerationManifestServiceTests
{
    [Fact(DisplayName = "Generation Manifest creates deterministic immutable provider-neutral execution packages from the complete upstream planning pipeline")]
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
            inputs.RuntimeProviderState,
            inputs.MicrosoftRuntimeState,
            createdUtc);
        var second = service.CreateManifestState(
            inputs.Planning,
            inputs.SpecificationState,
            inputs.ProviderState,
            inputs.ExecutionPlanningState,
            inputs.RuntimeProviderState,
            inputs.MicrosoftRuntimeState,
            createdUtc);

        Assert.NotNull(first.Manifest);
        Assert.Equal(GenerationManifestReadinessState.ReadyForGenerator, first.Readiness);
        Assert.Equal(GenerationManifestContract.SchemaVersionV1, first.Manifest!.Metadata.SchemaVersion);
        Assert.Equal("generationManifest:planningOutcome:designPackage:executive-summary", first.Manifest.Metadata.ManifestId);
        Assert.Equal(createdUtc.UtcDateTime, first.Manifest.Metadata.CreatedUtc);

        Assert.Equal(inputs.Planning.Outcome.References.DesignPackageRef, first.Manifest.SourceReferences.DesignPackageRef);
        Assert.Equal(inputs.Planning.Outcome.References.GenerationRequestRef, first.Manifest.SourceReferences.GenerationRequestRef);
        Assert.Equal(inputs.Planning.Outcome.References.ExecutionPlanRef, first.Manifest.SourceReferences.ExecutionPlanRef);
        Assert.Equal(inputs.Planning.Outcome.Metadata.OutcomeId, first.Manifest.SourceReferences.PlanningOutcomeRef);
        Assert.Equal(inputs.RuntimeProviderState.Request!.RequestId, first.Manifest.SourceReferences.RuntimeProviderRef);
        Assert.Equal(inputs.ProviderState.Request!.Metadata.RequestId, first.Manifest.SourceReferences.GenerationProviderRequestRef);
        Assert.Equal(inputs.ExecutionPlanningState.Plan!.Metadata.ExecutionPlanId, first.Manifest.SourceReferences.GenerationProviderExecutionPlanRef);
        Assert.Equal(inputs.SpecificationState.Specification!.SpecificationId, first.Manifest.SourceReferences.PbirGenerationSpecificationRef);

        Assert.Equal(inputs.Planning.Outcome.ReadinessSummary.CapabilitySummary.RequiredCapabilities, first.Manifest.CapabilitySummary.NegotiatedCapabilities);
        Assert.Equal(inputs.ProviderState.Provider!.ProviderId, first.Manifest.CapabilitySummary.SelectedGenerationProvider.ProviderId);
        Assert.Equal(inputs.ProviderState.Provider.ProviderName, first.Manifest.CapabilitySummary.SelectedGenerationProvider.ProviderName);
        Assert.Equal(inputs.ProviderState.Provider.ProviderVersion, first.Manifest.CapabilitySummary.SelectedGenerationProvider.ProviderVersion);
        Assert.Equal(inputs.MicrosoftRuntimeState.Definition!.ProviderId, first.Manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderId);
        Assert.Equal(inputs.MicrosoftRuntimeState.Definition.ProviderName, first.Manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderName);
        Assert.Equal(inputs.MicrosoftRuntimeState.Definition.ProviderVersion, first.Manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderVersion);
        Assert.Equal(inputs.MicrosoftRuntimeState.Context!.MicrosoftSkillSummary.RequiredSkillIds, first.Manifest.CapabilitySummary.SelectedSkills);
        Assert.Equal(inputs.MicrosoftRuntimeState.Context.MicrosoftSkillSummary.CandidateProviderIds, first.Manifest.CapabilitySummary.SelectedProviderCandidates);

        Assert.True(first.Manifest.ExecutionConstraints.DryRunOnly);
        Assert.False(first.Manifest.ExecutionConstraints.DeploymentAllowed);
        Assert.False(first.Manifest.ExecutionConstraints.ProviderInvocationAllowed);
        Assert.False(first.Manifest.ExecutionConstraints.ApiInvocationAllowed);
        Assert.False(first.Manifest.ExecutionConstraints.CliInvocationAllowed);

        Assert.Equal(inputs.Planning.Outcome.ReadinessSummary.Status, first.Manifest.ReadinessSummary.PlanningReadiness);
        Assert.Equal(inputs.RuntimeProviderState.Readiness, first.Manifest.ReadinessSummary.RuntimeReadiness);
        Assert.Equal(inputs.ProviderState.Readiness, first.Manifest.ReadinessSummary.ProviderReadiness);
        Assert.Equal(inputs.ExecutionPlanningState.Readiness, first.Manifest.ReadinessSummary.GenerationReadiness);

        Assert.Equal(inputs.Planning.Outcome.ReadinessSummary.ApprovalStatus, first.Manifest.ApprovalSummary.DesignApproval);
        Assert.Equal(inputs.Planning.Outcome.Status, first.Manifest.ApprovalSummary.PlanningApproval.OutcomeStatus);
        Assert.Equal(inputs.MicrosoftRuntimeState.AcceptsExecutionCandidate, first.Manifest.ApprovalSummary.RuntimeApproval.AcceptsExecutionCandidate);
        Assert.True(first.Manifest.ApprovalSummary.ProviderApproval.ProviderApproved);
        Assert.Contains(first.Manifest.Lineage.ImmutableUpstreamLineage, reference => reference == first.Manifest.SourceReferences.DesignPackageRef);
        Assert.Contains(first.Manifest.Lineage.ImmutableUpstreamLineage, reference => reference == first.Manifest.SourceReferences.RuntimeProviderRef);
        Assert.Equal(Serialize(first), Serialize(second));
    }

    [Fact(DisplayName = "Generation Manifest validation fails for missing references, invalid readiness consistency, provider incompatibility, schema mismatches, and lineage integrity drift")]
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
            inputs.RuntimeProviderState,
            inputs.MicrosoftRuntimeState,
            createdUtc);

        var invalidManifest = baseline.Manifest! with
        {
            Metadata = baseline.Manifest!.Metadata with
            {
                SchemaVersion = "generation-manifest/v2"
            },
            SourceReferences = baseline.Manifest.SourceReferences with
            {
                RuntimeProviderRef = string.Empty,
                PlanningOutcomeRef = "planningOutcome:different"
            },
            ReadinessSummary = baseline.Manifest.ReadinessSummary with
            {
                RuntimeReadiness = RuntimeProviderReadinessState.Blocked,
                ProviderReadiness = GenerationProviderReadinessState.Blocked
            },
            CapabilitySummary = baseline.Manifest.CapabilitySummary with
            {
                SelectedProviderCandidates = []
            },
            Lineage = baseline.Manifest.Lineage with
            {
                ImmutableUpstreamLineage = baseline.Manifest.Lineage.ImmutableUpstreamLineage
                    .Where(reference => !string.Equals(reference, baseline.Manifest.SourceReferences.PlanningOutcomeRef, StringComparison.Ordinal))
                    .ToArray()
            }
        };

        var validation = validator.Validate(
            invalidManifest,
            inputs.Planning,
            inputs.SpecificationState,
            inputs.ProviderState,
            inputs.ExecutionPlanningState,
            inputs.RuntimeProviderState,
            inputs.MicrosoftRuntimeState);

        Assert.False(validation.IsValid);
        Assert.Contains("sourceReferences.runtimeProviderRef", validation.Diagnostics.MissingRequiredFields);
        Assert.Contains("generation-manifest/v2", validation.Diagnostics.UnsupportedSchemaVersions);
        Assert.Contains("sourceReferences.planningOutcomeRef must match planningOutcome.metadata.outcomeId.", validation.Diagnostics.InvalidReferences);
        Assert.Contains("readinessSummary.runtimeReadiness must match runtimeProvider.readiness.", validation.Diagnostics.ReadinessConsistencyFailures);
        Assert.Contains("capabilitySummary.selectedProviderCandidates must match microsoft runtime candidate provider ids.", validation.Diagnostics.ProviderCompatibilityFailures);
        Assert.Contains("lineage.immutableUpstreamLineage must contain every required upstream reference.", validation.Diagnostics.LineageIntegrityFailures);
    }

    [Fact(DisplayName = "Generation Manifest readiness distinguishes incomplete, blocked, and readyForGenerator states deterministically")]
    public void ReadinessService_EvaluatesEveryStateCorrectly()
    {
        var readiness = new GenerationManifestReadinessService();
        var incompleteValidation = new GenerationManifestValidationResult(
            new GenerationManifestValidationDiagnostics(
                MissingRequiredSections: ["sourceReferences"],
                MissingRequiredFields: [],
                InvalidReferences: [],
                UnsupportedSchemaVersions: [],
                LineageIntegrityFailures: [],
                ReadinessConsistencyFailures: [],
                ProviderCompatibilityFailures: [],
                GenerationSpecificationCompletenessFailures: [],
                BoundaryViolations: []));
        var blockedValidation = new GenerationManifestValidationResult(
            new GenerationManifestValidationDiagnostics(
                MissingRequiredSections: [],
                MissingRequiredFields: [],
                InvalidReferences: [],
                UnsupportedSchemaVersions: [],
                LineageIntegrityFailures: [],
                ReadinessConsistencyFailures: ["readinessSummary.generationReadiness must match generationProviderExecutionPlan.readiness."],
                ProviderCompatibilityFailures: [],
                GenerationSpecificationCompletenessFailures: [],
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

    [Fact(DisplayName = "Generation Manifest contract inventory covers required metadata, source references, capability summary, readiness summary, approval summary, constraints, and lineage field paths")]
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

    internal static (
        PlanningOrchestrationResult Planning,
        PbirGenerationSpecificationState SpecificationState,
        GenerationProviderFrameworkState ProviderState,
        GenerationProviderExecutionPlanningState ExecutionPlanningState,
        RuntimeProviderFrameworkState RuntimeProviderState,
        MicrosoftRuntimeProviderFrameworkState MicrosoftRuntimeState) CreateReadyInputs()
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
        var runtimeService = new RuntimeProviderAbstractionFrameworkService(runtimeRegistry);
        var runtimeRegistration = runtimeService.CreateDefaultRegistration(
            planning.ExecutionProviderState!.ProviderDefinition!,
            planning.ExecutionProviderState.ProviderRequest!);
        runtimeRegistry.Register(runtimeRegistration);
        var runtimeProviderState = runtimeService.CreateRuntimeCandidate(planning, runtimeRegistration.ProviderId);

        var microsoftRuntimeService = new MicrosoftRuntimeProviderContractFrameworkService(runtimeRegistry);
        var microsoftRuntimeDefinition = microsoftRuntimeService.CreateDefaultProviderDefinition();
        runtimeRegistry.Register(microsoftRuntimeService.CreateDefaultRegistration(microsoftRuntimeDefinition, planning));
        var microsoftRuntimeState = microsoftRuntimeService.CreateMicrosoftRuntimeState(planning, microsoftRuntimeDefinition.ProviderId);

        return (planning, specificationState, providerState, executionPlanningState, runtimeProviderState, microsoftRuntimeState);
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
