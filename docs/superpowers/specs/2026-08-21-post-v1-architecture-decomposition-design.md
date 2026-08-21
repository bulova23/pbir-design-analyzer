# PBIR Design Analyzer Post-v1.0 Architecture Decomposition Design

Date: 2026-08-21

Status: Design and implementation planning only. No production decomposition is authorized by this document.

Baseline: `origin/main`, tag `v1.0.0`, commit `4c56eaf37f4829640051ec121d9f6f5103aa7084`.

## Purpose and scope

This design defines a behavior-preserving, incremental decomposition of the remaining high-coupling scoring and score-workspace modules. It treats the v1.0 safeguards as the compatibility boundary and keeps deferred product expansion out of scope.

In scope:

- `service-dotnet/Services/Pbir/PbirScoringService.cs`
- `vscode-extension/webview-src/analyzer-score/App.tsx`
- `vscode-extension/src/views/PbirScorePanel.ts`
- directly coupled score contracts, payload normalization, protocol, router, and existing workflow services
- evidence-based disposition of adjacent large modules
- architecture controls and an implementation plan

Out of scope:

- Consultant Deliverables & Export Platform feature work
- Visual Intelligence or screenshot interpretation
- Enterprise Governance expansion
- provider execution, deployment, or broad report generation
- deleting dormant provider/runtime infrastructure
- changing scoring formulas, readiness policy, protocol versions, mutation authority, or product behavior

## Evidence baseline

The v1.0 release evidence establishes representative scoring goldens, deterministic repeat, generated score-panel contract freshness, protocol validation, selected-page clamping, packaged scoring, mutation preview/apply/rollback, and export acceptance. The product scope classifies deterministic PBIR review as Core, Fabric App Review as advisory Optional, dormant provider/runtime infrastructure as Experimental, and provider execution, Visual Intelligence, broad generation/deployment, and enterprise governance expansion as Deferred.

The repository has no `docs/architecture/current.md`; the closest active architecture authority is `docs/architecture/contract-schema-and-ownership-strategy.md`, supplemented by `docs/product/scope.md`, `AGENTS.md`, the v1 readiness report, and the completed consolidation plan. This design does not silently create a second product-scope authority.

## Current architecture and dependency assessment

### Candidate inventory

| Module | Lines | Classification | Why it is large | Disposition |
|---|---:|---|---|---|
| `PbirScoringService.cs` | 9,997 | Mixed domain/orchestration monolith with historical aggregation | Ten framework scorers, visual metadata and intent inference, story assessment helper implementations, bookmark overlays, report consistency, page/report orchestration, and result preparation share one class | Include; decompose in vertical slices |
| `App.tsx` | 4,573 | Presentation monolith with application orchestration | Pure formatters/projections, seven workspace sections, issue filtering, persona defaults, local state, protocol consumption, and rendering accumulated in one file | Include; decompose by feature/state ownership |
| `PbirScorePanel.ts` | 920 | Host lifecycle/orchestration monolith | VS Code lifecycle, surface dispatch, score invocation, payload projection, persistence, diagnostics, workflow wiring, and message error containment coexist | Include; thin the coordinator without weakening router safety |
| `scorePanel.ts` | 1,287 | Contract aggregation / compatibility surface | It contains the broad score-panel state and message vocabulary, including presentation and workflow fields | Include only for ownership annotations and generated-contract migration; do not split mechanically |
| `scoreResultPayload.ts` | 1,118 | Boundary normalization/projection monolith | Runtime payload validation, compatibility defaults, surface/analyzer selection, normalized findings, readiness, fix plan, overview, and cross-page projections are coupled at the host/webview boundary | Include after baseline; split normalizers from presentation projections |
| `RecommendationEngineService.cs` | 2,567 | Cohesive domain service, not a scoring monolith | Discovery recommendation generation and rationale shaping have a coherent domain, although the file has historical growth | Defer; add dependency evidence only |
| `LocalPbirGenerationProviderService.cs` | 1,973 | Future-provider orchestration monolith | Contract-only/local generation, artifact verification, mutation/materialization concerns, and analyzer calls share future infrastructure | Leave alone for this program; enforce it stays outside scoring composition |
| `scorePanelProtocol.ts` | 467 | Cohesive transport/protocol boundary | Envelope versioning, parsing, state validation, and page-index clamping are one safety boundary | Leave cohesive; add tests/ownership comments, do not split |
| `scorePanelMessageRouter.ts` | 173 | Cohesive routing adapter | Validated message dispatch and callback invocation form one transport boundary | Leave cohesive; preserve centralized routing and panel try/catch |

The line count is a risk signal, not a decomposition rule. `scorePanelProtocol.ts` and `scorePanelMessageRouter.ts` are intentionally cohesive and should not be split merely to improve metrics.

### Production composition root

`service-dotnet/RpcHost/Program.cs` constructs `PbirProjectService`, `PbirTreeBuilder`, `PbirScoringService`, and `PbirGovernanceService`; `AnalyzerRpcDispatcher` routes score, tree, governance, materialization, and authoring methods. The shipped composition root does not register Phase 35, runtime-provider, or authoring infrastructure directly, although authoring/materialization adapters exist behind explicit RPC routes. The target decomposition must preserve this composition shape and make any new scoring-stage registration explicit.

The extension composition root is `PbirScorePanel`: it constructs the state service, validated message router, audit workflow, export workflow, and fix workflow. It then loads the webview and routes fire-and-forget VS Code messages through `handleMessage`, whose `try/catch` is a required user-visible error boundary.

### Existing test and contract protection

- Backend: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`, `ScoringCharacterizationTests.cs`, `RepresentativeCorpusCharacterizationTests.cs`, post-v1 baseline tests, score assembly tests, configuration tests, cross-page/story tests, and `ArchitectureDependencyBoundaryTests.cs`.
- Extension: `App.test.tsx`, `scoreResultPayload.test.ts`, `scorePanelProtocol.test.ts`, `scorePanelMessageRouter.test.ts`, `pbirScorePanelScoring.test.ts`, navigation tests, workflow-service tests, and generated contract validation.
- Contracts: `contracts/score-panel/v1/schema.json`, generated C# `RpcHost/GeneratedScorePanelContract.g.cs`, generated TypeScript `src/generated/scorePanelContract.ts`, and `npm run validate:contract`.
- Release evidence: representative fixture goldens, normalized fingerprints, package acceptance, and mutation/rollback evidence in `docs/release-evidence/`.

## Scoring responsibility and dependency map

### Actual pipeline

```text
reportPath + optional config/pageName
  -> ReportDiscoveryService.ResolveRequiredReportLocation
  -> ReportModelLoader.LoadReportModel
  -> ThemeResolutionService.ResolveThemeColors
  -> ScoringConfigurationService (framework weights + navigation settings)
  -> report/page context preparation
  -> framework scoring and metadata/story analysis
  -> bookmark-aware overlay when applicable
  -> per-page scoring with partial-failure capture and original-order restoration
  -> CrossPageNarrativeOrchestrator report/page promotion
  -> ScoreResultAssemblyService + ScoreCompatibilityAdapter
  -> ScoreResult / PageScore
```

The service has two public behavioral modes: full report and exact-name single page. Full-report mode computes report-level scores before the per-page loop, scores pages in parallel with a maximum degree of four, records page failures without recalculating the report composite, and restores original page order before exposing `PageScores`. Single-page mode throws for an unknown exact page name, handles zero-data-visual pages specially, and may overlay bookmark-state averages.

### Responsibility map

| Responsibility | Current owner | Inputs | Outputs/side effects | Coupling/risk | Target owner |
|---|---|---|---|---|---|
| Report location and model load | `PbirScoringService` delegates to discovery/loader | path | `PbirReportLocation`, `ReportModel` | Low; already cohesive services | Existing discovery/loader behind `ReportAnalysisContext` |
| Theme/config normalization | service delegates to theme/config services | report JSON, location, config | theme colors, weights, navigation settings | Ordering affects all scorers | Context factory using existing services |
| Visual composition/layout inventory | private helpers in service | visuals, navigation settings | counts, layout issues, metadata inputs | Shared by many stages | `VisualCompositionAnalyzer` / context-owned projection |
| Ten scoring frameworks | private methods in service | pages, theme, config, recommendation buffer | score + ordered feedback + recommendation mutations | Shared `PageData`, recommendations, repeated helpers | Domain analyzers grouped by actual cohesion, not one-class-per-method |
| Accessibility color math | private pure helpers | hex colors | normalized colors, contrast/colorblind decisions | Pure and low coupling | `AccessibilityColorMath` |
| Visual metadata and semantic descriptors | private helpers in service | page/visual JSON | metadata, semantic assignments, chart intent | Used by visual/story/consistency paths | `VisualMetadataAnalyzer` |
| Story signal/inference helpers | large private/internal region plus `StoryAssessmentOrchestrator` | page, filters, consistency, story assessment types | internal story assessments and promoted summaries | Historical aggregation; public behavior sensitive | Existing story domain services, extracted in story vertical slice |
| Report consistency/cross-page analysis | service private helpers plus `CrossPageNarrativeOrchestrator` | ordered pages and visual metadata | consistency context, report/page findings | Requires all pages and deterministic sorting | `ReportConsistencyAnalyzer` plus existing cross-page orchestrator |
| Bookmark-aware re-score | service private methods | page, report JSON, theme, config | averaged framework values, per-state scores, recommendation | Re-enters scoring and changes final scores | Explicit `BookmarkAwareScoring` boundary after stage extraction |
| Finding/feedback/evidence construction | `FeedbackItem`, `ScoredFeedback`, affected visual helpers plus stage code | finding facts and affected visuals | ordered `FrameworkFeedbackItem`/evidence | Duplicated construction and ordering risk | `FrameworkFeedbackBuilder` with append-only ordered API |
| Composite/legacy score calculation | `ScoreResult`/`PageScore` computed properties + compatibility adapter | framework values and weights | rounded composite and legacy aliases | Public contract; rounding/weight semantics critical | Keep model authority; extract no formula until characterization exists |
| Readiness | backend result fields plus extension readiness analyzer | normalized result/report surface | readiness assessment/findings | Must not migrate scoring decisions into UI | Existing analyzer/profile readiness layer |
| Result assembly | `ScoreResultAssemblyService` | `ScoreResultAssemblyInput` | `ScoreResult`/`PageScore` | Already extracted and tested | Keep as boundary; do not duplicate |
| Diagnostics/fingerprint | extension `scoreDiagnostics`, backend scoring diagnostics fields | result, report path, versions | normalized diagnostic/fingerprint evidence | Sensitive to timestamps/path filtering | Keep downstream and stable |

### Hidden coupling and determinism hazards

1. The recommendation buffer is mutable and is passed into several scoring methods. Extraction must define ownership and preserve append order.
2. Feedback lists and affected-visual lists are contract-visible. `OrderBy`/`ThenBy`, source-page order, and construction order must remain unchanged.
3. Full-report page work is parallel but explicitly re-ordered by original page index. A stage must not rely on concurrent insertion order.
4. The report composite is calculated before page-level partial failure handling. Moving page scoring earlier would change semantics.
5. Bookmark-aware scoring recursively invokes framework scoring and then mutates the result through `ScoreCompatibilityAdapter`; it is not an ordinary independent stage.
6. Story assessment helpers are partly called by separate services through internal static methods. Moving them without adapter shims would create hidden test and consumer breakage.
7. `ScoreResult` and `PageScore` own composite rounding/weights and legacy properties. A new calculator must not create a second formula authority.

### Scoring extraction boundary

The target is an orchestration service that owns mode dispatch, context creation, stage registration/order, parallel page execution, error capture, bookmark overlay coordination, cross-page promotion, and result assembly. Stages consume immutable or read-only `ReportAnalysisContext` and return a typed `StageAnalysisResult` containing ordered framework feedback, optional recommendations, and evidence projections. Stages must not know about VS Code, RPC, mutation execution, provider infrastructure, or webview contracts.

The initial target stage set is:

- `AccessibilityScoreAnalyzer` after pure color math extraction.
- `VisualBestPracticesScoreAnalyzer` with visual composition/metadata dependencies made explicit.
- `ReportConsistencyAnalyzer` for report-level cross-page facts.
- `StoryAssessmentAnalyzer` as a wrapper around existing story orchestrators and helper adapters.
- remaining framework analyzers only after shared context and feedback construction are stable.

No generic plug-in framework is introduced. Stage registration is a small explicit ordered collection owned by the scoring orchestrator; it is not discovered from reflection.

## Webview responsibility and state map

### Current App responsibilities

`App.tsx` contains approximately 90 top-level helpers/renderers plus the root component. Its logic falls into four categories:

| Category | Current examples | Target ownership |
|---|---|---|
| Pure presentation projection | score tones, feedback breakdown, finding labels, readiness labels, overview cards, story narratives, review status | Feature-local projection modules or shared score-workspace presentation utilities |
| Feature rendering | overview, issues, evidence, readiness, fix plan, story assessment, rendered review, review summary | One component/module per durable workspace section |
| Local application state | view state, active page, expansion toggles, issue filters/grouping, feedback/persona state, fix expansion | Root workspace reducer for cross-feature state plus local feature hooks for feature-only state |
| Transport interaction | window message listener, envelope creation, host commands | `useScorePanelHostMessages` and `useScorePanelCommands`; feature components receive typed callbacks |

The root currently initializes 20+ state values and directly processes `scoreState`, `auditState`, `auditAnalyzing`, and error messages. It also resets issue filters and persona defaults based on received score state. This makes unrelated feature changes share rerender and lifecycle coupling.

### Target feature boundaries

The durable sections remain the product order: Overview, Issues, Fix Plan, Review Summary, Story Assessment, Rendered Review, Evidence, with secondary Export. Readiness is a projection within Overview/Issues rather than a new top-level product section unless current behavior evidence shows otherwise.

Each feature receives a stable `ScoreWorkspaceViewModel` projection and typed commands. It does not calculate backend scores.

| Feature | Owns | Reads | Commands/events | Tests |
|---|---|---|---|---|
| Overview | overview cards, persona presentation, cross-page matrix navigation | overview summary, selected page, normalized findings, readiness, matrix | select page/context, set workspace persona, matrix issue filter | existing App assertions plus projection tests |
| Issues | filters, grouping, finding/evidence rows | normalized findings, page list, selected page, readiness roles | change/clear/reset filters, reveal visual/target | filter/grouping tests |
| Fix Plan | opportunity list and expansion/selection display | fix workflow payload, normalized findings, advisory enrichments | preview/approve/apply/rollback/regenerate commands | existing fix interaction tests |
| Review Summary | reviewer status/commentary and intent feedback | review entries, comments, page summaries | set status, note, intent feedback | review-summary component tests |
| Story Assessment | story narrative, signals, gaps, confidence, guided improvements | page story fields, story snapshots/diffs, reviewer feedback | confirm intent, save note, select page | existing story presentation tests |
| Rendered Review | checklist, status, note, screenshot attachment | rendered review state, findings, audit attachments | set status/note, attach screenshot | rendered review tests |
| Evidence | framework feedback, metadata, Fabric App advisory evidence, audit coverage | score result, audit state | reveal visual, upload/assign/analyze screenshots | evidence rendering tests |
| Readiness projection | readiness callouts/cards and readiness filter semantics | `readinessAssessment`, readiness findings | navigate to page/finding | readiness tests |

State design: use a small `useReducer` for cross-feature workspace state only—view state, selected page index, workspace persona, issue filter/grouping, section expansion, and review filters. Keep feature-only transient state local. Do not add Redux/Zustand. The reducer must call `clampSelectedPageIndex` whenever a score payload changes or a page selection command arrives.

### Webview transport boundary

The webview owns no scoring decision. `useScorePanelHostMessages` parses `ScorePanelHostToWebviewMessagePayload` via the existing protocol parser before dispatching reducer actions. `useScorePanelCommands` emits the existing versioned envelope. `App` composes these hooks and feature components. `scorePanelProtocol.ts` and `scorePanelMessageRouter.ts` remain cohesive and unchanged in contract/version semantics.

## Panel responsibility map

### Current responsibilities

| Responsibility | Current methods/area | Target owner |
|---|---|---|
| singleton/lifecycle/webview HTML/disposal | `createOrShow`, constructor, `getReactHtml`, `dispose` | `PbirScorePanel` |
| validated message routing/error containment | `handleMessage`, router construction | `PbirScorePanel` + existing router |
| score invocation | `refresh`, `executePbirOptimizationScore` | `ScorePanelAnalysisCoordinator` |
| surface discovery and Fabric App advisory branch | `refresh` | `ScorePanelSurfaceCoordinator` or analysis coordinator using analyzer registry |
| score normalization and presentation projection | `refresh`, `buildPresentationResult`, `postScoreState` | existing payload/projection services, then `ScorePanelStatePublisher` |
| state storage and page clamping | `scoreState` callbacks | existing `scorePanelStateService` |
| fix workflow | existing service plus callback wiring | existing `scorePanelFixWorkflowService`; panel stores only lifecycle dependencies |
| audit workflow | existing service plus session/provider | existing `scorePanelAuditWorkflowService`; panel retains provider/session accessors |
| export workflow | existing service | existing `scorePanelExportWorkflowService` |
| rendered review persistence/projection | `renderedReviewState`, setters | `ScorePanelRenderedReviewCoordinator` |
| story snapshot persistence | `refreshStoryAssessmentState` | `ScorePanelStoryAssessmentCoordinator` |
| intent feedback persistence | `handleSetIntentFeedback` | `ScorePanelIntentFeedbackCoordinator` |
| diagnostics/telemetry | `captureScoreDiagnostics`, `readBackendVersion`, telemetry in refresh | `ScorePanelDiagnosticsCoordinator` |
| analyzer workspace return persistence | `persistAnalyzerWorkspaceReturn` | existing store behind a small coordinator |

The first panel extractions should reuse existing workflow services and function factories. The plan must not create arbitrary “helper” files that hide ownership. The panel remains the VS Code adapter and coordinator, but it should no longer implement domain workflows or assemble the complete score projection itself.

## Adjacent-module disposition

`RecommendationEngineService.cs` is a large but coherent Discovery domain service. It does not sit on the shipped score-panel critical path and its model boundary tests intentionally prevent it from widening `ScoreResult`/`PageScore`. It is deferred; only architecture tests may mention its non-dependence on scoring.

`LocalPbirGenerationProviderService.cs` is future-provider infrastructure with direct scoring calls used for generated-artifact verification. It must not be pulled into scoring decomposition. Instead, an architecture test should prevent scoring from referencing Discovery/provider namespaces, while provider tests continue to prove the existing analyzer invocation. It is deferred until a separately authorized generation/provider program.

`scorePanel.ts` remains a broad cross-boundary contract until the generated schema migration is complete. Split by ownership only after contract inventory identifies a stable envelope/state/workflow subset; do not create parallel DTOs.

`scoreResultPayload.ts` is included in a later boundary cleanup: retain the normalizer as the single untrusted-input validator, extract pure presentation projection functions, and keep adapters for legacy PascalCase/backend payloads. No score calculation moves into it.

## Target architecture

```text
VS Code panel lifecycle
  -> validated score-panel router/protocol
  -> panel coordinators (analysis, state publication, audit, export, fix, diagnostics)
  -> AnalyzerBridgeService / Fabric advisory analyzer
  -> normalized ScoreResult payload
  -> webview host-message hook + workspace reducer
  -> feature projections/components

RpcHost composition root
  -> scoring orchestrator
      -> immutable report analysis context
      -> explicit ordered scoring stages
      -> bookmark-aware coordinator
      -> cross-page/story coordinators
      -> ScoreResultAssemblyService + compatibility adapter
  -> governance/tree/materialization/authoring routes remain separate
```

Ownership rules:

- Backend `ScoreResult` and page-score outputs remain authoritative.
- Normalized findings are the shared issue model; UI projections cannot invent scoring findings.
- Surface, analyzer, and profile remain distinct.
- Fabric App Review remains advisory and uses existing bounded screenshot evidence primitives.
- AI proposal enrichment remains advisory-only.
- Deterministic preview/apply/rollback remains the only mutation path.
- Shared repository snapshots remain analyzer-independent.
- Transport/contracts do not depend on implementation services.

## Architectural invariants and anti-regression controls

### Behavior preservation

Every extraction must compare the v1 corpus and targeted tests for total score, category scores, findings and IDs/codes, severity, evidence, readiness, diagnostics, fingerprint, page/finding/evidence ordering, null/default semantics, and mutation eligibility. Timestamps and machine paths remain normalized exactly as current characterization does.

Scoring-specific invariants:

- preserve report-before-page scoring order;
- preserve max parallelism and original page order restoration;
- preserve partial page failure semantics and error messages;
- preserve exact page-name matching and available-page exception text;
- preserve zero-visual results;
- preserve bookmark-state average overlay and legacy compatibility properties;
- preserve score rounding and configured weight normalization;
- preserve feedback/recommendation/evidence append order.

Panel/webview invariants:

- protocol versions/schema versions are rejected before state consumption;
- selected page index is clamped to current payload page count;
- `PbirScorePanel.handleMessage` keeps its `try/catch` and visible error path;
- webview DOM order, section defaults, labels, roles, and Show/Hide behavior remain stable;
- Fabric App review does not gain deterministic mutation authority;
- mutation preview/apply/rollback messages remain unchanged.

### Enforceable controls

1. Backend architecture tests inspect project references/source/imports and assert scoring cannot reference VS Code, RPC transport implementation, Discovery/provider execution, authoring execution, or Phase 35/runtime-provider namespaces. The composition root must explicitly list scoring stages and may not use reflection registration.
2. TypeScript import-boundary tests prevent webview feature modules from importing backend/VS Code implementation modules, prevent UI modules from importing mutation engines directly, and prevent panel lifecycle code from becoming the score-domain owner.
3. Contract freshness remains a generated-file check. `scorePanel.ts` and `scoreResultPayload.ts` may adapt generated data but may not create a parallel cross-language DTO authority.
4. PR checklist rules require justification, owner, characterization evidence, and rollback plan when adding domain behavior to `PbirScoringService`, `App.tsx`, or `PbirScorePanel`.
5. Complexity reports are advisory initially. Record current baselines, ratchet only after each wave, and fail new increases in orchestration roots after the legacy baseline is reduced. Do not fail legacy files solely for current size.
6. Composition tests require an explicit registration test for every new scoring stage and assert stage order is deterministic.

## Assembly decision

Module/namespace boundaries plus architecture tests are sufficient for the first decomposition. The current `PbirScoringService` is constructed in one core assembly and already has internal service seams; splitting assemblies now would create project-reference churn without improving runtime isolation. The existing composition root does not accidentally register experimental/provider services, and source-level architecture tests already protect that fact.

Revisit assemblies only after the dependency graph shows a stable, acyclic boundary and one of these benefits is measurable: forbidden references become impossible rather than test-detected, build/test isolation materially improves, or shipped runtime surface is reduced. If justified later, use staged `Domain`, `Scoring`, `Authoring`, `Transport`, and non-shipped `Experimental` assemblies. Do not create assemblies merely to demonstrate decomposition.

## Extraction sequence

1. Baseline and dependency maps; no production behavior.
2. Pure scoring helper extraction: accessibility color math, then feedback/evidence builder only after ordering tests.
3. Pure webview projections/formatters and a host-message hook; no DOM behavior change.
4. First scoring stage: accessibility, with context/result seam and golden comparison.
5. Visual composition/metadata and visual best-practice stage.
6. Report consistency/cross-page stage, preserving all-page ordering.
7. Story assessment wrapper and helper migration, preserving internal adapters.
8. Remaining framework stages and explicit scoring orchestrator reduction.
9. Webview feature extraction one section at a time, with reducer/state ownership.
10. Panel analysis/state-publication/diagnostics coordinators, while lifecycle/router remain in the panel.
11. Payload normalizer/projection cleanup and architecture ratchet.
12. Full corpus, deterministic, package, installed-host, and mutation acceptance.

## Validation cadence

Per extraction commit: targeted unit tests, changed backend/extension/webview tests, TypeScript compile or backend build as applicable, contract freshness when contract files are touched, and the relevant architecture test.

Per pull request: backend Release suite, extension Jest, webview Jest, lint, TypeScript compile, architecture tests, contract validation, representative characterization/goldens, and deterministic repeat.

Milestones after scoring, webview, and panel waves: `npm run build`, package verification, packaged backend/VSIX acceptance, installed-host smoke where available, and mutation preview/apply/rollback/export acceptance. Five-target packaging is a milestone gate, not a per-helper check.

## Completion criteria

### Scoring

- orchestration owns mode dispatch/context/stage order/parallel capture/assembly;
- independent scoring domains have typed inputs and targeted tests;
- dependency direction is architecture-tested;
- corpus outputs and deterministic fingerprints are unchanged;
- feedback/evidence construction has one owner per output path;
- no provider or authoring dependency is introduced.

### Webview

- feature ownership is visible in modules and tests;
- feature-specific state is local or reducer-owned with a documented reason;
- root `App` primarily composes features and transport hooks;
- scoring/readiness decisions remain outside rendering;
- DOM/protocol behavior is unchanged.

### Panel

- lifecycle/webview creation/disposal remains in `PbirScorePanel`;
- score, audit, export, fix, persistence, and diagnostics workflows have explicit owners;
- validated routing remains centralized;
- `handleMessage` error containment remains intact;
- mutation authority and public protocol contracts are unchanged.

## Highest-risk extractions

1. Full-report page-scoring loop: parallelism, page order, partial errors, and report-composite timing.
2. Bookmark-aware scoring: recursive stage invocation and result mutation through compatibility adapters.
3. Story helper migration: internal static consumers and validated promotion semantics.
4. Finding/evidence builder extraction: IDs, ordering, affected visual references, and null semantics.
5. `scoreResultPayload.ts` projection cleanup: required/optional defaults and legacy PascalCase normalization.
6. Panel `refresh`: PBIR versus Fabric App branch, advisory enrichment, persistence, diagnostics, and telemetry.
7. Webview state extraction: selected-page clamping, section defaults, persona/filter resets, and message timing.

## Exact implementation starting point

After plan approval, begin with one low-risk scoring extraction: move the pure accessibility color helpers `TryNormalizeHex`, `LooksLikeRedGreenPair`, `IsRedDominant`, `IsGreenDominant`, `SimulatesToSimilarUnderDeuteranopia`, `SimulateDeuteranopia`, and `HexToRgb` from `PbirScoringService` into `service-dotnet/Services/Pbir/AccessibilityColorMath.cs`. Keep the methods internal and preserve call order and formulas. Add direct unit coverage for normalization, red/green detection, and deuteranopia similarity, then run the existing scoring characterization and deterministic repeat. This changes no public contract, no stage ordering, no score formula, and no composition root.

## Decision summary

Proceed with namespaces/modules plus architecture tests, not immediate assembly splitting. Decompose by domain ownership and orchestration seams, not line count. Keep the generated contract, backend score authority, protocol validation, selected-page clamping, deterministic mutation boundary, and v1 corpus as non-negotiable gates. Defer adjacent Discovery/provider monoliths unless dependency evidence proves they are on the shipped scoring workflow.
