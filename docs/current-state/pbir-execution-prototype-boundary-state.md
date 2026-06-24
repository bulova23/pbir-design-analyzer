# PBIR Execution Prototype Boundary Current State

## Summary

PBIR Execution Prototype Boundary is now implemented as the first gated post-runtime prototype seam downstream from Microsoft Runtime Provider Contract.

Its role is:

- define `pbir-execution-prototype/v1` as the authoritative PBIR execution-boundary state
- define `pbir-execution-request/v1` as the authoritative PBIR execution request envelope
- define `pbir-mock-execution-result/v1` as the authoritative deterministic mock-result artifact
- accept only `readyForMicrosoftRuntimeProvider` Microsoft runtime candidates
- validate PBIR-only target eligibility, approvals, dry-run constraints, provider category, and non-deployment constraints through `PbirExecutionSafetyGate`
- produce a deterministic dry-run summary from existing planning and runtime metadata
- optionally produce deterministic mock execution results from explicit fixture identifiers and explicit fixture output paths

Its dry-run summary remains a planning-facing preview and not a real generation path. The authoritative pre-generation contract now lives separately in `docs/current-state/pbir-generation-specification-framework-state.md`.

It is not a Microsoft Skills runtime, not a Microsoft API surface, not a CLI runner, not a provider invocation path, not a real PBIR generator, and not a deployment path.

## Current Product Position

PBIR Execution Prototype Boundary now sits after:

- Design Package Consumption Layer
- Generation Request Framework
- Execution Plan Framework
- Provider Adapter Framework
- Microsoft Adapter Specification
- Capability Negotiation Framework
- Microsoft Skills Catalog
- Microsoft Skill Provider Adapter
- Planning Orchestration Framework
- PBIR Generation Specification Framework
- Runtime Provider Framework
- Microsoft Runtime Provider Contract

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- Planning Frameworks prepare approved planning and runtime candidates
- Microsoft Runtime Provider Contract validates Microsoft runtime compatibility only
- PBIR Execution Prototype Boundary shapes a safe dry-run or mocked-execution request only
- Analyzer Workspace remains the downstream validation owner for any future generated artifacts

## What Exists Today

The implemented PBIR prototype layer currently includes:

- `pbir-execution-prototype/v1`
- `pbir-execution-request/v1`
- `pbir-mock-execution-result/v1`
- `PbirExecutionPrototypeBoundaryService`
- `PbirExecutionSafetyGate`
- explicit execution modes:
  - `dryRun`
  - `mockedExecution`
- boundary tests proving the layer rejects live execution, deployment, Fabric App, Fabric Data App, unsupported providers, missing approvals, and non-dry-run requests outside mocked execution

## PBIR Execution Request Envelope

The authoritative PBIR request artifact is `pbir-execution-request/v1`.

Its required sections are:

- request metadata
- planning outcome reference
- execution candidate reference
- Microsoft runtime context reference
- selected skill/provider metadata
- target profile
- PBIR-specific constraints
- approval state
- execution mode
- dry-run flag

The request is derived from:

- `planning-outcome/v1`
- `microsoft-runtime-request/v1`
- `microsoft-runtime-context/v1`
- existing Microsoft skill and provider-selection metadata already present in planning

It remains boundary-only and non-executing.

## Safety Gate Model

`PbirExecutionSafetyGate` currently validates:

- target profile is `pbirReport/default`
- runtime readiness is `readyForMicrosoftRuntimeProvider`
- the Microsoft runtime candidate was accepted as an execution candidate
- required design and generation approvals are present
- provider category remains Microsoft-only
- live provider invocation is not requested
- deployment is not requested
- non-dry-run execution is allowed only for `mockedExecution`
- mocked execution requires a deterministic fixture id

Validation fails closed.

## Dry-Run Behavior

The default mode is `dryRun`.

Dry-run currently produces:

- deterministic planned page summary
- deterministic planned visual summary
- deterministic semantic-binding summary
- explicit safety constraints
- explicit warnings that the boundary remains advisory-only and non-generative

Repeated dry-runs over the same planning/runtime inputs produce identical summaries.

## Mocked Execution Behavior

`mockedExecution` is allowed only behind the PBIR safety gate and only when an explicit mock fixture id is supplied.

Mocked execution currently produces:

- deterministic result metadata
- request reference back to `pbir-execution-request/v1`
- planned page, visual, and semantic-binding summaries copied from the dry-run plan
- constraint and warning carry-forward
- `generatedArtifactRefs`

`generatedArtifactRefs` remains empty unless explicit fixture output paths are supplied.

No real artifact generation occurs.

## Current Trust Boundaries

The current PBIR execution prototype boundary does not:

- execute Microsoft Skills
- invoke Microsoft APIs
- invoke CLI commands
- invoke providers
- mutate PBIR projects
- generate real PBIR artifacts
- generate Fabric App artifacts
- generate Fabric Data App artifacts
- deploy assets
- automate Analyzer Workspace

## Remaining Live Execution Gap

The current repo state still excludes:

- live Microsoft Skills execution
- Microsoft API invocation
- CLI-backed execution
- provider invocation
- real PBIR artifact generation
- artifact intake and quarantine
- deployment workflows
- Fabric App generation
- Fabric Data App generation
- Analyzer Workspace automation
