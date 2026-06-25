using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationManifestService
{
    private readonly GenerationManifestValidator _validator;
    private readonly GenerationManifestReadinessService _readinessService;

    internal GenerationManifestService()
        : this(new GenerationManifestValidator(), new GenerationManifestReadinessService())
    {
    }

    internal GenerationManifestService(
        GenerationManifestValidator validator,
        GenerationManifestReadinessService readinessService)
    {
        _validator = validator;
        _readinessService = readinessService;
    }

    internal GenerationManifestState CreateManifestState(
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        MicrosoftRuntimeProviderFrameworkState runtimeState,
        DateTimeOffset createdUtc)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(specificationState);
        ArgumentNullException.ThrowIfNull(providerState);
        ArgumentNullException.ThrowIfNull(executionPlanningState);
        ArgumentNullException.ThrowIfNull(runtimeState);

        if (specificationState.Specification is null ||
            providerState.Provider is null ||
            providerState.Request is null ||
            executionPlanningState.Plan is null ||
            runtimeState.Request is null ||
            runtimeState.Context is null)
        {
            var validation = new GenerationManifestValidationResult(
                new GenerationManifestValidationDiagnostics(
                    MissingRequiredSections:
                    new[]
                    {
                        specificationState.Specification is null ? "generationSpecification" : string.Empty,
                        providerState.Provider is null ? "capabilitySummary.selectedProvider" : string.Empty,
                        providerState.Request is null ? "references.generationProviderRequestRef" : string.Empty,
                        executionPlanningState.Plan is null ? "references.generationProviderExecutionPlanRef" : string.Empty,
                        runtimeState.Request is null ? "references.runtimeProviderRef" : string.Empty,
                        runtimeState.Context is null ? "capabilitySummary.selectedSkills" : string.Empty,
                    }.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray(),
                    MissingRequiredFields: [],
                    InvalidReferences: [],
                    UnsupportedSchemaVersions: [],
                    LineageIntegrityFailures: [],
                    ReadinessConsistencyFailures: [],
                    ProviderCompatibilityFailures: [],
                    BoundaryViolations: []));

            return new GenerationManifestState(
                Manifest: null,
                Validation: validation,
                Readiness: _readinessService.Evaluate(validation));
        }

        var manifest = CreateManifest(
            planning,
            specificationState.Specification,
            providerState.Provider,
            providerState.Request,
            executionPlanningState,
            runtimeState,
            createdUtc);
        var manifestValidation = _validator.Validate(
            manifest,
            planning,
            specificationState,
            providerState,
            executionPlanningState,
            runtimeState);
        var readiness = _readinessService.Evaluate(manifestValidation);

        return new GenerationManifestState(
            Manifest: manifest,
            Validation: manifestValidation,
            Readiness: readiness);
    }

    private static GenerationManifest CreateManifest(
        PlanningOrchestrationResult planning,
        PbirGenerationSpecification specification,
        GenerationProviderDefinition provider,
        GenerationProviderRequest providerRequest,
        GenerationProviderExecutionPlanningState executionPlanningState,
        MicrosoftRuntimeProviderFrameworkState runtimeState,
        DateTimeOffset createdUtc)
    {
        var executionPlan = executionPlanningState.Plan!;
        var immutableReferences = BuildImmutableReferences(
            planning.Outcome,
            specification.SpecificationId,
            providerRequest.Metadata.RequestId,
            executionPlan.Metadata.ExecutionPlanId,
            runtimeState.Request!.RequestId);

        return new GenerationManifest(
            Metadata: new GenerationManifestMetadata(
                ManifestId: $"generationManifest:{planning.Outcome.Metadata.OutcomeId}",
                SchemaVersion: GenerationManifestContract.SchemaVersionV1,
                CreatedUtc: createdUtc.UtcDateTime),
            References: new GenerationManifestReferences(
                DesignPackageRef: planning.Outcome.References.DesignPackageRef,
                GenerationRequestRef: planning.Outcome.References.GenerationRequestRef,
                ExecutionPlanRef: planning.Outcome.References.ExecutionPlanRef,
                PlanningOutcomeRef: planning.Outcome.Metadata.OutcomeId,
                RuntimeProviderRef: runtimeState.Request!.RequestId,
                GenerationProviderRequestRef: providerRequest.Metadata.RequestId,
                GenerationProviderExecutionPlanRef: executionPlan.Metadata.ExecutionPlanId),
            GenerationSpecification: new GenerationManifestSpecificationSummary(
                PbirGenerationSpecificationRef: specification.SpecificationId),
            CapabilitySummary: new GenerationManifestCapabilitySummary(
                NegotiatedCapabilities: PreserveOrder(planning.Outcome.ReadinessSummary.CapabilitySummary.RequiredCapabilities),
                ProviderCapabilities: PreserveOrder(provider.SupportedCapabilities),
                SelectedProvider: new GenerationManifestSelectedProvider(
                    ProviderId: provider.ProviderId,
                    ProviderName: provider.ProviderName,
                    ProviderVersion: provider.ProviderVersion),
                SelectedSkills: PreserveOrder(runtimeState.Context!.MicrosoftSkillSummary.RequiredSkillIds)),
            ExecutionConstraints: new GenerationManifestExecutionConstraints(
                DryRunOnly: true,
                DeploymentAllowed: false,
                ProviderInvocationAllowed: false,
                ApiInvocationAllowed: false,
                CliInvocationAllowed: false),
            ApprovalSummary: new GenerationManifestApprovalSummary(
                DesignApproval: planning.Outcome.ReadinessSummary.ApprovalStatus,
                PlanningApproval: new GenerationManifestPlanningApprovalSummary(
                    OutcomeStatus: planning.Outcome.Status,
                    PlanningReadiness: planning.Outcome.ReadinessSummary.Status,
                    ExecutionProviderReadiness: planning.Outcome.ReadinessSummary.ExecutionProviderReadiness),
                RuntimeReadiness: runtimeState.Readiness,
                GenerationReadiness: executionPlanningState.Readiness),
            Lineage: new GenerationManifestLineage(
                UpstreamLineage: BuildLineageEntries(
                    planning.Outcome,
                    specification.SpecificationId,
                    providerRequest.Metadata.RequestId,
                    executionPlan.Metadata.ExecutionPlanId,
                    runtimeState.Request!.RequestId),
                ImmutableReferences: immutableReferences));
    }

    private static PlanningLineageEntry[] BuildLineageEntries(
        PlanningOutcome outcome,
        string specificationRef,
        string generationProviderRequestRef,
        string generationProviderExecutionPlanRef,
        string runtimeProviderRef)
    {
        return outcome.Lineage.UpstreamLineage
            .Concat(outcome.Lineage.PlanningLineage)
            .Concat(
            [
                new PlanningLineageEntry("generationProviderExecutionPlan", generationProviderExecutionPlanRef, "Generation provider execution plan"),
                new PlanningLineageEntry("generationProviderRequest", generationProviderRequestRef, "Generation provider request"),
                new PlanningLineageEntry("pbirGenerationSpecification", specificationRef, "PBIR generation specification"),
                new PlanningLineageEntry("runtimeProvider", runtimeProviderRef, "Runtime provider request"),
            ])
            .Distinct()
            .OrderBy(entry => entry.Stage, StringComparer.Ordinal)
            .ThenBy(entry => entry.ReferenceId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Label, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] BuildImmutableReferences(
        PlanningOutcome outcome,
        string specificationRef,
        string generationProviderRequestRef,
        string generationProviderExecutionPlanRef,
        string runtimeProviderRef)
    {
        return new[]
        {
            outcome.References.DesignPackageRef,
            outcome.References.GenerationRequestRef,
            outcome.References.ExecutionPlanRef,
            outcome.Metadata.OutcomeId,
            specificationRef,
            generationProviderRequestRef,
            generationProviderExecutionPlanRef,
            runtimeProviderRef
        }
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] PreserveOrder(IReadOnlyList<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
