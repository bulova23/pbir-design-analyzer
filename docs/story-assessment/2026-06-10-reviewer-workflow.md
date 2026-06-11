# Story Assessment Reviewer Workflow

Date: 2026-06-10

Status: Initial Level 1 expert-review workflow

## Goal

Define a repeatable reviewer workflow for PBIR-first Story Assessment validation.

The workflow exists to prevent reviewer anchoring, preserve disagreement data, and separate ambiguity from signal failure.

## Workflow Stages

### Stage 1: Human Judgment First

The reviewer inspects the PBIR page without seeing system output first.

The reviewer records:

- inferred story
- archetype choice
- coherence judgment
- competing story presence
- top story gaps
- reviewer confidence
- ambiguity or low-information flags

### Stage 2: System Output Second

After the independent judgment is recorded, the reviewer inspects the system output.

The reviewer compares:

- narrative direction
- archetype fit
- coherence signal
- competing story signal
- story-gap usefulness
- confidence explanation

### Stage 3: Disagreement Logging Third

The reviewer records any mismatch between human judgment and system output.

Each mismatch should state:

- what differed
- whether the page was ambiguous
- whether the system was materially wrong
- whether the system explanation was useful despite disagreement
- whether the guidance would still improve the page

## Ambiguous Versus Failed Handling

### Treat As Ambiguous

Use ambiguous when:

- the page supports multiple legitimate stories
- the page lacks enough visible evidence for a strong claim
- the page appears exploratory rather than narrative-led
- two archetypes remain plausible after review

### Treat As Failed

Use failed when:

- the system’s interpretation is materially misleading
- the system creates false certainty from weak evidence
- the signal explanation is not defensible from visible evidence
- the signal produces unhelpful remediation guidance

## Reviewer Disagreement Summary

At the end of a review batch, summarize:

- total pages reviewed
- pages with strong agreement
- pages with acceptable partial agreement
- pages marked ambiguous
- pages with failed system judgment
- recurring failure patterns

## Output Expectations

The workflow should produce review artifacts that are:

- comparable across reviewers
- inspectable by engineers
- strong enough to guide Level 1 promotion decisions
- reusable when building the later Level 2 corpus strategy
