# Report Design Studio User Guide

## What Report Design Studio Is

Report Design Studio is the design-side companion to PBIR Design Analyzer.

Its purpose is to help a consultant think through the right report before the report is built or revised. It is meant to turn a design conversation into an explicit workflow:

- define the report intent
- compare design directions
- review a draft design baseline
- prepare that draft for review
- hand the draft to Analyzer Workspace for validation
- review refinement recommendations
- compare iterations over time

Report Design Studio is not the scoring engine.

Report Design Studio is not the place where report validation becomes authoritative.

Report Design Studio is the place where design intent, design options, and design refinements are organized before Analyzer Workspace evaluates the result.

Important current MVP reality:

- the shipped UI is primarily a workflow shell and review surface
- most early stages are explanatory and read-only in the main shell today
- the shell shows stage status, approval meaning, and design-review content
- the shell does not yet expose a full inline consultant authoring flow for Design Brief, Concept Studio, and Draft Studio

## How It Relates To The Rest Of The Product

### PBIR Design Analyzer

PBIR Design Analyzer is the overall VS Code extension and platform.

Report Design Studio is one workflow inside that platform.

PBIR Design Analyzer also includes Analyzer Workspace, scoring, Story Assessment, Issues, Fix Plan, export, screenshot flows, and navigation helpers.

### Story Assessment

Story Assessment is analyzer output.

It explains what a page appears to say, what weakens the story, and what should change first.

Report Design Studio can use Story Assessment results later as advisory input during Refinement Studio, but Story Assessment itself does not happen inside Design Studio.

### Analyzer Workspace

Analyzer Workspace is where validation happens.

Report Design Studio can prepare a design candidate and hand it off, but Analyzer Workspace owns the review result. That means:

- Design Studio can approve design baselines
- Design Studio can approve refinement choices
- Design Studio can prepare a review candidate
- Design Studio cannot validate its own work

## What Report Design Studio Helps A Consultant Do

Use Report Design Studio when you want to answer:

- What report are we trying to create?
- Who is it for?
- What business question should it answer?
- What is the best report structure?
- Which concept should become the baseline?
- Is the draft ready to be reviewed?
- What analyzer feedback should be accepted into the next iteration?

In plain terms:

Design Studio helps consultants design the right report before building the report.

## How To Launch Report Design Studio

Use one of these entry points:

1. Open the **PBIR Design Analyzer** explorer view.
2. Use the rocket command in the explorer title bar: **PBIR Design Analyzer: Open Report Design Studio**.
3. Right-click a report node in the PBIR tree and choose **PBIR Design Analyzer: Open Report Design Studio**.
4. Use the Command Palette and run **PBIR Design Analyzer: Open Report Design Studio**.

If no report is already selected, the extension may prompt you to choose a PBIR report.

## What You See When It Opens

The main shell has:

- a workflow rail on the left
- a stage summary area in the center
- stage-specific review content
- approval teaching cards
- stage-specific actions when the MVP exposes them

What you can actually click in the current shipped shell:

- workflow stage buttons in the left rail
- `Open Analyzer Workspace` in Review Design when that stage is ready
- `Approve Proposal`, `Reject Proposal`, and `Defer Proposal` in Refinement Studio when proposals exist
- iteration selectors in Compare Iterations when two or more iterations exist

What you should not expect in the current shipped shell:

- a writable Design Brief form
- start, save, submit, or approve buttons for Design Brief
- concept-generation or baseline-selection controls in the shell
- draft-generation or draft-approval controls in the shell

The workflow rail stages are:

1. Design Brief
2. Concept Studio
3. Draft Studio
4. Prepare For Review
5. Review Design
6. Refinement Studio
7. Compare Iterations

## What The Status Labels Mean

### Ready

Ready means the stage is available for review or the next workflow step.

Ready does not mean approved.

Example:

- Draft Studio can be `Ready` because the concept baseline is approved and a draft can now be reviewed.

### Approved

Approved means Design Studio accepted the current baseline for that stage.

Approved does not mean validated by Analyzer Workspace.

Example:

- A draft can be `Approved` inside Design Studio, which means it is the accepted draft baseline for the next step.

### Validated

Validated means Analyzer Workspace returned the review outcome and Design Studio has analyzer-owned evidence for that iteration.

Validated is stronger than Approved.

Example:

- Compare Iterations can show a validated outcome after Analyzer Workspace reviewed a prepared candidate and recorded the result.

## What The Approval Types Mean

### Design Approval

Owner: Design Studio

Meaning:

- the current design baseline is accepted for the next design stage

Examples:

- the Design Brief is accepted and Concept Studio may proceed
- a concept baseline is accepted and Draft Studio may proceed
- a draft is accepted and Prepare For Review may proceed

Does not mean:

- the report is validated
- the report was built
- the report was changed automatically

### Materialization Approval

Owner: Design Studio

Meaning:

- the approved draft is eligible to become a review candidate

Does not mean:

- analyzers already ran
- PBIR files were created
- the report was mutated

### Refinement Approval

Owner: Design Studio

Meaning:

- a refinement recommendation is accepted into the next iteration path

Does not mean:

- the refined design is validated

### Validation Approval

Owner: Analyzer Workspace

Meaning:

- Analyzer Workspace evaluated a prepared candidate and returned the validation outcome with explicit provenance

Does not mean:

- Design Studio self-approved the result
- deployment approval exists

## When You Are Done

For the current MVP, a consultant is effectively done when all of these are true:

1. the design direction is clear
2. the chosen concept baseline is clear
3. the draft baseline is clear
4. the design has been handed to Analyzer Workspace for review
5. the refinement decisions are recorded
6. Compare Iterations shows the current state of the design and validation outcome

In the current MVP, “done” is more about reaching a documented and reviewable design state than completing an automated production workflow.

## Stage Summary

### Design Brief

In the current shipped shell, this stage is a read-only workflow summary.

It tells you that the Design Brief stage is where the design intent baseline belongs.

The underlying Design Studio foundation expects the brief model to contain:

- audience
- business objective
- key decisions
- primary KPIs
- intended story
- success criteria
- report type

The underlying model can also capture:

- dimensions
- navigation expectations
- consumption context
- decision cadence
- narrative risks or constraints
- required evidence domains
- target analyzable surface family

But the current shipped shell does not show those fields as editable inputs.

### Concept Studio

In the current shipped shell, this stage is a read-only concept review surface.

It lets you inspect concept-review content, but not generate or approve the concept baseline directly in the shell.

When concept review content exists, you should review:

- chapter structure
- KPI hierarchy
- navigation structure
- analytical flow

### Draft Studio

In the current shipped shell, this stage is a read-only draft review surface.

It lets you inspect the current draft review summary, but not generate or approve drafts directly in the shell.

You should review:

- draft pages
- draft layouts
- draft navigation
- KPI placement

### Prepare For Review

Use this stage to confirm the draft is ready to be reviewed.

In user terms, this means:

- the draft has been accepted
- Design Studio can prepare a review candidate
- the candidate is eligible, preview-only, or blocked

### Review Design

Use this stage to open Analyzer Workspace intentionally.

This is the point where design review leaves Design Studio and validation becomes Analyzer Workspace work.

### Refinement Studio

Use this stage to review analyzer-driven design recommendations.

You can:

- approve a proposal
- reject a proposal
- defer a proposal

### Compare Iterations

Use this stage to review:

- What Improved
- What Was Accepted
- What Changed
- Approval Evolution
- Validation Evolution

## Current MVP Limitation

The current MVP shell is strongest as a guided review shell, not as a fully connected self-serve authoring workflow.

Some early-stage actions exist in underlying slices and tests, but are not exposed as a complete end-user action path in the main shipped shell. That means this guide explains the workflow intent and the visible shell behavior, but it does not claim that a consultant can complete the full early-stage flow entirely inside the current UI.

See:

- [report-design-studio-workflow-walkthrough.md](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/docs/report-design-studio-workflow-walkthrough.md:1)
- [report-design-studio-uat-gap-analysis.md](/Users/bcrowell/Documents/GitHub/pbir-design-analyzer/docs/report-design-studio-uat-gap-analysis.md:1)
