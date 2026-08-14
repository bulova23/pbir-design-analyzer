# Phase 46 — Minimal VS Code Authoring Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose Generate, Import, and Analyze from the existing `pbir-authoring-rpc/v1` dispatcher through the current stdio JSON-RPC host and three minimal VS Code commands.

**Architecture:** Add one thin `pbir/authoring` host adapter that validates the three-operation allowlist, deserializes the existing typed request envelope, invokes `PbirAuthoringRpcDispatcher`, and returns its typed response. Additive handle-aware Analyze resolution remains in the core dispatcher so opaque handles never become paths in TypeScript. The extension reads only a selected typed generation-request JSON file, selects a PBIR folder for import, retains session handles, and presents bounded results through the existing output channel and notifications.

**Tech Stack:** .NET 8, System.Text.Json, xUnit, TypeScript, VS Code API, Jest.

---

### Task 1: Close the handle-aware Analyze contract gap

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcContract.cs`
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcDispatcher.cs`
- Test: `service-dotnet/tests/Discovery/PbirAuthoringRpcValidationAndAnalysisTests.cs`

- [x] **Step 1: Write failing tests** for Analyze using the artifact handle returned by Generate and the snapshot handle returned by Import, asserting score and artifact/source identity are preserved.
- [x] **Step 2: Run the focused tests** with `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirAuthoringRpcValidationAndAnalysisTests`; confirm the current directory-only Analyze contract cannot satisfy the handle workflow.
- [x] **Step 3: Add optional artifact/snapshot handle fields** to the existing Analyze request and store the generated/imported source directory only inside the dispatcher session dictionaries.
- [x] **Step 4: Resolve Analyze input in this order:** validated artifact handle, validated snapshot handle, explicit report directory; reject stale/missing handles with the existing structured AnalyzerFailed category.
- [x] **Step 5: Run the focused tests again** and then the Phase 45 RPC tests; expected result is all green with no changes to generation v1–v7 or mutation/Validate behavior.

### Task 2: Add the thin RpcHost adapter and route allowlist

**Files:**
- Create: `service-dotnet/RpcHost/PbirAuthoringRpcContracts.cs`
- Create: `service-dotnet/RpcHost/PbirAuthoringRpcAdapter.cs`
- Modify: `service-dotnet/RpcHost/AnalyzerRpcDispatcher.cs`
- Test: `service-dotnet/tests/PbirAuthoringRpcAdapterTests.cs`
- Test: `service-dotnet/tests/RpcHostScopeBoundaryTests.cs`

- [x] **Step 1: Write failing adapter tests** for Generate, Import, Analyze, malformed parameters, unknown operations, and the absence of Mutation/Validate registration.
- [x] **Step 2: Run only the new adapter tests** and confirm failure because `pbir/authoring` is not a known method.
- [x] **Step 3: Implement one adapter method** that checks payload size/object shape, deserializes the existing core request using the core contract and enum converter, rejects operations other than Generate/Import/Analyze, invokes one dispatcher instance, and serializes the existing response with diagnostics/error/timing fields intact.
- [x] **Step 4: Register exactly `pbir/authoring`** in `AnalyzerRpcDispatcher`; keep generic JSON-RPC framing and existing routes unchanged.
- [x] **Step 5: Add a direct-dispatch-versus-adapter equivalence test** comparing score, identity, diagnostics, fidelity, and timing presence for representative Generate, Import, and Analyze calls.
- [x] **Step 6: Run adapter, Phase 45, and existing RpcHost tests**; expected result is no Mutation or standalone Validate route and no raw stack trace in the response envelope.

### Task 3: Add the minimal extension client workflow

**Files:**
- Create: `vscode-extension/src/services/rpc/PbirAuthoringWorkflow.ts`
- Modify: `vscode-extension/src/services/rpc/AnalyzerBridgeService.ts`
- Modify: `vscode-extension/src/commands/register.ts`
- Modify: `vscode-extension/src/platform/extensionIds.ts`
- Modify: `vscode-extension/package.json`
- Test: `vscode-extension/src/test/PbirAuthoringWorkflow.test.ts`
- Test: `vscode-extension/src/test/packageManifest.test.ts`

- [x] **Step 1: Write failing unit tests** for typed request-file mapping, Generate/Import/Analyze request payloads, opaque handle retention, invalid selection cancellation, unsupported request schema, structured error messages, and concise result formatting.
- [x] **Step 2: Run the focused Jest test** and confirm failure before implementation.
- [x] **Step 3: Add `executeAuthoringRequest`** to the existing bridge, using the current JSON-RPC client, cancellation token, timeout, and backend output logging.
- [x] **Step 4: Implement the workflow service** with session-only artifact/snapshot state. Generate accepts only a selected JSON file whose pinned schema version is v1–v7 and wraps the typed document without interpreting PBIR. Import accepts only a selected folder and passes its path. Analyze prefers the latest opaque artifact, then snapshot, then a selected supported report folder.
- [x] **Step 5: Register exactly three commands** using the existing PBIR command prefix: Generate Report, Import Report, Analyze Report. Present score, diagnostics, fidelity, identity, and timing in the existing extension output channel and a short notification; map stable RPC categories to concise messages.
- [x] **Step 6: Run focused extension tests and TypeScript compilation**; expected result is no webview or mutation authority added.

### Task 4: Documentation and closeout evidence

**Files:**
- Create: `docs/superpowers/specs/2026-08-14-phase46-vscode-authoring-integration-design.md`
- Create: `docs/superpowers/implementation-notes/2026-08-14-phase46-vscode-authoring-integration.md`
- Create: `docs/current-state/phase46-vscode-authoring-integration-state.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/current-state/pbir-authoring-rpc-state.md`
- Modify: `.agent-memory/repo-map.md`
- Create: `.agent-memory/sessions/20260814-phase46-vscode-authoring-integration.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`

- [x] **Step 1: Document** the command matrix, architecture diagram, workflows, error mapping, transport proof, equivalence evidence, performance observations, limitations, and Phase 47 recommendation.
- [x] **Step 2: Run the repository-authoritative validation set:** focused Phase 46 and Phase 45 backend tests, full Release backend tests, RpcHost build, focused/full extension Jest, webview Jest, TypeScript, production build, VSIX package, scoped lint, and `git diff --check`.
- [x] **Step 3: Record exact counts and expected Windows skips**, preserve the lint baseline, inspect `git diff --stat` and `git status --short`, and verify all work remains unstaged and uncommitted.

---

## Self-review

- Scope covers only Generate, Import, Analyze and leaves Mutation and standalone Validate unregistered.
- Handle resolution is backend-owned; the extension never infers paths or reads PBIR files.
- Existing JSON-RPC routes, generation request schemas, analyzer scoring, and materialization authority remain unchanged.
- The only known contract correction is additive Analyze handle support, required to make the requested session workflow possible.
