# PBIR Materialization Application Orchestration — Repository Phase 31 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Expose canonical Phase 29 serialization and Phase 30 preview, apply, conflict, and recovery inspection through one fail-closed backend application orchestration boundary.

**Architecture:** Add stateless Phase 31 contracts and one orchestration service. Apply deterministically recreates and validates the approved preview before constructing the Phase 30 apply request; Phase 30 retains all filesystem, locking, journaling, receipt, rollback, recovery, schema, and publication authority.

**Tech Stack:** .NET 8, existing Phase 29 and Phase 30 services, CancellationToken, xUnit, and existing temporary/fault filesystem test infrastructure. No new production package.

---

Status: Approved and executed on 2026-08-02. All changes remain uncommitted as required.

Validation outcome: 14 Phase 31 tests passed; 111 focused Phase 29–31 tests passed; the full backend suite passed 665 tests with zero failures or skips; the complete extension and webview Jest run passed 105 suites and 527 tests; standalone TypeScript compilation passed; and the eight-test offline schema/boundary gate verified exactly eight pinned schema resources. Document, whitespace, roadmap, scope, and changed-boundary checks passed.

## File Map

Create PbirMaterializationOrchestrationModels.cs for versioned contracts, PbirMaterializationOrchestrationService.cs for composition, three focused xUnit files for contracts/behavior/boundaries, and pbir-materialization-application-orchestration-state.md for current state. Modify Phase 30 apply only for transaction-safe cancellation points, plus roadmap, architecture, serializer/materialization, test, and memory documents.

## Task 1: Contract Red/Green

- [x] Add failing tests asserting six Phase 31 schema versions, the exact typed outcome set, immutable preview identity fields, and request operation/approval/transaction fields.
- [x] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirMaterializationOrchestrationContractTests`; the red gate failed on missing Phase 31 types.
- [x] Add the minimal contract records and enum, then rerun the focused test to green.

## Task 2: Preview And Recovery Red/Green

- [x] Add failing tests for absent, empty, exact-match, managed-replacement, conflict, recovery-required, serializer failure, unsafe destination, unsupported operation, pre-cancellation, and read-only recovery inspection.
- [x] Implement Preview and InspectRecovery by composing PbirDeployableSerializerService and PbirDeployableMaterializationPreviewService; do not duplicate serialization, hashing, schema, or filesystem logic.
- [x] Rerun focused tests and Phase 29–30 preview suites.

## Task 3: Apply Red/Green

- [x] Add failing tests for missing approval, malformed/future contracts, unsupported operation, unsafe transaction ID, mismatched preview identity, stale destination, schema failure, transaction reuse, create, and managed replacement.
- [x] Implement Apply to reserialize, recreate preview from the original request ID, compare complete identity, and delegate only current create/managed-replacement previews to PbirDeployableMaterializationApplyService.
- [x] Rerun focused apply and Phase 30 apply/rollback tests.

## Task 4: Cancellation And Concurrency Red/Green

- [x] Add injected-filesystem staging cancellation and concurrent-apply tests proving an unchanged/restored target, terminal journal state, and at most one commit.
- [x] Add CancellationToken checks to Phase 30 apply at pre-I/O, post-lock, staging-loop, post-staging, and pre-promotion safe points; restore or abort through existing Phase 30 logic before rethrowing.
- [x] Map cancellation to the typed result and rerun focused tests.

## Task 5: Diagnostic And Dependency Boundaries

- [x] Add tests serializing failure results and proving absolute output/control/staging/backup/quarantine paths and exception messages are absent.
- [x] Add reflection tests proving the orchestrator cannot reach filesystem, path, transaction, schema, preview-writer, HTTP, process, Skills, Desktop, Analyzer, deployment, or extension-host types.
- [x] Implement fixed diagnostic mapping and update changed Phase 30 method-boundary assertions.

## Task 6: Documentation And Required Validation

- [x] Update the Phase 31 design/plan status, current-state, ROADMAP.md, original seven-phase plan, architecture-gap, Phase 29/30 state, repo map, current focus, session summary, and session note.
- [x] Run focused Phase 29–31 backend tests, full backend tests with zero failures/skips, full Jest, TypeScript compile, all eight offline schema gates, document/whitespace/roadmap/scope/changed-boundary checks, `git diff --check`, and final diff/status inspection.
- [x] Leave every Phase 31 change uncommitted.
