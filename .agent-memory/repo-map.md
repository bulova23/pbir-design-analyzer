# Repo Map

## Purpose

- VS Code extension and .NET backend for PBIR design analysis and review-workspace generation.

## Primary Stack

- TypeScript
- React
- .NET 8
- Jest
- xUnit

## Important Paths

- `vscode-extension/src/`: extension host, commands, score payload shaping, review/export helpers
- `vscode-extension/webview-src/`: React webview sources
- `service-dotnet/Services/Pbir/`: backend scoring, PBIR parsing, semantic/story/governance heuristics
- `service-dotnet/RpcHost/`: packaged backend entrypoint
- `docs/`: product docs, roadmap notes, specs, and plans
- `.codex/skills/`: repo-local Codex skills

## Core Product Areas

- `vscode-extension/src/`: extension host, commands, score payload shaping, review/export helpers
- `vscode-extension/webview-src/analyzer-score/`: score-panel React workspace
- `service-dotnet/Services/Pbir/`: backend scoring, PBIR parsing, semantic/story/governance heuristics
- `service-dotnet/RpcHost/`: packaged backend entrypoint

## Important Docs

- `README.md`: product overview and release-level usage
- `vscode-extension/README.md`: extension install and workflow details
- `docs/HOW_TO_USE.md`: detailed review-workspace walkthrough
- `docs/CHANGELOG.md`: release notes
- `docs/ROADMAP.md`: post-`0.2.0` roadmap ordering and epic links
- `docs/superpowers/specs/` and `docs/superpowers/plans/`: implementation specs and plans
- `docs/current-state/`: authoritative current-state snapshots for planning, runtime, PBIR specification, and execution-boundary layers
  - includes `generation-provider-framework-state.md` for the provider-neutral contract seam after PBIR specifications
  - includes `generation-provider-execution-planning-framework-state.md` for the downstream provider-neutral execution-planning seam after generation-provider requests
  - includes `generation-manifest-framework-state.md` for the immutable provider-neutral execution package seam plus deterministic end-to-end pipeline verification after runtime-provider, Microsoft runtime, provider, and execution-planning readiness
  - includes `architecture-certification-state.md`, `architecture-readiness-report.md`, and `architecture-gap-analysis.md` for Phase 20 planning-architecture certification, readiness classification, and remaining implementation gap categories
  - includes `reference-generator-state.md` for the Phase 21 local deterministic Reference PBIR Generator prototype and its non-execution safety model
  - includes `pbir-intermediate-representation-state.md` for Phase 22 canonical pbir-ir/v1, pbir-serializer-request/v1, IR lifecycle, serializer boundary, and remaining serializer implementation gap
  - includes `pbir-preview-serializer-state.md` for Phase 23 pbir-preview-artifact/v1, pbir-preview-manifest/v1, local preview behavior, serializer safety boundary, and the remaining deployable PBIR serialization gap

## Important Memory Files

- `AGENTS.md`: repo-level operating contract
- `.agent-memory/current-focus.md`: current branch/release objective
- `.agent-memory/session-summaries.md`: compact durable milestone summary
- `.agent-memory/sessions/2026-05-31-0-2-0-release-summary.md`: final release snapshot
- `.agent-memory/sessions/2026-05-31-roadmap-next-epics-summary.md`: deferred-epic summary

## Key Commands

- `cd vscode-extension && npm run build`
- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- `cd vscode-extension && npm run package`

## Validation Entry Points

- `cd vscode-extension && npm run build`
- `cd vscode-extension && npm test`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## Startup Docs

- `AGENTS.md`
- `README.md`

## Major Constraints

- Keep fixture-based tests opt-in.
- Preserve packaged backend behavior.
- Keep scoring authoritative and presentation-only derivations separate.
- Keep normalized findings as the shared issue model.

## Release Constraints

- Keep scoring authoritative and presentation-only derivations separate.
- Keep normalized findings as the shared issue model.
- Keep workspace personas separate from reviewer-comment personas.
- Keep generated test-host artifacts and raw session clutter out of release merges.
