using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirMutationExecutor
{
    internal PbirMutationExecutionResult Execute(PbirMutationPlan plan)
    {
        if (!plan.IsValid || plan.Snapshot.IrState.Ir is null)
            return new(plan.Snapshot.IrState, [], [], plan.Diagnostics);
        var ir = plan.Snapshot.IrState.Ir;
        var pages = ir.Pages.ToList();
        var visuals = ir.Visuals.ToList();
        foreach (var operation in plan.Operations)
        {
            switch (operation.Kind)
            {
                case LocalPbirMutationOperationKind.AddPage:
                    pages.Add(new(operation.Page!.PageId, operation.Page.PageId, "", "", operation.Page.Order, operation.Page.DisplayName));
                    break;
                case LocalPbirMutationOperationKind.RemovePage:
                    pages.RemoveAll(x => x.PageId == operation.Target!.PageId);
                    visuals.RemoveAll(x => x.PageId == operation.Target.PageId);
                    break;
                case LocalPbirMutationOperationKind.RenamePage:
                    pages = pages.Select(x => x.PageId == operation.Target!.PageId ? x with { DisplayName = operation.DisplayName ?? x.DisplayName } : x).ToList();
                    break;
                case LocalPbirMutationOperationKind.MovePage:
                    pages = pages.Select(x => x.PageId == operation.Target!.PageId ? x with { Order = operation.Order ?? x.Order } : x).ToList();
                    break;
                case LocalPbirMutationOperationKind.AddVisual:
                    visuals.Add(ToIrVisual(operation.Visual!));
                    break;
                case LocalPbirMutationOperationKind.RemoveVisual:
                    visuals.RemoveAll(x => x.VisualId == operation.Target!.VisualId);
                    break;
                case LocalPbirMutationOperationKind.ReplaceVisual:
                    visuals = visuals.Select(x => x.VisualId == operation.Target!.VisualId ? ToIrVisual(operation.Replacement!, x.VisualId) : x).ToList();
                    break;
                case LocalPbirMutationOperationKind.MoveVisual:
                    visuals = visuals.Select(x => x.VisualId == operation.Target!.VisualId ? x with { PageId = operation.Target.PageId ?? operation.Visual?.PageId ?? x.PageId, Order = operation.Order ?? x.Order } : x).ToList();
                    break;
                case LocalPbirMutationOperationKind.ResizeVisual:
                    visuals = visuals.Select(x => x.VisualId == operation.Target!.VisualId && operation.Layout is not null ? x with { Layout = new(operation.Layout.X ?? x.Layout?.X ?? 0, operation.Layout.Y ?? x.Layout?.Y ?? 0, operation.Layout.Width ?? x.Layout?.Width ?? 1, operation.Layout.Height ?? x.Layout?.Height ?? 1) } : x).ToList();
                    break;
                case LocalPbirMutationOperationKind.UpdateBinding:
                    visuals = visuals.Select(x => x.VisualId == operation.Target!.VisualId && operation.Binding is not null ? x with { Bindings = (x.Bindings ?? []).Append(new(operation.Binding.BindingId, ToRole(operation.Binding.Role), ToKind(operation.Binding.Kind), operation.Binding.Token, operation.Binding.Entity, operation.Binding.Property, (x.Bindings?.Count ?? 0))).ToArray() } : x).ToList();
                    break;
            }
        }
        pages = pages.OrderBy(x => x.Order).ThenBy(x => x.PageId, StringComparer.Ordinal).ToList();
        visuals = visuals.OrderBy(x => x.PageId, StringComparer.Ordinal).ThenBy(x => x.Order).ThenBy(x => x.VisualId, StringComparer.Ordinal).ToList();
        var updated = ir with { Pages = pages, Visuals = visuals };
        var state = new PbirIntermediateRepresentationState(updated, new PbirIntermediateRepresentationValidationResult(PbirIntermediateRepresentationValidationDiagnostics.Empty), PbirIntermediateRepresentationReadinessState.ReadyForSerializer);
        return new(state, plan.AffectedPages, plan.AffectedVisuals, []);
    }

    private static PbirIntermediateRepresentationVisual ToIrVisual(LocalPbirGenerationVisual visual, string? id = null) => new(id ?? visual.VisualId, visual.PageId, visual.VisualType, "", visual.VisualId, [], visual.Order, visual.Layout is null ? null : new(visual.Layout.X ?? 0, visual.Layout.Y ?? 0, visual.Layout.Width ?? 1, visual.Layout.Height ?? 1), visual.Bindings.Select((x, i) => new PbirIntermediateRepresentationBinding(x.BindingId, ToRole(x.Role), ToKind(x.Kind), x.Token, x.Entity, x.Property, i)).ToArray());
    private static PbirIntermediateRepresentationBindingRole ToRole(LocalPbirGenerationBindingRole role) => Enum.Parse<PbirIntermediateRepresentationBindingRole>(role.ToString());
    private static PbirIntermediateRepresentationBindingKind ToKind(LocalPbirGenerationBindingKind kind) => Enum.Parse<PbirIntermediateRepresentationBindingKind>(kind.ToString());
}
