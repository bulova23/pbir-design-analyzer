using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class DesignStudioExecutionReadinessContract
{
    internal const string SchemaVersionV1 = "design-studio-execution-readiness/v1";
}

internal enum DesignStudioExecutionReadinessSummary
{
    NotReady,
    ReadyForDesignReview,
    ReadyForAnalyzerReview,
    ReadyForGenerationProvider,
    Blocked,
}

internal enum DesignStudioExecutionPreviewReviewStatus
{
    NotAvailable,
    Pending,
    MarkedReviewed,
    RevisionRequested,
    Deferred,
    AnalyzerCandidateMetadataPrepared,
}

internal sealed record DesignStudioExecutionReadinessBoundaryRequests(
    [property: JsonPropertyName("executionRequested")] bool ExecutionRequested,
    [property: JsonPropertyName("providerInvocationRequested")] bool ProviderInvocationRequested,
    [property: JsonPropertyName("microsoftSkillsExecutionRequested")] bool MicrosoftSkillsExecutionRequested,
    [property: JsonPropertyName("apiInvocationRequested")] bool ApiInvocationRequested,
    [property: JsonPropertyName("cliInvocationRequested")] bool CliInvocationRequested,
    [property: JsonPropertyName("deploymentRequested")] bool DeploymentRequested,
    [property: JsonPropertyName("automaticAnalyzerValidationRequested")] bool AutomaticAnalyzerValidationRequested,
    [property: JsonPropertyName("automaticAnalyzerLaunchRequested")] bool AutomaticAnalyzerLaunchRequested)
{
    internal static DesignStudioExecutionReadinessBoundaryRequests None { get; } = new(
        ExecutionRequested: false,
        ProviderInvocationRequested: false,
        MicrosoftSkillsExecutionRequested: false,
        ApiInvocationRequested: false,
        CliInvocationRequested: false,
        DeploymentRequested: false,
        AutomaticAnalyzerValidationRequested: false,
        AutomaticAnalyzerLaunchRequested: false);
}

internal sealed record DesignStudioExecutionReadinessContext(
    [property: JsonPropertyName("previewReviewSchemaVersion")] string PreviewReviewSchemaVersion,
    ArchitectureCertificationState ArchitectureCertificationState,
    GenerationManifestState GenerationManifestState,
    GenerationPipelineVerificationState PipelineVerificationState,
    PbirGenerationSpecificationState PbirGenerationSpecificationState,
    PbirIntermediateRepresentationState PbirIntermediateRepresentationState,
    PbirPreviewPackageState PreviewPackageState,
    PbirReviewHandoffState ReviewHandoffState,
    DesignStudioExecutionPreviewReviewStatus PreviewReviewStatus);

internal sealed record DesignStudioExecutionReadinessSafetyGateResult(
    [property: JsonPropertyName("isAllowed")] bool IsAllowed,
    [property: JsonPropertyName("reasons")] IReadOnlyList<string> Reasons);

internal sealed record DesignStudioExecutionReadinessStageItem(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("value")] string Value);

internal sealed record DesignStudioExecutionReadinessStageSummary(
    [property: JsonPropertyName("stageId")] string StageId,
    [property: JsonPropertyName("section")] string Section,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("items")] IReadOnlyList<DesignStudioExecutionReadinessStageItem> Items);

internal sealed record DesignStudioExecutionReadinessWarningSummary(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("message")] string Message);

internal sealed record DesignStudioExecutionReadinessLineageReference(
    [property: JsonPropertyName("stage")] string Stage,
    [property: JsonPropertyName("referenceId")] string ReferenceId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion);

internal sealed record DesignStudioExecutionReadinessArchitectureCertificationReference(
    [property: JsonPropertyName("certificationId")] string CertificationId,
    [property: JsonPropertyName("readinessReportId")] string ReadinessReportId,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("readiness")] ArchitectureReadinessState Readiness,
    [property: JsonPropertyName("isCertified")] bool IsCertified);

internal sealed record DesignStudioExecutionReadinessTrustBoundary(
    [property: JsonPropertyName("executionAllowed")] bool ExecutionAllowed,
    [property: JsonPropertyName("providerInvocationAllowed")] bool ProviderInvocationAllowed,
    [property: JsonPropertyName("microsoftSkillsExecutionAllowed")] bool MicrosoftSkillsExecutionAllowed,
    [property: JsonPropertyName("apiInvocationAllowed")] bool ApiInvocationAllowed,
    [property: JsonPropertyName("cliInvocationAllowed")] bool CliInvocationAllowed,
    [property: JsonPropertyName("deploymentAllowed")] bool DeploymentAllowed,
    [property: JsonPropertyName("automaticAnalyzerValidationAllowed")] bool AutomaticAnalyzerValidationAllowed,
    [property: JsonPropertyName("automaticAnalyzerLaunchAllowed")] bool AutomaticAnalyzerLaunchAllowed);

internal sealed record DesignStudioExecutionReadinessDashboard(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("dashboardId")] string DashboardId,
    [property: JsonPropertyName("createdUtc")] DateTime CreatedUtc,
    [property: JsonPropertyName("readinessSummary")] DesignStudioExecutionReadinessSummary ReadinessSummary,
    [property: JsonPropertyName("stageSummaries")] IReadOnlyList<DesignStudioExecutionReadinessStageSummary> StageSummaries,
    [property: JsonPropertyName("warningSummaries")] IReadOnlyList<DesignStudioExecutionReadinessWarningSummary> WarningSummaries,
    [property: JsonPropertyName("reviewerActionsAvailable")] IReadOnlyList<string> ReviewerActionsAvailable,
    [property: JsonPropertyName("lineageReferences")] IReadOnlyList<DesignStudioExecutionReadinessLineageReference> LineageReferences,
    [property: JsonPropertyName("architectureCertificationReference")] DesignStudioExecutionReadinessArchitectureCertificationReference ArchitectureCertificationReference,
    [property: JsonPropertyName("trustBoundary")] DesignStudioExecutionReadinessTrustBoundary TrustBoundary);

internal sealed record DesignStudioExecutionReadinessState(
    DesignStudioExecutionReadinessDashboard? Dashboard,
    DesignStudioExecutionReadinessSafetyGateResult Safety,
    DesignStudioExecutionReadinessSummary ReadinessSummary);
