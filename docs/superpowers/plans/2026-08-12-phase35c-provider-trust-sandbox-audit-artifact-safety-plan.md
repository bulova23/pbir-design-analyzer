# Phase 35C Provider Trust, Sandbox, Audit, and Artifact Safety Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an offline-only, fail-closed assurance boundary for future providers without activating any real provider.

**Architecture:** Add immutable Phase35C records and narrow deterministic evaluators beside Phase35A/35B. A single activation gate composes their typed decisions but never invokes adapters. Extend Phase35B only with boundary/integration tests and preserve its existing offline fake execution behavior.

**Tech Stack:** .NET 8, C# records/enums, SHA-256 canonical JSON, xUnit.

---

### Task 1: Add red Phase35C contract and activation tests

**Files:**
- Create: `service-dotnet/tests/Discovery/Phase35CRuntimeTests.cs`
- Create: `service-dotnet/tests/Discovery/Phase35CBoundaryTests.cs`

- [x] **Step 1: Write tests for trust, sandbox, credential, policy, activation, and production denial.** Use the existing Phase35B test fixture shape and assert exact enum reasons, not free-form messages.
- [x] **Step 2: Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~Phase35C` and confirm the tests fail because Phase35C types do not exist.**

### Task 2: Implement immutable Phase35C contracts and pure evaluators

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35CContracts.cs`
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35CProviderTrustEvaluator.cs`
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35CSandboxPolicyEvaluator.cs`
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35CCredentialBoundaryPolicy.cs`
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35CResourcePolicyEvaluator.cs`
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35CReplayProtectionService.cs`

- [x] **Step 1: Define closed enums, versioned records, and deterministic reason codes.** Keep secret material out of all records; grants contain only opaque references and hashes.
- [x] **Step 2: Implement trust/attestation validation with injected clock, expiration, provider-version, implementation-hash, capability, execution-mode, sandbox, and policy-version binding.**
- [x] **Step 3: Implement sandbox, credential, resource, and replay evaluators with missing/unknown values denied.**
- [x] **Step 4: Run the Phase35C focused tests and make them pass.**

### Task 3: Implement audit, artifact safety, corpus, and conformance

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35CDurableAuditStore.cs`
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35CArtifactSafetyPipeline.cs`
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35COutputValidationEvaluator.cs`
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35CConformanceEvaluator.cs`

- [x] **Step 1: Add append-only hash-chain audit records, deterministic fixture serialization, sequence/gap checks, and tamper detection.**
- [x] **Step 2: Add fake offline scanner outcomes and the identity/type/size/redaction/quarantine pipeline.**
- [x] **Step 3: Add versioned synthetic output corpus fixtures and required/forbidden property evaluation.**
- [x] **Step 4: Add adapter conformance checks for identity, capabilities, readiness, cancellation, failure mapping, lineage, audit, secret leakage, and artifact classification.**
- [x] **Step 5: Add tests for clean/suspicious/malformed/unsupported/failure/unknown scanners, corpus pass/fail, conformance pass, and deliberately broken adapters.**

### Task 4: Add the authoritative activation gate

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35C/Phase35CActivationGate.cs`
- Modify: `service-dotnet/Services/Discovery/Phase35B/Phase35BProviderRegistry.cs` only if a typed production-catalog assertion is needed
- Test: `service-dotnet/tests/Discovery/Phase35CRuntimeTests.cs`

- [x] **Step 1: Compose all required decisions into an immutable activation result with closed denial reasons.**
- [x] **Step 2: Ensure `CreateProduction`/production catalog remains non-executable and cannot produce an eligible decision.**
- [x] **Step 3: Add an integration test proving Phase35B offline fake execution remains available only through its existing test seam and the production catalog remains unavailable.**

### Task 5: Add documentation, threat model, and repository memory

**Files:**
- Create/update: `docs/current-state/phase35c-provider-trust-sandbox-audit-artifact-safety-state.md`
- Create/update: `docs/current-state/phase35c-runtime-threat-model.md`
- Update: `docs/current-state/architecture-gap-analysis.md`
- Update: `docs/current-state/generation-provider-framework-state.md`
- Update: `docs/ROADMAP.md`
- Update: `.agent-memory/current-focus.md`
- Update: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-08-12-phase35c-provider-trust-sandbox-audit-artifact-safety.md`

- [x] **Step 1: Document actual classes, activation conditions, trust/sandbox/credential/audit/replay/artifact/conformance/corpus boundaries, residual risk, and Phase35D recommendation.**
- [x] **Step 2: Record that Phase35A/35B/35C are uncommitted only if Git confirms that state; otherwise report the actual checked-in state.**

### Task 6: Run validation and inspect the final worktree

- [x] **Step 1: Run focused Phase35A/35B/35C tests and the full backend suite.**
- [x] **Step 2: Run RPC regression, schema/boundary gates, extension Jest, webview Jest, TypeScript compile, .NET build, package build, documentation checks, scoped lint, and `git diff --check` as applicable.**
- [x] **Step 3: Compare lint errors with the baseline and confirm no forbidden provider APIs were added.**
- [x] **Step 4: Confirm no files are staged or committed by this session and preserve unrelated changes.**
