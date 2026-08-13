# Phase 35G Containment Architecture Decision Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Record an evidence-backed choice between local Virtualization.framework execution and controlled Windows/Linux remote execution without enabling a provider.

**Architecture:** Preserve Phase 35A–F contracts and add one small Phase 35G decision record. Use documentation as the authoritative architecture artifact; do not create a runtime abstraction for both options. Select a remote controlled boundary because future Desktop-dependent provider behavior requires Windows, while Apple Virtualization documents macOS/Linux guests.

**Tech Stack:** .NET 8 internal records and xUnit; Markdown; Apple Developer and Microsoft Learn primary documentation.

---

### Task 1: Add the non-enabling decision contract

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35G/Phase35GContainmentDecision.cs`
- Modify: `service-dotnet/Services/Discovery/Phase35F/Phase35FContainmentDecision.cs`
- Test: `service-dotnet/tests/Discovery/Phase35GContainmentTests.cs`

- [ ] Add a versioned decision record with `RemoteControlled` selected and `ProviderExecutionEnabled=false`.
- [ ] Extend the Phase 35F mechanism enum so future local virtualized and remote mechanisms are representable while the selector still returns `NoneLocalMacOs`.
- [ ] Test selection, non-enablement, prerequisites, and historical Phase 35F behavior.

### Task 2: Publish the design and evidence record

**Files:**
- Create: `docs/current-state/phase35g-containment-architecture-decision-state.md`
- Create: `docs/current-state/phase35g-virtualization-architecture-analysis.md`
- Create: `docs/current-state/phase35g-remote-execution-architecture-analysis.md`
- Create: `docs/current-state/phase35g-containment-threat-model.md`
- Create: `docs/architecture/phase35g-remote-controlled-execution-adr.md`

- [ ] Record repository evidence, mandatory properties, platform compatibility, decision matrix, diagrams, security model, deployment model, failure model, threats, proof-of-concept status, remaining blockers, and Phase 35H scope.
- [ ] Link Apple and Microsoft primary sources and label unknowns as unknown.
- [ ] State that no POC was necessary and no provider execution occurred.

### Task 3: Reconcile roadmap and current-state references

**Files:**
- Modify: `docs/ROADMAP.md`
- Modify: `docs/current-state/architecture-gap-analysis.md`
- Modify: `docs/current-state/generation-provider-framework-state.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-08-12-phase35g-containment-architecture.md`

- [ ] Map Phase 35G after Phase 35F and before any future provider execution phase.
- [ ] Preserve Phase 35F’s historical local-macOS rejection and the authoritative “No runtime generation provider is available” conclusion.
- [ ] Record the selected remote boundary as not implemented and list Phase 35H prerequisites.

### Task 4: Validate the decision-only change

**Files:**
- No additional files.

- [ ] Run Phase 35G and Phase 35A–F focused xUnit tests.
- [ ] Run the full backend, RPC, extension, webview, TypeScript, .NET, packaging, schema/boundary, documentation, scoped lint, and `git diff --check` gates.
- [ ] Report the unchanged repository lint baseline separately from changed-file validation.
- [ ] Confirm Git state is uncommitted and unstaged for Phase 35G, with unrelated dirty files preserved.

