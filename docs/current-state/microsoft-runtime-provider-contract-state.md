# Microsoft Runtime Provider Contract Current State

Phase 35A does not activate this compatibility contract. It remains descriptive/pre-execution metadata, and **No runtime generation provider is available** until a later Phase 35B+ executable adapter satisfies the separate governance contract.

## Summary

Microsoft Runtime Provider Contract is now implemented as the Microsoft-specific contract-only layer downstream from Planning Orchestration Framework and alongside the generic Runtime Provider Framework.

Its role is:

- define `microsoft-runtime-provider/v1` as the descriptive Microsoft runtime provider definition
- define `microsoft-runtime-request/v1` as the Microsoft-specific pre-execution runtime request
- define `microsoft-runtime-context/v1` as the Microsoft-specific lineage and capability context
- map Microsoft target-profile support and capability support from the existing Microsoft adapter specification into runtime-provider compatibility rules
- carry Microsoft skill-catalog metadata, required skill ids, optional skill ids, and skill-provider readiness without invoking anything
- carry skill-provider-selection metadata plus selected Microsoft skill-provider candidates without invoking anything
- validate Microsoft-specific target-profile support, capability compatibility, review inheritance, provenance completeness, and schema-version compatibility
- classify Microsoft runtime readiness without executing anything
- register and discover a descriptive Microsoft runtime provider through the existing Runtime Provider Registry

It is not a Microsoft Skills execution provider, not a Microsoft API surface, not a CLI runner, not a provider invocation path, and not an artifact-generation surface.

The downstream PBIR-only prototype seam is documented separately in `docs/current-state/pbir-execution-prototype-boundary-state.md` so this layer can remain a Microsoft runtime compatibility contract rather than an execution runtime.

## Current Product Position

Microsoft Runtime Provider Contract now sits after:

- Design Package Consumption Layer
- Generation Request Framework
- Execution Plan Framework
- Provider Adapter Framework
- Microsoft Adapter Specification
- Capability Negotiation Framework
- Microsoft Skills Catalog
- Microsoft Skill Provider Adapter
- Execution Provider Contract Framework
- Planning Orchestration Framework

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- Planning Frameworks prepare planning outcomes and execution candidates
- Runtime Provider Framework defines generic pre-execution runtime abstractions
- Microsoft Runtime Provider Contract defines Microsoft runtime compatibility only
- Analyzer Workspace remains the downstream validation owner for future generated artifacts

## What Exists Today

The implemented Microsoft runtime layer currently includes:

- `microsoft-runtime-provider/v1`
- `microsoft-runtime-request/v1`
- `microsoft-runtime-context/v1`
- `MicrosoftRuntimeProviderContractFrameworkService`
- `MicrosoftRuntimeProviderValidator`
- `MicrosoftRuntimeReadinessService`
- descriptive Microsoft provider registration through `RuntimeProviderRegistry`
- explicit Microsoft runtime readiness states:
  - `invalid`
  - `unsupported`
  - `plannedOnly`
  - `blocked`
  - `candidate`
  - `readyForMicrosoftRuntimeProvider`
- boundary tests proving the layer stays pre-execution only

## Microsoft Runtime Provider Definition

The authoritative Microsoft provider definition is `microsoft-runtime-provider/v1`.

Its required sections are:

- schema version
- provider identity
  - provider id
  - provider name
  - provider version
  - provider category
- supported target profiles
  - target profile id
  - artifact type
  - support status
  - required capabilities
- supported capabilities
  - capability id
  - support status
  - provider capability requirements
- supported execution modes

The current descriptive provider identity is:

- provider id: `microsoft.runtime-provider.contract`
- provider name: `Microsoft Runtime Provider Contract`
- provider version: `1.0.0`
- provider category: `microsoft`

## Microsoft Runtime Request Contract

The authoritative Microsoft runtime request artifact is `microsoft-runtime-request/v1`.

Its required sections are:

- request metadata
- planning outcome reference
- execution candidate reference
- target profile
- Microsoft capability requirements
- Microsoft skill requirements
  - candidate provider ids
  - skill-provider readiness
- review requirements
- execution constraints
- provenance

The request is derived from:

- `runtime-provider-request/v1`
- `planning-outcome/v1`
- runtime execution-candidate identity conventions from Phase 10
- `microsoft-adapter-specification/v1`

It remains pre-execution only.

## Microsoft Runtime Context Contract

The authoritative Microsoft runtime context artifact is `microsoft-runtime-context/v1`.

Its required sections are:

- runtime provider reference
- planning lineage
- generation request lineage
- execution plan lineage
- capability negotiation lineage
- approval lineage
- target profile
- Microsoft capability summary
- Microsoft skill summary
  - candidate provider ids
  - skill-provider readiness

Context is lineage-only and capability-scope-only.

It does not track runtime execution state.

## Microsoft Runtime Validation Model

`MicrosoftRuntimeProviderValidator` currently validates:

- runtime request shape
- Microsoft capability compatibility
- Microsoft skill-catalog schema compatibility
- skill-provider-selection schema compatibility
- Microsoft skill readiness before future runtime-provider handoff
- Microsoft skill-provider readiness before future runtime-provider handoff
- supported target profile handling
- unsupported target profile rejection
- planned target profile handling
- approval requirement inheritance
- provenance completeness
- schema version compatibility

Validation fails closed.

Unsupported targets and invalid capability mappings are rejected.

Planned targets remain descriptive and non-executable.

## Microsoft Runtime Readiness Model

`MicrosoftRuntimeReadinessService` currently determines one of:

- `invalid`
  - the Microsoft runtime request or context is structurally incomplete or internally inconsistent
- `unsupported`
  - the Microsoft runtime target or registration is not supported by the contract
- `plannedOnly`
  - the Microsoft runtime target is intentionally planned-only and remains non-executable
- `blocked`
  - the upstream planning outcome is blocked or failed for reasons that are not the preserved planned-only target handling
- `candidate`
  - the Microsoft runtime contract is coherent, but required approvals remain unsatisfied
- `readyForMicrosoftRuntimeProvider`
  - the request satisfies Microsoft runtime-provider contract requirements

`readyForMicrosoftRuntimeProvider` does not imply execution.

It only means the request is ready for a future Microsoft runtime provider implementation to consider.

## Supported And Deferred Target Handling

The current Microsoft runtime contract preserves:

- supported
  - `pbirReport/default`
- planned-only
  - `fabricDataApp/default`
- unsupported
  - `fabricApp/default`
  - any unknown target profile

This preserves the existing Microsoft adapter terminology and roadmap boundaries without creating execution behavior.

## Registry Model

The current Microsoft runtime layer registers a descriptive provider through the existing `RuntimeProviderRegistry`.

The registry currently supports:

- provider registration
- discovery by provider category and target profile
- capability lookup

Registration remains metadata-only.

The registry does not load providers, construct providers, or invoke providers.

## Current Trust Boundaries

The current Microsoft runtime contract does not:

- execute Microsoft Skills
- invoke Microsoft APIs
- invoke CLI commands
- invoke providers
- generate artifacts
- generate PBIR outputs
- generate Fabric App outputs
- generate Fabric Data App outputs
- deploy assets
- validate generated outputs
- automate Analyzer Workspace

The only new downstream consumer is the PBIR execution prototype boundary, which remains dry-run and mocked-execution only.

## Remaining Execution Gap

The current repo state still excludes:

- Microsoft runtime provider implementations
- Microsoft Skills execution
- CLI-backed Microsoft execution
- provider invocation
- PBIR generation
- Fabric App generation
- Fabric Data App generation
- artifact intake and quarantine
- deployment workflows
- Analyzer Workspace automation

The repo now includes a PBIR-only dry-run and mocked-execution boundary prototype, but it still excludes any live Microsoft runtime execution.
