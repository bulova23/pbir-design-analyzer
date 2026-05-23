  PBIR Design Analyzer — Analysis Report
  
  Extension: PBIR Design Analyzer v0.1.13 (unreleased)
  Branch: Rich-Visual-Metadata-Extraction
  Date: May 23, 2026
  Scope: Architecture, feature coverage, gap assessment, and enhancement recommendations

  ---
  1. Executive Summary
  
  The PBIR Design Analyzer is a well-architected VS Code extension for local Power BI PBIP/PBIR report analysis. The
   core scoring engine is sophisticated and production-ready, the TypeScript/React frontend is cleanly structured,
  and the .NET backend integration via JSON-RPC is solid. The 0.1.13 release represents a significant capability
  expansion from the original five-framework baseline to an eleven-framework model with navigation scoring, visual
  metadata extraction, finding classification, and several new heuristic categories.

  However, several important gaps exist between the spec, the backlog, and the current implementation. The most
  consequential are a non-functional placeholder in bookmark state scoring, a stub governance dynamic-rules
  evaluator that does nothing, a hardcoded canvas assumption that breaks scoring accuracy on non-standard canvas
  sizes, and accessibility scoring that only checks theme palette colors rather than the actual on-canvas reading
  experience. The planned Visual Audit Mode is entirely unimplemented and represents the largest forward-looking
  investment opportunity.

  ---
  2. Architecture Analysis

  2.1 Strengths

  LSP/JSON-RPC Bridge Pattern
  The TypeScript ↔ .NET communication layer is clean and well-isolated. LSPModelService handles request/response
  queuing, timeout management, and state transitions. The graceful local fallback when the backend is unavailable is
   a well-implemented resilience pattern.

  Scoring Engine Design
  PbirScoringService.cs at ~5,000 lines is large but logically structured. Each framework is a self-contained method
   returning (double score, List<FrameworkFeedbackItem> feedback). The composition pattern makes it easy to add new
  frameworks without touching existing ones. The VisualComposition abstraction for navigation/data/hidden visual
  classification is solid.

  React Webview Architecture
  The panel/webview separation with typed message contracts (scorePanel.ts, configPanel.ts) provides clear
  host-to-webview and webview-to-host communication boundaries. Vite builds with manifest-based asset resolution and
   dev server support are correct patterns for VS Code webview work.

  Config Store Design
  The migration, normalization, and validation pipeline in store.ts is thorough. Framework ID aliasing (cogload →
  cognitive, wcag → accessibility) prevents silent breakage when config keys drift.

  Type Safety
  Strict TypeScript mode throughout. Defensive normalization in scoreResultPayload.ts with camelCase/PascalCase
  property fallback protects against payload schema drift between backend versions.

  2.2 Architecture Concerns

  LSP Terminology Misnomer
  The codebase uses LSP (Language Server Protocol) terminology throughout — LspHost, LSPModelService,
  vscode-languageclient — but the actual protocol is JSON-RPC over stdio, not the LSP message format. LSP is a
  higher-level protocol on top of JSON-RPC with specific message shapes (initialize, textDocument/didOpen, etc.)
  that are not used here. The spec notes this as deferred, but it creates misleading naming for contributors and
  will cause friction if a real LSP feature is ever added.

  Backend Subprocess Management
  The .NET backend is spawned as a subprocess with custom environment variable injection (DOTNET_ROOT, PATH). This
  works but creates platform-specific complexity that is not covered by tests. Silent failure if DOTNET_ROOT is
  wrong produces a confusing fallback-to-local-mode experience.

  Singleton Overuse
  PbirScorePanel, PbirConfigPanel, and LSPModelService are all singletons. This is appropriate for VS Code panels
  but creates hidden coupling that makes unit testing harder and could cause state leakage between test runs.

  Canvas Constants Hardcoded
  PbirScoringService.cs lines 24–29 hardcode canvas dimensions at 1280×720 and derive a fixed 12×12 grid. While the
  code has a GetColumnWidth() helper that reads per-page dimensions, the constants are used as the baseline and only
   optionally overridden. Actual Power BI report canvases are commonly 1280×720, 1366×768, or custom sizes. Reports
  with non-standard canvases will produce systematically wrong grid alignment scores.

  ---
  3. Feature Coverage Against V1 Spec

  ┌────────────────────────────────┬───────────────┬───────────────────────────────────────────────────────────┐
  │        Spec Requirement        │    Status     │                           Notes                           │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Open PBIP project              │ ✅ Complete   │ Auto-detection + file picker                              │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Refresh reports                │ ✅ Complete   │ Tree provider                                             │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ PBIR tree view                 │ ✅ Complete   │ LSP + local fallback                                      │
  │ (report/page/visual)           │               │                                                           │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Full-report scoring            │ ✅ Complete   │ 11 frameworks                                             │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Single-page scoring            │ ✅ Complete   │ Param-dispatched                                          │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Navigation scoring             │ ✅ Complete   │ Configurable weight, framework interactions               │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Analyzer configuration panel   │ ✅ Complete   │ React, weight validation                                  │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Score persistence              │ ✅ Complete   │ Migration + validation                                    │
  │ (globalState)                  │               │                                                           │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Governance check command       │ ✅ Partial    │ Composite score + approved themes work; dynamic rules are │
  │                                │               │  stub                                                     │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Finding classification badges  │ ✅ Complete   │ Objective / StrongHeuristic / StylePreference             │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Visual metadata extraction     │ ✅ Partial    │ Current branch adds rich metadata; depth varies by visual │
  │                                │               │  type                                                     │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Per-page score breakdown       │ ✅ Complete   │ Page tabs in score panel                                  │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Bookmark/custom visual         │ ✅ Complete   │ Crashes prevented                                         │
  │ tolerance                      │               │                                                           │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Bookmark state scoring         │ ⚠️             │ Uses full-page score for all states (line 297)            │
  │                                │ Placeholder   │                                                           │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Zero-visual page handling      │ ✅ Complete   │ Returns 0 with explanation                                │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Partial failure handling       │ ✅ Complete   │ ScoringErrors dictionary                                  │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Quick fix surface              │ ⚠️  Minimal    │ Only 3 types: SnapToGrid, ReduceColorVariance,            │
  │                                │               │ RemoveDecorativeElements                                  │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Audience presets               │ ❌ Not        │ Documented in Reddit backlog                              │
  │                                │ started       │                                                           │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Visual Audit Mode              │ ❌ Not        │ Full plan written, zero implementation                    │
  │                                │ started       │                                                           │
  ├────────────────────────────────┼───────────────┼───────────────────────────────────────────────────────────┤
  │ Export / share results         │ ❌ Not        │ No export capability                                      │
  │                                │ started       │                                                           │
  └────────────────────────────────┴───────────────┴───────────────────────────────────────────────────────────┘

  ---
  4. Gap Assessment

  4.1 Critical Gaps

  GAP-01: Bookmark State Scoring is a Non-Functional Placeholder
  PbirScoringService.cs lines 293–300:
  // For now, use the full page score for all states as a placeholder
  // In production, this would filter visuals and recompute scores
  perStateScores[state.StateName] = result.CompositeScore;
  Bookmark-heavy reports (the primary integration fixture has bookmark references) show per-state scores that are
  identical to the full-page score regardless of which visuals are visible in each state. This means the
  bookmark-state complexity analysis cited in the spec is not producing meaningful data. The LayoutStateGenerator
  and BookmarkStateAnalyzer infrastructure exists but is not wired to actually filter visuals and recompute.

  GAP-02: Dynamic Governance Rules Evaluation is a Stub
  PbirGovernanceService.cs lines 313–323:
  private void EvaluateDynamicRules(GovernancePolicy policy, List<string> reasons)
  {
      if (policy.DynamicRules.Count == 0) return;
      // Placeholder for dynamic rule evaluation.
      _logger.LogDebug("[Governance] Evaluating {RuleCount} dynamic rules.", policy.DynamicRules.Count);
  }   
  All 10 governance defaults in governance-defaults.json (maxVisualsPerPage, maxBookmarksPerPage, allowPieCharts,
  requirePageTitle, etc.) cannot be enforced through the governance check command because EvaluateDynamicRules is a
  no-op. Only composite score threshold and approved theme list work. Organizations expecting governance enforcement
   on page visual counts, pie chart prohibitions, or page title requirements will not see those rules fire.
   
  GAP-03: Accessibility Scoring is Severely Shallow
  The accessibility implementation (ComputeAccessibilityScore) only checks theme palette hex colors against WCAG 2.1
   AA contrast ratio against a white background. It does not:
  - Check text rendered on non-white backgrounds (colored cards, dark-themed reports)
  - Evaluate font sizes for readability
  - Check data label contrast on chart fill areas
  - Account for colorblind users (deuteranopia, protanopia)
  - Detect alt-text or title attributes on visuals
  - Check keyboard navigation considerations
  
  A report using a dark theme where all colors pass against white could still be completely inaccessible because the
   actual background is dark. The spec says accessibility should cover "contrast, readability, and
  accessibility-oriented choices" — the current implementation only addresses the first dimension for one specific
  background color.

  GAP-04: requirePageTitle Governance Rule Checks Metadata, Not Visible Intent
  The Reddit backlog doc explicitly flags this: "page title governance currently treats page metadata as a page
  title rather than verifying visible title intent." The governance rule requirePageTitle evaluates page metadata
  names (always present) rather than detecting whether a text visual or visual title is visible on the canvas. A
  page named "Page 1" with no visible title passes this rule under the current logic. This is directly called out in
   the UAT checklist as expected behavior to test in 0.1.13.

  GAP-05: Default Framework Weights Discrepancy Between Spec and Implementation
  The spec (PBIR_ANALYZER_V1_SPEC.md, lines 143–148) defines default weights as:
  - Gestalt: 25, Cognitive: 20, DataInk: 15, Accessibility: 15, Visual: 15, Enterprise Governance: 10 (total: 100)
  
  The store.ts implementation has:
  - Gestalt: 30, Cognitive: 20, DataInk: 15, Accessibility: 15, Visual: 20 (total: 100, governance not listed as
  enabled)
  
  The spec requires Enterprise Governance to be a default-enabled framework at 10% weight, but the store initializes
   it as optional/disabled. This means new installations get different scoring behavior than the spec intends, and
  governance is effectively invisible in default configurations.

  4.2 Significant Gaps

  GAP-06: Canvas Size Assumption
  Grid alignment scoring uses CanvasWidth = 1280 and CanvasHeight = 720 as baseline constants. While there are
  helper methods that attempt to read actual page dimensions, many scoring paths use the hardcoded constants.
  Reports built on 1366×768, 16:10, or custom canvas sizes will have grid alignment calculations that produce
  meaningless results because column/row boundaries won't correspond to actual snap points.

  GAP-07: Quick Fix Surface is Too Narrow
  Only three quick fix types are implemented. The Reddit backlog identifies six higher-value operations that are not
   present: consolidate filters to top/left, normalize card alignment, reduce visual count by identifying low-signal
   visuals, replace donut charts with comparison charts, standardize label naming patterns, and padding/spacing
  corrections. The current quick fix surface is minimal enough that users are unlikely to use it.

  GAP-08: No Audience Presets
  The Reddit backlog specifies executive, operational, and analyst presets that would affect density thresholds,
  required comparison context, and acceptable control counts. Without presets, the same thresholds apply to an
  executive overview page (which should be sparse) and an analyst detail page (which legitimately has more visuals).
   This causes false positives on dense analyst pages and false negatives on executive pages that lack the
  comparison context an executive should have.

  GAP-09: No Export or Sharing Capability
  Score results exist only in the VS Code panel. There is no way to export a score result as a PDF report, share it
  as JSON, or integrate results into a CI/CD pipeline. This limits the extension to interactive developer use and
  prevents it from being used in governance workflows where results need to be documented or shared.

  GAP-10: Marketplace Availability
  The extension is distributed as a .vsix file only. It is not published to the VS Code Marketplace. This limits
  discoverability and makes installation require manual steps. The publisher ID bcrowell exists in package.json but
  no marketplace publication is present.

  4.3 Minor Gaps

  GAP-11: Performance — Sequential Per-Page Scoring
  The full-report scoring loop in ComputeReportScore iterates pages sequentially. For a 21-page report like the
  primary fixture, this takes 6–10 seconds. Each page's framework calculations are independent and could be
  parallelized using Task.WhenAll or Parallel.ForEach with appropriate concurrency controls, reducing scoring time
  significantly.

  GAP-12: Optional Framework Scoring Depth Inconsistency
  The five optional frameworks (graphical perception, Stephen Few, Tufte, density, narrative) have fewer scoring
  sub-criteria than the five default frameworks. Narrative scoring, for example, checks for headline KPI presence,
  trend presence, and comparison presence, but the Reddit backlog identifies this as too shallow. Users who enable
  these optional frameworks may receive scores with limited actionable feedback compared to core frameworks.

  GAP-13: No Telemetry or Usage Analytics
  There is no instrumentation to understand which commands users invoke most, which frameworks surface the most
  findings, or which recommendations are most common. This makes it difficult to prioritize future development based
   on actual usage patterns.

  GAP-14: Dev Server Mode Not Documented for Contributors
  The Vite dev server mode (watch:webview) enables hot reload during webview development, but it is not mentioned in
   CONTRIBUTING.md or HOW_TO_USE.md. Contributors modifying the React panels will find this out by reading the build
   scripts rather than the docs.

  GAP-15: Stale Test Infrastructure
  There are two PbirScoringServiceTests.cs files at different paths (service-dotnet/tests/ and
  service-dotnet/tests/Services/). The coverage for the .NET scoring logic is minimal compared to the complexity of
  PbirScoringService.cs. The 11 scoring framework methods have no targeted unit tests for boundary conditions (zero
  visuals, all-decorative pages, threshold edge cases).

  ---
  5. Enhancement Recommendations

  Priority 1 — Fix Before v1 is Considered Complete

  REC-01: Implement Bookmark State Scoring
  Wire LayoutStateGenerator → visual filter → per-framework recompute. Each generated state should filter
  page.Visuals to include only non-hidden visuals for that state, then call the framework scoring methods on the
  filtered list. The perStateScores result becomes meaningful. This closes the gap between the spec requirement
  ("bookmark-heavy reports must not crash scoring") and the actual goal ("deeper per-state analysis").

  REC-02: Implement Dynamic Governance Rules
  Replace the EvaluateDynamicRules stub with actual evaluation against ScoreResult data. The 10 governance defaults
  each have clearly defined value semantics:
  - maxVisualsPerPage: compare against VisualMetadata visual count per page
  - allowPieCharts: check ScoreResult.Feedback["visualBestPractices"] or re-parse
  - requirePageTitle: detect visible title text visual on canvas (see REC-03)
  - maxHiddenVisuals: compare against HiddenVisualCount
  - minWhiteSpaceRatio: compute from occupied visual bounds vs. page area

  REC-03: Fix requirePageTitle to Detect Visible Titles
  The rich visual metadata added in the current branch (Feature 1) now captures VisibleTitleText. Use this to
  determine whether a page has a visible text element that serves as a title. A page passes requirePageTitle only if
   it has a textbox, visual title, or card with non-empty visible text in the upper portion of the canvas (e.g., top
   15% of canvas height).

  REC-04: Align Default Weights to Spec
  Update store.ts default framework weights to match the canonical spec: Gestalt 25, Cognitive 20, DataInk 15,
  Accessibility 15, Visual 15, Enterprise Governance 10. Ensure Enterprise Governance is default-enabled. Add a
  migration step to upgrade existing configurations from the old defaults.

  REC-05: Expand Accessibility Scoring
  At minimum, add:
  - Background color detection from BackgroundFillColor (now available from rich metadata) to check contrast against
   actual background, not just white
  - Font size check: visuals with font size below 10pt in a data context should generate a warning
  - Basic colorblind palette check: detect red/green color pairs that simulate problematic for deuteranopia

  Priority 2 — High-Value Post-v1

  REC-06: Implement Audience Presets
  Add three presets to the config panel: Executive, Operational, Analyst. Each preset adjusts:
  - Cognitive load threshold (executive: max 4 visuals; analyst: max 8)
  - Required comparison context severity (executive: error; analyst: warning)
  - Navigation control tolerance (operational: higher)
  - Density scoring ceiling (executive: tighter)
  Presets can be stored as config bundles in governance-defaults.json or as a separate presets.json.

  REC-07: Expand Quick Fix Surface
  Implement at minimum:
  - ConsolidateFilters: identifies scattered slicers and recommends moving to left rail or top band
  - NormalizeCardAlignment: identifies inconsistent KPI card top edges and recommends uniform Y alignment
  - ReplaceDonutWithBar: triggered when pie/donut visuals are present on overview pages
  - StandardizeLabelNaming: triggered when metric label inconsistency finding is present
  
  REC-08: Implement Governance Rule Export
  Add a command pbirAnalyzer.exportGovernanceReport that writes a markdown or JSON summary of the governance check
  result. This enables CI/CD integration and documentation workflows.

  REC-09: Parallelize Per-Page Scoring
  In ComputeReportScore, replace the sequential foreach (var page in pages) loop with a parallel approach. Each
  page's frameworks are independent. Using Parallel.ForEach with a concurrency limit of 4 would reduce 21-page
  report scoring from ~8 seconds to ~3 seconds.

  REC-10: Marketplace Publication
  Publish to the VS Code Marketplace under publisher bcrowell. Update the README to include marketplace badge and
  one-click install. This is the single highest-impact action for user acquisition.

  Priority 3 — Strategic Investments

  REC-11: Visual Audit Mode — Phase 1 (Screenshot Upload)
  The full design exists in future_visual_audit_mode.md. Phase 1 (screenshot upload + AI-assisted findings) can be
  implemented without browser automation. Key decisions already made:
  - VisualAuditSession stored in globalStorageUri
  - Filename-to-page auto-matching with review queue for unmatched
  - Structured AI provider abstraction with findingType, severity, confidence
  - Non-scored in V1 (additive evidence layer only)
  - AI credentials in VS Code SecretStorage

  REC-12: Narrative Scoring Expansion
  As documented in the Reddit backlog, narrative scoring needs sub-criteria beyond headline/trend/comparison
  detection:
  - Visible page purpose (title implies question or outcome, not just "Page 1")
  - KPI comparison context (each KPI has at least one of: prior period delta, target, trend sparkline)
  - Supporting evidence flow (KPI cluster has at least one explanatory chart nearby)
  - Overview-to-detail readability (page sequencing in report-level scoring)

  REC-13: Chart Semantics Expansion
  The Reddit backlog Feature 4 requires at minimum three distinct semantic chart-choice findings beyond pie
  detection:
  - Categorical line chart detection (line chart where X-axis appears categorical from field role hints)
  - Missing comparison on KPI-heavy pages (KPI cards present, no bar/column chart present)
  - Redundant axis + label combination (visual has both data labels and heavy axis labels)

  REC-14: Telemetry (Privacy-Respecting)
  Add VS Code telemetry using the extension's existing output channel for local diagnostics, and opt-in VS Code
  TelemetryReporter for usage patterns (commands invoked, framework score distribution, report page counts). No
  visual data or file contents should be transmitted.

  ---
  6. Technical Debt

  ┌────────────────────────────────────────────┬───────────────────────────────────────┬───────────────────────┐
  │                    Item                    │               Location                │       Severity        │
  ├────────────────────────────────────────────┼───────────────────────────────────────┼───────────────────────┤
  │ #pragma warning disable CS0618 appears ~4  │ Legacy field suppression for          │ Low — remove legacy   │
  │ times in PbirScoringService.cs             │ LayoutScore, ThemeScore,              │ fields in next major  │
  │                                            │ GovernanceScore                       │ version               │
  ├────────────────────────────────────────────┼───────────────────────────────────────┼───────────────────────┤
  │ Two PbirScoringServiceTests.cs files at    │ tests/ vs tests/Services/             │ Medium — consolidate, │
  │ different paths                            │                                       │  deduplicate          │
  ├────────────────────────────────────────────┼───────────────────────────────────────┼───────────────────────┤
  │ LSP / LspHost naming throughout            │ Not LSP; is JSON-RPC over stdio       │ Low — rename in next  │
  │                                            │                                       │ cleanup pass          │
  ├────────────────────────────────────────────┼───────────────────────────────────────┼───────────────────────┤
  │ Scoring constants CanvasWidth,             │                                       │ Medium — scoring      │
  │ CanvasHeight, GridCols, GridRows hardcoded │ Should derive from page metadata      │ accuracy issue        │
  │  at class level                            │                                       │                       │
  ├────────────────────────────────────────────┼───────────────────────────────────────┼───────────────────────┤
  │ Webview dev server mode undocumented       │ CONTRIBUTING.md, HOW_TO_USE.md        │ Low                   │
  ├────────────────────────────────────────────┼───────────────────────────────────────┼───────────────────────┤
  │ Older command aliases not cleaned up       │ register.ts                           │ Low                   │
  ├────────────────────────────────────────────┼───────────────────────────────────────┼───────────────────────┤
  │ Plans/backup markdown docs referenced in   │ docs/ historical files                │ Low                   │
  │ spec as deletable                          │                                       │                       │
  ├────────────────────────────────────────────┼───────────────────────────────────────┼───────────────────────┤
  │ Optional framework test coverage minimal   │ PbirScoringServiceTests.cs            │ Medium                │
  └────────────────────────────────────────────┴───────────────────────────────────────┴───────────────────────┘

  ---
  7. Testing Coverage Assessment

  TypeScript Extension Tests
  - Path resolution: well covered (pbirProjectPathing.test.ts)
  - Local tree building: well covered (pbirTreeProvider.localFallback.test.ts)
  - Config store: covered (designAnalyzerConfigStore.test.ts)
  - Score result normalization: covered (scoreResultPayload.test.ts)
  - Governance command: covered (pbirGovernanceCommand.test.ts)
  - Score command: partially covered (pbirScoreCommand.treeItem.test.ts)
  
  Missing TypeScript Coverage
  - LSPModelService request/response routing
  - PbirScorePanel message handling
  - PbirConfigPanel message handling
  - webviewAssets manifest resolution
  - quickFixes.ts fix generation
  
  C# Backend Tests
  - PbirScoringServiceTests.cs (two copies) — limited coverage of the 11-framework scoring logic
  - PbirGovernanceServiceTests.cs — governance evaluation
  - PbirTreeBuilderTests.cs — tree structure building
  - LspHostJsonRpcTests.cs — JSON-RPC dispatch

  Missing .NET Coverage
  - Per-framework boundary conditions (zero visuals, all-decorative, threshold edges)
  - Navigation scoring weight variations
  - Canvas size normalization
  - Bookmark state filter/recompute path (when implemented)
  - Dynamic governance rule evaluation (when implemented)
  - WCAG calculator edge cases (malformed hex, #RGB shorthand, named colors)

  Recommendation: Add parameterized xUnit tests for each scoring framework with at least three fixture pages: pass
  (score near 100), borderline, and fail (score near 0). This would lock in expected behavior and catch regressions
  when scoring logic is modified.

  ---
  8. UX and Product Observations

  Score Panel
  - The multi-page tab model with instant switching (cached results) is a well-designed interaction pattern.
  - Finding classification badges (Objective/Heuristic/Style) are a meaningful UX addition that helps users
  calibrate how seriously to take each finding.
  - The "Reveal" action linking findings to tree visuals is the killer feature for developer workflow.
  - Gap: There is no way to see a finding list sorted by severity across all frameworks simultaneously. Users must
  expand each framework section individually to find all High-severity findings.

  Config Panel
  - Weight validation (must total 100) is good. The UX for redistributing weights is not described in the spec but
  will be critical: when a user enables a new framework, the existing weights no longer total 100 and they must
  manually rebalance. Consider auto-rebalance on enable/disable.

  Tree View
  - The tree shows visual type labels which is useful for context.
  - Gap: No visual count badge on page nodes. Users cannot see at a glance that Page 3 has 22 visuals (a governance
  concern) without expanding.

  Onboarding
  - There is no walkthrough or welcome view. First-time users must discover commands through the activity bar. A VS
  Code walkthrough (contributes.walkthroughs) would significantly reduce friction.

  ---
  9. Prioritized Recommendations Summary

  ┌──────────┬────────────────────────────────────────────────┬────────────────────────────┬────────┐
  │ Priority │                 Recommendation                 │           Impact           │ Effort │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P0       │ REC-04: Align default weights to spec          │ Score correctness          │ Low    │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P0       │ REC-02: Implement dynamic governance rules     │ Core feature completeness  │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P0       │ REC-03: Fix requirePageTitle for visible title │ Core feature correctness   │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P0       │ REC-05: Expand accessibility scoring           │ Core feature depth         │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P1       │ REC-10: Marketplace publication                │ User acquisition           │ Low    │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P1       │ REC-01: Implement bookmark state scoring       │ Completeness               │ High   │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P1       │ REC-06: Audience presets                       │ Scoring relevance          │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P1       │ REC-09: Parallel per-page scoring              │ Performance                │ Low    │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P2       │ REC-07: Expand quick fix surface               │ Actionability              │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P2       │ REC-08: Governance report export               │ Enterprise workflow        │ Low    │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P2       │ REC-12: Narrative scoring expansion            │ Heuristic quality          │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P2       │ REC-13: Chart semantics expansion              │ Heuristic quality          │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P3       │ REC-11: Visual Audit Mode Phase 1              │ New capability             │ High   │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P3       │ REC-14: Telemetry                              │ Development prioritization │ Medium │
  │ P1       │ REC-01: Implement bookmark state scoring       │ Completeness               │ High   │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P1       │ REC-06: Audience presets                       │ Scoring relevance          │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P1       │ REC-09: Parallel per-page scoring              │ Performance                │ Low    │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P2       │ REC-07: Expand quick fix surface               │ Actionability              │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P2       │ REC-08: Governance report export               │ Enterprise workflow        │ Low    │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P2       │ REC-12: Narrative scoring expansion            │ Heuristic quality          │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P2       │ REC-13: Chart semantics expansion              │ Heuristic quality          │ Medium │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P3       │ REC-11: Visual Audit Mode Phase 1              │ New capability             │ High   │
  ├──────────┼────────────────────────────────────────────────┼────────────────────────────┼────────┤
  │ P3       │ REC-14: Telemetry                              │ Development prioritization │ Medium │
  └──────────┴────────────────────────────────────────────────┴────────────────────────────┴────────┘

  ---
  10. Conclusion

  The PBIR Design Analyzer is a technically sound and increasingly feature-rich extension. The architecture choices
  (LSP bridge, graceful degradation, React webviews, 11-framework model) are well-suited to the problem domain. The
  gap between spec and implementation is primarily in areas that were intentionally deferred or marked as
  placeholder — bookmark state scoring, dynamic governance rules, and accessibility depth — rather than reflecting
  sloppy execution.

  The most urgent work is closing the three correctness gaps (REC-04 weight alignment, REC-02 governance
  enforcement, REC-03 title detection) since these affect every user who runs a governance check. The highest
  strategic value is in marketplace publication (zero effort, maximum reach) and Visual Audit Mode (large effort,
  differentiating capability that no existing tool provides for local PBIR files).

✻ Crunched for 5m 36s

※ recap: Analyzing the PBIR Design Analyzer VS Code extension to produce a gap assessment and enhancement report.
  The report is complete and ready for your review. (disable recaps in /config)

❯ /model                                                                                                            
  ⎿  Set model to Opus 4.7 (1M context) for this session

❯ create a plan around the above assessment. Change REC-04 to be Gestalt: 30, Cognitive: 20, DataInk: 15,           
  Accessibility: 15, Visual: 20 (total: 100, governance not listed as enabled) as the default. As not everyone will 
   have Enterprise Governance. Present the plan for my approval. do not make any code changes.                      
  
⏺ Before I write the plan, let me clarify scope with a few questions.
  ⎿  Invalid tool parameters
  ⎿  Invalid tool parameters
  ⎿  Invalid tool parameters

⏺ User answered Claude's questions:
  ⎿  · Which recommendations should this plan cover? → all recommendations from 9. Prioritized Recommendations 
     Summary. break the plan down into phases based on the first column.
     · How should the plan treat Visual Audit Mode (REC-11)? → Include full Phase 1 design
     · Should marketplace publication (REC-10) be included as a plan step? → No — out of scope for this plan

⏺ Now let me write the plan to the file.

⏺ Updated plan
  ⎿  /plan to preview

⏺ Plan written. Summary of what it contains:

  - Context: explains the gap between current state and v1-ready, plus the REC-04 direction reversal you specified
  - Phase 0 (P0 Correctness): REC-04 spec update (store wins), REC-02 dynamic governance rules, REC-03 visible title
   detection, REC-05 accessibility expansion
  - Phase 1 (P1 High-Value): REC-01 bookmark state scoring, REC-06 audience presets, REC-09 parallel scoring (REC-10
   marketplace excluded per your direction)
  - Phase 2 (P2 Quality): REC-07 quick fixes, REC-08 governance export, REC-12 narrative expansion, REC-13 chart
  semantics
  - Phase 3 (P3 Strategic): REC-11 Visual Audit Mode Phase 1 with full design, REC-14 privacy-respecting telemetry
  - Cross-cutting concerns, verification plan, and explicit out-of-scope list

────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
 Ready to code?

 Here is Claude's plan:
╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌
 PBIR Design Analyzer — Enhancement Plan

 Context

 The PBIR Design Analyzer (v0.1.13 unreleased, branch Rich-Visual-Metadata-Extraction) is a VS Code extension that
 scores Power BI PBIP/PBIR reports against 11 design frameworks. A repository-wide analysis (see prior assistant
 message in this conversation) identified 14 prioritized recommendations spanning correctness gaps, performance,
 UX, and one large strategic capability (Visual Audit Mode).

 The gaps fall into three categories:

 1. Correctness gaps that affect users today — placeholder governance rule evaluation, a metadata-only
 requirePageTitle check that ignores visible titles, shallow accessibility scoring that only compares theme colors
 against white, and a spec-vs-store default-weights discrepancy.
 2. Quality and performance gaps — non-functional bookmark state scoring, sequential page scoring, missing audience
  presets, thin quick-fix surface, no export.
 3. Strategic capability gaps — no Visual Audit Mode (screenshot upload + AI-assisted findings) and no telemetry to
  inform future work.

 This plan organizes all 14 recommendations into four execution phases keyed off the priority column from the
 assessment. Marketplace publication (REC-10) is excluded per direction. Direction reversal for REC-04: the store
 defaults (Gestalt 30 / Cognitive 20 / DataInk 15 / Accessibility 15 / Visual 20, governance not default-enabled)
 are canonical — the spec is the document that needs updating, because not all customers run Enterprise Governance.

 The intended outcome is a v1.0-ready extension with defensible scoring correctness (Phase 0), a credible
 enterprise feature set (Phase 1), a polished feedback experience (Phase 2), and a forward-looking visual audit
 capability (Phase 3).

 ---
 Phase 0 — Correctness (P0)

 Goal: every default install produces score results that match the documented spec and every governance rule that
 ships in defaults actually fires.

 REC-04 — Align spec to store defaults (reversed direction)

 Files
 - docs/PBIR_ANALYZER_V1_SPEC.md (lines 121–148 framework set + default weights)
 - docs/CHANGELOG.md — note the spec correction

 Change
 - Update Section "Default Weights": Gestalt 30, Cognitive 20, DataInk 15, Accessibility 15, Visual 20 (sum 100).
 - Move governance (Enterprise Governance) out of the core/default-enabled framework list and into the optional
 list.
 - Update Section "Framework Set" and "Acceptance Criteria for V1" wording to remove the implicit assumption that
 governance is always enabled.

 Rationale
 Reverses the assessment finding. The store is correct; the spec drifted. Customers without Enterprise Governance
 should not be forced into a default that includes it.

 Verification
 - Diff spec against vscode-extension/src/analyzer/config/store.ts getDefaultFrameworks() to confirm exact match.
 - No code changes; spec-only.

 ---
 REC-02 — Implement dynamic governance rule evaluation

 Files
 - service-dotnet/Services/Pbir/PbirGovernanceService.cs — replace EvaluateDynamicRules stub (lines 313–323)
 - service-dotnet/Services/Pbir/Models/GovernancePolicy.cs — confirm DynamicRules shape supports all 10 default
 rules
 - service-dotnet/Services/Pbir/Models/ScoreResult.cs — verify it surfaces the data the rules need (visual counts
 per page, theme id, pie usage, custom visual presence)
 - service-dotnet/tests/PbirGovernanceServiceTests.cs — add per-rule fixtures

 Pattern
 EvaluateDynamicRules(policy, score, reasons) should switch on rule key and compare rule value against the
 corresponding field on ScoreResult / its PageScores. Each rule that fails appends to reasons with a clear message.

 Default rules to wire up (all 10 from vscode-extension/config/governance-defaults.json):
 - maxVisualsPerPage, maxBookmarksPerPage, maxLayoutStatesPerPage, maxHiddenVisuals — per-page count comparisons
 against PageScore.DataVisualCount, HiddenVisualCount, and bookmark/state collections
 - minWhiteSpaceRatio — compute occupied bounds vs canvas area on each page
 - allowPieCharts — inspect Feedback["visualBestPractices"] pie-finding presence or re-scan visuals
 - allowCustomVisuals — scan VisualMetadata for custom visual type strings
 - requirePageTitle — delegate to REC-03's visible title detector
 - requireFilterPanel — check slicer presence in PageScore.VisualMetadata
 - themeStandard — string-compare themeId against rule value

 Verification
 - xUnit fixtures: one pass-case and one fail-case per rule
 - Manual: enable each rule in .vscode/settings.json against Sales & Production.pbip, run governance check, confirm
  pass/fail aligns with rule

 ---
 REC-03 — Fix requirePageTitle to detect visible titles

 Files
 - service-dotnet/Services/Pbir/PbirScoringService.cs — add HasVisibleTitle(page) helper using VisibleTitleText
 field from rich visual metadata (already extracted on the current branch)
 - Called from REC-02's governance rule evaluator, REC-12's narrative scoring, and existing visual best practices
 scoring

 Detection rule
 A page has a visible title when any non-hidden visual on the page meets all of:
 - Visual type is textbox, has a non-empty VisibleTitleText, or is a card/visual whose own title block contains
 non-empty text
 - Position is in the upper portion of the canvas (Y ≤ 15% of canvas height)
 - Text is not a generic placeholder like Page 1, New page, empty string

 Verification
 - xUnit: synthetic page with textbox in top band (pass), page with bottom-band text (fail), page with only
 metadata name (fail), page with Page 1 text (fail)
 - Manual: confirm requirePageTitle rule correctly distinguishes pages with and without visible titles in the
 fixture

 ---
 REC-05 — Expand accessibility scoring

 Files
 - service-dotnet/Services/Pbir/PbirScoringService.cs — ComputeAccessibilityScore (around line 1065)
 - service-dotnet/Services/Pbir/WcagContrastCalculator.cs — add helpers for colorblind simulation if needed
 - service-dotnet/Services/Pbir/Models/VisualMetadataSummary.cs — ensure BackgroundFillColor, FontColor, FontSize
 are surfaced (current branch adds these)

 New sub-criteria (each contributes to the 100-point Accessibility score)
 1. Theme palette contrast (existing) — keep current check, weight reduced
 2. Background-aware text contrast — for each visual with BackgroundFillColor + FontColor, compute actual contrast.
  Cite specific visuals on failure.
 3. Font size readability — flag visuals where data-context font size < 10pt
 4. Basic colorblind pair detection — detect theme palettes with red/green pairs within ΔE threshold that simulate
 problematic for deuteranopia (use a simple sRGB simulation matrix)

 Verification
 - xUnit fixtures with: white theme passing all checks, dark theme failing background-aware check, small-font
 fixture failing readability, red-green palette failing colorblind check
 - Manual: run on a known-inaccessible report (one of the test fixtures could be modified to use red/green pairs)

 ---
 Phase 1 — High-Value (P1)

 Goal: deliver the most impactful capability gaps. Marketplace publication is intentionally out of scope per
 direction.

 REC-01 — Implement bookmark state scoring

 Files
 - service-dotnet/Services/Pbir/PbirScoringService.cs — replace placeholder block (lines 281–307)
 - service-dotnet/Services/Pbir/BookmarkStateAnalyzer.cs — add FilterVisualsForState(visuals, state) if not present
 - service-dotnet/Services/Pbir/LayoutStateGenerator.cs — confirm state output shape

 Pattern
 For each generated layout state:
 1. Filter page.Visuals to include only visuals visible in that state (use bookmark hiddenVisuals and
 targetVisualIds to determine visibility)
 2. Build a PageData shadow with the filtered visual list
 3. Call the existing per-framework scoring methods on the shadow
 4. Store the resulting composite score in perStateScores[stateName]

 The average across states becomes the page's bookmark-aware score; the per-state breakdown surfaces in the score
 panel.

 Verification
 - xUnit: synthetic page with 3 bookmarks each showing a different visual subset — confirm per-state scores differ
 - Manual: open a bookmark-heavy fixture, score it, expand per-state breakdown in score panel

 ---
 REC-06 — Audience presets

 Files
 - New: vscode-extension/config/audience-presets.json — three preset bundles
 - vscode-extension/src/analyzer/config/types.ts — AudiencePreset interface, field on DesignAnalyzerConfig
 - vscode-extension/src/analyzer/config/store.ts — load presets, apply on selection
 - vscode-extension/webview-src/analyzer-config/App.tsx — preset dropdown above framework table
 - service-dotnet/Services/Pbir/PbirScoringService.cs — receive preset thresholds via existing config JsonElement,
 override defaults where preset specifies

 Preset shape
 {
   "executive": { "maxVisualsPerPage": 4, "comparisonContextSeverity": "error", "navigationToleranceMultiplier":
 0.7 },
   "operational": { "maxVisualsPerPage": 6, "comparisonContextSeverity": "warning",
 "navigationToleranceMultiplier": 1.2 },
   "analyst": { "maxVisualsPerPage": 8, "comparisonContextSeverity": "info", "navigationToleranceMultiplier": 1.0 }
 }

 Selecting a preset overlays its values onto the config; the user can still tune individual weights afterward.

 Verification
 - Jest: preset overlay correctness, persistence
 - xUnit: cognitive load and visual best practices scoring respects preset thresholds
 - Manual: switch presets in config panel, rescore, confirm thresholds shift

 ---
 REC-09 — Parallel per-page scoring

 Files
 - service-dotnet/Services/Pbir/PbirScoringService.cs — ComputeReportScore per-page foreach (around line 464)

 Pattern
 Replace foreach (var page in pages) with Parallel.ForEach(pages, new ParallelOptions { MaxDegreeOfParallelism = 4
 }, page => { ... }). Use a ConcurrentBag<PageScore> for result.PageScores and a ConcurrentDictionary<string,
 string> for result.ScoringErrors. After the parallel loop, sort PageScores by original page index to preserve
 display order.

 Risks
 - The current per-page scoring methods take a recommendations list parameter that mutates. Either pass a fresh
 per-iteration list and merge afterward, or refactor framework methods to return recommendations rather than
 mutate.

 Verification
 - xUnit: parallel run produces identical composite scores to sequential run on the fixture
 - Benchmark: 21-page report scoring time drops from ~8s to <4s

 ---
 Phase 2 — Quality Push (P2)

 Goal: meaningfully improve the depth and actionability of feedback.

 REC-07 — Expand quick fix surface

 Files
 - vscode-extension/src/analyzer/score/quickFixes.ts — extend buildQuickFixList
 - vscode-extension/webview-src/analyzer-score/App.tsx — render new fix types

 New fix types
 1. ConsolidateFilters — triggered by filterScatter / filterPlacement findings; suggests target band
 2. NormalizeCardAlignment — triggered by topBandKpiConsistency finding; suggests uniform Y
 3. ReplaceDonutWithBar — triggered by pie/donut finding on overview page
 4. StandardizeLabelNaming — triggered by metricLabelConsistency finding

 Quick fixes remain advisory in v1 (no auto-apply); they surface as actionable text with the affected visuals.

 Verification
 - Jest: each fix type triggers from its corresponding finding shape
 - Manual: each fix appears in the score panel for a fixture that contains the matching finding

 ---
 REC-08 — Governance report export

 Files
 - vscode-extension/src/commands/pbirCommands.ts — new command pbir.exportGovernanceReport
 - vscode-extension/src/commands/register.ts — alias pbirAnalyzer.exportGovernanceReport
 - vscode-extension/package.json — command registration + activation event

 Output
 Two formats, user picks at export time:
 - Markdown summary (human-readable, suitable for PR comments)
 - JSON (suitable for CI/CD ingestion)

 Both contain: report path, score timestamp, composite score, per-framework scores, governance pass/fail, blocked
 rules with reasons.

 Verification
 - Jest: markdown and JSON shapes match fixtures
 - Manual: export from score panel, open file, confirm contents

 ---
 REC-12 — Narrative scoring expansion

 Files
 - service-dotnet/Services/Pbir/PbirScoringService.cs — ComputeNarrativeDesignScore

 New sub-criteria
 1. Visible page purpose (delegates to REC-03 visible title detector)
 2. KPI comparison context — each KPI card must have at least one of: prior period delta, target reference, trend
 sparkline (detect via adjacent line chart on the same Y band)
 3. Supporting evidence flow — KPI cluster has at least one explanatory chart within a defined proximity
 4. Overview-to-detail readability — first page of report is sparser than later pages (report-level signal)

 Verification
 - xUnit: fixtures for each sub-criterion (pass / fail)
 - Manual: score a KPI-only page, confirm narrative findings cite missing comparison context

 ---
 REC-13 — Chart semantics expansion

 Files
 - service-dotnet/Services/Pbir/PbirScoringService.cs — ComputeGraphicalPerceptionScore,
 ComputeVisualBestPracticesScore

 New findings
 1. Categorical line chart — line chart whose X-axis field role hint suggests a categorical (not ordinal/temporal)
 field
 2. Missing comparison on KPI-heavy page — page has ≥2 KPI cards but no bar/column comparison visual
 3. Redundant axis + labels — visual has both data labels and full axis labeling

 These require the rich visual metadata from the current branch (FieldRoleHint, HasDataLabels, HasAxisLabels).

 Verification
 - xUnit: one fixture per finding type
 - Manual: confirm each finding surfaces with clear, actionable message

 ---
 Phase 3 — Strategic (P3)

 Goal: deliver capabilities that differentiate the extension and inform future direction.

 REC-11 — Visual Audit Mode Phase 1 (screenshot upload)

 Reference design: docs/future_visual_audit_mode.md (already a thorough plan).

 File layout (all new)
 - vscode-extension/src/analyzer/audit/types.ts — VisualAuditSession, VisualCapture, VisualAuditFinding,
 VisualAuditPageCoverage
 - vscode-extension/src/analyzer/audit/session.ts — load/save/migrate session in extensionContext.globalStorageUri
 - vscode-extension/src/analyzer/audit/filenameMatching.ts — normalize and match filenames to PBIR page names
 - vscode-extension/src/analyzer/audit/providers/VisualAuditProvider.ts — provider abstraction
 - vscode-extension/src/analyzer/audit/providers/AnthropicVisualAuditProvider.ts — initial concrete provider;
 credentials via vscode.SecretStorage
 - vscode-extension/src/commands/pbirCommands.ts — new commands: pbir.uploadScreenshots, pbir.attachScreenshot,
 pbir.removeScreenshot, pbir.assignUnmatched
 - vscode-extension/src/views/PbirScorePanel.ts — render audit coverage card + per-page audit findings
 - vscode-extension/webview-src/analyzer-score/App.tsx — coverage card + capture selector + audit findings list
 - vscode-extension/src/analyzer/contracts/scorePanel.ts — extend message contract with audit payload
 - vscode-extension/package.json — register new commands

 Workflow
 1. User invokes Upload Report Screenshots → picks folder/files → extension copies into session asset folder →
 manifest persisted
 2. Filename matcher auto-assigns to PBIR pages; unmatched go to review queue
 3. For each capture, provider receives: page name, PBIR visual metadata for that page, existing framework findings
  for that page, screenshot bytes
 4. Provider returns structured findings with findingType, severity, confidence, text, recommendation, optional
 regionHint
 5. Findings render as a separate evidence layer in the score panel; non-scored in V1

 Data location
 All session data and screenshot assets live in extensionContext.globalStorageUri, never in the repo.

 Provider abstraction
 The provider interface should accept the union of inputs above and return VisualAuditFinding[]. The Anthropic
 provider is the first implementation; others can plug in without changing the consumer.

 Default classification rules
 - objective only for clearly visible issues (clipped text, overlap, error states)
 - strongHeuristic for hierarchy, scan path, spacing, density observations
 - stylePreference for polish observations

 Verification
 - Jest: filename matching with realistic Power BI export naming patterns; session persistence across reload;
 unmatched assignment flow; coverage summary math
 - Score panel tests: empty state, single screenshot, multi-state captures, findings rendering alongside PBIR
 findings
 - Provider contract: valid response, malformed response fallback, low-confidence findings marked
 - Manual UAT (matches docs/future_visual_audit_mode.md test plan):
   - Import 5+ screenshots for a multi-page report
   - Confirm missing-page detection
   - Confirm page mapping corrections persist after VS Code reload
   - Confirm assets survive reload

 ---
 REC-14 — Telemetry (privacy-respecting)

 Files
 - vscode-extension/package.json — add @vscode/extension-telemetry dependency, declare telemetry usage in
 description
 - New: vscode-extension/src/telemetry/reporter.ts — wrapper around TelemetryReporter, gated on user opt-in (VS
 Code's global telemetry.telemetryLevel setting)
 - vscode-extension/src/extension.ts — initialize reporter at activate, dispose at deactivate
 - Call sites: each command handler in vscode-extension/src/commands/pbirCommands.ts, each panel open in
 PbirScorePanel.ts / PbirConfigPanel.ts

 Events to emit (no PII, no file contents, no visual data)
 - command.invoked with {commandName}
 - scoring.completed with {pageCount, durationMs, compositeScoreBucket} (bucket: 0-49 / 50-69 / 70-89 / 90-100)
 - governance.evaluated with {blocked, reasonCount}
 - framework.enabled / framework.disabled with {frameworkId}

 Verification
 - Jest: reporter is a no-op when telemetryLevel is off
 - Manual: confirm telemetry calls fire in VS Code output channel debug mode

 ---
 Cross-Cutting Concerns

 These apply across all phases and are not standalone work items.

 - Tests first for correctness fixes: P0 items (REC-02, REC-03, REC-05) should ship with tests written before the
 implementation lands, since these are correctness fixes for known-broken behavior.
 - Backwards compatibility: Each phase must preserve the existing ScoreResult and GovernanceCheckResult shapes; new
  fields are additive only.
 - Migration: Config schema changes (audience presets in Phase 1) must include a migration in store.ts that
 upgrades existing user configs.
 - Documentation: Every phase updates docs/CHANGELOG.md. Phases that change user-visible behavior update
 docs/HOW_TO_USE.md.
 - Branch strategy: Each REC item should land on its own feature branch with a focused PR. Phase 0 items can ship
 as a single 0.2.0 release; subsequent phases ship as 0.3.0, 0.4.0, 1.0.0.

 ---
 Verification Plan (End-to-End)

 Phase exit criteria:

 ┌───────┬──────────────────────────────────────────────────────────────────────────────────────────────────────┐
 │ Phase │                                            Exit Criteria                                             │
 ├───────┼──────────────────────────────────────────────────────────────────────────────────────────────────────┤
 │       │ Spec matches store. All 10 governance rules fire on appropriate fixtures. requirePageTitle           │
 │ 0     │ distinguishes visible vs. metadata-only titles. Accessibility cites specific visuals on dark-themed  │
 │       │ test fixtures.                                                                                       │
 ├───────┼──────────────────────────────────────────────────────────────────────────────────────────────────────┤
 │ 1     │ Per-state bookmark scores differ when state visual subsets differ. Audience presets shift thresholds │
 │       │  visibly. 21-page report scoring time < 4s.                                                          │
 ├───────┼──────────────────────────────────────────────────────────────────────────────────────────────────────┤
 │ 2     │ Each of the 4 new quick fixes appears for a matching fixture. Governance export produces valid       │
 │       │ markdown and JSON. New narrative + chart semantics findings appear with clear messages.              │
 ├───────┼──────────────────────────────────────────────────────────────────────────────────────────────────────┤
 │       │ Screenshot upload session survives VS Code reload. Filename matching auto-assigns ≥80% of well-named │
 │ 3     │  exports. Audit findings render alongside PBIR findings without disrupting composite score.          │
 │       │ Telemetry emits expected events and respects user opt-out.                                           │
 └───────┴──────────────────────────────────────────────────────────────────────────────────────────────────────┘

 Manual UAT fixture: ~/Documents/GitHub/PBITesting/Sales & Production.pbip (21 pages, 172 visuals, bookmark
 references, custom visuals). Each phase exit requires a clean UAT pass against this fixture.

 ---
 Out of Scope (Per Direction)

 - REC-10 Marketplace publication — deferred to a separate release-engineering plan
 - Technical debt items from Section 6 of the assessment (LSP rename, duplicate test files, canvas constant
 cleanup, etc.) — opportunistic cleanup during related work, not first-class plan items
 - Per-report config storage — explicitly out of scope in the v1 spec; this plan respects that
 - Visual Audit Mode Phase 2 (Chromium inspection) — covered in the future doc; defer detailed planning until Phase
  1 ships and signal quality is proven