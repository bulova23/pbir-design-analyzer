using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftRuntimeReadinessService
{
    internal MicrosoftRuntimeReadinessState Evaluate(
        PlanningOrchestrationResult planning,
        MicrosoftRuntimeProviderDefinition definition,
        RuntimeProviderRegistration? registration,
        MicrosoftRuntimeProviderValidationResult validation,
        MicrosoftRuntimeRequest request,
        MicrosoftRuntimeContext context)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (validation.Diagnostics.PlannedTargetProfiles.Count > 0)
        {
            return MicrosoftRuntimeReadinessState.PlannedOnly;
        }

        if (planning.Outcome.Status is PlanningOutcomeStatus.PlanningBlocked or PlanningOutcomeStatus.PlanningFailed ||
            validation.Diagnostics.BlockingFailures.Count > 0)
        {
            return MicrosoftRuntimeReadinessState.Blocked;
        }

        if (validation.Diagnostics.MissingRequiredSections.Count > 0 ||
            validation.Diagnostics.MissingRequiredFields.Count > 0 ||
            validation.Diagnostics.VersionMismatches.Count > 0 ||
            validation.Diagnostics.ProvenanceFailures.Count > 0)
        {
            return MicrosoftRuntimeReadinessState.Invalid;
        }

        if (registration is null ||
            !string.Equals(registration.ProviderCategory, definition.ProviderCategory, StringComparison.Ordinal) ||
            !registration.SupportedRequestSchemaVersions.Contains(RuntimeProviderRequestContract.SchemaVersionV1, StringComparer.Ordinal))
        {
            return MicrosoftRuntimeReadinessState.Unsupported;
        }

        if (validation.Diagnostics.UnsupportedTargetProfiles.Count > 0)
        {
            return MicrosoftRuntimeReadinessState.Unsupported;
        }

        if (validation.Diagnostics.IncompatibleCapabilities.Count > 0 ||
            validation.Diagnostics.ApprovalFailures.Count > 0)
        {
            return MicrosoftRuntimeReadinessState.Invalid;
        }

        return request.ReviewRequirements.DesignApprovalRequired && !request.ReviewRequirements.DesignApproved ||
            request.ReviewRequirements.GenerationApprovalRequired && !request.ReviewRequirements.GenerationApproved
            ? MicrosoftRuntimeReadinessState.Candidate
            : MicrosoftRuntimeReadinessState.ReadyForMicrosoftRuntimeProvider;
    }
}
