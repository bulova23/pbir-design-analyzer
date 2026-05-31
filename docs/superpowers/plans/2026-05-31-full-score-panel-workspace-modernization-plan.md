# Full Score Panel Workspace Modernization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the score-panel modernization by adding an Overview workspace, filtered/grouped Issues workflow, a Fix Plan remediation queue, refined Evidence placement, and secondary Export positioning without changing underlying scoring logic.

**Architecture:** Keep the scoring engine unchanged. Add host-side presentation builders that derive overview and remediation summaries from `ScoreResult`, normalized findings, and existing review state, then reshape the webview around `Overview -> Issues -> Fix Plan -> Evidence -> Export`. Optional cross-page matrix and persona ordering only ship if they remain presentation-only and map cleanly from existing data.

**Tech Stack:** TypeScript, React, Jest, CSS, VS Code webview UI

---

## File Structure

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - Add presentation-summary contract types for Overview, Fix Plan, optional matrix, and persona ordering metadata.
- Create: `vscode-extension/src/analyzer/score/overviewSummary.ts`
  - Build deterministic overview summaries from existing score state and normalized findings.
- Create: `vscode-extension/src/analyzer/score/fixPlan.ts`
  - Build remediation queue items linked back to normalized findings.
- Create if cleanly supported: `vscode-extension/src/analyzer/score/crossPageMatrix.ts`
  - Build matrix-ready cross-page data from existing page signals.
- Create if cleanly supported: `vscode-extension/src/analyzer/score/personaOrdering.ts`
  - Reorder presentation surfaces without mutating severity/confidence.
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
  - Add overview, fix plan, and optional matrix/persona payload shaping.
- Create: `vscode-extension/src/test/overviewSummary.test.ts`
  - Cover maturity/risk labels, strengths/weaknesses, and top issues/actions.
- Create: `vscode-extension/src/test/fixPlan.test.ts`
  - Cover remediation queue ordering, effort labels, and source finding references.
- Create if implemented: `vscode-extension/src/test/crossPageMatrix.test.ts`
  - Cover matrix derivation.
- Create if implemented: `vscode-extension/src/test/personaOrdering.test.ts`
  - Cover presentation-only reordering.
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
  - Assert new payload objects.
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
  - Reorganize the workspace around Overview, Issues, Fix Plan, Evidence, and secondary Export actions.
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
  - Add overview, filter bar, fix-plan, evidence, matrix, and secondary-export styles.
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
  - Cover overview rendering, filters, fix plan, evidence/export placement, and optional matrix/persona UI.

## Implementation Notes

- Do not add or change scoring algorithms.
- Keep derivation logic out of React.
- Keep normalized findings as the shared issue model.
- Treat review packet preview as Evidence or secondary Export-adjacent content, not a primary surface.
- Optional matrix/persona work should be skipped rather than forced if the data model becomes weak or noisy.

### Task 1: Extend the score-panel contract for presentation summaries

**Files:**
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Test: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] **Step 1: Add a failing payload-shape test for overview and fix-plan fields**

Add assertions that `ScorePanelState.result` supports:
- `overviewSummary`
- `fixPlan`
- optional `crossPageMatrix`

and that fix-plan items carry `sourceFindingIds`.

- [ ] **Step 2: Run the focused payload test and verify it fails**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/scoreResultPayload.test.ts`

Expected: FAIL because the new payload fields do not exist yet.

- [ ] **Step 3: Add contract types**

Add frontend-facing interfaces for:
- `OverviewSummary`
- `OverviewInsight`
- `OverviewAction`
- `SeverityDistribution`
- `CrossPageSummary`
- `FixPlanItem`
- optional `CrossPageMatrix` and `CrossPageMatrixCell`

Keep these presentation-only.

- [ ] **Step 4: Re-run the focused payload test**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/scoreResultPayload.test.ts`

Expected: still FAIL on missing builder output, but type-level changes compile.

### Task 2: Build the Overview summary builder with TDD

**Files:**
- Create: `vscode-extension/src/analyzer/score/overviewSummary.ts`
- Create: `vscode-extension/src/test/overviewSummary.test.ts`

- [ ] **Step 1: Write failing tests for deterministic Overview derivation**

Cover:
- maturity band derivation from existing score + issue pressure
- risk band derivation from finding severity distribution
- top strengths / weaknesses rollups
- top issues / top actions ordering
- cross-page summary language
- benchmark summary language

- [ ] **Step 2: Run the focused overview test and verify it fails**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/overviewSummary.test.ts`

Expected: FAIL because `buildOverviewSummary()` does not exist.

- [ ] **Step 3: Implement `buildOverviewSummary()`**

Use only:
- `ScoreResult`
- normalized findings
- report consistency summary
- benchmark comparison
- page scores

No new scoring logic. Make label derivation deterministic and traceable.

- [ ] **Step 4: Re-run the focused overview test**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/overviewSummary.test.ts`

Expected: PASS

### Task 3: Build the Fix Plan remediation queue with TDD

**Files:**
- Create: `vscode-extension/src/analyzer/score/fixPlan.ts`
- Create: `vscode-extension/src/test/fixPlan.test.ts`

- [ ] **Step 1: Write failing tests for remediation queue derivation**

Cover:
- priority ordering by severity/confidence/scope
- deterministic effort labels
- affected page rollups
- source finding references
- consultant-friendly action phrasing

- [ ] **Step 2: Run the focused fix-plan test and verify it fails**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/fixPlan.test.ts`

Expected: FAIL because `buildFixPlan()` does not exist.

- [ ] **Step 3: Implement `buildFixPlan()`**

Derive from:
- normalized findings
- quick fixes
- recommendations

Ensure each item includes `sourceFindingIds`.

- [ ] **Step 4: Re-run the focused fix-plan test**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/fixPlan.test.ts`

Expected: PASS

### Task 4: Add optional matrix and persona helpers only if data stays clean

**Files:**
- Create if used: `vscode-extension/src/analyzer/score/crossPageMatrix.ts`
- Create if used: `vscode-extension/src/test/crossPageMatrix.test.ts`
- Create if used: `vscode-extension/src/analyzer/score/personaOrdering.ts`
- Create if used: `vscode-extension/src/test/personaOrdering.test.ts`

- [ ] **Step 1: Evaluate whether matrix data maps cleanly from existing signals**

Use:
- page scores
- page intent/actionability/benchmark summaries
- report consistency notes
- normalized findings per page

If the mapping is noisy or requires pseudo-scores, skip the matrix.

- [ ] **Step 2: If clean, write a failing matrix test**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/crossPageMatrix.test.ts`

Expected: FAIL

- [ ] **Step 3: Implement the minimal matrix builder if justified**

Keep it presentation-only and navigational.

- [ ] **Step 4: Evaluate persona-aware ordering**

If it can be implemented as a pure presentation reorder over existing findings and overview/fix-plan items, add it. If not, defer it.

### Task 5: Thread presentation summaries into the score payload

**Files:**
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] **Step 1: Extend the failing payload tests with overview/fix-plan assertions**

Assert that payload generation includes:
- `overviewSummary`
- `fixPlan`
- optional `crossPageMatrix` only when builder emits it

- [ ] **Step 2: Implement payload wiring**

Call:
- `buildOverviewSummary()`
- `buildFixPlan()`
- optional matrix/persona helpers

Keep payload compatibility intact.

- [ ] **Step 3: Re-run focused payload tests**

Run: `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/scoreResultPayload.test.ts`

Expected: PASS

### Task 6: Rebuild the webview into the full review workspace

**Files:**
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] **Step 1: Add failing webview tests for workspace IA**

Cover:
- Overview renders before Issues
- Overview shows maturity/risk/top issues/top actions/severity distribution
- Issues supports filters and grouping controls
- Fix Plan renders remediation queue items
- Evidence remains collapsed by default
- packet preview is demoted under Evidence or secondary export area
- Export remains accessible but secondary

- [ ] **Step 2: Run the focused webview test and verify it fails**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: FAIL on the new workspace expectations.

- [ ] **Step 3: Implement the workspace shell around existing content**

Reorganize the render order:
- hero
- Overview
- Issues
- Fix Plan
- Evidence
- Export actions / secondary export area

Add:
- issue filter state
- grouping controls
- smart-collapse defaults
- secondary export action area

- [ ] **Step 4: Re-run the focused webview test**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: PASS

### Task 7: Run focused and full validation

**Files:**
- No additional file edits required

- [ ] **Step 1: Run focused extension tests**

Run:
- `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/overviewSummary.test.ts src/test/fixPlan.test.ts src/test/scoreResultPayload.test.ts`
- add optional matrix/persona tests if implemented

Expected: PASS

- [ ] **Step 2: Run focused webview tests**

Run: `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`

Expected: PASS

- [ ] **Step 3: Compile the extension**

Run: `cd vscode-extension && npm run compile`

Expected: PASS

- [ ] **Step 4: Run the full extension test suite**

Run: `cd vscode-extension && npm test`

Expected: PASS

- [ ] **Step 5: Run the full backend suite**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release`

Expected: PASS

### Task 8: Record decisions, deferrals, and residual risks

**Files:**
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-05-31-<time>-full-workspace-modernization.md`

- [ ] **Step 1: Record implementation decisions**

Document:
- overview remains presentation-only
- export remains secondary
- whether matrix shipped or was deferred
- whether persona ordering shipped or was deferred

- [ ] **Step 2: Record residual risks**

Capture:
- any heuristics used for maturity/risk or effort bands
- any matrix limitations
- any remaining UI density or follow-on polish items
