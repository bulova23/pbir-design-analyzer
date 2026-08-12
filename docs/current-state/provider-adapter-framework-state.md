# Provider Adapter Framework Current State

Phase 35A intentionally does not add an executable provider adapter. This framework remains planning-only; **No runtime generation provider is available**.

## Summary

Provider Adapter Framework is now implemented as a planning-only compatibility layer downstream from Provider Planning Framework.

Its role is:

- consume a valid generation-request/v1 contract and execution-plan/v1 artifact
- derive a versioned provider-adapter/v1 input contract
- register future provider adapters without instantiating them
- evaluate adapter compatibility by target profile, capability set, source-plan coherence, and source-contract versions
- surface readiness for a future execution provider without executing anything

It is not a provider implementation, not a Microsoft Skills execution path, and not an artifact-generation surface.

## Current Product Position

Provider Adapter Framework currently sits between Provider Planning Framework and any future execution provider.

The current repo state now also includes downstream Microsoft Adapter Specification and Capability Negotiation seams that consume provider-adapter/v1 compatibility output descriptively without turning Provider Adapter Framework into a Microsoft execution surface.

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- Generation Request Framework creates execution contracts
- Provider Planning Framework creates execution plans
- Provider Adapter Framework evaluates adapter compatibility
- Microsoft Adapter Specification defines Microsoft capability mappings
- Capability Negotiation Framework resolves capability requirements
- Execution Provider Contract Framework defines runtime provider contracts
- Analyzer Workspace validates generated artifacts

## What Exists Today

The implemented adapter framework currently includes:

- provider-adapter/v1 contract
- Provider Adapter definition contract
- Provider Adapter request contract
- Provider Adapter planning-response contract
- ProviderAdapterRegistry
- ProviderAdapterCompatibilityService
- ProviderAdapterFrameworkService
- explicit readiness states:
  - discovered
  - compatible
  - incompatible
  - unsupported
  - readyForExecutionProvider
- boundary tests proving the framework stays planning-only

## Provider Adapter Contract

The current authoritative adapter-planning input artifact is provider-adapter/v1.

Its required request sections are:

- schema version
- execution plan reference
- generation request reference
- source contract versions
- target artifact profile
- capability requirements
- constraints
- review requirements
- success contract

Its required definition sections are:

- adapter id
- adapter name
- adapter version
- provider category
- supported target profiles
- supported capabilities
- unsupported capabilities
- supported Generation Request schema versions
- supported Execution Plan schema versions

The request is derived from Generation Request and Execution Plan and does not replace either contract.

## Adapter Registry

ProviderAdapterRegistry currently supports:

- register adapters
- discover a specific adapter
- discover all registered adapters
- lookup by capability
- lookup by target profile
- compatibility evaluation delegation through ProviderAdapterCompatibilityService

The registry currently models multiple future providers as definitions only.

It does not instantiate providers, execute providers, invoke CLI, or call APIs.

## Compatibility Evaluation Flow

The current compatibility flow is:

Generation Request  
↓  
Execution Plan  
↓  
provider-adapter/v1 request  
↓  
adapter discovery  
↓  
compatibility evaluation  
↓  
planning response  
↓  
readyForExecutionProvider or fail-closed state

Compatibility currently validates:

- target profile compatibility
- capability compatibility
- execution-plan and Generation Request coherence
- version compatibility

Compatibility outputs are:

- compatible
- incompatible
- unsupported

Planning response outputs are:

- accepted
- rejected
- unsupported
- incompatible

## Current Readiness Model

Provider Adapter readiness currently means planning and compatibility state only.

The current meanings are:

- discovered: the requested adapter definition exists in the registry
- compatible: the adapter definition is compatible with the request
- incompatible: the request or source-contract relationship fails compatibility validation
- unsupported: the adapter does not support the requested target or capabilities
- readyForExecutionProvider: a future execution provider could accept the plan

These states do not imply:

- provider execution
- artifact generation
- artifact validation
- deployment
- Analyzer Workspace automation

## Current Trust Boundaries

The current framework does not:

- execute Microsoft Skills
- execute any provider
- invoke CLI commands
- call provider APIs
- generate PBIR artifacts
- generate Fabric App artifacts
- generate Fabric Data App artifacts
- deploy assets
- validate artifacts
- automate Analyzer Workspace

## Execution-Provider Seam

The current repo now has an explicit downstream seam:

- Generation Request remains the authoritative execution contract
- Execution Plan remains the authoritative planning artifact
- provider-adapter/v1 remains the compatibility-critical adapter input contract
- execution-provider/v1 now formalizes the downstream provider request, response, approval, and audit contract
- future runtime providers must sit downstream from registry discovery, compatibility acceptance, capability negotiation, and execution-provider/v1

Any future provider implementation should consume these contracts and readiness states rather than adding a new upstream contract.

The Microsoft-specific next seam is now:

- provider-adapter/v1 remains provider-neutral
- microsoft-adapter-specification/v1 describes Microsoft target-profile compatibility and capability mappings
- capability-negotiation/v1 resolves Microsoft-facing capability requirements against provider-neutral compatibility
- execution-provider/v1 defines the future runtime contract accepted downstream from all three layers
- future Microsoft execution providers must sit downstream from all four layers
