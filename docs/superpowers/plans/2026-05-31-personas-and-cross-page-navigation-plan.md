# Reviewer Personas And Cross-Page Navigation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add workspace-level reviewer personas and a navigation-aware cross-page matrix on top of the modernized score panel without changing any underlying scoring, severity, or confidence logic.

**Architecture:** Keep the existing scoring and normalized-finding layers unchanged. Add two pure presentation adapters in the extension layer: a persona presentation adapter that reorders and emphasizes existing findings, overview items, and fix-plan items; and a richer matrix builder that converts normalized findings into page-by-dimension navigation cells that drive Issues filters.

**Tech Stack:** TypeScript, React, Jest, CSS, VS Code webview UI

---

## File Structure

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - Add a new workspace-persona contract and replace the lightweight matrix contract with a richer navigation model.
- Create: `vscode-extension/src/analyzer/score/personaPresentation.ts`
  - Build persona profiles, ordering rules, and presentation-only default filter recommendations.
- Modify: `vscode-extension/src/analyzer/score/crossPageMatrix.ts`
  - Replace the current count-grid builder with a page-by-dimension navigation summary builder.
- Modify: `vscode-extension/src/analyzer/score/overviewSummary.ts`
  - Expose only the base overview state needed for persona-aware reordering if current assumptions block it.
- Modify: `vscode-extension/src/analyzer/score/fixPlan.ts`
  - Preserve stable queue metadata needed for persona-aware reordering.
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
  - Emit persona profiles and the richer matrix summary without changing existing score semantics.
- Create: `vscode-extension/src/test/personaPresentation.test.ts`
  - Cover persona ordering and presentation-only guarantees.
- Modify: `vscode-extension/src/test/crossPageMatrix.test.ts`
  - Replace the current lightweight expectations with navigation-aware matrix coverage.
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
  - Assert persona metadata and richer matrix payload support.
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
  - Add persona selection, matrix click-to-filter behavior, and coordinated Issues/Fix Plan ordering.
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
  - Add persona selector, active-filter summary, and clickable matrix styles.
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
  - Cover persona rendering, persona-aware ordering, and matrix-driven filtering.

## Phase 1: Stabilize Contracts Around The New Enhancement Slice

### Task 1: Split Workspace Personas From Reviewer-Comment Personas

**Files:**
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Test: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] Add a failing payload test that expects a new workspace persona type and persona profiles to exist without removing the legacy reviewer-comment persona support.
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/scoreResultPayload.test.ts` and confirm the new persona fields are missing.
- [ ] Add `ReviewPresentationPersona`, `ReviewPresentationPersonaProfile`, and `PersonaPresentationState` to the contract layer.
- [ ] Keep the existing comment-generator persona type intact in this phase, even if it is renamed later.
- [ ] Re-run the payload test and confirm type-level changes compile while payload shaping still fails.

### Task 2: Replace The Matrix Contract With A Navigation Model

**Files:**
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Test: `vscode-extension/src/test/crossPageMatrix.test.ts`

- [ ] Add a failing contract-oriented test for page-row matrix data with:
  - dimensions
  - status
  - finding count
  - high-severity count
  - related finding IDs
  - summary text
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/crossPageMatrix.test.ts` and confirm the current lightweight matrix shape no longer satisfies the expected payload.
- [ ] Replace the old `CrossPageMatrix` shape with `CrossPageMatrixSummary`, `CrossPageMatrixRow`, `CrossPageMatrixCell`, and `CrossPageMatrixDimension`.
- [ ] Re-run the focused matrix test and confirm the contract now compiles while the builder still fails.

## Phase 2: Build Pure Presentation Adapters

### Task 3: Implement The Persona Presentation Adapter With TDD

**Files:**
- Create: `vscode-extension/src/analyzer/score/personaPresentation.ts`
- Create: `vscode-extension/src/test/personaPresentation.test.ts`

- [ ] Write failing tests for:
  - default ordering
  - executive prioritizing actionability/storytelling
  - consultant prioritizing remediation-ready multi-page items
  - governance prioritizing cross-page/governance consistency findings
  - accessibility prioritizing accessibility findings
  - no mutation of score, severity, or confidence values
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/personaPresentation.test.ts` and confirm the adapter does not exist.
- [ ] Implement:
  - built-in persona profiles
  - stable presentation ordering
  - recommended default filters
  - top-issues and top-actions selection helpers
  - fix-plan reordering helpers
- [ ] Re-run the focused persona test until it passes.

### Task 4: Replace The Matrix Builder With Navigation-Aware Derivation

**Files:**
- Modify: `vscode-extension/src/analyzer/score/crossPageMatrix.ts`
- Modify: `vscode-extension/src/test/crossPageMatrix.test.ts`

- [ ] Expand the failing matrix test to cover:
  - impact-area to dimension mapping
  - `weak`, `watch`, `strong`, `unknown` statuses
  - cross-page findings attaching to multiple rows
  - related finding ID preservation
  - safe handling of missing page coverage
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/crossPageMatrix.test.ts` and confirm the current builder fails the richer expectations.
- [ ] Implement a deterministic matrix builder that:
  - uses normalized findings first
  - uses page scores only for page coverage, not new scoring
  - emits page rows with dimension cells
  - emits summary text per cell
- [ ] Re-run the focused matrix test until it passes.

### Task 5: Keep Overview And Fix Plan As Base-State Builders

**Files:**
- Modify: `vscode-extension/src/analyzer/score/overviewSummary.ts`
- Modify: `vscode-extension/src/analyzer/score/fixPlan.ts`
- Test: `vscode-extension/src/test/overviewSummary.test.ts`
- Test: `vscode-extension/src/test/fixPlan.test.ts`

- [ ] Review the current builders and add failing tests only if they need extra stable metadata to support persona-aware reordering.
- [ ] Keep persona logic out of these builders; only add base-state fields if the persona adapter cannot operate safely with the current objects.
- [ ] Run focused builder tests:
  - `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/overviewSummary.test.ts src/test/fixPlan.test.ts`
- [ ] Confirm no scoring semantics changed.

## Phase 3: Thread Persona And Matrix Data Into The Payload

### Task 6: Extend Payload Generation Safely

**Files:**
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] Add failing payload tests that expect:
  - `personaPresentation` metadata
  - richer `crossPageMatrix`
  - safe omission when page coverage is insufficient
- [ ] Run `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/scoreResultPayload.test.ts` and confirm failure.
- [ ] Wire in:
  - persona profile metadata
  - active default persona
  - richer matrix summary
- [ ] Preserve compatibility when matrix data is absent.
- [ ] Re-run the focused payload test until it passes.

## Phase 4: Integrate The New Interaction Model Into The Webview

### Task 7: Add Persona Selection To Overview

**Files:**
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add failing webview tests for:
  - selector renders in Overview
  - helper text explains presentation-only behavior
  - changing persona reorders visible top issues or Fix Plan items
  - score display remains unchanged
- [ ] Run `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx` and confirm failure.
- [ ] Implement a top-level persona selector with local state for the active workspace persona.
- [ ] Apply the persona adapter outside JSX so Overview, Issues, and Fix Plan render already-ordered data.
- [ ] Re-run the focused webview test until the selector behavior passes.

### Task 8: Turn The Matrix Into A Navigation Surface

**Files:**
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add failing webview tests for:
  - matrix renders as page rows and dimension columns
  - matrix cells are clickable buttons
  - clicking a cell sets page and impact filters
  - the Issues section becomes the visible drill-in target
  - persona selection remains unchanged after matrix interaction
- [ ] Run `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx` and confirm failure.
- [ ] Implement:
  - matrix cell buttons
  - page + dimension filter mapping
  - active-filter summary
  - scroll/focus handoff into Issues
- [ ] Re-run the focused webview test until it passes.

### Task 9: Preserve Manual Filter Intent

**Files:**
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add a failing webview test proving persona changes do not unexpectedly wipe a user’s manually set filters after the user has interacted with them.
- [ ] Implement a small distinction between:
  - persona-suggested defaults
  - user-modified filters
- [ ] Add a low-friction reset path back to persona defaults if needed.
- [ ] Re-run the focused webview test until it passes.

## Phase 5: Integration Polish And Validation

### Task 10: Keep Evidence And Export Secondary

**Files:**
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] Add or update webview assertions so the new persona and matrix features do not move Evidence or Export back into the primary review path.
- [ ] Keep packet preview under Evidence or adjacent to the secondary export boundary.
- [ ] Re-run focused webview tests to confirm the main reading path remains `Overview -> Issues -> Fix Plan -> Evidence -> Export`.

### Task 11: Run Focused Validation

**Files:**
- No code changes required.

- [ ] Run:
  - `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/personaPresentation.test.ts src/test/crossPageMatrix.test.ts src/test/scoreResultPayload.test.ts`
  - `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`
  - `cd vscode-extension && npm run compile`
- [ ] Record any residual risk in repo memory if a low-value edge case remains deferred.

### Task 12: Run Full Validation

**Files:**
- No code changes required.

- [ ] Run:
  - `cd vscode-extension && npm test`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- [ ] Confirm no backend scoring changes were introduced.
- [ ] Record implementation decisions, deferrals, and residual risks in `.agent-memory/`.

## Explicit Non-Goals

- Do not change scoring algorithms.
- Do not change severity or confidence values.
- Do not redesign export.
- Do not add screenshot overlays.
- Do not add a charting library.
- Do not write anything back to PBIR.

## Recommended Execution Order

1. Stabilize contract boundaries first.
2. Build the persona adapter and matrix builder as pure helpers.
3. Thread the richer data into the payload.
4. Wire persona and matrix behavior into the webview.
5. Validate that Evidence and Export remain secondary.
6. Run full validation and update repo memory.
