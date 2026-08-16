# Phase 44 Semantic Binding Projection and Full Round-Trip Fidelity

## Executive summary

Phase 44 extends the Phase 43 PBIR reader so imported query-state bindings are projected through the existing Phase 40/41 descriptor catalogs into `PbirIntermediateRepresentationBinding`. No import-only semantic model was added. The reader canonicalizes supported aliases, including Pie Category to the descriptor’s Legend role and serializer Tooltips to the shared Tooltip role, while slicer Category uses the existing Phase 41 descriptor path.

Unsupported query-state roles remain in the schema-supported Phase 43 authoring envelope, receive a `PreservedButUntyped` diagnostic, and are excluded from typed mutation. Invalid descriptor/kind combinations receive `Invalid` diagnostics and block imported IR readiness.

## Semantic coverage matrix

| Visual family | Imported roles | Projected shared roles | Remaining unsupported semantics |
| --- | --- | --- | --- |
| Card | Fields, Tooltips | Value, Tooltip | Unknown/future roles and opaque formatting |
| Table | Values, Tooltips | Value, Tooltip | Unknown/future roles and opaque formatting |
| Clustered Column Chart | Category, Y, Tooltips | Category, Value, Tooltip | Future chart roles not in the descriptor |
| Line Chart | Category, Y, Series, Tooltips | Category, Value, Series, Tooltip | Future chart roles not in the descriptor |
| Bar Chart | Category, Y, Tooltips | Category, Value, Tooltip | Future chart roles not in the descriptor |
| Pie Chart | Category, Y, Tooltips | Legend, Value, Tooltip | Future chart roles not in the descriptor |
| Slicer | Category | Category | Sync metadata and unsupported slicer authoring |

## Equivalence, fidelity, and analyzer evidence

Semantic equivalence compares visual identity/family, page ownership, canonical descriptor role, measure/dimension kind, normalized token/entity/property references, and projection order. JSON property order, whitespace, and serializer normalization are not semantic changes. The focused imported pipeline test performs reader projection, shared-IR layout mutation, Phase 43 envelope merge, and analyzer-before/after scoring. The layout mutation is semantically equivalent because bindings are unchanged, and the analyzer composite score remains unchanged. Analyzer output remains evidence only.

The existing Phase 43 fidelity service continues to classify byte identity and canonical JSON equality separately, and now exposes additive path sets for authoring-identical, semantic-equivalent, intentionally-changed, and unsupported evidence. Unsupported envelope content is never collapsed into typed semantic success.

## Stage timing observation

One seven-visual representative run on the development host measured: reader/import 4 ms; semantic projection 1 ms; authoring merge 1 ms; mutation planning 8 ms; mutation execution 0 ms; serialization 2 ms; analyzer 116 ms. These are observations, not thresholds or optimization claims. Import results carry reader and projection timings; the timing test measures the remaining existing service boundaries without adding a public performance contract.

## Remaining gaps

Bookmarks, drillthrough, shared slicers, semantic-model/DAX generation, unsupported future query roles, and typed mutation for opaque authoring domains remain unsupported. Phase 44 remains backend-only and adds no RPC, VS Code integration, new visual families, hosted execution, or provider-security changes.
