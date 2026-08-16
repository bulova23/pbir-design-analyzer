using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftSkillsCapabilityCatalogFrameworkService
{
    private readonly MicrosoftSkillCompatibilityValidator _validator;
    private readonly MicrosoftSkillResolutionService _resolutionService;
    private readonly MicrosoftSkillReadinessService _readinessService;

    internal MicrosoftSkillsCapabilityCatalogFrameworkService()
        : this(
            new MicrosoftSkillCompatibilityValidator(),
            new MicrosoftSkillResolutionService(),
            new MicrosoftSkillReadinessService())
    {
    }

    internal MicrosoftSkillsCapabilityCatalogFrameworkService(
        MicrosoftSkillCompatibilityValidator validator,
        MicrosoftSkillResolutionService resolutionService,
        MicrosoftSkillReadinessService readinessService)
    {
        _validator = validator;
        _resolutionService = resolutionService;
        _readinessService = readinessService;
    }

    internal MicrosoftSkillsCatalogDocument CreateDefaultCatalogDocument()
    {
        return new MicrosoftSkillsCatalogDocument(
            SchemaVersion: MicrosoftSkillsCatalogContract.SchemaVersionV1,
            CatalogId: MicrosoftSkillsCatalogContract.CatalogId,
            CatalogVersion: MicrosoftSkillsCatalogContract.CatalogVersionV1,
            ProviderCategory: MicrosoftAdapterSpecificationContract.ProviderCategory,
            Skills:
            [
                new MicrosoftSkillDefinition(
                    SchemaVersion: MicrosoftSkillDefinitionContract.SchemaVersionV1,
                    SkillId: "microsoft.skill.powerbi.report-authoring",
                    SkillName: "Power BI Report Authoring",
                    SkillVersion: "1.0.0",
                    SkillCategory: "authoring",
                    ProvidedCapabilities: ["layoutGeneration", "pageGeneration", "semanticGeneration"],
                    SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile],
                    SupportedExecutionModes: [ExecutionProviderMode.Manual, ExecutionProviderMode.Assisted],
                    UnsupportedCapabilities: ["deploymentSupport"],
                    UnsupportedProfiles: [GenerationRequestContract.FabricAppDefaultProfile],
                    PrerequisiteCapabilities: [],
                    Status: MicrosoftSkillAvailabilityStatus.Available),
                new MicrosoftSkillDefinition(
                    SchemaVersion: MicrosoftSkillDefinitionContract.SchemaVersionV1,
                    SkillId: "microsoft.skill.powerbi.report-design",
                    SkillName: "Power BI Report Design",
                    SkillVersion: "1.0.0",
                    SkillCategory: "design",
                    ProvidedCapabilities: ["navigationGeneration"],
                    SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile],
                    SupportedExecutionModes: [ExecutionProviderMode.Assisted],
                    UnsupportedCapabilities: ["deploymentSupport"],
                    UnsupportedProfiles: [GenerationRequestContract.FabricAppDefaultProfile],
                    PrerequisiteCapabilities: ["layoutGeneration"],
                    Status: MicrosoftSkillAvailabilityStatus.Available),
                new MicrosoftSkillDefinition(
                    SchemaVersion: MicrosoftSkillDefinitionContract.SchemaVersionV1,
                    SkillId: "microsoft.skill.powerbi.validate-report",
                    SkillName: "Power BI Validate Report",
                    SkillVersion: "1.0.0",
                    SkillCategory: "validation",
                    ProvidedCapabilities: ["validationSupport"],
                    SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile],
                    SupportedExecutionModes: [ExecutionProviderMode.Manual, ExecutionProviderMode.Assisted],
                    UnsupportedCapabilities: ["deploymentSupport"],
                    UnsupportedProfiles: [GenerationRequestContract.FabricAppDefaultProfile],
                    PrerequisiteCapabilities: ["pageGeneration"],
                    Status: MicrosoftSkillAvailabilityStatus.Available),
                new MicrosoftSkillDefinition(
                    SchemaVersion: MicrosoftSkillDefinitionContract.SchemaVersionV1,
                    SkillId: "microsoft.skill.fabric.data-app-template",
                    SkillName: "Fabric Data App Template",
                    SkillVersion: "1.0.0",
                    SkillCategory: "template",
                    ProvidedCapabilities: ["deploymentSupport", "layoutGeneration", "navigationGeneration", "semanticGeneration"],
                    SupportedTargetProfiles: [GenerationRequestContract.FabricDataAppDefaultProfile],
                    SupportedExecutionModes: [ExecutionProviderMode.Assisted, ExecutionProviderMode.Automated],
                    UnsupportedCapabilities: [],
                    UnsupportedProfiles: [GenerationRequestContract.FabricAppDefaultProfile],
                    PrerequisiteCapabilities: [],
                    Status: MicrosoftSkillAvailabilityStatus.Planned)
            ]);
    }

    internal MicrosoftSkillsCatalog CreateDefaultCatalog()
    {
        return new MicrosoftSkillsCatalog(new MicrosoftSkillsCatalogDocument(
            SchemaVersion: MicrosoftSkillsCatalogContract.SchemaVersionV1,
            CatalogId: MicrosoftSkillsCatalogContract.CatalogId,
            CatalogVersion: MicrosoftSkillsCatalogContract.CatalogVersionV1,
            ProviderCategory: MicrosoftAdapterSpecificationContract.ProviderCategory,
            Skills: []));
    }

    internal MicrosoftSkillPlanningState EvaluatePlanning(
        CapabilityNegotiationFrameworkState negotiationState,
        MicrosoftSkillsCatalogDocument? catalogDocument = null)
    {
        ArgumentNullException.ThrowIfNull(negotiationState);

        catalogDocument ??= CreateDefaultCatalogDocument();
        var catalog = new MicrosoftSkillsCatalog(catalogDocument);
        var result = negotiationState.Result;
        if (result is null)
        {
            return new MicrosoftSkillPlanningState(
                Catalog: catalogDocument,
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

        var requiredCapabilities = result.Requirements
            .Where(requirement => requirement.RequirementLevel == CapabilityRequirementLevel.Required)
            .Select(requirement => requirement.CapabilityId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var optionalCapabilities = result.Requirements
            .Where(requirement =>
                requirement.RequirementLevel == CapabilityRequirementLevel.Optional ||
                requirement.RequirementLevel == CapabilityRequirementLevel.Preferred)
            .Select(requirement => requirement.CapabilityId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(capability => capability, StringComparer.Ordinal)
            .ToArray();
        var resolution = _resolutionService.Resolve(
            catalog,
            result.TargetProfileId,
            requiredCapabilities,
            optionalCapabilities,
            preferredExecutionModes: [ExecutionProviderMode.Assisted, ExecutionProviderMode.Manual]);
        var validation = _validator.Validate(
            catalog,
            result.TargetProfileId,
            requiredCapabilities,
            optionalCapabilities,
            resolution);
        var readiness = _readinessService.Evaluate(validation, resolution);

        return new MicrosoftSkillPlanningState(
            Catalog: catalogDocument,
            CapabilityNegotiationResult: result,
            Resolution: resolution,
            Validation: validation,
            Readiness: readiness);
    }

    internal MicrosoftSkillPlanningState PrepareForSkillProvider(MicrosoftSkillPlanningState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Resolution is null
            ? state
            : state with
            {
                Readiness = _readinessService.PrepareForSkillProvider(state.Readiness, state.Resolution)
            };
    }
}
