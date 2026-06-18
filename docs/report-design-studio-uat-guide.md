# Report Design Studio UAT Guide

## Purpose

This guide validates the current executable Report Design Studio workflow against consultant expectations.

It is specifically intended to confirm:

- the documentation matches the executable workflow
- approvals are understandable
- trust boundaries are understandable
- the analyzer return path is understandable
- workflow completion and reopen are understandable

## UAT Rules

- Do not change code.
- Do not change workflow behavior.
- Do not rely on hidden seeded shortcuts when evaluating the user path.
- Record pass or fail from the visible workflow and these docs together.

## Common Setup

Before running any scenario:

1. Open VS Code.
2. Open the PBIR workspace.
3. Open the PBIR Design Analyzer explorer.
4. Launch PBIR Design Analyzer: Open Report Design Studio.
5. Confirm the workflow rail shows:
   - Design Brief
   - Concept Studio
   - Draft Studio
   - Prepare For Review
   - Review Design
   - Refinement Studio
   - Compare Iterations
   - Workflow Completion

## Core Workflow Validation

For every scenario, validate this exact path:

1. Complete Design Brief.
2. Approve Brief.
3. Generate Concepts.
4. Select a baseline.
5. Approve Concept.
6. Generate Draft.
7. Approve Draft.
8. Create Review Candidate.
9. Approve Review Candidate.
10. Launch Analyzer Workspace.
11. Return a real analyzer result.
12. Attach Analyzer Results.
13. Review Refinement Studio proposals.
14. Review Compare Iterations.
15. Complete Iteration.
16. Reopen Iteration if reopen behavior is being tested.

---

## Scenario A: Executive Dashboard

### Scenario Goal

Design a report for an executive audience that answers:

- Are we on target?
- Where is attention needed?
- What should leadership do next?

### Actions

1. Run the full core workflow.
2. Confirm the brief and concept language fit an executive audience.
3. Confirm the draft reads like an executive baseline.
4. Confirm Review Design makes the analyzer handoff feel intentional.
5. Confirm Workflow Completion feels like a real closeout step rather than a status badge.

### Expected Results

- executive intent is easy to express in Design Brief
- concept baseline choice is understandable
- draft review feels coherent
- analyzer ownership is clear
- completion is explicit and separate from validation approval

### Pass / Fail Checklist

- [ ] Design Brief is executable and understandable.
- [ ] Concept Studio is executable and understandable.
- [ ] Draft Studio is executable and understandable.
- [ ] Prepare For Review is executable and understandable.
- [ ] Review Design clearly signals Analyzer Workspace ownership.
- [ ] The analyzer return path is understandable.
- [ ] Attach Analyzer Results is understandable.
- [ ] Refinement Studio proposals read like executive design advice.
- [ ] Compare Iterations explains what changed.
- [ ] Workflow Completion clearly shows when the iteration is complete.

---

## Scenario B: Operational Monitoring

### Scenario Goal

Design a report for an operational audience that needs:

- recurring monitoring
- fast issue detection
- drill paths for action

### Actions

1. Run the full core workflow.
2. Confirm cadence, navigation, and evidence expectations are easy to capture in Design Brief.
3. Confirm concept and draft stages support operational flow decisions.
4. Confirm Prepare For Review and Review Design remain understandable.
5. Confirm reopen preserves a credible workflow state after completion.

### Expected Results

- operational design context is easy to capture
- concept and draft artifacts support navigation decisions
- review-candidate approval is understandable
- reopen preserves completion history

### Pass / Fail Checklist

- [ ] Design Brief supports operational context clearly.
- [ ] Concept Studio supports navigation and KPI structure decisions.
- [ ] Draft Studio makes the operational flow understandable.
- [ ] Prepare For Review does not feel opaque.
- [ ] Review Design feels intentional rather than like a failure state.
- [ ] Analyzer return and attach behavior are understandable.
- [ ] Reopen behavior is understandable and preserves audit history.
- [ ] Workflow Completion is distinct from validation approval.

---

## Scenario C: Analytical Investigation

### Scenario Goal

Design a report for diagnostic investigation where the user needs:

- a clear question
- an evidence path
- a drill sequence
- a conclusion path

### Actions

1. Run the full core workflow.
2. Confirm the investigative path can be expressed clearly in Design Brief.
3. Confirm Concept Studio makes the reasoning path understandable.
4. Confirm Draft Studio supports a real investigation flow.
5. Confirm the user can still follow Review Design, return, attach, comparison, and completion without facilitator help.

### Expected Results

- the workflow remains coherent
- the return path remains trustworthy
- closeout remains explicit

### Pass / Fail Checklist

- [ ] Design Brief supports analytical investigation intent.
- [ ] Concept Studio makes the investigation path understandable.
- [ ] Draft Studio makes the investigation draft reviewable.
- [ ] Review Design still feels safe and intentional.
- [ ] The analyzer return path remains understandable under higher cognitive load.
- [ ] Compare Iterations still makes improvement understandable.
- [ ] Workflow Completion still makes closeout understandable.
- [ ] The scenario is fast enough for self-serve consultant use.

---

## Cross-Scenario Questions

For every scenario, answer:

- [ ] Did the consultant know how to start?
- [ ] Did the consultant know what to do next at each stage?
- [ ] Did the consultant understand which approvals belonged to Design Studio versus Analyzer Workspace?
- [ ] Did the consultant understand that analyzer return requires an explicit attach step?
- [ ] Did the consultant know when the workflow was complete?
- [ ] Did the consultant understand how reopen works?
- [ ] Did the consultant complete the workflow from documentation and product behavior alone?

## Trust Boundary Checks

Record pass only if the consultant can explain:

- Design Studio owns design approvals, refinement decisions, completion, and reopen
- Analyzer Workspace owns validation
- completing the workflow does not imply validation approval
- attached analyzer results remain analyzer-owned

## Evidence To Record

Record:

- points of hesitation
- unclear labels
- unclear ownership transitions
- unclear analyzer return behavior
- unclear completion or reopen behavior
- places where the docs and shell still disagree
- places where the workflow is correct but too slow for self-serve use

## Exit Decision

Choose one:

- Pass for guided pilot
- Pass for self-serve consultant use
- Fail for self-serve consultant use

Use [report-design-studio-uat-gap-analysis.md](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/docs/report-design-studio-uat-gap-analysis.md:1) to record the gaps that remain after the run.
