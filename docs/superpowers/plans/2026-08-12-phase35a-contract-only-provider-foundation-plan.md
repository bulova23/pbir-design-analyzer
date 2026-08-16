# Phase 35A — Contract-Only Provider Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a deterministic backend-only governance contract package for future generation providers without adding any executable provider path.

**Architecture:** Add a focused `Phase35A` contract namespace under `Services/Discovery` with immutable records, closed enums, pure projection/validation/readiness/lifecycle/hash helpers, and a static provider catalog. Consume the existing governed `GenerationProviderRequest`; do not modify scoring, RPC, or existing runtime-provider abstractions.

**Tech Stack:** .NET 8, C# records/enums, `System.Text.Json`, SHA-256, xUnit.

---

### Task 1: Add red contract tests

**Files:**
- Create: `service-dotnet/tests/Discovery/Phase35AContractFoundationTests.cs`
- Create: `service-dotnet/tests/Discovery/Phase35ABoundaryTests.cs`

- [ ] Write tests first for schema/version rejection, closed enum serialization, stable provider identity, projection determinism, authorization default denial, readiness fail-closed, lifecycle transitions, hashing, lineage, retry classification, redaction, quarantine, failure validation, fake isolation, and unknown-value rejection.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~Phase35A` and verify the tests fail because the new contract types do not yet exist.

### Task 2: Add immutable Phase 35A contract model

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35A/Phase35AContracts.cs`

- [ ] Add schema constants, closed enums, immutable records, explicit default denied policy, and contract exceptions for provider profiles, requests, authorization, policy, readiness, lifecycle, receipts, results, artifacts, failures, hashes, lineage, retries, redaction, and quarantine.
- [ ] Keep all records provider-neutral and omit command, endpoint, credential, process, shell, MCP, or filesystem execution fields.

### Task 3: Add canonicalization and validation

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35A/Phase35ACanonicalJson.cs`
- Create: `service-dotnet/Services/Discovery/Phase35A/Phase35AContractValidator.cs`

- [ ] Implement deterministic camelCase JSON with string-only enum serialization and lowercase SHA-256.
- [ ] Implement pure validation for versions, identities, capabilities, hashes, references, enum values, artifact/result relationships, policy, and redaction/quarantine constraints.

### Task 4: Add authoritative projection, readiness, and lifecycle

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35A/Phase35ARequestProjector.cs`
- Create: `service-dotnet/Services/Discovery/Phase35A/Phase35AReadinessEvaluator.cs`
- Create: `service-dotnet/Services/Discovery/Phase35A/Phase35ALifecycle.cs`

- [ ] Project only from existing `GenerationProviderFrameworkState` and preserve upstream references and hashes.
- [ ] Require explicit authorization, provider execution classification, policy match, capability match, and lineage integrity before readiness can be ready; current catalog must remain unavailable.
- [ ] Implement a deterministic state machine with rejected invalid transitions.

### Task 5: Add catalog and offline fake isolation

**Files:**
- Create: `service-dotnet/Services/Discovery/Phase35A/Phase35AProviderCatalog.cs`
- Modify: `service-dotnet/tests/Discovery/Phase35ABoundaryTests.cs`

- [ ] Register `powerbi-report-author@0.1.4`, Power BI Desktop, Power BI Modeling MCP, existing Skills metadata, and reference/materialization surfaces with explicit non-runtime classifications.
- [ ] Ensure catalog APIs return immutable metadata only and expose no executable provider interface or discovery probe.

### Task 6: Update authoritative documentation and memory

**Files:**
- Modify: `docs/ROADMAP.md`
- Modify: `docs/current-state/architecture-gap-analysis.md`
- Modify: `docs/current-state/generation-provider-framework-state.md`
- Modify: `docs/current-state/runtime-provider-framework-state.md`
- Modify: `docs/current-state/execution-provider-framework-state.md`
- Create: `docs/current-state/phase35a-contract-only-provider-foundation-state.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-08-12-phase35a-contract-only-provider-foundation.md`

- [ ] State that Phase 35A is contracts and deterministic governance only, Phase 35B+ is executable integration, and no runtime generation provider is available.
- [ ] Record the provider matrix, strict boundary, test/validation outcomes, and next handoff.

### Task 7: Validate without committing

- [ ] Run focused Phase 35A tests, full backend xUnit, RPC tests, extension tests, webview tests, compile/type-check, eight schema gates, scoped lint, architecture/boundary checks, whitespace/document checks, and `git diff --check` using repository-authoritative commands.
- [ ] Inspect `git status --short` and `git diff --stat`; confirm all Phase 35A changes remain uncommitted and unrelated dirty files are untouched.

