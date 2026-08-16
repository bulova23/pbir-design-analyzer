# Phase 47 — Interactive Mutation Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose one backend-authoritative RenamePage preview/confirm/execute workflow through VS Code and return analyzer before/after evidence.

**Architecture:** Extend the existing `pbir-authoring-rpc/v1` Mutate payload with preview/execute mode and typed semantic preview/result data. The backend re-plans on execution and returns a new artifact handle; the thin VS Code workflow renders backend data and retains opaque handles.

**Tech Stack:** .NET 8, C#, xUnit, TypeScript, VS Code API, Jest, existing JSON-RPC adapter and PBIR authoring services.

---

### Task 1: Add the typed preview and comparison contracts

**Files:**
- Modify: `service-dotnet/Services/Discovery/Models/LocalPbirMutationModels.cs`
- Modify: `service-dotnet/RpcHost/../Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcContract.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringRpcContractTests.cs`

- [ ] **Step 1: Write failing contract tests** for preview/execute mode, semantic preview fields, source/after analyzer summaries, score delta, and identity evidence.
- [ ] **Step 2: Run the focused contract test and confirm it fails because the new fields do not exist.**
- [ ] **Step 3: Add the minimal records/enums and nullable response fields.** Preserve existing serialized names and defaults for Generate/Import/Analyze.
- [ ] **Step 4: Run the focused contract test and confirm it passes.**

### Task 2: Implement backend preview and RenamePage public admission

**Files:**
- Modify: `service-dotnet/RpcHost/PbirAuthoringRpcAdapter.cs`
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcDispatcher.cs`
- Modify: `service-dotnet/Services/Discovery/PbirMutationPlanner.cs` only if a missing no-op/invalid-name diagnostic is required
- Test: `service-dotnet/tests/Discovery/PbirAuthoringRpcMutationTests.cs`
- Test: `service-dotnet/tests/PbirAuthoringRpcAdapterTests.cs`

- [ ] **Step 1: Write failing tests** for exact-one-RenamePage public admission, rejection of ResizeVisual/other mutations, preview generation without materialization, deterministic no-op preview, and execute re-planning.
- [ ] **Step 2: Run the focused backend/RPC tests and confirm the expected failures.**
- [ ] **Step 3: Implement adapter admission and dispatcher preview/execute branches.** Preview returns planner-derived semantic data and never calls serializer/materialization. Execute re-resolves the snapshot and calls the existing planner before executor.
- [ ] **Step 4: Add backend-owned analyzer-before lookup and after comparison, fidelity, identity evidence, and timings.** Return the new artifact handle while leaving the snapshot dictionary entry unchanged.
- [ ] **Step 5: Run focused tests and confirm they pass.**

### Task 3: Add import page metadata and transport mapping

**Files:**
- Modify: `service-dotnet/RpcHost/PbirAuthoringRpcContract.cs`
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcDispatcher.cs`
- Modify: `vscode-extension/src/services/rpc/PbirAuthoringWorkflow.ts`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringRpcImportTests.cs`
- Test: `vscode-extension/src/test/PbirAuthoringWorkflow.test.ts`

- [ ] **Step 1: Write failing tests** proving import returns only transport-safe page ID/display-name metadata and the workflow can create Quick Pick items from it without reading PBIR files.
- [ ] **Step 2: Run the focused tests and confirm failure.**
- [ ] **Step 3: Add page metadata to the import result and map it through the existing adapter JSON options.**
- [ ] **Step 4: Store the metadata with the opaque snapshot in the workflow and add pure helpers for RenamePage request construction and preview formatting.**
- [ ] **Step 5: Run focused tests and confirm pass.**

### Task 4: Add the VS Code Rename Page command

**Files:**
- Modify: `vscode-extension/src/services/rpc/PbirAuthoringWorkflow.ts`
- Modify: `vscode-extension/src/commands/register.ts`
- Modify: `vscode-extension/src/platform/extensionIds.ts`
- Modify: `vscode-extension/package.json`
- Test: `vscode-extension/src/test/PbirAuthoringWorkflow.test.ts`

- [ ] **Step 1: Write failing workflow tests** for missing import state, page selection cancellation, name cancellation, preview cancellation, no-op non-execution, successful preview/execute/analyzer presentation, and handle retention.
- [ ] **Step 2: Run the focused Jest test and confirm failure.**
- [ ] **Step 3: Implement the command with VS Code Quick Pick/Input Box/confirmation primitives.** Call preview first and execute only after confirmation; retain only returned opaque handles.
- [ ] **Step 4: Register the command and package contribution.** Do not add a webview or mutation menu.
- [ ] **Step 5: Run focused Jest and TypeScript checks.**

### Task 5: Document Phase 47 and record evidence

**Files:**
- Modify: `docs/ROADMAP.md`
- Modify: `docs/current-state/phase46-imported-page-rename-state.md` or add `docs/current-state/phase47-interactive-mutation-workflow-state.md`
- Create: `docs/superpowers/implementation-notes/2026-08-14-phase47-interactive-mutation-workflow.md`
- Modify: `vscode-extension/README.md` or the existing authoring guide location
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/20260814-phase47-interactive-mutation-workflow.md`

- [ ] **Step 1: Document the workflow diagram, one-entry mutation matrix, preview model, no-undo boundary, limitations, and Phase 48 recommendation.**
- [ ] **Step 2: Record observed planner, preview, execute, analyzer, and RPC timings without thresholds.**
- [ ] **Step 3: Update memory with the final validation state and next recommendation.**

### Task 6: Validate the complete phase

- [ ] **Step 1: Run focused backend mutation/contract/adapter tests.**
- [ ] **Step 2: Run backend Release regression and record passed/skipped counts.**
- [ ] **Step 3: Run extension Jest, webview Jest, TypeScript compilation, production build, and packaging.**
- [ ] **Step 4: Run changed-file lint according to the repository baseline and record the unchanged full-lint baseline.**
- [ ] **Step 5: Run `git diff --check`, inspect `git status`, and verify all changes are unstaged/uncommitted.**
