# Report Discovery Wizard Implementation Plan

**Goal:** Add a planning-backed Report Discovery Wizard that inspects a semantic model, returns a curated set of consultant-style analytics experience recommendations, and converts a selected recommendation into Design Studio starting artifacts without changing validation ownership or generation authority.

**Architecture:** Build a new upstream discovery workflow with four internal layers: semantic-model discovery, opportunity identification, recommendation ranking, and Experience Blueprint generation. Keep Design Studio as the artifact-authoring workflow, keep Analyzer Workspace as the validation owner, and treat future Design Package and Microsoft Skills integration as downstream optional seams.

**Tech Stack:** TypeScript, React webviews, VS Code extension host, existing repository snapshot and semantic-model infrastructure, .NET 8 backend services where semantic analysis belongs, Jest, xUnit

---

## Planning Constraints

- This is a phased implementation plan only.
- Do not implement provider-backed generation in these phases.
- Do not implement Microsoft Skills integration in these phases beyond defining future seams.
- Do not change Analyzer ownership.
- Do not change Design Studio trust boundaries.
- Keep discovery recommendations advisory-only.

## Target Outcome

Users should be able to:

1. Select a semantic model or supported model-backed surface.
2. Run Report Discovery Wizard.
3. Receive:
   - Top 3 Primary Recommendations
   - 2 Alternate Recommendations
4. Understand each recommendation quickly through a summary card.
5. Inspect a detailed Experience Blueprint.
6. Select one recommendation.
7. Enter Design Studio with a discovery-backed Design Brief, Concept Candidates, and Initial Draft seed structure.

## Recommended Architecture Slices

The implementation should be split into these internal layers:

- Discovery Profile layer
- Opportunity Catalog layer
- Recommendation Engine layer
- Experience Blueprint layer
- Design Studio seeding adapter
- future Design Package adapter
- future provider handoff adapter

This separation matters for maintainability. It prevents recommendation heuristics, UI rendering, and downstream artifact seeding from collapsing into one large workflow object.

## Phase 1: Semantic Model Discovery

### Scope

- introduce an internal Discovery Profile model
- inspect semantic-model metadata needed for opportunity reasoning
- normalize discovered signals into reusable internal structures
- capture ambiguity and confidence notes rather than flattening weak signals into false certainty

### Dependencies

- existing repository snapshot and semantic-model loading infrastructure
- stable semantic-model metadata access paths
- current analyzable-surface vocabulary where discovery must identify supported source surfaces

### Architecture Impacts

- adds a new internal discovery layer upstream of Design Studio
- should reuse shared repository snapshot logic instead of creating discovery-local scanning systems
- should keep Discovery Profile internal and separate from score payloads, findings, and Design Studio artifact contracts

### Trust-Boundary Impacts

- no analyzer ownership changes
- no Design Studio ownership changes
- no report mutation
- no findings generation
- no validation semantics added

### Testing Strategy

- xUnit coverage for metadata extraction and normalization
- regression tests for sparse, ambiguous, and domain-rich semantic models
- tests confirming no score payload or public analyzer contract expansion
- tests confirming discovery degrades explicitly when metadata is incomplete

### Success Criteria

- the system can produce a Discovery Profile from supported semantic-model inputs
- the profile captures measures, dimensions, hierarchies, time signals, relationship clues, and domain hints
- weak or missing metadata produces explicit ambiguity notes rather than silent assumptions
- no analyzer or Design Studio public contracts are widened

## Phase 2: Opportunity Identification

### Scope

- introduce an internal Opportunity Catalog model
- map Discovery Profile signals into candidate business opportunities
- classify candidate opportunities across executive, operational, investigative, and app-shaped patterns
- attach audience and business-outcome hypotheses to each opportunity

### Dependencies

- completed Discovery Profile layer
- stable internal vocabulary for experience categories and business domains
- design-approved opportunity taxonomy

### Architecture Impacts

- adds a dedicated reasoning layer between discovery and recommendation ranking
- should keep opportunity logic separate from UI cards and Design Studio seeding
- should prefer configuration-backed mapping tables or templates over scattered heuristics in UI code

### Trust-Boundary Impacts

- opportunities remain advisory internal candidates
- no generation authority introduced
- no analyzer findings or validation status implied

### Testing Strategy

- xUnit coverage for opportunity inference rules
- table-driven tests for common semantic patterns:
  - revenue and territory
  - customer profitability
  - inventory and operations
  - service operations
  - root-cause investigation
- regression tests for duplicate or near-duplicate opportunity collapse

### Success Criteria

- the system can identify credible opportunities from a Discovery Profile
- each opportunity includes inferred audience, business outcome, and candidate experience types
- the opportunity layer avoids bloating into a broad unmanaged catalog
- duplicate opportunities are normalized before recommendation ranking

## Phase 3: Recommendation Engine

### Scope

- add recommendation scoring and ranking
- implement the curated output model:
  - Top 3 Primary Recommendations
  - 2 Alternate Recommendations
- implement diversity and deduplication logic
- attach recommendation-level confidence, business value, and implementation complexity
- generate explanation payloads for recommendation summary cards and detailed views

### Dependencies

- completed Opportunity Catalog
- agreed ranking dimensions and weighting model
- defined recommendation explanation contract

### Architecture Impacts

- creates a dedicated recommendation layer that should stay independent of blueprint generation
- ranking logic should be isolated and testable, not embedded inside webview presentation reducers
- explanation generation should consume structured evidence, not only free-form text

### Trust-Boundary Impacts

- recommendations remain advisory
- recommendation ranking does not mint validation or analyzer-owned credibility
- no direct downstream generation or mutation path

### Testing Strategy

- deterministic ranking tests for representative opportunity sets
- tests for diversity enforcement across near-duplicate candidates
- tests for primary versus alternate recommendation behavior
- tests for explanation payload completeness and stable cardinality

### Success Criteria

- the engine never returns more than 5 recommendations
- the top 3 recommendations are strong, differentiated, and consultant-defensible
- 2 alternates provide credible secondary directions rather than redundant variations
- every recommendation includes summary-card and detailed explanation content

## Phase 4: Experience Recommendation Types

### Scope

- implement blueprint generation for the minimum required experience types:
  - PBIR Reports
  - Fabric Apps
  - Fabric Data Apps
  - Executive Dashboards
  - Operational Monitoring Experiences
  - Analytical Investigation Experiences
- define page, KPI, filter, visual, navigation, and analytical-flow templates for each type
- establish type-specific complexity and confidence rules

### Dependencies

- completed Recommendation Engine
- approved experience-type semantics
- design-approved Experience Blueprint contract

### Architecture Impacts

- adds a blueprinting layer that converts recommendations into structured downstream artifacts
- should use template-driven type adapters rather than a single monolithic branching function
- should preserve provenance from opportunity and recommendation into blueprint output

### Trust-Boundary Impacts

- blueprints remain design seeds, not generated assets
- no validation ownership changes
- no automatic materialization into analyzable surfaces

### Testing Strategy

- snapshot-style tests for blueprint structure by experience type
- tests for page count, KPI set, and filter derivation
- tests for analytical-flow generation
- tests confirming blueprint output remains provider-neutral

### Success Criteria

- every recommendation produces a usable Experience Blueprint
- blueprint outputs are concrete enough to seed Design Studio
- type-specific outputs are meaningfully differentiated
- blueprint generation does not imply report generation

## Phase 5: Design Studio Integration

### Scope

- add recommendation selection flow
- convert a selected recommendation into:
  - Design Brief
  - Concept Candidates
  - Initial Draft seed structure
- preserve lineage from source semantic model and selected recommendation into Design Studio artifacts
- add Design Studio entry routing from the discovery workflow

### Dependencies

- completed Experience Blueprint layer
- current Design Studio artifact model and workflow contracts
- stable Design Studio protocol and persistence seams

### Architecture Impacts

- introduces a discovery-to-Design Studio adapter
- should not inject recommendation logic directly into core Design Studio artifact models beyond explicit provenance fields
- should preserve Design Studio as the owner of downstream design artifacts after seeding

### Trust-Boundary Impacts

- recommendation selection must not bypass Design Studio approvals
- recommendation selection must not create validation approval
- recommendation selection must not create deployable assets
- Analyzer Workspace ownership remains unchanged later in the workflow

### Testing Strategy

- host-side tests for recommendation-to-artifact conversion
- webview tests for recommendation selection and summary/detail presentation
- integration tests for Design Brief prepopulation, Concept Candidate creation, and Initial Draft seed creation
- lineage and stale-reference tests

### Success Criteria

- selecting a recommendation creates downstream Design Studio starting artifacts
- Design Studio opens with clear seeded context and provenance
- seeded artifacts remain editable and approvable under current Design Studio rules
- no trust-boundary regressions are introduced

## Phase 6: Design Package Generation

### Scope

- define and create an internal Design Package contract from the selected Experience Blueprint and seeded Design Studio context
- include:
  - audience
  - personas
  - KPIs
  - pages
  - filters
  - visual recommendations
  - navigation
  - analytical flow
  - success criteria
  - recommendation rationale
- preserve recommendation and blueprint provenance inside the package

### Dependencies

- completed Design Studio integration
- stable Experience Blueprint contract
- approved provider-neutral package vocabulary

### Architecture Impacts

- introduces a stable downstream handoff object
- should keep Design Package distinct from Design Brief, Concept, and Draft artifacts
- should avoid entangling package structure with a specific provider or CLI

### Trust-Boundary Impacts

- Design Package is a handoff artifact, not a generated solution
- package creation does not imply execution or deployment
- package remains advisory until a later approved generation architecture exists

### Testing Strategy

- contract tests for Design Package shape
- provenance tests from recommendation to package
- tests ensuring package generation is deterministic from the same blueprint input
- tests ensuring package output remains provider-neutral

### Success Criteria

- the system can emit a stable Design Package from a selected recommendation
- the package contains enough structure for future provider-backed generation planning
- the package is decoupled from Microsoft-specific execution details

## Phase 7: Microsoft Skills Integration

### Scope

- define the integration seam only after Design Package stability exists
- add optional provider adapter interfaces for Microsoft Power BI Skills or CLI consumption of Design Packages
- document future execution posture and handoff requirements

### Dependencies

- completed Design Package phase
- approved provider-neutral execution contract
- explicit future generation design approval

### Architecture Impacts

- introduces provider adapters downstream of Design Package, not inside discovery logic
- should preserve optionality so non-Microsoft providers can consume the same package contract later
- should keep provider execution outside the discovery and Design Studio core workflows

### Trust-Boundary Impacts

- no direct provider authority from Discovery Wizard
- no direct provider authority from Design Studio without later explicit generation architecture
- any future generated output must still return through Analyzer Workspace for validation

### Testing Strategy

- contract-only tests for provider adapter payload shaping
- tests confirming provider adapters are optional and failure-isolated
- tests confirming no execution path appears unless explicitly enabled by a future approved design

### Success Criteria

- a stable provider seam exists without hard-coupling the product to Microsoft implementation details
- discovery and Design Studio remain viable without Microsoft Skills
- the architecture is ready for future provider-backed generation planning without changing current trust boundaries

## Cross-Phase Risks To Manage

### 1. Recommendation Sprawl

Risk:

- the curated model drifts into a large catalog and loses consultant quality

Mitigation:

- enforce the 5-recommendation ceiling at the engine boundary

### 2. Boundary Collapse

Risk:

- discovery logic leaks into Analyzer Workspace or Design Studio ownership

Mitigation:

- keep discovery, blueprinting, seeding, and validation as separate contracts with explicit provenance

### 3. Hard-Coded Experience Heuristics

Risk:

- experience-type rules become scattered and hard to evolve

Mitigation:

- centralize experience templates and ranking logic in focused internal layers

### 4. Premature Provider Coupling

Risk:

- Microsoft Skills concerns distort the core design too early

Mitigation:

- keep Design Package as the only planned provider handoff seam

### 5. Weak Provenance

Risk:

- downstream Design Studio artifacts cannot explain which recommendation seeded them

Mitigation:

- preserve semantic model, Discovery Profile, recommendation, blueprint, and seed lineage across the full chain

## Recommended Validation Sequence

Implement and validate in this order:

1. Discovery Profile
2. Opportunity Catalog
3. Curated Recommendation Engine
4. Experience Blueprint generation
5. Design Studio seeding
6. Design Package contract
7. future provider seam

Do not invert that order by starting with provider execution.

## Definition Of Done Mapping

This plan satisfies the requested completion shape by defining implementation phases that:

- create a design spec
- create an implementation plan
- define trust boundaries
- define Design Studio integration
- define the future Microsoft Skills integration path
- preserve planning-only scope

The actual implementation should not begin until this phased plan is explicitly approved for execution.
