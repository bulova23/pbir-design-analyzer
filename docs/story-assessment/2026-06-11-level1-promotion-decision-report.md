# Story Assessment 2.0 Level 1 Promotion Decision Report

Date: 2026-06-11

Status: Level 1 promotion decision after the latest targeted tuning pass

Scope:

- PBIR-only
- backend-internal evidence only
- no public contract changes
- no score-panel changes
- no UI changes

Implementation follow-up:

- The approved first slice has now been implemented as Guided Story Improvements.
- The shipped surface stays constrained to the six validated Story Gap categories listed in this report.
- The broader Story Assessment internals discussed below remain internal-only.

## Decision Summary

Story Assessment 2.0 is ready for narrow contract-promotion design only in one area:

- high-value Story Gap candidates that point to visible, author-actionable report improvements

Everything else should remain internal for now or move to broader validation before any public design work.

The strongest result from Level 1 is not a full page-story classifier. It is a bounded set of evidence-backed authoring gaps that are:

- actionable
- explainable
- stable enough to describe without exposing internal diagnostics

## Level 1 Evidence Base

Corpus used:

- `Sales Analysis`
- `Sales & Production`
- duplicate `Sales & Production` copy for determinism

Latest validation posture:

- backend tests passed: `211` passed, `0` failed
- duplicate `Sales & Production` export remained deterministic
- special-page false positives were reduced materially in the previous tuning slice
- targeted follow-up removed the `Customer Analysis` overclaim from `PerformanceMonitor` to `NarrativeWalkthrough`
- `RetKeyInf` remains unresolved on the real corpus because the page still lacks bounded supporting cues

Observed strengths:

- deterministic output
- useful Story Gap records
- better special-page guardrails
- evidence-backed diagnostics
- narrowed false-positive behavior

Observed weaknesses:

- full archetype output still overgeneralizes some pages
- semantic coherence is still too conservative to expose directly
- Confidence Breakdown is useful internally but not sufficiently discriminative for contract promotion
- compact or custom page variants still create edge-case misses

## 1. Outputs Ready For Narrow Contract-Promotion Design

Ready now:

- Story Gap Assessment, but only as a filtered subset of high-value visible report issues

Recommended promotion candidates:

- missing title/question anchor
- missing benchmark/target
- missing prior-period context
- missing primary metric
- missing primary dimension
- scattered filters

Reason:

- these gaps were repeatedly useful in Level 1 review
- they are explainable without exposing internal scoring machinery
- they map to visible report improvements
- they can be phrased as advisory findings without leaking internal lifecycle fields

Promotion posture:

- advisory-only
- page-level
- no raw evidence IDs
- no raw confidence internals
- no promotion-state or surface-scope exposure

## 2. Outputs That Must Remain Internal

Must remain internal:

- raw Signal Registry
- Special Page Assessment records
- promotion states
- surface-scope classifications
- raw archetype match results
- full Archetype Classification
- raw semantic extracted terms and coherence clusters
- competing-story diagnostics
- raw topology penalties and reinforcement notes
- full Confidence Breakdown Assessment
- evidence reference IDs
- remediation-layer internals

Reason:

- these outputs are still diagnostic infrastructure, not product-ready user explanations
- several of them are still unstable across edge cases
- many are useful only as hidden guardrails or reviewer tooling
- exposing them would overfit the current narrow corpus and leak implementation detail

## 3. Outputs That Require More Corpus Validation

Require broader Level 1 corpus validation before any promotion design:

- Special Page Assessment as a surfaced concept
- filtered Archetype Classification summaries
- Semantic Coherence summaries
- filtered Competing Story warnings
- compact Key Influencer alias handling
- customer or segmentation diagnostic handling

Reason:

- special-page handling is already valuable as a hidden guardrail, but not yet reliable enough to expose as a user-facing classification
- `RetKeyInf` remains unresolved in the real corpus
- `Customer Analysis` improved, but still did not meet the conservative threshold for explicit segmentation-page labeling
- the current corpus is still too small to decide whether these labels generalize

## 4. Outputs That Require Level 2 Formal Corpus Validation

Require Level 2 validation before any serious contract promotion:

- full Archetype Classification
- full Semantic Coherence output
- competing-story promotion
- Confidence Breakdown Assessment
- cross-surface promotion claims
- any score-like or confidence-like Story Assessment summary

Reason:

- these outputs are classification-heavy and more sensitive to false positives
- they need broader report diversity, reviewer agreement, and formal false-positive tracking
- Level 1 evidence shows usefulness for internal diagnosis, not readiness for product exposure

## 5. Story Gaps Strong Enough To Consider For UI Exposure

Strong enough for first-slice consideration:

- missing title/question anchor
- missing benchmark/target
- missing prior-period context
- missing primary metric
- missing primary dimension
- scattered filters

Why these made the cut:

- they align with the strongest repeated Level 1 observations
- they are visible and remediable in the report layer
- they avoid speculative model-only advice
- they stay understandable without explaining internal clustering or archetype logic

Not ready for UI exposure:

- generic semantic metadata advice
- low-confidence model-layer guidance
- competing-story restructure advice
- coherence-driven narrative conflict advice
- special-page-specific diagnostics

## 6. Page Types That Should Remain Diagnostic-Only

Keep diagnostic-only:

- Tooltip
- Q&A
- What If
- Key Influencers
- Market Basket
- Reference / Legal
- Validation / Sandbox
- Customer / Segmentation Diagnostic

Reason:

- their current value is as hidden classification guardrails that prevent bad overclaims
- surfacing these page types now would create product semantics that have not yet been validated broadly
- some remain partially unresolved on compact or custom variants

Recommended posture:

- use special-page handling internally to suppress or downgrade inappropriate promotion
- do not expose special-page labels in the first user-facing slice

## 7. Recommended First User-Facing Story Assessment 2.0 Slice

The first user-facing slice should be:

- a small advisory Story Gaps set
- limited to the six candidate gaps above
- phrased as page-level narrative clarity and decision-context findings
- guarded internally by special-page suppression rules

It should not include:

- archetype labels
- coherence labels
- confidence breakdown
- competing-story language
- special-page labels
- raw evidence traces

Recommended form:

- additive advisory findings, not a new score
- narrow page-level messages
- only visible report-authoring improvements
- hidden backend guardrails to suppress false positives on diagnostic-only pages

## Promoted Candidates

- `missing title/question anchor`
- `missing benchmark/target`
- `missing prior-period context`
- `missing primary metric`
- `missing primary dimension`
- `scattered filters`
- `special page handling` as hidden guardrail only

## Internal-Only Fields

- Signal Registry
- PromotionState
- StoryAssessmentSurfaceScope
- Special Page Assessment
- Archetype Classification
- Semantic Coherence clusters and competing-story internals
- Filter Topology penalties
- Confidence Breakdown Assessment
- raw evidence references

## Deferred Fields

- filtered archetype summaries
- filtered semantic coherence summaries
- competing-story warnings
- explicit customer/segmentation diagnostic labeling
- compact special-page alias handling beyond bounded current rules

## Risks

- the corpus is still too small for broad contract decisions
- coherence remains too conservative for product-facing interpretation
- unresolved compact variants like `RetKeyInf` show that some page families still need bounded parser or evidence improvements
- exposing classification outputs too early would make future tuning harder because product language would harden around unstable internals

## Next Implementation Recommendation

Do not promote full Story Assessment structures.

Next recommended slice:

1. Design a narrow advisory contract for the six Story Gap candidates only.
2. Keep special-page handling internal as a suppression guardrail.
3. Run a broader Level 1 corpus before considering any archetype or coherence exposure.
4. Reserve Level 2 formal validation for full classification, confidence, and competing-story outputs.

## Bottom Line

Ready for narrow contract-promotion planning:

- filtered Story Gaps only

Not ready for promotion:

- classification-heavy Story Assessment internals

Best first product slice:

- a small, evidence-backed Story Gaps layer that tells report authors when visible narrative anchors, context, or filter structure are missing
