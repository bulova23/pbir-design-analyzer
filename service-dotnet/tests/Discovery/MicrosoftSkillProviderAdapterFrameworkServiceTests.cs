using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class MicrosoftSkillProviderAdapterFrameworkServiceTests
{
    [Fact(DisplayName = "Microsoft Skill Provider registry registers providers, discovers them, and looks up capabilities, skills, and target profiles without loading providers")]
    public void MicrosoftSkillProviderRegistry_RegistersDiscoversAndLooksUpProviders()
    {
        var registry = new MicrosoftSkillProviderRegistry();
        var first = new MicrosoftSkillProviderDefinition(
            SchemaVersion: MicrosoftSkillProviderContract.SchemaVersionV1,
            ProviderId: "microsoft.skill-provider.powerbi-firstparty",
            ProviderName: "Microsoft Power BI Skills Provider",
            ProviderVersion: "1.0.0",
            ProviderCategory: MicrosoftAdapterSpecificationContract.ProviderCategory,
            ProviderStatus: MicrosoftSkillProviderStatus.Available,
            SupportedExecutionModes: [ExecutionProviderMode.Manual, ExecutionProviderMode.Assisted],
            SupportedSkills: ["microsoft.skill.powerbi.report-authoring", "microsoft.skill.powerbi.report-design"],
            SupportedCapabilities: ["layoutGeneration", "navigationGeneration", "pageGeneration", "semanticGeneration"],
            SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile]);
        var second = first with
        {
            ProviderId = "microsoft.skill-provider.fabric-data-app",
            ProviderStatus = MicrosoftSkillProviderStatus.Planned,
            SupportedSkills = ["microsoft.skill.fabric.data-app-template"],
            SupportedCapabilities = ["deploymentSupport", "layoutGeneration", "navigationGeneration", "semanticGeneration"],
            SupportedTargetProfiles = [GenerationRequestContract.FabricDataAppDefaultProfile]
        };

        registry.Register(first);
        registry.Register(second);

        Assert.True(registry.TryGetProvider(first.ProviderId, out var resolved));
        Assert.NotNull(resolved);
        Assert.Equal(first.ProviderVersion, resolved!.ProviderVersion);
        Assert.Single(registry.DiscoverByCategory(MicrosoftAdapterSpecificationContract.ProviderCategory, GenerationRequestContract.PbirReportDefaultProfile));
        Assert.Equal(
            new[] { second.ProviderId, first.ProviderId },
            registry.FindProvidersByCapability("layoutGeneration").Select(provider => provider.ProviderId).OrderBy(id => id, StringComparer.Ordinal).ToArray());
        Assert.Single(registry.FindProvidersBySkill("microsoft.skill.powerbi.report-authoring"));
        Assert.Single(registry.FindProvidersByTargetProfile(GenerationRequestContract.FabricDataAppDefaultProfile));
    }

    [Fact(DisplayName = "Microsoft Skill Provider resolution maps required skills to candidate providers, calculates coverage, and becomes ready for a future skill-provider adapter")]
    public void EvaluatePlanning_PbirSkills_ResolveDeterministically()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var service = new MicrosoftSkillProviderAdapterFrameworkService();

        var first = service.EvaluatePlanning(planning.MicrosoftSkillState!);
        var second = service.EvaluatePlanning(planning.MicrosoftSkillState!);
        var prepared = service.PrepareForSkillProviderAdapter(first);

        Assert.NotNull(first.Selection);
        Assert.Equal(MicrosoftSkillProviderAdapterContract.SchemaVersionV1, first.Adapter.SchemaVersion);
        Assert.Equal(SkillProviderSelectionContract.SchemaVersionV1, first.Selection!.SchemaVersion);
        Assert.Equal(MicrosoftSkillProviderReadinessState.Satisfied, first.Readiness);
        Assert.Equal(MicrosoftSkillProviderReadinessState.ReadyForSkillProviderAdapter, prepared.Readiness);
        Assert.Equal(first.Selection.SelectionId, second.Selection!.SelectionId);
        Assert.Equal(GenerationRequestContract.PbirReportDefaultProfile, first.Selection.TargetProfileId);
        Assert.Contains(first.Selection.RequiredSkills, skillId => skillId == "microsoft.skill.powerbi.report-authoring");
        Assert.Contains(first.Selection.RequiredSkills, skillId => skillId == "microsoft.skill.powerbi.report-design");
        Assert.Contains(first.Selection.SelectedProviderCandidates, provider => provider.ProviderId == "microsoft.skill-provider.powerbi-firstparty");
        Assert.Empty(first.Selection.UnsupportedSkills);
        Assert.Equal(4, first.Selection.CoverageSummary.RequiredCapabilitiesCovered.Count);
        Assert.Equal(SerializeSelection(first.Selection), SerializeSelection(second.Selection));
    }

    [Fact(DisplayName = "Microsoft Skill Provider resolution keeps unsupported skills and capabilities unresolved when no provider covers them")]
    public void EvaluatePlanning_UnsupportedSkills_RemainUnresolved()
    {
        var service = new MicrosoftSkillsCapabilityCatalogFrameworkService();
        var skillState = service.EvaluatePlanning(
            new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage()).CapabilityNegotiationState!);
        var providerService = new MicrosoftSkillProviderAdapterFrameworkService();

        var mutatedSkillState = skillState with
        {
            Resolution = skillState.Resolution! with
            {
                RequiredSkills = skillState.Resolution.RequiredSkills
                    .Concat(
                    [
                        new MicrosoftSkillCandidate(
                            SkillId: "microsoft.skill.unknown",
                            SkillVersion: "1.0.0",
                            SkillStatus: MicrosoftSkillAvailabilityStatus.Available,
                            MatchedCapabilities: ["screenshotEvidence"])
                    ])
                    .ToArray(),
                UnresolvedCapabilities = new MicrosoftSkillUnresolvedCapabilitySummary(
                    RequiredCapabilities: ["screenshotEvidence"],
                    OptionalCapabilities: [],
                    UnsupportedCapabilities: ["screenshotEvidence"])
            }
        };

        var state = providerService.EvaluatePlanning(mutatedSkillState);

        Assert.NotNull(state.Selection);
        Assert.Equal(MicrosoftSkillProviderReadinessState.PartiallySatisfied, state.Readiness);
        Assert.Contains("microsoft.skill.unknown", state.Selection!.UnsupportedSkills);
        Assert.Contains("screenshotEvidence", state.Selection.CoverageSummary.UnresolvedRequiredCapabilities);
    }

    [Fact(DisplayName = "Microsoft Skill Provider compatibility validation fails invalid providers, invalid prerequisites, and version mismatches")]
    public void MicrosoftSkillProviderCompatibilityValidator_InvalidProvidersFailClosed()
    {
        var skillService = new MicrosoftSkillsCapabilityCatalogFrameworkService();
        var skillState = skillService.EvaluatePlanning(
            new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage()).CapabilityNegotiationState!);
        var provider = new MicrosoftSkillProviderDefinition(
            SchemaVersion: "microsoft-skill-provider/v2",
            ProviderId: "microsoft.skill-provider.broken",
            ProviderName: "",
            ProviderVersion: "2.0.0",
            ProviderCategory: MicrosoftAdapterSpecificationContract.ProviderCategory,
            ProviderStatus: MicrosoftSkillProviderStatus.Available,
            SupportedExecutionModes: [],
            SupportedSkills: ["microsoft.skill.powerbi.report-design"],
            SupportedCapabilities: ["navigationGeneration"],
            SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile]);
        var registry = new MicrosoftSkillProviderRegistry();
        registry.Register(provider);
        var selection = new SkillProviderSelection(
            SchemaVersion: "skill-provider-selection/v2",
            SelectionId: "skillProviderSelection:test",
            TargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
            RequiredSkills: ["microsoft.skill.powerbi.report-authoring", "microsoft.skill.powerbi.report-design"],
            CandidateProviders:
            [
                new MicrosoftSkillProviderCandidate(
                    ProviderId: provider.ProviderId,
                    ProviderVersion: provider.ProviderVersion,
                    ProviderStatus: provider.ProviderStatus,
                    MatchedSkills: ["microsoft.skill.powerbi.report-design"],
                    MatchedCapabilities: ["navigationGeneration"],
                    MatchedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile])
            ],
            SelectedProviderCandidates:
            [
                new MicrosoftSkillProviderCandidate(
                    ProviderId: provider.ProviderId,
                    ProviderVersion: provider.ProviderVersion,
                    ProviderStatus: provider.ProviderStatus,
                    MatchedSkills: ["microsoft.skill.powerbi.report-design"],
                    MatchedCapabilities: ["navigationGeneration"],
                    MatchedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile])
            ],
            UnsupportedSkills: ["microsoft.skill.powerbi.report-authoring"],
            CoverageSummary: new MicrosoftSkillProviderCoverageSummary(
                RequiredSkillsRequested: ["microsoft.skill.powerbi.report-authoring", "microsoft.skill.powerbi.report-design"],
                RequiredSkillsCovered: ["microsoft.skill.powerbi.report-design"],
                OptionalSkillsRequested: [],
                OptionalSkillsCovered: [],
                RequiredCapabilitiesRequested: ["layoutGeneration", "navigationGeneration"],
                RequiredCapabilitiesCovered: ["navigationGeneration"],
                OptionalCapabilitiesRequested: [],
                OptionalCapabilitiesCovered: [],
                UnresolvedRequiredCapabilities: ["layoutGeneration"],
                UnresolvedOptionalCapabilities: [],
                SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile]),
            ReadinessSummary: new MicrosoftSkillProviderSelectionReadinessSummary(
                Readiness: MicrosoftSkillProviderReadinessState.PartiallySatisfied,
                KnownProviderIds: [provider.ProviderId],
                BlockingIssues: ["layoutGeneration"],
                UnresolvedSkills: ["microsoft.skill.powerbi.report-authoring"],
                UnresolvedCapabilities: ["layoutGeneration"]));
        var validator = new MicrosoftSkillProviderCompatibilityValidator();

        var validation = validator.Validate(skillState, registry, selection);

        Assert.False(validation.IsValid);
        Assert.Contains("microsoft-skill-provider/v2", validation.Diagnostics.VersionMismatches);
        Assert.Contains("skill-provider-selection/v2", validation.Diagnostics.VersionMismatches);
        Assert.Contains("providerName", string.Join("|", validation.Diagnostics.MissingRequiredFields));
        Assert.Contains("layoutGeneration", validation.Diagnostics.UnsatisfiedPrerequisites);
    }

    [Fact(DisplayName = "Microsoft Skill Provider readiness evaluates unsupported, partiallySatisfied, satisfied, and readyForSkillProviderAdapter states correctly")]
    public void MicrosoftSkillProviderReadinessService_EvaluatesEveryStateCorrectly()
    {
        var readiness = new MicrosoftSkillProviderReadinessService();
        var supportedValidation = new MicrosoftSkillProviderCompatibilityValidationResult(MicrosoftSkillProviderCompatibilityDiagnostics.Empty);
        var unsupportedValidation = new MicrosoftSkillProviderCompatibilityValidationResult(
            new MicrosoftSkillProviderCompatibilityDiagnostics(
                MissingRequiredSections: [],
                MissingRequiredFields: [],
                DuplicateProviderIds: [],
                UnsupportedTargetProfiles: [GenerationRequestContract.FabricAppDefaultProfile],
                UnsupportedSkills: ["microsoft.skill.fabric.app"],
                UnsupportedCapabilities: ["deploymentSupport"],
                UnsatisfiedPrerequisites: [],
                VersionMismatches: [],
                IntegrityFailures: []));
        var satisfiedSelection = new SkillProviderSelection(
            SchemaVersion: SkillProviderSelectionContract.SchemaVersionV1,
            SelectionId: "skillProviderSelection:ready",
            TargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
            RequiredSkills: ["microsoft.skill.powerbi.report-authoring"],
            CandidateProviders:
            [
                new MicrosoftSkillProviderCandidate(
                    ProviderId: "microsoft.skill-provider.powerbi-firstparty",
                    ProviderVersion: "1.0.0",
                    ProviderStatus: MicrosoftSkillProviderStatus.Available,
                    MatchedSkills: ["microsoft.skill.powerbi.report-authoring"],
                    MatchedCapabilities: ["layoutGeneration"],
                    MatchedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile])
            ],
            SelectedProviderCandidates:
            [
                new MicrosoftSkillProviderCandidate(
                    ProviderId: "microsoft.skill-provider.powerbi-firstparty",
                    ProviderVersion: "1.0.0",
                    ProviderStatus: MicrosoftSkillProviderStatus.Available,
                    MatchedSkills: ["microsoft.skill.powerbi.report-authoring"],
                    MatchedCapabilities: ["layoutGeneration"],
                    MatchedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile])
            ],
            UnsupportedSkills: [],
            CoverageSummary: new MicrosoftSkillProviderCoverageSummary(
                RequiredSkillsRequested: ["microsoft.skill.powerbi.report-authoring"],
                RequiredSkillsCovered: ["microsoft.skill.powerbi.report-authoring"],
                OptionalSkillsRequested: [],
                OptionalSkillsCovered: [],
                RequiredCapabilitiesRequested: ["layoutGeneration"],
                RequiredCapabilitiesCovered: ["layoutGeneration"],
                OptionalCapabilitiesRequested: [],
                OptionalCapabilitiesCovered: [],
                UnresolvedRequiredCapabilities: [],
                UnresolvedOptionalCapabilities: [],
                SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile]),
            ReadinessSummary: new MicrosoftSkillProviderSelectionReadinessSummary(
                Readiness: MicrosoftSkillProviderReadinessState.Satisfied,
                KnownProviderIds: ["microsoft.skill-provider.powerbi-firstparty"],
                BlockingIssues: [],
                UnresolvedSkills: [],
                UnresolvedCapabilities: []));
        var partialSelection = satisfiedSelection with
        {
            UnsupportedSkills = ["microsoft.skill.powerbi.report-design"],
            CoverageSummary = satisfiedSelection.CoverageSummary with
            {
                UnresolvedRequiredCapabilities = ["navigationGeneration"]
            }
        };

        Assert.Equal(MicrosoftSkillProviderReadinessState.Unsupported, readiness.Evaluate(unsupportedValidation, satisfiedSelection));
        Assert.Equal(MicrosoftSkillProviderReadinessState.PartiallySatisfied, readiness.Evaluate(supportedValidation, partialSelection));
        Assert.Equal(MicrosoftSkillProviderReadinessState.Satisfied, readiness.Evaluate(supportedValidation, satisfiedSelection));
        Assert.Equal(
            MicrosoftSkillProviderReadinessState.ReadyForSkillProviderAdapter,
            readiness.PrepareForSkillProviderAdapter(MicrosoftSkillProviderReadinessState.Satisfied, satisfiedSelection));
    }

    [Fact(DisplayName = "Planning orchestration and Microsoft runtime provider integrate skill-provider selection without adding execution behavior")]
    public void PlanningAndRuntime_MicrosoftSkillProviderSelection_RemainsPlanningOnly()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var registry = new RuntimeProviderRegistry();
        var runtimeService = new MicrosoftRuntimeProviderContractFrameworkService(registry);
        var definition = runtimeService.CreateDefaultProviderDefinition();
        var registration = runtimeService.CreateDefaultRegistration(definition, planning);

        registry.Register(registration);

        var runtime = runtimeService.CreateMicrosoftRuntimeState(planning, registration.ProviderId);

        Assert.NotNull(planning.MicrosoftSkillProviderState);
        Assert.Equal(MicrosoftSkillProviderReadinessState.ReadyForSkillProviderAdapter, planning.MicrosoftSkillProviderState!.Readiness);
        Assert.NotNull(runtime.Request);
        Assert.NotNull(runtime.Context);
        Assert.Equal(SkillProviderSelectionContract.SchemaVersionV1, runtime.Request!.RequestMetadata.SkillProviderSelectionSchemaVersion);
        Assert.Contains("microsoft.skill-provider.powerbi-firstparty", runtime.Request.SkillRequirements.CandidateProviderIds);
        Assert.Equal(runtime.Request.SkillRequirements.CandidateProviderIds, runtime.Context!.MicrosoftSkillSummary.CandidateProviderIds);
    }

    [Fact(DisplayName = "Microsoft Skill Provider adapter framework remains metadata-only with no skill execution, API invocation, provider invocation, CLI execution, or artifact generation surface")]
    public void MicrosoftSkillProviderAdapterBoundary_RemainsMetadataOnly()
    {
        var forbiddenTokens = new[] { "Execute", "Invoke", "Api", "Cli", "GenerateArtifact", "Deploy", "AnalyzerRunner", "RunSkill" };
        Type[] types =
        [
            typeof(MicrosoftSkillProviderAdapterFrameworkService),
            typeof(MicrosoftSkillProviderRegistry),
            typeof(MicrosoftSkillProviderResolutionService),
            typeof(MicrosoftSkillProviderReadinessService),
            typeof(MicrosoftSkillProviderCompatibilityValidator),
            typeof(MicrosoftSkillProviderAdapterDefinition),
            typeof(MicrosoftSkillProviderDefinition),
            typeof(SkillProviderSelection)
        ];

        foreach (var type in types)
        {
            Assert.DoesNotContain(forbiddenTokens, token => type.Name.Contains(token, StringComparison.OrdinalIgnoreCase));

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.DoesNotContain(forbiddenTokens, token => method.Name.Contains(token, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact(DisplayName = "Microsoft Skill Provider contracts inventory the required field paths for adapter, provider, and selection contracts")]
    public void MicrosoftSkillProviderContracts_InventoryCoversRequiredFieldPaths()
    {
        var adapterInventoryPaths = MicrosoftSkillProviderAdapterContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var adapterModelPaths = EnumerateFieldPaths(typeof(MicrosoftSkillProviderAdapterDefinition), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var providerInventoryPaths = MicrosoftSkillProviderContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var providerModelPaths = EnumerateFieldPaths(typeof(MicrosoftSkillProviderDefinition), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var selectionInventoryPaths = SkillProviderSelectionContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var selectionModelPaths = EnumerateFieldPaths(typeof(SkillProviderSelection), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(adapterModelPaths.ToHashSet(StringComparer.Ordinal), adapterInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(providerModelPaths.ToHashSet(StringComparer.Ordinal), providerInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(selectionModelPaths.ToHashSet(StringComparer.Ordinal), selectionInventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static string SerializeSelection(SkillProviderSelection selection)
    {
        return string.Join("|",
            selection.SelectionId,
            selection.TargetProfileId,
            string.Join(",", selection.RequiredSkills),
            string.Join(",", selection.SelectedProviderCandidates.Select(provider => provider.ProviderId)),
            string.Join(",", selection.UnsupportedSkills),
            string.Join(",", selection.CoverageSummary.RequiredCapabilitiesCovered),
            string.Join(",", selection.CoverageSummary.UnresolvedRequiredCapabilities),
            selection.ReadinessSummary.Readiness);
    }

    private static IReadOnlyList<string> EnumerateFieldPaths(Type type, string? prefix)
    {
        var fieldPaths = new List<string>();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var path = string.IsNullOrWhiteSpace(prefix)
                ? property.Name
                : $"{prefix}.{property.Name}";
            fieldPaths.Add(path);

            var nestedType = GetNestedContractType(property.PropertyType);
            if (nestedType is not null)
            {
                fieldPaths.AddRange(EnumerateFieldPaths(nestedType, path));
            }
        }

        return fieldPaths;
    }

    private static Type? GetNestedContractType(Type type)
    {
        if (IsScalar(type))
        {
            return null;
        }

        if (TryGetEnumerableElementType(type, out var elementType))
        {
            return IsScalar(elementType) ? null : elementType;
        }

        return type.Namespace == typeof(MicrosoftSkillProviderDefinition).Namespace ? type : null;
    }

    private static bool TryGetEnumerableElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsEnum ||
            type == typeof(string) ||
            type == typeof(bool) ||
            type == typeof(int) ||
            type == typeof(long);
    }
}
