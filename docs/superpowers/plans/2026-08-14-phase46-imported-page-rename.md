# Phase 46 — Imported Page Rename Authoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. This plan is design-approved scope only; do not add RPC or VS Code work.

**Goal:** Add one direct typed backend mutation for renaming an imported page display name while preserving imported identity and unrelated PBIR content.

**Architecture:** Reuse `LocalPbirMutationRequest/v1`, the existing reader, planner, executor, hybrid envelope, single merge boundary, serializer/validator, fidelity, and analyzer services. Add only the page display-name merge mapping; keep generation v1–v7 and all cross-process surfaces unchanged.

**Tech Stack:** .NET 8, C# records, `System.Text.Json`/`JsonNode`, pinned PBIR schema lock, existing serializer/materializer/analyzer, xUnit.

---

### Task 1: Freeze the operation inventory

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/PbirAuthoringEnvelopeModels.cs`
- Test: `service-dotnet/tests/Discovery/LocalPbirMutationContractTests.cs`

- [ ] Add one contract assertion that `RenamePage` is `TypedAndMergeable` and that `UpdateTheme` remains `PreservedButNotAuthorable`; keep `ResizeVisual` unchanged.
- [ ] Do not add a new operation kind, request version, generic property path, replacement JSON, or façade.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~LocalPbirMutationContractTests"`; expect all contract tests to pass after the implementation is present.

### Task 2: Admit and validate the typed page target

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirMutationPlanner.cs`
- Test: `service-dotnet/tests/Discovery/PbirMutationPlannerTests.cs` or the existing focused mutation test file

- [ ] Validate `RenamePage` using the existing `target.pageId` lookup and reject unknown or missing page IDs with the existing typed target diagnostic.
- [ ] Reject null, empty, or whitespace-only `displayName` values with a stable field-specific diagnostic; trim only if the existing request normalization contract already does so, otherwise preserve the supplied non-whitespace value exactly.
- [ ] Keep operation ordering deterministic using the existing kind/target ordering and keep the imported-envelope classification gate before operation acceptance.
- [ ] Add tests for valid target, missing target, unknown target, empty name, and preserved-but-not-authorable `UpdateTheme`.

### Task 3: Apply the copy-on-write typed IR overlay

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirMutationExecutor.cs`
- Test: `service-dotnet/tests/Discovery/Phase46ImportedPageRenameTests.cs`

- [ ] Keep the existing executor behavior for all operations and update only the matching page record’s typed display-name field for `RenamePage`.
- [ ] Do not mutate the imported envelope or source JSON in place; the executor must return a new IR state that retains the original source envelope.
- [ ] Test that the typed IR changes the page display name while page ID, page order, visual IDs, and visual order remain unchanged.

### Task 4: Add the page-owned merge path

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringMergeService.cs`
- Test: `service-dotnet/tests/Discovery/Phase46ImportedPageRenameTests.cs`

- [ ] Add a page-owner merge branch that resolves the imported page document by owner identity and changes only the pinned schema’s display-name property.
- [ ] Preserve the source folder/name path, source identity, unrelated page properties, visual documents, bindings, and Phase 42 interaction documents.
- [ ] Return the exact changed semantic path and leave unchanged pages byte/source-equivalent where the current serializer permits it.
- [ ] Reject a page document whose pinned schema mapping does not identify one supported display-name owner; do not guess from arbitrary JSON or rebuild the page.
- [ ] Add assertions for extra admitted property survival, identity/path stability, deterministic document ordering, and no accidental visual changes.

### Task 5: Connect serializer, validation, fidelity, and analyzer evidence

**Files:**
- Modify: existing mutation result/evidence integration files only if the current provider path does not already expose the fields
- Test: `service-dotnet/tests/Discovery/Phase46ImportedPageRenameTests.cs`

- [ ] Serialize the resolved imported page through the existing deterministic serializer and recompute artifact/manifest/file hashes.
- [ ] Require the existing pinned schema, structural, cross-reference, and hash validators before a ready result is returned.
- [ ] Attach fidelity evidence showing the page display-name path as expected and no unrelated missing/unexpected paths; retain analyzer-before/after as evidence only.
- [ ] Add no-op/equivalent-request determinism assertions and a failure assertion that validation/fidelity failure produces no ready artifact.

### Task 6: Run compatibility regression and close the implementation gate

**Files:**
- Test: focused Phase 46 test file and existing backend suites
- Verify: `docs/current-state/phase46-imported-page-rename-state.md`, `docs/superpowers/implementation-notes/2026-08-14-phase46-imported-page-rename.md`

- [ ] Run focused Phase 46 tests and the existing Phase 42–45 authoring/fidelity/analyzer/serializer tests.
- [ ] Run the full backend Release suite and record the expected Windows skips without adding a Windows requirement.
- [ ] Run Core Release build, extension Jest, webview Jest, TypeScript/production build, and `git diff --check` if implementation is authorized.
- [ ] Confirm no `RpcHost`, VS Code, transport, generation v1–v7, Phase 42, analyzer/scoring, or provider-runtime files changed unexpectedly.
- [ ] Update the current-state and implementation-note records only after objective acceptance criteria pass.

## Explicit non-implementation gate for this goal

The current Phase 46 goal defines and documents the milestone only. No task
above is authorized in this goal; implementation begins only in a separately
approved execution goal. The existing superseded `PbirAuthoringRpc` proposal,
uncommitted/untracked work, and unrelated repository changes must remain
untouched.

