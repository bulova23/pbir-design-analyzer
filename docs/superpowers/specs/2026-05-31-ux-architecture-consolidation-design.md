# UX Architecture Consolidation Design

Date: 2026-05-31

Status: Reviewed through wireframe-level mockups; ready for implementation planning

## Goal

Deliver a final presentation-layer refinement pass for the score-panel workspace before major platform expansion begins.

This epic does not add new scoring capability. It consolidates and clarifies the existing review workflow so users can move through the workspace with less duplication, less scrolling, and stronger context:

1. Issues become a clear diagnosis surface
2. Fix Plan becomes a true remediation queue
3. Page-purpose reasoning becomes a unified summary-first workflow
4. Cross-page matrix behavior becomes context-aware

## Why This Epic Comes First

This is a foundation-strengthening effort rather than a feature-expansion effort.

It should precede the larger deferred roadmap epics because:

- it reduces cognitive load in the core daily workflow
- it improves consultant usability by making remediation planning clearer
- it improves executive usability by making summary interpretation faster
- it lowers future feature complexity before export, visual intelligence, and governance add more surfaces
- it stabilizes the workspace architecture before downstream platform expansion begins

Recommended roadmap order after this addition:

1. UX Architecture Consolidation
2. Consultant Deliverables & Export Platform
3. Visual Intelligence & Screenshot Analysis
4. Enterprise Governance & Advanced Review

## Scope

Include:

- differentiating Issues from Fix Plan at the presentation level
- consolidating page-purpose reasoning into a single Page Purpose Analysis container
- making Page Purpose Analysis summary-first by default
- adding business-context `Why This Matters` summaries derived from existing signals
- making the cross-page matrix context-aware between report view and page view
- preserving existing normalized-findings drilldown and navigation behavior

## Non-Goals

- no scoring changes
- no severity changes
- no confidence changes
- no backend scoring changes
- no normalized findings redesign
- no workspace persona redesign
- no export redesign
- no new analytics
- no new scoring-derived hidden metrics

## Current State

The current `0.2.0` workspace already has the right major destination points:

1. Overview
2. Issues
3. Fix Plan
4. Evidence
5. Export

The remaining problems are compositional rather than architectural.

### 1. Issues And Fix Plan Still Feel Too Similar

Issues currently presents atomic findings, evidence, severity, and a local `Fix first` recommendation.

Fix Plan already exists, but it still reads too much like a second issue list:

- one item per problem or near-problem
- repeated recommendation language
- repeated page/scope/severity metadata
- insufficient emphasis on grouped remediation actions

This creates duplication in reading flow and makes it harder for users to distinguish diagnosis from execution planning.

### 2. Page-Purpose Reasoning Is Fragmented

The current page-review path spreads one reasoning workflow across multiple peer sections:

- `Inferred Page Story`
- `Page Intent Profile`
- `Actionability`
- `Benchmark and Archetype`
- `Intent Feedback`

All of these are individually useful, but together they fragment one conceptual task: understanding what the page is trying to do, whether it supports that purpose, and whether the user agrees.

The result is:

- repeated context switching
- more vertical scrolling than necessary
- weaker executive scanning
- weaker consultant storytelling continuity

### 3. Matrix Context Is Too Broad In Page Review

The matrix is already useful as a report-level triage aid, but the same broad report view becomes less efficient once a user is already focused on one page.

Current pain points:

- page review still inherits a report-wide comparison surface
- the user must keep re-scanning unrelated pages
- matrix context is not narrowed to the current page
- the navigation concept is strong, but the local review context is weaker than it should be

### 4. Business Impact Is Not Framed Early Enough

Existing framework, benchmark, and actionability sections expose analytical reasoning, but they do not consistently lead with business risk in the most concise possible form.

That makes it slower for users to answer:

- why does this matter to decision makers
- what is the risk of leaving this unfixed
- why is this remediation worth doing

## Target State

The target workspace keeps the current top-level architecture, but clarifies the role of each surface.

### 1. Issues = Diagnosis

Issues remains the authoritative atomic-finding surface.

It answers:

- what is wrong
- how severe it is
- why it matters
- how confident we are
- what evidence supports it

Issues remains finding-centric and normalized-finding-driven.

### 2. Fix Plan = Remediation Queue

Fix Plan becomes a consolidated remediation queue built from existing findings and recommendations.

It answers:

- what should be done first
- which fixes resolve multiple findings
- what effort is required
- what business or workflow impact the action has
- which pages are affected

Each remediation item should include:

- title
- impact
- effort
- short `Why` rationale specific to that action
- resolved finding outcomes
- affected pages
- source finding traceability

The queue becomes action-centric rather than finding-centric.

### 3. Page Purpose Analysis = Unified Reasoning Workflow

Replace the current peer-card sprawl with one parent container called `Page Purpose Analysis`.

The default state should be summary-first.

#### Default Summary Surface

The collapsed/default summary should emphasize fast scanning:

- inferred purpose
- confidence
- actionability score
- benchmark status
- top gaps
- one concise `Why This Matters` paragraph

This summary is the primary location for business-context explanation.

Example intent of the summary:

- explain the page’s likely audience and purpose
- explain what decision-support context is missing
- explain the risk of misinterpretation or weak decision-making

#### Expandable Full Reasoning

When expanded, the same container reveals the full reasoning workflow:

1. What We Think This Page Is
2. Expected Behavior
3. Decision Support
4. Benchmark Comparison
5. User Validation

This preserves existing analytical depth while making it optional rather than mandatory for every scan.

### 4. Matrix = Context-Aware Navigation Aid

The matrix keeps its role as a navigation and comparison surface, but adapts to context.

#### Overview / Report Context

Show the full report matrix.

Purpose:

- weak-page discovery
- cross-page comparison
- report-wide navigation

#### Page Context

Show only the selected page’s row or local page strip.

Purpose:

- reinforce current-page context
- reduce unrelated scanning
- provide quick jumps into Issues for the selected page and dimension
- preserve access to `Back to full matrix`

### 5. Qualitative-First Matrix Signals

Matrix cells should not lead with numbers.

Primary signal:

- `Strong`
- `Watch`
- `Weak`
- `Unknown`

Secondary/supporting signal:

- finding count

This reduces ambiguity around whether a number is a score, count, or rating scale, and improves executive readability.

## Design Principles

### 1. Presentation Only

All changes remain downstream from existing score and findings contracts.

### 2. Summary First, Depth On Demand

Executive scanning should be faster by default. Consultant depth should remain available without crowding the first read.

### 3. Diagnosis And Remediation Must Feel Different

Issues and Fix Plan should not look like reordered duplicates of each other.

### 4. Business Context Should Arrive Early

`Why This Matters` language should appear before framework-deep interpretation and should stay concise and traceable to existing signals.

### 5. Reuse Existing Contracts Where Possible

This is a consolidation effort, not a re-platforming effort.

## Architecture

Keep the current layering:

- scoring layer remains authoritative
- normalized findings remain the shared issue model
- presentation builders derive summary and queue structures from existing state
- React renders those prepared structures and manages interaction state

Recommended presentation additions:

- `page purpose analysis builder` or adapter
- enhanced `fix plan builder` for grouped remediation actions
- matrix context adapter or UI-mode layer
- lightweight business-impact summary helpers

## Data Flow

`ScoreResult`
`+ normalized findings`
`+ actionability`
`+ benchmark comparison`
`+ inferred story / intent state`
`-> page purpose analysis summary builder`
`-> remediation queue builder`
`-> context-aware matrix presentation adapter`
`-> score panel payload`
`-> React workspace`

Important boundary:

No builder may mutate score, severity, or confidence semantics.

## UX Details

### Page Purpose Analysis

Reuse existing inputs from:

- inferred story summary
- page intent profile
- actionability breakdown
- benchmark comparison
- intent feedback

Default behavior:

- render executive summary first
- hide deeper reasoning behind explicit expand action
- preserve override and validation controls when expanded

### Why This Matters

Use two different forms:

#### A. Executive Overview Narrative

Primary location: Page Purpose Analysis summary.

Purpose:

- explain business risk
- frame the page in audience terms
- connect missing signals to decision quality

This should be a concise paragraph, not a long narrative.

#### B. Remediation Queue Reinforcement

Secondary location: individual remediation items.

Purpose:

- explain why this specific action is worth doing
- reinforce impact without repeating the full narrative

This should be a short `Why:` line or impact statement, not a repeated paragraph.

### Remediation Queue

Recommended item structure:

- action title
- impact
- effort
- short `Why` statement
- `Resolves` outcome list
- affected pages
- source finding references

Queue ordering should emphasize:

- grouped actions that resolve multiple findings
- lower-effort high-impact fixes first where appropriate
- cross-page leverage where applicable

### Matrix Behavior

Report view:

- full matrix
- status-first cells
- count as supporting text

Page view:

- selected page only
- same status language
- direct jump into Issues filters
- explicit return path to full matrix

## Migration Strategy

### Reuse

Reuse existing:

- `normalizedFindings`
- overview summary infrastructure where helpful
- fix-plan derivation foundations
- inferred story / intent / actionability / benchmark / feedback source data
- current matrix finding-to-dimension mapping where valid
- existing Issues filters and navigation hooks

### Consolidate

Consolidate:

- page-purpose reasoning sections into `Page Purpose Analysis`
- fragmented business-context explanation into summary-first `Why This Matters`
- overlapping recommendation language into grouped remediation items

### Remove Or Demote

Remove as peer-level primaries:

- separate page-purpose peer cards as the default reading path
- number-first matrix interpretation
- issue-like repetition inside Fix Plan

### Keep Unchanged

Keep unchanged:

- underlying score values
- finding severity/confidence
- normalized finding IDs and issue drilldown
- workspace personas
- Evidence and Export as secondary workflows

## Test Strategy

- page-purpose summary vs expanded-state rendering tests
- regression tests for existing intent validation and override flows
- fix-plan grouping and traceability tests
- matrix context-switch rendering tests
- matrix status-label rendering tests
- Issues-to-Fix-Plan differentiation tests
- regression tests confirming no score, severity, or confidence mutation

## Dependencies

- stable `0.2.0` workspace payload
- stable normalized findings contract
- stable page-purpose source data in the current payload
- stable matrix-to-Issues navigation behavior

## Outcome

This epic should be the final workspace refinement phase before major platform expansion begins.

When complete, the workspace should feel cleaner, more stable, and easier to extend:

- executives should understand risk faster
- consultants should move from diagnosis to action more efficiently
- future deliverables, visual evidence, and governance features should have a calmer interaction foundation to build on
