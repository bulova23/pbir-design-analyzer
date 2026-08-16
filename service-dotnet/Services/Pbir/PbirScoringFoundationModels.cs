using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

internal sealed record LoadedReportModel(
    System.Text.Json.Nodes.JsonObject ReportJson,
    List<PageData> Pages,
    List<FilterDefinitionData> ReportFilters);

internal readonly record struct CanvasMetadata(double Width, double Height);

internal sealed record PageData
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public List<VisualData> Visuals { get; init; } = [];
    public CanvasMetadata? Canvas { get; init; }
    public List<FilterDefinitionData> PageFilters { get; init; } = [];
}

internal sealed record VisualData
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double W { get; init; }
    public double H { get; init; }
    public bool IsHidden { get; init; }
    public VisualTextMetadata Text { get; init; } = VisualTextMetadata.Empty;
    public VisualLabelMetadata Labels { get; init; } = VisualLabelMetadata.Empty;
    public VisualFieldRoleMetadata FieldRoles { get; init; } = VisualFieldRoleMetadata.Empty;
    public VisualFormattingMetadata Formatting { get; init; } = VisualFormattingMetadata.Empty;
    public FilterTopologyMetadata Filter { get; init; } = FilterTopologyMetadata.Empty;

    public bool IsSlicer => Type is "slicer" or "advancedSlicerVisual";
    public bool IsKpiCard => Type is "card" or "kpiVisual" or "multiRowCard";
    public bool IsPieDonut => Type is "pieChart" or "donutChart";
    public bool IsTrend => Type is "lineChart" or "areaChart" or "lineAndStackedColumnChart" or "lineAndClusteredColumnChart";
    public bool IsComparison => Type is "clusteredColumnChart" or "clusteredBarChart"
                                      or "stackedColumnChart" or "stackedBarChart"
                                      or "barChart" or "columnChart" or "waterfallChart";
    public string? VisibleTitleText => Text.VisibleTitleText;
    public string? VisibleSubtitleText => Text.VisibleSubtitleText;
    public string? TextBoxText => Text.TextBoxText;
    public string? BestVisibleText => FirstNonBlank(VisibleTitleText, TextBoxText, VisibleSubtitleText);
    public bool HasVisibleTitleIntent => !string.IsNullOrWhiteSpace(BestVisibleText);

    public bool IsNavigationElement
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Type))
            {
                return false;
            }

            var normalized = Type.Trim().ToLowerInvariant();
            return normalized is "actionbutton"
                or "navigationbutton"
                or "basicshape"
                or "shape"
                or "image"
                or "slicer"
                or "advancedslicervisual"
                or "qnavisual"
                || normalized.Contains("button", StringComparison.Ordinal)
                || normalized.Contains("image", StringComparison.Ordinal)
                || normalized.EndsWith("slicer", StringComparison.Ordinal);
        }
    }

    public bool IsDecorative => string.IsNullOrWhiteSpace(Type)
        || Type is "image" or "textbox" or "shape" or "basicShape" or "actionButton" or "navigationButton";

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

internal sealed record VisualTextMetadata(string? VisibleTitleText, string? VisibleSubtitleText, string? TextBoxText)
{
    public static VisualTextMetadata Empty { get; } = new(null, null, null);
}

internal sealed record VisualLabelMetadata(bool? HasLegend, bool? HasAxisLabels, bool? HasDataLabels)
{
    public static VisualLabelMetadata Empty { get; } = new(null, null, null);
}

internal sealed record VisualFieldRoleMetadata(
    IReadOnlyList<string> CategoryHints,
    IReadOnlyList<string> ValueHints,
    IReadOnlyList<string> SeriesHints,
    IReadOnlyList<string> MeasureHints)
{
    public static VisualFieldRoleMetadata Empty { get; } = new([], [], [], []);
}

internal sealed record VisualFormattingMetadata(
    string? BackgroundFillColor,
    string? FontColor,
    bool? HasBorder,
    double? CornerRadius,
    bool? HasShadow)
{
    public static VisualFormattingMetadata Empty { get; } = new(null, null, null, null, null);
}

internal sealed record FilterTopologyMetadata(
    IReadOnlyList<string> FieldHints,
    string? HierarchyPattern,
    int HierarchyDepth,
    string? FilterType)
{
    public static FilterTopologyMetadata Empty { get; } = new([], null, 0, null);
}

internal sealed record FilterDefinitionData(
    string SourceId,
    StoryFilterScope Scope,
    string DisplayLabel,
    IReadOnlyList<string> FieldHints,
    string? HierarchyPattern,
    int HierarchyDepth,
    string? FilterType,
    string? PlacementZone,
    bool IsMalformed);
