# Deterministic Modern PBIR Serializer — Repository Phase 29 Design

Date: 2026-07-26

Status: Proposed for approval. No production implementation is authorized by this document.

## Roadmap Mapping

Repository Phase 29 implements **Original Roadmap Phase 4A — Deterministic Modern PBIR Serialization**.

The original seven-phase roadmap remains:

1. Design Package Consumption Layer
2. Skills Prompt Generation
3. Generation Request Framework
4. PBIR Generation
5. Analyzer Handoff
6. Refinement Loop
7. Fabric App Generation

Repository Phases 21–28 decomposed prerequisites and review infrastructure that made Phase 4 safer but obscured the original sequence. Phase 29 is the first implementation slice inside original Phase 4:

- **Original Phase 4A:** deterministic modern PBIR serialization in memory — Repository Phase 29
- **Original Phase 4B:** safe local deployable PBIR materialization with preview/apply/rollback — next separate repository phase
- later original Phase 4 work: provider or Microsoft Skills execution, Desktop verification, deployment, and publishing — not authorized

Phase 29 stops after serialization. It does not complete all of original Phase 4.

## Decision Summary

Add a strict, backend-only compiler from canonical pbir-ir/v1 to an in-memory modern PBIR file inventory.

The compiler:

- consumes pbir-ir/v1 through pbir-serializer-request/v1
- accepts a new versioned modern-serializer request that locks target schemas, a safe relative semantic-model reference, a fixed layout profile, and explicit semantic-binding resolutions
- emits definition.pbir and the required definition/ hierarchy in memory
- emits definition/report.json but never root-level report.json
- validates the complete candidate before returning any artifact or manifest
- returns diagnostics and a rejected or blocked readiness state with no partial output on any unsupported or incomplete input
- performs no filesystem, provider, network, API, CLI, Microsoft Skills, Desktop, deployment, or Analyzer action

## Architectural Review Findings

Ranked by long-term risk.

### 1. Highest Risk: Treating Current pbir-ir/v1 Placeholders As Deployable Detail

The current IR can contain values such as auto dimensions, friendly KPI names without table ownership, and slot-based placement. Those values are sufficient for preview but are not automatically valid PBIR semantic queries.

Design response:

- keep pbir-ir/v1 stable
- require explicit request-side resolution of every semantic token to an immutable table/property/kind reference
- allow only the documented slot layout grammar and fixed layout profile
- reject auto, ambiguous, missing, or unused resolutions
- record unsupported IR sections in diagnostics

This means some IR currently produced by the upstream pipeline will be rejected until it carries resolvable intent. Rejection is preferable to inventing model references.

### 2. Highest Risk: Confusing Modern definition/report.json With PBIR-Legacy report.json

Modern PBIR requires report metadata at definition/report.json. PBIR-Legacy uses report.json at the report root. The two representations are mutually exclusive for this phase.

Design response:

- require definition/report.json
- reject and test for root-level report.json
- name path checks against normalized full relative paths, not only file names
- document the distinction in contracts, diagnostics, and current-state docs

### 3. High Risk: Schema Drift Through Live Downloads

Microsoft PBIR schemas evolve independently. Resolving latest schemas at runtime would make production output and tests nondeterministic.

Design response:

- pin exact schema URLs and a reviewed Microsoft schema repository commit
- keep production offline
- vendor the exact official schemas and required references as test fixtures with provenance and hashes
- require an intentional contract/version change to add a new schema version

### 4. High Risk: A Serializer That Returns Partial Output

Returning a few valid files alongside a failure makes later materialization ambiguous and unsafe.

Design response:

- build into a private candidate inventory
- validate paths, identities, cross-references, schema versions, content, hashes, and lineage
- expose artifact and manifest only after the whole candidate passes
- expose no artifact bytes or partial manifest when readiness is blocked or rejected

### 5. Medium Risk: Generalizing Too Early

A generic PBIR/PBIR-Legacy/PBIP serializer would combine incompatible formats and prematurely couple the future writer to serialization.

Design response:

- modern PBIR only
- in-memory inventory only
- no generic writer abstraction
- no reuse or widening of the preview-only writer
- no PBIP, semantic-model, resource, bookmark, mobile, or report-extension generation

## Considered Approaches

### A. Strict Request-Bound Compiler — Recommended

Keep pbir-ir/v1 unchanged. Add explicit model-binding and layout-profile inputs at the serializer request boundary. Compile only supported visual and navigation shapes and fail closed otherwise.

Benefits:

- preserves the canonical IR contract
- makes every emitted semantic reference traceable to caller-supplied input
- supports deterministic offline validation
- cleanly hands an immutable inventory to a future writer

Cost:

- existing placeholder-heavy IR may be rejected
- the supported visual subset is deliberately small

### B. Widen pbir-ir/v1 Before Serialization

Add typed coordinates and fully qualified semantic-model objects directly to pbir-ir/v1.

Rejected for Phase 29 because it would turn the first serializer into an upstream contract migration, increase backward-compatibility risk, and violate the instruction to consume canonical pbir-ir/v1 through the existing boundary.

### C. Emit Schema-Valid Skeleton Visuals

Emit visuals without complete queries or use default fields and layout values.

Rejected because schema validity alone does not preserve design intent. This approach would invent information and produce misleading deployable output.

## Contract Set

All contracts are backend-internal in Phase 29 but versioned because they form the future writer boundary.

### pbir-deployable-serializer-request/v1

Required fields:

- schemaVersion
- requestId
- serializerRequestRef
- serializerRequestSchemaVersion
- pbirIrRef
- pbirIrSchemaVersion
- pbirIrContentHash
- targetFormat: modernPbir
- definitionPropertiesSchemaVersion: 2.0.0
- definitionSchemaVersion: 1.0.0
- datasetReference
- layoutProfileId: modern-grid-1280x720/v1
- semanticModelInventory
- semanticModelInventoryRef
- semanticModelInventoryContentHash
- visualBindings
- executionPolicy

datasetReference supports only:

- byPath
- a normalized relative path
- forward-slash separators
- no empty segments, dot segments, parent traversal, URI scheme, drive prefix, absolute path, or control characters

The request explicitly keeps these false:

- filesystemMaterializationAllowed
- providerInvocationAllowed
- microsoftSkillsExecutionAllowed
- apiInvocationAllowed
- cliInvocationAllowed
- deploymentAllowed
- desktopAutomationAllowed
- analyzerAutomationAllowed

semanticModelInventory contains immutable entries:

- token used by the IR
- entity/table name
- property name
- kind: measure or column

semanticModelInventoryRef and semanticModelInventoryContentHash identify the caller-supplied, immutable model snapshot used for reference validation. The serializer verifies the inventory hash before resolving any binding. It does not rescan a repository or query a live semantic model.

visualBindings contains:

- visualId
- one or more role projections
- role name
- source semantic token

Every source token must occur in the linked IR semantic record. Every resolved entity/property/kind must exist exactly once in the request inventory. Extra bindings and duplicate inventory identities are invalid.

### pbir-deployable-artifact/v1

Contains:

- schemaVersion
- artifactId
- targetFormat
- files
- lineage
- hashes

Each file contains:

- normalized relativePath
- contentType: application/json
- exact UTF-8 content
- byteLength
- hashSha256
- Microsoft schema URL and version
- source IR references

The artifact inventory is immutable and ordered by relative path using ordinal comparison.

### pbir-deployable-manifest/v1

Contains:

- schemaVersion
- manifestId
- artifactRef
- source references
- schema lock
- ordered file inventory
- supported features
- warnings
- unsupported sections
- lineage
- hashes

The manifest is suitable as input to a future deployable writer but carries no path root, overwrite policy, or materialization authority.

### pbir-deployable-validation/v1

Contains:

- schemaVersion
- isValid
- validatedFileCount
- schemaValidationResults
- structuralValidationResults
- crossReferenceValidationResults
- hashValidationResults

### pbir-deployable-readiness/v1

Readiness values:

- incomplete — required contract or IR information is absent
- blocked — complete input is present but unsafe, unsupported, contradictory, or invalid
- readyForSerialization — request and IR passed preflight
- serialized — the complete candidate passed post-serialization validation

Only serialized may contain artifact and manifest.

### pbir-deployable-diagnostics/v1

Ordered diagnostic categories:

- missingRequiredFields
- unsupportedSchemaVersions
- unsupportedVisualTypes
- incompleteSemanticBindings
- invalidModelReferences
- invalidPaths
- duplicateIdentities
- invalidLayoutDefinitions
- invalidNavigationDefinitions
- schemaIncompatibilities
- hashViolations
- lineageViolations
- boundaryViolations
- warnings
- unsupportedSections

Diagnostics use stable codes plus human-readable messages. Ordering is code, path/reference, then message using ordinal comparison.

### pbir-deployable-lineage/v1

Contains:

- pbirIrRef
- pbirIrContentHash
- serializerRequestRef
- modernSerializerRequestRef
- semanticModelInventoryRef
- semanticModelInventoryContentHash
- upstreamLineage copied from the IR
- immutableLineage containing every upstream reference plus artifact and manifest identities
- lineageHash

No lineage collection is mutated after construction.

### pbir-deployable-hashes/v1

Contains:

- inputHash
- fileSetHash
- artifactHash
- manifestHash
- lineageHash

Hash rules:

- SHA-256, lowercase hexadecimal
- hashes cover exact UTF-8 bytes
- inputHash covers canonical IR content hash plus canonical modern request content
- fileSetHash covers ordered relative path, byte length, and file hash tuples
- self-hashes omit only their own hash field
- timestamps are not generated implicitly; identical explicit inputs produce identical outputs

## Supported Modern PBIR Shape

For a request that passes validation, the minimum inventory is:

- definition.pbir
- definition/version.json
- definition/report.json
- definition/pages/pages.json
- definition/pages/[pageIdentity]/page.json for every page
- definition/pages/[pageIdentity]/visuals/[visualIdentity]/visual.json for every supported visual

Not emitted:

- report.json at the report root
- bookmarks
- mobile layouts
- report extensions
- static resources
- custom visuals
- semantic models
- PBIP project files
- .platform

### Deterministic Identities

- Page and visual object names are 20 lowercase hexadecimal characters.
- Names are derived from SHA-256 over a domain-separated tuple of IR id plus canonical source identity.
- Folder and document object names are identical.
- Collisions and duplicate source identities fail closed; no suffix is invented.

### Canonical JSON

- UTF-8 without BOM
- LF newline
- one trailing LF
- two-space indentation
- explicit property order
- ordinal ordering for maps and sets
- source order only where order is semantically meaningful and already canonical
- invariant-culture numbers
- no null or default-valued optional properties unless required by the locked schema

### Pages And Navigation

Supported:

- page order from IR Page.Order
- landing page through definition/pages/pages.json activePageName
- visible report pages
- 1280×720 canvas with FitToPage
- ordinary page-tab navigation
- sequential page transitions only when they exactly assert the same page order

Rejected:

- duplicate page ids, identities, or order values
- landing page not present
- nonsequential transitions
- bookmark state beyond the canonical page and landing markers
- drillthrough, tooltip pages, hidden pages, or navigation requiring buttons/bookmarks
- page filters that cannot be resolved exactly

### Layout

Supported placement grammar:

- page:[pageId]/slot:[positive integer]

The locked modern-grid-1280x720/v1 profile maps slots to fixed nonoverlapping grid rectangles. A page must not reuse a slot. Visual z-order and tab order derive from canonical visual order.

Rejected:

- free-form placement
- duplicate slots
- placement page mismatch
- coordinates outside the fixed profile
- layout containers that omit or duplicate visual references
- responsive hints requiring a mobile artifact

### Visuals And Semantic Bindings

Initial visual allowlist:

- card
- table
- clusteredColumnChart
- lineChart

The allowlist is a serializer configuration constant with provenance, not a permissive pass-through.

Role requirements:

- card: exactly one measure projection
- table: one or more column or measure projections in Values
- clusteredColumnChart: exactly one category column plus one or more measure projections
- lineChart: exactly one category column plus one or more measure projections

Supported semantic expressions:

- direct model column reference
- direct model measure reference

Rejected:

- unsupported or custom visual types
- auto dimensions
- implicit aggregation
- calculated columns or measures
- visual calculations
- field parameters
- hierarchies
- ambiguous token resolution
- filters, relationships, KPIs, drill behavior, or interactions that cannot be represented exactly by the supported subset

The serializer does not infer tables, fields, roles, aggregations, display names, sort order, filters, or formatting.

## Microsoft Schema Lock

Reviewed source commit:

- microsoft/json-schemas commit 34356d97e1218c79331780f8f5b77b03f2d13f35

Locked emitted-document schemas:

- definition.pbir:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definitionProperties/2.0.0/schema.json
  - file format version 4.0
- definition/version.json:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json
  - report definition version 1.0.0
- definition/report.json:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definition/report/1.0.0/schema.json
- definition/pages/pages.json:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definition/pagesMetadata/1.0.0/schema.json
- page.json:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definition/page/1.0.0/schema.json
- visual.json:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json

Required local reference fixtures also include the locked 1.0.0 formatting-object and semantic-query schemas referenced by emitted schemas.

Official sources:

- [Power BI Desktop project report folder and modern PBIR structure](https://learn.microsoft.com/en-us/power-bi/developer/projects/projects-report)
- [Microsoft PBIR JSON schemas](https://github.com/microsoft/json-schemas/tree/main/fabric/item/report)
- [definition.pbir schema family](https://github.com/microsoft/json-schemas/tree/main/fabric/item/report/definitionProperties)
- [modern PBIR definition schema family](https://github.com/microsoft/json-schemas/tree/main/fabric/item/report/definition)

Production code never downloads these URLs. Schema upgrades require an explicit code, fixture, documentation, and compatibility change.

## Serialization Flow

1. Validate pbir-ir/v1 state and its existing pbir-serializer-request/v1 reference/hash.
2. Validate pbir-deployable-serializer-request/v1 and all trust-boundary flags.
3. Normalize and validate paths, identities, ordering, layout slots, navigation, model inventory, and visual bindings.
4. Return incomplete or blocked with no candidate output if preflight fails.
5. Build a private ordered file candidate.
6. Validate every candidate document against the locked schema contract and deterministic subset validators.
7. Validate file paths, root-format exclusivity, cross-references, identities, byte lengths, and hashes.
8. Construct immutable lineage, artifact, and manifest.
9. Revalidate artifact and manifest self-consistency.
10. Return serialized with the complete artifact and manifest.

## Trust Boundary

The Phase 29 public method accepts in-memory records and returns in-memory records.

Forbidden dependencies and behavior:

- System.IO file or directory mutation
- preview-writer reuse
- process execution
- HTTP or other network clients
- provider registries or runtimes
- Microsoft Skills
- APIs
- CLIs
- deployment or publishing
- Power BI Desktop automation
- Analyzer Workspace launch or validation
- Design Studio execution controls

Reflection- and source-based trust-boundary tests protect these exclusions.

## Testing Strategy

Tests are written before implementation.

Positive coverage:

- byte-identical output and hashes for identical IR and request
- coherent required modern PBIR inventory
- no root-level report.json
- stable page/visual ids, ordering, paths, canonical JSON, hashes, and lineage
- card, table, clustered column, and line visual projections
- local schema-fixture validation for every emitted document

Fail-closed coverage:

- unsupported schemas or target format
- unsupported visual type
- missing, ambiguous, duplicate, extra, or wrong-kind semantic binding
- invalid model reference
- auto or invented semantic requirement
- unsafe dataset or artifact path
- duplicate page, visual, container, slot, or generated identity
- invalid navigation or unsupported bookmarks/drill behavior
- schema incompatibility
- tampered content, byte length, hash, file set, manifest, or lineage
- any forbidden execution flag

Boundary coverage:

- no filesystem writes or writer dependency
- no network, provider, Microsoft Skills, API, CLI, Desktop, deployment, publishing, or Analyzer surface
- rejected input returns null artifact and null manifest

Full validation records actual counts for:

- backend xUnit tests
- all extension and webview Jest tests
- TypeScript compilation

## Documentation Changes At Implementation Close

- add modern PBIR serializer current-state documentation
- update PBIR IR and preview serializer current state without widening preview behavior
- update architecture-gap analysis
- update docs/ROADMAP.md with the repository-phase-to-original-roadmap mapping
- update the original roadmap plan with the Phase 4A/4B mapping
- update repository memory and the timestamped session note
- name the next separate phase as safe local deployable PBIR materialization with preview/apply/rollback controls

## Phase Exit Gate

Phase 29 is complete only when:

- all supported inputs produce deterministic, schema-valid in-memory modern PBIR artifacts
- every rejection returns no artifact and no manifest
- focused and full validation pass with actual counts recorded
- documentation maps Repository Phase 29 to original Phase 4A
- documentation names original Phase 4B as the next separate safe materialization phase

After the exit gate, stop. Do not begin materialization or any provider-execution work without a new goal.
