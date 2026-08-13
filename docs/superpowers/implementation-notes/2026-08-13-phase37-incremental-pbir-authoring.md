# Phase 37 — Incremental PBIR Authoring

## Executive summary

Phase 37 expands the backend-only local PBIR generation provider additively. The provider now accepts a typed v2 request with multiple ordered pages, multiple card and table visuals, direct measure/dimension bindings, and bounded 1280x720 layout values. It still delegates serialization to Phase 29, local persistence to Phase 31, and scoring to the existing analyzer.

No RPC, VS Code command, hosted execution, Windows execution, semantic-model generation, DAX authoring, chart type, or provider-security change was added.

## Supported feature matrix

| Area | Phase 37 support |
| --- | --- |
| Pages | Multiple pages with caller-supplied stable ids, display names, and deterministic order |
| Visuals | Card and table only |
| Card bindings | One or more direct measure bindings in Fields |
| Table bindings | Direct measure and dimension bindings in Values |
| Layout | Explicit x/y/width/height, deterministic auto-placement, 1280x720 bounds, non-overlap |
| Round-trip | Phase 29 schema validation, Phase 31 materialization, existing analyzer scoring |

## Representative generated report

The fixture request phase37-sales-authoring produces:

\`\`\`text
definition.pbir
definition/version.json
definition/report.json
definition/pages/pages.json
definition/pages/<overview-page-id>/page.json
definition/pages/<overview-page-id>/visuals/<card-id>/visual.json
definition/pages/<overview-page-id>/visuals/<table-id>/visual.json
definition/pages/<detail-page-id>/page.json
definition/pages/<detail-page-id>/visuals/<table-id>/visual.json
\`\`\`

The overview page contains a scalar Revenue card and a Region/Revenue table. The detail page contains a Region/Revenue table. Page metadata, page order, visual identities, bindings, and coordinates are derived only from the request.

## Analyzer and determinism

The representative artifact is schema-valid and contains 2 pages and 3 visuals. Its materialized round-trip reports RoundTripVerified, 2 analyzer page scores, 3 generated visuals, and an analyzer composite score of 92.5. Repeated generation produced byte-identical files and equal hashes:

- artifact: c5d2143af81427ef3bf3ac66f9bcdf4f8173b6780949c3d9700ededae190371c
- manifest: e9c852175322ef67653cefa36f0e66c4b685c1f55cc67eca9b51bbefd95b7398
- file set: 7096fbaeb342f5d28ae9244be3d84121dd59a71fc4d448cb99b60d44238693a7
- lineage: ff1acd2e269d4fdadcaff52a22b5d7c705f21b91ec5d5d1a848842f6694edab3

One local representative timing observation was generation 41 ms, materialization 119 ms, and analyzer 111 ms. These are observations, not performance thresholds.

## Known limitations

Charts and category/series/axis/query semantics are deferred to Phase 38. Formatting, filters, interactions, themes, bookmarks, drillthrough, custom visuals, calculated measures, semantic-model generation, PBIP project generation, Desktop validation, hosted/remote execution, RPC, and VS Code integration remain unsupported.
