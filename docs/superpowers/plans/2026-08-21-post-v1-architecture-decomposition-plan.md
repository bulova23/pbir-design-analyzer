# PBIR Design Analyzer Post-v1.0 Architecture Decomposition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Incrementally separate scoring, score-workspace, and panel orchestration responsibilities while preserving v1.0 behavior, contracts, determinism, and mutation boundaries.

**Architecture:** Keep the current core assembly and introduce explicit namespace/module ownership first. Reduce `PbirScoringService` to orchestration over typed, ordered scoring stages; reduce `App.tsx` to workspace composition over feature projections and a small reducer; reduce `PbirScorePanel` to VS Code lifecycle, validated routing, and coordinator wiring. Existing generated contracts, protocol parsers, workflow services, score assembly, and mutation paths remain authoritative.

**Tech Stack:** .NET 8/C# with xUnit, TypeScript/React with Jest and Testing Library, generated JSON Schema score-panel contracts, VS Code webview protocol, repository characterization/golden scripts, packaged VSIX acceptance.

---

## Preconditions and execution rules

- Execute tasks on a fresh branch from `4c56eaf37f4829640051ec121d9f6f5103aa7084`.
- Preserve unrelated dirty worktree changes; do not reset or overwrite release evidence.
- Keep each task independently reviewable and revertible.
- Do not change score formulas, readiness policy, protocol versions, section labels/order, mutation commands, or release scope.
- Before every scoring extraction, run the narrowest existing scoring test and the characterization command. Before every UI extraction, run the relevant webview Jest test and protocol tests.
- Do not split `scorePanelProtocol.ts` or `scorePanelMessageRouter.ts` unless a later evidence-based task proves a coherent second boundary.

## File ownership map

| Area | Files to create or own | Existing authority retained |
|---|---|---|
| Scoring context/stages | `service-dotnet/Services/Pbir/Scoring/` | `PbirScoringService`, `ScoreResultAssemblyService`, `ScoreResult`/`PageScore` |
| Scoring pure helpers | `service-dotnet/Services/Pbir/AccessibilityColorMath.cs`, later focused files under `Scoring/` | Existing formulas and ordered feedback |
| Scoring tests | `service-dotnet/tests/Services/Scoring/`, characterization tests | Existing `PbirScoringServiceTests` and goldens |
| Workspace state/transport hooks | `vscode-extension/webview-src/analyzer-score/hooks/`, `state/` | `scorePanelProtocol.ts`, generated contract, router |
| Workspace features | `vscode-extension/webview-src/analyzer-score/features/` | Existing DOM contract in `App.test.tsx` |
| Presentation projections | `vscode-extension/webview-src/analyzer-score/presentation/` or existing `src/analyzer/score/` only where cross-consumer reuse is proven | Backend score and normalized findings |
| Panel coordinators | `vscode-extension/src/views/scorePanel/` | Existing audit/export/fix/state services |
| Boundary tests | `service-dotnet/tests/Architecture/`, `vscode-extension/src/test/architecture/` | Existing source-boundary and contract tests |
| Evidence | `docs/release-evidence/` | Historical v1 evidence remains unchanged |

## Wave 0 — Baseline and dependency maps

### Task 1: Capture decomposition baseline and dependency inventory

**Objective:** Freeze the starting line-count, responsibility, import/reference, test, and architecture-control baseline before changing production code.

**Files:**
- Create: `docs/architecture/post-v1-decomposition-baseline.md`
- Create: `scripts/report-decomposition-baseline.mjs`
- Test: `service-dotnet/tests/Architecture/DecompositionBaselineTests.cs` or an equivalent script-level assertion file

**Prerequisites:** None beyond the release commit.

- [ ] **Step 1: Record exact candidates and baselines.** Include the nine target files, line counts, declarations, imports, direct test references, public/internal methods, mutable fields, side effects, and classification from the design document. Use `wc -l`, `rg`, and a deterministic script output rather than hand-entered counts.
- [ ] **Step 2: Record runtime composition.** Trace `service-dotnet/RpcHost/Program.cs`, `AnalyzerRpcDispatcher.cs`, `vscode-extension/src/views/PbirScorePanel.ts`, `scorePanelMessageRouter.ts`, and `scorePanelProtocol.ts`. Mark shipped, advisory, mutation, provider, and deferred paths.
- [ ] **Step 3: Record control coverage.** Link architecture tests, contract freshness, protocol tests, selected-page clamping tests, characterization/golden scripts, deterministic repeat, package acceptance, and mutation/rollback acceptance.
- [ ] **Step 4: Add a baseline report command.** The command must fail if a named candidate disappears or if a new candidate is added without an explicit disposition entry. It must report size/complexity as advisory data only.
- [ ] **Step 5: Verify.** Run `node scripts/report-decomposition-baseline.mjs` and confirm stable output on two consecutive runs.

**Characterization requirement:** No production behavior changes; attach the existing v1 readiness evidence references and current golden/fingerprint identifiers.

**Architecture control:** Baseline inventory becomes the required reference for future ratchets and prevents silent scope expansion.

**Acceptance criteria:** Baseline is reproducible, includes all required modules and adjacent dispositions, and distinguishes cohesive modules from monoliths.

**Rollback strategy:** Delete only the new baseline script/report if the program is not approved; no runtime files are changed.

**Evidence artifact:** `docs/architecture/post-v1-decomposition-baseline.md` plus the command output attached to the implementation PR.

### Task 2: Add an explicit scoring-stage registration seam without moving scoring logic

**Objective:** Define the future orchestration seam and deterministic stage-order control while leaving the current implementation path intact.

**Files:**
- Create: `service-dotnet/Services/Pbir/Scoring/IScoringStage.cs`
- Create: `service-dotnet/Services/Pbir/Scoring/ScoringStageId.cs`
- Create: `service-dotnet/Services/Pbir/Scoring/ScoringStageRegistry.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs` only to expose an internal registration seam if required
- Test: `service-dotnet/tests/Architecture/ScoringStageRegistrationTests.cs`

**Prerequisites:** Task 1.

- [ ] **Step 1: Define the smallest typed seam.** Use an internal interface shaped like `Analyze(ReportAnalysisContext context, ScoringStageInput input) -> ScoringStageResult`; keep the result ordered and immutable/read-only. Do not pass `ScoreResult`, VS Code messages, or provider services into the stage interface.
- [ ] **Step 2: Define explicit ordering.** `ScoringStageRegistry` accepts a read-only ordered list and rejects duplicate IDs. It must not scan assemblies or use reflection.
- [ ] **Step 3: Add registry tests.** Verify stable order, duplicate rejection, explicit registration, and absence of provider/authoring dependencies.
- [ ] **Step 4: Run.** Use `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~ScoringStageRegistrationTests` and the existing architecture test.

**Characterization requirement:** Existing `PbirScoringService` remains the only production scorer; run the representative scoring characterization unchanged.

**Architecture control:** New scoring behavior requires explicit stage registration and cannot enter through reflection or provider discovery.

**Acceptance criteria:** The seam is internal, typed, deterministic, not used to change current output, and covered by tests.

**Rollback strategy:** Revert the new seam files; no existing scoring method is moved.

**Evidence artifact:** Stage registration test output and a dependency report showing no new references from scoring to Discovery/provider infrastructure.

### Task 3: Establish the scoring context as a read-only input snapshot

**Objective:** Make shared report inputs explicit before extracting any scoring domain.

**Files:**
- Create: `service-dotnet/Services/Pbir/Scoring/ReportAnalysisContext.cs`
- Create: `service-dotnet/Services/Pbir/Scoring/ReportAnalysisContextFactory.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Test: `service-dotnet/tests/Services/Scoring/ReportAnalysisContextTests.cs`

**Prerequisites:** Task 2.

- [ ] **Step 1: Model read-only context.** Include report location, report model/pages in source order, report filters, resolved theme colors, normalized framework weights, navigation settings, and report consistency input. Use `IReadOnlyList`/read-only dictionaries at the boundary; keep the existing mutable recommendation buffer outside the context until its ownership is extracted.
- [ ] **Step 2: Build context with existing services.** Move only the repeated loading/configuration statements from `ComputePageScore` and `ComputeReportScore`; preserve exact exception behavior and logging.
- [ ] **Step 3: Add context tests.** Verify source page order, config defaults, theme resolution delegation, exact page lookup behavior, and no mutation of the source page list.
- [ ] **Step 4: Re-run.** Run targeted scoring tests, `ScoringCharacterizationTests`, and the deterministic repeat command.

**Characterization requirement:** Compare total/category scores, feedback order, page order, scoring errors, and fingerprints before/after on every representative fixture.

**Architecture control:** Future stages receive data, not services or transport objects; context construction remains the sole input-normalization owner.

**Acceptance criteria:** Both scoring modes use the context without output differences, and the context has no provider, authoring, VS Code, or RPC dependency.

**Rollback strategy:** Restore the two original loading blocks and remove the context files.

**Evidence artifact:** Context unit-test output and golden comparison report.

## Wave 1 — Low-risk pure extractions

### Task 4: Extract accessibility color mathematics

**Objective:** Remove the first independent pure helper cluster from `PbirScoringService` without changing formulas or public contracts.

**Files:**
- Create: `service-dotnet/Services/Pbir/AccessibilityColorMath.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Test: `service-dotnet/tests/Services/AccessibilityColorMathTests.cs`
- Modify: `service-dotnet/tests/Services/PbirScoringServiceTests.cs` only if an existing reflection target must be redirected

**Prerequisites:** Task 3.

- [ ] **Step 1: Move these exact pure methods unchanged:** `TryNormalizeHex`, `LooksLikeRedGreenPair`, `IsRedDominant`, `IsGreenDominant`, `SimulatesToSimilarUnderDeuteranopia`, `SimulateDeuteranopia`, and `HexToRgb`.
- [ ] **Step 2: Update the three current call sites.** Accessibility scoring and visual metadata must call the new internal helper; semantic color guardrails must call the same helper.
- [ ] **Step 3: Add direct tests.** Cover valid/invalid hex normalization, case normalization, red/green dominance, deuteranopia similarity/difference, and malformed input behavior.
- [ ] **Step 4: Run.** `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~AccessibilityColorMathTests|FullyQualifiedName~PbirScoringServiceTests`; then run the characterization command twice.

**Characterization requirement:** Accessibility scores, visual metadata colors, semantic color findings, evidence, and fingerprints must match the baseline.

**Architecture control:** Pure color logic cannot acquire report-loading, logging, transport, provider, or mutation dependencies.

**Acceptance criteria:** No public API changes; direct tests pass; characterization and deterministic repeat are identical.

**Rollback strategy:** Revert the call-site changes and delete the helper/test file.

**Evidence artifact:** Targeted test output and normalized golden diff showing no changes.

### Task 5: Extract webview score-formatting and label projections

**Objective:** Move pure App formatting/label helpers out of `App.tsx` without changing rendered markup.

**Files:**
- Create: `vscode-extension/webview-src/analyzer-score/presentation/scoreLabels.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Test: `vscode-extension/webview-src/analyzer-score/presentation/scoreLabels.test.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx` only for import/test fixtures if necessary

**Prerequisites:** Task 1; no dependency on backend task completion.

- [ ] **Step 1: Move pure helpers only.** Start with `getScoreTone`, `formatPoints`, `getFeedbackCriterionLabel`, finding/severity/scope/impact labels, readiness band/effort labels, fix category/state/outcome labels, and matrix status labels. Keep JSX render functions in place.
- [ ] **Step 2: Preserve exact strings/classes.** Copy existing switch cases and fallback values byte-for-byte; do not normalize labels as part of extraction.
- [ ] **Step 3: Add focused table tests.** Cover every enum branch and fallback path represented in current App tests.
- [ ] **Step 4: Run.** `cd vscode-extension && npx jest webview-src/analyzer-score/presentation/scoreLabels.test.ts webview-src/analyzer-score/App.test.tsx --runInBand` and `npm run compile:webview`.

**Characterization requirement:** App DOM assertions, section labels, badges, fallback text, and accessibility labels remain unchanged.

**Architecture control:** Pure presentation utilities cannot import VS Code, backend services, mutation engines, or protocol implementation.

**Acceptance criteria:** `App.tsx` imports the helpers, has no duplicated label implementation, and all existing App assertions pass.

**Rollback strategy:** Restore local helper definitions and remove the new module/test.

**Evidence artifact:** Webview Jest output and DOM snapshot/role assertion results.

### Task 6: Extract score/workspace pure projections

**Objective:** Move pure overview, issue, and story projection logic out of App before moving JSX or state.

**Files:**
- Create: `vscode-extension/webview-src/analyzer-score/presentation/overviewProjection.ts`
- Create: `vscode-extension/webview-src/analyzer-score/presentation/issueProjection.ts`
- Create: `vscode-extension/webview-src/analyzer-score/presentation/storyProjection.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Test: corresponding `*.test.ts` files beside the projections

**Prerequisites:** Task 5.

- [ ] **Step 1: Move overview projections.** Extract `toOverviewInsight`, `toOverviewAction`, `buildPageOverviewCardContent`, readiness callout builders, and persona-default filter construction.
- [ ] **Step 2: Move issue projections.** Extract readiness-role mapping, dimension-to-impact mapping, visible finding filtering, issue filtering, grouping, and active-filter summaries.
- [ ] **Step 3: Move story projections.** Extract story signal normalization/deduplication, narrative construction, strong/missing signal selection, and review-status key construction.
- [ ] **Step 4: Add pure tests.** Use existing `App.test.tsx` fixtures to assert ordering, filter semantics, selected-page behavior, readiness hiding, and persona defaults.
- [ ] **Step 5: Run.** Targeted Jest, complete webview Jest, and TypeScript webview compile.

**Characterization requirement:** Every projection result must preserve finding order, affected-page order, readiness role behavior, and narrative fallback text.

**Architecture control:** Projection modules accept score contracts and return view data; they cannot import protocol transport or execute commands.

**Acceptance criteria:** App rendering calls projections, pure logic is independently tested, and DOM behavior is unchanged.

**Rollback strategy:** Restore functions to App while keeping tests as a reference; revert only the extraction commit.

**Evidence artifact:** Projection test results plus App Jest output.

## Wave 2 — Scoring domain decomposition

### Task 7: Extract the feedback/evidence builder behind ordered append semantics

**Objective:** Centralize repeated framework feedback/evidence construction without changing IDs, ordering, null behavior, or affected visual references.

**Files:**
- Create: `service-dotnet/Services/Pbir/Scoring/FrameworkFeedbackBuilder.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Test: `service-dotnet/tests/Services/FrameworkFeedbackBuilderTests.cs`
- Modify: `service-dotnet/tests/Characterization/ScoringCharacterizationTests.cs` only to improve normalized evidence assertions

**Prerequisites:** Tasks 3–4 and baseline characterization.

- [ ] **Step 1: Inventory call sites.** Cover both `BuildAffectedVisuals` overloads, `FeedbackItem`, `ScoredFeedback`, and `Clamp`; record every call-site order before editing.
- [ ] **Step 2: Define ordered APIs.** `AddFinding`, `AddScoredFinding`, and `BuildAffectedVisuals` must append to caller-owned lists and return no unordered set. Preserve existing `FindingTypes`, text, possible/earned points, and null behavior.
- [ ] **Step 3: Replace call sites by framework section.** Convert one framework section per commit, starting with a low-risk section such as Stephen Few/Tufte, and keep the remaining local calls valid during migration.
- [ ] **Step 4: Add tests.** Assert exact output objects, null handling, affected visual order, and score clamping.
- [ ] **Step 5: Run.** Targeted tests, full backend suite, characterization twice, and compare fingerprints/evidence order.

**Characterization requirement:** Finding IDs/codes, feedback order, severity/evidence, and recommendation order must be identical.

**Architecture control:** One builder owns framework feedback/evidence construction; UI/payload layers cannot synthesize backend findings.

**Acceptance criteria:** No duplicate builder semantics remain in the migrated sections, all goldens match, and no public model changes occur.

**Rollback strategy:** Revert one framework migration at a time; keep builder tests for the next attempt.

**Evidence artifact:** Call-site inventory, builder unit tests, and golden comparison.

### Task 8: Extract `AccessibilityScoreAnalyzer` with a typed stage result

**Objective:** Move accessibility scoring into an independently testable stage while preserving the current output and helper calls.

**Files:**
- Create: `service-dotnet/Services/Pbir/Scoring/AccessibilityScoreAnalyzer.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Modify: `service-dotnet/Services/Pbir/Scoring/ScoringStageRegistry.cs`
- Test: `service-dotnet/tests/Services/Scoring/AccessibilityScoreAnalyzerTests.cs`
- Modify: `service-dotnet/tests/Services/PbirScoringServiceTests.cs` for seam-level coverage

**Prerequisites:** Tasks 3, 4, and 7.

- [ ] **Step 1: Define stage input/output.** The analyzer receives theme colors, ordered pages, report context, and caller-owned recommendation/feedback accumulation through an explicit typed result; it does not receive `ScoreResult`.
- [ ] **Step 2: Move only accessibility orchestration and scoring methods.** Preserve constants, feedback text, recommendation append order, and calls to `AccessibilityColorMath`.
- [ ] **Step 3: Register explicitly.** Add the stage at the same position in current framework evaluation; do not reorder other frameworks.
- [ ] **Step 4: Add tests.** Cover empty pages, palette contrast, canvas contrast, colorblind palette, malformed colors, and exact feedback/evidence outputs.
- [ ] **Step 5: Run.** Targeted analyzer tests, existing scoring tests, full backend suite, characterization repeat, and deterministic fingerprint compare.

**Characterization requirement:** Accessibility category score, all feedback/evidence, recommendation order, composite, and fingerprint remain unchanged.

**Architecture control:** Stage has no transport/provider/authoring dependency; registration test proves explicit deterministic order.

**Acceptance criteria:** Accessibility behavior is independently testable and the orchestrator delegates it without changing `ScoreResult` or `PageScore`.

**Rollback strategy:** Remove registration and restore the original method call; keep the isolated tests for reference.

**Evidence artifact:** Analyzer test output and corpus diff report.

### Task 9: Extract visual composition/metadata and Visual Best Practices

**Objective:** Separate the visual analysis domain from report orchestration while keeping visual metadata and semantic color evidence consistent.

**Files:**
- Create: `service-dotnet/Services/Pbir/Scoring/VisualCompositionAnalyzer.cs`
- Create: `service-dotnet/Services/Pbir/Scoring/VisualMetadataAnalyzer.cs`
- Create: `service-dotnet/Services/Pbir/Scoring/VisualBestPracticesScoreAnalyzer.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Test: `service-dotnet/tests/Services/Scoring/VisualCompositionAnalyzerTests.cs`, `VisualMetadataAnalyzerTests.cs`, `VisualBestPracticesScoreAnalyzerTests.cs`

**Prerequisites:** Task 8 and successful characterization baseline.

- [ ] **Step 1: Extract composition.** Preserve canvas/grid constants, visual ordering, visible/hidden/navigation counts, and layout issue ordering.
- [ ] **Step 2: Extract metadata.** Preserve semantic descriptor inference, color normalization, chart intent, page story summary inputs, and metadata list ordering.
- [ ] **Step 3: Extract Visual Best Practices.** Pass explicit context and composition/metadata outputs; preserve all penalty/bonus formulas and recommendation appends.
- [ ] **Step 4: Add domain tests.** Use current scoring fixtures and direct `PageData` builders; assert exact metadata and feedback values.
- [ ] **Step 5: Run.** Targeted tests, full backend suite, corpus characterization twice, and package-independent deterministic repeat.

**Characterization requirement:** Visual metadata, semantic color findings, framework score, affected visuals, recommendation order, and fingerprint are unchanged.

**Architecture control:** Visual stages depend only on Pbir domain models/utilities and cannot import the panel, protocol, provider, or authoring layers.

**Acceptance criteria:** Visual concerns have explicit owners; `PbirScoringService` retains only orchestration calls for these concerns.

**Rollback strategy:** Revert composition, metadata, and VBP extraction as a single vertical slice if any golden changes are unexplained.

**Evidence artifact:** Visual stage test results and normalized metadata/fingerprint comparison.

### Task 10: Extract report consistency and cross-page analysis

**Objective:** Make report-level analysis independently testable without changing all-page ordering or cross-page promotion.

**Files:**
- Create: `service-dotnet/Services/Pbir/Scoring/ReportConsistencyAnalyzer.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Modify: `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeOrchestrator.cs` only for a typed input adapter if needed
- Test: `service-dotnet/tests/Services/Scoring/ReportConsistencyAnalyzerTests.cs`, existing cross-page tests

**Prerequisites:** Task 9.

- [ ] **Step 1: Capture the current consistency context contract.** Include source page order, dominant-pattern tie breaks, affected-page sorting, issue category ordering, semantic-color ordering, and report summary fields.
- [ ] **Step 2: Move consistency methods.** Do not move Story Assessment helper methods in this task.
- [ ] **Step 3: Preserve cross-page promotion.** Keep `CrossPageNarrativeOrchestrator.Build` after page scores are restored to source order; use an adapter if the orchestrator currently depends on concrete result types.
- [ ] **Step 4: Add tests.** Cover no pages, one page, stable ties, multi-page drift, affected-page order, and page-level consistency attachment.
- [ ] **Step 5: Run.** Targeted cross-page tests, full backend suite, characterization repeat, and deterministic comparison.

**Characterization requirement:** Report consistency summary, page notes, normalized findings, evidence, and fingerprints remain identical.

**Architecture control:** Cross-page stage depends on report domain inputs only and cannot call provider or mutation code.

**Acceptance criteria:** Cross-page logic has a clear owner and page scoring still restores original page order before promotion.

**Rollback strategy:** Restore the private methods and existing orchestrator call if any ordering difference cannot be explained by the baseline.

**Evidence artifact:** Cross-page test output and ordered normalized diff.

### Task 11: Extract Story Assessment as a compatibility-preserving stage

**Objective:** Remove historical Story Assessment accumulation from `PbirScoringService` without breaking internal helper consumers or promotion semantics.

**Files:**
- Create: `service-dotnet/Services/Pbir/Scoring/StoryAssessmentAnalyzer.cs`
- Create: `service-dotnet/Services/Pbir/Scoring/StoryAssessmentCompatibilityAdapter.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Modify: `service-dotnet/Services/Pbir/StoryAssessmentOrchestrator.cs`, `StorySignalRegistryService.cs`, `SpecialPageAssessmentService.cs` only to consume the adapter
- Test: existing Story Assessment tests plus `service-dotnet/tests/Services/Scoring/StoryAssessmentAnalyzerTests.cs`

**Prerequisites:** Task 10 and a written inventory of every `PbirScoringService` internal static method consumer.

- [ ] **Step 1: Inventory helper consumers.** Include `StoryAssessmentOrchestrator`, `StorySignalRegistryService`, `SpecialPageAssessmentService`, validation/export tools, and reflection-based tests. No consumer may be silently dropped.
- [ ] **Step 2: Introduce an adapter with identical internal method behavior.** Preserve sparse/special-page semantics, confidence dimensions, gap ordering, promotion state, and guided-improvement ordering.
- [ ] **Step 3: Move helper implementations by cohesive subdomain.** Move signal registry/topology first, then special-page/archetype, then semantic coherence/gaps/confidence, and finally guided improvements. Keep each migration compiling and characterized.
- [ ] **Step 4: Add stage-level tests.** Assert exact internal assessment objects and promoted public summaries for representative pages.
- [ ] **Step 5: Run.** Story tests, full backend suite, characterization twice, and deterministic repeat.

**Characterization requirement:** Story fields, internal-to-public promotion, finding IDs/evidence, confidence, guided-improvement order, and fingerprints are unchanged.

**Architecture control:** Story ownership is explicit; no new scoring stage may call private static helpers through reflection.

**Acceptance criteria:** Existing helper consumers use the compatibility adapter or new service directly; `PbirScoringService` no longer contains the historical Story Assessment implementation region.

**Rollback strategy:** Roll back one story subdomain at a time, retaining adapter tests and consumer inventory.

**Evidence artifact:** Consumer inventory, story stage tests, and golden comparison.

### Task 12: Reduce `PbirScoringService` to orchestration and preserve bookmark semantics

**Objective:** Finish scoring decomposition only after stage extractions are proven, keeping mode dispatch, page concurrency, bookmark overlays, partial errors, and result assembly in one explicit orchestrator.

**Files:**
- Create: `service-dotnet/Services/Pbir/Scoring/PbirReportScoringOrchestrator.cs`
- Create: `service-dotnet/Services/Pbir/Scoring/BookmarkAwareScoringCoordinator.cs`
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs` to delegate public API
- Modify: `service-dotnet/Services/Pbir/ScoreResultAssemblyService.cs` only if a typed input adapter is required
- Test: `service-dotnet/tests/Services/Scoring/PbirReportScoringOrchestratorTests.cs`, bookmark tests, characterization tests

**Prerequisites:** Tasks 7–11.

- [ ] **Step 1: Move report/page orchestration.** Preserve `ScoreAsync` validation, exact page-name behavior, zero-visual guards, report-level-before-page scoring, maximum parallelism four, partial error capture, and original-order restoration.
- [ ] **Step 2: Isolate bookmark coordination.** Reuse the ordered stage registry; preserve per-state score averaging, `PerStateScores`, legacy score population, and bookmark recommendation append.
- [ ] **Step 3: Keep assembly authoritative.** Continue to use `ScoreResultAssemblyService` and `ScoreCompatibilityAdapter`; do not introduce another composite calculator.
- [ ] **Step 4: Add orchestration tests.** Cover full report, single page, missing page, zero visuals, one failed page, order restoration, bookmark overlays, config weights, and result assembly.
- [ ] **Step 5: Run.** Full backend suite, architecture tests, characterization twice, deterministic repeat, and package-independent RPC score smoke.

**Characterization requirement:** All v1 score, finding, evidence, readiness-input, diagnostics, fingerprint, and ordering assertions must match.

**Architecture control:** `PbirScoringService` becomes a public compatibility facade/orchestrator; stage registration and dependency direction are enforced.

**Acceptance criteria:** The service has no framework implementation or Story Assessment helper region; the public constructor/API and `ScoreResult` output remain stable.

**Rollback strategy:** Keep the facade and restore the previous internal orchestrator if the full corpus differs; revert stage migrations independently where possible.

**Evidence artifact:** Orchestrator test output, architecture graph, corpus diff, and deterministic repeat evidence.

## Wave 3 — Score workspace decomposition

### Task 13: Add validated host-message and command hooks

**Objective:** Move protocol consumption/emission out of the root component while preserving envelope parsing, error behavior, and command payloads.

**Files:**
- Create: `vscode-extension/webview-src/analyzer-score/hooks/useScorePanelHostMessages.ts`
- Create: `vscode-extension/webview-src/analyzer-score/hooks/useScorePanelCommands.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Test: hook tests and existing `webview-src/analyzer-score/App.test.tsx`

**Prerequisites:** Tasks 5–6.

- [ ] **Step 1: Move message listener.** The hook must call `parseScorePanelHostMessage` before state consumption and preserve `loading`, `error`, `scoreState`, `auditState`, and `auditAnalyzing` behavior.
- [ ] **Step 2: Move command envelope.** The command hook must call `withScorePanelEnvelope` and expose typed callbacks only; it cannot import mutation engines.
- [ ] **Step 3: Add tests.** Reject protocol mismatch/malformed state, preserve selected-page clamping input, and assert exact posted messages.
- [ ] **Step 4: Run.** Hook tests, App test, protocol/router tests, all webview Jest, and TypeScript compile.

**Characterization requirement:** Message timing, envelope fields, selected page initialization, and DOM state after score/audit messages remain unchanged.

**Architecture control:** Webview transport is isolated from feature rendering and score decisions.

**Acceptance criteria:** `App.tsx` no longer directly owns `window.addEventListener` or envelope construction.

**Rollback strategy:** Restore the listener/command functions in App and remove hooks.

**Evidence artifact:** Protocol and App test output.

### Task 14: Introduce the workspace reducer and selected-page clamp invariant

**Objective:** Make cross-feature state ownership explicit without introducing a third-party state library.

**Files:**
- Create: `vscode-extension/webview-src/analyzer-score/state/workspaceState.ts`
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/src/views/scorePanelProtocol.ts` only if a shared typed action adapter is needed
- Test: `vscode-extension/webview-src/analyzer-score/state/workspaceState.test.ts`

**Prerequisites:** Task 13.

- [ ] **Step 1: Define state/actions.** Include view state, active page index, workspace persona, issue filters/grouping, section expansion, and review-status filter. Keep audit capture analysis and expanded opportunity IDs feature-local unless cross-feature evidence requires otherwise.
- [ ] **Step 2: Implement reducer transitions.** On score state, clamp index through `clampSelectedPageIndex`, reset persona/filter defaults exactly as current App does, and preserve Overview-expanded/other-sections-collapsed defaults.
- [ ] **Step 3: Add reducer tests.** Cover score replacement with fewer pages, page selection bounds, persona resets, filter clear/reset, expansion toggles, and malformed-state rejection upstream.
- [ ] **Step 4: Run.** Reducer tests, App test, protocol tests, all webview Jest, and compile.

**Characterization requirement:** Selected page, section expansion, filters, grouping, persona behavior, and rerender-visible DOM remain unchanged.

**Architecture control:** Selected-page clamping becomes a single reducer invariant; feature components cannot store competing page indexes.

**Acceptance criteria:** Root state is a small reducer plus feature-local state; no Redux/Zustand dependency is introduced.

**Rollback strategy:** Revert reducer wiring and retain reducer tests as executable behavioral documentation.

**Evidence artifact:** Reducer test output and App DOM assertions.

### Task 15: Extract workspace features one at a time

**Objective:** Move JSX and feature-specific state into durable feature modules while keeping the root as composition.

**Files:**
- Create/modify: `vscode-extension/webview-src/analyzer-score/features/OverviewWorkspace.tsx`
- Create/modify: `features/IssuesWorkspace.tsx`
- Create/modify: `features/FixPlanWorkspace.tsx`
- Create/modify: `features/ReviewSummaryWorkspace.tsx`
- Create/modify: `features/StoryAssessmentWorkspace.tsx`
- Create/modify: `features/RenderedReviewWorkspace.tsx`
- Create/modify: `features/EvidenceWorkspace.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Test: feature-level `*.test.tsx` files plus existing App test

**Prerequisites:** Tasks 6, 13, and 14.

- [ ] **Step 1: Extract Issues first.** Pass normalized findings, projections, filters, grouping, and typed reveal callbacks. Preserve all `aria-label`s, select values, grouping order, and evidence rows.
- [ ] **Step 2: Extract Overview.** Preserve persona selector, selected-page matrix navigation, readiness callouts, and return-to-report behavior.
- [ ] **Step 3: Extract Fix Plan.** Treat fix commands as callbacks; preserve advisory enrichment labels and preview/approve/apply/rollback command payloads.
- [ ] **Step 4: Extract Story Assessment and Review Summary.** Preserve reviewer-comment persona distinction, intent feedback commands, diff rendering, and review status filters.
- [ ] **Step 5: Extract Rendered Review and Evidence.** Preserve checklist status/note/screenshot callbacks, framework details, Fabric App advisory evidence, and audit coverage.
- [ ] **Step 6: Add feature tests.** Move the smallest relevant assertions from App into each feature and keep one root composition smoke test.
- [ ] **Step 7: Run after each feature.** Targeted feature test, App test, protocol/router tests, all webview Jest, compile, then lint for changed TypeScript.

**Characterization requirement:** Preserve DOM order, default expansion, section labels, roles, command envelopes, evidence ordering, and selected-page behavior.

**Architecture control:** Feature modules may depend on contracts/projections/UI primitives but not on backend services, VS Code APIs, or mutation engines.

**Acceptance criteria:** App is primarily composition and state wiring; every feature has an owner and focused tests.

**Rollback strategy:** Revert each feature extraction independently; do not combine all seven features into one commit.

**Evidence artifact:** Per-feature Jest output and root DOM smoke results.

## Wave 4 — Panel orchestration decomposition

### Task 16: Extract score result publication and analysis coordination

**Objective:** Remove score invocation and score-state publication logic from `PbirScorePanel` while preserving the PBIR/Fabric App branch and normalized payload behavior.

**Files:**
- Create: `vscode-extension/src/views/scorePanel/scorePanelAnalysisCoordinator.ts`
- Create: `vscode-extension/src/views/scorePanel/scorePanelStatePublisher.ts`
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Test: `vscode-extension/src/test/scorePanelAnalysisCoordinator.test.ts`, `scorePanelStatePublisher.test.ts`, existing panel scoring tests

**Prerequisites:** Wave 3 complete enough that payload/state behavior is independently tested.

- [ ] **Step 1: Define coordinator dependencies.** Inject surface discovery, config loading, bridge scoring, Fabric advisory analyzer, normalization, advisory enrichment, telemetry, and state service accessors. Do not import VS Code UI except through explicit notification/diagnostics adapters.
- [ ] **Step 2: Move `refresh` analysis branch.** Preserve unsupported/ambiguous surface handling, Fabric App advisory-only path, PBIR direct Analyze request, error messages, telemetry fields, and timing.
- [ ] **Step 3: Move `postScoreState` assembly.** Preserve navigation targets, fix workflow payload, rendered review state, story snapshot state, review packet preview, and protocol envelope.
- [ ] **Step 4: Add tests.** Cover PBIR success/failure, Fabric App advisory success/failure, no bridge, surface unsupported, payload normalization, page index clamping, and exact state fields.
- [ ] **Step 5: Run.** Targeted tests, full extension Jest, webview Jest, TypeScript compile, lint, and protocol tests.

**Characterization requirement:** Score state, readiness, findings, evidence, diagnostics, fingerprints, and mutation payloads remain unchanged.

**Architecture control:** Panel lifecycle delegates analysis; analysis coordinator cannot execute mutation or provider code.

**Acceptance criteria:** `PbirScorePanel.refresh` becomes a coordinator call with the same externally visible results and errors.

**Rollback strategy:** Restore `refresh`/`postScoreState` methods if any output differs; keep injected tests to diagnose the difference.

**Evidence artifact:** Coordinator tests and before/after score-state JSON comparison.

### Task 17: Extract intent, story snapshot, rendered review, and diagnostics coordinators

**Objective:** Give persistence and diagnostics workflows explicit owners while retaining lifecycle and validated routing in the panel.

**Files:**
- Create: `vscode-extension/src/views/scorePanel/scorePanelIntentFeedbackCoordinator.ts`
- Create: `vscode-extension/src/views/scorePanel/scorePanelStoryAssessmentCoordinator.ts`
- Create: `vscode-extension/src/views/scorePanel/scorePanelRenderedReviewCoordinator.ts`
- Create: `vscode-extension/src/views/scorePanel/scorePanelDiagnosticsCoordinator.ts`
- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Test: focused coordinator tests and existing intent/story/rendered-review/diagnostics tests

**Prerequisites:** Task 16.

- [ ] **Step 1: Move intent feedback persistence.** Preserve report session IDs, analyzer version, timestamps, store calls, and score-state republish.
- [ ] **Step 2: Move story snapshot persistence.** Preserve current/prior snapshot comparison, page diff map, timestamp semantics, and empty-page reset.
- [ ] **Step 3: Move rendered review state.** Preserve checklist merge rules, finding/page identity, mutation follow-up text, status/note persistence, and screenshot attachment routing.
- [ ] **Step 4: Move diagnostics.** Preserve score determinism diagnostic fields, backend ping behavior, output-channel text, and non-fatal backend-version failure.
- [ ] **Step 5: Run.** Focused tests, panel scoring/navigation tests, full extension Jest, compile, lint, and protocol/router tests.

**Characterization requirement:** Persistence keys, session identity, rendered-review checklist ordering, screenshot metadata, and diagnostics output remain unchanged.

**Architecture control:** Persistence and diagnostics are not implemented in the VS Code lifecycle class; coordinator dependencies are explicit and testable.

**Acceptance criteria:** Panel callback methods delegate to coordinators, and `PbirScorePanel` retains only lifecycle, routing, dependency wiring, and narrow coordination.

**Rollback strategy:** Revert coordinators independently; retain existing workflow services and stores.

**Evidence artifact:** Coordinator unit-test output and persisted-state comparison.

### Task 18: Clean the payload/projection boundary without creating parallel DTOs

**Objective:** Separate untrusted score normalization from presentation projections while preserving generated contract authority and legacy compatibility.

**Files:**
- Create: `vscode-extension/src/views/scorePayload/normalizeScoreResult.ts`
- Create: `vscode-extension/src/views/scorePayload/buildScoreProjections.ts`
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Modify: `vscode-extension/src/views/scorePanel.ts` only where generated types/adapters are mechanically split
- Test: `vscode-extension/src/test/scoreResultPayload.test.ts`, new projection tests, contract tests

**Prerequisites:** Tasks 13–17 and a current contract inventory.

- [ ] **Step 1: Keep the normalizer authoritative.** Move required-field validation, PascalCase/camelCase normalization, optional defaults, and surface/analyzer/profile identity detection into `normalizeScoreResult.ts`.
- [ ] **Step 2: Move pure projections.** Move normalized findings, readiness findings, overview summary, fix plan, cross-page matrix, and page-purpose projections into `buildScoreProjections.ts` only when each function has one input/output owner.
- [ ] **Step 3: Preserve adapters.** Keep legacy payload acceptance and generated schema compatibility; do not introduce hand-maintained C# or TypeScript DTOs.
- [ ] **Step 4: Add compatibility tests.** Verify valid payloads, additive unknown fields, missing required fields, null mutation fields, unsupported versions, PascalCase payloads, Fabric App advisory payloads, and normalized finding order.
- [ ] **Step 5: Run.** `npm run validate:contract`, targeted tests, full extension/webview Jest, compile, lint, and characterization/package comparison.

**Characterization requirement:** All score-state projections, readiness/finding evidence, mutation payloads, and protocol compatibility behavior remain unchanged.

**Architecture control:** Generated schema remains cross-language authority; projection code cannot calculate authoritative scores or mutate reports.

**Acceptance criteria:** `scoreResultPayload.ts` is a small boundary facade, no parallel DTO authority exists, and compatibility fixtures pass.

**Rollback strategy:** Restore the facade’s original implementations while preserving generated contract files and tests.

**Evidence artifact:** Contract validation output, compatibility matrix, and normalized payload diff.

## Wave 5 — Adjacent-module disposition and architecture ratchet

### Task 19: Add dependency/import boundary enforcement after decomposition

**Objective:** Prevent the new boundaries from regrowing into orchestration roots and prove the assembly decision with executable controls.

**Files:**
- Create/modify: `service-dotnet/tests/Architecture/ScoringDependencyBoundaryTests.cs`
- Create/modify: `vscode-extension/src/test/architecture/scoreWorkspaceDependencyBoundary.test.ts`
- Create: `scripts/report-decomposition-complexity.mjs`
- Modify: `docs/architecture/post-v1-decomposition-baseline.md`
- Modify: `CONTRIBUTING.md` and `.github/PULL_REQUEST_TEMPLATE.md`

**Prerequisites:** Tasks 12, 15, and 18.

- [ ] **Step 1: Backend guards.** Assert scoring namespace/source files do not reference VS Code, RPC host implementation, Discovery/provider execution, authoring execution, Phase 35/runtime-provider namespaces, or mutation execution. Assert `Program.cs` stage registration is explicit.
- [ ] **Step 2: TypeScript guards.** Assert feature modules do not import `vscode`, bridge services, fix engines, or panel implementation; assert App imports features/hooks but not implementation workflow services.
- [ ] **Step 3: Complexity report.** Report LOC/cyclomatic proxies for the three orchestration roots and all new modules. Ratchet only increases introduced after the new baseline; do not fail existing legacy size in the first pass.
- [ ] **Step 4: PR rules.** Add checklist entries requiring domain owner, characterization evidence, affected invariant list, and rollback plan for changes to orchestration roots/contracts/protocol.
- [ ] **Step 5: Verify deliberate failures.** In disposable changes, add one forbidden import/reference and confirm the architecture test fails; remove it and confirm the test passes.

**Characterization requirement:** Architecture-only changes must run the relevant characterization suite to prove no production behavior was changed.

**Architecture control:** These tests and PR rules become the anti-regression gate for future additions.

**Acceptance criteria:** Forbidden dependencies fail deterministically; complexity is advisory/ratcheted; assembly decision is documented as module/namespace-first.

**Rollback strategy:** Revert only the new control files/checklist entries; retain the decomposition changes.

**Evidence artifact:** Passing architecture tests, deliberate-failure logs, and updated baseline report.

### Task 20: Decide whether any adjacent module is justified for follow-up

**Objective:** Close the scope loop with evidence instead of automatically splitting `RecommendationEngineService` or `LocalPbirGenerationProviderService`.

**Files:**
- Create: `docs/architecture/post-v1-adjacent-module-disposition.md`
- Test: reuse architecture/dependency graph tests; no production code unless a separately approved follow-up is identified

**Prerequisites:** Task 19 and the completed runtime dependency graph.

- [ ] **Step 1: Measure reachability.** Trace shipped score workflow references to both adjacent modules and record whether each is on the v1 score-panel path.
- [ ] **Step 2: Classify.** Leave `RecommendationEngineService` as deferred cohesive Discovery domain unless score-path dependency evidence shows direct coupling; leave `LocalPbirGenerationProviderService` deferred future-provider infrastructure unless it violates scoring dependency boundaries.
- [ ] **Step 3: Record follow-up criteria.** A follow-up may be opened only if a module has mixed ownership, direct score-path coupling, duplicated contracts, or a proven testability problem.
- [ ] **Step 4: Run.** Dependency graph, architecture tests, and no-production-change characterization smoke.

**Characterization requirement:** No adjacent module behavior changes in this task.

**Architecture control:** Scope expansion requires evidence and separate approval.

**Acceptance criteria:** Each adjacent module has an explicit disposition, owner, evidence, and next trigger; no automatic file splitting occurs.

**Rollback strategy:** Remove the disposition report only; it does not alter runtime code.

**Evidence artifact:** `docs/architecture/post-v1-adjacent-module-disposition.md`.

## Wave 6 — Milestone validation and packaged acceptance

### Task 21: Run the post-scoring decomposition milestone gate

**Objective:** Prove scoring decomposition equivalence before proceeding with or accepting later UI/panel work.

**Files:**
- Create: `docs/release-evidence/post-v1-decomposition-scoring-milestone.md`
- Modify: characterization scripts only if they need an explicit decomposition run identifier

**Prerequisites:** Task 12 and relevant controls from Task 19.

- [ ] **Step 1: Run targeted scoring tests and full backend suite.** Use `dotnet test service-dotnet/tests/Tests.csproj -c Release`.
- [ ] **Step 2: Run characterization twice.** Compare normalized scores, findings, evidence, readiness inputs, diagnostics, page/error ordering, and fingerprints.
- [ ] **Step 3: Run architecture and contract checks.** Use the repository architecture tests and `cd vscode-extension && npm run validate:contract`.
- [ ] **Step 4: Record failures honestly.** An unexplained golden/fingerprint change blocks the milestone and triggers rollback to the last extraction task.

**Acceptance criteria:** Full backend suite and characterization repeat pass with no unexplained output difference.

**Rollback strategy:** Revert only the last scoring extraction that caused the mismatch.

**Evidence artifact:** Scoring milestone report with command output, fixture IDs, fingerprints, and approved diff status.

### Task 22: Run the full workspace/panel milestone gate

**Objective:** Prove webview and panel decomposition equivalence before packaging.

**Files:**
- Create: `docs/release-evidence/post-v1-decomposition-workspace-milestone.md`

**Prerequisites:** Tasks 15, 17, 18, and 19.

- [ ] **Step 1: Run extension and webview tests.** From `vscode-extension`, use `npm test`, `npm run lint`, and the TypeScript compile/build commands used by the release pipeline.
- [ ] **Step 2: Run protocol/contract tests.** Include score panel protocol, router, payload, selected-page clamping, generated-contract freshness, and App feature tests.
- [ ] **Step 3: Run panel workflow tests.** Include scoring boundary, navigation, audit, export, fix workflow, rendered review, and diagnostics tests.
- [ ] **Step 4: Record DOM/contract evidence.** Confirm section order/defaults, exact labels/roles, command envelopes, and state field compatibility.

**Acceptance criteria:** Extension/webview suites, lint, compile, contract checks, and panel workflows pass with behavior-equivalent evidence.

**Rollback strategy:** Revert the last feature/coordinator extraction with the failing DOM or protocol diff.

**Evidence artifact:** Workspace milestone report and test output.

### Task 23: Run packaged acceptance and close the decomposition program

**Objective:** Prove that the decomposed source still produces the v1.0-quality packaged workflow.

**Files:**
- Create: `docs/release-evidence/post-v1-decomposition-final-acceptance.md`
- Modify: no production files unless a separately approved acceptance defect is found

**Prerequisites:** Tasks 21–22; all architecture controls pass.

- [ ] **Step 1: Run the repository build/package path.** Use `cd vscode-extension && npm run build`, package verification, and the existing packaged backend/VSIX acceptance scripts.
- [ ] **Step 2: Run representative packaged score.** Compare normalized fingerprint/result to the decomposition baseline.
- [ ] **Step 3: Run mutation safety acceptance.** On disposable copies, prove preview, apply, rollback, and re-score behavior remains unchanged; do not add mutation authority.
- [ ] **Step 4: Run export and advisory checks.** Prove export downstream behavior and Fabric App Review advisory evidence remain separate from deterministic PBIR scoring/mutation.
- [ ] **Step 5: Record completion criteria.** Include orchestrator ownership, stage testability, feature ownership, panel coordinator ownership, architecture controls, assembly decision, adjacent dispositions, and all validation outputs.

**Acceptance criteria:** Package/build/VSIX acceptance, deterministic repeat, mutation/rollback, export, contract, architecture, backend, extension, and webview gates pass; no deferred feature entered scope.

**Rollback strategy:** Keep the last accepted decomposition milestone as the rollback point; do not republish or alter v1.0 artifacts.

**Evidence artifact:** Final decomposition acceptance report linked from the next post-v1 architecture release notes/PR.

## Completion definition

The program is complete only when Tasks 1–23 are reviewed and the following are objectively true:

- `PbirScoringService` is a compatibility facade/orchestrator with explicit typed stage ownership and no provider/authoring/UI dependency.
- Independent scoring domains and shared feedback/evidence construction have focused tests; score formulas and `ScoreResult`/`PageScore` remain authoritative.
- Full-report ordering, partial failures, bookmark overlays, story promotion, readiness inputs, findings, evidence, diagnostics, and fingerprints match the v1 corpus.
- `App.tsx` composes feature modules and hooks; feature state is owned by a reducer/local hook with one selected-page clamp invariant.
- `PbirScorePanel` owns lifecycle and validated routing while coordinators own analysis, publication, persistence, rendered review, and diagnostics.
- Generated contracts remain the only cross-language authority; protocol versions and mutation boundaries are unchanged.
- Architecture/import/composition tests and PR ownership controls prevent regrowth; complexity reports are ratcheted only against the new baseline.
- `RecommendationEngineService.cs` and `LocalPbirGenerationProviderService.cs` are either explicitly deferred with evidence or separately approved for a later program.
- Module/namespace boundaries remain sufficient unless a later dependency graph proves assembly isolation materially improves enforcement, build/test isolation, or shipped surface.
