# Execution Provider Framework Current State

## Summary

Execution Provider Contract Framework is now implemented as a deterministic contract-only layer downstream from Capability Negotiation Framework.

Its role is:

- consume a coherent `generation-request/v1`, `execution-plan/v1`, and `capability-negotiation/v1` stack
- define `execution-provider/v1` as the authoritative future-runtime provider contract
- model provider definitions, provider requests, provider responses, approval inheritance, eligibility evaluation, and audit lineage
- determine whether a future runtime provider request is `notEligible`, `conditionallyEligible`, `eligible`, or `approvedForExecutionProvider`

It is not a runtime provider, not a Microsoft Skills invocation path, not a CLI execution path, and not an artifact-generation surface.

## Current Product Position

Execution Provider Contract Framework currently sits between planning-only capability negotiation and any future runtime provider implementation.

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

The implemented execution-provider layer currently includes:

- `execution-provider/v1` contract
- Execution Provider definition contract
- Execution Provider request contract
- Execution Provider response contract
- `ExecutionApprovalPolicy`
- `ExecutionAuditRecord`
- `ExecutionProviderValidator`
- `ExecutionEligibilityService`
- `ExecutionProviderContractFrameworkService`
- explicit readiness states:
  - `notEligible`
  - `conditionallyEligible`
  - `eligible`
  - `approvedForExecutionProvider`
- boundary tests proving the layer stays contract-only

## Execution Lifecycle Model

The current deterministic lifecycle is:

Generation Request  
↓  
Execution Plan  
↓  
Capability Negotiation Result  
↓  
Execution Provider Request  
↓  
Execution Provider Response

No execution occurs in this lifecycle.

## Execution Provider Contract

The current authoritative execution-provider artifact is `execution-provider/v1`.

Its required provider-definition sections are:

- provider id
- provider name
- provider version
- provider category
- supported capabilities
- supported target profiles
- supported execution modes
- supported source contract versions

Its required request sections are:

- schema version
- request id
- Generation Request reference
- Execution Plan reference
- Capability Negotiation reference
- source contract versions
- review requirements
- success contract
- execution constraints
- requested execution mode
- approval policy

Its required response sections are:

- provider id
- request id
- response status
- eligibility status
- readiness status
- response reasons

## Execution Modes

The current framework defines these descriptive execution modes:

- `manual`
- `assisted`
- `automated`

These modes are contract metadata only.

They do not trigger behavior, orchestration, or side effects.

## Eligibility Model

`ExecutionEligibilityService` currently determines one of:

- `eligible`
  - lineage is coherent
  - inherited approvals are satisfied
  - required capabilities are supported
  - provider definitions are compatible
  - readiness requirements are complete
- `conditionallyEligible`
  - the planning stack is coherent but an inherited approval or readiness precondition is still pending
- `ineligible`
  - the provider definition, target support, capability support, or execution mode is unsupported
- `blocked`
  - lineage, approval inheritance, or contract-version integrity is invalid

Current eligibility validation covers:

- missing references
- invalid lineage
- invalid approval chains
- unsupported provider definitions
- incompatible execution modes
- version mismatches

## Approval Model

`ExecutionApprovalPolicy` currently preserves inherited approval requirements from upstream contracts:

- design approval required
- generation approval required
- analyzer validation required

The current framework also tracks the currently satisfiable pre-execution approval states:

- design approved
- generation approved

Analyzer validation remains a downstream requirement, not a pre-execution completion signal.

This phase validates approval inheritance and sequencing, but does not automate approvals.

## Auditability Model

`ExecutionAuditRecord` currently preserves:

- execution request lineage
  - Generation Request reference
  - Execution Plan reference
  - Execution Provider Request reference
- negotiation lineage
  - Capability Negotiation reference
  - Capability Negotiation schema version
- provider lineage
  - provider id
  - provider version
  - provider category
- approval lineage
  - inherited approval requirements
  - satisfied pre-execution approvals

Auditability is contract-only.

No execution logging exists because no execution exists.

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
- validate generated outputs
- automate Analyzer Workspace

## Runtime Implementation Gap

The current repo state still excludes:

- Microsoft Skills runtime providers
- CLI-backed runtime providers
- provider invocation
- artifact generation
- artifact intake and quarantine
- deployment workflows
- Analyzer Workspace automation
