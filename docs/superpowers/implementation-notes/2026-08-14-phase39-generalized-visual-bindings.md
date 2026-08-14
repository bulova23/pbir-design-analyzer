# Phase 39 Generalized Visual Bindings Implementation Notes

## Result

Implemented backend-only v4 role-bearing bindings over the existing Phase 29 serializer and Phase 31 materialization pipeline. The shared IR visual now has an additive optional binding list containing binding id, role, kind, source token, model identity, and projection order.

The provider accepts Card, Table, and Clustered Column Chart. The chart serializer mapping is Category → Category and Value → Y. Existing Card and Table v1–v3 mappings remain unchanged.

## Analyzer and Determinism

The representative two-page, three-visual catalog round-tripped through materialization and the analyzer with composite score 92.5. The focused v4 determinism test produced byte-identical files and these repeated hashes:

- artifact: `6d7e998d2dd45f226774205eac4f547db4e955034a93370762a854840a83d21d`
- manifest: `e51bbd8078b9378e32954f17f6492b527f98f75791909065086e0f9bd2907c93`
- file set: `e44ecaa322b63178d38e0d4ba9ce9443b3abd3cf2962427a0fc217da8e5cca3a`
- lineage: `cfc522bbe46e74c0846095e67824746522559622c17a58d80fcec660e38a08c9`

## Performance Observation

One representative run recorded 2 ms generation, 73 ms materialization, and 23 ms analyzer execution. These are observations from the focused test, not benchmark thresholds. No repository rescan, analyzer-local memoization, or new execution boundary was introduced.

## Test Results

The focused provider suite passed 30 tests, including Phase 36–38 regression coverage, v4 contract coverage, chart role mapping, invalid role/kind rejection, catalog round-trip, and deterministic hashes. Full backend, .NET build, extension build, TypeScript compilation, and diff checks are recorded in the Phase 39 session note.

## Known Limitations

Series, Axis, Legend, and Tooltip roles are represented by the typed vocabulary but are not accepted by the Phase 39 chart. Only Clustered Column Chart is added; line, bar, pie, combo, drillthrough, bookmarks, custom visuals, semantic-model generation, DAX generation, Desktop automation, hosted execution, RPC, and VS Code commands remain out of scope.
