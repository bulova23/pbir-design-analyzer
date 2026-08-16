# Report Design Studio User Guide

## What Report Design Studio Is

Report Design Studio is the design workflow inside PBIR Design Analyzer.

It helps a consultant move from report intent to a reviewed design iteration through an explicit staged workflow:

- Design Brief
- Approve Brief
- Concept Studio
- Generate Concepts
- Select Baseline
- Approve Concept
- Draft Studio
- Generate Draft
- Approve Draft
- Prepare For Review
- Create Review Candidate
- Approve Review Candidate
- Review Design
- Launch Analyzer Workspace
- Return Real Analyzer Result
- Attach Analyzer Results
- Refinement Studio
- Compare Iterations
- Workflow Completion
- Complete Iteration

Report Design Studio does not replace Analyzer Workspace.

Report Design Studio does not validate its own work.

Report Design Studio does not mutate the PBIR report automatically.

## Self-Serve Onboarding

Use this workflow when you need to design or revise a report intentionally before relying on analyzer review.

Recommended first-time path:

1. Open a PBIR report in VS Code.
2. Launch Report Design Studio.
3. Complete and approve the Design Brief.
4. Generate concepts and approve one baseline.
5. Generate and approve a draft.
6. Prepare and approve a review candidate.
7. Launch Analyzer Workspace from Review Design.
8. Return analyzer results to Design Studio and attach them.
9. Review advisory proposals in Refinement Studio.
10. Compare the iteration history.
11. Complete the iteration explicitly in Workflow Completion.

If you are new to the product, start with an executive dashboard or operational monitoring scenario before using the analytical investigation path. Round 6 found that the analytical investigation path is still the slowest self-serve scenario.

## How To Launch Report Design Studio

Use one of these entry points:

1. Open the PBIR Design Analyzer explorer.
2. Use PBIR Design Analyzer: Open Report Design Studio from the explorer title bar.
3. Right-click a report node in the PBIR tree and choose PBIR Design Analyzer: Open Report Design Studio.
4. Use the Command Palette and run PBIR Design Analyzer: Open Report Design Studio.

## What You See

The shell includes:

- a workflow rail
- a selected-stage summary
- stage-specific execution and review content
- approval cards
- stage-local workflow actions

The current executable workflow rail stages are:

1. Design Brief
2. Concept Studio
3. Draft Studio
4. Prepare For Review
5. Review Design
6. Refinement Studio
7. Compare Iterations
8. Workflow Completion

## Executable Workflow

### Design Brief

Design Brief is an executable authoring stage.

The shell supports:

- Save Draft
- Submit For Approval
- Approve Brief
- essentials-first editing
- optional advanced brief details

Core brief fields include:

- Audience
- Business Objective
- Key Decisions
- Primary KPIs
- Dimensions
- Intended Story
- Success Criteria
- Report Type
- Navigation Expectations

Advanced details can include:

- Consumption Context
- Decision Cadence
- Narrative Risks Or Constraints
- Required Evidence Domains
- Target Analyzable Surface Family

Done signal:

- the brief is valid
- the brief is approved
- Concept Studio unlocks

### Concept Studio

Concept Studio is an executable concept-baseline stage.

The shell supports:

- Generate Concepts
- baseline selection
- Submit Baseline For Approval
- Approve Concept Baseline

Concept Studio remains design-only. It creates concept artifacts, not PBIR assets and not analyzable review candidates.

Done signal:

- a preferred baseline is selected
- the baseline is approved
- Draft Studio unlocks

### Draft Studio

Draft Studio is an executable draft-baseline stage.

The shell supports:

- Generate Draft
- Submit Draft For Approval
- Approve Draft

Draft Studio shows reviewable draft artifacts such as:

- Draft Pages
- Draft Layouts
- Draft Navigation
- KPI Placement

Done signal:

- the draft baseline is generated
- the draft is approved
- Prepare For Review unlocks

### Prepare For Review

Prepare For Review turns an approved draft into a review candidate without mutating the report.

The shell supports:

- Create Review Candidate
- Submit Candidate For Approval
- Approve Candidate

This stage shows:

- candidate lineage
- approvals used
- review diagnostics
- target analyzer
- target analyzer profile
- readiness and eligibility

Done signal:

- a review candidate exists
- the candidate is approved
- Review Design unlocks

### Review Design

Review Design is the trust-boundary stage between Design Studio and Analyzer Workspace.

The shell supports:

- Open Analyzer Workspace
- Mark Review Completed
- Attach Analyzer Results

Review Design progresses through these practical states:

- Review Not Started
- Review Launched
- Awaiting Analyzer Results
- Analyzer Results Available
- Results Attached

Done signal:

- Analyzer Workspace has reviewed the candidate
- real analyzer results have returned
- analyzer results have been attached explicitly
- Refinement Studio unlocks

### Refinement Studio

Refinement Studio converts attached analyzer output into advisory design proposals.

The shell supports proposal decisions such as:

- Approve Proposal
- Reject Proposal
- Defer Proposal

Refinement decisions are design decisions. They are not validation approval.

### Compare Iterations

Compare Iterations shows what changed across iterations.

Use it to review:

- iteration-to-iteration changes
- approval evolution
- validation evolution
- attached analyzer-result lineage
- recommendation outcomes

Compare Iterations is a review surface. It does not own validation and it does not complete the workflow by itself.

### Workflow Completion

Workflow Completion is a separate closeout stage after Compare Iterations.

The shell supports:

- Complete Iteration
- Reopen Iteration

This stage shows:

- completion checklist
- outstanding items
- approvals satisfied
- recommendation summary
- completion and reopen audit history

Workflow Completion is explicit because completion is not the same thing as:

- design approval
- review-candidate approval
- refinement approval
- validation approval
- deployment approval

## Approval Model

### Design Approval

Owner: Design Studio

Used for:

- Approve Brief
- Approve Concept
- Approve Draft

Meaning:

- the current design artifact is accepted as the baseline for the next workflow step

### Materialization Approval

Owner: Design Studio

Used for:

- Approve Review Candidate

Meaning:

- the approved draft can be handed to Analyzer Workspace as a review candidate

### Refinement Approval

Owner: Design Studio

Used for:

- proposal decisions inside Refinement Studio

Meaning:

- Design Studio recorded the consultant's advisory refinement decision

### Validation Approval

Owner: Analyzer Workspace

Meaning:

- Analyzer Workspace returned and owns the validation outcome and its provenance

## Trust Boundaries

Design Studio owns:

- brief authoring
- concept generation and baseline approval
- draft generation and approval
- review-candidate preparation and approval
- refinement decisions
- workflow completion and reopen
- iteration management

Analyzer Workspace owns:

- analyzer execution
- findings
- validation approval
- analyzer provenance
- returned analyzer-result identity

Design Studio never:

- self-validates
- grants validation approval
- mutates the report automatically
- treats attached analyzer results as Design Studio-authored findings

## Analyzer Return Path

The analyzer return path is explicit:

1. Review Design opens Analyzer Workspace.
2. Analyzer Workspace performs the review.
3. Analyzer Workspace records the analyzer-owned result.
4. Design Studio returns to Review Design.
5. Review Design discovers the real returned result.
6. The user chooses Attach Analyzer Results.
7. Design Studio records the attached result in iteration history.
8. Refinement Studio unlocks using the attached analyzer-backed result.

The attach step is intentional. Launching review alone does not unlock refinement.

## Workflow Completion And Reopen

Use Workflow Completion when you want to close the current iteration explicitly.

Complete Iteration means:

- the current design iteration is closed as a workflow state
- checklist and audit details are recorded

Complete Iteration does not mean:

- validation approval was granted
- the report is deployed
- publication happened

Reopen Iteration means:

- the completed iteration is moved back into an active workflow state
- completion history is preserved
- the consultant can continue refinement and iteration work intentionally

## Screenshots

No current Design Studio workflow screenshots are checked into the repository for these guides. Existing image assets under `docs/` are not current Design Studio workflow captures, so none were added in this alignment pass.
