# Reference PBIR Generator Current State

## Status

Phase 21 adds the first local deterministic reference generator prototype.

The generator contract is reference-pbir-generator/v1.

The output contract is reference-generation-output/v1.

## Purpose

The reference generator proves that the certified planning architecture can drive deterministic artifact creation from generation-manifest/v1.

It is not a production generator.

It does not create deployable PBIR projects, deployed reports, Fabric artifacts, Fabric App artifacts, or Fabric Data App artifacts.

## Current Product Position

Reference PBIR Generator sits after:

- architecture-certification/v1
- generation-manifest/v1
- pbir-generation-specification/v1

It consumes:

- GenerationManifestState
- ArchitectureCertificationState
- PbirGenerationSpecificationState
- ReferenceGenerationOptions

It produces:

- reference-generation-output/v1
- canonical pbir-ir/v1 reference output
- deterministic JSON reference files
- deterministic Markdown lineage output
- deterministic IR input, content, and lineage hashes
- deterministic SHA-256 hashes
- immutable lineage references
- generation metadata

It sits before the Phase 23 PBIR Preview Serializer, which may consume canonical pbir-ir/v1 to render local human-review preview artifacts only.

It also sits upstream from the Phase 24 PBIR Local Artifact Writer Boundary, which may consume pbir-ir/v1 and pbir-preview-manifest/v1 to produce dry-run write manifests only.

## Architecture

The delivered backend components are:

- IReferenceGenerationProvider
- ReferencePbirGenerationService
- ReferenceGenerationSafetyGate
- ReferenceGenerationOptions
- ReferenceGenerationOutput
- ReferenceGeneratedFile
- ReferenceGenerationState

The service is intentionally small:

- validate safety first
- reject unsafe or incomplete inputs
- create canonical pbir-ir/v1 through PbirIntermediateRepresentationService
- derive deterministic reference structures from the generation manifest and PBIR generation specification
- calculate content hashes
- preserve immutable lineage and generation metadata

The implementation uses in-memory local file descriptors. It does not write files, invoke external tools, or depend on network access.

## Safety Model

ReferenceGenerationSafetyGate fails closed unless all of the following are true:

- architecture certification exists
- architecture readiness is readyForExecutionImplementation
- generation manifest exists
- generation manifest readiness is readyForGenerator
- generation manifest schema is generation-manifest/v1
- PBIR generation specification exists
- PBIR generation specification is readyForGenerationProvider
- dry-run generation is enabled
- output is local-only
- deployment is disabled
- provider invocation is disabled
- Microsoft API invocation is disabled
- CLI invocation is disabled
- network access is disabled

Rejected output returns no generated artifacts.

## Deterministic Generation Guarantees

For identical inputs and identical caller-supplied generatedUtc:

- output metadata is identical
- generated JSON and Markdown content is identical
- generated file hashes are identical
- file-set hash is identical
- input hash is identical
- output hash is identical
- immutable lineage ordering is identical

The file-set hash is based only on deterministic generated file descriptors:

- relative path
- content hash
- byte length

The timestamp is preserved as generation metadata and is caller-supplied so tests and future callers can keep deterministic control over time.

## Reference Output

The current generator creates only deterministic local reference artifacts:

- reference-pbir-generator/v1/manifest-summary.json
- reference-pbir-generator/v1/canonical-pbir-ir.json
- reference-pbir-generator/v1/lineage.md

These files are not a PBIR project.

They are reference artifacts for validating planning-to-IR determinism.

The canonical PBIR IR file uses pbir-ir/v1 and carries page, visual, semantic, navigation, layout, success-criteria, lineage, and hash sections.

## Boundary Protection

The reference generator does not:

- execute Microsoft Skills
- invoke providers
- call Microsoft APIs
- invoke CLI commands
- use network access
- deploy assets
- publish artifacts
- mutate reports
- automate Analyzer Workspace
- serialize deployable PBIR
- create deployable PBIR projects
- create Fabric artifacts
- write local artifact files

## Remaining Production Gaps

The following remain intentionally unimplemented:

- PBIR serialization
- deployable PBIR serialization
- real local artifact writing
- production PBIR generation
- deployable PBIR project materialization
- Microsoft Skills execution
- provider invocation
- Microsoft API invocation
- CLI-backed execution
- artifact quarantine and validation intake
- deployment or publishing
- Fabric App generation
- Fabric Data App generation
- Analyzer Workspace automation
