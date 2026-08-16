# Optimization Report Scoring Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Optimization Report scoring use one authoritative Import → Analyze(handle) contract so neither the legacy missing-`reportPath` failure nor the authoring bounded-object failure can recur.

**Architecture:** The score panel will construct a bounded authoring Import request from its selected report reference, retain the returned opaque snapshot handle only for the subsequent Analyze request, and normalize the analyzer result into the existing score-panel contract. The backend authoring dispatcher already resolves snapshot handles to backend-owned directories and invokes the authoritative PbirScoringService; direct path-based `model/pbir/scoreReport` remains an explicit compatibility route for existing non-Optimization-Report callers.

**Tech Stack:** TypeScript, VS Code extension host, JSON-RPC, .NET 8, xUnit, Jest.

---

### Task 1: Lock the intended request flow with focused failing tests

**Files:**
- Modify: `vscode-extension/src/test/pbirScorePanelScoring.test.ts`
- Modify: `service-dotnet/tests/PbirAuthoringRpcDispatcherTests.cs` only if an existing test needs a precise contract assertion

- [ ] **Step 1: Replace the legacy request assertion with a two-stage Optimization Report workflow assertion.**
  Assert that the score-panel request builder produces exactly one bounded Import request followed by Analyze using the returned opaque snapshot, including config and optional page name, and that neither request uses `model/pbir/scoreReport` or a generation payload.

- [ ] **Step 2: Add assertions for both historical error strings.**
  Assert the Optimization Report request construction cannot contain `reportPath` as the Analyze input and cannot be null/undefined at the authoring boundary; retain the separate adapter test proving malformed null params still gets the bounded-object diagnostic.

- [ ] **Step 3: Run the focused Jest test and confirm it fails against the current legacy builder/implementation.**
  Run: `cd vscode-extension && npx jest src/test/pbirScorePanelScoring.test.ts --runInBand`
  Expected: FAIL because the current builder returns the legacy path-scoring payload.

### Task 2: Implement one authoritative Optimization Report bridge flow

**Files:**
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Modify: `vscode-extension/src/services/rpc/PbirAuthoringWorkflow.ts` only if shared response/request types need extraction

- [ ] **Step 1: Add a typed request-flow helper for Import and Analyze.**
  Keep the report path only in the Import request. Make the Analyze request accept a typed snapshot handle returned by Import, plus config and page name.

- [ ] **Step 2: Change the PBIR branch of `PbirScorePanel.refresh()`.**
  Invoke `executeAuthoringRequest` with `operation: 'import'`, require a successful `importResult.snapshot`, then invoke `operation: 'analyze'` with that snapshot. Read `response.analyzer.result`, preserve the current normalization, enrichment, telemetry, persistence, diagnostics, and score-panel rendering behavior.

- [ ] **Step 3: Add safe diagnostics at the bridge boundary.**
  Log only operation/source kind/handle presence/path presence, for example `sourceKind=SnapshotHandle handlePresent=true reportPathPresent=false operation=Analyze`; never log raw handles or PBIR contents.

- [ ] **Step 4: Run the focused Jest test and confirm it passes.**
  Run: `cd vscode-extension && npx jest src/test/pbirScorePanelScoring.test.ts --runInBand`
  Expected: PASS.

### Task 3: Protect backend operation-specific contracts

**Files:**
- Modify: `service-dotnet/Services/Discovery/PbirAuthoringRpc/PbirAuthoringRpcDispatcher.cs` only if diagnostics or Analyze validation need tightening
- Modify: `service-dotnet/tests/PbirAuthoringRpcDispatcherTests.cs`
- Modify: `service-dotnet/tests/PbirAuthoringRpcAdapterTests.cs` only for exact historical regression coverage

- [ ] **Step 1: Add/adjust Analyze(handle) contract coverage.**
  Prove Import → Analyze(snapshot) resolves through backend-owned state and reaches scoring without a generation request or webview path reconstruction.

- [ ] **Step 2: Preserve explicit path adapter coverage if supported.**
  Prove Analyze(reportDirectory) continues to work as a deliberate direct adapter input, separate from the Optimization Report handle flow.

- [ ] **Step 3: Retain the malformed authoring request test.**
  Confirm the exact bounded-object error remains tied to malformed `pbir/authoring` input and cannot be emitted by a valid Optimization Report flow.

- [ ] **Step 4: Run focused backend tests.**
  Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirAuthoringRpc"`
  Expected: all focused tests pass.

### Task 4: Validate complete extension and backend behavior

**Files:**
- No source changes expected unless validation exposes a real contract defect.
- Add/update the session note and memory closeout files per `AGENTS.md`.

- [ ] **Step 1: Run extension Jest and webview Jest.**
  Run: `cd vscode-extension && npm test -- --runInBand`
  Record exact extension/webview counts and unrelated failures separately.

- [ ] **Step 2: Run TypeScript compilation, production build, changed-file lint, and diff checks.**
  Run `npm run compile`, `npm run build`, targeted ESLint for changed TypeScript files, and `git diff --check`.

- [ ] **Step 3: Run full backend Release tests.**
  Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release`.
  Record exact pass/skip/fail results.

- [ ] **Step 4: Package the production VSIX and inspect its bundled backend.**
  Run: `npm run package`; verify the packaged extension contains the authoring Analyze route and the score panel bundle does not route Optimization Report scoring through `model/pbir/scoreReport`.

- [ ] **Step 5: Perform real VS Code/VSIX smoke validation if the local workflow supports it.**
  Install or launch the exact generated VSIX, open a PBIR report, open Optimization Report, verify score rendering, and capture whether the backend diagnostics show `SnapshotHandle` Analyze.

- [ ] **Step 6: Report repository status without committing or staging.**
  Include HEAD, changed/staged files, commits, generated artifacts, exact validation results, compatibility behavior, and any validation limitations.

