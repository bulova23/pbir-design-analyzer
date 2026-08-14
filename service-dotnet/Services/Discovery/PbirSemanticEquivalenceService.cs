using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirSemanticEquivalenceService
{
    internal PbirSemanticEquivalenceResult Compare(
        PbirIntermediateRepresentation before,
        PbirIntermediateRepresentation after,
        IReadOnlySet<string>? expectedChangedVisualIds = null)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        expectedChangedVisualIds ??= new HashSet<string>(StringComparer.Ordinal);

        var unchanged = new List<string>();
        var expected = new List<string>();
        var unexpected = new List<string>();
        var beforeVisuals = before.Visuals.ToDictionary(visual => visual.VisualId, StringComparer.Ordinal);
        var afterVisuals = after.Visuals.ToDictionary(visual => visual.VisualId, StringComparer.Ordinal);

        foreach (var visualId in beforeVisuals.Keys.Concat(afterVisuals.Keys).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
        {
            var path = $"{visualId}/bindings";
            if (!beforeVisuals.TryGetValue(visualId, out var beforeVisual) || !afterVisuals.TryGetValue(visualId, out var afterVisual))
            {
                Classify(path, expectedChangedVisualIds.Contains(visualId), expected, unexpected);
                continue;
            }

            if (beforeVisual.VisualType != afterVisual.VisualType || beforeVisual.PageId != afterVisual.PageId)
            {
                Classify($"{visualId}/identity", expectedChangedVisualIds.Contains(visualId), expected, unexpected);
            }

            if (BindingsEqual(beforeVisual.Bindings ?? [], afterVisual.Bindings ?? []))
            {
                unchanged.Add(path);
            }
            else
            {
                Classify(path, expectedChangedVisualIds.Contains(visualId), expected, unexpected);
            }
        }

        return new(expected.Count == 0 && unexpected.Count == 0, unchanged, expected, unexpected);
    }

    private static bool BindingsEqual(IReadOnlyList<PbirIntermediateRepresentationBinding> left, IReadOnlyList<PbirIntermediateRepresentationBinding> right) =>
        left.OrderBy(binding => binding.ProjectionOrder).ThenBy(binding => binding.BindingId, StringComparer.Ordinal)
            .Select(ToSemanticKey)
            .SequenceEqual(right.OrderBy(binding => binding.ProjectionOrder).ThenBy(binding => binding.BindingId, StringComparer.Ordinal).Select(ToSemanticKey));

    private static object ToSemanticKey(PbirIntermediateRepresentationBinding binding) =>
        (binding.Role, binding.Kind, binding.Token.Normalize(), binding.Entity.Normalize(), binding.Property.Normalize(), binding.ProjectionOrder);

    private static void Classify(string path, bool expectedChange, List<string> expected, List<string> unexpected)
    {
        if (expectedChange) expected.Add(path);
        else unexpected.Add(path);
    }
}
