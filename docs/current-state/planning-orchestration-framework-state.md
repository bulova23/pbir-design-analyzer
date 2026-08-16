# Planning Orchestration Framework Current State

## Summary

Planning Orchestration Framework is now implemented as the authoritative planning-only coordinator across the existing Design Package, Generation Request, Execution Plan, Provider Adapter, Microsoft Adapter Specification, Capability Negotiation, Microsoft Skills Catalog, and Execution Provider contract layers.

Its role is:

- consume a trusted Design Package
- compose the existing planning frameworks in a deterministic order
- validate stage transitions, predecessor outputs, version compatibility, readiness consistency, and reference integrity
- produce `planning-orchestration/v1` lifecycle state
- produce `planning-outcome/v1` as the terminal planning artifact
- stop before any runtime provider activity

It is not an execution workflow, not a Microsoft Skills runtime, not a CLI runtime, not an artifact-generation surface, and not a deployment path.

The downstream PBIR specification seam is documented separately in `docs/current-state/pbir-generation-specification-framework-state.md`, and the downstream PBIR prototype seam is documented in `docs/current-state/pbir-execution-prototype-boundary-state.md`; Planning Orchestration itself remains planning-only.

## Current Product Position

Planning Orchestration Framework now sits downstream from Design Studio package production and upstream from any future runtime provider implementation.

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs
- Design Package Consumption Layer normalizes package inputs
- Generation Request Framework creates provider-neutral generation contracts
- Provider Planning Framework creates execution plans
- Provider Adapter Framework evaluates provider-neutral adapter compatibility
- Microsoft Adapter Specification translates Microsoft planning requirements
- Capability Negotiation Framework resolves capability requirements
- Microsoft Skills Catalog resolves Microsoft skill candidates and skill-provider readiness
- Execution Provider Contract Framework evaluates provider eligibility
- Planning Orchestration Framework coordinates planning only
- Runtime Provider Framework defines pre-execution runtime abstractions only
- Microsoft Runtime Provider Contract defines Microsoft runtime compatibility only
- PBIR Generation Specification Framework translates planning-approved PBIR intent into specification-only artifact definitions
- Analyzer Workspace remains the downstream validation owner for future generated artifacts

## What Exists Today

The implemented orchestration layer currently includes:

- `planning-orchestration/v1`
- `planning-outcome/v1`
- `PlanningOrchestrationService`
- `PlanningReadinessAggregator`
- explicit stage history and transition history
- explicit transition validation for:
  - Design Package → Generation Request
  - Generation Request → Execution Plan
  - Execution Plan → Provider Adapter Evaluation
  - Provider Adapter Evaluation → Microsoft Planning Translation
  - Microsoft Planning Translation → Capability Negotiation
  - Capability Negotiation → Microsoft Skills Catalog Resolution
  - Microsoft Skills Catalog Resolution → Microsoft Skill Provider Selection
  - Microsoft Skill Provider Selection → Execution Provider Eligibility
- explicit planning outcome statuses:
  - `draft`
  - `planningComplete`
  - `planningBlocked`
  - `planningFailed`
  - `approvedForExecutionProvider`
- explicit readiness summary aggregation
- explicit planning failure classification
- boundary tests proving the layer remains execution-free

## Planning Lifecycle Model

The current deterministic lifecycle is:

Design Package  
↓  
Generation Request  
↓  
Execution Plan  
↓  
Provider Adapter Evaluation  
↓  
Microsoft Planning Translation  
↓  
Capability Negotiation  
↓  
Microsoft Skills Catalog Resolution  
↓  
Microsoft Skill Provider Selection  
↓  
Execution Provider Eligibility  
↓  
Planning Outcome

No execution occurs in this lifecycle.

## Planning Outcome Contract

The current authoritative planning outcome artifact is `planning-outcome/v1`.

Its required sections are:

- metadata
  - schema version
  - outcome id
- references
  - Design Package reference
  - Generation Request reference
  - Execution Plan reference
  - Capability Negotiation reference
  - Execution Provider reference
- status
- readiness summary
  - readiness status
  - blocking issues
  - unresolved requirements
  - capability summary
  - approval status
  - execution-provider readiness
- lineage
  - upstream lineage
  - planning lineage
  - approval lineage

Failures remain part of the planning outcome and are not thrown as runtime execution results.

## Stage Transition Model

`PlanningOrchestrationService` currently validates every transition against explicit rules.

Validation currently covers:

- allowed stage progression
- required predecessor outputs
- source contract version compatibility
- reference integrity across request, plan, negotiation, and provider inputs
- Microsoft skill-catalog resolution presence before execution-provider eligibility
- Microsoft skill-provider selection presence before execution-provider eligibility
- readiness conflicts that would otherwise blur planning ownership

Invalid transitions fail closed into `planning-outcome/v1`.

## Readiness Aggregation Model

`PlanningReadinessAggregator` currently produces:

- overall planning readiness
- blocking conditions
- unresolved requirements
- capability resolution summary
- approval status inherited from upstream planning contracts
- execution-provider readiness without implying runtime execution

Readiness does not mean execution has happened.

`approvedForExecutionProvider` means the planning stack is coherent and fully approved for a future execution provider contract only.

## Current Trust Boundaries

The current framework does not:

- execute Microsoft Skills
- invoke providers
- call provider APIs
- invoke CLI commands
- generate artifacts
- generate PBIR outputs
- generate Fabric App outputs
- generate Fabric Data App outputs
- deploy assets
- validate generated outputs
- automate Analyzer Workspace

## Runtime Implementation Gap

The current repo state still excludes:

- Runtime Provider implementations
- Microsoft runtime provider implementations
- PBIR generation providers
- Microsoft Skills runtime execution
- CLI-backed runtime execution
- provider invocation
- artifact generation
- artifact intake and quarantine
- deployment workflows
- Analyzer Workspace automation
