# Reference PBIR Generator Phase 21 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a local deterministic reference PBIR generator prototype that consumes generation-manifest/v1 and proves the certified planning architecture can drive artifact creation without provider execution.

**Architecture:** Implement a Discovery-layer reference generator with versioned contracts, a safety gate, an interface, and a service. The service emits deterministic in-memory local file descriptors, lineage, metadata, and hashes only; it never creates deployable PBIR projects or invokes Microsoft Skills, providers, APIs, CLI, network, or deployment.

**Tech Stack:** .NET 8, C# records, xUnit, System.Text.Json, SHA-256 hashing.

---

### Task 1: Failing Reference Generator Contract Tests

**Files:**
- Create: `service-dotnet/tests/Discovery/ReferencePbirGenerationServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests for deterministic reference output, stable hashes, immutable lineage, metadata preservation, safety rejection, and boundary protection.

- [ ] **Step 2: Run focused tests to verify RED**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~ReferencePbirGenerationServiceTests`

Expected: FAIL because Phase 21 reference generator types do not exist yet.

### Task 2: Reference Generator Models

**Files:**
- Create: `service-dotnet/Services/Discovery/Models/ReferencePbirGenerationModels.cs`

- [ ] **Step 1: Implement versioned models**

Add reference-pbir-generator/v1, reference-generation-output/v1, generation options, generated file descriptors, output metadata, lineage, hashes, diagnostics, safety result, and state records.

- [ ] **Step 2: Keep models execution-free**

Do not add provider invocation, Microsoft API, CLI, deployment, network, or deployable PBIR project fields.

### Task 3: Safety Gate

**Files:**
- Create: `service-dotnet/Services/Discovery/ReferenceGenerationSafetyGate.cs`

- [ ] **Step 1: Implement validation**

Validate certification exists, manifest exists and is readyForGenerator, PBIR specification is ready for generation provider, dry-run is enabled, output is local-only, deployment is disabled, provider invocation is disabled, Microsoft API invocation is disabled, CLI invocation is disabled, and network access is disabled.

- [ ] **Step 2: Return deterministic reasons**

Return distinct, ordinal-sorted rejection reasons.

### Task 4: Reference PBIR Generation Service

**Files:**
- Create: `service-dotnet/Services/Discovery/ReferencePbirGenerationService.cs`

- [ ] **Step 1: Implement interface and service**

Add IReferenceGenerationProvider and ReferencePbirGenerationService. The service consumes GenerationManifestState, ArchitectureCertificationState, PbirGenerationSpecificationState, options, and a caller-supplied timestamp.

- [ ] **Step 2: Emit deterministic artifacts**

Create deterministic JSON and Markdown local file descriptors under reference-pbir-generator/v1 with SHA-256 hashes, file-set hash, input hash, immutable lineage, and metadata.

### Task 5: Documentation And Memory

**Files:**
- Create: `docs/current-state/reference-generator-state.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-06-26-reference-pbir-generator-phase21.md`

- [ ] **Step 1: Document current state**

Describe the reference generator architecture, safety model, deterministic generation guarantees, and remaining production gaps.

- [ ] **Step 2: Update repo memory**

Record Phase 21 deliverables, validation, and next recommended step.

### Task 6: Validation

- [ ] **Step 1: Run focused backend tests**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~ReferencePbirGenerationServiceTests`

Expected: PASS.

- [ ] **Step 2: Run required backend tests**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release`

Expected: PASS.

- [ ] **Step 3: Run required extension tests**

Run: `cd vscode-extension && npm test`

Expected: PASS.

- [ ] **Step 4: Run required extension compile**

Run: `cd vscode-extension && npm run compile`

Expected: PASS.
