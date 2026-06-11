# Story Assessment Reviewer Rubric

Date: 2026-06-10

Status: Initial Level 1 expert-review rubric

## Purpose

This rubric standardizes PBIR-first expert review for Story Assessment 2.0 validation.

It is intended to be stronger than informal commentary while remaining lighter than a formal benchmark corpus.

## Reviewer Order Of Operations

Reviewers must work in this order:

1. human judgment first
2. system output second
3. disagreement logging third

This prevents the system output from anchoring the reviewer before an independent page judgment is recorded.

## Required Reviewer Questions

For each PBIR page, the reviewer should answer:

1. What story is this page trying to tell?
2. Which archetype best fits the page?
3. Is the page semantically coherent?
4. Are competing stories present?
5. What story gaps materially weaken decision support?
6. How confident is the reviewer in that judgment?
7. Is the reasoning explainable?
8. Is the guidance actionable for a report author?

## Reviewer Output Fields

Each review record should capture:

- inferred story
- archetype choice
- archetype confidence
- coherence judgment
- competing story presence
- top story gaps
- reviewer confidence judgment
- explanation quality
- actionability quality
- ambiguity notes
- disagreement notes, when applicable

## Bounded Rating Scales

### Inferred Story Quality

- strong
- partial
- weak

### Archetype Fit

- correct
- acceptable
- incorrect

### Coherence Judgment

- coherent
- borderline
- incoherent

### Competing Story Presence

- present
- borderline
- absent

### Story Gap Usefulness

- actionable
- partly actionable
- not actionable

### Confidence Judgment

- high
- medium
- low

### Explanation Quality

- clear
- partially clear
- unclear

### Actionability Quality

- direct
- indirect
- diagnostic only

## Independent Human Judgment Stage

Before viewing any system output, the reviewer should record:

- inferred story
- archetype choice
- coherence judgment
- competing story presence
- top story gaps
- confidence judgment

If the page is ambiguous or low-information, the reviewer should say so explicitly rather than forcing certainty.

## System Comparison Stage

After independent judgment is recorded, the reviewer may inspect system output and compare:

- story inference
- archetype selection
- coherence signal
- competing story signal
- gap quality
- confidence explanation

The reviewer should focus on whether the system is:

- directionally correct
- overconfident
- under-explained
- non-actionable

## Disagreement Logging Stage

If the reviewer and the system differ, record:

- what differed
- whether the page was ambiguous
- whether the system was wrong or merely too certain
- whether the system explanation was still useful
- whether the resulting guidance would help improve the page anyway

## Ambiguous Versus Failed Signal Handling

### Ambiguous

Mark a signal as ambiguous when:

- the page legitimately supports more than one narrative reading
- the evidence is weak or incomplete
- multiple archetypes are reasonable
- reviewers could disagree without either interpretation being clearly wrong

### Failed

Mark a signal as failed when:

- the system inferred the wrong narrative direction
- the archetype is materially misleading
- coherence or competing-story detection is clearly incorrect
- the explanation cannot be defended from visible evidence
- the guidance is not useful for improving report quality

## Four-Dimension Review Lens

Every signal should be judged across:

- accuracy
- consistency
- explainability
- actionability

Reviewers should not treat raw correctness as sufficient for promotion.

## Promotion Interpretation

Level 1 success means a signal is eligible for contract exposure, not that it is platform-critical.

Signals that are:

- accurate but opaque
- accurate but non-actionable
- useful but inconsistent

should remain internal or advisory until stronger evidence exists.
