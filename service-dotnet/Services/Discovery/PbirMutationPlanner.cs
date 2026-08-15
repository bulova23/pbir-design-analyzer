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
        var seenTargets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var operation in request.Operations)
        {
            var targetKey = $"{operation.Kind}:{TargetKey(operation.Target)}";
            if (!seenTargets.Add(targetKey))
            {
                diagnostics.Add(new("PBIR48-DUPLICATE-TARGET", "operations", "The mutation request targets the same object more than once."));
                continue;
            }
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
                if (operation.Page is null)
                    diagnostics.Add(new("PBIR42-PAGE-001", "page", "Add Page requires a typed page."));
                else if (operation.Page.Order < 0 || operation.Page.Order > pages.Count)
                    diagnostics.Add(new("PBIR48-PAGE-INVALID-POSITION", "page.order", "The new page position is outside the report page range."));
                else
                {
                    var pageId = string.IsNullOrWhiteSpace(operation.Page.PageId)
                        ? DeterministicPageId(request.MutationId, operation.Page.DisplayName, operation.Page.Order)
                        : operation.Page.PageId;
                    if (pages.ContainsKey(pageId))
                    {
                        var existing = pages[pageId];
                        if (existing.DisplayName != operation.Page.DisplayName || existing.Order != operation.Page.Order)
                            diagnostics.Add(new("PBIR42-CONFLICT-001", "page.pageId", "Page ID already exists."));
                    }
                    else
                    {
                        var normalized = operation with { Page = operation.Page with { PageId = pageId } };
                        pages.Add(pageId, new(pageId, pageId, "", "", operation.Page.Order, operation.Page.DisplayName));
                        affectedPages.Add(pageId);
                        accepted.Add(normalized);
                    }
                }
                continue;
            }
            if (operation.Kind is LocalPbirMutationOperationKind.RemovePage or LocalPbirMutationOperationKind.RenamePage or LocalPbirMutationOperationKind.MovePage)
            {
                if (string.IsNullOrWhiteSpace(targetPage) || !pages.ContainsKey(targetPage))
                    diagnostics.Add(new("PBIR42-TARGET-001", "target.pageId", "Page target is missing or unknown."));
                else if (operation.Kind == LocalPbirMutationOperationKind.RemovePage && pages.Count <= 1)
                    diagnostics.Add(new("PBIR48-PAGE-REMOVAL-CONFLICT", "target.pageId", "The report must retain at least one navigable page."));
                else if (operation.Kind == LocalPbirMutationOperationKind.RemovePage &&
                         (string.Equals(ir.Navigation.LandingPage, targetPage, StringComparison.Ordinal) ||
                          ir.Navigation.PageTransitions.Any(transition => transition.FromPageId == targetPage || transition.ToPageId == targetPage)))
                    diagnostics.Add(new("PBIR48-NAVIGATION-CONFLICT", "target.pageId", "The page is referenced by report navigation and cannot be removed safely."));
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
                else if (operation.Kind == LocalPbirMutationOperationKind.MovePage && (operation.Order is null || operation.Order < 0 || operation.Order >= pages.Count))
                    diagnostics.Add(new("PBIR48-PAGE-INVALID-POSITION", "order", "The destination page position is outside the report page range."));
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
                else if (operation.Kind == LocalPbirMutationOperationKind.MoveVisual && operation.Target?.PageId is not null && !pages.ContainsKey(operation.Target.PageId))
                    diagnostics.Add(new("PBIR48-DELETED-TARGET", "target.pageId", "The destination page target is missing or deleted."));
                else if (operation.Kind is LocalPbirMutationOperationKind.MoveVisual or LocalPbirMutationOperationKind.ResizeVisual && !IsValidLayout(ir, operation, visual))
                    diagnostics.Add(new("PBIR48-LAYOUT-CONFLICT", "layout", "The proposed visual layout is outside the canvas or conflicts with an existing visual."));
                else { affectedPages.Add(visual.PageId); affectedVisuals.Add(targetVisual); accepted.Add(operation); }
                continue;
            }
            diagnostics.Add(new("PBIR42-UNSUPPORTED-001", $"operations[{operation.Kind}]", "This operation is not representable by the current shared IR/serializer boundary."));
        }
        var fingerprint = Fingerprint(request);
        return new(request.MutationId, fingerprint, snapshot, accepted, affectedPages.OrderBy(x => x, StringComparer.Ordinal).ToArray(), affectedVisuals.OrderBy(x => x, StringComparer.Ordinal).ToArray(), diagnostics, CreateDiffs(ir, accepted));
    }

    private static PbirMutationPlan Empty(PbirLocalReportImportSnapshot snapshot, string id, List<LocalPbirMutationDiagnostic> diagnostics) => new(id, string.Empty, snapshot, [], [], [], diagnostics, []);
    internal static string Fingerprint(LocalPbirMutationRequest request) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)))).ToLowerInvariant();
    private static string TargetKey(LocalPbirMutationTarget? target) => string.Join('|', target?.PageId, target?.VisualId, target?.Section, target?.SlotId, target?.NavigationId, target?.SlicerId);

    private static string DeterministicPageId(string mutationId, string displayName, int order) =>
        "page:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{mutationId}|{displayName}|{order}")))[..16].ToLowerInvariant();

    private static bool IsValidLayout(PbirIntermediateRepresentation ir, LocalPbirMutationOperation operation, PbirIntermediateRepresentationVisual visual)
    {
        var layout = visual.Layout;
        if (operation.Layout is not null)
            layout = new(operation.Layout.X ?? layout?.X ?? 0, operation.Layout.Y ?? layout?.Y ?? 0, operation.Layout.Width ?? layout?.Width ?? 1, operation.Layout.Height ?? layout?.Height ?? 1);
        if (layout is null || layout.X < 0 || layout.Y < 0 || layout.Width <= 0 || layout.Height <= 0 || layout.X + layout.Width > 1280 || layout.Y + layout.Height > 720)
            return false;
        // Imported PBIR may intentionally contain layered or partially overlapping
        // visuals; the pinned serializer owns whether that overlap is supported.
        // The planner therefore rejects impossible bounds here and leaves any
        // schema-specific overlap decision to the existing serializer validator.
        return true;
    }

    private static IReadOnlyList<LocalPbirMutationSemanticDiff> CreateDiffs(PbirIntermediateRepresentation ir, IReadOnlyList<LocalPbirMutationOperation> operations) => operations.Select(operation =>
        operation.Kind switch
        {
            LocalPbirMutationOperationKind.AddPage => new LocalPbirMutationSemanticDiff(LocalPbirMutationSemanticDiffKind.PageAdded, operation.Page!.PageId, AfterDisplayName: operation.Page.DisplayName, AfterOrder: operation.Page.Order),
            LocalPbirMutationOperationKind.RemovePage => new LocalPbirMutationSemanticDiff(LocalPbirMutationSemanticDiffKind.PageRemoved, operation.Target!.PageId!),
            LocalPbirMutationOperationKind.RenamePage => new LocalPbirMutationSemanticDiff(LocalPbirMutationSemanticDiffKind.PageRenamed, operation.Target!.PageId!, BeforeDisplayName: ir.Pages.Single(page => page.PageId == operation.Target.PageId).DisplayName, AfterDisplayName: operation.DisplayName),
            LocalPbirMutationOperationKind.MovePage => new LocalPbirMutationSemanticDiff(LocalPbirMutationSemanticDiffKind.PageMoved, operation.Target!.PageId!, BeforeOrder: ir.Pages.Single(page => page.PageId == operation.Target.PageId).Order, AfterOrder: operation.Order),
            LocalPbirMutationOperationKind.MoveVisual => new LocalPbirMutationSemanticDiff(LocalPbirMutationSemanticDiffKind.VisualMoved, operation.Target!.VisualId!, BeforePageId: ir.Visuals.Single(visual => visual.VisualId == operation.Target.VisualId).PageId, AfterPageId: operation.Target.PageId ?? operation.Visual?.PageId, BeforeOrder: ir.Visuals.Single(visual => visual.VisualId == operation.Target.VisualId).Order, AfterOrder: operation.Order),
            LocalPbirMutationOperationKind.ResizeVisual => new LocalPbirMutationSemanticDiff(LocalPbirMutationSemanticDiffKind.VisualResized, operation.Target!.VisualId!, BeforeLayout: ToLayout(ir.Visuals.Single(visual => visual.VisualId == operation.Target.VisualId).Layout), AfterLayout: ToLayout(operation.Layout)),
            _ => null!
        }).Where(diff => diff is not null).ToArray();

    private static LocalPbirGenerationVisualLayout? ToLayout(PbirIntermediateRepresentationVisualLayout? layout) => layout is null ? null : new(layout.X, layout.Y, layout.Width, layout.Height);
    private static LocalPbirGenerationVisualLayout? ToLayout(LocalPbirGenerationLayout? layout) => layout is null || layout.X is null || layout.Y is null || layout.Width is null || layout.Height is null ? null : new(layout.X.Value, layout.Y.Value, layout.Width.Value, layout.Height.Value);

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
