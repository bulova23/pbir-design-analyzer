using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ExecutionEligibilityService
{
    internal ExecutionEligibilityEvaluation Evaluate(
        ExecutionProviderDefinition providerDefinition,
        ExecutionProviderRequest providerRequest,
        GenerationRequest generationRequest,
        ExecutionPlan executionPlan,
        CapabilityNegotiationResult negotiationResult)
    {
        ArgumentNullException.ThrowIfNull(providerDefinition);
        ArgumentNullException.ThrowIfNull(providerRequest);
        ArgumentNullException.ThrowIfNull(generationRequest);
        ArgumentNullException.ThrowIfNull(executionPlan);
        ArgumentNullException.ThrowIfNull(negotiationResult);

        var invalidLineage = new List<string>();
        var invalidApprovalChains = new List<string>();
        var unsupportedProviderDefinitions = new List<string>();
        var incompatibleExecutionModes = new List<string>();
        var versionMismatches = new List<string>();
        var capabilityRequirementFailures = new List<string>();
        var readinessRequirementFailures = new List<string>();
        var approvalRequirementFailures = new List<string>();

        if (!string.Equals(providerRequest.GenerationRequestRef, generationRequest.RequestId, StringComparison.Ordinal))
        {
            invalidLineage.Add("providerRequest.generationRequestRef must match generationRequest.requestId.");
        }

        if (!string.Equals(providerRequest.ExecutionPlanRef, executionPlan.ExecutionPlanId, StringComparison.Ordinal))
        {
            invalidLineage.Add("providerRequest.executionPlanRef must match executionPlan.executionPlanId.");
        }

        if (!string.Equals(providerRequest.NegotiationResultRef, negotiationResult.NegotiationId, StringComparison.Ordinal))
        {
            invalidLineage.Add("providerRequest.negotiationResultRef must match capabilityNegotiation.negotiationId.");
        }

        if (!string.Equals(executionPlan.SourceReferences.GenerationRequestRef, generationRequest.RequestId, StringComparison.Ordinal))
        {
            invalidLineage.Add("executionPlan.sourceReferences.generationRequestRef must match generationRequest.requestId.");
        }

        if (!string.Equals(negotiationResult.TargetProfileId, generationRequest.TargetArtifactProfile.ProfileId, StringComparison.Ordinal))
        {
            invalidLineage.Add("capabilityNegotiation.targetProfileId must match generationRequest.targetArtifactProfile.profileId.");
        }

        if (providerRequest.ApprovalPolicy.GenerationApproved && !providerRequest.ApprovalPolicy.DesignApproved)
        {
            invalidApprovalChains.Add("generation approval cannot be satisfied before design approval.");
        }

        if (providerRequest.ApprovalPolicy.DesignApprovalRequired != generationRequest.ReviewPolicy.DesignApprovalRequired ||
            providerRequest.ApprovalPolicy.DesignApprovalRequired != executionPlan.ReviewRequirements.DesignApprovalRequired)
        {
            invalidApprovalChains.Add("design approval requirement must be inherited without modification.");
        }

        if (providerRequest.ApprovalPolicy.GenerationApprovalRequired != generationRequest.ReviewPolicy.GenerationApprovalRequired ||
            providerRequest.ApprovalPolicy.GenerationApprovalRequired != executionPlan.ReviewRequirements.GenerationApprovalRequired)
        {
            invalidApprovalChains.Add("generation approval requirement must be inherited without modification.");
        }

        if (providerRequest.ApprovalPolicy.AnalyzerValidationRequired != generationRequest.ReviewPolicy.AnalyzerReviewRequired ||
            providerRequest.ApprovalPolicy.AnalyzerValidationRequired != executionPlan.ReviewRequirements.AnalyzerReviewRequired)
        {
            invalidApprovalChains.Add("analyzer validation requirement must be inherited without modification.");
        }

        if (!providerDefinition.SupportedTargetProfiles.Contains(providerRequest.ExecutionConstraints.RequiredTargetProfileId, StringComparer.Ordinal))
        {
            unsupportedProviderDefinitions.Add(providerRequest.ExecutionConstraints.RequiredTargetProfileId);
        }

        if (!string.Equals(providerDefinition.ProviderCategory, providerRequest.ExecutionConstraints.RequiredProviderCategory, StringComparison.Ordinal))
        {
            unsupportedProviderDefinitions.Add(providerDefinition.ProviderCategory);
        }

        if (!providerDefinition.SupportedExecutionModes.Contains(providerRequest.RequestedExecutionMode))
        {
            incompatibleExecutionModes.Add(providerRequest.RequestedExecutionMode.ToString());
        }

        foreach (var capability in providerRequest.ExecutionConstraints.RequiredCapabilities)
        {
            if (!providerDefinition.SupportedCapabilities.Contains(capability, StringComparer.Ordinal))
            {
                capabilityRequirementFailures.Add(capability);
            }
        }

        ValidateVersion(
            providerRequest.SchemaVersion,
            ExecutionProviderContract.SchemaVersionV1,
            versionMismatches);
        ValidateVersion(
            providerRequest.SourceContractVersions.GenerationRequestSchemaVersion,
            GenerationRequestContract.SchemaVersionV1,
            versionMismatches);
        ValidateVersion(
            providerRequest.SourceContractVersions.ExecutionPlanSchemaVersion,
            ExecutionPlanContract.SchemaVersionV1,
            versionMismatches);
        ValidateVersion(
            providerRequest.SourceContractVersions.CapabilityNegotiationSchemaVersion,
            CapabilityNegotiationContract.SchemaVersionV1,
            versionMismatches);

        ValidateSupportedVersion(
            providerDefinition.SupportedGenerationRequestSchemaVersions,
            providerRequest.SourceContractVersions.GenerationRequestSchemaVersion,
            versionMismatches);
        ValidateSupportedVersion(
            providerDefinition.SupportedExecutionPlanSchemaVersions,
            providerRequest.SourceContractVersions.ExecutionPlanSchemaVersion,
            versionMismatches);
        ValidateSupportedVersion(
            providerDefinition.SupportedCapabilityNegotiationSchemaVersions,
            providerRequest.SourceContractVersions.CapabilityNegotiationSchemaVersion,
            versionMismatches);

        if (negotiationResult.ReadinessStatus != CapabilityNegotiationReadinessState.ReadyForExecutionProvider)
        {
            readinessRequirementFailures.Add("capability negotiation must be ready for an execution provider.");
        }

        if (providerRequest.ApprovalPolicy.DesignApprovalRequired && !providerRequest.ApprovalPolicy.DesignApproved)
        {
            approvalRequirementFailures.Add("design approval has not been satisfied.");
        }

        if (providerRequest.ApprovalPolicy.GenerationApprovalRequired && !providerRequest.ApprovalPolicy.GenerationApproved)
        {
            approvalRequirementFailures.Add("generation approval has not been satisfied.");
        }

        var diagnostics = new ExecutionProviderDiagnostics(
            MissingRequiredSections: [],
            MissingRequiredFields: [],
            InvalidLineage: invalidLineage.Distinct(StringComparer.Ordinal).ToArray(),
            InvalidApprovalChains: invalidApprovalChains.Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedProviderDefinitions: unsupportedProviderDefinitions.Distinct(StringComparer.Ordinal).ToArray(),
            IncompatibleExecutionModes: incompatibleExecutionModes.Distinct(StringComparer.Ordinal).ToArray(),
            VersionMismatches: versionMismatches.Distinct(StringComparer.Ordinal).ToArray(),
            CapabilityRequirementFailures: capabilityRequirementFailures.Distinct(StringComparer.Ordinal).ToArray(),
            ReadinessRequirementFailures: readinessRequirementFailures.Distinct(StringComparer.Ordinal).ToArray(),
            ApprovalRequirementFailures: approvalRequirementFailures.Distinct(StringComparer.Ordinal).ToArray());

        return new ExecutionEligibilityEvaluation(GetStatus(diagnostics), diagnostics);
    }

    private static ExecutionEligibilityStatus GetStatus(ExecutionProviderDiagnostics diagnostics)
    {
        if (diagnostics.HasBlockingFailures)
        {
            return ExecutionEligibilityStatus.Blocked;
        }

        if (diagnostics.HasUnsupportedFailures)
        {
            return ExecutionEligibilityStatus.Ineligible;
        }

        if (diagnostics.HasConditionalFailures)
        {
            return ExecutionEligibilityStatus.ConditionallyEligible;
        }

        return ExecutionEligibilityStatus.Eligible;
    }

    private static void ValidateVersion(string candidate, string expected, ICollection<string> versionMismatches)
    {
        if (!string.Equals(candidate, expected, StringComparison.Ordinal))
        {
            versionMismatches.Add(candidate);
        }
    }

    private static void ValidateSupportedVersion(
        IReadOnlyCollection<string> supportedVersions,
        string candidate,
        ICollection<string> versionMismatches)
    {
        if (!supportedVersions.Contains(candidate, StringComparer.Ordinal))
        {
            versionMismatches.Add(candidate);
        }
    }
}
