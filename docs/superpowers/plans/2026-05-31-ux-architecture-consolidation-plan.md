# UX Architecture Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Consolidate the score-panel workspace UX so diagnosis, remediation, page-purpose reasoning, and matrix navigation are clearer without changing scoring or normalized-finding semantics.

**Architecture:** Keep scoring, severity, confidence, and normalized findings unchanged. Refactor the presentation layer so page-purpose inputs are summarized through one parent container, fix-plan items become grouped remediation actions with short action-specific rationale, and the matrix adapts between report and page contexts using status-first labels.

**Tech Stack:** TypeScript, React, Jest, current score-panel payload builders, VS Code webview UI

---

## Major Workstreams

### Task 1: Stabilize The UX-Consolidation Contracts
- [ ] Identify the existing payload objects that already cover page-purpose reasoning, fix-plan traceability, and matrix status data.
- [ ] Add or adjust only the presentation-layer contract fields needed for:
  - Page Purpose Analysis summary state
  - remediation-item `impact`, `why`, and resolved-outcome summaries
  - matrix view mode support
- [ ] Keep all changes downstream from `ScoreResult` and `normalizedFindings`.

### Task 2: Build Page Purpose Analysis As A Summary-First Container
- [ ] Reuse current inputs from inferred story, intent profile, actionability, benchmark, and intent feedback.
- [ ] Add a presentation builder that emits:
  - default summary content
  - `Why This Matters` narrative
  - expandable detailed sections
- [ ] Preserve all existing override and feedback behavior when the full reasoning view is expanded.

### Task 3: Convert Fix Plan Into A True Remediation Queue
- [ ] Refactor the fix-plan presentation builder so the primary unit is a grouped action rather than a near-duplicate finding row.
- [ ] Add deterministic presentation-only fields for:
  - `impact`
  - `effort`
  - short action-specific `why`
  - resolved outcomes
  - source-finding traceability
- [ ] Keep the queue explainable from existing findings and recommendations.

### Task 4: Make The Matrix Context-Aware And Qualitative-First
- [ ] Preserve the current report-level matrix behavior in Overview.
- [ ] Add page-level matrix rendering for selected-page review mode.
- [ ] Make status labels primary:
  - `Strong`
  - `Watch`
  - `Weak`
  - `Unknown`
- [ ] Keep finding counts as supporting metadata rather than the lead signal.

### Task 5: Integrate The New Reading Path In The Webview
- [ ] Update the page-review layout so the first-scan order becomes:
  - Overview summary
  - Page Purpose Analysis
  - Issues
  - Fix Plan
- [ ] Preserve Evidence and Export as secondary workflows.
- [ ] Ensure matrix drill-ins still land users in Issues with the right context.

### Task 6: Preserve Boundaries And Regression Safety
- [ ] Add regression tests proving there are no scoring, severity, or confidence changes.
- [ ] Add focused tests for:
  - summary vs expanded Page Purpose Analysis
  - remediation grouping and traceability
  - context-aware matrix rendering
  - status-first matrix labels
  - Issues vs Fix Plan differentiation
- [ ] Validate the existing intent feedback and page-review flows still work.

## Non-Goals

- no backend scoring work
- no normalized-finding redesign
- no persona redesign
- no export redesign
- no analytics additions
