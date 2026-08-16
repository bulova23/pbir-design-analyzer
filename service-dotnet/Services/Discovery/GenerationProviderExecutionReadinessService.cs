using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationProviderExecutionReadinessService
{
    internal GenerationProviderExecutionPlanReadinessState Evaluate(GenerationProviderExecutionPlanValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);

        if (validation.Diagnostics.HasBlockingFailures)
        {
            return GenerationProviderExecutionPlanReadinessState.Blocked;
        }

        if (validation.Diagnostics.HasCompatibilityFailures)
        {
            return GenerationProviderExecutionPlanReadinessState.PartiallyPrepared;
        }

        return GenerationProviderExecutionPlanReadinessState.Prepared;
    }

    internal GenerationProviderExecutionPlanReadinessState PrepareForExecutionProvider(
        GenerationProviderExecutionPlanReadinessState readiness,
        GenerationProviderExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (readiness != GenerationProviderExecutionPlanReadinessState.Prepared)
        {
            return readiness;
        }

        var approvals = plan.ExecutionDependencies.RequiredApprovals;
        var approvalsSatisfied =
            (!approvals.DesignApprovalRequired || approvals.DesignApproved) &&
            (!approvals.GenerationApprovalRequired || approvals.GenerationApproved);
        var providerReady = plan.ExecutionDependencies.ProviderReadiness.CurrentReadiness ==
            plan.ExecutionDependencies.ProviderReadiness.RequiredReadiness;
        var runtimeReady = plan.ExecutionDependencies.RuntimeReadiness.CurrentReadiness ==
            plan.ExecutionDependencies.RuntimeReadiness.RequiredReadiness;
        var specificationReady = plan.ExecutionDependencies.SpecificationCompleteness.CurrentReadiness ==
            plan.ExecutionDependencies.SpecificationCompleteness.RequiredReadiness;

        return approvalsSatisfied && providerReady && runtimeReady && specificationReady
            ? GenerationProviderExecutionPlanReadinessState.ReadyForExecutionProvider
            : GenerationProviderExecutionPlanReadinessState.Prepared;
    }
}
