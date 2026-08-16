using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class RuntimeProviderAbstractionFrameworkService
{
    private readonly RuntimeProviderRegistry _registry;
    private readonly RuntimeProviderValidator _validator;
    private readonly RuntimeReadinessService _readinessService;

    internal RuntimeProviderAbstractionFrameworkService(RuntimeProviderRegistry registry)
        : this(registry, new RuntimeProviderValidator(), new RuntimeReadinessService())
    {
    }

    internal RuntimeProviderAbstractionFrameworkService(
        RuntimeProviderRegistry registry,
        RuntimeProviderValidator validator,
        RuntimeReadinessService readinessService)
    {
        _registry = registry;
        _validator = validator;
        _readinessService = readinessService;
    }

    internal RuntimeProviderRegistration CreateDefaultRegistration(
        ExecutionProviderDefinition providerDefinition,
        ExecutionProviderRequest providerRequest)
    {
        ArgumentNullException.ThrowIfNull(providerDefinition);
        ArgumentNullException.ThrowIfNull(providerRequest);

        return new RuntimeProviderRegistration(
            ProviderId: $"runtime-provider:{providerDefinition.ProviderId}",
            ProviderName: providerDefinition.ProviderName,
            ProviderVersion: providerDefinition.ProviderVersion,
            ProviderCategory: providerDefinition.ProviderCategory,
            ExecutionProviderRef: providerRequest.RequestId,
            SupportedRequestSchemaVersions: [RuntimeProviderRequestContract.SchemaVersionV1],
            SupportedContextSchemaVersions: [RuntimeProviderContextContract.SchemaVersionV1],
            SupportedResultSchemaVersions: [RuntimeProviderResultContract.SchemaVersionV1],
            SupportedTargetProfiles: providerDefinition.SupportedTargetProfiles
                .Distinct(StringComparer.Ordinal)
                .OrderBy(profile => profile, StringComparer.Ordinal)
                .ToArray(),
            SupportedCapabilities: providerDefinition.SupportedCapabilities
                .Distinct(StringComparer.Ordinal)
                .OrderBy(capability => capability, StringComparer.Ordinal)
                .ToArray());
    }

    internal RuntimeProviderRequest BuildRequest(PlanningOrchestrationResult planning)
    {
        ArgumentNullException.ThrowIfNull(planning);

        return new RuntimeProviderRequest(
            SchemaVersion: RuntimeProviderRequestContract.SchemaVersionV1,
            RequestId: $"runtimeRequest:{planning.Outcome.Metadata.OutcomeId}",
            PlanningOutcomeRef: planning.Outcome.Metadata.OutcomeId,
            ExecutionProviderRef: planning.Outcome.References.ExecutionProviderRef,
            ExecutionPlanRef: planning.Outcome.References.ExecutionPlanRef,
            CapabilityResolutionRef: planning.Outcome.References.NegotiationRef,
            SourceContractVersions: new RuntimeProviderSourceContractVersions(
                PlanningOutcomeSchemaVersion: planning.Outcome.Metadata.SchemaVersion,
                ExecutionProviderSchemaVersion: planning.ExecutionProviderState?.ProviderRequest?.SchemaVersion ?? ExecutionProviderContract.SchemaVersionV1,
                ExecutionPlanSchemaVersion: planning.ExecutionPlanState.Plan?.SchemaVersion ?? ExecutionPlanContract.SchemaVersionV1,
                CapabilityResolutionSchemaVersion: planning.CapabilityNegotiationState?.Result?.SchemaVersion ?? CapabilityNegotiationContract.SchemaVersionV1),
            ApprovalState: new RuntimeProviderApprovalState(
                DesignApprovalRequired: planning.Outcome.ReadinessSummary.ApprovalStatus.DesignApprovalRequired,
                GenerationApprovalRequired: planning.Outcome.ReadinessSummary.ApprovalStatus.GenerationApprovalRequired,
                AnalyzerValidationRequired: planning.Outcome.ReadinessSummary.ApprovalStatus.AnalyzerValidationRequired,
                DesignApproved: planning.Outcome.ReadinessSummary.ApprovalStatus.DesignApproved,
                GenerationApproved: planning.Outcome.ReadinessSummary.ApprovalStatus.GenerationApproved),
            ExecutionConstraints: new RuntimeProviderExecutionConstraints(
                RequiredCapabilities: planning.ExecutionProviderState?.ProviderRequest?.ExecutionConstraints.RequiredCapabilities ?? [],
                UnresolvedCapabilities: planning.Outcome.ReadinessSummary.CapabilitySummary.UnresolvedCapabilities,
                RequiredTargetProfileId: planning.GenerationRequestState.Request?.TargetArtifactProfile.ProfileId ?? string.Empty,
                RequiredProviderCategory: planning.ExecutionProviderState?.ProviderDefinition?.ProviderCategory ?? string.Empty));
    }

    internal RuntimeProviderContext CreateExecutionContext(PlanningOrchestrationResult planning, RuntimeProviderRequest request)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(request);

        return new RuntimeProviderContext(
            SchemaVersion: RuntimeProviderContextContract.SchemaVersionV1,
            ContextId: $"runtimeContext:{request.RequestId}",
            ExecutionLineage: new RuntimeExecutionLineage(
                RequestRef: request.RequestId,
                PlanningOutcomeRef: request.PlanningOutcomeRef,
                ExecutionProviderRef: request.ExecutionProviderRef,
                ExecutionPlanRef: request.ExecutionPlanRef,
                CapabilityResolutionRef: request.CapabilityResolutionRef),
            PlanningLineage: new RuntimeProviderContextLineage(
                UpstreamLineage: planning.Outcome.Lineage.UpstreamLineage,
                PlanningLineage: planning.Outcome.Lineage.PlanningLineage),
            ApprovalLineage: request.ApprovalState,
            TargetProfileId: planning.GenerationRequestState.Request?.TargetArtifactProfile.ProfileId ?? string.Empty,
            ProviderCategory: planning.ExecutionProviderState?.ProviderDefinition?.ProviderCategory ?? string.Empty);
    }

    internal RuntimeProviderValidationResult ValidateRequest(
        PlanningOrchestrationResult planning,
        string providerId,
        RuntimeProviderRequest request,
        RuntimeProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        return _validator.Validate(planning, request, context);
    }

    internal RuntimeProviderResult EvaluateReadiness(
        PlanningOrchestrationResult planning,
        RuntimeProviderRegistration? registration,
        RuntimeProviderValidationResult validation,
        RuntimeProviderRequest request,
        RuntimeProviderContext context)
    {
        return _readinessService.Evaluate(planning, registration, validation, request, context);
    }

    internal RuntimeProviderFrameworkState CreateRuntimeCandidate(
        PlanningOrchestrationResult planning,
        string providerId)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        _registry.TryGetProvider(providerId, out var registration);
        var request = BuildRequest(planning);
        var context = CreateExecutionContext(planning, request);
        var validation = ValidateRequest(planning, providerId, request, context);
        var result = EvaluateReadiness(planning, registration, validation, request, context);
        var candidate = result.ReadinessStatus is RuntimeProviderReadinessState.Candidate or RuntimeProviderReadinessState.ReadyForRuntimeProvider
            ? new RuntimeExecutionCandidate(
                SchemaVersion: RuntimeProviderContract.SchemaVersionV1,
                CandidateId: $"executionCandidate:{request.RequestId}",
                RequestRef: request.RequestId,
                ContextRef: context.ContextId,
                ResultRef: result.ResultId,
                ReadinessStatus: result.ReadinessStatus)
            : null;

        return new RuntimeProviderFrameworkState(
            PlanningOutcome: planning.Outcome,
            ExecutionProviderState: planning.ExecutionProviderState,
            Registration: registration,
            Request: request,
            Context: context,
            Result: result,
            ExecutionCandidate: candidate,
            Readiness: result.ReadinessStatus,
            Validation: validation);
    }
}
