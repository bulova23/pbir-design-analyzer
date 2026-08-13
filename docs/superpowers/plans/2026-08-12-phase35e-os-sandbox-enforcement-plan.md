# Phase 35E — OS Sandbox Enforcement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a fail-closed macOS sandbox boundary for a controlled repository-owned fixture while preserving the disabled production catalog.

**Architecture:** Add immutable Phase35E contracts, exact identity and policy binding, a macOS Seatbelt adapter, one direct process runner, lifecycle/evidence/audit projection, and focused tests. Keep Phase35B untouched and do not add provider execution.

**Tech Stack:** .NET 8, macOS Seatbelt, `System.Diagnostics.Process` in one runner, xUnit, SHA-256 canonical evidence.

---

### Task 1: Add failing contracts and boundary tests

**Files:** Create `service-dotnet/tests/Discovery/Phase35ERuntimeTests.cs`, `service-dotnet/tests/Discovery/Phase35EBoundaryTests.cs`.

- [ ] Define tests for capability fail-closed behavior, exact identity mismatch, policy binding, environment/path validation, bounded output, timeout/cancellation, result classification, evidence hashing, audit lifecycle, and static forbidden surfaces.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~Phase35E` and confirm failure because Phase35E types do not exist.

### Task 2: Add immutable Phase35E contracts and admission

**Files:** Create `service-dotnet/Services/Discovery/Phase35E/Phase35EContracts.cs`, `Phase35EAdmission.cs`, `Phase35EExecutableIdentityVerifier.cs`, `Phase35EPolicyBinder.cs`.

- [ ] Model exact provider/package/certification identity, typed executable mapping, capability states, policy bindings, execution spec, failure taxonomy, result, evidence, and cleanup.
- [ ] Reject unsupported platforms, unknown controls, non-absolute paths, path substitution, invalid finite limits, and mismatched certification/package hashes before process creation.
- [ ] Keep caller input out of executable, argument, environment, and working-directory authority.

### Task 3: Add macOS adapter and runner

**Files:** Create `Phase35EMacSandboxAdapter.cs`, `Phase35ESandboxedProcessRunner.cs`, `Phase35ESandboxLifecycleCoordinator.cs`.

- [ ] Generate a deterministic Seatbelt profile with deny-default, network denial, approved read/write roots, temporary/output roots, and child-process denial.
- [ ] Launch only `/usr/bin/sandbox-exec` through the dedicated runner with structured arguments, redirected bounded streams, no stdin, timeout/cancellation, owned-process termination, and scoped cleanup.
- [ ] Report memory/CPU/process-count as unsupported and deny profiles that require them.

### Task 4: Add deterministic fixture and integration coverage

**Files:** Create `service-dotnet/tests/Fixtures/Phase35ESandboxFixture/Phase35ESandboxFixture.csproj`, `Program.cs`; modify `service-dotnet/tests/Tests.csproj` only for fixture build support.

- [ ] Implement a closed mode enum for success, environment inspection, filesystem/network/child-process attempts, timeout, bounded/excessive output, malformed result, and non-zero exit.
- [ ] Exercise the fixture only through the Phase35E runner; never expose it as a provider catalog entry or general command bridge.
- [ ] Mark platform-dependent assertions explicitly and fail admission rather than skipping required-control tests on unsupported platforms.

### Task 5: Add evidence/audit integration and documentation

**Files:** Create `Phase35EEvidenceCollector.cs`, `docs/current-state/phase35e-os-sandbox-enforcement-state.md`, `docs/current-state/phase35e-os-sandbox-threat-model.md`; modify `docs/ROADMAP.md`, `docs/current-state/architecture-gap-analysis.md`, `docs/current-state/generation-provider-framework-state.md`, `.agent-memory/repo-map.md`.

- [ ] Hash canonical bounded evidence and append admission, identity, creation, start, violation, termination, exit, and cleanup events to Phase35C audit.
- [ ] Document the enforcement matrix, capability matrix, TOCTOU limitation, deprecated Seatbelt residual risk, and exact remaining provider blockers.

### Task 6: Validate and close out without Git mutation

- [ ] Run focused Phase35E, Phase35A–E, full backend, RPC, extension, webview, build/package, boundary, docs, lint, and `git diff --check` validation where applicable.
- [ ] Compare lint output to the recorded baseline and update memory/session records with exact results.
- [ ] Confirm no staging, commit, reset, clean, or unrelated-file rewrite.
