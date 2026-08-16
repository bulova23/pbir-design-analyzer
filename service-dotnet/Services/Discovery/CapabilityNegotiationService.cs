using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class CapabilityNegotiationService
{
    private readonly CapabilityNegotiationValidator _validator;
    private readonly MicrosoftAdapterSpecificationValidator _specificationValidator;

    internal CapabilityNegotiationService()
        : this(new CapabilityNegotiationValidator(), new MicrosoftAdapterSpecificationValidator())
    {
    }

    internal CapabilityNegotiationService(
        CapabilityNegotiationValidator validator,
        MicrosoftAdapterSpecificationValidator specificationValidator)
    {
        _validator = validator;
        _specificationValidator = specificationValidator;
    }

    internal CapabilityNegotiationSubstitutionCatalog CreateDefaultSubstitutionCatalog()
    {
        return new CapabilityNegotiationSubstitutionCatalog(
            SchemaVersion: CapabilityNegotiationContract.SubstitutionCatalogSchemaVersionV1,
            CatalogId: CapabilityNegotiationContract.DefaultSubstitutionCatalogId,
            CatalogVersion: CapabilityNegotiationContract.DefaultSubstitutionCatalogVersionV1,
            Rules:
            [
                new CapabilityNegotiationSubstitutionRule(
                    RuleId: "navigation-from-layout",
                    OriginalCapabilityId: "navigationGeneration",
                    SubstituteCapabilityId: "layoutGeneration",
                    AppliesToTargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
                    SubstitutionReason: "Navigation generation is derived deterministically from layout planning."),
                new CapabilityNegotiationSubstitutionRule(
                    RuleId: "navigation-from-layout-data-app",
                    OriginalCapabilityId: "navigationGeneration",
                    SubstituteCapabilityId: "layoutGeneration",
                    AppliesToTargetProfileId: GenerationRequestContract.FabricDataAppDefaultProfile,
                    SubstitutionReason: "Navigation generation is derived deterministically from layout planning."),
                new CapabilityNegotiationSubstitutionRule(
                    RuleId: "page-from-layout",
                    OriginalCapabilityId: "pageGeneration",
                    SubstituteCapabilityId: "layoutGeneration",
                    AppliesToTargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
                    SubstitutionReason: "Page generation is derived deterministically from layout planning.")
            ]);
    }

    internal CapabilityNegotiationFrameworkState Negotiate(
        GenerationRequest generationRequest,
        ExecutionPlan executionPlan,
        ProviderAdapterRequest adapterRequest,
        ProviderAdapterDefinition adapterDefinition,
        MicrosoftAdapterSpecification specification,
        CapabilityNegotiationSubstitutionCatalog? substitutionCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(generationRequest);
        ArgumentNullException.ThrowIfNull(executionPlan);
        ArgumentNullException.ThrowIfNull(adapterRequest);
        ArgumentNullException.ThrowIfNull(adapterDefinition);
        ArgumentNullException.ThrowIfNull(specification);

        substitutionCatalog ??= CreateDefaultSubstitutionCatalog();

        var baseValidation = _validator.Validate(
            generationRequest,
            executionPlan,
            adapterRequest,
            adapterDefinition,
            specification,
            substitutionCatalog);
        var specificationValidation = _specificationValidator.Validate(specification);
        var diagnostics = MergeDiagnostics(baseValidation.Diagnostics, specificationValidation.Diagnostics);

        if (diagnostics.HasFailures)
        {
            return new CapabilityNegotiationFrameworkState(
                GenerationRequest: generationRequest,
                ExecutionPlan: executionPlan,
                AdapterRequest: adapterRequest,
                AdapterDefinition: adapterDefinition,
                Specification: specification,
                Result: null,
                Readiness: CapabilityNegotiationReadinessState.Blocked,
                Diagnostics: diagnostics);
        }

        var targetProfileId = generationRequest.TargetArtifactProfile.ProfileId;
        var capabilityMappingLookup = specification.CapabilityMappings
            .ToDictionary(mapping => mapping.CapabilityId, mapping => mapping, StringComparer.Ordinal);
        var targetMapping = specification.TargetProfileMappings
            .First(mapping => string.Equals(mapping.TargetProfileId, targetProfileId, StringComparison.Ordinal));
        var requirements = BuildRequirements(specification, targetMapping);
        var directCapabilities = adapterDefinition.SupportedCapabilities
            .Concat(adapterRequest.CapabilityRequirements ?? [])
            .Concat(executionPlan.ProviderPlanningMetadata.SupportedCapabilities ?? [])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var resolutions = new List<CapabilityResolution>();
        var substitutions = new List<CapabilitySubstitution>();
        var unsupportedRequiredCapabilities = new List<string>();

        foreach (var requirement in requirements)
        {
            var resolution = ResolveRequirement(
                requirement,
                targetProfileId,
                capabilityMappingLookup,
                directCapabilities,
                substitutionCatalog,
                substitutions);
            resolutions.Add(resolution);

            if (resolution.RequirementLevel == CapabilityRequirementLevel.Required &&
                resolution.Resolution == CapabilityResolutionStatus.Blocked)
            {
                unsupportedRequiredCapabilities.Add(requirement.CapabilityId);
            }
        }

        diagnostics = diagnostics with
        {
            UnsupportedRequiredCapabilities = unsupportedRequiredCapabilities
                .Distinct(StringComparer.Ordinal)
                .OrderBy(capability => capability, StringComparer.Ordinal)
                .ToArray(),
        };

        var summary = new CapabilityNegotiationResolutionSummary(
            SatisfiedCount: resolutions.Count(resolution => resolution.Resolution == CapabilityResolutionStatus.Satisfied),
            SubstitutedCount: resolutions.Count(resolution => resolution.Resolution == CapabilityResolutionStatus.Substituted),
            UnsupportedCount: resolutions.Count(resolution => resolution.Resolution == CapabilityResolutionStatus.Unsupported),
            BlockedCount: resolutions.Count(resolution => resolution.Resolution == CapabilityResolutionStatus.Blocked),
            OmittedCount: resolutions.Count(resolution => resolution.Resolution == CapabilityResolutionStatus.Omitted),
            AllRequiredCapabilitiesSatisfied: resolutions
                .Where(resolution => resolution.RequirementLevel == CapabilityRequirementLevel.Required)
                .All(resolution =>
                    resolution.Resolution == CapabilityResolutionStatus.Satisfied ||
                    resolution.Resolution == CapabilityResolutionStatus.Substituted));
        var readiness = DetermineReadiness(summary, resolutions, diagnostics);
        var result = new CapabilityNegotiationResult(
            SchemaVersion: CapabilityNegotiationContract.SchemaVersionV1,
            NegotiationId: $"capneg:{targetProfileId}:{executionPlan.ExecutionPlanId}",
            TargetProfileId: targetProfileId,
            ProviderCategory: specification.ProviderIdentity.ProviderCategory,
            Requirements: requirements,
            Resolutions: resolutions,
            Substitutions: substitutions
                .Distinct()
                .OrderBy(substitution => substitution.OriginalCapabilityId, StringComparer.Ordinal)
                .ThenBy(substitution => substitution.SubstituteCapabilityId, StringComparer.Ordinal)
                .ToArray(),
            ResolutionSummary: summary,
            ReadinessStatus: readiness);

        return new CapabilityNegotiationFrameworkState(
            GenerationRequest: generationRequest,
            ExecutionPlan: executionPlan,
            AdapterRequest: adapterRequest,
            AdapterDefinition: adapterDefinition,
            Specification: specification,
            Result: result,
            Readiness: readiness,
            Diagnostics: diagnostics);
    }

    internal CapabilityNegotiationFrameworkState PrepareForExecutionProvider(CapabilityNegotiationFrameworkState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Result is not null &&
            state.Result.ResolutionSummary.AllRequiredCapabilitiesSatisfied &&
            state.Readiness != CapabilityNegotiationReadinessState.Blocked
            ? state with
            {
                Readiness = CapabilityNegotiationReadinessState.ReadyForExecutionProvider,
                Result = state.Result with
                {
                    ReadinessStatus = CapabilityNegotiationReadinessState.ReadyForExecutionProvider,
                }
            }
            : state;
    }

    private static CapabilityResolution ResolveRequirement(
        CapabilityRequirement requirement,
        string targetProfileId,
        IReadOnlyDictionary<string, MicrosoftAdapterCapabilityMapping> capabilityMappings,
        IReadOnlyCollection<string> directCapabilities,
        CapabilityNegotiationSubstitutionCatalog substitutionCatalog,
        ICollection<CapabilitySubstitution> substitutions)
    {
        if (directCapabilities.Contains(requirement.CapabilityId, StringComparer.Ordinal))
        {
            return new CapabilityResolution(
                CapabilityId: requirement.CapabilityId,
                CapabilityCategory: requirement.CapabilityCategory,
                RequirementLevel: requirement.RequirementLevel,
                Resolution: CapabilityResolutionStatus.Satisfied,
                ResolvedCapabilityId: requirement.CapabilityId,
                ResolutionReason: "Capability is directly supported by the provider-neutral adapter inputs.",
                SourceContract: requirement.SourceContract);
        }

        var appliedRules = substitutionCatalog.Rules
            .Where(rule =>
                string.Equals(rule.OriginalCapabilityId, requirement.CapabilityId, StringComparison.Ordinal) &&
                string.Equals(rule.AppliesToTargetProfileId, targetProfileId, StringComparison.Ordinal))
            .OrderBy(rule => rule.RuleId, StringComparer.Ordinal)
            .ToArray();

        foreach (var rule in appliedRules)
        {
            if (directCapabilities.Contains(rule.SubstituteCapabilityId, StringComparer.Ordinal))
            {
                substitutions.Add(new CapabilitySubstitution(
                    RuleId: rule.RuleId,
                    OriginalCapabilityId: rule.OriginalCapabilityId,
                    SubstituteCapabilityId: rule.SubstituteCapabilityId,
                    SubstitutionReason: rule.SubstitutionReason,
                    AppliesToTargetProfileId: rule.AppliesToTargetProfileId));
                return new CapabilityResolution(
                    CapabilityId: requirement.CapabilityId,
                    CapabilityCategory: requirement.CapabilityCategory,
                    RequirementLevel: requirement.RequirementLevel,
                    Resolution: CapabilityResolutionStatus.Substituted,
                    ResolvedCapabilityId: rule.SubstituteCapabilityId,
                    ResolutionReason: rule.SubstitutionReason,
                    SourceContract: requirement.SourceContract);
            }
        }

        return requirement.RequirementLevel switch
        {
            CapabilityRequirementLevel.Optional => new CapabilityResolution(
                CapabilityId: requirement.CapabilityId,
                CapabilityCategory: requirement.CapabilityCategory,
                RequirementLevel: requirement.RequirementLevel,
                Resolution: CapabilityResolutionStatus.Omitted,
                ResolvedCapabilityId: null,
                ResolutionReason: "Optional capability is not supported and is omitted deterministically.",
                SourceContract: requirement.SourceContract),
            CapabilityRequirementLevel.Preferred => new CapabilityResolution(
                CapabilityId: requirement.CapabilityId,
                CapabilityCategory: requirement.CapabilityCategory,
                RequirementLevel: requirement.RequirementLevel,
                Resolution: CapabilityResolutionStatus.Unsupported,
                ResolvedCapabilityId: null,
                ResolutionReason: "Preferred capability is not supported by the current provider-neutral inputs.",
                SourceContract: requirement.SourceContract),
            _ => new CapabilityResolution(
                CapabilityId: requirement.CapabilityId,
                CapabilityCategory: requirement.CapabilityCategory,
                RequirementLevel: requirement.RequirementLevel,
                Resolution: CapabilityResolutionStatus.Blocked,
                ResolvedCapabilityId: null,
                ResolutionReason: "Required capability is not supported by the current provider-neutral inputs.",
                SourceContract: requirement.SourceContract),
        };
    }

    private static IReadOnlyList<CapabilityRequirement> BuildRequirements(
        MicrosoftAdapterSpecification specification,
        MicrosoftAdapterTargetProfileMapping targetMapping)
    {
        var mappings = specification.CapabilityMappings
            .ToDictionary(mapping => mapping.CapabilityId, mapping => mapping, StringComparer.Ordinal);
        var required = targetMapping.RequiredCapabilities
            .Select(capabilityId => CreateRequirement(capabilityId, CapabilityRequirementLevel.Required, mappings))
            .ToArray();
        var optional = targetMapping.OptionalCapabilities
            .Select(capabilityId => CreateRequirement(capabilityId, CapabilityRequirementLevel.Optional, mappings))
            .ToArray();
        var preferred = specification.CapabilityMappings
            .Where(mapping => mapping.SupportStatus == MicrosoftAdapterSupportStatus.Supported)
            .Select(mapping => mapping.CapabilityId)
            .Except(targetMapping.RequiredCapabilities, StringComparer.Ordinal)
            .Except(targetMapping.OptionalCapabilities, StringComparer.Ordinal)
            .Except(targetMapping.UnsupportedCapabilities, StringComparer.Ordinal)
            .OrderBy(capabilityId => capabilityId, StringComparer.Ordinal)
            .Select(capabilityId => CreateRequirement(capabilityId, CapabilityRequirementLevel.Preferred, mappings))
            .ToArray();

        return required
            .Concat(preferred)
            .Concat(optional)
            .OrderBy(requirement => requirement.RequirementLevel)
            .ThenBy(requirement => requirement.CapabilityId, StringComparer.Ordinal)
            .ToArray();
    }

    private static CapabilityRequirement CreateRequirement(
        string capabilityId,
        CapabilityRequirementLevel requirementLevel,
        IReadOnlyDictionary<string, MicrosoftAdapterCapabilityMapping> mappings)
    {
        var mapping = mappings[capabilityId];
        return new CapabilityRequirement(
            CapabilityId: capabilityId,
            CapabilityCategory: ClassifyCapabilityCategory(capabilityId),
            RequirementLevel: requirementLevel,
            SourceContract: MicrosoftAdapterSpecificationContract.SchemaVersionV1,
            ProviderCapabilityRequirements: mapping.ProviderCapabilityRequirements
                .OrderBy(requirement => requirement, StringComparer.Ordinal)
                .ToArray());
    }

    private static string ClassifyCapabilityCategory(string capabilityId)
    {
        if (capabilityId.Contains("layout", StringComparison.OrdinalIgnoreCase))
        {
            return "layout";
        }

        if (capabilityId.Contains("navigation", StringComparison.OrdinalIgnoreCase))
        {
            return "navigation";
        }

        if (capabilityId.Contains("page", StringComparison.OrdinalIgnoreCase))
        {
            return "page";
        }

        if (capabilityId.Contains("semantic", StringComparison.OrdinalIgnoreCase))
        {
            return "semantic";
        }

        if (capabilityId.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            return "validation";
        }

        if (capabilityId.Contains("deployment", StringComparison.OrdinalIgnoreCase) ||
            capabilityId.Contains("artifact", StringComparison.OrdinalIgnoreCase))
        {
            return "deployment";
        }

        return "general";
    }

    private static CapabilityNegotiationReadinessState DetermineReadiness(
        CapabilityNegotiationResolutionSummary summary,
        IReadOnlyList<CapabilityResolution> resolutions,
        CapabilityNegotiationDiagnostics diagnostics)
    {
        if (diagnostics.HasFailures || summary.BlockedCount > 0)
        {
            return CapabilityNegotiationReadinessState.Blocked;
        }

        if (summary.SubstitutedCount > 0 ||
            summary.UnsupportedCount > 0 ||
            summary.OmittedCount > 0 ||
            resolutions.Any(resolution => resolution.RequirementLevel != CapabilityRequirementLevel.Required))
        {
            return CapabilityNegotiationReadinessState.PartiallyResolved;
        }

        return CapabilityNegotiationReadinessState.Resolved;
    }

    private static CapabilityNegotiationDiagnostics MergeDiagnostics(
        CapabilityNegotiationDiagnostics baseDiagnostics,
        MicrosoftAdapterSpecificationDiagnostics specificationDiagnostics)
    {
        return new CapabilityNegotiationDiagnostics(
            MissingRequiredSections: baseDiagnostics.MissingRequiredSections
                .Concat(specificationDiagnostics.MissingRequiredSections)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            MissingRequiredFields: baseDiagnostics.MissingRequiredFields
                .Concat(specificationDiagnostics.MissingRequiredFields)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            MissingCapabilityDefinitions: baseDiagnostics.MissingCapabilityDefinitions,
            InvalidSubstitutions: baseDiagnostics.InvalidSubstitutions,
            CircularSubstitutions: baseDiagnostics.CircularSubstitutions,
            UnsupportedRequiredCapabilities: baseDiagnostics.UnsupportedRequiredCapabilities,
            VersionMismatches: baseDiagnostics.VersionMismatches
                .Concat(specificationDiagnostics.UnsupportedSchemaVersions)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            CompatibilityFailures: baseDiagnostics.CompatibilityFailures
                .Concat(specificationDiagnostics.ReviewRequirementFailures)
                .Distinct(StringComparer.Ordinal)
                .ToArray());
    }
}
