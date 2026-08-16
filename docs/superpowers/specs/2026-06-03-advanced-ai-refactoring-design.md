# Advanced AI Refactoring Design

Date: 2026-06-03

Status: Approved planning direction captured; implementation deferred

## Goal

Add an advisory `Advanced AI Refactoring` layer that can propose larger-scale report improvements while preserving the existing deterministic execution trust boundary.

Phase 4 should answer:

- `Can AI propose better report designs?`

Phase 4 must not change the answer to:

- `How are report modifications executed?`

## Strategic Positioning

Phase 1 proved deterministic fix opportunity generation.

Phase 2 hardened preview, approval, apply, rollback, and re-analysis.

Phase 3 added grounded advisory proposal enrichment above remediation.

Phase 4 should extend that progression from:

- better wording
- better rationale
- better prioritization

into:

- better refactoring scenarios
- better design alternatives
- better experience-level tradeoff analysis

without introducing:

- autonomous redesign
- model-generated mutations
- direct report mutation authority

## Canonical Architecture

The current architecture is:

- `Issues`
- `Remediation Queue`
- `AI Proposal Enrichment`
- `Fix Opportunity Engine`
- `Deterministic Mutation Layer`

Phase 4 should extend it to:

- `Issues`
- `Remediation Queue`
- `AI Refactoring Proposals`
- `Fix Opportunity Engine`
- `Deterministic Mutation Layer`

The new layer is additive and advisory.

Each layer keeps one job:

- `Issues` identifies problems
- `Remediation Queue` expresses solution intent
- `AI Refactoring Proposals` suggests larger advisory redesign options and tradeoffs
- `Fix Opportunity Engine` operationalizes only supported deterministic opportunities
- `Deterministic Mutation Layer` applies approved file changes and rollback plans

## Permanent Trust Boundary

This boundary remains non-negotiable.

### AI May

- propose refactoring scenarios
- explain why a design direction is stronger
- prioritize scenario options
- compare tradeoffs across alternatives
- identify evidence gaps
- recommend sequencing for human review
- suggest which advisory concepts could map to deterministic opportunities later

### AI May Not

- mutate PBIR directly
- mutate Fabric Apps directly
- generate executable mutations for direct application
- bypass deterministic validation
- bypass preview
- bypass approval
- bypass apply
- bypass rollback
- bypass re-analysis

### Compilation Rule

Every Phase 4 proposal must end in one of two states:

- `compilable`
  - some portion can be translated into remediation items, fix opportunities, and deterministic mutation plans
- `advisoryOnly`
  - the proposal remains recommendation-only because no deterministic compiler exists

There is no third path.

## Product Problem

The product can already:

- identify issues
- build remediation intent
- enrich remediation wording
- generate supported deterministic opportunities
- preview exact changes
- apply safely
- roll back safely

It cannot yet do enough at the design-strategy layer:

- compare alternative layouts across a page
- reason about KPI hierarchy explicitly
- recommend stronger narrative sequencing
- propose better navigation patterns
- optimize for executive consumption as a distinct design objective
- explain tradeoffs between competing design directions

These are advisory refactoring gaps, not deterministic execution gaps.

## Scope

### Phase 4 Includes

- layout refactoring proposals
- KPI hierarchy proposals
- storytelling refactoring
- navigation refactoring
- executive experience optimization
- alternative design scenarios with tradeoffs
- accessibility and governance alignment framing inside advisory proposals
- provider-agnostic orchestration
- grounded evidence, provenance, validation, and fallback behavior
- explicit classification of `compilable` versus `advisoryOnly`

### Phase 4 Excludes

- autonomous report redesign
- autonomous execution
- direct PBIR mutation by models
- direct Fabric App mutation by models
- DAX generation
- report generation
- Fabric App generation
- freeform design studio behavior
- replacing remediation as the solution-intent layer
- creating a second mutation pipeline

## Core Design Principles

### Remediation-Led Workflow Stays Intact

Refactoring proposals remain downstream from `Issues` and `Remediation Queue`.

They do not:

- replace normalized findings
- replace remediation items
- create a second issue system
- create a freeform prompt-driven design mode

Phase 4 is still review-led and remediation-led.

### Proposal Quality, Not Execution Autonomy

Phase 4 should improve:

- proposal breadth
- design-system reasoning
- scenario comparison quality
- business framing
- executive readability framing

It must not improve execution authority.

### Scenarios, Not Single Answers

Large-scale design recommendations should generally produce multiple bounded options rather than one model-picked answer.

The default product posture should be:

- `Option A`
- `Option B`
- `Option C`

with explicit tradeoffs, confidence, and evidence coverage.

### Grounding Before Generation

All provider calls must be grounded in deterministic local evidence such as:

- normalized findings
- remediation queue items
- score metadata
- page purpose analysis
- page story summaries
- page and visual metadata
- cross-page matrix signals
- supported deterministic opportunity categories
- accessibility/governance flags when present

The model should not infer hidden structure without that being marked advisory.

### Shared Proposal Atoms

Refactoring output should be built from reusable proposal atoms rather than freeform paragraphs only.

Recommended atoms:

- design objective
- proposed change
- affected scope
- rationale
- evidence references
- business impact
- tradeoffs
- confidence
- execution classification
- downstream compilation hints

This keeps Phase 4 compatible with future surfaces and future providers.

### Compile Down Or Stay Advisory

Phase 4 cannot create a parallel execution universe.

If a scenario recommends:

- visual regrouping
- spacing changes
- layout alignment
- title hierarchy cleanup

and the deterministic engine can express part of that safely, the system may compile that subset into:

- remediation
- fix opportunities
- deterministic mutation plans

Anything else remains clearly advisory.

## Advisory Proposal Domains

### 1. Layout Refactoring

Examples:

- density reduction
- visual grouping
- whitespace rebalancing
- page zoning
- section hierarchy restructuring

Expected outputs:

- alternative layout scenarios
- rationale for information grouping
- tradeoffs between compactness and readability
- partial mapping to deterministic layout opportunities where possible

### 2. KPI Hierarchy

Examples:

- primary KPI recommendations
- supporting KPI recommendations
- executive emphasis recommendations
- metric prominence adjustments

Expected outputs:

- KPI tiering suggestions
- title/subtitle framing
- summary-versus-detail balance guidance
- explicit evidence for why a KPI should move up or down in prominence

### 3. Storytelling Refactoring

Examples:

- narrative flow improvements
- page sequencing suggestions
- supporting evidence placement
- summary-to-detail progression

Expected outputs:

- narrative sequence proposals
- before/after storyline summaries
- rationale tied to page purpose and audience
- advisory sequencing steps

### 4. Navigation Refactoring

Examples:

- navigation restructuring
- drill-path recommendations
- supporting-page relationships
- destination clarity improvements

Expected outputs:

- proposed navigation map
- relationship graph between pages
- primary versus secondary path recommendations
- tradeoffs between discoverability and simplicity

### 5. Executive Experience Optimization

Examples:

- executive dashboard redesign proposals
- benchmark visibility recommendations
- decision-support emphasis improvements
- signal-to-noise reduction

Expected outputs:

- executive-first option sets
- readability and actionability framing
- benchmark placement alternatives
- recommendation ordering optimized for leadership review

### 6. Accessibility And Governance Alignment

Examples:

- readability risks in dense refactors
- insufficient semantic grouping
- weak standardization against governance conventions

Expected outputs:

- advisory warnings inside scenarios
- confidence downgrades when evidence is incomplete
- flags for proposals that conflict with governance or accessibility rules

## Recommended Output Model

Phase 4 should add a first-class advisory model for refactoring scenarios.

Recommended conceptual contracts:

- `RefactoringProposal`
- `RefactoringScenario`
- `RefactoringScenarioOption`
- `RefactoringTradeoff`
- `RefactoringEvidenceLink`
- `RefactoringCompilationHint`
- `RefactoringValidationResult`

Suggested shape:

- proposal is anchored to one remediation item or a bounded remediation cluster
- proposal contains one or more scenarios
- each scenario contains one or more options
- each option carries:
  - summary
  - rationale
  - impact areas
  - confidence
  - evidence references
  - tradeoffs
  - execution classification
  - partial deterministic mapping hints

## Relationship To Existing Architecture

### Not A Second Remediation System

Refactoring proposals must attach to existing remediation concepts.

They may:

- aggregate multiple related remediation items
- organize larger design alternatives around them
- recommend sequencing across them

They may not:

- create unrelated issue queues
- replace fix-plan generation
- invent an independent approval/apply workflow

### Relationship To Fix Opportunities

Fix opportunities remain the only executable layer.

Phase 4 may influence:

- which opportunities are highlighted
- which opportunities are grouped together
- how opportunities are explained to users
- which unsupported concepts remain advisory-only

Phase 4 may not create executable authority outside `FixOpportunity`.

### Relationship To Deterministic Execution

Phase 4 may produce:

- `layout scenario suggests a cleaner hierarchy`

but execution still depends on:

- `supported deterministic mutations exist`
- `preview can be generated`
- `approval is granted`
- `apply succeeds`
- `rollback exists`
- `re-analysis validates outcome`

## Provider Strategy

Provider architecture should remain provider-agnostic and enricher-driven.

### Recommended Components

- `refactoring context builder`
- `domain enrichers`
- `scenario composer`
- `provider abstraction`
- `validation pipeline`
- `fallback scenario builder`
- `compilation classifier`
- `telemetry and debug evidence`

### Provider Rules

- providers receive bounded grounded context only
- providers return advisory structures, not mutation plans
- providers must include provenance
- providers must tolerate partial-domain execution
- provider failure must degrade gracefully to fallback behavior

### Hallucination Guards

Validation should reject or downgrade output that:

- invents visuals, pages, measures, fields, or navigation paths
- claims deterministic support where none exists
- overstates business impact as proven outcome
- collapses advisory options into mandatory execution language
- contradicts findings, scores, or page metadata
- leaks into freeform redesign beyond bounded scope

## Cross-Surface Architecture

Phase 4 should be designed as a PBIR-first implementation of a broader cross-surface advisory pattern.

It should align with the `Analyzable Surface` concept from the Fabric Apps Analytics Review design without coupling initial implementation to Fabric App support.

### Surface Interaction Model

- PBIR reports are the first implementation surface
- Fabric Apps should later reuse the same scenario and validation model
- future screenshot or hybrid surfaces should be able to emit the same proposal atoms

### Surface-Specific Evidence, Shared Proposal Shape

Different surfaces will provide different evidence:

- PBIR metadata and visual structure
- Fabric App code, routes, tokens, and screenshots
- future screenshot bundles

But the advisory output shape should stay stable.

That allows:

- one workspace
- one proposal model
- multiple evidence adapters

## Relationship To Fabric App Readiness And Review

Phase 4 should document, not implement, the Fabric interaction.

### Fabric App Readiness

Fabric App Readiness can eventually consume Phase 4 concepts by:

- surfacing migration-oriented refactoring scenarios for PBIR reports
- identifying which report patterns translate well to app-like experiences
- marking proposal areas that remain advisory-only before migration

### Fabric App Review

Fabric App Review can eventually reuse Phase 4 concepts by:

- generating layout, storytelling, and navigation scenarios for app surfaces
- comparing analytical app experience options
- applying the same advisory-only validation model

### Coupling Rule

Phase 4 should not depend on Fabric-specific implementation to ship on PBIR.

Fabric Apps should reuse the model later through shared contracts and evidence adapters.

## User Experience Model

The intended user flow becomes:

- review findings
- review remediation queue
- inspect `AI Refactoring Proposals`
- compare `Option A / B / C`
- inspect evidence and tradeoffs
- determine whether any part maps to supported deterministic opportunities
- preview deterministic changes when available
- approve apply
- re-analyze results

Recommended UI treatment:

- scenarios are clearly labeled advisory
- each option shows evidence coverage and confidence
- compilable subsets are labeled separately from advisory-only ideas
- deterministic previews remain visually distinct from refactoring prose

## Rollout Strategy

Recommended staged rollout:

### Stage 1

- PBIR only
- advisory scenarios only
- single-option plus fallback-safe comparisons
- no automatic compilation

### Stage 2

- bounded `Option A / B / C` scenarios
- stronger domain enrichers
- explicit evidence/tradeoff rendering
- advisory-only telemetry and quality review

### Stage 3

- deterministic compilation hints for supported subsets
- selective mapping into remediation emphasis and fix opportunity grouping
- no change to execution boundary

### Stage 4

- shared proposal contracts ready for Fabric App reuse
- surface-specific evidence adapters added later through separate initiatives

## Testing Strategy

Phase 4 needs validation at five levels.

### 1. Proposal Quality

Test that scenarios are:

- relevant to grounded findings
- meaningfully different across options
- not repetitive restatements of remediation text
- useful for layout, KPI, storytelling, navigation, and executive framing

### 2. Grounding

Test that providers and enrichers:

- only use supplied evidence
- preserve page/report identifiers
- do not fabricate unsupported report structure

### 3. Validation And Hallucination Prevention

Test that validators reject or downgrade:

- invented artifacts
- unsupported execution claims
- contradictory priorities
- governance conflicts
- outcome overclaims

### 4. Comparison Generation

Test that multi-option scenarios:

- produce real tradeoffs
- maintain scenario diversity
- preserve shared factual grounding across options

### 5. Advisory-Only Enforcement

Test that no Phase 4 output can:

- directly enqueue mutations
- skip remediation mapping
- bypass fix opportunities
- bypass preview/apply/rollback/re-analysis

## Self-Review

### Risks

- proposal sprawl could overwhelm users with advisory content
- weak scenario diversity could produce near-duplicate options
- compilation hints could be mistaken for executable authority
- executive optimization could become subjective without enough evidence anchors
- provider variance could destabilize output quality

### Scope Creep Risks

- drifting from advisory scenarios into report design studio behavior
- allowing scenario output to become a second remediation queue
- folding Fabric App implementation into the PBIR-first Phase 4 build
- treating generated rationale as permission to mutate unsupported surfaces

### Architecture Concerns

- if proposal contracts live outside existing remediation and score payload seams, Phase 4 will fragment the workspace
- if deterministic compilation logic is mixed into provider orchestration, the trust boundary will blur
- if cross-surface reuse is ignored now, Fabric App adoption will require a second proposal model later

### Rollout Recommendation

Implement Phase 4 as a PBIR-first advisory layer in staged slices.

Ship scenario generation and validation before any deterministic compilation hints.

Keep the first release narrow:

- layout
- storytelling
- navigation
- executive readability

Add KPI hierarchy, accessibility, and governance alignment once the scenario and validation model is stable.

## Initiative Sequencing Recommendation

Between `Phase 4 Advanced AI Refactoring` and `Fabric Apps Analytics Review`, Phase 4 should be implemented first.

Reasoning:

- it extends the already-implemented Phase 3 advisory architecture directly
- it delivers value to the current PBIR user base without requiring new surface discovery or analyzer infrastructure first
- it is lower risk because it preserves the existing PBIR-only execution boundary and workspace seams
- its proposal contracts can be designed to align with the `Analyzable Surface` architecture now, reducing later Fabric App rework

Recommended order:

1. implement Phase 4 on PBIR with shared proposal contracts
2. validate scenario quality and advisory-only enforcement
3. then implement Fabric Apps Analytics Review using the same proposal model where appropriate
