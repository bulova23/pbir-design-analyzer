using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class MicrosoftAdapterSpecificationContract
{
    internal const string SchemaVersionV1 = "microsoft-adapter-specification/v1";
    internal const string SpecificationId = "microsoft-adapter-specification";
    internal const string SpecificationVersionV1 = "1.0.0";
    internal const string ProviderId = "microsoftPowerBi";
    internal const string ProviderCategory = "microsoft";
    internal const string ProviderDisplayName = "Microsoft Power BI Skills";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaMetadata",
        "SchemaMetadata.SchemaVersion",
        "SchemaMetadata.SpecificationId",
        "SchemaMetadata.SpecificationVersion",
        "ProviderIdentity",
        "ProviderIdentity.ProviderId",
        "ProviderIdentity.ProviderCategory",
        "ProviderIdentity.ProviderDisplayName",
        "SupportedTargetProfiles",
        "SupportedTargetProfiles.TargetProfileId",
        "SupportedTargetProfiles.ArtifactType",
        "SupportedTargetProfiles.SupportStatus",
        "CapabilityMappings",
        "CapabilityMappings.CapabilityId",
        "CapabilityMappings.ProviderCapabilityRequirements",
        "CapabilityMappings.SupportStatus",
        "TargetProfileMappings",
        "TargetProfileMappings.TargetProfileId",
        "TargetProfileMappings.RequiredCapabilities",
        "TargetProfileMappings.OptionalCapabilities",
        "TargetProfileMappings.UnsupportedCapabilities",
        "TargetProfileMappings.PlanningRequirements",
        "CompatibilityCatalog",
        "CompatibilityCatalog.SupportedCombinations",
        "CompatibilityCatalog.UnsupportedCombinations",
        "CompatibilityCatalog.FutureCombinations",
        "ConstraintCatalog",
        "ConstraintCatalog.UnsupportedArtifactTypes",
        "ConstraintCatalog.UnsupportedExperienceTypes",
        "ConstraintCatalog.UnsupportedCapabilityCombinations",
        "ReviewRequirementsCatalog",
        "ReviewRequirementsCatalog.DesignApprovalRequired",
        "ReviewRequirementsCatalog.GenerationApprovalRequired",
        "ReviewRequirementsCatalog.AnalyzerValidationRequired",
        "ReviewRequirementsCatalog.InheritedContractVersions",
    ];
}

internal sealed record MicrosoftAdapterSpecification(
    [property: JsonPropertyName("schemaMetadata")] MicrosoftAdapterSchemaMetadata SchemaMetadata,
    [property: JsonPropertyName("providerIdentity")] MicrosoftAdapterProviderIdentity ProviderIdentity,
    [property: JsonPropertyName("supportedTargetProfiles")] IReadOnlyList<MicrosoftAdapterTargetProfileSupport> SupportedTargetProfiles,
    [property: JsonPropertyName("capabilityMappings")] IReadOnlyList<MicrosoftAdapterCapabilityMapping> CapabilityMappings,
    [property: JsonPropertyName("targetProfileMappings")] IReadOnlyList<MicrosoftAdapterTargetProfileMapping> TargetProfileMappings,
    [property: JsonPropertyName("compatibilityCatalog")] MicrosoftAdapterCompatibilityCatalog CompatibilityCatalog,
    [property: JsonPropertyName("constraintCatalog")] MicrosoftAdapterConstraintCatalog ConstraintCatalog,
    [property: JsonPropertyName("reviewRequirementsCatalog")] MicrosoftAdapterReviewRequirementsCatalog ReviewRequirementsCatalog);

internal sealed record MicrosoftAdapterSchemaMetadata(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("specificationId")] string SpecificationId,
    [property: JsonPropertyName("specificationVersion")] string SpecificationVersion);

internal sealed record MicrosoftAdapterProviderIdentity(
    [property: JsonPropertyName("providerId")] string ProviderId,
    [property: JsonPropertyName("providerCategory")] string ProviderCategory,
    [property: JsonPropertyName("providerDisplayName")] string ProviderDisplayName);

internal enum MicrosoftAdapterSupportStatus
{
    Supported,
    Planned,
    Unsupported,
}

internal sealed record MicrosoftAdapterTargetProfileSupport(
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("artifactType")] string ArtifactType,
    [property: JsonPropertyName("supportStatus")] MicrosoftAdapterSupportStatus SupportStatus,
    [property: JsonPropertyName("notes")] string Notes);

internal sealed record MicrosoftAdapterCapabilityMapping(
    [property: JsonPropertyName("capabilityId")] string CapabilityId,
    [property: JsonPropertyName("providerCapabilityRequirements")] IReadOnlyList<string> ProviderCapabilityRequirements,
    [property: JsonPropertyName("supportStatus")] MicrosoftAdapterSupportStatus SupportStatus,
    [property: JsonPropertyName("notes")] string Notes);

internal sealed record MicrosoftAdapterTargetProfileMapping(
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<string> RequiredCapabilities,
    [property: JsonPropertyName("optionalCapabilities")] IReadOnlyList<string> OptionalCapabilities,
    [property: JsonPropertyName("unsupportedCapabilities")] IReadOnlyList<string> UnsupportedCapabilities,
    [property: JsonPropertyName("planningRequirements")] IReadOnlyList<string> PlanningRequirements);

internal sealed record MicrosoftAdapterCompatibilityCatalog(
    [property: JsonPropertyName("supportedCombinations")] IReadOnlyList<MicrosoftAdapterCapabilityCombination> SupportedCombinations,
    [property: JsonPropertyName("unsupportedCombinations")] IReadOnlyList<MicrosoftAdapterCapabilityCombination> UnsupportedCombinations,
    [property: JsonPropertyName("futureCombinations")] IReadOnlyList<MicrosoftAdapterCapabilityCombination> FutureCombinations);

internal sealed record MicrosoftAdapterCapabilityCombination(
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("capabilityRequirements")] IReadOnlyList<string> CapabilityRequirements,
    [property: JsonPropertyName("notes")] string Notes);

internal sealed record MicrosoftAdapterConstraintCatalog(
    [property: JsonPropertyName("unsupportedArtifactTypes")] IReadOnlyList<string> UnsupportedArtifactTypes,
    [property: JsonPropertyName("unsupportedExperienceTypes")] IReadOnlyList<string> UnsupportedExperienceTypes,
    [property: JsonPropertyName("unsupportedCapabilityCombinations")] IReadOnlyList<MicrosoftAdapterUnsupportedCapabilityCombination> UnsupportedCapabilityCombinations);

internal sealed record MicrosoftAdapterUnsupportedCapabilityCombination(
    [property: JsonPropertyName("capabilityRequirements")] IReadOnlyList<string> CapabilityRequirements,
    [property: JsonPropertyName("reason")] string Reason);

internal sealed record MicrosoftAdapterReviewRequirementsCatalog(
    [property: JsonPropertyName("designApprovalRequired")] bool DesignApprovalRequired,
    [property: JsonPropertyName("generationApprovalRequired")] bool GenerationApprovalRequired,
    [property: JsonPropertyName("analyzerValidationRequired")] bool AnalyzerValidationRequired,
    [property: JsonPropertyName("inheritedContractVersions")] IReadOnlyList<string> InheritedContractVersions);

internal enum MicrosoftAdapterCombinationStatus
{
    Supported,
    Unsupported,
    Future,
}

internal enum MicrosoftAdapterPlanningReadinessState
{
    Unsupported,
    PartiallySupported,
    Supported,
    ReadyForMicrosoftAdapter,
}

internal sealed record MicrosoftProviderPlanningTranslation(
    [property: JsonPropertyName("targetProfileId")] string TargetProfileId,
    [property: JsonPropertyName("sourceCapabilityRequirements")] IReadOnlyList<string> SourceCapabilityRequirements,
    [property: JsonPropertyName("resolvedCapabilityRequirements")] IReadOnlyList<string> ResolvedCapabilityRequirements,
    [property: JsonPropertyName("requiredCapabilities")] IReadOnlyList<string> RequiredCapabilities,
    [property: JsonPropertyName("missingCapabilities")] IReadOnlyList<string> MissingCapabilities,
    [property: JsonPropertyName("planningRequirements")] IReadOnlyList<string> PlanningRequirements);

internal sealed record MicrosoftAdapterSpecificationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> UnsupportedSchemaVersions,
    IReadOnlyList<string> UnsupportedTargetProfiles,
    IReadOnlyList<string> UnsupportedCapabilityRequirements,
    IReadOnlyList<string> FutureTargetProfiles,
    IReadOnlyList<string> FutureCapabilityRequirements,
    IReadOnlyList<string> ConstraintFailures,
    IReadOnlyList<string> ReviewRequirementFailures)
{
    internal static MicrosoftAdapterSpecificationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], [], []);

    internal bool HasValidationFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        UnsupportedSchemaVersions.Count > 0 ||
        ReviewRequirementFailures.Count > 0;
}

internal sealed record MicrosoftAdapterSpecificationValidationResult(
    MicrosoftAdapterSpecificationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasValidationFailures;
}

internal sealed record MicrosoftAdapterPlanningState(
    MicrosoftAdapterSpecification Specification,
    ProviderAdapterRequest? AdapterRequest,
    ExecutionPlan? ExecutionPlan,
    MicrosoftProviderPlanningTranslation? Translation,
    MicrosoftAdapterCombinationStatus CompatibilityStatus,
    MicrosoftAdapterPlanningReadinessState Readiness,
    MicrosoftAdapterSpecificationDiagnostics Diagnostics);
