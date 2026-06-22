# 2026-06-21 Design Package Provider Planning Framework Phase 4

## Objective

- implement only Phase 4 Provider Planning Framework for Design Package → Microsoft Skills Integration
- add `execution-plan/v1` as the authoritative provider-planning artifact
- add provider-neutral planning creation, validation, readiness, and capability declarations
- stop before Microsoft Skills execution, CLI execution, provider adapters, artifact generation, deployment, and Analyzer Workspace automation

## Work Performed

- read `AGENTS.md`, repo memory files, the approved integration spec and plan, `docs/current-state/discovery-wizard-state.md`, `docs/current-state/design-studio-state.md`, and the Phase 3 Discovery generation-request framework code
- added test-first coverage in `service-dotnet/tests/Discovery/ExecutionPlanFrameworkServiceTests.cs` for:
  - valid execution-plan creation
  - deterministic and repeatable plan output
  - schema-version failure handling
  - missing-section failure handling
  - dependency integrity failure handling
  - capability inconsistency failure handling
  - unsupported-target failure handling
  - blocked-readiness enforcement
  - planning-only boundary protection
  - contract inventory drift protection
- updated `service-dotnet/tests/Discovery/GenerationRequestFrameworkServiceTests.cs` so Generation Request readiness continues to own prompt derivation only and no longer implies a separate provider-planning package
- added planning framework services:
  - `service-dotnet/Services/Discovery/ExecutionPlanBuilder.cs`
  - `service-dotnet/Services/Discovery/ExecutionPlanValidator.cs`
  - `service-dotnet/Services/Discovery/ExecutionPlanFrameworkService.cs`
- added planning contract models:
  - `service-dotnet/Services/Discovery/Models/ExecutionPlanModels.cs`
- added `service-dotnet/Services/Discovery/GenerationRequestTargetProfileCatalog.cs` so Generation Request and Execution Plan validation share one target-profile compatibility definition
- updated:
  - `service-dotnet/Services/Discovery/Models/GenerationRequestModels.cs`
  - `service-dotnet/Services/Discovery/GenerationRequestBuilder.cs`
  - `service-dotnet/Services/Discovery/GenerationRequestValidator.cs`
  - `service-dotnet/Services/Discovery/GenerationRequestFrameworkService.cs`
  - `docs/current-state/discovery-wizard-state.md`
  - `docs/current-state/design-studio-state.md`
- added `docs/current-state/provider-planning-framework-state.md` documenting:
  - `execution-plan/v1`
  - the provider capability model
  - planning framework architecture
  - remaining execution gaps
- preserved the authoritative boundary:
  - Design Package stays upstream and provider-neutral
  - Generation Request stays the authoritative execution contract
  - prompt segments stay derived from Generation Request
  - Execution Plan stays a derived planning-only artifact
- added explicit Execution Plan readiness states:
  - `draft`
  - `valid`
  - `blocked`
  - `readyForProviderAdapter`

## Generated Or Changed Files

- `service-dotnet/Services/Discovery/Models/ExecutionPlanModels.cs`
- `service-dotnet/Services/Discovery/ExecutionPlanBuilder.cs`
- `service-dotnet/Services/Discovery/ExecutionPlanValidator.cs`
- `service-dotnet/Services/Discovery/ExecutionPlanFrameworkService.cs`
- `service-dotnet/Services/Discovery/GenerationRequestTargetProfileCatalog.cs`
- `service-dotnet/Services/Discovery/Models/GenerationRequestModels.cs`
- `service-dotnet/Services/Discovery/GenerationRequestBuilder.cs`
- `service-dotnet/Services/Discovery/GenerationRequestValidator.cs`
- `service-dotnet/Services/Discovery/GenerationRequestFrameworkService.cs`
- `service-dotnet/tests/Discovery/ExecutionPlanFrameworkServiceTests.cs`
- `service-dotnet/tests/Discovery/GenerationRequestFrameworkServiceTests.cs`
- `docs/current-state/provider-planning-framework-state.md`
- `docs/current-state/discovery-wizard-state.md`
- `docs/current-state/design-studio-state.md`

## Validation

- focused gate:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~ExecutionPlanFrameworkServiceTests|FullyQualifiedName~GenerationRequestFrameworkServiceTests"`
- required validation:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`

## Outcome

- Provider Planning Framework now exists as a distinct provider-neutral layer that consumes valid Generation Requests and produces `execution-plan/v1`
- Execution Plans are deterministic, versioned, readiness-aware, and execution-free
- Generation Request remains the authoritative execution contract
- prompt segments remain derived from Generation Request rather than from Execution Plan
- no provider execution, Microsoft Skills invocation, adapter implementation, artifact generation, or deployment path was added

## Next Recommended Step

- stop after Phase 4 as requested
- do not begin Microsoft provider adapters, CLI execution, or artifact-generation work unless a new goal explicitly opens that scope
