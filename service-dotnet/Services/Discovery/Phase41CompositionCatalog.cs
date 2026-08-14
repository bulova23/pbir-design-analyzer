using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase41PageTemplateCatalog
{
    private const int PageWidth = 1280;
    private const int PageHeight = 720;

    internal static LocalPbirGenerationPageTemplate Get(string name) => name switch
    {
        "executiveSummary" => Template(name, [
            Section("header", LocalPbirGenerationSectionKind.Header, Slot("Header", LocalPbirGenerationSectionKind.Header, ["card", "table", "clusteredColumnChart", "lineChart", "barChart", "pieChart", "slicer"], false, 48, 24, 1184, 48)),
            Section("kpi", LocalPbirGenerationSectionKind.KpiRow,
                Slot("Kpi1", LocalPbirGenerationSectionKind.KpiRow, ["card"], true, 48, 96, 336, 160),
                Slot("Kpi2", LocalPbirGenerationSectionKind.KpiRow, ["card"], false, 400, 96, 336, 160)),
            Section("analysis", LocalPbirGenerationSectionKind.PrimaryAnalysis, Slot("PrimaryChart", LocalPbirGenerationSectionKind.PrimaryAnalysis, ["clusteredColumnChart", "lineChart", "barChart", "pieChart"], true, 48, 280, 736, 360)),
            Section("detail", LocalPbirGenerationSectionKind.DetailGrid, Slot("DetailTable", LocalPbirGenerationSectionKind.DetailGrid, ["table"], false, 800, 280, 432, 360)),
            Section("filter", LocalPbirGenerationSectionKind.FilterRail, Slot("RegionSlicer", LocalPbirGenerationSectionKind.FilterRail, ["slicer"], false, 1032, 96, 192, 160)),
            Section("footer", LocalPbirGenerationSectionKind.FooterNavigation, Slot("Navigation", LocalPbirGenerationSectionKind.FooterNavigation, [], false, 48, 664, 1184, 32))], "Navigation", "RegionSlicer"),
        "overview" => Template(name, [
            Section("header", LocalPbirGenerationSectionKind.Header, Slot("Header", LocalPbirGenerationSectionKind.Header, ["card", "table", "clusteredColumnChart", "lineChart", "barChart", "pieChart", "slicer"], false, 48, 24, 1184, 48)),
            Section("primary", LocalPbirGenerationSectionKind.PrimaryAnalysis, Slot("PrimaryChart", LocalPbirGenerationSectionKind.PrimaryAnalysis, ["clusteredColumnChart", "lineChart", "barChart", "pieChart"], true, 48, 96, 736, 544)),
            Section("secondary", LocalPbirGenerationSectionKind.SecondaryAnalysis, Slot("SecondaryChart", LocalPbirGenerationSectionKind.SecondaryAnalysis, ["clusteredColumnChart", "lineChart", "barChart", "pieChart"], false, 800, 96, 432, 264)),
            Section("filter", LocalPbirGenerationSectionKind.FilterRail, Slot("Filter1", LocalPbirGenerationSectionKind.FilterRail, ["slicer"], false, 800, 376, 432, 264)),
            Section("footer", LocalPbirGenerationSectionKind.FooterNavigation, Slot("Navigation", LocalPbirGenerationSectionKind.FooterNavigation, [], false, 48, 664, 1184, 32))], "Navigation", "Filter1"),
        "detail" => Template(name, [
            Section("header", LocalPbirGenerationSectionKind.Header, Slot("Header", LocalPbirGenerationSectionKind.Header, ["card", "table", "clusteredColumnChart", "lineChart", "barChart", "pieChart", "slicer"], false, 48, 24, 1184, 48)),
            Section("filter", LocalPbirGenerationSectionKind.FilterRail, Slot("Filter1", LocalPbirGenerationSectionKind.FilterRail, ["slicer"], false, 48, 96, 240, 544)),
            Section("detail", LocalPbirGenerationSectionKind.DetailGrid, Slot("DetailTable", LocalPbirGenerationSectionKind.DetailGrid, ["table"], true, 312, 96, 920, 544)),
            Section("footer", LocalPbirGenerationSectionKind.FooterNavigation, Slot("Navigation", LocalPbirGenerationSectionKind.FooterNavigation, [], false, 48, 664, 1184, 32))], "Navigation", "Filter1"),
        "comparison" => Template(name, [
            Section("header", LocalPbirGenerationSectionKind.Header, Slot("Header", LocalPbirGenerationSectionKind.Header, ["card", "table", "clusteredColumnChart", "lineChart", "barChart", "pieChart", "slicer"], false, 48, 24, 1184, 48)),
            Section("analysis", LocalPbirGenerationSectionKind.PrimaryAnalysis,
                Slot("PrimaryChart", LocalPbirGenerationSectionKind.PrimaryAnalysis, ["clusteredColumnChart", "lineChart", "barChart", "pieChart"], true, 48, 96, 576, 544),
                Slot("SecondaryChart", LocalPbirGenerationSectionKind.PrimaryAnalysis, ["clusteredColumnChart", "lineChart", "barChart", "pieChart"], true, 656, 96, 576, 544)),
            Section("footer", LocalPbirGenerationSectionKind.FooterNavigation, Slot("Navigation", LocalPbirGenerationSectionKind.FooterNavigation, [], false, 48, 664, 1184, 32))], "Navigation", null),
        _ => throw new ArgumentException($"Unsupported Phase 41 page template: {name}", nameof(name))
    };

    private static LocalPbirGenerationPageTemplate Template(string name, IReadOnlyList<LocalPbirGenerationSectionDefinition> sections, string navigationSlotId, string? slicerSlotId) =>
        new(name, PageWidth, PageHeight, sections, navigationSlotId, slicerSlotId);

    private static LocalPbirGenerationSectionDefinition Section(string id, LocalPbirGenerationSectionKind kind, params LocalPbirGenerationSlotDefinition[] slots) => new(id, kind, slots);

    private static LocalPbirGenerationSlotDefinition Slot(string id, LocalPbirGenerationSectionKind section, IReadOnlyList<string> allowedVisualTypes, bool required, int x, int y, int width, int height) =>
        new(id, section, allowedVisualTypes, required, new(x, y, width, height));
}
