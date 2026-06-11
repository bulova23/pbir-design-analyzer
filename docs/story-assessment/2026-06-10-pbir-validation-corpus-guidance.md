# PBIR Story Assessment Validation Corpus Guidance

Date: 2026-06-10

Status: Initial Level 1 expert-review guidance

## Purpose

This guidance defines the PBIR-first corpus strategy for Story Assessment 2.0 Level 1 validation.

The goal is to validate narrative signals against real PBIR pages before those signals become product-facing dependencies.

## Phase 1 Scope

This corpus is for PBIR expert-review validation only.

It is intended to support evaluation of:

- Signal Registry behavior
- Archetype Classification
- Semantic Coherence
- Filter Topology
- Story Gaps
- Confidence Breakdown

It is not intended to validate Fabric Apps or Report Design Studio outputs in Phase 1.

## Recommended Corpus Size

- 20 to 50 real PBIR pages
- at least 5 distinct PBIR reports

This size is intended to support fast learning without waiting for a full formal benchmark corpus.

## Required Corpus Diversity

The corpus should include variation across:

- report purpose
- page density
- page layout style
- narrative clarity
- filter complexity
- semantic model quality
- page role

Target page-role diversity should include, where available:

- executive overview pages
- operational monitoring pages
- analytical drill or diagnosis pages
- appendix or reference-like pages

## Inclusion Criteria

Include PBIR pages that meet all of the following:

- page loads from a real PBIR report under normal scoring conditions
- page contains enough visible content to support narrative inference
- page can be reviewed independently by a human reviewer
- page is representative of real authoring patterns rather than synthetic demo-only edge cases

## Exclusion Criteria

Exclude pages when any of the following applies:

- page is structurally broken or unreadable due to fixture corruption
- page is almost entirely empty or hidden-state driven
- page contains too little visible evidence to support a meaningful narrative judgment
- page is a pure utility shell with no reviewable narrative surface
- page depends on unavailable external context that reviewers cannot reconstruct

Excluded pages may still be useful later for robustness testing, but they should not anchor Level 1 signal quality judgments.

## Ambiguous And Low-Information Page Handling

Some PBIR pages should remain in the corpus even when they are hard to classify.

These should be marked rather than discarded when they are:

- genuinely multi-purpose
- intentionally exploratory
- low-information but still representative of real author behavior
- weakly titled or semantically sparse

For those pages, reviewers should explicitly label:

- ambiguous story intent
- low-information narrative surface
- partial evidence only

These pages are important because they reveal where a signal should remain cautious rather than overconfident.

## Corpus Composition Guidance

The corpus should not be dominated by a single team, style, or archetype.

Avoid:

- only polished executive dashboards
- only one report family
- only simple comparison pages
- only pages with strong semantic metadata

Prefer a deliberate mix of:

- strong and weak titles
- strong and weak measure descriptions
- clear and noisy semantic clusters
- simple and complex filter setups
- coherent and competing narrative pages

## Sampling Guidance

When more than 50 candidate pages are available, sample to preserve breadth rather than volume.

Prefer adding a different report or page type over adding another near-duplicate page.

## Reviewer Metadata To Capture

For each page in the corpus, record:

- report identifier
- page name
- page role guess
- reviewer confidence in judging the page
- whether the page is ambiguous or low-information
- notes on missing context

## Corpus Maintenance

The Level 1 corpus should be versioned as a reviewed set rather than treated as a permanent benchmark.

Update the corpus only when:

- new report families materially improve diversity
- existing pages prove unreviewable
- reviewer feedback shows the set is biased toward one narrative pattern

## Relationship To Level 2

This guidance is intentionally lighter than the future Level 2 formal corpus.

Level 2 will require:

- larger scale
- labeled benchmark assets
- multi-reviewer agreement analysis
- repeatable calibration

Level 1 should optimize for fast trustworthy learning.
