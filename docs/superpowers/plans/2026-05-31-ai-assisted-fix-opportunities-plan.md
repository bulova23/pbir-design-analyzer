# AI-Assisted Fix Opportunities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a remediation-led deterministic fix workflow that can generate, preview, apply, roll back, and re-analyze safe metadata-only report refactors.

**Architecture:** Keep `Issues` diagnostic and `Remediation Queue` actionable. Add a `Fix Opportunity Engine` under remediation items and a permanent `Deterministic Mutation Layer` that applies only safe existing-object metadata changes through explicit preview, apply, rollback, and re-analysis workflows.

**Tech Stack:** TypeScript, React, Jest, VS Code webview UI, current score-panel payload builders, PBIR/report metadata JSON mutation code, .NET analyzer re-analysis path where needed

## Status

Phase 1 implementation shipped in `0.3.0` on 2026-06-01. The core deterministic fix workflow in this plan is complete in source, tests, changelog, and packaged smoke coverage.

What still remains:

- reconcile this plan's stale unchecked boxes with shipped work
- decide whether to extract webview helper code/tests into the originally planned `fixOpportunities.*` files or keep the current `App.tsx`/`App.test.tsx` implementation
- extend `docs/ROADMAP.md` if we still want explicit AI-fix phase progression and trust-boundary guardrails called out there, not just in release docs
- package and smoke-test the post-`0.3.0` single-page planner follow-up before calling that follow-up release-ready

2026-06-01 follow-up decision:

- `fixOpportunities.ts` helper extraction was intentionally declined for the `0.3.1` follow-up release
- rationale:
  - the current fix-opportunity UI logic remains localized in `App.tsx`
  - no new maintainability boundary was required to ship the single-page planner fix
  - extracting only for parity with the original file map would add churn without improving behavior or safety

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

- [x] Define new presentation and execution contracts in `vscode-extension/src/analyzer/contracts/scorePanel.ts` for:
  - `FixOpportunity`
  - `FixMutation`
  - `RollbackPlan`
  - `FixPreviewRow`
  - `FixOpportunityState`
  - `FixApplyResult`
  - `FixOutcomeSummary`
- [x] Keep the contract boundary explicit:
  - remediation item = conceptual solution intent
  - fix opportunity = executable proposal
  - mutation = actual file edit
- [x] Add comments documenting the permanent execution trust boundary.

### Task 2: Write Failing Builder Tests For Supported Safe Domains

- [x] Create `vscode-extension/src/test/fixOpportunityBuilder.test.ts`.
- [x] Add failing tests proving deterministic generation from remediation items for:
  - title text standardization
  - title placement normalization
  - semantic color normalization
  - alignment / spacing / grid normalization
  - navigation consistency
  - cross-page consistency on existing objects
- [x] Add failing tests proving unsupported remediation items remain advisory and generate zero opportunities.
- [x] Run only this test file first and confirm failure reason is missing builder behavior rather than bad fixtures.

### Task 3: Build The Fix Opportunity Builder

- [x] Create `vscode-extension/src/analyzer/fixes/fixOpportunityBuilder.ts`.
- [x] Implement deterministic mapping from remediation items plus normalized findings into zero/one/many fix opportunities.
- [x] Keep generation remediation-led rather than finding-led.
- [x] Preserve:
  - `sourceFindingIds`
  - `expectedResolutions`
  - `affectedPages`
  - `targetObjectIds`
- [x] Keep unsupported remediation items advisory only.
- [x] Re-run the new builder tests and make them pass.

### Task 4: Add Typed Mutation Planning

- [x] Create `vscode-extension/src/analyzer/fixes/fixMutationPlanner.ts`.
- [x] Implement typed mutation planning for safe existing-object edits only:
  - title text
  - title position
  - position / size normalization
  - semantic color / theme-role updates
  - navigation placement normalization
- [x] Explicitly refuse to plan mutations for:
  - visual creation
  - visual deletion
  - chart swaps
  - DAX/model/TMDL changes
- [x] Add focused tests in `fixOpportunityBuilder.test.ts` or a new mutation planner test proving the unsupported operations are blocked.

### Task 5: Add Rollback Plan Generation Before Apply

- [x] Create `vscode-extension/src/analyzer/fixes/rollbackPlanBuilder.ts`.
- [x] Add failing tests in `vscode-extension/src/test/fixApplyEngine.test.ts` proving apply cannot proceed without a rollback plan.
- [x] Implement rollback plan generation from:
  - original file content
  - reverse mutations
- [x] Keep rollback deterministic and independent from future regeneration.
- [x] Re-run the focused tests and confirm green.

### Task 6: Build Structured Preview Rows

- [x] Create `vscode-extension/src/analyzer/fixes/fixPreview.ts`.
- [x] Add failing tests covering preview rows shaped around:
  - object
  - property
  - before
  - after
- [x] Implement preview generation from typed mutations, including page and object references.
- [x] Keep narrative explanation secondary to the mutation list.
- [x] Re-run preview-focused tests and confirm green.

### Task 7: Build Apply Validation And Mutation Execution

- [x] Create `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts`.
- [x] Add failing tests in `vscode-extension/src/test/fixApplyEngine.test.ts` for:
  - validation before apply
  - stale preview detection when before-values drift
  - backup before mutation
  - all-or-nothing apply
  - no partial success on failed validation
- [x] Implement apply order:
  1. validate assumptions
  2. capture backup
  3. apply mutations
  4. record apply result
- [x] Re-run apply engine tests and confirm green.

### Task 8: Build Rollback Execution

- [x] Extend `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts` or create a sibling rollback executor.
- [x] Add failing tests for:
  - restoring original metadata values
  - restoring original file content for touched sections
  - deterministic rollback without regenerating fix logic
- [x] Implement rollback using the precomputed rollback plan.
- [x] Re-run rollback-focused tests and confirm green.

### Task 9: Add Re-Analysis Outcome Evaluation

- [x] Create `vscode-extension/src/analyzer/fixes/fixOutcomeEvaluator.ts`.
- [x] Add failing tests in `vscode-extension/src/test/fixOutcomeEvaluator.test.ts` for lifecycle and outcome states:
  - `Applied`
  - `Rolled Back`
  - `Stale`
  - `Failed Validation`
  - `Applied With Unexpected Outcome`
  - `Resolved`
  - `Improved`
  - `Unchanged`
  - `Unexpected`
- [x] Implement deterministic comparison between expected resolutions and re-analysis results.
- [x] Re-run outcome tests and confirm green.

### Task 10: Thread Fix Opportunity Metadata Into Score Payload

- [x] Modify `vscode-extension/src/views/scoreResultPayload.ts`.
- [x] Decide the minimum payload needed so the webview can:
  - show supported remediation items
  - request previews
  - surface apply / rollback state
- [x] Keep scoring, severity, and confidence semantics unchanged.
- [x] Add or extend payload regression tests in `vscode-extension/src/test/scoreResultPayload.test.ts`.

### Task 11: Surface Fix Opportunities In The Webview

- [ ] Create `vscode-extension/webview-src/analyzer-score/fixOpportunities.ts`.
- [x] Modify `vscode-extension/webview-src/analyzer-score/App.tsx` so supported remediation items expose:
  - `Generate Fix Opportunities`
  - preview state
  - apply state
  - rollback state
  - re-analysis outcome state
- [x] Keep the workflow remediation-led and do not add primary generation buttons to raw issue cards.
- [x] Add helper copy that reinforces the deterministic execution boundary.

Note: the helper extraction originally planned for `fixOpportunities.ts` was not done. The shipped implementation keeps the logic in `App.tsx`.

### Task 12: Add Webview Tests For Preview / Apply / Rollback UX

- [x] Extend `vscode-extension/webview-src/analyzer-score/App.test.tsx`.
- [x] Add tests proving:
  - supported remediation items expose fix opportunity actions
  - unsupported remediation items remain advisory
  - preview renders structured mutation rows
  - stale opportunities are blocked from apply
  - lifecycle states render correctly
  - `Applied With Unexpected Outcome` is visible when re-analysis does not match expectations
- [ ] Add focused helper tests in `vscode-extension/webview-src/analyzer-score/fixOpportunities.test.ts`.

Note: the helper test file remains open only because the helper extraction was not done.

### Task 13: Preserve Platform Semantics

- [x] Add regression coverage proving this feature does not change:
  - score values
  - severity values
  - confidence values
  - normalized finding semantics
- [x] Re-run existing focused tests that cover remediation and persona presentation.

### Task 14: Document The Roadmap Guardrails

- [ ] Update `docs/ROADMAP.md` to add the deterministic execution principle and execution trust boundary as AI-fix roadmap guardrails.
- [x] Update `docs/CHANGELOG.md` when implementation lands.
- [ ] Keep the roadmap progression explicit:
  - Phase 1 deterministic fix opportunity engine
  - Phase 2 hardening
  - Phase 3 AI-assisted proposal enrichment
  - Phase 4 advanced AI refactoring

Note: `docs/ROADMAP.md` reflects the shipped `0.3.0` workflow and general guardrails, but it does not yet spell out the AI-fix phase progression requested here.

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

- [x] `cd vscode-extension && npm test`
- [x] `cd vscode-extension && npm run lint` or targeted lint if repo-wide lint still has unrelated pre-existing failures
- [x] `cd vscode-extension && npm run package`
- [x] manual VS Code smoke check of:
  - remediation-led fix opportunity generation
  - structured preview
  - stale apply prevention
  - rollback
  - automatic re-analysis outcome reporting

Follow-up note: the post-`0.3.0` single-page planner fix was validated in source and against a real fixture, but it has not yet been packaged and smoke-tested as a follow-up release.

## Execution Notes

- Follow TDD strictly for builder, apply, rollback, and outcome logic.
- Keep file responsibilities narrow; do not collapse opportunity building, mutation planning, and application into one file.
- Commit after each major workstream or logically complete vertical slice.
