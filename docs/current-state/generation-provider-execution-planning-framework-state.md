# Generation Provider Execution Planning Framework Current State

## Summary

Generation Provider Execution Planning Framework is now implemented as the provider-neutral execution-planning layer downstream from Generation Provider Framework and upstream from any future provider runtime.

Its role is:

- define `generation-provider-execution-plan/v1` as the authoritative execution-planning contract
- consume `generation-provider-request/v1`
- preserve references to `pbir-generation-specification/v1` and `planning-outcome/v1`
- prepare deterministic provider-neutral execution stages without invoking any provider
- validate reference integrity, stage ordering, readiness compatibility, provider compatibility, and schema compatibility
- evaluate execution-plan readiness before any future execution provider handoff

It is not a PBIR generator, not a Microsoft Skills runtime, not a provider invocation path, not a Microsoft API surface, not a CLI runner, not a deployment path, and not a report-mutation workflow.

## Current Product Position

Generation Provider Execution Planning Framework now sits after:

- Design Package Consumption Layer
- Generation Request Framework
- Planning Orchestration Framework
- PBIR Generation Specification Framework
- Generation Provider Framework

It sits before:

- any future Microsoft Skills execution provider
- any future Copilot, Claude, OpenAI, local, or test generation runtime
- any future provider invocation
- any future API or CLI execution path
- any future artifact generation or deployment workflow

Its ownership is:

- Discovery Wizard recommends
- Design Studio designs and approves
- planning and specification layers normalize generation intent
- Generation Provider Framework publishes the provider-neutral provider request
- Generation Provider Execution Planning Framework prepares the provider-neutral execution plan
- future providers remain downstream consumers only
- Analyzer Workspace remains the downstream validation owner for any future generated artifact

## What Exists Today

The implemented Phase 17 layer currently includes:

- `generation-provider-execution-plan/v1`
- `GenerationProviderExecutionPlanningService`
- `GenerationProviderExecutionPlanValidator`
- `GenerationProviderExecutionReadinessService`
- deterministic execution-stage planning
- explicit readiness states:
  - `blocked`
  - `partiallyPrepared`
  - `prepared`
  - `readyForExecutionProvider`

## Execution Plan Contract

The authoritative execution-planning artifact is `generation-provider-execution-plan/v1`.

Its required sections are:

- metadata
  - execution plan id
  - schema version
- references
  - generation provider request reference
  - PBIR generation specification reference
  - planning outcome reference
- execution stages
- execution constraints
- execution dependencies

The plan is derived from `generation-provider-request/v1`.

It does not replace the upstream request contract.

## Execution Stage Model

The current deterministic stage sequence is:

1. specification validation
2. provider capability validation
3. execution preparation
4. provider handoff preparation

This sequence is provider-neutral and deterministic.

It does not contain provider-specific execution logic.

## Execution Constraints

The current execution-planning contract keeps these constraints explicit:

- dry-run only
- mock execution permitted
- deployment prohibited
- provider invocation prohibited
- API invocation prohibited
- CLI invocation prohibited
- report mutation prohibited

These constraints preserve the Phase 17 non-execution trust boundary.

## Execution Dependency Model

The current execution dependencies track:

- required approvals
- provider readiness
- runtime readiness
- specification completeness

`readyForExecutionProvider` means the execution plan is complete enough for a future generation provider.

It does not mean execution occurred.

## Validation Model

`GenerationProviderExecutionPlanValidator` currently validates:

- execution-plan schema compatibility
- generation-provider request schema compatibility
- provider-definition schema compatibility
- planning-outcome schema compatibility
- reference integrity across request, specification, and planning outcome
- deterministic execution-stage ordering
- provider compatibility with required capabilities, target profile, artifact type, and mode
- readiness compatibility with provider, specification, and planning outcome readiness
- non-execution boundary constraints

Validation fails closed.

## Readiness Model

`GenerationProviderExecutionReadinessService` currently determines one of:

- `blocked`
  - required sections, fields, references, schema versions, stage ordering, or trust boundaries are invalid
- `partiallyPrepared`
  - the plan shape is valid but provider or readiness compatibility is still incomplete
- `prepared`
  - the plan is structurally valid and compatibility checks pass
- `readyForExecutionProvider`
  - the plan is prepared and its tracked approvals and upstream readiness dependencies are satisfied

`readyForExecutionProvider` does not imply generation occurred.

It only means the provider-neutral execution plan is complete enough for a future execution provider to consume.

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

- Microsoft Skills execution
- Copilot, Claude, OpenAI, local, or test provider execution
- provider invocation
- Microsoft API invocation
- CLI-backed execution
- real PBIR generation
- artifact intake and quarantine
- deployment workflows
- Fabric App generation
- Fabric Data App generation
- Analyzer Workspace automation
