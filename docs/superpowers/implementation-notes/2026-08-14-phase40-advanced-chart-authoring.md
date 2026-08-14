# Phase 40 Advanced Chart Authoring Implementation Notes

## Delivered

- Added additive v5 request models and strongly typed axis, legend, tooltip, template, and bounded conditional-formatting records.
- Added a static six-visual descriptor catalog with role requirements and serializer mappings.
- Added v5 generation and round-trip overloads while leaving v1–v4 request records and overloads intact.
- Added deterministic template projection and schema-safe chart formatting projection.
- Extended serializer validation and role support for Line, Bar, and Pie charts.
- Added descriptor, v5 generation, determinism, materialization, analyzer, and regression coverage.

## Representative evidence

- Visuals: Card, Table, Clustered Column Chart, Line Chart, Bar Chart, Pie Chart.
- Analyzer composite score: 88.45.
- Generation: 73 ms.
- Materialization: 124 ms.
- Analyzer: 97 ms.
- Repeated generation produced equal artifact and manifest hashes and byte-identical file tuples.

## Schema boundary

The pinned visual-container schema rejects arbitrary new axis, legend, tooltip, and conditional-formatting object shapes. V5 therefore validates those typed authoring inputs and projects supported presentation effects into existing title, axis-label, legend, background, and data-color objects. Tooltip role emission is intentionally deferred until a pinned schema-backed query projection is available.

## Phase 41 recommendation

Shift from individual visual expansion to report composition: reusable report sections, page templates, navigation, slicers, and richer interaction models. Continue deferring public RPC and VS Code surfaces.

