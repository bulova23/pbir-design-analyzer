using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class RuntimeProviderValidator
{
    internal RuntimeProviderValidationResult Validate(
        PlanningOrchestrationResult planning,
        RuntimeProviderRequest request,
        RuntimeProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var missingSections = new List<string>();
        var missingFields = new List<string>();
        var invalidReferences = new List<string>();
        var invalidLineage = new List<string>();
        var invalidApprovalState = new List<string>();
        var capabilityResolutionFailures = new List<string>();
        var executionConstraintFailures = new List<string>();
        var versionMismatches = new List<string>();

        ValidateNotBlank(request.RequestId, "runtimeProviderRequest.requestId", missingFields);
        ValidateNotBlank(request.PlanningOutcomeRef, "runtimeProviderRequest.planningOutcomeRef", missingFields);
        ValidateNotBlank(request.ExecutionProviderRef, "runtimeProviderRequest.executionProviderRef", missingFields);
        ValidateNotBlank(request.ExecutionPlanRef, "runtimeProviderRequest.executionPlanRef", missingFields);
        ValidateNotBlank(request.CapabilityResolutionRef, "runtimeProviderRequest.capabilityResolutionRef", missingFields);
        ValidateNotEmpty(request.ExecutionConstraints.RequiredCapabilities, "runtimeProviderRequest.executionConstraints.requiredCapabilities", missingSections);
        ValidateNotBlank(context.ContextId, "runtimeProviderContext.contextId", missingFields);
        ValidateNotBlank(context.TargetProfileId, "runtimeProviderContext.targetProfileId", missingFields);
        ValidateNotBlank(context.ProviderCategory, "runtimeProviderContext.providerCategory", missingFields);

        ValidateVersion(request.SchemaVersion, RuntimeProviderRequestContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(context.SchemaVersion, RuntimeProviderContextContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(request.SourceContractVersions.PlanningOutcomeSchemaVersion, PlanningOutcomeContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(request.SourceContractVersions.ExecutionProviderSchemaVersion, ExecutionProviderContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(request.SourceContractVersions.ExecutionPlanSchemaVersion, ExecutionPlanContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(request.SourceContractVersions.CapabilityResolutionSchemaVersion, CapabilityNegotiationContract.SchemaVersionV1, versionMismatches);

        if (!string.Equals(request.PlanningOutcomeRef, planning.Outcome.Metadata.OutcomeId, StringComparison.Ordinal))
        {
            invalidReferences.Add("runtimeProviderRequest.planningOutcomeRef must match planningOutcome.metadata.outcomeId.");
        }

        if (!string.Equals(request.ExecutionProviderRef, planning.Outcome.References.ExecutionProviderRef, StringComparison.Ordinal))
        {
            invalidReferences.Add("runtimeProviderRequest.executionProviderRef must match planningOutcome.references.executionProviderRef.");
        }

        if (!string.Equals(request.ExecutionPlanRef, planning.Outcome.References.ExecutionPlanRef, StringComparison.Ordinal))
        {
            invalidReferences.Add("runtimeProviderRequest.executionPlanRef must match planningOutcome.references.executionPlanRef.");
        }

        if (!string.Equals(request.CapabilityResolutionRef, planning.Outcome.References.NegotiationRef, StringComparison.Ordinal))
        {
            invalidReferences.Add("runtimeProviderRequest.capabilityResolutionRef must match planningOutcome.references.negotiationRef.");
        }

        if (!string.Equals(context.ExecutionLineage.RequestRef, request.RequestId, StringComparison.Ordinal))
        {
            invalidLineage.Add("runtimeProviderContext.executionLineage.requestRef must match runtimeProviderRequest.requestId.");
        }

        if (!string.Equals(context.ExecutionLineage.PlanningOutcomeRef, request.PlanningOutcomeRef, StringComparison.Ordinal))
        {
            invalidLineage.Add("runtimeProviderContext.executionLineage.planningOutcomeRef must match runtimeProviderRequest.planningOutcomeRef.");
        }

        if (!string.Equals(context.ExecutionLineage.ExecutionProviderRef, request.ExecutionProviderRef, StringComparison.Ordinal))
        {
            invalidLineage.Add("runtimeProviderContext.executionLineage.executionProviderRef must match runtimeProviderRequest.executionProviderRef.");
        }

        if (!string.Equals(context.ExecutionLineage.ExecutionPlanRef, request.ExecutionPlanRef, StringComparison.Ordinal))
        {
            invalidLineage.Add("runtimeProviderContext.executionLineage.executionPlanRef must match runtimeProviderRequest.executionPlanRef.");
        }

        if (!string.Equals(context.ExecutionLineage.CapabilityResolutionRef, request.CapabilityResolutionRef, StringComparison.Ordinal))
        {
            invalidLineage.Add("runtimeProviderContext.executionLineage.capabilityResolutionRef must match runtimeProviderRequest.capabilityResolutionRef.");
        }

        var planningApproval = planning.Outcome.ReadinessSummary.ApprovalStatus;
        if (request.ApprovalState.GenerationApproved && !request.ApprovalState.DesignApproved)
        {
            invalidApprovalState.Add("generation approval cannot be satisfied before design approval.");
        }

        if (request.ApprovalState.DesignApprovalRequired != planningApproval.DesignApprovalRequired ||
            request.ApprovalState.GenerationApprovalRequired != planningApproval.GenerationApprovalRequired ||
            request.ApprovalState.AnalyzerValidationRequired != planningApproval.AnalyzerValidationRequired ||
            request.ApprovalState.DesignApproved != planningApproval.DesignApproved ||
            request.ApprovalState.GenerationApproved != planningApproval.GenerationApproved)
        {
            invalidApprovalState.Add("runtime provider approval state must inherit planning outcome approval status without modification.");
        }

        var resolvedCapabilities = planning.ExecutionProviderState?.ProviderRequest?.ExecutionConstraints.RequiredCapabilities
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal) ?? [];
        var negotiationResult = planning.CapabilityNegotiationState?.Result;
        var unresolvedCapabilities = negotiationResult?.Resolutions
            .Where(resolution =>
                resolution.RequirementLevel == CapabilityRequirementLevel.Required &&
                (resolution.Resolution == CapabilityResolutionStatus.Blocked ||
                 resolution.Resolution == CapabilityResolutionStatus.Unsupported))
            .Select(resolution => resolution.CapabilityId)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal) ?? [];

        foreach (var capability in request.ExecutionConstraints.RequiredCapabilities)
        {
            if (!resolvedCapabilities.Contains(capability))
            {
                capabilityResolutionFailures.Add(capability);
            }
        }

        foreach (var capability in request.ExecutionConstraints.UnresolvedCapabilities)
        {
            if (!unresolvedCapabilities.Contains(capability))
            {
                executionConstraintFailures.Add(capability);
            }
        }

        if (!string.Equals(request.ExecutionConstraints.RequiredTargetProfileId, planning.GenerationRequestState.Request?.TargetArtifactProfile.ProfileId, StringComparison.Ordinal))
        {
            executionConstraintFailures.Add("runtimeProviderRequest.executionConstraints.requiredTargetProfileId");
        }

        if (!string.Equals(request.ExecutionConstraints.RequiredProviderCategory, planning.ExecutionProviderState?.ProviderDefinition?.ProviderCategory, StringComparison.Ordinal))
        {
            executionConstraintFailures.Add("runtimeProviderRequest.executionConstraints.requiredProviderCategory");
        }

        if (!string.Equals(context.TargetProfileId, request.ExecutionConstraints.RequiredTargetProfileId, StringComparison.Ordinal))
        {
            executionConstraintFailures.Add("runtimeProviderContext.targetProfileId");
        }

        if (!string.Equals(context.ProviderCategory, request.ExecutionConstraints.RequiredProviderCategory, StringComparison.Ordinal))
        {
            executionConstraintFailures.Add("runtimeProviderContext.providerCategory");
        }

        return new RuntimeProviderValidationResult(
            new RuntimeProviderValidationDiagnostics(
                MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
                MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
                InvalidReferences: invalidReferences.Distinct(StringComparer.Ordinal).ToArray(),
                InvalidLineage: invalidLineage.Distinct(StringComparer.Ordinal).ToArray(),
                InvalidApprovalState: invalidApprovalState.Distinct(StringComparer.Ordinal).ToArray(),
                CapabilityResolutionFailures: capabilityResolutionFailures.Distinct(StringComparer.Ordinal).ToArray(),
                ExecutionConstraintFailures: executionConstraintFailures.Distinct(StringComparer.Ordinal).ToArray(),
                VersionMismatches: versionMismatches.Distinct(StringComparer.Ordinal).ToArray()));
    }

    private static void ValidateVersion(string candidate, string expected, ICollection<string> versionMismatches)
    {
        if (!string.Equals(candidate, expected, StringComparison.Ordinal))
        {
            versionMismatches.Add(candidate);
        }
    }

    private static void ValidateNotBlank(string? value, string fieldName, ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldName);
        }
    }

    private static void ValidateNotEmpty<T>(IReadOnlyCollection<T>? values, string sectionName, ICollection<string> missingSections)
    {
        if (values is null || values.Count == 0)
        {
            missingSections.Add(sectionName);
        }
    }
}
