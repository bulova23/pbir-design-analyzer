# Report Design Studio UX Phase 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the first coherent user-facing Report Design Studio workflow shell on top of existing Design Studio architecture, without changing workflow authority, trust boundaries, or analyzer ownership.

**Architecture:** Add a report-scoped Design Studio entry path and a workspace-style shell that orchestrates the existing Design Brief, Concept, Draft, Materialization, Analyzer Handoff, Refinement, and Closed Loop foundations. Keep the shell presentation-only where possible, consume existing stores and contracts, and preserve explicit handoff into Analyzer Workspace as a peer workflow.

**Tech Stack:** TypeScript, React webviews, VS Code extension host, existing Design Studio contracts/stores, existing Analyzer Workspace shell patterns, Jest

---

## Scope Guardrails

- This plan is UX orchestration only.
- Do not redesign Design Studio architecture.
- Do not add provider execution.
- Do not add AI generation features.
- Do not add new analyzer features.
- Do not embed Analyzer Workspace inside Design Studio.
- Do not collapse approval kinds.

## Information Architecture

### Primary Navigation

The Design Studio shell should expose one persistent workflow rail with these stages:

1. Design Brief
2. Concept Studio
3. Draft Studio
4. Materialize Candidate
5. Analyze Draft
6. Suggested Improvements
7. Compare Iterations

### Always Visible

- thread identity
- workflow rail
- current stage status
- current approval checkpoint for the active stage
- next-step CTA

### Contextual

- stage-specific artifact details
- source analyzer summaries
- deeper comparison details
- provenance drawer contents

### Hidden By Default

- raw IDs
- raw protocol details
- full lineage traces
- provider-specific complexity not needed for the current action

## Workflow Architecture

### Entry And Re-Entry Model

- primary first-launch entry: Explorer
- persistent return entry: Activity Bar
- power-user entry: Command Palette
- validation return entry: Analyzer Workspace back-to-studio action

### Stage Progression Model

- Design stages progress through approval gates
- Materialization is an explicit transition stage
- Analyzer handoff is an explicit peer-workflow launch
- Refinement is a proposal-review stage
- Comparison is the explicit closed-loop review stage

### State Ownership

- Design Studio owns stage orchestration and design approvals
- Materialization stage owns candidate review and materialization approval
- Analyzer Workspace owns validation execution and validation approval
- Compare Iterations reflects status; it does not change analyzer ownership

## Navigation Architecture

### Recommended Shell Pattern

- workspace-style shell
- left workflow rail
- central stage canvas
- top thread header

### Navigation Rules

- users may move backward to earlier stages at any time
- forward movement is allowed only when the current gating conditions are satisfied
- blocked stages remain visible, never hidden
- approvals are shown as stage-local checkpoints, not global banners

### Route And Command Model

Recommended commands:

- `pbirAnalyzer.openDesignStudio`
- `pbirAnalyzer.openDesignStudioForActiveReport`
- `pbirAnalyzer.resumeLatestDesignStudioThread`
- `pbirAnalyzer.returnToDesignStudioFromAnalyzer`

Recommended internal navigation state:

- current stage
- selected artifact
- selected iteration
- selected comparison pair when relevant

## Shell Design

### Header

Should show:

- report or thread name
- current stage label
- latest approved draft or concept summary
- latest analyzer return state when available

### Workflow Rail

Should show:

- stage label
- status:
  - blocked
  - ready
  - in review
  - approved
  - returned
- approval indicator when the stage owns one

### Stage Canvas

Each stage should contain:

- stage purpose text
- primary artifact summary
- explicit checkpoint card
- one primary next-step action
- expandable provenance summary

## MVP Stage Design Tasks

### Phase 1: Entry And Shell Foundation

Objective:

- expose Design Studio as a first-class report workflow

Implementation focus:

- Explorer entry
- Activity Bar return entry
- top-level shell container
- persistent workflow rail
- shell status mapping from existing stores

Validation:

- host command tests
- shell rendering tests
- smoke check that blocked and ready states render correctly

### Phase 2: Design Stages Consolidation

Objective:

- make Design Brief, Concept Studio, and Draft Studio feel like one workflow rather than three isolated views

Implementation focus:

- shared shell framing around existing views
- grouped brief sections and helper copy
- richer concept structure summaries
- draft artifact review summary and visible approval checkpoint

Validation:

- webview tests for grouped sections, approvals, and stage transitions
- focused store tests to confirm no gating regression

### Phase 3: Materialization And Handoff UX

Objective:

- expose the trust boundary clearly before analysis

Implementation focus:

- dedicated Materialize Candidate stage
- candidate readiness presentation
- executable vs preview-only vs unsupported messaging
- explicit Analyze Draft handoff stage with peer-workflow CTA

Validation:

- materialization coordinator regression tests
- handoff service regression tests
- UX tests proving approval and ownership copy remains distinct

### Phase 4: Suggested Improvements UX

Objective:

- turn refinement proposals into a consultant-usable review stage

Implementation focus:

- proposal grouping by user meaning
- rationale and expected-impact summaries
- explicit approve or reject proposal actions
- source analyzer summary links

Validation:

- refinement store tests
- webview tests for grouped proposal cards and approval semantics

### Phase 5: Compare Iterations UX

Objective:

- complete the visible closed loop

Implementation focus:

- baseline vs current iteration comparison
- summary deltas for design, analyzer, and approval status
- validation checkpoint visibility
- expandable lineage details

Validation:

- iteration store tests
- comparison view tests
- manual smoke against the full workflow path

## File And Component Plan

### Extension Host Areas

Likely files to modify or add around existing Design Studio seams:

- `vscode-extension/src/extension.ts`
- `vscode-extension/src/commands/register.ts`
- `vscode-extension/src/design-studio/contracts/designStudioNavigation.ts`
- new or expanded Design Studio shell registration and command wiring files near existing command/bootstrap seams

Responsibilities:

- Design Studio launch
- stage-aware navigation state
- Analyzer Workspace return wiring

### Webview Areas

Likely files to modify or add:

- `vscode-extension/webview-src/design-studio/`
- `vscode-extension/webview-src/design-studio/views/DesignBriefView.tsx`
- `vscode-extension/webview-src/design-studio/views/ConceptStudioView.tsx`
- `vscode-extension/webview-src/design-studio/views/DraftStudioView.tsx`
- `vscode-extension/webview-src/design-studio/views/ClosedLoopView.tsx`
- new shell, stage-card, approval-card, provenance-summary, materialization, and refinement components under `webview-src/design-studio/components/`

Responsibilities:

- shell composition
- workflow rail
- stage summaries
- approval UX
- provenance disclosure
- materialization, handoff, and refinement presentation

## Approval UX Implementation Rules

- one stage, one visible checkpoint card
- each checkpoint card must name:
  - approval kind
  - owner
  - unlock
  - non-effects
- validation approval must always identify Analyzer Workspace ownership
- stage transitions must never imply that approval also executed analysis or mutation

## Provenance And Lineage Implementation Rules

- default to compact “Why this exists” summaries
- offer deeper traceability through expanders or drawers
- keep raw IDs and implementation-grade diagnostics secondary
- never require a user to parse full provenance arrays to understand next actions

## Rollout Phases

### Release Slice 1

- entry surfaces
- shell foundation
- Design Brief, Concept, and Draft consolidation

### Release Slice 2

- Materialize Candidate stage
- Analyze Draft stage
- Analyzer Workspace return path

### Release Slice 3

- Suggested Improvements stage
- Compare Iterations stage
- final approval and provenance polish

Recommended release posture:

- ship in slices only if each slice is coherent on its own
- do not expose half-finished transition stages without shell framing

## Validation Strategy

### Automated Validation

- preserve existing Design Studio store and contract tests
- add host command and navigation tests
- add shell rendering tests
- add approval-card semantics tests
- add Materialization and Analyzer Handoff copy-boundary tests
- add comparison-stage rendering tests

### Manual Validation

Run one end-to-end smoke path that verifies:

1. user enters Design Studio from Explorer
2. user can see blocked and ready stages correctly
3. approvals are understandable and distinct
4. materialization explains candidate readiness clearly
5. analyzer handoff feels like a normal continuation
6. refinement proposals are understandable without reading architecture docs
7. comparison shows what changed and whether it improved

### Regression Watch List

- selection vs approval confusion
- materialization approval vs validation approval confusion
- hidden analyzer execution implications
- stale navigation state after analyzer return
- provenance surfaces becoming too noisy

## Usability Testing Strategy

Use a consultant-style scenario test with 4 to 6 users or internal reviewers familiar with PBIR but not with Design Studio internals.

Primary scenarios:

1. start a new design thread from a report
2. approve a concept baseline and move into draft review
3. materialize a candidate and explain what will happen next
4. launch Analyzer Workspace and return with suggestions
5. compare two iterations and decide whether to keep the refined version

Success criteria:

- users can name the current stage without help
- users can explain what each approval does
- users can explain whether the report changed yet
- users understand that Analyzer Workspace owns validation
- users can identify why the current iteration exists

Failure criteria:

- users think analysis happens inside Design Studio
- users think approval mutates the report
- users cannot distinguish design approval from validation approval
- users cannot tell whether a candidate is executable or preview-only

## Next Recommended Execution Order

1. Entry and shell foundation
2. Design-stage consolidation
3. Materialization and Analyzer Handoff UX
4. Suggested Improvements UX
5. Compare Iterations UX

This order preserves the architectural foundations already in the repo while fixing the current highest-risk product gap: missing workflow coherence.
