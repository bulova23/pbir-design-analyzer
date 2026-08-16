using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class GenerationRequestContract
{
    internal const string SchemaVersionV1 = "generation-request/v1";
    internal const string AdvisoryConstructionOnlyAuthority = "advisoryConstructionOnly";
    internal const string ProviderNeutralAdapterFamily = "providerNeutral";
    internal const string PromptSegmentsOnlyExecutionMode = "promptSegmentsOnly";
    internal const string PbirReportDefaultProfile = "pbirReport/default";
    internal const string FabricDataAppDefaultProfile = "fabricDataApp/default";
    internal const string FabricAppDefaultProfile = "fabricApp/default";

    internal static IReadOnlyList<string> RequiredFieldInventory { get; } =
    [
        "SchemaVersion",
        "RequestId",
        "SourceDesignPackageRef",
        "TargetArtifactProfile",
        "TargetArtifactProfile.ArtifactType",
        "TargetArtifactProfile.ProfileId",
        "TargetArtifactProfile.SourceExperienceType",
        "GenerationMode",
        "GenerationMode.Authority",
        "GenerationMode.ReviewRequired",
        "GenerationMode.AllowPartialOutput",
        "DesignIntent",
        "DesignIntent.PrimaryAudience",
        "DesignIntent.SecondaryAudiences",
        "DesignIntent.BusinessOutcome",
        "DesignIntent.AnalyticalFlow",
        "DesignIntent.AnalyticalFlow.Question",
        "DesignIntent.AnalyticalFlow.Investigation",
        "DesignIntent.AnalyticalFlow.Evidence",
        "DesignIntent.AnalyticalFlow.Decision",
        "StructuralIntent",
        "StructuralIntent.Pages",
        "StructuralIntent.Pages.Name",
        "StructuralIntent.Pages.Purpose",
        "StructuralIntent.Pages.NavigationIntent",
        "StructuralIntent.Navigation",
        "StructuralIntent.Navigation.Hierarchy",
        "StructuralIntent.Navigation.WorkflowPath",
        "StructuralIntent.VisualHints",
        "StructuralIntent.VisualHints.PageName",
        "StructuralIntent.VisualHints.VisualType",
        "StructuralIntent.VisualHints.VisualPurpose",
        "DataIntent",
        "DataIntent.Kpis",
        "DataIntent.Kpis.Name",
        "DataIntent.Kpis.Purpose",
        "DataIntent.Kpis.Grouping",
        "DataIntent.Filters",
        "DataIntent.Filters.GlobalFilters",
        "DataIntent.Filters.PageFilters",
        "DataIntent.Filters.PageFilters.PageName",
        "DataIntent.Filters.PageFilters.Filters",
        "DataIntent.SemanticBinding",
        "DataIntent.SemanticBinding.SemanticModelRef",
        "DataIntent.SemanticBinding.SemanticModelLabel",
        "SuccessContract",
        "SuccessContract.BusinessSuccessCriteria",
        "SuccessContract.AnalyticalSuccessCriteria",
        "SuccessContract.ValidationRequirements",
        "Provenance",
        "Provenance.SourceDesignPackageRef",
        "Provenance.Lineage",
        "Provenance.Lineage.Stage",
        "Provenance.Lineage.ReferenceId",
        "Provenance.Lineage.Label",
        "Provenance.AdapterMetadata",
        "Provenance.AdapterMetadata.AdapterFamily",
        "Provenance.AdapterMetadata.ExecutionMode",
        "Provenance.AdapterMetadata.ProviderSpecificExecution",
        "ReviewPolicy",
        "ReviewPolicy.DesignApprovalRequired",
        "ReviewPolicy.GenerationApprovalRequired",
        "ReviewPolicy.AnalyzerReviewRequired",
    ];
}

internal enum GenerationRequestArtifactType
{
    PbirReport,
    FabricDataApp,
    FabricApp,
}

internal sealed record GenerationRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("sourceDesignPackageRef")] string SourceDesignPackageRef,
    [property: JsonPropertyName("targetArtifactProfile")] GenerationRequestTargetArtifactProfile TargetArtifactProfile,
    [property: JsonPropertyName("generationMode")] GenerationRequestMode GenerationMode,
    [property: JsonPropertyName("designIntent")] GenerationRequestDesignIntent DesignIntent,
    [property: JsonPropertyName("structuralIntent")] GenerationRequestStructuralIntent StructuralIntent,
    [property: JsonPropertyName("dataIntent")] GenerationRequestDataIntent DataIntent,
    [property: JsonPropertyName("successContract")] GenerationRequestSuccessContract SuccessContract,
    [property: JsonPropertyName("provenance")] GenerationRequestProvenance Provenance,
    [property: JsonPropertyName("reviewPolicy")] GenerationRequestReviewPolicy ReviewPolicy);

internal sealed record GenerationRequestTargetArtifactProfile(
    [property: JsonPropertyName("artifactType")] GenerationRequestArtifactType ArtifactType,
    [property: JsonPropertyName("profileId")] string ProfileId = "",
    [property: JsonPropertyName("sourceExperienceType")] OpportunityExperienceType SourceExperienceType = OpportunityExperienceType.PbirReport);

internal sealed record GenerationRequestMode(
    [property: JsonPropertyName("authority")] string Authority,
    [property: JsonPropertyName("reviewRequired")] bool ReviewRequired,
    [property: JsonPropertyName("allowPartialOutput")] bool AllowPartialOutput);

internal sealed record GenerationRequestDesignIntent(
    [property: JsonPropertyName("primaryAudience")] string PrimaryAudience,
    [property: JsonPropertyName("secondaryAudiences")] IReadOnlyList<string> SecondaryAudiences,
    [property: JsonPropertyName("businessOutcome")] string BusinessOutcome,
    [property: JsonPropertyName("analyticalFlow")] GenerationRequestAnalyticalFlow AnalyticalFlow);

internal sealed record GenerationRequestAnalyticalFlow(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("investigation")] string Investigation,
    [property: JsonPropertyName("evidence")] string Evidence,
    [property: JsonPropertyName("decision")] string Decision);

internal sealed record GenerationRequestStructuralIntent(
    [property: JsonPropertyName("pages")] IReadOnlyList<GenerationRequestPageIntent> Pages,
    [property: JsonPropertyName("navigation")] GenerationRequestNavigationIntent Navigation,
    [property: JsonPropertyName("visualHints")] IReadOnlyList<GenerationRequestVisualHint> VisualHints);

internal sealed record GenerationRequestPageIntent(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("navigationIntent")] string NavigationIntent);

internal sealed record GenerationRequestNavigationIntent(
    [property: JsonPropertyName("hierarchy")] IReadOnlyList<string> Hierarchy,
    [property: JsonPropertyName("workflowPath")] IReadOnlyList<string> WorkflowPath);

internal sealed record GenerationRequestVisualHint(
    [property: JsonPropertyName("pageName")] string PageName,
    [property: JsonPropertyName("visualType")] string VisualType,
    [property: JsonPropertyName("visualPurpose")] string VisualPurpose);

internal sealed record GenerationRequestDataIntent(
    [property: JsonPropertyName("kpis")] IReadOnlyList<GenerationRequestKpiIntent> Kpis,
    [property: JsonPropertyName("filters")] GenerationRequestFilters Filters,
    [property: JsonPropertyName("semanticBinding")] GenerationRequestSemanticBinding SemanticBinding);

internal sealed record GenerationRequestKpiIntent(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("grouping")] string Grouping);

internal sealed record GenerationRequestFilters(
    [property: JsonPropertyName("globalFilters")] IReadOnlyList<string> GlobalFilters,
    [property: JsonPropertyName("pageFilters")] IReadOnlyList<GenerationRequestPageFilter> PageFilters);

internal sealed record GenerationRequestPageFilter(
    [property: JsonPropertyName("pageName")] string PageName,
    [property: JsonPropertyName("filters")] IReadOnlyList<string> Filters);

internal sealed record GenerationRequestSemanticBinding(
    [property: JsonPropertyName("semanticModelRef")] string SemanticModelRef,
    [property: JsonPropertyName("semanticModelLabel")] string SemanticModelLabel);

internal sealed record GenerationRequestSuccessContract(
    [property: JsonPropertyName("businessSuccessCriteria")] IReadOnlyList<string> BusinessSuccessCriteria,
    [property: JsonPropertyName("analyticalSuccessCriteria")] IReadOnlyList<string> AnalyticalSuccessCriteria,
    [property: JsonPropertyName("validationRequirements")] IReadOnlyList<string> ValidationRequirements);

internal sealed record GenerationRequestProvenance(
    [property: JsonPropertyName("sourceDesignPackageRef")] string SourceDesignPackageRef,
    [property: JsonPropertyName("lineage")] IReadOnlyList<DesignPackageReference> Lineage,
    [property: JsonPropertyName("adapterMetadata")] GenerationRequestAdapterMetadata AdapterMetadata);

internal sealed record GenerationRequestAdapterMetadata(
    [property: JsonPropertyName("adapterFamily")] string AdapterFamily,
    [property: JsonPropertyName("executionMode")] string ExecutionMode,
    [property: JsonPropertyName("providerSpecificExecution")] bool ProviderSpecificExecution);

internal sealed record GenerationRequestReviewPolicy(
    [property: JsonPropertyName("designApprovalRequired")] bool DesignApprovalRequired,
    [property: JsonPropertyName("generationApprovalRequired")] bool GenerationApprovalRequired,
    [property: JsonPropertyName("analyzerReviewRequired")] bool AnalyzerReviewRequired);

internal sealed record GenerationRequestPromptSegment(
    int Order,
    string Title,
    string Content);

internal enum GenerationRequestReadinessState
{
    Draft,
    Valid,
    Blocked,
    ReadyForProviderPlanning,
}

internal sealed record GenerationRequestValidationDiagnostics(
    IReadOnlyList<string> MissingRequiredSections,
    IReadOnlyList<string> MissingRequiredFields,
    IReadOnlyList<string> MissingInputs,
    IReadOnlyList<string> UnsupportedTargetProfiles,
    IReadOnlyList<string> UnsupportedSchemaVersions,
    IReadOnlyList<string> CompatibilityFailures)
{
    internal static GenerationRequestValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], []);

    internal bool HasFailures =>
        MissingRequiredSections.Count > 0 ||
        MissingRequiredFields.Count > 0 ||
        MissingInputs.Count > 0 ||
        UnsupportedTargetProfiles.Count > 0 ||
        UnsupportedSchemaVersions.Count > 0 ||
        CompatibilityFailures.Count > 0;
}

internal sealed record GenerationRequestValidationResult(
    GenerationRequestValidationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasFailures;
}

internal sealed record GenerationRequestCreationResult(
    GenerationRequest? Request,
    GenerationRequestValidationDiagnostics Diagnostics)
{
    internal bool IsValid => !Diagnostics.HasFailures;
}

internal sealed record GenerationRequestFrameworkState(
    GenerationRequest? Request,
    GenerationRequestReadinessState Readiness,
    GenerationRequestValidationDiagnostics Diagnostics,
    IReadOnlyList<GenerationRequestPromptSegment> PromptSegments);
