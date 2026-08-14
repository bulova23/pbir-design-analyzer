using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal enum LocalPbirGenerationFontWeight
{
    Normal,
    Bold
}

internal enum LocalPbirGenerationTextAlignment
{
    Left,
    Center,
    Right
}

internal enum LocalPbirGenerationFilterScope
{
    Report,
    Page,
    Visual
}

internal enum LocalPbirGenerationInteractionMode
{
    CrossHighlight,
    CrossFilter,
    Disabled
}

internal enum LocalPbirGenerationTableWidthBehavior
{
    FitToContent,
    FitToPage
}

internal enum LocalPbirGenerationAxisOrientation
{
    Horizontal,
    Vertical
}

internal enum LocalPbirGenerationLegendPlacement
{
    Top,
    Bottom,
    Left,
    Right
}

internal enum LocalPbirGenerationConditionalFormattingKind
{
    Threshold,
    PositiveNegative,
    NullDefault
}

internal enum LocalPbirGenerationVisualTemplate
{
    Default,
    Executive,
    Compact
}

internal sealed record LocalPbirGenerationAxisConfiguration(
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("visible")] bool Visible = true,
    [property: JsonPropertyName("orientation")] LocalPbirGenerationAxisOrientation Orientation = LocalPbirGenerationAxisOrientation.Horizontal,
    [property: JsonPropertyName("format")] string? Format = null);

internal sealed record LocalPbirGenerationLegendConfiguration(
    [property: JsonPropertyName("visible")] bool Visible = true,
    [property: JsonPropertyName("placement")] LocalPbirGenerationLegendPlacement Placement = LocalPbirGenerationLegendPlacement.Right,
    [property: JsonPropertyName("title")] string? Title = null);

internal sealed record LocalPbirGenerationConditionalFormatting(
    [property: JsonPropertyName("kind")] LocalPbirGenerationConditionalFormattingKind Kind,
    [property: JsonPropertyName("color")] LocalPbirGenerationColor Color,
    [property: JsonPropertyName("threshold")] decimal? Threshold = null,
    [property: JsonPropertyName("negativeColor")] LocalPbirGenerationColor? NegativeColor = null,
    [property: JsonPropertyName("defaultColor")] LocalPbirGenerationColor? DefaultColor = null);

internal sealed record LocalPbirGenerationColor(
    [property: JsonPropertyName("hex")] string Hex);

internal sealed record LocalPbirGenerationPadding(
    [property: JsonPropertyName("top")] int Top,
    [property: JsonPropertyName("right")] int Right,
    [property: JsonPropertyName("bottom")] int Bottom,
    [property: JsonPropertyName("left")] int Left);

internal sealed record LocalPbirGenerationTextStyle(
    [property: JsonPropertyName("fontFamily")] string? FontFamily = null,
    [property: JsonPropertyName("fontSize")] int? FontSize = null,
    [property: JsonPropertyName("fontWeight")] LocalPbirGenerationFontWeight? FontWeight = null,
    [property: JsonPropertyName("color")] LocalPbirGenerationColor? Color = null,
    [property: JsonPropertyName("alignment")] LocalPbirGenerationTextAlignment? Alignment = null);

internal sealed record LocalPbirGenerationBoxStyle(
    [property: JsonPropertyName("background")] LocalPbirGenerationColor? Background = null,
    [property: JsonPropertyName("borderColor")] LocalPbirGenerationColor? BorderColor = null,
    [property: JsonPropertyName("borderWidth")] int? BorderWidth = null,
    [property: JsonPropertyName("padding")] LocalPbirGenerationPadding? Padding = null);

internal sealed record LocalPbirGenerationCardFormatting(
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("subtitle")] string? Subtitle = null,
    [property: JsonPropertyName("label")] LocalPbirGenerationTextStyle? Label = null,
    [property: JsonPropertyName("numberFormat")] string? NumberFormat = null,
    [property: JsonPropertyName("box")] LocalPbirGenerationBoxStyle? Box = null,
    [property: JsonPropertyName("alignment")] LocalPbirGenerationTextAlignment? Alignment = null);

internal sealed record LocalPbirGenerationTableColumnFormatting(
    [property: JsonPropertyName("property")] string Property,
    [property: JsonPropertyName("alignment")] LocalPbirGenerationTextAlignment Alignment,
    [property: JsonPropertyName("numberFormat")] string? NumberFormat = null);

internal sealed record LocalPbirGenerationTableFormatting(
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("subtitle")] string? Subtitle = null,
    [property: JsonPropertyName("header")] LocalPbirGenerationTextStyle? Header = null,
    [property: JsonPropertyName("row")] LocalPbirGenerationTextStyle? Row = null,
    [property: JsonPropertyName("alternateRowColor")] LocalPbirGenerationColor? AlternateRowColor = null,
    [property: JsonPropertyName("numberFormat")] string? NumberFormat = null,
    [property: JsonPropertyName("columns")] IReadOnlyList<LocalPbirGenerationTableColumnFormatting>? Columns = null,
    [property: JsonPropertyName("widthBehavior")] LocalPbirGenerationTableWidthBehavior? WidthBehavior = null,
    [property: JsonPropertyName("box")] LocalPbirGenerationBoxStyle? Box = null);

internal sealed record LocalPbirGenerationChartFormatting(
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("axisLabels")] bool? AxisLabels = null,
    [property: JsonPropertyName("legendVisible")] bool? LegendVisible = null,
    [property: JsonPropertyName("colors")] IReadOnlyList<LocalPbirGenerationColor>? Colors = null,
    [property: JsonPropertyName("background")] LocalPbirGenerationColor? Background = null);

internal sealed record LocalPbirGenerationSlicerFormatting(
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("label")] LocalPbirGenerationTextStyle? Label = null);

internal sealed record LocalPbirGenerationVisualAuthoring(
    [property: JsonPropertyName("card")] LocalPbirGenerationCardFormatting? Card = null,
    [property: JsonPropertyName("table")] LocalPbirGenerationTableFormatting? Table = null,
    [property: JsonPropertyName("chart")] LocalPbirGenerationChartFormatting? Chart = null,
    [property: JsonPropertyName("slicer")] LocalPbirGenerationSlicerFormatting? Slicer = null,
    [property: JsonPropertyName("filters")] IReadOnlyList<LocalPbirGenerationEqualityFilter>? Filters = null,
    [property: JsonPropertyName("interaction")] LocalPbirGenerationInteractionSettings? Interaction = null,
    [property: JsonPropertyName("padding")] LocalPbirGenerationPadding? Padding = null,
    [property: JsonPropertyName("template")] LocalPbirGenerationVisualTemplate? Template = null,
    [property: JsonPropertyName("axis")] LocalPbirGenerationAxisConfiguration? Axis = null,
    [property: JsonPropertyName("legend")] LocalPbirGenerationLegendConfiguration? Legend = null,
    [property: JsonPropertyName("tooltips")] IReadOnlyList<LocalPbirGenerationBinding>? Tooltips = null,
    [property: JsonPropertyName("conditionalFormatting")] LocalPbirGenerationConditionalFormatting? ConditionalFormatting = null);

internal sealed record LocalPbirGenerationPageAuthoring(
    [property: JsonPropertyName("background")] LocalPbirGenerationColor? Background = null,
    [property: JsonPropertyName("filters")] IReadOnlyList<LocalPbirGenerationEqualityFilter>? Filters = null,
    [property: JsonPropertyName("padding")] LocalPbirGenerationPadding? Padding = null);

internal sealed record LocalPbirGenerationEqualityFilter(
    [property: JsonPropertyName("filterId")] string FilterId,
    [property: JsonPropertyName("kind")] LocalPbirGenerationBindingKind Kind,
    [property: JsonPropertyName("entity")] string Entity,
    [property: JsonPropertyName("property")] string Property,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("displayName")] string? DisplayName = null);

internal sealed record LocalPbirGenerationTheme(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("fontFamily")] string? FontFamily = null,
    [property: JsonPropertyName("fontSize")] int? FontSize = null,
    [property: JsonPropertyName("backgroundColor")] LocalPbirGenerationColor? BackgroundColor = null,
    [property: JsonPropertyName("accentColor")] LocalPbirGenerationColor? AccentColor = null,
    [property: JsonPropertyName("palette")] IReadOnlyList<LocalPbirGenerationColor>? Palette = null);

internal sealed record LocalPbirGenerationReportMetadata(
    [property: JsonPropertyName("author")] string? Author = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("displayName")] string? DisplayName = null);

internal sealed record LocalPbirGenerationInteractionSettings(
    [property: JsonPropertyName("mode")] LocalPbirGenerationInteractionMode Mode,
    [property: JsonPropertyName("enabled")] bool Enabled = true);

internal sealed record LocalPbirGenerationLayoutSettings(
    [property: JsonPropertyName("margin")] int Margin = 24,
    [property: JsonPropertyName("spacing")] int Spacing = 16,
    [property: JsonPropertyName("alignment")] LocalPbirGenerationTextAlignment Alignment = LocalPbirGenerationTextAlignment.Left,
    [property: JsonPropertyName("visualPadding")] int VisualPadding = 0);
