# Report Design Studio Workflow Walkthrough

## Purpose

This walkthrough matches the current executable Report Design Studio workflow.

It is written for the current shell, not for earlier read-only MVP descriptions.

## Before You Start

1. Open the PBIR workspace in VS Code.
2. Open the PBIR Design Analyzer explorer.
3. Launch PBIR Design Analyzer: Open Report Design Studio.
4. Confirm the workflow rail shows:
   - Design Brief
   - Concept Studio
   - Draft Studio
   - Prepare For Review
   - Review Design
   - Refinement Studio
   - Compare Iterations
   - Workflow Completion

## End-To-End Path

Use this exact sequence:

1. Complete Design Brief.
2. Approve Brief.
3. Open Concept Studio.
4. Generate Concepts.
5. Select Baseline.
6. Approve Concept.
7. Open Draft Studio.
8. Generate Draft.
9. Approve Draft.
10. Open Prepare For Review.
11. Create Review Candidate.
12. Approve Review Candidate.
13. Open Review Design.
14. Launch Analyzer Workspace.
15. Complete review in Analyzer Workspace.
16. Return real analyzer results to Design Studio.
17. Attach Analyzer Results.
18. Open Refinement Studio.
19. Review proposal decisions.
20. Open Compare Iterations.
21. Open Workflow Completion.
22. Complete Iteration.

## Stage 1: Design Brief

### What You Do

1. Open Design Brief.
2. Fill the required brief fields.
3. Use Save Draft to validate and persist the brief.
4. Use Submit For Approval.
5. Use Approve Brief.

### What The Stage Means

This is the design intent baseline for the thread.

It captures:

- who the report is for
- what business objective it supports
- what decisions it should enable
- which KPIs and dimensions matter
- what story and navigation path the report should follow

### Done Signal

- the approval status is Approved
- Concept Studio unlocks
- the shell guidance changes to continue to Concept Studio

## Stage 2: Concept Studio

### What You Do

1. Open Concept Studio.
2. Use Generate Concepts.
3. Review concept alternatives.
4. Choose a preferred baseline.
5. Submit the baseline for approval.
6. Approve the concept baseline.

### What The Stage Means

Concept Studio creates and compares report-design directions before any draft is accepted.

It is still design-only. No review candidate exists yet.

### Done Signal

- one concept is selected as the preferred baseline
- concept approval is complete
- Draft Studio unlocks

## Stage 3: Draft Studio

### What You Do

1. Open Draft Studio.
2. Use Generate Draft.
3. Review the generated draft artifacts:
   - Draft Pages
   - Draft Layouts
   - Draft Navigation
   - KPI Placement
4. Submit the draft for approval.
5. Approve the draft.

### What The Stage Means

Draft Studio turns the approved concept baseline into a reviewable draft baseline.

The draft is isolated and non-production. It is still not analyzer validation and still not a report mutation.

### Done Signal

- the draft is approved
- Prepare For Review unlocks

## Stage 4: Prepare For Review

### What You Do

1. Open Prepare For Review.
2. Review readiness, lineage, approvals used, and diagnostics.
3. Use Create Review Candidate.
4. Submit the candidate for approval.
5. Approve the review candidate.

### What The Stage Means

This stage prepares an analyzable candidate from the approved draft without changing the report.

This is where Design Studio confirms the candidate is ready to be handed to Analyzer Workspace.

### Done Signal

- candidate status is approved
- Review Design unlocks

## Stage 5: Review Design

### What You Do

1. Open Review Design.
2. Review Candidate Summary, Review Readiness, Review Status, and Analyzer Ownership.
3. Use Open Analyzer Workspace.
4. Complete the review in Analyzer Workspace.
5. Return to Review Design.
6. Confirm the shell shows analyzer results are available.
7. Use Attach Analyzer Results.

### What The Stage Means

Review Design is the handoff and return loop between Design Studio and Analyzer Workspace.

This stage makes the trust boundary explicit:

- Design Studio launches review only
- Analyzer Workspace owns validation
- analyzer results return from Analyzer Workspace
- Design Studio must attach the returned result explicitly

### Return Path States

The executable return path moves through:

1. Review Not Started
2. Review Launched
3. Awaiting Analyzer Results
4. Analyzer Results Available
5. Results Attached

### Done Signal

- analyzer results are attached to the iteration
- Refinement Studio unlocks

## Stage 6: Refinement Studio

### What You Do

1. Open Refinement Studio.
2. Review advisory proposals created from the attached analyzer result.
3. Approve, Reject, or Defer proposals as needed.

### What The Stage Means

Refinement Studio is advisory-only.

It helps the consultant decide what design changes should shape the next iteration.

It does not grant validation approval and it does not edit the report automatically.

### Done Signal

- proposal decisions are recorded clearly enough to compare iterations and close the workflow intentionally

## Stage 7: Compare Iterations

### What You Do

1. Open Compare Iterations.
2. Review what changed between iterations.
3. Confirm attached analyzer-result history and approval evolution are understandable.

### What The Stage Means

Compare Iterations is the history and comparison surface.

Use it to confirm:

- what changed
- what was accepted, rejected, or deferred
- whether validation status changed
- which analyzer-backed result belongs to the iteration

### Done Signal

- the current iteration history is understandable
- you are ready to close or reopen the workflow intentionally

## Stage 8: Workflow Completion

### What You Do

1. Open Workflow Completion.
2. Review the completion checklist.
3. Review outstanding items, approvals satisfied, and recommendation summary.
4. Use Complete Iteration when the workflow is ready to close.
5. Use Reopen Iteration later if more work is needed.

### What The Stage Means

Workflow Completion is a separate stage because closeout is not the same thing as approval or validation.

It records:

- checklist state
- completion state
- completion audit history
- reopen audit history

### Done Signal

- completion state is Completed
- audit details are recorded

## Approval And Ownership Model

Design Studio owns:

- brief approval
- concept approval
- draft approval
- review-candidate approval
- refinement decisions
- workflow completion
- workflow reopen

Analyzer Workspace owns:

- analyzer execution
- findings
- validation approval
- validation provenance

## Trust Boundary Summary

Design Studio prepares, launches, records, and compares workflow state.

Analyzer Workspace evaluates the review candidate and owns the validation result.

Design Studio cannot:

- validate the design itself
- infer validation approval from completion
- skip the attach step after analyzer return

## When The Workflow Is Complete

The workflow is complete when:

1. the brief, concept, draft, and review candidate are approved
2. Analyzer Workspace has reviewed the candidate
3. real analyzer results have returned
4. those results are attached
5. refinement decisions are recorded
6. iteration comparison has been reviewed
7. Workflow Completion is explicitly marked complete

## Reopen Workflow

Use Reopen Iteration when a completed iteration needs more refinement or follow-up analysis.

Reopen preserves the audit trail. It does not erase prior completion history.

## Screenshots

No current Design Studio workflow screenshots are available in the repository for this walkthrough, so this alignment update does not add screenshots.
