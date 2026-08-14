# Phase 42 Report Mutation Design

## Goal

Add a backend-only, additive `local-pbir-mutation-request/v1` operation that imports a schema-supported local PBIR report, plans deterministic changes against the shared PBIR intermediate representation, executes only validated changes, and sends the resulting IR through the existing serializer, materialization, schema validation, analyzer, hashing, and lineage paths.

## Boundaries

Generation remains unchanged and continues to own report creation. Import, planning, execution, and evidence are separate units. The importer is a narrow reader for the repository's pinned PBIR schemas and does not become a generic JSON parser or editing framework. Unsupported, ambiguous, malformed, or schema-incompatible constructs fail closed before serialization.

Phase 42 remains backend-only. It does not add RPC, VS Code commands, Windows or hosted execution, Desktop automation, provider-security changes, semantic-model or DAX generation, bookmarks, drillthrough, shared slicers, custom visuals, or mutation of arbitrary JSON.

## Import boundary

`PbirLocalReportReader` accepts an explicit local report directory and reads only the known definition files: report metadata, page order, page files, and visual-container files. It validates file presence, pinned schema URLs/versions, page and visual identities, references, supported visual shapes, and target relationships. It projects supported fields into the existing `PbirIntermediateRepresentation`; fields not represented by the IR are retained only when the current serializer can reproduce them deterministically, otherwise import rejects the report rather than silently dropping content.

The reader returns a typed snapshot containing the imported IR, source artifact/file hashes, identity map, and diagnostics. It never mutates the source directory. Stable PBIR folder identities are carried as page and visual identities; new identities are generated deterministically from the mutation request and source identity, and existing identities are never regenerated.

## Mutation contract

The new request contains schema version, mutation ID, source report directory, output directory/materialization settings, and an ordered list of closed typed operations. Operations include add/remove/rename/move page; add/remove/replace/move/resize visual; update binding, formatting, theme, filter, navigation, and slicer. Every operation has an explicit target selector or a typed payload. Selectors may identify page ID, visual ID, section, slot, navigation ID, or slicer ID. Ambiguous selectors are errors.

The contract does not accept generic JSON Patch, arbitrary JSON paths, callbacks, external commands, or analyzer-produced mutation authority. Mutation requests are additive to generation requests and have independent result, evidence, and diagnostic contracts.

## Planning and execution

`PbirMutationPlanner` validates the full request, resolves every target against one immutable imported snapshot, detects duplicate IDs, missing/ambiguous targets, incompatible replacements, slot conflicts, navigation conflicts, and layout conflicts, then emits a deterministic plan. No serializer or filesystem write occurs during planning.

`PbirMutationExecutor` applies the plan to a copy of the shared IR using immutable record replacement and stable ordering. It preserves unrelated pages, visuals, bindings, formatting, and layouts. Reapplying a mutation with the same mutation ID and already-satisfied target state produces a no-change plan; conflicting reuse of a mutation ID with different source or operation content is rejected. The executor updates hashes and lineage through the existing IR integrity and serializer services.

## Verification and evidence

The mutation provider imports and plans first, executes only a valid plan, serializes through `PbirDeployableSerializerService`, and optionally runs existing Phase 31 materialization preview/apply orchestration followed by the existing analyzer round-trip. Result evidence records mutation ID, ordered operations, affected pages/visuals, preserved identities, changed/unchanged file hashes, analyzer result, lineage, timings, and no-change/idempotency status.

Analyzer and schema validation are mandatory for a successful mutation result. A failed import, plan, serializer, schema, materialization, or analyzer gate returns diagnostics and no output artifact.

## Initial supported shape and limitations

The first importer supports only PBIR files emitted by the pinned Phase 29 serializer and the closed visual descriptor catalog already accepted by Phases 39–41. It supports identity, page order/name, visual type/layout/order, supported bindings, supported filters/formatting/theme/navigation/slicer fields, and the serializer's deterministic metadata. It rejects custom visuals, unsupported visual-container objects, arbitrary theme objects, bookmarks, drillthrough, synchronized/shared slicers, semantic-model/DAX changes, and constructs that cannot round-trip through the current IR/serializer without loss.

## Testing and performance

Tests cover importer acceptance/rejection, identity projection, every supported operation, conflict detection, no-op reapplication, hash preservation for untouched files, deterministic output, analyzer regression, and materialization integration. A representative benchmark records import, planning, execution, serialization, materialization, and analyzer timings beside the existing full-generation timing; observations are documented without making performance claims beyond measured local runs.

