# 2026-06-21 Design Package Microsoft Adapter Specification Phase 6

## Objective

- implement only the Phase 6 Microsoft Adapter Specification scope
- introduce `microsoft-adapter-specification/v1` as the descriptive Microsoft capability-mapping contract
- add deterministic Microsoft planning translation, compatibility classification, and readiness handling
- stop before Microsoft Skills execution, CLI execution, provider implementations, artifact generation, deployment, and Analyzer Workspace automation

## Started

- read `AGENTS.md`, repo memory files, the approved integration spec and plan, the current-state docs, and the existing Generation Request, Provider Planning, and Provider Adapter framework seams
- treated the approved design docs as the design gate and added failing xUnit coverage first for specification loading, validation, capability translation, compatibility categories, readiness transitions, and boundary protection

## Delivered

- added `service-dotnet/Services/Discovery/Models/MicrosoftAdapterSpecificationModels.cs`
- added `service-dotnet/Services/Discovery/MicrosoftAdapterSpecificationValidator.cs`
- added `service-dotnet/Services/Discovery/MicrosoftProviderPlanningTranslator.cs`
- added `service-dotnet/Services/Discovery/MicrosoftAdapterSpecificationService.cs`
- added `service-dotnet/tests/Discovery/MicrosoftAdapterSpecificationServiceTests.cs`
- added `docs/current-state/microsoft-adapter-specification-state.md`
- updated `docs/current-state/discovery-wizard-state.md`
- updated `docs/current-state/design-studio-state.md`
- updated `docs/current-state/provider-planning-framework-state.md`
- updated `docs/current-state/provider-adapter-framework-state.md`
- formalized `microsoft-adapter-specification/v1` with:
  - schema metadata
  - provider identity
  - supported target profiles
  - capability mappings
  - target-profile mappings
  - compatibility catalog
  - constraint catalog
  - review-requirements catalog
- added explicit Microsoft planning readiness states:
  - `unsupported`
  - `partiallySupported`
  - `supported`
  - `readyForMicrosoftAdapter`
- kept the Microsoft layer descriptive only, with no execution surface

## Validation

- focused gate:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~MicrosoftAdapterSpecificationServiceTests`
- required validation:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`

## Explicit Non-Implementation Boundary

- no Microsoft Skills execution
- no CLI execution
- no provider implementation
- no PBIR or Fabric artifact generation
- no deployment
- no Analyzer Workspace invocation or validation automation

## Next Recommended Step

- stop after Phase 6 as requested
- do not begin Microsoft provider adapters, CLI execution, artifact generation, deployment, or Analyzer Workspace automation unless a new goal explicitly opens the next phase
