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
  - includes the Phase 33 stateless local PBIR RPC adapter and its strict contracts/validation over Core Phase 31 orchestration
- `service-dotnet/Services/Discovery/Phase35A/`: Phase 35A contract-only provider governance; pure models, projection, validation, readiness, lifecycle, hashing, and metadata catalog only
- `service-dotnet/Services/Discovery/Phase35B/`: Phase 35B offline-only runtime composition; exact provider resolution, gates, immutable sessions, lifecycle, validation, artifact intake, timeout/cancellation, audit, and diagnostics
- `service-dotnet/Services/Discovery/Phase35C/`: Phase 35C offline assurance boundary; trust/attestation, sandbox policy, opaque credential boundary, replay/resource policy, durable hash-chain audit abstraction, artifact safety, output corpus, conformance, and activation gate
- `service-dotnet/Services/Discovery/Phase35D/`: Phase 35D offline provider certification; deterministic package identity, signed attestation verification, certification evidence/lifecycle, exact activation binding, non-executing conformance, and bounded protected audit/replay persistence
- `service-dotnet/Services/Discovery/Phase35E/`: Phase 35E macOS Seatbelt sandbox admission, exact executable identity binding, capability/policy binding, bounded runner, lifecycle, and evidence projection; production catalog remains disabled
- `service-dotnet/Services/Discovery/Phase35F/`: Phase 35F per-control macOS containment decision/evidence selector; no local mechanism selected and no process creation
- `service-dotnet/Services/Discovery/Phase35G/`: Phase 35G non-enabling containment architecture decision record; controlled remote execution selected
- `service-dotnet/Services/Discovery/Phase35H/`: Phase 35H typed authenticated inert remote boundary proof; client/worker protocol, replay/lifecycle ledger, quarantine, and audit correlation
- `service-dotnet/Services/Discovery/Phase35I/`: Phase 35I portable Windows worker/runner admission, Phase35C resource projection, session path binding, lifecycle/result/evidence contracts, and proof classification
- `service-dotnet/Phase35I.Runtime/`: Phase 35I Windows-only native restricted-token, suspended-process, Job Object, assignment, resume, termination, and handle boundary
- `service-dotnet/Phase35I.InertRunner/`: Phase 35I repository-owned closed inert workload executable
- `docs/current-state/phase35j-windows-execution-validation-state.md`: Phase 35J gate status and measured environment limitation
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
  - includes `pbir-local-writer-boundary-state.md` for Phase 24 pbir-local-writer/v1, pbir-local-write-request/v1, pbir-local-write-manifest/v1, dry-run local write planning, overwrite risk, rollback planning, forbidden deployable artifact policy, and the remaining real writer gap
  - includes `pbir-local-preview-writer-state.md` for Phase 25 pbir-local-preview-writer/v1, pbir-local-preview-write-result/v1, preview-only file writing, hash-matched overwrite protection, rollback metadata references, forbidden deployable artifact policy, and the remaining deployable PBIR writer gap
  - includes `pbir-preview-package-review-handoff-state.md` for Phase 26 pbir-preview-package/v1, pbir-review-handoff/v1, metadata-only preview package inventory, Design Studio review handoff, Analyzer Workspace validation boundary preservation, and remaining deployable PBIR and Analyzer automation gaps
  - includes `design-studio-preview-review-state.md` for Phase 27 design-studio-preview-review/v1, the Design Studio Preview Review stage, review-only actions, protocol validation, preview package/handoff metadata rendering, and remaining deployable PBIR and Analyzer automation gaps
  - includes `design-studio-execution-readiness-state.md` for Phase 28 design-studio-execution-readiness/v1, the informational execution-readiness dashboard, stage and warning aggregation, protocol validation, safety gates, and remaining execution implementation gaps
  - includes `pbir-modern-serializer-state.md` for Repository Phase 29 / original Phase 4A, deterministic in-memory modern PBIR artifacts, locked local schema conformance, and fail-closed semantic projection
  - includes `pbir-deployable-materialization-state.md` for implemented Repository Phase 30 / original Phase 4B, read-only target preview, embedded pinned-schema validation, staged directory promotion, managed replacement, journals, receipts, rollback, recovery, retry, and cleanup behavior
  - includes `pbir-materialization-application-orchestration-state.md` for Repository Phase 31 post-4B application composition, typed outcomes, validated-preview/fresh-transaction enforcement, cancellation, concurrency, recovery inspection, and diagnostic redaction
  - includes `rpc-transport-state.md` for Repository Phase 32 strict bounded framing, request lifecycle, cancellation, concurrency, serialized response writing, shutdown, cleanup, and diagnostic guarantees
  - includes `pbir-materialization-provider-adapter-state.md` for the provider/runtime boundary and Phase 32 prerequisite
  - includes `pbir-materialization-rpc-adapter-state.md` for implemented Repository Phase 33 local PBIR RPC routes, contracts, outcomes, limits, lifecycle, and exclusions
  - includes `vscode-local-pbir-materialization-workflow-state.md` for Repository Phase 34 Design Studio workflow, lifecycle, redaction, and route-only boundaries
  - includes `phase35a-contract-only-provider-foundation-state.md` for authoritative Phase 35A contracts and no-provider conclusion
  - includes `phase35b-governed-runtime-provider-architecture-state.md` and `phase35b-runtime-threat-model.md` for the offline composition root, threat model, and Phase 35C prerequisites

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
