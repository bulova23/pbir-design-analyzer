# Issues Workspace Foundation Design

Date: 2026-05-30

Status: Approved direction captured; ready for implementation planning

## Goal

Establish the first high-leverage UI/UX modernization slice for the PBIR Design Analyzer score panel by normalizing findings and making the Issues workspace the primary review surface.

This slice is intentionally narrower than the broader modernization vision. It does not attempt to ship the full five-zone workspace shell. It builds the data model and workflow foundation that later `Overview`, `Fix Plan`, `Export`, persona-aware prioritization, and heatmap features will depend on.

## Why This Slice Comes First

The current score panel is still organized around mixed-purpose sections and source-specific data structures:

- framework feedback items
- review summary state
- cross-page consistency issues
- quick-fix recommendations
- screenshot audit findings
- metadata-derived observations
- benchmark and actionability cards

These structures are useful, but they are not normalized into one review object model. As a result, the UI remains framework-first and section-stacked. Any attempt to build report-level overview, richer filtering, or persona-aware prioritization before normalizing findings would hard-code more UI around scattered payloads.

The normalized finding model is therefore the keystone.

## Current-State Constraints

The current report UI already has meaningful capability that must be preserved:

- deterministic framework scoring and feedback
- per-page and report-level consistency signals
- deterministic story, intent, actionability, and benchmark summaries
- screenshot-audit findings with rendered-vs-metadata source labels
- metadata inspection
- review packet preview/export workflow

The problem is not analytical depth. The problem is that the score panel exposes too many primary sections before the user can quickly answer:

- what is wrong
- how bad it is
- how confident the analyzer is
- what pages are affected
- what should be fixed first

## Approved Scope

This first implementation slice will:

1. Introduce a normalized findings model.
2. Build an Issues workspace as the primary review surface.
3. Render first-class finding attributes:
   - severity
   - confidence
   - scope
   - detection type
   - affected pages
   - impact area
   - framework impact
   - recommendation
4. Move framework, metadata, and screenshot-audit details behind an Evidence section and drilldown.
5. Add smart-collapse defaults so the UI emphasizes issue triage over exhaustive reading.

## Explicit Deferrals

The following are intentionally out of scope until the finding model stabilizes:

- full cross-page heatmap
- persona-aware prioritization
- export workspace redesign
- advanced configuration workspace integration
- maturity/risk scoring refinements
- full five-zone page shell

## Design Principles For This Slice

### 1. Issues First

The default reading path should begin with normalized findings, not framework cards or export tooling.

### 2. Preserve Explainability

Framework scoring, metadata, and audit evidence remain available, but they move behind issue drilldown and an Evidence section.

### 3. Normalize Before Optimizing

Do not add advanced navigation or persona-specific sorting until the score panel can render all major findings through one consistent contract.

### 4. Avoid Shell-First Refactoring

Do not start by building a full multi-zone page skeleton and then backfilling content. Build the finding model and issue workflow first, then layer broader page architecture around it later.

## Proposed Architecture

### A. Normalized Finding Contract

Create a first-class UI contract that can represent findings from multiple analyzer subsystems.

Each finding should include:

- `id`
- `title`
- `summary`
- `severity`
- `confidence`
- `scope`
- `detectionType`
- `affectedPages`
- `impactArea`
- `frameworkImpact`
- `recommendation`
- `evidence`
- `sourceKind`
- `sourceSection`

Recommended enums:

- `severity`: `high | medium | low | info`
- `confidence`: `0-100` numeric score plus optional label helper
- `scope`: `visual | page | crossPage | report`
- `detectionType`: `deterministic | aiAssisted | mixed`
- `impactArea`: `layout | storytelling | accessibility | governance | density | navigation | kpiEffectiveness | benchmark | actionability | metadata`

The contract should be frontend-owned first. It does not need an immediate backend rewrite. The score payload normalizer can derive the new finding objects from existing score result structures.

### B. Findings Builder Layer

Add a focused transformation layer that maps current report structures into normalized findings.

Initial sources should include:

- report consistency issues
- failed or weak framework feedback items
- actionability gaps
- benchmark gaps
- quick-fix-derived issues where they represent concrete remediation
- screenshot audit findings

This builder should produce a single collection for overall-report and per-page rendering.

### C. Issues Workspace

The Issues workspace becomes the new primary review surface.

Default behavior:

- show grouped findings
- sort by severity first, then confidence
- show compact cards first
- keep details collapsed by default

Each issue card should visibly answer:

- what is wrong
- why it matters
- how severe it is
- how confident the analyzer is
- what pages or visuals are affected
- which frameworks are implicated
- what to do next

The first slice does not require full search, heatmaps, or persona-driven ranking. It needs a strong single-path triage workflow.

### D. Evidence Relocation

Framework accordions, metadata explorers, and screenshot-audit deep detail should move behind:

- per-finding drilldown content, or
- a dedicated lower-priority Evidence section

The important behavior change is that evidence supports findings rather than competing with them.

### E. Smart-Collapse Defaults

Default expansion rules:

- Issues workspace visible and expanded
- finding groups expanded only for the highest-severity bucket
- individual finding details collapsed by default
- Evidence section collapsed by default
- packet preview, metadata detail, and audit detail no longer in the main reading path

## Information Architecture For The First Slice

This slice does not yet create the full final shell. The intended order is:

1. Existing summary/header area, lightly compressed if needed
2. Issues workspace
3. Supporting helper sections still needed for current workflows
4. Evidence section

This preserves current capability while clearly changing the center of gravity of the page.

## Data Ownership And Boundaries

### Contract Boundary

The normalized finding contract should live in the score panel contract layer so both host-side payload shaping and webview rendering share the same schema.

### Transformation Boundary

Finding derivation logic should live in a dedicated score/presentation helper rather than inside the React component. `App.tsx` should consume already-shaped findings instead of inventing them inline.

### Presentation Boundary

The Issues workspace rendering should be componentized enough that finding cards, finding groups, and evidence blocks can evolve independently. The goal is to prevent `App.tsx` from becoming a larger monolith during this transition.

## Risks

### Risk 1: Duplicate Or Conflicting Findings

Multiple subsystems may describe the same underlying problem differently. The first slice should prefer deterministic clarity over over-aggressive deduplication. Light grouping is safer than premature merging logic.

### Risk 2: Losing Evidence Fidelity

If evidence is hidden too aggressively, expert users may feel capability was removed. The design must keep drilldown obvious and preserve source attribution.

### Risk 3: UI Churn Without Stable Semantics

If the finding model is too loosely defined, the Issues workspace becomes another re-skin. The schema needs stable meaning before later overview, filtering, and persona work lands.

## Success Criteria

This slice is successful when:

- the score panel has a first-class normalized finding array
- the primary review surface is issue-centric rather than framework-centric
- a user can scan the default view and understand the main problems without reading framework details
- framework, metadata, and audit content remain available but become secondary evidence
- later work on overview, fix plan, heatmaps, and personas can build on the finding model instead of bypassing it

## Recommended Next Order After This Slice

1. Overview
2. Fix Plan
3. Evidence refinement
4. Export cleanup
5. Persona-aware prioritization
6. Cross-page heatmap

This ordering preserves the dependency chain you approved: normalize findings first, then layer more advanced workspace structure around that stable foundation.
