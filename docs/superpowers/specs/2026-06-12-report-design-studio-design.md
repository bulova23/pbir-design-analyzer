# Report Design Studio Design

Date: 2026-06-12

Status: Design approved for implementation planning; no code changes in this document

## Goal

Design Report Design Studio as the design and refinement companion to PBIR Design Analyzer so users can design better reports before they are built, while preserving the platform's validation-first philosophy.

Report Design Studio should help users move through:

Design Brief  
↓  
Concept Studio  
↓  
Draft Studio  
↓  
Refinement Studio  
↓  
Materialize  
↓  
Analyzer Workspace  

without turning the platform into an AI report generator and without bypassing review, assessment, improvement, or validation.

## Authoritative Inputs

This design uses the following as authoritative inputs:

- `AGENTS.md`
- `docs/ROADMAP.md`
- `docs/superpowers/specs/2026-06-12-story-assessment-2-2-design.md`
- `docs/superpowers/specs/2026-06-12-cross-page-narrative-consistency-design.md`
- current analyzable-surface, analyzer, analyzer-profile, and validation-boundary guidance in repo memory

## Business Objective

PBIR Design Analyzer already supports:

- review
- story assessment
- Guided Story Improvements
- Issues
- Fix Plan
- direct navigation
- Story Assessment comparison over time

The next product evolution is to help users shape better analytics experiences before implementation details harden into reports.

Report Design Studio should bridge:

Prompt  
↓  
Generate  
↓  
Modify  
↓  
Deploy

and:

Review  
↓  
Assess  
↓  
Improve  
↓  
Validate

The resulting platform direction is:

Analytics Experience Design Platform

not:

AI Report Generator

## Planning Boundary

This is a design specification only.

It does not:

- implement code
- commit to a public release
- implement generation
- implement AI providers
- implement Microsoft skills integration
- implement Design Studio UI
- modify Story Assessment logic
- bypass existing review and validation flows

## Fixed Architecture Decisions

The following are fixed for this design:

1. Report Design Studio is a peer workflow to the analyzer workspace.
2. Design artifacts are first-class internal objects.
3. Analyzable surfaces are derived objects.
4. Materialization is the explicit trust and architecture boundary between creation and validation.
5. The analyzer workspace remains the authoritative quality gate.
6. Design Studio may propose concepts, structures, alternatives, and refinements.
7. Design Studio may not bypass Story Assessment, Guided Story Improvements, Issues, Fix Plan, or validation.

## Current-State Constraints

Report Design Studio must preserve the existing platform boundaries:

- scoring remains authoritative
- normalized findings remain the shared issue model
- analyzable surface, analyzer, and analyzer profile remain separate concepts
- review and export remain downstream from scoring
- AI proposal enrichment remains advisory-only
- deterministic preview/apply/rollback remains the only report-edit execution path
- shared repository snapshots must remain analyzer-independent and reusable
- the analyzer workspace remains a review and validation surface, not a creation surface
- public Story Assessment exposure remains intentionally narrow
- future provider integrations must remain optional and provider-neutral

## Architectural Review Findings

The following risks are ranked by long-term maintenance impact and drive this design.

### 1. Highest Risk: Design Studio Becomes A Second Analyzer Workspace

If Report Design Studio is built as another tab set inside the current score-panel workspace, the platform will blur creation and validation responsibilities. That would make the analyzer less authoritative, create duplicated workflow logic, and make future non-review surfaces harder to support.

Design response:

- keep Report Design Studio as a separate workflow
- let it consume analyzer outputs rather than embed analyzer ownership
- keep the analyzer workspace as the quality gate

### 2. Highest Risk: Drafts Become Analyzer-Shaped Too Early

If concepts and drafts are treated as analyzable surfaces from the beginning, the system will drift into a generate-score-generate-score loop. That would collapse design intent into scoring mechanics and make non-materialized design artifacts second-class.

Design response:

- make briefs, concepts, drafts, alternatives, and refinements first-class design artifacts
- derive analyzable surfaces only through explicit materialization
- keep design artifacts valuable even when no PBIR exists yet

### 3. High Risk: Provider Models Leak Into Core Product Architecture

If the studio is defined around Microsoft skills, a specific AI provider, or current PBIR generation patterns, the architecture will be locked to one generation model and will be expensive to extend to Fabric Apps, future surfaces, or non-Microsoft providers.

Design response:

- create provider-neutral design and generation interfaces
- treat Microsoft skills as optional providers, not core architecture
- keep provider calls behind advisory orchestration seams

### 4. High Risk: Design Proposals Gain Mutation Authority

If the studio can silently generate, modify, or deploy report assets, the platform will violate the current trust model and collapse the deterministic preview/apply/rollback boundary.

Design response:

- prohibit silent generation, silent modification, direct deployment, and hidden mutations
- require preview, review, approval, and validation before any report change is applied
- route any future artifact-to-report mutation through the existing deterministic execution path

### 5. High Risk: Shared Infrastructure Fragments Across Workflows

If Design Studio builds its own repository reading, navigation, analyzer discovery, or evidence models, the repository will accumulate duplicate infrastructure that drifts over time.

Design response:

- reuse shared repository snapshots
- reuse analyzable surface vocabulary
- reuse analyzer registry concepts
- reuse navigation target contracts where validation workflows need to round-trip into review
- keep workflow-specific orchestration separate from shared infrastructure

### 6. Medium Risk: Closed-Loop Optimization Becomes Opaque Automation

If closed-loop refinement is framed as automatic optimization, users will lose visibility into why a concept changed and whether the result actually improved.

Design response:

- design a visible loop:
  - Draft
  - Assess
  - Improve
  - Re-Assess
  - Compare
  - Approve
- keep each refinement proposal reviewable and attributable
- reuse diff-style comparison thinking rather than hiding revisions

## Design Principles

### 1. Design And Validation Stay Separate

Design Studio is a creation environment.
Analyzer Workspace is a validation environment.

### 2. Artifacts First

Briefs, concepts, drafts, alternatives, and refinements are primary internal product objects, not temporary prompts.

### 3. Materialization Is Explicit

No design artifact becomes analyzable until it is intentionally materialized into a reviewable surface shape.

### 4. Provider Neutrality By Default

Provider integrations are adapters, not architectural dependencies.

### 5. Advisory Before Authority

Design Studio proposes.
Analyzer Workspace evaluates.
Deterministic execution applies changes only after approval.

### 6. Validation First

No new design output should be promoted to user-facing authority until it can be evaluated through the same validation-first discipline used elsewhere in the repo.

## Chosen Architecture

Report Design Studio should be a separate workflow sharing core platform infrastructure with the analyzer workspace.

Recommended high-level flow:

`shared repository snapshot`
`+ design brief`
`+ concept artifacts`
`+ draft artifacts`
`+ refinement alternatives`
`+ optional provider outputs`
`-> design artifact store`
`-> materialization gateway`
`-> analyzable surface candidate`
`-> analyzer registry`
`-> analyzer workspace`
`-> Story Assessment`
`-> Guided Story Improvements`
`-> Issues`
`-> Fix Plan`
`-> compare and refine loop`

This preserves the platform hierarchy:

Design Studio artifacts  
↓  
materialization boundary  
↓  
analyzable surface  
↓  
analyzer workspace  
↓  
validated outputs  
↓  
refinement input back into Design Studio

## Shared Infrastructure Versus Separate Workflow Ownership

### Shared Platform Infrastructure

Shared across Design Studio and Analyzer Workspace:

- repository snapshot
- analyzable surface model
- analyzer registry
- analyzer profile registry
- navigation target contracts
- Story Assessment outputs
- Guided Story Improvements outputs
- Issues outputs
- Fix Plan outputs
- Cross-Page Narrative outputs
- comparison and snapshot primitives where applicable

### Design Studio-Owned Workflow Areas

Owned by Report Design Studio:

- design brief workflow
- concept workflow
- draft workflow
- alternatives workflow
- refinement workflow
- provider orchestration for design assistance
- materialization request workflow

### Analyzer-Owned Workflow Areas

Owned by Analyzer Workspace:

- assessment
- validation
- findings generation
- recommendation generation
- review-facing navigation
- approval gating for validated changes

## Core Workflow Model

Report Design Studio contains five architectural capabilities.

### 1. Design Briefs

Purpose:

- capture intended user, business, and narrative goals before structure generation begins

Output:

- intended story contract

### 2. Concept Studio

Purpose:

- shape report ideas before implementation assets exist

Output:

- concept-only recommendations for chapters, pages, KPI hierarchy, navigation, and analytical flow

### 3. Draft Studio

Purpose:

- create isolated, reviewable, non-production draft structures

Output:

- draft report and page design artifacts

### 4. Refinement Studio

Purpose:

- consume validated analyzer outputs and propose alternatives and improvements

Output:

- advisory layout, KPI, navigation, and story refinement alternatives

### 5. Closed-Loop Optimization

Purpose:

- connect design artifacts to repeated analyze-improve-compare cycles without blurring ownership

Output:

- controlled design iteration history with explicit approvals

## Design Artifact Model

Design artifacts should be internal, first-class objects with stable identities and lifecycle metadata.

The initial artifact family should include:

- DesignBrief
- ReportConcept
- PageConcept
- NavigationConcept
- KpiHierarchyConcept
- DraftReportArtifact
- DraftPageArtifact
- DraftLayoutArtifact
- RefinementProposal
- MaterializationRequest
- MaterializedSurfaceCandidate
- DesignIterationRecord

### Shared Artifact Metadata

Each design artifact should carry:

- stable internal id
- parent design thread id
- artifact kind
- version
- status
- source provenance
- created at
- updated at
- author source
  - user
  - provider
  - system
- approval state
- validation linkage

### Artifact Lifecycle States

Recommended lifecycle vocabulary:

- Draft
- Proposed
- Reviewed
- Approved
- Materialized
- Analyzed
- Superseded
- Archived

This lifecycle is separate from analyzer promotion state.

## Design Brief Model

The Design Brief is the intended story contract for the design workflow.

Recommended Design Brief shape:

- audience
- business objective
- key decisions
- primary KPIs
- dimensions
- intended story
- success criteria
- report type
- navigation expectations
- consumption context
- decision cadence
- narrative risks or constraints
- required evidence domains
- target analyzable surface family

### Design Brief Semantics

- audience describes who will use the experience
- business objective describes why the report should exist
- key decisions describe what user actions or interventions the report should support
- primary KPIs and dimensions define the intended analytical spine
- intended story defines the narrative contract the design should aim to satisfy
- success criteria define how the user will judge design usefulness
- report type and navigation expectations define expected experience shape

The brief should be required before concept generation becomes authoritative inside the studio workflow.

## Concept Studio Architecture

Concept Studio should transform a Design Brief into concept artifacts, not report assets.

Concept Studio responsibilities:

- page recommendations
- KPI hierarchy definition
- report chapter definition
- navigation structure proposal
- analytical flow proposal
- story sequencing proposal
- alternative concept comparison

Concept Studio outputs:

- report chapter map
- page concept set
- KPI hierarchy concept
- navigation concept
- analytical flow concept
- optional alternate concepts

Concept Studio should not:

- emit PBIR files
- mutate reports
- materialize analyzable surfaces automatically

## Draft Studio Architecture

Draft Studio should transform approved concepts into isolated draft design artifacts.

Draft Studio responsibilities:

- generate draft report structures
- generate draft page structures
- generate KPI layout frameworks
- generate navigation frameworks
- preserve isolation from production assets
- attach provenance to any provider-assisted output

Draft Studio outputs:

- DraftReportArtifact
- DraftPageArtifact
- DraftLayoutArtifact
- DraftNavigationArtifact
- provider provenance notes

Draft Studio design constraints:

- drafts are non-production
- drafts are reviewable
- drafts may be incomplete
- drafts may exist without a corresponding PBIR
- drafts may support future non-PBIR surfaces

## Refinement Studio Architecture

Refinement Studio should consume validated analyzer outputs and translate them into advisory design alternatives.

Primary inputs:

- Story Assessment
- Guided Story Improvements
- Issues
- Fix Plan
- Cross-Page Narrative
- comparison and snapshot outputs

Refinement Studio responsibilities:

- propose layout alternatives
- propose KPI alternatives
- propose navigation alternatives
- propose story-structure alternatives
- map validated findings back to specific design artifacts
- preserve proposal provenance and confidence

Refinement Studio outputs:

- RefinementProposal
- design alternative bundles
- rationale linked to analyzer outputs
- compareable before and after design states

Refinement Studio must not:

- mutate reports directly
- treat analyzer outputs as editable truth
- bypass materialization or deterministic change application

## Materialization Gateway

Materialization is the explicit boundary between design and validation.

### Purpose

Convert approved design artifacts into analyzable surface candidates that existing analyzer contracts can evaluate.

### Why A Gateway Exists

- preserve design artifact independence
- make validation explicit
- prevent continuous hidden scoring during design authoring
- keep analyzer compatibility a derived concern
- support future surfaces and providers without reshaping the whole studio

### Materialization Inputs

- approved Design Brief
- approved concept artifacts
- selected draft artifacts
- optional refinement proposals
- target surface family
- target analyzer and profile

### Materialization Outputs

- MaterializedSurfaceCandidate
- materialization diagnostics
- provenance trace
- explicit handoff metadata into Analyzer Workspace

### Materialization Modes

Recommended initial modes:

- concept-to-structure preview
- draft-to-analyzable-surface candidate
- refinement-proposal-to-candidate comparison

### Materialization Rules

- materialization must be explicit
- materialization must be previewable
- materialization must be attributable to source artifacts
- materialization must not silently apply report changes
- materialization does not imply approval or deployment

### Materialization Boundary Semantics

Before materialization:

- design artifacts are design-owned
- Story Assessment is not authoritative over them

After materialization:

- a derived analyzable surface candidate may enter analyzer workflows
- findings apply to the derived candidate, not retroactively as silent truth over the underlying design artifacts

## Provider-Neutral Integration Model

Report Design Studio should support optional providers through stable platform interfaces.

### Provider Categories

- design-assistance providers
- generation providers
- screenshot-iteration providers
- semantic-model-aware recommendation providers
- Microsoft Fabric or Power BI authoring skill providers

### Provider Responsibilities

Providers may:

- suggest concepts
- propose draft structures
- propose alternatives
- annotate rationale
- assist materialization preparation

Providers may not:

- become required for core workflow operation
- bypass approval or validation
- mutate reports directly
- replace analyzer authority

### Core Provider Interface Expectations

Each provider integration should expose:

- capability metadata
- supported artifact kinds
- supported surface families
- provider provenance output
- failure and degradation behavior
- optionality posture

### Microsoft Skills Integration Posture

Microsoft Fabric skills, Power BI authoring skills, PBIR generation patterns, screenshot iteration patterns, and semantic-model-aware generation should be designed as optional providers behind the same interfaces.

The product must remain viable without them.

## Relationship To Existing Features

Report Design Studio consumes existing systems. It does not replace them.

### Story Assessment

- remains the primary page-level story quality gate after materialization

### Guided Story Improvements

- feeds Refinement Studio proposal generation

### Issues

- provides structured problem statements tied to derived analyzable surfaces

### Fix Plan

- supplies downstream validated remediation thinking to inform refinement proposals

### Deep Links

- support round-trip navigation from analyzer findings back to materialized draft context and then to source design artifacts when mappings exist

### Diff Mode

- informs closed-loop comparison between earlier and later materialized candidates

### Cross-Page Narrative

- acts as report-level narrative validation input for concepts, navigation structures, and chapter flow

### Fabric App Review

- remains a separate analyzer workflow that the studio may target later through the same materialization concept

## Trust Boundary Rules

Never allow:

- silent report generation
- silent report modification
- direct deployment
- hidden mutations
- direct provider-to-report execution
- direct Design Studio bypass of analyzer validation

Always require:

- preview
- review
- approval
- validation

before any report changes are applied.

### Authority Model

- Design Studio may propose
- providers may assist
- analyzers may evaluate
- deterministic execution may apply approved changes

No single layer should own all four responsibilities.

### Approval Rules

- design artifact approval is not validation approval
- validation approval is not deployment approval
- provider output cannot self-approve
- materialization cannot silently promote a draft to production

## Closed-Loop Optimization Architecture

Closed-loop optimization should be explicit and human-legible:

Draft  
↓  
Assess  
↓  
Improve  
↓  
Re-Assess  
↓  
Compare  
↓  
Approve

### Loop Components

- selected design artifact version
- materialized candidate
- analyzer result set
- refinement proposal set
- comparison record
- explicit approval checkpoint

### Comparison Semantics

The loop should compare:

- concept changes
- draft changes
- analyzer output changes
- recommendation changes
- narrative flow changes where available

It should not hide changes inside opaque provider rewrites.

## Surface-Neutral Architecture Direction

The first implementation may be PBIR-first, but the studio model should remain surface-neutral.

The architecture should support:

- PBIR
- Fabric Apps
- future analyzable surfaces
- future report-generation providers
- non-Microsoft design or generation providers

Surface-specific assumptions should live in adapters:

- materialization adapters
- provider adapters
- navigation mapping adapters

not in the core studio artifact model.

## Phased Delivery

### Phase 1: Design Briefs

Deliver:

- Design Brief model
- design thread identity
- brief persistence
- brief validation rules

### Phase 2: Concept Studio

Deliver:

- concept artifact model
- page, KPI, chapter, and navigation concepts
- concept comparison support

### Phase 3: Draft Studio

Deliver:

- draft artifact model
- isolated draft structures
- provider-neutral draft generation seams

### Phase 4: Refinement Studio

Deliver:

- analyzer-output ingestion into studio
- refinement proposal model
- proposal provenance and alternative comparison

### Phase 5: Closed-Loop Optimization

Deliver:

- materialization gateway
- assess-improve-reassess-compare workflow
- approval checkpoints

## Validation Strategy

### Design Validation

Validate whether:

- briefs are specific enough to produce stable concepts
- concepts remain distinct from drafts
- drafts remain distinct from analyzable surfaces
- refinement proposals remain advisory and attributable

### User Validation

Validate whether users can:

- articulate intent clearly through briefs
- understand concept outputs without needing PBIR assets
- distinguish design approval from validation approval
- use the closed loop without confusion

### Architecture Validation

Validate whether:

- shared infrastructure is reused instead of duplicated
- provider adapters remain optional
- materialization boundaries remain explicit
- analyzer ownership remains intact

### Provider Validation

Validate whether:

- provider outputs carry provenance
- provider failures degrade gracefully
- provider outputs can be ignored without breaking the core workflow
- different providers can target the same artifact kinds without reshaping the core model

## Regression Strategy

Report Design Studio work must not regress:

- analyzer workspace responsibilities
- Story Assessment public contract boundaries
- deterministic preview/apply/rollback authority
- shared repository snapshot reuse
- analyzable surface and analyzer separation
- score-panel protocol stability

Regression emphasis should focus on ownership boundaries, not only runtime correctness.

## Long-Term Success Criteria

This design succeeds if the platform can answer:

How do we help users design better reports before they are built?

with an architecture that:

- improves design quality before implementation hardens
- keeps design and validation separate
- preserves analyzer authority
- supports future surfaces and providers
- avoids provider lock-in
- keeps all design assistance advisory until explicit validation occurs

## Final Recommendation

Report Design Studio should be introduced as a separate, artifact-first design workflow that shares platform infrastructure with PBIR Design Analyzer but does not live inside the current analyzer workspace.

The key architecture bet is the explicit materialization gateway:

- it preserves trust boundaries
- it prevents design from collapsing into continuous hidden scoring
- it keeps analyzable surfaces derived rather than primary
- it allows future multi-surface and multi-provider expansion without rewriting the studio core

No public release or contract promotion should be assumed until the workflow, artifact model, and trust boundary are validated in practice.
