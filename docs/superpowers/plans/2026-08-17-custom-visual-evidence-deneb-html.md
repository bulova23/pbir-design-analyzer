# Custom Visual Evidence (Deneb / HTML Content) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stop Deneb, HTML Content, and other non-native PBIR visuals from being silently scored as native "comparison" charts. Extract real, statically-derived evidence instead and surface it as advisory findings — zero score impact, no rendering.

**Architecture:** A new backend `Services/Pbir/CustomVisualEvidence/` directory (mirroring the existing `CrossPageNarrative/` isolation precedent) detects non-native visual types via a shared allow-list (promoted from `PbirGovernanceService`), extracts structured evidence for Deneb and HTML Content specifically, and attaches it to the existing per-visual metadata model that already rides the RPC contract to the webview. A new `buildCustomVisualFindings` function in the existing `normalizedFindings.ts` turns that evidence into `NormalizedFinding` entries, routed into the existing Rendered Review checklist by evidence kind.

**Tech Stack:** .NET 8 / C# (`System.Text.Json.Nodes`), TypeScript (VS Code extension host), xUnit, Jest.

**Spec:** `docs/superpowers/specs/2026-08-17-custom-visual-evidence-deneb-html-design.md`

---

## Verified facts this plan depends on

These were confirmed against primary sources during planning — not assumed:

- Deneb's `visualType` is `deneb<GUID>` (verified against `deneb-viz/deneb`'s own `pbiviz.json`: `deneb7E15AEF80B9E4D4F8E12924291ECE89A`). Detection must be a **prefix match**, not an exact-string match.
- Deneb's Vega/Vega-Lite spec is stored at `visual.objects.vega.jsonSpec` (a JSON string), with `visual.objects.vega.provider` (`"vegaLite"` or `"vega"`) and `visual.objects.vega.jsonConfig` alongside it (verified against Deneb's own `capabilities.json`). Vega-Lite specs have a flat `mark`/`encoding` shape; raw Vega specs use `marks[].encode`/`axes`/`legends` and are structurally different — **this plan only extracts structured encoding evidence for `provider: "vegaLite"`**. `provider: "vega"` falls back to a lighter "raw Vega spec present, not structurally analyzed" record. This is a deliberate v1 scope limit, not a bug — call it out to the user when reporting completion.
- HTML Content's `visualType` is `htmlContent<GUID>` (verified against `dm-p/powerbi-visuals-html-content`'s `pbiviz.json`: `htmlContent443BE3AD55E043BF878BED274D3A6865`). This is the actively-maintained AppSource visual; an older, unrelated "HTML Viewer" visual may use a different GUID with no recognizable prefix — it will correctly fall into the generic custom-visual bucket instead, which is fine.
- **The actual HTML content is NOT statically inspectable.** It's bound to a `content` data role (measure/field-bound), verified against HTML Content's `capabilities.json`. What *is* statically available: `visual.objects.contentFormatting.{showRawHtml, enableDiagnostics, overrideInlineStyling, format, renderMode}` (booleans/strings — Power BI's own built-in sanitization toggles for this visual, a stronger signal than string-scanning content we don't have), and `visual.objects.templates.{bodyTemplate, rowTemplate}` / `visual.objects.stylesheet.stylesheet` (author-written static text properties, worth scanning for `<script`/`on\w+=`/external URLs since these — unlike `content` — are not data-bound).
- Existing `_knownVisualTypes` allow-list: `service-dotnet/Services/Pbir/PbirGovernanceService.cs:313-328`.
- Existing JSON-reading pattern in this codebase: `System.Text.Json.Nodes.JsonObject`/`JsonNode`, `node?["prop"]?.GetValue<T>()`, `as JsonObject` casts, per-visual `try`/`catch` with `_logger.LogWarning` (see `service-dotnet/Services/Pbir/ReportModelLoader.cs:169-198`).
- `VisualData` internal record: `service-dotnet/Services/Pbir/PbirScoringFoundationModels.cs:21-71`. Built by `ReportModelLoader.CreateVisualData` (`ReportModelLoader.cs:294-320`).
- `InferChartIntent`'s early-return guard: `service-dotnet/Services/Pbir/PbirScoringService.cs:3069-3074`.
- `VisualMetadataItem` C# DTO: `service-dotnet/Services/Pbir/Models/VisualMetadataSummary.cs:102-189`. Built by `BuildVisualMetadataItem` (`PbirScoringService.cs:2866-2900`), which is where `ChartIntent = chartIntent` gets mapped — the exact site to mirror for the new field.
- `VisualMetadataItem` TS contract: `vscode-extension/src/analyzer/contracts/scorePanel.ts:377-407`. `PageVisualMetadataSummary`: `scorePanel.ts:409-426`. `PageScore.visualMetadata?`: `scorePanel.ts:456`. `ScoreResult.visualMetadata?`: `scorePanel.ts:1117` (single-page-scoring mode).
- `normalizedFindings.ts`'s dual-mode structure: `buildNormalizedFindings` (line 478) branches on `result.pageScores` (full-report mode, delegates per-page work to `pushPageFindings` at line 376) vs. an `else` branch reading `result.feedback`/`result.visualMetadata` directly (single-page mode). **New custom-visual finding logic must be wired into both branches** — this is the one place in this plan where getting the wiring site wrong silently drops findings in one of the two scoring modes.
- `classifyRenderedReviewFinding`: `vscode-extension/src/analyzer/renderedReview/reviewModel.ts` (already special-cases `semanticModel` evidence by kind, not by keyword — the pattern to follow).

---

## Task 1: Promote the native visual-type catalog to a shared location

**Files:**
- Create: `service-dotnet/Services/Pbir/CustomVisualEvidence/NativeVisualTypeCatalog.cs`
- Modify: `service-dotnet/Services/Pbir/PbirGovernanceService.cs:313-328`
- Test: `service-dotnet/tests/PbirGovernanceServiceTests.cs`

This is a pure move — no behavior change. Do it first so later tasks have one source of truth.

- [ ] **Step 1: Write the failing test proving the catalog is reusable from outside `PbirGovernanceService`**

Add to `service-dotnet/tests/PbirGovernanceServiceTests.cs` (inside the existing `PbirGovernanceServiceTests` class):

```csharp
[Fact]
public void NativeVisualTypeCatalog_IsNative_RecognizesKnownNativeTypesCaseInsensitively()
{
    Assert.True(NativeVisualTypeCatalog.IsNative("barChart"));
    Assert.True(NativeVisualTypeCatalog.IsNative("BARCHART"));
    Assert.True(NativeVisualTypeCatalog.IsNative("tableEx"));
    Assert.False(NativeVisualTypeCatalog.IsNative("deneb7E15AEF80B9E4D4F8E12924291ECE89A"));
    Assert.False(NativeVisualTypeCatalog.IsNative("htmlContent443BE3AD55E043BF878BED274D3A6865"));
}
```

Add `using PowerBIModelingService.Services.Pbir.CustomVisualEvidence;` to the top of the test file.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd service-dotnet && dotnet test tests/Tests.csproj --filter "FullyQualifiedName~NativeVisualTypeCatalog_IsNative" --nologo`
Expected: FAIL — build error, `NativeVisualTypeCatalog` does not exist yet.

- [ ] **Step 3: Create the shared catalog**

Create `service-dotnet/Services/Pbir/CustomVisualEvidence/NativeVisualTypeCatalog.cs`:

```csharp
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
```

- [ ] **Step 4: Update `PbirGovernanceService.cs` to use the shared catalog**

In `service-dotnet/Services/Pbir/PbirGovernanceService.cs`, remove the `_knownVisualTypes` field (lines 313-328) and its doc comment. Add `using PowerBIModelingService.Services.Pbir.CustomVisualEvidence;` near the top of the file. At line 511 (`.Where(t => !_knownVisualTypes.Contains(t))`), change to:

```csharp
.Where(t => !NativeVisualTypeCatalog.IsNative(t))
```

- [ ] **Step 5: Run the new test and the full governance suite**

Run: `cd service-dotnet && dotnet test tests/Tests.csproj --filter "FullyQualifiedName~PbirGovernanceServiceTests|FullyQualifiedName~NativeVisualTypeCatalog" --nologo`
Expected: PASS, all tests including the pre-existing `allowCustomVisuals` governance tests (proves the move didn't change behavior).

- [ ] **Step 6: Commit**

```bash
git add service-dotnet/Services/Pbir/CustomVisualEvidence/NativeVisualTypeCatalog.cs service-dotnet/Services/Pbir/PbirGovernanceService.cs service-dotnet/tests/PbirGovernanceServiceTests.cs
git commit -m "refactor(pbir): promote native visual-type catalog to shared location"
```

---

## Task 2: Define the `CustomVisualEvidence` C# model

**Files:**
- Create: `service-dotnet/Services/Pbir/CustomVisualEvidence/CustomVisualEvidence.cs`

Pure data model, no logic — no test needed for a plain record definition (nothing to assert against yet; it's exercised by Task 3's tests).

- [ ] **Step 1: Create the model file**

```csharp
namespace PowerBIModelingService.Services.Pbir.CustomVisualEvidence;

public sealed record DenebEncodingEvidence(string Channel, string FieldOrMeasure);

public sealed record CustomVisualEvidence
{
    // Plain string, not an enum: this model has no existing enum precedent anywhere in
    // Services/Pbir/Models, and the scoring RPC serializer is not confirmed to have a
    // JsonStringEnumConverter configured (only the separate authoring RPC path does).
    // Matches the existing convention for classification fields in this file, e.g.
    // ChartIntentSummary.Intent. Always one of "deneb", "htmlContent", "genericCustom".
    public required string Kind { get; init; }
    public required string VisualType { get; init; }

    // Deneb-specific (null when Kind != Deneb, or when the spec provider is "vega" rather than "vegaLite")
    public string? DenebMarkType { get; init; }
    public List<DenebEncodingEvidence> DenebEncodings { get; init; } = [];
    public bool? DenebHasAxisTitles { get; init; }
    public bool? DenebHasLegend { get; init; }
    public bool? DenebHasTooltip { get; init; }
    public bool? DenebHasTitle { get; init; }
    public bool DenebIsRawVegaProvider { get; init; }
    public bool DenebSpecUnparseable { get; init; }

    // HTML Content-specific (null when Kind != HtmlContent)
    public bool? HtmlShowRawHtml { get; init; }
    public bool? HtmlOverrideInlineStyling { get; init; }
    public bool? HtmlEnableDiagnostics { get; init; }
    public string? HtmlFormat { get; init; }
    public bool HtmlStaticTemplateHasScriptTag { get; init; }
    public bool HtmlStaticTemplateHasExternalResource { get; init; }
    public bool HtmlContentIsDynamicallyBound { get; init; }
}
```

- [ ] **Step 2: Commit**

```bash
git add service-dotnet/Services/Pbir/CustomVisualEvidence/CustomVisualEvidence.cs
git commit -m "feat(pbir): add CustomVisualEvidence model"
```

---

## Task 3: Implement `CustomVisualEvidenceExtractor`

**Files:**
- Create: `service-dotnet/Services/Pbir/CustomVisualEvidence/CustomVisualEvidenceExtractor.cs`
- Test: `service-dotnet/tests/Services/CustomVisualEvidenceExtractorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `service-dotnet/tests/Services/CustomVisualEvidenceExtractorTests.cs`:

```csharp
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
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd service-dotnet && dotnet test tests/Tests.csproj --filter "FullyQualifiedName~CustomVisualEvidenceExtractorTests" --nologo`
Expected: FAIL — build error, `CustomVisualEvidenceExtractor` does not exist yet.

- [ ] **Step 3: Implement the extractor**

Create `service-dotnet/Services/Pbir/CustomVisualEvidence/CustomVisualEvidenceExtractor.cs`:

```csharp
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
                var field = (node as JsonObject)?["field"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(field))
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
        JsonObject obj => obj["type"]?.GetValue<string>(),
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

        var literal = node?["expr"]?["Literal"]?["Value"]?.GetValue<string>();
        if (literal is null)
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

        var literal = node?["expr"]?["Literal"]?["Value"]?.GetValue<string>();
        return literal is null ? null : bool.TryParse(literal, out var parsed) ? parsed : null;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd service-dotnet && dotnet test tests/Tests.csproj --filter "FullyQualifiedName~CustomVisualEvidenceExtractorTests" --nologo`
Expected: PASS, all 8 tests.

- [ ] **Step 5: Commit**

```bash
git add service-dotnet/Services/Pbir/CustomVisualEvidence/CustomVisualEvidenceExtractor.cs service-dotnet/tests/Services/CustomVisualEvidenceExtractorTests.cs
git commit -m "feat(pbir): extract structured evidence for Deneb and HTML Content visuals"
```

---

## Task 4: Wire the extractor into the scoring pipeline

**Files:**
- Modify: `service-dotnet/Services/Pbir/PbirScoringFoundationModels.cs:21-71` (add field to `VisualData`)
- Modify: `service-dotnet/Services/Pbir/ReportModelLoader.cs:171-201, 294-320` (call extractor, pass raw visual JSON through)
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs:3069-3074` (skip chart-intent for non-native types), `:2866-2900` (map the new field)
- Modify: `service-dotnet/Services/Pbir/Models/VisualMetadataSummary.cs:102-189` (add field to `VisualMetadataItem`)
- Test: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`

- [ ] **Step 1: Write the failing end-to-end test**

Add to `service-dotnet/tests/Services/PbirScoringServiceTests.cs`, using the existing `CreateTempPbirFolderWithDirectoryVisuals` helper (already used by e.g. `ScoreAsync_DirectoryVisualParser_UsesVisualJsonTitleMetadata` at line 605) — this writes raw `visual.json` content per visual directly, which is what's needed to include a Deneb `objects.vega` block:

```csharp
[Fact]
public async Task ScoreAsync_VisualMetadata_AttachesCustomVisualEvidenceAndSkipsChartIntentForDeneb()
{
    var tempDir = CreateTempPbirFolderWithDirectoryVisuals(
        """{"displayName":"Page 1"}""",
        ("deneb1",
        """
        {"name":"deneb1","position":{"x":0,"y":0,"width":320,"height":180},
         "visual":{"visualType":"deneb7E15AEF80B9E4D4F8E12924291ECE89A",
           "objects":{"vega":{
             "provider":{"expr":{"Literal":{"Value":"'vegaLite'"}}},
             "jsonSpec":{"expr":{"Literal":{"Value":"'{\"mark\":\"bar\",\"encoding\":{\"x\":{\"field\":\"Category\"},\"y\":{\"field\":\"Total Sales\"}}}'"}}}
           }}}}
        """),
        ("bar1",
        """
        {"name":"bar1","position":{"x":320,"y":0,"width":320,"height":180},
         "visual":{"visualType":"barChart"},
         "fieldRoles":{"category":["Region"],"value":["Revenue"]}}
        """));
    var svc = BuildScoringService();

    var result = await svc.ScoreAsync(tempDir);

    Assert.NotNull(result.VisualMetadata);
    var denebItem = result.VisualMetadata!.Visuals.Single(v => v.VisualId == "deneb1");
    Assert.NotNull(denebItem.CustomVisualEvidence);
    Assert.Equal("deneb", denebItem.CustomVisualEvidence!.Kind);
    Assert.Equal("bar", denebItem.CustomVisualEvidence.DenebMarkType);
    Assert.Null(denebItem.ChartIntent);

    var barItem = result.VisualMetadata.Visuals.Single(v => v.VisualId == "bar1");
    Assert.Null(barItem.CustomVisualEvidence);
    Assert.NotNull(barItem.ChartIntent);
}
```

Add `using PowerBIModelingService.Services.Pbir.CustomVisualEvidence;` to this test file's imports if not already present from Task 1.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd service-dotnet && dotnet test tests/Tests.csproj --filter "FullyQualifiedName~ScoreReport_WithDenebVisual" --nologo`
Expected: FAIL — `CustomVisualEvidence` member does not exist on `VisualMetadataItem` yet.

- [ ] **Step 3: Add the field to `VisualData`**

In `service-dotnet/Services/Pbir/PbirScoringFoundationModels.cs`, add to the `VisualData` record (after the `Filter` property, line 34):

```csharp
    public CustomVisualEvidence.CustomVisualEvidence? CustomVisualEvidence { get; init; }
```

Add `using PowerBIModelingService.Services.Pbir.CustomVisualEvidence;` — note this creates a namespace/property name collision (`CustomVisualEvidence` is both the namespace and the type). Use the fully-qualified type name in the property declaration as shown above rather than a bare `CustomVisualEvidence?` to avoid ambiguity, and do not add the `using` for this file — reference `CustomVisualEvidence.CustomVisualEvidence` and `CustomVisualEvidence.CustomVisualEvidenceExtractor` fully qualified at each use site in this file instead.

- [ ] **Step 4: Call the extractor in `ReportModelLoader.CreateVisualData`**

In `service-dotnet/Services/Pbir/ReportModelLoader.cs`, add `using PowerBIModelingService.Services.Pbir.CustomVisualEvidence;` at the top. In `CreateVisualData` (line 294-320), add one more line to the returned `VisualData`:

```csharp
            CustomVisualEvidence = CustomVisualEvidenceExtractor.Extract(visual!, visualType),
```

`visual` here is the `JsonObject? visual = visualJson["visual"] as JsonObject;` already computed at line 179 in the caller — thread it through as a new parameter of `CreateVisualData` (it currently receives `visualJson` — the parent — but not `visual` the nested object directly; add `JsonObject visual` as a new parameter and pass `visual` from both call sites at lines 137 and 183).

- [ ] **Step 5: Skip chart-intent classification for non-native visual types**

In `service-dotnet/Services/Pbir/PbirScoringService.cs`, in `InferChartIntent` (line 3069-3074):

```csharp
    private static ChartIntentSummary? InferChartIntent(VisualData visual, PageData page)
    {
        if (visual.IsHidden || visual.IsNavigationElement || visual.IsDecorative || visual.IsSlicer
            || visual.CustomVisualEvidence is not null)
        {
            return null;
        }
```

- [ ] **Step 6: Add the field to the `VisualMetadataItem` C# DTO**

In `service-dotnet/Services/Pbir/Models/VisualMetadataSummary.cs`, add after the `ChartIntent` property (line 189):

```csharp
    /// <summary>Gets or sets evidence extracted for a non-native (custom/AppSource) visual type, when applicable.</summary>
    public CustomVisualEvidence.CustomVisualEvidence? CustomVisualEvidence { get; init; }
```

- [ ] **Step 7: Map the field in `BuildVisualMetadataItem`**

In `service-dotnet/Services/Pbir/PbirScoringService.cs`, in `BuildVisualMetadataItem` (line 2866-2900), add after `ChartIntent = chartIntent,` (line 2899):

```csharp
        CustomVisualEvidence = visual.CustomVisualEvidence,
```

- [ ] **Step 8: Run the new test and the full backend suite**

Run: `cd service-dotnet && dotnet test tests/Tests.csproj --nologo`
Expected: PASS, all tests (previous count plus the new ones from Tasks 1, 3, and this task).

- [ ] **Step 9: Commit**

```bash
git add service-dotnet/Services/Pbir/PbirScoringFoundationModels.cs service-dotnet/Services/Pbir/ReportModelLoader.cs service-dotnet/Services/Pbir/PbirScoringService.cs service-dotnet/Services/Pbir/Models/VisualMetadataSummary.cs service-dotnet/tests/Services/PbirScoringServiceTests.cs
git commit -m "feat(pbir): wire custom visual evidence into the scoring pipeline"
```

---

## Task 5: Extend the TypeScript contract

**Files:**
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts:49-92, 377-407`

No test for this task — it's a pure type addition, exercised by Tasks 6 and 7's tests.

- [ ] **Step 1: Add `'customVisual'` to the evidence-reference kind union**

In `vscode-extension/src/analyzer/contracts/scorePanel.ts`, in `NormalizedFindingEvidenceReference` (line 49-71), add `'customVisual'` to the `kind` union (line 50-64), alphabetically placed with the rest: after `'consistency'`, before `'designToken'`... actually match this file's existing ordering exactly as-is and append `'customVisual'` in the same relative style already used (the existing list is not strictly alphabetical — just add it as one more line in the union, e.g. after `'metadata'`).

- [ ] **Step 2: Add the `CustomVisualEvidence` TS type**

Add near `VisualMetadataItem` (before line 377):

```typescript
export interface DenebEncodingEvidence {
  channel: string;
  fieldOrMeasure: string;
}

export type CustomVisualEvidenceKind = 'deneb' | 'htmlContent' | 'genericCustom';

export interface CustomVisualEvidence {
  kind: CustomVisualEvidenceKind;
  visualType: string;
  denebMarkType?: string;
  denebEncodings?: DenebEncodingEvidence[];
  denebHasAxisTitles?: boolean;
  denebHasLegend?: boolean;
  denebHasTooltip?: boolean;
  denebHasTitle?: boolean;
  denebIsRawVegaProvider?: boolean;
  denebSpecUnparseable?: boolean;
  htmlShowRawHtml?: boolean;
  htmlOverrideInlineStyling?: boolean;
  htmlEnableDiagnostics?: boolean;
  htmlFormat?: string;
  htmlStaticTemplateHasScriptTag?: boolean;
  htmlStaticTemplateHasExternalResource?: boolean;
  htmlContentIsDynamicallyBound?: boolean;
}
```

Note: the C# DTO uses PascalCase enum member names (`Deneb`, `HtmlContent`, `GenericCustom`) and PascalCase booleans (`DenebIsRawVegaProvider`); the RPC layer's JSON serialization already camelCases every other field in this file (confirmed: `ChartIntent` → `chartIntent`), so no special handling is needed here — just make sure the TS field names match the camelCased form of the C# property names exactly (`denebIsRawVegaProvider`, not `isDenebRawVegaProvider` or similar — verify this against the actual JSON payload in Task 4's test, or a manual scoring run, before moving on, since a silent name mismatch here means the field always deserializes as `undefined` with no compile error).

- [ ] **Step 3: Add the field to `VisualMetadataItem`**

After `chartIntent?: ChartIntentSummary;` (line 406):

```typescript
  customVisualEvidence?: CustomVisualEvidence;
```

- [ ] **Step 4: Compile check**

Run: `cd vscode-extension && npx tsc --noEmit -p .`
Expected: no errors (pure additive types, nothing consumes them yet).

- [ ] **Step 5: Commit**

```bash
git add vscode-extension/src/analyzer/contracts/scorePanel.ts
git commit -m "feat(contracts): add CustomVisualEvidence to the score panel contract"
```

---

## Task 6: Synthesize findings from custom visual evidence

**Files:**
- Modify: `vscode-extension/src/analyzer/score/normalizedFindings.ts:376-390 (pushPageFindings), 478-523 (buildNormalizedFindings)`
- Test: `vscode-extension/src/test/normalizedFindings.test.ts`

- [ ] **Step 1: Write the failing tests**

Add to `vscode-extension/src/test/normalizedFindings.test.ts`:

```typescript
import type { CustomVisualEvidence, PageVisualMetadataSummary } from '../analyzer/contracts/scorePanel';
import { buildNormalizedFindings } from '../analyzer/score/normalizedFindings';

describe('buildCustomVisualFindings (via buildNormalizedFindings)', () => {
  function visualMetadata(evidence: CustomVisualEvidence): PageVisualMetadataSummary {
    return {
      pageName: 'Overview',
      semanticColorMap: [],
      visualCount: 1,
      visibleTitleVisualCount: 0,
      textVisualCount: 0,
      slicerCount: 0,
      legendVisualCount: 0,
      axisLabelVisualCount: 0,
      dataLabelVisualCount: 0,
      formattedVisualCount: 0,
      visuals: [
        {
          visualId: 'v1',
          visualType: evidence.visualType,
          x: 0,
          y: 0,
          width: 100,
          height: 100,
          isHidden: false,
          isNavigationElement: false,
          isDecorative: false,
          isSlicer: false,
          hasVisibleTitleIntent: false,
          categoryHints: [],
          valueHints: [],
          seriesHints: [],
          measureHints: [],
          semanticColors: [],
          customVisualEvidence: evidence,
        },
      ],
    };
  }

  it('emits a finding for a Deneb visual missing a tooltip encoding', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'deneb',
        visualType: 'deneb7E15AEF80B9E4D4F8E12924291ECE89A',
        denebMarkType: 'line',
        denebHasTooltip: false,
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.detectionType).toBe('deterministic');
    expect(finding!.summary.toLowerCase()).toContain('tooltip');
  });

  it('emits a finding for an HTML Content visual with a scripted static template', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'htmlContent',
        visualType: 'htmlContent443BE3AD55E043BF878BED274D3A6865',
        htmlStaticTemplateHasScriptTag: true,
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.summary.toLowerCase()).toContain('script');
  });

  it('emits a generic not-analyzed finding for an unrecognized custom visual', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: visualMetadata({
        kind: 'genericCustom',
        visualType: 'PBI_CV_1234567890ABCDEF',
      }),
    } as never);

    const finding = findings.find((f) => f.evidence.some((e) => e.kind === 'customVisual'));
    expect(finding).toBeDefined();
    expect(finding!.summary.toLowerCase()).toContain('not analyzed');
  });

  it('does not emit a custom-visual finding when no visual carries customVisualEvidence', () => {
    const findings = buildNormalizedFindings({
      scoredPageName: 'Overview',
      visualMetadata: {
        pageName: 'Overview',
        semanticColorMap: [],
        visualCount: 0,
        visibleTitleVisualCount: 0,
        textVisualCount: 0,
        slicerCount: 0,
        legendVisualCount: 0,
        axisLabelVisualCount: 0,
        dataLabelVisualCount: 0,
        formattedVisualCount: 0,
        visuals: [],
      },
    } as never);

    expect(findings.some((f) => f.evidence.some((e) => e.kind === 'customVisual'))).toBe(false);
  });
});
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd vscode-extension && npx jest normalizedFindings.test.ts`
Expected: FAIL — no findings emitted (customVisualEvidence isn't consumed anywhere yet), and `evidence.kind === 'customVisual'` never matches.

- [ ] **Step 3: Implement `buildCustomVisualFindings` and wire it in**

In `vscode-extension/src/analyzer/score/normalizedFindings.ts`, add a new function (near the other `build*Finding` functions, e.g. after `buildBenchmarkFinding`):

```typescript
function buildCustomVisualFinding(pageName: string, visual: VisualMetadataItem): NormalizedFinding | null {
  const evidence = visual.customVisualEvidence;
  if (!evidence) {
    return null;
  }

  const { title, summary, recommendation } = describeCustomVisualEvidence(evidence);

  return {
    id: `custom-visual-${sanitizeIdPart(pageName)}-${sanitizeIdPart(visual.visualId)}`,
    title,
    summary,
    severity: 'medium',
    confidence: 90,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: [pageName],
    impactArea: 'visualQuality',
    frameworkImpact: [],
    recommendation,
    sourceKind: 'customVisual',
    sourceSection: 'issues',
    evidence: [
      {
        kind: 'customVisual',
        label: evidence.kind === 'deneb' ? 'Deneb visual' : evidence.kind === 'htmlContent' ? 'HTML Content visual' : 'Custom visual',
        pageName,
        visualId: visual.visualId,
        detail: evidence.visualType,
      },
    ],
  };
}

function describeCustomVisualEvidence(evidence: CustomVisualEvidence): {
  title: string;
  summary: string;
  recommendation: string;
} {
  if (evidence.kind === 'deneb') {
    if (evidence.denebSpecUnparseable) {
      return {
        title: 'Deneb visual has an unreadable specification',
        summary: 'The embedded Vega/Vega-Lite specification could not be parsed, so this visual is not semantically analyzed.',
        recommendation: 'Verify this visual renders correctly and review it manually.',
      };
    }

    if (evidence.denebIsRawVegaProvider) {
      return {
        title: 'Deneb visual uses raw Vega — not semantically analyzed',
        summary: 'This Deneb visual is authored in the raw Vega grammar rather than Vega-Lite, which this analyzer does not structurally parse yet.',
        recommendation: 'Review this visual manually.',
      };
    }

    const gaps: string[] = [];
    if (evidence.denebHasTooltip === false) gaps.push('no tooltip encoding');
    if (evidence.denebHasLegend === false) gaps.push('no legend');
    if (evidence.denebHasAxisTitles === false) gaps.push('no axis titles');
    if (evidence.denebHasTitle === false) gaps.push('no chart title');

    return {
      title: 'Deneb visual is not semantically analyzed',
      summary: gaps.length > 0
        ? `Deterministic scoring cannot see inside this Deneb visual's chart shape. Structural gaps found: ${gaps.join(', ')}.`
        : "Deterministic scoring cannot see inside this Deneb visual's chart shape, though its specification includes a tooltip, legend, axis titles, and a title.",
      recommendation: 'Attach a screenshot in Rendered Review to confirm the rendered outcome visually.',
    };
  }

  if (evidence.kind === 'htmlContent') {
    const flags: string[] = [];
    if (evidence.htmlStaticTemplateHasScriptTag) flags.push('an inline <script> block');
    if (evidence.htmlStaticTemplateHasExternalResource) flags.push('a reference to an external resource');
    if (evidence.htmlShowRawHtml) flags.push('raw HTML rendering enabled');

    return {
      title: 'HTML Content visual is not semantically analyzed',
      summary: evidence.htmlContentIsDynamicallyBound
        ? `This HTML Content visual's content is bound to a measure or field, so its rendered output cannot be statically inspected.${flags.length > 0 ? ` Its static template contains ${flags.join(' and ')}.` : ''}`
        : `This HTML Content visual's static template contains ${flags.length > 0 ? flags.join(' and ') : 'no flagged content'}.`,
      recommendation: 'Verify this visual\'s behavior manually and attach a screenshot in Rendered Review.',
    };
  }

  return {
    title: `Custom visual type not analyzed: ${evidence.visualType}`,
    summary: 'This visual type is not on the native list and has no dedicated evidence extractor, so it is not analyzed.',
    recommendation: 'Attach a screenshot in Rendered Review to confirm the rendered outcome visually.',
  };
}

function pushCustomVisualFindings(
  findings: NormalizedFinding[],
  pageName: string | undefined,
  visualMetadata: PageVisualMetadataSummary | undefined,
): void {
  if (!pageName || !visualMetadata) {
    return;
  }

  for (const visual of visualMetadata.visuals) {
    const finding = buildCustomVisualFinding(pageName, visual);
    if (finding) {
      findings.push(finding);
    }
  }
}
```

Add `CustomVisualEvidence`, `PageVisualMetadataSummary`, and `VisualMetadataItem` to the existing `import type { ... } from '../contracts/scorePanel';` block at the top of the file.

Now wire it into both scoring-mode branches. In `pushPageFindings` (line 376), widen the `Pick<PageScore, ...>` type to also include `'visualMetadata'`, and add a call at the end of the function body:

```typescript
  pushCustomVisualFindings(findings, page.pageName, page.visualMetadata);
```

In `buildNormalizedFindings`'s single-page `else` branch (around line 505-517, before the closing brace of that `else` block), add:

```typescript
    pushCustomVisualFindings(findings, result.scoredPageName, result.visualMetadata);
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd vscode-extension && npx jest normalizedFindings.test.ts`
Expected: PASS, all 4 new tests plus all pre-existing tests in this file.

- [ ] **Step 5: Commit**

```bash
git add vscode-extension/src/analyzer/score/normalizedFindings.ts vscode-extension/src/test/normalizedFindings.test.ts
git commit -m "feat(findings): synthesize advisory findings from custom visual evidence"
```

---

## Task 7: Route custom-visual findings into the Rendered Review checklist by evidence kind

**Files:**
- Modify: `vscode-extension/src/analyzer/renderedReview/types.ts` (add category value)
- Modify: `vscode-extension/src/analyzer/renderedReview/reviewModel.ts` (routing + guidance)
- Test: `vscode-extension/src/test/renderedReview.test.ts`

- [ ] **Step 1: Write the failing test**

Add to `vscode-extension/src/test/renderedReview.test.ts` (match this file's existing import/fixture style exactly):

```typescript
it('classifies a customVisual-evidence finding as unsupportedVisualType, independent of wording', () => {
  const finding: NormalizedFinding = {
    id: 'custom-visual-overview-v1',
    title: 'Some unrelated title that mentions nothing about categories below',
    summary: 'Some unrelated summary text.',
    severity: 'medium',
    confidence: 90,
    scope: 'page',
    detectionType: 'deterministic',
    affectedPages: ['Overview'],
    impactArea: 'visualQuality',
    frameworkImpact: [],
    recommendation: 'Attach a screenshot.',
    sourceKind: 'customVisual',
    sourceSection: 'issues',
    evidence: [{ kind: 'customVisual', label: 'Deneb visual', pageName: 'Overview', visualId: 'v1' }],
  };

  const result = classifyRenderedReviewFinding(finding);

  expect(result.classification).toBe('renderedReviewRecommended');
  expect(result.category).toBe('unsupportedVisualType');
});
```

Check this test file's existing imports for `classifyRenderedReviewFinding` and `NormalizedFinding` and reuse them rather than re-importing.

- [ ] **Step 2: Run test to verify it fails**

Run: `cd vscode-extension && npx jest renderedReview.test.ts`
Expected: FAIL — `result.category` is `undefined` (falls through to `'deterministic'` classification since no keyword in the title/summary matches any existing `CATEGORY_RULES` entry).

- [ ] **Step 3: Add the new category and routing**

In `vscode-extension/src/analyzer/renderedReview/types.ts`, add `'unsupportedVisualType'` to the `RenderedReviewCategory` union (alongside `'whitespaceBalance'`, etc.).

In `vscode-extension/src/analyzer/renderedReview/reviewModel.ts`, add a new entry to `CATEGORY_RULES`:

```typescript
  {
    category: 'unsupportedVisualType',
    label: 'Unsupported visual type',
    terms: [], // routed by evidence kind below, not by keyword matching
    guidance: {
      why: 'This visual type is not semantically analyzed by deterministic scoring — it may be a Deneb chart, an HTML Content visual, or another custom/AppSource visual.',
      lookFor: 'Confirm the visual renders as intended and communicates what it should.',
      expectedOutcome: 'The visual should be legible, correctly styled, and free of unexpected behavior.',
    },
  },
```

Modify `classifyRenderedReviewFinding` to check evidence kind before falling back to keyword matching:

```typescript
export function classifyRenderedReviewFinding(finding: NormalizedFinding): {
  classification: RenderedReviewClassification;
  category?: RenderedReviewCategory;
} {
  if (finding.evidence.some((evidence) => evidence.kind === 'semanticModel') || /semantic/i.test(finding.sourceKind)) {
    return { classification: 'semantic' };
  }

  if (finding.evidence.some((evidence) => evidence.kind === 'customVisual')) {
    return { classification: 'renderedReviewRecommended', category: 'unsupportedVisualType' };
  }

  const rule = findRule(finding);
  return rule
    ? { classification: 'renderedReviewRecommended', category: rule.category }
    : { classification: 'deterministic' };
}
```

Because `terms: []` for this new category, `findRule` will never match it via text — that's intentional, it's reached only through the explicit evidence-kind check above, not the keyword fallback path.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd vscode-extension && npx jest renderedReview.test.ts`
Expected: PASS, all tests including the new one.

- [ ] **Step 5: Commit**

```bash
git add vscode-extension/src/analyzer/renderedReview/types.ts vscode-extension/src/analyzer/renderedReview/reviewModel.ts vscode-extension/src/test/renderedReview.test.ts
git commit -m "feat(rendered-review): route custom-visual findings by evidence kind"
```

---

## Task 8: Full regression pass

**Files:** none (verification only)

- [ ] **Step 1: Run the full backend suite**

Run: `cd service-dotnet && dotnet test tests/Tests.csproj --nologo`
Expected: PASS, 0 failed. Compare the total count against the pre-Task-1 baseline (1013 passed as of the last verified run this session) — it should be higher by the number of new tests added across Tasks 1, 3, 4, 6, 7.

- [ ] **Step 2: Run the full frontend suites**

Run: `cd vscode-extension && npx tsc --noEmit -p . && npm test -- --silent`
Expected: TypeScript compiles clean; both the extension Jest config and `jest.webview.config.cjs` pass with 0 failures.

- [ ] **Step 3: Confirm no behavior change for native visuals**

Run: `cd service-dotnet && dotnet test tests/Tests.csproj --filter "FullyQualifiedName~ChartIntent|FullyQualifiedName~ClassifyAnalyticalTask" --nologo`
Expected: PASS — all pre-existing native chart-intent classification tests (including the `gauge`/native-type fix from the earlier merged follow-up) are unaffected by this feature.

- [ ] **Step 4: Report completion**

Summarize to the user: total tests added, confirm the two v1 scope limits called out at the top of this plan (raw-Vega Deneb specs get the lighter fallback record; only the two AppSource visuals with GitHub-verified GUIDs are recognized by prefix, everything else custom falls into the generic bucket), and confirm zero score-weight or governance behavior changed for any existing visual type.
