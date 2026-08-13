# Phase 35K Windows Containment Test Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Phase35I Windows skip-only scaffold with executable, deterministic integration coverage that runs only on a certified Windows worker and leaves Phase35I runtime architecture unchanged.

**Architecture:** A test-only `Phase35IWindowsHarness` will validate the host, stage the repository-owned inert runner from fixed build output, compute package/executable identity, create exact Phase35I requests, invoke admission/runtime/evidence, and remove its private workspace. Tests will assert only existing Phase35I result/evidence fields; unsupported native telemetry will remain an explicit proof limitation instead of introducing runtime instrumentation.

**Tech Stack:** .NET 8, `net8.0-windows` xUnit tests, existing Phase35I runtime/contracts/evidence, repository-owned `Phase35I.InertRunner`.

---

### Task 1: Replace the scaffold with a reusable Windows harness

**Files:**
- Modify: `service-dotnet/tests/Discovery/Phase35IWindowsIntegrationTests.cs`

- [ ] Add runtime skip detection for Windows, x64, and .NET 8; throw xUnit `SkipException` with structured `NotApplicable:<reason>` messages when any requirement is absent.
- [ ] Add a disposable harness that creates a private worker root, locates only the fixed repository-owned `Phase35I.InertRunner/bin/{Configuration}/net8.0` output, copies the package to `runner/`, computes deterministic package and executable SHA-256 identities, creates the exact worker profile and Phase35H request, invokes `Phase35IAdmission`, `Phase35IWindowsRuntime`, and `Phase35IEvidenceBuilder`, and deletes the workspace idempotently.
- [ ] Add shared evidence assertions for lifecycle, canonical payload/hash, worker profile, runner identity, containment profile, request hash, and Phase35H correlation.
- [ ] Keep every test method decorated with `Category=WindowsIntegration`; do not use an unconditional `Skip` attribute so Windows can execute the bodies.

### Task 2: Add positive containment workload tests

**Files:**
- Modify: `service-dotnet/tests/Discovery/Phase35IWindowsIntegrationTests.cs`

- [ ] Implement successful inert launch, launch-order/result-state, Job Object assignment/limits/no-breakaway, direct child, nested child/process-limit, timeout, cancellation, explicit-environment, restricted-file, bounded-artifact, and cleanup tests using only closed inert workloads and actual Phase35I evidence.
- [ ] Assert `ProvenForInertWorkload` only when Windows runtime evidence reports job assignment; otherwise fail the Windows test rather than upgrading proof based on compilation or discovery.
- [ ] Assert timeout/cancellation lifecycle and cleanup, and verify unrelated parent test execution remains alive after cancellation.
- [ ] Assert artifact metadata from the existing Phase35H result/evidence correlation without inventing a new artifact protocol.

### Task 3: Add deterministic failure mapping coverage

**Files:**
- Modify: `service-dotnet/tests/Discovery/Phase35IWindowsIntegrationTests.cs`

- [ ] Cover runner identity mismatch, invalid worker profile, invalid executable path, and unsupported workload through admission/runtime inputs and assert the existing `Phase35IFailureCode` values.
- [ ] Exercise the runtime's structured timeout/cancellation failures and assert the existing native failure taxonomy is closed and represented in evidence.
- [ ] Do not force token/Job/launch native failures through unsafe machine mutation; document that those branches require a certified Windows worker capability fault or measured Phase35L remediation.

### Task 4: Update authoritative documentation and memory

**Files:**
- Create: `docs/superpowers/specs/2026-08-13-phase35k-windows-containment-test-design.md`
- Modify: `docs/current-state/phase35i-windows-integration-test-guide.md`
- Modify: `docs/current-state/phase35j-windows-execution-validation-state.md`
- Modify: `docs/current-state/phase35i-windows-containment-state.md`
- Modify: `docs/ROADMAP.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/20260813-phase35k-windows-containment-tests.md`

- [ ] Record the harness, fixed runner staging and identity calculation, environment detection, test inventory, evidence limitations, and exact non-Windows skip semantics.
- [ ] State that Phase35K makes the suite executable but does not create Windows evidence on macOS; recommend Phase35L only for certified-worker execution and measured failure remediation.

### Task 5: Validate without committing or staging

**Files:**
- No additional files.

- [ ] Run focused Phase35I portable tests, boundary tests, Windows discovery, full backend tests, .NET builds, extension checks, documentation/boundary scans, and `git diff --check` where the local environment supports them.
- [ ] Preserve the expected macOS result: Windows tests discovered and skipped with structured reasons; no Windows proof claim.
- [ ] Confirm HEAD is unchanged, no files are staged, and generated build output is not included in the Phase35K change set.
