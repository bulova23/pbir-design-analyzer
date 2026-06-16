# Report Design Studio UAT Guide

## Purpose

This guide provides consultant-style user acceptance testing for the current Report Design Studio MVP.

It is written to validate whether the MVP is understandable, usable, and trustworthy for three common report-design scenarios.

## UAT Rules

- Do not change code.
- Do not seed hidden state during the walkthrough unless the scenario explicitly calls for it.
- Record pass or fail based on what a consultant can understand and complete from the visible product workflow.
- If a stage cannot be completed because the required control is missing, mark it as a fail and log it in the gap analysis.

## Common Setup

Before running any scenario:

1. Open VS Code.
2. Open the PBIR workspace.
3. Open the **PBIR Design Analyzer** explorer.
4. Launch **PBIR Design Analyzer: Open Report Design Studio**.
5. Confirm the workflow rail shows:
   - Design Brief
   - Concept Studio
   - Draft Studio
   - Prepare For Review
   - Review Design
   - Refinement Studio
   - Compare Iterations

---

## Scenario A: Executive Dashboard

### Scenario Goal

Design a report for an executive audience that quickly answers:

- Are we on target?
- Where is attention needed?
- What should leadership do next?

### Starting State

- Report Design Studio opens for the target PBIR report.
- No prior consultant context is assumed.

### Actions

1. Launch Report Design Studio.
2. Read the shell header and current stage.
3. Open Design Brief and determine whether the report purpose is understandable.
4. Open Concept Studio and compare the available concept direction.
5. Determine whether a baseline choice is understandable.
6. Open Draft Studio and decide whether the draft feels like an executive dashboard baseline.
7. Open Prepare For Review and confirm whether the readiness state is understandable.
8. Open Review Design and confirm whether the handoff to Analyzer Workspace is understandable.
9. Open Refinement Studio and review the recommendation language.
10. Open Compare Iterations and confirm whether improvement is understandable.

### Expected Results

- The workflow purpose is understandable.
- Executive design language is recognizable.
- The difference between concept, draft, review, and refinement is understandable.
- Approval teaching is understandable.
- Refinement proposals read like executive design advice.

### Pass / Fail Checklist

- [ ] Launch path is obvious.
- [ ] Design Studio purpose is understandable from the shell.
- [ ] Design Brief tells the user what this stage is for.
- [ ] Concept Studio makes the executive baseline choice understandable.
- [ ] Draft Studio makes the draft feel reviewable.
- [ ] Prepare For Review explains readiness in consultant language.
- [ ] Review Design clearly signals that Analyzer Workspace owns validation.
- [ ] Refinement Studio recommendations are understandable.
- [ ] Compare Iterations explains what improved.
- [ ] A new consultant could complete this scenario without facilitator help.

### Notes

- Record where the user hesitates.
- Record where architecture vocabulary becomes visible.

---

## Scenario B: Operational Monitoring

### Scenario Goal

Design a report for an operational audience that needs:

- recurring monitoring
- fast detection of issues
- drill paths for action

### Starting State

- Report Design Studio opens for the target PBIR report.
- The report is assumed to support repeated operational review.

### Actions

1. Launch Report Design Studio.
2. Open Design Brief and assess whether cadence, navigation, and evidence context are easy to understand.
3. Open Concept Studio and assess whether KPI hierarchy and navigation structure support operational monitoring.
4. Open Draft Studio and assess whether the pages and navigation feel like an operational flow.
5. Open Prepare For Review and assess whether readiness and diagnostics are understandable.
6. Open Review Design and confirm whether the handoff feels intentional.
7. Open Refinement Studio and assess whether navigation and KPI recommendations feel useful.
8. Open Compare Iterations and assess whether the user can explain the operational improvement path.

### Expected Results

- Navigation and KPI structure are easy to understand.
- Draft review gives enough confidence to continue.
- The middle stages do not feel like engineering-only states.
- Refinement suggestions feel operationally relevant.

### Pass / Fail Checklist

- [ ] Design Brief supports operational design context clearly.
- [ ] Concept Studio exposes enough structure for navigation decisions.
- [ ] Draft Studio makes the report flow understandable.
- [ ] Prepare For Review does not feel opaque.
- [ ] Review Design feels like a workflow step, not a failure state.
- [ ] Refinement Studio suggestions are relevant to monitoring/report flow.
- [ ] Compare Iterations helps the user understand what changed.
- [ ] A new consultant could complete this scenario without facilitator help.

---

## Scenario C: Analytical Investigation

### Scenario Goal

Design a report for diagnostic investigation where the user needs:

- a clear question
- an evidence path
- a drill sequence
- a conclusion path

### Starting State

- Report Design Studio opens for the target PBIR report.
- The user does not know the Design Studio architecture.

### Actions

1. Launch Report Design Studio.
2. Open Design Brief and assess whether the investigative purpose can be expressed clearly.
3. Open Concept Studio and assess whether the question, investigation path, evidence, and conclusion are understandable.
4. Open Draft Studio and assess whether the draft supports a real investigation workflow.
5. Open Prepare For Review and assess whether the readiness step is still understandable under higher cognitive load.
6. Open Review Design and assess whether the handoff is trustworthy.
7. Open Refinement Studio and assess whether investigation-focused proposals are understandable and actionable.
8. Open Compare Iterations and assess whether the user can tell whether the investigation got better.

### Expected Results

- The investigative path should be understandable.
- The concept stage should help the user judge the reasoning flow.
- Draft review should help the user judge whether the investigative sequence is coherent.
- Refinement should make the reasoning weaknesses obvious.

### Pass / Fail Checklist

- [ ] Design Brief supports analytical investigation design intent.
- [ ] Concept Studio makes the investigation path understandable.
- [ ] Draft Studio makes the investigation draft feel reviewable.
- [ ] Prepare For Review remains understandable under complex reasoning needs.
- [ ] Review Design still feels safe and intentional.
- [ ] Refinement Studio highlights reasoning problems clearly.
- [ ] Compare Iterations shows whether the investigation improved.
- [ ] A new consultant could complete this scenario without facilitator help.

---

## Cross-Scenario Questions

For every scenario, answer:

- [ ] Did the consultant know how to start?
- [ ] Did the consultant know what to do next at each stage?
- [ ] Did the consultant understand which approvals belonged to Design Studio versus Analyzer Workspace?
- [ ] Did the consultant know when the workflow was complete?
- [ ] Did the consultant complete the workflow from documentation and product behavior alone?

## Evidence To Record

Record:

- points of hesitation
- unclear labels
- missing buttons
- missing transitions
- missing approval explanations
- places where the shell explains a workflow step but does not let the user perform it

## Exit Decision

At the end of UAT, choose one:

- `Pass for guided pilot`
- `Pass with major caveats`
- `Fail for self-serve consultant use`

The current MVP should be expected to land in either:

- `Pass for guided pilot`
- `Fail for self-serve consultant use`

See [report-design-studio-uat-gap-analysis.md](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/docs/report-design-studio-uat-gap-analysis.md:1).
