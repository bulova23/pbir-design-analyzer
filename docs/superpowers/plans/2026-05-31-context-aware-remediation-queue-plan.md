# Context-Aware Remediation Queue Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the remediation queue respond to the selected problem area while staying broader and more stable than the exact filtered issue list.

**Architecture:** Keep `Issues` fully filter-driven and diagnostic. Add a presentation-layer remediation-focus model so `Fix Plan` derives from `Page`, `Dimension`, and `Impact`, while treating `Severity`, `Scope`, and `Detection` as diagnostic-only inputs that may influence priority or explanation but do not fully constrain remediation generation.

**Tech Stack:** TypeScript, React, Jest, current score-panel payload builders, VS Code webview UI

---

## Major Workstreams

### Task 1: Add Remediation-Focus Contracts
- [ ] Define presentation-layer contract types for:
  - remediation focus
  - remediation source summary
  - queue derivation metadata
- [ ] Keep these contracts downstream from normalized findings and existing score payload semantics.

### Task 2: Build A Context-Aware Remediation Derivation Layer
- [ ] Add a pure builder that accepts:
  - active issue filters
  - visible findings
  - broader finding pool
  - selected page context
- [ ] Drive queue generation from:
  - `Page`
  - `Dimension`
  - `Impact`
- [ ] Treat:
  - `Severity`
  - `Scope`
  - `Detection`
  as diagnostic-only for generation purposes.

### Task 3: Preserve Severity Influence Without Hard-Pruning
- [ ] Use severity to influence ordering, badges, and queue explanations.
- [ ] Allow related medium/low actions to remain in the queue when they support the same remediation theme as visible high-severity findings.
- [ ] Add explicit resolved-finding severity summaries such as:
  - `1 High · 2 Medium`

### Task 4: Surface Remediation Scope In The Webview
- [ ] Replace the generic Fix Plan heading copy with a clearer scope model:
  - `Remediation Focus: <Page> · <Dimension>`
- [ ] Add helper text explaining that:
  - actions are grouped by problem area
  - diagnostic filters affect Issues more strictly than remediation
- [ ] Keep the explanation compact by default.

### Task 5: Thread Matrix And Filter Context Into The Queue
- [ ] Ensure matrix clicks and page-review context update remediation focus automatically.
- [ ] Ensure changing `Page`, `Dimension`, or `Impact` changes the queue.
- [ ] Ensure changing `Severity`, `Scope`, or `Detection` does not fully rebuild the queue into a different problem domain.

### Task 6: Validation
- [ ] Add builder tests for remediation-driving vs diagnostic-only filter behavior.
- [ ] Add UI tests for remediation scope messaging and visible-vs-source explanation.
- [ ] Add regression tests proving score, severity, and confidence semantics remain unchanged.

## Non-Goals

- no AI remediation implementation yet
- no scoring changes
- no normalized-finding redesign
- no persona redesign
- no issue-filter redesign
