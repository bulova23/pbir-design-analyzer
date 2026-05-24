namespace PowerBIModelingService.Services.Pbir.Models;

/// <summary>
/// Summarizes the richer visual metadata extracted for a scored page.
/// Exposed to the VS Code UI so users can inspect what the parser actually detected.
/// </summary>
public sealed class PageVisualMetadataSummary
{
    /// <summary>Gets or sets the display name of the page the metadata belongs to.</summary>
    public required string PageName { get; init; }

    /// <summary>Gets or sets the first visible page title detected on the page, when available.</summary>
    public string? VisiblePageTitle { get; init; }

    /// <summary>
    /// Gets or sets the first visible page title that satisfies the strict governance rule
    /// (positioned in the top band of the canvas and not a vague placeholder).
    /// <c>null</c> when the page has no meaningful top-band title. Used by the
    /// <c>requirePageTitle</c> governance check.
    /// </summary>
    public string? StrictVisiblePageTitle { get; init; }

    /// <summary>Gets or sets the parsed canvas width for the page, when PBIR provided it.</summary>
    public double? CanvasWidth { get; init; }

    /// <summary>Gets or sets the parsed canvas height for the page, when PBIR provided it.</summary>
    public double? CanvasHeight { get; init; }

    /// <summary>Gets or sets the total visual count on the page, including hidden visuals.</summary>
    public int VisualCount { get; init; }

    /// <summary>Gets or sets the count of visible visuals that expose a title, subtitle, or text intent.</summary>
    public int VisibleTitleVisualCount { get; init; }

    /// <summary>Gets or sets the count of text visuals detected on the page.</summary>
    public int TextVisualCount { get; init; }

    /// <summary>Gets or sets the count of slicer visuals detected on the page.</summary>
    public int SlicerCount { get; init; }

    /// <summary>Gets or sets the count of visuals with parsed legend presence.</summary>
    public int LegendVisualCount { get; init; }

    /// <summary>Gets or sets the count of visuals with parsed axis labels presence.</summary>
    public int AxisLabelVisualCount { get; init; }

    /// <summary>Gets or sets the count of visuals with parsed data labels presence.</summary>
    public int DataLabelVisualCount { get; init; }

    /// <summary>Gets or sets the count of visuals with any parsed surface formatting metadata.</summary>
    public int FormattedVisualCount { get; init; }

    /// <summary>Gets or sets the list of per-visual metadata records for the page.</summary>
    public List<VisualMetadataItem> Visuals { get; init; } = [];
}

/// <summary>
/// Structured metadata for an individual visual detected during PBIR scoring.
/// </summary>
public sealed class VisualMetadataItem
{
    /// <summary>Gets or sets the stable PBIR visual identifier.</summary>
    public required string VisualId { get; init; }

    /// <summary>Gets or sets the Power BI visual type.</summary>
    public required string VisualType { get; init; }

    /// <summary>Gets or sets the visual X coordinate.</summary>
    public double X { get; init; }

    /// <summary>Gets or sets the visual Y coordinate.</summary>
    public double Y { get; init; }

    /// <summary>Gets or sets the visual width.</summary>
    public double Width { get; init; }

    /// <summary>Gets or sets the visual height.</summary>
    public double Height { get; init; }

    /// <summary>Gets or sets a value indicating whether the visual is hidden.</summary>
    public bool IsHidden { get; init; }

    /// <summary>Gets or sets a value indicating whether the visual is classified as a navigation/control element.</summary>
    public bool IsNavigationElement { get; init; }

    /// <summary>Gets or sets a value indicating whether the visual is classified as decorative.</summary>
    public bool IsDecorative { get; init; }

    /// <summary>Gets or sets a value indicating whether the visual is a slicer/filter control.</summary>
    public bool IsSlicer { get; init; }

    /// <summary>Gets or sets the parsed visible title text, when available.</summary>
    public string? VisibleTitleText { get; init; }

    /// <summary>Gets or sets the parsed visible subtitle text, when available.</summary>
    public string? VisibleSubtitleText { get; init; }

    /// <summary>Gets or sets the parsed textbox/body text, when available.</summary>
    public string? TextBoxText { get; init; }

    /// <summary>Gets or sets the best visible text anchor for the visual.</summary>
    public string? BestVisibleText { get; init; }

    /// <summary>Gets or sets a value indicating whether any visible title/text intent was detected.</summary>
    public bool HasVisibleTitleIntent { get; init; }

    /// <summary>Gets or sets whether a legend was detected for the visual.</summary>
    public bool? HasLegend { get; init; }

    /// <summary>Gets or sets whether axis labels were detected for the visual.</summary>
    public bool? HasAxisLabels { get; init; }

    /// <summary>Gets or sets whether data labels were detected for the visual.</summary>
    public bool? HasDataLabels { get; init; }

    /// <summary>Gets or sets the parsed category role hints.</summary>
    public List<string> CategoryHints { get; init; } = [];

    /// <summary>Gets or sets the parsed value role hints.</summary>
    public List<string> ValueHints { get; init; } = [];

    /// <summary>Gets or sets the parsed series role hints.</summary>
    public List<string> SeriesHints { get; init; } = [];

    /// <summary>Gets or sets the parsed measure role hints.</summary>
    public List<string> MeasureHints { get; init; } = [];

    /// <summary>Gets or sets the parsed background fill color, when available.</summary>
    public string? BackgroundFillColor { get; init; }

    /// <summary>Gets or sets the parsed font color, when available.</summary>
    public string? FontColor { get; init; }

    /// <summary>Gets or sets whether a border was detected for the visual.</summary>
    public bool? HasBorder { get; init; }

    /// <summary>Gets or sets the parsed corner radius, when available.</summary>
    public double? CornerRadius { get; init; }

    /// <summary>Gets or sets whether a shadow/elevation treatment was detected.</summary>
    public bool? HasShadow { get; init; }
}
