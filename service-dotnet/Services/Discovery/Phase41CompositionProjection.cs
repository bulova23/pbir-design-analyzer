using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase41CompositionProjection
{
    internal static Phase41CompositionProjectionResult Resolve(
        LocalPbirGenerationPage page,
        IReadOnlyList<LocalPbirGenerationVisual> visuals,
        LocalPbirGenerationPageComposition composition,
        IReadOnlyList<LocalPbirGenerationPage>? allPages = null)
    {
        var diagnostics = Phase41CompositionValidation.Validate(page, visuals, composition, allPages);
        if (diagnostics.Count > 0)
            return new(new Dictionary<string, LocalPbirGenerationVisualLayout>(StringComparer.Ordinal), diagnostics);

        var template = Phase41PageTemplateCatalog.Get(composition.Template);
        var slots = template.Sections.SelectMany(section => section.Slots).ToDictionary(slot => slot.SlotId, StringComparer.Ordinal);
        var assignments = composition.SlotAssignments.ToDictionary(assignment => assignment.VisualId, assignment => assignment.SlotId, StringComparer.Ordinal);
        var layouts = new Dictionary<string, LocalPbirGenerationVisualLayout>(StringComparer.Ordinal);
        var automatic = template.Sections.SelectMany(section => section.Slots).Where(slot => slot.AllowedVisualTypes.Count > 0).ToArray();
        var automaticIndex = 0;
        foreach (var visual in visuals.OrderBy(visual => visual.Order).ThenBy(visual => visual.VisualId, StringComparer.Ordinal))
        {
            if (visual.Layout is { X: int x, Y: int y, Width: int width, Height: int height })
            {
                layouts[visual.VisualId] = new(x, y, width, height);
                continue;
            }
            if (assignments.TryGetValue(visual.VisualId, out var slotId))
            {
                layouts[visual.VisualId] = slots[slotId].Layout;
                continue;
            }
            while (automaticIndex < automatic.Length && !automatic[automaticIndex].AllowedVisualTypes.Contains(visual.VisualType, StringComparer.Ordinal))
                automaticIndex++;
            if (automaticIndex < automatic.Length)
                layouts[visual.VisualId] = automatic[automaticIndex++].Layout;
        }

        return new(layouts, []);
    }
}
