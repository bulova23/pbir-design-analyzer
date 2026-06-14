# Report Design Studio UX Phase 1 Design

Date: 2026-06-13

Status: Approved design direction for UX orchestration planning only; no code changes in this document

## Goal

Define the first coherent user-facing Report Design Studio experience on top of the existing Design Studio architecture so users can move through:

Design  
↓  
Materialize  
↓  
Analyze  
↓  
Refine  
↓  
Compare

without feeling like they are moving between disconnected tools and without changing the underlying trust boundaries or workflow contracts.

## Authoritative Inputs

This design uses the following as authoritative inputs:

- `docs/superpowers/specs/2026-06-12-report-design-studio-design.md`
- `docs/superpowers/plans/2026-06-12-report-design-studio-plan.md`
- `docs/report-design-studio-manual-smoke-test.md`
- `docs/superpowers/specs/2026-06-12-story-assessment-2-2-design.md`
- existing Analyzer Workspace shell patterns in `vscode-extension/webview-src/analyzer-score/`
- current Design Studio contracts, stores, and view slices in `vscode-extension/src/design-studio/` and `vscode-extension/webview-src/design-studio/`

## Problem Statement

Report Design Studio architecture is ahead of its product UX.

The repo already has coherent foundations for:

- Design Brief
- Concept Studio
- Draft Studio
- Materialization
- Analyzer Handoff
- Refinement Studio
- Closed Loop comparison
- approval separation
- lineage and provenance

What it does not yet have is a coherent user-facing workflow shell with:

- a primary entry point
- a stable navigation model
- first-class stage transitions
- understandable approval checkpoints
- a natural Analyzer Workspace handoff

The result is a product that is internally consistent but not yet user-comprehensible.

## Non-Goals

This design does not:

- redesign Design Brief, Concept Studio, Draft Studio, Refinement Studio, Materialization, Analyzer Handoff, or Closed Loop architecture
- introduce new provider integrations
- introduce AI generation behavior
- redesign Analyzer Workspace
- change scoring or validation authority
- add new analyzer features
- expose implementation-grade lineage details by default

## Fixed Constraints

The UX must preserve these architectural boundaries:

- Report Design Studio is a separate workflow from Analyzer Workspace.
- Design Studio owns design artifacts and advisory iteration workflow.
- Materialization is the explicit boundary between design artifacts and analyzable surfaces.
- Analyzer Workspace remains the validation owner after handoff.
- Analyzer outputs remain advisory input to refinement unless and until deterministic execution is explicitly invoked elsewhere.
- Approval kinds remain separate:
  - Design Approval
  - Materialization Approval
  - Refinement Approval
  - Validation Approval
- Lineage and provenance must stay available, but they must be progressively disclosed.

## Architectural Review Findings

These findings are ranked by long-term risk and drive the UX design.

### 1. Highest Risk: Workflow Shell Debt Will Hide Good Architecture

The current product has strong contracts and weak orchestration. If more functionality lands before a coherent shell exists, every new slice will bolt onto isolated views and state transitions that users cannot understand.

Design response:

- define one primary shell
- define one primary entry path
- make each workflow stage explicit and navigable

### 2. Highest Risk: Approval Semantics Will Collapse In The UI

The data model cleanly separates design, materialization, refinement, and validation approval. The current UX does not teach those differences well enough.

Design response:

- show only one approval checkpoint card per stage
- always state owner, unlock, and non-effects
- never reuse the same visual treatment for different approval kinds without labels

### 3. High Risk: Analyzer Handoff Will Feel Like A Failure State

The current handoff reads like a technical fallback instead of a normal continuation. If that persists, users will misread validation as a broken branch of Design Studio.

Design response:

- design Analyze Draft as a handoff stage, not an embedded analyzer error state
- make the transition intentional, explicit, and resumable

### 4. High Risk: Materialization And Refinement Will Stay “Invisible Architecture”

Materialization and Refinement are the most trust-sensitive parts of the workflow, but they are currently the least visible in-product.

Design response:

- make both first-class stages in the shell
- translate diagnostics and proposals into consultant-friendly review surfaces

### 5. Medium Risk: Full Lineage Exposure Will Overwhelm Users

The platform has useful lineage, provenance, and diagnostics, but raw traces will read like internal state rather than trust signals.

Design response:

- default to compact traceability summaries
- reveal detailed provenance only on demand

## Design Principles

### 1. Workflow First

Phase 1 exists to make the current architecture understandable, not smarter.

### 2. Separate Workflow, Shared Platform

Design Studio and Analyzer Workspace remain separate workflows that reuse shared infrastructure and hand off through explicit boundaries.

### 3. Stage Clarity Over Clever Navigation

Users should always know:

- where they are
- what is ready
- what is blocked
- what approval is being requested
- what the next step is

### 4. Progressive Disclosure

Show the consultant-grade explanation first. Hide protocol-shaped state, internal IDs, and deep lineage until asked for.

### 5. Advisory Language Must Stay Honest

Design Studio may recommend, stage, and compare. It must not imply automatic mutation, automatic validation, or automatic improvement.

## Primary Entry Experience

### Evaluated Entry Paths

#### Explorer Entry

Pros:

- tied to the report/repo context
- matches how users already enter analyzer workflows
- makes it obvious which report thread is being designed

Cons:

- less visible for users who live in open panels after first launch

#### Activity Bar Entry

Pros:

- persistent and discoverable after adoption
- useful for returning to active threads

Cons:

- too global for first entry
- risks implying Design Studio is detached from a report context

#### Command Entry

Pros:

- fast for advanced users
- easy to script and re-open

Cons:

- low discoverability
- weak workflow framing

#### Workspace Entry From Analyzer Workspace

Pros:

- useful for refine-and-compare return paths

Cons:

- wrong first impression for a design-first workflow
- suggests Design Studio is a child of validation

### Recommendation

Primary entry point:

- Explorer entry on the active PBIR report root or Design Studio thread node

Secondary entry points:

- Activity Bar entry for return visits and thread resumption
- Command Palette entry for power users
- Analyzer Workspace “Refine In Design Studio” return action after validation

Reasoning:

- first entry should be report-scoped and context-rich
- later re-entry should be workspace-fast
- Analyzer Workspace should feed Design Studio only after an analysis step, not own its launch story

## Primary Shell

### Evaluated Shell Patterns

#### Wizard-Style Flow

Rejected as primary shell.

Reason:

- Design Studio is iterative, not linear
- users need to revisit earlier stages
- approvals and comparisons require stateful re-entry

#### Pure Tabbed Stage Navigation

Rejected as primary shell.

Reason:

- tabs flatten stage readiness and approval semantics
- handoff and compare stages need stronger status signaling

#### Workspace-Style Shell With Persistent Workflow Rail

Recommended.

Reason:

- matches the existing Analyzer Workspace mental model
- supports iterative re-entry
- gives each stage a durable home
- can show blocked, ready, approved, and returned states without hidden logic

### Recommended Shell

Use a workspace-style shell with:

- a persistent left workflow rail
- a central stage canvas
- a compact top thread header
- a stage summary strip inside the active canvas

The shell should not add a permanent right-side inspector in Phase 1. Provenance and lineage should open in expandable sections or drawers inside the current stage.

### Shell Structure

#### Top Header

Always visible:

- thread name
- current report or project context
- current stage
- latest approved baseline
- latest analyzer return status

#### Left Workflow Rail

Always visible:

1. Design Brief
2. Concept Studio
3. Draft Studio
4. Materialize Candidate
5. Analyze Draft
6. Suggested Improvements
7. Compare Iterations

Each item shows:

- status badge
- blocked or ready state
- approval checkpoint presence when relevant

#### Stage Canvas

Contextual:

- stage-specific content
- approval card
- next-step CTA
- traceability summary

## Workflow Navigation

### Navigation Model

The left workflow rail is always visible.

The current stage canvas is always visible.

Contextual stage tools are visible only inside the current stage.

Hidden by default:

- detailed provenance trace
- raw IDs
- internal diagnostics that only matter for debugging
- unsupported future-provider options

### Stage Behavior

#### Design Brief

Purpose:

- create the design intent baseline

Always visible in the stage:

- grouped brief sections
- brief readiness
- design approval card
- generate concepts CTA

Contextual:

- helper text for advanced fields
- “why this matters” explanations

#### Concept Studio

Purpose:

- compare alternate concepts and choose a baseline

Always visible:

- current preferred baseline
- concept alternatives
- concept structure summary
- design approval card

Contextual:

- chapter map
- KPI hierarchy
- navigation structure
- analytical flow

#### Draft Studio

Purpose:

- review draft artifacts before materialization

Always visible:

- active draft version
- draft artifact inventory
- design approval card
- materialize candidate CTA

Contextual:

- page-by-page draft preview
- provider provenance summary when provider input exists

#### Materialize Candidate

Purpose:

- turn an approved draft into an analyzable candidate explicitly

Always visible:

- source approved draft
- target analyzer and profile
- candidate readiness state:
  - executable
  - preview-only
  - unsupported
- materialization approval card
- analyze draft CTA when executable

Contextual:

- degraded mapping notes
- omitted evidence notes
- provenance summary

#### Analyze Draft

Purpose:

- launch validation without implying embedded execution

Always visible:

- candidate summary
- Analyzer Workspace ownership statement
- open Analyzer Workspace CTA
- return-to-studio expectations

Contextual:

- handoff diagnostics
- last handoff timestamp

#### Suggested Improvements

Purpose:

- review analyzer-derived recommendations as advisory design changes

Always visible:

- grouped proposal list
- proposal rationale
- affected artifact references
- expected impact summary
- refinement approval card

Contextual:

- source analyzer section:
  - Story Assessment
  - Issues
  - Fix Plan
  - Cross-Page Narrative

#### Compare Iterations

Purpose:

- show what changed, why it changed, and whether it improved

Always visible:

- baseline iteration
- current iteration
- change summary
- validation outcome delta
- validation approval checkpoint state

Contextual:

- deeper lineage chain
- comparison filters

## Materialization UX

Phase 1 should introduce a dedicated Materialize Candidate stage.

Users must understand:

- what is being materialized:
  - the currently approved draft baseline
- what will be analyzed:
  - the derived candidate surface
- what is not changing:
  - the report is not being mutated
  - the analyzer is not running yet
  - approval does not equal execution

Recommended stage copy shape:

- Source: approved draft baseline
- Output: analyzable candidate
- Ownership after handoff: Analyzer Workspace
- No report changes are made here

Diagnostics should be translated into three consultant-facing buckets:

- Ready for analysis
- Preview only
- Needs attention

## Analyzer Handoff UX

Phase 1 should make Analyze Draft an intentional transition stage rather than a fallback message inside Analyzer Workspace.

Users should understand:

- Design Studio owns the candidate until handoff
- Analyzer Workspace owns validation after handoff
- no automatic mutation occurs
- returning results to Design Studio is explicit

Recommended flow:

1. Review candidate summary in Analyze Draft
2. Launch Analyzer Workspace with explicit CTA
3. Complete validation in Analyzer Workspace
4. Return to Suggested Improvements or Compare Iterations via explicit return action

## Refinement UX

Phase 1 should translate refinement proposals into a consultant-friendly Suggested Improvements stage.

The stage should group proposals by user meaning, not backend source shape:

- Story clarity
- KPI and benchmark clarity
- navigation and scan path
- comparison and context
- consistency and narrative flow

Each proposal card should show:

- recommendation title
- why it was suggested
- affected draft or concept area
- expected improvement
- source analyzer signal summary
- explicit approve or reject action

The UI should avoid showing “refinement proposal ingestion” language. That is architecture, not product UX.

## Closed Loop UX

Phase 1 should make Compare Iterations the visible end of the loop.

Users should understand:

- what changed
- why it changed
- whether it improved

The default view should compare two iterations:

- baseline
- current

with three summary blocks:

- Design changes
- Analyzer result changes
- Approval and readiness changes

Deep lineage remains expandable, not default.

## Approval UX

Approval must be explicit, stage-local, and named by function.

### Design Approval

Used in:

- Design Brief
- Concept Studio
- Draft Studio

Meaning:

- this artifact is ready to become the baseline for the next design stage

Must say:

- owner
- what this unlocks
- what it does not do

### Materialization Approval

Used in:

- Materialize Candidate

Meaning:

- this approved draft may be turned into a candidate for analysis

Must say:

- no report mutation happens here
- validation does not happen here

### Refinement Approval

Used in:

- Suggested Improvements

Meaning:

- these proposals are accepted into the next design iteration

Must say:

- approval creates the next design path
- approval does not validate the result

### Validation Approval

Used in:

- Compare Iterations
- any Analyzer Workspace return summary shown in Design Studio

Meaning:

- Analyzer Workspace accepted or rejected the validation outcome

Must say:

- owner is Analyzer Workspace
- Design Studio cannot self-approve validation

## Provenance And Lineage UX

Users need trust signals, not implementation traces.

### Default Provenance Summary

Show a compact “Why this exists” summary inside stage canvases:

- based on approved brief vX
- derived from concept baseline Y
- built from draft version Z
- informed by analyzer run A when relevant

### Expandable Traceability

Expose deeper provenance only on demand through:

- Show sources
- Show approval history
- Show analyzer linkage

The detailed view may include version references and timestamps. Internal IDs and full diagnostics should stay secondary.

### What Stays Hidden In Phase 1

- raw protocol messages
- internal artifact IDs by default
- full provenance trace arrays
- implementation-grade diagnostics unless they affect user action

## MVP Recommendation

### MVP UI Should Contain

- Explorer-first entry into Design Studio
- workspace-style shell with persistent workflow rail
- explicit stages for:
  - Design Brief
  - Concept Studio
  - Draft Studio
  - Materialize Candidate
  - Analyze Draft
  - Suggested Improvements
  - Compare Iterations
- one named approval card per stage
- compact traceability summaries
- explicit Analyzer Workspace handoff CTA and return path

### Hidden Until Later Releases

- advanced lineage inspectors as primary UI
- provider-specific capability controls beyond minimal provenance
- visual diff sophistication beyond consultant-readable summaries
- multi-thread management complexity beyond one active thread experience
- any embedded Analyzer Workspace experience inside Design Studio
- any automatic materialize-analyze-refine loop

## Final Answers

### 1. What is the primary Design Studio entry point?

Explorer entry on the active PBIR report or Design Studio thread, with Activity Bar, Command Palette, and Analyzer Workspace return actions as secondary entry paths.

### 2. What is the primary Design Studio shell?

A workspace-style shell with a persistent left workflow rail, compact top thread header, and central stage canvas.

### 3. What workflow should users experience?

Design Brief  
→ Concept Studio  
→ Draft Studio  
→ Materialize Candidate  
→ Analyze Draft  
→ Suggested Improvements  
→ Compare Iterations

with explicit return loops from analysis into refinement and comparison.

### 4. What should the MVP UI contain?

The shell, the seven workflow stages, explicit approval cards, candidate readiness and handoff messaging, consultant-friendly refinement review, and compact provenance summaries.

### 5. What should remain hidden until later releases?

Provider-specific complexity, deep lineage internals, advanced visual diffing, embedded analyzer behavior, and any automation that blurs approval or ownership boundaries.
