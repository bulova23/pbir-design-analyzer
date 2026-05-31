# Full Score Panel Workspace Modernization Design

Date: 2026-05-31

Status: Approved design direction captured; ready for implementation planning after spec review

## Goal

Complete the PBIR Design Analyzer score-panel modernization by evolving the current issue-centric foundation into a true review workspace with this reading path:

1. Overview
2. Issues
3. Fix Plan
4. Evidence
5. Export

This pass builds on the already-implemented normalized findings model and Issues-first review surface. It does not replace the scoring engine or reduce analytical depth. It reorganizes and interprets existing signals so users can understand report health, triage findings, plan remediation, inspect supporting evidence, and only then export downstream artifacts.

## Problem Statement

The first modernization slice solved the keystone problem by introducing normalized findings and making Issues the default review surface. The score panel still has meaningful gaps before it feels like a complete review workspace:

- there is no compact report-level Overview layer for executive triage
- the Issues surface lacks first-class filters and alternate groupings
- quick fixes exist, but there is no consultant-friendly Fix Plan workspace
- Evidence is demoted, but not yet clearly organized as an expert investigation area
- review packet preview and export remain too close to the main reading path

The current product already has valuable scoring, consistency, benchmark, actionability, metadata, screenshot-audit, and export capability. The remaining work is primarily information architecture and presentation shaping.

## Design Principles

### 1. Overview Interprets, It Does Not Score

Overview may aggregate, summarize, classify, and translate existing signals into executive-friendly language. It may not introduce new scoring algorithms, new weights, hidden score adjustments, or feedback into the scoring engine.

### 2. Findings Remain The Shared Review Model

Normalized findings remain the authoritative issue object for triage, filtering, grouping, remediation, and optional prioritization.

### 3. Scoring, Findings, And Presentation Stay Separate

The architecture should preserve a clean boundary:

- scoring remains authoritative and unchanged
- normalized findings remain the shared issue model
- presentation-summary builders create workspace views from existing signals
- React renders prepared data and handles user interaction

### 4. Export Is Downstream

Export is not a primary review activity. It remains accessible, but it must not dominate the main path or appear as a first-class top-level workspace in this pass.

### 5. Evidence Must Remain Intact

Framework analysis, metadata, screenshot audit, scoring internals, and packet preview remain available. They become secondary expert workflows rather than competing primary sections.

### 6. Optional Enhancements Must Be Low-Risk

Cross-page matrix and persona-aware prioritization should only ship if they can be implemented as presentation-only features using existing data, without introducing fragile new semantics or backend churn.

## Architecture And Data Flow

Three distinct layers should govern the score panel:

### A. Scoring Layer

The existing backend result model remains authoritative:

- `ScoreResult`
- page scores
- framework scoring
- actionability
- benchmark/archetype summaries
- report consistency
- screenshot audit state
- metadata summaries
- review packet preview/export state

This pass should not introduce new scoring algorithms or mutate existing score outputs.

### B. Findings Layer

The existing normalized findings contract remains the shared issue model. It powers:

- issue rendering
- issue filtering
- issue grouping
- top-issue selection
- remediation references
- optional persona-aware ordering

No new review surface should bypass the normalized finding model when it is representing a problem or recommended action.

### C. Presentation-Summary Layer

Add dedicated presentation builders that derive workspace-ready structures from existing score state and normalized findings:

- `overview summary builder`
- `fix plan builder`
- optional `matrix builder`
- optional `persona ordering helper`

These builders remain deterministic and explainable.

### Data Flow

`ScoreResult`
`+ normalized findings`
`+ existing review packet / consistency / benchmark / actionability state`
`-> overview summary builder`
`-> fix plan builder`
`-> optional matrix builder`
`-> score panel payload`
`-> React workspace UI`

## Workspace Information Architecture

### 1. Overview

Overview becomes the top decision snapshot and executive interpretation layer.

#### Purpose

Answer quickly:

- how healthy is this report overall
- what are the biggest problems
- what should be fixed first
- where is the report strong or weak across pages

#### Contents

- overall score
- maturity band
- risk band
- top strengths
- top weaknesses
- top 3 issues
- top 3 recommended actions
- severity distribution
- benchmark summary language
- cross-page summary language
- optional cross-page matrix if supported cleanly

#### Derivation Rules

All labels are presentation-only and traceable to existing outputs.

Examples:

- maturity band derived from existing composite score plus finding distribution
- risk band derived from high-severity finding count and consistency/benchmark signals
- top strengths derived from strong framework feedback, benchmark positives, and absence of severe issue density
- top weaknesses derived from highest-priority normalized findings
- cross-page summary derived from report consistency issues and page-level summaries

The Overview builder may classify and summarize. It may not change any score.

### 2. Issues

Issues remains the primary working surface.

#### Purpose

Give users the fastest path from triage to investigation.

#### Contents

- normalized finding cards
- filters
- grouped views
- sortable issue lists
- drilldowns with evidence references

#### Required Filters

- severity
- page
- impact area
- scope
- detection type

#### Grouping Modes

- severity
- category / impact area

Default state:

- high-severity findings visible first
- highest-priority group expanded
- individual detail collapsed by default

### 3. Fix Plan

Fix Plan becomes the consultant-friendly remediation queue built from existing recommendations and findings.

#### Purpose

Translate scattered advisory content into a prioritized action plan.

#### Contents

- prioritized remediation queue
- severity
- effort
- scope
- affected pages
- recommended action
- linked source finding references
- consultant-friendly next-step framing

#### Derivation Rules

Fix Plan items are presentation-layer remediation objects derived from:

- normalized findings
- quick fixes
- recommendations
- actionability/benchmark gaps where relevant

Each fix-plan item must link back to source finding IDs. Effort is a presentation-only classification and must be deterministic and explainable.

### 4. Evidence

Evidence becomes the expert investigation area.

#### Purpose

Preserve analytical depth and explainability for power users without forcing it into the primary path.

#### Contents

- Design Framework Analysis
- metadata explorer / metadata overview
- AI Screenshot Audit
- scoring internals
- cross-page detail notes
- review packet preview

Default state:

- Evidence collapsed by default
- subsections collapsed by default
- easy drill-in from issue cards and fix-plan references

### 5. Export

Export remains secondary and downstream.

#### Purpose

Support report sharing and artifact generation after the review workflow is complete.

#### Contents

- export commands/actions
- packet generation
- share/download actions

#### Boundary

Export should not become a top-level workspace in this pass. Packet preview may live under Evidence or adjacent to Export, but it must not appear before the user has meaningfully traversed findings and remediation.

## Component And Model Changes

### Host-Side Presentation Builders

Add host-side builders rather than pushing derivation into React.

#### Overview Summary Builder

Responsibilities:

- compute maturity band
- compute risk band
- build strengths/weaknesses rollups
- build top issues list
- build top actions list
- build severity distribution
- build benchmark summary language
- build cross-page summary language
- optionally emit matrix-ready data

Recommended location:

- `vscode-extension/src/analyzer/score/` or adjacent presentation helper area

#### Fix Plan Builder

Responsibilities:

- derive remediation items from findings and quick fixes
- assign deterministic effort bands
- aggregate page scope
- link queue items to source finding IDs
- generate consultant-friendly action phrasing

#### Optional Matrix Builder

Responsibilities:

- derive page-by-dimension matrix data only from existing signals
- emit display-ready matrix cells with severity/summary metadata

Only add if the available data maps cleanly to page-by-dimension cells without inventing weak pseudo-scores.

#### Optional Persona Ordering Helper

Responsibilities:

- reorder findings, overview emphasis, and fix-plan ordering for a selected persona
- never mutate severity, confidence, or scores

Only add if it is a simple presentation reorder over existing fields.

### Contract Changes

Extend the score-panel contract with new presentation-only payload objects:

- `OverviewSummary`
- `OverviewStrength`
- `OverviewWeakness`
- `OverviewAction`
- `SeverityDistribution`
- `CrossPageSummary`
- `FixPlanItem`
- optional `CrossPageMatrix`
- optional persona/view-state fields

These live in the score-panel contract layer and remain frontend-facing.

### Payload Changes

Update payload shaping to include:

- overview summary
- fix plan
- optional matrix data
- filter metadata if needed for UI efficiency

Preserve backward compatibility with current score panel and review/export workflows.

### React / Webview Changes

Reorganize the current page order around:

1. hero
2. Overview
3. Issues
4. Fix Plan
5. Evidence
6. Export actions / secondary export area

Key UI changes:

- compress the current summary card into an Overview entry point
- keep Issues as the primary interactive working area
- add filter controls and grouping controls above Issues
- replace flat Quick Fixes with a fuller Fix Plan section
- keep Review Summary where it supports active workflow, but position it as a supporting section
- move packet preview under Evidence or the export boundary

## Derivation Strategy Details

### Maturity Bands

Presentation-only labels such as:

- Emerging
- Developing
- Mature
- Advanced

Derived from existing score state and issue density. Must be deterministic and documented in tests.

### Risk Bands

Presentation-only labels such as:

- Low
- Moderate
- Elevated
- High

Derived from existing high-severity / medium-severity issue counts and related summary signals. Must not feed back into scoring.

### Top Strengths

Derived from:

- strong framework performance
- positive benchmark language
- strong actionability or consistency signals where present
- limited severe issue pressure in those areas

### Top Weaknesses

Derived from:

- highest-priority normalized findings
- repeated impact-area failures
- adverse benchmark or consistency signals

### Top Actions

Derived from:

- highest-priority fix-plan items
- highest-impact recommendations linked to findings

### Effort Bands For Fix Plan

Presentation-only labels such as:

- low effort
- medium effort
- high effort

Derived heuristically from finding scope and recommendation type, not from scoring.

## Optional Enhancements And Deferrals

### Optional In This Pass

Only ship if low-risk and strongly supported by current data:

- lightweight cross-page matrix / heatmap
- persona-aware prioritization
- smarter executive summary language polish

### Explicit Deferrals

Remain out of scope for this pass:

- scoring-engine rewrite
- new framework scoring
- score mutation from review feedback
- full configuration workspace redesign
- export system overhaul
- new AI commentary systems beyond existing supported outputs
- heavyweight new dependencies
- backend-first overview-model redesign

## Testing And Validation Plan

### Unit / Mapping Coverage

Add or update tests for:

- overview summary builder
- maturity/risk label derivation
- top strengths / weaknesses rollups
- top issues / top actions ordering
- fix-plan item derivation
- fix-plan queue ordering and source finding references
- optional matrix builder if implemented
- optional persona reordering helper if implemented

### Payload Coverage

Add or update tests for:

- overview payload shape
- fix-plan payload shape
- Evidence placement / packet-preview payload assumptions

### Webview Coverage

Add or update tests for:

- Overview rendering
- Issues filters
- Issues grouping controls
- Fix Plan rendering
- Evidence collapse defaults
- packet preview positioning
- export remaining secondary
- existing workflows remaining intact

### Validation Commands

Run:

- focused webview tests
- focused extension tests
- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`

Use narrower validation after each material change and full validation before completion.

## Non-Goals

This modernization pass does not:

- add new scoring algorithms
- change framework weights or framework outputs
- re-rank findings by hidden logic
- turn Export into a top-level workspace
- remove framework analysis, metadata, screenshot audit, or scoring internals
- redesign PDF or document export formats unless required for integration
- add speculative cross-page scoring with weak data

## Risks

### 1. Presentation Logic Drift

If overview or fix-plan builders become too smart, they can blur into scoring. This must be prevented through explicit builder boundaries and focused tests.

### 2. React Growth

If too much shaping logic lands in `App.tsx`, the workspace refactor will create another monolith. Builders and small render helpers should keep the page composable.

### 3. Weak Optional Features

Matrix and persona ordering can degrade trust if implemented with thin data. They should ship only if the data mapping is clearly explainable.

### 4. Duplicate Decision Surfaces

Overview, Issues, and Fix Plan must feel complementary rather than repetitive. Each layer needs a distinct purpose:

- Overview explains the report state
- Issues supports triage and inspection
- Fix Plan supports remediation sequencing

## Success Criteria

The score panel should feel like a workspace rather than a long report.

A user should be able to:

1. open the analysis
2. understand overall health quickly
3. see the highest-priority issues
4. filter and inspect findings
5. review recommended fixes
6. drill into evidence only when needed
7. export/share from a dedicated downstream area

Analytical depth must remain available, but it must no longer dominate the default reading path.
