# Story Assessment 2.2 Design

Date: 2026-06-12

Status: Design approved for implementation planning; no code changes in this document

## Goal

Design Story Assessment 2.2 as a workflow-acceleration release that adds:

1. Deep Link Navigation
2. Story Assessment Diff Mode

without expanding Story Assessment intelligence, exposing research-stage internals, or redesigning Story Assessment 2.1.

## Authoritative Inputs

This design uses the following as authoritative inputs:

- `docs/story-assessment/2026-06-11-level1-promotion-decision-report.md`
- `docs/superpowers/specs/2026-06-11-guided-story-improvements-design.md`
- `docs/superpowers/plans/2026-06-11-guided-story-improvements-plan.md`
- Story Assessment 2.0 validation findings
- Story Assessment 2.1 implementation results recorded in:
  - `.agent-memory/sessions/2026-06-11-190451-guided-story-improvements-implementation.md`
  - `.agent-memory/sessions/2026-06-11-211626-story-assessment-unified-narrative.md`
  - `.agent-memory/sessions/2026-06-11-213018-story-assessment-first-ui-review-refinement.md`

## Business Objective

Story Assessment 2.1 already answers:

- What We Believe This Page Is Trying To Say
- Story Maturity
- Strong Signals
- Missing Signals
- Top Story Improvements

The next release should reduce author friction, not add new story signals.

Story Assessment 2.2 should help users:

- move directly from a story recommendation to the page or visual that needs work
- understand whether their edits improved the story over time

## Non-Goals

This design does not include:

- Cross-Page Narrative Consistency
- Measure Description Mining
- Report Design Studio
- additional Story Assessment signal promotion
- Archetype exposure
- Confidence Breakdown exposure
- any deterministic mutation authority beyond the existing preview/apply/rollback path

## Current-State Constraints

Story Assessment 2.2 must preserve the current product boundaries:

- scoring remains authoritative
- Guided Story Improvements remains the narrow public promotion slice
- Issues and Fix Plan remain downstream consumers
- score-panel host/webview messaging remains a versioned protocol boundary
- selected page state must remain clamped to the latest payload page count
- AI proposal enrichment remains advisory-only

The current implementation already provides:

- a safe public `guidedStoryImprovements` payload
- normalized findings derived downstream from those recommendations
- a score-panel host/webview protocol with validation
- `revealVisual` host support that can reveal a PBIR visual in the explorer and open its file

That existing reveal capability should be reused rather than replaced.

## Architectural Review Findings

The following risks are ranked by long-term maintenance impact and drive this design:

### 1. Highest Risk: Story-Assessment-Specific Navigation Logic Forks The Existing Finding Workflow

If deep links are implemented as Story Assessment-only UI code, the repository will end up with one navigation path for Story Assessment and another for Issues, Fix Plan, and Fabric review. That would violate the normalized-findings-first architecture and make future reuse expensive.

Design response:

- define one shared score-panel navigation target model
- derive navigation targets once in the extension presentation layer
- reuse those targets from Story Assessment, Issues, Fix Plan, and future analyzers

### 2. High Risk: Diff Mode Drifts Into Internal-Signal Replay

If diff mode compares internal archetypes, confidence breakdowns, or raw signal registry fields, the feature will become unstable, difficult to explain, and contract-risky.

Design response:

- diff mode compares only public Story Assessment outputs already shown to the user
- no archetypes, confidence breakdown, coherence, or raw evidence traces enter the snapshot model

### 3. High Risk: Repo-Polluting History Files Create Operational Debt

Writing diff history into the PBIR repo would create accidental commit noise, merge churn, and unclear ownership of analyzer metadata.

Design response:

- store snapshots in extension-owned persistent storage outside the PBIR repo
- follow the same global-storage JSON pattern already used for intent feedback, audit sessions, and preview options

### 4. Medium Risk: Backend Contract Expansion For Navigation Metadata Widens Promotion Scope Too Early

Adding Story Assessment-specific target metadata directly to backend score outputs would widen the promoted contract before the repo has proven that target inference is stable.

Design response:

- keep deep-link target derivation in the extension presentation layer for 2.2
- use only public payload plus page visual metadata
- leave optional backend-assisted targeting as a later optimization, not a release requirement

## Design Principles

### 1. Workflow First

Both features exist to reduce friction. They should not read like a new scoring subsystem.

### 2. Public Outputs Only

Story Assessment Diff Mode must compare only safe public outputs. Deep links may use public page metadata to locate targets, but they must not expose internal evidence IDs or research-stage diagnostics.

### 3. Downstream From Scoring

Neither feature changes how Story Assessment scores, classifies, or promotes data. They are downstream presentation and workflow layers.

### 4. Reusable Navigation Infrastructure

Deep-link targeting must be designed as shared finding navigation infrastructure so future Issues, Fix Plan, and Fabric App Review can reuse it without a second contract shape.

### 5. Conservative Targeting

A wrong deep link is worse than no deep link. The system should prefer explicit page fallback over speculative visual targeting.

## Feature 1: Deep Link Navigation

### User Outcome

From a Story Assessment recommendation, the user can go directly to the best available target:

- visual
- page
- report element

with a single action.

### Navigation Model

Introduce one presentation-layer navigation target model for the score panel:

```ts
export type ScorePanelNavigationTargetKind = 'visual' | 'page' | 'report';

export interface ScorePanelNavigationTarget {
  kind: ScorePanelNavigationTargetKind;
  pageName?: string;
  visualId?: string;
  reportElement?: 'reportJson' | 'pageJson' | 'themeJson';
  label: string;
  reason: string;
  supportState: 'direct' | 'fallback' | 'unavailable';
}
```

This target is presentation metadata. It does not change scoring semantics and does not belong to backend Story Assessment internals.

### Placement In The Architecture

Deep-link targets should be derived in the extension presentation layer:

`ScoreResult`
`+ page visual metadata`
`+ guided story improvements`
`+ normalized findings`
`-> navigation target builder`
`-> score panel payload`
`-> Story Assessment / Issues / Fix Plan UI`

Why this layer:

- it preserves the current backend promotion boundary
- it keeps navigation downstream from scoring
- it allows future consumers to share the same targets

### Host/Webview Contract Direction

Do not leave navigation as `revealVisual` forever.

For 2.2, the score-panel protocol should evolve to a generic message:

```ts
{ type: 'navigateToTarget'; target: ScorePanelNavigationTarget }
```

Host behavior:

- `visual`: reuse current explorer reveal behavior and open the visual file
- `page`: reveal the page in the explorer and open `page.json`
- `report`: reveal the report root element and open the requested report-level file

Implementation note:

- existing `revealVisual` behavior can remain as a compatibility path during rollout
- the new host action should become the shared path for all future finding navigation

### Story Improvement Mapping

The six validated Story Assessment categories should map as follows:

| Story improvement | Preferred target | Fallback | Notes |
|---|---|---|---|
| Missing Title / Question Anchor | page | report | Usually a page-framing issue, not a visual issue. If a title textbox exists, it may be used only when clearly dominant. |
| Missing Benchmark / Target | visual | page | Target the lead KPI, scorecard, or primary comparison visual. |
| Missing Prior-Period Context | visual | page | Target the primary metric trend visual or KPI driving the current story. |
| Missing Primary Metric | visual | page | Target the lead headline visual or the top-of-scan visual cluster. |
| Missing Primary Dimension | visual | page | Target the lead comparison or grouping visual. |
| Scattered Filters | visual or page | report | If clear slicer visuals exist, target the dominant filter cluster; otherwise target the page. |

### Target Selection Rules

#### Visual-Level Navigation

Use visual-level navigation only when the extension can identify a stable target from public metadata.

Target ranking should prefer:

1. existing affected visual references already present in public findings
2. explicit visual metadata cues from the lead story area
3. visible title-bearing KPI or chart visuals in the top scan path
4. visuals whose type matches the recommendation domain:
   - KPI/card/scorecard for benchmark and primary metric
   - line/area/trend chart for prior-period context
   - bar/column/comparison chart for primary dimension
   - slicer visuals for scattered filters

If multiple visuals tie and no clear winner exists:

- do not pick arbitrarily
- degrade to page-level navigation

#### Page-Level Navigation

Use page-level navigation when:

- the recommendation concerns page framing
- no stable target visual can be chosen
- the necessary visual does not exist yet

This is the default for missing title/question anchor.

#### Report-Level Navigation

Report-level navigation is required in the architecture even though the current six Story Assessment categories are mostly page-local.

It should be used when:

- the recommendation belongs to report structure rather than one page
- the user is viewing a report-level Story Assessment summary in the future
- future Issues or Fabric review findings target report root assets

For Story Assessment 2.2, report-level navigation is mainly a fallback surface and future-compatibility seam.

### Unsupported Case Behavior

Unsupported cases must be explicit and deterministic:

- no target visual exists:
  - navigate to the page target
  - explain that the visual is missing rather than hidden
- recommendation applies to the page:
  - navigate to `page.json`
  - preserve the improvement wording in the UI
- recommendation applies to the report:
  - navigate to the report root target
- target cannot be resolved:
  - disable the action and show an explanatory tooltip or helper text
  - do not post a speculative host action

### UX Placement

Deep-link affordances should appear in three places:

1. Story Assessment Top Story Improvements
2. Issues items generated from Guided Story Improvements
3. Fix Plan items derived from the same recommendations

Recommended interaction pattern:

- a compact secondary action on each recommendation row: `Go to target`
- when only a page target exists, use a contextual label such as `Open page anchor`
- when the target is unavailable, show helper text instead of a dead button

This keeps Story Assessment readable while still making the navigation obvious.

### Future Compatibility

The navigation target model should be designed for reuse by:

- Issues
- Fix Plan
- Fabric App Review
- cross-page matrix navigation
- future report-level advisory findings

The shared target shape prevents the repo from inventing one-off navigation actions for each surface.

## Feature 2: Story Assessment Diff Mode

### User Outcome

After making report changes and re-running analysis, the user can answer:

- what improved
- what regressed
- what stayed the same

without manually comparing pages.

### Core Rule

Diff Mode must use only public Story Assessment outputs already visible in the product.

It must not require:

- archetypes
- confidence breakdown
- coherence
- competing-story diagnostics
- raw evidence IDs

### Snapshot Model

Diff mode should store snapshots in an extension-owned persistence model:

```ts
export interface StoryAssessmentPageSnapshot {
  pageName: string;
  storyType?: string;
  storyMaturity: 'Draft' | 'Developing' | 'Strong' | 'Mature';
  strongSignals: string[];
  missingSignals: string[];
  topImprovementIds: string[];
  recommendations: Array<{
    id: string;
    title: string;
    priority: 'high' | 'medium' | 'informational';
    summary: string;
    expectedImpact: string;
    navigationTarget?: ScorePanelNavigationTarget;
  }>;
}

export interface StoryAssessmentReportSnapshot {
  reportPath: string;
  reportKey: string;
  capturedAt: string;
  pageCount: number;
  pages: StoryAssessmentPageSnapshot[];
}
```

Important boundary:

- snapshot fields are built from current public payload plus presentation-only derived labels already shown in the UI
- no internal backend validation models enter storage

### Why Snapshot The Public Story View Instead Of The Raw Score

The user’s question is not “did an internal confidence cluster change.”

The user’s question is “did the story get better.”

Therefore the snapshot should mirror the public story contract:

- story maturity
- strong signals
- missing signals
- top story improvements

This keeps the comparison understandable and stable.

### Comparison Model

Diff mode should compare:

- Story Maturity change
- added and removed Strong Signals
- added and removed Missing Signals
- resolved story improvements
- newly introduced story improvements
- unchanged story improvements

Recommended result shape:

```ts
export interface StoryAssessmentDiffResult {
  pageName: string;
  maturityChange: {
    before: string;
    after: string;
    direction: 'improved' | 'regressed' | 'unchanged';
  };
  resolvedRecommendations: string[];
  newRecommendations: string[];
  unchangedRecommendations: string[];
  addedStrongSignals: string[];
  removedStrongSignals: string[];
  addedMissingSignals: string[];
  removedMissingSignals: string[];
  summary: string;
}
```

### Diff Semantics

The comparison rules should be simple and explainable:

- improved:
  - Story Maturity moves upward
  - one or more top story improvements are resolved
  - Missing Signals shrink
- regressed:
  - Story Maturity moves downward
  - new recommendations appear
  - Missing Signals grow
- unchanged:
  - the story posture remains materially the same

No numeric story delta score is required for 2.2. A summary sentence is enough.

### UX Placement

Diff Mode should appear as an optional embedded block inside Story Assessment rather than a new tab.

Recommended layout:

- default Story Assessment remains the primary view
- when a prior snapshot exists, show a compact `What Changed` block beneath Top Story Improvements
- provide an expand/collapse affordance for more detail

This keeps the user in the existing workflow:

Story Assessment  
↓  
Top Story Improvements  
↓  
What Changed  
↓  
Go to target

### History Strategy Evaluation

Three options were evaluated.

#### Option 1: VS Code `workspaceState`

Pros:

- easy to wire quickly
- no file I/O

Cons:

- inconsistent with current repo storage patterns
- opaque and harder to inspect during debugging
- weaker testability for persisted JSON evolution
- less suitable if the snapshot payload grows

#### Option 2: Repo-Local File

Pros:

- visible on disk
- easy to inspect manually

Cons:

- pollutes the PBIR repo with analyzer metadata
- creates accidental commit and merge noise
- unclear ownership in shared repos

#### Option 3: Extension Global Storage JSON

Pros:

- matches existing intent feedback, audit session, and review packet preview storage patterns
- durable across VS Code restarts
- inspectable and testable
- avoids repo pollution

Cons:

- requires a small persistence helper

Recommendation:

- use extension global storage JSON keyed by report path hash

### Change Explanation Model

The UI should explain changes in plain language, not internal diagnostics.

Examples:

- `Story maturity improved from Developing to Strong.`
- `Resolved: Add a benchmark or target.`
- `New concern: The page now lacks a clear primary dimension.`
- `Unchanged: Consolidate scattered filters.`

The explanation should remain recommendation-centric and visible-authoring-centric.

### Snapshot Lifecycle

2.2 should support one baseline and one latest comparison by default:

- after a successful analysis, save the new snapshot
- when the next analysis completes, compare the previous snapshot to the current one
- replace the prior snapshot with the latest current snapshot after the diff is computed

This avoids building a full timeline product before the workflow value is proven.

Future history depth can be added later without changing the core comparison model.

## Combined Workflow Architecture

The two features should share one improvement-centered flow:

1. analyze current page or report
2. render Story Assessment with recommendations
3. allow direct navigation to the best target
4. re-analyze after edits
5. show what changed using the prior public snapshot
6. let unresolved or new recommendations reuse the same navigation targets

This requires three shared pieces:

- shared navigation target model
- shared recommendation identifiers
- shared public snapshot builder

### Why These Features Belong Together Architecturally

Both features are about shortening the loop between:

- see the recommendation
- make the edit
- confirm the improvement

Deep links shorten the move to action.

Diff mode shortens the move back to confidence.

## Backward Compatibility

Story Assessment 2.2 must remain additive:

- old score payloads without navigation metadata or diff state must still render safely
- deep-link actions should be optional and disappear cleanly when targets are unavailable
- diff mode should appear only when a prior snapshot exists
- the existing `revealVisual` path may remain supported during rollout while generic target navigation is introduced

## Validation Strategy

The implementation should prove:

- navigation targets are deterministic and conservative
- unsupported targets fall back to page or disabled states cleanly
- host/webview protocol validation rejects malformed navigation payloads
- diff snapshots contain only public story fields
- diff comparisons do not leak internal Story Assessment signals
- old payloads still render safely

## Release Recommendation

Recommended rollout:

### Phase 1

Ship Deep Link Navigation first.

Why:

- it reuses existing reveal foundations
- it provides immediate workflow value
- it has lower persistence and comparison complexity than Diff Mode

### Phase 2

Ship Story Assessment Diff Mode next.

Why:

- it introduces new storage and lifecycle logic
- it needs a stable public snapshot model first

### Phase 3

Run combined workflow validation.

Why:

- the value of 2.2 is the full loop from recommendation to edit to confirmation

## Final Recommendation

Ship Story Assessment 2.2 as a workflow release built on two shared presentation-layer primitives:

- a reusable navigation-target model
- a public Story Assessment snapshot and diff model

Do not widen backend Story Assessment promotion scope for this release.

Do not expose research-stage internals.

Do not create Story Assessment-only navigation or storage paths that bypass the existing finding, protocol, and persistence architecture.
