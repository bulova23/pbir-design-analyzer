# Report Design Studio Workflow Walkthrough

## Purpose

This walkthrough explains the current MVP workflow as a consultant would experience it.

It also distinguishes between:

- actions the current shell exposes directly
- workflow states the shell explains
- workflow steps that are still incomplete in the current MVP

## Before You Start

1. Open the PBIR project in VS Code.
2. Open the **PBIR Design Analyzer** explorer.
3. Launch **PBIR Design Analyzer: Open Report Design Studio**.
4. Confirm the Design Studio shell opens and the workflow rail is visible.

## Quick Path

The intended workflow model is:

1. Define the design intent in Design Brief.
2. Compare concepts in Concept Studio.
3. Approve a concept baseline.
4. Review the draft baseline in Draft Studio.
5. Prepare that approved draft for review.
6. Open Analyzer Workspace intentionally.
7. Return to Refinement Studio to review recommendations.
8. Use Compare Iterations to confirm what improved.

The current shipped shell does not expose that full path as clickable stage-local actions from the beginning.

What the current shell actually provides is:

- visible workflow stages
- visible stage status
- visible approval teaching
- visible concept, draft, readiness, handoff, refinement, and iteration review content when those artifacts exist
- live buttons only for:
  - opening Analyzer Workspace from Review Design
  - approving, rejecting, or deferring refinement proposals
  - choosing iterations in Compare Iterations

## Stage 1: Design Brief

### What It Is

Design Brief is the intent baseline for the report.

It defines who the report is for, what decision it supports, what story it should tell, and how success will be judged.

### What You Actually See In The Current Shipped Shell

In the current shipped shell, you do not see a brief form.

You see:

- the `Design Brief` stage in the workflow rail
- a stage summary card
- a stage status badge such as `Not started`
- a Design Approval card
- Approval stages teaching cards

There is nowhere in the current shipped shell to:

- type an audience
- type a business objective
- click save
- click submit
- click request approval

### Underlying Brief Model

The underlying Design Brief foundation expects these core fields:

- Audience
- Business Objective
- Key Decisions
- Primary KPIs
- Intended Story
- Success Criteria
- Report Type

The deeper contract also expects:

- Dimensions
- Navigation Expectations

Advanced optional context can include:

- Consumption Context
- Decision Cadence
- Narrative Risks Or Constraints
- Required Evidence Domains
- Target Analyzable Surface Family

### What To Do In The Current Shell

In the current shipped shell:

1. click `Design Brief`
2. read the stage description
3. read the Design Approval card
4. use the shell to understand what this stage is supposed to represent

The current shell is explanatory here, not interactive.

### What Happens Next

Once the Design Brief is approved, Concept Studio becomes the next valid stage.

### Done Signal

You are done with Design Brief when:

- the business intent is clear
- the brief is valid
- the brief is approved for concept work

Current MVP gap:

- the shell teaches this state, but it does not provide actual start/save/submit/approve controls for this stage

## Stage 2: Concept Studio

### What It Is

Concept Studio compares alternate report concepts before any draft baseline is accepted.

A concept is a proposed report design direction. It defines:

- chapter structure
- KPI hierarchy
- navigation structure
- analytical flow

### What Concepts Mean

A concept is not a built report.

A concept is a design option for how the report should tell the story.

### What You Actually See In The Current Shipped Shell

In the current MVP shell:

1. click `Concept Studio` in the workflow rail
2. review the selected baseline label
3. review:
   - Chapter Structure
   - KPI Hierarchy
   - Navigation Structure
   - Analytical Flow
4. review the comparison sections between the selected baseline and the alternate concept

What you do not see in the current shipped shell:

- a `Generate Concepts` button
- a baseline selector
- an approval button for the concept baseline

### What To Do In The Current Shell

Use the stage as a read-only review surface:

1. inspect the concept baseline shown
2. inspect the comparison content
3. decide whether the concept direction makes sense

The shell does not currently let you choose or approve the baseline directly.

### What Happens Next

Once the concept baseline is approved, Draft Studio becomes available.

### Done Signal

You are done with Concept Studio when:

- one concept is clearly preferred
- that concept has been approved as the baseline
- the draft stage can proceed from that baseline

Current MVP gap:

- concept review is visible
- concept selection and approval are not exposed as shell-based self-serve actions

## Stage 3: Draft Studio

### What It Is

Draft Studio is where the chosen concept becomes a reviewable draft baseline.

A draft is still non-production. It is not a real report mutation.

It is a design baseline the consultant can review before handing anything to Analyzer Workspace.

### What You Actually See In The Current Shipped Shell

When draft review content exists, the shell can show:

- draft pages
- draft layouts
- draft navigation
- KPI placement guidance

What you do not see in the current shipped shell:

- a `Generate Draft Artifacts` button
- a draft approval button

### What To Do In The Current Shell

If draft review content exists:

1. click `Draft Studio`
2. review the draft status
3. review Draft Pages, Draft Layouts, and Draft Navigation
4. decide whether the draft reads like a coherent report design

If draft review content does not exist:

1. click `Draft Studio`
2. treat the stage as a read-only workflow summary

The shell does not currently let you generate or approve drafts directly.

### What Happens Next

Once the draft is approved, Prepare For Review becomes meaningful.

### Done Signal

You are done with Draft Studio when:

- the draft is coherent enough to review
- the draft has been approved as the design baseline
- the next step is to prepare it for Analyzer Workspace review

## Stage 4: Prepare For Review

### What This Stage Does

Prepare For Review tells you whether the approved draft can become a review candidate.

This stage is the bridge between a design artifact and a review candidate.

### What Readiness Means

Readiness means:

- the draft is approved
- the system can derive a review candidate from it
- the candidate is either executable, preview-only, or blocked

### What Materialization Means In User Terms

Materialization is platform language for:

- “turn this approved design into something Analyzer Workspace can review”

In user terms:

- Design Studio is preparing a review candidate
- it is not editing the report
- it is not validating the report

### What To Do

In the current MVP shell:

1. click `Prepare For Review`
2. read:
   - readiness label
   - eligibility
   - analyzer
   - profile
   - diagnostics
3. confirm whether the stage is:
   - Ready for analysis
   - Preview only
   - Needs attention

There is no separate “start review preparation” or “approve materialization” button in the current shell.

This stage is primarily a read-only readiness explanation surface.

### What Happens Next

If the candidate is executable, Review Design becomes ready.

### Done Signal

You are done with Prepare For Review when:

- the candidate is ready
- the diagnostics are acceptable
- Review Design can open Analyzer Workspace

## Stage 5: Review Design

### What Happens Here

Review Design is where Design Studio hands the candidate to Analyzer Workspace.

### Who Owns Validation

Analyzer Workspace owns validation.

Design Studio does not validate its own design.

### What To Do

In the current MVP shell:

1. click `Review Design`
2. confirm the readiness label
3. read the diagnostics
4. click `Open Analyzer Workspace`

### What The Button Means

**Open Analyzer Workspace** means:

- Design Studio is explicitly opening the analyzer workflow
- analysis has not already started automatically

### What Happens Next

Analyzer Workspace opens for the candidate.

That is the validation step.

### Done Signal

You are done with Review Design when:

- Analyzer Workspace opens successfully
- the candidate is ready for explicit review there

## Stage 6: Refinement Studio

### What It Is

Refinement Studio turns analyzer output into advisory design proposals.

These proposals are not automatic mutations.

They are consultant review items.

### How To Review Recommendations

In the current MVP shell:

1. click **Refinement Studio**
2. review each group:
   - Story Improvements
   - Layout Improvements
   - KPI Improvements
   - Navigation Improvements
   - Report Structure Improvements
3. for each proposal, review:
   - title
   - summary
   - recommendation
   - rationale
   - expected impact
   - source analyzer output
   - affected design artifacts
   - proposal comparison

### How To Approve, Reject, Or Defer Recommendations

The current MVP shell exposes live buttons:

- **Approve Proposal**
- **Reject Proposal**
- **Defer Proposal**

Use them as follows:

1. approve when the recommendation should influence the next design iteration
2. reject when the recommendation should not be accepted
3. defer when the recommendation may be useful later but should not be accepted now

### What Happens Next

Accepted or rejected recommendations become part of the iteration history and refinement approval state.

### Done Signal

You are done with Refinement Studio when:

- each meaningful recommendation has a decision
- the refinement state reflects the accepted path for the next iteration

## Stage 7: Compare Iterations

### What This Stage Does

Compare Iterations explains how the design evolved over time.

### What Improved

This section summarizes the positive movement between the earlier and later iteration.

### What Changed

This section lists change highlights and recommendation evolution that are not simply accepted recommendations.

### What Was Accepted

This section isolates the recommendations that were explicitly accepted.

### What To Do

In the current MVP shell:

1. click **Compare Iterations**
2. review:
   - Iteration Timeline
   - Progress Snapshot
   - What Improved
   - What Was Accepted
   - What Changed
   - Approval Evolution
   - Validation Evolution
3. if two or more iterations exist, choose:
   - **Before iteration**
   - **After iteration**

### Done Signal

You are done with Compare Iterations when:

- you can explain what changed
- you know which recommendations were accepted
- you know whether validation improved

## Approval Guide

### Ready

Use this as:

- “this stage can now be reviewed or handed forward”

Example:

- Prepare For Review is `Ready` when the draft can be turned into an executable review candidate.

### Approved

Use this as:

- “Design Studio accepted this baseline for the next stage”

Example:

- Concept Studio is `Approved` when one concept baseline has been accepted for Draft Studio.

### Validated

Use this as:

- “Analyzer Workspace returned the review outcome and that result now belongs to the iteration history”

Example:

- Compare Iterations can show `Validated` once Analyzer Workspace returned analyzer-owned evidence.

## When The Workflow Completes

For the current MVP, the workflow is complete when:

1. the design baseline is clear
2. the draft baseline is clear
3. Analyzer Workspace reviewed the candidate
4. refinement decisions are recorded
5. Compare Iterations explains the result

## Reality Check

The current shell can explain the workflow well.

The current shell cannot yet carry a new consultant through the Design Brief -> Concept -> Draft path using stage-local data-entry, save, submit, or approval controls, because those controls are not exposed in the shipped UI.

That is a product gap, not a documentation gap.

See [report-design-studio-uat-gap-analysis.md](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/docs/report-design-studio-uat-gap-analysis.md:1).
