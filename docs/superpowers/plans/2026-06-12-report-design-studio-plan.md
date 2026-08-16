# Report Design Studio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce Report Design Studio as a separate, artifact-first workflow with an explicit materialization gateway that feeds the existing analyzer workspace without bypassing validation.

**Architecture:** Build a new design-workflow lane that owns briefs, concepts, drafts, refinement proposals, and iteration history as first-class internal artifacts. Reuse shared repository snapshots, analyzable-surface vocabulary, analyzer registry concepts, and validated analyzer outputs through explicit adapters, with materialization as the only path into analyzable surface candidates.

**Tech Stack:** TypeScript, React webviews, VS Code extension host, .NET 8 backend, existing repository snapshot and analyzer infrastructure, Jest, xUnit

---

## Scope And Execution Rules

- This plan is for future implementation work.
- The plan assumes no public contract expansion unless later validation evidence supports it.
- The plan keeps generation providers optional.
- The plan keeps Story Assessment logic unchanged unless a later design explicitly approves changes.
- The plan treats Report Design Studio as a peer workflow to the analyzer workspace.

## File Map

### Planned Spec And Planning Files

- Existing: `docs/superpowers/specs/2026-06-12-report-design-studio-design.md`
- Existing: `docs/superpowers/plans/2026-06-12-report-design-studio-plan.md`

### Planned Extension Host Areas

- Create: `vscode-extension/src/design-studio/`
- Create: `vscode-extension/src/design-studio/contracts/`
- Create: `vscode-extension/src/design-studio/state/`
- Create: `vscode-extension/src/design-studio/materialization/`
- Create: `vscode-extension/src/design-studio/providers/`
- Create: `vscode-extension/src/design-studio/navigation/`
- Modify: `vscode-extension/src/extension.ts`
- Modify: existing shared host registration and webview bootstrap files that currently register workspace entry points

Responsibilities:

- studio command registration
- studio persistence coordination
- materialization orchestration
- analyzer handoff
- provider adapter registration
- deep-link round-trip mapping for materialized candidates

### Planned Webview Areas

- Create: `vscode-extension/webview-src/design-studio/`
- Create: `vscode-extension/webview-src/design-studio/components/`
- Create: `vscode-extension/webview-src/design-studio/state/`
- Create: `vscode-extension/webview-src/design-studio/protocol/`
- Create: `vscode-extension/webview-src/design-studio/views/`

Responsibilities:

- separate design workflow UI
- brief, concept, draft, refinement, and compare views
- explicit materialization and approval checkpoints
- provider provenance presentation

### Planned Shared Contract Areas

- Create: `vscode-extension/src/design-studio/contracts/designStudioModels.ts`
- Create: `vscode-extension/src/design-studio/contracts/designStudioProtocol.ts`
- Create: `vscode-extension/src/design-studio/contracts/designStudioNavigation.ts`

Responsibilities:

- internal artifact models
- studio host/webview protocol
- materialization request and response contracts
- design-to-analyzer handoff contracts

### Planned Backend Areas

- Create: `service-dotnet/Services/DesignStudio/`
- Create: `service-dotnet/Services/DesignStudio/Models/`
- Create: `service-dotnet/Services/DesignStudio/Materialization/`
- Create: `service-dotnet/Services/DesignStudio/Providers/`
- Modify: existing analyzer discovery or shared service registration files only where the new workflow must register shared infrastructure consumers

Responsibilities:

- internal artifact persistence or validation helpers where backend support is needed
- materialization adapters that derive analyzable surface candidates
- provider-neutral backend-side seams for future provider orchestration
- validation-only export support for design artifacts if needed later

### Planned Tests

- Create: `vscode-extension/tests/design-studio/`
- Create: `vscode-extension/webview-src/design-studio/__tests__/`
- Create: `service-dotnet/tests/DesignStudio/`

Responsibilities:

- workflow-boundary coverage
- trust-boundary enforcement coverage
- materialization boundary coverage
- provider optionality coverage
- analyzer handoff regression coverage

## Rollout Strategy

Implementation should proceed in five phases aligned to the design.

### Phase 1: Design Briefs

Outcome:

- first-class Design Brief workflow with persistence, validation, and identity

### Phase 2: Concept Studio

Outcome:

- concept-only workflow for page, KPI, chapter, navigation, and analytical-flow design

### Phase 3: Draft Studio

Outcome:

- isolated non-production draft artifacts with provider-neutral draft generation seams

### Phase 4: Refinement Studio

Outcome:

- analyzer-output-driven refinement proposals that remain advisory

### Phase 5: Closed-Loop Optimization

Outcome:

- explicit materialization gateway and analyze-improve-compare loop

## Task 1: Establish Studio Boundaries And Internal Contracts

**Files:**
- Create: `vscode-extension/src/design-studio/contracts/designStudioModels.ts`
- Create: `vscode-extension/src/design-studio/contracts/designStudioProtocol.ts`
- Create: `vscode-extension/src/design-studio/contracts/designStudioNavigation.ts`
- Create: `service-dotnet/Services/DesignStudio/Models/`
- Test: `vscode-extension/tests/design-studio/designStudioContracts.test.ts`
- Test: `service-dotnet/tests/DesignStudio/DesignStudioModelBoundaryTests.cs`

- [ ] Define the internal artifact vocabulary for:
  - `DesignBrief`
  - `ReportConcept`
  - `PageConcept`
  - `NavigationConcept`
  - `KpiHierarchyConcept`
  - `DraftReportArtifact`
  - `DraftPageArtifact`
  - `RefinementProposal`
  - `MaterializationRequest`
  - `MaterializedSurfaceCandidate`
  - `DesignIterationRecord`

- [ ] Define lifecycle and approval vocabularies that are separate from analyzer promotion state.

- [ ] Define studio host/webview protocol messages for:
  - load studio state
  - save artifact
  - propose artifact
  - approve artifact
  - request materialization
  - compare iterations
  - open analyzer handoff

- [ ] Add boundary tests that assert:
  - studio artifacts are internal-only
  - analyzable surfaces remain derived objects
  - studio contracts do not modify existing public score payloads

- [ ] Run focused tests:
  - `cd vscode-extension && npm test -- design-studio`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudioModelBoundaryTests`

- [ ] Commit:

```bash
git add vscode-extension/src/design-studio/contracts service-dotnet/Services/DesignStudio/Models service-dotnet/tests/DesignStudio vscode-extension/tests/design-studio
git commit -m "feat(design-studio): define internal studio contracts"
```

## Task 2: Implement Design Brief Foundation

**Files:**
- Create: `vscode-extension/src/design-studio/state/designBriefStore.ts`
- Create: `vscode-extension/webview-src/design-studio/views/DesignBriefView.tsx`
- Create: `vscode-extension/webview-src/design-studio/state/designBriefReducer.ts`
- Test: `vscode-extension/tests/design-studio/designBriefStore.test.ts`
- Test: `vscode-extension/webview-src/design-studio/__tests__/DesignBriefView.test.tsx`

- [ ] Implement the Design Brief model with required fields:
  - audience
  - business objective
  - key decisions
  - primary KPIs
  - dimensions
  - intended story
  - success criteria
  - report type
  - navigation expectations

- [ ] Add validation rules that prevent concept generation from proceeding without a valid brief.

- [ ] Add persistence and versioning for the brief inside studio-owned state.

- [ ] Add tests for:
  - required field enforcement
  - version persistence
  - explicit approval state transitions

- [ ] Run focused tests:
  - `cd vscode-extension && npm test -- designBrief`

- [ ] Commit:

```bash
git add vscode-extension/src/design-studio/state vscode-extension/webview-src/design-studio
git commit -m "feat(design-studio): add design brief foundation"
```

## Task 3: Implement Concept Studio Artifact Layer

**Files:**
- Create: `vscode-extension/src/design-studio/state/conceptStore.ts`
- Create: `vscode-extension/webview-src/design-studio/views/ConceptStudioView.tsx`
- Create: `vscode-extension/webview-src/design-studio/components/ConceptComparison.tsx`
- Test: `vscode-extension/tests/design-studio/conceptStore.test.ts`
- Test: `vscode-extension/webview-src/design-studio/__tests__/ConceptStudioView.test.tsx`

- [ ] Add concept artifact models for:
  - chapter map
  - page recommendations
  - KPI hierarchy
  - navigation structure
  - analytical flow
  - alternate concepts

- [ ] Ensure Concept Studio emits concept-only outputs and cannot create PBIR assets directly.

- [ ] Add comparison support for alternate concepts and explicit user approval of a chosen concept baseline.

- [ ] Add tests for:
  - concept generation gating on approved brief
  - alternate-concept comparison
  - no direct materialization without explicit request

- [ ] Run focused tests:
  - `cd vscode-extension && npm test -- conceptStudio`

- [ ] Commit:

```bash
git add vscode-extension/src/design-studio/state vscode-extension/webview-src/design-studio
git commit -m "feat(design-studio): add concept studio artifact workflow"
```

## Task 4: Implement Draft Studio Artifact Layer

**Files:**
- Create: `vscode-extension/src/design-studio/state/draftStore.ts`
- Create: `vscode-extension/src/design-studio/providers/draftProviderAdapter.ts`
- Create: `vscode-extension/webview-src/design-studio/views/DraftStudioView.tsx`
- Test: `vscode-extension/tests/design-studio/draftStore.test.ts`
- Test: `vscode-extension/tests/design-studio/draftProviderAdapter.test.ts`

- [ ] Add draft artifact models for:
  - draft report structures
  - draft page structures
  - draft KPI layouts
  - draft navigation frameworks

- [ ] Ensure drafts remain isolated, reviewable, and non-production.

- [ ] Add provider-neutral draft adapter seams that can later support Microsoft or non-Microsoft providers.

- [ ] Persist provider provenance alongside draft outputs.

- [ ] Add tests for:
  - isolated draft status
  - provider provenance capture
  - no direct deployment or direct mutation paths

- [ ] Run focused tests:
  - `cd vscode-extension && npm test -- draftStudio`

- [ ] Commit:

```bash
git add vscode-extension/src/design-studio/providers vscode-extension/src/design-studio/state vscode-extension/webview-src/design-studio
git commit -m "feat(design-studio): add draft studio foundation"
```

## Task 5: Implement Provider-Neutral Capability Registry

**Files:**
- Create: `vscode-extension/src/design-studio/providers/designProviderRegistry.ts`
- Create: `service-dotnet/Services/DesignStudio/Providers/IDesignStudioProvider.cs`
- Create: `service-dotnet/Services/DesignStudio/Providers/ProviderCapabilityModels.cs`
- Test: `vscode-extension/tests/design-studio/designProviderRegistry.test.ts`
- Test: `service-dotnet/tests/DesignStudio/DesignStudioProviderBoundaryTests.cs`

- [ ] Define capability metadata for provider classes:
  - design assistance
  - generation assistance
  - screenshot iteration assistance
  - semantic-model-aware assistance

- [ ] Ensure providers are optional and discoverable rather than required.

- [ ] Add failure and degradation semantics so provider absence does not break core workflow operation.

- [ ] Add tests for:
  - optional provider registration
  - capability discovery
  - graceful provider absence
  - provider inability to bypass approval or validation

- [ ] Run focused tests:
  - `cd vscode-extension && npm test -- designProviderRegistry`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudioProviderBoundaryTests`

- [ ] Commit:

```bash
git add vscode-extension/src/design-studio/providers service-dotnet/Services/DesignStudio/Providers service-dotnet/tests/DesignStudio
git commit -m "feat(design-studio): add provider-neutral capability registry"
```

## Task 6: Implement Refinement Studio Analyzer Consumption Layer

**Files:**
- Create: `vscode-extension/src/design-studio/state/refinementStore.ts`
- Create: `vscode-extension/src/design-studio/navigation/designArtifactBacklinkResolver.ts`
- Create: `vscode-extension/webview-src/design-studio/views/RefinementStudioView.tsx`
- Test: `vscode-extension/tests/design-studio/refinementStore.test.ts`
- Test: `vscode-extension/tests/design-studio/designArtifactBacklinkResolver.test.ts`

- [ ] Add ingestion adapters for:
  - Story Assessment
  - Guided Story Improvements
  - Issues
  - Fix Plan
  - Cross-Page Narrative

- [ ] Map validated analyzer outputs back to source design artifacts through explicit linkage records.

- [ ] Ensure refinement outputs remain advisory proposals and alternatives, not direct edits.

- [ ] Add tests for:
  - analyzer-output ingestion
  - refinement-proposal provenance
  - no direct report mutation

- [ ] Run focused tests:
  - `cd vscode-extension && npm test -- refinementStudio`

- [ ] Commit:

```bash
git add vscode-extension/src/design-studio/state vscode-extension/src/design-studio/navigation vscode-extension/webview-src/design-studio
git commit -m "feat(design-studio): add refinement studio analyzer ingestion"
```

## Task 7: Implement Materialization Gateway

**Files:**
- Create: `vscode-extension/src/design-studio/materialization/materializationCoordinator.ts`
- Create: `vscode-extension/src/design-studio/materialization/materializationMapper.ts`
- Create: `service-dotnet/Services/DesignStudio/Materialization/`
- Test: `vscode-extension/tests/design-studio/materializationCoordinator.test.ts`
- Test: `service-dotnet/tests/DesignStudio/DesignStudioMaterializationTests.cs`

- [ ] Implement explicit materialization requests that convert approved design artifacts into analyzable surface candidates.

- [ ] Add support for initial materialization modes:
  - concept-to-structure preview
  - draft-to-surface candidate
  - refinement-proposal-to-candidate comparison

- [ ] Reuse shared analyzable-surface vocabulary and analyzer registry selection without redefining analyzer ownership.

- [ ] Add provenance trace and diagnostics for each materialized candidate.

- [ ] Add tests for:
  - no implicit materialization
  - analyzable surfaces as derived objects only
  - explicit analyzer handoff payload shape
  - graceful failure when target surface family is unsupported

- [ ] Run focused tests:
  - `cd vscode-extension && npm test -- materialization`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudioMaterializationTests`

- [ ] Commit:

```bash
git add vscode-extension/src/design-studio/materialization service-dotnet/Services/DesignStudio/Materialization service-dotnet/tests/DesignStudio
git commit -m "feat(design-studio): add explicit materialization gateway"
```

## Task 8: Integrate Analyzer Handoff Without Expanding Analyzer Ownership

**Files:**
- Modify: `vscode-extension/src/extension.ts`
- Modify: existing workspace-entry and analyzer-launch registration files
- Create: `vscode-extension/src/design-studio/materialization/analyzerHandoffService.ts`
- Test: `vscode-extension/tests/design-studio/analyzerHandoffService.test.ts`

- [ ] Add commands and workflow seams that open Analyzer Workspace from a materialized candidate rather than embedding design UI inside the analyzer workspace.

- [ ] Reuse existing analyzer registry selection and analyzable surface discovery patterns where possible.

- [ ] Ensure analyzer results return to Design Studio only through explicit refinement ingestion, not hidden shared mutable state.

- [ ] Add tests for:
  - separate workflow launch
  - handoff payload integrity
  - no score-panel contract widening

- [ ] Run focused tests:
  - `cd vscode-extension && npm test -- analyzerHandoff`

- [ ] Commit:

```bash
git add vscode-extension/src/extension.ts vscode-extension/src/design-studio/materialization
git commit -m "feat(design-studio): integrate analyzer handoff as peer workflow"
```

## Task 9: Implement Closed-Loop Comparison And Approval Workflow

**Files:**
- Create: `vscode-extension/src/design-studio/state/iterationStore.ts`
- Create: `vscode-extension/webview-src/design-studio/views/ClosedLoopView.tsx`
- Create: `vscode-extension/webview-src/design-studio/components/IterationComparison.tsx`
- Test: `vscode-extension/tests/design-studio/iterationStore.test.ts`
- Test: `vscode-extension/webview-src/design-studio/__tests__/ClosedLoopView.test.tsx`

- [ ] Persist iteration records linking:
  - source artifact version
  - materialized candidate
  - analyzer outputs
  - refinement proposals
  - explicit approvals

- [ ] Add compare support for:
  - concept changes
  - draft changes
  - analyzer output changes
  - recommendation changes

- [ ] Ensure the approval workflow distinguishes:
  - design approval
  - validation approval
  - future deployment approval

- [ ] Add tests for:
  - iteration lineage
  - approval-stage separation
  - no hidden auto-optimization loop

- [ ] Run focused tests:
  - `cd vscode-extension && npm test -- closedLoop`

- [ ] Commit:

```bash
git add vscode-extension/src/design-studio/state vscode-extension/webview-src/design-studio
git commit -m "feat(design-studio): add closed-loop comparison workflow"
```

## Task 10: Enforce Trust Boundary And Regression Guardrails

**Files:**
- Create: `vscode-extension/tests/design-studio/trustBoundary.test.ts`
- Create: `service-dotnet/tests/DesignStudio/DesignStudioTrustBoundaryTests.cs`
- Modify: documentation and troubleshooting paths that explain workflow posture

- [ ] Add regression coverage that asserts the studio cannot:
  - silently generate production report assets
  - silently modify reports
  - directly deploy outputs
  - bypass Story Assessment, Guided Story Improvements, Issues, Fix Plan, or validation

- [ ] Add regression coverage that asserts analyzer workspace ownership remains intact.

- [ ] Document trust-boundary behavior for future contributors and reviewers.

- [ ] Run focused tests:
  - `cd vscode-extension && npm test -- trustBoundary`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudioTrustBoundaryTests`

- [ ] Commit:

```bash
git add vscode-extension/tests/design-studio service-dotnet/tests/DesignStudio docs
git commit -m "test(design-studio): enforce trust boundary and ownership guardrails"
```

## Task 11: Validation Strategy Execution

**Files:**
- Create: `docs/story-assessment/` or future design-validation docs as approved
- Create: validation corpus notes for design artifacts and materialized candidates
- Modify: repo memory and validation workflow docs as needed

- [ ] Define design-validation fixtures for:
  - brief quality
  - concept quality
  - draft isolation
  - refinement usefulness

- [ ] Define user-validation scenarios for:
  - intent capture
  - concept selection
  - design-versus-validation comprehension
  - closed-loop iteration comprehension

- [ ] Define provider-validation scenarios for:
  - provider absence
  - provider provenance
  - provider disagreement
  - mixed-provider artifact generation

- [ ] Define architecture-validation checks for:
  - shared infrastructure reuse
  - workflow separation
  - materialization explicitness
  - analyzer authority preservation

- [ ] Commit:

```bash
git add docs .agent-memory
git commit -m "docs(design-studio): add validation strategy artifacts"
```

## Task 12: Full Regression And Release Readiness Review

**Files:**
- Review: all files touched in Tasks 1-11
- Modify: release-facing docs only if and when the feature scope is approved for exposure

- [ ] Run full extension validation:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`

- [ ] Run full backend validation:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`

- [ ] Run focused manual workflow review for:
  - separate Design Studio launch
  - brief-to-concept flow
  - draft-to-materialization flow
  - analyzer handoff
  - refinement return path

- [ ] Confirm that no public score or Story Assessment contracts widened unintentionally.

- [ ] Commit:

```bash
git add .
git commit -m "chore(design-studio): finalize implementation slice"
```

## Provider Integration Strategy

Use a layered provider approach:

1. Core studio workflow operates with zero providers.
2. Provider capability registry advertises optional assistance.
3. Provider outputs always include provenance.
4. Provider outputs are stored as advisory inputs or draft proposals, never authoritative truth.
5. Provider-assisted drafts enter validation only through explicit materialization.

## Trust-Boundary Enforcement Strategy

Enforce the following invariants throughout implementation:

- no direct provider-to-report mutation path
- no direct draft-to-production path
- no silent analyzer invocation from ordinary design editing
- no analyzer result writes that implicitly mutate source design artifacts
- no bypass of deterministic preview/apply/rollback for actual report changes

## Regression Strategy

Each phase must run regression checks for:

- analyzable surface and analyzer separation
- score-panel contract stability
- Story Assessment public contract non-expansion
- repository snapshot reuse rather than duplicate scan paths
- design workflow separation from validation workflow

## Self-Review Coverage Check

Spec coverage to verify during execution:

- separate peer workflow
- first-class design artifacts
- explicit materialization boundary
- Design Brief architecture
- Concept Studio architecture
- Draft Studio architecture
- Refinement Studio architecture
- closed-loop optimization
- provider neutrality
- trust-boundary enforcement

No task should be considered complete until at least one explicit test or review step verifies the corresponding boundary.
