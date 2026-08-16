using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationProviderFrameworkService
{
    private readonly GenerationProviderValidator _validator;
    private readonly GenerationProviderReadinessService _readinessService;

    internal GenerationProviderFrameworkService()
        : this(new GenerationProviderValidator(), new GenerationProviderReadinessService())
    {
    }

    internal GenerationProviderFrameworkService(
        GenerationProviderValidator validator,
        GenerationProviderReadinessService readinessService)
    {
        _validator = validator;
        _readinessService = readinessService;
    }

    internal GenerationProviderDefinition CreateDefaultProviderDefinition()
    {
        return new GenerationProviderDefinition(
            SchemaVersion: GenerationProviderDefinitionContract.SchemaVersionV1,
            ProviderId: "microsoft.skills.generation-provider",
            ProviderName: "Microsoft Skills Generation Provider",
            ProviderVersion: "1.0.0",
            SupportedArtifactTypes: [GenerationProviderArtifactType.PbirReport],
            SupportedCapabilities:
            [
                "pageGeneration",
                "visualGeneration",
                "semanticGeneration",
                "navigationGeneration",
                "successCriteriaPreservation"
            ],
            SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile],
            SupportedGenerationModes: [GenerationProviderMode.StructuredRequest],
            Status: GenerationProviderStatus.Available);
    }

    internal GenerationProviderRegistry CreateDefaultRegistry(IReadOnlyCollection<GenerationProviderDefinition>? providers = null)
    {
        providers ??= [CreateDefaultProviderDefinition()];

        var registry = new GenerationProviderRegistry();
        foreach (var provider in providers)
        {
            registry.Register(provider);
        }

        return registry;
    }

    internal GenerationProviderFrameworkState CreateProviderState(
        PbirGenerationSpecificationState specificationState,
        GenerationProviderDefinition? provider = null)
    {
        ArgumentNullException.ThrowIfNull(specificationState);

        provider ??= CreateDefaultProviderDefinition();
        var request = CreateRequest(specificationState);
        var validation = _validator.Validate(specificationState, request, provider);
        var readiness = _readinessService.Evaluate(validation, provider);
        var context = CreateContext(specificationState, provider, readiness, validation);
        var result = CreateResult(request, readiness, validation);

        return new GenerationProviderFrameworkState(
            SchemaVersion: GenerationProviderContract.SchemaVersionV1,
            Provider: provider,
            Request: request,
            Context: context,
            Result: result,
            Validation: validation,
            Readiness: readiness);
    }

    private static GenerationProviderRequest CreateRequest(PbirGenerationSpecificationState specificationState)
    {
        var specification = specificationState.Specification;
        var artifact = specification?.ArtifactSpecifications.FirstOrDefault();
        var targetProfileId = artifact?.TargetProfileId ?? string.Empty;
        var artifactType = ResolveArtifactType(targetProfileId);
        var specificationId = specification?.SpecificationId ?? string.Empty;
        var planningOutcomeId = specification?.SourceReferences.PlanningOutcomeRef ?? string.Empty;

        return new GenerationProviderRequest(
            SchemaVersion: GenerationProviderRequestContract.SchemaVersionV1,
            Metadata: new GenerationProviderRequestMetadata(
                RequestId: $"generationProviderRequest:{specificationId}"),
            References: new GenerationProviderRequestReferences(
                PlanningOutcomeReference: new GenerationProviderPlanningOutcomeReference(
                    OutcomeId: planningOutcomeId,
                    SchemaVersion: PlanningOutcomeContract.SchemaVersionV1),
                ExecutionCandidateReference: new GenerationProviderExecutionCandidateReference(
                    CandidateId: $"generationProviderCandidate:{specificationId}",
                    SchemaVersion: RuntimeProviderContract.SchemaVersionV1,
                    CandidateRef: $"generationProviderCandidate:{specificationId}"),
                PbirSpecificationReference: new GenerationProviderPbirSpecificationReference(
                    SpecificationId: specificationId,
                    SchemaVersion: specification?.SchemaVersion ?? string.Empty,
                    ArtifactSpecificationIds: specification?.ArtifactSpecifications.Select(value => value.ArtifactSpecificationId).ToArray() ?? [])),
            Requirements: new GenerationProviderRequirements(
                CapabilityRequirements: new GenerationProviderCapabilityRequirements(
                    ArtifactType: artifactType,
                    TargetProfileId: targetProfileId,
                    RequiredCapabilities: BuildRequiredCapabilities(artifact)),
                ProviderRequirements: new GenerationProviderProviderRequirements(
                    ProviderDefinitionSchemaVersion: GenerationProviderDefinitionContract.SchemaVersionV1,
                    AllowedStatuses: [GenerationProviderStatus.Available, GenerationProviderStatus.Planned, GenerationProviderStatus.Deprecated],
                    RequiredGenerationModes: [GenerationProviderMode.StructuredRequest]),
                Constraints: new GenerationProviderConstraints(
                    AllowApiInvocation: false,
                    AllowCliInvocation: false,
                    AllowDeployment: false,
                    AllowReportMutation: false)));
    }

    private static GenerationProviderContext CreateContext(
        PbirGenerationSpecificationState specificationState,
        GenerationProviderDefinition provider,
        GenerationProviderReadinessState readiness,
        GenerationProviderValidationResult validation)
    {
        var specification = specificationState.Specification;
        var artifact = specification?.ArtifactSpecifications.FirstOrDefault();
        var specificationId = specification?.SpecificationId ?? string.Empty;
        var executionCandidateId = $"generationProviderCandidate:{specificationId}";
        var blockingIssues = validation.Diagnostics.MissingRequiredSections
            .Concat(validation.Diagnostics.MissingRequiredFields)
            .Concat(validation.Diagnostics.SpecificationCompletenessFailures)
            .Concat(validation.Diagnostics.BoundaryViolations)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var unsupportedIssues = validation.Diagnostics.UnsupportedSchemaVersions
            .Concat(validation.Diagnostics.UnsupportedArtifactTypes)
            .Concat(validation.Diagnostics.UnsupportedTargetProfiles)
            .Concat(validation.Diagnostics.UnsupportedGenerationModes)
            .Concat(validation.Diagnostics.ProviderCompatibilityFailures)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new GenerationProviderContext(
            SchemaVersion: GenerationProviderContextContract.SchemaVersionV1,
            ContextId: $"generationProviderContext:{specificationId}",
            ProviderMetadata: new GenerationProviderContextProviderMetadata(
                ProviderId: provider.ProviderId,
                ProviderName: provider.ProviderName,
                ProviderVersion: provider.ProviderVersion,
                Status: provider.Status),
            SpecificationMetadata: new GenerationProviderSpecificationMetadata(
                SpecificationId: specificationId,
                SchemaVersion: specification?.SchemaVersion ?? string.Empty,
                ArtifactType: ResolveArtifactType(artifact?.TargetProfileId),
                TargetProfileId: artifact?.TargetProfileId ?? string.Empty,
                ArtifactCount: specification?.ArtifactSpecifications.Count ?? 0),
            PlanningMetadata: new GenerationProviderPlanningMetadata(
                PlanningOutcomeId: specification?.SourceReferences.PlanningOutcomeRef ?? string.Empty,
                ExecutionCandidateId: executionCandidateId,
                Lineage: specification?.SourceReferences.Lineage ?? []),
            ReadinessMetadata: new GenerationProviderReadinessMetadata(
                Readiness: readiness,
                BlockingIssues: blockingIssues,
                UnsupportedIssues: unsupportedIssues));
    }

    private static GenerationProviderResult CreateResult(
        GenerationProviderRequest request,
        GenerationProviderReadinessState readiness,
        GenerationProviderValidationResult validation)
    {
        return new GenerationProviderResult(
            SchemaVersion: GenerationProviderResultContract.SchemaVersionV1,
            ResultId: $"generationProviderResult:{request.Metadata.RequestId}",
            RequestId: request.Metadata.RequestId,
            Status: ToStatus(readiness),
            Reasons: BuildReasons(validation, readiness));
    }

    private static GenerationProviderResultStatus ToStatus(GenerationProviderReadinessState readiness)
    {
        return readiness switch
        {
            GenerationProviderReadinessState.ReadyForGenerationProvider => GenerationProviderResultStatus.Accepted,
            GenerationProviderReadinessState.Candidate => GenerationProviderResultStatus.Rejected,
            GenerationProviderReadinessState.Unsupported => GenerationProviderResultStatus.Unsupported,
            _ => GenerationProviderResultStatus.Blocked,
        };
    }

    private static IReadOnlyList<string> BuildReasons(
        GenerationProviderValidationResult validation,
        GenerationProviderReadinessState readiness)
    {
        return readiness switch
        {
            GenerationProviderReadinessState.ReadyForGenerationProvider => ["provider-neutral contract is ready for future provider consumption."],
            GenerationProviderReadinessState.Candidate => validation.Diagnostics.ProviderCompatibilityFailures
                .Concat(["provider is descriptive but not yet available for generation-provider readiness."])
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            GenerationProviderReadinessState.Unsupported => validation.Diagnostics.UnsupportedSchemaVersions
                .Concat(validation.Diagnostics.UnsupportedArtifactTypes)
                .Concat(validation.Diagnostics.UnsupportedTargetProfiles)
                .Concat(validation.Diagnostics.UnsupportedGenerationModes)
                .Concat(validation.Diagnostics.ProviderCompatibilityFailures)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            _ => validation.Diagnostics.MissingRequiredSections
                .Concat(validation.Diagnostics.MissingRequiredFields)
                .Concat(validation.Diagnostics.SpecificationCompletenessFailures)
                .Concat(validation.Diagnostics.BoundaryViolations)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
        };
    }

    private static GenerationProviderArtifactType ResolveArtifactType(string? targetProfileId)
    {
        return targetProfileId switch
        {
            GenerationRequestContract.FabricDataAppDefaultProfile => GenerationProviderArtifactType.FabricDataApp,
            GenerationRequestContract.FabricAppDefaultProfile => GenerationProviderArtifactType.FabricApp,
            _ => GenerationProviderArtifactType.PbirReport
        };
    }

    private static IReadOnlyList<string> BuildRequiredCapabilities(PbirArtifactSpecification? artifact)
    {
        var capabilities = new List<string>();

        if (artifact is null)
        {
            return capabilities;
        }

        if (artifact.PageSpecifications.Count > 0)
        {
            capabilities.Add("pageGeneration");
        }

        if (artifact.VisualSpecifications.Count > 0)
        {
            capabilities.Add("visualGeneration");
        }

        if (artifact.SemanticSpecifications.Count > 0)
        {
            capabilities.Add("semanticGeneration");
        }

        if (!string.IsNullOrWhiteSpace(artifact.NavigationSpecifications.LandingPage))
        {
            capabilities.Add("navigationGeneration");
        }

        if (artifact.SuccessCriteria.BusinessSuccessCriteria.Count > 0 ||
            artifact.SuccessCriteria.AnalyticalSuccessCriteria.Count > 0)
        {
            capabilities.Add("successCriteriaPreservation");
        }

        return capabilities
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
