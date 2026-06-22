using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftAdapterSpecificationService
{
    private readonly MicrosoftAdapterSpecificationValidator _validator;
    private readonly MicrosoftProviderPlanningTranslator _translator;

    internal MicrosoftAdapterSpecificationService()
        : this(new MicrosoftAdapterSpecificationValidator(), new MicrosoftProviderPlanningTranslator())
    {
    }

    internal MicrosoftAdapterSpecificationService(
        MicrosoftAdapterSpecificationValidator validator,
        MicrosoftProviderPlanningTranslator translator)
    {
        _validator = validator;
        _translator = translator;
    }

    internal MicrosoftAdapterSpecification CreateDefaultSpecification()
    {
        return new MicrosoftAdapterSpecification(
            SchemaMetadata: new MicrosoftAdapterSchemaMetadata(
                SchemaVersion: MicrosoftAdapterSpecificationContract.SchemaVersionV1,
                SpecificationId: MicrosoftAdapterSpecificationContract.SpecificationId,
                SpecificationVersion: MicrosoftAdapterSpecificationContract.SpecificationVersionV1),
            ProviderIdentity: new MicrosoftAdapterProviderIdentity(
                ProviderId: MicrosoftAdapterSpecificationContract.ProviderId,
                ProviderCategory: MicrosoftAdapterSpecificationContract.ProviderCategory,
                ProviderDisplayName: MicrosoftAdapterSpecificationContract.ProviderDisplayName),
            SupportedTargetProfiles:
            [
                new MicrosoftAdapterTargetProfileSupport(
                    TargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
                    ArtifactType: "pbirReport",
                    SupportStatus: MicrosoftAdapterSupportStatus.Supported,
                    Notes: "PBIR report planning can be described without enabling execution."),
                new MicrosoftAdapterTargetProfileSupport(
                    TargetProfileId: GenerationRequestContract.FabricDataAppDefaultProfile,
                    ArtifactType: "fabricDataApp",
                    SupportStatus: MicrosoftAdapterSupportStatus.Planned,
                    Notes: "Fabric Data App remains future-facing and descriptive only."),
                new MicrosoftAdapterTargetProfileSupport(
                    TargetProfileId: GenerationRequestContract.FabricAppDefaultProfile,
                    ArtifactType: "fabricApp",
                    SupportStatus: MicrosoftAdapterSupportStatus.Unsupported,
                    Notes: "Fabric App terminology and runtime mapping remain unresolved.")
            ],
            CapabilityMappings:
            [
                new MicrosoftAdapterCapabilityMapping(
                    CapabilityId: "layoutGeneration",
                    ProviderCapabilityRequirements: ["layoutGeneration"],
                    SupportStatus: MicrosoftAdapterSupportStatus.Supported,
                    Notes: "Translate provider-neutral layout intent into Microsoft layout requirements."),
                new MicrosoftAdapterCapabilityMapping(
                    CapabilityId: "navigationGeneration",
                    ProviderCapabilityRequirements: ["layoutGeneration"],
                    SupportStatus: MicrosoftAdapterSupportStatus.Supported,
                    Notes: "Navigation intent is derived from structural layout planning."),
                new MicrosoftAdapterCapabilityMapping(
                    CapabilityId: "pageGeneration",
                    ProviderCapabilityRequirements: ["layoutGeneration"],
                    SupportStatus: MicrosoftAdapterSupportStatus.Supported,
                    Notes: "Page structure remains a descriptive output of layout planning."),
                new MicrosoftAdapterCapabilityMapping(
                    CapabilityId: "semanticGeneration",
                    ProviderCapabilityRequirements: ["semanticGeneration"],
                    SupportStatus: MicrosoftAdapterSupportStatus.Supported,
                    Notes: "Semantic bindings and KPI intent remain deterministic."),
                new MicrosoftAdapterCapabilityMapping(
                    CapabilityId: "deploymentSupport",
                    ProviderCapabilityRequirements: ["artifactGeneration"],
                    SupportStatus: MicrosoftAdapterSupportStatus.Planned,
                    Notes: "Deployment remains a future downstream concern."),
                new MicrosoftAdapterCapabilityMapping(
                    CapabilityId: "validationSupport",
                    ProviderCapabilityRequirements: ["validation"],
                    SupportStatus: MicrosoftAdapterSupportStatus.Planned,
                    Notes: "Analyzer validation stays downstream from any future Microsoft execution provider.")
            ],
            TargetProfileMappings:
            [
                new MicrosoftAdapterTargetProfileMapping(
                    TargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
                    RequiredCapabilities: ["layoutGeneration", "navigationGeneration", "pageGeneration", "semanticGeneration"],
                    OptionalCapabilities: ["validationSupport"],
                    UnsupportedCapabilities: ["deploymentSupport"],
                    PlanningRequirements:
                    [
                        "Preserve PBIR report-definition structure.",
                        "Preserve page-level navigation intent.",
                        "Preserve semantic-model KPI and filter bindings."
                    ]),
                new MicrosoftAdapterTargetProfileMapping(
                    TargetProfileId: GenerationRequestContract.FabricDataAppDefaultProfile,
                    RequiredCapabilities: ["deploymentSupport", "layoutGeneration", "navigationGeneration", "semanticGeneration"],
                    OptionalCapabilities: ["validationSupport"],
                    UnsupportedCapabilities: [],
                    PlanningRequirements:
                    [
                        "Preserve data-app route structure and KPI intent.",
                        "Preserve semantic-model bindings for the app surface.",
                        "Leave deployment and template realization to a future execution provider."
                    ]),
                new MicrosoftAdapterTargetProfileMapping(
                    TargetProfileId: GenerationRequestContract.FabricAppDefaultProfile,
                    RequiredCapabilities: [],
                    OptionalCapabilities: [],
                    UnsupportedCapabilities: ["deploymentSupport"],
                    PlanningRequirements:
                    [
                        "Do not translate Fabric App requests until terminology mapping is explicit."
                    ])
            ],
            CompatibilityCatalog: new MicrosoftAdapterCompatibilityCatalog(
                SupportedCombinations:
                [
                    new MicrosoftAdapterCapabilityCombination(
                        TargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
                        CapabilityRequirements: ["layoutGeneration", "navigationGeneration", "pageGeneration", "semanticGeneration"],
                        Notes: "PBIR report planning is fully described.")
                ],
                UnsupportedCombinations:
                [
                    new MicrosoftAdapterCapabilityCombination(
                        TargetProfileId: GenerationRequestContract.FabricAppDefaultProfile,
                        CapabilityRequirements: ["deploymentSupport"],
                        Notes: "Fabric App remains unsupported until the target mapping is stabilized.")
                ],
                FutureCombinations:
                [
                    new MicrosoftAdapterCapabilityCombination(
                        TargetProfileId: GenerationRequestContract.FabricDataAppDefaultProfile,
                        CapabilityRequirements: ["deploymentSupport", "layoutGeneration", "navigationGeneration", "semanticGeneration"],
                        Notes: "Fabric Data App remains future-facing and descriptive only.")
                ]),
            ConstraintCatalog: new MicrosoftAdapterConstraintCatalog(
                UnsupportedArtifactTypes: ["fabricApp"],
                UnsupportedExperienceTypes: [OpportunityExperienceType.FabricApp.ToString()],
                UnsupportedCapabilityCombinations:
                [
                    new MicrosoftAdapterUnsupportedCapabilityCombination(
                        CapabilityRequirements: ["deploymentSupport", "validationSupport"],
                        Reason: "Deployment and validation remain outside the specification-only phase.")
                ]),
            ReviewRequirementsCatalog: new MicrosoftAdapterReviewRequirementsCatalog(
                DesignApprovalRequired: true,
                GenerationApprovalRequired: true,
                AnalyzerValidationRequired: true,
                InheritedContractVersions:
                [
                    GenerationRequestContract.SchemaVersionV1,
                    ExecutionPlanContract.SchemaVersionV1,
                    ProviderAdapterContract.SchemaVersionV1
                ]));
    }

    internal MicrosoftAdapterSpecificationValidationResult ValidateSpecification(MicrosoftAdapterSpecification specification)
    {
        return _validator.Validate(specification);
    }

    internal MicrosoftAdapterPlanningState EvaluatePlanning(
        MicrosoftAdapterSpecification specification,
        ProviderAdapterRequest adapterRequest,
        ExecutionPlan executionPlan)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(adapterRequest);
        ArgumentNullException.ThrowIfNull(executionPlan);

        var validation = _validator.Validate(specification);
        if (!validation.IsValid)
        {
            return new MicrosoftAdapterPlanningState(
                Specification: specification,
                AdapterRequest: adapterRequest,
                ExecutionPlan: executionPlan,
                Translation: null,
                CompatibilityStatus: MicrosoftAdapterCombinationStatus.Unsupported,
                Readiness: MicrosoftAdapterPlanningReadinessState.Unsupported,
                Diagnostics: validation.Diagnostics);
        }

        var translation = _translator.Translate(specification, adapterRequest, executionPlan);
        var targetSupport = specification.SupportedTargetProfiles
            .FirstOrDefault(profile => string.Equals(profile.TargetProfileId, translation.TargetProfileId, StringComparison.Ordinal));
        var targetMapping = specification.TargetProfileMappings
            .FirstOrDefault(mapping => string.Equals(mapping.TargetProfileId, translation.TargetProfileId, StringComparison.Ordinal));
        var capabilitySupport = specification.CapabilityMappings
            .ToDictionary(mapping => mapping.CapabilityId, mapping => mapping.SupportStatus, StringComparer.Ordinal);
        var unsupportedTargetProfiles = new List<string>();
        var futureTargetProfiles = new List<string>();
        var unsupportedCapabilities = new List<string>();
        var futureCapabilities = new List<string>();
        var constraintFailures = new List<string>();
        var reviewRequirementFailures = new List<string>();

        if (targetSupport is null || targetSupport.SupportStatus == MicrosoftAdapterSupportStatus.Unsupported)
        {
            unsupportedTargetProfiles.Add(translation.TargetProfileId);
        }
        else if (targetSupport.SupportStatus == MicrosoftAdapterSupportStatus.Planned)
        {
            futureTargetProfiles.Add(translation.TargetProfileId);
        }

        foreach (var capability in translation.RequiredCapabilities)
        {
            if (!capabilitySupport.TryGetValue(capability, out var supportStatus))
            {
                futureCapabilities.Add(capability);
                continue;
            }

            if (supportStatus == MicrosoftAdapterSupportStatus.Unsupported)
            {
                unsupportedCapabilities.Add(capability);
            }
            else if (supportStatus == MicrosoftAdapterSupportStatus.Planned)
            {
                futureCapabilities.Add(capability);
            }
        }

        if (targetMapping is not null)
        {
            if (targetSupport?.SupportStatus == MicrosoftAdapterSupportStatus.Unsupported)
            {
                unsupportedCapabilities.AddRange(targetMapping.UnsupportedCapabilities);
            }
            else
            {
                unsupportedCapabilities.AddRange(targetMapping.UnsupportedCapabilities.Where(capability =>
                    translation.RequiredCapabilities.Contains(capability, StringComparer.Ordinal) ||
                    translation.ResolvedCapabilityRequirements.Contains(capability, StringComparer.Ordinal)));
            }
        }

        foreach (var combination in specification.ConstraintCatalog.UnsupportedCapabilityCombinations ?? [])
        {
            if (combination.CapabilityRequirements.All(capability =>
                translation.RequiredCapabilities.Contains(capability, StringComparer.Ordinal) ||
                translation.ResolvedCapabilityRequirements.Contains(capability, StringComparer.Ordinal) ||
                targetMapping?.UnsupportedCapabilities.Contains(capability, StringComparer.Ordinal) == true))
            {
                constraintFailures.Add(combination.Reason);
            }
        }

        if (!adapterRequest.ReviewRequirements.DesignApprovalRequired || !executionPlan.ReviewRequirements.DesignApprovalRequired)
        {
            reviewRequirementFailures.Add("design approval must stay required.");
        }

        if (!adapterRequest.ReviewRequirements.GenerationApprovalRequired || !executionPlan.ReviewRequirements.GenerationApprovalRequired)
        {
            reviewRequirementFailures.Add("generation approval must stay required.");
        }

        if (!adapterRequest.ReviewRequirements.AnalyzerReviewRequired || !executionPlan.ReviewRequirements.AnalyzerReviewRequired)
        {
            reviewRequirementFailures.Add("analyzer validation must stay required.");
        }

        var diagnostics = new MicrosoftAdapterSpecificationDiagnostics(
            MissingRequiredSections: validation.Diagnostics.MissingRequiredSections,
            MissingRequiredFields: validation.Diagnostics.MissingRequiredFields,
            UnsupportedSchemaVersions: validation.Diagnostics.UnsupportedSchemaVersions,
            UnsupportedTargetProfiles: unsupportedTargetProfiles.Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedCapabilityRequirements: unsupportedCapabilities.Distinct(StringComparer.Ordinal).ToArray(),
            FutureTargetProfiles: futureTargetProfiles.Distinct(StringComparer.Ordinal).ToArray(),
            FutureCapabilityRequirements: futureCapabilities
                .Concat(translation.MissingCapabilities)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(capability => capability, StringComparer.Ordinal)
                .ToArray(),
            ConstraintFailures: constraintFailures.Distinct(StringComparer.Ordinal).ToArray(),
            ReviewRequirementFailures: reviewRequirementFailures.Distinct(StringComparer.Ordinal).ToArray());
        var compatibilityStatus = GetCompatibilityStatus(diagnostics);
        var readiness = compatibilityStatus switch
        {
            MicrosoftAdapterCombinationStatus.Supported => MicrosoftAdapterPlanningReadinessState.Supported,
            MicrosoftAdapterCombinationStatus.Future => MicrosoftAdapterPlanningReadinessState.PartiallySupported,
            _ => MicrosoftAdapterPlanningReadinessState.Unsupported,
        };

        return new MicrosoftAdapterPlanningState(
            Specification: specification,
            AdapterRequest: adapterRequest,
            ExecutionPlan: executionPlan,
            Translation: translation,
            CompatibilityStatus: compatibilityStatus,
            Readiness: readiness,
            Diagnostics: diagnostics);
    }

    internal MicrosoftAdapterPlanningState PrepareForMicrosoftAdapter(MicrosoftAdapterPlanningState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Readiness == MicrosoftAdapterPlanningReadinessState.Supported
            ? state with
            {
                Readiness = MicrosoftAdapterPlanningReadinessState.ReadyForMicrosoftAdapter,
            }
            : state;
    }

    private static MicrosoftAdapterCombinationStatus GetCompatibilityStatus(MicrosoftAdapterSpecificationDiagnostics diagnostics)
    {
        if (diagnostics.UnsupportedTargetProfiles.Count > 0 ||
            diagnostics.UnsupportedCapabilityRequirements.Count > 0 ||
            diagnostics.ConstraintFailures.Count > 0 ||
            diagnostics.ReviewRequirementFailures.Count > 0)
        {
            return MicrosoftAdapterCombinationStatus.Unsupported;
        }

        if (diagnostics.FutureTargetProfiles.Count > 0 || diagnostics.FutureCapabilityRequirements.Count > 0)
        {
            return MicrosoftAdapterCombinationStatus.Future;
        }

        return MicrosoftAdapterCombinationStatus.Supported;
    }
}
