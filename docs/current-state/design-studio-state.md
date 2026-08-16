# Design Studio Current State

## Summary

Design Studio is the current user-facing design workflow inside PBIR Design Analyzer.

Its purpose is to move a report or seeded recommendation through explicit design stages before analyzer review, while preserving the repo’s validation-first trust model.

Design Studio currently exists as:

- a VS Code command surface
- an extension-host workflow
- a webview shell
- a staged design-state workflow
- an explicit handoff loop into Analyzer Workspace

## Current Product Position

Design Studio currently sits between upstream design inputs and downstream validation.

Its role is:

- author and approve design artifacts
- shape concept and draft baselines
- prepare a review candidate
- launch review into Analyzer Workspace
- attach analyzer results explicitly
- manage advisory refinement and iteration history

The current downstream planning chain now reaches beyond Design Studio without changing that ownership:

- Design Package remains the upstream planning artifact produced from approved design context
- generation-request/v1 remains the authoritative execution contract for future provider execution
- execution-plan/v1 remains a derived planning-only artifact for future provider adapters
- provider-adapter/v1 remains the adapter-compatibility input contract for future execution providers
- microsoft-adapter-specification/v1 remains the descriptive Microsoft capability-mapping seam for future Microsoft execution providers

Design Studio still does not execute provider generation and does not gain adapter ownership.

It is not a validation surface and it is not a hidden report-mutation path.

## How It Is Exposed Today

Design Studio is currently exposed through the extension as a real workflow entry point.

Current launch paths include:

- explorer title action
- report-node context action
- command palette command

The current extension command is the Report Design Studio entry point inside PBIR Design Analyzer.

## Current Executable Workflow

The current executable shell stages are:

1. Design Brief
2. Concept Studio
3. Draft Studio
4. Prepare For Review
5. Preview Review
6. Review Design
7. Refinement Studio
8. Compare Iterations
9. Workflow Completion

This workflow is represented in the current webview shell and associated state contracts.

## What Each Stage Currently Does

### Design Brief

Design Brief is currently an executable authoring stage.

It supports:

- draft editing
- save draft
- submit for approval
- approve brief

It captures the current baseline design intent, including:

- audience
- business objective
- key decisions
- primary KPIs
- dimensions
- intended story
- success criteria
- report type
- navigation expectations

Advanced details can also be carried for execution context and later review.

### Concept Studio

Concept Studio is currently the concept-baseline stage.

It supports:

- concept generation
- alternate concept comparison
- preferred baseline selection
- baseline approval

It remains design-only. It does not generate PBIR assets or analyzable artifacts directly.

### Draft Studio

Draft Studio is currently the draft-baseline stage.

It supports:

- draft generation
- draft review
- draft approval

The current shell presents draft-oriented artifacts such as:

- draft pages
- draft layouts
- draft navigation
- KPI placement

### Prepare For Review

Prepare For Review is the explicit materialization boundary inside the current workflow.

It supports:

- create review candidate
- submit candidate for approval
- approve candidate

The candidate remains a review candidate, not a production asset.

### Preview Review

Preview Review is the review-only PBIR preview package inspection surface.

It supports:

- preview package summary inspection
- preview file inventory inspection
- hash inventory inspection
- lineage inspection
- warning and rejected-artifact inspection
- rollback metadata inspection
- review handoff state inspection
- mark preview reviewed
- request revision
- defer review
- prepare analyzer candidate metadata

Preview Review consumes design-studio-preview-review/v1 state backed by pbir-preview-package/v1 and pbir-review-handoff/v1 metadata.

It does not approve anything automatically, run Analyzer validation, launch Analyzer Workspace automatically, mutate PBIR files, generate deployable PBIR files, create report.json, create definition.pbir, execute Microsoft Skills, invoke providers, call APIs, invoke CLI commands, deploy assets, or publish assets.

Preview Review now also renders the Design Studio Execution Readiness Dashboard.

The dashboard consumes design-studio-execution-readiness/v1 view state and summarizes architecture, planning, generation, runtime, skills, review, warnings, lineage, and trust-boundary status. It is informational only and does not trigger generation, provider invocation, deployment, Microsoft Skills execution, or Analyzer Workspace automation.

### Review Design

Review Design is the current handoff loop between Design Studio and Analyzer Workspace.

It supports:

- open Analyzer Workspace
- mark review completed
- attach analyzer results

The current workflow requires explicit attachment of returned analyzer results. Validation is not inferred from prior design approvals.

### Refinement Studio

Refinement Studio currently converts attached analyzer output into advisory design proposals.

It supports proposal decisions such as:

- approve proposal
- reject proposal
- defer proposal

These are design decisions, not validation decisions.

### Compare Iterations

Compare Iterations is the current iteration-history review surface.

It is used to review:

- changes across iterations
- approval progression
- validation progression
- analyzer-result lineage
- recommendation outcomes

### Workflow Completion

Workflow Completion is the explicit closeout stage.

It supports:

- complete iteration
- reopen iteration

Completion is a workflow action, not analyzer validation.

## Current Approval Model

Design Studio currently distinguishes among:

- ready
- approved
- validated

Current ownership is split clearly:

- Design Studio owns design approval
- Design Studio workflow state owns readiness progression
- Analyzer Workspace owns validation

The shell explicitly teaches this distinction in the current UI model.

## Current Trust Boundaries

The current Design Studio trust boundary is explicit and intentionally narrow.

Design Studio currently must not:

- validate its own outputs
- issue validation approval
- mutate PBIR reports automatically
- create production assets through materialization
- treat Preview Review as validation
- use preview package review actions as report mutation authority
- bypass deterministic preview, apply, and rollback
- let provider outputs self-approve
- collapse design approval into validation approval
- execute provider plans
- invoke Microsoft Skills or CLI generation

Materialization currently creates candidates only.

Analyzer Workspace remains the downstream validation owner.

## Current Protocol And State Boundaries

Design Studio currently treats host and webview messaging as a versioned trust boundary.

The current system expects:

- protocol validation before state is consumed
- rejection of unsupported message versions
- rejection of unsupported message types
- nested payload validation
- rejection of malformed or cross-thread lineage
- rejection of malformed preview package review payloads
- rejection of malformed execution readiness payloads
- rejection of preview review messages that imply hidden Analyzer execution, provider invocation, deployment, or report mutation

This is part of the current architecture, not a future aspiration.

## Current Relationship To Discovery Wizard

Design Studio can currently be seeded from Discovery Wizard outputs.

That seeded starting point can currently include:

- Design Brief
- concept baseline inputs
- draft seed artifacts

This means Design Studio is already the downstream design consumer of discovery output, even though Discovery Wizard itself is still backend-first rather than a shipped end-user shell.

## Current Relationship To Analyzer Workspace

Design Studio and Analyzer Workspace are currently separate workflows with explicit handoff.

What is true today:

- Design Studio launches review
- Analyzer Workspace performs review and validation
- analyzer results return to Design Studio only through explicit attachment
- Design Studio records iteration history and refinement context after attachment

This preserves Analyzer Workspace authority.

## Current Limitations

The current Design Studio workflow still excludes:

- automatic PBIR generation
- direct report mutation
- direct deployment
- provider-required workflow steps
- self-validation
- hidden analyzer execution
- automatic preview approval
- automatic Analyzer launch from preview package review

It also should not be described as a generic AI report generator. Its current architecture is still design-first, advisory-first, and validation-gated.

Repository Phase 34 adds a local PBIR materialization workflow to the existing materialize stage. It is a route-only consumer of Phase 33 with explicit apply confirmation, read-only recovery inspection, cancellation, and lifecycle reset. It does not add provider, Skills, Analyzer, deployment, publishing, or filesystem authority.

## Current State Assessment

The current repo documentation and implemented shell together support this summary:

- Design Studio is a real executable workflow in the product
- the workflow shell, approvals, and handoff loop are implemented
- trust boundaries are explicit and heavily documented
- Design Studio is the current design-authoring surface, not just a design spec

The important architectural distinction from Discovery Wizard is:

- Discovery Wizard is currently implemented mainly as backend advisory infrastructure
- Design Studio is currently implemented as a shipped extension-host and webview workflow
