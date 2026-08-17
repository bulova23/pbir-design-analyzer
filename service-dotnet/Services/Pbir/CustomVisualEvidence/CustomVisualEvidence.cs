namespace PowerBIModelingService.Services.Pbir.CustomVisualEvidence;

public sealed record DenebEncodingEvidence(string Channel, string FieldOrMeasure);

public sealed record CustomVisualEvidence
{
    // Plain string, not an enum: this value is round-tripped through the scoring RPC's
    // JSON serializer options, which are not confirmed to have a JsonStringEnumConverter
    // configured (only the separate authoring RPC path does) — an enum here risks silently
    // serializing as an integer instead of a string. Matches the existing convention for
    // classification fields in this file, e.g. ChartIntentSummary.Intent. Always one of
    // "deneb", "htmlContent", "genericCustom".
    public required string Kind { get; init; }
    public required string VisualType { get; init; }

    // Deneb-specific (null when Kind != "deneb", or when the spec provider is "vega" rather than "vegaLite")
    public string? DenebMarkType { get; init; }
    public List<DenebEncodingEvidence> DenebEncodings { get; init; } = [];
    public bool? DenebHasAxisTitles { get; init; }
    public bool? DenebHasLegend { get; init; }
    public bool? DenebHasTooltip { get; init; }
    public bool? DenebHasTitle { get; init; }
    public bool DenebIsRawVegaProvider { get; init; }
    public bool DenebSpecUnparseable { get; init; }

    // HTML Content-specific (null when Kind != "htmlContent")
    public bool? HtmlShowRawHtml { get; init; }
    public bool? HtmlOverrideInlineStyling { get; init; }
    public bool? HtmlEnableDiagnostics { get; init; }
    public string? HtmlFormat { get; init; }
    public bool HtmlStaticTemplateHasScriptTag { get; init; }
    public bool HtmlStaticTemplateHasExternalResource { get; init; }
    public bool HtmlContentIsDynamicallyBound { get; init; }
}
