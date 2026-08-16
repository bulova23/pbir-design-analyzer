using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirExecutionSafetyGate
{
    internal PbirExecutionSafetyGateResult Validate(
        PlanningOrchestrationResult planning,
        MicrosoftRuntimeProviderFrameworkState runtime,
        PbirExecutionPrototypeOptions options)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(options);

        var targetProfileId = runtime.Request?.TargetProfile.TargetProfileId ?? string.Empty;
        var reasons = new List<string>();

        if (!string.Equals(targetProfileId, GenerationRequestContract.PbirReportDefaultProfile, StringComparison.Ordinal))
        {
            reasons.Add(targetProfileId);
            reasons.Add($"PBIR execution prototype boundary supports only {GenerationRequestContract.PbirReportDefaultProfile}; received {targetProfileId}.");
        }

        if (runtime.Readiness != MicrosoftRuntimeReadinessState.ReadyForMicrosoftRuntimeProvider)
        {
            reasons.Add("Runtime readiness must be readyForMicrosoftRuntimeProvider.");
        }

        if (!runtime.AcceptsExecutionCandidate)
        {
            reasons.Add("Runtime candidate must be accepted by the Microsoft runtime provider contract.");
        }

        var approvalState = runtime.Request?.ReviewRequirements;
        if (approvalState is not null)
        {
            if (approvalState.DesignApprovalRequired && !approvalState.DesignApproved)
            {
                reasons.Add("Required design approval is missing.");
            }

            if (approvalState.GenerationApprovalRequired && !approvalState.GenerationApproved)
            {
                reasons.Add("Required generation approval is missing.");
            }
        }

        var providerCategory = runtime.Definition?.ProviderCategory ?? runtime.Registration?.ProviderCategory ?? string.Empty;
        if (!string.Equals(providerCategory, MicrosoftAdapterSpecificationContract.ProviderCategory, StringComparison.Ordinal))
        {
            reasons.Add($"Unsupported provider category {providerCategory}. PBIR execution prototype boundary requires {MicrosoftAdapterSpecificationContract.ProviderCategory}.");
        }

        if (options.AllowLiveProviderInvocation)
        {
            reasons.Add("Live provider invocation is not allowed.");
        }

        if (options.AllowDeployment)
        {
            reasons.Add("Deployment is not allowed.");
        }

        if (options.ExecutionMode == PbirExecutionMode.DryRun && !options.DryRun)
        {
            reasons.Add("Non-dry-run execution must use mockedExecution mode.");
        }

        if (options.ExecutionMode == PbirExecutionMode.MockedExecution && string.IsNullOrWhiteSpace(options.MockFixtureId))
        {
            reasons.Add("Mocked execution requires a deterministic fixture id.");
        }

        return new PbirExecutionSafetyGateResult(
            IsAllowed: reasons.Count == 0,
            TargetProfileId: targetProfileId,
            RuntimeReadiness: runtime.Readiness,
            ExecutionMode: options.ExecutionMode,
            DryRun: options.DryRun,
            Reasons: reasons);
    }
}
