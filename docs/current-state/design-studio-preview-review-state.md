# Design Studio Preview Review Current State

## Status

Phase 27 adds the Design Studio preview review surface.

The Design Studio preview review contract is design-studio-preview-review/v1.

## Purpose

Design Studio can now expose PBIR preview package and review handoff metadata as a review-only surface after preview package creation.

This phase is UI and workflow integration only. It does not generate deployable PBIR files, create report.json, create definition.pbir, execute Microsoft Skills, invoke providers, call Microsoft APIs, invoke CLI commands, deploy assets, publish assets, mutate reports, launch Analyzer Workspace automatically, or run Analyzer validation.

## Current Product Position

The surface sits downstream from:

- pbir-preview-package/v1
- pbir-review-handoff/v1

It is represented in the VS Code extension as:

- a versioned Design Studio preview review state contract
- a DesignStudioPreviewReviewSafetyGate
- persisted Design Studio preview review state
- a Preview Review workflow stage between Prepare For Review and Review Design
- webview rendering for package and handoff metadata
- explicit review-only actions

## Contract

design-studio-preview-review/v1 captures:

- preview review id
- preview package reference
- review handoff reference
- reviewer action
- reviewer notes
- review timestamp
- readiness state
- preview package summary
- preview file inventory
- hash inventory
- lineage summary
- warnings
- rejected artifacts
- rollback metadata
- Analyzer Workspace boundary metadata
- review-only boundary flags

The contract stores references and metadata only. It does not store deployable PBIR content.

## Review Actions

The supported actions are:

- mark preview reviewed
- request revision
- defer review
- prepare analyzer candidate metadata

mark preview reviewed records human review state only. It is not validation.

request revision records that the preview package needs revision. It does not mutate PBIR files.

defer review records that review is postponed. It does not approve anything.

prepare analyzer candidate metadata records metadata that can support a future manual Analyzer candidate. It does not execute Analyzer Workspace, launch Analyzer Workspace, or attach validation results.

## Protocol Validation

Design Studio host and webview messages remain a versioned protocol boundary.

The protocol now validates:

- Preview Review workflow stage ids
- design-studio-preview-review/v1 schema version
- pbir-preview-package/v1 package reference
- pbir-review-handoff/v1 handoff reference
- file inventory shape
- hash inventory shape
- lineage shape
- rollback metadata shape
- Analyzer Workspace boundary booleans
- review-only boundary booleans
- explicit preview review action messages

Unsupported protocol versions are rejected before state is consumed.

Malformed preview package payloads are rejected before rendering.

## Safety Gate

DesignStudioPreviewReviewSafetyGate rejects:

- deployable artifact references
- report.json references
- definition.pbir references
- model.bim references
- incomplete SHA-256 hashes
- malformed lineage
- Analyzer Workspace validation that already occurred
- automatic Analyzer execution requests
- automatic Analyzer launch requests
- Microsoft Skills execution requests
- provider invocation requests
- API invocation requests
- CLI invocation requests
- deployment requests

Rejected input is not persisted as an active preview review.

## Review-Only Boundary

The persisted state records that these remain false:

- report mutation allowed
- Analyzer execution allowed
- Analyzer launch allowed
- Microsoft Skills execution allowed
- provider invocation allowed
- API invocation allowed
- CLI invocation allowed
- deployment allowed
- deployable PBIR generation allowed
- report.json generation allowed
- definition.pbir generation allowed

## Remaining Gaps

Deployable PBIR generation remains unimplemented.

report.json generation remains unimplemented.

definition.pbir generation remains unimplemented.

Microsoft Skills execution remains unimplemented.

Provider, API, and CLI invocation remain unimplemented.

Deployment remains unimplemented.

Analyzer Workspace automation remains unimplemented.

Analyzer Workspace validation remains a separate downstream manual workflow.
