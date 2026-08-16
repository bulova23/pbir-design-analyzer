# Context-Aware Remediation Queue Design

Date: 2026-05-31

Status: Approved design direction captured; ready for implementation planning

## Goal

Refine the post-`0.2.1` score-panel workspace so the remediation layer feels dynamically connected to the current review context without collapsing back into a second copy of `Issues`.

This follow-up keeps the diagnosis/remediation separation introduced in the UX Architecture Consolidation Epic, but strengthens the connection between them:

- `Issues` remains the exact filtered diagnosis view
- `Fix Plan` becomes a context-aware remediation queue derived from the selected problem area

## Release Intent

This work is a `0.2.2` follow-up. It should not be folded back into the `0.2.1` spec set.

The purpose is to improve trust and workflow continuity after the `0.2.1` UX consolidation release:

- users should understand why the remediation queue is broader than the visible issue list
- users should see the queue update when they change the problem domain being reviewed
- the remediation layer should become the future insertion point for AI-assisted fixes

## Problem Statement

The current `0.2.1` separation between `Issues` and `Fix Plan` is architecturally correct, but the workflow still feels partially disconnected.

### What Works Now

- `Issues` is clearly a diagnosis layer
- `Fix Plan` is clearly a remediation layer
- remediation items are grouped actions rather than raw finding copies

### What Still Feels Wrong

The current queue still appears too static relative to the issue set in view.

Example:

- the user filters to `Page = Customer Analysis`
- then changes `Dimension = Layout`
- then changes to `Dimension = Story`

They expect the remediation queue to change with the selected problem area.

If the queue remains too general, the user reads it as disconnected from the visible review context.

## Design Principle

`Issues` and `Fix Plan` should not respond to filters in exactly the same way.

They answer different questions:

- `Issues`: What problems match my current filters?
- `Fix Plan`: What actions best improve the selected problem area?

That difference is intentional and should be visible in the UI and data flow.

## Target Behavior

### 1. Two Filter Classes

The workspace should treat issue filters in two different classes.

#### Remediation-Driving Filters

These define the problem domain being reviewed and should drive queue generation:

- `Page`
- `Dimension`
- `Impact`

These filters should strongly affect which remediation actions appear.

#### Diagnostic-Only Filters

These refine the visible diagnosis list, but should not fully reshape the queue:

- `Severity`
- `Scope`
- `Detection`

These may influence remediation ordering, highlighting, and explanation, but should not act as hard inclusion/exclusion gates for queue generation.

### 2. Stable Remediation Scope

If the user selects:

- `Page = Customer Analysis`
- `Dimension = Layout`

then the remediation queue should focus on layout-oriented actions such as:

- reduce visual density
- improve alignment
- simplify page layout

If the user selects:

- `Page = Customer Analysis`
- `Dimension = Story`

then the queue should switch to story-oriented actions such as:

- add page purpose anchor
- improve narrative hierarchy
- add benchmarks and decision context

This is the core context-awareness requirement.

### 3. Severity Is Informative, Not Absolute

If `Severity = High` is active in `Issues`, the queue should not collapse to only actions that map to high-severity findings.

Instead:

- high-severity findings should influence priority
- the queue may still include related medium or low actions that belong to the same remediation theme

Example:

Visible Issues:

- `Visual Density` (`High`)

Related but filtered-out findings:

- `Alignment` (`Medium`)
- `Grouping` (`Low`)

The queue may still include:

1. reduce visual density
2. align visual grid
3. improve grouping

because those actions together improve the selected problem area.

## UX Model

### Header And Scope Communication

The queue should explicitly communicate what drives it.

Recommended header:

- `Remediation Focus: Customer Analysis · Layout`

Recommended helper line:

- `Actions are grouped by problem area rather than individual findings.`

Expanded helper text may be used when helpful:

- `This remediation queue is generated from the selected page and problem area. Diagnostic filters such as Severity, Scope, and Detection affect Issues but do not fully constrain remediation recommendations.`

### Severity-Crossing Explanation

If queue items resolve findings outside the currently visible severity slice, the UI should say so explicitly.

Example:

- `Resolves: 1 High · 2 Medium`

This is not optional helper chrome. It is part of the remediation architecture because it explains why the queue is broader than the visible issue list.

## Data And Derivation Model

### Remediation Context

Add a dedicated presentation-layer concept for the queue:

- remediation focus
- remediation source summary
- action set derived from the selected problem domain

Recommended shape:

```ts
export interface RemediationFocus {
  pageName?: string;
  dimension?: CrossPageMatrixDimension;
  impactArea?: NormalizedFindingImpactArea;
}

export interface RemediationSourceSummary {
  visibleFindingCount: number;
  sourceFindingCount: number;
  severityCounts: {
    high: number;
    medium: number;
    low: number;
    info: number;
  };
}
```

The important distinction:

- `visibleFindingCount` = exact issue count after all issue filters
- `sourceFindingCount` = broader finding pool used to generate remediation actions

### Queue Generation Rules

The queue should derive from:

1. current page context
2. current dimension context
3. current impact context
4. visible normalized findings
5. related normalized findings in the same problem area, even if not visible due to diagnostic-only filters

Hard rule:

- queue generation must remain deterministic and presentation-only

No scoring, severity, or confidence semantics may change.

### Recommended Derivation Strategy

1. Build the exact `Issues` subset from all active filters.
2. Extract the remediation-driving context from `Page`, `Dimension`, and `Impact`.
3. Expand from the visible subset to a broader same-context finding pool.
4. Generate grouped remediation actions from that broader pool.
5. Order actions using severity density and impact, while retaining medium/low related actions when they support the same remediation theme.

## Relationship To AI-Assisted Fixes

This queue is the correct future insertion point for AI remediation.

Not recommended:

- putting AI apply/preview buttons directly on every issue card

Recommended:

- the issue layer remains diagnostic
- the remediation queue becomes the unit of AI suggestion, preview, and eventual apply workflow

Future examples:

- `Preview AI Fix`
- `Generate remediation checklist`
- `Apply safe metadata-only remediation`

This design should preserve that path without implementing AI now.

## Architecture Boundaries

Preserve:

- scoring architecture
- normalized findings
- persona system
- matrix navigation foundation
- existing payload semantics for score, severity, and confidence

Do not change:

- scoring algorithms
- benchmark calculations
- severity/confidence values
- existing issue filter semantics

This is a presentation-layer follow-up only.

## Test Strategy

- queue derivation tests proving `Page`, `Dimension`, and `Impact` drive action generation
- regression tests proving `Severity`, `Scope`, and `Detection` do not hard-prune the queue
- UI tests for `Remediation Focus` header and helper copy
- tests for visible-vs-source finding summary and severity-crossing explanation
- regression tests confirming scores, severity, and confidence remain unchanged

## Outcome

When complete:

- `Issues` will feel exact and diagnostic
- `Fix Plan` will feel broader, steadier, and more trustworthy
- users will understand why the two layers differ
- the remediation layer will be a stronger foundation for future AI-assisted fix workflows
