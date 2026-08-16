# 2026-06-21 Design Package Generation Request Framework Phase 3

## Objective

- implement only Phase 3 Generation Request Framework for Design Package → Microsoft Skills Integration
- add provider-neutral request creation, validation, readiness, and provider-planning preparation
- stop before Microsoft Skills execution, CLI execution, provider adapters, artifact generation, analyzer handoff automation, and all later phases

## Work Performed

- read `AGENTS.md`, repo memory files, the approved Phase 3 spec and plan, `docs/current-state/discovery-wizard-state.md`, `docs/current-state/design-studio-state.md`, and the existing Design Package consumption plus Generation Request code
- added test-first coverage in `service-dotnet/tests/Discovery/GenerationRequestFrameworkServiceTests.cs` for:
  - valid framework request creation
  - blocked invalid requests
  - readiness transitions
  - prompt-segment determinism and repeatability
  - provider-neutral boundary protection
  - contract inventory drift protection after framework additions
- added framework services:
  - `service-dotnet/Services/Discovery/GenerationRequestBuilder.cs`
  - `service-dotnet/Services/Discovery/GenerationRequestValidator.cs`
  - `service-dotnet/Services/Discovery/GenerationRequestPromptSegmentOrchestrator.cs`
  - `service-dotnet/Services/Discovery/GenerationRequestFrameworkService.cs`
- updated:
  - `service-dotnet/Services/Discovery/Models/GenerationRequestModels.cs`
  - `service-dotnet/Services/Discovery/Models/DesignPackageConsumptionModels.cs`
  - `service-dotnet/Services/Discovery/DesignPackageConsumptionService.cs`
  - `service-dotnet/Services/Discovery/GenerationRequestService.cs`
  - `docs/current-state/discovery-wizard-state.md`
- preserved the Design Package as the upstream provider-neutral artifact and kept Generation Request as the authoritative execution-planning contract
- preserved prompt segments as derived deterministic artifacts only
- added explicit request readiness states:
  - `draft`
  - `valid`
  - `blocked`
  - `readyForProviderPlanning`
- added explicit target-profile metadata with compatibility checks against the source experience type
- kept provenance and review policy locked to provider-neutral, review-gated semantics

## Generated Or Changed Files

- `service-dotnet/Services/Discovery/GenerationRequestBuilder.cs`
- `service-dotnet/Services/Discovery/GenerationRequestValidator.cs`
- `service-dotnet/Services/Discovery/GenerationRequestPromptSegmentOrchestrator.cs`
- `service-dotnet/Services/Discovery/GenerationRequestFrameworkService.cs`
- `service-dotnet/Services/Discovery/Models/GenerationRequestModels.cs`
- `service-dotnet/Services/Discovery/Models/DesignPackageConsumptionModels.cs`
- `service-dotnet/Services/Discovery/DesignPackageConsumptionService.cs`
- `service-dotnet/Services/Discovery/GenerationRequestService.cs`
- `service-dotnet/tests/Discovery/GenerationRequestFrameworkServiceTests.cs`
- `docs/current-state/discovery-wizard-state.md`

## Validation

- focused gates:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~GenerationRequestFrameworkServiceTests`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~GenerationRequestServiceTests`
- required validation:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`

## Outcome

- Generation Request Framework now exists as a distinct provider-neutral layer above Design Package consumption and below any future provider adapter
- the framework can create valid `generation-request/v1` requests from consumed Design Package input
- readiness is explicit and does not imply design approval, execution, generation success, or Analyzer validation
- provider-planning preparation now packages the authoritative Generation Request together with deterministic derived prompt segments for future adapters
- execution and provider adapters remain intentionally unimplemented

## Next Recommended Step

- stop after Phase 3 as requested
- do not begin Microsoft adapter, CLI, or artifact-generation work unless a new goal explicitly opens Phase 4
