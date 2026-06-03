# Fabric Apps Analytics Review Design

Date: 2026-06-03

## Goal

Evolve PBIR Design Analyzer into an analytics experience review platform that can assess both PBIR reports and semantic-model-backed analytical Fabric Apps through the same score-panel workspace.

The product direction is:

- one workspace
- multiple analyzers
- shared findings, remediation, evidence, and governance review patterns

This design is intentionally scoped to the reporting and analytics path for Fabric Apps, not general application development.

## Product Framing

PBIR Design Analyzer should not become:

- a generic Fabric App builder
- a code generator
- an operational app review tool
- a general software engineering platform

It should become:

- an analytics experience review platform
- focused on semantic-model-driven experiences
- optimized for dashboard quality, storytelling, actionability, accessibility, governance, and review workflows

Fabric Apps are treated here as the next evolution of analytical experiences beyond traditional Power BI reports.

## External Product Assumptions

This design is based on the current Fabric Apps product shape as documented and discussed on 2026-06-03:

- Fabric Apps are currently in preview
- Fabric Apps are broader than reporting, but this spec intentionally narrows to the analytical path
- the reporting-relevant path is semantic-model-backed analytical experiences and dashboard-style data apps
- Fabric Apps introduce a code-first authoring model with Rayfin-driven project and deployment workflows
- analytical Fabric App experiences trade built-in Power BI report affordances for flexibility, explicit code control, and higher governance risk

If Microsoft materially changes the analytical Fabric Apps model later, this spec should be revisited before implementation.

## Scope

Include:

- semantic-model-backed Fabric Apps
- analytical/data-app style Fabric Apps
- dashboard-style applications
- executive dashboards
- operational dashboards in the reporting sense
- analytical applications
- visualization-as-code scenarios
- Rayfin-driven reporting experiences
- semantic-model-backed experiences
- report-to-app migration readiness

Exclude:

- operational applications in the CRUD or workflow sense
- workflow applications
- managed SQL applications
- GraphQL application development
- transactional systems
- forms and data-entry experiences
- application business logic concerns
- general software engineering concerns unrelated to analytics UX
- code generation or autonomous application generation

## Core Principle

The existing score-panel workspace remains the primary UI surface:

- `Overview`
- `Issues`
- `Fix Plan`
- `Evidence`
- secondary `Export`

Fabric App support changes the analyzer, evidence source, and governance source. It does not introduce a separate top-level Fabric App workspace in Version 1.

## First-Class Platform Concept: Analyzable Surface

`Analyzable Surface` is a first-class platform concept, not just a convenient label.

It is the core abstraction that allows PBIR Design Analyzer to evolve from a PBIR-focused tool into an analytics experience review platform without fragmenting the workspace or duplicating review models.

An analyzable surface is any input type the platform can review through the shared workspace.

Initial and future examples:

- PBIR report
- Fabric App
- screenshot bundle
- future analytics experience surfaces

Every analyzable surface must be able to produce platform-native review outputs:

- findings
- evidence
- remediation guidance
- governance signals

Every analyzable surface should also expose enough identity and capability metadata for the workspace and presentation builders to behave consistently.

Recommended conceptual contract:

- `surfaceType`
- `displayName`
- `sourceLocation`
- `availableEvidenceKinds`
- `availableAnalyzerProfiles`
- `analysisCapabilities`
- `governanceCapabilities`

This abstraction prevents the architecture from drifting into separate product silos such as:

- PBIR review tool
- Fabric App review tool

Instead, the platform becomes:

- one analytics review workspace
- multiple analyzable surfaces
- multiple analyzer pipelines
- one normalized review model

This concept should be treated as durable platform architecture for future roadmap work, not as a Fabric-only extension point.

## Surface vs Analyzer Boundary

The platform should distinguish clearly between:

- `Analyzable Surface`
- `Analyzer`

A surface is the thing being reviewed.

Examples:

- PBIR report
- Fabric App
- screenshot bundle
- future surface types

An analyzer is the review engine that operates on a surface.

Examples:

- `PBIR Analyzer`
- `Fabric App Readiness Analyzer`
- `Fabric App Review Analyzer`
- `Screenshot Analyzer`
- `Governance Analyzer`

This distinction matters because readiness assessment is not a separate surface.

It is:

- an analyzer
- operating on a PBIR report surface
- producing readiness-oriented findings, evidence, remediation guidance, and governance signals

Likewise, Fabric App review is:

- an analyzer
- operating on a Fabric App surface

Not a separate workspace or parallel product model.

## Architecture

### Existing Durable Boundaries

Preserve the repo's current architecture boundaries:

- scoring or analysis outputs remain authoritative for their analyzer
- normalized findings remain the shared issue model
- workspace personas remain presentation-only
- review and export remain downstream from analysis
- AI remains advisory-only
- deterministic preview/apply/rollback remains the only report-edit execution path

Fabric App support must extend these boundaries, not weaken them.

### Recommended Layers

- analyzable surface model
- surface discovery
- analyzer registry
- analyzer profile model
- surface-specific analysis adapters
- normalized findings adapter
- shared evidence model
- shared remediation model
- shared governance signal model
- existing workspace presentation builders

### Architectural Shape

`Analyzable Surface`
`-> surface discovery`
`-> analyzer selection`
`-> analyzer profile selection`
`-> surface-specific analysis`
`-> normalized findings + evidence + remediation + governance signals`
`-> Overview / Issues / Fix Plan / Evidence / Export`

The workspace should not need to know whether a finding came from PBIR metadata, TypeScript layout code, screenshot review, navigation config, or semantic-model usage evidence.

## Surface Discovery

The platform needs a consistent way to answer:

`What analyzable surface am I looking at?`

Surface discovery should occur before analyzer selection.

Examples:

- PBIR project
  - `-> PBIR report surface`
- Fabric App repo
  - `-> Fabric App surface`
- screenshot bundle
  - `-> screenshot surface`

Recommended responsibilities:

- identify the surface type
- collect minimum identity metadata
- expose supported analyzers
- expose supported analyzer profiles
- fail clearly when the surface is unsupported or ambiguous

Recommended flow:

`Analyzable Surface`
`-> Surface Discovery`
`-> Analyzer Selection`
`-> Analyzer Profile Selection`
`-> Analysis`
`-> Findings / Evidence / Remediation / Governance`
`-> Workspace`

This will become more important as the number of supported surfaces grows.

## Surface Types In Version 1

### 1. PBIR Report

Current supported surface.

Primary evidence sources:

- PBIR metadata
- page and visual structure
- bookmarks and navigation controls
- scoring outputs
- screenshot audit when available

### 2. Fabric App

A direct analytical surface produced from a Fabric App repo that represents a semantic-model-backed dashboard or analytical experience.

Primary evidence sources:

- TypeScript and related frontend source
- navigation and route structure
- design tokens and CSS variables
- screenshot captures
- semantic-model query usage artifacts
- app configuration relevant to analytics UX review

### 3. Screenshot Bundle

Future surface type for grouped screenshots or captured analytical states reviewed through the same workspace.

## Analyzer Strategy

Adopt a shared analyzer platform model:

- `PBIR Analyzer`
- `Fabric App Readiness Analyzer`
- `Fabric App Review Analyzer`
- future `Screenshot Analyzer`
- future `Governance Analyzer`

All analyzers feed the same workspace model.

The difference between analyzers should be:

- what they inspect
- what evidence they derive
- what governance signals they emit

Not:

- a separate UX paradigm
- a separate findings system
- a separate remediation workflow

Examples in this spec:

- `Fabric App Readiness Analyzer` operating on `PBIR report`
- `Fabric App Review Analyzer` operating on `Fabric App`

## Analyzer Profiles

Add `Analyzer Profiles` as a first-class analysis configuration concept above individual analyzers.

An analyzer profile is a bounded review lens that shapes how a given analyzer emphasizes findings, evidence, and remediation without creating a different workspace.

Examples:

- `default`
- `executive`
- `consultant`
- `governance`
- `accessibility`
- future `migrationReadiness`
- future `fabricAppQuality`

Analyzer profiles should:

- reuse the existing workspace structure
- reuse the normalized findings model
- influence prioritization, grouping, and summary emphasis
- not silently change underlying evidence truth

Recommended conceptual contract:

- `profileId`
- `supportedSurfaceTypes`
- `supportedAnalyzerTypes`
- `summaryEmphasis`
- `findingPrioritizationRules`
- `fixPlanEmphasis`
- `evidenceEmphasis`
- `governanceEmphasis`

Examples in this spec:

- `migrationReadiness` profile for `Fabric App Readiness Analyzer`
- `fabricAppQuality` profile for `Fabric App Review Analyzer`

This keeps the product coherent:

- same workspace
- same surface abstraction
- same issue model
- different analyzer plus profile combinations

## Phase 1: Fabric App Readiness Assessment

### Goal

Assess PBIR reports for their readiness to migrate into analytical Fabric Apps.

### Questions This Phase Answers

- Which report pages are strong migration candidates?
- Which report patterns translate well to visualization-as-code?
- Which report behaviors depend too heavily on native Power BI mechanics?
- Which pages are better left as reports?
- What would need redesign versus straightforward translation?

### Scope

Include:

- readiness scoring
- migration candidate identification
- migration blockers
- unsupported pattern detection
- migration-oriented findings and recommendations

Do not include:

- generating Fabric App code
- generating Rayfin projects
- modifying Fabric App repos
- operational-app guidance

### Readiness Output Contract

Phase 1 should produce a stable readiness-oriented output contract above the existing findings model.

Recommended output shape:

- `surfaceType`
- `analyzerType: fabricAppReadiness`
- `profileId`
- `overallReadinessScore`
- `readinessBand`
- `migrationSummary`
- `candidatePages`
- `blockers`
- `unsupportedPatterns`
- `redesignRequiredAreas`
- `recommendedNextActions`
- `supportingEvidence`

Recommended page-level fields:

- `pageName`
- `readinessScore`
- `candidateState: strongCandidate | possibleCandidate | redesignRequired | keepAsReport`
- `positiveSignals`
- `blockers`
- `unsupportedPatterns`
- `migrationNotes`

This contract should remain advisory and should feed:

- `Overview` summary cards
- `Issues` findings groups
- `Fix Plan` migration preparation recommendations
- `Evidence` portability rationale and supporting proof

The readiness contract is not:

- a deployment contract
- a conversion guarantee
- an automatic migration plan

It is the output contract of `Fabric App Readiness Analyzer` when operating on a `PBIR report` surface.

### Readiness Dimensions

Recommended readiness dimensions:

- layout portability
- interaction portability
- narrative portability
- semantic-model suitability
- navigation portability
- governance portability
- accessibility portability
- custom-visualization opportunity

These are advisory review dimensions, not deployment guarantees.

### Example Readiness Signals

Positive signals:

- pages with clear KPI, comparison, and trend structures
- strong page-level narrative hierarchy
- limited dependence on implicit Power BI-only interactions
- semantically clean measures and labels
- layouts that map well to explicit code-driven composition

Negative signals:

- heavy drillthrough dependence
- brittle bookmark-state complexity
- reliance on filter-pane behavior as primary navigation
- pages whose value depends on native report chrome or report-only affordances
- dense layouts that would require substantial redesign rather than translation

### Expected Findings

Examples:

- `Good Fabric App candidate: executive KPI overview`
- `Migration blocker: report relies on implicit cross-filtering across too many visuals`
- `Redesign needed: page narrative depends on filter pane and drillthrough`
- `Opportunity: page could benefit from visualization-as-code custom layout`
- `Unsupported pattern: native report affordance has no direct Fabric App equivalent`

### Overview Additions

Phase 1 adds summary outputs such as:

- Fabric App readiness score
- migration readiness summary
- candidate pages
- top blockers
- likely redesign effort

### Issues Additions

Issues should surface:

- readiness blockers
- unsupported patterns
- portability risks
- redesign-required findings
- high-value migration opportunities

### Fix Plan Additions

Fix Plan should surface:

- migration preparation actions
- report-hardening actions before migration
- redesign recommendations
- keep-as-report recommendations where appropriate

### Evidence Additions

Evidence can include:

- PBIR-derived interaction evidence
- page metadata and navigation evidence
- screenshot evidence
- portability rationale
- semantic-model evidence already available from the report context

### Relationship to AI Fixes

Fabric App Readiness Assessment should stay separate from the current AI-fix execution pipeline.

The current AI-fix architecture is built around deterministic PBIR edits with explicit preview/apply/rollback boundaries.

Readiness outputs may inform future advisory fix sequencing, but they should not:

- create mutation authority
- generate Fabric App code
- bypass deterministic PBIR execution controls
- imply automatic report-to-app conversion

Recommended relationship:

- readiness findings may surface migration preparation opportunities
- readiness findings may influence advisory prioritization in `Fix Plan`
- readiness findings may coexist with deterministic PBIR fixes in the same workspace
- readiness findings must remain advisory while PBIR deterministic fixes remain the only execution-capable path

## Phase 2: Fabric App Review Mode

### Goal

Review semantic-model-backed analytical Fabric App repos as first-class analytical experiences inside the same workspace.

### Questions This Phase Answers

- Is this Fabric App a high-quality analytical experience?
- Does it preserve clarity, narrative flow, and actionability?
- Are design tokens and layout patterns consistent?
- Is navigation coherent?
- Is accessibility acceptable?
- Does semantic-model usage support trustworthy analytical UX?
- Does the app meet organization review and governance expectations?

### Scope

Include:

- TypeScript review relevant to analytics UX
- layout review
- navigation review
- design-token review
- accessibility review
- semantic-model usage review
- screenshot audit
- governance review

Exclude:

- backend architecture review
- GraphQL review
- CRUD review
- transaction handling review
- non-analytics code quality review

### Review Domains

Recommended Phase 2 review domains:

- layout and composition
- narrative and storytelling
- navigation and analytical flow
- design token consistency
- accessibility and readability
- semantic-model-backed interaction quality
- evidence and actionability
- governance and standardization

### Evidence Sources

Primary evidence should come from:

- TypeScript component structure that defines analytical layout and interaction
- route or navigation definitions
- CSS variables, token files, and style contracts
- DAX or semantic-model query artifacts when present in reviewable form
- screenshots or captured app states
- selected configuration files relevant to analytical UX

Version 1 should prefer bounded, analytics-relevant evidence extraction over broad repository analysis.

### Example Findings

- `Token inconsistency: KPI cards bypass shared spacing and color tokens`
- `Navigation issue: executive summary has no clear return path to supporting evidence`
- `Accessibility issue: contrast and font scale fall below analytical readability expectations`
- `Semantic-model usage concern: interaction appears to fragment business logic across views`
- `Storytelling gap: dashboard lacks explicit scan path from headline KPI to supporting comparison`
- `Governance issue: app deviates from approved analytical design token set`

### Overview Additions

Phase 2 overview should support:

- app quality summary
- high-level analytical UX health
- governance posture
- accessibility posture
- semantic-model-backed experience quality summary

### Issues Additions

Issues should support:

- token violations
- navigation problems
- layout and density problems
- accessibility findings
- semantic-model usage concerns
- screenshot-linked findings
- governance findings

### Fix Plan Additions

Fix Plan should remain advisory-first and should support:

- remediation actions
- standardization actions
- governance actions
- migration follow-up recommendations

No code generation or automatic repo mutation is in scope for this design.

### Evidence Additions

Evidence should support:

- screenshot evidence
- code-derived findings
- design-token evidence
- semantic-model evidence
- navigation evidence

### Relationship to AI Fixes

Fabric App Review Mode should also remain advisory-first and separate from deterministic PBIR execution behavior.

The repo's existing AI-fix trust boundary applies:

- AI may summarize, prioritize, explain, and enrich
- AI may not mutate directly
- deterministic preview/apply/rollback remains scoped to supported PBIR edits only

For this spec, Fabric App review findings may contribute to:

- advisory remediation wording
- prioritized review queues
- governance review follow-up

They must not contribute to:

- direct repo mutation
- automatic TypeScript edits
- generated analytical app code
- hidden code transformation pipelines

## Relationship to Report Design Studio

Future Report Design Studio capabilities may generate new analytics experiences.

Generated experiences should still enter the platform through the same `Analyzable Surface` architecture.

Generation and review remain separate concerns.

Generated artifacts should be reviewable through:

- `Overview`
- `Issues`
- `Fix Plan`
- `Evidence`
- `Export`

using the same workspace and analyzer model.

This preserves platform consistency and prevents Report Design Studio from becoming a separate review ecosystem.

## Findings Model Impact

The current normalized findings model should remain the shared issue contract.

It will need additive expansion, not replacement, to support:

- `surfaceType`
- `analyzerType`
- richer evidence provenance
- migration readiness categories
- app-review-specific governance categories

Recommended example fields:

- `surfaceType: pbirReport | fabricApp | screenshotBundle | future`
- `analyzerType: pbir | fabricAppReadiness | fabricAppReview | future`
- `evidenceSourceType`
- `readinessImpact`
- `portabilityState`

This remains a normalized findings extension, not a second issue model.

## Evidence Model Impact

The shared evidence model should be widened so evidence can be linked from:

- PBIR metadata
- screenshots
- code locations
- token definitions
- navigation definitions
- semantic-model-related artifacts

The evidence section should render these as evidence types, not as separate surface-specific mini-products.

## Governance Model Impact

Fabric App governance in this spec is still analytics-governance, not app-platform governance.

Examples:

- approved analytical token sets
- accessibility minimums
- required navigation affordances
- approved visualization patterns
- required evidence/drill-support conventions
- semantic-model-backed experience standards

Out of scope:

- backend secret management
- infrastructure security posture
- GraphQL policy design
- operational app permissions architecture

## UX Flow

Shared Version 1 flow:

1. User opens an analyzable surface.
2. System identifies the surface type.
3. System chooses the matching analyzer.
4. Analyzer emits findings, evidence, remediation, and governance signals.
5. User reviews results in the existing workspace:
   - `Overview`
   - `Issues`
   - `Fix Plan`
   - `Evidence`
   - `Export`

This keeps the platform coherent and prevents parallel review products.

## Non-Goals

- no separate top-level Fabric App workspace
- no Fabric App code generation
- no Fabric App project scaffolding
- no operational-app analysis
- no CRUD/workflow review
- no generic software engineering lint platform
- no hidden mutation authority for AI
- no deterministic repo editing workflow in this phase

## Potential Future Surfaces

The `Analyzable Surface` abstraction should support future expansion without changing the workspace model.

Potential future surfaces include:

- screenshot bundle
- review packet or deliverable package
- benchmark or design-standard pack
- mobile or responsive capture set
- bookmark-state bundle
- semantic-model interaction trace set

Each future surface should be admitted only if it can produce:

- findings
- evidence
- remediation guidance
- governance signals

This keeps future roadmap work aligned with the platform direction:

- one review workspace
- many analyzable surfaces
- bounded analyzers and profiles

Not:

- one new workspace per feature area

## Risks

- Fabric Apps are broad and still evolving, so review scope can drift into general app analysis unless bounded tightly.
- Repo review can become noisy if evidence extraction is not constrained to analytics-relevant surfaces.
- Users may expect code generation or full migration automation; the UI must clearly label this work as advisory review.
- Governance can overreach into generic frontend policy unless kept centered on analytics experience quality.

## Mitigations

- keep `Analyzable Surface` explicit in architecture and UX copy
- keep findings mapped to analytics UX categories
- keep evidence extraction bounded and source-typed
- keep Fabric App support advisory-first
- keep app-governance scoped to analytical standards

## Test Strategy

Design-level validation should cover:

- surface identification tests
- analyzer selection tests
- normalized findings adaptation tests
- readiness scoring and blocker derivation tests
- app-review evidence extraction tests
- screenshot-to-finding linkage tests
- governance signal derivation tests
- shared workspace rendering regression tests

## Dependencies

- stable normalized findings contract
- stable shared workspace builders
- current evidence workflows
- screenshot audit foundations
- governance extension points
- clear local discovery rules for Fabric App repo surfaces

## Rollout Recommendation

### Release Slice 1

`Fabric App Readiness Assessment`

Why first:

- strongest fit with current PBIR-centered product
- uses existing report scoring and evidence foundations
- answers a timely market question without entering general app review

### Release Slice 2

`Fabric App Review Mode`

Why second:

- extends the shared workspace once the readiness story is established
- introduces direct repo review only after the product has a stable analyzable-surface abstraction

## Summary

This design keeps the product coherent:

- same workspace
- different analyzers
- shared findings
- shared remediation
- shared evidence
- shared governance review patterns

The core move is not “support Fabric Apps” as a separate product.

The core move is:

`PBIR Design Analyzer`
`-> Analytics Experience Review Platform`

With `Analyzable Surface` as the architectural bridge that lets PBIR reports, analytical Fabric Apps, and future analytics surfaces flow through one review model.
