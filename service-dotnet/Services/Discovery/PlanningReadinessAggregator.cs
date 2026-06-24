using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PlanningReadinessAggregator
{
    internal PlanningReadinessSummary Aggregate(
        CapabilityNegotiationFrameworkState? negotiationState,
        MicrosoftSkillProviderPlanningState? microsoftSkillProviderState,
        ExecutionProviderFrameworkState? executionProviderState,
        IReadOnlyList<PlanningFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        var blockingIssues = failures
            .Select(failure => failure.Message)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToArray();
        var requiredCapabilities = negotiationState?.Result?.Requirements
            .Where(requirement => requirement.RequirementLevel == CapabilityRequirementLevel.Required)
            .Select(requirement => requirement.CapabilityId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray() ?? [];
        var resolvedCapabilities = negotiationState?.Result?.Resolutions
            .Where(resolution =>
                resolution.Resolution == CapabilityResolutionStatus.Satisfied ||
                resolution.Resolution == CapabilityResolutionStatus.Substituted)
            .Select(resolution => resolution.CapabilityId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray() ?? [];
        var unresolvedCapabilities = negotiationState?.Result?.Resolutions
            .Where(resolution =>
                resolution.Resolution == CapabilityResolutionStatus.Blocked ||
                resolution.Resolution == CapabilityResolutionStatus.Unsupported)
            .Select(resolution => resolution.CapabilityId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray() ?? [];
        var unresolvedRequirements = unresolvedCapabilities
            .Concat(microsoftSkillProviderState?.Selection?.UnsupportedSkills ?? [])
            .Concat(microsoftSkillProviderState?.Selection?.CoverageSummary.UnresolvedRequiredCapabilities ?? [])
            .Concat(executionProviderState?.Diagnostics.ApprovalRequirementFailures ?? [])
            .Concat(executionProviderState?.Diagnostics.ReadinessRequirementFailures ?? [])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToArray();
        var approvalPolicy = executionProviderState?.ApprovalPolicy ?? new ExecutionApprovalPolicy(
            DesignApprovalRequired: true,
            GenerationApprovalRequired: true,
            AnalyzerValidationRequired: true,
            DesignApproved: false,
            GenerationApproved: false);
        var providerReadiness = executionProviderState?.Readiness ?? ExecutionProviderReadinessState.NotEligible;

        return new PlanningReadinessSummary(
            Status: ToStatus(failures, providerReadiness, executionProviderState?.Eligibility),
            BlockingIssues: blockingIssues,
            UnresolvedRequirements: unresolvedRequirements,
            CapabilitySummary: new PlanningCapabilitySummary(
                RequiredCapabilities: requiredCapabilities,
                ResolvedCapabilities: resolvedCapabilities,
                UnresolvedCapabilities: unresolvedCapabilities),
            ApprovalStatus: new PlanningApprovalStatus(
                DesignApprovalRequired: approvalPolicy.DesignApprovalRequired,
                GenerationApprovalRequired: approvalPolicy.GenerationApprovalRequired,
                AnalyzerValidationRequired: approvalPolicy.AnalyzerValidationRequired,
                DesignApproved: approvalPolicy.DesignApproved,
                GenerationApproved: approvalPolicy.GenerationApproved),
            ExecutionProviderReadiness: providerReadiness);
    }

    private static PlanningReadinessStatus ToStatus(
        IReadOnlyList<PlanningFailure> failures,
        ExecutionProviderReadinessState providerReadiness,
        ExecutionEligibilityStatus? eligibility)
    {
        if (failures.Any(failure =>
            failure.FailureType == PlanningFailureType.InvalidInput ||
            failure.FailureType == PlanningFailureType.InvalidReference ||
            failure.FailureType == PlanningFailureType.InvalidTransition ||
            failure.FailureType == PlanningFailureType.InvalidVersion ||
            failure.FailureType == PlanningFailureType.MissingDependency ||
            failure.FailureType == PlanningFailureType.ReadinessConflict))
        {
            return PlanningReadinessStatus.Blocked;
        }

        if (providerReadiness == ExecutionProviderReadinessState.ApprovedForExecutionProvider)
        {
            return PlanningReadinessStatus.ApprovedForExecutionProvider;
        }

        if (eligibility == ExecutionEligibilityStatus.Eligible ||
            eligibility == ExecutionEligibilityStatus.ConditionallyEligible)
        {
            return PlanningReadinessStatus.ReadyForExecutionProvider;
        }

        return failures.Count > 0
            ? PlanningReadinessStatus.Blocked
            : PlanningReadinessStatus.Draft;
    }
}
