# PBIR Local Preview Writer Current State

## Status

Phase 25 adds the PBIR Local Preview File Writer.

The writer framework contract is pbir-local-preview-writer/v1.

The output manifest contract is pbir-local-preview-write-result/v1.

## Purpose

The PBIR Local Preview File Writer is the first actual local file-writing capability in the PBIR generation pipeline.

It writes only non-deployable preview files that were already planned by pbir-local-write-manifest/v1 and backed by deterministic preview serializer or IR content.

It does not write deployable PBIR files, generate report.json, generate definition.pbir, generate model.bim, generate TMDL, create PBIP projects, invoke Microsoft Skills, invoke providers, call Microsoft APIs, invoke CLI commands, deploy assets, publish assets, or mutate reports.

## Current Product Position

The writer sits after:

- pbir-ir/v1
- pbir-preview-artifact/v1
- pbir-preview-manifest/v1
- pbir-local-write-request/v1
- pbir-local-write-manifest/v1

It consumes:

- PbirPreviewArtifact
- PbirPreviewManifest
- PbirIntermediateRepresentationState
- PbirLocalWriteRequest
- PbirLocalWriteManifest
- local output base directory

It produces:

- pbir-local-preview-write-result/v1
- physical local preview files
- written-file inventory
- content hashes
- source lineage
- source write manifest reference
- rollback plan reference
- skipped file inventory
- rejected file inventory
- warnings

Phase 26 adds a downstream PBIR Preview Package and Review Handoff layer that consumes pbir-local-preview-write-result/v1 as metadata input. It creates review package and handoff records only, not deployable PBIR output or Analyzer automation.

## Architecture

The delivered backend components are:

- PbirLocalPreviewFileWriterService
- PbirLocalPreviewFileWriterSafetyGate
- PbirLocalPreviewFileContentFactory
- PbirLocalPreviewWriteResult
- PbirLocalPreviewWrittenFile
- PbirLocalPreviewRollbackPlanReference
- PbirLocalPreviewFileWriterState

The service flow is:

1. Validate the preview artifact, preview manifest, PBIR IR, local write request, dry-run write manifest, and output base directory through the safety gate.
2. Reject unsafe input without creating files.
3. Resolve deterministic content for each planned preview file.
4. Validate that resolved content hashes and byte lengths match the approved dry-run write manifest.
5. Validate overwrite policy before any file is written.
6. Create local directories only for approved preview paths.
7. Write preview files using deterministic UTF-8 content.
8. Produce pbir-local-preview-write-result/v1 with hashes, lineage, rollback metadata reference, skipped files, rejected files, and warnings.

## Allowed Preview Outputs

The writer may write only these artifact types:

- preview Markdown
- preview JSON
- canonical IR JSON
- preview manifest JSON
- diagnostics Markdown

Current deterministic relative paths are inherited from pbir-local-write-manifest/v1:

- pbir-local-writer/v1/preview/report-preview.md
- pbir-local-writer/v1/preview/report-preview.json
- pbir-local-writer/v1/ir/canonical-pbir-ir.json
- pbir-local-writer/v1/manifests/pbir-preview-manifest.json
- pbir-local-writer/v1/diagnostics/local-write-diagnostics.md

These are local preview artifact paths.

They are not Power BI project paths.

## Forbidden Deployable Outputs

The safety gate rejects:

- report.json
- definition.pbir
- model.bim
- TMDL
- PBIP project structure paths
- deployable report artifacts
- deployable PBIR artifact requests
- non-local output paths
- manifest entries not approved by the dry-run writer boundary
- missing rollback metadata
- unsafe overwrite policies
- deployment requests
- provider invocation requests
- Microsoft API requests
- CLI requests
- Microsoft Skills execution requests

Rejected input returns no write result and writes no files.

## Overwrite Protection

The actual writer supports only:

- fail if exists
- allow overwrite only when hash matches

Blind overwrite is not supported.

When fail if exists is selected, any existing output file rejects the whole write before file mutation.

When allow overwrite only when hash matches is selected, an existing output file may be replaced only if its current content hash already matches the approved manifest hash. Hash mismatch rejects the whole write before file mutation.

The Phase 24 dry-run boundary may still classify overwrite risk from caller-supplied inventory, but Phase 25 performs filesystem overwrite checks before writing.

## Rollback Support

Rollback metadata is required before local preview writing.

The writer records a rollback plan reference in pbir-local-preview-write-result/v1.

Automatic rollback execution is not implemented.

## Determinism Model

For identical preview artifact, preview manifest, PBIR IR, local write request, write manifest, output base directory, and generated UTC:

- resolved file content is identical
- written intended paths are identical
- written content hashes are identical
- written byte lengths are identical
- result file-set hash is identical
- result manifest hash is identical
- immutable lineage ordering is identical

## Current Trust Boundaries

The PBIR Local Preview File Writer does not:

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

## Remaining Deployable PBIR Writer Gap

Deployable PBIR writing remains unimplemented.

The repo still has no deployable PBIR serializer, no report.json generation, no definition.pbir generation, no Power BI project materialization, no Microsoft Skills execution, no provider invocation, no Microsoft API invocation, no CLI invocation, and no deployment workflow.

Future deployable writer work must be a separate phase with explicit serializer contracts, approval gates, deterministic preview/apply/rollback semantics, and strict separation from pbir-local-preview-writer/v1.

The downstream pbir-preview-package/v1 and pbir-review-handoff/v1 contracts remain review-only metadata layers and do not close the deployable PBIR writer gap.
