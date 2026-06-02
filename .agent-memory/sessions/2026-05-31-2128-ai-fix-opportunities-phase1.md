# 2026-05-31 21:28 - Deterministic Fix Opportunity Engine Phase 1

## Objective

Implement Phase 1 of AI-Assisted Fixes as a deterministic, remediation-led Fix Opportunity Engine without changing scoring, severity, confidence, or backend scoring semantics.

## What Changed

- Added Phase 1 fix-opportunity contracts to `vscode-extension/src/analyzer/contracts/scorePanel.ts`:
  - `FixOpportunity`
  - `FixMutation`
  - `RollbackPlan`
  - preview, apply-result, lifecycle-state, and outcome-summary types
- Added deterministic fix-engine modules under `vscode-extension/src/analyzer/fixes/`:
  - `fixOpportunityBuilder.ts`
  - `fixMutationPlanner.ts`
  - `fixPreview.ts`
  - `fixApplyEngine.ts`
  - `rollbackPlanBuilder.ts`
  - `fixOutcomeEvaluator.ts`
- Wired fix opportunities into score payload generation in `vscode-extension/src/views/scoreResultPayload.ts`.
- Extended `vscode-extension/src/views/PbirScorePanel.ts` to:
  - approve opportunities
  - apply deterministic mutations with validation-first behavior
  - roll back by restoring recorded file backups
  - re-analyze automatically after apply
  - surface post-apply outcome states
- Extended `vscode-extension/webview-src/analyzer-score/App.tsx` so remediation items now own the fix workflow:
  - opportunities render under remediation items, not findings
  - preview shows structured mutation rows (`Object`, `Property`, `Before`, `After`)
  - approve/apply/rollback actions post to the host
  - lifecycle state and re-analysis outcomes are visible in the remediation section
  - unsupported remediation stays advisory
- Added tests:
  - `vscode-extension/src/test/fixOpportunityBuilder.test.ts`
  - `vscode-extension/src/test/fixApplyEngine.test.ts`
  - `vscode-extension/src/test/fixOutcomeEvaluator.test.ts`
  - updated `vscode-extension/webview-src/analyzer-score/App.test.tsx`

## Architecture Decisions

- Kept the remediation-first chain intact:
  - `Issues`
  - `Remediation Queue`
  - `Fix Opportunity Engine`
  - `Deterministic Mutation Layer`
- Preserved the execution trust boundary:
  - all report changes flow through explicit opportunities
  - all opportunities expose explicit mutations and previews
  - rollback plans are created before apply
  - re-analysis verifies outcomes after apply
- Connected webview remediation items to payload opportunities by source-finding traceability rather than assuming UI queue item ids match the payload remediation ids exactly.

## Validation

- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`
- `cd vscode-extension && npx eslint webview-src/analyzer-score/App.tsx webview-src/analyzer-score/App.test.tsx src/views/PbirScorePanel.ts src/views/scoreResultPayload.ts src/analyzer/fixes/*.ts src/test/fixOpportunityBuilder.test.ts src/test/fixApplyEngine.test.ts src/test/fixOutcomeEvaluator.test.ts`

## Residual Risks

- Manual VS Code smoke coverage has not been rerun for the new fix-opportunity workflow.
- `PbirScorePanel.refresh()` still resets selected tab state during refresh/re-analysis, so apply/rollback may return the user to a broader context than they started from.
- No version bump or new `.vsix` package was created in this session. If this Phase 1 feature is intended to ship, release work should move to the next version after `0.2.2`.
