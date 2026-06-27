# PBIR Local Writer Boundary Phase 24 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a dry-run-only PBIR local artifact writer safety boundary that plans local artifact output without writing deployable PBIR files.

**Architecture:** Add backend discovery-layer contracts for pbir-local-writer/v1, pbir-local-write-request/v1, and pbir-local-write-manifest/v1. The boundary service consumes pbir-ir/v1 plus pbir-preview-manifest/v1, validates a write request through a fail-closed safety gate, and returns deterministic planned paths, hashes, overwrite risk, rollback plan, warnings, and rejected artifact inventory without filesystem mutation.

**Tech Stack:** .NET 8, C# records, deterministic System.Text.Json serialization, SHA-256 hashing, xUnit.

---

### Task 1: Failing Boundary Coverage

**Files:**
- Create: `service-dotnet/tests/Discovery/PbirLocalArtifactWriterBoundaryServiceTests.cs`

- [x] **Step 1: Write tests for deterministic dry-run manifests, overwrite detection, rollback planning, safety rejection, and no execution surface.**

- [x] **Step 2: Run focused tests to verify they fail because Phase 24 types are missing.**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirLocalArtifactWriterBoundaryServiceTests`

Expected: FAIL with missing type/build errors.

### Task 2: Models

**Files:**
- Create: `service-dotnet/Services/Discovery/Models/PbirLocalArtifactWriterModels.cs`

- [ ] **Step 1: Add pbir-local-writer/v1, pbir-local-write-request/v1, and pbir-local-write-manifest/v1 contracts.**

- [ ] **Step 2: Add request, manifest, planned file, overwrite risk, rollback, diagnostics, safety, and state records.**

### Task 3: Safety Gate

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirLocalArtifactWriterSafetyGate.cs`

- [ ] **Step 1: Reject deployable PBIR artifacts, provider/API/CLI/Microsoft Skills/deployment requests, non-local roots, missing dry-run, and unsafe overwrite policy.**

- [ ] **Step 2: Keep rejected requests manifest-free and file-write-free.**

### Task 4: Boundary Service

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirLocalArtifactWriterBoundaryService.cs`

- [ ] **Step 1: Consume pbir-preview-manifest/v1 and pbir-ir/v1.**

- [ ] **Step 2: Produce deterministic local write manifest with planned output files, paths, content hashes, lineage, overwrite risk, rollback plan, warnings, and rejected artifacts.**

- [ ] **Step 3: Use caller-supplied existing local paths only for overwrite risk detection; do not write or delete files.**

### Task 5: Docs And Memory

**Files:**
- Create: `docs/current-state/pbir-local-writer-boundary-state.md`
- Modify: `docs/current-state/pbir-preview-serializer-state.md`
- Modify: `docs/current-state/architecture-gap-analysis.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-06-26-pbir-local-writer-boundary-phase24.md`

- [ ] **Step 1: Document the boundary contracts, forbidden deployable artifact policy, overwrite/rollback safety model, and remaining real writer gap.**

### Task 6: Validation

**Files:**
- No source edits.

- [ ] **Step 1: Run focused backend test.**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirLocalArtifactWriterBoundaryServiceTests`

- [ ] **Step 2: Run required backend and extension validation.**

Run:
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- `cd vscode-extension && npm test`
- `cd vscode-extension && npm run compile`
