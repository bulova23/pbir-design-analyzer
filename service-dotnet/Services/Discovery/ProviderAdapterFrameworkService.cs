using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ProviderAdapterFrameworkService
{
    private readonly ProviderAdapterRegistry _registry;
    private readonly ProviderAdapterCompatibilityService _compatibilityService;
    private readonly GenerationRequestValidator _generationRequestValidator;
    private readonly ExecutionPlanValidator _executionPlanValidator;

    internal ProviderAdapterFrameworkService(
        ProviderAdapterRegistry registry,
        ProviderAdapterCompatibilityService compatibilityService)
        : this(registry, compatibilityService, new GenerationRequestValidator(), new ExecutionPlanValidator())
    {
    }

    internal ProviderAdapterFrameworkService(
        ProviderAdapterRegistry registry,
        ProviderAdapterCompatibilityService compatibilityService,
        GenerationRequestValidator generationRequestValidator,
        ExecutionPlanValidator executionPlanValidator)
    {
        _registry = registry;
        _compatibilityService = compatibilityService;
        _generationRequestValidator = generationRequestValidator;
        _executionPlanValidator = executionPlanValidator;
    }

    internal ProviderAdapterRequestCreationResult BuildAdapterRequest(
        GenerationRequest generationRequest,
        ExecutionPlan executionPlan,
        string schemaVersion = ProviderAdapterContract.SchemaVersionV1)
    {
        ArgumentNullException.ThrowIfNull(generationRequest);
        ArgumentNullException.ThrowIfNull(executionPlan);

        var requestValidation = _generationRequestValidator.Validate(generationRequest);
        var planValidation = _executionPlanValidator.Validate(executionPlan);
        var missingRequiredSections = requestValidation.Diagnostics.MissingRequiredSections
            .Concat(planValidation.Diagnostics.MissingRequiredSections)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var missingRequiredFields = requestValidation.Diagnostics.MissingRequiredFields
            .Concat(planValidation.Diagnostics.MissingRequiredFields)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (missingRequiredSections.Length > 0 || missingRequiredFields.Length > 0)
        {
            return new ProviderAdapterRequestCreationResult(
                Request: null,
                Diagnostics: new ProviderAdapterCompatibilityDiagnostics(
                    MissingRequiredSections: missingRequiredSections,
                    MissingRequiredFields: missingRequiredFields,
                    TargetCompatibilityFailures: [],
                    CapabilityCompatibilityFailures: [],
                    ExecutionPlanCompatibilityFailures: [],
                    VersionCompatibilityFailures: []));
        }

        var request = new ProviderAdapterRequest(
            SchemaVersion: schemaVersion,
            ExecutionPlanRef: executionPlan.ExecutionPlanId,
            GenerationRequestRef: generationRequest.RequestId,
            SourceContractVersions: new ProviderAdapterSourceContractVersions(
                GenerationRequestSchemaVersion: generationRequest.SchemaVersion,
                ExecutionPlanSchemaVersion: executionPlan.SchemaVersion),
            TargetArtifactProfile: generationRequest.TargetArtifactProfile,
            CapabilityRequirements: executionPlan.ProviderPlanningMetadata.SupportedCapabilities,
            Constraints: new ProviderAdapterConstraintSet(
                UnsupportedTargets: executionPlan.PlanningConstraints.UnsupportedTargets,
                UnsupportedCapabilities: executionPlan.PlanningConstraints.UnsupportedCapabilities,
                ReviewRequirements: executionPlan.PlanningConstraints.ReviewRequirements,
                ValidationRequirements: executionPlan.PlanningConstraints.ValidationRequirements),
            ReviewRequirements: executionPlan.ReviewRequirements,
            SuccessContract: generationRequest.SuccessContract);

        return new ProviderAdapterRequestCreationResult(request, ProviderAdapterCompatibilityDiagnostics.Empty);
    }

    internal ProviderAdapterFrameworkState DiscoverAdapter(
        string adapterId,
        ProviderAdapterRequest adapterRequest,
        GenerationRequest generationRequest,
        ExecutionPlan executionPlan)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);
        ArgumentNullException.ThrowIfNull(adapterRequest);
        ArgumentNullException.ThrowIfNull(generationRequest);
        ArgumentNullException.ThrowIfNull(executionPlan);

        var adapterDefinition = _registry.Discover(adapterId);
        return adapterDefinition is null
            ? new ProviderAdapterFrameworkState(
                GenerationRequest: generationRequest,
                ExecutionPlan: executionPlan,
                AdapterRequest: adapterRequest,
                AdapterDefinition: null,
                PlanningResponse: new ProviderAdapterPlanningResponse(
                    AdapterId: adapterId,
                    Status: ProviderAdapterPlanningResponseStatus.Rejected,
                    Compatibility: new ProviderAdapterCompatibilityEvaluation(
                        ProviderAdapterCompatibilityStatus.Incompatible,
                        new ProviderAdapterCompatibilityDiagnostics(
                            MissingRequiredSections: [],
                            MissingRequiredFields: [],
                            TargetCompatibilityFailures: [],
                            CapabilityCompatibilityFailures: [],
                            ExecutionPlanCompatibilityFailures: [$"adapterRegistry does not contain adapter {adapterId}."],
                            VersionCompatibilityFailures: []))),
                Readiness: ProviderAdapterPlanningReadinessState.Incompatible,
                Diagnostics: new ProviderAdapterCompatibilityDiagnostics(
                    MissingRequiredSections: [],
                    MissingRequiredFields: [],
                    TargetCompatibilityFailures: [],
                    CapabilityCompatibilityFailures: [],
                    ExecutionPlanCompatibilityFailures: [$"adapterRegistry does not contain adapter {adapterId}."],
                    VersionCompatibilityFailures: []))
            : new ProviderAdapterFrameworkState(
                GenerationRequest: generationRequest,
                ExecutionPlan: executionPlan,
                AdapterRequest: adapterRequest,
                AdapterDefinition: adapterDefinition,
                PlanningResponse: null,
                Readiness: ProviderAdapterPlanningReadinessState.Discovered,
                Diagnostics: ProviderAdapterCompatibilityDiagnostics.Empty);
    }

    internal ProviderAdapterFrameworkState EvaluateCompatibility(ProviderAdapterFrameworkState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.AdapterDefinition is null || state.AdapterRequest is null || state.GenerationRequest is null || state.ExecutionPlan is null)
        {
            return state with
            {
                Readiness = ProviderAdapterPlanningReadinessState.Incompatible,
                PlanningResponse = state.PlanningResponse ?? new ProviderAdapterPlanningResponse(
                    AdapterId: state.AdapterDefinition?.AdapterId ?? "unknown",
                    Status: ProviderAdapterPlanningResponseStatus.Rejected,
                    Compatibility: new ProviderAdapterCompatibilityEvaluation(
                        ProviderAdapterCompatibilityStatus.Incompatible,
                        state.Diagnostics)),
            };
        }

        var evaluation = _compatibilityService.Evaluate(
            state.AdapterDefinition,
            state.AdapterRequest,
            state.ExecutionPlan,
            state.GenerationRequest);

        return state with
        {
            Diagnostics = evaluation.Diagnostics,
            PlanningResponse = new ProviderAdapterPlanningResponse(
                AdapterId: state.AdapterDefinition.AdapterId,
                Status: ToPlanningResponseStatus(evaluation.Status),
                Compatibility: evaluation),
            Readiness = evaluation.Status switch
            {
                ProviderAdapterCompatibilityStatus.Compatible => ProviderAdapterPlanningReadinessState.Compatible,
                ProviderAdapterCompatibilityStatus.Unsupported => ProviderAdapterPlanningReadinessState.Unsupported,
                _ => ProviderAdapterPlanningReadinessState.Incompatible,
            }
        };
    }

    internal ProviderAdapterFrameworkState PrepareForExecutionProvider(ProviderAdapterFrameworkState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Readiness == ProviderAdapterPlanningReadinessState.Compatible
            ? state with
            {
                Readiness = ProviderAdapterPlanningReadinessState.ReadyForExecutionProvider,
            }
            : state;
    }

    internal ProviderAdapterFrameworkState EvaluateAdapter(
        string adapterId,
        GenerationRequest generationRequest,
        ExecutionPlan executionPlan,
        string schemaVersion = ProviderAdapterContract.SchemaVersionV1)
    {
        var requestResult = BuildAdapterRequest(generationRequest, executionPlan, schemaVersion);
        if (requestResult.Request is null)
        {
            return new ProviderAdapterFrameworkState(
                GenerationRequest: generationRequest,
                ExecutionPlan: executionPlan,
                AdapterRequest: null,
                AdapterDefinition: null,
                PlanningResponse: new ProviderAdapterPlanningResponse(
                    AdapterId: adapterId,
                    Status: ProviderAdapterPlanningResponseStatus.Rejected,
                    Compatibility: new ProviderAdapterCompatibilityEvaluation(
                        ProviderAdapterCompatibilityStatus.Incompatible,
                        requestResult.Diagnostics)),
                Readiness: ProviderAdapterPlanningReadinessState.Incompatible,
                Diagnostics: requestResult.Diagnostics);
        }

        var discovered = DiscoverAdapter(adapterId, requestResult.Request, generationRequest, executionPlan);
        var evaluated = EvaluateCompatibility(discovered);
        return PrepareForExecutionProvider(evaluated);
    }

    private static ProviderAdapterPlanningResponseStatus ToPlanningResponseStatus(ProviderAdapterCompatibilityStatus status)
    {
        return status switch
        {
            ProviderAdapterCompatibilityStatus.Compatible => ProviderAdapterPlanningResponseStatus.Accepted,
            ProviderAdapterCompatibilityStatus.Unsupported => ProviderAdapterPlanningResponseStatus.Unsupported,
            _ => ProviderAdapterPlanningResponseStatus.Incompatible,
        };
    }
}
