# 2026-06-25 Design Package Microsoft Skills Integration Phase 17

## Objective

- implement only the Phase 17 Generation Provider Execution Planning Framework scope
- add `generation-provider-execution-plan/v1`
- create the execution-planning layer that consumes `generation-provider-request/v1` and prepares provider-neutral execution plans without invoking any provider
- add execution-plan validation and readiness evaluation
- stop before PBIR generation, Microsoft Skills execution, provider invocation, API invocation, CLI invocation, deployment, Fabric App generation, and Fabric Data App generation

## Started

- read `AGENTS.md`, repo memory files, failure-avoidance notes, the approved integration spec and plan, and the adjacent current-state documents
- confirmed the new seam should sit downstream from `generation-provider-request/v1` and upstream from any future provider runtime or Microsoft Skills implementation
- identified the implementation pattern to follow from adjacent phases:
  - versioned contract file
  - planning service
  - validator
  - readiness service
  - focused xUnit coverage
  - current-state documentation
- beginning with failing tests for deterministic execution-plan creation, validation, readiness, and non-execution boundary protection

## Delivered

- added:
  - `service-dotnet/Services/Discovery/Models/GenerationProviderExecutionPlanningModels.cs`
  - `service-dotnet/Services/Discovery/GenerationProviderExecutionPlanningService.cs`
  - `service-dotnet/Services/Discovery/GenerationProviderExecutionPlanValidator.cs`
  - `service-dotnet/Services/Discovery/GenerationProviderExecutionReadinessService.cs`
  - `service-dotnet/tests/Discovery/GenerationProviderExecutionPlanningServiceTests.cs`
  - `docs/current-state/generation-provider-execution-planning-framework-state.md`
- implemented:
  - `generation-provider-execution-plan/v1`
  - deterministic provider-neutral execution-stage sequencing
  - validation for reference integrity, stage ordering, readiness compatibility, provider compatibility, and schema compatibility
  - readiness states:
    - `blocked`
    - `partiallyPrepared`
    - `prepared`
    - `readyForExecutionProvider`
- updated:
  - `docs/current-state/generation-provider-framework-state.md`
  - `docs/current-state/pbir-generation-specification-framework-state.md`
  - `.agent-memory/repo-map.md`

## Validation

- focused gate:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~GenerationProviderExecutionPlanningServiceTests`
- required validation:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`

## Notes

- kept Phase 17 isolated from the older generic `execution-plan/v1` seam to avoid mutating earlier planning abstractions
- preserved strict non-execution boundaries:
  - no PBIR generation
  - no Microsoft Skills execution
  - no provider invocation
  - no API invocation
  - no CLI invocation
  - no deployment
