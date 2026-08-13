# Phase 35F macOS Containment Mechanism Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Evaluate macOS containment mechanisms and make local provider admission fail closed until every required Phase 35C control is proven enforced.

**Architecture:** Preserve Phase35E as the only process-boundary owner. Add a small Core-side Phase35F selector/evidence contract that records the platform and per-control enforcement states, selects no local macOS mechanism on the observed target, and never launches a workload. Remove the unused unrestricted Phase35E process implementation so no generic fallback remains.

**Tech Stack:** .NET 8 Core contracts, .NET 8 Phase35E runtime assembly, xUnit, macOS codesign/sandbox/Virtualization evidence, Markdown architecture records.

---

### Task 1: Establish the platform evidence and design decision

**Files:**
- Create: `docs/superpowers/specs/2026-08-12-phase35f-macos-containment-mechanism-design.md`
- Create: `docs/superpowers/plans/2026-08-12-phase35f-macos-containment-mechanism-plan.md`

- [x] Record macOS, Darwin, architecture, .NET, signing, developer-tool, and Seatbelt probe evidence.
- [x] Compare App Sandbox, Hardened Runtime, helper/XPC, Virtualization.framework, container, and remote execution against required controls.
- [x] Select `none-local-macos/v1`; do not add a provider or fixture launch path.

### Task 2: Add the fail-closed capability/evidence seam test-first

**Files:**
- Create: `service-dotnet/tests/Discovery/Phase35FContainmentTests.cs`
- Create: `service-dotnet/Services/Discovery/Phase35F/Phase35FContainmentDecision.cs`

- [x] Add a test proving the current target selects no local mechanism, returns `PlatformContainmentUnavailable`, reports filesystem/network/child-process controls unsupported, and hashes evidence.
- [x] Add a test proving code signing/identity and Hardened Runtime are not mislabeled as filesystem or network containment.
- [x] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~Phase35FContainmentTests --no-restore` and verify the tests pass.

### Task 3: Remove the unrestricted Phase35E fallback

**Files:**
- Modify: `service-dotnet/Services/Discovery/Phase35E/Phase35ESandboxedProcessRunner.cs`

- [x] Remove the unused production `Phase35EProcessBoundary` that could launch an executable directly without containment.
- [x] Retain only the adapter-bound runtime path and test-owned boundary implementations.
- [x] Run Phase35E and Phase35F focused tests and the static boundary scan; 11 tests passed with no skips.

### Task 4: Publish authoritative state and threat records

**Files:**
- Create: `docs/current-state/phase35f-macos-containment-decision-state.md`
- Create: `docs/current-state/phase35f-macos-containment-threat-model.md`
- Modify: `docs/current-state/phase35e-os-sandbox-enforcement-state.md`
- Modify: `docs/current-state/phase35e-os-sandbox-threat-model.md`
- Modify: `docs/current-state/architecture-gap-analysis.md`
- Modify: `docs/current-state/generation-provider-framework-state.md`
- Modify: `docs/ROADMAP.md`

- [x] Document the exact candidate matrix, control matrix, evidence record, signing/deployment implications, threat residuals, CI split, and platform fallback behavior.
- [x] Preserve the historical Seatbelt failure and correct Phase35E language that implied enforcement.
- [x] Keep the production catalog and `powerbi-report-author@0.1.4` non-executable.

### Task 5: Update repository memory and validate

**Files:**
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/session-summaries.md`
- Create or finalize: `.agent-memory/sessions/2026-08-12-phase35f-macos-containment.md`

- [x] Record the no-local-mechanism decision, exact focused validation results, and next phase recommendation.
- [x] Run the focused Phase35A–F backend tests, full backend, extension Jest/webview Jest, TypeScript/build/package, boundary/schema/document/scope checks, and `git diff --check`; all passed with zero skips except the documented 43-error pre-existing lint baseline.
- [x] Leave all Phase35F changes unstaged and uncommitted; preserve unrelated dirty files.
