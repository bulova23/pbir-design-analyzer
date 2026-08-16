using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed record PbirMutationPlan(
    string MutationId,
    string Fingerprint,
    PbirLocalReportImportSnapshot Snapshot,
    IReadOnlyList<LocalPbirMutationOperation> Operations,
    IReadOnlyList<string> AffectedPages,
    IReadOnlyList<string> AffectedVisuals,
    IReadOnlyList<LocalPbirMutationDiagnostic> Diagnostics,
    IReadOnlyList<LocalPbirMutationSemanticDiff>? SemanticDiffs = null)
{
    internal bool IsValid => Diagnostics.Count == 0;
    internal bool IsNoOp => IsValid && Operations.Count == 0;
    internal IReadOnlyList<LocalPbirMutationSemanticDiff> Diffs => SemanticDiffs ?? [];
}

internal sealed record PbirMutationExecutionResult(
    PbirIntermediateRepresentationState IrState,
    IReadOnlyList<string> ChangedPages,
    IReadOnlyList<string> ChangedVisuals,
    IReadOnlyList<LocalPbirMutationDiagnostic> Diagnostics);
