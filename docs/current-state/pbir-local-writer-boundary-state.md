# PBIR Local Writer Boundary Current State

## Status

Phase 24 adds the PBIR Local Artifact Writer Safety Boundary.

Phase 25 adds a downstream PBIR Local Preview File Writer that consumes this boundary output for non-deployable preview files only.

Phase 26 adds a downstream PBIR Preview Package and Review Handoff layer that packages metadata and creates review handoff records only.

The writer boundary contract is pbir-local-writer/v1.

The write request contract is pbir-local-write-request/v1.

The write manifest contract is pbir-local-write-manifest/v1.

## Purpose

The PBIR Local Artifact Writer Boundary defines the safety model for a future local artifact writer.

It plans local artifact output only.

It does not write files, create directories, serialize deployable PBIR, emit report.json, emit definition.pbir, invoke Microsoft Skills, invoke providers, call Microsoft APIs, invoke CLI commands, or deploy assets.

## Current Product Position

The boundary sits after:

- pbir-ir/v1
- pbir-preview-manifest/v1

It consumes:

- PbirIntermediateRepresentationState
- PbirPreviewManifest
- PbirLocalWriteRequest
- caller-supplied existing local path inventory

It produces:

- pbir-local-write-manifest/v1
- deterministic planned output file descriptors
- deterministic intended local paths
- deterministic intended content hashes
- source lineage
- overwrite risk assessment
- rollback plan
- warnings
- rejected artifact inventory

The boundary implementation uses in-memory descriptors only.

The downstream Phase 25 preview writer is the first component that writes files, and it still consumes this dry-run manifest as the required approval source.

## Architecture

The delivered backend components are:

- PbirLocalArtifactWriterBoundaryService
- PbirLocalArtifactWriterSafetyGate
- PbirLocalWriteRequest
- PbirLocalWriteManifest
- PbirLocalPlannedWriteFile
- PbirLocalOverwriteRisk
- PbirLocalRollbackPlan
- PbirLocalArtifactWriterState

The service flow is:

1. Validate the source PBIR IR, source preview manifest, and write request through the safety gate.
2. Reject unsafe requests without a write manifest.
3. Calculate deterministic planned local paths under the requested local output root.
4. Calculate deterministic intended content hashes from the source preview manifest, source PBIR IR, and generated diagnostics descriptor.
5. Detect overwrite risk from the caller-supplied existing local path inventory.
6. Build a dry-run rollback plan for every planned file.
7. Produce pbir-local-write-manifest/v1.

## Local Write Request Model

pbir-local-write-request/v1 records:

- request id
- schema version
- source PBIR IR reference
- source PBIR IR schema version
- source PBIR IR content hash
- source preview manifest reference
- source preview manifest schema version
- source preview manifest hash
- target output root
- requested artifact types
- overwrite policy
- rollback policy
- dry-run flag
- deployment request flag
- provider invocation request flag
- Microsoft API request flag
- CLI request flag
- Microsoft Skills execution request flag

Allowed requested artifact types are:

- previewMarkdown
- previewJson
- irJson
- manifestJson
- diagnosticsMarkdown

Forbidden requested artifact types are:

- reportJson
- definitionPbir
- modelBim
- tmdl
- pbipProject
- deployableReport

## Local Write Manifest Model

pbir-local-write-manifest/v1 contains:

- writer descriptor
- metadata
- source lineage
- planned files
- overwrite risk
- rollback plan
- warnings
- rejected artifacts
- hashes

Planned files capture:

- artifact type
- deterministic relative path
- deterministic intended path
- content type
- purpose
- source hash
- intended SHA-256 hash
- byte length
- overwrite risk flag
- will-write flag

The will-write flag is always false in Phase 24.

## Planned Local Paths

Current deterministic relative paths are:

- pbir-local-writer/v1/preview/report-preview.md
- pbir-local-writer/v1/preview/report-preview.json
- pbir-local-writer/v1/ir/canonical-pbir-ir.json
- pbir-local-writer/v1/manifests/pbir-preview-manifest.json
- pbir-local-writer/v1/diagnostics/local-write-diagnostics.md

These are planned local descriptor paths.

They are not Power BI project paths.

## Overwrite And Rollback Safety Model

Overwrite risk is detected from a caller-supplied list of existing local relative paths.

The boundary does not scan the filesystem.

The boundary rejects overwrite policies that allow replacing existing files.

The dry-run boundary supports these safe planning policies:

- fail if exists
- skip existing
- allow overwrite only when hash matches

The rollback plan is dry-run-only.

The Phase 25 local preview writer accepts only fail if exists and allow overwrite only when hash matches for actual filesystem writes.

For planned files with no overwrite risk, the rollback action is no-op dry run.

For planned files with overwrite risk, the rollback action records restore existing local file so a future writer can preserve the existing artifact before any real write path exists.

## Forbidden Deployable Artifact Policy

PbirLocalArtifactWriterSafetyGate fails closed.

It rejects:

- deployable PBIR artifact requests
- report.json
- definition.pbir
- model.bim
- TMDL
- PBIP project output
- deployable report output
- deployment requests
- provider invocation requests
- Microsoft API requests
- CLI requests
- Microsoft Skills execution requests
- non-local output roots
- missing dry-run flag
- dry-run set to false
- overwrite policies that replace existing files
- missing or mismatched source PBIR IR references
- missing or mismatched source preview manifest references

Rejected requests return no write manifest.

## Determinism Model

For identical PBIR IR, preview manifest, write request, existing local path inventory, and generated UTC:

- planned files are identical
- intended paths are identical
- intended hashes are identical
- overwrite risk is identical
- rollback plan is identical
- write manifest hash is identical
- immutable lineage ordering is identical

## Current Trust Boundaries

The PBIR Local Artifact Writer Boundary does not:

- write files
- create directories
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

## Remaining Real Writer Gap

Actual local preview writing is implemented only for non-deployable preview artifacts through pbir-local-preview-writer/v1.

Review-ready preview package metadata and review handoff records are implemented through pbir-preview-package/v1 and pbir-review-handoff/v1.

The repo still has no deployable PBIR serializer, no report.json generation, no definition.pbir generation, no Power BI project materialization, no Microsoft Skills execution, no provider invocation, no Microsoft API invocation, no CLI invocation, and no deployment workflow.

Future deployable writer work must be separate from the local preview writer and must add explicit deployable serializer contracts, approval gates, overwrite protection, and rollback execution.
