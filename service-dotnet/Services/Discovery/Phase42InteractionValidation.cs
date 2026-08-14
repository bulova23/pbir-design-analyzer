using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase42InteractionValidation
{
    internal static Phase42InteractionValidationResult Validate(
        string pageId,
        IReadOnlyList<LocalPbirGenerationVisual> visuals,
        IReadOnlyList<LocalPbirGenerationSlicerInteractionRule> rules,
        IReadOnlyList<string>? otherPageVisualIds = null)
    {
        var diagnostics = new List<LocalPbirGenerationDiagnostic>();
        var visualById = visuals.ToDictionary(visual => visual.VisualId, StringComparer.Ordinal);
        var interactionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in rules.OrderBy(rule => rule.InteractionId, StringComparer.Ordinal))
        {
            var field = $"pages[{pageId}].interactions[{rule.InteractionId}]";
            if (!interactionIds.Add(rule.InteractionId))
                diagnostics.Add(new("PBIR42-INTERACTION-DUPLICATE-ID-001", field, "Interaction identifiers must be unique on a page."));

            if (!visualById.TryGetValue(rule.SourceVisualId, out var source))
            {
                diagnostics.Add(new("PBIR42-INTERACTION-SOURCE-001", $"{field}.sourceVisualId", "The interaction source visual does not exist on the page."));
            }
            else if (source.VisualType != "slicer")
            {
                diagnostics.Add(new("PBIR42-INTERACTION-SOURCE-001", $"{field}.sourceVisualId", "The interaction source visual must be a slicer."));
            }

            if (rule.TargetVisualIds.Count == 0)
                diagnostics.Add(new("PBIR42-INTERACTION-TARGET-001", $"{field}.targetVisualIds", "An interaction must identify at least one target visual."));
            if (rule.TargetVisualIds.Count != rule.TargetVisualIds.Distinct(StringComparer.Ordinal).Count())
                diagnostics.Add(new("PBIR42-INTERACTION-DUPLICATE-TARGET-001", $"{field}.targetVisualIds", "Interaction target visuals must be unique."));

            foreach (var targetId in rule.TargetVisualIds)
            {
                if (targetId == rule.SourceVisualId)
                    diagnostics.Add(new("PBIR42-INTERACTION-SELF-001", $"{field}.targetVisualIds", "A slicer interaction may not target its source slicer."));
                else if (otherPageVisualIds?.Contains(targetId, StringComparer.Ordinal) == true)
                    diagnostics.Add(new("PBIR42-INTERACTION-CROSS-PAGE-001", $"{field}.targetVisualIds", "Cross-page interaction targets are not supported."));
                else if (!visualById.ContainsKey(targetId))
                    diagnostics.Add(new("PBIR42-INTERACTION-TARGET-001", $"{field}.targetVisualIds", "The interaction target visual does not exist on the page."));
            }
        }

        return new(
            rules.OrderBy(rule => rule.InteractionId, StringComparer.Ordinal).ToArray(),
            diagnostics);
    }
}
