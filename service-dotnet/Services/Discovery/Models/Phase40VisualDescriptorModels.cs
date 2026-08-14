using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed record Phase40VisualRoleProjection(
    LocalPbirGenerationBindingRole BindingRole,
    string SerializerRole,
    bool Required,
    int MinimumCount,
    LocalPbirGenerationBindingKind? Kind,
    IReadOnlyList<string>? ImportedRoleAliases = null)
{
    internal IReadOnlyList<string> ImportAliases => ImportedRoleAliases ?? [SerializerRole];
}

internal sealed record Phase40VisualDescriptor(
    string VisualType,
    IReadOnlyList<LocalPbirGenerationBindingRole> SupportedBindingRoles,
    IReadOnlyList<Phase40VisualRoleProjection> SerializerRoles,
    bool SupportsAxis,
    bool SupportsLegend,
    bool SupportsTooltip,
    bool SupportsConditionalFormatting,
    bool SupportsChartFormatting);

internal static class Phase40VisualDescriptorCatalog
{
    internal static IReadOnlyList<Phase40VisualDescriptor> All { get; } =
    [
        Descriptor("card", [Role(LocalPbirGenerationBindingRole.Value, "Fields", true, 1, LocalPbirGenerationBindingKind.Measure), Tooltip()], false, false, true, true, false),
        Descriptor("table", [Role(LocalPbirGenerationBindingRole.Value, "Values", true, 1, null), Tooltip()], false, false, true, true, false),
        Descriptor("clusteredColumnChart", [Role(LocalPbirGenerationBindingRole.Category, "Category", true, 1, LocalPbirGenerationBindingKind.Dimension), Role(LocalPbirGenerationBindingRole.Value, "Y", true, 1, LocalPbirGenerationBindingKind.Measure), Tooltip()], true, true, true, true, true),
        Descriptor("lineChart", [Role(LocalPbirGenerationBindingRole.Category, "Category", true, 1, LocalPbirGenerationBindingKind.Dimension), Role(LocalPbirGenerationBindingRole.Value, "Y", true, 1, LocalPbirGenerationBindingKind.Measure), Role(LocalPbirGenerationBindingRole.Series, "Series", false, 0, LocalPbirGenerationBindingKind.Dimension), Tooltip()], true, true, true, true, true),
        Descriptor("barChart", [Role(LocalPbirGenerationBindingRole.Category, "Category", true, 1, LocalPbirGenerationBindingKind.Dimension), Role(LocalPbirGenerationBindingRole.Value, "Y", true, 1, LocalPbirGenerationBindingKind.Measure), Tooltip()], true, true, true, true, true),
        Descriptor("pieChart", [Role(LocalPbirGenerationBindingRole.Legend, "Category", true, 1, LocalPbirGenerationBindingKind.Dimension, ["Category"]), Role(LocalPbirGenerationBindingRole.Value, "Y", true, 1, LocalPbirGenerationBindingKind.Measure), Tooltip()], false, true, true, true, true)
    ];

    internal static Phase40VisualDescriptor Get(string visualType) =>
        All.SingleOrDefault(value => value.VisualType == visualType)
        ?? throw new ArgumentException($"Unsupported Phase 40 visual type: {visualType}", nameof(visualType));

    internal static IReadOnlyList<Phase40VisualRoleProjection> ResolveImportedRoles(string visualType, string importedRole)
    {
        if (visualType == "slicer")
        {
            return importedRole == "Category"
                ? [new(LocalPbirGenerationBindingRole.Category, "Category", true, 1, LocalPbirGenerationBindingKind.Dimension, ["Category"])]
                : [];
        }

        return Get(visualType).SerializerRoles
            .Where(role => role.ImportAliases.Contains(importedRole, StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    private static Phase40VisualDescriptor Descriptor(string visualType, IReadOnlyList<Phase40VisualRoleProjection> roles, bool axis, bool legend, bool tooltip, bool conditionalFormatting, bool chartFormatting) =>
        new(visualType, roles.Select(role => role.BindingRole).ToArray(), roles, axis, legend, tooltip, conditionalFormatting, chartFormatting);

    private static Phase40VisualRoleProjection Role(LocalPbirGenerationBindingRole bindingRole, string serializerRole, bool required, int minimumCount, LocalPbirGenerationBindingKind? kind) =>
        new(bindingRole, serializerRole, required, minimumCount, kind);

    private static Phase40VisualRoleProjection Role(LocalPbirGenerationBindingRole bindingRole, string serializerRole, bool required, int minimumCount, LocalPbirGenerationBindingKind? kind, IReadOnlyList<string> aliases) =>
        new(bindingRole, serializerRole, required, minimumCount, kind, aliases);

    private static Phase40VisualRoleProjection Tooltip() =>
        Role(LocalPbirGenerationBindingRole.Tooltip, "Tooltips", false, 0, null, ["Tooltips"]);
}

internal sealed record Phase40VisualTemplate(
    LocalPbirGenerationVisualTemplate Name,
    LocalPbirGenerationChartFormatting Chart,
    LocalPbirGenerationAxisConfiguration Axis,
    LocalPbirGenerationLegendConfiguration Legend);

internal static class Phase40VisualTemplateCatalog
{
    internal static Phase40VisualTemplate Get(string name) => name switch
    {
        "default" => new(LocalPbirGenerationVisualTemplate.Default, new(Background: new("#FFFFFF")), new(), new()),
        "executive" => new(LocalPbirGenerationVisualTemplate.Executive, new(Background: new("#F7F9FC"), Title: "Executive Summary"), new(Visible: true, Orientation: LocalPbirGenerationAxisOrientation.Horizontal), new(Visible: true, Placement: LocalPbirGenerationLegendPlacement.Bottom)),
        "compact" => new(LocalPbirGenerationVisualTemplate.Compact, new(Background: new("#FFFFFF"), Title: null), new(Visible: false), new(Visible: false)),
        _ => throw new ArgumentException($"Unsupported Phase 40 visual template: {name}", nameof(name))
    };
}

internal sealed record Phase41VisualDescriptor(
    string VisualType,
    IReadOnlyList<LocalPbirGenerationBindingRole> SupportedBindingRoles,
    bool SupportsTooltip);

internal static class Phase41VisualDescriptorCatalog
{
    internal static IReadOnlyList<Phase41VisualDescriptor> All { get; } =
    [
        new("slicer", [LocalPbirGenerationBindingRole.Category], false)
    ];

    internal static Phase41VisualDescriptor Get(string visualType) => All.SingleOrDefault(value => value.VisualType == visualType)
        ?? throw new ArgumentException($"Unsupported Phase 41 visual type: {visualType}", nameof(visualType));
}
