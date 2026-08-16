using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class CapabilityNegotiationContract
{
    internal const string SchemaVersionV1 = "capability-negotiation/v1";
    internal const string SubstitutionCatalogSchemaVersionV1 = "capability-substitution-catalog/v1";
    internal const string DefaultSubstitutionCatalogId = "default-capability-substitution-catalog";
    internal const string DefaultSubstitutionCatalogVersionV1 = "1.0.0";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "NegotiationId",
        "TargetProfileId",
        "ProviderCategory",
        "Requirements",
        "Requirements.CapabilityId",
        "Requirements.CapabilityCategory",
        "Requirements.RequirementLevel",
        "Requirements.SourceContract",
        "Requirements.ProviderCapabilityRequirements",
        "Resolutions",
        "Resolutions.CapabilityId",
        "Resolutions.CapabilityCategory",
        "Resolutions.RequirementLevel",
        "Resolutions.Resolution",
        "Resolutions.ResolvedCapabilityId",
        "Resolutions.ResolutionReason",
        "Resolutions.SourceContract",
        "Substitutions",
        "Substitutions.RuleId",
        "Substitutions.OriginalCapabilityId",
        "Substitutions.SubstituteCapabilityId",
        "Substitutions.SubstitutionReason",
        "Substitutions.AppliesToTargetProfileId",
        "ResolutionSummary",
        "ResolutionSummary.SatisfiedCount",
        "ResolutionSummary.SubstitutedCount",
        "ResolutionSummary.UnsupportedCount",
        "ResolutionSummary.BlockedCount",
        "ResolutionSummary.OmittedCount",
        "ResolutionSummary.AllRequiredCapabilitiesSatisfied",
        "ReadinessStatus",
    ];
}

internal enum CapabilityRequirementLevel
{
    Required,
    Preferred,
    Optional,
}

internal enum CapabilityResolutionStatus
{
    Satisfied,
    Substituted,
    Unsupported,
    Blocked,
    Omitted,
}

internal enum CapabilityNegotiationReadinessState
{
    Unresolved,
    PartiallyResolved,
    Resolved,
    Blocked,
    ReadyForExecutionProvider,
}

internal sealed record CapabilityRequirement(
    [property: JsonPropertyName("capabilityId")] string CapabilityId,
    [property: JsonPropertyName("capabilityCategory")] string CapabilityCategory,
    [property: JsonPropertyName("requirementLevel")] CapabilityRequirementLevel RequirementLevel,
    [property: JsonPropertyName("sourceContract")] string SourceContract,
    [property: JsonPropertyName("providerCapabilityRequirements")] IReadOnlyList<string> ProviderCapabilityRequirements);

internal sealed record CapabilityResolution(
    [property: JsonPropertyName("capabilityId")] string CapabilityId,
    [property: JsonPropertyName("capabilityCategory")] string CapabilityCategory,
    [property: JsonPropertyName("requirementLevel")] CapabilityRequirementLevel RequirementLevel,
    [property: JsonPropertyName("resolution")] CapabilityResolutionStatus Resolution,
    [property: JsonPropertyName("resolvedCapabilityId")] string? ResolvedCapabilityId,
    [property: JsonPropertyName("resolutionReason")] string ResolutionReason,
    [property: JsonPropertyName("sourceContract")] string SourceContract);

internal sealed record CapabilityNegotiationSubstitutionRule(
    [property: JsonPropertyName("ruleId")] string RuleId,
    [property: JsonPropertyName("originalCapabilityId")] string OriginalCapabilityId,
    [property: JsonPropertyName("substituteCapabilityId")] string SubstituteCapabilityId,
    [property: JsonPropertyName("appliesToTargetProfileId")] string AppliesToTargetProfileId,
    [property: JsonPropertyName("substitutionReason")] string SubstitutionReason);

internal sealed record CapabilitySubstitution(
    [property: JsonPropertyName("ruleId")] string RuleId,
    [property: JsonPropertyName("originalCapabilityId")] string OriginalCapabilityId,
    [property: JsonPropertyName("substituteCapabilityId")] string SubstituteCapabilityId,
    [property: JsonPropertyName("substitutionReason")] string SubstitutionReason,
    [property: JsonPropertyName("appliesToTargetProfileId")] string AppliesToTargetProfileId);

internal sealed record CapabilityNegotiationSubstitutionCatalog(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("catalogId")] string CatalogId,
    [property: JsonPropertyName("catalogVersion")] string CatalogVersion,
    [property: JsonPropertyName("rules")] IReadOnlyList<CapabilityNegotiationSubstitutionRule> Rules);

internal sealed record CapabilityNegotiationResolutionSummary(
    [property: JsonPropertyName("satisfiedCount")] int SatisfiedCount,
    [property: JsonPropertyName("substitutedCount")] int SubstitutedCount,
    [property: JsonPropertyName("unsupportedCount")] int UnsupportedCount,
    [property: JsonPropertyName("blockedCount")] int BlockedCount,
    [property: JsonPropertyName("omittedCount")] int OmittedCount,
    [property: JsonPropertyName("allRequiredCapabilitiesSatisfied")] bool AllRequiredCapabilitiesSatisfied);

internal sealed record CapabilityNegotiationResult(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("negotiationId")] string NegotiationId,
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory,
    [property: JsonPropertyName("requirements")] IReadOnlyList<CapabilityRequirement> Requirements,
    [property: JsonPropertyName("resolutions")] IReadOnlyList<CapabilityResolution> Resolutions,
    [property: JsonPropertyName("substitutions")] IReadOnlyList<CapabilitySubstitution> Substitutions,
    [property: JsonPropertyName("resolutionSummary")] CapabilityNegotiationResolutionSummary ResolutionSummary,
    [property: JsonPropertyName("readinessStatus")] CapabilityNegotiationReadinessState ReadinessStatus);

internal sealed record CapabilityNegotiationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> MissingCapabilityDefinitions,
    IReadOnlyList<string> InvalidSubstitutions,
    IReadOnlyList<string> CircularSubstitutions,
    IReadOnlyList<string> UnsupportedRequiredCapabilities,
    IReadOnlyList<string> VersionMismatches,
    IReadOnlyList<string> CompatibilityFailures)
{
    internal static CapabilityNegotiationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], []);

    internal bool HasFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        MissingCapabilityDefinitions.Count > 0 ||
        InvalidSubstitutions.Count > 0 ||
        CircularSubstitutions.Count > 0 ||
        UnsupportedRequiredCapabilities.Count > 0 ||
        VersionMismatches.Count > 0 ||
        CompatibilityFailures.Count > 0;
}

internal sealed record CapabilityNegotiationValidationResult(
    CapabilityNegotiationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasFailures;
}

internal sealed record CapabilityNegotiationFrameworkState(
    GenerationRequest? GenerationRequest,
    ExecutionPlan? ExecutionPlan,
    ProviderAdapterRequest? AdapterRequest,
    ProviderAdapterDefinition? AdapterDefinition,
    MicrosoftAdapterSpecification? Specification,
    CapabilityNegotiationResult? Result,
    CapabilityNegotiationReadinessState Readiness,
    CapabilityNegotiationDiagnostics Diagnostics);
