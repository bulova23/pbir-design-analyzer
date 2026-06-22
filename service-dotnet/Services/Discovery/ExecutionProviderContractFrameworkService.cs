using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ExecutionProviderContractFrameworkService
{
    private readonly ExecutionProviderValidator _validator;
    private readonly ExecutionEligibilityService _eligibilityService;

    internal ExecutionProviderContractFrameworkService()
        : this(new ExecutionProviderValidator(), new ExecutionEligibilityService())
    {
    }

    internal ExecutionProviderContractFrameworkService(
        ExecutionProviderValidator validator,
        ExecutionEligibilityService eligibilityService)
    {
        _validator = validator;
        _eligibilityService = eligibilityService;
    }

    internal ExecutionProviderDefinition CreateDefaultProviderDefinition()
    {
        return new ExecutionProviderDefinition(
            ProviderId: "microsoft.contract.execution-provider",
            ProviderName: "Microsoft Contract-Only Execution Provider",
            ProviderVersion: "1.0.0",
            ProviderCategory: MicrosoftAdapterSpecificationContract.ProviderCategory,
            SupportedCapabilities: ["layoutGeneration", "semanticGeneration"],
            SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile],
            SupportedExecutionModes: [ExecutionProviderMode.Manual, ExecutionProviderMode.Assisted, ExecutionProviderMode.Automated],
            SupportedGenerationRequestSchemaVersions: [GenerationRequestContract.SchemaVersionV1],
            SupportedExecutionPlanSchemaVersions: [ExecutionPlanContract.SchemaVersionV1],
            SupportedCapabilityNegotiationSchemaVersions: [CapabilityNegotiationContract.SchemaVersionV1]);
    }

    internal ExecutionProviderValidationResult ValidateProviderDefinition(ExecutionProviderDefinition providerDefinition)
    {
        return _validator.ValidateProviderDefinition(providerDefinition);
    }

    internal ExecutionProviderRequest BuildProviderRequest(
        ExecutionProviderDefinition providerDefinition,
        GenerationRequest generationRequest,
        ExecutionPlan executionPlan,
        CapabilityNegotiationResult negotiationResult,
        ExecutionApprovalPolicy approvalPolicy,
        ExecutionProviderMode executionMode)
    {
        ArgumentNullException.ThrowIfNull(providerDefinition);
        ArgumentNullException.ThrowIfNull(generationRequest);
        ArgumentNullException.ThrowIfNull(executionPlan);
        ArgumentNullException.ThrowIfNull(negotiationResult);
        ArgumentNullException.ThrowIfNull(approvalPolicy);

        return new ExecutionProviderRequest(
            SchemaVersion: ExecutionProviderContract.SchemaVersionV1,
            RequestId: $"execprov:{providerDefinition.ProviderId}:{executionPlan.ExecutionPlanId}",
            GenerationRequestRef: generationRequest.RequestId,
            ExecutionPlanRef: executionPlan.ExecutionPlanId,
            NegotiationResultRef: negotiationResult.NegotiationId,
            SourceContractVersions: new ExecutionProviderSourceContractVersions(
                GenerationRequestSchemaVersion: generationRequest.SchemaVersion,
                ExecutionPlanSchemaVersion: executionPlan.SchemaVersion,
                CapabilityNegotiationSchemaVersion: negotiationResult.SchemaVersion),
            ReviewRequirements: executionPlan.ReviewRequirements,
            SuccessContract: generationRequest.SuccessContract,
            ExecutionConstraints: new ExecutionProviderConstraintSet(
                RequiredCapabilities: negotiationResult.Resolutions
                    .Where(resolution =>
                        resolution.RequirementLevel == CapabilityRequirementLevel.Required &&
                        !string.IsNullOrWhiteSpace(resolution.ResolvedCapabilityId))
                    .Select(resolution => resolution.ResolvedCapabilityId!)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(capability => capability, StringComparer.Ordinal)
                    .ToArray(),
                RequiredTargetProfileId: generationRequest.TargetArtifactProfile.ProfileId,
                RequiredProviderCategory: negotiationResult.ProviderCategory),
            RequestedExecutionMode: executionMode,
            ApprovalPolicy: approvalPolicy);
    }

    internal ExecutionProviderFrameworkState EvaluateProvider(
        ExecutionProviderDefinition providerDefinition,
        GenerationRequest generationRequest,
        ExecutionPlan executionPlan,
        CapabilityNegotiationResult negotiationResult,
        ExecutionApprovalPolicy approvalPolicy,
        ExecutionProviderMode executionMode)
    {
        ArgumentNullException.ThrowIfNull(providerDefinition);
        ArgumentNullException.ThrowIfNull(generationRequest);
        ArgumentNullException.ThrowIfNull(executionPlan);
        ArgumentNullException.ThrowIfNull(negotiationResult);
        ArgumentNullException.ThrowIfNull(approvalPolicy);

        var validation = _validator.ValidateProviderDefinition(providerDefinition);
        var providerRequest = BuildProviderRequest(
            providerDefinition,
            generationRequest,
            executionPlan,
            negotiationResult,
            approvalPolicy,
            executionMode);

        var evaluation = Merge(validation.Diagnostics, _eligibilityService.Evaluate(
            providerDefinition,
            providerRequest,
            generationRequest,
            executionPlan,
            negotiationResult).Diagnostics);
        var eligibility = DetermineEligibility(validation, evaluation);
        var readiness = ToReadiness(eligibility);
        var response = new ExecutionProviderResponse(
            ProviderId: providerDefinition.ProviderId,
            RequestId: providerRequest.RequestId,
            Status: ToResponseStatus(eligibility, evaluation),
            Eligibility: eligibility,
            ReadinessStatus: readiness,
            Reasons: CollectReasons(evaluation));
        var auditRecord = new ExecutionAuditRecord(
            ExecutionRequestLineage: new ExecutionRequestLineage(
                GenerationRequestRef: generationRequest.RequestId,
                ExecutionPlanRef: executionPlan.ExecutionPlanId,
                ProviderRequestRef: providerRequest.RequestId),
            NegotiationLineage: new ExecutionNegotiationLineage(
                NegotiationResultRef: negotiationResult.NegotiationId,
                NegotiationSchemaVersion: negotiationResult.SchemaVersion),
            ProviderLineage: new ExecutionProviderLineage(
                ProviderId: providerDefinition.ProviderId,
                ProviderVersion: providerDefinition.ProviderVersion,
                ProviderCategory: providerDefinition.ProviderCategory),
            ApprovalLineage: new ExecutionApprovalLineage(
                DesignApprovalRequired: approvalPolicy.DesignApprovalRequired,
                GenerationApprovalRequired: approvalPolicy.GenerationApprovalRequired,
                AnalyzerValidationRequired: approvalPolicy.AnalyzerValidationRequired,
                DesignApproved: approvalPolicy.DesignApproved,
                GenerationApproved: approvalPolicy.GenerationApproved));

        return new ExecutionProviderFrameworkState(
            GenerationRequest: generationRequest,
            ExecutionPlan: executionPlan,
            NegotiationResult: negotiationResult,
            ProviderDefinition: providerDefinition,
            ProviderRequest: providerRequest,
            ProviderResponse: response,
            ApprovalPolicy: approvalPolicy,
            AuditRecord: auditRecord,
            Eligibility: eligibility,
            Readiness: readiness,
            Diagnostics: evaluation);
    }

    internal ExecutionProviderFrameworkState PrepareForExecutionProvider(ExecutionProviderFrameworkState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Eligibility == ExecutionEligibilityStatus.Eligible &&
            state.ProviderResponse is not null
            ? state with
            {
                Readiness = ExecutionProviderReadinessState.ApprovedForExecutionProvider,
                ProviderResponse = state.ProviderResponse with
                {
                    ReadinessStatus = ExecutionProviderReadinessState.ApprovedForExecutionProvider,
                }
            }
            : state;
    }

    private static ExecutionEligibilityStatus DetermineEligibility(
        ExecutionProviderValidationResult validation,
        ExecutionProviderDiagnostics diagnostics)
    {
        if (!validation.IsValid ||
            diagnostics.InvalidLineage.Count > 0 ||
            diagnostics.InvalidApprovalChains.Count > 0 ||
            diagnostics.VersionMismatches.Count > 0)
        {
            return ExecutionEligibilityStatus.Blocked;
        }

        if (diagnostics.UnsupportedProviderDefinitions.Count > 0 ||
            diagnostics.IncompatibleExecutionModes.Count > 0 ||
            diagnostics.CapabilityRequirementFailures.Count > 0)
        {
            return ExecutionEligibilityStatus.Ineligible;
        }

        if (diagnostics.ApprovalRequirementFailures.Count > 0 ||
            diagnostics.ReadinessRequirementFailures.Count > 0)
        {
            return ExecutionEligibilityStatus.ConditionallyEligible;
        }

        return ExecutionEligibilityStatus.Eligible;
    }

    private static ExecutionProviderReadinessState ToReadiness(ExecutionEligibilityStatus eligibility)
    {
        return eligibility switch
        {
            ExecutionEligibilityStatus.Eligible => ExecutionProviderReadinessState.Eligible,
            ExecutionEligibilityStatus.ConditionallyEligible => ExecutionProviderReadinessState.ConditionallyEligible,
            _ => ExecutionProviderReadinessState.NotEligible,
        };
    }

    private static ExecutionProviderResponseStatus ToResponseStatus(
        ExecutionEligibilityStatus eligibility,
        ExecutionProviderDiagnostics diagnostics)
    {
        return eligibility switch
        {
            ExecutionEligibilityStatus.Eligible => ExecutionProviderResponseStatus.Accepted,
            ExecutionEligibilityStatus.Blocked => ExecutionProviderResponseStatus.Blocked,
            ExecutionEligibilityStatus.Ineligible when diagnostics.UnsupportedProviderDefinitions.Count > 0 ||
                diagnostics.IncompatibleExecutionModes.Count > 0 ||
                diagnostics.CapabilityRequirementFailures.Count > 0 => ExecutionProviderResponseStatus.Unsupported,
            _ => ExecutionProviderResponseStatus.Rejected,
        };
    }

    private static IReadOnlyList<string> CollectReasons(ExecutionProviderDiagnostics diagnostics)
    {
        return diagnostics.MissingRequiredSections
            .Concat(diagnostics.MissingRequiredFields)
            .Concat(diagnostics.InvalidLineage)
            .Concat(diagnostics.InvalidApprovalChains)
            .Concat(diagnostics.UnsupportedProviderDefinitions)
            .Concat(diagnostics.IncompatibleExecutionModes)
            .Concat(diagnostics.VersionMismatches)
            .Concat(diagnostics.CapabilityRequirementFailures)
            .Concat(diagnostics.ReadinessRequirementFailures)
            .Concat(diagnostics.ApprovalRequirementFailures)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reason => reason, StringComparer.Ordinal)
            .ToArray();
    }

    private static ExecutionProviderDiagnostics Merge(
        ExecutionProviderDiagnostics left,
        ExecutionProviderDiagnostics right)
    {
        return new ExecutionProviderDiagnostics(
            MissingRequiredSections: left.MissingRequiredSections.Concat(right.MissingRequiredSections).Distinct(StringComparer.Ordinal).ToArray(),
            MissingRequiredFields: left.MissingRequiredFields.Concat(right.MissingRequiredFields).Distinct(StringComparer.Ordinal).ToArray(),
            InvalidLineage: left.InvalidLineage.Concat(right.InvalidLineage).Distinct(StringComparer.Ordinal).ToArray(),
            InvalidApprovalChains: left.InvalidApprovalChains.Concat(right.InvalidApprovalChains).Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedProviderDefinitions: left.UnsupportedProviderDefinitions.Concat(right.UnsupportedProviderDefinitions).Distinct(StringComparer.Ordinal).ToArray(),
            IncompatibleExecutionModes: left.IncompatibleExecutionModes.Concat(right.IncompatibleExecutionModes).Distinct(StringComparer.Ordinal).ToArray(),
            VersionMismatches: left.VersionMismatches.Concat(right.VersionMismatches).Distinct(StringComparer.Ordinal).ToArray(),
            CapabilityRequirementFailures: left.CapabilityRequirementFailures.Concat(right.CapabilityRequirementFailures).Distinct(StringComparer.Ordinal).ToArray(),
            ReadinessRequirementFailures: left.ReadinessRequirementFailures.Concat(right.ReadinessRequirementFailures).Distinct(StringComparer.Ordinal).ToArray(),
            ApprovalRequirementFailures: left.ApprovalRequirementFailures.Concat(right.ApprovalRequirementFailures).Distinct(StringComparer.Ordinal).ToArray());
    }
}
