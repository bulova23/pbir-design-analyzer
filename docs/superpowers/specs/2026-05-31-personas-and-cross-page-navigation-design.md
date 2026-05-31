# Reviewer Personas And Cross-Page Navigation Design

Date: 2026-05-31

Status: Drafted from the current implemented workspace architecture; ready for review

## Goal

Add the next two workspace enhancements on top of the completed `Overview -> Issues -> Fix Plan -> Evidence -> Export` architecture:

1. reviewer personas as presentation modes
2. cross-page matrix navigation as a report-level triage and drill-in surface

These enhancements must remain presentation-only. They may reorder, summarize, filter, and emphasize existing findings, but they may not change scores, severities, confidences, or scoring logic.

## Review Of The Request Against The Current Implementation

The request is directionally correct, but part of it overlaps with partial implementations that already exist in the codebase.

### What Already Exists

- The score panel already has a first-class `normalizedFindings` model and an Issues-first workspace.
- `OverviewSummary` and `FixPlanItem` already exist as presentation-only derived objects.
- `crossPageMatrix` already exists in the score payload and Overview UI.
- A `ReviewerPersona` type already exists, but it currently supports only the page-level `Reviewer Comment Generator`.

### Gaps Relative To The Request

#### 1. Persona Naming And Responsibilities Conflict

The current `ReviewerPersona` contract in [scorePanel.ts](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension/src/analyzer/contracts/scorePanel.ts:25) is:

- `coach`
- `consultant`
- `executiveReviewer`
- `strictDesignCritic`

Those values are used only by [reviewerComments.ts](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension/src/analyzer/score/reviewerComments.ts:1) for tone generation. They do not match the requested review-workspace personas:

- `default`
- `executive`
- `consultant`
- `governance`
- `accessibility`

If the next enhancement simply reuses `ReviewerPersona`, the codebase will conflate:

- page-comment tone
- workspace prioritization mode

That is the wrong boundary.

#### 2. The Existing Cross-Page Matrix Is Too Lightweight

The current matrix builder in [crossPageMatrix.ts](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension/src/analyzer/score/crossPageMatrix.ts:1) emits only:

- page name
- row label
- highest severity
- finding count

It does not provide:

- actionability as a first-class dimension
- status labels such as `strong`, `watch`, `weak`, `unknown`
- high-severity counts
- confidence averages
- related finding IDs
- summaries/tooltips
- click-to-filter navigation semantics

The current Overview matrix is therefore a visual indicator, not a navigation aid.

#### 3. Overview And Fix Plan Are Not Persona-Aware

The current overview and fix-plan builders in [overviewSummary.ts](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension/src/analyzer/score/overviewSummary.ts:1) and [fixPlan.ts](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension/src/analyzer/score/fixPlan.ts:1) emit one deterministic ordering. They do not support:

- persona-specific top issues
- persona-specific top actions
- persona-specific fix-plan ordering
- persona-specific evidence emphasis

#### 4. Issues Already Has Filters, But No Cross-Surface Driver

The Issues workspace already has filter state in [App.tsx](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension/webview-src/analyzer-score/App.tsx:44), but nothing upstream currently drives those filters from:

- an overview persona mode
- a matrix cell click

That makes this enhancement an integration task more than a greenfield filtering task.

## Design Principles

### 1. Personas Are Presentation Modes, Not Reviewer Types

The new workspace personas must be modeled separately from the existing page-comment persona system. Workspace personas control:

- ordering
- emphasis
- default filters
- summary phrasing priorities

They do not control comment-generator voice.

### 2. Matrix Data Is Navigation Data, Not Scoring Data

The matrix should summarize existing page/dimension signals and connect users into Issues. It is not a new scoring surface and it should not invent hidden per-cell scores when the underlying data is only finding-based.

### 3. Keep Derivation Logic Out Of React

Persona ordering, matrix derivation, top-issue selection, and export-summary emphasis should live in pure TypeScript helpers under the score/presentation layer. React should render and hold interaction state, not invent prioritization logic inside JSX.

### 4. Preserve Existing Review Depth

Evidence, packet preview, framework analysis, and reviewer comment generation remain intact. These enhancements change the route to that detail, not the availability of that detail.

## Architecture And Data Flow

The implementation should keep three distinct layers:

### A. Authoritative Score State

- `ScoreResult`
- page scores
- framework feedback
- normalized findings
- overview summary
- fix plan
- existing review packet/export state

These remain authoritative and unchanged in meaning.

### B. Presentation Adapters

Add pure helpers for:

- persona-aware ordering and emphasis
- matrix summary derivation
- optional export-summary emphasis

Recommended flow:

`ScoreResult`
`+ normalized findings`
`+ overview summary`
`+ fix plan`
`-> persona presentation adapter`
`-> matrix builder`
`-> score panel payload / UI state`
`-> React workspace`

### C. React Interaction State

React should own only:

- active workspace persona
- active Issues filters
- active grouping mode
- selected matrix cell
- scroll/focus transitions

React should not determine what “executive-first” or “governance-first” means.

## Contract Changes

### 1. Add A New Workspace Persona Contract

Do not reuse the current `ReviewerPersona` union.

Recommended new contract:

```ts
export type ReviewPresentationPersona =
  | 'default'
  | 'executive'
  | 'consultant'
  | 'governance'
  | 'accessibility';

export interface ReviewPresentationPersonaProfile {
  id: ReviewPresentationPersona;
  label: string;
  description: string;
  emphasizedImpactAreas: NormalizedFindingImpactArea[];
  emphasizedScopes: NormalizedFindingScope[];
  defaultSeverityFilter?: NormalizedFindingSeverity[];
  defaultDetectionTypes?: NormalizedFindingDetectionType[];
  overviewEmphasis: Array<'issues' | 'actions' | 'strengths' | 'weaknesses' | 'benchmark' | 'consistency'>;
  fixPlanEmphasis: Array<'severity' | 'effort' | 'scope' | 'evidence' | 'crossPage'>;
}
```

This avoids breaking the existing reviewer-comment generator. If desired later, the old comment-persona contract can be renamed separately, but that rename is not required for this enhancement.

### 2. Replace The Existing Matrix Contract With A Richer Navigation Model

Recommended contract:

```ts
export type CrossPageMatrixDimension =
  | 'layout'
  | 'story'
  | 'accessibility'
  | 'consistency'
  | 'navigation'
  | 'actionability';

export interface CrossPageMatrixCell {
  pageName: string;
  dimension: CrossPageMatrixDimension;
  score?: number;
  severity?: NormalizedFindingSeverity;
  findingCount: number;
  highSeverityCount: number;
  confidenceAverage?: number;
  status: 'strong' | 'watch' | 'weak' | 'unknown';
  relatedFindingIds: string[];
  summary: string;
}

export interface CrossPageMatrixRow {
  pageName: string;
  cells: CrossPageMatrixCell[];
}

export interface CrossPageMatrixSummary {
  dimensions: CrossPageMatrixDimension[];
  rows: CrossPageMatrixRow[];
}
```

Important note: the current matrix is area-rows by page-columns. The new request is page-rows by dimension-cells. The spec should treat this as a replacement of the existing presentation contract, not a small additive tweak.

### 3. Add Persona-Aware Presentation Output Types

Recommended payload additions:

```ts
export interface PersonaPresentationState {
  activePersona: ReviewPresentationPersona;
  availablePersonas: ReviewPresentationPersonaProfile[];
}
```

The underlying `overviewSummary`, `fixPlan`, and `normalizedFindings` remain base-state objects. Persona mode is applied on top of them.

## Persona Presentation Adapter

Create a dedicated adapter:

- `vscode-extension/src/analyzer/score/personaPresentation.ts`

Responsibilities:

- provide built-in persona profiles
- sort findings without mutating them
- select top issues and top actions by persona
- reorder fix-plan items by persona
- derive recommended default filters by persona
- expose persona-specific evidence emphasis metadata

### Ordering Rules

#### Default

1. severity
2. confidence
3. scope
4. stable original order

#### Executive

1. high severity
2. actionability
3. KPI effectiveness
4. storytelling
5. benchmark
6. cross-page issues that reduce executive readability

#### Consultant

1. high severity
2. remediation clarity
3. evidence-backed findings
4. multi-page issues
5. effort-aware fix sequencing

#### Governance

1. cross-page scope
2. governance and metadata impact
3. naming and semantic-color consistency
4. layout and navigation drift
5. remaining severity order

#### Accessibility

1. accessibility impact
2. likely WCAG-related findings
3. contrast and color-encoding findings
4. readability and navigation
5. remaining severity order

### Important Constraint

The adapter may return reordered arrays and emphasis hints, but it may not rewrite:

- `severity`
- `confidence`
- `recommendation`
- `frameworkImpact`

## Cross-Page Matrix Builder

Create or replace:

- `vscode-extension/src/analyzer/score/crossPageMatrix.ts`

Responsibilities:

- map normalized findings into matrix dimensions
- attach related finding IDs
- compute count and high-severity count
- compute average confidence when findings exist
- derive deterministic `status`
- emit safe `unknown` cells when page/dimension coverage is missing

### Dimension Mapping

| Finding Impact Area | Matrix Dimension |
|---|---|
| `layout` | `layout` |
| `density` | `layout` |
| `storytelling` | `story` |
| `kpiEffectiveness` | `story` |
| `benchmark` | `story` by default, unless the recommendation is explicitly action-oriented |
| `governance` | `consistency` |
| `metadata` | `consistency` |
| `navigation` | `navigation` |
| `actionability` | `actionability` |
| `accessibility` | `accessibility` |

For this pass, keep the mapping deterministic and coarse. Do not infer extra categories from prose.

### Status Rules

#### `weak`

- at least one high-severity finding exists for the page/dimension, or
- multiple medium findings exist, or
- a cross-page finding affects the page and dimension materially

#### `watch`

- at least one medium or low finding exists, or
- evidence exists but is mixed

#### `strong`

- no high or medium findings exist for that page/dimension
- and there is enough underlying page coverage to avoid `unknown`

#### `unknown`

- no page-specific or cross-page evidence maps cleanly to that cell

## Workspace Information Architecture

### Overview

Add a persona selector to the Overview header.

Required behavior:

- visible near the existing executive summary header
- helper copy explains presentation-only behavior
- changing persona updates:
  - top issues
  - top actions
  - issue ordering
  - default Issues filters
  - Fix Plan ordering
  - Evidence callout emphasis

Recommended helper text:

> Review modes change how findings are prioritized and explained. They do not change the underlying score.

Place the richer cross-page matrix after the decision snapshot and before lower-priority overview detail.

### Issues

Keep Issues as the primary working surface.

Enhance it so:

- persona changes can seed default filters without permanently overriding manual user choices
- matrix clicks can set page + dimension filters
- a small active-filter summary explains why the current list is filtered

Recommended interaction rule:

- persona change applies recommended defaults only when the current filters are still untouched or when the user explicitly resets to persona defaults

This avoids surprising users by wiping custom filters every time they change modes.

### Fix Plan

Apply persona-aware ordering to the existing remediation queue.

Examples:

- `consultant` emphasizes severity, effort clarity, and evidence-backed items
- `governance` moves cross-page standardization work up
- `executive` emphasizes high business-impact and actionability-linked items

Do not generate a different set of fix-plan items unless the base queue is empty. Reorder and relabel emphasis only.

### Evidence

Keep Evidence secondary.

Enhance discoverability from personas by adding low-emphasis callout copy such as:

- executive: “Review supporting evidence for narrative and KPI issues.”
- governance: “Review consistency evidence across naming, color, and layout patterns.”

This is an emphasis cue, not a new evidence model.

### Export

Keep Export downstream and secondary.

If export wording emphasis is implemented in this pass, it should be limited to:

- choosing persona-aware wording for already-derived review summaries

Do not redesign export UX or packet content structure here.

## Component And File Changes

### Contracts

- Modify [scorePanel.ts](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension/src/analyzer/contracts/scorePanel.ts)
  - add `ReviewPresentationPersona`
  - add `ReviewPresentationPersonaProfile`
  - add `PersonaPresentationState`
  - replace/upgrade the current cross-page matrix contract to `CrossPageMatrixSummary`

### Presentation Builders

- Create `vscode-extension/src/analyzer/score/personaPresentation.ts`
- Replace or substantially rewrite `vscode-extension/src/analyzer/score/crossPageMatrix.ts`
- Modify `vscode-extension/src/analyzer/score/overviewSummary.ts` only if it needs to expose base-state hooks for persona reordering, not persona logic itself
- Modify `vscode-extension/src/analyzer/score/fixPlan.ts` only if it needs stable metadata the persona adapter can consume

### Payload Shaping

- Modify `vscode-extension/src/views/scoreResultPayload.ts`
  - emit richer matrix summary
  - emit persona availability metadata
  - keep existing payload compatibility for missing persona/matrix data

### Webview

- Modify [App.tsx](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension/webview-src/analyzer-score/App.tsx)
  - add top-level review-mode selector
  - apply persona-aware ordering and defaults
  - wire matrix click-to-filter behavior
  - preserve manual filter overrides

- Modify [styles.css](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension/webview-src/analyzer-score/styles.css)
  - add segmented or compact select styles
  - add matrix-cell button styling
  - add active-filter summary styles

## Optional Enhancements And Deferrals

### In Scope If Low-Risk

- persona-aware export-summary wording emphasis
- confidence average on matrix cells
- matrix tooltip/summary labels
- “reset to persona defaults” action for Issues filters

### Explicitly Deferred

- score changes based on persona
- new benchmark calculations
- screenshot overlays
- AI-generated commentary
- export redesign
- configuration workspace redesign
- new backend scoring fields purely for persona or matrix support

## Testing And Validation Plan

Add or update focused coverage for:

### Persona Adapter

- executive prioritizes actionability/storytelling
- consultant prioritizes remediation-ready items
- governance prioritizes cross-page/governance findings
- accessibility prioritizes accessibility findings
- persona sorting does not mutate finding severity or confidence

### Matrix Builder

- impact areas map to expected dimensions
- high severity yields `weak`
- no clean data yields `unknown`
- cross-page findings affect each impacted page
- related finding IDs are preserved

### Payload

- richer matrix payload is emitted safely
- persona profiles are emitted safely
- missing matrix data does not break payload generation

### Webview

- persona selector renders
- helper text explains presentation-only behavior
- persona change reorders Issues and Fix Plan presentation
- clicking a matrix cell filters Issues by page + dimension
- score display does not change when persona changes
- active persona is preserved when matrix filters are applied

### Validation Commands

- `cd vscode-extension && npx jest -c jest.config.cjs --runTestsByPath src/test/personaPresentation.test.ts src/test/crossPageMatrix.test.ts src/test/scoreResultPayload.test.ts`
- `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/analyzer-score/App.test.tsx`
- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`

## Non-Goals

This enhancement does not:

- change scoring algorithms
- mutate score outputs
- change finding severity or confidence
- add large charting dependencies
- redesign export
- add screenshot overlays
- write anything back to PBIR

## Recommendation

Proceed with both enhancements, but treat them as extension-layer presentation work built on the existing workspace rather than fresh greenfield features.

The most important implementation constraint is this:

- add a new workspace-persona model instead of overloading the current reviewer-comment persona type
- replace the lightweight matrix with a navigation-aware summary model instead of trying to stretch the current count grid incrementally

That keeps the architecture coherent and prevents two small legacy abstractions from becoming long-term UI contracts.
