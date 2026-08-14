# Repository Phase 42 — Explicit Slicer Interaction Authoring Implementation Plan

Status: **PROPOSED — APPROVAL REQUIRED**

This plan must not be executed until the Phase 42 design and plan are
explicitly approved. It is intentionally limited to the smallest next
roadmap milestone after Phase 41.

## Goal and boundaries

Implement additive `local-pbir-generation-request/v7` explicit same-page
slicer interactions over the existing Phase 41 composition and Phase 29–31
PBIR path. Preserve V1–v6 behavior and do not add RPC, VS Code, provider
activation, Windows/hosted execution, Desktop automation, semantic-model or
DAX generation, deployment, publishing, bookmarks, drillthrough,
synchronized slicers, or report-level composition expansion.

## Task 1 — Approval and baseline

- Record explicit approval and the unchanged Phase 41 boundary.
- Verify the worktree and run focused Phase 41 provider/serializer tests.
- Confirm pinned-schema evidence for `visualInteractions` source/target/type
  entries before changing production code.

## Task 2 — Failing V7 contract tests

Add tests first for:

- V7 schema identity and additive request construction;
- valid slicer-to-visual and slicer-to-page interaction records;
- unknown/non-slicer/self/duplicate/cross-page target rejection;
- unsupported mode and conflicting-rule rejection;
- deterministic normalization and compatibility with V1–v6.

Run the focused tests and confirm the new tests fail for the missing V7
contract/projection behavior.

## Task 3 — Typed V7 records and normalization

Likely files:

- `service-dotnet/Services/Discovery/Models/LocalPbirGenerationModels.cs`
- `service-dotnet/Services/Discovery/Models/Phase41CompositionModels.cs`
- a new `Phase42InteractionModels.cs` if separation improves ownership

Add the V7 request and explicit interaction records without modifying the
meaning of V1–v6 records. Normalize V6-compatible inputs only at the V7
adapter boundary and retain stable diagnostic field paths.

## Task 4 — Validation and IR projection

Likely files:

- new `service-dotnet/Services/Discovery/Phase42InteractionValidation.cs`
- new `service-dotnet/Services/Discovery/Phase42InteractionProjection.cs`
- `service-dotnet/Services/Discovery/Models/PbirIntermediateRepresentationModels.cs`
- `service-dotnet/Services/Discovery/PbirIntermediateRepresentationService.cs`

Validate same-page scope, source/target identity, cardinality, mode,
duplicates, conflicts, and deterministic ordering. Add the narrowest typed IR
record needed to preserve explicit source/target/type semantics; do not add a
generic event model.

## Task 5 — Schema-backed serializer integration

Likely files:

- `service-dotnet/Services/Discovery/PbirDeployableSerializerService.cs`
- focused serializer schema tests

Emit only the pinned-schema-supported `visualInteractions` shape. Preserve
the existing global interaction fallback for V1–v6. Reject unsupported
interaction shapes before artifact creation and do not change scoring.

## Task 6 — Provider round-trip and determinism coverage

Add focused provider tests for:

- one slicer filtering a selected set of same-page visuals;
- page-scope interaction expansion;
- disabled interaction;
- deterministic repeated generation, hashes, materialization, and analyzer
  round-trip;
- V1–v6 output compatibility.

Record representative hashes, timings, and analyzer results in a new Phase 42
implementation note only after implementation is approved and complete.

## Task 7 — Documentation and roadmap closeout

Update the Phase 42 current-state document, implementation note, and roadmap
only after the implementation evidence exists. Document the exact schema
shape, compatibility result, limitations, and why public surfaces remain
deferred.

## Task 8 — Validation and session closeout

Run the focused Phase 42 tests, affected serializer/provider regressions, the
full Release backend suite, required extension/webview/type/build checks,
scoped lint comparison, and `git diff --check` in the repository’s established
order. Preserve the existing lint baseline and expected Windows skips. Keep
unrelated work untouched and report the exact next roadmap recommendation.
