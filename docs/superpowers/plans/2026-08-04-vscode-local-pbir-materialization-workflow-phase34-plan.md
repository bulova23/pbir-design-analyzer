# VS Code Local PBIR Materialization Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a narrow, read-only-preview/explicit-apply/recovery workflow to the existing Report Design Studio using only the three Phase 33 local PBIR RPC routes.

**Architecture:** Keep all transport and filesystem authority behind `AnalyzerBridgeService` and Phase 33. Add one host-side coordinator with typed redacted presentation state and lifecycle generation guards, then render that state in the existing Design Studio materialize stage. No new webview authority or parallel panel is introduced.

**Tech Stack:** VS Code extension host TypeScript, React webview, Jest, existing `vscode-languageclient` bridge, existing Design Studio protocol and CSS conventions.

---

### Task 1: Lock the host workflow contract with failing tests

**Files:**
- Create: `vscode-extension/src/services/materialization/PbirMaterializationWorkflow.ts`
- Create: `vscode-extension/src/test/pbirMaterializationWorkflow.test.ts`

- [ ] Define the coordinator interface around `executeRequest`, `showProgress`, confirmation, and cancellation seams; include only the three route names.
- [ ] Write tests for preview request construction, redacted response projection, all fifteen outcome mappings, exact preview identity propagation, fresh transaction-ID generation, and apply gating.
- [ ] Run `npm test -- --runInBand src/test/pbirMaterializationWorkflow.test.ts` and verify the new tests fail because the coordinator is absent.
- [ ] Implement the smallest state machine that passes the tests, clearing applyable preview after applyable terminal outcomes and requiring a new preview after stale/conflict/failure/recovery-required/cancelled results.
- [ ] Re-run the focused test and verify it passes without touching backend code.

### Task 2: Add host lifecycle, cancellation, confirmation, and recovery tests

**Files:**
- Modify: `vscode-extension/src/services/materialization/PbirMaterializationWorkflow.ts`
- Modify: `vscode-extension/src/test/pbirMaterializationWorkflow.test.ts`

- [ ] Add deterministic tests for cancellation during each operation, progress cleanup, confirmation rejection, double-submit suppression, disconnect/restart reset, disposal, and ignored late responses.
- [ ] Add tests proving recovery inspection calls only `pbir/materialization/recovery/inspect` and never calls an apply route or filesystem API.
- [ ] Implement request-generation invalidation and cancellation-source disposal; preserve cancellation and reconnect guidance without attempting rollback.
- [ ] Run the focused host suite and verify all lifecycle tests pass.

### Task 3: Register the existing-surface entry point

**Files:**
- Modify: `vscode-extension/src/commands/pbirCommands.ts`
- Modify: `vscode-extension/src/platform/extensionIds.ts`
- Modify: `vscode-extension/package.json`
- Modify: `vscode-extension/src/test/packageManifest.test.ts`
- Modify: `vscode-extension/src/test/pbirMaterializationCommand.test.ts`

- [ ] Add one command that opens or focuses the existing Design Studio materialize stage, passing the active report target and bridge; do not create a second panel or direct filesystem path reader.
- [ ] Register the command using the existing command registration pattern and add it to the existing explorer view title/context only where a report target is available.
- [ ] Test command registration, target propagation, and unavailable-bridge messaging.
- [ ] Run the focused command tests and TypeScript compilation.

### Task 4: Extend the existing Design Studio protocol and host panel

**Files:**
- Modify: `vscode-extension/src/design-studio/contracts/designStudioProtocol.ts`
- Modify: `vscode-extension/src/views/PbirDesignStudioPanel.ts`
- Modify: `vscode-extension/src/test/designStudioProtocol.test.ts`
- Modify: `vscode-extension/src/test/pbirDesignStudioPanel.materialization.test.ts`

- [ ] Add versioned intent messages for preview, apply-confirmed, recovery inspection, and cancellation; validate their exact shape before use.
- [ ] Add a safe host-to-webview materialization workflow view model and route all operations through the coordinator.
- [ ] Reset the workflow when the panel disposes or the bridge reports disconnect/restart; ignore late responses from older generations.
- [ ] Test message validation, progress/outcome rendering, confirmation callback, cancellation, disposal, and no raw payload/path forwarding.

### Task 5: Render the workflow in the existing materialize stage

**Files:**
- Create: `vscode-extension/webview-src/design-studio/components/LocalPbirMaterializationWorkflow.tsx`
- Modify: `vscode-extension/webview-src/design-studio/App.tsx`
- Modify: `vscode-extension/webview-src/design-studio/styles.css`
- Create: `vscode-extension/webview-src/design-studio/__tests__/LocalPbirMaterializationWorkflow.test.tsx`
- Modify: `vscode-extension/webview-src/design-studio/__tests__/App.test.tsx`

- [ ] Write tests for idle/preview/loading/confirmation/apply/recovery and every terminal outcome, asserting safe summaries only.
- [ ] Add accessible headings, status/live-region announcements, keyboard-operable buttons, disabled in-flight controls, explicit confirmation text, and visible cancellation.
- [ ] Render destination classification, artifact counts, deterministic identity, conflict/recovery information, and bounded diagnostics using existing Design Studio terminology.
- [ ] Ensure applyable preview state is never recreated from webview state after stale, conflict, failure, cancellation, restart, or disposal.
- [ ] Run focused webview tests and inspect the rendered DOM accessibility semantics.

### Task 6: Update architecture documents and repository memory

**Files:**
- Create: `docs/current-state/vscode-local-pbir-materialization-workflow-state.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/superpowers/plans/2026-06-20-design-package-microsoft-skills-integration-plan.md`
- Modify: `docs/current-state/architecture-gap-analysis.md`
- Modify: `docs/current-state/pbir-materialization-rpc-adapter-state.md`
- Modify: `docs/current-state/pbir-materialization-provider-adapter-state.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-08-04T-phase34-vscode-local-pbir-materialization-workflow.md`

- [ ] Record Phase 33 and Phase 34 explicitly, keep Phase 35 onward provisional/unauthorized, and state that provider/Skills execution, generated-artifact intake, Analyzer handoff, refinement, Fabric App generation, deployment, and publishing are not implemented by Phase 34.
- [ ] Record the single Design Studio entry point, three-route boundary, state-reset rules, and residual risks.
- [ ] Update focus and session summaries with validation totals and any unvalidated gates.

### Task 7: Run the required validation inventory

**Files:**
- No additional production changes; inspect the complete diff.

- [ ] Run focused host and webview tests, changed-boundary and scope tests, Phase 29–34 regression inventory, backend suite with zero failures/skips, RPC transport/adapter suites, full extension Jest, full webview Jest, combined Jest, TypeScript compile, and scoped lint over every changed TypeScript/JavaScript file.
- [ ] Compare repository lint against its documented baseline and run roadmap/document/placeholder/whitespace/scope/production-boundary/changed-boundary/repository-output gates plus `git diff --check`.
- [ ] Confirm branch, changed-file list, and that no commit or push occurred.
