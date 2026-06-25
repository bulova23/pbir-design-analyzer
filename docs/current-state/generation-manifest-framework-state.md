# Generation Manifest Framework Current State

## Summary

Generation Manifest Framework is now implemented as the immutable provider-neutral manifest layer downstream from Generation Provider Execution Planning and upstream from any future generator.

Its role is:

- define `generation-manifest/v1` as the canonical execution handoff document
- compose upstream planning metadata into one deterministic immutable manifest
- preserve references to Design Package, Generation Request, Execution Plan, Planning Outcome, runtime provider, generation provider request, generation-provider execution plan, and PBIR generation specification
- summarize negotiated capabilities, provider capabilities, selected provider, and selected skills without invoking any runtime
- validate completeness, lineage integrity, readiness consistency, provider compatibility, and schema compatibility
- expose manifest readiness for future generator consumption only

It is not a PBIR generator, not a Microsoft Skills runtime, not a provider invocation path, not a Microsoft API surface, not a CLI runner, not a deployment path, and not a report-mutation workflow.

## Current Product Position

Generation Manifest Framework now sits after:

- Design Package Consumption Layer
- Generation Request Framework
- Planning Orchestration Framework
- PBIR Generation Specification Framework
- Generation Provider Framework
- Generation Provider Execution Planning Framework
- Microsoft Runtime Provider Contract

It sits before:

- any future PBIR generator
- any future Microsoft Skills execution provider
- any future Copilot, Claude, OpenAI, local, or test generation runtime
- any future provider invocation
- any future API or CLI execution path
- any future artifact generation or deployment workflow

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- planning, specification, provider, and runtime layers normalize generation metadata
- Generation Manifest Framework creates the immutable provider-neutral handoff package
- future generators remain downstream consumers only
- Analyzer Workspace remains the downstream validation owner for any future generated artifact

## What Exists Today

The implemented Phase 18 layer currently includes:

- `generation-manifest/v1`
- `GenerationManifestService`
- `GenerationManifestValidator`
- `GenerationManifestReadinessService`
- deterministic manifest composition
- explicit readiness states:
  - `incomplete`
  - `blocked`
  - `readyForGenerator`

## Manifest Contract

The authoritative manifest artifact is `generation-manifest/v1`.

Its required sections are:

- metadata
  - manifest id
  - schema version
  - created UTC
- references
  - design package reference
  - generation request reference
  - execution plan reference
  - planning outcome reference
  - runtime provider reference
  - generation provider request reference
  - generation-provider execution plan reference
- generation specification
  - PBIR generation specification reference
- capability summary
  - negotiated capabilities
  - provider capabilities
  - selected provider
  - selected skills
- execution constraints
  - dry-run only
  - deployment allowed
  - provider invocation allowed
  - API invocation allowed
  - CLI invocation allowed
- approval summary
  - design approval
  - planning approval
  - runtime readiness
  - generation readiness
- lineage
  - upstream lineage
  - immutable references

The manifest does not replace the upstream artifacts it references.

It packages them into one immutable handoff document.

## Manifest Lifecycle

The current manifest lifecycle is:

1. planning artifacts are created upstream
2. provider-neutral generation planning is completed
3. runtime provider metadata is resolved
4. `GenerationManifestService` composes the deterministic manifest
5. `GenerationManifestValidator` verifies completeness and integrity
6. `GenerationManifestReadinessService` classifies the manifest for future generator consumption

No generation occurs in this lifecycle.

## Lineage Model

The current lineage model preserves:

- complete upstream planning lineage from `planning-outcome/v1`
- downstream metadata lineage for:
  - `pbir-generation-specification/v1`
  - `generation-provider-request/v1`
  - `generation-provider-execution-plan/v1`
  - `microsoft-runtime-request/v1`
- immutable references as a deterministic ordered reference set

This preserves full handoff traceability without adding any mutation or execution authority.

## Validation Model

`GenerationManifestValidator` currently validates:

- manifest schema compatibility
- planning-outcome schema compatibility
- PBIR generation specification schema compatibility
- generation provider framework, request, definition, and execution-plan schema compatibility
- Microsoft runtime provider and runtime-request schema compatibility
- reference integrity across all required upstream artifacts
- capability-summary compatibility with planning, generation provider, and runtime skill state
- readiness consistency across planning, runtime, and generation execution-planning states
- complete immutable-reference coverage
- deterministic complete lineage preservation
- non-execution boundary constraints

Validation fails closed.

## Readiness Model

`GenerationManifestReadinessService` currently determines one of:

- `incomplete`
  - required sections or fields are missing
- `blocked`
  - references, schema versions, lineage integrity, readiness consistency, provider compatibility, or trust boundaries are invalid
- `readyForGenerator`
  - the manifest is complete, internally consistent, and preserves all required metadata for a future generator

`readyForGenerator` does not imply generation occurred.

It only means a future generator has the required metadata handoff package.

## Determinism Model

The current manifest implementation guarantees:

- identical inputs plus identical `createdUtc` produce identical manifests
- stable property ordering from the record contract
- stable capability ordering by preserving upstream deterministic order
- stable immutable-reference ordering
- stable upstream-lineage ordering

This keeps manifest serialization deterministic without inventing any execution side effects.

## Current Trust Boundaries

The current framework does not:

- generate PBIR artifacts
- invoke Microsoft Skills
- invoke providers
- call Microsoft APIs
- invoke CLI commands
- deploy assets
- mutate reports
- automate Analyzer Workspace

## Remaining Execution Gap

The current repo state still excludes:

- PBIR generation
- Microsoft Skills execution
- provider invocation
- Microsoft API invocation
- CLI-backed execution
- real artifact generation
- artifact intake and quarantine
- deployment workflows
- Fabric App generation
- Fabric Data App generation
- Analyzer Workspace automation
