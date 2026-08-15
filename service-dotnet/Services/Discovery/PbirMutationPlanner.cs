using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirMutationPlanner
{
    internal PbirMutationPlan Plan(PbirLocalReportImportSnapshot snapshot, LocalPbirMutationRequest request)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var diagnostics = new List<LocalPbirMutationDiagnostic>();
        if (request is null)
        {
            diagnostics.Add(new("PBIR42-REQUEST-001", "request", "Mutation request is required."));
            return Empty(snapshot, string.Empty, diagnostics);
        }
        if (request.SchemaVersion != LocalPbirMutationRequestContract.SchemaVersionV1)
            diagnostics.Add(new("PBIR42-REQUEST-002", "schemaVersion", "Unsupported mutation request schema version."));
        if (string.IsNullOrWhiteSpace(request.MutationId))
            diagnostics.Add(new("PBIR42-REQUEST-003", "mutationId", "Mutation ID is required."));
        if (request.Operations is null)
            diagnostics.Add(new("PBIR42-REQUEST-004", "operations", "Operations are required."));
        var ir = snapshot.IrState.Ir;
        if (ir is null || !snapshot.IrState.Validation.IsValid || snapshot.IrState.Readiness == PbirIntermediateRepresentationReadinessState.Blocked)
            diagnostics.Add(new("PBIR42-IMPORT-001", "sourceDirectory", "The imported report is not a valid shared IR snapshot."));
        if (diagnostics.Count > 0) return Empty(snapshot, request.MutationId ?? string.Empty, diagnostics);

        var duplicatePageIds = ir!.Pages
            .GroupBy(x => x.PageId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicatePageIds.Length > 0)
        {
            diagnostics.Add(new("PBIR46-PAGE-003", "pages.pageId", "Imported page identity is duplicated."));
            return Empty(snapshot, request.MutationId ?? string.Empty, diagnostics);
        }
        var pages = ir.Pages.ToDictionary(x => x.PageId, StringComparer.Ordinal);
        var visuals = ir.Visuals.ToDictionary(x => x.VisualId, StringComparer.Ordinal);
        var affectedPages = new HashSet<string>(StringComparer.Ordinal);
        var affectedVisuals = new HashSet<string>(StringComparer.Ordinal);
        var accepted = new List<LocalPbirMutationOperation>();
        foreach (var operation in request.Operations.OrderBy(x => x.Kind).ThenBy(x => TargetKey(x.Target), StringComparer.Ordinal))
        {
            if (ir.AuthoringEnvelope is not null &&
                PbirAuthoringMutationInventory.Classify(operation.Kind) != PbirAuthoringMutationClassification.TypedAndMergeable)
            {
                var classification = PbirAuthoringMutationInventory.Classify(operation.Kind);
                diagnostics.Add(new(
                    classification == PbirAuthoringMutationClassification.PreservedButNotAuthorable
                        ? "PBIR43-PRESERVED-001"
                        : "PBIR43-UNSUPPORTED-001",
                    $"operations[{operation.Kind}]",
                    classification == PbirAuthoringMutationClassification.PreservedButNotAuthorable
                        ? "The imported source content is preserved, but this operation has no typed merge path."
                        : "This imported operation is outside the bounded Phase 43 authoring inventory."));
                continue;
            }
            var targetPage = operation.Target?.PageId;
            var targetVisual = operation.Target?.VisualId;
            if (operation.Kind == LocalPbirMutationOperationKind.AddPage)
            {
                if (operation.Page is null || string.IsNullOrWhiteSpace(operation.Page.PageId))
                    diagnostics.Add(new("PBIR42-PAGE-001", "page", "Add Page requires a page with an ID."));
                else if (pages.ContainsKey(operation.Page.PageId))
                {
                    var existing = pages[operation.Page.PageId];
                    if (existing.DisplayName == operation.Page.DisplayName && existing.Order == operation.Page.Order) { }
                    else diagnostics.Add(new("PBIR42-CONFLICT-001", "page.pageId", "Page ID already exists."));
                }
                else { pages.Add(operation.Page.PageId, new(operation.Page.PageId, operation.Page.PageId, "", "", operation.Page.Order, operation.Page.DisplayName)); affectedPages.Add(operation.Page.PageId); accepted.Add(operation); }
                continue;
            }
            if (operation.Kind is LocalPbirMutationOperationKind.RemovePage or LocalPbirMutationOperationKind.RenamePage or LocalPbirMutationOperationKind.MovePage)
            {
                if (string.IsNullOrWhiteSpace(targetPage) || !pages.ContainsKey(targetPage))
                    diagnostics.Add(new("PBIR42-TARGET-001", "target.pageId", "Page target is missing or unknown."));
                else if (operation.Kind == LocalPbirMutationOperationKind.RenamePage && string.IsNullOrWhiteSpace(operation.DisplayName))
                    diagnostics.Add(new("PBIR46-PAGE-001", "displayName", "Rename Page requires a non-empty display name."));
                else if (operation.Kind == LocalPbirMutationOperationKind.RenamePage && ir.AuthoringEnvelope is not null && !HasSupportedPageDisplayNameOwner(ir, targetPage))
                    diagnostics.Add(new("PBIR46-PAGE-002", "target.pageId", "The imported page has no unambiguous pinned display-name owner."));
                else if (operation.Kind == LocalPbirMutationOperationKind.RenamePage &&
                         string.Equals(pages[targetPage].DisplayName, operation.DisplayName, StringComparison.Ordinal))
                {
                    // A same-name rename is a deterministic no-op. Keep it valid for preview,
                    // but do not add an executable operation or affected object.
                }
                else { affectedPages.Add(targetPage); accepted.Add(operation); }
                continue;
            }
            if (operation.Kind == LocalPbirMutationOperationKind.AddVisual)
            {
                if (operation.Visual is null || string.IsNullOrWhiteSpace(operation.Visual.VisualId))
                    diagnostics.Add(new("PBIR42-VISUAL-001", "visual", "Add Visual requires a visual with an ID."));
                else if (visuals.ContainsKey(operation.Visual.VisualId))
                {
                    var existing = visuals[operation.Visual.VisualId];
                    if (existing.PageId != operation.Visual.PageId || existing.VisualType != operation.Visual.VisualType) diagnostics.Add(new("PBIR42-CONFLICT-002", "visual.visualId", "Visual ID already exists with different content."));
                }
                else if (!pages.ContainsKey(operation.Visual.PageId))
                    diagnostics.Add(new("PBIR42-TARGET-002", "visual.pageId", "Visual page target is unknown."));
                else { affectedPages.Add(operation.Visual.PageId); affectedVisuals.Add(operation.Visual.VisualId); accepted.Add(operation); }
                continue;
            }
            if (operation.Kind is LocalPbirMutationOperationKind.RemoveVisual or LocalPbirMutationOperationKind.ReplaceVisual or LocalPbirMutationOperationKind.MoveVisual or LocalPbirMutationOperationKind.ResizeVisual or LocalPbirMutationOperationKind.UpdateBinding)
            {
                if (string.IsNullOrWhiteSpace(targetVisual) || !visuals.TryGetValue(targetVisual, out var visual))
                    diagnostics.Add(new("PBIR42-TARGET-003", "target.visualId", "Visual target is missing or unknown."));
                else if (operation.Kind == LocalPbirMutationOperationKind.ReplaceVisual && operation.Replacement is null)
                    diagnostics.Add(new("PBIR42-VISUAL-002", "replacement", "Replace Visual requires a replacement visual."));
                else { affectedPages.Add(visual.PageId); affectedVisuals.Add(targetVisual); accepted.Add(operation); }
                continue;
            }
            diagnostics.Add(new("PBIR42-UNSUPPORTED-001", $"operations[{operation.Kind}]", "This operation is not representable by the current shared IR/serializer boundary."));
        }
        var fingerprint = Fingerprint(request);
        return new(request.MutationId, fingerprint, snapshot, accepted, affectedPages.OrderBy(x => x, StringComparer.Ordinal).ToArray(), affectedVisuals.OrderBy(x => x, StringComparer.Ordinal).ToArray(), diagnostics);
    }

    private static PbirMutationPlan Empty(PbirLocalReportImportSnapshot snapshot, string id, List<LocalPbirMutationDiagnostic> diagnostics) => new(id, string.Empty, snapshot, [], [], [], diagnostics);
    internal static string Fingerprint(LocalPbirMutationRequest request) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)))).ToLowerInvariant();
    private static string TargetKey(LocalPbirMutationTarget? target) => string.Join('|', target?.PageId, target?.VisualId, target?.Section, target?.SlotId, target?.NavigationId, target?.SlicerId);

    private static bool HasSupportedPageDisplayNameOwner(PbirIntermediateRepresentation ir, string pageId)
    {
        var owners = ir.AuthoringEnvelope?.Items
            .Where(item => item.OwnerKind == PbirAuthoringOwnerKind.Page && string.Equals(item.OwnerId, pageId, StringComparison.Ordinal))
            .ToArray() ?? [];
        if (owners.Length != 1) return false;
        var root = owners[0].SourceDocument;
        return root.TryGetProperty("displayName", out var displayName) && displayName.ValueKind == JsonValueKind.String;
    }
}
