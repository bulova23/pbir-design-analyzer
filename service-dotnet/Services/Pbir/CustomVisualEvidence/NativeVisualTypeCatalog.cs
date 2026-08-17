namespace PowerBIModelingService.Services.Pbir.CustomVisualEvidence;

/// <summary>
/// Known first-party Power BI visual type identifiers (case-insensitive). Anything outside
/// this set is a custom (third-party/AppSource) visual. Shared by governance's
/// allowCustomVisuals rule and by custom-visual evidence extraction, so both stay in sync.
/// </summary>
public static class NativeVisualTypeCatalog
{
    private static readonly HashSet<string> _knownVisualTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "barChart", "columnChart", "clusteredBarChart", "clusteredColumnChart",
        "stackedBarChart", "stackedColumnChart", "hundredPercentStackedBarChart", "hundredPercentStackedColumnChart",
        "lineChart", "areaChart", "stackedAreaChart", "lineStackedColumnComboChart", "lineClusteredColumnComboChart",
        "pieChart", "donutChart",
        "scatterChart", "treemap", "waterfallChart", "funnel", "gauge",
        "card", "multiRowCard", "kpi",
        "tableEx", "pivotTable", "matrix",
        "slicer", "filterSlicer", "advancedSlicer",
        "map", "filledMap", "shapeMap", "azureMap",
        "image", "textbox", "shape", "basicShape", "actionButton", "navigationButton",
        "pageNavigator", "bookmarkNavigator",
        "decompositionTreeVisual", "qnaVisual", "keyDriversVisual", "aiNarrativesVisual",
        "ribbonChart",
    };

    public static bool IsNative(string? visualType) =>
        !string.IsNullOrWhiteSpace(visualType) && _knownVisualTypes.Contains(visualType);
}
