# Phase 38 Rich PBIR Authoring Design

## Goal

Extend the existing backend-only local PBIR generation provider from structural Phase 37 reports to presentation-quality reports while preserving deterministic artifacts, the existing Phase 29 serializer and Phase 31 materialization path, and the legacy v1/v2 request behavior.

## Compatibility and scope

Phase 36 `local-pbir-generation-request/v1` and Phase 37 `local-pbir-generation-request/v2` remain supported without changing their generated output. Phase 38 adds an internal additive `local-pbir-generation-request/v3` contract. No RPC, VS Code, provider registry, external execution, new visual type, semantic-model generation, or mutation authority is added.

All new authoring values are typed C# records and enums. The provider validates them before constructing the shared IR and serializer request. Unsupported or ambiguous values fail closed with stable diagnostics; raw JSON formatting blobs are not accepted.

## Architecture

The v3 request is normalized into the existing shared IR plus a typed authoring payload carried by the deployable serializer request. The serializer remains the only component that emits PBIR JSON. It emits only properties supported by the pinned schemas:

- report metadata, report filters, and `themeCollection` in `definition/report.json`
- page filters and page presentation metadata in each `page.json`
- visual filters, formatting objects, typed number formats, and supported interaction settings in each `visual.json`

The serializer uses stable ordering for pages, visuals, filters, palette entries, formatting properties, and theme metadata. Authoring payloads participate in the existing IR/input/file/artifact/manifest hashes, so equivalent requests produce byte-identical artifacts and different authoring requests cannot collide silently.

## Typed authoring model

The request adds optional report metadata, a theme, report/page/visual equality filters, interaction settings, layout options, and visual formatting. Formatting is composed from typed values for title/subtitle text, font family/size/weight/color, alignment, padding, background, border, and numeric format. Card and table formatting are constrained to their supported object names; unsupported object names are rejected rather than passed through.

Themes contain a deterministic name, typography, background/accent colors, and a sorted custom palette. Duplicate theme definitions or duplicate palette keys are rejected. A missing theme uses the deterministic default theme and does not add a custom theme object.

Equality filters identify a model entity, property, kind, and scalar value. They are emitted as schema-valid categorical filters with an explicit source expression and equality `In` condition. Scope is report, page, or visual. Duplicate filter identities at the same scope and conflicting values for the same field at a narrower scope are rejected.

Interactions expose only the supported deterministic enable/disable and cross-filter default settings. The current pinned visual-container schema has no general interaction matrix, so matrix-style requests are rejected with an unsupported diagnostic.

Layout remains automatic and deterministic. v3 adds page margins, inter-visual spacing, alignment, grouping metadata, and visual padding as inputs to the existing grid placement; it does not permit arbitrary overlapping or out-of-canvas placements.

## Validation and failure behavior

Validation occurs in the provider before serialization and again through the existing serializer safety gate, input validator, pinned schema validator, materialization validator, and analyzer round-trip. Invalid colors, sizes, padding, alignment, formats, filter values, duplicate theme objects, conflicting filters, unsupported formatting objects, and unsupported interaction matrices return rejected results with no partial artifact.

The analyzer round-trip continues to assert page and visual counts and now also asserts that generated report/page/visual metadata remains parseable. Performance timing continues to measure generation, materialization, and analyzer phases; timing is observational and excluded from content hashes.

## Testing

Tests cover v1/v2 compatibility, v3 generation, all supported formatting scopes, default/custom themes, equality filters at all scopes, interaction enable/disable, automatic layout margins and padding, invalid and unsupported values, duplicate themes, conflicting filters, schema conformance, analyzer round-trip, deterministic bytes and hashes, and measured performance fields. Existing Phase 29–37 tests remain regression gates.

## Known limitations

Charts, generalized category/series/axis bindings, advanced filter expressions, bookmarks, drillthrough, custom visuals, semantic-model generation, arbitrary interaction matrices, Desktop execution, RPC, and VS Code integration remain deferred. Phase 39 should introduce the generalized visual-binding model required for chart support before any public generation surface is widened.
