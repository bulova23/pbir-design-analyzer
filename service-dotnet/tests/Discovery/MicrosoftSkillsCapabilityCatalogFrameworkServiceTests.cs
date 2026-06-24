using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class MicrosoftSkillsCapabilityCatalogFrameworkServiceTests
{
    [Fact(DisplayName = "Microsoft Skills catalog registers skills, discovers them by capability and target profile, and remains descriptive only")]
    public void MicrosoftSkillsCatalog_RegistersDiscoversAndLooksUpSkills()
    {
        var service = new MicrosoftSkillsCapabilityCatalogFrameworkService();
        var catalog = service.CreateDefaultCatalog();
        var authoringSkill = service.CreateDefaultCatalogDocument().Skills.First(skill =>
            skill.SkillId == "microsoft.skill.powerbi.report-authoring");

        catalog.Register(authoringSkill);

        Assert.True(catalog.TryGetSkill(authoringSkill.SkillId, out var resolved));
        Assert.NotNull(resolved);
        Assert.Equal(authoringSkill.SkillVersion, resolved!.SkillVersion);
        Assert.Contains(
            catalog.DiscoverByCapability("layoutGeneration"),
            skill => skill.SkillId == authoringSkill.SkillId);
        Assert.Contains(
            catalog.DiscoverByTargetProfile(GenerationRequestContract.PbirReportDefaultProfile),
            skill => skill.SkillId == authoringSkill.SkillId);
        Assert.Contains(
            catalog.FindSkillsByExecutionMode(ExecutionProviderMode.Assisted),
            skill => skill.SkillId == authoringSkill.SkillId);
    }

    [Fact(DisplayName = "Microsoft Skills resolution maps PBIR capabilities to required and optional skill candidates and becomes ready for a future skill provider")]
    public void EvaluatePlanning_PbirCapabilities_ResolveDeterministically()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var service = new MicrosoftSkillsCapabilityCatalogFrameworkService();

        var first = service.EvaluatePlanning(planning.CapabilityNegotiationState!);
        var second = service.EvaluatePlanning(planning.CapabilityNegotiationState!);
        var prepared = service.PrepareForSkillProvider(first);

        Assert.NotNull(first.Resolution);
        Assert.Equal(MicrosoftSkillsCatalogContract.SchemaVersionV1, first.Catalog.SchemaVersion);
        Assert.Equal(MicrosoftSkillReadinessState.Satisfied, first.Readiness);
        Assert.Equal(MicrosoftSkillReadinessState.ReadyForSkillProvider, prepared.Readiness);
        Assert.Equal(first.Resolution!.ResolutionId, second.Resolution!.ResolutionId);
        Assert.Equal(GenerationRequestContract.PbirReportDefaultProfile, first.Resolution.TargetProfileId);
        Assert.Contains(first.Resolution.RequiredSkills, skill => skill.SkillId == "microsoft.skill.powerbi.report-authoring");
        Assert.Contains(first.Resolution.RequiredSkills, skill => skill.SkillId == "microsoft.skill.powerbi.report-design");
        Assert.Contains(first.Resolution.OptionalSkills, skill => skill.SkillId == "microsoft.skill.powerbi.validate-report");
        Assert.Equal(4, first.Resolution.CapabilityCoverage.RequiredCapabilitiesCovered.Count);
        Assert.Empty(first.Resolution.UnresolvedCapabilities.RequiredCapabilities);
        Assert.Equal(SerializeResolution(first.Resolution), SerializeResolution(second.Resolution));
    }

    [Fact(DisplayName = "Microsoft Skills resolution leaves unsupported capabilities unresolved and optional skill mapping stays non-blocking")]
    public void EvaluatePlanning_UnsupportedCapabilities_RemainUnresolved()
    {
        var basePlanning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var negotiationState = basePlanning.CapabilityNegotiationState! with
        {
            Result = basePlanning.CapabilityNegotiationState!.Result! with
            {
                Requirements = basePlanning.CapabilityNegotiationState.Result!.Requirements
                    .Concat(
                    [
                        new CapabilityRequirement(
                            CapabilityId: "screenshotEvidence",
                            CapabilityCategory: "evidence",
                            RequirementLevel: CapabilityRequirementLevel.Required,
                            SourceContract: CapabilityNegotiationContract.SchemaVersionV1,
                            ProviderCapabilityRequirements: ["screenshotEvidence"])
                    ])
                    .ToArray()
            }
        };
        var service = new MicrosoftSkillsCapabilityCatalogFrameworkService();

        var state = service.EvaluatePlanning(negotiationState);

        Assert.NotNull(state.Resolution);
        Assert.Equal(MicrosoftSkillReadinessState.PartiallySatisfied, state.Readiness);
        Assert.Contains("screenshotEvidence", state.Resolution!.UnresolvedCapabilities.RequiredCapabilities);
        Assert.Empty(state.Resolution.UnresolvedCapabilities.OptionalCapabilities);
    }

    [Fact(DisplayName = "Microsoft Skills compatibility validation fails invalid catalogs, unsatisfied prerequisites, and version mismatches")]
    public void MicrosoftSkillCompatibilityValidator_InvalidCatalogsFailClosed()
    {
        var service = new MicrosoftSkillsCapabilityCatalogFrameworkService();
        var baseCatalog = service.CreateDefaultCatalogDocument();
        var invalidCatalog = baseCatalog with
        {
            SchemaVersion = "microsoft-skills-catalog/v2",
            Skills =
            [
                baseCatalog.Skills.First() with
                {
                    SchemaVersion = "microsoft-skill-definition/v2",
                    SkillId = "",
                    PrerequisiteCapabilities = ["semanticGeneration", "nonexistentCapability"]
                }
            ]
        };
        var catalog = new MicrosoftSkillsCatalog(invalidCatalog);
        var validator = new MicrosoftSkillCompatibilityValidator();
        var resolution = new MicrosoftSkillResolutionResult(
            ResolutionId: "microsoftSkillResolution:test",
            TargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
            CandidateSkillSet:
            [
                new MicrosoftSkillCandidate(
                    SkillId: "broken",
                    SkillVersion: "1.0.0",
                    SkillStatus: MicrosoftSkillAvailabilityStatus.Available,
                    MatchedCapabilities: ["layoutGeneration"])
            ],
            RequiredSkills: [],
            OptionalSkills: [],
            CapabilityCoverage: new MicrosoftSkillCapabilityCoverageSummary(
                RequiredCapabilitiesRequested: ["layoutGeneration"],
                RequiredCapabilitiesCovered: [],
                OptionalCapabilitiesRequested: [],
                OptionalCapabilitiesCovered: []),
            UnresolvedCapabilities: new MicrosoftSkillUnresolvedCapabilitySummary(
                RequiredCapabilities: ["layoutGeneration"],
                OptionalCapabilities: [],
                UnsupportedCapabilities: ["layoutGeneration"]));

        var validation = validator.Validate(
            catalog,
            GenerationRequestContract.PbirReportDefaultProfile,
            requiredCapabilities: ["layoutGeneration"],
            optionalCapabilities: [],
            resolution);

        Assert.False(validation.IsValid);
        Assert.Contains("microsoft-skills-catalog/v2", validation.Diagnostics.VersionMismatches);
        Assert.Contains("microsoft-skill-definition/v2", validation.Diagnostics.VersionMismatches);
        Assert.Contains("skills[0].skillId", validation.Diagnostics.MissingRequiredFields);
        Assert.Contains("nonexistentCapability", validation.Diagnostics.UnsatisfiedPrerequisites);
    }

    [Fact(DisplayName = "Planning orchestration and Microsoft runtime provider integrate Microsoft Skills readiness without adding execution behavior")]
    public void PlanningAndRuntime_MicrosoftSkillsMetadata_RemainsPlanningOnly()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var registry = new RuntimeProviderRegistry();
        var runtimeService = new MicrosoftRuntimeProviderContractFrameworkService(registry);
        var definition = runtimeService.CreateDefaultProviderDefinition();
        var registration = runtimeService.CreateDefaultRegistration(definition, planning);

        registry.Register(registration);

        var runtime = runtimeService.CreateMicrosoftRuntimeState(planning, registration.ProviderId);

        Assert.NotNull(planning.MicrosoftSkillState);
        Assert.Equal(MicrosoftSkillReadinessState.ReadyForSkillProvider, planning.MicrosoftSkillState!.Readiness);
        Assert.NotNull(runtime.Request);
        Assert.NotNull(runtime.Context);
        Assert.Equal(MicrosoftSkillsCatalogContract.SchemaVersionV1, runtime.Request!.RequestMetadata.MicrosoftSkillsCatalogSchemaVersion);
        Assert.Equal(MicrosoftSkillReadinessState.ReadyForSkillProvider, runtime.Request.SkillRequirements.Readiness);
        Assert.Contains(runtime.Request.SkillRequirements.RequiredSkillIds, id => id == "microsoft.skill.powerbi.report-authoring");
        Assert.Equal(runtime.Request.SkillRequirements.RequiredSkillIds, runtime.Context!.MicrosoftSkillSummary.RequiredSkillIds);
    }

    [Fact(DisplayName = "Microsoft Skills readiness evaluates unsupported, partiallySatisfied, satisfied, and readyForSkillProvider states correctly")]
    public void MicrosoftSkillReadinessService_EvaluatesEveryStateCorrectly()
    {
        var readiness = new MicrosoftSkillReadinessService();
        var supportedValidation = new MicrosoftSkillCompatibilityValidationResult(MicrosoftSkillCompatibilityDiagnostics.Empty);
        var unsupportedValidation = new MicrosoftSkillCompatibilityValidationResult(
            new MicrosoftSkillCompatibilityDiagnostics(
                MissingRequiredSections: [],
                MissingRequiredFields: [],
                DuplicateSkillIds: [],
                UnsupportedTargetProfiles: [GenerationRequestContract.FabricAppDefaultProfile],
                UnsupportedCapabilities: ["deploymentSupport"],
                UnsatisfiedPrerequisites: [],
                VersionMismatches: [],
                IntegrityFailures: []));
        var satisfiedResolution = new MicrosoftSkillResolutionResult(
            ResolutionId: "microsoftSkillResolution:ready",
            TargetProfileId: GenerationRequestContract.PbirReportDefaultProfile,
            CandidateSkillSet:
            [
                new MicrosoftSkillCandidate(
                    SkillId: "microsoft.skill.powerbi.report-authoring",
                    SkillVersion: "1.0.0",
                    SkillStatus: MicrosoftSkillAvailabilityStatus.Available,
                    MatchedCapabilities: ["layoutGeneration", "pageGeneration", "semanticGeneration"]),
                new MicrosoftSkillCandidate(
                    SkillId: "microsoft.skill.powerbi.report-design",
                    SkillVersion: "1.0.0",
                    SkillStatus: MicrosoftSkillAvailabilityStatus.Available,
                    MatchedCapabilities: ["navigationGeneration"])
            ],
            RequiredSkills:
            [
                new MicrosoftSkillCandidate(
                    SkillId: "microsoft.skill.powerbi.report-authoring",
                    SkillVersion: "1.0.0",
                    SkillStatus: MicrosoftSkillAvailabilityStatus.Available,
                    MatchedCapabilities: ["layoutGeneration", "pageGeneration", "semanticGeneration"])
            ],
            OptionalSkills: [],
            CapabilityCoverage: new MicrosoftSkillCapabilityCoverageSummary(
                RequiredCapabilitiesRequested: ["layoutGeneration"],
                RequiredCapabilitiesCovered: ["layoutGeneration"],
                OptionalCapabilitiesRequested: [],
                OptionalCapabilitiesCovered: []),
            UnresolvedCapabilities: new MicrosoftSkillUnresolvedCapabilitySummary(
                RequiredCapabilities: [],
                OptionalCapabilities: [],
                UnsupportedCapabilities: []));
        var partialResolution = satisfiedResolution with
        {
            UnresolvedCapabilities = new MicrosoftSkillUnresolvedCapabilitySummary(
                RequiredCapabilities: ["navigationGeneration"],
                OptionalCapabilities: [],
                UnsupportedCapabilities: ["navigationGeneration"])
        };

        Assert.Equal(MicrosoftSkillReadinessState.Unsupported, readiness.Evaluate(unsupportedValidation, satisfiedResolution));
        Assert.Equal(MicrosoftSkillReadinessState.PartiallySatisfied, readiness.Evaluate(supportedValidation, partialResolution));
        Assert.Equal(MicrosoftSkillReadinessState.Satisfied, readiness.Evaluate(supportedValidation, satisfiedResolution));
        Assert.Equal(
            MicrosoftSkillReadinessState.ReadyForSkillProvider,
            readiness.PrepareForSkillProvider(MicrosoftSkillReadinessState.Satisfied, satisfiedResolution));
    }

    [Fact(DisplayName = "Microsoft Skills capability catalog framework remains metadata-only with no skill execution, API invocation, provider invocation, CLI execution, or artifact generation surface")]
    public void MicrosoftSkillsCatalogBoundary_RemainsMetadataOnly()
    {
        var forbiddenTokens = new[] { "Execute", "Invoke", "Api", "Cli", "GenerateArtifact", "Deploy", "AnalyzerRunner" };
        Type[] types =
        [
            typeof(MicrosoftSkillsCapabilityCatalogFrameworkService),
            typeof(MicrosoftSkillsCatalog),
            typeof(MicrosoftSkillResolutionService),
            typeof(MicrosoftSkillReadinessService),
            typeof(MicrosoftSkillCompatibilityValidator),
            typeof(MicrosoftSkillDefinition),
            typeof(MicrosoftSkillsCatalogDocument)
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

    [Fact(DisplayName = "Microsoft Skills contracts inventory the required field paths for skill definitions and the catalog document")]
    public void MicrosoftSkillsContracts_InventoryCoversRequiredFieldPaths()
    {
        var skillInventoryPaths = MicrosoftSkillDefinitionContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var skillModelPaths = EnumerateFieldPaths(typeof(MicrosoftSkillDefinition), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var catalogInventoryPaths = MicrosoftSkillsCatalogContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var catalogModelPaths = EnumerateFieldPaths(typeof(MicrosoftSkillsCatalogDocument), prefix: null)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(skillModelPaths.ToHashSet(StringComparer.Ordinal), skillInventoryPaths.ToHashSet(StringComparer.Ordinal));
        Assert.Subset(catalogModelPaths.ToHashSet(StringComparer.Ordinal), catalogInventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    private static string SerializeResolution(MicrosoftSkillResolutionResult result)
    {
        return string.Join("|",
            result.ResolutionId,
            result.TargetProfileId,
            string.Join(",", result.RequiredSkills.Select(skill => skill.SkillId)),
            string.Join(",", result.OptionalSkills.Select(skill => skill.SkillId)),
            string.Join(",", result.CapabilityCoverage.RequiredCapabilitiesCovered),
            string.Join(",", result.UnresolvedCapabilities.RequiredCapabilities),
            string.Join(",", result.UnresolvedCapabilities.UnsupportedCapabilities));
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

        return type.Namespace == typeof(MicrosoftSkillDefinition).Namespace ? type : null;
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
