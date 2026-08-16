# Report Discovery Wizard Design

Date: 2026-06-18

Status: Design specification only. No code changes are included in this document.

## Vision

Report Discovery Wizard extends the product from:

I know what report I want.

to:

I have a semantic model. What should I build?

The wizard should behave like an experienced analytics consultant. It should inspect a semantic model, identify the most credible analytics opportunities, and return a small curated set of recommendations with strong business justification and a concrete Experience Blueprint for each recommendation.

The wizard is an upstream advisory workflow.

Semantic Model  
↓  
Report Discovery Wizard  
↓  
Top 3 Primary Recommendations  
+ 2 Alternate Recommendations  
↓  
Design Studio  
↓  
Analyzer Workspace  

## Authoritative Inputs

This design uses the following as authoritative inputs:

- `AGENTS.md`
- `docs/2026-06-18-Design-Studio-roadmap.md`
- `docs/report-design-studio-user-guide.md`
- `docs/report-design-studio-workflow-walkthrough.md`
- `docs/report-design-studio-mvp-validation-review-round6.md`
- `docs/superpowers/specs/2026-06-12-report-design-studio-design.md`
- `docs/2026-06-02_power-bi-agent-skills-reference-review.md`
- current repo memory covering Design Studio trust boundaries, Analyzer Workspace validation ownership, and provider-neutral architecture

## Goals

- Help users discover what analytics experiences should be built from a semantic model.
- Produce consultant-style recommendations rather than a broad search-style catalog.
- Return at most 5 recommendations:
  - Top 3 Primary Recommendations
  - 2 Alternate Recommendations
- Make every recommendation concrete through an Experience Blueprint.
- Convert a selected recommendation into Design Studio starting artifacts:
  - Design Brief
  - Concept Candidates
  - Initial Draft seed structure
- Preserve Design Studio as the design-authoring workflow.
- Preserve Analyzer Workspace as the validation owner.
- Keep recommendations advisory and provider-neutral.
- Create a future-ready Design Package foundation that can later feed Microsoft Power BI Skills, Fabric App generation, or other provider-backed generation paths.

## Non-Goals

- No direct PBIR generation.
- No direct Fabric App generation.
- No Microsoft Skills or CLI execution in this phase.
- No change to Analyzer Workspace ownership of validation, findings, or review approval.
- No change to Design Studio trust boundaries.
- No automatic report mutation.
- No broad recommendation catalog of 8-12 experiences.
- No new analyzable surface type invented solely for discovery.
- No hidden execution path that bypasses deterministic preview, apply, and rollback.

## Product Positioning

Report Discovery Wizard is not a replacement for Design Studio and not a second Analyzer Workspace.

- Report Discovery Wizard owns semantic-model interpretation and recommendation curation.
- Design Studio owns design authoring, approvals, concept selection, draft shaping, and workflow progression.
- Analyzer Workspace owns analysis, findings, validation, and validated review outcomes.

This keeps the product hierarchy legible:

Discovery recommends.  
Design Studio designs.  
Analyzer Workspace validates.

## User Personas

### 1. Consultant

Needs:

- fast identification of the best analytics experience to propose
- a concrete starting structure
- strong business rationale for client conversations

Success signal:

- can move from semantic model to a credible recommended experience in one guided workflow

### 2. Internal BI Team Lead

Needs:

- help translating a semantic model into a roadmap of high-value experiences
- confidence that recommendations reflect business outcomes rather than only technical structure

Success signal:

- can select a recommendation and enter Design Studio with a pre-populated design baseline

### 3. Business-Facing Analyst

Needs:

- recommendations expressed in business language
- fewer choices and clearer tradeoffs
- an explanation of what will actually be built

Success signal:

- can understand why a recommendation exists and what pages, KPIs, filters, and decisions it supports

### 4. Future Provider Operator

Needs:

- a stable Design Package contract that can later feed provider-backed generation
- preserved advisory-only boundaries until explicit generation architecture is introduced

Success signal:

- can consume a Design Package later without changing the discovery architecture

## Design Principles

### 1. Curated Guidance Over Search Results

The wizard should feel like expert consultant guidance, not a recommendation search engine. Fewer recommendations with stronger explanations are preferred over broad coverage.

### 2. Semantic Model First

The semantic model is the authoritative input for discovery. Recommendations should be derived from real model structure and metadata, not generic prompt output.

### 3. Experience Blueprints Are Required

A recommendation without a concrete blueprint is not actionable enough. Every recommendation must define what the experience would contain.

### 4. Advisory Only

Discovery may recommend and pre-populate. It may not generate deployable assets, validate itself, or mutate reports.

### 5. Preserve Existing Product Boundaries

Design Studio remains the design-authoring workflow. Analyzer Workspace remains the quality gate. Discovery must fit upstream without collapsing those roles.

### 6. Provider Neutrality

Future Microsoft Skills and CLI integration must plug into stable package seams rather than become the architecture.

## Recommendation Output Model

The wizard returns a curated maximum of 5 recommendations:

- 3 Primary Recommendations
- 2 Alternate Recommendations

Recommendations should be intentionally diverse enough that the user sees materially different experience directions rather than five near-duplicates.

Each recommendation must include:

- Recommendation Name
- Recommended Experience Type
- Confidence
- Business Value
- Implementation Complexity
- Why We Recommend It
- Expected Audience
- Expected Business Outcome
- Experience Blueprint
- Recommendation Summary Card

## Recommendation Summary Card

Every recommendation should expose a fast-scan card with:

- recommendation name
- business value
- implementation complexity
- confidence
- page count
- KPI count
- global filter count
- expected audience
- short why summary
- expected business outcome

The summary card exists for fast comparison.

The full recommendation detail exists for decision quality.

## Experience Blueprint

Every recommendation must generate an Experience Blueprint.

The Experience Blueprint is the bridge between:

Semantic Model  
↓  
Discovery Wizard  
↓  
Design Studio

The Experience Blueprint must contain:

- recommended pages
- primary KPIs
- suggested global filters
- suggested page filters
- suggested visual types by page
- analytical flow
- navigation intent
- expected audience
- expected business outcome
- success criteria seed

The Experience Blueprint is not a generated report. It is the structured design baseline that Design Studio can consume.

## Analytics Experience Types

The wizard must support recommendation evaluation across at least these experience types:

- PBIR Reports
- Fabric Apps
- Fabric Data Apps
- Executive Dashboards
- Operational Monitoring Experiences
- Analytical Investigation Experiences

### Experience Type Semantics

- PBIR Report:
  - report-centric experience with pages, filters, visuals, and narrative flow
- Fabric App:
  - broader packaged consumption experience that may group report views, navigation, and audience pathways
- Fabric Data App:
  - data-centric app pattern focused on structured exploration and business workflows around semantic-model access
- Executive Dashboard:
  - concise KPI-first leadership experience optimized for high-level decision support
- Operational Monitoring Experience:
  - monitoring and exception-management experience optimized for operational cadence, alert-like scanning, and action prioritization
- Analytical Investigation Experience:
  - question-driven investigative experience optimized for root-cause analysis and drill-based reasoning

## Semantic Model Discovery

### Goal

Understand what analytical capabilities the semantic model already supports.

### Metadata To Analyze

At minimum the discovery layer should inspect:

- measures
- measure folders
- calculation intent inferred from measure names and descriptions
- dimensions
- hierarchies
- date tables and date-intelligence signals
- relationships
- relationship directionality
- relationship cardinality
- fact-like versus dimension-like table roles
- table and column naming semantics
- column data types
- KPI-like measures
- profitability, forecast, inventory, service, operational, or customer domain signals inferred from semantic naming
- perspectives if present
- descriptions or annotations if present
- display folders if present
- model breadth and sparsity
- evidence of audience-specific subject areas

### Discovery Output

The discovery layer should emit a normalized Discovery Profile containing:

- business domains inferred from the model
- analytical capabilities inferred from the model
- candidate audiences
- experience-fit signals
- KPI clusters
- dimension clusters
- workflow clues
- model-strength signals
- model-gaps and ambiguity notes

The Discovery Profile is internal discovery state. It is not a public analyzer result and not a Design Studio artifact.

## Opportunity Identification

### Goal

Translate model structure into business questions and experience opportunities.

### Opportunity Categories

At minimum the wizard should identify opportunities such as:

- executive reporting
- operational monitoring
- profitability analysis
- customer analysis
- sales performance
- forecast accuracy
- inventory optimization
- service operations
- root cause investigation
- comparative performance management

### Opportunity Identification Logic

Opportunity identification should combine:

- semantic model domain signals
- KPI readiness
- time-analysis readiness
- audience clues
- operational-versus-executive cadence clues
- drill-path richness
- relationship support for segmentation and decomposition
- expected business actionability

This should produce an internal Opportunity Catalog ranked before recommendation curation.

## Experience Recommendation Engine

### Goal

Convert the Opportunity Catalog into a small curated set of consultant-quality recommendations.

### Engine Stages

1. Build Discovery Profile.
2. Identify candidate opportunities.
3. Map each opportunity to one or more experience types.
4. Generate an Experience Blueprint for each viable opportunity.
5. Score and rank candidates.
6. Apply diversity and deduplication rules.
7. Return:
   - Top 3 Primary Recommendations
   - 2 Alternate Recommendations

### Recommendation Structure

Each recommendation must include:

- Recommendation Name
- Recommended Experience Type
- Confidence:
  - High
  - Medium
  - Low
- Business Value:
  - High
  - Medium
  - Low
- Implementation Complexity:
  - High
  - Medium
  - Low
- Why We Recommend It
- Expected Audience
- Expected Business Outcome
- Experience Blueprint

## Recommendation Ranking

### Scoring Dimensions

Recommendations should be ranked using a weighted blend of:

- semantic coverage:
  - how much of the needed KPI and dimension structure exists
- business actionability:
  - how clearly the experience supports real decisions
- analytical fit:
  - how well the model supports the required investigative or monitoring pattern
- audience clarity:
  - how confidently the likely consumers can be inferred
- blueprint completeness:
  - how concretely pages, KPIs, filters, and flow can be specified
- implementation complexity:
  - how difficult the experience is expected to be relative to value
- model confidence:
  - how ambiguous or incomplete the model interpretation is

### Primary Recommendation Selection

Primary recommendations should be the highest-value, highest-confidence, most defensible options.

The engine should prefer:

- clear business outcome
- clear audience
- strong semantic support
- concrete blueprint quality
- meaningful differentiation from the other primary recommendations

### Alternate Recommendation Selection

Alternate recommendations should not simply be ranks 4 and 5 of the same pattern.

They should represent credible secondary directions that:

- target a different audience
- emphasize a different business outcome
- use the same semantic model in a different experience shape
- or represent a more specialized but still defensible opportunity

### Diversity Rule

If multiple candidates are near-duplicates, the engine should keep the strongest one and use the remaining slots for more differentiated recommendations.

## Recommendation Explanations

### Goal

Users should understand not only what is recommended, but why it is credible.

### Explanation Components

Each recommendation explanation should include:

- the semantic signals behind the recommendation
- the expected audience
- the expected business outcome
- the reason the recommended experience type fits better than nearby alternatives
- the main factors that increase confidence
- the main factors that limit confidence

### Explanation Style

Explanations should be:

- business-first
- evidence-backed
- concise enough to scan
- concrete enough to defend in a consultant conversation

### Example Explanation Pattern

Why We Recommend It:

- strong revenue measures
- rich territory hierarchy
- date intelligence support
- executive KPI coverage

Confidence note:

- confidence is high because the model strongly supports time-based revenue and territory analysis

Complexity note:

- complexity is medium because the experience is broad enough to need multiple focused pages but does not require deep investigative branching

## Experience Blueprint Detail

### Recommended Pages

Every recommendation should include a proposed page set.

Examples:

1. Executive Summary
2. Revenue Performance
3. Territory Performance
4. Customer Analysis
5. Forecast Accuracy

### Primary KPIs

Every recommendation should include the KPIs that define the experience.

Examples:

- Revenue
- Gross Margin
- YoY Growth
- Forecast Accuracy
- Customer Retention

### Suggested Filters

The blueprint should define:

- global filters
- page-specific filters

Examples:

Global Filters:

- Date
- Region
- Territory
- Product Category
- Customer Segment

Page Filters:

- Revenue page:
  - Product Category
  - Customer Segment
- Forecast page:
  - Forecast Period
  - Territory

### Suggested Visual Types

The blueprint should recommend visual classes by page intent.

Examples:

- Executive Summary:
  - KPI Cards
  - Trend Charts
  - Scorecards
- Revenue Analysis:
  - Bar Charts
  - Line Charts
  - Decomposition Trees
- Operational Monitoring:
  - Status Grids
  - Exception Tables
  - Trend Visuals

### Analytical Flow

Each recommendation must express:

Question  
↓  
Investigation  
↓  
Evidence  
↓  
Decision

Example:

Question:
Why is revenue down?

Investigation:
Analyze territory and product performance.

Evidence:
Territory trends, product mix, customer movement.

Decision:
Identify corrective sales actions.

### Navigation Intent

The blueprint should state how the user should move through the experience, for example:

- summary to drill
- monitor to exception to detail
- question to branch analysis
- executive overview to regional comparison

## Design Studio Integration

### Goal

A selected recommendation should become an immediately usable Design Studio starting point rather than a dead-end suggestion.

### Integration Flow

Recommendation Selected  
↓  
Create Discovery-Backed Design Brief  
↓  
Create Concept Candidates  
↓  
Create Initial Draft Seed  
↓  
Enter Design Studio Workflow

### Design Brief Creation

The wizard should automatically create a Design Brief pre-populated from:

- expected audience
- expected business outcome
- analytical flow
- success criteria seed
- report or experience type
- navigation intent
- key KPI set
- dimension emphasis

The resulting Design Brief is still a Design Studio artifact and still follows Design Studio approval rules.

### Concept Candidate Creation

The wizard should automatically create Concept Candidates from:

- recommended pages
- KPI groupings
- filter structure
- navigation structure
- page intent
- analytical flow

These should enter Concept Studio as structured initial alternatives, not as final approved concepts.

### Initial Draft Seed Creation

The wizard should automatically create an Initial Draft seed from:

- blueprint page set
- recommended visual types
- KPI placement intent
- navigation hierarchy
- audience emphasis

This is a Design Studio draft baseline seed, not a validated report and not a generated production asset.

### Integration Rules

- recommendation selection does not skip Design Studio approvals
- recommendation selection does not skip Analyzer Workspace review later
- recommendation selection does not create deployable PBIR directly
- recommendation selection does not create validation approval
- recommendation selection must preserve provenance back to:
  - semantic model source
  - discovery profile
  - selected recommendation
  - generated blueprint

## Design Package Generation

### Goal

The Experience Blueprint should become the foundation of a future Design Package.

### Design Package Contents

The Design Package should contain:

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
- provenance from the selected discovery recommendation

### Design Package Role

The Design Package is the stable handoff object between:

- Discovery Wizard
- Design Studio
- future provider-backed generation

This package should remain provider-neutral and generation-independent.

## Future Microsoft Skills Integration

### Goal

Define the future path without coupling the current architecture to Microsoft implementation details.

### Integration Posture

Microsoft Power BI Skills or CLI should be treated as optional downstream providers that may later consume a Design Package.

The future path is:

Semantic Model  
↓  
Discovery Wizard  
↓  
Selected Recommendation  
↓  
Experience Blueprint  
↓  
Design Package  
↓  
Optional Microsoft Skills / CLI  
↓  
Generated Asset  
↓  
Analyzer Workspace

### Required Boundaries

- the wizard must not directly invoke Microsoft Skills in this phase
- the Design Package must be the provider handoff seam
- Microsoft Skills must remain optional
- provider outputs must remain advisory until a later explicit generation architecture is approved
- generated outputs must still pass through Analyzer Workspace for validation

### What This Phase Should Define Now

This design should define:

- the Design Package contract direction
- the recommendation-to-package lineage model
- the provider-neutral handoff seam

This design should not define:

- provider execution details
- CLI command surfaces
- deployment flows
- direct mutation authority

## Trust Boundaries

### Preserved Boundaries

The Report Discovery Wizard must preserve the following:

- Design Studio trust boundaries
- Analyzer Workspace validation ownership
- advisory-only recommendation posture
- deterministic preview, apply, and rollback as the only report-edit execution path
- shared repository snapshot reuse rather than analyzer-local rescans or discovery-local ad hoc caching

### Discovery Wizard Authority

Discovery Wizard may:

- inspect semantic-model metadata
- infer opportunities
- rank recommendations
- generate Experience Blueprints
- pre-populate downstream Design Studio artifacts

Discovery Wizard may not:

- validate recommendations as if they were analyzer results
- mint findings
- mint normalized findings
- self-assign validation approval
- generate deployable assets
- mutate reports
- launch hidden provider execution
- bypass Design Studio approvals
- bypass Analyzer Workspace review

### Design Studio Authority

Design Studio remains responsible for:

- design brief ownership
- concept candidate ownership
- draft seed ownership
- design approvals
- refinement workflow
- workflow completion

### Analyzer Workspace Authority

Analyzer Workspace remains responsible for:

- executing analyzer review
- findings and recommendation generation
- validation ownership
- provenance of reviewed outputs

## Validation Ownership

Validation ownership stays unchanged.

- Discovery Wizard validates only its own internal input completeness and mapping integrity.
- Design Studio validates only its own workflow readiness and artifact lineage.
- Analyzer Workspace validates analyzable candidates and generated outputs.

A discovery recommendation is not a validated result.

An Experience Blueprint is not a validated design.

A Design Package is not a validation artifact.

If future provider-backed generation occurs, the produced asset must still be reviewed through Analyzer Workspace before it can claim validated quality.

## Public Contract And Maintainability Guidance

### Internal Versus Public Models

The following should stay internal first:

- Discovery Profile
- Opportunity Catalog
- Recommendation ranking internals
- Experience Blueprint derivation metadata

Only stable downstream contracts should graduate into shared protocol shapes.

### Maintainability Rules

- keep discovery, recommendation, blueprinting, and Design Studio seeding as separate layers
- avoid embedding experience-type heuristics directly into UI components
- avoid binding ranking logic to a single provider or prompt style
- preserve provenance so downstream artifacts can be traced back to discovery rationale
- prefer configuration-backed experience templates over hard-coded scattered heuristics

## Key Questions Answered

### What metadata should be analyzed?

Measures, dimensions, hierarchies, date intelligence, relationships, relationship cardinality, naming semantics, KPI-like measures, domain clues, perspectives, descriptions, display folders, model breadth, and audience or workflow clues present in the semantic model.

### How are opportunities scored?

Using a weighted blend of semantic coverage, business actionability, analytical fit, audience clarity, blueprint completeness, implementation complexity, and model confidence.

### How are recommendations explained?

Through business-first explanations that cite semantic signals, expected audience, business outcome, experience-type fit, confidence drivers, and complexity drivers.

### How many recommendations are shown?

At most 5:

- 3 Primary Recommendations
- 2 Alternate Recommendations

### How are recommendations selected?

The engine ranks viable candidates, removes near-duplicates, applies diversity rules, and returns the strongest consultant-defensible set rather than a broad catalog.

### How do recommendations become Design Briefs?

Selecting a recommendation auto-creates a discovery-backed Design Brief pre-populated with audience, business outcome, analytical flow, navigation intent, KPI emphasis, and success criteria seed.

### How do recommendations become Concepts?

The Experience Blueprint creates Concept Candidates using pages, KPI groupings, filters, analytical flow, and navigation structure.

### How do recommendations become Drafts?

The Experience Blueprint seeds an Initial Draft baseline using recommended visuals, KPI placement, page intent, and navigation hierarchy inside Design Studio.

## Recommended Next Architectural Step

Implement Discovery Profile and Opportunity Catalog as internal-only workflow layers first.

Do not start with provider-backed generation.

Do not start with Microsoft Skills execution.

Do not widen Analyzer Workspace or Design Studio ownership to absorb discovery logic.
