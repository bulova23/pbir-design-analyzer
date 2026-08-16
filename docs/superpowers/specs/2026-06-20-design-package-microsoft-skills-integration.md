# Design Package → Microsoft Skills / CLI Integration Design

Date: 2026-06-20

Status: Design specification only. No code changes are included in this document.

## Purpose

Define the architecture, contracts, workflows, trust boundaries, and provenance model required to convert Discovery Wizard Design Packages into Microsoft Power BI Skills and CLI consumable generation requests without changing Discovery Wizard, Design Studio, or Analyzer Workspace ownership.

## Authoritative Inputs

- `AGENTS.md`
- `docs/report-discovery-wizard-mvp-readiness-assessment.md`
- `docs/superpowers/specs/2026-06-18-report-discovery-wizard-design.md`
- `docs/superpowers/plans/2026-06-18-report-discovery-wizard-plan.md`
- `docs/superpowers/specs/2026-06-12-report-design-studio-design.md`
- `docs/superpowers/specs/2026-05-31-full-score-panel-workspace-modernization-design.md`
- `docs/report-design-studio-trust-boundary.md`
- `docs/architecture/contract-schema-and-ownership-strategy.md`
- existing backend-internal Design Package contract in `service-dotnet/Services/Discovery/Models/DesignPackageModels.cs`
- Microsoft public guidance reviewed on 2026-06-20:
  - Power BI agentic capabilities overview
  - Power BI report planner and management skills overview
  - Power BI report authoring skill overview
  - Power BI report design skill overview
  - Power BI Desktop Bridge overview
  - PBIR / PBIP documentation
  - Fabric report definition documentation
  - Fabric Apps and data app template documentation

## Executive Summary

The Design Package is ready to become the upstream planning contract for generated artifacts, but it should not be exposed directly as the provider-facing execution contract.

The recommended architecture introduces a new versioned Generation Request boundary between the existing Design Package and any Microsoft-specific adapter. The Design Package remains the discovery and design handoff artifact. The Generation Request becomes the stable provider-neutral execution brief. Microsoft-specific adapters then translate that request into:

- Power BI report authoring flows for PBIR generation
- Fabric data app template flows for Fabric Data App generation
- later Fabric App generation paths once the product's Fabric terminology is disambiguated against Microsoft's new Fabric Apps preview surface

This preserves the current trust model:

- Discovery Wizard remains advisory-only
- Design Studio remains the design owner
- generation becomes a constrained construction step with no approval authority
- Analyzer Workspace remains the validation owner

Generated artifacts must never bypass review. The correct lifecycle is:

Design Package  
↓  
Generation Request  
↓  
Generated Artifact  
↓  
Analyzer Workspace  
↓  
Review  
↓  
Refinement

## Architectural Review Findings

Ranked by long-term risk.

### 1. Highest Risk: Exposing The Raw Design Package As The Provider Contract

The current Design Package is backend-internal and discovery-shaped. It contains useful content for generation, but it is not yet a stable public contract with required versus optional rules, versioning, or adapter semantics. Direct provider consumption would couple future Microsoft-specific assumptions back into Discovery Wizard output semantics.

Design response:

- keep the Design Package as the authoritative upstream planning artifact
- add a new versioned Generation Request contract as the provider-facing boundary
- perform all provider-specific shaping after the Design Package seam, not inside Discovery Wizard

### 2. Highest Risk: Collapsing Design Ownership Into Microsoft Planner Or Authoring Skills

Microsoft's report planner skill owns requirement gathering, brief production, plan production, and approval gating for its native workflow. If this product delegates those responsibilities to the planner skill, Discovery Wizard and Design Studio lose architectural ownership.

Design response:

- treat Microsoft planner/design/authoring capabilities as downstream execution adapters
- keep Discovery Wizard and Design Studio authoritative for recommendation, blueprint, and design intent
- use Microsoft skills to consume a locked generation brief, not to redefine it

### 3. Highest Risk: Generated Artifacts Gain Implied Authority

If a generated PBIR or Fabric artifact is treated as approved because it came from a Design Package, the current validation-first architecture collapses.

Design response:

- generated artifacts are unapproved by default
- review is always required
- Analyzer Workspace remains the only validation authority
- generation approval means permission to generate, not permission to accept

### 4. High Risk: Microsoft Surface Terminology Is Already Diverging From Repo Terminology

The repo uses Fabric App and Fabric Data App as experience types. Current Microsoft guidance now uses Fabric Apps as a preview app platform and a data app template built around Rayfin CLI and semantic-model connectivity. There is now a real risk that repo-internal terminology and Microsoft product terminology drift apart.

Design response:

- formally map internal experience types to Microsoft target profiles
- keep target-profile resolution explicit in the Generation Request
- do not assume the repo's existing Fabric App label equals a single Microsoft runtime target

### 5. High Risk: One Giant Prompt Becomes The De Facto Contract

A single free-form prompt would be hard to validate, diff, test, version, and keep backward compatible. It would also blur what the product intended versus what the adapter improvised.

Design response:

- use a hybrid model:
  - structured Generation Request JSON is authoritative
  - adapter-composed prompt segments are derived
  - provider instructions remain deterministic projections of structured fields

### 6. Medium Risk: Analyzer Handoff Becomes Implicit Automation

If generation auto-launches review or auto-attaches results, Design Studio and Analyzer Workspace boundaries become harder to reason about.

Design response:

- use staged handoff
- allow automatic candidate preparation only
- require explicit user action to open Analyzer Workspace
- require explicit result attachment on return

### 7. Medium Risk: Failure Cases Are Treated As Prompt Quality Problems Instead Of Contract States

Malformed output, partial output, unsupported target types, and validation failures need typed outcomes rather than generic "generation failed" messaging.

Design response:

- define typed generation outcome states
- preserve partial and degraded artifacts as quarantined review candidates only when structurally safe
- keep unsupported and malformed outputs outside analyzer handoff

## Goals

- Convert a Design Package into a stable provider-neutral generation contract.
- Support Microsoft Power BI Skills and CLI consumption without embedding Microsoft-specific semantics into Discovery Wizard.
- Preserve Design Studio ownership of design intent and approvals.
- Preserve Analyzer Workspace ownership of review and validation.
- Preserve lineage from semantic model through generated artifact and analyzer result.
- Support phased artifact generation across PBIR Report, Fabric App, and Fabric Data App targets.
- Create a testable and versionable contract strategy before implementation.

## Non-Goals

- No implementation of Microsoft skills.
- No implementation of CLI execution.
- No provider runtime execution.
- No change to Discovery Wizard recommendation logic.
- No change to Design Studio approvals or ownership.
- No change to Analyzer Workspace validation ownership.
- No direct deployment or publish architecture.
- No bypass of deterministic preview/apply/rollback for report mutations already governed elsewhere in the repo.

## Fixed Boundaries

These remain unchanged:

- Discovery Wizard is advisory-only.
- Design Studio owns design intent, design approvals, and design iteration.
- generation may construct artifacts but may not self-approve them
- Analyzer Workspace owns review, findings, and validation
- AI or skills may enrich or generate, but never carry mutation authority by themselves
- generated artifacts are downstream from scoring and design
- provenance must remain explicit across every handoff

## Current And Target Workflow

Current:

Semantic Model  
↓  
Discovery Wizard  
↓  
Recommendation  
↓  
Experience Blueprint  
↓  
Design Studio  
↓  
Design Package

Target:

Design Package  
↓  
Generation Request  
↓  
Microsoft Adapter  
↓  
Generated Artifact  
↓  
Analyzer Workspace  
↓  
Review  
↓  
Refinement

## Canonical Architecture

### Layers

#### 1. Design Package Layer

Authoritative upstream planning artifact produced from Discovery Wizard and Design Studio context.

Responsibilities:

- preserve advisory recommendation lineage
- preserve business outcome, audience, KPI, page, navigation, and rationale intent
- remain provider-neutral

#### 2. Generation Request Layer

New versioned provider-neutral execution contract derived from the Design Package.

Responsibilities:

- classify target artifact type
- declare required inputs and transforms
- normalize optional versus required semantics
- declare review requirements and handoff expectations
- carry generation-safe provenance

#### 3. Microsoft Adapter Layer

Provider-specific translation layer that consumes the Generation Request and emits:

- Microsoft skill inputs
- CLI invocation plans
- artifact import metadata

Responsibilities:

- map provider-neutral intent into Microsoft-native command and prompt structures
- validate prerequisites and unsupported combinations
- never reinterpret upstream ownership

#### 4. Generated Artifact Intake Layer

Quarantine and classify generated output before analyzer handoff.

Responsibilities:

- parse and fingerprint generated artifacts
- capture generator diagnostics
- classify outcome as complete, partial, malformed, or unsupported
- create analyzer handoff candidates only when structurally safe

#### 5. Analyzer Handoff Layer

Reuses the existing explicit handoff pattern from Design Studio.

Responsibilities:

- create a staged review candidate
- preserve generation provenance
- require explicit analyzer launch
- require explicit analyzer result attachment

## Contract Strategy

### Contract Owners

- Design Package:
  - existing backend-internal discovery/design output contract
  - remains authoritative for upstream planning semantics
- Generation Request:
  - new versioned contract and the recommended public boundary for provider adapters
  - should be treated as the compatibility-critical seam
- Microsoft adapter contracts:
  - adapter-internal projection models
  - must not become upstream schema owners

### Why A New Generation Request Contract Is Required

The current Design Package contract is rich enough for planning but not strict enough for provider execution because it does not yet define:

- field-level required versus optional semantics for generation
- artifact target capability constraints
- target-specific transforms
- outcome and failure semantics
- adapter versioning rules

Therefore the Design Package should be consumed through a transformation layer, not exposed directly.

## Design Package Consumption Model

### Consumed Fields

These fields should be consumed in the first implementation wave.

| Design Package field | Generation role | Required? | Notes |
| --- | --- | --- | --- |
| `PackageId` | upstream reference | required | becomes source package reference |
| `DiscoveryContext` | provenance root | required | source lineage only, not provider prompt text |
| `Audience.PrimaryAudience` | target-user anchor | required | primary persona for all targets |
| `Audience.SecondaryAudiences` | audience tailoring | optional | useful for app navigation and audience-specific routes |
| `Audience.Personas` | role context | optional | keep as enrichment, not minimum build blocker |
| `ExperienceDefinition.ExperienceType` | target profile resolution | required | drives target capability routing |
| `ExperienceDefinition.BusinessOutcome` | business objective | required | anchor for brief and success definition |
| `ExperienceDefinition.Confidence` | trust signal | optional | should influence review warnings, not provider behavior |
| `ExperienceDefinition.BusinessValue` | prioritization metadata | optional | useful for review display and queueing |
| `ExperienceDefinition.Complexity` | generation posture | optional | may change strictness and expected partiality |
| `Pages` | page or route intent | required for PBIR, optional for app targets | primary structure source |
| `Kpis` | KPI binding intent | required | required for all target profiles |
| `Filters` | filter context | required | preserve scope; do not let provider infer from scratch |
| `VisualRecommendations` | visualization hints | optional initially | useful for PBIR and data app generation but should not block MVP |
| `Navigation` | sequence and route shape | required | especially important for multi-page reports and apps |
| `AnalyticalFlow` | narrative chain | required | required for rationale and review alignment |
| `SuccessCriteria` | acceptance baseline | required | becomes review contract, not provider free-form advice |
| `RecommendationRationale` | design-defense context | optional for generation, required for provenance display | useful to explain why an artifact exists |
| `ProviderGuidance` | generation summary | optional | valuable seed, but should not be sole execution contract |
| `Provenance` | lineage display and audit | required | preserve and extend, never discard |

### Fields That Must Be Transformed

- `ExperienceDefinition.ExperienceType`
  - transform into a `targetArtifactProfile`
  - examples:
    - `PbirReport`
    - `FabricDataApp`
    - `FabricApp`
- `Pages`
  - transform into either report pages, app routes, or app modules
- `VisualRecommendations`
  - transform into target-specific visual capability hints
- `Navigation`
  - transform into either report navigation order or application routing model
- `SuccessCriteria`
  - transform into machine-readable review checkpoints
- `Provenance`
  - transform into immutable generation lineage plus adapter-execution metadata

### Fields That Should Not Be Used As Direct Execution Instructions

- `Confidence`
- `BusinessValue`
- `Complexity`
- `RecommendationRationale.SupportingSemanticSignals`
- `RecommendationRationale.ProvenanceNotes`

These are review and audit signals. They should not be handed to the provider as if they were artifact instructions.

## Generation Request Contract

### Required Top-Level Sections

- `schemaVersion`
- `requestId`
- `sourceDesignPackageRef`
- `targetArtifactProfile`
- `generationMode`
- `designIntent`
- `structuralIntent`
- `dataIntent`
- `successContract`
- `provenance`
- `reviewPolicy`

### Conceptual Shape

```json
{
  "schemaVersion": "generation-request/v1",
  "requestId": "genreq:...",
  "sourceDesignPackageRef": "designPackage:...",
  "targetArtifactProfile": {
    "artifactType": "pbirReport",
    "adapter": "microsoftPowerBi",
    "capabilityProfile": "reportAuthoring"
  },
  "generationMode": {
    "authority": "advisoryConstructionOnly",
    "allowPartialOutput": true,
    "requireHumanReview": true
  },
  "designIntent": {
    "primaryAudience": "...",
    "businessOutcome": "...",
    "analyticalFlow": {
      "question": "...",
      "investigation": "...",
      "evidence": "...",
      "decision": "..."
    }
  },
  "structuralIntent": {
    "pagesOrRoutes": [],
    "navigation": {},
    "visualHints": []
  },
  "dataIntent": {
    "kpis": [],
    "filters": {},
    "semanticModelBinding": {}
  },
  "successContract": {
    "businessSuccessCriteria": [],
    "analyticalSuccessCriteria": [],
    "reviewRequired": true,
    "validationRequired": true
  },
  "provenance": {
    "lineage": [],
    "adapterProfile": "microsoftPowerBi/reportAuthoring"
  },
  "reviewPolicy": {
    "designApprovalRequired": true,
    "generationApprovalRequired": true,
    "analyzerReviewRequired": true
  }
}
```

### Compatibility Rules

- additive optional fields are allowed
- required-field renames are breaking
- new `artifactType` values are breaking unless all consumers are tolerant first
- adapter projections must fail closed on unknown required semantics

## Skills Prompt Generation Model

### Recommendation

Use a hybrid model.

Not recommended:

- one giant prompt
- prompt-only contract
- direct raw Design Package injection

Recommended:

#### 1. Structured Generation Request JSON

Authoritative source of truth for execution intent.

#### 2. Adapter-Composed Prompt Segments

Derived from the request and assembled deterministically:

- target summary
- audience and business outcome
- required structure
- required data bindings
- explicit do-not-change constraints
- success criteria
- validation instructions

#### 3. Optional CLI Execution Plan

Separate from the prompt. This includes:

- prerequisite checks
- command plan
- expected artifact paths
- validation steps

### Why Hybrid Wins

- JSON is testable and diffable
- prompt segments remain readable for human inspection
- CLI plans stay operational rather than being hidden in prompt text
- provider-specific evolution does not force Design Package contract churn

## Microsoft Adapter Profiles

### 1. PBIR Report Adapter Profile

Primary fit for initial implementation.

Use Microsoft capabilities this way:

- `powerbi-report-authoring`
  - primary authoring mechanism
  - Microsoft explicitly positions this skill as the PBIR file authoring and validation path
- `powerbi-report-design`
  - optional downstream enrichment when additional visual-design detail is needed
  - should not replace Design Studio ownership
- `powerbi-report-planner`
  - not the primary ownership path here because it duplicates requirement gathering and approval
  - may be used only as a compatibility bridge if a future execution environment requires planner-shaped inputs
- `validate-report`
  - required structural validation step before intake
- Power BI Desktop Bridge
  - optional screenshot and live verification step before analyzer handoff

### 2. Fabric Data App Adapter Profile

Phased support.

Current Microsoft guidance indicates:

- the data app template is the preferred path for analytics-oriented Fabric Apps connected to semantic models
- Rayfin CLI is currently the supported creation path

Therefore this target should use:

- structured app intent from the Generation Request
- semantic model link and workspace context
- template-specific route, KPI, and visual intent
- CLI-backed scaffold plan

### 3. Fabric App Adapter Profile

Deferred until terminology and target-runtime semantics are stabilized inside this repo.

Reason:

- Microsoft Fabric Apps preview is now a concrete product surface
- the repo's internal Fabric App concept predates or abstracts over that product
- implementation should not proceed until the mapping is explicit

## Artifact Type Support Strategy

### Initial Support

- PBIR Report

Reason:

- best current contract fit
- strongest Microsoft documentation coverage
- easiest alignment with existing Analyzer Workspace review surface

### Phase 2 Support

- Fabric Data App

Reason:

- Microsoft has a specific template and CLI-backed path
- still preview and operationally distinct from PBIR
- requires a different artifact intake and validation story

### Deferred Support

- Fabric App

Reason:

- terminology collision risk
- runtime and validation semantics are not yet aligned enough with current repo assumptions

## Trust Boundaries

### Discovery Wizard

Authority:

- discovers opportunities
- recommends experiences
- produces blueprint and package intent

No authority:

- no generation approval
- no provider execution
- no artifact acceptance
- no analyzer validation

### Design Studio

Authority:

- owns design approval
- owns the selected design baseline
- owns explicit permission to proceed to generation

No authority:

- no provider-side success claims
- no validation approval
- no analyzer result fabrication

### Generation Layer

Authority:

- transform a Design Package into a Generation Request
- invoke Microsoft adapters in the future implementation
- produce generated artifacts and diagnostics

No authority:

- no design approval
- no validation approval
- no deployment approval
- no bypass of Analyzer Workspace
- no silent mutation authority over repository assets beyond the scoped generation workspace

### Analyzer Workspace

Authority:

- review generated artifacts
- produce findings
- determine validation outcome
- feed refinement back into Design Studio

No authority:

- no upstream redesign of Discovery Wizard intent
- no implicit acceptance of generation because it structurally succeeded

## Human-In-The-Loop Controls

### Required Approvals

#### 1. Design Approval

Required before generation can be requested.

Owner:

- Design Studio

#### 2. Generation Approval

Required before the adapter can construct a new artifact.

Owner:

- Design Studio workflow or equivalent host-side orchestration

Meaning:

- permission to generate
- not permission to accept

#### 3. Review Approval

Required after Analyzer Workspace review and before the iteration is treated as validated or ready for refinement closeout.

Owner:

- Analyzer Workspace validation flow

### Generated Artifacts Can Never Bypass Review

Answer:

- no

Even if:

- generation completed without errors
- provider validation passed
- the artifact renders in Desktop or the browser

Analyzer review is still required because rendering success is not equivalent to product validation.

## Generated Artifact Lifecycle

### States

1. `designPackageReady`
2. `generationApproved`
3. `generationRequested`
4. `generationInProgress`
5. `generatedComplete`
6. `generatedPartial`
7. `generatedMalformed`
8. `generatedUnsupported`
9. `candidatePrepared`
10. `reviewQueued`
11. `reviewInProgress`
12. `validated`
13. `rejected`
14. `returnedForRefinement`

### Rules

- review is required for every generated artifact admitted into the product workflow
- validation is required before any generated artifact is treated as trusted output
- malformed or unsupported output must not enter analyzer handoff
- partial output may enter analyzer handoff only when structurally safe and clearly labeled as degraded

## Analyzer Integration

### Recommended Handoff Model

Staged handoff.

Why:

- matches existing Design Studio to Analyzer Workspace boundary
- avoids silent analyzer execution
- preserves explicit review intent

### Handoff Sequence

1. Generation intake creates a generated-artifact record.
2. Intake validates structural eligibility.
3. If eligible, the system creates an analyzer handoff candidate.
4. The user explicitly opens Analyzer Workspace.
5. Analyzer results return through the existing explicit attachment pattern.

### Not Recommended

- automatic analyzer execution immediately after generation
- automatic result attachment
- direct generated artifact promotion into validated state

## Provenance Model

### Canonical Lineage

Semantic Model  
↓  
Discovery Profile  
↓  
Opportunity  
↓  
Recommendation  
↓  
Experience Blueprint  
↓  
Design Package  
↓  
Generation Request  
↓  
Generated Artifact  
↓  
Analyzer Result

### Required Provenance Additions

The current Design Package lineage should be extended with:

- `generationRequest`
- `generationAdapter`
- `generatedArtifact`
- `generatedArtifactFingerprint`
- `generationExecution`
- `analyzerHandoffCandidate`
- `analyzerRun`

### Storage Model

Store provenance in three layers:

#### 1. Immutable Lineage References

- stage
- reference id
- label

#### 2. Execution Metadata

- adapter id
- adapter version
- requested target profile
- execution timestamps
- CLI or skill execution reference ids
- validation tool results

#### 3. Display Metadata

- generation status
- partiality flags
- degraded warnings
- review-required state

### Display Model

Display provenance in both:

- Design Studio generation history
- Analyzer Workspace candidate summary

Key display elements:

- source semantic model reference
- selected recommendation and package id
- target artifact type
- generation status
- adapter used
- validation and review state

## Failure Handling

### Failure Classes

#### 1. Generation Failure

Examples:

- skill invocation failure
- CLI failure
- prerequisite failure

Behavior:

- no analyzer handoff candidate
- diagnostics preserved
- user can retry after correction

#### 2. Partial Generation

Examples:

- some pages generated
- some visuals omitted
- app shell scaffolded without complete bindings

Behavior:

- allowed only if structurally parseable
- must be labeled degraded
- analyzer handoff allowed only with explicit warning

#### 3. Malformed Output

Examples:

- invalid PBIR structure
- broken generated app scaffold
- invalid definition payload

Behavior:

- quarantine only
- no analyzer handoff
- require regeneration or manual repair

#### 4. Unsupported Artifact Type

Examples:

- Design Package maps to a target profile with no implemented adapter

Behavior:

- fail fast before execution
- preserve request and rationale

## Backward Compatibility Strategy

- do not widen the current public `ScoreResult` or page score contracts
- keep the Design Package backend-internal until a deliberate public schema is introduced
- add the Generation Request as the compatibility boundary instead of retrofitting raw Design Package exposure
- make artifact-type support additive through new target profiles, not by changing existing profile semantics

## Recommended Architecture Decisions

1. Introduce `generation-request/v1` as the new public provider-neutral boundary.
2. Keep the Design Package authoritative upstream but backend-internal.
3. Support PBIR Report first.
4. Use a hybrid structured JSON plus derived prompt model.
5. Require explicit generation approval and explicit analyzer handoff.
6. Require analyzer review for every generated artifact.
7. Treat Fabric Data App as the second target profile.
8. Defer Fabric App until terminology and runtime mapping are explicit.

## Open Mapping Decision To Lock Before Implementation

The repo should explicitly document whether:

- internal `Fabric App` maps to Microsoft Fabric Apps preview broadly
- internal `Fabric Data App` maps specifically to the Microsoft data app template
- Power BI org apps remain a separate downstream publish target rather than a generated artifact type

This decision should be locked before Phase 4 implementation begins.
