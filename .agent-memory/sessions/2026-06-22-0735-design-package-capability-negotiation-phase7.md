# 2026-06-22 Design Package Capability Negotiation Phase 7

## Objective

- implement only the Phase 7 Capability Negotiation Framework scope
- introduce `capability-negotiation/v1` as the deterministic capability-resolution contract
- add provider-neutral requirement gathering, substitution handling, validation, and readiness evaluation across:
  - `generation-request/v1`
  - `execution-plan/v1`
  - `provider-adapter/v1`
  - `microsoft-adapter-specification/v1`
- stop before Microsoft Skills execution, CLI execution, provider implementations, artifact generation, deployment, and Analyzer Workspace automation

## Started

- read `AGENTS.md`, repo memory files, the approved integration spec and plan, and the current-state docs for provider planning, provider adapters, and Microsoft adapter specification
- confirmed the current worktree is already dirty from prior phases and will avoid reverting unrelated changes
- treated the existing design docs plus the new explicit goal scope as the design gate because the older plan still labels a different Phase 7
- started tracing the existing Generation Request, Execution Plan, Provider Adapter, and Microsoft specification seams to place capability negotiation downstream from all four layers without introducing execution behavior

## Delivered

- added `service-dotnet/Services/Discovery/Models/CapabilityNegotiationModels.cs`
- added `service-dotnet/Services/Discovery/CapabilityNegotiationValidator.cs`
- added `service-dotnet/Services/Discovery/CapabilityNegotiationService.cs`
- added `service-dotnet/tests/Discovery/CapabilityNegotiationServiceTests.cs`
- added `docs/current-state/capability-negotiation-framework-state.md`
- updated `docs/current-state/provider-adapter-framework-state.md`
- updated `docs/current-state/microsoft-adapter-specification-state.md`
- introduced `capability-negotiation/v1` with:
  - capability requirement classification
  - capability resolution modeling
  - explicit substitution modeling
  - deterministic resolution summary
  - negotiation readiness state
- added versioned substitution-catalog support with explicit deterministic rules for:
  - navigation generation from layout generation
  - page generation from layout generation
- kept the capability negotiation layer planning-only and provider-neutral with no execution surface

## Validation

- focused gate:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~CapabilityNegotiationServiceTests`
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

- stop after Phase 7 as requested
- do not begin execution-provider work, Microsoft Skills execution, CLI execution, artifact generation, deployment, or Analyzer Workspace automation unless a new goal explicitly opens the next phase
