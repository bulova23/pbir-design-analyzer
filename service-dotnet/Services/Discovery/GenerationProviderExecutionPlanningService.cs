using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationProviderExecutionPlanningService
{
    private readonly GenerationProviderExecutionPlanValidator _validator;
    private readonly GenerationProviderExecutionReadinessService _readinessService;

    internal GenerationProviderExecutionPlanningService()
        : this(new GenerationProviderExecutionPlanValidator(), new GenerationProviderExecutionReadinessService())
    {
    }

    internal GenerationProviderExecutionPlanningService(
        GenerationProviderExecutionPlanValidator validator,
        GenerationProviderExecutionReadinessService readinessService)
    {
        _validator = validator;
        _readinessService = readinessService;
    }

    internal GenerationProviderExecutionPlanningState CreatePlanState(
        GenerationProviderRequest request,
        GenerationProviderDefinition provider,
        PbirGenerationSpecificationState specificationState,
        PlanningOutcome planningOutcome)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(specificationState);
        ArgumentNullException.ThrowIfNull(planningOutcome);

        var plan = CreatePlan(request, provider, specificationState, planningOutcome);
        var validation = _validator.Validate(plan, request, provider, specificationState, planningOutcome);
        var readiness = _readinessService.Evaluate(validation);
        readiness = _readinessService.PrepareForExecutionProvider(readiness, plan);

        return new GenerationProviderExecutionPlanningState(
            Plan: plan,
            Validation: validation,
            Readiness: readiness);
    }

    private static GenerationProviderExecutionPlan CreatePlan(
        GenerationProviderRequest request,
        GenerationProviderDefinition provider,
        PbirGenerationSpecificationState specificationState,
        PlanningOutcome planningOutcome)
    {
        var providerReadiness = new GenerationProviderReadinessService().Evaluate(
            new GenerationProviderValidator().Validate(specificationState, request, provider),
            provider);

        return new GenerationProviderExecutionPlan(
            Metadata: new GenerationProviderExecutionPlanMetadata(
                ExecutionPlanId: $"generationProviderExecutionPlan:{request.Metadata.RequestId}",
                SchemaVersion: GenerationProviderExecutionPlanContract.SchemaVersionV1),
            References: new GenerationProviderExecutionPlanReferences(
                GenerationProviderRequestRef: request.Metadata.RequestId,
                PbirGenerationSpecificationRef: request.References.PbirSpecificationReference.SpecificationId,
                PlanningOutcomeRef: request.References.PlanningOutcomeReference.OutcomeId),
            ExecutionStages:
            [
                new GenerationProviderExecutionStage(
                    StageId: "specificationValidation",
                    StageName: "Specification Validation",
                    Sequence: 1,
                    RequiredDependencyIds: ["specificationCompleteness"]),
                new GenerationProviderExecutionStage(
                    StageId: "providerCapabilityValidation",
                    StageName: "Provider Capability Validation",
                    Sequence: 2,
                    RequiredDependencyIds: ["providerReadiness"]),
                new GenerationProviderExecutionStage(
                    StageId: "executionPreparation",
                    StageName: "Execution Preparation",
                    Sequence: 3,
                    RequiredDependencyIds: ["runtimeReadiness"]),
                new GenerationProviderExecutionStage(
                    StageId: "providerHandoffPreparation",
                    StageName: "Provider Handoff Preparation",
                    Sequence: 4,
                    RequiredDependencyIds: ["requiredApprovals"])
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
                    DesignApprovalRequired: planningOutcome.ReadinessSummary.ApprovalStatus.DesignApprovalRequired,
                    GenerationApprovalRequired: planningOutcome.ReadinessSummary.ApprovalStatus.GenerationApprovalRequired,
                    AnalyzerReviewRequired: planningOutcome.ReadinessSummary.ApprovalStatus.AnalyzerValidationRequired,
                    DesignApproved: planningOutcome.ReadinessSummary.ApprovalStatus.DesignApproved,
                    GenerationApproved: planningOutcome.ReadinessSummary.ApprovalStatus.GenerationApproved),
                ProviderReadiness: new GenerationProviderExecutionProviderDependency(
                    CurrentReadiness: providerReadiness,
                    RequiredReadiness: GenerationProviderReadinessState.ReadyForGenerationProvider),
                RuntimeReadiness: new GenerationProviderExecutionRuntimeDependency(
                    CurrentReadiness: planningOutcome.ReadinessSummary.Status,
                    RequiredReadiness: PlanningReadinessStatus.ApprovedForExecutionProvider),
                SpecificationCompleteness: new GenerationProviderExecutionSpecificationDependency(
                    CurrentReadiness: specificationState.Readiness,
                    RequiredReadiness: PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider)));
    }
}
