# Microsoft Adapter Specification Current State

## Summary

Microsoft Adapter Specification is now implemented as a descriptive planning-only layer downstream from Provider Adapter Framework.

Its role is:

- consume provider-adapter/v1 planning input plus execution-plan/v1 target context
- define microsoft-adapter-specification/v1 as the authoritative Microsoft capability-mapping contract
- map provider-neutral planning requirements into Microsoft capability requirements deterministically
- classify supported, unsupported, and future Microsoft target-profile combinations
- surface Microsoft planning readiness for a future capability negotiation and execution-provider seam without executing anything

It is not a Microsoft execution provider, not a Microsoft Skills invocation path, and not an artifact-generation surface.

## Current Product Position

Microsoft Adapter Specification currently sits between provider-neutral adapter compatibility and Capability Negotiation Framework.

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- Generation Request Framework creates execution contracts
- Provider Planning Framework creates execution plans
- Provider Adapter Framework evaluates provider-neutral adapter compatibility
- Microsoft Adapter Specification defines Microsoft capability mappings
- Capability Negotiation Framework resolves capability requirements
- Execution Provider Contract Framework defines runtime provider contracts
- Analyzer Workspace validates generated artifacts

## What Exists Today

The implemented Microsoft specification layer currently includes:

- microsoft-adapter-specification/v1 contract
- schema metadata, provider identity, supported target profiles, capability mappings, target-profile mappings, compatibility catalog, constraint catalog, and review-requirements catalog
- MicrosoftProviderPlanningTranslator
- MicrosoftAdapterSpecificationValidator
- MicrosoftAdapterSpecificationService
- Microsoft planning readiness states:
  - unsupported
  - partiallySupported
  - supported
  - readyForMicrosoftAdapter
- boundary tests proving the layer stays descriptive only

## Microsoft Adapter Specification Contract

The current authoritative Microsoft planning artifact is microsoft-adapter-specification/v1.

Its required sections are:

- schema metadata
- provider identity
- supported target profiles
- capability mappings
- target-profile mappings
- compatibility catalog
- constraint catalog
- review-requirements catalog

This contract is descriptive only. It does not replace generation-request/v1, execution-plan/v1, or provider-adapter/v1.

## Capability Catalog

The current Microsoft capability catalog declares:

- layout generation
- page generation
- navigation generation
- semantic generation
- deployment support
- validation support

In the current repo state:

- PBIR planning resolves layout, page, navigation, and semantic capabilities as supported
- Fabric Data App planning remains future-facing because deployment support is only planned
- Fabric App remains unsupported because terminology and target-runtime mapping are not locked

No capability declaration performs execution.

## Compatibility Catalog

MicrosoftAdapterCompatibilityCatalog currently defines:

- supported combinations
  - PBIR Report target profile with layout, page, navigation, and semantic capability requirements
- unsupported combinations
  - Fabric App target profile combinations
- future combinations
  - Fabric Data App target profile combinations that require planned deployment support

These combinations remain descriptive only.

No runtime provider invocation occurs.

## Translation Rules

MicrosoftProviderPlanningTranslator currently:

- translates provider-neutral layout generation into Microsoft layout, page, and navigation requirements
- translates provider-neutral semantic generation into Microsoft semantic requirements
- translates target profiles into deterministic Microsoft planning requirements
- preserves explicit review requirements inherited from existing contracts

Translation remains deterministic and side-effect free.

It does not:

- execute Microsoft Skills
- invoke APIs
- invoke CLI
- create artifacts
- deploy assets
- validate outputs

## Review Requirements Catalog

The current Microsoft review-requirements catalog preserves:

- design approval required
- generation approval required
- analyzer validation required

These are inherited from:

- generation-request/v1
- execution-plan/v1
- provider-adapter/v1

## Current Readiness Model

Microsoft planning readiness currently means descriptive compatibility state only.

The current meanings are:

- unsupported: the requested target or capability combination is not supported by the specification
- partiallySupported: the target is future-facing or depends on planned Microsoft capabilities
- supported: the target and capability requirements are fully described by the specification
- readyForMicrosoftAdapter: a future Microsoft execution provider could theoretically accept the plan

These states do not imply:

- Microsoft Skills execution
- CLI execution
- provider invocation
- artifact generation
- deployment
- Analyzer Workspace automation

Capability Negotiation Framework now consumes this layer downstream and turns its descriptive mappings into explicit required, preferred, optional, substituted, unsupported, blocked, and omitted capability decisions without turning Microsoft Adapter Specification into an execution surface.

Execution Provider Contract Framework then consumes the negotiated result downstream to define provider eligibility, approval inheritance, request/response contracts, and audit lineage without turning Microsoft Adapter Specification into runtime execution.

Microsoft Skills Catalog now consumes this specification further downstream to map negotiated capabilities into descriptive Microsoft skill metadata and skill-provider readiness without turning Microsoft Adapter Specification into a provider implementation.

Microsoft Skill Provider Adapter now consumes the descriptive skill metadata further downstream to map known skills into descriptive provider candidates and provider-selection readiness without turning Microsoft Adapter Specification into a provider implementation.

Microsoft Runtime Provider Contract then consumes the specification plus the resolved Microsoft skill metadata to define Microsoft-specific runtime target support, request validation, planned-only handling, and readiness classification without turning Microsoft Adapter Specification into a provider implementation.

## Constraint Catalog

The current Microsoft constraint catalog explicitly preserves:

- unsupported artifact types
  - fabricApp
- unsupported experience types
  - FabricApp
- unsupported capability combinations
  - deployment support with validation support in the current specification-only phase

## Remaining Execution Gaps

The current repo state still excludes:

- Microsoft Skills execution providers
- Microsoft skill invocation
- CLI-backed Microsoft execution
- PBIR generation
- Fabric App generation
- Fabric Data App generation
- artifact intake and quarantine
- deployment workflows
- Analyzer Workspace automation
