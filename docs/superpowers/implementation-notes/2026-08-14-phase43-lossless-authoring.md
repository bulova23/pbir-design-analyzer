# Phase 43 Lossless Authoring Implementation Notes

## Reconciled implementation slice

Task 1 is approved and the semantic-losslessness contract is frozen in
`docs/current-state/phase43-lossless-authoring-state.md`. The existing code below
is foundation evidence being completed task-by-task; it is not treated as a
complete Phase 43 implementation until the acceptance gate passes.

- Added `pbir-authoring-envelope/v1` with bounded owner, source-document, source-hash, property-order, classification, and identity-provenance records.
- Extended imported shared IR with the optional authoring envelope without changing generation-only defaults.
- Added schema-lock admission for owned report, pages metadata, page, and visual definition documents. Invalid JSON and unsupported schema URLs produce fail-closed diagnostics.
- Preserved original source text for unchanged owned documents and reused imported page/visual identities for serializer paths.
- Added one focused authoring merge service. It returns untouched source content unchanged and overlays typed visual layout changes only on the position object.
- Added fidelity classification for byte-identical, semantically identical, expected normalized, missing, and unexpected differences.

## Preservation matrix

| Construct | Imported | Preserved | Typed mutation | Regenerated | Unsupported boundary |
| --- | --- | --- | --- | --- | --- |
| Report/page/visual identity and ownership | yes | yes | identity provenance is explicit; mutation override remains narrow | new generated objects | duplicate/unknown ownership fails closed |
| Visual layout | yes | yes | resize/move typed layout overlay | new visual fallback | invalid position blocks |
| Formatting | source document | yes | no generic opaque mutation | generation path only for new objects | unsupported schema blocks |
| Theme | source document where owned | yes | no generic opaque mutation | generation path for new reports | unsupported schema blocks |
| Filters | source document where owned | yes | unmodeled operations remain rejected | generation path for new reports | unsupported schema blocks |
| Navigation | source document/pages metadata | yes | unmodeled operations remain rejected | generation path for new reports | unsupported schema blocks |
| Slicer metadata | visual source document | yes | unmodeled operations remain rejected | generation path for new slicers | unsupported visual/schema blocks |
| Unknown pinned-schema properties | bounded owner only | yes | never directly mutable | never silently regenerated | non-admitted content fails closed |

## Validation status

The final focused Phase 43/42/reader/fidelity/projection slice passed 21/21. The full backend Release suite passed 951 tests with 11 expected Windows skips (962 total). The bounded timing observation was reader/envelope `3 ms`, semantic projection `0 ms`, planning `6 ms`, execution `0 ms`, merge `1 ms`, deterministic serialization `1 ms`, analyzer `84 ms` on the representative local fixture. These are observations, not thresholds.

Analyzer/scoring remains a separate boundary: existing analyzer tests and the imported projection pipeline remain callable, while opaque content is not forced into scoring. No Phase 43 optimization project or public RPC was added.

## Phase 44 follow-up

Phase 44 closes the reader semantic projection gap recorded below for the supported visual families by resolving imported query-state roles through the shared descriptor catalog. It adds shared-IR semantic equivalence, unsupported-role diagnostics, and imported analyzer-before/after evidence while retaining all Phase 43 envelope and mutation boundaries.

## Known limitations

Formatting, themes, filters, navigation, slicer metadata, bindings, and other unsupported typed mutations are preserved but cannot be changed through the opaque envelope. Arbitrary JSON patching remains prohibited. Byte identity is available for preserved source documents, while changed documents are canonicalized and reported as expected differences. Phase 44 remains a separate milestone and its pre-existing files were not modified by this execution.
