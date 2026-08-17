using System.Text.Json;
using System.Text.Json.Nodes;

namespace PowerBIModelingService.Services.Pbir.CustomVisualEvidence;

/// <summary>
/// Extracts safe, read-only, statically-derived evidence for visuals whose visualType is not
/// on the native allow-list. Never renders anything. Deneb and HTML Content get dedicated
/// extraction; everything else gets a minimal "not analyzed" record.
/// </summary>
public static class CustomVisualEvidenceExtractor
{
    public static CustomVisualEvidence? Extract(JsonObject visual, string visualType)
    {
        if (NativeVisualTypeCatalog.IsNative(visualType))
        {
            return null;
        }

        if (visualType.StartsWith("deneb", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractDeneb(visual, visualType);
        }

        if (visualType.StartsWith("htmlContent", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractHtmlContent(visual, visualType);
        }

        return new CustomVisualEvidence
        {
            Kind = "genericCustom",
            VisualType = visualType,
        };
    }

    private static CustomVisualEvidence ExtractDeneb(JsonObject visual, string visualType)
    {
        var vega = visual["objects"]?["vega"] as JsonObject;
        var provider = ReadFormattingStringValue(vega?["provider"]) ?? "vegaLite";
        var isRawVega = string.Equals(provider, "vega", StringComparison.OrdinalIgnoreCase);

        var evidence = new CustomVisualEvidence
        {
            Kind = "deneb",
            VisualType = visualType,
            DenebIsRawVegaProvider = isRawVega,
        };

        if (isRawVega)
        {
            return evidence;
        }

        var specJson = ReadFormattingStringValue(vega?["jsonSpec"]);
        if (string.IsNullOrWhiteSpace(specJson))
        {
            return evidence;
        }

        JsonObject? spec;
        try
        {
            spec = JsonNode.Parse(specJson) as JsonObject;
        }
        catch (JsonException)
        {
            return evidence with { DenebSpecUnparseable = true };
        }

        if (spec is null)
        {
            return evidence with { DenebSpecUnparseable = true };
        }

        var encoding = spec["encoding"] as JsonObject;
        var encodings = new List<DenebEncodingEvidence>();
        if (encoding is not null)
        {
            foreach (var (channel, node) in encoding)
            {
                if ((node as JsonObject)?["field"] is JsonValue fieldValue
                    && fieldValue.TryGetValue<string>(out var field)
                    && !string.IsNullOrWhiteSpace(field))
                {
                    encodings.Add(new DenebEncodingEvidence(channel, field));
                }
            }
        }

        return evidence with
        {
            DenebMarkType = ReadMarkType(spec["mark"]),
            DenebEncodings = encodings,
            DenebHasTooltip = encoding?.ContainsKey("tooltip") ?? false,
            DenebHasLegend = encoding is not null && encoding.Any(pair =>
                (pair.Value as JsonObject)?["legend"] is not null),
            DenebHasAxisTitles = encoding is not null && encoding.Any(pair =>
                (pair.Value as JsonObject)?["axis"] is not null) || spec["axis"] is not null,
            DenebHasTitle = spec["title"] is not null,
        };
    }

    private static string? ReadMarkType(JsonNode? mark) => mark switch
    {
        JsonValue value when value.TryGetValue<string>(out var s) => s,
        JsonObject obj when obj["type"] is JsonValue typeValue && typeValue.TryGetValue<string>(out var t) => t,
        _ => null,
    };

    private static CustomVisualEvidence ExtractHtmlContent(JsonObject visual, string visualType)
    {
        var contentFormatting = visual["objects"]?["contentFormatting"] as JsonObject;
        var templates = visual["objects"]?["templates"] as JsonObject;
        var stylesheet = visual["objects"]?["stylesheet"] as JsonObject;

        var bodyTemplate = ReadFormattingStringValue(templates?["bodyTemplate"]);
        var rowTemplate = ReadFormattingStringValue(templates?["rowTemplate"]);
        var stylesheetText = ReadFormattingStringValue(stylesheet?["stylesheet"]);
        var staticText = string.Join(' ', new[] { bodyTemplate, rowTemplate, stylesheetText }.Where(s => !string.IsNullOrEmpty(s)));

        return new CustomVisualEvidence
        {
            Kind = "htmlContent",
            VisualType = visualType,
            HtmlShowRawHtml = ReadFormattingBoolValue(contentFormatting?["showRawHtml"]),
            HtmlOverrideInlineStyling = ReadFormattingBoolValue(contentFormatting?["overrideInlineStyling"]),
            HtmlEnableDiagnostics = ReadFormattingBoolValue(contentFormatting?["enableDiagnostics"]),
            HtmlFormat = ReadFormattingStringValue(contentFormatting?["format"]),
            HtmlStaticTemplateHasScriptTag = staticText.Contains("<script", StringComparison.OrdinalIgnoreCase),
            HtmlStaticTemplateHasExternalResource = System.Text.RegularExpressions.Regex.IsMatch(
                staticText, @"(?:src|href)\s*=\s*[""']https?://", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            HtmlContentIsDynamicallyBound = string.IsNullOrEmpty(bodyTemplate) && string.IsNullOrEmpty(rowTemplate),
        };
    }

    // PBIR formatting-pane properties are either a literal value or an { expr: { Literal: { Value: "'...'" } } }
    // measure/expression wrapper. This reads the literal case only — a measure-bound property returns null,
    // which callers treat as "not statically known" rather than guessing.
    private static string? ReadFormattingStringValue(JsonNode? node)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var direct))
        {
            return direct;
        }

        if (node?["expr"]?["Literal"]?["Value"] is not JsonValue literalValue || !literalValue.TryGetValue<string>(out var literal))
        {
            return null;
        }

        return literal.Length >= 2 && literal.StartsWith('\'') && literal.EndsWith('\'')
            ? literal[1..^1]
            : literal;
    }

    private static bool? ReadFormattingBoolValue(JsonNode? node)
    {
        if (node is JsonValue value && value.TryGetValue<bool>(out var direct))
        {
            return direct;
        }

        if (node?["expr"]?["Literal"]?["Value"] is not JsonValue literalValue || !literalValue.TryGetValue<string>(out var literal))
        {
            return null;
        }

        return bool.TryParse(literal, out var parsed) ? parsed : null;
    }
}
