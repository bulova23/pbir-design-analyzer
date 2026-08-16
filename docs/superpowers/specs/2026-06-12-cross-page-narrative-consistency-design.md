# Cross-Page Narrative Consistency Design

Date: 2026-06-12

Status: Design approved for implementation planning; no code changes in this document

## Goal

Design Story Assessment 3.0 as a report-level narrative assessment layer that answers:

- what the report is trying to accomplish
- what role each page plays
- whether the report flows logically
- whether users are guided through a coherent analytical journey

without redesigning Story Assessment 2.2, changing public contracts, adding UI work, or introducing AI-dependent execution.

## Authoritative Inputs

This design uses the following as authoritative inputs:

- `docs/superpowers/specs/2026-06-10-story-assessment-2-design-validation.md`
- `docs/superpowers/specs/2026-06-11-guided-story-improvements-design.md`
- `docs/superpowers/specs/2026-06-12-story-assessment-2-2-design.md`
- `docs/ROADMAP.md`
- current analyzable-surface and analyzer-boundary guidance in `AGENTS.md`

## Business Objective

Story Assessment 2.x answers:

- does this page tell a story

Story Assessment 3.0 should answer:

- does this report tell a story

The product value is not a second copy of page scoring. The value is a deterministic report-level narrative layer that can explain:

- how pages relate to one another
- whether navigation and drill paths reinforce the intended analytical journey
- whether the report contains disconnected, contradictory, or context-breaking pages
- what report-level gaps prevent the report from functioning as a coherent experience

## Planning Boundary

This is a design specification only.

It does not:

- implement code
- change Story Assessment 2.2 behavior
- add score-panel UI
- add or widen score-panel contracts
- expose research-stage Story Assessment internals
- require Design Studio
- require Fabric App implementation
- require AI generation

## Current-State Constraints

Cross-Page Narrative Consistency must preserve the current product boundaries:

- scoring remains authoritative
- page-level Story Assessment outputs remain the only current Story Assessment promotion slice
- normalized findings remain the shared issue model
- analyzable surface, analyzer, and analyzer profile remain separate concepts
- cross-page matrix navigation remains presentation-only and finding-driven
- review and export remain downstream from scoring
- AI proposal enrichment remains advisory-only
- deterministic preview/apply/rollback remains the only report-edit execution path
- shared repository snapshots remain analyzer-independent
- PBIR is the first implementation surface

## Architectural Review Findings

The following risks are ranked by long-term maintenance impact and drive this design.

### 1. Highest Risk: Cross-Page Narrative Reimplements Page-Level Story Assessment

If report-level narrative logic rescans each page with a second set of page heuristics, Story Assessment will fork into two parallel systems that drift in language, evidence, and maintenance cost.

Design response:

- require Cross-Page Narrative to consume existing page-level Story Assessment outputs first
- allow only narrow report-level supplements for page-role and relationship evidence
- prohibit duplicate page-level maturity or gap inference logic

### 2. Highest Risk: Report-Level Narrative Leaks Into Public Contracts Before Validation

The repository has already chosen a validation-first posture for Story Assessment. Promoting report-level narrative labels, scores, or classifications before review evidence exists would repeat the exact failure mode the Story Assessment 2.0 design tried to prevent.

Design response:

- keep the entire Cross-Page Narrative model internal in the first implementation slice
- use the existing validation lifecycle:
  - Internal
  - Level 1 Validated
  - Contract Eligible
  - Production
  - Cross-Surface Candidate
  - Level 2 Validated
  - Platform Critical

### 3. High Risk: Cross-Page Narrative Collides With Existing Report Consistency Features

The repo already has report consistency outputs and cross-page presentation features. If Story Assessment 3.0 introduces another free-standing cross-page subsystem, the architecture will fragment into:

- visual consistency
- issue matrix navigation
- narrative consistency

with overlapping page relationships and no shared vocabulary.

Design response:

- define Cross-Page Narrative as a story-specific report-analysis layer
- reuse existing report metadata, normalized findings, navigation metadata, and report consistency evidence where appropriate
- keep ownership clear:
  - report consistency remains broad cross-page quality checking
  - Cross-Page Narrative remains narrative sequencing and coherence

### 4. High Risk: Navigation Semantics Become PBIR-Specific Dead Ends

The first implementation is PBIR-first, but the architecture now supports multiple analyzable surfaces. If report narrative depends directly on PBIR-only file shapes instead of surface-neutral navigation concepts, future Fabric or other surfaces will require a rewrite.

Design response:

- model report narrative around page graph, role graph, and navigation relationships
- allow PBIR adapters to supply drillthrough, page order, and page metadata
- keep the core narrative evaluator surface-aware but surface-neutral

### 5. Medium Risk: Report-Level Scoring Becomes An Opaque Composite Number

A single score without dimension breakdown or evidence will become hard to validate, hard to defend, and easy to overfit.

Design response:

- define separate report-level dimensions
- require evidence-backed explainability for every dimension
- treat the composite score as a summary, not the primary artifact

## Design Principles

### 1. Report-Level, Not Page-Level Again

Cross-Page Narrative evaluates relationships between pages, not whether each page independently tells a story.

### 2. Internal-First Validation

The first implementation slice should build internal-only models, validation exports, and reviewer workflows before any promotion discussion.

### 3. Downstream From Existing Story Assessment

Page intent, page story maturity, and Guided Story Improvements are inputs. They are not rewritten here.

### 4. Surface-Neutral Core, PBIR-First Adapters

The first shipping implementation may be PBIR-first, but the model must describe:

- page role
- flow relationship
- continuity relationship
- orphan state

in a way that later analyzers can reuse.

### 5. Deterministic And Explainable

No recommendation, narrative score, or narrative role should exist without deterministic evidence that can be exported for review.

## Scope

The design covers:

- page-role classification
- narrative flow analysis
- cross-page consistency checks
- orphan and isolation detection
- report-level narrative scoring
- report-level story-gap recommendations
- validation and rollout strategy

The design does not cover:

- UI presentation
- contract promotion
- report mutation
- Design Studio
- Fabric App-specific execution flows

## Chosen Architecture

Cross-Page Narrative Consistency should be an internal report-analysis layer inside Story Assessment, not a new workspace and not a replacement for report consistency.

Recommended flow:

`shared repository snapshot`
`+ report page metadata`
`+ existing page-level Story Assessment outputs`
`+ existing navigation/drill metadata`
`+ normalized findings and report consistency context`
`-> page role classifier`
`-> narrative graph builder`
`-> continuity and orphan evaluators`
`-> report narrative strength scorer`
`-> report story-gap builder`
`-> internal validation/export artifacts`

This keeps scoring authoritative while preserving the existing product hierarchy:

page Story Assessment  
↓  
report Cross-Page Narrative  
↓  
future narrow promotion decisions  
↓  
presentation consumers

## Core Internal Model

The report-level model should be internal-only in Phase 1 and should be shaped around five units.

### 1. Page Narrative Role

Determines what job a page plays inside the report.

### 2. Narrative Relationship Graph

Represents expected and observed relationships between pages:

- sequence adjacency
- drillthrough edge
- shared topic continuity
- summary-to-detail edge
- supporting-context edge

### 3. Consistency Assessment

Evaluates whether adjacent or related pages maintain coherent business framing.

### 4. Orphan Assessment

Determines whether a page is disconnected from the main narrative path.

### 5. Narrative Strength Assessment

Produces dimensional report-level ratings, evidence, and actionable gaps.

## Narrative Role Model

### Role Set

The initial role taxonomy should be:

- Overview
- Executive Summary
- Operational Monitor
- Comparative Analysis
- Diagnostic Investigation
- Detail Drill
- Scenario Exploration
- Exception Analysis
- Supporting Context
- Reference / Legal
- Tooltip
- Q&A
- Validation / Sandbox

### Role Definitions

#### Overview

Landing or orienting page that introduces the report topic, primary KPIs, and broad navigation path.

#### Executive Summary

Decision-facing summary page that emphasizes overall posture, high-level takeaways, and limited next-step direction.

#### Operational Monitor

Steady-state monitoring page focused on recurring KPIs, status, threshold tracking, and operational review.

#### Comparative Analysis

Page centered on comparisons across regions, products, segments, or other dimensions.

#### Diagnostic Investigation

Page intended for root-cause analysis, decomposition, or contributor inspection.

#### Detail Drill

Page that exists mainly to receive drillthrough or contextual descent from a higher-level page.

#### Scenario Exploration

Interactive what-if or scenario page used to compare modeled outcomes.

#### Exception Analysis

Page focused on anomalies, outliers, failures, or threshold breaches.

#### Supporting Context

Supplementary page that reinforces the main narrative but is not itself a primary chapter.

#### Reference / Legal

Compliance, methodology, notes, glossary, definitions, or legal text page.

#### Tooltip

Tooltip or hover-helper page that should not behave like a normal report chapter.

#### Q&A

Conversational, exploratory, or generated-answer page that should not be treated as a narrative chapter.

#### Validation / Sandbox

Author-testing, QA, or scratch page not intended for the primary end-user journey.

### Role Confidence

Role confidence should be:

- High
- Medium
- Low

Confidence is determined from:

- direct evidence strength
- absence of conflicting evidence
- stability of the role across multiple signal families
- whether the page matches a special-page suppression class

### Role Assignment Rules

Role assignment should be deterministic and evidence-weighted.

Input families:

- page name and page-order position
- page-level Story Assessment outputs:
  - inferred story
  - story maturity
  - guided story improvements
- visual mix and layout posture
- presence of detail tables, decomposition visuals, slicers, and scenario controls
- drillthrough source/target relationships
- page metadata hints already available to PBIR analysis
- existing special-page guardrails

Assignment rules:

- assign exactly one primary role per page
- allow zero or more secondary hints internally for reviewer export
- special-page classes win over ordinary role inference
- detail drill requires either explicit drillthrough evidence or strong detail-page evidence
- supporting context should be a fallback role only when the page contributes relevant context but lacks a stronger narrative chapter identity

### Role Promotion Criteria

No role label is eligible for public contract promotion until Level 1 validation shows:

- strong reviewer agreement on role identity for at least the primary non-special roles
- explainable evidence traces for assigned roles
- low ambiguity between adjacent roles such as:
  - Overview vs Executive Summary
  - Comparative Analysis vs Diagnostic Investigation
  - Supporting Context vs Reference / Legal
- clear suppression reliability for:
  - Tooltip
  - Q&A
  - Validation / Sandbox

Recommended promotion sequence:

1. internal only for all roles
2. Level 1 review for role accuracy and confidence calibration
3. consider promotion only for a reduced public vocabulary if reviewer confusion remains high

## Narrative Flow Analysis

### Core Question

Do the pages form a coherent analytical journey rather than a disconnected set of views?

### Flow Units

The evaluator should inspect:

- page ordering
- explicit page navigation paths where available
- drillthrough relationships
- summary-to-detail descent
- report hierarchy
- report segmentation into chapters or islands

### Narrative Graph

The narrative graph should model:

- nodes:
  - pages
- edges:
  - ordered-next
  - ordered-previous
  - drillthrough
  - reverse-drill-support
  - topic-continuity
  - role-compatible-transition
  - segment-membership

The graph should separate:

- observed edges from report structure
- inferred edges from narrative compatibility

### Good-Flow Expectations

Typical strong patterns include:

- Overview -> Executive Summary -> Comparative Analysis -> Detail Drill
- Executive Summary -> Operational Monitor -> Exception Analysis -> Diagnostic Investigation
- Overview -> Performance Summary -> Regional Breakdown -> Store Detail

### Weak-Flow Patterns

Common weak patterns include:

- abrupt jump from high-level decision page to legal/reference page without transition
- drill target that changes business domain unexpectedly
- detail page appearing before any framing page
- narrative sequence that alternates between unrelated domains
- tooltip or sandbox pages appearing inside the primary journey

### Flow Evaluation Dimensions

Evaluate:

- opening adequacy:
  - does the report start with a framing page
- transition logic:
  - do adjacent pages have compatible roles and topics
- depth progression:
  - does the report move from summary to detail in understandable steps
- segmentation quality:
  - are narrative islands intentional and coherent
- drill alignment:
  - do drillthrough relationships reinforce the primary path

## Cross-Page Consistency Model

Cross-Page Narrative should evaluate narrative consistency, not generic styling consistency.

Required checks:

- KPI continuity
- metric naming continuity
- dimension naming continuity
- narrative focus continuity
- business objective alignment

Examples of negative checks:

- revenue-focused summary page drilling into headcount detail without explicit business-context bridge
- executive summary disconnected from the pages that contain its supporting evidence
- page sequence that changes the business question without signaling the transition
- adjacent pages using materially different labels for the same metric or dimension

### Consistency Evidence Sources

Use:

- page titles and visible framing text
- page-level inferred story summaries
- page-level strong signals and missing signals
- public metric and category hints already captured in visual metadata
- existing report consistency outputs where they provide supporting context
- drillthrough mappings and page adjacency

### Consistency Severity

Narrative consistency findings should be rated:

- High:
  - breaks interpretation or trust
- Medium:
  - weakens continuity but the user can still recover
- Low:
  - creates friction without collapsing the main story

## Orphan Detection

### Definitions

#### Orphaned Page

A page that contributes little or nothing to the report’s dominant narrative and has weak relationship evidence to the main graph.

#### Unreachable Page

A page that lacks meaningful incoming navigation or drill support from the primary narrative path when its role implies it should be reachable.

#### Unused Drill Target

A detail or drill page with drill-oriented structure but no meaningful inbound drill relationship.

#### Isolated Analysis Island

A small cluster of related pages that connect internally but not to the report’s main journey.

### Detection Rules

Flag orphan risk when a page shows several of the following:

- low role confidence
- no strong topic continuity with adjacent pages
- no inbound or outbound narrative edge to the primary segment
- special-page indicators embedded among primary pages
- detail/drill posture without any valid narrative parent
- repeated business-objective mismatch against the dominant report objective

Suppression rules:

- Reference / Legal, Tooltip, Q&A, and Validation / Sandbox may be intentionally disconnected and should usually downgrade to advisory orphan warnings instead of hard failures

## Narrative Strength Model

### Report-Level Dimensions

The first scoring model should use:

- Flow
- Consistency
- Navigation
- Continuity
- Actionability

### Dimension Definitions

#### Flow

How well the report moves through a logical chapter order.

#### Consistency

How well pages preserve business framing, terminology, and topic alignment.

#### Navigation

How well explicit navigation and drill relationships support the intended journey.

#### Continuity

How well adjacent and related pages feel like parts of one story instead of separate analyses.

#### Actionability

How well the report helps a user progress from overview to interpretable detail with usable decision support.

### Scoring Model

Use a bounded internal 0-100 dimension model with a composite report narrative score.

Recommended initial weighting:

- Flow: 25
- Consistency: 20
- Navigation: 20
- Continuity: 20
- Actionability: 15

Composite score usage rules:

- never use the composite without the dimension breakdown
- do not promote the composite publicly before Level 2 validation
- use the composite mainly for validation export ranking and regression detection

### Confidence Model

Narrative score confidence should be separate from the score itself and should evaluate:

- evidence density
- graph completeness
- role certainty
- contradiction rate between evidence families
- navigation metadata availability

Recommended confidence labels:

- High
- Medium
- Low

Low confidence should:

- reduce severity of derived recommendations
- explicitly call out missing evidence domains
- block any promotion consideration

### Explainability Model

Each dimension should produce:

- rating or score
- strongest supporting evidence
- strongest weakening evidence
- missing evidence notes
- affected pages
- dominant narrative summary

The report-level summary should answer:

- what the report appears to be trying to accomplish
- what narrative path it currently follows
- what most weakens that journey

## Report-Level Story Gaps

Cross-Page Narrative should create report-level recommendations that are:

- actionable
- consultant-friendly
- evidence-backed
- bounded to the report layer unless evidence clearly points elsewhere

### Initial Gap Set

Recommended gap categories:

- Missing Executive Entry Point
- Missing Narrative Bridge
- Missing Drill Path
- Broken Drill Alignment
- Inconsistent KPI Hierarchy
- Inconsistent Naming Layer
- Disconnected Analysis Page
- Orphan Detail Page
- Unsignaled Context Shift
- Fragmented Report Segmentation

### Recommendation Shape

Each report-level gap should contain:

- stable internal identifier
- title
- summary of the narrative problem
- why it matters
- expected impact
- affected pages
- evidence references
- confidence
- actionability assessment
- recommended remediation layer

### Gap Suppression Rules

Suppress or downgrade recommendations when:

- the report is dominated by non-narrative utility pages
- evidence is too sparse for a defensible dominant-report objective
- the mismatch is explainable by a valid appendix/reference segment
- the only problem is low-confidence role inference

## Dominant Report Objective

Cross-Page Narrative should infer one internal dominant report objective for evaluation purposes.

Examples:

- executive performance review
- operational monitoring
- comparative business analysis
- diagnostic investigation

This objective is internal-only in Phase 1 and is used to judge:

- whether page roles fit together
- whether context shifts are intentional
- whether orphan detection is meaningful

## Relationship To Existing Features

### Story Assessment 2.2

Cross-Page Narrative consumes Story Assessment 2.2 outputs but does not modify them.

It must:

- reuse page-level Story Assessment outputs
- leave deep-link navigation unchanged
- leave diff mode unchanged

### Guided Story Improvements

Guided Story Improvements remain page-level and validated. Cross-Page Narrative should not expand or reinterpret those six categories as report-level public recommendations.

Instead:

- page-level Guided Story Improvements remain inputs
- report-level gaps are separate internal records

### Issues

No new Issues contract is introduced in this design. If future promotion occurs, normalized findings should remain the shared issue model for any promoted report-level narrative findings.

### Fix Plan

Fix Plan remains downstream. Cross-Page Narrative must not create a second remediation execution path.

### Fabric App Readiness

No dependency. Cross-Page Narrative should remain compatible with multi-surface architecture but does not require Fabric App readiness features.

### Fabric App Review

No implementation dependency. The core model should be surface-neutral enough that Fabric App Review could later provide its own page-role and navigation adapters without changing the report-level evaluator shape.

## PBIR-First Implementation Boundary

Phase 1 should support PBIR only.

PBIR adapters may use:

- page order
- page names
- drillthrough metadata
- visual metadata and title hints
- report-level metadata already available through PBIR scoring

Phase 1 should not require:

- Fabric App file traversal
- screenshot intelligence
- semantic-model lineage expansion beyond already-available evidence
- AI classification

## Validation Strategy

Cross-Page Narrative should follow the same validation-first discipline already established for Story Assessment 2.0.

### Level 1 Validation

Purpose:

- determine whether the internal model is accurate enough to remain in active development and whether any subset is eventually contract-eligible

Required process:

- expert review on a curated PBIR report corpus
- reviewer rubric focused on report-level narrative coherence
- review of:
  - dominant report objective
  - page roles
  - main narrative path
  - orphan decisions
  - report-level gaps

Recommended corpus:

- 12 to 20 reports
- each with multiple pages
- deliberate mix of:
  - executive summary reports
  - operational monitoring reports
  - drill-heavy analysis reports
  - mixed-quality reports
  - reports with appendix/reference pages

Reviewer rubric questions:

1. What is the report trying to accomplish?
2. What role does each page play?
3. Does the report follow a coherent analytical journey?
4. Which pages feel disconnected or misplaced?
5. Do drill paths support the narrative?
6. Which report-level gaps most weaken decision support?

Level 1 outputs:

- reviewer agreement on primary role labels
- reviewer agreement on dominant report objective
- precision and recall observations for orphan detection
- usefulness assessment of report-level gaps
- documented false positives and false negatives

### Level 2 Validation

Purpose:

- determine whether any narrative dimensions are stable enough for platform-critical usage

Required process:

- larger PBIR report corpus
- multiple reviewers
- consistency measurements
- calibration of confidence thresholds
- promotion criteria for any public or platform-critical use

Recommended scale:

- 50 or more reports

Promotion criteria for platform-critical consideration:

- strong inter-reviewer agreement on primary page roles
- acceptable stability for flow and orphan findings
- evidence that low-confidence cases are safely downgraded
- regression harness coverage on curated canonical reports

## Rollout Strategy

Recommended rollout:

### Phase 1: Internal Model Foundation

- internal page-role classifier
- internal narrative graph
- internal flow, consistency, and orphan assessments
- validation export support

### Phase 2: PBIR Corpus Validation

- Level 1 review corpus
- rubric completion
- confidence calibration
- report-level gap tuning

### Phase 3: Promotion Review

- decide whether any subset is contract-eligible
- likely candidates:
  - report-level gaps only
- likely non-candidates until Level 2:
  - detailed role taxonomy
  - composite report narrative score
  - full narrative graph

### Phase 4: Future Cross-Surface Adaptation

- evaluate which evidence families are cross-surface candidates
- keep PBIR-specific adapters separate from core evaluator logic

## Recommended Non-Promotion Stance

The safest initial posture is:

- keep page-role labels internal
- keep the report narrative score internal
- keep the dominant report objective internal
- consider only report-level story-gap promotion after validation

This matches the repository’s existing preference for narrow promotion of actionable advisory outputs rather than early exposure of research-stage classification layers.

## Definition Of Done For The Future Implementation

The implementation corresponding to this design should be considered complete only when:

1. internal page-role classification exists
2. internal flow, consistency, and orphan assessment exists
3. internal report-level narrative scoring exists
4. report-level story-gap records exist
5. validation export artifacts exist
6. Level 1 validation process is documented and runnable
7. no public contract or UI change is required for the first slice
