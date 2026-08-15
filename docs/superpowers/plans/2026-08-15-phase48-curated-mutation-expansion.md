# Phase 48 — Curated Mutation Expansion Implementation Plan

> **For agentic workers:** Implement this plan task-by-task with test-first validation. Keep all changes unstaged and uncommitted as required by the Phase 48 request.

**Goal:** Expose six curated mutation families through the existing preview/execute authoring workflow with typed previews, semantic diffs, fail-closed planning, and a thin VS Code picker.

**Architecture:** Extend the existing typed planner, executor, RPC response, and workflow in place. The public adapter owns only the explicit allowlist; the planner owns interpretation and validation; execution re-plans from immutable snapshots and returns new opaque artifact handles.

**Tech Stack:** .NET 8/C#, xUnit, TypeScript, VS Code API, Jest, existing PBIR serializer/materialization/analyzer services.

---

### Task 1: Establish failing backend contract tests

**Files:**
- Modify: `service-dotnet/tests/Discovery/PbirMutationPlannerTests.cs`
- Modify: `service-dotnet/tests/Discovery/PbirAuthoringRpcMutationTests.cs`

- [ ] Add tests for one-operation enforcement, request-order preservation, duplicate targets, invalid page positions, page-removal navigation conflicts, invalid bounds, move/resize no-ops, and typed planner diagnostics.
- [ ] Add RPC tests for typed previews and semantic diffs for AddPage, RemovePage, MovePage, MoveVisual, and ResizeVisual; retain RenamePage coverage.
- [ ] Add lifecycle tests proving stale handles reject, source files remain unchanged, new identities are deterministic, and analyzer before/after data is returned.
- [ ] Run the focused tests and record the expected failures before implementation.

### Task 2: Implement planner validation and semantic plan data

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirMutationModels.cs`
- Modify: `service-dotnet/Services/Discovery/PbirMutationPlanning.cs`
- Modify: `service-dotnet/Services/Discovery/PbirMutationPlanner.cs`
- Modify: `service-dotnet/Services/Discovery/PbirMutationExecutor.cs`

- [ ] Add typed semantic diff records and operation-specific preview input data to the internal plan model.
- [ ] Preserve request order, reject public multi-operation requests at the RPC boundary, and retain deterministic fingerprints.
- [ ] Validate exact page positions and movement, duplicate/deleted targets, removal/navigation safety, and layout bounds/overlap rules using existing deterministic layout conventions.
- [ ] Keep AddPage narrow and derive deterministic identity from the accepted request rather than frontend state.
- [ ] Implement only the six admitted preview/diff shapes while preserving backend-only operation support and structured diagnostics.
- [ ] Run the focused backend tests and refactor only after green.

### Task 3: Extend RPC typed preview/diff response without changing v1

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcContract.cs`
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcDispatcher.cs`
- Modify: `service-dotnet/RpcHost/PbirAuthoringRpcAdapter.cs`
- Modify: `service-dotnet/tests/PbirAuthoringRpcAdapterTests.cs`

- [ ] Add discriminated typed preview payloads and semantic diff records under the existing response envelope.
- [ ] Replace the RenamePage-only adapter gate with the explicit six-entry allowlist and reject multiple public operations with stable structured errors.
- [ ] Map planner diagnostics to structured categories without exposing raw exceptions or filesystem/IR data.
- [ ] Ensure preview is non-materializing, execute re-plans, no-ops do not execute, and timing fields remain populated.
- [ ] Run RPC and adapter tests, then the backend Release test project.

### Task 4: Add the shared thin VS Code mutation workflow

**Files:**
- Modify: `vscode-extension/src/services/rpc/PbirAuthoringWorkflow.ts`
- Modify: `vscode-extension/src/test/PbirAuthoringWorkflow.test.ts`
- Modify: extension command registration file located by existing `pbir.authoring` command wiring.

- [ ] Add curated mutation selection and operation-specific input collection for pages, visuals, position, and dimensions.
- [ ] Route every operation through one preview/confirm/execute helper that renders backend-owned typed preview/diff data.
- [ ] Update page/visual metadata only from backend responses needed for selection; do not calculate or maintain mutation state locally.
- [ ] Add tests for prompts, confirmation, cancellation, no-op, backend rejection, structured errors, and handle retention.
- [ ] Run extension Jest, webview Jest, TypeScript compilation, and changed-file lint.

### Task 5: Update Phase 48 documentation and memory

**Files:**
- Modify: `docs/ROADMAP.md`
- Modify: `README.md` where public authoring workflow is described
- Create: `docs/current-state/phase48-curated-mutation-expansion-state.md`
- Create: `docs/superpowers/implementation-notes/2026-08-15-phase48-curated-mutation-expansion.md`
- Create: `docs/pbir-authoring-workflow.md`
- Modify: `.agent-memory/current-focus.md`
- Create: `.agent-memory/sessions/20260815-phase48-curated-mutation-expansion.md`
- Modify: `.agent-memory/session-summaries.md`

- [ ] Document the public mutation matrix, representative previews/diffs, analyzer comparisons, identities, timings, limitations, and Phase 49 gate.
- [ ] Clearly separate public operations from backend-only typed capabilities and preserve the no-capability-discovery boundary.
- [ ] Record exact validation counts, expected Windows skips, lint baseline, and any unvalidated limitation.

### Task 6: Full verification and working-tree handoff

**Files:**
- Inspect all changed files and `git diff --check` output.

- [ ] Run focused mutation/RPC tests.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release`.
- [ ] Run extension Jest, webview Jest, TypeScript compilation, production build, VSIX packaging, and changed-file lint.
- [ ] Run `git diff --check`, inspect `git status --short`, and verify no files are staged or committed.
- [ ] Finalize session memory with exact evidence and next recommendation.
