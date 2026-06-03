# AI Fix Phase 2 Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Harden the shipped deterministic fix workflow with safer multi-opportunity sequencing, stronger conflict detection, richer rollback/session history, clearer stale handling, and better grouped diff/outcome reporting.

**Architecture:** Keep the current remediation-led workflow, fix opportunity engine, and deterministic mutation layer intact. Add Phase 2 orchestration and presentation state above the existing single-opportunity pipeline so grouped preview/apply/rollback remains explicit, reversible, and independent from future AI enrichment.

**Tech Stack:** TypeScript, React, Jest, VS Code webview UI, existing score-panel payload/state contracts, deterministic PBIR mutation/apply engine

---

## File Map

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - add grouped preview/apply/conflict/session-history contracts
- Modify: `vscode-extension/src/analyzer/fixes/fixOpportunityBuilder.ts`
  - keep opportunity metadata sufficient for compatibility evaluation
- Create: `vscode-extension/src/analyzer/fixes/fixCompatibility.ts`
  - evaluate compatibility, overlap, stale/conflict reasons, and selection safety
- Create: `vscode-extension/src/analyzer/fixes/fixBatchPreview.ts`
  - build grouped preview models for selected opportunities
- Create: `vscode-extension/src/analyzer/fixes/fixSessionHistory.ts`
  - define apply-session and rollback-history shaping helpers
- Modify: `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts`
  - add grouped sequencing and all-or-nothing batch orchestration
- Modify: `vscode-extension/src/analyzer/fixes/fixOutcomeEvaluator.ts`
  - add grouped outcome summaries without changing individual outcome semantics
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
  - orchestrate selection, grouped preview, grouped apply confirmation, rollback history, and regeneration messaging
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
  - thread Phase 2 state into the webview payload
- Create or Modify: `vscode-extension/webview-src/analyzer-score/fixOpportunities.ts`
  - if Phase 2 UI logic grows, centralize grouped preview/state formatting helpers here
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
  - add multi-select, grouped preview, conflict messaging, session history, and richer outcome presentation
- Create: `vscode-extension/webview-src/analyzer-score/fixOpportunities.test.ts`
  - cover grouped preview/state formatting helpers if helper extraction occurs
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
  - cover multi-select UX, conflict states, grouped preview, session history, and regeneration messaging
- Create: `vscode-extension/src/test/fixCompatibility.test.ts`
- Create: `vscode-extension/src/test/fixBatchPreview.test.ts`
- Modify: `vscode-extension/src/test/fixApplyEngine.test.ts`
- Modify: `vscode-extension/src/test/fixOutcomeEvaluator.test.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/CHANGELOG.md`

## Major Workstreams

### Task 1: Extend Phase 2 Contracts

- [ ] Define grouped preview, compatibility, conflict, selection, and session-history contracts in `vscode-extension/src/analyzer/contracts/scorePanel.ts`.
- [ ] Keep contract boundaries explicit:
  - opportunity = executable single proposal
  - grouped preview = presentation of a selected set
  - apply session = execution and history record
  - conflict state = reason apply is blocked
- [ ] Document that Phase 2 stays downstream from scoring and preserves the deterministic mutation layer trust boundary.

### Task 2: Add Compatibility And Conflict Tests First

- [ ] Create `vscode-extension/src/test/fixCompatibility.test.ts`.
- [ ] Add failing tests for:
  - overlapping property mutations on the same object
  - incompatible opportunity categories
  - stale preview detection
  - changed target object detection
  - selection sets that remain compatible
- [ ] Run only the new test file first and confirm failure reasons match missing compatibility behavior.

### Task 3: Build Compatibility Evaluation

- [ ] Create `vscode-extension/src/analyzer/fixes/fixCompatibility.ts`.
- [ ] Implement deterministic compatibility evaluation for:
  - overlapping mutations
  - incompatible selections
  - stale or drifted targets
  - missing rollback coverage
- [ ] Return user-facing conflict reasons plus machine-readable conflict codes.
- [ ] Re-run compatibility tests and confirm green.

### Task 4: Add Grouped Preview Builder

- [ ] Create `vscode-extension/src/analyzer/fixes/fixBatchPreview.ts`.
- [ ] Create `vscode-extension/src/test/fixBatchPreview.test.ts`.
- [ ] Add failing tests proving grouped preview can:
  - merge multiple selected opportunities
  - group rows by page/object/property
  - summarize changed objects and touched files
  - separate mutation facts from expected outcomes
- [ ] Implement grouped preview shaping and make the focused tests pass.

### Task 5: Harden Apply Sequencing

- [ ] Extend `vscode-extension/src/test/fixApplyEngine.test.ts` with failing batch tests for:
  - deterministic apply order
  - validation before any mutation
  - all-or-nothing failure handling
  - no partial success when one selected opportunity conflicts
  - rollback availability after grouped apply
- [ ] Modify `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts` to orchestrate grouped selection apply using the compatibility evaluator and grouped preview.
- [ ] Preserve the validation-first sequence:
  1. compatibility and stale checks
  2. backup capture
  3. ordered mutation apply
  4. apply-session record
  5. re-analysis trigger
- [ ] Re-run apply engine tests and confirm green.

### Task 6: Add Session History And Rollback State

- [ ] Create `vscode-extension/src/analyzer/fixes/fixSessionHistory.ts`.
- [ ] Add tests proving session history can record:
  - applied opportunities
  - rollback availability
  - rollback success/failure
  - superseded/regenerated opportunities
- [ ] Keep rollback deterministic from stored plans rather than regeneration.
- [ ] Thread session history through host state without introducing a separate workspace.

### Task 7: Expand Outcome Reporting

- [ ] Extend `vscode-extension/src/test/fixOutcomeEvaluator.test.ts` with failing grouped-outcome tests for:
  - per-opportunity outcomes within a batch
  - grouped summaries by status
  - `AppliedWithUnexpectedOutcome` visibility at session level
- [ ] Modify `vscode-extension/src/analyzer/fixes/fixOutcomeEvaluator.ts` to emit grouped summaries while preserving the existing individual outcome semantics.
- [ ] Re-run outcome tests and confirm green.

### Task 8: Thread Phase 2 Payload State

- [ ] Modify `vscode-extension/src/views/scoreResultPayload.ts`.
- [ ] Extend `vscode-extension/src/test/scoreResultPayload.test.ts` with failing tests for:
  - selected opportunity metadata
  - conflict reasons
  - grouped preview payload
  - session history payload
- [ ] Keep scores, severities, confidences, and normalized findings unchanged.
- [ ] Re-run payload tests and confirm green.

### Task 9: Add Host Orchestration

- [ ] Modify `vscode-extension/src/views/PbirScorePanel.ts`.
- [ ] Add state/message handling for:
  - selecting opportunities
  - previewing selected opportunities
  - approving selected opportunities
  - applying selected opportunities with explicit confirmation
  - rolling back from stored session history
  - regenerating stale opportunities
- [ ] Keep the host responsible for execution and the webview responsible for presentation.

### Task 10: Add Webview Phase 2 UX

- [ ] Modify `vscode-extension/webview-src/analyzer-score/App.tsx`.
- [ ] If the fix-opportunity presentation logic becomes materially larger, extract grouped formatting/state helpers into `vscode-extension/webview-src/analyzer-score/fixOpportunities.ts`.
- [ ] Add UI support for:
  - selecting multiple compatible opportunities
  - grouped preview
  - explicit batch approval/apply confirmation
  - conflict and stale messaging
  - session history and rollback visibility
  - richer grouped outcome summaries
- [ ] Keep the workflow remediation-led inside `Fix Plan`.

### Task 11: Add Webview Tests

- [ ] Extend `vscode-extension/webview-src/analyzer-score/App.test.tsx`.
- [ ] Add tests proving:
  - compatible opportunities can be selected together
  - incompatible selections are blocked with clear messages
  - grouped preview renders page/object/property summaries
  - explicit confirmation is required before grouped apply
  - stale opportunities can be regenerated
  - session history and rollback availability render correctly
  - grouped outcome reporting distinguishes mutation facts from outcome interpretation
- [ ] Add focused helper tests in `vscode-extension/webview-src/analyzer-score/fixOpportunities.test.ts` if helper extraction happens.

### Task 12: Preserve Phase 1 Semantics

- [ ] Add regression coverage proving Phase 2 does not change:
  - score values
  - severity values
  - confidence values
  - normalized finding semantics
  - the remediation-led placement of fix workflows
- [ ] Re-run existing focused tests for remediation queue and persona presentation to ensure no presentation regressions leaked across the workspace.

### Task 13: Update Docs

- [ ] Update `docs/ROADMAP.md` to reflect Phase 2 status once implementation lands.
- [ ] Update `docs/CHANGELOG.md` when the implementation ships.
- [ ] Keep the AI-fix phase ladder explicit:
  - Phase 1 deterministic engine
  - Phase 2 hardening
  - Phase 3 AI-assisted enrichment
  - Phase 4 advanced AI refactoring

## Non-Goals

- no model calls
- no provider integration
- no LLM-generated proposals
- no new visual creation
- no visual deletion
- no chart replacement
- no DAX or model/TMDL semantic edits
- no page redesign
- no separate refactor workspace
- no advanced AI refactoring in Phase 2

## Validation Checklist

- [ ] `cd vscode-extension && npm test`
- [ ] `cd vscode-extension && npm run compile`
- [ ] `cd vscode-extension && npm run package`
- [ ] `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- [ ] targeted ESLint on changed files if repo-wide lint still has unrelated known failures
- [ ] packaged extension smoke check against:
  - the real `Sales & Production.pbip` fixture
  - a deterministic PBIR fixture that exercises supported multi-opportunity cases

## Testing Strategy Notes

- Start with focused red/green cycles for compatibility, grouped preview, apply sequencing, and grouped outcomes.
- Prefer isolated deterministic fixtures for overlap/conflict/stale scenarios rather than relying only on the real business-report fixture.
- Keep one explicit smoke pass for the packaged extension because grouped preview/apply/rollback UX must still be verified end to end in the real extension host.

## Execution Notes

- Follow TDD strictly for compatibility, grouped preview, apply orchestration, rollback history, and grouped outcome logic.
- Preserve narrow file responsibilities; do not collapse compatibility, grouped preview, apply, and session history into one module.
- Commit after each major hardening slice or logically complete vertical path.
