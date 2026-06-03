# AI Fix Phase 2 Hardening Design

Date: 2026-06-01

Status: Drafted for roadmap planning after the `0.3.1` Phase 1 follow-up release

## Goal

Harden the shipped deterministic fix workflow so preview, approval, apply, rollback, and outcome reporting are safer, clearer, and more scalable without changing the core Phase 1 architecture.

Phase 2 is not a new AI architecture.

Phase 2 strengthens the existing trust loop:

- preview
- approve
- apply
- rollback
- re-analyze

## Strategic Positioning

Phase 1 proved that PBIR Design Analyzer can move from:

- analyze
- recommend

to:

- fix
- validate

for a narrow set of deterministic, metadata-only refactors.

Phase 2 should not broaden the mutation surface first.

Phase 2 should improve operational maturity first:

- safer sequencing
- stronger conflict detection
- clearer rollback and history visibility
- better stale handling
- better diff and outcome explanation

This keeps the product trustworthy before later AI-assisted enrichment.

## Permanent Guardrail

Intelligence may improve proposal quality later.

Intelligence must not replace deterministic execution.

That principle remains permanent across all later phases.

Phase 2 hardens deterministic execution. It does not introduce model calls, provider integration, or AI-generated mutation behavior.

## Current Architecture To Preserve

The canonical stack remains:

- `Issues`
- `Fix Plan`
- `Fix Opportunity Engine`
- `Deterministic Mutation Layer`

Each layer still has one job:

- `Issues` diagnose
- `Fix Plan` expresses remediation intent
- `Fix Opportunity Engine` proposes executable deterministic opportunities
- `Deterministic Mutation Layer` performs explicit, reversible file changes

Phase 2 must preserve this separation.

## Product Problem

Phase 1 can safely mutate a single supported opportunity with preview, approval, apply, rollback, and outcome reporting.

What it does not yet do well enough:

- sequence multiple compatible opportunities
- explain conflicts before apply
- preserve rich session history
- provide batch-safe approval patterns
- visualize larger diffs clearly
- explain stale state clearly
- separate mutation facts from outcome interpretation more cleanly

These are maturity gaps, not architectural gaps.

## Scope

### Phase 2 Includes

- multi-opportunity apply sequencing
- stronger stale/conflict detection
- overlap and incompatibility detection
- richer rollback/session history
- safer batch preview and approval
- grouped before/after diff visualization
- clearer stale regeneration messaging
- richer post-apply outcome summaries

### Phase 2 Excludes

- model calls
- AI provider integration
- LLM-generated proposals
- visual creation
- visual deletion
- chart replacement
- visual-type swaps
- DAX/model/TMDL semantic edits
- page redesign
- advanced AI refactoring
- separate refactor workspace

## Core Design Principles

### Remediation-Led Workflow Stays Intact

Phase 2 keeps fix generation under `Fix Plan`.

It does not:

- move fix generation to raw issue cards
- introduce a separate refactor workspace
- make mutation workflows primary over diagnosis

`Fix Plan` remains the bridge between diagnosis and mutation.

### Preview Before Mutation

Users must be able to inspect the exact mutation set before any apply action.

That remains true for:

- one opportunity
- many selected opportunities
- re-applied or regenerated opportunities

### Explicit Approval Before Apply

Batch behavior must never become silent mutation.

Users may batch preview and batch approve, but apply still requires an explicit final confirmation step that clearly states:

- how many opportunities will apply
- how many files will change
- whether the run is all-or-nothing
- what rollback coverage exists

### Conflict Detection Before Mutation

The system should block apply when:

- preview data is stale
- target objects changed
- opportunities overlap on the same properties
- opportunities are logically incompatible
- rollback coverage is incomplete

### Deterministic Rollback From Stored Plans

Rollback must remain independent from regeneration.

If the system needs to regenerate an opportunity in order to roll it back, the design has failed.

## Phase 2 Capability Areas

### 1. Multi-Opportunity Apply Sequencing

The user can select multiple compatible opportunities from the remediation queue.

The system should:

- validate selection compatibility
- compute a deterministic apply order
- merge the preview into one grouped mutation set
- preserve all-or-nothing behavior when any selected opportunity fails validation

Not every combination should be allowed.

Examples of unsafe combinations:

- two opportunities mutating the same property on the same object
- two opportunities with mutually exclusive layout anchors
- a stale opportunity selected alongside a current one

### 2. Stronger Conflict Detection

Phase 2 should distinguish conflict classes instead of collapsing everything into generic staleness.

Suggested classes:

- stale preview
- target object changed
- overlapping mutation
- incompatible opportunity set
- missing rollback coverage

Each class should surface a user-facing explanation and a deterministic machine-readable reason.

### 3. Richer Rollback And Session History

The product should preserve a fix-session history within the current score-panel workflow.

That history should show:

- which opportunities were applied
- which opportunities remain available for rollback
- which rollback actions succeeded
- which opportunities were superseded by regeneration

This is not a long-term server history feature.

It is session-scoped operational transparency for the current review run.

### 4. Safer Batch Approval Patterns

Phase 2 should allow:

- preview selected fixes
- approve selected fixes
- apply selected fixes

But the flow must remain explicit:

- selection
- grouped preview
- approval
- final apply confirmation

No automatic apply should happen as a side effect of selection or approval.

### 5. Better Post-Apply Diff Visualization

The current preview rows are exact but flat.

Phase 2 should add grouped visualization by:

- page
- object
- property

The UI should clearly separate:

- what changed
- what the analyzer expected to improve
- what the re-analysis actually reported

### 6. Better Stale-Preview Handling

Phase 2 should make stale state actionable.

The user should see:

- that the preview is stale
- why it became stale
- whether regeneration is available
- whether the selected set must be rebuilt

### 7. Better Outcome Reporting

The product already reports:

- `Resolved`
- `Improved`
- `Unchanged`
- `Unexpected`
- `AppliedWithUnexpectedOutcome`

Phase 2 should present these states more clearly at both:

- individual opportunity level
- grouped apply-session level

The design goal is clearer interpretation, not new scoring semantics.

## UX Shape

Phase 2 should stay inside the existing score-panel workspace.

Recommended placement:

- keep opportunity selection under remediation items in `Fix Plan`
- show grouped preview/diff in the same remediation-led flow
- add a lightweight session-history block in or adjacent to `Fix Plan`
- keep `Evidence` secondary

This preserves the current reading path:

- diagnose in `Issues`
- decide in `Fix Plan`
- inspect mutation details before apply
- validate outcomes after re-analysis

## Data And State Additions

Phase 2 likely needs presentation and execution state for:

- selected opportunity ids
- grouped preview sets
- apply-session id
- conflict reasons
- compatibility state
- rollback availability state
- session history entries

These additions should stay downstream from scoring.

They must not mutate:

- raw score values
- severity
- confidence
- normalized finding semantics

## Risks

### UX Complexity Risk

Multi-select and session history can make `Fix Plan` noisy.

Mitigation:

- progressive disclosure
- grouped summaries
- preserve remediation-first reading order

### False Safety Risk

Batch actions can create a false sense that every combination is safe.

Mitigation:

- block incompatible selections
- explain why blocked
- keep deterministic all-or-nothing semantics explicit

### State Explosion Risk

Selection, approval, apply, rollback, stale, and re-analysis states can become difficult to reason about.

Mitigation:

- keep state contracts explicit
- separate compatibility/conflict state from outcome state
- keep rollback history independent from proposal generation

## Testing Strategy

Phase 2 should add targeted coverage for:

- compatibility evaluation
- overlap/conflict detection
- grouped preview building
- multi-opportunity apply sequencing
- rollback/session history persistence within the panel session
- grouped outcome summaries
- stale regeneration flows
- webview rendering for blocked, stale, approved, applied, and rolled-back grouped states

Validation should include:

- focused Jest tests for planners/builders/state helpers
- webview Jest coverage for selection, grouped preview, conflict messaging, and session history
- targeted extension-host tests for apply/rollback orchestration
- packaged extension smoke testing against:
  - the real `Sales & Production.pbip` fixture
  - a deterministic PBIR fixture that exercises supported multi-opportunity cases

## Success Criteria

Phase 2 is successful when:

- users can safely preview and apply multiple compatible opportunities
- incompatible selections are blocked with concrete reasons
- rollback availability and session history are visible and trustworthy
- stale state is clearly explained and recoverable
- grouped diffs and outcome summaries improve operator comprehension
- the deterministic trust loop remains explicit and unchanged in principle
