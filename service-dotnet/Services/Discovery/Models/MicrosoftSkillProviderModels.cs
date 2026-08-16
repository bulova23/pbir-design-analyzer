using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class MicrosoftSkillProviderAdapterContract
{
    internal const string SchemaVersionV1 = "microsoft-skill-provider-adapter/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "AdapterId",
        "AdapterName",
        "AdapterVersion",
        "ProviderCategory",
        "ProviderSchemaVersion",
        "MicrosoftSkillsCatalogSchemaVersion",
        "SkillProviderSelectionSchemaVersion",
        "MicrosoftRuntimeProviderSchemaVersion",
        "SupportedTargetProfiles",
        "SupportedExecutionModes",
    ];
}

internal static class MicrosoftSkillProviderContract
{
    internal const string SchemaVersionV1 = "microsoft-skill-provider/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "ProviderId",
        "ProviderName",
        "ProviderVersion",
        "ProviderCategory",
        "ProviderStatus",
        "SupportedExecutionModes",
        "SupportedSkills",
        "SupportedCapabilities",
        "SupportedTargetProfiles",
    ];
}

internal static class SkillProviderSelectionContract
{
    internal const string SchemaVersionV1 = "skill-provider-selection/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "SelectionId",
        "TargetProfileId",
        "RequiredSkills",
        "CandidateProviders",
        "CandidateProviders.ProviderId",
        "CandidateProviders.ProviderVersion",
        "CandidateProviders.ProviderStatus",
        "CandidateProviders.MatchedSkills",
        "CandidateProviders.MatchedCapabilities",
        "CandidateProviders.MatchedTargetProfiles",
        "SelectedProviderCandidates",
        "SelectedProviderCandidates.ProviderId",
        "SelectedProviderCandidates.ProviderVersion",
        "SelectedProviderCandidates.ProviderStatus",
        "SelectedProviderCandidates.MatchedSkills",
        "SelectedProviderCandidates.MatchedCapabilities",
        "SelectedProviderCandidates.MatchedTargetProfiles",
        "UnsupportedSkills",
        "CoverageSummary",
        "CoverageSummary.RequiredSkillsRequested",
        "CoverageSummary.RequiredSkillsCovered",
        "CoverageSummary.OptionalSkillsRequested",
        "CoverageSummary.OptionalSkillsCovered",
        "CoverageSummary.RequiredCapabilitiesRequested",
        "CoverageSummary.RequiredCapabilitiesCovered",
        "CoverageSummary.OptionalCapabilitiesRequested",
        "CoverageSummary.OptionalCapabilitiesCovered",
        "CoverageSummary.UnresolvedRequiredCapabilities",
        "CoverageSummary.UnresolvedOptionalCapabilities",
        "CoverageSummary.SupportedTargetProfiles",
        "ReadinessSummary",
        "ReadinessSummary.Readiness",
        "ReadinessSummary.KnownProviderIds",
        "ReadinessSummary.BlockingIssues",
        "ReadinessSummary.UnresolvedSkills",
        "ReadinessSummary.UnresolvedCapabilities",
    ];
}

internal enum MicrosoftSkillProviderStatus
{
    Available,
    Planned,
    Deprecated,
    Unsupported,
}

internal enum MicrosoftSkillProviderReadinessState
{
    Unsupported,
    PartiallySatisfied,
    Satisfied,
    ReadyForSkillProviderAdapter,
}

internal sealed record MicrosoftSkillProviderAdapterDefinition(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("adapterId")] string AdapterId,
    [property: JsonPropertyName("adapterName")] string AdapterName,
    [property: JsonPropertyName("adapterVersion")] string AdapterVersion,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory,
    [property: JsonPropertyName("providerSchemaVersion")] string ProviderSchemaVersion,
    [property: JsonPropertyName("microsoftSkillsCatalogSchemaVersion")] string MicrosoftSkillsCatalogSchemaVersion,
    [property: JsonPropertyName("skillProviderSelectionSchemaVersion")] string SkillProviderSelectionSchemaVersion,
    [property: JsonPropertyName("microsoftRuntimeProviderSchemaVersion")] string MicrosoftRuntimeProviderSchemaVersion,
    [property: JsonPropertyName("supportedTargetProfiles")] IReadOnlyList<string> SupportedTargetProfiles,
    [property: JsonPropertyName("supportedExecutionModes")] IReadOnlyList<ExecutionProviderMode> SupportedExecutionModes);

internal sealed record MicrosoftSkillProviderDefinition(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerName")] string ProviderName,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory,
    [property: JsonPropertyName("providerStatus")] MicrosoftSkillProviderStatus ProviderStatus,
    [property: JsonPropertyName("supportedExecutionModes")] IReadOnlyList<ExecutionProviderMode> SupportedExecutionModes,
    [property: JsonPropertyName("supportedSkills")] IReadOnlyList<string> SupportedSkills,
    [property: JsonPropertyName("supportedCapabilities")] IReadOnlyList<string> SupportedCapabilities,
    [property: JsonPropertyName("supportedTargetProfiles")] IReadOnlyList<string> SupportedTargetProfiles);

internal sealed record MicrosoftSkillProviderCandidate(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerVersion")] string ProviderVersion,
    [property: JsonPropertyName("providerStatus")] MicrosoftSkillProviderStatus ProviderStatus,
    [property: JsonPropertyName("matchedSkills")] IReadOnlyList<string> MatchedSkills,
    [property: JsonPropertyName("matchedCapabilities")] IReadOnlyList<string> MatchedCapabilities,
    [property: JsonPropertyName("matchedTargetProfiles")] IReadOnlyList<string> MatchedTargetProfiles);

internal sealed record MicrosoftSkillProviderCoverageSummary(
    [property: JsonPropertyName("requiredSkillsRequested")] IReadOnlyList<string> RequiredSkillsRequested,
    [property: JsonPropertyName("requiredSkillsCovered")] IReadOnlyList<string> RequiredSkillsCovered,
    [property: JsonPropertyName("optionalSkillsRequested")] IReadOnlyList<string> OptionalSkillsRequested,
    [property: JsonPropertyName("optionalSkillsCovered")] IReadOnlyList<string> OptionalSkillsCovered,
    [property: JsonPropertyName("requiredCapabilitiesRequested")] IReadOnlyList<string> RequiredCapabilitiesRequested,
    [property: JsonPropertyName("requiredCapabilitiesCovered")] IReadOnlyList<string> RequiredCapabilitiesCovered,
    [property: JsonPropertyName("optionalCapabilitiesRequested")] IReadOnlyList<string> OptionalCapabilitiesRequested,
    [property: JsonPropertyName("optionalCapabilitiesCovered")] IReadOnlyList<string> OptionalCapabilitiesCovered,
    [property: JsonPropertyName("unresolvedRequiredCapabilities")] IReadOnlyList<string> UnresolvedRequiredCapabilities,
    [property: JsonPropertyName("unresolvedOptionalCapabilities")] IReadOnlyList<string> UnresolvedOptionalCapabilities,
    [property: JsonPropertyName("supportedTargetProfiles")] IReadOnlyList<string> SupportedTargetProfiles);

internal sealed record MicrosoftSkillProviderSelectionReadinessSummary(
    [property: JsonPropertyName("readiness")] MicrosoftSkillProviderReadinessState Readiness,
    [property: JsonPropertyName("knownProviderIds")] IReadOnlyList<string> KnownProviderIds,
    [property: JsonPropertyName("blockingIssues")] IReadOnlyList<string> BlockingIssues,
    [property: JsonPropertyName("unresolvedSkills")] IReadOnlyList<string> UnresolvedSkills,
    [property: JsonPropertyName("unresolvedCapabilities")] IReadOnlyList<string> UnresolvedCapabilities);

internal sealed record SkillProviderSelection(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("selectionId")] string SelectionId,
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("requiredSkills")] IReadOnlyList<string> RequiredSkills,
    [property: JsonPropertyName("candidateProviders")] IReadOnlyList<MicrosoftSkillProviderCandidate> CandidateProviders,
    [property: JsonPropertyName("selectedProviderCandidates")] IReadOnlyList<MicrosoftSkillProviderCandidate> SelectedProviderCandidates,
    [property: JsonPropertyName("unsupportedSkills")] IReadOnlyList<string> UnsupportedSkills,
    [property: JsonPropertyName("coverageSummary")] MicrosoftSkillProviderCoverageSummary CoverageSummary,
    [property: JsonPropertyName("readinessSummary")] MicrosoftSkillProviderSelectionReadinessSummary ReadinessSummary);

internal sealed record MicrosoftSkillProviderCompatibilityDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> DuplicateProviderIds,
    IReadOnlyList<string> UnsupportedTargetProfiles,
    IReadOnlyList<string> UnsupportedSkills,
    IReadOnlyList<string> UnsupportedCapabilities,
    IReadOnlyList<string> UnsatisfiedPrerequisites,
    IReadOnlyList<string> VersionMismatches,
    IReadOnlyList<string> IntegrityFailures)
{
    internal static MicrosoftSkillProviderCompatibilityDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], [], []);

    internal bool HasFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        DuplicateProviderIds.Count > 0 ||
        UnsupportedTargetProfiles.Count > 0 ||
        UnsupportedSkills.Count > 0 ||
        UnsupportedCapabilities.Count > 0 ||
        UnsatisfiedPrerequisites.Count > 0 ||
        VersionMismatches.Count > 0 ||
        IntegrityFailures.Count > 0;
}

internal sealed record MicrosoftSkillProviderCompatibilityValidationResult(
    MicrosoftSkillProviderCompatibilityDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasFailures;
}

internal sealed record MicrosoftSkillProviderPlanningState(
    MicrosoftSkillProviderAdapterDefinition Adapter,
    IReadOnlyList<MicrosoftSkillProviderDefinition> Providers,
    MicrosoftSkillPlanningState SkillPlanningState,
    SkillProviderSelection? Selection,
    MicrosoftSkillProviderCompatibilityValidationResult Validation,
    MicrosoftSkillProviderReadinessState Readiness);
