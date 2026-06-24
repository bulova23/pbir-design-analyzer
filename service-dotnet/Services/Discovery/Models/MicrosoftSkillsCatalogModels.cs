using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class MicrosoftSkillDefinitionContract
{
    internal const string SchemaVersionV1 = "microsoft-skill-definition/v1";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "SkillId",
        "SkillName",
        "SkillVersion",
        "SkillCategory",
        "ProvidedCapabilities",
        "SupportedTargetProfiles",
        "SupportedExecutionModes",
        "UnsupportedCapabilities",
        "UnsupportedProfiles",
        "PrerequisiteCapabilities",
        "Status",
    ];
}

internal static class MicrosoftSkillsCatalogContract
{
    internal const string SchemaVersionV1 = "microsoft-skills-catalog/v1";
    internal const string CatalogId = "microsoft-skills-catalog";
    internal const string CatalogVersionV1 = "1.0.0";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "CatalogId",
        "CatalogVersion",
        "ProviderCategory",
        "Skills",
        "Skills.SchemaVersion",
        "Skills.SkillId",
        "Skills.SkillName",
        "Skills.SkillVersion",
        "Skills.SkillCategory",
        "Skills.ProvidedCapabilities",
        "Skills.SupportedTargetProfiles",
        "Skills.SupportedExecutionModes",
        "Skills.UnsupportedCapabilities",
        "Skills.UnsupportedProfiles",
        "Skills.PrerequisiteCapabilities",
        "Skills.Status",
    ];
}

internal enum MicrosoftSkillAvailabilityStatus
{
    Available,
    Planned,
    Deprecated,
    Unsupported,
}

internal enum MicrosoftSkillReadinessState
{
    Unsupported,
    PartiallySatisfied,
    Satisfied,
    ReadyForSkillProvider,
}

internal sealed record MicrosoftSkillDefinition(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("skillId")] string SkillId,
    [property: JsonPropertyName("skillName")] string SkillName,
    [property: JsonPropertyName("skillVersion")] string SkillVersion,
    [property: JsonPropertyName("skillCategory")] string SkillCategory,
    [property: JsonPropertyName("providedCapabilities")] IReadOnlyList<string> ProvidedCapabilities,
    [property: JsonPropertyName("supportedTargetProfiles")] IReadOnlyList<string> SupportedTargetProfiles,
    [property: JsonPropertyName("supportedExecutionModes")] IReadOnlyList<ExecutionProviderMode> SupportedExecutionModes,
    [property: JsonPropertyName("unsupportedCapabilities")] IReadOnlyList<string> UnsupportedCapabilities,
    [property: JsonPropertyName("unsupportedProfiles")] IReadOnlyList<string> UnsupportedProfiles,
    [property: JsonPropertyName("prerequisiteCapabilities")] IReadOnlyList<string> PrerequisiteCapabilities,
    [property: JsonPropertyName("status")] MicrosoftSkillAvailabilityStatus Status);

internal sealed record MicrosoftSkillsCatalogDocument(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("catalogId")] string CatalogId,
    [property: JsonPropertyName("catalogVersion")] string CatalogVersion,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory,
    [property: JsonPropertyName("skills")] IReadOnlyList<MicrosoftSkillDefinition> Skills);

internal sealed record MicrosoftSkillCandidate(
    [property: JsonPropertyName("skillId")] string SkillId,
    [property: JsonPropertyName("skillVersion")] string SkillVersion,
    [property: JsonPropertyName("skillStatus")] MicrosoftSkillAvailabilityStatus SkillStatus,
    [property: JsonPropertyName("matchedCapabilities")] IReadOnlyList<string> MatchedCapabilities);

internal sealed record MicrosoftSkillCapabilityCoverageSummary(
    [property: JsonPropertyName("requiredCapabilitiesRequested")] IReadOnlyList<string> RequiredCapabilitiesRequested,
    [property: JsonPropertyName("requiredCapabilitiesCovered")] IReadOnlyList<string> RequiredCapabilitiesCovered,
    [property: JsonPropertyName("optionalCapabilitiesRequested")] IReadOnlyList<string> OptionalCapabilitiesRequested,
    [property: JsonPropertyName("optionalCapabilitiesCovered")] IReadOnlyList<string> OptionalCapabilitiesCovered);

internal sealed record MicrosoftSkillUnresolvedCapabilitySummary(
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<string> RequiredCapabilities,
    [property: JsonPropertyName("optionalCapabilities")] IReadOnlyList<string> OptionalCapabilities,
    [property: JsonPropertyName("unsupportedCapabilities")] IReadOnlyList<string> UnsupportedCapabilities);

internal sealed record MicrosoftSkillResolutionResult(
    [property: JsonPropertyName("resolutionId")] string ResolutionId,
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("candidateSkillSet")] IReadOnlyList<MicrosoftSkillCandidate> CandidateSkillSet,
    [property: JsonPropertyName("requiredSkills")] IReadOnlyList<MicrosoftSkillCandidate> RequiredSkills,
    [property: JsonPropertyName("optionalSkills")] IReadOnlyList<MicrosoftSkillCandidate> OptionalSkills,
    [property: JsonPropertyName("capabilityCoverage")] MicrosoftSkillCapabilityCoverageSummary CapabilityCoverage,
    [property: JsonPropertyName("unresolvedCapabilities")] MicrosoftSkillUnresolvedCapabilitySummary UnresolvedCapabilities);

internal sealed record MicrosoftSkillCompatibilityDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> DuplicateSkillIds,
    IReadOnlyList<string> UnsupportedTargetProfiles,
    IReadOnlyList<string> UnsupportedCapabilities,
    IReadOnlyList<string> UnsatisfiedPrerequisites,
    IReadOnlyList<string> VersionMismatches,
    IReadOnlyList<string> IntegrityFailures)
{
    internal static MicrosoftSkillCompatibilityDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], []);

    internal bool HasFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        DuplicateSkillIds.Count > 0 ||
        UnsupportedTargetProfiles.Count > 0 ||
        UnsupportedCapabilities.Count > 0 ||
        UnsatisfiedPrerequisites.Count > 0 ||
        VersionMismatches.Count > 0 ||
        IntegrityFailures.Count > 0;
}

internal sealed record MicrosoftSkillCompatibilityValidationResult(
    MicrosoftSkillCompatibilityDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasFailures;
}

internal sealed record MicrosoftSkillPlanningState(
    MicrosoftSkillsCatalogDocument Catalog,
    CapabilityNegotiationResult? CapabilityNegotiationResult,
    MicrosoftSkillResolutionResult? Resolution,
    MicrosoftSkillCompatibilityValidationResult Validation,
    MicrosoftSkillReadinessState Readiness);

