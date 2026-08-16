using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase41CompositionValidation
{
    internal static IReadOnlyList<LocalPbirGenerationDiagnostic> Validate(
        LocalPbirGenerationPage page,
        IReadOnlyList<LocalPbirGenerationVisual> visuals,
        LocalPbirGenerationPageComposition composition,
        IReadOnlyList<LocalPbirGenerationPage>? allPages = null)
    {
        var diagnostics = new List<LocalPbirGenerationDiagnostic>();
        LocalPbirGenerationPageTemplate template;
        try { template = Phase41PageTemplateCatalog.Get(composition.Template); }
        catch (ArgumentException) { return [new("PBIR41-COMPOSITION-TEMPLATE-001", $"pages[{page.PageId}].composition.template", "The page template is unsupported.")]; }

        var slots = template.Sections.SelectMany(section => section.Slots).ToDictionary(slot => slot.SlotId, StringComparer.Ordinal);
        var visualIds = visuals.Select(visual => visual.VisualId).ToHashSet(StringComparer.Ordinal);
        var assignedSlots = new HashSet<string>(StringComparer.Ordinal);
        foreach (var assignment in composition.SlotAssignments)
        {
            if (!assignedSlots.Add(assignment.SlotId))
                diagnostics.Add(new("PBIR41-COMPOSITION-DUPLICATE-SLOT-001", assignment.SlotId, "A slot may be assigned only once."));
            if (!slots.TryGetValue(assignment.SlotId, out var slot))
            {
                diagnostics.Add(new("PBIR41-COMPOSITION-SLOT-001", assignment.SlotId, "The slot is not defined by the page template."));
                continue;
            }
            if (assignment.SlotId == template.NavigationSlotId)
            {
                diagnostics.Add(new("PBIR41-COMPOSITION-COMPATIBILITY-002", assignment.SlotId, "Navigation slots contain navigation metadata and cannot receive visuals."));
                continue;
            }
            var visual = visuals.SingleOrDefault(item => item.VisualId == assignment.VisualId);
            if (visual is null || !visualIds.Contains(assignment.VisualId))
            {
                diagnostics.Add(new("PBIR41-COMPOSITION-VISUAL-001", assignment.VisualId, "The slot assignment references an unknown visual."));
                continue;
            }
            if (slot.AllowedVisualTypes.Count > 0 && !slot.AllowedVisualTypes.Contains(visual.VisualType, StringComparer.Ordinal))
                diagnostics.Add(new("PBIR41-COMPOSITION-COMPATIBILITY-001", assignment.SlotId, "The visual type is incompatible with the slot."));
            if (visual.Layout is not null && (visual.Layout.X, visual.Layout.Y, visual.Layout.Width, visual.Layout.Height) != (slot.Layout.X, slot.Layout.Y, slot.Layout.Width, slot.Layout.Height))
                diagnostics.Add(new("PBIR41-COMPOSITION-LAYOUT-CONFLICT-001", assignment.VisualId, "Explicit visual layout conflicts with the template slot layout."));
        }

        foreach (var required in slots.Values.Where(slot => slot.Required))
            if (!assignedSlots.Contains(required.SlotId))
                diagnostics.Add(new("PBIR41-COMPOSITION-REQUIRED-SLOT-001", required.SlotId, "A required template slot is not assigned."));

        foreach (var visual in visuals.Where(visual => visual.VisualType == "slicer"))
        {
            var binding = visual.Bindings.SingleOrDefault();
            if (binding is null || binding.Kind != LocalPbirGenerationBindingKind.Dimension || binding.Role != LocalPbirGenerationBindingRole.Category)
                diagnostics.Add(new("PBIR41-SLICER-BINDING-001", $"visuals[{visual.VisualId}].bindings", "A slicer requires exactly one dimension binding in the Category role."));
        }

        if (composition.Slicer?.Interaction is { } interaction)
        {
            var visualIdsForInteraction = interaction.TargetVisualIds ?? [];
            if (visualIdsForInteraction.Count != visualIdsForInteraction.Distinct(StringComparer.Ordinal).Count() ||
                visualIdsForInteraction.Any(target => !visualIds.Contains(target)))
            {
                diagnostics.Add(new("PBIR41-SLICER-INTERACTION-001", $"slicers[{composition.Slicer.VisualId}].interaction.targetVisualIds", "Slicer interaction targets must identify unique visuals on the same page."));
            }
            if (!interaction.TargetPage && visualIdsForInteraction.Count == 0)
            {
                diagnostics.Add(new("PBIR41-SLICER-INTERACTION-002", $"slicers[{composition.Slicer.VisualId}].interaction", "A slicer interaction must target visuals or the page."));
            }
        }

        if (composition.Navigation is not null)
        {
            var pageIds = (allPages ?? [page]).Select(item => item.PageId).ToHashSet(StringComparer.Ordinal);
            var navigationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var target in composition.Navigation.Targets)
            {
                if (!navigationIds.Add(target.NavigationId))
                    diagnostics.Add(new("PBIR41-NAVIGATION-DUPLICATE-ID-001", target.NavigationId, "Navigation identifiers must be unique."));
                if (target.Kind == LocalPbirGenerationNavigationTargetKind.Page && (target.PageId is null || !pageIds.Contains(target.PageId)))
                    diagnostics.Add(new("PBIR41-NAVIGATION-TARGET-001", target.NavigationId, "Navigation target page does not identify a requested page."));
                if (target.Kind == LocalPbirGenerationNavigationTargetKind.Page && target.PageId == page.PageId)
                    diagnostics.Add(new("PBIR41-NAVIGATION-SELF-001", target.NavigationId, "Navigation may not target its own page."));
            }
        }

        return diagnostics;
    }
}
