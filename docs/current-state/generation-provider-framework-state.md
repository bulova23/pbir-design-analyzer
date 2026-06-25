# Generation Provider Framework Current State

## Summary

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

The current Generation Provider Framework does not:

- generate PBIR artifacts
- invoke Microsoft Skills
- invoke Microsoft APIs
- invoke CLI commands
- deploy assets
- mutate reports
- automate Analyzer Workspace

## Remaining Generation Gap

The current repo state still excludes:

- downstream execution-provider implementation
- Microsoft Skills execution
- Copilot, Claude, OpenAI, local, or test generator execution
- Microsoft API invocation
- CLI-backed execution
- real PBIR generation
- artifact intake and quarantine
- deployment workflows
- Fabric App generation
- Fabric Data App generation
- Analyzer Workspace automation
