using System.Text.Json.Nodes;
using PowerBIModelingService.Services.Pbir.CustomVisualEvidence;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

public sealed class CustomVisualEvidenceExtractorTests
{
    [Fact]
    public void Extract_NativeVisualType_ReturnsNull()
    {
        var visual = JsonNode.Parse("""{"visualType": "barChart", "objects": {}}""")!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "barChart");

        Assert.Null(result);
    }

    [Fact]
    public void Extract_DenebVegaLiteSpec_ExtractsMarkAndEncodings()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "deneb7E15AEF80B9E4D4F8E12924291ECE89A",
          "objects": {
            "vega": {
              "provider": { "expr": { "Literal": { "Value": "'vegaLite'" } } },
              "jsonSpec": { "expr": { "Literal": { "Value": "'{\"mark\":\"bar\",\"encoding\":{\"x\":{\"field\":\"Category\"},\"y\":{\"field\":\"Total Sales\"},\"tooltip\":{\"field\":\"Total Sales\"}},\"title\":\"Sales by Category\",\"axis\":{}}'" } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "deneb7E15AEF80B9E4D4F8E12924291ECE89A");

        Assert.NotNull(result);
        Assert.Equal("deneb", result!.Kind);
        Assert.Equal("bar", result.DenebMarkType);
        Assert.Contains(result.DenebEncodings, e => e.Channel == "x" && e.FieldOrMeasure == "Category");
        Assert.Contains(result.DenebEncodings, e => e.Channel == "y" && e.FieldOrMeasure == "Total Sales");
        Assert.True(result.DenebHasTooltip);
        Assert.True(result.DenebHasTitle);
        Assert.False(result.DenebIsRawVegaProvider);
    }

    [Fact]
    public void Extract_DenebMissingTooltipEncoding_ReportsNoTooltip()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "deneb7E15AEF80B9E4D4F8E12924291ECE89A",
          "objects": {
            "vega": {
              "provider": { "expr": { "Literal": { "Value": "'vegaLite'" } } },
              "jsonSpec": { "expr": { "Literal": { "Value": "'{\"mark\":\"line\",\"encoding\":{\"x\":{\"field\":\"Month\"},\"y\":{\"field\":\"Revenue\"}}}'" } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "deneb7E15AEF80B9E4D4F8E12924291ECE89A");

        Assert.NotNull(result);
        Assert.False(result!.DenebHasTooltip);
    }

    [Fact]
    public void Extract_DenebMalformedSpecJson_FallsBackToUnparseableFlag()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "deneb7E15AEF80B9E4D4F8E12924291ECE89A",
          "objects": {
            "vega": {
              "provider": { "expr": { "Literal": { "Value": "'vegaLite'" } } },
              "jsonSpec": { "expr": { "Literal": { "Value": "'{not valid json'" } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "deneb7E15AEF80B9E4D4F8E12924291ECE89A");

        Assert.NotNull(result);
        Assert.Equal("deneb", result!.Kind);
        Assert.True(result.DenebSpecUnparseable);
        Assert.Null(result.DenebMarkType);
    }

    [Fact]
    public void Extract_DenebRawVegaProvider_SkipsStructuredEncodingExtraction()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "deneb7E15AEF80B9E4D4F8E12924291ECE89A",
          "objects": {
            "vega": {
              "provider": { "expr": { "Literal": { "Value": "'vega'" } } },
              "jsonSpec": { "expr": { "Literal": { "Value": "'{\"marks\":[]}'" } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "deneb7E15AEF80B9E4D4F8E12924291ECE89A");

        Assert.NotNull(result);
        Assert.True(result!.DenebIsRawVegaProvider);
        Assert.Null(result.DenebMarkType);
        Assert.Empty(result.DenebEncodings);
    }

    [Fact]
    public void Extract_HtmlContentWithRawHtmlAndScriptInBodyTemplate_FlagsBoth()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "htmlContent443BE3AD55E043BF878BED274D3A6865",
          "objects": {
            "contentFormatting": {
              "showRawHtml": { "expr": { "Literal": { "Value": "true" } } },
              "overrideInlineStyling": { "expr": { "Literal": { "Value": "false" } } },
              "format": { "expr": { "Literal": { "Value": "'html'" } } }
            },
            "templates": {
              "bodyTemplate": { "expr": { "Literal": { "Value": "'<div><script>alert(1)</script></div>'" } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "htmlContent443BE3AD55E043BF878BED274D3A6865");

        Assert.NotNull(result);
        Assert.Equal("htmlContent", result!.Kind);
        Assert.True(result.HtmlShowRawHtml);
        Assert.True(result.HtmlStaticTemplateHasScriptTag);
    }

    [Fact]
    public void Extract_HtmlContentWithNoStaticTemplate_FlagsContentAsDynamic()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "htmlContent443BE3AD55E043BF878BED274D3A6865",
          "objects": {
            "contentFormatting": {
              "showRawHtml": { "expr": { "Literal": { "Value": "false" } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "htmlContent443BE3AD55E043BF878BED274D3A6865");

        Assert.NotNull(result);
        Assert.True(result!.HtmlContentIsDynamicallyBound);
        Assert.False(result.HtmlStaticTemplateHasScriptTag);
    }

    [Fact]
    public void Extract_UnrecognizedCustomVisualType_ReturnsGenericEvidence()
    {
        var visual = JsonNode.Parse("""{"visualType": "PBI_CV_1234567890ABCDEF", "objects": {}}""")!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "PBI_CV_1234567890ABCDEF");

        Assert.NotNull(result);
        Assert.Equal("genericCustom", result!.Kind);
        Assert.Equal("PBI_CV_1234567890ABCDEF", result.VisualType);
    }

    [Fact]
    public void Extract_DenebFieldRepeatEncoding_DoesNotThrowAndExcludesChannel()
    {
        // "field" as an object (Vega-Lite field-repeat templating, e.g. {"repeat":"column"}) is a
        // real, documented pattern — not the plain-string shape ReadFormattingStringValue-style
        // readers expect. Must degrade gracefully rather than crash.
        var visual = JsonNode.Parse("""
        {
          "visualType": "deneb7E15AEF80B9E4D4F8E12924291ECE89A",
          "objects": {
            "vega": {
              "provider": { "expr": { "Literal": { "Value": "'vegaLite'" } } },
              "jsonSpec": { "expr": { "Literal": { "Value": "'{\"mark\":\"bar\",\"encoding\":{\"x\":{\"field\":{\"repeat\":\"column\"}},\"y\":{\"field\":\"Total Sales\"}}}'" } } }
            }
          }
        }
        """)!.AsObject();

        var exception = Record.Exception(() => CustomVisualEvidenceExtractor.Extract(visual, "deneb7E15AEF80B9E4D4F8E12924291ECE89A"));

        Assert.Null(exception);
        var result = CustomVisualEvidenceExtractor.Extract(visual, "deneb7E15AEF80B9E4D4F8E12924291ECE89A");
        Assert.NotNull(result);
        Assert.False(result!.DenebSpecUnparseable);
        Assert.DoesNotContain(result.DenebEncodings, e => e.Channel == "x");
        Assert.Contains(result.DenebEncodings, e => e.Channel == "y" && e.FieldOrMeasure == "Total Sales");
    }

    [Fact]
    public void Extract_HtmlContentTemplateWithExternalResource_FlagsExternalResource()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "htmlContent443BE3AD55E043BF878BED274D3A6865",
          "objects": {
            "templates": {
              "bodyTemplate": { "expr": { "Literal": { "Value": "'<div><a href=\"https://example.com\">link</a></div>'" } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "htmlContent443BE3AD55E043BF878BED274D3A6865");

        Assert.NotNull(result);
        Assert.True(result!.HtmlStaticTemplateHasExternalResource);
    }

    [Fact]
    public void Extract_HtmlContentTemplateWithoutExternalResource_DoesNotFlag()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "htmlContent443BE3AD55E043BF878BED274D3A6865",
          "objects": {
            "templates": {
              "bodyTemplate": { "expr": { "Literal": { "Value": "'<div>plain content</div>'" } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "htmlContent443BE3AD55E043BF878BED274D3A6865");

        Assert.NotNull(result);
        Assert.False(result!.HtmlStaticTemplateHasExternalResource);
    }

    [Fact]
    public void Extract_DenebEncodingWithLegendAndAxis_FlagsBoth()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "deneb7E15AEF80B9E4D4F8E12924291ECE89A",
          "objects": {
            "vega": {
              "provider": { "expr": { "Literal": { "Value": "'vegaLite'" } } },
              "jsonSpec": { "expr": { "Literal": { "Value": "'{\"mark\":\"bar\",\"encoding\":{\"x\":{\"field\":\"Category\",\"axis\":{\"title\":\"Category\"}},\"color\":{\"field\":\"Segment\",\"legend\":{\"title\":\"Segment\"}}}}'" } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "deneb7E15AEF80B9E4D4F8E12924291ECE89A");

        Assert.NotNull(result);
        Assert.True(result!.DenebHasLegend);
        Assert.True(result.DenebHasAxisTitles);
    }

    [Fact]
    public void Extract_HtmlShowRawHtmlBoundToNonLiteralExpr_ReturnsNull()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "htmlContent443BE3AD55E043BF878BED274D3A6865",
          "objects": {
            "contentFormatting": {
              "showRawHtml": { "expr": { "Aggregation": { "Function": 0, "Expression": {} } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "htmlContent443BE3AD55E043BF878BED274D3A6865");

        Assert.NotNull(result);
        Assert.Null(result!.HtmlShowRawHtml);
    }

    [Fact]
    public void Extract_HtmlShowRawHtmlAsBareLiteral_ReadsCorrectly()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "htmlContent443BE3AD55E043BF878BED274D3A6865",
          "objects": {
            "contentFormatting": {
              "showRawHtml": true
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "htmlContent443BE3AD55E043BF878BED274D3A6865");

        Assert.NotNull(result);
        Assert.True(result!.HtmlShowRawHtml);
    }

    [Fact]
    public void Extract_DenebProviderOmitted_DefaultsToVegaLiteBehavior()
    {
        var visual = JsonNode.Parse("""
        {
          "visualType": "deneb7E15AEF80B9E4D4F8E12924291ECE89A",
          "objects": {
            "vega": {
              "jsonSpec": { "expr": { "Literal": { "Value": "'{\"mark\":\"bar\",\"encoding\":{\"x\":{\"field\":\"Category\"}}}'" } } }
            }
          }
        }
        """)!.AsObject();

        var result = CustomVisualEvidenceExtractor.Extract(visual, "deneb7E15AEF80B9E4D4F8E12924291ECE89A");

        Assert.NotNull(result);
        Assert.False(result!.DenebIsRawVegaProvider);
        Assert.Equal("bar", result.DenebMarkType);
        Assert.Contains(result.DenebEncodings, e => e.Channel == "x" && e.FieldOrMeasure == "Category");
    }
}
