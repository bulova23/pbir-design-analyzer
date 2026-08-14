# Phase 43 Lossless Authoring IR Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Do not implement this plan in the planning/reconciliation goal.

**Goal:** Safely edit an existing valid pinned-schema PBIR report through the existing Phase 42 mutation foundation while preserving unrelated valid content and all v1–v7 generation behavior.

**Architecture:** Use the existing shared IR plus a schema-admitted source-document envelope. Apply a closed typed mutation to a copy of the typed IR, merge only supported fields into owned source documents, serialize through the existing deterministic serializer, and validate before analyzer/materialization evidence. Generation without an imported envelope remains on the existing rebuild path.

**Tech Stack:** .NET 8, C# records, `System.Text.Json`, pinned local PBIR schema lock, existing serializer/materializer/analyzer, xUnit.

---

### Task 1: Freeze the reconciled Phase 43 contract

**Objective:** Make the semantic losslessness, hybrid ownership, typed/opaque boundary, error classes, and acceptance gate unambiguous.

**Likely files:** `docs/superpowers/specs/2026-08-14-phase43-lossless-authoring-ir-design.md`, `docs/superpowers/plans/2026-08-14-phase43-lossless-authoring-ir.md`, `docs/ROADMAP.md`, `docs/current-state/pbir-intermediate-representation-state.md`.

**Implementation scope:** Record the existing HEAD discrepancy and classify the committed envelope/reader/merge/fidelity work as partial evidence. State that no new request version, RPC, or UI surface is required. Keep Phase 42’s typed operation catalog as the initial mutation boundary.

**Tests/evidence:** Documentation consistency scan; all design/plan contract names match; no production files changed.

**Stop condition:** Stop if the contract requires arbitrary JSON mutation, byte-for-byte guarantees, or a public surface.

### Task 2: Establish the source-envelope ownership and admission boundary

**Objective:** Ensure every preserved document has one owner, one pinned schema identity, one stable relative path, and detached source content.

**Likely files:** `service-dotnet/Services/Discovery/Models/PbirAuthoringEnvelopeModels.cs`, `PbirIntermediateRepresentationModels.cs`, `PbirLocalReportReader.cs`, `PbirAuthoringEnvelopeReader.cs`, `PbirDeployableSerializerModels.cs`.

**Implementation scope:** Retain the existing envelope records but make admission cover only the pinned owned definition inventory. Clone source JSON, preserve source hashes, reject invalid JSON/schema/owner/path, and ensure unsupported files do not silently become ready output. Keep the envelope optional so generation IR has no source owner.

**Tests/evidence:** Contract tests for typed/opaque/unsupported classifications; reader tests for invalid JSON, unsupported schema, path ownership, detached source content, and deterministic item ordering.

**Stop condition:** Stop if an admitted document cannot be mapped to a pinned owner or if source content has dual mutable authorities.

### Task 3: Complete typed imported identity and order resolution

**Objective:** Preserve existing page/visual folder identities and semantic ordering while retaining deterministic identities for new objects.

**Likely files:** `PbirLocalReportReader.cs`, `PbirMutationPlanner.cs`, `PbirMutationExecutor.cs`, `PbirDeployableSerializerService.cs`, new focused identity tests.

**Implementation scope:** Resolve imported identity from envelope ownership first and generated identity only for new objects. Validate uniqueness, missing/ambiguous targets, page/visual references, and order. Do not make explicit identity overrides part of the minimum contract; if the existing model retains them, validate them as closed typed values.

**Tests/evidence:** Imported identity/path assertions, new-object determinism, collision/missing-target failures, page/visual order preservation, and unchanged v1–v7 generation hashes.

**Stop condition:** Stop if an imported object’s output path can be regenerated from the IR ID instead of its source identity.

### Task 4: Define the typed mutation overlay inventory

**Objective:** Connect only supported Phase 42 operations to explicit merge paths.

**Likely files:** `LocalPbirMutationModels.cs`, `PbirMutationPlanner.cs`, `PbirMutationExecutor.cs`, `PbirMutationPlanning.cs`, `LocalPbirMutationProviderService.cs`.

**Implementation scope:** Inventory each operation as `typed-and-mergeable`, `preserved-but-not-authorable`, or `unsupported`. Initially require end-to-end support for visual move/resize or page rename and preservation of Phase 42 interaction records/bindings. Reject formatting/theme/filter/navigation/slicer operations until their typed merge paths exist; never treat their opaque source as a mutation escape hatch.

**Tests/evidence:** Operation matrix tests proving accepted operations have a merge path, unsupported operations return typed diagnostics, and no operation accepts JSON paths/replacement fragments.

**Stop condition:** Stop if a request can name an arbitrary property or if planner acceptance does not imply a deterministic merge path.

### Task 5: Implement the single copy-on-write merge boundary

**Objective:** Apply typed changes to cloned owned documents while preserving every unrelated property.

**Likely files:** `PbirAuthoringMergeService.cs`, `PbirResolvedAuthoringModels.cs`, `PbirMutationExecutor.cs`, merge tests.

**Implementation scope:** Extend the existing layout-only merge to the closed typed overlay inventory. For each changed field, replace only the service-owned JSON subtree; leave all other source properties untouched. Return resolved documents, changed semantic paths, preserved source hashes, and typed diagnostics. Do not expose a general JSON DOM service.

**Tests/evidence:** Merge precedence tests for untouched source versus typed mutation, unknown property preservation, interaction preservation, missing-owner conflicts, and deterministic resolved document ordering.

**Stop condition:** Stop on any merge that rebuilds a whole imported page/visual or silently drops an unmodeled property.

### Task 6: Integrate hybrid documents into serializer and schema validation

**Objective:** Serialize resolved imported documents without changing generation-only output and validate the final artifact.

**Likely files:** `PbirDeployableSerializerService.cs`, `PbirDeployableSerializerValidator.cs`, serializer model tests.

**Implementation scope:** Keep generated writer methods as the no-envelope fallback. For imported reports, replace only matching owned files or add/remove files through explicit typed object operations; do not silently ignore missing resolved documents. Recompute file/hash/manifest metadata after merge. Run the existing pinned schema, structural, cross-reference, and hash validation before readiness.

**Tests/evidence:** No-op serializer round trip, bounded mutation serializer round trip, schema validation for report/pages/page/visual/interactions, and v1–v7 generation regression tests.

**Stop condition:** Stop if the serializer accepts a preserved document that the validator rejects, or if generated output changes without an imported envelope.

### Task 7: Add fidelity evidence and analyzer boundary checks

**Objective:** Turn the standalone fidelity helper into a read-only acceptance gate and prove analyzer separation.

**Likely files:** `PbirRoundTripFidelityService.cs`, `PbirRoundTripFidelityModels.cs`, `LocalPbirMutationModels.cs`, `LocalPbirMutationProviderService.cs`, analyzer/mutation tests.

**Implementation scope:** Compare source/output by byte hash, canonical semantic JSON, expected changed paths, missing paths, and unexpected paths. Attach evidence additively to the existing mutation result. Run the existing analyzer before and after; record deltas as evidence only. Do not change scoring authority or analyzer inputs to accommodate opaque content.

**Tests/evidence:** No-op has no unexpected differences and stable score; one bounded mutation has only expected differences; analyzer remains callable; unexpected/missing output blocks readiness.

**Stop condition:** Stop if fidelity comparison can mark an unrelated change expected without a typed mutation path.

### Task 8: Add focused golden fixtures

**Objective:** Prove preservation with small representative documents rather than a generic corpus.

**Likely files:** existing repository PBIR test fixture locations; `service-dotnet/tests/Discovery/Phase43RoundTripFixtureTests.cs`; fixture documentation.

**Implementation scope:** Use an existing generated report as baseline and add focused variants for page/visual layout, bindings, slicer interactions, formatting/theme/filter/navigation metadata, stable identities, and one valid admitted additional property not projected into typed IR. Add one invalid/unsupported schema fixture. Keep fixtures opt-in if they require external PBIR files.

**Tests/evidence:** Canonical no-op equivalence; unknown-property survival through a bounded edit; identity/order/interactions retained; invalid fixture fails closed; repeated runs produce equal canonical output.

**Stop condition:** Stop if a fixture proves only byte equality and not semantic preservation, or if the unknown-content case is not schema-admitted.

### Task 9: Measure bounded performance and document limitations

**Objective:** Record whether retaining source documents creates a material local cost and document the exact supported/unsupported boundary.

**Likely files:** `docs/current-state/phase43-lossless-authoring-state.md`, `docs/superpowers/implementation-notes/2026-08-14-phase43-lossless-authoring.md`, `docs/ROADMAP.md`, phase 42/IR state docs.

**Implementation scope:** Measure import, plan, execute, merge, serialize, schema validation, and analyzer stages on the representative fixture. Report observations only; do not add a threshold without evidence. Mark Phase 43 complete only after the acceptance gate passes; keep Phase 44 RPC deferred.

**Tests/evidence:** Deterministic timing harness or existing test timing output; documentation validation; roadmap/current-state consistency.

**Stop condition:** Stop if performance evidence requires a new storage system, hosted execution, or architectural cache.

### Task 10: Run the approval gate and close documentation

**Objective:** Verify the complete compatibility surface and hand off the plan for approval without implementing it in this goal.

**Likely files:** `.agent-memory/current-focus.md`, `.agent-memory/session-summaries.md`, `.agent-memory/sessions/`, planning documents only.

**Implementation scope:** For the later implementation session, run focused Phase 43 tests, full backend Release, Core Release build, extension Jest/webview Jest/compile/build, schema evidence checks, repository documentation checks, and `git diff --check`. In this planning goal, run only the documentation/schema inspection checks and report the already-run 8/8 targeted test result.

**Tests/evidence:** No production implementation is performed here; the approval gate is a written checklist with exact commands and expected compatibility results.

**Stop condition:** Do not begin Phase 44, public RPC, VS Code, Desktop, Windows, hosted execution, or any production Phase 43 code from this goal.

## Exact first task

Task 1 is the first implementation task: freeze the reconciled semantic losslessness contract and operation matrix in the design/current-state documents, then obtain approval before touching production code.
