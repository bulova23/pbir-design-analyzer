# Deterministic Modern PBIR Serializer Current State

## Status

Repository Phase 29 implements original roadmap Phase 4A: deterministic modern PBIR serialization.

Phase 29 is serialization only. It produces a complete in-memory artifact inventory and manifest but does not write deployable files.

## Delivered Boundary

The serializer consumes only:

- canonical pbir-ir/v1
- the existing pbir-serializer-request/v1 boundary
- an explicit pbir-deployable-serializer-request/v1
- an immutable semantic-model inventory and content hash
- explicit visual role projections

It does not consume raw Design Package or Design Studio objects.

The supported target is modern PBIR only. A successful artifact contains:

- definition.pbir
- definition/version.json
- definition/report.json
- definition/pages/pages.json
- one page.json per supported page
- one visual.json per supported visual

The serializer never generates a root-level report.json. Root-level report.json is PBIR-Legacy and is mutually exclusive with the modern definition hierarchy.

## Supported Subset

The current supported subset is intentionally narrow:

- visible report pages with page-tab navigation
- one deterministic 1280×720 layout profile
- six fixed visual slots per page
- card
- table
- clustered column chart
- line chart
- direct semantic-model column references
- direct semantic-model measure references
- explicit Category, Y, Values, and Fields role projections

Every model entry, role, projection order, query reference, and native query reference comes from validated input. Aggregation must be explicitly none. Display name and format must be explicitly null. The serializer does not infer semantic properties, visual roles, formatting, filters, sorting, themes, or annotations.

Current upstream IR may contain auto-derived or broader semantic intent that cannot be represented faithfully by this subset. Those inputs are rejected; the serializer does not repair or invent missing information.

## Determinism And Validation

Identical canonical inputs produce identical:

- generated page and visual identities
- canonical UTF-8 JSON bytes
- ordered file inventories
- per-file hashes
- file-set, artifact, manifest, input, and lineage hashes
- immutable lineage
- warnings and unsupported-section diagnostics

Runtime validation covers the locked schema URLs and versions, supported document templates, required structure, cross-references, identities, paths, and hashes. It recomputes the canonical IR content hash, requires exact semantic inventory/token/relationship coverage, and hashes every mutable artifact and manifest field, including source IR references, schema locks, supported features, warnings, unsupported sections, and complete lineage.

Full Microsoft Draft 7 JSON Schema conformance is a deterministic test-time guarantee. Tests use pinned local schema fixtures from Microsoft json-schemas commit 34356d97e1218c79331780f8f5b77b03f2d13f35. Production tests require no schema download or network access.

## Fail-Closed Behavior

Unsupported visual types, incomplete bindings, invalid semantic-model references, unsafe paths, duplicate identities, incompatible schema versions, invalid navigation, invalid layout, hash tampering, and any information that would require invention produce:

- no deployable artifact
- no deployable manifest
- stable diagnostics
- incomplete or blocked readiness

## Trust Boundary

Phase 29 contains no:

- deployable filesystem writer
- preview-writer widening
- PBIP project materialization
- semantic-model generation
- provider or Microsoft Skills execution
- API, network, or CLI invocation
- Power BI Desktop automation
- deployment or publishing
- Analyzer Workspace launch or validation
- generated-artifact refinement loop
- Fabric App or Fabric Data App generation

The existing preview serializer remains preview-only and byte-stable when serializer implementation availability becomes true. It gains no deployable dependency or authority.

## Downstream Materialization

The next phase is:

**Safe Local Deployable PBIR Materialization with Preview/Apply/Rollback Controls**

That work is implemented as Repository Phase 30 / original roadmap Phase 4B. The separate materializer consumes the unchanged Phase 29 artifact and manifest, revalidates them before local publication, and does not reuse or widen the preview-only file writer. Phase 29 remains the only component that serializes modern PBIR content.

Repository Phase 31 now composes this unchanged serializer with Phase 30 through the PBIR Materialization Application Orchestration boundary. The orchestrator calls PbirDeployableSerializerService directly and does not reproduce serializer projection, validation, schema-lock, canonical JSON, identity, lineage, or hashing logic.
