# Generation Provider Framework Current State

## Phase 36 Concrete Local Provider

Repository Phase 36 adds the first concrete backend-only local generation provider in `LocalPbirGenerationProviderService`. It consumes `local-pbir-generation-request/v1` for exactly one page, one `card` visual, and one direct measure binding, maps that request into the existing Phase 29 IR and deployable serializer inputs, persists through the existing Phase 31 orchestration, and immediately verifies the result through `PbirScoringService`.

This does not change the provider-neutral framework into a runtime registry and does not activate the Phase 35 provider execution architecture. It is a deliberately narrow local product provider with no RPC, VS Code, Microsoft Skills, API, CLI, hosted, Windows, Desktop, or semantic-model-generation surface. Phase 31 remains the only filesystem mutation authority, and Analyzer scoring remains authoritative for round-trip results.

## Summary

Phase 35A adds a stricter downstream governed contract package documented in `phase35a-contract-only-provider-foundation-state.md`. The existing Generation Provider Framework remains provider-neutral planning metadata; Phase 35A does not promote its `readyForGenerationProvider` state to runtime availability.

Generation Provider Framework is now implemented as the provider-neutral contract layer downstream from PBIR Generation Specification Framework and upstream from any future artifact generator.

Its role is:

- define `generation-provider/v1` as the framework state contract
- define `generation-provider-definition/v1` as the metadata-only provider definition
- define `generation-provider-request/v1` as the provider-neutral generation request
- define `generation-provider-context/v1` as the provider-neutral lineage and readiness context
- define `generation-provider-result/v1` as the descriptive contract result
- map `pbir-generation-specification/v1` into provider-neutral request requirements without introducing provider-specific behavior
- validate specification completeness, provider compatibility, artifact-type support, target-profile support, and schema compatibility
- classify readiness for future generation-provider consumption without executing anything
- register and discover descriptive generation providers through a metadata-only registry

The downstream execution-planning seam is documented separately in `docs/current-state/generation-provider-execution-planning-framework-state.md` so the request contract can remain distinct from the future execution-plan contract.

It is not a PBIR generator, not a Microsoft Skills runtime, not a Microsoft API surface, not a CLI runner, not a deployment path, and not a report-mutation workflow.

## Current Product Position

Generation Provider Framework now sits after:

- Design Package Consumption Layer
- Generation Request Framework
- Planning Orchestration Framework
- PBIR Generation Specification Framework

It sits before:

- the new Generation Provider Execution Planning Framework
- any future Microsoft Skills generation provider
- any future Copilot, Claude, OpenAI, local, or test generator
- any future provider execution runtime
- any future artifact intake or deployment workflow

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- planning and specification layers normalize generation intent
- Generation Provider Framework publishes the provider-neutral request a future generator may consume
- future providers remain downstream consumers only
- Analyzer Workspace remains the downstream validation owner for any future generated artifact

## What Exists Today

The implemented Phase 16 layer currently includes:

- `generation-provider/v1`
- `generation-provider-definition/v1`
- `generation-provider-request/v1`
- `generation-provider-context/v1`
- `generation-provider-result/v1`
- `GenerationProviderFrameworkService`
- `GenerationProviderRegistry`
- `GenerationProviderValidator`
- `GenerationProviderReadinessService`
- downstream deterministic consumption by Generation Provider Execution Planning Framework
- explicit readiness states:
  - `unsupported`
  - `blocked`
  - `candidate`
  - `readyForGenerationProvider`

## Generation Provider Definition

The authoritative provider definition is `generation-provider-definition/v1`.

Each provider definition currently requires:

- provider id
- provider name
- provider version
- supported artifact types
- supported target profiles
- supported generation modes
- status

The registry also tracks supported capabilities for lookup, but remains metadata-only.

## Generation Provider Request

The authoritative request artifact is `generation-provider-request/v1`.

Its required sections are:

- metadata
  - request id
- references
  - planning outcome reference
  - execution candidate reference
  - PBIR specification reference
- requirements
  - capability requirements
  - provider requirements
  - constraints

The current request mapping is deterministic from `pbir-generation-specification/v1`.

It currently preserves:

- artifact type
- target profile id
- required generation capabilities
- planning outcome identity
- PBIR specification identity
- explicit metadata-only constraints

## Generation Provider Context

The authoritative context artifact is `generation-provider-context/v1`.

Its required sections are:

- provider metadata
- specification metadata
- planning metadata
- readiness metadata

Context is lineage-only and readiness-only.

It does not track execution state.

## Generation Provider Result

The authoritative result artifact is `generation-provider-result/v1`.

Allowed result statuses are:

- `accepted`
- `rejected`
- `unsupported`
- `blocked`

The result remains descriptive only.

It does not contain generated artifacts, generated files, or deployed outputs.

## Registry Model

`GenerationProviderRegistry` currently supports:

- registration
- provider discovery
- capability lookup
- artifact-type lookup
- target-profile lookup

Registration remains metadata-only.

The registry does not load providers, construct providers, or invoke providers.

## Validation Model

`GenerationProviderValidator` currently validates:

- generation-provider request shape
- PBIR specification completeness
- provider-definition schema compatibility
- request schema compatibility
- provider artifact-type support
- provider target-profile support
- provider generation-mode support
- metadata-only boundary constraints

Validation fails closed.

## Readiness Model

`GenerationProviderReadinessService` currently determines one of:

- `unsupported`
  - the provider or request is incompatible with the requested contract
- `blocked`
  - the underlying PBIR specification or provider request is incomplete or violates boundaries
- `candidate`
  - the contract is coherent but the provider remains planned or deprecated
- `readyForGenerationProvider`
  - a future provider could consume the provider-neutral request

`readyForGenerationProvider` does not imply generation occurred.

It only means the provider-neutral contract is complete enough for a future generator to consume.

## Current Trust Boundaries

The provider-neutral Generation Provider Framework does not:

- generate PBIR artifacts
- invoke Microsoft Skills
- invoke Microsoft APIs
- invoke CLI commands
- deploy assets
- mutate reports
- automate Analyzer Workspace

## Remaining Generation Gap

The current repo state still excludes:

- provider-neutral runtime registry activation; the Phase 36 local provider is an explicit backend service, not a registry-loaded provider

- downstream execution-provider implementation
- Microsoft Skills execution
- Copilot, Claude, OpenAI, local, or test generator execution
- Microsoft API invocation
- CLI-backed execution
- broad or general-purpose PBIR generation
- artifact intake and quarantine
- deployment workflows
- Fabric App generation
- Fabric Data App generation
- Analyzer Workspace automation
## Phase 35B Runtime Boundary

Phase 35B is an orchestration proof only. It consumes the projected Phase 35A request and coordinates a typed offline adapter seam without report generation, PBIR materialization, external execution, or publication. Resolution is exact and fail-closed; there is no fallback, probing, or heuristic selection.

## Phase 35C Assurance Boundary

Phase 35C adds a provider trust and activation admission boundary downstream from Phase 35B. Trust requires explicit identity, attestation, expiration, and policy-version binding; execution also requires sandbox policy approval, opaque credential grants, finite resources, replay protection, audit availability, conformance, output-corpus approval, and artifact-scanner availability. Phase35E adds an explicit OS-boundary seam and capability report, but the observed macOS Seatbelt mechanism fails the safe capability probe and therefore denies admission. Phase35F evaluates App Sandbox, Hardened Runtime, signed helpers/XPC, Virtualization.framework, container runtimes, and remote execution and selects no local macOS mechanism until every required capability is `Enforced`. The `Phase35CActivationGate` never invokes a provider. The production catalog remains non-executable; Phase 36 is intentionally outside that catalog and does not alter its admission conclusion.

## Phase 35D Certification Boundary

Phase 35D certifies an exact package identity and signed attestation for controlled pre-production eligibility only. Its conformance runner calls adapter declaration/validation methods and Phase 35C evidence evaluators but never calls adapter execution. Certification records bind provider/version/implementation/package/profile/evidence/policy identity, expiration, and lifecycle status. The Phase 35D activation binding can return `PreProductionEligible`; it cannot establish production eligibility. Phase35E's OS process boundary remains fail-closed because the available macOS custom Seatbelt mechanism is not safely enforceable on the observed runtime. Phase35G records `remote-controlled-execution/v1` as the future containment boundary, with Windows as the first worker profile for likely Desktop-dependent behavior. Phase35I now owns the portable Windows worker/runner admission and one native containment boundary, but Windows integration evidence remains unexecuted; the remote service, credentials, scanner, replay reconciliation, and executable adapter remain unimplemented.
