# Phase 38 Rich PBIR Authoring Current State

## Executive Summary

Phase 38 adds presentation-quality authoring to the backend-only local PBIR generation provider without widening the visual catalog or public API surface. The additive v3 request preserves the Phase 36 and Phase 37 contracts and reuses the shared IR, Phase 29 serializer, Phase 31 materialization path, deterministic hashing, validation, lineage, and analyzer round-trip.

The provider now supports typed formatting for Card and Table visuals, deterministic report themes, equality filters at report/page/visual scope, basic interaction metadata, report metadata, and deterministic layout margins and spacing. Unsupported or malformed authoring is rejected before serialization.

## Formatting Matrix

| Area | Supported | Boundary |
| --- | --- | --- |
| Report formatting | Metadata, theme metadata, palette annotation, interaction setting | No arbitrary report JSON or advanced expressions |
| Page formatting | Background color, page filters | No custom page-size authoring beyond existing deterministic canvas |
| Card formatting | Title, label style, number format, alignment, box styling | Existing Card visual only |
| Table formatting | Title, subtitle, header/row styles, alternate rows, number format, column alignment, width behavior, box styling | Existing Table visual only |
| Themes | Default/custom theme identity, typography, background/accent colors, deterministic palette | Theme values are schema-safe metadata and visual formatting; no custom theme resource JSON |
| Filters | Deterministic equality filters at report, page, and visual scope | Advanced expressions, ranges, tuples, and relative filters deferred |
| Interactions | Cross-highlight, cross-filter, and disabled interaction metadata | No bookmarks, drillthrough, or advanced behavior |

## Generated Example

The representative test request creates two pages, an overview Card and Table, and a detail Table. It applies the Sales Light theme, Revenue Card number formatting and colors, table headers and alternating rows, a report filter for Sales Year 2026, and a page filter for Sales Region North. It also emits deterministic cross-highlight metadata between visuals on the overview page.

## Analyzer and Determinism Results

The representative request is schema-valid, materializes through the Phase 31 preview/apply path, and is observed by the analyzer as two pages and three visuals. The representative analyzer composite score was 92.5. The focused determinism test generates the same request twice and verifies byte-identical artifacts and identical artifact, manifest, file-set, and lineage hashes. Analyzer scores remain authoritative and are captured by the round-trip result; timing fields capture generation, materialization, and analyzer durations for the representative run.

## Performance Summary

Phase 38 records generation, materialization, and analyzer milliseconds in the existing performance result. One representative run recorded 1 ms generation, 109 ms materialization, and 120 ms analyzer time. The test suite measures the representative request but does not establish a benchmark threshold. Formatting is a bounded serializer pass over already-materialized IR; no analyzer-local cache or repository rescan was introduced. Observations should be gathered from repeated representative runs before optimization.

## Test and Build Coverage

Focused provider coverage includes v1/v2 compatibility, typed formatting, theme/filter/interaction emission, invalid colors, duplicate filters, unsupported formatting, deterministic hashes, schema validation, materialization, and analyzer regression. Backend Release validation passed 881 tests with 11 expected Windows skips (892 total). Extension Jest passed 494 tests and webview Jest passed 68 tests; TypeScript compilation, extension build, VSIX packaging, and git diff --check passed. ESLint remains the unchanged repository baseline of 43 errors.

## Known Limitations

- Only Card and Table visuals are supported.
- Filters are equality-only and accept one value per field per scope.
- Themes do not accept arbitrary PBIR theme JSON or external resources.
- Advanced conditional formatting, expressions, bookmarks, drillthrough, custom visuals, charts, semantic-model generation, DAX generation, Desktop automation, hosted execution, RPC, and VS Code commands remain out of scope.
- Visual interaction metadata is basic and deterministic; it does not model advanced interaction behavior.

## Phase 39 Recommendation

Introduce chart visual support through a generalized visual-binding model with category, series, and axis semantics while preserving backward compatibility with the existing Card and Table binding model. Do not expose a public RPC or VS Code surface until the visual-binding model is stable across all supported visual types.
