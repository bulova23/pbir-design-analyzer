# Runtime Provider Framework Current State

## Summary

Phase 35A is separate from this pre-execution abstraction. Its `Unavailable` conclusion is authoritative for runtime generation and does not infer readiness from this framework's `readyForRuntimeProvider` planning state.

Runtime Provider Framework is now implemented as the contract-only pre-execution abstraction layer downstream from Planning Orchestration Framework and Execution Provider Contract Framework.

Its role is:

- consume an approved or blocked planning outcome together with the upstream execution-provider contract context
- define runtime-provider/v1 as the execution-candidate contract seam
- define runtime-provider-request/v1
- define runtime-provider-context/v1
- define runtime-provider-result/v1
- define the pre-execution lifecycle from planning outcome to execution candidate
- evaluate runtime readiness without invoking any provider

It is not a runtime implementation, not a Microsoft Skills execution path, not a provider invocation path, not a CLI runner, and not an artifact-generation surface.

The new downstream PBIR prototype seam is documented separately in `docs/current-state/pbir-execution-prototype-boundary-state.md` so the generic runtime-provider abstraction can remain provider-neutral.

## Current Product Position

Runtime Provider Framework now sits after planning orchestration and before any future execution-provider implementation.

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- Planning Frameworks prepare execution candidates
- Runtime Provider Framework defines runtime abstractions only
- Analyzer Workspace remains the downstream validation owner for future generated artifacts

## What Exists Today

The implemented runtime-provider layer currently includes:

- runtime-provider/v1
- runtime-provider-request/v1
- runtime-provider-context/v1
- runtime-provider-result/v1
- Runtime Execution Candidate model
- IRuntimeProvider contract
- RuntimeProviderValidator
- RuntimeReadinessService
- RuntimeProviderRegistry
- RuntimeProviderAbstractionFrameworkService
- explicit readiness states:
  - invalid
  - blocked
  - unsupported
  - candidate
  - readyForRuntimeProvider
- boundary tests proving the layer remains pre-execution only

## Runtime Lifecycle Model

The current deterministic lifecycle is:

Planning Outcome  
↓  
Runtime Provider Request  
↓  
Runtime Provider Validation  
↓  
Runtime Provider Readiness  
↓  
Execution Candidate

Execution Candidate does not execute anything.

Execution Candidate only means the planning output is represented in a runtime-provider contract shape that may be used by a future runtime implementation.

## Runtime Provider Request Contract

The current authoritative runtime request artifact is runtime-provider-request/v1.

Its required sections are:

- schema version
- request id
- planning outcome reference
- execution provider reference
- execution plan reference
- capability resolution reference
- source contract versions
- approval state
- execution constraints

## Runtime Provider Context Contract

The current authoritative runtime context artifact is runtime-provider-context/v1.

Its required sections are:

- schema version
- context id
- execution lineage
- planning lineage
- approval lineage
- target profile
- provider category

Context is lineage-only and scope-only.

It does not track runtime execution state.

## Runtime Provider Result Contract

The current authoritative runtime result artifact is runtime-provider-result/v1.

Its result statuses are:

- accepted
- rejected
- unsupported
- blocked
- validationFailed

These are pre-execution results only.

No generated artifacts or runtime outputs exist in this phase.

## Runtime Readiness Model

RuntimeReadinessService currently determines one of:

- invalid
  - the runtime request or context is structurally incomplete
- blocked
  - the upstream planning outcome is blocked or contract lineage/version integrity fails
- unsupported
  - no registered runtime provider metadata satisfies the request contract
- candidate
  - the runtime request is valid and supported, but remains a pre-execution candidate rather than a fully ready runtime handoff
- readyForRuntimeProvider
  - the runtime request satisfies all runtime-provider contract requirements

readyForRuntimeProvider does not imply execution.

It only means the request is ready for a future runtime-provider implementation to consider.

## Runtime Validation Model

RuntimeProviderValidator currently validates:

- references
- lineage
- approval state inheritance
- capability resolution
- execution constraints
- schema versions

Validation fails closed.

## Runtime Registry Model

RuntimeProviderRegistry currently provides:

- provider registration
- provider discovery by category and target profile
- provider capability lookup

The registry stores registration metadata only.

It does not load providers, construct providers, or invoke providers.

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

## Remaining Execution Gap

The current repo state still excludes:

- a runtime generation provider; **No runtime generation provider is available**

- runtime provider implementations
- Microsoft runtime provider implementations
- Microsoft Skills execution
- provider invocation
- CLI-backed execution
- artifact generation
- artifact intake and quarantine
- deployment workflows
- Analyzer Workspace automation

Microsoft-specific runtime compatibility is now defined separately in `docs/current-state/microsoft-runtime-provider-contract-state.md` so the generic runtime-provider abstraction can remain provider-neutral.

Microsoft skill-catalog metadata is now defined separately in `docs/current-state/microsoft-skills-catalog-state.md` so the generic runtime-provider abstraction can remain provider-neutral and execution-free.

Microsoft skill-provider mapping metadata is now defined separately in `docs/current-state/microsoft-skill-provider-adapter-state.md` so the generic runtime-provider abstraction can remain provider-neutral and execution-free.

PBIR execution-boundary behavior is now defined separately in `docs/current-state/pbir-execution-prototype-boundary-state.md` so the runtime-provider abstraction can remain provider-neutral and contract-first.
## Phase 35B Runtime Composition

Phase 35B adds an offline-only composition root beside the Phase 35A contract package. It coordinates exact provider resolution, authorization and readiness gates, immutable sessions, explicit lifecycle transitions, fixed validation stages, artifact intake, timeout/cancellation classification, in-memory audit projection, and structured diagnostics. The normal catalog remains unavailable and contains no executable adapter. Controlled fake adapters exist only inside tests.

The runtime does not invoke Desktop, PBIR generation/materialization, processes, shell, HTTP/network, MCP, Skills, credentials, publication, or mutation. Phase 35C must address sandbox/trust, credential isolation, durable audit, artifact scanning, conformance tests, and output validation before provider activation.
