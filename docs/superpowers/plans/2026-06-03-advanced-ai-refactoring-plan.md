# Advanced AI Refactoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an advisory `Advanced AI Refactoring` layer that can generate grounded layout, KPI, storytelling, navigation, executive-readability, accessibility, and governance-alignment scenarios without changing deterministic execution authority.

**Architecture:** Keep `Issues`, `Remediation Queue`, `Fix Opportunity Engine`, and the `Deterministic Mutation Layer` intact. Insert an `AI Refactoring Proposals` layer above deterministic execution and below remediation-led user decision-making, with provider-agnostic orchestration, scenario validation, fallback behavior, and explicit `compilable` versus `advisoryOnly` classification.

**Tech Stack:** TypeScript, React, Jest, VS Code extension host/webview, existing `proposalEnrichment` architecture, existing score-panel payload/state contracts, existing deterministic fix pipeline, existing findings/remediation presentation builders

---

## Scope Guardrails

Implement only advisory refactoring support.

Do:

- keep normalized findings authoritative
- keep remediation as the solution-intent layer
- keep proposal output advisory
- keep deterministic execution unchanged
- keep PBIR as the first implementation surface
- design contracts for future Fabric App reuse

Do not:

- generate executable mutations from models
- create a second remediation system
- create direct mutation paths
- add DAX or report generation
- implement Fabric Apps in this phase
- build report design studio behavior

## File Map

### Core Contracts

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - add refactoring proposal contracts, scenario options, tradeoffs, evidence links, and compilation classification
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringProposalTypes.ts`
  - keep Phase 4 contracts focused if `scorePanel.ts` would become too large

### Advisory Refactoring Orchestration

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringContextBuilder.ts`
  - build bounded grounded context from findings, remediation, page purpose, page story, visual metadata, and deterministic support signals
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringScenarioBuilder.ts`
  - shape advisory scenario requests and merge provider/fallback outputs into stable proposal structures
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringProvider.ts`
  - provider abstraction for advisory refactoring only
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringValidators.ts`
  - validate evidence fidelity, option diversity, and advisory-only enforcement
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringFallbacks.ts`
  - deterministic fallback scenario wording
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringOrchestrator.ts`
  - orchestrate context, enrichers, provider calls, validation, fallback, and provenance
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringTelemetry.ts`
  - timing, validation, fallback, rejection, and advisory-only metrics

### Domain Enrichers

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/layoutRefactoringEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/kpiHierarchyEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/storytellingRefactoringEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/navigationRefactoringEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/executiveExperienceEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/accessibilityAlignmentEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/governanceAlignmentEnricher.ts`

### Compilation Classification

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringCompilationClassifier.ts`
  - classify proposal sections as `compilable` or `advisoryOnly`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringDeterministicHints.ts`
  - map supported advisory concepts to existing deterministic categories without generating mutations

### Host And Payload Integration

- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
  - thread proposal data into the score-panel payload
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
  - host-side orchestration, feature gating, and fallback behavior
- Modify: `vscode-extension/src/analyzer/score/fixPlan.ts`
  - allow bounded remediation clustering metadata for refactoring proposal attachment
- Modify: `vscode-extension/src/analyzer/score/overviewSummary.ts`
  - add overview summary hooks for scenario-ready design themes if needed
- Modify: `vscode-extension/src/analyzer/score/personaPresentation.ts`
  - support executive-first scenario emphasis without changing findings semantics

### Webview Integration

- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
  - render `AI Refactoring Proposals`, scenario comparisons, confidence, tradeoffs, and compilation labels
- Create: `vscode-extension/webview-src/analyzer-score/refactoringProposals.ts`
  - presentation helpers for labels, grouping, and option badges
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
  - style option comparison blocks and advisory/deterministic distinctions

### Tests

- Create: `vscode-extension/src/test/refactoringContextBuilder.test.ts`
- Create: `vscode-extension/src/test/refactoringScenarioBuilder.test.ts`
- Create: `vscode-extension/src/test/refactoringValidators.test.ts`
- Create: `vscode-extension/src/test/refactoringCompilationClassifier.test.ts`
- Create: `vscode-extension/src/test/refactoringOrchestrator.test.ts`
- Create: `vscode-extension/src/test/refactoringEnrichers.test.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify: `vscode-extension/src/test/fixPlan.test.ts`
- Modify: `vscode-extension/src/test/personaPresentation.test.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
- Create: `vscode-extension/webview-src/analyzer-score/refactoringProposals.test.ts`

### Docs And Durable Memory

- Modify: `docs/ROADMAP.md`
  - mark Phase 4 implementation status and ordering once the work ships
- Modify: `docs/CHANGELOG.md`
  - document advisory-only refactoring support when released
- Update durable memory during implementation:
  - `.agent-memory/current-focus.md`
  - `.agent-memory/session-summaries.md`
  - timestamped `.agent-memory/sessions/*`

## Dependency Map

### Existing Foundations To Reuse

- `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - current findings, fix-plan, enrichment, and deterministic fix contracts
- `vscode-extension/src/analyzer/proposalEnrichment/*`
  - provider, validation, fallback, and orchestration patterns from Phase 3
- `vscode-extension/src/analyzer/score/fixPlan.ts`
  - remediation queue shaping
- `vscode-extension/src/analyzer/score/overviewSummary.ts`
  - overview summarization seams
- `vscode-extension/src/analyzer/score/personaPresentation.ts`
  - executive/consultant presentation emphasis
- `vscode-extension/src/views/scoreResultPayload.ts`
  - payload shaping seam
- `vscode-extension/src/views/PbirScorePanel.ts`
  - host orchestration seam
- `vscode-extension/webview-src/analyzer-score/App.tsx`
  - shared workspace UI seam
- `vscode-extension/src/analyzer/fixes/*`
  - deterministic categories and trust boundary references

### Adjacent Roadmap Dependencies

- Phase 2 hardening
  - preserve the grouped preview/apply/rollback trust loop unchanged
- Phase 3 proposal enrichment
  - reuse advisory provider, validation, and fallback patterns instead of creating a new AI stack
- Fabric Apps Analytics Review
  - align contracts to future cross-surface reuse but do not block PBIR-first implementation on Fabric surface work

## Major Workstreams

### Workstream 1: Lock The Trust Boundary Into Contracts

**Outcome:** Phase 4 has explicit advisory contracts and no path to executable authority leakage.

### Task 1: Extend score-panel contracts for refactoring proposals

**Files:**

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringProposalTypes.ts`
- Test: `vscode-extension/src/test/refactoringCompilationClassifier.test.ts`

- [ ] Add failing tests for:
  - `RefactoringProposal`
  - `RefactoringScenario`
  - `RefactoringScenarioOption`
  - `RefactoringTradeoff`
  - `RefactoringEvidenceLink`
  - `RefactoringCompilationHint`
  - `RefactoringValidationResult`
- [ ] Add failing tests proving proposal contracts can express:
  - multiple options
  - evidence links
  - confidence
  - `compilable` versus `advisoryOnly`
- [ ] Run `cd vscode-extension && npx jest --runInBand src/test/refactoringCompilationClassifier.test.ts` and confirm failure.
- [ ] Implement contract additions with comments stating:
  - refactoring proposals are advisory
  - fix opportunities remain the only executable layer
  - compilation hints are not mutation authority
- [ ] Re-run the focused contract test and confirm it passes.

### Task 2: Add compilation classification types and tests

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringCompilationClassifier.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringDeterministicHints.ts`
- Test: `vscode-extension/src/test/refactoringCompilationClassifier.test.ts`

- [ ] Add failing tests for:
  - supported layout guidance classifies as partially `compilable`
  - unsupported storytelling-only guidance classifies as `advisoryOnly`
  - mixed-option scenarios preserve per-option classification
  - compilation hints never include concrete mutations
- [ ] Run `cd vscode-extension && npx jest --runInBand src/test/refactoringCompilationClassifier.test.ts` and confirm failure.
- [ ] Implement a classifier that maps advisory concepts to existing deterministic categories only:
  - `alignment`
  - `spacing`
  - `grid`
  - `title`
  - `navigation`
- [ ] Re-run the focused classifier tests and confirm they pass.

### Workstream 2: Build Grounded Refactoring Context

**Outcome:** Providers receive bounded, evidence-rich context and nothing execution-sensitive.

### Task 3: Add context-builder tests first

**Files:**

- Create: `vscode-extension/src/test/refactoringContextBuilder.test.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringContextBuilder.ts`

- [ ] Add failing tests proving the context builder includes:
  - normalized findings
  - remediation items
  - page purpose analysis
  - page story summaries
  - visual metadata
  - cross-page cues when present
  - deterministic support hints
- [ ] Add failing tests proving the context builder excludes:
  - raw file contents
  - mutation plans
  - rollback plans
  - apply-session history
  - score rewrites
- [ ] Run `cd vscode-extension && npx jest --runInBand src/test/refactoringContextBuilder.test.ts` and confirm failure.

### Task 4: Implement the grounded context builder

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringContextBuilder.ts`
- Test: `vscode-extension/src/test/refactoringContextBuilder.test.ts`

- [ ] Implement deterministic context shaping for:
  - layout
  - KPI hierarchy
  - storytelling
  - navigation
  - executive experience
  - accessibility
  - governance alignment
- [ ] Keep context bounded, serializable, and explicit about supported deterministic categories.
- [ ] Re-run the focused context-builder tests and confirm they pass.

### Workstream 3: Add Provider, Scenario Builder, And Validators

**Outcome:** Phase 4 produces bounded scenario options with validation, fallback, and provenance.

### Task 5: Add provider abstraction and scenario-builder tests

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringProvider.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringScenarioBuilder.ts`
- Create: `vscode-extension/src/test/refactoringScenarioBuilder.test.ts`

- [ ] Add failing tests proving scenario building can produce:
  - one bounded option
  - `Option A / B / C` comparisons
  - option-level tradeoffs
  - evidence links and confidence
- [ ] Add mocked-provider tests proving the provider contract returns advisory structures only.
- [ ] Run `cd vscode-extension && npx jest --runInBand src/test/refactoringScenarioBuilder.test.ts` and confirm failure.

### Task 6: Implement provider abstraction and scenario builder

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringProvider.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringScenarioBuilder.ts`
- Test: `vscode-extension/src/test/refactoringScenarioBuilder.test.ts`

- [ ] Define provider interfaces for:
  - grounded input
  - requested domains
  - option count
  - refusal/error reporting
  - provenance capture
- [ ] Implement scenario shaping that normalizes provider output into stable option structures.
- [ ] Re-run the focused scenario-builder tests and confirm they pass.

### Task 7: Add validator tests for hallucination and advisory-only enforcement

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringValidators.ts`
- Create: `vscode-extension/src/test/refactoringValidators.test.ts`

- [ ] Add failing tests proving validators reject or downgrade output that:
  - invents pages, visuals, KPIs, or drill paths
  - contradicts findings or page metadata
  - overclaims business outcomes as actual results
  - claims deterministic execution support without classifier backing
  - produces near-duplicate options with fake tradeoffs
  - leaks into direct-execution language
- [ ] Run `cd vscode-extension && npx jest --runInBand src/test/refactoringValidators.test.ts` and confirm failure.

### Task 8: Implement validators and deterministic fallbacks

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringValidators.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringFallbacks.ts`
- Test: `vscode-extension/src/test/refactoringValidators.test.ts`

- [ ] Implement validation codes for:
  - `inventedArtifact`
  - `unsupportedExecutionClaim`
  - `contradictoryEvidence`
  - `optionDuplication`
  - `outcomeOverclaim`
  - `scopeEscape`
- [ ] Implement fallback builders that return deterministic advisory wording when provider output is unavailable or rejected.
- [ ] Re-run the focused validator tests and confirm they pass.

### Task 9: Build the refactoring orchestrator

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringOrchestrator.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringTelemetry.ts`
- Create: `vscode-extension/src/test/refactoringOrchestrator.test.ts`

- [ ] Add failing tests for orchestration flow:
  1. build context
  2. choose domains
  3. invoke provider
  4. classify compilability
  5. validate output
  6. downgrade or replace invalid sections
  7. return stable advisory payload with provenance
- [ ] Run `cd vscode-extension && npx jest --runInBand src/test/refactoringOrchestrator.test.ts` and confirm failure.
- [ ] Implement orchestration so failure is non-blocking for deterministic fix flows.
- [ ] Re-run the orchestrator tests and confirm they pass.

### Workstream 4: Add Domain Enrichers In Bounded Slices

**Outcome:** Phase 4 grows by domain without destabilizing the shared proposal model.

### Task 10: Add initial PBIR-first enrichers

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/layoutRefactoringEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/storytellingRefactoringEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/navigationRefactoringEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/executiveExperienceEnricher.ts`
- Create: `vscode-extension/src/test/refactoringEnrichers.test.ts`

- [ ] Add failing tests proving enricher routing is deterministic from grounded remediation and findings data.
- [ ] Run `cd vscode-extension && npx jest --runInBand src/test/refactoringEnrichers.test.ts` and confirm failure.
- [ ] Implement the first four enrichers only:
  - layout
  - storytelling
  - navigation
  - executive experience
- [ ] Re-run the enricher tests and confirm they pass.

### Task 11: Add secondary enrichers after the first slice is stable

**Files:**

- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/kpiHierarchyEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/accessibilityAlignmentEnricher.ts`
- Create: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/enrichers/governanceAlignmentEnricher.ts`
- Modify: `vscode-extension/src/test/refactoringEnrichers.test.ts`

- [ ] Add failing tests for:
  - KPI emphasis scenarios
  - accessibility warning overlays in proposals
  - governance-alignment downgrades or warnings
- [ ] Run `cd vscode-extension && npx jest --runInBand src/test/refactoringEnrichers.test.ts` and confirm failure.
- [ ] Implement the three secondary enrichers and confirm the test suite passes.

### Workstream 5: Thread Proposals Into Payload And UI

**Outcome:** Users can compare advisory scenarios without confusing them with deterministic execution.

### Task 12: Add payload support for refactoring proposals

**Files:**

- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Modify: `vscode-extension/src/analyzer/score/fixPlan.ts`
- Modify: `vscode-extension/src/analyzer/score/overviewSummary.ts`
- Modify: `vscode-extension/src/analyzer/score/personaPresentation.ts`
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Modify: `vscode-extension/src/test/fixPlan.test.ts`
- Modify: `vscode-extension/src/test/personaPresentation.test.ts`

- [ ] Add failing tests proving payload shaping can:
  - attach refactoring proposals to remediation items or remediation clusters
  - preserve score, severity, confidence, and normalized findings semantics unchanged
  - omit Phase 4 cleanly when disabled or unavailable
- [ ] Run the focused payload tests and confirm failure.
- [ ] Implement payload wiring and re-run the focused tests until they pass.

### Task 13: Add host-side orchestration and feature gating

**Files:**

- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Modify: `vscode-extension/src/analyzer/proposalEnrichment/refactoring/refactoringTelemetry.ts`
- Test: `vscode-extension/src/test/refactoringOrchestrator.test.ts`

- [ ] Decide whether proposal generation is:
  - eager during payload build
  - lazy on remediation expansion
- [ ] Implement loading state, failure state, fallback state, and telemetry emission.
- [ ] Keep preview/apply/rollback commands and fix selection state independent from Phase 4 loading.

### Task 14: Add webview rendering and regression tests

**Files:**

- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Create: `vscode-extension/webview-src/analyzer-score/refactoringProposals.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
- Create: `vscode-extension/webview-src/analyzer-score/refactoringProposals.test.ts`

- [ ] Add failing tests proving the UI:
  - renders scenario cards and options
  - distinguishes `compilable` versus `advisoryOnly`
  - shows evidence/tradeoff/confidence labels
  - keeps deterministic preview rows visually separate
- [ ] Run `cd vscode-extension && npx jest -c jest.webview.config.cjs --runInBand webview-src/analyzer-score/App.test.tsx webview-src/analyzer-score/refactoringProposals.test.ts` and confirm failure.
- [ ] Implement the UI and re-run the focused webview tests until they pass.

### Workstream 6: Hardening, Evaluation, And Rollout

**Outcome:** Phase 4 can ship behind a flag with measurable quality and strong advisory-only enforcement.

### Task 15: Add evaluation fixtures and quality checks

**Files:**

- Create: `vscode-extension/src/test/fixtures/refactoring/layout-density.json`
- Create: `vscode-extension/src/test/fixtures/refactoring/kpi-hierarchy.json`
- Create: `vscode-extension/src/test/fixtures/refactoring/storytelling-sequence.json`
- Create: `vscode-extension/src/test/fixtures/refactoring/navigation-restructure.json`
- Create: `vscode-extension/src/test/fixtures/refactoring/executive-dashboard.json`
- Modify: `vscode-extension/src/test/refactoringValidators.test.ts`
- Modify: `vscode-extension/src/test/refactoringScenarioBuilder.test.ts`

- [ ] Add fixture-backed tests for:
  - proposal relevance
  - option diversity
  - evidence fidelity
  - fallback stability
  - advisory-only enforcement
- [ ] Keep fixtures compact and fully local so tests remain deterministic.

### Task 16: Run narrow then broad validation

**Files:**

- No code changes expected unless failures are found.

- [ ] Run focused source tests:
  - `cd vscode-extension && npx jest --runInBand src/test/refactoringContextBuilder.test.ts src/test/refactoringScenarioBuilder.test.ts src/test/refactoringValidators.test.ts src/test/refactoringCompilationClassifier.test.ts src/test/refactoringOrchestrator.test.ts src/test/refactoringEnrichers.test.ts src/test/scoreResultPayload.test.ts src/test/fixPlan.test.ts src/test/personaPresentation.test.ts`
- [ ] Run focused webview tests:
  - `cd vscode-extension && npx jest -c jest.webview.config.cjs --runInBand webview-src/analyzer-score/App.test.tsx webview-src/analyzer-score/refactoringProposals.test.ts`
- [ ] Run broader regression:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
- [ ] If any validation fails twice, stop and record the new hypothesis before retrying.

### Task 17: Docs, release notes, and durable memory

**Files:**

- Modify: `docs/ROADMAP.md`
- Modify: `docs/CHANGELOG.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-06-03-2145-phase4-advanced-ai-refactoring-implementation.md`

- [ ] Document shipped scope explicitly:
  - advisory-only
  - PBIR-first
  - no model-generated mutations
  - no Fabric App implementation
- [ ] Record rollout learnings and any quality caveats in durable memory.

## Rollout Recommendation

Release Phase 4 behind a feature flag in three PBIR-first slices:

1. Scenario contracts, context building, validation, and fallback only
2. Initial enrichers plus UI comparison rendering
3. Compilation classification and deterministic hint labeling

Do not enable cross-surface reuse, Fabric App integration, or broader design-studio behavior in the first rollout.

## Definition Of Done

Phase 4 is ready to ship when:

- refactoring proposals are visible and clearly advisory
- at least four core domains are supported:
  - layout
  - storytelling
  - navigation
  - executive experience
- proposal output is grounded, validated, and fallback-safe
- per-option `compilable` versus `advisoryOnly` labels are correct
- deterministic preview/apply/rollback behavior remains unchanged
- the regression suite passes
- docs and durable memory are updated
