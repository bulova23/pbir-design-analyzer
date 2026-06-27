# PBIR Preview Package and Review Handoff Current State

## Status

Phase 26 adds the PBIR Preview Package and Review Handoff layer.

The preview package contract is pbir-preview-package/v1.

The review handoff contract is pbir-review-handoff/v1.

## Purpose

The PBIR Preview Package creates a deterministic metadata-only bundle around safe local preview outputs.

The Review Handoff creates an explicit record for Design Studio or Analyzer Workspace review.

This phase is review-only. It does not generate deployable PBIR files, create report.json, create definition.pbir, execute Microsoft Skills, invoke providers, call Microsoft APIs, invoke CLI commands, deploy assets, publish assets, mutate reports, or automate Analyzer Workspace validation.

## Current Product Position

The package and handoff layer sits after:

- pbir-ir/v1
- pbir-preview-manifest/v1
- pbir-local-write-manifest/v1
- pbir-local-preview-write-result/v1

It consumes:

- PbirLocalPreviewWriteResult
- PbirPreviewManifest
- PbirIntermediateRepresentationState
- GenerationManifestState
- PbirReviewHandoffRequest

It produces:

- pbir-preview-package/v1
- pbir-review-handoff/v1
- deterministic package metadata
- file inventory
- hash inventory
- lineage
- warning and rejected-artifact inventory
- rollback metadata reference
- Design Studio approval context
- Analyzer Workspace validation boundary metadata

Package contents are metadata and references only. No zip file is created.

Phase 27 adds a downstream Design Studio Preview Review surface that consumes these records as review-only metadata. The Design Studio surface does not create or modify preview packages or handoffs, does not approve outputs automatically, and does not run Analyzer Workspace validation.

Phase 28 adds a downstream Design Studio Execution Readiness Dashboard that summarizes preview package readiness, preview review status, review handoff readiness, warnings, lineage, and trust-boundary status. The dashboard is informational only and does not create or modify preview packages or handoffs, does not invoke providers, does not execute Microsoft Skills, does not deploy assets, and does not run Analyzer Workspace validation.

## Architecture

The delivered backend components are:

- PbirPreviewPackageService
- PbirReviewHandoffService
- PbirReviewHandoffSafetyGate
- PbirPreviewPackage
- PbirReviewHandoff
- PbirReviewHandoffRequest

The package service flow is:

1. Validate the local preview write result, preview manifest, and PBIR IR references.
2. Reject incomplete or deployable artifact references.
3. Create deterministic package metadata.
4. Preserve written preview file inventory.
5. Preserve file hashes plus aggregate preview write result, preview manifest, PBIR IR, and rollback hashes.
6. Preserve source lineage and append the package id to immutable lineage.
7. Preserve warnings, rejected artifacts, and rollback metadata reference.

The handoff service flow is:

1. Validate the preview package, generation manifest, and handoff request through the safety gate.
2. Reject unsafe handoffs before creating a handoff record.
3. Preserve preview package, design package, generation manifest, and PBIR IR references.
4. Preserve Design Studio approval context from generation-manifest/v1.
5. Preserve Analyzer Workspace validation boundary as not run and not automatic.
6. Preserve deployment boundary as not requested and not allowed.
7. Classify review readiness.

## Preview Package Model

pbir-preview-package/v1 contains:

- schema version
- package descriptor
  - metadata-only
  - local-only
  - no physical file content
  - no zip creation
  - deployable artifacts forbidden
- metadata
  - package id
  - generated UTC
  - source preview write result reference
- file inventory
  - artifact type
  - relative path
  - intended path
  - physical path reference
  - content type
  - source hash
  - SHA-256 hash
  - byte length
- hash inventory
  - file hashes
  - preview write result hash
  - preview manifest hash
  - PBIR IR content hash
  - rollback plan hash
- lineage
- rollback plan reference
- warnings
- rejected artifacts
- package hashes

The package does not copy file contents and does not package files into an archive.

## Review Handoff Model

pbir-review-handoff/v1 contains:

- handoff id
- schema version
- preview package reference
- design package reference
- generation manifest reference
- PBIR IR reference
- review target
- review readiness
- required reviewer action
- Design Studio approval context
- Analyzer Workspace validation boundary
- deployment boundary
- warnings
- lineage
- hashes

The handoff is an audit and coordination record. It is not an execution request.

## Review Readiness Model

The review readiness states are:

- incomplete
- readyForDesignReview
- readyForAnalyzerReview
- blocked

readyForDesignReview means a human can review preview outputs.

readyForAnalyzerReview means the preview package has enough metadata to become a future Analyzer candidate.

Neither state means validation occurred.

blocked means the safety gate rejected the handoff before a handoff record was created.

## Design Studio Review Boundary

Design Studio approval context is preserved from generation-manifest/v1.

The handoff records:

- design package reference
- generation manifest reference
- design approval required
- generation approval required
- Analyzer validation required
- design approved
- generation approved

The handoff does not approve outputs and does not mutate design artifacts.

The downstream Design Studio Preview Review surface exposes this handoff with explicit review-only actions:

- mark preview reviewed
- request revision
- defer review
- prepare analyzer candidate metadata

These actions update Design Studio review state only. They are not validation, report mutation, provider execution, deployment approval, or Analyzer Workspace automation.

## Analyzer Workspace Validation Boundary

Analyzer Workspace remains manual and downstream.

The handoff records:

- validation occurred: false
- automatic validation requested: false
- automatic validation allowed: false
- workspace launch requested: false
- validation status: no Analyzer Workspace validation has occurred

The safety gate rejects automatic Analyzer Workspace validation requests.

No Analyzer Workspace launch, validation run, or validation approval is implemented.

## Safety Gate

PbirReviewHandoffSafetyGate rejects handoff when:

- preview package references forbidden deployable artifacts
- preview package file or hash inventory is missing SHA-256 hashes
- preview package lineage is incomplete
- generation manifest or Design Studio approval context is missing
- automatic Analyzer Workspace validation is requested
- Analyzer Workspace launch is requested
- deployment is requested
- generation manifest execution constraints no longer remain dry-run and non-invoking

Rejected handoffs return no handoff record.

## Current Trust Boundaries

The PBIR Preview Package and Review Handoff layer does not:

- generate deployable PBIR artifacts
- create report.json
- create definition.pbir
- create model.bim
- create TMDL
- create PBIP project files
- execute Microsoft Skills
- invoke providers
- call Microsoft APIs
- invoke CLI commands
- deploy assets
- publish artifacts
- mutate reports
- automate Analyzer Workspace
- run Analyzer Workspace validation
- approve outputs

## Remaining Gaps

Deployable PBIR generation remains unimplemented.

report.json generation remains unimplemented.

definition.pbir generation remains unimplemented.

Microsoft Skills execution remains unimplemented.

Provider, API, and CLI invocation remain unimplemented.

Deployment remains unimplemented.

Analyzer Workspace automation remains unimplemented.

Future deployable PBIR or Analyzer automation work must be a separate phase with explicit deterministic contracts, approval gates, preview/apply/rollback semantics, and a clear boundary from pbir-preview-package/v1 and pbir-review-handoff/v1.

The downstream design-studio-preview-review/v1 contract preserves the same boundary.

The downstream design-studio-execution-readiness/v1 contract also preserves the same boundary.
