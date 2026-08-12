# Phase 35D — Pre-Production Provider Certification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an offline-safe, exact-identity provider certification layer that can establish pre-production eligibility without activating a production provider.

**Architecture:** Add focused Phase35D contracts, identity resolver, signature verifier, evidence/certification evaluator, lifecycle/binding services, and bounded persistence beside Phase35C. Reuse Phase35A canonical JSON and Phase35C evaluators; never invoke provider execution.

**Tech Stack:** .NET 8, immutable C# records, platform RSA/SHA-256, xUnit, local atomic file persistence.

---

### Task 1: Add red contract, identity, attestation, and boundary tests

**Files:** Create `service-dotnet/tests/Discovery/Phase35DRuntimeTests.cs`, `service-dotnet/tests/Discovery/Phase35DBoundaryTests.cs`.

- [ ] Cover stable identity, all signature outcomes, candidate/profile contracts, and forbidden execution surface references.
- [ ] Run focused tests and confirm the new types are not yet available.

### Task 2: Implement package identity and signed attestation verification

**Files:** Create `service-dotnet/Services/Discovery/Phase35D/Phase35DContracts.cs`, `Phase35DPackageIdentityResolver.cs`, `Phase35DSignedAttestationVerifier.cs`.

- [ ] Implement immutable records and closed reason enums.
- [ ] Hash only approved metadata with Phase35A canonical JSON.
- [ ] Verify RSA/SHA-256 signatures in-process and fail closed for unsupported/malformed evidence.
- [ ] Run identity and signature tests.

### Task 3: Implement conformance, evidence, certification lifecycle, and binding

**Files:** Create `Phase35DConformanceRunner.cs`, `Phase35DCertificationEvaluator.cs`, `Phase35DCertificationLifecycle.cs`, `Phase35DCertificationActivationBinding.cs`; modify Phase35C activation denial reasons only if required for typed certification denial.

- [ ] Compose existing Phase35C decisions and conformance/corpus evaluators.
- [ ] Build canonical evidence bundles and immutable records.
- [ ] Enforce legal lifecycle transitions, expiry, revocation, supersession, and exact activation matching.
- [ ] Prove pre-production success and production denial.

### Task 4: Implement bounded protected audit/replay persistence

**Files:** Create `Phase35DProtectedAuditReplayStore.cs`; extend runtime tests.

- [ ] Persist explicit audit/replay state with atomic replacement and integrity hash.
- [ ] Validate restart, mutation, deletion, sequence gaps, hash corruption, duplicates, and partial-write recovery.
- [ ] Run focused persistence tests.

### Task 5: Add authoritative current-state/threat/roadmap documentation and memory

**Files:** Create `docs/current-state/phase35d-provider-certification-state.md`, `docs/current-state/phase35d-provider-certification-threat-model.md`; modify `docs/ROADMAP.md`, `docs/current-state/architecture-gap-analysis.md`, `docs/current-state/generation-provider-framework-state.md`, `.agent-memory/repo-map.md`, `.agent-memory/session-summaries.md`.

- [ ] Document actual relationships, sanitized evidence/hash, residual gaps, and Phase 35E recommendation.
- [ ] Preserve the statement that no production runtime generation provider is available.

### Task 6: Validate and close out without Git mutation

- [ ] Run focused Phase35D, Phase35A–D, full backend, RPC, extension, webview, build, boundary, docs, lint, and diff checks as available.
- [ ] Record exact outcomes, known baseline failures, and remaining gaps in the session note.
- [ ] Confirm nothing was staged or committed and update current focus/session summary.
