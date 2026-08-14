# Phase 42 Report Mutation Architecture

Phase 42 adds a backend-only import, planning, and execution foundation for deterministic PBIR mutation.

## Operation matrix

| Operation | Phase 42 status |
| --- | --- |
| Add/remove/rename/move page | Planned and executed in shared IR |
| Add/remove/replace/move/resize visual | Planned and executed in shared IR |
| Update binding | Planned and executed for direct bindings |
| Update formatting, theme, filter, navigation, slicer | Rejected until the shared IR carries these fields losslessly |

## Data flow

Local PBIR directory → narrow schema reader → validated shared IR snapshot → deterministic planner → immutable IR executor → existing serializer/materialization/analyzer path.

The reader accepts the pinned serializer schemas and the closed Phase 39–41 visual catalog. It hashes the source JSON files and preserves the PBIR page/visual folder names in the import snapshot. It does not write to the source directory.

## Identities and hashes

The import snapshot preserves source folder identities. Page and visual mutation targets use logical IDs projected from those identities. The current serializer still derives output folder names from IR ID and logical identity, so untouched-artifact hash preservation and exact folder identity preservation are not yet proven through serialized mutation output. This is an explicit blocker for claiming the full Phase 42 objective.

## Phase 43 recommendation

Phase 43 is the approved follow-up and adds the bounded hybrid lossless-authoring envelope. It preserves imported owned documents and identity provenance while keeping typed mutation authority and schema validation explicit. Its design and implementation records are in docs/superpowers/specs/2026-08-14-phase43-lossless-authoring-ir-design.md and docs/superpowers/plans/2026-08-14-phase43-lossless-authoring-ir.md.

Do not expose this foundation through RPC yet. Before a minimal internal RPC surface, extend the shared IR and serializer request with lossless authoring fields and explicit identity overrides, then add an end-to-end mutation provider that performs serialization, materialization, schema validation, analyzer round-trip, lineage, and hash evidence in one operation. Reassess API stability only after those contracts have targeted compatibility tests.
