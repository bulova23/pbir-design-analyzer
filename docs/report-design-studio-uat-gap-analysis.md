# Report Design Studio UAT Gap Analysis

## Purpose

This document records the workflow gaps that prevent Report Design Studio MVP from being fully self-serve for a new consultant using documentation alone.

## Summary

Report Design Studio MVP has a coherent workflow model and a useful shell.

It does not yet expose a complete self-serve consultant action path across the early and middle stages.

The product can be explained.

The product cannot yet be fully completed by a new consultant from the shipped UI alone.

## Gap 1: Early-Stage Authoring Actions Are Not Fully Exposed In The Main Shell

### Problem

The repository contains Design Brief, Concept Studio, and Draft Studio foundations with actions such as:

- Save Brief
- Request Approval
- Generate Concepts
- select baseline
- approve baseline
- generate draft artifacts

But the main shipped Design Studio shell currently emphasizes review summaries, approval teaching, and downstream workflow surfaces rather than a fully connected stage-local authoring path.

### User Impact

A new consultant can understand what Design Brief, Concept Studio, and Draft Studio are for, but cannot reliably complete those stages from the main shell alone.

### Documentation Impact

Documentation can explain the expected workflow, but must honestly say that some required controls are not fully present in the current shell.

### Priority

Critical

## Gap 2: Approval Progress Is Better Taught Than Executed

### Problem

The MVP clearly explains:

- Ready
- Approved
- Validated

and:

- Design Approval
- Materialization Approval
- Refinement Approval
- Validation Approval

But the user can often see approval teaching more clearly than the actual approval action path for the early stages.

### User Impact

The consultant understands the meaning of approvals, but may not know how to produce them in the product for the Design Brief, Concept, and Draft stages.

### Priority

Critical

## Gap 3: Prepare For Review Still Depends On Platform Vocabulary

### Problem

Prepare For Review is a better stage label than raw materialization language, but the stage detail still exposes:

- analyzer
- profile
- executable eligibility

This is accurate, but not fully consultant-native.

### User Impact

A new consultant can follow the stage, but may not know what to do when they see technical readiness detail instead of a plain-language recommendation.

### Priority

High

## Gap 4: Review Design Still Requires Architecture Trust

### Problem

The handoff is structurally correct and the **Open Analyzer Workspace** button is explicit.

But the user still needs to understand that:

- Design Studio is finished with design preparation
- Analyzer Workspace now owns validation
- the handoff is not a failure state

### User Impact

A new consultant may hesitate because the workflow crosses tools and the current MVP still exposes that boundary as a platform transition rather than a seamless business workflow step.

### Priority

High

## Gap 5: Concept Review Is Understandable But Not Yet Fast

### Problem

Concept Studio now exposes:

- Chapter Structure
- KPI Hierarchy
- Navigation Structure
- Analytical Flow
- comparison blocks

But review is still list-heavy and scroll-heavy.

### User Impact

A consultant can understand the concept comparison, but not at the speed expected for self-serve decision-making in more complex scenarios.

### Priority

Medium

## Gap 6: Draft Review Is Better Than Before But Still Not Tangible Enough For Broad Self-Serve Use

### Problem

Draft Studio now exposes real review structure, but it is still primarily textual and structural.

It does not yet make the draft feel like a visible report design artifact in the way many consultants expect.

### User Impact

Consultants may approve a draft based on workflow confidence rather than clear design evidence.

### Priority

Medium

## Gap 7: Analytical Investigation Is Still The Weakest Scenario

### Problem

Analytical investigation requires the strongest reasoning visibility:

- question
- investigation path
- evidence sequence
- conclusion path

The MVP exposes these better than earlier phases, but still mostly through text-heavy presentation.

### User Impact

A new consultant is least likely to trust the workflow in this scenario without help.

### Priority

High

## Gap 8: Compare Iterations Explains Change Better Than It Shows Change

### Problem

Compare Iterations has strong text sections:

- What Improved
- What Was Accepted
- What Changed
- Approval Evolution
- Validation Evolution

But it remains more audit-oriented than design-review-oriented.

### User Impact

The consultant can describe the iteration history, but may not quickly see whether the design experience itself improved.

### Priority

Medium

## Gap 9: Workflow Completion Is Understandable, But The Product Does Not Yet Close The Loop For A New User

### Problem

Documentation can define “done” clearly:

- design baseline accepted
- review candidate prepared
- Analyzer Workspace reviewed it
- refinement decisions recorded
- iteration comparison reviewed

But the product still expects the user to infer some of that closure rather than making it explicit in a final workflow state.

### User Impact

A new consultant may finish the work without feeling sure they are truly finished.

### Priority

Medium

## Missing Workflow Steps

- A fully exposed shell-based Design Brief completion and approval path
- A fully exposed shell-based Concept selection and approval path
- A fully exposed shell-based Draft approval path
- A clearer “return from Analyzer Workspace” completion cue
- A clearer final completion signal for the entire iteration

## Missing Buttons Or Controls

- Design Brief action controls are not fully exposed in the main shell
- Concept generation and baseline approval controls are not fully exposed in the main shell
- Draft approval controls are not fully exposed in the main shell
- A stage-local explicit “continue” or “next recommended step” control is missing in several stages

## Missing Transitions

- clearer early-stage transition from brief to concept
- clearer transition from concept approval to draft approval
- clearer post-review return transition from Analyzer Workspace into Refinement Studio
- clearer final transition from refinement decisions into a “workflow complete” state

## Unclear Ownership Areas

These are improved, but still not fully effortless for a first-time consultant:

- why Design Approval is not Validation Approval
- why Materialization Approval does not mean analysis already happened
- why Review Design opens Analyzer Workspace instead of validating in place
- why Compare Iterations is the validation-aware closeout view

## Places Documentation Cannot Fully Bridge The Product

Documentation cannot fully compensate for:

- missing stage-local action controls
- incomplete early-stage shell execution paths
- technical middle-stage detail language
- lack of a strong final workflow completion state

If a required button or transition does not exist in the shell, documentation can only describe the intended action and record the gap.

## Final Question

### Could A New Consultant Successfully Use Report Design Studio From The Documentation Alone?

No, not reliably.

### What Is Missing?

The biggest missing pieces are:

1. complete shell-based actions for Design Brief, Concept Studio, and Draft Studio
2. clearer middle-stage workflow language in Prepare For Review and Review Design
3. a stronger completion signal after validation and refinement
4. faster concept and draft review for complex scenarios

### What Should Be Fixed First?

Fix these first:

1. expose the early-stage save, approval, and generation actions directly in the main shell
2. make the next step explicit in every stage
3. simplify middle-stage readiness and handoff language
4. add a clear workflow completion state after refinement and comparison

## Recommendation

Current recommendation:

- suitable for guided internal pilot use
- not ready for broad self-serve consultant use
