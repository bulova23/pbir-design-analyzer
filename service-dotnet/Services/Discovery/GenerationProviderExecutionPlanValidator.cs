using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationProviderExecutionPlanValidator
{
    private static readonly IReadOnlyList<string> ExpectedStageOrder =
    [
        "specificationValidation",
        "providerCapabilityValidation",
        "executionPreparation",
        "providerHandoffPreparation",
    ];

    internal GenerationProviderExecutionPlanValidationResult Validate(
        GenerationProviderExecutionPlan plan,
        GenerationProviderRequest request,
        GenerationProviderDefinition provider,
        PbirGenerationSpecificationState specificationState,
        PlanningOutcome planningOutcome)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(specificationState);
        ArgumentNullException.ThrowIfNull(planningOutcome);

        var missingRequiredSections = new List<string>();
        var missingRequiredFields = new List<string>();
        var invalidReferences = new List<string>();
        var stageOrderingFailures = new List<string>();
        var readinessCompatibilityFailures = new List<string>();
        var providerCompatibilityFailures = new List<string>();
        var unsupportedSchemaVersions = new List<string>();
        var boundaryViolations = new List<string>();

        ValidateMetadata(plan, missingRequiredSections, missingRequiredFields, unsupportedSchemaVersions);
        ValidateReferences(plan, request, specificationState, planningOutcome, missingRequiredSections, missingRequiredFields, invalidReferences);
        ValidateStages(plan, missingRequiredSections, missingRequiredFields, stageOrderingFailures);
        ValidateConstraints(plan, request, boundaryViolations);
        ValidateCompatibility(
            plan,
            request,
            provider,
            specificationState,
            planningOutcome,
            readinessCompatibilityFailures,
            providerCompatibilityFailures,
            unsupportedSchemaVersions);

        return new GenerationProviderExecutionPlanValidationResult(
            new GenerationProviderExecutionPlanValidationDiagnostics(
                MissingRequiredSections: DistinctAndOrder(missingRequiredSections),
                MissingRequiredFields: DistinctAndOrder(missingRequiredFields),
                InvalidReferences: DistinctAndOrder(invalidReferences),
                StageOrderingFailures: DistinctAndOrder(stageOrderingFailures),
                ReadinessCompatibilityFailures: DistinctAndOrder(readinessCompatibilityFailures),
                ProviderCompatibilityFailures: DistinctAndOrder(providerCompatibilityFailures),
                UnsupportedSchemaVersions: DistinctAndOrder(unsupportedSchemaVersions),
                BoundaryViolations: DistinctAndOrder(boundaryViolations)));
    }

    private static void ValidateMetadata(
        GenerationProviderExecutionPlan plan,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields,
        ICollection<string> unsupportedSchemaVersions)
    {
        if (plan.Metadata is null)
        {
            missingRequiredSections.Add("metadata");
            return;
        }

        ValidateNotBlank(plan.Metadata.ExecutionPlanId, "metadata.executionPlanId", missingRequiredFields);
        ValidateSchemaVersion(plan.Metadata.SchemaVersion, GenerationProviderExecutionPlanContract.SchemaVersionV1, unsupportedSchemaVersions);
    }

    private static void ValidateReferences(
        GenerationProviderExecutionPlan plan,
        GenerationProviderRequest request,
        PbirGenerationSpecificationState specificationState,
        PlanningOutcome planningOutcome,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields,
        ICollection<string> invalidReferences)
    {
        if (plan.References is null)
        {
            missingRequiredSections.Add("references");
            return;
        }

        ValidateNotBlank(plan.References.GenerationProviderRequestRef, "references.generationProviderRequestRef", missingRequiredFields);
        ValidateNotBlank(plan.References.PbirGenerationSpecificationRef, "references.pbirGenerationSpecificationRef", missingRequiredFields);
        ValidateNotBlank(plan.References.PlanningOutcomeRef, "references.planningOutcomeRef", missingRequiredFields);

        if (!string.Equals(plan.References.GenerationProviderRequestRef, request.Metadata.RequestId, StringComparison.Ordinal))
        {
            invalidReferences.Add("references.generationProviderRequestRef must match generationProviderRequest.metadata.requestId.");
        }

        if (!string.Equals(plan.References.PbirGenerationSpecificationRef, specificationState.Specification?.SpecificationId, StringComparison.Ordinal))
        {
            invalidReferences.Add("references.pbirGenerationSpecificationRef must match pbirGenerationSpecification.specificationId.");
        }

        if (!string.Equals(plan.References.PlanningOutcomeRef, planningOutcome.Metadata.OutcomeId, StringComparison.Ordinal))
        {
            invalidReferences.Add("references.planningOutcomeRef must match planningOutcome.metadata.outcomeId.");
        }
    }

    private static void ValidateStages(
        GenerationProviderExecutionPlan plan,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields,
        ICollection<string> stageOrderingFailures)
    {
        if (plan.ExecutionStages is null || plan.ExecutionStages.Count == 0)
        {
            missingRequiredSections.Add("executionStages");
            return;
        }

        foreach (var stage in plan.ExecutionStages)
        {
            ValidateNotBlank(stage.StageId, "executionStages.stageId", missingRequiredFields);
            ValidateNotBlank(stage.StageName, "executionStages.stageName", missingRequiredFields);

            if (stage.Sequence <= 0)
            {
                missingRequiredFields.Add("executionStages.sequence");
            }
        }

        var actualStageOrder = plan.ExecutionStages
            .Select(stage => stage.StageId)
            .ToArray();

        if (!actualStageOrder.SequenceEqual(ExpectedStageOrder, StringComparer.Ordinal))
        {
            stageOrderingFailures.Add("executionStages must remain in deterministic provider-neutral order.");
        }

        var expectedSequences = Enumerable.Range(1, ExpectedStageOrder.Count).ToArray();
        var actualSequences = plan.ExecutionStages.Select(stage => stage.Sequence).ToArray();
        if (!actualSequences.SequenceEqual(expectedSequences))
        {
            stageOrderingFailures.Add("executionStages.sequence must remain contiguous and deterministic.");
        }
    }

    private static void ValidateConstraints(
        GenerationProviderExecutionPlan plan,
        GenerationProviderRequest request,
        ICollection<string> boundaryViolations)
    {
        if (plan.ExecutionConstraints is null)
        {
            boundaryViolations.Add("executionConstraints must exist.");
            return;
        }

        if (!plan.ExecutionConstraints.DryRunOnly ||
            !plan.ExecutionConstraints.DeploymentProhibited ||
            !plan.ExecutionConstraints.ProviderInvocationProhibited ||
            !plan.ExecutionConstraints.ApiInvocationProhibited ||
            !plan.ExecutionConstraints.CliInvocationProhibited ||
            !plan.ExecutionConstraints.ReportMutationProhibited)
        {
            boundaryViolations.Add("executionConstraints must preserve the Phase 17 non-execution trust boundary.");
        }

        if (request.Requirements.Constraints.AllowApiInvocation ||
            request.Requirements.Constraints.AllowCliInvocation ||
            request.Requirements.Constraints.AllowDeployment ||
            request.Requirements.Constraints.AllowReportMutation)
        {
            boundaryViolations.Add("generationProviderRequest.constraints must remain execution-free in Phase 17.");
        }
    }

    private static void ValidateCompatibility(
        GenerationProviderExecutionPlan plan,
        GenerationProviderRequest request,
        GenerationProviderDefinition provider,
        PbirGenerationSpecificationState specificationState,
        PlanningOutcome planningOutcome,
        ICollection<string> readinessCompatibilityFailures,
        ICollection<string> providerCompatibilityFailures,
        ICollection<string> unsupportedSchemaVersions)
    {
        ValidateSchemaVersion(request.SchemaVersion, GenerationProviderRequestContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(provider.SchemaVersion, GenerationProviderDefinitionContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(planningOutcome.Metadata.SchemaVersion, PlanningOutcomeContract.SchemaVersionV1, unsupportedSchemaVersions);

        var providerValidation = new GenerationProviderValidator().Validate(specificationState, request, provider);
        AddAll(readinessCompatibilityFailures, providerValidation.Diagnostics.SpecificationCompletenessFailures);
        AddAll(readinessCompatibilityFailures, providerValidation.Diagnostics.UnsupportedSchemaVersions);
        AddAll(providerCompatibilityFailures, providerValidation.Diagnostics.ProviderCompatibilityFailures);
        AddAll(providerCompatibilityFailures, providerValidation.Diagnostics.UnsupportedArtifactTypes);
        AddAll(providerCompatibilityFailures, providerValidation.Diagnostics.UnsupportedTargetProfiles);
        AddAll(providerCompatibilityFailures, providerValidation.Diagnostics.UnsupportedGenerationModes);

        if (provider.Status == GenerationProviderStatus.Unsupported)
        {
            providerCompatibilityFailures.Add("provider.status must remain compatible with execution planning.");
        }

        if (plan.ExecutionDependencies.ProviderReadiness.CurrentReadiness != GenerationProviderReadinessState.ReadyForGenerationProvider)
        {
            readinessCompatibilityFailures.Add("provider is not ready for generation provider planning.");
        }

        if (plan.ExecutionDependencies.SpecificationCompleteness.CurrentReadiness != PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider)
        {
            readinessCompatibilityFailures.Add("specification is not ready for generation provider planning.");
        }

        if (plan.ExecutionDependencies.RuntimeReadiness.CurrentReadiness != PlanningReadinessStatus.ApprovedForExecutionProvider)
        {
            readinessCompatibilityFailures.Add("planning outcome is not approved for execution provider readiness.");
        }
    }

    private static void ValidateSchemaVersion(string actual, string expected, ICollection<string> unsupportedSchemaVersions)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            unsupportedSchemaVersions.Add(actual);
        }
    }

    private static void ValidateNotBlank(string? value, string fieldPath, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add(fieldPath);
        }
    }

    private static string[] DistinctAndOrder(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddAll(ICollection<string> target, IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            target.Add(value);
        }
    }
}
