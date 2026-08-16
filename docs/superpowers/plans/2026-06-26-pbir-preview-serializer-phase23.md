# PBIR Preview Serializer Phase 23 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a local-only PBIR preview serializer boundary that consumes canonical PBIR IR and emits deterministic human-reviewable preview artifacts without deployable PBIR generation.

**Architecture:** The preview serializer sits downstream from pbir-ir/v1 and pbir-serializer-request/v1. It validates the existing request contract, rejects execution or deployable output intent, then emits in-memory Markdown and JSON preview artifact descriptors plus a preview manifest with hashes, lineage, warnings, and unsupported sections.

**Tech Stack:** .NET 8, C# records, xUnit, deterministic System.Text.Json serialization, SHA-256 hashes.

---

### Task 1: Preview Serializer Contract Tests

**Files:**
- Create: `service-dotnet/tests/Discovery/PbirPreviewSerializerServiceTests.cs`
- Use: `service-dotnet/tests/Discovery/PbirIntermediateRepresentationServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create xUnit tests proving deterministic Markdown and JSON preview artifacts, stable output hashes, required summaries, preview manifest lineage, safety rejections, and boundary protection.

- [ ] **Step 2: Run focused red gate**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirPreviewSerializerServiceTests`

Expected: FAIL because Phase 23 preview serializer types do not exist yet.

### Task 2: Preview Models

**Files:**
- Create: `service-dotnet/Services/Discovery/Models/PbirPreviewSerializerModels.cs`

- [ ] **Step 1: Add pbir-preview-artifact/v1 and pbir-preview-manifest/v1 model records**

Include preview output types, options, safety results, diagnostics, generated file descriptors, manifest source references, hashes, lineage, warnings, unsupported sections, and state/readiness records.

- [ ] **Step 2: Keep contracts local-only**

Ensure the model has no deployable PBIR filenames, provider execution contracts, API/CLI request contracts, or deployment authority.

### Task 3: Safety Gate and Validator

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirPreviewSerializerSafetyGate.cs`
- Create: `service-dotnet/Services/Discovery/PbirPreviewSerializerValidator.cs`

- [ ] **Step 1: Implement fail-closed safety gate**

Reject missing/incomplete IR, invalid serializer request schema or hash references, deployable output requests, report.json, definition.pbir, model.bim, TMDL, Power BI project files, provider invocation, Microsoft API invocation, CLI invocation, deployment, Microsoft Skills execution, and non-local output paths.

- [ ] **Step 2: Implement validator**

Validate IR readiness, serializer request schema, preview output types, lineage integrity, file/manifest hash stability, generated preview files, warnings, unsupported sections, and boundary protection.

### Task 4: Preview Serializer Service

**Files:**
- Create: `service-dotnet/Services/Discovery/PbirPreviewSerializerService.cs`

- [ ] **Step 1: Implement local preview generation**

Consume `PbirIntermediateRepresentationState` and `PbirSerializerRequest`, validate safety first, and return rejected state with no generated files if unsafe.

- [ ] **Step 2: Emit deterministic preview files**

Generate only:
- `pbir-preview-artifact/v1/report-preview.md`
- `pbir-preview-artifact/v1/report-preview.json`

Include page, visual/page layout, semantic binding, and navigation summaries. Compute deterministic file hashes, file-set hash, manifest hash, and output hash.

### Task 5: Green Gate and Documentation

**Files:**
- Modify: `docs/current-state/pbir-intermediate-representation-state.md`
- Modify: `docs/current-state/reference-generator-state.md`
- Modify: `docs/current-state/generation-manifest-framework-state.md`
- Modify: `docs/current-state/architecture-gap-analysis.md`
- Create: `docs/current-state/pbir-preview-serializer-state.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-06-26-143218-pbir-preview-serializer-phase23.md`

- [ ] **Step 1: Run focused green gate**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirPreviewSerializerServiceTests`

Expected: PASS.

- [ ] **Step 2: Update current-state docs and memory**

Document pbir-preview-artifact/v1, pbir-preview-manifest/v1, local preview behavior, forbidden deployable outputs, and the remaining deployable PBIR serialization gap.

### Task 6: Required Validation

**Files:**
- Validation only.

- [ ] **Step 1: Run backend suite**

Run: `dotnet test service-dotnet/tests/Tests.csproj -c Release`

Expected: PASS.

- [ ] **Step 2: Run extension tests**

Run: `cd vscode-extension && npm test`

Expected: PASS.

- [ ] **Step 3: Run extension compile**

Run: `cd vscode-extension && npm run compile`

Expected: PASS.
