# Phase 43 Lossless Authoring IR — Reconciled Design

## Decision

Phase 43 is a backend-only, internal authoring capability for safely editing an existing valid PBIR report through the already-approved Phase 42 mutation foundation. It is not a new generation request version, RPC route, VS Code feature, or general PBIR editor.

The smallest correct architecture is a hybrid: the typed shared IR remains the only authoring authority for fields the provider intentionally supports; an imported, schema-admitted source-document envelope owns untouched valid PBIR content; a single bounded merge boundary applies typed mutations to copies of owned source documents; the existing serializer and schema validator produce the result. Generation-only IRs continue through the existing deterministic rebuild path.

This design corrects the committed Phase 43 artifacts at `4797756c`: those artifacts are protected evidence and remain unmodified in production code, but their helper implementation is partial. In particular, the current merge service overlays visual layout only, the fidelity comparer is not connected to mutation evidence, and no end-to-end analyzer comparison proves lossless authoring.

## Meaning of lossless

Lossless means semantic preservation of every valid, schema-admitted property in the bounded owned PBIR document set that is not intentionally changed by a supported typed mutation. A no-op may retain original bytes for owned documents. A changed document may be canonically serialized; JSON whitespace and property order are not public behavior.

The contract is therefore semantic losslessness with opportunistic physical fidelity, not byte-for-byte fidelity. It preserves page and visual folder identities, references, ordering that affects report behavior, layout, bindings, filters, formatting, navigation metadata, slicer metadata, themes, and valid additional properties in admitted owned documents. It does not promise preservation of invalid JSON, unsupported schema versions, files outside the owned definition inventory, or serialization formatting.

## Current lossiness and evidence

Phase 42 imports a narrow projection into `PbirIntermediateRepresentation`: page/visual identity, order, display name, supported visual type, layout, and supported bindings. The Phase 42 serializer then rebuilds report, pages, page, and visual documents from typed state. Properties not represented by that IR are dropped or regenerated. Existing identities are captured in maps but the serializer historically derives output folders from generated IR identity. The mutation executor changes typed IR only and does not carry source JSON into serialization.

The committed Phase 43 envelope and reader partially address this by retaining source text, source hashes, schema URLs, owner paths, and imported page/visual identities. They do not yet establish complete preservation or mutation coverage.

## Actual information flow

```text
existing PBIR directory
  → PbirLocalReportReader
  → typed PbirIntermediateRepresentation + PbirAuthoringEnvelope
  → PbirMutationPlanner
  → PbirMutationExecutor (copy-on-write typed state)
  → PbirAuthoringMergeService (typed overlay over owned source documents)
  → PbirDeployableSerializerService
  → PbirDeployableSerializerValidator / pinned schema checks
  → optional Phase 31 materialization
  → existing analyzer and scoring
```

Generation remains separate:

```text
typed local-pbir-generation-request/v1–v7
  → existing provider/shared IR
  → existing deterministic serializer/materializer/analyzer path
```

The authoring envelope is an internal extension of the existing shared IR, not a third analyzer model. The analyzer continues to consume the narrower semantic model it needs. Opaque preserved properties remain outside scoring unless an existing analyzer reader already understands them.

## Ownership and copy strategy

The decision is **HYBRID**.

- The imported source-document envelope owns preserved raw JSON and its source hashes.
- The typed IR owns supported semantic fields and mutation intent.
- The mutation plan/executor owns the copy-on-write typed overlay; it never mutates source files or the envelope in place.
- The merge service owns precedence and produces a resolved document set.
- The serializer owns final artifact inventory, hashes, lineage, and output validation.

Unchanged imported documents are copied from the envelope. Changed owned documents are patched only at typed, service-owned properties. Generated objects with no imported owner are rebuilt by the existing serializer. This follows the existing Phase 42 import/plan/execute separation and avoids rebuilding unrelated valid content.

## Typed and opaque boundary

Typed authoring is limited to mutation operations already represented by the closed Phase 42 request models and to explicitly added typed fields with tests. The initial Phase 43 mutation proof is visual move/resize or page rename, plus preservation of Phase 42 interactions and all unrelated source content. Formatting, theme, filter, navigation, slicer, bookmarks, drillthrough, synchronized slicers, semantic-model changes, and DAX remain preserved-but-not-typed unless a later task adds a closed contract.

Opaque preservation stores a cloned JSON object and metadata for an admitted owner. It is never exposed as JSON Patch, JSON Pointer, arbitrary replacement JSON, or a caller-selected path. Unsupported mutations fail with a typed diagnostic. Opaque content cannot bypass the pinned schema validator.

## Identity and ordering

For imported pages and visuals, the source folder/name identity is retained and used for output paths and references. For newly generated objects, the existing deterministic identity allocation remains authoritative. An explicit identity override is not part of the minimum Phase 43 contract; if retained by the committed envelope model, it must be validated for ownership and uniqueness before selection and cannot rename an imported object implicitly.

Page order, visual order, and arrays whose PBIR semantics depend on order are preserved semantically and normalized deterministically. JSON object property order, whitespace, and line endings may be canonicalized. Source order metadata is diagnostic evidence, not a promise of byte order.

## Schema boundary and errors

The pinned `PbirDeployableSchemaLock` inventory is authoritative for admitted files and schema URLs. Admission requires valid JSON, an owned definition path, and the expected pinned schema. Final output still passes the existing serializer validator and materialization schema validator. Schema-admitted additional properties are preserved; schema-invalid content is rejected rather than preserved as a loophole.

The bounded failure classes are: invalid source PBIR, unsupported schema or owner, ambiguous identity, missing mutation target, unsupported mutation, preservation conflict, identity collision, resulting schema-invalid document, and unsupported authoring request. They use existing typed diagnostic/result conventions and stable codes; raw JSON exceptions are internal details.

## Round-trip contract

1. **No-op:** a valid imported report enters the authoring path and serializes to semantically equivalent owned documents. Unchanged documents may be byte-identical. Missing, schema-invalid, or unexpected differences fail the fidelity gate.
2. **Bounded mutation:** one supported typed mutation changes only its declared semantic paths; unrelated valid owned content, interactions, identities, and analyzer-relevant bindings remain semantically equivalent.
3. **Validation:** every ready result passes pinned schema/structural/cross-reference/hash validation and remains analyzable.
4. **Determinism:** equivalent source plus equivalent typed mutation produces equivalent canonical artifact content and stable hashes.

## Analyzer boundary

The reader projects imported data into both the typed IR and the envelope, but the analyzer receives only the existing semantic model. Phase 43 compares analyzer-before and analyzer-after results as evidence; it does not let scoring or analyzer output authorize mutations. A known limitation is acceptable where a valid opaque property is preserved but intentionally excluded from scoring.

## Non-goals and compatibility

There is no change to local generation request versions v1–v7, Phase 29–31 boundaries, Phase 41 composition, Phase 42 same-page slicer interactions, RPC, VS Code, extension contracts, Desktop, Windows, hosted execution, provider runtime, scoring formulas, or analyzer authority. No synchronized slicers, bookmarks, drillthrough, arbitrary JSON authoring, or general report-editor surface is added.

The principal compatibility risk is serializer behavior for imported envelopes: identity/path resolution and merge precedence could alter generated output if the envelope is accidentally attached to generation IR. The implementation must guard the imported-envelope path separately and prove byte/hash-equivalent canonical output for v1–v7 generation fixtures.

## Acceptance gate

Phase 43 is ready only when all are objectively demonstrated:

1. Valid pinned-schema PBIR imports into typed IR plus an owned envelope.
2. No-op round trip is semantically equivalent and reports any normalization explicitly.
3. A bounded typed mutation changes its intended path and no unrelated owned path.
4. Imported page/visual identities and order remain stable; new identities remain deterministic.
5. Unknown-but-valid admitted properties survive the bounded mutation.
6. Interactions and analyzer-relevant bindings survive the round trip.
7. Output passes schema, structural, cross-reference, and hash validation.
8. Output remains analyzable and scoring is unchanged for a no-op.
9. Repeated equivalent operations produce deterministic canonical output.
10. v1–v7, Phase 41, Phase 42, analyzer, scoring, provider-runtime, and backend regression suites remain green.

## Fixtures

Use the smallest repository-owned generated PBIR fixture as the baseline, then add focused variants containing: one page and multiple visuals; a slicer with same-page interactions; layout and bindings; report/page/visual formatting; filters; navigation metadata; a theme object; one valid additional property not projected into typed IR; stable identities; and an invalid/unsupported schema case. Compare canonical semantic paths and fidelity classifications, not raw whole-file hashes except for unchanged source documents.
