# Phase 35I Windows Containment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a portable, fail-closed Phase35I admission/evidence layer and a single Windows-native inert containment runtime without claiming Windows proof on the current macOS host.

**Architecture:** Core owns immutable contracts, admission, resource projection, path binding, evidence, and audit correlation. `Phase35I.Runtime` owns every Windows process/token/Job Object API. `Phase35I.InertRunner` is the only launched executable and exposes closed workload modes.

**Tech Stack:** .NET 8, C#, xUnit, `System.Text.Json`, Windows P/Invoke isolated to `net8.0-windows`.

**Execution status:** Implemented on 2026-08-13. Portable and boundary tests pass; Windows integration tests are explicitly skipped on the macOS checkout. No commit or staging was performed.

---

### Task 1: Add failing portable contract/admission tests

**Files:**
- Create: `service-dotnet/tests/Discovery/Phase35IContainmentTests.cs`

- [ ] Write tests for exact worker/runner identity, rejection of caller paths and arbitrary launch inputs, closed workloads, Phase35C resource projection, session-root binding, evidence hash/proof classification, and non-Windows partial proof.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~Phase35I` and verify the tests fail because the Phase35I types do not exist.

### Task 2: Implement portable contracts and admission

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35I/Phase35IContracts.cs`
- Create: `service-dotnet/Services/Discovery/Phase35I/Phase35IAdmission.cs`
- Create: `service-dotnet/Services/Discovery/Phase35I/Phase35IResourceProjection.cs`
- Create: `service-dotnet/Services/Discovery/Phase35I/Phase35IPathBinder.cs`
- Create: `service-dotnet/Services/Discovery/Phase35I/Phase35IEvidence.cs`

- [ ] Define closed enums/records for worker profile, package/executable identity, containment profile, workload, admission, resource projection, lifecycle/result, native failure, evidence, and proof status.
- [ ] Validate only authoritative certified metadata; reject caller executable paths, command text, shell arguments, dynamic names, arbitrary working directories, arbitrary environment dictionaries, path traversal, non-finite policy, and mismatched profiles.
- [ ] Project Phase35C duration, process count, memory, output/result bytes, artifact count/bytes, and concurrency into separately labeled OS-enforced/worker-enforced controls.
- [ ] Derive a normalized session path beneath a worker-owned root.
- [ ] Hash canonical evidence with the existing Phase35A canonical JSON convention and correlate it to the Phase35H request/audit hash.
- [ ] Run the focused Phase35I tests and verify green.

### Task 3: Add the repository-owned inert runner

**Files:**
- Create: `service-dotnet/Phase35I.InertRunner/Phase35I.InertRunner.csproj`
- Create: `service-dotnet/Phase35I.InertRunner/Program.cs`

- [ ] Implement only the closed workload modes required by Phase35H plus child/nested-child attempts, restricted-file check, bounded diagnostics, and structured failure.
- [ ] Make the command-line contract an internal workload enum; reject unknown options and never invoke shell, PowerShell, command interpreter, arbitrary process paths, downloaded code, or arbitrary assemblies.
- [ ] Build the runner and record its certified relative identity for the Windows runtime tests.

### Task 4: Add the Windows runtime project and native boundary

**Files:**
- Create: `service-dotnet/Phase35I.Runtime/Phase35I.Runtime.csproj`
- Create: `service-dotnet/Phase35I.Runtime/Phase35IWindowsRuntime.cs`
- Create: `service-dotnet/Phase35I.Runtime/Phase35IWindowsNative.cs`
- Modify: `service-dotnet/tests/Tests.csproj`
- Modify: `service-dotnet/Properties/AssemblyInfo.cs`

- [ ] Target `net8.0-windows`, enable Windows targeting for cross-platform compilation, and reference Core plus the inert runner output metadata without adding native APIs to Core.
- [ ] Put all P/Invoke declarations and calls in the runtime project: `CreateRestrictedToken`, `CreateProcessAsUser`, `CreateJobObject`, `SetInformationJobObject`, `AssignProcessToJobObject`, `IsProcessInJob`, `ResumeThread`, `TerminateJobObject`, `GetExitCodeProcess`, and explicit handle close operations.
- [ ] Construct explicit handles, environment, startup info, suspended launch flags, Job Object limits, no-breakaway policy, and deterministic cleanup. Resume only after successful assignment verification.
- [ ] Map native errors into the portable failure taxonomy and return `PartiallyProven` unless actual Windows evidence is collected.
- [ ] Add the runtime project reference and run a cross-platform build.

### Task 5: Add Windows integration and boundary tests

**Files:**
- Create: `service-dotnet/tests/Discovery/Phase35IWindowsIntegrationTests.cs`
- Modify: `service-dotnet/tests/Discovery/Phase35IBoundaryTests.cs`

- [ ] Mark tests with `Trait("Category", "WindowsIntegration")` and skip on non-Windows with an explicit reason.
- [ ] Cover launch ordering, identity, Job Object limits, child/nested child, process count, timeout, cancellation, environment exclusion, ACL denial, cleanup, kill-on-close, and native failure mapping using actual Windows behavior.
- [ ] Scan all production source outside `Phase35I.Runtime` for native API names and prohibited shell/provider/credential/publication capabilities.
- [ ] Run portable, boundary, and Windows test discovery separately; report skipped Windows tests as not applicable.

### Task 6: Update authoritative documentation and memory

**Files:**
- Create: `docs/current-state/phase35i-windows-containment-state.md`
- Create: `docs/current-state/phase35i-windows-containment-profile.md`
- Create: `docs/current-state/phase35i-windows-containment-threat-model.md`
- Create: `docs/current-state/phase35i-windows-integration-test-guide.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/current-state/architecture-gap-analysis.md`
- Modify: `docs/current-state/phase35h-remote-boundary-proof-state.md`
- Modify: `docs/current-state/phase35h-windows-containment-analysis.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/20260813-phase35i-windows-containment.md`

- [ ] Document the actual component map, API boundary, identity binding, launch ordering, Job Object matrix, token model, child behavior, environment/handle policy, timeout/cancellation, evidence sample, Phase35H correlation, explicit non-Windows skip, and residual blockers.
- [ ] State `PartiallyProven` on the current host and recommend Phase35J as Windows execution validation rather than another architecture layer.
- [ ] Update memory at session close and preserve the uncommitted/unstaged repository state.

### Task 7: Run validation and report repository state

**Files:**
- No additional source files; validation outputs only.

- [ ] Run focused Phase35A–I tests, full backend tests, RPC tests, extension/webview tests, TypeScript/build/package, Core and runtime builds, Windows test discovery, boundary/document scans, scoped lint, and `git diff --check`.
- [ ] Do not rerun an unchanged failing command more than twice; record the exact cause and changed hypothesis for any unresolved failure.
- [ ] Confirm `git status --short` has no staged files and no commits were made; report HEAD and all Phase35I paths separately from generated files.
