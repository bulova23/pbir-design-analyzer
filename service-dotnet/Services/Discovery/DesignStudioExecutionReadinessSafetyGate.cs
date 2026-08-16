using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class DesignStudioExecutionReadinessSafetyGate
{
    internal DesignStudioExecutionReadinessSafetyGateResult Validate(
        DesignStudioExecutionReadinessContext context,
        DesignStudioExecutionReadinessBoundaryRequests boundaryRequests)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(boundaryRequests);

        var reasons = new List<string>();

        if (context.PreviewReviewSchemaVersion != DesignStudioExecutionReadinessContract.SchemaVersionV1)
        {
            reasons.Add("readiness dashboard payload must use design-studio-execution-readiness/v1.");
        }

        if (context.GenerationManifestState.Manifest is null)
        {
            reasons.Add("generation manifest is required for execution readiness aggregation.");
        }

        if (context.GenerationManifestState.Manifest is not null)
        {
            var constraints = context.GenerationManifestState.Manifest.ExecutionConstraints;
            if (!constraints.DryRunOnly ||
                constraints.ProviderInvocationAllowed ||
                constraints.ApiInvocationAllowed ||
                constraints.CliInvocationAllowed ||
                constraints.DeploymentAllowed)
            {
                reasons.Add("generation manifest execution constraints must remain dry-run and non-invoking.");
            }
        }

        if (boundaryRequests.ExecutionRequested)
        {
            reasons.Add("execution requests are not allowed from Design Studio execution readiness.");
        }

        if (boundaryRequests.ProviderInvocationRequested)
        {
            reasons.Add("provider invocation requests are not allowed from Design Studio execution readiness.");
        }

        if (boundaryRequests.MicrosoftSkillsExecutionRequested)
        {
            reasons.Add("Microsoft Skills execution requests are not allowed from Design Studio execution readiness.");
        }

        if (boundaryRequests.ApiInvocationRequested)
        {
            reasons.Add("API invocation requests are not allowed from Design Studio execution readiness.");
        }

        if (boundaryRequests.CliInvocationRequested)
        {
            reasons.Add("CLI invocation requests are not allowed from Design Studio execution readiness.");
        }

        if (boundaryRequests.DeploymentRequested)
        {
            reasons.Add("deployment requests are not allowed from Design Studio execution readiness.");
        }

        if (boundaryRequests.AutomaticAnalyzerValidationRequested)
        {
            reasons.Add("automatic Analyzer validation requests are not allowed from Design Studio execution readiness.");
        }

        if (boundaryRequests.AutomaticAnalyzerLaunchRequested)
        {
            reasons.Add("automatic Analyzer launch requests are not allowed from Design Studio execution readiness.");
        }

        return new DesignStudioExecutionReadinessSafetyGateResult(
            IsAllowed: reasons.Count == 0,
            Reasons: reasons
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray());
    }
}
