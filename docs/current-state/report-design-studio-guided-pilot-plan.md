# Report Design Studio Guided Internal Pilot Plan

Date: 2026-06-14

## Purpose

This pilot exists to validate Report Design Studio with real internal consulting workflows before further investment.

The current MVP is:

- ready for guided internal pilot usage
- not yet ready for broad self-serve consultant usage
- not ready to begin provider-backed generation

This document defines how to run the pilot without changing the product scope, architecture, or trust model.

## Scope

In scope:

- pilot planning
- guided internal pilot execution
- evidence capture from real workflow usage
- readiness evaluation for broader internal usage

Out of scope:

- new feature implementation
- architecture changes
- workflow expansion beyond the current MVP
- provider-backed generation
- provider integration work
- automatic mutation, materialization, validation, or deployment changes

## Authoritative Constraints

The pilot must preserve the current product boundaries:

- Design Brief, Concept Studio, Draft Studio, Prepare For Review, Review Design, Refinement Studio, and Compare Iterations remain the workflow under test.
- The pilot evaluates usability, trust, speed, and usefulness. It does not redesign the workflow.
- Design Approval, Refinement Approval, Materialization Approval, and Validation Approval remain separate.
- Ready, Approved, and Validated remain distinct workflow states with different owners and effects.
- Design Studio remains advisory-only.
- Analyzer Workspace remains the validation owner.
- Deterministic preview, apply, and rollback remains the only report-edit execution path outside this pilot scope.

## Pilot Objectives

Validate:

1. Workflow usability
2. Consultant adoption
3. Workflow speed
4. Approval understanding
5. Trust-boundary understanding
6. Design quality impact
7. Recommendation usefulness

## Primary Questions

The pilot must generate enough evidence to answer:

1. Is Report Design Studio ready for broader internal use?
2. What blocks self-serve consultant use?
3. What blocks provider-backed generation?
4. Which scenario performs best?
5. Which scenario performs worst?
6. Is another UX phase required?
7. Should provider-backed generation start now?

## Participants

Target participant roles:

- internal consultants
- solution architects
- report designers
- Power BI consultants

Recommended participant mix:

- at least 1 internal consultant
- at least 1 solution architect
- at least 1 report designer
- at least 1 Power BI consultant
- avoid concentrating the pilot in a single role or a single product team

Minimum participant count:

- 6 participants

Reason:

- enough to expose repeatable workflow friction across the three scenarios
- enough to compare experienced design practitioners with architecture-oriented reviewers
- still small enough to run as a guided pilot with direct observation

Ideal participant count:

- 10 to 12 participants

Reason:

- enough to identify pattern frequency by scenario and role
- enough to separate one-off opinions from recurring usability or trust-boundary issues
- still manageable for manual evidence review and synthesis

Optional structure:

- 2 pilot waves
- wave 1: 4 to 6 participants
- wave 2: 4 to 6 participants after only documentation or facilitation adjustments, not product changes

## Scenarios

Each participant should complete one run in each scenario.

### Scenario A: Executive Dashboard

Representative examples:

- CEO dashboard
- executive scorecard
- revenue and margin review

Validation emphasis:

- brief clarity
- concept confidence
- KPI and narrative readability
- approval comprehension at normal consulting speed

### Scenario B: Operational Monitoring

Representative examples:

- sales operations
- inventory management
- service management

Validation emphasis:

- navigation clarity
- operational KPI structure
- workflow speed
- recommendation usefulness during refinement

### Scenario C: Analytical Investigation

Representative examples:

- root cause analysis
- diagnostic reporting
- performance investigation

Validation emphasis:

- reasoning-path clarity
- evidence-flow readability
- trust and approval comprehension under higher cognitive load
- whether the MVP remains usable in the weakest current scenario

## Pilot Workflow

Each participant should complete:

Design Brief  
↓  
Concept Studio  
↓  
Draft Studio  
↓  
Prepare For Review  
↓  
Review Design  
↓  
Refinement Studio  
↓  
Compare Iterations

Required facilitation rules:

- the facilitator may explain the task goal and scenario context
- the facilitator may restate stage labels if the participant loses orientation
- the facilitator must not teach hidden product logic in advance
- the facilitator must not reinterpret approvals in a way that bypasses the product wording
- the facilitator must not compensate for unclear trust boundaries unless the participant is blocked
- every intervention must be recorded as evidence

## Pilot Execution Format

Recommended session format per participant:

- 10 minutes: briefing and scenario setup
- 20 to 30 minutes: guided workflow completion
- 10 to 15 minutes: structured debrief

Recommended total participant effort:

- 40 to 55 minutes per participant per scenario set when scenarios are scoped tightly

Recommended pilot sequencing:

1. Start with Scenario A or Scenario B.
2. Run Scenario C after the participant has seen at least one lower-friction scenario.
3. Randomize the first two scenarios across participants to reduce order bias.

## Data Capture

Capture evidence from:

- direct facilitator notes
- participant ratings
- observed hesitation or confusion
- participant quotes
- workflow completion state
- time tracking by stage
- approval and trust-boundary comprehension checks
- quality and usefulness feedback on recommendations

Evidence sources may include:

- manual observation notes
- screen recording if permitted internally
- timestamped stage transitions
- post-session questionnaire
- debrief transcript or written summary

## Measurements

### Workflow Completion

Record one outcome per scenario:

- completed
- partially completed
- abandoned

Interpretation guidance:

- completed: participant finishes all workflow stages and answers debrief questions
- partially completed: participant reaches a later stage but cannot complete the full workflow without heavy intervention
- abandoned: participant stops or the facilitator stops the session because the workflow is no longer productive

### Time Metrics

Capture:

- brief creation time
- concept approval time
- draft approval time
- refinement review time

Recommended measurement method:

- start timing when the participant begins the stage
- stop timing when the participant explicitly indicates readiness to move on or requests facilitator intervention
- record intervention count alongside time so slow sessions are not misread as product-only friction

### Understanding Metrics

Rate each stage on a 1-5 scale:

- Design Brief clarity
- Concept Studio clarity
- Draft Studio clarity
- Prepare For Review clarity
- Review Design clarity
- Refinement Studio clarity
- Compare Iterations clarity

Suggested scale:

- 1 = unclear
- 2 = mostly unclear
- 3 = mixed
- 4 = mostly clear
- 5 = clear

### Approval Understanding

Measure participant understanding of:

- Ready
- Approved
- Validated

Required check:

- ask the participant to explain each state in their own words
- record both rating and explanation quality

Suggested scale:

- 1 = cannot explain
- 2 = mostly incorrect
- 3 = partly correct
- 4 = mostly correct
- 5 = clearly correct

### Trust Understanding

Measure participant understanding of:

- Design Approval
- Refinement Approval
- Materialization Approval
- Validation Approval

Required check:

- ask who owns each approval and what it does not authorize

Success condition:

- participants should distinguish design workflow approval from validation authority without facilitator rescue

### Recommendation Quality

Rate:

- usefulness
- clarity
- actionability

Suggested scale:

- 1 = poor
- 2 = weak
- 3 = acceptable
- 4 = strong
- 5 = very strong

### Design Confidence

Ask:

“Would you trust this workflow for real client report design?”

Capture:

- yes
- yes, with guidance
- not yet

Also capture the reason in the participant’s own words.

## Success Metrics

The pilot should be considered directionally successful only if all of the following are met:

- at least 80% of participants complete Scenario A
- at least 80% of participants complete Scenario B
- at least 60% of participants complete Scenario C
- average clarity score is at least 4.0 for Design Brief, Concept Studio, Draft Studio, Review Design, and Refinement Studio
- average approval-understanding score is at least 4.0
- average trust-understanding score is at least 4.0
- average recommendation usefulness score is at least 4.0
- at least 70% of participants answer “yes” or “yes, with guidance” to the design-confidence question

These thresholds are intentionally stricter for broader internal readiness than for guided pilot completion.

## Adoption Metrics

Track:

- willingness to use the workflow again
- willingness to use it on a real client engagement
- willingness to recommend it to another consultant
- degree of facilitator dependence

Suggested adoption interpretation:

- healthy guided adoption: participant would use it again with light support
- weak guided adoption: participant sees value but still depends on facilitator translation
- poor adoption: participant does not see enough value or trust to reuse it

## Findings Classification

Classify findings as:

- Critical
- High
- Medium
- Low

Use these definitions:

- Critical: blocks workflow completion, breaks trust boundaries, or makes the MVP unsafe to expand
- High: materially slows adoption, creates repeated misunderstanding, or undermines confidence in real consulting use
- Medium: creates friction, avoidable confusion, or lower recommendation value without fully blocking use
- Low: minor polish issue with limited effect on completion or confidence

For each finding include:

- evidence
- frequency
- recommendation

Recommended frequency labels:

- isolated
- recurring
- common
- near-universal

## Readiness Criteria

### Ready For Broader Internal Use

Recommend only if:

- no Critical findings remain
- no repeated trust-boundary misunderstandings remain
- Scenario A and Scenario B perform strongly
- Scenario C is at least usable with guided support
- facilitator rescue is limited
- recommendation usefulness and design confidence are both strong

### Not Ready For Broad Self-Serve Consultant Use

Remain in this state if any of the following are true:

- participants repeatedly confuse approval states
- participants repeatedly confuse trust ownership
- Scenario C continues to underperform materially
- middle-stage workflow language still requires active translation
- participants do not trust the workflow without guidance

### Not Ready For Provider-Backed Generation

Remain in this state unless all of the following are true:

- self-serve workflow comprehension is strong
- approval and trust-boundary understanding are stable without facilitation
- participants can distinguish advisory design help from validation authority
- findings show that usability, not missing generation, is the main residual gap

## Decision Gate

At pilot close, provide one recommendation:

A. Expand Internal Usage

Use when:

- the guided pilot meets the success metrics
- findings are mostly Medium or Low
- adoption and trust are strong enough to widen internal access carefully

B. Run Another UX Improvement Cycle

Use when:

- the workflow is valuable but repeated friction remains
- self-serve readiness is still blocked by understanding, speed, or confidence gaps
- the pilot confirms the MVP direction but not the current usability ceiling

C. Pause Further Development

Use when:

- the workflow does not produce credible value in real consulting practice
- trust or approval confusion remains severe
- participants do not trust the workflow for real client work even with guidance

## Recommended Default Outcome Bias

Before the pilot runs, the expected default is:

- guided internal pilot should proceed
- broader self-serve consultant rollout should remain blocked unless the pilot materially outperforms the Round 3 expectation
- provider-backed generation should remain blocked unless the pilot shows unexpectedly strong self-serve comprehension and trust stability

## Deliverables

The pilot should produce:

- completed results workbook in `docs/report-design-studio-guided-pilot-results.md`
- scenario-by-scenario findings summary
- participant-count summary
- readiness decision using the A/B/C gate
- explicit answer to the seven final questions

## Definition Of Done

The pilot-planning work is complete when:

1. Pilot plan exists.
2. Pilot results template exists.
3. Success metrics exist.
4. Adoption metrics exist.
5. Readiness criteria exist.
6. Recommendation framework exists.
7. No code changes made.
8. Repo memory updated.
