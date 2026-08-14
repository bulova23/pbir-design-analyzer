# Phase 43 Lossless Authoring Implementation Notes

## Implemented slice

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

Focused Phase 43/42 backend tests passed 44/44. The full backend suite passed 932 tests with 11 expected Windows skips. Core Release build passed with 0 warnings and 0 errors. Extension TypeScript compilation passed; extension Jest passed 494/494; webview Jest passed 68/68; the production extension build passed; and `git diff --check` passed.

The complete imported analyzer-before/after comparison and stage-level import/planning/execution/serialization/analyzer benchmark remain open because the narrow reader semantic binding projection is not yet sufficient to drive the existing strict serializer/analyzer input contract for every generated visual family. Phase 44 RPC is not recommended until that gap is closed.

## Phase 44 follow-up

Phase 44 closes the reader semantic projection gap recorded below for the supported visual families by resolving imported query-state roles through the shared descriptor catalog. It adds shared-IR semantic equivalence, unsupported-role diagnostics, and imported analyzer-before/after evidence while retaining all Phase 43 envelope and mutation boundaries.

## Known limitations

Formatting, themes, filters, navigation, slicer metadata, and other unsupported typed mutations are preserved but cannot be changed through the opaque envelope. Arbitrary JSON patching remains prohibited. Byte identity is available for preserved source documents, while changed documents are canonicalized and reported as expected differences.
