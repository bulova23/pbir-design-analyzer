# Phase 42 Explicit Slicer Interaction Implementation Notes

## Decision and bounded correction

Decision: `PHASE 42 APPROVED WITH BOUNDED SCHEMA CORRECTIONS`.

The proposed design used conceptual `CrossFilter`, `CrossHighlight`, and
`Disabled` names and allowed a page-scope target. The pinned repository schema
does not use those values and requires a visual-name target. The implementation
therefore uses `Default`, `DataFilter`, `HighlightFilter`, and `NoFilter`, and
supports explicit same-page visual targets only.

## Implementation

- Added additive `local-pbir-generation-request/v7` and typed interaction rules
  to page compositions.
- Added deterministic validation for source slicer identity, same-page target
  identity, duplicate IDs/targets, self-reference, empty targets, and disabled
  mode.
- Added typed IR interaction records and serializer projection to the pinned
  page-level `visualInteractions` array.
- Preserved the v1–v6 global interaction fallback and left scoring unchanged.

## Pinned schema evidence

The checked-in lock is
`service-dotnet/tests/Fixtures/PbirSchemas/fabric/item/report/definition/page/1.0.0/schema.json`.
Its page property `visualInteractions` is an array of `VisualInteraction`
objects. Each object requires `source`, `target`, and `type`; the type
definition accepts `Default`, `DataFilter`, `HighlightFilter`, or `NoFilter`.
The entry is page-scoped by its location, but the target itself is a visual
name. No page-scope target sentinel or cross-page form is schema-supported.

## Representative evidence

The focused Phase 42 suite passes 9/9, including deterministic artifact
generation and analyzer/provider round-trip for a page with a slicer, line
chart, and explicit `DataFilter` interaction. Existing Phase 41 baseline
coverage passed 52/52 before implementation. The full Release backend suite
passed 929 tests with 11 expected Windows skips. Extension and webview Jest
passed 494/494 and 68/68; TypeScript/extension/webview build passed. Scoped
lint remains the pre-existing 43-error baseline. Concurrent uncommitted Phase
43 files were preserved and were not part of the Phase 42 implementation.

## Boundaries

No public RPC, VS Code, Desktop, Windows, hosted execution, synchronized
slicers, bookmarks, drillthrough, cross-page interactions, or scoring change
was added. The next named milestone is Phase 43 — Lossless Authoring IR; it
remains unstarted by this goal.
