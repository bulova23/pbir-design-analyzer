# Design Package → Microsoft Skills / CLI Integration Implementation Plan

Date: 2026-06-20

Status: Planning document only. No code changes are included in this document.

## Goal

Implement a provider-neutral generation framework that converts Discovery Wizard Design Packages into Microsoft Power BI Skills and CLI consumable requests, produces generated artifacts under explicit approvals, and routes those artifacts into Analyzer Workspace for validation without changing Discovery Wizard, Design Studio, or Analyzer Workspace ownership.

## Planning Assumptions

- Discovery Wizard MVP is complete and the current Design Package is planning-grade trustworthy.
- The implementation must preserve advisory-only discovery boundaries.
- The implementation must preserve Design Studio design ownership.
- The implementation must preserve Analyzer Workspace validation ownership.
- Microsoft target capabilities are preview-heavy and should be adapter-contained.

## Delivery Principles

- contract-first before execution
- provider-neutral core with Microsoft-first adapter
- review-required for every generated artifact
- additive support by target profile
- no hidden automation across trust boundaries

## Phase 1 – Design Package Consumption Layer

### Scope

- define the Design Package consumption inventory
- classify current Design Package fields as required, optional, transformed, or review-only
- formalize the transformation boundary from Design Package to Generation Request input
- document internal ownership and compatibility rules

### Dependencies

- existing Design Package models
- contract ownership guidance in `docs/architecture/contract-schema-and-ownership-strategy.md`
- Discovery Wizard and Design Studio architecture docs

### Architecture Impacts

- adds a formal consumption layer instead of direct adapter access to raw Design Package objects
- preserves the existing backend-internal Design Package while preparing a future stable execution boundary

### Trust-Boundary Impacts

- none to workflow authority
- clarifies that Discovery Wizard still ends at Design Package production

### Testing Strategy

- contract inventory tests for consumed versus ignored fields
- drift tests for required versus optional semantics
- compatibility tests proving Design Package changes do not silently alter generation semantics

### Success Criteria

- every Design Package field has an explicit consumption classification
- no Microsoft-specific fields are added to the Design Package contract
- raw Design Package exposure to adapters is prohibited by design

## Phase 2 – Skills Prompt Generation

### Scope

- define `generation-request/v1`
- define adapter-composed prompt segment rules
- define target-profile resolution for PBIR Report, Fabric Data App, and deferred Fabric App
- define do-not-infer constraints for prompts

### Dependencies

- Phase 1 consumption inventory
- current Microsoft public guidance for report authoring, report design, report planner, and data app template workflows

### Architecture Impacts

- introduces the new versioned Generation Request boundary
- separates structured intent from provider prompt text

### Trust-Boundary Impacts

- prevents prompt text from becoming the real contract
- keeps provider adapters downstream from locked design intent

### Testing Strategy

- schema validation tests for Generation Request payloads
- projection tests for prompt segment generation by target profile
- negative tests for missing required inputs and unsupported target combinations

### Success Criteria

- Generation Request JSON is authoritative and versioned
- prompt generation is deterministic from structured fields
- no single free-form prompt is required to preserve intent

## Phase 3 – Generation Request Framework

### Scope

- add target artifact profiles
- add generation modes and review-policy states
- add typed generation outcome states
- add provenance extensions for request, adapter, and generated artifact execution

### Dependencies

- Phase 2 schema and prompt model
- existing Design Studio provenance and analyzer handoff patterns

### Architecture Impacts

- introduces the provider-neutral orchestration core
- creates the stable seam future providers can implement without changing Discovery Wizard or Design Studio

### Trust-Boundary Impacts

- generation authority is explicitly constrained to construction and diagnostics
- approval and validation authority remain elsewhere

### Testing Strategy

- generation-request lifecycle state tests
- provenance-shape tests
- failure-state classification tests

### Success Criteria

- request, execution, and outcome states are explicit
- provenance covers package, request, execution, artifact, and handoff
- unsupported and malformed outcomes fail closed

## Phase 4 – PBIR Generation

### Current Subphase Status

- **Phase 4A — Deterministic Modern PBIR Serialization:** implemented by Repository Phase 29
  - consumes canonical pbir-ir/v1 only
  - emits an in-memory modern PBIR artifact inventory and manifest
  - emits definition.pbir and the definition hierarchy, including definition/report.json
  - never emits PBIR-Legacy root-level report.json
  - adds no writer or execution authority
- **Phase 4B — Safe Local Deployable PBIR Materialization with Preview/Apply/Rollback Controls:** proposed as Repository Phase 30; design and implementation plan await explicit approval
  - requires a new goal
  - must add a separate deployable writer
  - must not reuse or widen the preview-only writer
- Provider execution, Microsoft Skills execution, PBIP materialization, Desktop verification, deployment, and publishing: not started

### Scope

- implement the Microsoft PBIR adapter profile
- map Generation Request fields to report-authoring-oriented execution inputs
- define prerequisite checks for PBIP/PBIR project context
- define validation flow using PBIR validation and optional Desktop verification

### Dependencies

- Phases 1-3
- Microsoft report authoring skill guidance
- PBIR and report-definition documentation

### Architecture Impacts

- adds the first concrete provider adapter
- establishes the baseline artifact intake path for generated reports

### Trust-Boundary Impacts

- generation still produces unapproved artifacts only
- successful report creation does not imply validation approval

### Testing Strategy

- adapter projection tests from Generation Request to PBIR-oriented inputs
- structural validation tests for generated report intake
- end-to-end non-deployment tests for complete, partial, malformed, and unsupported outcomes

### Success Criteria

- PBIR is the first supported generated artifact type
- generated PBIR artifacts can be classified and quarantined safely
- analyzer handoff eligibility is based on structural safety, not generation optimism

## Phase 5 – Analyzer Handoff

### Scope

- convert eligible generated artifacts into analyzer handoff candidates
- extend Design Studio or host-side workflow to surface generation status and explicit review launch
- preserve explicit attach-on-return behavior for analyzer results

### Dependencies

- Phase 4 artifact intake
- existing Design Studio to Analyzer Workspace handoff design

### Architecture Impacts

- connects generation output to existing validation workflows without collapsing them
- reuses staged handoff patterns rather than creating a second validation path

### Trust-Boundary Impacts

- reinforces Analyzer Workspace as the validation owner
- forbids automatic analyzer execution and automatic result attachment

### Testing Strategy

- handoff eligibility tests
- stale or malformed provenance rejection tests
- protocol and status tests for generated-candidate review launch

### Success Criteria

- generated artifacts can enter Analyzer Workspace only through an explicit staged handoff
- analyzer ownership remains visible in the workflow
- result attachment remains explicit and auditable

## Phase 6 – Refinement Loop

### Scope

- return analyzer findings and validation outcomes to the generation history
- allow regenerated iterations from prior Design Package or revised Design Package inputs
- display provenance and comparison across generated iterations

### Dependencies

- Phase 5 analyzer handoff
- existing Design Studio refinement and compare-iterations patterns

### Architecture Impacts

- closes the loop without moving refinement authority out of Design Studio
- makes generated iterations first-class reviewable history entries

### Trust-Boundary Impacts

- refinement remains advisory and human-directed
- analyzer findings inform regeneration, but do not auto-regenerate

### Testing Strategy

- provenance round-trip tests
- iteration-history comparison tests
- regression tests proving validation status is not inferred from design or generation approvals

### Success Criteria

- generated iterations can be reviewed, compared, and refined with full lineage
- the workflow clearly distinguishes generated, reviewed, validated, and superseded artifacts

## Phase 7 – Fabric App Generation

### Scope

- lock the product mapping between internal Fabric App labels and Microsoft target surfaces
- implement the next Microsoft adapter after that mapping is explicit
- support either Fabric Data App first or broader Fabric App support depending on the mapping decision

### Dependencies

- Phase 3 request framework
- explicit terminology decision from the design spec
- current Microsoft Fabric Apps preview guidance

### Architecture Impacts

- adds a second runtime target with different generation and intake behavior than PBIR
- may require app-repo intake, route validation, and browser-based verification rather than report-file validation only

### Trust-Boundary Impacts

- no change to approval ownership
- generated apps remain unvalidated until Analyzer Workspace or a future analyzer-compatible review path accepts them

### Testing Strategy

- target-profile resolution tests
- app-template prerequisite and failure-class tests
- intake tests for supported scaffold, partial scaffold, malformed scaffold, and unsupported deployment shapes

### Success Criteria

- terminology mismatch is resolved before implementation ships
- Fabric-oriented generation uses a distinct adapter profile instead of overloading PBIR assumptions
- generated app artifacts follow the same explicit review discipline as PBIR outputs

## Cross-Phase Workstreams

### Contract Governance

- maintain a required versus optional inventory for Generation Request
- add schema-version rejection behavior for unsupported consumers
- document additive versus breaking changes

### Provenance And Auditability

- extend lineage through generation request, adapter execution, artifact fingerprint, and analyzer run
- expose provenance summaries in both generation and review workflows

### UX And Workflow Messaging

- distinguish design approval, generation approval, and validation approval in the UI
- surface partial and degraded generation clearly
- make review-required status impossible to miss

### Operational Guardrails

- fail fast on unsupported target profiles
- quarantine malformed output
- block analyzer handoff when structural safety is missing

## Recommended Implementation Order

1. Phase 1 – Design Package Consumption Layer
2. Phase 2 – Skills Prompt Generation
3. Phase 3 – Generation Request Framework
4. Phase 4A – Repository Phase 29 deterministic modern PBIR serialization, followed by approval-gated Repository Phase 30 / original Phase 4B materialization and separately authorized later execution work
5. Phase 5 – Analyzer Handoff
6. Phase 6 – Refinement Loop
7. Phase 7 – Fabric App Generation

## Phase Exit Gates

### Gate After Phase 3

- Generation Request schema is stable enough for adapter work
- provenance and failure states are explicit

### Gate After Phase 4

- Phase 4A serialization produces schema-conformant deterministic in-memory modern PBIR artifacts
- Phase 4B materialization and later execution work remain separately approval-gated
- generated output classification remains fail closed

### Gate After Phase 5

- generated artifacts can enter validation without authority collapse

### Gate After Phase 7

- second target profile proves the architecture is actually provider-neutral and not PBIR-only

## Deferred Items

- direct provider execution in production environments
- publish or deployment orchestration
- report-management integration for workspace CRUD
- automatic Desktop Bridge orchestration as a required runtime dependency
- any mutation path that would bypass existing deterministic preview/apply/rollback rules elsewhere in the product
