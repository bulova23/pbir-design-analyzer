using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class GenerationPipelineVerificationContract
{
    internal const string SchemaVersionV1 = "generation-pipeline-verification/v1";
}

internal sealed record GenerationPipelineStageVerification(
    [property: JsonPropertyName("stageId")] string StageId,
    [property: JsonPropertyName("sequence")] int Sequence,
    [property: JsonPropertyName("referenceId")] string ReferenceId,
    [property: JsonPropertyName("readiness")] string Readiness,
    [property: JsonPropertyName("completed")] bool Completed);

internal sealed record GenerationPipelineVerification(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("verificationId")] string VerificationId,
    [property: JsonPropertyName("manifestRef")] string ManifestRef,
    [property: JsonPropertyName("stageResults")] IReadOnlyList<GenerationPipelineStageVerification> StageResults,
    [property: JsonPropertyName("preservedReferences")] IReadOnlyList<string> PreservedReferences,
    [property: JsonPropertyName("lineageReferenceIds")] IReadOnlyList<string> LineageReferenceIds);

internal sealed record GenerationPipelineVerificationDiagnostics(
    IReadOnlyList<string> IncompleteStages,
    IReadOnlyList<string> MissingReferences,
    IReadOnlyList<string> InvalidReadinessTransitions,
    IReadOnlyList<string> LineageFailures,
    IReadOnlyList<string> IncompatibleProviders,
    IReadOnlyList<string> BoundaryViolations)
{
    internal static GenerationPipelineVerificationDiagnostics Empty { get; } =
        new([], [], [], [], [], []);

    internal bool HasFailures =>
        IncompleteStages.Count > 0 ||
        MissingReferences.Count > 0 ||
        InvalidReadinessTransitions.Count > 0 ||
        LineageFailures.Count > 0 ||
        IncompatibleProviders.Count > 0 ||
        BoundaryViolations.Count > 0;
}

internal sealed record GenerationPipelineVerificationState(
    GenerationPipelineVerification? Verification,
    GenerationPipelineVerificationDiagnostics Diagnostics)
{
    internal bool IsVerified => Verification is not null && !Diagnostics.HasFailures;
}
