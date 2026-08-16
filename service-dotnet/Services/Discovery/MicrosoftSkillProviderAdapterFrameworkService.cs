using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class MicrosoftSkillProviderAdapterFrameworkService
{
    private readonly MicrosoftSkillProviderCompatibilityValidator _validator;
    private readonly MicrosoftSkillProviderResolutionService _resolutionService;
    private readonly MicrosoftSkillProviderReadinessService _readinessService;

    internal MicrosoftSkillProviderAdapterFrameworkService()
        : this(
            new MicrosoftSkillProviderCompatibilityValidator(),
            new MicrosoftSkillProviderResolutionService(),
            new MicrosoftSkillProviderReadinessService())
    {
    }

    internal MicrosoftSkillProviderAdapterFrameworkService(
        MicrosoftSkillProviderCompatibilityValidator validator,
        MicrosoftSkillProviderResolutionService resolutionService,
        MicrosoftSkillProviderReadinessService readinessService)
    {
        _validator = validator;
        _resolutionService = resolutionService;
        _readinessService = readinessService;
    }

    internal MicrosoftSkillProviderAdapterDefinition CreateDefaultAdapterDefinition()
    {
        return new MicrosoftSkillProviderAdapterDefinition(
            SchemaVersion: MicrosoftSkillProviderAdapterContract.SchemaVersionV1,
            AdapterId: "microsoft.skill-provider.adapter-framework",
            AdapterName: "Microsoft Skill Provider Adapter Framework",
            AdapterVersion: "1.0.0",
            ProviderCategory: MicrosoftAdapterSpecificationContract.ProviderCategory,
            ProviderSchemaVersion: MicrosoftSkillProviderContract.SchemaVersionV1,
            MicrosoftSkillsCatalogSchemaVersion: MicrosoftSkillsCatalogContract.SchemaVersionV1,
            SkillProviderSelectionSchemaVersion: SkillProviderSelectionContract.SchemaVersionV1,
            MicrosoftRuntimeProviderSchemaVersion: MicrosoftRuntimeProviderContract.SchemaVersionV1,
            SupportedTargetProfiles:
            [
                GenerationRequestContract.PbirReportDefaultProfile,
                GenerationRequestContract.FabricDataAppDefaultProfile
            ],
            SupportedExecutionModes:
            [
                ExecutionProviderMode.Manual,
                ExecutionProviderMode.Assisted,
                ExecutionProviderMode.Automated
            ]);
    }

    internal IReadOnlyList<MicrosoftSkillProviderDefinition> CreateDefaultProviderDefinitions()
    {
        return
        [
            new MicrosoftSkillProviderDefinition(
                SchemaVersion: MicrosoftSkillProviderContract.SchemaVersionV1,
                ProviderId: "microsoft.skill-provider.powerbi-firstparty",
                ProviderName: "Microsoft Power BI Skills Provider",
                ProviderVersion: "1.0.0",
                ProviderCategory: MicrosoftAdapterSpecificationContract.ProviderCategory,
                ProviderStatus: MicrosoftSkillProviderStatus.Available,
                SupportedExecutionModes: [ExecutionProviderMode.Manual, ExecutionProviderMode.Assisted],
                SupportedSkills:
                [
                    "microsoft.skill.powerbi.report-authoring",
                    "microsoft.skill.powerbi.report-design",
                    "microsoft.skill.powerbi.validate-report"
                ],
                SupportedCapabilities:
                [
                    "layoutGeneration",
                    "navigationGeneration",
                    "pageGeneration",
                    "semanticGeneration",
                    "validationSupport"
                ],
                SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile]),
            new MicrosoftSkillProviderDefinition(
                SchemaVersion: MicrosoftSkillProviderContract.SchemaVersionV1,
                ProviderId: "microsoft.skill-provider.fabric-data-app",
                ProviderName: "Microsoft Fabric Data App Skills Provider",
                ProviderVersion: "1.0.0",
                ProviderCategory: MicrosoftAdapterSpecificationContract.ProviderCategory,
                ProviderStatus: MicrosoftSkillProviderStatus.Planned,
                SupportedExecutionModes: [ExecutionProviderMode.Assisted, ExecutionProviderMode.Automated],
                SupportedSkills: ["microsoft.skill.fabric.data-app-template"],
                SupportedCapabilities:
                [
                    "deploymentSupport",
                    "layoutGeneration",
                    "navigationGeneration",
                    "semanticGeneration"
                ],
                SupportedTargetProfiles: [GenerationRequestContract.FabricDataAppDefaultProfile])
        ];
    }

    internal MicrosoftSkillProviderRegistry CreateDefaultRegistry(IReadOnlyCollection<MicrosoftSkillProviderDefinition>? providers = null)
    {
        providers ??= CreateDefaultProviderDefinitions();

        var registry = new MicrosoftSkillProviderRegistry();
        foreach (var provider in providers)
        {
            registry.Register(provider);
        }

        return registry;
    }

    internal MicrosoftSkillProviderPlanningState EvaluatePlanning(
        MicrosoftSkillPlanningState skillState,
        IReadOnlyCollection<MicrosoftSkillProviderDefinition>? providers = null)
    {
        ArgumentNullException.ThrowIfNull(skillState);

        var adapter = CreateDefaultAdapterDefinition();
        providers ??= CreateDefaultProviderDefinitions();
        var registry = CreateDefaultRegistry(providers);
        var selection = skillState.Resolution is null
            ? null
            : _resolutionService.Resolve(skillState, registry);
        var validation = _validator.Validate(skillState, registry, selection);
        var readiness = selection is null
            ? MicrosoftSkillProviderReadinessState.Unsupported
            : _readinessService.Evaluate(validation, selection);

        return new MicrosoftSkillProviderPlanningState(
            Adapter: adapter,
            Providers: registry.Registrations,
            SkillPlanningState: skillState,
            Selection: selection is null ? null : UpdateSelectionReadiness(selection, readiness),
            Validation: validation,
            Readiness: readiness);
    }

    internal MicrosoftSkillProviderPlanningState PrepareForSkillProviderAdapter(MicrosoftSkillProviderPlanningState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Selection is null)
        {
            return state;
        }

        var readiness = _readinessService.PrepareForSkillProviderAdapter(state.Readiness, state.Selection);
        return state with
        {
            Selection = UpdateSelectionReadiness(state.Selection, readiness),
            Readiness = readiness
        };
    }

    private static SkillProviderSelection UpdateSelectionReadiness(
        SkillProviderSelection selection,
        MicrosoftSkillProviderReadinessState readiness)
    {
        return selection with
        {
            ReadinessSummary = selection.ReadinessSummary with
            {
                Readiness = readiness
            }
        };
    }
}
