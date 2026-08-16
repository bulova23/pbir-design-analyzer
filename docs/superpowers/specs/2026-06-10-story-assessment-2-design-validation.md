# Story Assessment 2.0 Design Validation

Date: 2026-06-10

Status: Approved planning direction captured; implementation deferred

## Goal

Define a validation-first architecture for Story Assessment 2.0 before expanding contracts, scoring dependencies, or UI obligations.

The core question is not how many new Story Assessment fields can be added.

It is:

- which signals can be trusted
- why they can be trusted
- when they are safe to promote

This design assumes PBIR-first validation and cross-surface-aware architecture.

## Business Objective

Story Assessment already provides useful narrative inference for PBIR reports.

The next decision is whether it can become the platform’s primary quality engine.

That decision should not be made by feature ambition alone.

It should be made only after the proposed signals prove they are:

- accurate
- consistent
- explainable
- actionable

The product goal is Trustworthy Story Assessment, not maximum Story Assessment surface area.

## Planning Boundary

This design is validation architecture and rollout guidance only.

It does not:

- implement new signals
- change current scoring behavior
- change current score contracts
- redesign the Story Assessment UI
- commit the platform to Story Assessment as the primary quality layer yet

## Current State

Current Story Assessment is already user-facing in a limited form through the existing PBIR scoring flow.

The current model exposes a compact set of user-facing outputs such as:

- detected story
- supported decision
- why this matters
- story gaps
- story confidence

The approved enhancement proposal expands that model materially with:

- Signal Registry
- Archetype Classification
- Semantic Coherence Scoring
- Filter Topology Extraction
- extended Story Assessment contracts
- Structured Story Gaps
- Competing Story Detection
- Cross-Page Narrative Analysis
- Measure Description Mining
- full reasoning trace

That proposed expansion is large enough that validation must become a first-class architecture concern.

## PBIR-First Scope

Phase 1 validation is PBIR-only.

It must validate the following using real PBIR reports:

- Signal Registry
- Archetype Classification
- Semantic Coherence
- Filter Topology
- Story Gaps
- Confidence Breakdown

Phase 1 does not require Fabric App or Report Design Studio validation.

Instead, every proposed signal must be classified as one of:

- PBIR-specific
- cross-surface candidate
- future-surface specific

This keeps the architecture extensible without making cross-surface support a Phase 1 blocker.

## Core Design Principle

Story Assessment 2.0 should follow staged validation-first promotion:

Internal Signal  
↓  
Level 1 Expert Review Validation  
↓  
Contract Eligible  
↓  
Production Usage  
↓  
Cross-Surface Candidate  
↓  
Level 2 Formal Corpus Validation  
↓  
Platform Critical

No signal may skip stages.

No signal becomes platform-defining only because it appears plausible in internal testing.

## Validation Model

### Four-Dimension Gate

Every new Story Assessment signal must be evaluated against four dimensions:

1. Accuracy
2. Consistency
3. Explainability
4. Actionability

These dimensions are intentionally stricter than raw correctness.

### Accuracy

Question:

Does the signal correctly describe the story or narrative condition present on the PBIR page?

Examples:

- does the inferred archetype match human judgment
- does semantic coherence correctly detect a focused versus split narrative
- does a story gap correspond to a real missing narrative element

### Consistency

Question:

Does the signal behave predictably across similar PBIR pages, reports, and reviewer interpretations?

Examples:

- does the same page score similarly across runs
- do similar page structures map to similar archetypes
- do reviewers repeatedly agree on whether a competing story exists

### Explainability

Question:

Can the signal be explained clearly enough that users and future maintainers understand why it fired?

Examples:

- can the system show the contributing evidence
- can a reviewer audit the reasoning path
- can the signal be defended without opaque model-only logic

### Actionability

Question:

Does the signal help users improve report quality rather than merely describe the report?

Examples:

- does a story gap lead to a concrete remediation path
- does coherence feedback suggest what to simplify, align, or separate
- does confidence breakdown reveal what the author can strengthen

Signals that are accurate but not actionable should remain internal or advisory rather than becoming central quality indicators.

## Validation Levels

### Level 1: Expert Review Validation

Level 1 is the gate for contract eligibility.

It requires:

- real PBIR corpus
- multiple reports
- multiple page types
- documented reviewer rubric
- human reviewer scoring
- documented false positives
- documented false negatives
- documented reviewer disagreement

Recommended sample size:

- 20 to 50 real PBIR pages
- at least 5 reports
- a deliberate mix of executive, operational, analytical, and appendix-like pages where possible

Level 1 is strong enough to support contract exposure for signals that prove reliable and useful.

It is intentionally lighter than a formal benchmark corpus so the team can learn quickly.

### Level 2: Formal Corpus Validation

Level 2 is the gate for platform-critical trust.

It requires:

- labeled corpus
- multiple reviewers
- inter-rater agreement measurement
- benchmark datasets
- repeatable scoring
- calibration-friendly evaluation artifacts

Recommended scale:

- 100 or more pages

Level 2 is not required to begin Story Assessment 2.0 rollout.

It is required before a signal becomes a platform-critical quality dependency.

## Reviewer Rubric Architecture

### Rubric Purpose

The rubric exists to keep Phase 1 stronger than informal opinion while avoiding the cost of a full labeled corpus.

It should let reviewers judge both page intent and signal usefulness using shared criteria.

### Required Reviewer Questions

Each reviewed PBIR page should answer:

1. What story is this page trying to tell?
2. Which archetype best fits the page?
3. Is the page semantically coherent or split across competing stories?
4. Which narrative gaps materially weaken decision support?
5. Which signals are strong evidence versus weak hints?
6. Would the resulting guidance help improve the page?

### Required Reviewer Outputs

For each page, reviewers should record:

- inferred story in plain language
- selected archetype
- confidence in archetype selection
- semantic coherence judgment
- competing story presence or absence
- top story gaps
- confidence in the page-level assessment
- notes on ambiguity, disagreement, or misleading signals

### Scoring Frame

The rubric should use a bounded scoring frame rather than free-form commentary only.

Recommended reviewer fields:

- story inference quality: strong, partial, weak
- archetype fit: correct, acceptable, incorrect
- coherence judgment: correct, borderline, incorrect
- story gap usefulness: actionable, partly actionable, not actionable
- explanation quality: clear, partially clear, unclear

This keeps Phase 1 review structured enough to compare signals and failure modes.

## Signal Registry Validation Architecture

### Purpose

The Signal Registry should become the canonical internal diagnostic map of what fired, why it fired, and how much it contributed.

It is a validation substrate before it is a product surface.

### Phase 1 Validation Questions

- which signals are required to infer page story reliably
- which signals are useful but optional
- which signals are too noisy to trust
- which signals are explainable enough to expose later
- which signals generate actionable downstream guidance

### Required Classification Per Signal

Each signal should be classified by:

- reliability tier: experimental, candidate, validated
- requirement role: required, supportive, optional
- surface scope: PBIR-specific, cross-surface candidate, future-surface specific
- explanation type: directly explainable, derived but explainable, opaque
- actionability type: direct remediation, indirect guidance, diagnostic only

### Promotion Rule

No individual signal becomes contract-visible unless:

- Level 1 review shows acceptable behavior across the four dimensions
- the signal has a stable explanation path
- the signal’s downstream product role is defined

## Archetype Classification Validation

### Initial Archetypes In Scope

- Performance Monitor
- Trend + Exception
- Ranking
- Comparison
- Decomposition
- Narrative Walkthrough

### Validation Objective

Determine whether archetypes are reliable enough to become a primary organizing layer for Story Assessment rather than just an internal heuristic.

### Human Review Methodology

Reviewers should classify each page independently before seeing system output.

The evaluation should then compare:

- reviewer archetype choice
- reviewer confidence
- system archetype choice
- system explanation and fired signals

### Evaluation Focus

- true positives on obvious archetypes
- ambiguous pages that span multiple narrative patterns
- false certainty where the system forces an archetype too aggressively
- whether archetype output improves gap quality versus generic story commentary

### Promotion Guidance

Archetype classification may become contract-eligible after Level 1 if:

- it is accurate enough on representative PBIR pages
- its misses are explainable
- it improves the usefulness of story gaps and confidence breakdown

It should not become platform-critical until Level 2 confirms stable behavior across a broader corpus.

## Semantic Coherence Validation

### Purpose

Semantic Coherence should answer whether the page is narratively focused or split across unrelated concepts.

### Validation Focus

- true positives on focused pages
- false positives where a legitimate multi-angle page is mislabeled as incoherent
- false negatives where competing stories are present but missed
- usefulness of dominant concept labeling

### Competing Story Detection

Competing story detection is valuable but higher risk than basic coherence scoring.

It should be treated as:

- Level 1 eligible for internal use and possibly cautious contract exposure
- Level 2 required before it is treated as a high-trust platform-critical indicator

### Promotion Guidance

Basic coherence scoring can likely advance earlier than competing story detection because it is easier to explain and usually maps to clearer remediation.

## Filter Topology Validation

### Purpose

Filter topology should be evaluated primarily as a reinforcement signal rather than a standalone story engine.

### Validation Questions

- does filter topology improve archetype confidence meaningfully
- does it reduce ambiguity on key PBIR page patterns
- does it create false certainty when filter metadata is present but semantically weak
- which topology patterns are specific to PBIR implementation details

### Expected Classification

Filter topology is likely mixed:

- some signals will be PBIR-specific
- some patterns may become cross-surface candidates
- some topology-derived inferences may remain implementation-specific and diagnostic only

### Promotion Guidance

Filter topology should not be promoted because it is available.

It should be promoted only where it demonstrably improves story inference quality or actionable story gaps.

## Story Gaps Validation

### Purpose

Story gaps are the most user-actionable part of the enhancement plan and therefore require strong validation even if the underlying signal model is still evolving.

### Validation Questions

- do the gaps correspond to real weaknesses in the narrative
- are the gaps specific enough to guide improvement
- do archetype-aware gaps outperform generic gap phrasing
- do reviewers judge the gaps as useful remediation guidance

### Promotion Guidance

Structured story gaps can become contract-eligible after Level 1 if they are:

- reliably tied to observed evidence
- clearly explained
- actionable in report-author terms

Gaps that are merely descriptive should remain internal or secondary.

## Confidence Breakdown Validation

### Purpose

Confidence Breakdown should replace a flat confidence label only after confidence becomes inspectable and defensible.

### Validation Questions

- does the breakdown correctly identify why confidence is high or low
- do users gain diagnostic value from the decomposition
- are low-confidence conditions remediable
- does the breakdown reduce over-trust in weak inferences

### Promotion Guidance

Confidence Breakdown is a strong Level 1 candidate because it improves explainability directly.

However, the decomposition should not imply precision that the underlying signals do not support.

## Contract Promotion Strategy

### Principle

Contract promotion should be field-by-field rather than package-wide.

The proposal’s contract candidates should not all move together.

### StoryAssessmentResult

Recommended treatment:

- keep raw Signal Registry internals internal initially
- allow validated summary fields to become contract-eligible after Level 1
- defer fields that expose unstable reasoning or ambiguous semantics

Likely Level 1 contract-eligible candidates after successful validation:

- detected archetype summary
- confidence breakdown summary
- validated story gaps summary
- semantic coherence summary in bounded form

Likely internal-first fields:

- raw per-signal weights
- unstable competing story internals
- exploratory reasoning-trace detail
- low-confidence filter-topology evidence maps

### StoryGap

Recommended treatment:

- likely contract-eligible after Level 1 if tied to evidence and remediation
- maintain a narrow, actionable shape first
- defer deep-link and advanced provenance fields until targeting trust is proven separately

### CompetingStory

Recommended treatment:

- keep internal or cautionary-only until stronger validation exists
- require Level 2 before becoming a highly trusted platform-critical contract dependency

## Proposed Signal Classification

### Likely Platform-Wide Candidates

- Story Archetypes
- Semantic Coherence
- Competing Stories
- Narrative Flow
- Decision Support

These should be designed for future cross-surface applicability even if Phase 1 validates them on PBIR only.

### Likely PBIR-Specific Signals

- visual-type heuristics tied closely to PBIR visual metadata
- certain filter-topology signals
- PBIR navigation metadata
- PBIR layout and title extraction heuristics

### Likely Future-Surface-Specific Signals

- Fabric-specific app topology and experience signals
- Design Studio authoring-state signals
- future model-authoring or generated-design metadata

## Cross-Surface Evaluation Model

Phase 1 does not validate additional surfaces.

It does require architectural classification for future reuse.

For each signal, the design should ask:

- could this signal conceptually work on Fabric Apps
- could it work on future Design Studio outputs
- what dependencies make it PBIR-specific today
- what abstraction would be needed to generalize it later

This creates cross-surface awareness without making cross-surface delivery part of the first validation gate.

## Story Assessment Positioning

### Relationship To Existing Product Areas

Story Assessment should be evaluated relative to:

- Issues
- Fix Plan
- AI Proposal Enrichment
- Fabric App Readiness
- Fabric App Review
- Report Design Studio

### Recommended Positioning Hypothesis

Story Assessment should not immediately replace these areas.

Instead, the design should test whether Story Assessment can become a primary interpretive quality layer that helps organize:

- page-level narrative quality
- issue prioritization
- remediation framing
- future cross-surface quality guidance

If validated, Story Assessment could evolve into a platform-level quality engine.

If not validated, it should remain a useful PBIR-specific analytical layer rather than being forced into platform primacy.

## Advanced Signal Deferral

The following items are strategically important but should remain later-phase work until the Phase 1 validation substrate is proven:

- Cross-Page Narrative Analysis
- Measure Description Mining
- full reasoning trace

Reason:

They increase sophistication and contract temptation before the basic validation loop is mature.

They should be layered onto a proven validation framework, not used to justify it.

## Risks

### Over-Promotion Risk

Signals may appear impressive in demos but fail consistency or actionability in real report review.

### Explanation Debt

Signals may become hard to justify if promotion outruns evidence provenance and reviewer-facing reasoning.

### Contract Lock-In

If unstable fields are exposed too early, the UI and downstream tooling may start depending on semantics that later need to change.

### PBIR Bias Risk

PBIR-first validation is the right first move, but the architecture must avoid encoding PBIR implementation details as if they were universal narrative truths.

## Dependencies

- stable access to a real PBIR corpus
- reviewer rubric and workflow
- backend-internal storage for signal diagnostics
- evaluation instrumentation for comparing human judgments with system output
- contract-boundary discipline in extension payload shaping

## Recommended Outcome

Proceed with a PBIR-first validation architecture that promotes signals gradually and treats validation as the primary product-design constraint.

The output of Story Assessment 2.0 Phase 1 should answer:

"Which signals deserve trust?"

before it answers:

"Which signals deserve UI space?"
