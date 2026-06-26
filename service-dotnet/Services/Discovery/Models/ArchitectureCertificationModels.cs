using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class ArchitectureValidationContract
{
    internal const string SchemaVersionV1 = "architecture-validation/v1";
}

internal static class ArchitectureCertificationContract
{
    internal const string SchemaVersionV1 = "architecture-certification/v1";
}

internal static class ArchitectureReadinessReportContract
{
    internal const string SchemaVersionV1 = "architecture-readiness-report/v1";
}

internal static class ArchitectureGapAnalysisContract
{
    internal const string SchemaVersionV1 = "architecture-gap-analysis/v1";
}

internal enum ArchitectureReadinessState
{
    Incomplete,
    ConditionallyReady,
    ArchitecturallyComplete,
    ReadyForExecutionImplementation,
}

internal sealed record ArchitectureFrameworkParticipation(
    [property: JsonPropertyName("frameworkId")] string FrameworkId,
    [property: JsonPropertyName("contract")] string Contract,
    [property: JsonPropertyName("service")] string Service,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("participates")] bool Participates);

internal sealed record ArchitectureBoundaryVerification(
    [property: JsonPropertyName("boundaryId")] string BoundaryId,
    [property: JsonPropertyName("assertion")] string Assertion,
    [property: JsonPropertyName("verified")] bool Verified,
    [property: JsonPropertyName("evidence")] IReadOnlyList<string> Evidence);

internal sealed record ArchitectureValidation(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("validationId")] string ValidationId,
    [property: JsonPropertyName("frameworkParticipation")] IReadOnlyList<ArchitectureFrameworkParticipation> FrameworkParticipation,
    [property: JsonPropertyName("trustBoundaryVerification")] IReadOnlyList<ArchitectureBoundaryVerification> TrustBoundaryVerification,
    [property: JsonPropertyName("ownershipVerification")] IReadOnlyList<ArchitectureBoundaryVerification> OwnershipVerification,
    [property: JsonPropertyName("providerNeutralityVerification")] IReadOnlyList<ArchitectureBoundaryVerification> ProviderNeutralityVerification);

internal sealed record ArchitectureValidationDiagnostics(
    IReadOnlyList<string> LayerSeparationViolations,
    IReadOnlyList<string> TrustBoundaryViolations,
    IReadOnlyList<string> OwnershipBoundaryViolations,
    IReadOnlyList<string> ProviderNeutralityViolations,
    IReadOnlyList<string> DeterminismViolations,
    IReadOnlyList<string> LineageViolations,
    IReadOnlyList<string> SchemaVersionViolations,
    IReadOnlyList<string> ReadinessTransitionViolations,
    IReadOnlyList<string> ApprovalTransitionViolations,
    IReadOnlyList<string> DeferredArchitectureGaps)
{
    internal static ArchitectureValidationDiagnostics Empty { get; } =
        new([], [], [], [], [], [], [], [], [], []);

    internal bool HasFailures =>
        LayerSeparationViolations.Count > 0 ||
        TrustBoundaryViolations.Count > 0 ||
        OwnershipBoundaryViolations.Count > 0 ||
        ProviderNeutralityViolations.Count > 0 ||
        DeterminismViolations.Count > 0 ||
        LineageViolations.Count > 0 ||
        SchemaVersionViolations.Count > 0 ||
        ReadinessTransitionViolations.Count > 0 ||
        ApprovalTransitionViolations.Count > 0;
}

internal sealed record ArchitectureValidationState(
    ArchitectureValidation? Validation,
    ArchitectureValidationDiagnostics Diagnostics)
{
    internal bool IsValid => Validation is not null && !Diagnostics.HasFailures;
}

internal sealed record ArchitectureCoverage(
    [property: JsonPropertyName("completedPhases")] IReadOnlyList<int> CompletedPhases,
    [property: JsonPropertyName("implementedContracts")] IReadOnlyList<string> ImplementedContracts,
    [property: JsonPropertyName("implementedServices")] IReadOnlyList<string> ImplementedServices,
    [property: JsonPropertyName("implementedSchemas")] IReadOnlyList<string> ImplementedSchemas);

internal sealed record ArchitectureCertification(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("certificationId")] string CertificationId,
    [property: JsonPropertyName("architectureCoverage")] ArchitectureCoverage ArchitectureCoverage,
    [property: JsonPropertyName("trustBoundaryVerification")] IReadOnlyList<ArchitectureBoundaryVerification> TrustBoundaryVerification,
    [property: JsonPropertyName("ownershipVerification")] IReadOnlyList<ArchitectureBoundaryVerification> OwnershipVerification,
    [property: JsonPropertyName("providerNeutralityVerification")] IReadOnlyList<ArchitectureBoundaryVerification> ProviderNeutralityVerification);

internal sealed record ArchitectureReadinessReport(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("reportId")] string ReportId,
    [property: JsonPropertyName("readiness")] ArchitectureReadinessState Readiness,
    [property: JsonPropertyName("executionCapabilityExists")] bool ExecutionCapabilityExists,
    [property: JsonPropertyName("guarantees")] IReadOnlyList<string> Guarantees,
    [property: JsonPropertyName("conditions")] IReadOnlyList<string> Conditions);

internal sealed record ArchitectureGap(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("summary")] string Summary);

internal sealed record ArchitectureGapAnalysis(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("analysisId")] string AnalysisId,
    [property: JsonPropertyName("architecturalGaps")] IReadOnlyList<ArchitectureGap> ArchitecturalGaps,
    [property: JsonPropertyName("remainingWork")] IReadOnlyList<ArchitectureGap> RemainingWork);

internal sealed record ArchitectureCertificationState(
    ArchitectureCertification? Certification,
    ArchitectureReadinessReport? ReadinessReport,
    ArchitectureGapAnalysis? GapAnalysis)
{
    internal bool IsCertified =>
        Certification is not null &&
        ReadinessReport is not null &&
        GapAnalysis is not null &&
        ReadinessReport.Readiness == ArchitectureReadinessState.ReadyForExecutionImplementation;
}

internal sealed record ArchitectureValidationContext(
    PlanningOrchestrationResult Planning,
    PbirExecutionPrototypeState PbirPrototypeState,
    PbirGenerationSpecificationState SpecificationState,
    GenerationProviderFrameworkState ProviderState,
    GenerationProviderExecutionPlanningState ExecutionPlanningState,
    RuntimeProviderFrameworkState RuntimeProviderState,
    MicrosoftRuntimeProviderFrameworkState MicrosoftRuntimeState,
    GenerationManifestState ManifestState,
    GenerationPipelineVerificationState PipelineVerificationState);
