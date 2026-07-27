# PBIR Intermediate Representation Current State

## Status

Phase 22 adds the canonical PBIR Intermediate Representation layer. Repository Phase 29 now adds the first deployable serializer downstream from that unchanged IR contract.

The IR contract is pbir-ir/v1.

The serializer boundary contract is pbir-serializer-request/v1.

## Purpose

PBIR IR is the authoritative internal representation between:

Design Studio  
↓  
Generation Specification  
↓  
PBIR IR  
↓  
Future Generation Providers

Serializers and future generation providers must consume PBIR IR instead of Design Studio artifacts directly.

PBIR IR is not a serializer, generator, provider invocation path, Microsoft Skills runtime, Microsoft API surface, CLI runner, deployment workflow, or deployable PBIR output.

## Current Product Position

PBIR IR sits after:

- generation-manifest/v1
- pbir-generation-specification/v1
- reference-pbir-generator/v1 safety validation

It sits before:

- the local PBIR Preview Serializer
- the Repository Phase 29 deterministic modern PBIR serializer
- any future production PBIR generator
- any future Microsoft Skills execution provider
- any future provider invocation path
- any future deployment workflow

The Phase 22 backend components are:

- PbirIntermediateRepresentationService
- PbirIntermediateRepresentationValidator
- PbirIntermediateRepresentationReadinessService
- pbir-ir/v1 model records
- pbir-serializer-request/v1 request contract records

## PBIR IR Schema

pbir-ir/v1 contains:

- metadata
  - ir id
  - schema version
  - generated UTC
- references
  - generation manifest reference
  - PBIR generation specification reference
- page IR
  - page identity
  - navigation behavior
  - intended purpose
  - deterministic order
- visual IR
  - visual identity
  - page binding
  - visual type
  - placement
  - semantic intent
  - interaction model
  - deterministic order
- semantic IR
  - semantic identity
  - page binding
  - measures
  - dimensions
  - KPIs
  - filters
  - drill behavior
  - relationships
- navigation IR
  - landing page
  - page transitions
  - bookmarks
  - drill paths
- layout IR
  - containers
  - spacing
  - alignment
  - responsive hints
- success criteria
  - business intent
  - analytical flow
  - planning outcome success criteria
- lineage
  - upstream lineage
  - immutable lineage
- hashes
  - input hash
  - content hash
  - lineage hash

## Canonical Mapping Rules

The service consumes only GenerationManifestState and PbirGenerationSpecificationState.

The IR id is derived deterministically from the generation manifest id.

Page IR is derived from PBIR page specifications and ordered by page id.

Visual IR is derived from PBIR visual specifications and ordered by visual identity.

Semantic IR is derived from PBIR semantic specifications, with dimensions inferred from page visual intent and relationships derived from filter and visual bindings.

Navigation IR is derived from PBIR navigation specifications and normalized into explicit transitions, landing bookmarks, and drill paths.

Layout IR is derived from declared pages and visual placements. It preserves placement intent but does not calculate final PBIR layout coordinates.

Success criteria preserve business intent, analytical flow, and planning-outcome requirements from the PBIR generation specification.

Immutable lineage is inherited from the generation manifest and extended with the PBIR generation specification id and IR id.

Hashes are deterministic SHA-256 values over the manifest/specification inputs, canonical IR content, and immutable IR lineage.

## Validation Model

PbirIntermediateRepresentationValidator validates:

- completeness
- required metadata and references
- schema compatibility
- page, visual, semantic, navigation, layout, success-criteria, lineage, and hash presence
- navigation integrity
- semantic integrity
- layout integrity
- non-execution boundary integrity

Validation fails closed.

## Readiness Model

PbirIntermediateRepresentationReadinessService evaluates:

- incomplete
  - required sections, fields, or supported schema versions are missing
- blocked
  - references, navigation, semantics, layout, or trust boundaries are invalid
- canonical
  - the IR is complete and coherent as an internal canonical representation
- readyForSerializer
  - the IR is complete enough for a future serializer request

readyForSerializer does not mean serialization occurred.

It means only that a future serializer could consume the IR contract.

## Serializer Boundary

pbir-serializer-request/v1 is a request contract only.

It records:

- request id
- PBIR IR reference
- PBIR IR schema version
- PBIR IR content hash
- serializer implementation availability flag
- provider invocation allowed flag
- deployment allowed flag
- Microsoft Skills execution allowed flag

The current request contract reports:

- serializer implementation available: true
- provider invocation allowed: false
- deployment allowed: false
- Microsoft Skills execution allowed: false

Phase 23 adds a local preview serializer that consumes this request contract and pbir-ir/v1 for human-reviewable preview artifacts only.

Phase 24 adds a downstream PBIR Local Artifact Writer Boundary that consumes pbir-ir/v1 plus pbir-preview-manifest/v1 to produce deterministic dry-run local write manifests. It plans paths, hashes, overwrite risk, and rollback metadata only.

Phase 25 adds a PBIR Local Preview File Writer that consumes pbir-ir/v1 through the approved preview/write-manifest chain and writes only non-deployable preview files.

Phase 26 adds PBIR Preview Package and Review Handoff contracts that consume pbir-ir/v1 lineage through the preview write result and preserve the generation manifest reference for Design Studio and future Analyzer review handoff.

Repository Phase 29 implements original Phase 4A serialization through pbir-deployable-serializer-request/v1. It consumes pbir-ir/v1 and produces an in-memory modern PBIR artifact inventory and manifest.

It does not write files, generate PBIP projects or semantic models, invoke providers or Microsoft Skills, call APIs or CLI tools, automate Desktop or Analyzer Workspace, deploy, or publish.

## Reference Generator Integration

Reference PBIR Generator now emits canonical PBIR IR as local deterministic reference output:

- reference-pbir-generator/v1/canonical-pbir-ir.json
- deterministic IR input hash
- deterministic IR content hash
- deterministic IR lineage hash
- immutable IR lineage

The reference generator still does not create deployable PBIR output.

## Preview Serializer Integration

PBIR Preview Serializer consumes canonical pbir-ir/v1 and pbir-serializer-request/v1.

It emits:

- pbir-preview-artifact/v1
- pbir-preview-manifest/v1
- deterministic Markdown preview descriptors
- deterministic JSON preview descriptors
- page, visual layout, semantic binding, and navigation summaries

It does not emit report.json, definition.pbir, model.bim, TMDL, Power BI project files, or any deployable PBIR asset.

## Current Trust Boundaries

The PBIR IR layer does not:

- serialize deployable PBIR
- generate deployable PBIR artifacts
- invoke Microsoft Skills
- invoke providers
- call Microsoft APIs
- invoke CLI commands
- deploy assets
- publish artifacts
- mutate reports
- automate Analyzer Workspace
- write deployable local artifact files

The downstream pbir-preview-package/v1 and pbir-review-handoff/v1 layers preserve this boundary by creating metadata and review handoff records only.

## Remaining Materialization Gap

The serializer implementation gap is closed for the supported modern PBIR subset.

The next separate phase is **Safe Local Deployable PBIR Materialization with Preview/Apply/Rollback Controls**.

That phase requires a new goal, must remain downstream from PBIR IR and the Phase 29 manifest, and must not reuse or widen the preview-only writer.
