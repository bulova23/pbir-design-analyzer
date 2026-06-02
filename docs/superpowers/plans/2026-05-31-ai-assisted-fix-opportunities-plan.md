# AI-Assisted Fix Opportunities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a remediation-led deterministic fix workflow that can generate, preview, apply, roll back, and re-analyze safe metadata-only report refactors.

**Architecture:** Keep `Issues` diagnostic and `Remediation Queue` actionable. Add a `Fix Opportunity Engine` under remediation items and a permanent `Deterministic Mutation Layer` that applies only safe existing-object metadata changes through explicit preview, apply, rollback, and re-analysis workflows.

**Tech Stack:** TypeScript, React, Jest, VS Code webview UI, current score-panel payload builders, PBIR/report metadata JSON mutation code, .NET analyzer re-analysis path where needed

---

## File Map

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - Add fix opportunity, mutation, rollback, preview, and lifecycle state contracts.
- Modify: `vscode-extension/src/analyzer/score/fixPlan.ts`
  - Keep remediation intent logic separate from executable opportunity logic.
- Create: `vscode-extension/src/analyzer/fixes/fixOpportunityBuilder.ts`
  - Build deterministic fix opportunities from remediation items and normalized findings.
- Create: `vscode-extension/src/analyzer/fixes/fixMutationPlanner.ts`
  - Produce typed low-level mutations for supported safe categories.
- Create: `vscode-extension/src/analyzer/fixes/rollbackPlanBuilder.ts`
  - Build rollback plans before apply is allowed.
- Create: `vscode-extension/src/analyzer/fixes/fixPreview.ts`
  - Build structured preview rows from mutations.
- Create: `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts`
  - Validate assumptions, create backups, apply mutations, and return apply results.
- Create: `vscode-extension/src/analyzer/fixes/fixOutcomeEvaluator.ts`
  - Compare re-analysis results to expected resolutions and assign outcome states.
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
  - Thread fix-opportunity metadata into the score-panel payload where appropriate.
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
  - Surface remediation-led fix opportunity generation, preview, apply, rollback, and outcome reporting.
- Create: `vscode-extension/webview-src/analyzer-score/fixOpportunities.ts`
  - UI helpers for grouping, status mapping, and preview formatting.
- Test: `vscode-extension/src/test/fixOpportunityBuilder.test.ts`
- Test: `vscode-extension/src/test/fixApplyEngine.test.ts`
- Test: `vscode-extension/src/test/fixOutcomeEvaluator.test.ts`
- Test: `vscode-extension/webview-src/analyzer-score/fixOpportunities.test.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/CHANGELOG.md`

## Major Workstreams

### Task 1: Add Deterministic Fix Opportunity Contracts

- [ ] Define new presentation and execution contracts in `vscode-extension/src/analyzer/contracts/scorePanel.ts` for:
  - `FixOpportunity`
  - `FixMutation`
  - `RollbackPlan`
  - `FixPreviewRow`
  - `FixOpportunityState`
  - `FixApplyResult`
  - `FixOutcomeSummary`
- [ ] Keep the contract boundary explicit:
  - remediation item = conceptual solution intent
  - fix opportunity = executable proposal
  - mutation = actual file edit
- [ ] Add comments documenting the permanent execution trust boundary.

### Task 2: Write Failing Builder Tests For Supported Safe Domains

- [ ] Create `vscode-extension/src/test/fixOpportunityBuilder.test.ts`.
- [ ] Add failing tests proving deterministic generation from remediation items for:
  - title text standardization
  - title placement normalization
  - semantic color normalization
  - alignment / spacing / grid normalization
  - navigation consistency
  - cross-page consistency on existing objects
- [ ] Add failing tests proving unsupported remediation items remain advisory and generate zero opportunities.
- [ ] Run only this test file first and confirm failure reason is missing builder behavior rather than bad fixtures.

### Task 3: Build The Fix Opportunity Builder

- [ ] Create `vscode-extension/src/analyzer/fixes/fixOpportunityBuilder.ts`.
- [ ] Implement deterministic mapping from remediation items plus normalized findings into zero/one/many fix opportunities.
- [ ] Keep generation remediation-led rather than finding-led.
- [ ] Preserve:
  - `sourceFindingIds`
  - `expectedResolutions`
  - `affectedPages`
  - `targetObjectIds`
- [ ] Keep unsupported remediation items advisory only.
- [ ] Re-run the new builder tests and make them pass.

### Task 4: Add Typed Mutation Planning

- [ ] Create `vscode-extension/src/analyzer/fixes/fixMutationPlanner.ts`.
- [ ] Implement typed mutation planning for safe existing-object edits only:
  - title text
  - title position
  - position / size normalization
  - semantic color / theme-role updates
  - navigation placement normalization
- [ ] Explicitly refuse to plan mutations for:
  - visual creation
  - visual deletion
  - chart swaps
  - DAX/model/TMDL changes
- [ ] Add focused tests in `fixOpportunityBuilder.test.ts` or a new mutation planner test proving the unsupported operations are blocked.

### Task 5: Add Rollback Plan Generation Before Apply

- [ ] Create `vscode-extension/src/analyzer/fixes/rollbackPlanBuilder.ts`.
- [ ] Add failing tests in `vscode-extension/src/test/fixApplyEngine.test.ts` proving apply cannot proceed without a rollback plan.
- [ ] Implement rollback plan generation from:
  - original file content
  - reverse mutations
- [ ] Keep rollback deterministic and independent from future regeneration.
- [ ] Re-run the focused tests and confirm green.

### Task 6: Build Structured Preview Rows

- [ ] Create `vscode-extension/src/analyzer/fixes/fixPreview.ts`.
- [ ] Add failing tests covering preview rows shaped around:
  - object
  - property
  - before
  - after
- [ ] Implement preview generation from typed mutations, including page and object references.
- [ ] Keep narrative explanation secondary to the mutation list.
- [ ] Re-run preview-focused tests and confirm green.

### Task 7: Build Apply Validation And Mutation Execution

- [ ] Create `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts`.
- [ ] Add failing tests in `vscode-extension/src/test/fixApplyEngine.test.ts` for:
  - validation before apply
  - stale preview detection when before-values drift
  - backup before mutation
  - all-or-nothing apply
  - no partial success on failed validation
- [ ] Implement apply order:
  1. validate assumptions
  2. capture backup
  3. apply mutations
  4. record apply result
- [ ] Re-run apply engine tests and confirm green.

### Task 8: Build Rollback Execution

- [ ] Extend `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts` or create a sibling rollback executor.
- [ ] Add failing tests for:
  - restoring original metadata values
  - restoring original file content for touched sections
  - deterministic rollback without regenerating fix logic
- [ ] Implement rollback using the precomputed rollback plan.
- [ ] Re-run rollback-focused tests and confirm green.

### Task 9: Add Re-Analysis Outcome Evaluation

- [ ] Create `vscode-extension/src/analyzer/fixes/fixOutcomeEvaluator.ts`.
- [ ] Add failing tests in `vscode-extension/src/test/fixOutcomeEvaluator.test.ts` for lifecycle and outcome states:
  - `Applied`
  - `Rolled Back`
  - `Stale`
  - `Failed Validation`
  - `Applied With Unexpected Outcome`
  - `Resolved`
  - `Improved`
  - `Unchanged`
  - `Unexpected`
- [ ] Implement deterministic comparison between expected resolutions and re-analysis results.
- [ ] Re-run outcome tests and confirm green.

### Task 10: Thread Fix Opportunity Metadata Into Score Payload

- [ ] Modify `vscode-extension/src/views/scoreResultPayload.ts`.
- [ ] Decide the minimum payload needed so the webview can:
  - show supported remediation items
  - request previews
  - surface apply / rollback state
- [ ] Keep scoring, severity, and confidence semantics unchanged.
- [ ] Add or extend payload regression tests in `vscode-extension/src/test/scoreResultPayload.test.ts`.

### Task 11: Surface Fix Opportunities In The Webview

- [ ] Create `vscode-extension/webview-src/analyzer-score/fixOpportunities.ts`.
- [ ] Modify `vscode-extension/webview-src/analyzer-score/App.tsx` so supported remediation items expose:
  - `Generate Fix Opportunities`
  - preview state
  - apply state
  - rollback state
  - re-analysis outcome state
- [ ] Keep the workflow remediation-led and do not add primary generation buttons to raw issue cards.
- [ ] Add helper copy that reinforces the deterministic execution boundary.

### Task 12: Add Webview Tests For Preview / Apply / Rollback UX

- [ ] Extend `vscode-extension/webview-src/analyzer-score/App.test.tsx`.
- [ ] Add tests proving:
  - supported remediation items expose fix opportunity actions
  - unsupported remediation items remain advisory
  - preview renders structured mutation rows
  - stale opportunities are blocked from apply
  - lifecycle states render correctly
  - `Applied With Unexpected Outcome` is visible when re-analysis does not match expectations
- [ ] Add focused helper tests in `vscode-extension/webview-src/analyzer-score/fixOpportunities.test.ts`.

### Task 13: Preserve Platform Semantics

- [ ] Add regression coverage proving this feature does not change:
  - score values
  - severity values
  - confidence values
  - normalized finding semantics
- [ ] Re-run existing focused tests that cover remediation and persona presentation.

### Task 14: Document The Roadmap Guardrails

- [ ] Update `docs/ROADMAP.md` to add the deterministic execution principle and execution trust boundary as AI-fix roadmap guardrails.
- [ ] Update `docs/CHANGELOG.md` when implementation lands.
- [ ] Keep the roadmap progression explicit:
  - Phase 1 deterministic fix opportunity engine
  - Phase 2 hardening
  - Phase 3 AI-assisted proposal enrichment
  - Phase 4 advanced AI refactoring

## Non-Goals

- no model calls
- no provider integration
- no LLM-generated proposals
- no new visual creation
- no visual deletion
- no chart replacement
- no DAX or model changes
- no page redesign
- no dedicated refactor workspace in Phase 1

## Validation Checklist

- [ ] `cd vscode-extension && npm test`
- [ ] `cd vscode-extension && npm run lint` or targeted lint if repo-wide lint still has unrelated pre-existing failures
- [ ] `cd vscode-extension && npm run package`
- [ ] manual VS Code smoke check of:
  - remediation-led fix opportunity generation
  - structured preview
  - stale apply prevention
  - rollback
  - automatic re-analysis outcome reporting

## Execution Notes

- Follow TDD strictly for builder, apply, rollback, and outcome logic.
- Keep file responsibilities narrow; do not collapse opportunity building, mutation planning, and application into one file.
- Commit after each major workstream or logically complete vertical slice.
