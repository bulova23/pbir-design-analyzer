# PBIR Preview Serializer Current State

## Status

Phase 23 adds the PBIR Preview Serializer boundary.

The preview artifact contract is pbir-preview-artifact/v1.

The preview manifest contract is pbir-preview-manifest/v1.

## Purpose

The preview serializer proves that canonical PBIR IR can be rendered into deterministic human-reviewable output.

It is not deployable PBIR serialization.

It does not create Power BI project files, PBIR report definitions, semantic model files, TMDL, or deployable artifacts.

## Current Product Position

PBIR Preview Serializer sits after:

- pbir-ir/v1
- pbir-serializer-request/v1

It consumes:

- PbirIntermediateRepresentationState
- PbirSerializerRequest
- PbirPreviewSerializerOptions

It produces:

- pbir-preview-artifact/v1
- pbir-preview-manifest/v1
- deterministic Markdown preview descriptors
- deterministic JSON preview descriptors
- page summaries
- visual and page layout summaries
- semantic binding summaries
- navigation summaries
- deterministic SHA-256 hashes
- immutable preview lineage
- warnings
- unsupported-section inventory

The implementation uses in-memory local file descriptors. It does not write files, invoke external tools, call network APIs, or deploy assets.

Phase 24 adds a downstream PBIR Local Artifact Writer Boundary that may consume pbir-preview-manifest/v1 and pbir-ir/v1 to produce dry-run local write manifests.

Phase 25 adds a downstream PBIR Local Preview File Writer that may write only approved local preview files from those manifests. It still does not create deployable PBIR artifacts.

Phase 26 adds a downstream PBIR Preview Package and Review Handoff layer that references safe preview outputs and prepares manual Design Studio or Analyzer Workspace review handoff records. It does not run validation or automate Analyzer Workspace.

Repository Phase 29 adds a separate in-memory modern PBIR serializer. The preview serializer does not call it, return its contracts, or gain deployable authority. Preview output remains byte-stable when serializer implementation availability becomes true.

## Architecture

The delivered backend components are:

- PbirPreviewSerializerService
- PbirPreviewSerializerSafetyGate
- PbirPreviewSerializerValidator
- PbirPreviewSerializerOptions
- PbirPreviewArtifact
- PbirPreviewManifest
- PbirPreviewGeneratedFile
- PbirPreviewSerializerState

The service flow is:

1. Validate safety before preview generation.
2. Reject unsafe or incomplete input without generated artifacts.
3. Validate pbir-serializer-request/v1 against the supplied pbir-ir/v1 reference and content hash.
4. Render deterministic Markdown and JSON preview descriptors.
5. Compute deterministic file, file-set, output, and manifest hashes.
6. Preserve source references and immutable lineage.
7. Record forbidden deployable sections as unsupported, not generated.

## Preview Artifact Model

pbir-preview-artifact/v1 contains:

- schema version
- metadata
  - artifact id
  - generated UTC
  - local output root
  - local-only flag
- source references
  - PBIR IR reference
  - PBIR IR schema version
  - PBIR IR content hash
  - serializer request reference
- generated file descriptors
  - relative path
  - content type
  - purpose
  - preview output type
  - content
  - byte length
  - SHA-256 content hash
- hashes
  - input hash
  - file-set hash
  - output hash

Current generated preview descriptors are:

- pbir-preview-artifact/v1/report-preview.md
- pbir-preview-artifact/v1/report-preview.json

These names are preview descriptor paths only. They are not Power BI project paths.

## Preview Manifest Model

pbir-preview-manifest/v1 contains:

- schema version
- metadata
  - manifest id
  - generated UTC
- source references
  - PBIR IR reference
  - PBIR IR schema version
  - PBIR IR content hash
  - serializer request reference
- generated preview file references
- lineage
  - upstream lineage
  - immutable preview lineage
- warnings
- unsupported sections
- hashes
  - input hash
  - file-set hash
  - manifest hash

The manifest is intended to make preview output reviewable and auditable without granting mutation or deployment authority.

## Safety Model

PbirPreviewSerializerSafetyGate fails closed.

It rejects:

- missing or incomplete PBIR IR
- invalid serializer request schema
- serializer request PBIR IR reference mismatches
- serializer request PBIR IR content hash mismatches
- deployable output requests
- report.json output requests
- definition.pbir output requests
- model.bim output requests
- TMDL output requests
- Power BI project file output requests
- provider invocation requests
- Microsoft API requests
- CLI requests
- Microsoft Skills execution requests
- deployment requests
- non-local output paths

Rejected requests return no preview artifact and no preview manifest.

## Validation Model

PbirPreviewSerializerValidator validates:

- preview artifact schema version
- preview manifest schema version
- source reference integrity
- generated preview file presence
- supported preview output types
- generated file byte length and SHA-256 stability
- file-set hash stability
- manifest hash stability
- immutable lineage coverage
- absence of deployable PBIR file references

Validation fails closed.

## Determinism Model

For identical PBIR IR, serializer request, options, and generated UTC:

- Markdown preview content is identical
- JSON preview content is identical
- generated file hashes are identical
- file-set hash is identical
- output hash is identical
- manifest hash is identical
- immutable lineage ordering is identical

## Current Trust Boundaries

The PBIR Preview Serializer does not:

- generate deployable PBIR artifacts
- create report.json
- create definition.pbir
- create model.bim
- create TMDL
- create Power BI project files
- execute Microsoft Skills
- invoke providers
- call Microsoft APIs
- invoke CLI commands
- deploy assets
- publish artifacts
- mutate reports
- automate Analyzer Workspace

The downstream local writer boundary and local preview writer preserve these restrictions. They plan or write local preview, IR, manifest, and diagnostics artifacts only, and they reject report.json, definition.pbir, model.bim, TMDL, PBIP project output, Microsoft Skills execution, provider/API/CLI invocation, and deployment.

The downstream preview package and review handoff layer also preserves these restrictions. It records metadata, references, lineage, warnings, rejected artifacts, rollback metadata, Design Studio approval context, and Analyzer Workspace validation boundaries only.

## Separate Deployable Serializer Boundary

Repository Phase 29 implements original roadmap Phase 4A serialization as a separate service downstream from pbir-ir/v1.

The preview serializer remains unchanged in authority and artifact type. It never emits definition.pbir, definition/report.json, root-level report.json, page definitions, or visual definitions.

The remaining gap is **Safe Local Deployable PBIR Materialization with Preview/Apply/Rollback Controls**. It requires a new goal and a separate writer. The repo still has no deployable local artifact writer, PBIP project materialization, Microsoft Skills execution, provider invocation, API or CLI invocation, Desktop automation, deployment, publishing, or Analyzer automation.
