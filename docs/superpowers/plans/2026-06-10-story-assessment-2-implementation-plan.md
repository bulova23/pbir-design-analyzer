# Story Assessment 2.0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a validation-first Story Assessment 2.0 rollout that proves which PBIR narrative signals are trustworthy before promoting them into contracts, production workflows, or platform-critical quality logic.

**Architecture:** Keep Story Assessment backend-first and PBIR-first in the first rollout. Introduce internal signal infrastructure, expert-review validation workflow, promotion gates, and contract-boundary rules before exposing expanded Story Assessment fields. Treat cross-surface applicability as an architectural classification exercise first, not an implementation blocker.

**Tech Stack:** .NET 8 backend scoring pipeline, existing PBIR scoring and metadata extraction models, TypeScript score payload shaping, existing VS Code score-panel contract guards, Jest, xUnit, documentation and repo-memory workflows

---

## Rollout Model

### Validation Phase

- Signal Registry
- Archetypes
- Semantic Coherence
- Filter Topology
- Story Gaps
- Confidence Breakdown

### Promotion Phase

- contract-safe Story Assessment extensions
- validated Story Gaps
- validated Confidence Breakdown

### Experience Phase

- deep links
- diff mode
- competing stories
- narrative analysis views

### Advanced Phase

- reasoning trace
- measure description mining

## Promotion Ladder

Every signal must move through the same sequence:

- internal signal
- Level 1 expert review validation
- contract eligible
- production usage
- cross-surface candidate
- Level 2 formal corpus validation
- platform critical

No signal skips phases.

## Ship Rules

### Must Exist Before Any Contract Promotion

- internal signal registry
- reviewer rubric
- Level 1 expert review workflow
- four-dimension validation criteria
- signal classification model
- contract-promotion rules

Reason:

Without these, Story Assessment 2.0 becomes a feature rollout rather than a trust-building rollout.

### Must Ship Together In The First Implementation Slice

- internal signal infrastructure
- PBIR-first evaluation corpus workflow
- archetype and coherence validation scaffolding
- story gap and confidence-breakdown validation scaffolding

Reason:

The validation framework is more important than any single signal implementation.

### Defer Until Validation Passes

- expanded public contracts
- UI dependencies on unvalidated fields
- deep-link behavior tied to new story gaps
- platform-critical positioning changes
- cross-surface rollout requirements

## File Map

### Backend Validation And Scoring Infrastructure

- `service-dotnet/Services/Pbir/PbirScoringService.cs`
- `service-dotnet/Services/Pbir/Models/ScoreResult.cs`
- `service-dotnet/Services/Pbir/Models/VisualMetadataSummary.cs`
- new Story Assessment support models under `service-dotnet/Services/Pbir/Models/`
- targeted Story Assessment modules under `service-dotnet/Services/Pbir/` as needed for:
  - signal registry
  - archetype evaluation
  - semantic coherence
  - filter topology
  - story gap shaping
  - confidence breakdown shaping

### Validation Fixtures And Tests

- `service-dotnet/tests/`
- targeted Story Assessment test files to be created for:
  - signal registry
  - archetype classification
  - semantic coherence
  - filter topology
  - story gap usefulness
  - confidence-breakdown provenance

### Extension Contract Boundary

- `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- `vscode-extension/src/views/scoreResultPayload.ts`
- `vscode-extension/src/views/PbirScorePanel.ts`
- `vscode-extension/webview-src/analyzer-score/App.tsx`
- related tests under `vscode-extension/src/test/` and `vscode-extension/webview-src/analyzer-score/`

### Documentation And Validation Assets

- `docs/PBIR_Story_Assessment_Enhancement_Plan.md`
- `docs/superpowers/specs/2026-06-10-story-assessment-2-design-validation.md`
- this plan file
- reviewer-rubric documentation to be created during implementation
- corpus and validation observations documentation to be created during implementation

## Workstream 1: Validation Substrate

**Outcome:** The codebase gains the structures needed to evaluate signals before promoting them.

### Task 1: Define internal Story Assessment signal contracts

- [ ] Add internal-only model types for:
  - signal registry entries
  - signal categories
  - reliability state
  - surface-scope classification
  - explanation type
  - actionability type
- [ ] Keep these models backend-internal and out of the current score-panel contract.
- [ ] Ensure the types support PBIR-first evaluation and later cross-surface classification.

### Task 2: Define the four-dimension evaluation model

- [ ] Add explicit evaluation enums or model fields for:
  - accuracy
  - consistency
  - explainability
  - actionability
- [ ] Encode how signals are judged at Level 1 versus Level 2.
- [ ] Keep the model deterministic and inspectable rather than free-form only.

### Task 3: Define signal promotion states

- [ ] Add an internal lifecycle model for:
  - internal
  - Level 1 validated
  - contract eligible
  - production
  - cross-surface candidate
  - Level 2 validated
  - platform critical
- [ ] Define failure and rollback behavior when a signal regresses or is disputed.

### Validation Strategy

- focused xUnit coverage for internal model shaping
- documentation review confirming no current public contract changed

## Workstream 2: PBIR Expert Review Validation Framework

**Outcome:** Story Assessment can be judged against real PBIR pages using a repeatable expert-review process.

### Task 1: Define the PBIR validation corpus strategy

- [ ] Document corpus composition rules:
  - 20 to 50 real PBIR pages
  - at least 5 reports
  - varied page types and narrative styles
- [ ] Define inclusion and exclusion criteria for pages.
- [ ] Define how ambiguous or low-information pages should be labeled in review.

### Task 2: Define the reviewer rubric

- [ ] Write the reviewer rubric document covering:
  - inferred story
  - archetype choice
  - coherence judgment
  - competing story presence
  - story gaps
  - confidence judgment
  - explanation quality
  - actionability quality
- [ ] Define bounded reviewer scoring values instead of narrative comments only.

### Task 3: Define reviewer workflow

- [ ] Specify reviewer order of operations:
  - independent human judgment first
  - system output second
  - disagreement logging third
- [ ] Specify how reviewer disagreement is recorded and summarized.
- [ ] Define when a signal is considered ambiguous versus failed.

### Validation Strategy

- document review against the approved spec
- pilot rubric walk-through on a small PBIR subset before broader use

## Workstream 3: Signal Registry Implementation

**Outcome:** The backend can capture story signals as inspectable internal evidence rather than implicit logic only.

### Task 1: Introduce internal Signal Registry plumbing

- [ ] Build internal signal capture for the proposed signal categories:
  - layout
  - semantic
  - context
  - interaction, if applicable
- [ ] Store raw value, fired state, contribution intent, remediability, and explanation hooks.
- [ ] Preserve additive behavior so existing user-facing outputs remain stable by default.

### Task 2: Classify each signal

- [ ] Label each signal as:
  - PBIR-specific
  - cross-surface candidate
  - future-surface specific
- [ ] Record required versus optional role.
- [ ] Record whether the signal is direct evidence or reinforcement only.

### Task 3: Add validation coverage

- [ ] Write tests proving representative signals capture expected internal evidence.
- [ ] Add malformed or partial PBIR cases proving graceful degradation.

### Validation Strategy

- xUnit signal-registry tests
- manual inspection of diagnostic outputs on representative PBIR pages

## Workstream 4: Archetype Classification Validation

**Outcome:** Archetypes are validated as trustworthy internal narrative categories before contract promotion.

### Task 1: Implement internal archetype scoring

- [ ] Add best-fit scoring for:
  - Performance Monitor
  - Trend + Exception
  - Ranking
  - Comparison
  - Decomposition
  - Narrative Walkthrough
- [ ] Record matched and missed signals for explanation.

### Task 2: Add Level 1 validation harness

- [ ] Create evaluation fixtures or runtime outputs that can be reviewed against the rubric.
- [ ] Record reviewer choice, system choice, and disagreement reasons.

### Task 3: Define promotion gate

- [ ] Establish the minimum Level 1 bar for contract eligibility.
- [ ] Require explanation quality and gap usefulness, not just classification accuracy.

### Validation Strategy

- archetype-focused xUnit coverage
- reviewer-comparison documentation from the PBIR corpus

## Workstream 5: Semantic Coherence And Competing Story Validation

**Outcome:** Coherence becomes measurable without over-promoting competing-story claims.

### Task 1: Implement internal coherence scoring

- [ ] Build clustering and dominant-concept logic as internal evidence.
- [ ] Record confidence and explanation hooks.

### Task 2: Separate basic coherence from competing-story detection

- [ ] Treat focused-versus-split coherence as the primary Phase 1 target.
- [ ] Treat competing-story detection as higher-risk and promotion-delayed.

### Task 3: Add validation workflow

- [ ] Test true positives, false positives, and false negatives.
- [ ] Capture reviewer disagreement on borderline multi-topic pages.

### Validation Strategy

- xUnit coherence coverage
- expert-review scoring on representative PBIR pages

## Workstream 6: Filter Topology Validation

**Outcome:** Filter topology is validated as a reinforcement signal rather than assumed to be universally meaningful.

### Task 1: Implement internal topology extraction

- [ ] Capture slicer/filter structure, scope, and hierarchy patterns.
- [ ] Map topology evidence to archetype reinforcement rather than standalone narrative truth.

### Task 2: Classify topology signals by surface scope

- [ ] Separate clearly PBIR-specific signals from possible cross-surface abstractions.
- [ ] Mark low-value or noisy topology signals as diagnostic-only.

### Task 3: Validate contribution

- [ ] Prove whether topology changes story inference quality materially.
- [ ] Remove or demote signals that add noise without improving explanation or actionability.

### Validation Strategy

- xUnit extraction tests
- corpus review notes comparing with and without topology reinforcement

## Workstream 7: Story Gaps And Confidence Breakdown Validation

**Outcome:** The most user-visible candidates are validated for usefulness before contract exposure.

### Task 1: Implement structured internal story gaps

- [ ] Generate gap records tied to evidence and remediation layer.
- [ ] Keep advanced deep-link fields out of the public contract until separately proven.

### Task 2: Implement internal confidence breakdown

- [ ] Replace a single opaque confidence rationale with inspectable internal dimension summaries.
- [ ] Make low-confidence causes explicit and, where possible, remediable.

### Task 3: Validate usefulness

- [ ] Evaluate whether reviewers consider the gaps actionable.
- [ ] Evaluate whether confidence breakdown improves trust calibration.
- [ ] Reject or narrow fields that are descriptive but not useful.

### Validation Strategy

- xUnit story-gap and confidence-breakdown tests
- rubric-based usefulness scoring from expert reviewers

## Workstream 8: Contract Promotion Design

**Outcome:** Public Story Assessment contracts grow only after specific fields pass Level 1.

### Task 1: Define internal-only versus contract-eligible fields

- [ ] Review proposed `StoryAssessmentResult`, `StoryGap`, and `CompetingStory` shapes.
- [ ] Mark each field as:
  - internal-only
  - contract-eligible after Level 1
  - Level 2 required

### Task 2: Add boundary guards

- [ ] Ensure payload shaping rejects or omits unvalidated fields.
- [ ] Preserve backward compatibility of the current score payload.

### Task 3: Define production-usage rules

- [ ] Specify which validated fields may drive UI messaging.
- [ ] Prohibit unvalidated fields from becoming downstream product dependencies.

### Validation Strategy

- payload-shaping unit tests
- score-panel contract parsing tests

## Workstream 9: Cross-Surface Readiness Classification

**Outcome:** The design becomes future-compatible without creating a Phase 1 delivery blocker.

### Task 1: Classify each signal for future surfaces

- [ ] Mark each signal as:
  - PBIR-specific
  - cross-surface candidate
  - future-surface specific

### Task 2: Record abstraction requirements

- [ ] Document what would need to change for Fabric App compatibility.
- [ ] Document what would need to change for Report Design Studio compatibility.

### Task 3: Keep Phase 1 PBIR-only

- [ ] Ensure no Fabric App or Design Studio validation becomes a contract-promotion prerequisite in the first rollout.

### Validation Strategy

- architecture review against the approved spec
- no runtime implementation required in this phase

## Workstream 10: Platform Positioning Decision

**Outcome:** The team gets an explicit answer about Story Assessment’s strategic role.

### Task 1: Define decision criteria

- [ ] Evaluate whether Story Assessment improves:
  - issue interpretation
  - remediation framing
  - future analyzer consistency
  - platform-level quality reasoning

### Task 2: Define non-promotion conditions

- [ ] Specify conditions where Story Assessment should remain a PBIR-focused analytical layer instead of becoming the primary quality engine.

### Task 3: Define platform-critical gate

- [ ] Require Level 2 corpus validation before any signal becomes a platform-critical dependency.

### Validation Strategy

- design review using the accumulated Level 1 results
- roadmap decision memo before any platform-wide repositioning

## Regression Requirements

- preserve current Story Assessment outputs by default until validated contract promotion is intentionally implemented
- preserve current score behavior unless an implementation step explicitly changes internals under validation
- do not let unvalidated signals alter Issues, Fix Plan, AI Proposal Enrichment, Fabric App Readiness, or Fabric App Review semantics

## Validation Commands

Focused validation should be used after each implementation workstream.

Expected broad validation once implementation begins:

- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- `cd vscode-extension && npm test`
- `cd vscode-extension && npm run compile`

If contract-facing extension changes are introduced later:

- `cd vscode-extension && npm run package:all`

## Documentation Requirements

Implementation should update:

- `docs/CHANGELOG.md`
- `docs/ROADMAP.md`
- reviewer rubric documentation
- validation observations documentation
- repo memory files

## Recommended Sequence

### Recommended 1

- validation substrate
- reviewer rubric
- PBIR corpus workflow

### Recommended 2

- internal Signal Registry
- archetype validation
- semantic coherence validation
- filter topology validation

### Recommended 3

- internal story gaps
- internal confidence breakdown
- Level 1 promotion review

### Recommended 4

- narrow contract promotion for validated fields only

### Recommended 5

- cross-surface candidate planning
- Level 2 corpus strategy
- platform-positioning decision

## Definition Of Done

This plan is complete when implementation eventually delivers:

- PBIR-first validation for the approved signal set
- a documented expert-review rubric and workflow
- field-by-field promotion gates
- contract-boundary enforcement
- cross-surface classification without cross-surface Phase 1 dependency
- a clear strategic decision about whether Story Assessment earns primary quality-engine status
