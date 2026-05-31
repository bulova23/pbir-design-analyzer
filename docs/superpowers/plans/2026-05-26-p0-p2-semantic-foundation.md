# PBIR Semantic Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first semantic-analysis tranche for PBIR Design Analyzer by shipping semantic color consistency, chart intent/chart-fit analysis, and cross-page consistency reporting.

**Architecture:** Extend the backend scoring pipeline first so page/report scoring emits richer semantic metadata and deterministic findings. Then update the RPC/webview contract and score panel to surface those semantics in the existing report UI. Keep the first tranche deterministic and metadata-driven; do not mix in AI review or screenshot-dependent logic yet.

**Tech Stack:** .NET 8, xUnit, TypeScript, React, Jest, VS Code webview contract models

---

## File Structure

### Backend scoring and models

- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
  - Add semantic color extraction helpers, chart intent inference helpers, and report-level consistency aggregation.
- Modify: `service-dotnet/Services/Pbir/Models/VisualMetadataSummary.cs`
  - Add fields for semantic color mapping, detected chart intent, and chart-fit annotations.
- Modify: `service-dotnet/Services/Pbir/Models/PageScore.cs`
  - Add page-level consistency and semantic summary payloads when needed.
- Modify: `service-dotnet/Services/Pbir/Models/ScoreResult.cs`
  - Add report-level semantic consistency summary for full-report scoring.

### Backend tests

- Modify: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`
  - Add focused tests for semantic color drift, chart intent inference, chart-fit warnings, and cross-page consistency.
- Modify: `service-dotnet/tests/RpcHostJsonRpcTests.cs`
  - Verify new score payload fields serialize correctly.

### Extension contract and payload normalization

- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - Add new semantic metadata and report summary types.
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
  - Normalize new backend fields into the webview contract.
- Modify: `vscode-extension/src/test/scoreResultPayload.test.ts`
  - Add payload normalization coverage for new semantic fields.

### Score panel UI

- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
  - Surface semantic color findings, chart intent tags, chart-fit rationale, and cross-page consistency sections.
- Modify: `vscode-extension/webview-src/analyzer-score/App.test.tsx`
  - Verify the new sections render from representative score payloads.
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
  - Add minimal styling for semantic badges, color swatches, and consistency sections.

---

### Task 1: Add semantic-analysis model shapes

**Files:**
- Modify: `service-dotnet/Services/Pbir/Models/VisualMetadataSummary.cs`
- Modify: `service-dotnet/Services/Pbir/Models/PageScore.cs`
- Modify: `service-dotnet/Services/Pbir/Models/ScoreResult.cs`
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Test: `service-dotnet/tests/RpcHostJsonRpcTests.cs`
- Test: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] **Step 1: Add backend DTOs for semantic color, chart intent, and report consistency**

Add small focused model types near the existing score/metadata models:

- `SemanticColorAssignment`
  - `semanticKey`
  - `displayLabel`
  - `color`
  - `sourceVisualId`
  - `sourcePageName`
- `ChartIntentSummary`
  - `intent`
  - `confidence`
  - `evidence`
  - `fitStatus`
  - `recommendedAlternatives`
- `ReportConsistencySummary`
  - `consistentTitleAnchors`
  - `consistentFilterBand`
  - `consistentMetricLabels`
  - `consistentSemanticColors`
  - `findings`

- [ ] **Step 2: Extend page/visual/result models to carry the new shapes**

Add these properties:

- `VisualMetadataItem.semanticColors`
- `VisualMetadataItem.chartIntent`
- `PageVisualMetadataSummary.semanticColorMap`
- `PageVisualMetadataSummary.chartIntentSummary`
- `PageScore.reportConsistencyNotes`
- `ScoreResult.reportConsistencySummary`

Keep additions nullable or default-empty so existing payload consumers remain valid.

- [ ] **Step 3: Run targeted backend serialization coverage**

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter RpcHostJsonRpcTests
```

Expected:
- PASS
- JSON remains camelCase
- new semantic fields serialize when populated

- [ ] **Step 4: Update TypeScript contract definitions**

Mirror the new model shapes in `vscode-extension/src/analyzer/contracts/scorePanel.ts` and keep naming in camelCase only.

- [ ] **Step 5: Add payload normalization coverage**

Extend `vscode-extension/src/test/scoreResultPayload.test.ts` with a payload sample that includes:

- `SemanticColorMap`
- `ChartIntentSummary`
- `ReportConsistencySummary`

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension
npm test -- --runInBand src/test/scoreResultPayload.test.ts
```

Expected:
- PASS
- normalized payload preserves new semantic fields

---

### Task 2: Implement page-level semantic color extraction

**Files:**
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Modify: `service-dotnet/Services/Pbir/Models/VisualMetadataSummary.cs`
- Test: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`

- [ ] **Step 1: Add helper methods for semantic color normalization**

Add focused helpers near existing color utilities:

- `NormalizeSemanticKey(string raw)`
- `ExtractSemanticColorAssignments(PageData page)`
- `TryInferSemanticKey(VisualData visual, string roleHint)`

Semantic keys should start from deterministic cases only:

- status/severity labels such as `good`, `bad`, `on track`, `at risk`
- repeated dimension values surfaced through category/series hints such as `region`, `segment`, `product category`

- [ ] **Step 2: Attach color assignments to visual metadata**

Update `BuildPageVisualMetadataSummary` so each visual can expose:

- the relevant semantic assignments detected from legend/category/series/title hints
- page-level rolled-up `semanticColorMap`

Do not infer colors when the visual exposes only background/font color and no category/series semantics.

- [ ] **Step 3: Add deterministic tests for page-level color capture**

Add tests covering:

- same semantic key, same color across two visuals => no issue
- same semantic key, different colors across two visuals => captured for later warning
- red/green conflicting status usage on a single page => captured

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirScoringServiceTests"
```

Expected:
- PASS
- new tests demonstrate deterministic extraction, not yet full scoring

---

### Task 3: Add semantic color consistency scoring

**Files:**
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Test: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`

- [ ] **Step 1: Add semantic color scoring helpers**

Implement focused methods:

- `AddSemanticColorConsistencyFeedback(...)`
- `BuildSemanticColorConflicts(...)`
- `BuildStatusColorConflicts(...)`

Feed findings into:

- `visualBestPractices`
- `accessibility` only when the issue is severity/status ambiguity

- [ ] **Step 2: Add recommendations for semantic drift**

Generate recommendations like:

- `[High] Semantic Color: Keep the same category or status meaning on the same color across visuals and pages.`
- `[Medium] Semantic Color: Reserve red/green for consistent bad/good status semantics only.`

- [ ] **Step 3: Add score impact rules**

Use deterministic, bounded penalties:

- page-local semantic drift => medium penalty
- contradictory status color semantics => higher penalty
- repeated consistent mappings => positive feedback item, not bonus points beyond subscore ceiling

- [ ] **Step 4: Validate the scoring behavior**

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirScoringServiceTests"
```

Expected:
- PASS
- feedback text includes semantic color language
- affected visuals are populated for failures

---

### Task 4: Implement chart intent inference metadata

**Files:**
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Modify: `service-dotnet/Services/Pbir/Models/VisualMetadataSummary.cs`
- Test: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`

- [ ] **Step 1: Add deterministic chart intent classifier**

Implement helpers:

- `InferChartIntent(VisualData visual)`
- `InferChartIntentEvidence(VisualData visual)`
- `ClassifyAnalyticalTask(string visualType, IReadOnlyList<string> categoryHints, IReadOnlyList<string> seriesHints, IReadOnlyList<string> measureHints, string? title)`

Allowed first-release intent classes:

- `comparison`
- `trend`
- `composition`
- `relationship`
- `distribution`
- `table-reference`

- [ ] **Step 2: Attach chart intent to visual metadata**

Update `BuildPageVisualMetadataSummary` to populate `chartIntent` on each data visual and a rolled-up `chartIntentSummary` for the page.

- [ ] **Step 3: Add deterministic inference tests**

Cover at least:

- line chart + month/category => `trend`
- clustered bar by region => `comparison`
- scatter with two measures => `relationship`
- pie/donut => `composition`
- matrix/table => `table-reference`

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirScoringServiceTests"
```

Expected:
- PASS
- chart intent metadata is present on page summaries

---

### Task 5: Add chart-fit warnings and recommendations

**Files:**
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Test: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`

- [ ] **Step 1: Add chart-fit evaluator on top of inferred intent**

Implement helpers:

- `EvaluateChartFit(...)`
- `SuggestAlternativeCharts(...)`

First-release misfit rules:

- line chart without temporal field => warn
- pie/donut used with too many categories => warn
- relationship intent without scatter-style encoding => warn
- composition intent shown with weak comparison-heavy chart and no total context => warn

- [ ] **Step 2: Route findings into the graphical perception and visual best practices frameworks**

Use:

- `graphicalPerception` for encoding-fit problems
- `visualBestPractices` for recommendation-style chart family suggestions

- [ ] **Step 3: Add tests for misfit detection**

Cover at least:

- categorical line misuse
- donut with many categories
- trend title but non-temporal axis

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirScoringServiceTests"
```

Expected:
- PASS
- recommendations name alternative chart families when the fit is weak

---

### Task 6: Implement report-level cross-page consistency aggregation

**Files:**
- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Modify: `service-dotnet/Services/Pbir/Models/ScoreResult.cs`
- Modify: `service-dotnet/Services/Pbir/Models/PageScore.cs`
- Test: `service-dotnet/tests/Services/PbirScoringServiceTests.cs`

- [ ] **Step 1: Add report-level consistency summary builder**

Implement helpers:

- `BuildReportConsistencySummary(IReadOnlyList<PageData> pages, IReadOnlyList<PageScore> pageScores)`
- `EvaluateTitleAnchorConsistency(...)`
- `EvaluateFilterBandConsistency(...)`
- `EvaluateMetricLabelConsistency(...)`
- `EvaluateSemanticColorConsistencyAcrossPages(...)`

- [ ] **Step 2: Populate result-level and page-level consistency summaries**

For full-report scoring:

- set `ScoreResult.reportConsistencySummary`
- add page-local notes when a page is the outlier against the report convention

- [ ] **Step 3: Add deterministic tests for cross-page consistency**

Cover at least:

- two pages with aligned title bands and consistent filter placement => pass
- one page with title/filter convention drift => fail
- repeated metric labels with inconsistent modifier ordering => fail
- repeated category colors drifting across pages => fail

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~PbirScoringServiceTests"
```

Expected:
- PASS
- report scoring exposes consistency findings without breaking single-page scoring

---

### Task 7: Normalize new backend fields into the extension payload

**Files:**
- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Test: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] **Step 1: Extend payload normalization helpers**

Add normalizers for:

- `semanticColorAssignments`
- `chartIntentSummary`
- `reportConsistencySummary`

Keep the current PascalCase/camelCase dual-read pattern.

- [ ] **Step 2: Add focused normalization assertions**

Extend the existing payload unit test to assert:

- page-level semantic color maps normalize
- visual-level chart intent fields normalize
- report-level consistency summary normalizes

- [ ] **Step 3: Run targeted extension tests**

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension
npm test -- --runInBand src/test/scoreResultPayload.test.ts
```

Expected:
- PASS
- no regressions in existing payload fields

---

### Task 8: Surface semantic analysis in the score panel UI

**Files:**
- Modify: `vscode-extension/webview-src/analyzer-score/App.tsx`
- Modify: `vscode-extension/webview-src/analyzer-score/styles.css`
- Test: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] **Step 1: Add semantic color and chart intent presentation blocks**

In the existing page detail area, add:

- semantic color map section with simple swatches and labels
- chart intent badges on visual metadata cards
- chart-fit notes when a visual has a warning

- [ ] **Step 2: Add report-level consistency section**

When `reportConsistencySummary` is present, render a report-wide section above page tabs that shows:

- consistency status chips
- concise findings list
- affected pages where applicable

- [ ] **Step 3: Keep the first UI slice additive**

Do not redesign the panel. Reuse existing card/list/badge patterns and minimal CSS only.

- [ ] **Step 4: Add webview rendering tests**

Cover:

- semantic color map display
- chart intent badge rendering
- report consistency section rendering

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension
npm test -- --runInBand webview-src/analyzer-score/App.test.tsx
```

Expected:
- PASS
- no snapshot-free rendering regressions for the current panel

---

### Task 9: Full regression pass for the P0-P2 tranche

**Files:**
- Modify as needed based on failures found in prior tasks
- Test: `service-dotnet/tests/Tests.csproj`
- Test: `vscode-extension/src/test/scoreResultPayload.test.ts`
- Test: `vscode-extension/webview-src/analyzer-score/App.test.tsx`

- [ ] **Step 1: Run backend regression suite**

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

Expected:
- PASS

- [ ] **Step 2: Run targeted extension tests**

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension
npm test -- --runInBand src/test/scoreResultPayload.test.ts webview-src/analyzer-score/App.test.tsx
```

Expected:
- PASS

- [ ] **Step 3: Run extension lint if any TypeScript changed**

Run:

```bash
cd /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/feat-semantic-color-chart-intent/vscode-extension
npm run lint
```

Expected:
- PASS

---

## Scope Guardrails

Included in this plan:

- deterministic semantic color consistency
- deterministic chart intent inference
- deterministic chart-fit warnings
- deterministic cross-page consistency summary
- score panel exposure of the new semantics

Explicitly out of scope for this plan:

- inferred page story and user confirmation workflow
- screenshot-grounded semantic validation
- AI-assisted narrative critique
- bookmark-state semantic diffs beyond existing bookmark-aware scoring
- archetype benchmarking

## Self-Review

- Spec coverage:
  - `P0` semantic color consistency is covered by Tasks 1-3 and 8.
  - `P1` chart intent and chart-fit is covered by Tasks 4-5 and 8.
  - `P2` cross-page consistency is covered by Tasks 6-8.
- Placeholder scan:
  - no `TODO`, `TBD`, or cross-references that require hidden context.
- Type consistency:
  - the plan consistently uses `semanticColorMap`, `chartIntentSummary`, and `reportConsistencySummary` across backend, normalization, and UI tasks.
