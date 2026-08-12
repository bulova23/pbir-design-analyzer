# Phase 35B — Governed Runtime Provider Architecture & Execution Framework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a deterministic, offline-only runtime composition root that coordinates future providers through the authoritative Phase 35A governance contracts.

**Architecture:** Add focused `Phase35B` contracts and services beside `Phase35A`. The orchestrator only coordinates gates, resolution, session replacement, lifecycle, typed offline adapter simulation, validation, artifact intake, cancellation, audit, and diagnostics. The normal Phase 35A catalog remains unavailable.

**Tech Stack:** .NET 8, C# records/enums, async `Task`, `CancellationToken`, `TimeProvider`, SHA-256/canonical Phase 35A helpers, xUnit.

---

### Task 1: Add the failing Phase 35B tests and boundary inventory

**Files:**
- Create: `service-dotnet/tests/Discovery/Phase35BRuntimeTests.cs`
- Create: `service-dotnet/tests/Discovery/Phase35BBoundaryTests.cs`

- [ ] Add tests for exact provider resolution, zero/multiple matches, capability/profile/policy mismatch, denied authorization, failed/unavailable readiness, production catalog unavailability, immutable session replacement, legal/illegal transitions, timeout/cancellation classification, fixed validation ordering, artifact acceptance/rejection/quarantine, redaction metadata, audit projection, retry classification, deterministic hashing/lineage, fake adapter isolation, and composed positive/negative paths.
- [ ] Add reflection/source boundary assertions that Phase35B types do not reference `System.Diagnostics.Process`, HTTP/network namespaces, MCP/Skills invocation, credential APIs, or dynamic loading.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~Phase35B`; confirm failure is due to missing Phase35B types, not a malformed test.

### Task 2: Add focused runtime contracts

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BContracts.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BProviderAdapter.cs`

- [ ] Define versioned runtime-only records and closed enums for runtime state/events, timeout policy, adapter descriptor, plan, validation stage result, artifact disposition, audit event/record, diagnostics, and execution outcome.
- [ ] Define the strongly typed offline adapter interface with request validation, capability declaration, readiness declaration, plan description, and `ExecuteOfflineAsync`; omit commands, paths, URLs, credentials, arbitrary dictionaries, delegates, and reflection.
- [ ] Reuse Phase 35A request/profile/policy/authorization/readiness/result/artifact/failure/retry/redaction/quarantine/lineage records directly.

### Task 3: Add gates, registry, resolution, session, and lifecycle

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BAuthorizationGate.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BReadinessGate.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BProviderRegistry.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BProviderResolutionService.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BSessionFactory.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BLifecycleCoordinator.cs`

- [ ] Implement deny-by-default exact authorization and policy scope checks.
- [ ] Implement readiness snapshot validation without changing the Phase 35A evaluator or catalog.
- [ ] Implement immutable explicit adapter registration and deterministic exact resolution with zero/multiple-match failures.
- [ ] Implement immutable session creation and closed runtime transitions with terminal-state rejection.

### Task 4: Add validation, artifact intake, timeout, audit, and diagnostics

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BValidationPipeline.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BArtifactIntakeService.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BTimeoutCoordinator.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BAuditProjectionService.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BDiagnostics.cs`

- [ ] Implement fixed-order, fail-fast typed validation stages with no hidden side effects.
- [ ] Validate Phase 35A result/artifact relationships, redaction metadata, quarantine, lineage, and hashes; return accepted, rejected, or quarantined dispositions.
- [ ] Implement linked-token timeout/caller-cancellation classification using injected `TimeProvider` and no process termination.
- [ ] Project immutable audit and redacted structured events; consume Phase 35A retry classification without running a retry loop.

### Task 5: Add the composition root and offline integration path

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BOrchestrator.cs`
- Create: `service-dotnet/Services/Discovery/Phase35B/Phase35BProductionCatalog.cs`
- Modify: `service-dotnet/tests/Discovery/Phase35BRuntimeTests.cs`

- [ ] Coordinate the focused services through a small async orchestrator and return a typed outcome for success and each negative terminal category.
- [ ] Keep the production catalog adapter-free/unavailable; construct a fake adapter only inside tests.
- [ ] Prove the full fake path from governed request through audit and completion and negative paths before adapter invocation.

### Task 6: Update documentation and repository memory

**Files:**
- Create: `docs/current-state/phase35b-governed-runtime-provider-architecture-state.md`
- Create: `docs/current-state/phase35b-runtime-threat-model.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/current-state/runtime-provider-framework-state.md`
- Modify: `docs/current-state/execution-provider-framework-state.md`
- Modify: `docs/current-state/generation-provider-framework-state.md`
- Modify: `docs/current-state/architecture-gap-analysis.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-08-12-phase35b-governed-runtime-provider-architecture.md`

- [ ] Record the composition architecture, actual class map, threat model, no-provider conclusion, lifecycle, validation, artifact, cancellation, and Phase 35C prerequisites.
- [ ] Keep Phase 35A and unrelated dirty files identified as pre-existing and do not rewrite their content.

### Task 7: Validate and close out without staging or committing

- [ ] Run focused Phase 35A and Phase 35B tests, full backend tests, RPC tests, extension/webview tests, TypeScript compilation, .NET build, pinned schema/boundary gates, boundary/API scans, documentation checks, scoped lint, and `git diff --check`.
- [ ] Compare lint output against the documented pre-existing baseline and ensure no Phase35B file adds errors.
- [ ] Inspect `git status --short`, `git diff --stat`, and `git diff --check`; confirm nothing is staged or committed and Phase 35A/35B/unrelated changes remain distinguishable.

