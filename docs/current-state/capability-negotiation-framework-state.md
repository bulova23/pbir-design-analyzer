# Capability Negotiation Framework Current State

## Summary

Capability Negotiation Framework is now implemented as a deterministic planning-only resolution layer downstream from Microsoft Adapter Specification.

Its role is:

- consume a coherent `generation-request/v1`, `execution-plan/v1`, `provider-adapter/v1`, and `microsoft-adapter-specification/v1` stack
- define `capability-negotiation/v1` as the authoritative capability-resolution artifact
- classify capability requirements as required, preferred, or optional
- resolve capabilities as satisfied, substituted, unsupported, blocked, or omitted
- determine whether the negotiated result is unresolved, partiallyResolved, resolved, blocked, or readyForExecutionProvider

It is not an execution provider, not a Microsoft Skills invocation path, and not an artifact-generation surface.

## Current Product Position

Capability Negotiation Framework currently sits between descriptive Microsoft capability mapping and the contract-only Execution Provider Framework.

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

The implemented negotiation layer currently includes:

- `capability-negotiation/v1` result contract
- capability requirement, capability resolution, substitution, and resolution-summary models
- versioned substitution catalog support
- `CapabilityNegotiationValidator`
- `CapabilityNegotiationService`
- explicit readiness states:
  - `unresolved`
  - `partiallyResolved`
  - `resolved`
  - `blocked`
  - `readyForExecutionProvider`
- boundary tests proving the layer stays planning-only

## Capability Negotiation Contract

The current authoritative capability-resolution artifact is `capability-negotiation/v1`.

Its required sections are:

- schema version
- negotiation id
- target profile id
- provider category
- requirements
- resolutions
- substitutions
- resolution summary
- readiness status

This contract is derived from the upstream planning stack and does not replace:

- `generation-request/v1`
- `execution-plan/v1`
- `provider-adapter/v1`
- `microsoft-adapter-specification/v1`

## Requirement Classification

The current framework classifies capabilities this way:

- required
  - capabilities the target profile must resolve before negotiation can proceed
- preferred
  - supported enrichments that are not mandatory for the target profile
- optional
  - capabilities that may be omitted without blocking readiness

The current default PBIR profile resolves:

- required
  - layout generation
  - navigation generation
  - page generation
  - semantic generation
- optional
  - validation support

## Resolution Model

The current framework resolves each capability deterministically as one of:

- satisfied
- substituted
- unsupported
- blocked
- omitted

Current meaning:

- satisfied
  - the provider-neutral inputs directly support the capability
- substituted
  - an explicit catalog rule resolved the capability through a deterministic alternate capability
- unsupported
  - a non-required capability could not be resolved
- blocked
  - a required capability could not be resolved
- omitted
  - an optional capability was intentionally dropped without blocking readiness

## Substitution Catalog

The current substitution system is explicit, deterministic, and versioned through `capability-substitution-catalog/v1`.

The default catalog currently includes:

- PBIR navigation generation → layout generation
- Fabric Data App navigation generation → layout generation
- PBIR page generation → layout generation

No implicit substitution is allowed.

If a capability needs substitution:

- a rule must exist
- the substitute capability must be defined
- circular rule chains fail closed

## Negotiation Lifecycle

The current deterministic lifecycle is:

Generation Request  
↓  
Execution Plan  
↓  
Provider Adapter  
↓  
Microsoft Adapter Specification  
↓  
Capability Negotiation  
↓  
readyForExecutionProvider or fail-closed state

Negotiation currently validates:

- source-contract version coherence
- request, plan, and adapter target-profile coherence
- target-profile capability definitions
- substitution-catalog integrity
- unsupported required capabilities

## Current Readiness Model

Capability negotiation readiness currently means resolution state only.

The current meanings are:

- unresolved
  - reserved for pre-resolution state before a result exists
- partiallyResolved
  - required capabilities are covered, but at least one preferred or optional capability is substituted, unsupported, or omitted
- resolved
  - every negotiated capability resolves cleanly without partial concessions
- blocked
  - the negotiation stack is invalid or at least one required capability remains unresolved
- readyForExecutionProvider
  - all required capabilities are satisfied or substituted and a future execution provider could theoretically accept the plan

These states do not imply:

- Microsoft Skills execution
- CLI execution
- provider invocation
- artifact generation
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

## Downstream Contract Seam

Capability Negotiation Framework now hands off to the implemented Execution Provider Contract Framework downstream.

That seam is still contract-only:

- negotiation resolves required capabilities
- execution-provider/v1 captures provider acceptance contracts, approval inheritance, and audit lineage
- no runtime provider implementation exists yet
