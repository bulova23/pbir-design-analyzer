using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftRuntimeProviderValidator
{
    internal MicrosoftRuntimeProviderValidationResult Validate(
        PlanningOrchestrationResult planning,
        MicrosoftRuntimeProviderDefinition definition,
        MicrosoftRuntimeRequest request,
        MicrosoftRuntimeContext context)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var missingSections = new List<string>();
        var missingFields = new List<string>();
        var unsupportedTargetProfiles = new List<string>();
        var plannedTargetProfiles = new List<string>();
        var incompatibleCapabilities = new List<string>();
        var approvalFailures = new List<string>();
        var provenanceFailures = new List<string>();
        var versionMismatches = new List<string>();
        var blockingFailures = new List<string>();

        ValidateNotBlank(request.RequestId, "microsoftRuntimeRequest.requestId", missingFields);
        ValidateNotBlank(request.PlanningOutcomeReference.OutcomeId, "microsoftRuntimeRequest.planningOutcomeReference.outcomeId", missingFields);
        ValidateNotBlank(request.ExecutionCandidateReference.CandidateId, "microsoftRuntimeRequest.executionCandidateReference.candidateId", missingFields);
        ValidateNotBlank(request.TargetProfile.TargetProfileId, "microsoftRuntimeRequest.targetProfile.targetProfileId", missingFields);
        ValidateNotBlank(context.ContextId, "microsoftRuntimeContext.contextId", missingFields);
        ValidateNotBlank(context.PlanningLineage.RuntimeRequestRef, "microsoftRuntimeContext.planningLineage.runtimeRequestRef", missingFields);
        ValidateNotBlank(request.RequestMetadata.MicrosoftSkillsCatalogSchemaVersion, "microsoftRuntimeRequest.requestMetadata.microsoftSkillsCatalogSchemaVersion", missingFields);
        ValidateNotBlank(request.RequestMetadata.SkillProviderSelectionSchemaVersion, "microsoftRuntimeRequest.requestMetadata.skillProviderSelectionSchemaVersion", missingFields);
        ValidateNotBlank(request.Provenance.GenerationRequestRef, "microsoftRuntimeRequest.provenance.generationRequestRef", missingFields);
        ValidateNotBlank(request.Provenance.ExecutionPlanRef, "microsoftRuntimeRequest.provenance.executionPlanRef", missingFields);
        ValidateNotBlank(request.Provenance.CapabilityNegotiationRef, "microsoftRuntimeRequest.provenance.capabilityNegotiationRef", missingFields);
        ValidateNotBlank(request.Provenance.ExecutionProviderRef, "microsoftRuntimeRequest.provenance.executionProviderRef", missingFields);
        ValidateNotEmpty(request.CapabilityRequirements.RequiredCapabilities, "microsoftRuntimeRequest.capabilityRequirements.requiredCapabilities", missingSections);
        ValidateNotEmpty(request.SkillRequirements.RequiredSkillIds, "microsoftRuntimeRequest.skillRequirements.requiredSkillIds", missingSections);
        ValidateNotEmpty(request.SkillRequirements.CandidateProviderIds, "microsoftRuntimeRequest.skillRequirements.candidateProviderIds", missingSections);
        ValidateNotEmpty(request.Provenance.Lineage, "microsoftRuntimeRequest.provenance.lineage", missingSections);

        ValidateVersion(request.SchemaVersion, MicrosoftRuntimeRequestContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(context.SchemaVersion, MicrosoftRuntimeContextContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(request.RequestMetadata.RuntimeProviderRequestSchemaVersion, RuntimeProviderRequestContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(request.RequestMetadata.ExecutionCandidateSchemaVersion, RuntimeProviderContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(request.RequestMetadata.MicrosoftAdapterSpecificationSchemaVersion, MicrosoftAdapterSpecificationContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(request.RequestMetadata.MicrosoftSkillsCatalogSchemaVersion, MicrosoftSkillsCatalogContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(request.RequestMetadata.SkillProviderSelectionSchemaVersion, SkillProviderSelectionContract.SchemaVersionV1, versionMismatches);
        ValidateVersion(request.PlanningOutcomeReference.SchemaVersion, PlanningOutcomeContract.SchemaVersionV1, versionMismatches);

        if (!string.Equals(request.PlanningOutcomeReference.OutcomeId, planning.Outcome.Metadata.OutcomeId, StringComparison.Ordinal))
        {
            provenanceFailures.Add("microsoftRuntimeRequest.planningOutcomeReference.outcomeId must match planningOutcome.metadata.outcomeId.");
        }

        if (!string.Equals(context.PlanningLineage.RuntimeRequestRef, request.RequestId, StringComparison.Ordinal))
        {
            provenanceFailures.Add("microsoftRuntimeContext.planningLineage.runtimeRequestRef must match microsoftRuntimeRequest.requestId.");
        }

        var targetSupport = definition.SupportedTargetProfiles
            .FirstOrDefault(profile => string.Equals(profile.TargetProfileId, request.TargetProfile.TargetProfileId, StringComparison.Ordinal));
        if (targetSupport is null)
        {
            unsupportedTargetProfiles.Add(request.TargetProfile.TargetProfileId);
        }
        else if (targetSupport.SupportStatus == MicrosoftRuntimeSupportStatus.Unsupported)
        {
            unsupportedTargetProfiles.Add(request.TargetProfile.TargetProfileId);
        }
        else if (targetSupport.SupportStatus == MicrosoftRuntimeSupportStatus.Planned)
        {
            plannedTargetProfiles.Add(request.TargetProfile.TargetProfileId);
        }

        var capabilitySupport = definition.SupportedCapabilities
            .ToDictionary(capability => capability.CapabilityId, capability => capability, StringComparer.Ordinal);
        var allowedCapabilities = targetSupport?.RequiredCapabilities.ToHashSet(StringComparer.Ordinal) ?? [];
        var isPlannedTarget = plannedTargetProfiles.Count > 0;

        foreach (var capability in request.CapabilityRequirements.RequiredCapabilities.Distinct(StringComparer.Ordinal))
        {
            if (!capabilitySupport.TryGetValue(capability, out var supportedCapability))
            {
                incompatibleCapabilities.Add(capability);
                continue;
            }

            if (supportedCapability.SupportStatus == MicrosoftRuntimeSupportStatus.Unsupported)
            {
                incompatibleCapabilities.Add(capability);
                continue;
            }

            if (!isPlannedTarget && !allowedCapabilities.Contains(capability))
            {
                incompatibleCapabilities.Add(capability);
            }
        }

        if ((planning.Outcome.Status is PlanningOutcomeStatus.PlanningBlocked or PlanningOutcomeStatus.PlanningFailed) &&
            plannedTargetProfiles.Count == 0)
        {
            blockingFailures.AddRange(planning.Outcome.ReadinessSummary.BlockingIssues);
            if (blockingFailures.Count == 0)
            {
                blockingFailures.Add(planning.Outcome.Status.ToString());
            }
        }

        if (!request.ReviewRequirements.DesignApprovalRequired ||
            !request.ReviewRequirements.GenerationApprovalRequired ||
            !request.ReviewRequirements.AnalyzerValidationRequired)
        {
            approvalFailures.Add("microsoft runtime review requirements must preserve design approval, generation approval, and analyzer validation requirements.");
        }

        if (request.SkillRequirements.Readiness != MicrosoftSkillReadinessState.ReadyForSkillProvider)
        {
            blockingFailures.Add(request.SkillRequirements.Readiness.ToString());
        }

        if (request.SkillRequirements.SkillProviderReadiness != MicrosoftSkillProviderReadinessState.ReadyForSkillProviderAdapter)
        {
            blockingFailures.Add(request.SkillRequirements.SkillProviderReadiness.ToString());
        }

        if (request.ReviewRequirements.GenerationApproved && !request.ReviewRequirements.DesignApproved)
        {
            approvalFailures.Add("generation approval cannot be satisfied before design approval.");
        }

        return new MicrosoftRuntimeProviderValidationResult(
            new MicrosoftRuntimeValidationDiagnostics(
                MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
                MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
                UnsupportedTargetProfiles: unsupportedTargetProfiles.Distinct(StringComparer.Ordinal).ToArray(),
                PlannedTargetProfiles: plannedTargetProfiles.Distinct(StringComparer.Ordinal).ToArray(),
                IncompatibleCapabilities: incompatibleCapabilities.Distinct(StringComparer.Ordinal).ToArray(),
                ApprovalFailures: approvalFailures.Distinct(StringComparer.Ordinal).ToArray(),
                ProvenanceFailures: provenanceFailures.Distinct(StringComparer.Ordinal).ToArray(),
                VersionMismatches: versionMismatches.Distinct(StringComparer.Ordinal).ToArray(),
                BlockingFailures: blockingFailures.Distinct(StringComparer.Ordinal).ToArray()));
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

    private static void ValidateNotEmpty<T>(IReadOnlyCollection<T>? values, string fieldName, ICollection<string> missingSections)
    {
        if (values is null || values.Count == 0)
        {
            missingSections.Add(fieldName);
        }
    }
}
