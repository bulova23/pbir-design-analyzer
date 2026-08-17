# PBIR Design Analyzer Roadmap

This roadmap reflects the post-`0.6.0` product direction after the review workspace expansion, Fabric App review foundations, screenshot and semantic-model evidence, cross-platform packaging, and backend hardening landed on the active branch.

## Current Hardening Status

The engineering hardening roadmap is now implemented through the recommended `0.6.0` bundle on the active branch:

- `0.5.1` deterministic trust restoration
- `0.5.2` operational coherence
- `0.6.0` scalability and protocol maturity

That means the current branch now includes:

- shared repo snapshots for high-impact local analysis paths
- async filesystem conversion on the local PBIR fallback tree and Fabric evidence flows
- single-snapshot Fabric evidence reuse
- versioned score-panel host/webview contracts with payload guards
- selected page-state validation
- externalized Fabric review and readiness scoring constants with provenance
- the first Guided Story Improvements slice powered by validated Story Assessment gaps only

Current Story Assessment promotion posture on the active branch:

- Guided Story Improvements is now the only user-facing Story Assessment slice
- it stays limited to six validated advisory categories
- broader Story Assessment internals still remain intentionally hidden until more validation evidence exists

The next roadmap epic after this hardening pass remains:

1. Consultant Deliverables & Export Platform
2. Visual Intelligence & Screenshot Analysis
3. Enterprise Governance & Advanced Review

## Current Release: 0.6.0

`0.6.0` is the current cross-platform Analytics Experience Review Platform release with:

- Overview
- Issues
- Fix Plan
- advisory proposal enrichment
- Evidence
- Fabric App Readiness
- Fabric App Review foundations
- screenshot evidence
- semantic-model evidence
- analyzable surfaces
- surface discovery
- analyzer registry
- analyzer profiles
- target-specific VSIX packages for Windows x64, Windows arm64, Linux x64, macOS x64, and macOS arm64
- secondary Export
- deterministic preview/apply/rollback trust loop for supported PBIR fix categories

Packaging note for the shipped `0.5.0` set:

- Windows arm64 uses a self-contained backend package for startup reliability on Windows 11 ARM
- the other current targets remain framework-dependent

The next roadmap epics build on that foundation without reopening the scoring architecture.

## Rendered Review

The Optimization Report includes a human-in-the-loop Rendered Review checklist.
It classifies findings, gives category-specific guidance, records reviewer
status and notes, supports user-supplied screenshot evidence, and includes the
review summary in downstream exports. Screenshot capture is manual — PBIR
Design Analyzer does not automate it. See [Rendered Review Guide](integrations/rendered-review-guide.md)
and the [Rendered Review UAT Guide](integrations/rendered-review-uat-guide.md).

Automatic rendered-design scoring against a screenshot provider is not on the
roadmap. Reviewers who want a faster way to capture Power BI report
screenshots for Rendered Review can use an external tool such as
[PBI Lens](https://github.com/thenguyentrong/pbi-lens); PBIR Design Analyzer
does not integrate with it, and deterministic scoring, findings, and policy
evaluation remain fully authoritative either way.

## Design Package To Microsoft Skills Roadmap Mapping

The original seven-phase roadmap remains distinct from the release and AI-fix roadmaps elsewhere in this document.

- Original Phases 1–3: substantially implemented through the existing consumption, prompt, request, runtime, review, and readiness infrastructure
- Original Phase 4A: implemented by Repository Phase 29 as deterministic in-memory modern PBIR serialization
- Original Phase 4B: implemented by Repository Phase 30 — **Safe Local Deployable PBIR Materialization with Preview/Apply/Rollback Controls**
- Original Phase 4 post-4B application integration: implemented by Repository Phase 31 as a narrow orchestration boundary over Phase 29 serialization and Phase 30 preview/apply/recovery inspection
- Repository Phase 32: **RPC Transport Hardening**, shared infrastructure only. It adds no original Phase 4 application feature or operation.
- Repository Phase 33: **Local PBIR RPC Adapter**, a provider-neutral local adapter over Phase 31 using the hardened RpcHost.
- Repository Phase 34: **VS Code Local PBIR Materialization Workflow**, integrated into the existing Report Design Studio materialize stage and using only the three Phase 33 routes.
- Original Phase 4 external provider execution, Microsoft Skills execution, PBIP materialization, Desktop verification, deployment, and publishing work: not started
- Original Phases 5–7: not started as execution phases

Repository Phase 29 emits definition.pbir and the modern definition hierarchy, including definition/report.json. It never emits PBIR-Legacy root-level report.json and does not materialize files.

Repository Phase 30 consumes only the validated Phase 29 artifact and manifest. It adds read-only destination preview, embedded pinned-schema validation, exact-byte staging, same-filesystem directory promotion, external journals and receipts, managed replacement, rollback quarantine, and interrupted-transaction recovery. Arbitrary nonempty targets fail closed, and the preview-only writer remains unchanged.

Repository Phase 31 is the first separately authorized post-4B integration slice. It composes the canonical Phase 29 serializer and Phase 30 services behind one backend application boundary, requires exact validated preview identity plus a fresh transaction ID for apply, exposes typed destination/conflict/recovery/failure outcomes, propagates cancellation safely, and redacts sensitive local paths. It adds no external provider, Skills, deployment, Desktop, Analyzer, or UI authority.

Repository Phase 32 is now explicitly mapped to generic RpcHost transport hardening. It provides strict bounded request parsing, bounded concurrent dispatch, serialized response framing, deterministic request cancellation, connection shutdown cleanup, and redacted transport diagnostics. It does not expose Phase 31 or add PBIR operations.

Repository Phase 33 adds exactly three local routes over Phase 31: PBIR materialization preview, apply, and recovery inspection. The adapter is stateless, read-only for preview/recovery, requires exact preview identity plus a fresh transaction ID for apply, and adds no provider, Skills, UI, deployment, publishing, Desktop, Analyzer, PBIP, semantic-model, or legacy-report authority. It advertises no new initialize capability, preserving existing LanguageClient traffic.

The later repository sequence is provisional planning only and does not authorize implementation. The architecture review in [Roadmap Correction: First PBIR Generation](architecture/roadmap-correction-first-pbir-generation-review.md) supersedes the critical-path interpretation below without rewriting the historical phase record:

1. Repository Phase 34: VS Code local PBIR materialization workflow (implemented in the separately authorized Phase 34 slice)
2. Repository Phase 35A: **Contract-Only Provider Foundation** (implemented)
3. Repository Phase 35B: **Governed Runtime Provider Architecture & Execution Framework** (composition-only, implemented)
4. Repository Phase 35C: **Provider Trust, Sandbox, Audit, and Artifact Safety Foundation** (implemented; offline assurance only)
5. Repository Phase 35D: **Pre-Production Provider Certification and Signed Attestation Foundation** (implemented; certification eligibility only, no production provider)
6. Repository Phase 35E: **OS Sandbox Enforcement and Controlled Execution Containment** (historical Seatbelt probe seam; no enforceable local macOS boundary)
7. Repository Phase 35F: **macOS Containment Mechanism Evaluation and Enforceable Sandbox Selection** (implemented; no acceptable local mechanism proven; admission remains closed)
8. Repository Phase 35G: **Containment Architecture Decision** (implemented; `remote-controlled-execution/v1` selected, not enabled)
9. Repository Phase 35H: controlled remote boundary proof (implemented as an inert in-process contract/transport proof; no Windows worker or provider activation)
10. Repository Phase 35I: Windows worker containment enforcement and certification prerequisite (implemented; `PartiallyProven` until Windows integration evidence)
11. Repository Phase 35J: real Windows execution-validation gate (blocked on a real worker; Phase35K test coverage is implemented; no provider activation)
12. Repository Phase 35K: real Windows containment test implementation and evidence generation (implemented; execution evidence pending)
13. Repository Phase 35L: certified Windows execution of the completed containment suite and measured failure remediation (deferred until a concrete provider establishes the requirement; no provider activation)
14. Repository Phase 36: **First Local PBIR Generation Provider** using the existing Phase 29–31 local path and a narrow supported subset (implemented; backend-only, one page/one card, immediate analyzer round-trip)
15. Repository Phase 37: **Incremental PBIR Authoring** over the stabilized Phase 36 path (multiple pages, card and table visuals, typed measure/dimension bindings, bounded deterministic layout, and analyzer round-trip; implemented backend-only)
16. Repository Phase 38: **Rich PBIR Authoring** — typed formatting, deterministic themes, equality filters, basic interactions, metadata, and layout enhancements over the existing Card and Table catalog (implemented backend-only)
17. Repository Phase 39: **Generalized Visual Bindings and Chart Support** — additive v4 role bindings and a backend-only Clustered Column Chart over the shared IR (implemented; Series, Legend, Tooltip, and Axis roles remain reserved for later phases; do not expose RPC or VS Code until the model stabilizes)
18. Repository Phases 40–41: original Phase 6 refinement loop
19. Repository Phase 42: **Report Mutation Foundation** — imported PBIR discovery, deterministic mutation planning, and structural execution (implemented backend-only; serialized fidelity was intentionally deferred)
20. Repository Phase 43: **Lossless Authoring IR, Identity Preservation, and Round-Trip Fidelity** — bounded hybrid envelope, typed merge precedence, imported identity retention, fidelity/hash classification, and analyzer comparison (COMPLETE; implemented backend-only; semantic-lossless bounded authoring only)
21. Repository Phase 44: **Semantic Binding Projection and Full Round-Trip Fidelity** — descriptor-based import projection, unsupported-role preservation, shared-IR semantic equivalence, analyzer-before/after evidence, and stage timing observations (implemented backend-only; RPC remains deferred)
22. Repository Phase 45: **Minimal Typed PBIR Authoring Contract** — complete as the transport-independent `pbir-authoring-rpc/v1` dispatcher over generation v1–v7, import, mutation, validation, and analysis. The core contract remains independent of RpcHost and VS Code.
23. Repository Phase 46: **Minimal VS Code Integration for Generate, Import, and Analyze** — implemented as one thin `pbir/authoring` route and three output-channel commands. Mutation and standalone Validate remain backend-only.
24. Repository Phase 47: **Interactive Mutation Workflow and Visual Diff Preview** — exactly one public `RenamePage` mutation through the existing authoring RPC, with backend-generated preview, confirmation, deterministic re-planning, analyzer before/after comparison, and opaque artifact-handle return. Other mutation domains remain backend-only.
25. Deferred milestone after first artifact: Windows Validation / Hosted Execution for a provider that demonstrates a Windows, Desktop, or untrusted-execution requirement

Each later phase requires separate authorization. Phase 32 must not be interpreted as authority for any item in this sequence.

Phase 34 did not implement external provider or Microsoft Skills execution. Phase 35A adds versioned contracts and deterministic governance. Phase 35B adds only an offline composition root and fake-provider test seam. Phase 35C adds the fail-closed trust, containment policy, credential, audit, replay/resource, artifact-safety, corpus, conformance, and activation contracts. Phase 35D adds deterministic package identity, signed offline attestation verification, certification evidence/lifecycle, exact activation binding, non-executing conformance, and bounded protected audit/replay persistence. Phase 35E adds the historical macOS Seatbelt probe seam; it does not establish enforcement on Darwin 27. Phase 35F evaluates local macOS mechanisms and selects no acceptable local mechanism. Phase 35G selects controlled remote execution as a future boundary for a provider that may depend on Power BI Desktop; that premise remains unproven and the boundary is not enabled. Phase 35H adds only an inert signed domain-protocol proof with an in-process transport harness, persisted replay/lifecycle state, correlated audit, and synthetic artifact quarantine. Phase 35I adds portable exact admission/evidence, a repository-owned inert runner, and one Windows-native restricted-token/Job Object boundary; it remains `PartiallyProven` because no certified Windows execution has occurred. Phase 35J records the environment gate. Phase 35K implements the closed Windows suite and preserves non-Windows skip behavior, but does not create Windows evidence on macOS. Phase 35L remains deferred until a concrete provider demonstrates a Windows, Desktop, or untrusted-execution requirement. Phase 36 now provides a separate deterministic backend-only local provider for a narrow PBIR subset; it does not activate the Phase 35 runtime provider catalog. Phase 39 adds the generalized role-bearing binding vocabulary to the shared IR and provider v4 contract, mapping Card, Table, and Clustered Column Chart through the existing Phase 29 serializer and Phase 31 materialization path. Series, Legend, Tooltip, richer Axis configuration, semantic-model generation, DAX generation, RPC/VS Code exposure, Analyzer automation, refinement, Fabric App generation, deployment, and publishing remain unimplemented.

## Story Assessment Promotion Boundary

Implemented now:

- Guided Story Improvements

Still deferred:

- archetype exposure
- semantic coherence exposure
- confidence breakdown exposure
- competing-story exposure
- special-page labels as product concepts

The Story Assessment boundary remains:

- advisory-only promotion from validated gaps
- no mutation authority
- no research-stage diagnostics in the public contract

## AI Fix Roadmap

The AI-fix roadmap remains intentionally staged:

1. Phase 1: Deterministic Fix Opportunity Engine
2. Phase 2: Preview / Apply / Rollback Hardening
3. Phase 3: AI-assisted proposal enrichment
4. Phase 4: Advanced AI refactoring
5. Phase 5: Report design studio

Permanent guardrails for every phase:

- intelligence may improve proposal quality later
- intelligence must not replace deterministic execution
- all report changes must still flow through the deterministic mutation layer
- all report changes must still respect the preview/apply/rollback execution trust boundary

Phase 2 hardening is now implemented on the active branch. It hardened the existing trust loop without introducing model calls, provider integration, or AI-driven execution.

Phase 3 proposal enrichment is also now implemented on the active branch as an advisory-only layer above the deterministic mutation workflow. It adds grounded, validated, fallback-safe proposal wording and expected-outcome guidance while keeping all report edits inside the existing preview/apply/rollback trust loop.

## Analytics Experience Review Platform

The platform direction now centers on a broader Analytics Experience Review Platform through the new `Analyzable Surface` architecture:

- analyzable surface
- surface discovery
- analyzer selection
- analyzer profile selection
- normalized findings, evidence, remediation, and governance outputs
- one shared workspace

Implemented first slice:

- `PBIR Report Surface`
- `Fabric App Readiness Analyzer`
- analyzer profiles:
  - `default`
  - `migrationReadiness`

Implemented second-surface foundation slice:

- Fabric App Surface
- Fabric App Review Analyzer
- analyzer profiles:
  - default
  - fabricAppQuality
- bounded evidence extraction for:
  - TypeScript layout
  - navigation
  - design tokens
  - screenshots
  - semantic-model usage
- richer finding-to-evidence linkage across Fabric App review findings
- shared-workspace rendering through:
  - Overview
  - Issues
  - Fix Plan
  - Evidence
- graceful degradation when screenshots or semantic-model artifacts are absent

This slice is advisory-only and answers:

- should this PBIR report become a Fabric App

It does not yet implement:

- governance integration
- screenshot intelligence
- Fabric App fixes
- code generation
- Fabric App mutation

## Recommended Order

### 1. AI Fix Phase 2: Preview / Apply / Rollback Hardening

**Business value:** High  
**Risk:** Low to Medium  
**Complexity:** Medium  
**Quick wins:** Medium  
**Strategic value:** Very High

Why first:

- the deterministic fix workflow is now shipped and should be hardened before any AI-enrichment work
- stronger conflict detection, batch safety, rollback visibility, and diff clarity improve trust in the current product surface
- hardening the trust loop reduces risk before broader deliverable, evidence, and governance expansion

Quick wins:

- clearer stale/conflict messaging
- safer multi-opportunity sequencing
- better rollback history visibility
- richer post-apply diff and outcome summaries

Longer-term value:

- stable execution foundation for later AI-assisted proposal enrichment
- lower-risk path to future multi-fix workflows
- clearer operational model for consultant and enterprise users

### 2. UX Architecture Consolidation

**Business value:** High  
**Risk:** Low to Medium  
**Complexity:** Medium  
**Quick wins:** High  
**Strategic value:** Very High

Why second:

- the daily review workspace is the core product surface users touch most often
- the current architecture is directionally right but still carries duplication, fragmented reasoning, and excess scroll depth
- tightening the workspace before adding more features reduces future product complexity
- all deferred epics benefit from a calmer, more stable review workflow foundation

Quick wins:

- clearer distinction between diagnosis and remediation
- summary-first page-purpose analysis
- qualitative-first matrix interpretation
- reduced duplication and improved scanability

Longer-term value:

- cleaner interaction model for consultant deliverables
- better platform base for visual evidence workflows
- stronger UI stability before governance and enterprise expansion

### 3. Consultant Deliverables & Export Platform

**Business value:** High  
**Risk:** Medium  
**Complexity:** Medium  
**Quick wins:** High  
**Strategic value:** High

Why first:

- export and packet-preview foundations already exist
- the product can turn current review outputs into clearer consultant/client deliverables quickly
- this improves real-world adoption, sharing, and monetizable presentation quality

Quick wins:

- cleaner export workspace
- persona-aware export-summary wording
- stronger executive summary language
- branded consultant-ready packet variants

Longer-term value:

- future DOCX/PDF architecture
- AI-generated executive narrative options
- export as a first-class deliverables platform

## 4. Visual Intelligence & Screenshot Analysis

**Business value:** High  
**Risk:** Medium  
**Complexity:** Medium to High  
**Quick wins:** Medium  
**Strategic value:** High

Why third:

- screenshot audit foundations already exist
- visual overlays and screenshot-to-finding linkage improve explainability and perceived intelligence
- this creates a strong product differentiator after export/deliverable polish

Quick wins:

- screenshot-to-finding linkage
- visual evidence navigation
- annotated evidence panels

Longer-term value:

- reading-order overlays
- density heatmaps
- alignment and focus-area highlighting

## 5. Enterprise Governance & Advanced Review

**Business value:** Medium to High  
**Risk:** High  
**Complexity:** High  
**Quick wins:** Low to Medium  
**Strategic value:** Very High

Why fourth:

- this touches the broadest set of contracts and workflows
- it depends on stable configuration, benchmark, and review patterns
- it is strategically strong but less release-efficient than deliverables and visual explainability

Quick wins:

- organization profile scaffolding
- better configuration IA
- benchmark summary expansion

Longer-term value:

- custom standards and templates
- industry-specific review modes
- bookmark-state analysis
- mobile/responsive review workflows

## Epic Summary

### AI Fix Phase 2: Preview / Apply / Rollback Hardening

See:

- [Design Spec](./superpowers/specs/2026-06-01-ai-fix-phase2-hardening-design.md)
- [Implementation Plan](./superpowers/plans/2026-06-01-ai-fix-phase2-hardening-plan.md)

Status:

- implemented on the active branch with compatibility evaluation, grouped preview, deterministic batch apply orchestration, rollback/session history visibility, stale regeneration messaging, and grouped outcome summaries
- preserves the deterministic mutation layer and keeps scoring, severity, confidence, and normalized finding semantics unchanged
- Phase 3 proposal enrichment is now the implemented advisory layer above this foundation

### UX Architecture Consolidation

See:

- [Design Spec](./superpowers/specs/2026-05-31-ux-architecture-consolidation-design.md)
- [Implementation Plan](./superpowers/plans/2026-05-31-ux-architecture-consolidation-plan.md)

### Consultant Deliverables & Export Platform

See:

- [Design Spec](./superpowers/specs/2026-05-31-consultant-deliverables-export-platform-design.md)
- [Implementation Plan](./superpowers/plans/2026-05-31-consultant-deliverables-export-platform-plan.md)

### Visual Intelligence & Screenshot Analysis

See:

- [Design Spec](./superpowers/specs/2026-05-31-visual-intelligence-screenshot-analysis-design.md)
- [Implementation Plan](./superpowers/plans/2026-05-31-visual-intelligence-screenshot-analysis-plan.md)

### Enterprise Governance & Advanced Review

See:

- [Design Spec](./superpowers/specs/2026-05-31-enterprise-governance-advanced-review-design.md)
- [Implementation Plan](./superpowers/plans/2026-05-31-enterprise-governance-advanced-review-plan.md)

### AI Fix Phase 3: AI-Assisted Proposal Enrichment

See:

- [Design Spec](./superpowers/specs/2026-06-02-ai-proposal-enrichment-design.md)
- [Implementation Plan](./superpowers/plans/2026-06-02-ai-proposal-enrichment-plan.md)

Status:

- implemented on the active branch as a grounded advisory layer for remediation-item title suggestions, explanation copy, why-this-matters summaries, advisory priority, expected outcomes, and fallback-safe alternatives
- keeps provider logic outside scoring and outside deterministic mutation application
- preserves the permanent trust boundary:
  - AI enriches proposals only
  - AI does not generate mutations
  - AI does not apply mutations
  - preview/apply/rollback and re-analysis remain deterministic
- next AI-fix roadmap step remains Phase 4 advanced AI refactoring, not hidden execution changes inside Phase 3

### Fabric Apps Analytics Review

See:

- [Design Spec](./superpowers/specs/2026-06-03-fabric-apps-analytics-review-design.md)
- [Implementation Plan](./superpowers/plans/2026-06-03-fabric-apps-analytics-review-plan.md)

Status:

- Release Slice 1 is now implemented on the active branch as `Fabric App Readiness Assessment`
- adds:
  - `Analyzable Surface` foundations for PBIR
  - automatic PBIR surface detection
  - analyzer registry and profile selection support
  - advisory migration-readiness scoring across layout, interaction, narrative, semantic-model, navigation, governance, accessibility, and visualization-as-code dimensions
  - readiness findings, evidence, and remediation integrated into the existing shared workspace
- keeps the trust boundary intact:
  - readiness remains advisory-only
  - deterministic mutation authority remains PBIR-only and unchanged
- Release Slice 2A foundations are now implemented on the active branch as `Fabric App Review Mode Foundations`
- adds:
  - Fabric App surface discovery
  - Fabric App Review Analyzer
  - bounded TypeScript, navigation, and design-token evidence extraction
  - shared-workspace Fabric App findings, fix-plan guidance, and evidence rendering
- keeps the trust boundary intact:
  - Fabric App review remains advisory-only
  - no Fabric App mutation path was introduced
- next step for this initiative is deeper Fabric App review, not code generation or migration automation

### Rayfin (Microsoft Fabric) — Deferred Evaluation

Microsoft announced [Rayfin](https://github.com/microsoft/rayfin) at Build 2026: an open-source, early-access SDK/CLI for generating full-stack applications hosted on Microsoft Fabric — a TypeScript frontend plus a Fabric SQL database or Power BI semantic-model backend, Entra ID auth, and a generated GraphQL API, deployed as a Fabric workspace item.

This is a different artifact from the `Fabric App Readiness Assessment` and `Fabric App Review Mode Foundations` above, which both operate on PBIR-adjacent surfaces (a PBIR report, or a generic web repo used as a stand-in surface). A Rayfin app has no PBIR content and no fixed declarative report schema — the frontend is arbitrary generated TypeScript/React code, so the static evidence-extraction approach this analyzer is built around does not transfer directly. Scoring a Rayfin app's UI would most likely require either a render-and-review approach (screenshot the running app and evaluate it, since there is no spec to parse) or a separate static code/accessibility review of the generated frontend — a different kind of analyzer than PBIR Design Analyzer is today. The one plausible overlap is evaluating a Rayfin app's Power BI semantic-model backend, if present, with this analyzer's existing semantic-model evidence capability.

Status: not started. Rayfin is early access (~2 months old as of this writing) and its architecture is still likely to change. Revisit once it stabilizes and real Rayfin app samples are available to design against, rather than building against announcement-stage documentation.

## Guardrails

### Phase 40 — Advanced Chart Authoring and Reusable Visual Templates

Phase 40 is implemented as a backend-only additive v5 contract over the generalized binding model. The closed catalog supports Card, Table, Clustered Column Chart, Line Chart, Bar Chart, and Pie Chart with deterministic templates and schema-safe formatting projection. V1–v4 remain backward compatible. Phase 41 should shift to report composition features: reusable report sections, page templates, navigation, slicers, and richer interaction models. Public RPC and VS Code exposure remain deferred.

### Phase 41 — Report Composition

Phase 41 is implemented as a backend-only additive v6 contract. The provider composes deterministic Executive Summary, Overview, Detail, and Comparison pages from typed sections and slots, validates typed page navigation, and adds a narrow Slicer descriptor with one Category Dimension binding. Composition projects into the existing visual-layout, IR, serializer, materialization, schema-validation, and analyzer path. V1–v5 remain compatibility paths. Shared/synchronized slicers, bookmarks, drillthrough, dedicated tooltip emission, and public RPC/VS Code exposure remain deferred.

### Phase 42 — Explicit Same-Page Slicer Interactions

Phase 42 is complete as the backend-only additive v7 contract for explicit
slicer-to-visual interactions. It emits the pinned page-level
`visualInteractions` entries with schema-backed `Default`, `DataFilter`,
`HighlightFilter`, or `NoFilter` values, validates source/target identity and
same-page scope, and preserves v1–v6 behavior. Page-scope targets,
synchronized slicers, bookmarks, drillthrough, cross-page interactions,
public RPC, and VS Code exposure remain deferred.

The report-mutation foundation present in the starting checkout is preserved
as an adjacent backend-only capability; this Phase 42 slice does not broaden
or reclassify that work.

### Phase 46 — Imported Page Rename Authoring

Phase 46 was the direct typed backend milestone selected after the Phase 45
decision. It implemented one imported `RenamePage` operation using the
existing `LocalPbirMutationRequest/v1` fields and the single copy-on-write
merge boundary. It preserves page folder identity, visual identity and order,
Phase 42 interactions, and unrelated pinned-schema-admitted source properties.

Backend orchestration and backend tests were the intended callers. Phase 47
now authorizes the separately bounded public workflow described below.

### Phase 47 — Interactive Mutation Workflow and Visual Diff Preview

Phase 47 adds only the Rename Page workflow through the existing
pbir-authoring-rpc/v1 route. Import returns transport-safe page metadata;
preview invokes the backend planner without materializing; confirmation gates
execute; execute re-plans from the immutable snapshot and then uses the
existing executor, merge, serializer, validator, materializer, and analyzer.
The result returns a new opaque artifact handle plus analyzer before/after,
fidelity, identity, diagnostics, and timings.

The public boundary rejects every other mutation kind. Dynamic mutation
capability discovery, batching, undo/redo, webview editing, and raw JSON remain
deferred until real Rename Page usage establishes the next stable contract.

### Phase 48 — Curated Mutation Expansion and Multi-Operation Planning

Phase 48 expands the public mutation allowlist to Rename Page, Add Page, Remove
Page, Move Page, Move Visual, and Resize Visual. Each request still contains
exactly one operation and follows backend planning, typed preview, standard
confirmation, authoritative re-planning, copy-on-write execution, analyzer
comparison, and new artifact-handle return. The planner owns target
resolution, order/navigation safety, layout validation, no-op detection,
semantic diffs, and identity evidence.

The public preview carries a common admissibility/identity model plus typed
page or visual details and semantic diff records. Add Visual, Remove Visual,
Replace Visual, binding, formatting, theme, filter, navigation, and slicer
operations remain backend-only. Capability discovery, public batching,
undo/redo, mutation registries, graphical editing, raw JSON editing, and
Windows/hosted execution remain deferred.

These roadmap epics should not:

- rewrite the core scoring engine unnecessarily
- mutate score or finding severity/confidence from presentation modes
- replace normalized findings as the shared issue model
- turn temporary roadmap experiments into hidden scoring logic
- bypass the deterministic mutation layer for report edits
- let future AI features bypass explicit preview, apply, rollback, and outcome reporting

The preferred path is:

- stable scoring layer
- stable findings layer
- stable deterministic mutation layer
- richer advisory review, evidence, export, and governance workflows built above them
