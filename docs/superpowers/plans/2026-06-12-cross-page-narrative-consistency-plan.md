# Cross-Page Narrative Consistency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an internal-only report-level narrative assessment layer that classifies page roles, evaluates report flow and continuity, detects orphaned pages, and produces evidence-backed report-level story gaps without changing public contracts or Story Assessment 2.2 behavior.

**Architecture:** Extend the backend Story Assessment pipeline with a deterministic internal Cross-Page Narrative layer that consumes existing page-level Story Assessment outputs plus PBIR report/navigation metadata, builds a report narrative graph, derives role/flow/consistency/orphan assessments, and emits validation-exportable internal artifacts only.

**Tech Stack:** .NET 8 backend scoring pipeline, existing Story Assessment validation models and export tooling, PBIR report metadata and drill/navigation extraction, xUnit, optional extension test adjustments only if internal export plumbing crosses an existing boundary

---

## Architecture Summary

Implementation should preserve the current layering:

- backend scoring and internal Story Assessment records remain authoritative
- Cross-Page Narrative is an internal report-analysis sublayer
- no score-panel contract expansion occurs in the first slice
- no new UI surface is added

Recommended internal flow:

`shared repository snapshot`
`+ PBIR report metadata`
`+ page Story Assessment outputs`
`+ drillthrough/navigation metadata`
`-> page role classification`
`-> narrative graph`
`-> flow + continuity + orphan evaluation`
`-> report-level narrative score`
`-> report story-gap generation`
`-> validation export`

## Data Model Plan

Add internal-only models for:

- `CrossPageNarrativeRoleId`
- `CrossPageNarrativeRoleConfidence`
- `CrossPageNarrativeRoleAssignment`
- `CrossPageNarrativeEdgeType`
- `CrossPageNarrativeEdge`
- `CrossPageNarrativeGraph`
- `CrossPageNarrativeDimensionScore`
- `CrossPageNarrativeScoreSummary`
- `CrossPageNarrativeGap`
- `CrossPageNarrativeAssessment`
- validation-export DTOs for report-level review

Model rules:

- reuse existing `StoryAssessmentPromotionState`, validation ratings, and surface-scope concepts where applicable
- keep all Cross-Page Narrative records internal
- attach evidence references and affected pages to every assessment and gap
- keep page-role classification separate from public page-story labels

## File Map

Likely backend touch points:

- Modify: `service-dotnet/Services/Pbir/Models/StoryAssessmentValidationModels.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Modify: `service-dotnet/tools/StoryAssessmentValidationExport/*`
- Modify: `service-dotnet/tests/Tests.csproj`

Recommended new backend files:

- Create: `service-dotnet/Services/Pbir/Models/CrossPageNarrativeModels.cs`
- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/PageNarrativeRoleClassifier.cs`
- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeGraphBuilder.cs`
- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeConsistencyEvaluator.cs`
- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeOrphanEvaluator.cs`
- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeScorer.cs`
- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeGapBuilder.cs`
- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeExportAdapter.cs`

Recommended tests:

- Create: `service-dotnet/tests/CrossPageNarrative/PageNarrativeRoleClassifierTests.cs`
- Create: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeGraphBuilderTests.cs`
- Create: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeConsistencyEvaluatorTests.cs`
- Create: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeOrphanEvaluatorTests.cs`
- Create: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeScorerTests.cs`
- Create: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeGapBuilderTests.cs`
- Create: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeExportAdapterTests.cs`

## Rollout Phases

### Phase 1: Internal Foundations

- add the internal model
- classify page roles
- build the report narrative graph
- derive dimensional report-level narrative scores

### Phase 2: Report-Level Findings

- add consistency and orphan evaluators
- add report-level story-gap builder
- attach explainability and confidence

### Phase 3: Validation Export And Corpus Review

- expose internal report-level artifacts through the validation export tool
- add reviewer-facing Markdown and JSON sections
- run Level 1 corpus review

### Phase 4: Promotion Decision

- decide whether any subset is contract-eligible
- defer public score or role taxonomy exposure unless Level 2 evidence supports it

## Regression Strategy

Preserve the following invariants through focused tests:

- no change to existing public `ScoreResult` contract shape
- no change to Guided Story Improvements behavior
- no change to Story Assessment 2.2 navigation or diff behavior
- existing report consistency outputs remain stable unless intentionally reused internally
- no new analyzer-specific filesystem rescans bypass the shared snapshot pattern

Required regression coverage:

- public contract non-leak tests
- malformed or sparse report graceful-degradation tests
- mixed-role report edge-case tests
- special-page suppression tests
- deterministic repeatability tests on the same report

## Validation Strategy

### Level 1

- build a curated PBIR corpus covering:
  - strong executive reports
  - operational monitor reports
  - drill-heavy reports
  - weak or fragmented reports
  - appendix/reference-heavy reports
- score each report and export Cross-Page Narrative internals
- run expert review against:
  - dominant report objective
  - page roles
  - narrative flow
  - orphan decisions
  - top report-level gaps

### Level 2

- expand to a larger PBIR report corpus
- measure reviewer agreement and confidence calibration
- define platform-critical promotion thresholds

## Future Compatibility Rules

- keep evaluator logic surface-neutral and adapter-driven
- keep PBIR extraction in dedicated helpers rather than embedding file-shape logic in the core scorer
- avoid direct dependencies on score-panel UI models
- keep report-level recommendations distinct from page-level Guided Story Improvements
- route any future promoted report-level output through normalized findings rather than a parallel issue model

## Task Plan

### Task 1: Lock The Internal Model Boundary

**Files:**

- Modify: `service-dotnet/Services/Pbir/Models/StoryAssessmentValidationModels.cs`
- Create: `service-dotnet/Services/Pbir/Models/CrossPageNarrativeModels.cs`
- Test: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeModelBoundaryTests.cs`

- [ ] Add failing boundary tests that assert Cross-Page Narrative records remain internal-only and do not leak into the public `ScoreResult` payload.
- [ ] Define the core internal enums and records for role assignments, graph edges, dimensional scores, orphan states, and report-level gaps.
- [ ] Reuse existing validation and promotion concepts where appropriate instead of inventing a second lifecycle vocabulary.
- [ ] Run focused xUnit model-boundary tests until they pass.

### Task 2: Add PBIR Report-Narrative Input Extraction

**Files:**

- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeInputBuilder.cs`
- Test: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeInputBuilderTests.cs`

- [ ] Write failing tests for extracting the minimal report-level narrative inputs from existing scoring context:
  - page order
  - page names
  - page Story Assessment outputs
  - drillthrough relationships where available
  - special-page flags
- [ ] Build a dedicated PBIR-first input builder that consumes existing data rather than triggering duplicate page rescans.
- [ ] Ensure sparse or partial navigation metadata degrades gracefully instead of aborting assessment generation.
- [ ] Re-run focused input-builder tests until they pass.

### Task 3: Implement Page Role Classification

**Files:**

- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/PageNarrativeRoleClassifier.cs`
- Test: `service-dotnet/tests/CrossPageNarrative/PageNarrativeRoleClassifierTests.cs`

- [ ] Write failing tests for primary-role assignment across the initial role taxonomy, including suppression behavior for Tooltip, Q&A, Reference / Legal, and Validation / Sandbox pages.
- [ ] Implement deterministic role rules driven by page-level Story Assessment outputs, visual/layout hints, drill posture, and page-position evidence.
- [ ] Add confidence assignment and conflicting-evidence downgrade logic.
- [ ] Re-run focused role-classifier tests until they pass.

### Task 4: Build The Narrative Graph

**Files:**

- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeGraphBuilder.cs`
- Test: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeGraphBuilderTests.cs`

- [ ] Write failing tests for graph construction covering ordered adjacency, drillthrough edges, inferred summary-to-detail edges, and segmented narrative islands.
- [ ] Implement the narrative graph builder with separate observed and inferred edge types.
- [ ] Add deterministic tie-breaking and edge-suppression rules so ambiguous relationships do not fabricate false precision.
- [ ] Re-run focused graph-builder tests until they pass.

### Task 5: Implement Flow And Consistency Evaluation

**Files:**

- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeConsistencyEvaluator.cs`
- Test: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeConsistencyEvaluatorTests.cs`

- [ ] Write failing tests for narrative transition quality, KPI continuity mismatches, naming continuity breaks, and abrupt business-context shifts.
- [ ] Implement report-level Flow, Consistency, and Continuity dimension evaluation with evidence-backed explanations.
- [ ] Reuse existing report consistency context as supporting evidence where helpful without collapsing the two concepts into one model.
- [ ] Re-run focused consistency-evaluator tests until they pass.

### Task 6: Implement Orphan And Navigation Assessment

**Files:**

- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeOrphanEvaluator.cs`
- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeNavigationEvaluator.cs`
- Test: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeOrphanEvaluatorTests.cs`

- [ ] Write failing tests for orphaned pages, unreachable drill targets, isolated analysis islands, and intentionally disconnected appendix-like pages.
- [ ] Implement orphan detection with advisory downgrades for valid special-page roles.
- [ ] Implement Navigation and Actionability dimension evaluation from explicit and inferred report paths.
- [ ] Re-run focused orphan and navigation tests until they pass.

### Task 7: Implement Report-Level Narrative Score And Gap Builder

**Files:**

- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeScorer.cs`
- Create: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeGapBuilder.cs`
- Test: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeScorerTests.cs`
- Test: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeGapBuilderTests.cs`

- [ ] Write failing tests for the five report-level dimensions:
  - Flow
  - Consistency
  - Navigation
  - Continuity
  - Actionability
- [ ] Implement the bounded internal scoring model plus separate confidence calculation.
- [ ] Implement report-level story-gap generation with stable internal identifiers, affected pages, evidence references, and remediation-layer classification.
- [ ] Re-run focused scorer and gap-builder tests until they pass.

### Task 8: Thread Cross-Page Narrative Into Backend Scoring

**Files:**

- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Test: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeIntegrationTests.cs`

- [ ] Write failing integration tests for report-level scoring runs that should generate Cross-Page Narrative internals while preserving the existing public payload.
- [ ] Integrate the new builder/evaluator pipeline into report-level PBIR scoring after page Story Assessment outputs are available.
- [ ] Ensure page-level scoring paths and single-page review paths either degrade safely or skip report-level generation explicitly.
- [ ] Re-run focused backend integration tests until they pass.

### Task 9: Extend Validation Export Tooling

**Files:**

- Modify: `service-dotnet/tools/StoryAssessmentValidationExport/*`
- Create: `service-dotnet/tests/CrossPageNarrative/CrossPageNarrativeExportAdapterTests.cs`

- [ ] Write failing tests for report-level validation export sections that include:
  - dominant report objective
  - page roles
  - main narrative path
  - orphan decisions
  - report-level gaps
  - dimension scores and confidence
- [ ] Add JSON and Markdown export shaping for the new internal artifacts.
- [ ] Keep export output clearly marked as internal validation material, not public product contract.
- [ ] Re-run focused export tests until they pass.

### Task 10: Run Validation Corpus And Promotion Review

**Files:**

- Modify: `docs/story-assessment/*` as needed for rubric/output capture
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`

- [ ] Run the Level 1 review process on the curated PBIR corpus and record role/flow/orphan/gap observations.
- [ ] Calibrate confidence and suppression thresholds from reviewer disagreement and false-positive patterns.
- [ ] Decide whether any subset is ready for contract-eligibility discussion; default to report-level gaps only if the evidence is strong.
- [ ] Record promotion decision artifacts and next-step recommendations in repo memory and story-assessment docs.

## Recommended Shipping Posture

Recommended first implementation posture:

- ship no public contract changes
- ship no UI changes
- ship internal validation/export capability only

Reason:

- page-role taxonomies and composite report scores are classification-heavy and should not be promoted before validation
- report-level story gaps are the most plausible future promotion candidate, but only after corpus review proves usefulness and stability
