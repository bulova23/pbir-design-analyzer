using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class RuntimeReadinessService
{
    internal RuntimeProviderResult Evaluate(
        PlanningOrchestrationResult planning,
        RuntimeProviderRegistration? registration,
        RuntimeProviderValidationResult validation,
        RuntimeProviderRequest request,
        RuntimeProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var readiness = DetermineReadiness(planning, registration, validation, request);

        return new RuntimeProviderResult(
            SchemaVersion: RuntimeProviderResultContract.SchemaVersionV1,
            ResultId: $"runtimeResult:{request.RequestId}",
            RequestId: request.RequestId,
            Status: ToStatus(readiness),
            ReadinessStatus: readiness,
            Reasons: CollectReasons(planning, registration, validation, request, readiness));
    }

    private static RuntimeProviderReadinessState DetermineReadiness(
        PlanningOrchestrationResult planning,
        RuntimeProviderRegistration? registration,
        RuntimeProviderValidationResult validation,
        RuntimeProviderRequest request)
    {
        if (planning.Outcome.Status is PlanningOutcomeStatus.PlanningBlocked or PlanningOutcomeStatus.PlanningFailed)
        {
            return RuntimeProviderReadinessState.Blocked;
        }

        if (validation.Diagnostics.MissingRequiredSections.Count > 0 ||
            validation.Diagnostics.MissingRequiredFields.Count > 0)
        {
            return RuntimeProviderReadinessState.Invalid;
        }

        if (validation.Diagnostics.InvalidReferences.Count > 0 ||
            validation.Diagnostics.InvalidLineage.Count > 0 ||
            validation.Diagnostics.InvalidApprovalState.Count > 0 ||
            validation.Diagnostics.VersionMismatches.Count > 0)
        {
            return RuntimeProviderReadinessState.Blocked;
        }

        if (registration is null ||
            !registration.SupportedRequestSchemaVersions.Contains(request.SchemaVersion, StringComparer.Ordinal) ||
            !registration.SupportedTargetProfiles.Contains(request.ExecutionConstraints.RequiredTargetProfileId, StringComparer.Ordinal) ||
            !string.Equals(registration.ExecutionProviderRef, request.ExecutionProviderRef, StringComparison.Ordinal) ||
            !string.Equals(registration.ProviderCategory, request.ExecutionConstraints.RequiredProviderCategory, StringComparison.Ordinal) ||
            request.ExecutionConstraints.RequiredCapabilities.Any(capability =>
                !registration.SupportedCapabilities.Contains(capability, StringComparer.Ordinal)))
        {
            return RuntimeProviderReadinessState.Unsupported;
        }

        if (validation.Diagnostics.CapabilityResolutionFailures.Count > 0 ||
            validation.Diagnostics.ExecutionConstraintFailures.Count > 0)
        {
            return RuntimeProviderReadinessState.Invalid;
        }

        return request.ApprovalState.DesignApprovalRequired && !request.ApprovalState.DesignApproved ||
            request.ApprovalState.GenerationApprovalRequired && !request.ApprovalState.GenerationApproved
            ? RuntimeProviderReadinessState.Candidate
            : RuntimeProviderReadinessState.ReadyForRuntimeProvider;
    }

    private static RuntimeProviderResultStatus ToStatus(RuntimeProviderReadinessState readiness)
    {
        return readiness switch
        {
            RuntimeProviderReadinessState.ReadyForRuntimeProvider => RuntimeProviderResultStatus.Accepted,
            RuntimeProviderReadinessState.Candidate => RuntimeProviderResultStatus.Rejected,
            RuntimeProviderReadinessState.Unsupported => RuntimeProviderResultStatus.Unsupported,
            RuntimeProviderReadinessState.Blocked => RuntimeProviderResultStatus.Blocked,
            _ => RuntimeProviderResultStatus.ValidationFailed,
        };
    }

    private static IReadOnlyList<string> CollectReasons(
        PlanningOrchestrationResult planning,
        RuntimeProviderRegistration? registration,
        RuntimeProviderValidationResult validation,
        RuntimeProviderRequest request,
        RuntimeProviderReadinessState readiness)
    {
        var reasons = validation.Diagnostics.MissingRequiredSections
            .Concat(validation.Diagnostics.MissingRequiredFields)
            .Concat(validation.Diagnostics.InvalidReferences)
            .Concat(validation.Diagnostics.InvalidLineage)
            .Concat(validation.Diagnostics.InvalidApprovalState)
            .Concat(validation.Diagnostics.CapabilityResolutionFailures)
            .Concat(validation.Diagnostics.ExecutionConstraintFailures)
            .Concat(validation.Diagnostics.VersionMismatches)
            .Concat(planning.Outcome.ReadinessSummary.BlockingIssues);

        if (registration is null)
        {
            reasons = reasons.Concat(["runtime provider registration was not found."]);
        }
        else
        {
            if (!registration.SupportedRequestSchemaVersions.Contains(request.SchemaVersion, StringComparer.Ordinal))
            {
                reasons = reasons.Concat([request.SchemaVersion]);
            }

            if (!registration.SupportedTargetProfiles.Contains(request.ExecutionConstraints.RequiredTargetProfileId, StringComparer.Ordinal))
            {
                reasons = reasons.Concat([request.ExecutionConstraints.RequiredTargetProfileId]);
            }

            if (!string.Equals(registration.ExecutionProviderRef, request.ExecutionProviderRef, StringComparison.Ordinal))
            {
                reasons = reasons.Concat(["runtime provider registration does not match the execution-provider reference."]);
            }

            foreach (var capability in request.ExecutionConstraints.RequiredCapabilities)
            {
                if (!registration.SupportedCapabilities.Contains(capability, StringComparer.Ordinal))
                {
                    reasons = reasons.Concat([capability]);
                }
            }
        }

        if (readiness == RuntimeProviderReadinessState.Candidate)
        {
            if (request.ApprovalState.DesignApprovalRequired && !request.ApprovalState.DesignApproved)
            {
                reasons = reasons.Concat(["design approval has not been satisfied."]);
            }

            if (request.ApprovalState.GenerationApprovalRequired && !request.ApprovalState.GenerationApproved)
            {
                reasons = reasons.Concat(["generation approval has not been satisfied."]);
            }
        }

        return reasons
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reason => reason, StringComparer.Ordinal)
            .ToArray();
    }
}
