# Phase 35H Remote Boundary Proof Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove a strongly typed, authenticated, independently revalidated remote execution boundary using only a repository-owned inert workload.

**Architecture:** Add a focused Phase35H package beside Phase35A–G. A signed in-process domain transport exercises real identity and tamper semantics; the worker owns validation, replay/idempotency, lifecycle, resource bounds, persisted reconciliation, audit, and artifact quarantine. The client performs only local request preparation and post-return validation.

**Tech Stack:** .NET 8, immutable C# records, RSA-SHA256 signatures, System.Text.Json canonical Phase35A hashing, xUnit.

---

### Task 1: Add the failing protocol and authentication tests

**Files:**
- Create: `service-dotnet/tests/Discovery/Phase35HRuntimeTests.cs`
- Create: `service-dotnet/tests/Discovery/Phase35HBoundaryTests.cs`

- [ ] Write tests for the closed operation enum, unknown major version rejection, required field/resource validation, signed request verification, unknown client rejection, worker identity mismatch, tamper rejection, and absence of generic command names in Phase35H source.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~Phase35H`; expect compilation failure because Phase35H types do not exist.

### Task 2: Implement typed contracts and authenticated envelopes

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35H/Phase35HContracts.cs`
- Create: `service-dotnet/Services/Discovery/Phase35H/Phase35HAuthentication.cs`

- [ ] Define only the five allowed operations, closed inert workload enum, lifecycle/failure/outcome enums, exact certification and worker identity records, resource/credential-reference records, request/result/artifact/audit records, and signed envelope.
- [ ] Implement ephemeral RSA key generation, SHA256 signature creation/verification, canonical request hash binding, and explicit client/worker identity allowlists.
- [ ] Run the Phase35H tests; expect contract/authentication tests to pass while worker-flow tests remain failing.

### Task 3: Add failing worker lifecycle, replay, and inert-runner tests

**Files:**
- Modify: `service-dotnet/tests/Discovery/Phase35HRuntimeTests.cs`

- [ ] Add tests for successful inert execution, deterministic hash, bounded artifact manifest, structured failure, duplicate submit idempotency, modified-payload rejection, authorized status retry, cancellation races, worker timeout, invalid resource policy, independent certification/profile/policy rejection, and raw-secret rejection.
- [ ] Add tests for persisted restart state: terminal records reconcile, incomplete work becomes `Uncertain`, and uncertain work is not replayed.
- [ ] Run the focused tests; expect failures for missing worker and runner types.

### Task 4: Implement the closed inert worker and ledger

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35H/Phase35HInertRunner.cs`
- Create: `service-dotnet/Services/Discovery/Phase35H/Phase35HWorker.cs`

- [ ] Implement the runner as a closed switch with deterministic bounded bytes and no process/file/network APIs; `TimedWait` is represented by a deterministic clock-controlled runner state.
- [ ] Implement authenticated submit/status/cancel/manifest/artifact methods, fail-closed validation order, exact certification binding, replay identity binding, resource checks, legal lifecycle transitions, worker-side timeout, typed cancellation, and idempotent duplicate handling.
- [ ] Persist only bounded execution records, replay identities, audit chain, and synthetic artifact bytes under a caller-provided session directory; reject path traversal and cross-session access.
- [ ] Append remote hash-chain audit events for receipt, validation, authorization, replay, resource decision, start, terminal outcome, manifest, quarantine, and completion.
- [ ] Run the focused Phase35H tests; expect worker-flow tests to pass.

### Task 5: Implement local client validation and artifact quarantine flow

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35H/Phase35HClient.cs`
- Modify: `service-dotnet/tests/Discovery/Phase35HRuntimeTests.cs`

- [ ] Implement typed client calls that sign requests, verify worker responses, record the remote evidence hash locally, validate manifest lineage/size/hash, retrieve only the exact artifact ID, and pass the candidate through `Phase35CArtifactSafetyPipeline`.
- [ ] Test clean acceptance, suspicious quarantine, hash mismatch, oversized transfer, unexpected artifact ID, unauthorized session, and correlated local/remote audit examples.
- [ ] Run focused tests and `git diff --check`.

### Task 6: Document proof status and repository memory

**Files:**
- Create: `docs/current-state/phase35h-remote-boundary-proof-state.md`
- Create: `docs/current-state/phase35h-remote-worker-trust-model.md`
- Create: `docs/current-state/phase35h-replay-idempotency-audit-artifact-model.md`
- Create: `docs/current-state/phase35h-windows-containment-analysis.md`
- Create: `docs/current-state/phase35h-threat-model.md`
- Modify: `docs/ROADMAP.md`, `.agent-memory/repo-map.md`, `.agent-memory/current-focus.md`, `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-08-13-phase35h-remote-boundary-proof.md`

- [ ] Record contract/authentication/in-process proof separately from Windows worker and OS isolation skips; include architecture diagram, protocol list, threat model, credential boundary, artifact path, exact remaining provider blockers, and one Phase35I recommendation.
- [ ] Record the live HEAD, clean initial state, uncommitted Phase35H paths, no staged files, no commits, and preserved unrelated work.

### Task 7: Run validation and inspect final state

- [ ] Run focused Phase35H and Phase35A–G tests, full backend, RPC, extension Jest, webview Jest, TypeScript compilation, .NET build, extension build/package, boundary/security/document scans, scoped lint, and `git diff --check`.
- [ ] Do not repeat a failing command more than twice without a new hypothesis; report skipped Windows/mTLS tests explicitly.
- [ ] Confirm `git status --short`, staged-file list, and commit count; leave every Phase35H change unstaged and uncommitted.
