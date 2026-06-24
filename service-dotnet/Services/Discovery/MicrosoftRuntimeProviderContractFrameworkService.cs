using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftRuntimeProviderContractFrameworkService
{
    private readonly RuntimeProviderRegistry _registry;
    private readonly MicrosoftRuntimeProviderValidator _validator;
    private readonly MicrosoftRuntimeReadinessService _readinessService;
    private readonly MicrosoftAdapterSpecificationService _specificationService;
    private readonly MicrosoftSkillsCapabilityCatalogFrameworkService _skillCatalogFrameworkService;
    private readonly MicrosoftSkillProviderAdapterFrameworkService _skillProviderAdapterFrameworkService;

    internal MicrosoftRuntimeProviderContractFrameworkService(RuntimeProviderRegistry registry)
        : this(
            registry,
            new MicrosoftRuntimeProviderValidator(),
            new MicrosoftRuntimeReadinessService(),
            new MicrosoftAdapterSpecificationService(),
            new MicrosoftSkillsCapabilityCatalogFrameworkService(),
            new MicrosoftSkillProviderAdapterFrameworkService())
    {
    }

    internal MicrosoftRuntimeProviderContractFrameworkService(
        RuntimeProviderRegistry registry,
        MicrosoftRuntimeProviderValidator validator,
        MicrosoftRuntimeReadinessService readinessService,
        MicrosoftAdapterSpecificationService specificationService,
        MicrosoftSkillsCapabilityCatalogFrameworkService skillCatalogFrameworkService,
        MicrosoftSkillProviderAdapterFrameworkService skillProviderAdapterFrameworkService)
    {
        _registry = registry;
        _validator = validator;
        _readinessService = readinessService;
        _specificationService = specificationService;
        _skillCatalogFrameworkService = skillCatalogFrameworkService;
        _skillProviderAdapterFrameworkService = skillProviderAdapterFrameworkService;
    }

    internal MicrosoftRuntimeProviderDefinition CreateDefaultProviderDefinition()
    {
        var specification = _specificationService.CreateDefaultSpecification();

        return new MicrosoftRuntimeProviderDefinition(
            SchemaVersion: MicrosoftRuntimeProviderContract.SchemaVersionV1,
            ProviderId: MicrosoftRuntimeProviderContract.ProviderId,
            ProviderName: MicrosoftRuntimeProviderContract.ProviderName,
            ProviderVersion: MicrosoftRuntimeProviderContract.ProviderVersion,
            ProviderCategory: specification.ProviderIdentity.ProviderCategory,
            SupportedTargetProfiles: specification.SupportedTargetProfiles
                .Select(profile => new MicrosoftRuntimeTargetProfileSupport(
                    TargetProfileId: profile.TargetProfileId,
                    ArtifactType: profile.ArtifactType,
                    SupportStatus: ToSupportStatus(profile.SupportStatus),
                    RequiredCapabilities: specification.TargetProfileMappings
                        .FirstOrDefault(mapping => string.Equals(mapping.TargetProfileId, profile.TargetProfileId, StringComparison.Ordinal))
                        ?.RequiredCapabilities
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(capability => capability, StringComparer.Ordinal)
                        .ToArray() ?? [],
                    Notes: profile.Notes))
                .OrderBy(profile => profile.TargetProfileId, StringComparer.Ordinal)
                .ToArray(),
            SupportedCapabilities: specification.CapabilityMappings
                .Select(capability => new MicrosoftRuntimeCapabilitySupport(
                    CapabilityId: capability.CapabilityId,
                    SupportStatus: ToSupportStatus(capability.SupportStatus),
                    ProviderCapabilityRequirements: capability.ProviderCapabilityRequirements
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(requirement => requirement, StringComparer.Ordinal)
                        .ToArray(),
                    Notes: capability.Notes))
                .OrderBy(capability => capability.CapabilityId, StringComparer.Ordinal)
                .ToArray(),
            SupportedExecutionModes: [ExecutionProviderMode.Manual, ExecutionProviderMode.Assisted, ExecutionProviderMode.Automated]);
    }

    internal RuntimeProviderRegistration CreateDefaultRegistration(
        MicrosoftRuntimeProviderDefinition definition,
        PlanningOrchestrationResult planning)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(planning);

        var registeredTargetProfiles = definition.SupportedTargetProfiles
            .Where(profile => profile.SupportStatus != MicrosoftRuntimeSupportStatus.Unsupported)
            .Select(profile => profile.TargetProfileId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(profile => profile, StringComparer.Ordinal)
            .ToArray();
        var registeredCapabilities = definition.SupportedCapabilities
            .Where(capability => capability.SupportStatus != MicrosoftRuntimeSupportStatus.Unsupported)
            .Select(capability => capability.CapabilityId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();

        return new RuntimeProviderRegistration(
            ProviderId: definition.ProviderId,
            ProviderName: definition.ProviderName,
            ProviderVersion: definition.ProviderVersion,
            ProviderCategory: definition.ProviderCategory,
            ExecutionProviderRef: planning.Outcome.References.ExecutionProviderRef,
            SupportedRequestSchemaVersions: [RuntimeProviderRequestContract.SchemaVersionV1],
            SupportedContextSchemaVersions: [RuntimeProviderContextContract.SchemaVersionV1],
            SupportedResultSchemaVersions: [RuntimeProviderResultContract.SchemaVersionV1],
            SupportedTargetProfiles: registeredTargetProfiles,
            SupportedCapabilities: registeredCapabilities);
    }

    internal MicrosoftRuntimeRequest BuildRequest(
        PlanningOrchestrationResult planning,
        MicrosoftRuntimeProviderDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(definition);

        var targetProfileId = planning.GenerationRequestState.Request?.TargetArtifactProfile.ProfileId ?? string.Empty;
        var targetSupport = definition.SupportedTargetProfiles
            .FirstOrDefault(profile => string.Equals(profile.TargetProfileId, targetProfileId, StringComparison.Ordinal));
        var artifactType = targetSupport?.ArtifactType ?? ToArtifactTypeLabel(planning.GenerationRequestState.Request?.TargetArtifactProfile.ArtifactType);
        var requestId = $"microsoftRuntimeRequest:{planning.Outcome.Metadata.OutcomeId}";
        var runtimeRequestRef = $"runtimeRequest:{planning.Outcome.Metadata.OutcomeId}";
        var candidateId = $"executionCandidate:{runtimeRequestRef}";
        var requiredCapabilities = targetSupport?.RequiredCapabilities
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray() ?? [];
        var providerCapabilityRequirements = definition.SupportedCapabilities
            .Where(capability => requiredCapabilities.Contains(capability.CapabilityId, StringComparer.Ordinal))
            .SelectMany(capability => capability.ProviderCapabilityRequirements)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(requirement => requirement, StringComparer.Ordinal)
            .ToArray();
        var skillState = planning.MicrosoftSkillState ??
            CreateFallbackSkillState(planning.CapabilityNegotiationState);
        var skillProviderState = planning.MicrosoftSkillProviderState ??
            CreateFallbackSkillProviderState(skillState);
        var requiredSkillIds = skillProviderState.Selection?.RequiredSkills
            .Distinct(StringComparer.Ordinal)
            .OrderBy(skillId => skillId, StringComparer.Ordinal)
            .ToArray() ?? skillState.Resolution?.RequiredSkills
            .Select(skill => skill.SkillId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(skillId => skillId, StringComparer.Ordinal)
            .ToArray() ?? [];
        var optionalSkillIds = skillState.Resolution?.OptionalSkills
            .Select(skill => skill.SkillId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(skillId => skillId, StringComparer.Ordinal)
            .ToArray() ?? [];
        var candidateProviderIds = skillProviderState.Selection?.SelectedProviderCandidates
            .Select(provider => provider.ProviderId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(providerId => providerId, StringComparer.Ordinal)
            .ToArray() ?? [];
        var unsupportedCapabilities = skillState.Resolution?.UnresolvedCapabilities.UnsupportedCapabilities ?? [];

        return new MicrosoftRuntimeRequest(
            SchemaVersion: MicrosoftRuntimeRequestContract.SchemaVersionV1,
            RequestId: requestId,
            RequestMetadata: new MicrosoftRuntimeRequestMetadata(
                RuntimeProviderRequestSchemaVersion: RuntimeProviderRequestContract.SchemaVersionV1,
                ExecutionCandidateSchemaVersion: RuntimeProviderContract.SchemaVersionV1,
                MicrosoftAdapterSpecificationSchemaVersion: MicrosoftAdapterSpecificationContract.SchemaVersionV1,
                MicrosoftSkillsCatalogSchemaVersion: skillState.Catalog.SchemaVersion,
                SkillProviderSelectionSchemaVersion: skillProviderState.Selection?.SchemaVersion ?? SkillProviderSelectionContract.SchemaVersionV1),
            PlanningOutcomeReference: new MicrosoftRuntimePlanningOutcomeReference(
                OutcomeId: planning.Outcome.Metadata.OutcomeId,
                SchemaVersion: planning.Outcome.Metadata.SchemaVersion),
            ExecutionCandidateReference: new MicrosoftRuntimeExecutionCandidateReference(
                CandidateId: candidateId,
                SchemaVersion: RuntimeProviderContract.SchemaVersionV1,
                RuntimeRequestRef: runtimeRequestRef),
            TargetProfile: new MicrosoftRuntimeTargetProfile(
                TargetProfileId: targetProfileId,
                ArtifactType: artifactType,
                SupportStatus: targetSupport?.SupportStatus ?? MicrosoftRuntimeSupportStatus.Unsupported),
            CapabilityRequirements: new MicrosoftRuntimeCapabilityRequirements(
                RequiredCapabilities: requiredCapabilities,
                ProviderCapabilityRequirements: providerCapabilityRequirements),
            SkillRequirements: new MicrosoftRuntimeSkillRequirements(
                RequiredSkillIds: requiredSkillIds,
                OptionalSkillIds: optionalSkillIds,
                CandidateProviderIds: candidateProviderIds,
                UnsupportedCapabilities: unsupportedCapabilities,
                Readiness: skillState.Readiness,
                SkillProviderReadiness: skillProviderState.Readiness),
            ReviewRequirements: new MicrosoftRuntimeReviewRequirements(
                DesignApprovalRequired: planning.Outcome.ReadinessSummary.ApprovalStatus.DesignApprovalRequired,
                GenerationApprovalRequired: planning.Outcome.ReadinessSummary.ApprovalStatus.GenerationApprovalRequired,
                AnalyzerValidationRequired: planning.Outcome.ReadinessSummary.ApprovalStatus.AnalyzerValidationRequired,
                DesignApproved: planning.Outcome.ReadinessSummary.ApprovalStatus.DesignApproved,
                GenerationApproved: planning.Outcome.ReadinessSummary.ApprovalStatus.GenerationApproved),
            ExecutionConstraints: new MicrosoftRuntimeExecutionConstraints(
                RequiredProviderCategory: definition.ProviderCategory,
                RequiredExecutionModes: definition.SupportedExecutionModes,
                UnresolvedCapabilities: planning.Outcome.ReadinessSummary.CapabilitySummary.UnresolvedCapabilities),
            Provenance: new MicrosoftRuntimeProvenance(
                GenerationRequestRef: planning.Outcome.References.GenerationRequestRef,
                ExecutionPlanRef: planning.Outcome.References.ExecutionPlanRef,
                CapabilityNegotiationRef: planning.Outcome.References.NegotiationRef,
                ExecutionProviderRef: planning.Outcome.References.ExecutionProviderRef,
                Lineage: planning.Outcome.Lineage.PlanningLineage));
    }

    internal MicrosoftRuntimeContext CreateContext(
        PlanningOrchestrationResult planning,
        MicrosoftRuntimeProviderDefinition definition,
        MicrosoftRuntimeRequest request)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(request);

        var plannedCapabilities = definition.SupportedCapabilities
            .Where(capability =>
                request.CapabilityRequirements.RequiredCapabilities.Contains(capability.CapabilityId, StringComparer.Ordinal) &&
                capability.SupportStatus == MicrosoftRuntimeSupportStatus.Planned)
            .Select(capability => capability.CapabilityId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();

        return new MicrosoftRuntimeContext(
            SchemaVersion: MicrosoftRuntimeContextContract.SchemaVersionV1,
            ContextId: $"microsoftRuntimeContext:{request.RequestId}",
            RuntimeProviderReference: new MicrosoftRuntimeProviderReference(
                ProviderId: definition.ProviderId,
                ProviderVersion: definition.ProviderVersion,
                ProviderCategory: definition.ProviderCategory),
            PlanningLineage: new MicrosoftRuntimePlanningLineage(
                RuntimeRequestRef: request.RequestId,
                PlanningOutcomeRef: planning.Outcome.Metadata.OutcomeId,
                ExecutionCandidateRef: request.ExecutionCandidateReference.CandidateId),
            GenerationRequestLineage: new MicrosoftRuntimeGenerationRequestLineage(
                GenerationRequestRef: request.Provenance.GenerationRequestRef),
            ExecutionPlanLineage: new MicrosoftRuntimeExecutionPlanLineage(
                ExecutionPlanRef: request.Provenance.ExecutionPlanRef),
            CapabilityNegotiationLineage: new MicrosoftRuntimeCapabilityNegotiationLineage(
                CapabilityNegotiationRef: request.Provenance.CapabilityNegotiationRef),
            ApprovalLineage: request.ReviewRequirements,
            TargetProfile: request.TargetProfile,
            MicrosoftCapabilitySummary: new MicrosoftRuntimeCapabilitySummary(
                RequiredCapabilities: request.CapabilityRequirements.RequiredCapabilities,
                ProviderCapabilityRequirements: request.CapabilityRequirements.ProviderCapabilityRequirements,
                PlannedCapabilities: plannedCapabilities),
            MicrosoftSkillSummary: new MicrosoftRuntimeSkillSummary(
                RequiredSkillIds: request.SkillRequirements.RequiredSkillIds,
                OptionalSkillIds: request.SkillRequirements.OptionalSkillIds,
                CandidateProviderIds: request.SkillRequirements.CandidateProviderIds,
                UnsupportedCapabilities: request.SkillRequirements.UnsupportedCapabilities,
                Readiness: request.SkillRequirements.Readiness,
                SkillProviderReadiness: request.SkillRequirements.SkillProviderReadiness));
    }

    internal MicrosoftRuntimeProviderValidationResult ValidateRequest(
        PlanningOrchestrationResult planning,
        MicrosoftRuntimeProviderDefinition definition,
        MicrosoftRuntimeRequest request,
        MicrosoftRuntimeContext context)
    {
        return _validator.Validate(planning, definition, request, context);
    }

    internal MicrosoftRuntimeReadinessState EvaluateReadiness(
        PlanningOrchestrationResult planning,
        MicrosoftRuntimeProviderDefinition definition,
        RuntimeProviderRegistration? registration,
        MicrosoftRuntimeProviderValidationResult validation,
        MicrosoftRuntimeRequest request,
        MicrosoftRuntimeContext context)
    {
        return _readinessService.Evaluate(planning, definition, registration, validation, request, context);
    }

    internal MicrosoftRuntimeProviderFrameworkState CreateMicrosoftRuntimeState(
        PlanningOrchestrationResult planning,
        string providerId)
    {
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        var definition = CreateDefaultProviderDefinition();
        _registry.TryGetProvider(providerId, out var registration);
        var request = BuildRequest(planning, definition);
        var context = CreateContext(planning, definition, request);
        var validation = ValidateRequest(planning, definition, request, context);
        var readiness = EvaluateReadiness(planning, definition, registration, validation, request, context);

        return new MicrosoftRuntimeProviderFrameworkState(
            Definition: definition,
            Registration: registration,
            Request: request,
            Context: context,
            Validation: validation,
            Readiness: readiness,
            AcceptsExecutionCandidate: readiness == MicrosoftRuntimeReadinessState.ReadyForMicrosoftRuntimeProvider);
    }

    private static MicrosoftRuntimeSupportStatus ToSupportStatus(MicrosoftAdapterSupportStatus status)
    {
        return status switch
        {
            MicrosoftAdapterSupportStatus.Supported => MicrosoftRuntimeSupportStatus.Supported,
            MicrosoftAdapterSupportStatus.Planned => MicrosoftRuntimeSupportStatus.Planned,
            _ => MicrosoftRuntimeSupportStatus.Unsupported,
        };
    }

    private MicrosoftSkillPlanningState CreateFallbackSkillState(CapabilityNegotiationFrameworkState? negotiationState)
    {
        if (negotiationState is not null)
        {
            return _skillCatalogFrameworkService.PrepareForSkillProvider(
                _skillCatalogFrameworkService.EvaluatePlanning(negotiationState));
        }

        var catalog = _skillCatalogFrameworkService.CreateDefaultCatalogDocument();
        return new MicrosoftSkillPlanningState(
            Catalog: catalog,
            CapabilityNegotiationResult: null,
            Resolution: null,
            Validation: new MicrosoftSkillCompatibilityValidationResult(
                new MicrosoftSkillCompatibilityDiagnostics(
                    MissingRequiredSections: ["capabilityNegotiation"],
                    MissingRequiredFields: [],
                    DuplicateSkillIds: [],
                    UnsupportedTargetProfiles: [],
                    UnsupportedCapabilities: [],
                    UnsatisfiedPrerequisites: [],
                    VersionMismatches: [],
                    IntegrityFailures: [])),
            Readiness: MicrosoftSkillReadinessState.Unsupported);
    }

    private MicrosoftSkillProviderPlanningState CreateFallbackSkillProviderState(MicrosoftSkillPlanningState skillState)
    {
        return _skillProviderAdapterFrameworkService.PrepareForSkillProviderAdapter(
            _skillProviderAdapterFrameworkService.EvaluatePlanning(skillState));
    }

    private static string ToArtifactTypeLabel(GenerationRequestArtifactType? artifactType)
    {
        return artifactType switch
        {
            GenerationRequestArtifactType.FabricApp => "fabricApp",
            GenerationRequestArtifactType.FabricDataApp => "fabricDataApp",
            _ => "pbirReport",
        };
    }
}
