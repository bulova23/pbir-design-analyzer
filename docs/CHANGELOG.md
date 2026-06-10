# Changelog

All notable changes to PBIR Design Analyzer are recorded here.

## 0.6.0 — 2026-06-10

### Performance And Scalability

- Added a shared repository snapshot seam for local PBIR fallback analysis and Fabric App review evidence extraction.
- Reworked the highest-impact extension-host repo traversal paths onto async filesystem access:
  - local PBIR tree fallback
  - Fabric review evidence extraction
- Eliminated repeated Fabric review repo walks by running TypeScript, navigation, token, screenshot, and semantic-model evidence extraction from one shared repository snapshot.

### Protocol And State Hardening

- Added explicit score-panel protocol and schema version metadata.
- Added shared host/webview payload guards so invalid or mismatched score-panel messages fail early with a clear error instead of reaching deep React render paths.
- Added selected page-state clamping on both the host and webview sides so stale `selectedPageIndex` values cannot point past the current page bounds after rescoring.

### Scoring Configuration

- Externalized Fabric review and Fabric readiness scoring constants into an inspectable shared configuration module with explicit provenance metadata.
- Preserved existing default behavior while adding bounded internal override hooks for:
  - Fabric review quality-score penalties and minimums
  - semantic-model evidence limits
  - readiness thresholds, classification boundaries, and finding confidences

### Architecture Notes

- The shared repository snapshot is analyzer-independent and intended for reuse across PBIR and Fabric analysis flows.
- The score-panel host/webview seam is now a versioned contract rather than an implicitly shared object shape.
- The current safe manual validation gap remains true virtual-workspace runtime proof; packaged metadata still declares the blocked posture explicitly.

### Validation

- Passed:
  - `cd vscode-extension && npx jest src/test/repositorySnapshot.test.ts src/test/typescriptEvidence.test.ts src/test/navigationEvidence.test.ts src/test/designTokenEvidence.test.ts src/test/screenshotEvidence.test.ts src/test/semanticModelEvidence.test.ts src/test/fabricAppReviewAnalyzer.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/scorePanelProtocol.test.ts src/test/fabricScoringConfig.test.ts src/test/readinessScoring.test.ts webview-src/analyzer-score/App.test.tsx --runInBand`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package:all`
- VSIX inspection confirmed:
  - packaged version remained `0.5.0`
  - target-specific artifacts remained correct for Windows x64, Windows arm64, Linux x64, macOS x64, and macOS arm64
  - packaged manifest metadata still declares:
    - `pbirAnalyzer.explorer`
    - unsupported untrusted workspaces
    - unsupported virtual workspaces
  - no stale release-facing `powerbi-modeling.*` or old explorer/config identifiers were reintroduced
- Workspace-posture smoke results:
  - attempted an actual untrusted-workspace VS Code test-host launch with `--disable-workspace-trust`
  - local extension-host runtime still reported `vscode.workspace.isTrusted === true`, so this environment could not prove the blocked posture beyond manifest declarations
  - true virtual-workspace runtime validation remains unavailable locally because no virtual workspace provider/session is available in this environment

## 0.5.2 — 2026-06-10

### Operational Coherence

- Consolidated extension runtime logging onto shared singleton output channels for:
  - general extension activity
  - backend activity
  - backend trace
  - score diagnostics
- Removed one-off output-channel creation during command error handling so diagnostics stay in the same predictable locations.

### Namespace And Metadata

- Promoted `pbirAnalyzer` as the canonical command, view, and configuration namespace.
- Moved the explorer view ID to `pbirAnalyzer.explorer`.
- Added legacy command aliases for the older `pbir.*` command family so existing command links and automation keep routing to the canonical registrations during migration.
- Added canonical `pbirAnalyzer.governance.*` settings.
- Kept legacy `powerbi-modeling.governance.*` fallback reads in code only, without continuing to expose those keys in release-facing contributed configuration metadata.

### Runtime Posture

- Declared unsupported posture for:
  - untrusted workspaces
  - virtual workspaces
- Made telemetry behavior explicit for this release train:
  - telemetry remains local-only and no-op
  - no debug console event emission
  - no production telemetry pipeline introduced in this scope

### Docs And Supportability

- Updated the troubleshooting guide so it matches the shipped command names, explorer label, and packaged-backend restart path.
- Added regression coverage for:
  - shared output-channel reuse
  - manifest capability declarations
  - canonical view/config metadata
  - legacy governance-setting fallback
  - legacy review-workflow command alias routing
  - explicit no-op telemetry behavior

### Validation

- Passed:
  - `cd vscode-extension && npx jest src/test/outputChannels.test.ts src/test/packageManifest.test.ts src/test/pbirGovernanceCommand.test.ts src/test/pbirReviewWorkflowExportCommand.test.ts src/test/telemetryReporter.test.ts --runInBand`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run package:all`
- VSIX inspection confirmed:
  - packaged version remained `0.5.0`
  - target-specific artifacts remained correct for Windows x64, Windows arm64, Linux x64, macOS x64, and macOS arm64
  - backend binaries remained target-specific
  - packaged manifest metadata used `pbirAnalyzer.explorer` plus explicit untrusted/virtual capability declarations
  - no stale command/view/config identifiers remained in release-facing metadata

## 0.5.1 — 2026-06-06

### Trust Restoration

- Hardened the deterministic fix engine so supported PBIR mutations now resolve against stable page identities and fail closed when page targeting is ambiguous.
- Added schema-correct title mutation support using PBIR container-object storage paths and PBIR literal shaping instead of flat dotted-property writes.
- Kept unsupported mutation families closed by default so the deterministic path only executes against explicitly supported mutation categories.

### Reliability

- Replaced direct fix writes with atomic temp-file plus rename orchestration for single-fix and batch-fix execution.
- Added deterministic rollback-on-failure so failed persistence or failed validation restores pre-apply file content instead of leaving partially mutated reports behind.
- Added post-write mutation validation to prevent silent corruption when written values do not round-trip to the expected PBIR state.
- Documented and enforced the current safe fallback strategy:
  - supported deterministic mutations use atomic validated canonical JSON rewrites when format-preserving surgical patching is not yet available

### Governance And Workflow

- Governance checks now read the report theme directly from PBIR metadata instead of trusting user-entered theme identifiers.
- Screenshot upload now opens the active score panel and triggers the intended upload workflow instead of re-running report scoring.

### Safety Tests

- Expanded deterministic fix coverage for:
  - schema-correct title writes
  - stale-target detection after title drift
  - duplicate display-name ambiguity fail-closed behavior
  - batch persistence failure with rollback protection
  - PBIR-derived governance theme verification
  - screenshot-upload command routing

### Validation

- Passed:
  - `cd vscode-extension && npx jest src/test/fixMutationPlanner.test.ts src/test/fixOpportunityBuilder.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/fixApplyEngine.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/fixMutationPlanner.test.ts src/test/fixApplyEngine.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/fixMutationPlanner.test.ts src/test/fixOpportunityBuilder.test.ts src/test/fixApplyEngine.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/pbirGovernanceCommand.test.ts src/test/pbirUploadScreenshotsCommand.test.ts --runInBand`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## 0.5.0 — 2026-06-05

### Release Position

- First cross-platform Analytics Experience Review Platform release
- Shared review workspace for PBIR reports and analytical Fabric Apps
- Story Assessment, Issues, Fix Plan, Evidence, Fabric App Readiness, Fabric App Review, and AI Proposal Enrichment positioned as one integrated platform

### New

- Fabric App Readiness Assessment
- Fabric App Review Mode foundations
- screenshot evidence
- semantic-model evidence
- analyzable surface architecture
- surface discovery
- analyzer registry
- analyzer profiles

### Improved

- cross-platform VSIX packaging for Windows x64, Windows arm64, Linux x64, macOS x64, and macOS arm64
- backend startup reliability
- runtime detection
- degraded-mode messaging
- richer evidence-driven review across metadata, navigation, screenshots, semantic-model usage, and code-derived signals
- backend startup diagnostics now report selected target, resolved backend path, runtime packaging mode, runtime detection, preflight exit details, and handshake failure reasons
- VSIX packaging now stages each target in isolation with a build lock, preventing backend cross-contamination across target artifacts
- Windows arm64 now ships as a self-contained backend target for `0.5.0`

### Safety

- deterministic fix-engine hardening
- safer mutation planning with unsafe title and semantic-color writes held back until schema-correct support exists
- severity outcome correction
- real backend readiness handshake and backend state monitoring
- stable single-page PBIR routing by page name

### Validation

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package:all`
- Built and inspected platform-targeted VSIX artifacts:
  - Windows x64
  - Windows arm64
  - Linux x64
  - macOS x64
  - macOS arm64
- Remaining runtime verification gap:
  - live backend startup on Windows x64, Linux x64, and macOS x64 was not executed locally in the macOS arm64 release-prep session
- Windows arm64 release decision:
  - keep the self-contained Windows arm64 target in the final `0.5.0` package set because fresh scoring succeeded on Windows 11 ARM after the backend runtime strategy change
  - document the larger package size as intentional because the backend bundles the .NET runtime for that target

### Packaging Notes

- final `0.5.0` manual upload set:
  - `pbir-design-analyzer-0.5.0-win32-x64.vsix`
  - `pbir-design-analyzer-0.5.0-win32-arm64.vsix`
  - `pbir-design-analyzer-0.5.0-linux-x64.vsix`
  - `pbir-design-analyzer-0.5.0-darwin-x64.vsix`
  - `pbir-design-analyzer-0.5.0-darwin-arm64.vsix`
- the icon PNG remains transparent in source and in the packaged extension; a light tile in the VS Code extension details page is treated as VS Code rendering behavior, not a packaging defect

## 0.4.0 — 2026-06-02

### AI Proposal Enrichment Phase 3

- Added an advisory-only `proposalEnrichments` layer to the score-panel result contract so remediation items can carry grounded explanation, priority, expected-outcome, title-suggestion, and alternative-guidance content without changing score semantics or deterministic execution authority.
- Added bounded Phase 3 enrichment modules for:
  - grounded remediation context building
  - advisory provider abstraction
  - hallucination and execution-leak validation
  - deterministic fallback wording
  - non-blocking orchestration back into the existing Fix Plan workflow
- Wired the score-panel host to populate fallback-safe proposal enrichment content while keeping preview/apply/rollback, mutation planning, and post-apply outcome reporting unchanged.
- Updated the Fix Plan webview to render clearly labeled `AI-enriched guidance` separately from deterministic opportunities and actual apply outcomes.
- Preserved the trust boundary:
  - AI enriches proposals only
  - AI does not generate mutations
  - AI does not apply mutations
  - deterministic preview/apply/rollback behavior is unchanged

### AI Fix Phase 2 Hardening

- Added deterministic compatibility evaluation for multi-opportunity fix selection, including machine-readable and user-readable reasons for overlapping mutations, incompatible categories, stale opportunities, target drift, and missing rollback coverage.
- Added grouped preview payloads and UI that summarize selected opportunities by page, object, property, changed files, changed objects, mutation facts, and expected outcomes.
- Added deterministic batch apply orchestration with validate → backup → ordered apply → session record → re-analysis flow, plus all-or-nothing blocking when one selected opportunity is stale or conflicts.
- Added session history and rollback visibility for grouped apply runs, including grouped outcome summaries, rollback history, and regeneration/stale messaging.
- Preserved the deterministic trust boundary: no model calls, no provider integration, no AI-generated mutations, and no scoring or normalized-finding semantic changes.

### Validation

- Passed:
  - `cd vscode-extension && npx jest --runInBand src/test/proposalEnrichmentContextBuilder.test.ts src/test/proposalEnrichmentValidators.test.ts src/test/proposalEnrichmentOrchestrator.test.ts src/test/scoreResultPayload.test.ts`
  - `cd vscode-extension && npx jest -c jest.webview.config.cjs --runInBand webview-src/analyzer-score/proposalEnrichment.test.ts webview-src/analyzer-score/App.test.tsx`
- Passed:
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npx jest --runInBand src/test/fixCompatibility.test.ts src/test/fixBatchPreview.test.ts src/test/fixApplyEngine.test.ts src/test/fixOutcomeEvaluator.test.ts src/test/fixSessionHistory.test.ts src/test/scoreResultPayload.test.ts`
  - `cd vscode-extension && npx jest -c jest.webview.config.cjs --runInBand webview-src/analyzer-score/App.test.tsx`
  - `cd vscode-extension && npx eslint src/analyzer/contracts/scorePanel.ts src/analyzer/fixes/fixCompatibility.ts src/analyzer/fixes/fixBatchPreview.ts src/analyzer/fixes/fixApplyEngine.ts src/analyzer/fixes/fixOutcomeEvaluator.ts src/analyzer/fixes/fixSessionHistory.ts src/views/PbirScorePanel.ts src/views/scoreResultPayload.ts src/test/fixCompatibility.test.ts src/test/fixBatchPreview.test.ts src/test/fixApplyEngine.test.ts src/test/fixOutcomeEvaluator.test.ts src/test/fixSessionHistory.test.ts src/test/scoreResultPayload.test.ts --ext ts && npx eslint webview-src/analyzer-score/App.tsx webview-src/analyzer-score/App.test.tsx --ext ts,tsx`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package`
- Smoke validated:
  - packaged extension installed into an isolated VS Code profile
  - deterministic grouped preview, apply, rollback, and session recording still worked unchanged

## 0.3.1 — 2026-06-01

### Single-Page Planner Follow-Up

- Fixed the deterministic fix planner so page-level scoring can generate supported opportunities from top-level `scoredPageName + visualMetadata` when `pageScores` are absent.
- Kept report-level planning behavior intact while restoring page-level Phase 1 opportunities for real single-page analysis.
- Updated advisory-only copy so unsupported remediation states more honestly that no safe metadata-only fix is currently available.

### AI Fix Roadmap Clarity

- Documented the staged AI-fix roadmap:
  - Phase 1 deterministic fix opportunity engine
  - Phase 2 preview/apply/rollback hardening
  - Phase 3 AI-assisted proposal enrichment
  - Phase 4 advanced AI refactoring
- Reasserted the permanent execution trust boundary and deterministic mutation layer principle in roadmap-facing documentation.

### Validation

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm run package`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- Smoke-tested the packaged extension in an isolated VS Code profile against the real `Sales & Production` PBIR fixture to verify:
  - full-report remediation remains honestly advisory when Phase 1 does not support the emitted remediation family
  - page-level `Net Sales` planning can consume top-level single-page metadata and surface supported deterministic opportunities
  - supported opportunities still expose preview/apply/rollback when safe mutations exist
  - unsupported remediation remains advisory with no webview-specific renderer errors observed
## 0.3.0 — 2026-06-01

### Deterministic Fix Opportunity Engine

- Extended the workspace from `Analyze → Recommend` to `Analyze → Recommend → Fix → Validate` for supported remediation domains.
- Added remediation-led deterministic fix opportunities for safe existing-object mutations such as title normalization, alignment normalization, navigation placement normalization, semantic color normalization, and cross-page consistency normalization.
- Kept fix generation under `Fix Plan` remediation items rather than generating fixes directly from individual findings.

### Preview, Apply, Rollback, And Re-Analysis

- Added structured preview rows that show exact `Object`, `Property`, `Before`, and `After` mutation details before apply.
- Added explicit fix-opportunity lifecycle states:
  - `Previewed`
  - `Approved`
  - `Applied`
  - `Rolled back`
  - `Stale`
  - `Failed validation`
  - `Applied with unexpected outcome`
- Added validation-first apply behavior so stale opportunities are blocked instead of being partially applied.
- Added deterministic rollback plans backed by recorded file-content backups.
- Added automatic re-analysis and post-apply outcome reporting with:
  - `Resolved`
  - `Improved`
  - `Unchanged`
  - `Unexpected`

### Trust And Workflow Improvements

- Kept unsupported remediation items advisory rather than inventing unsafe or opaque fixes.
- Preserved the active report/page tab across refresh-driven re-analysis so apply and rollback do not force the user back to the default tab.
- Preserved scores, severities, confidences, normalized findings, personas, and backend scoring semantics.

### Validation

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npx eslint webview-src/analyzer-score/App.tsx webview-src/analyzer-score/App.test.tsx src/views/PbirScorePanel.ts src/views/scoreResultPayload.ts src/analyzer/fixes/*.ts src/test/fixOpportunityBuilder.test.ts src/test/fixApplyEngine.test.ts src/test/fixOutcomeEvaluator.test.ts`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- Smoke-tested the packaged extension in an isolated VS Code profile:
  - against the real `Sales & Production` PBIR report for score-panel open, advisory remediation behavior, and renderer-log review
  - against a concrete PBIR fix-opportunity fixture for preview, apply, re-analysis, rollback, and unexpected-outcome handling

## 0.2.2 — 2026-05-31

### Context-Aware Remediation Queue

- Made `Fix Plan` context-aware so remediation actions derive from the selected page and problem area rather than mirroring the exact visible issue slice.
- Treated `Page`, `Dimension`, and `Impact` as remediation-driving filters while keeping `Severity`, `Scope`, and `Detection` diagnosis-only for queue generation.
- Added explicit `Remediation Focus` messaging so users can see why the queue differs from the current issue list.

### Remediation Traceability

- Added finding-coverage summaries such as `1 High · 2 Medium` so remediation actions explain why related medium-severity work can still appear while reviewing a high-severity slice.
- Improved source-finding traceability in the queue by listing the underlying finding titles and severity mix for each remediation action.
- Kept empty remediation domains visible with explanatory scope copy instead of dropping the section entirely.

### Validation

- Preserved scores, severity semantics, confidence semantics, normalized findings, personas, and benchmark/scoring logic.
- Passed targeted Jest coverage for context-aware remediation derivation and the analyzer score webview.

## 0.2.1 — 2026-05-31

### UX Architecture Consolidation

- Consolidated fragmented page-purpose reasoning into a single summary-first `Page Purpose Analysis` workflow.
- Added a presentation-only `Why This Matters` narrative so business impact is easier to understand before diving into framework detail.
- Preserved inferred story, intent profile, actionability, benchmark comparison, and intent feedback behavior behind explicit reasoning expansion.

### Remediation Queue And Reading Path

- Reworked Fix Plan into a true remediation queue instead of a second issue list.
- Added grouped actions, impact, effort, action-specific rationale, resolved outcomes, and source-finding traceability.
- Strengthened the intended reading path:
  - Overview Summary
  - Page Purpose Analysis
  - Issues
  - Fix Plan
  - Evidence
  - Export

### Context-Aware Matrix

- Kept the full matrix in report-level Overview mode.
- Narrowed the matrix to the selected page in page-review context.
- Made matrix cells status-first with `Strong`, `Watch`, `Weak`, and `Unknown`, while keeping counts as supporting detail.

### Validation

- Preserved scoring, severity, confidence, normalized findings, personas, and benchmark/scoring semantics.
- Passed compile and full Jest validation for the extension and score-panel webview.

## 0.2.0 — 2026-05-31

### Workspace Modernization

- Rebuilt the score panel into a review workspace with:
  - Overview
  - Issues
  - Fix Plan
  - Evidence
  - secondary Export
- Added smart collapse behavior so dense supporting sections do not dominate the default reading path.

### Normalized Findings And Review Workflow

- Added a normalized findings model so issues from multiple scoring/evidence subsystems render through a consistent triage contract.
- Made Issues the primary review surface with filtering and grouping controls.
- Added intent confirmation and review feedback workflows without mutating scores.

### Overview, Fix Plan, And Evidence

- Added presentation-only overview summaries, strengths/weaknesses rollups, top issues, and top actions.
- Added Fix Plan remediation sequencing from existing findings and recommendations.
- Moved framework analysis, metadata, screenshot audit, scoring internals, and packet preview into Evidence-oriented drilldown.

### Personas And Cross-Page Navigation

- Added workspace review modes: Default, Executive, Consultant, Governance, Accessibility.
- Kept workspace review modes presentation-only; they do not change scores, severity, or confidence.
- Replaced the lightweight count grid with a page-by-dimension cross-page matrix that filters Issues directly.

### Export And Review Packet Positioning

- Preserved review packet preview and export workflows while keeping Export downstream from the main review path.
- Continued support for current Markdown/HTML/PDF/JSON review export flows and consultant-style packet rendering.

### Known Limitations

- Persona defaults are heuristic and single-value rather than a second scoring model.
- Matrix dimension filters map to grouped impact areas in the UI layer.
- Export remains downstream rather than a dedicated top-level workspace.
- Screenshot overlays, AI-generated executive narrative, and advanced enterprise-governance workflows are planned for future versions, not implemented in `0.2.0`.

### Roadmap References

- `docs/ROADMAP.md`
- `docs/superpowers/specs/2026-05-31-consultant-deliverables-export-platform-design.md`
- `docs/superpowers/specs/2026-05-31-visual-intelligence-screenshot-analysis-design.md`
- `docs/superpowers/specs/2026-05-31-enterprise-governance-advanced-review-design.md`

## 0.1.13 — 2026-05-24

- Technical debt: renamed `LspHost/` → `RpcHost/` and `LSPModelService`/`LSPState` → `AnalyzerBridgeService`/`BridgeState` throughout — the transport is JSON-RPC over stdio, not Language Server Protocol.
- Technical debt: renamed `LspHostJsonRpcTests.cs` → `RpcHostJsonRpcTests.cs` and `ScoreResultModelTests.cs` (was a duplicate name for the data-model contract test file).
- Technical debt: removed dead `ColWidthPx`/`RowHeightPx` constants from `PbirScoringService`; fixed one stray `CanvasHeight` constant reference that should have used the per-page `canvasHeight` local variable.

- Added finding classification badges (`Objective`, `Heuristic`, `Style`) to score feedback.
- Expanded the analyzer heuristics across metadata extraction, narrative, hierarchy, chart semantics, and visual consistency.
- Bumped the extension to `0.1.13`.
- Public release docs updated to include the MIT license and changelog references.
- README headers now show the extension logo inline before the product title.
- Spec: corrected `PBIR_ANALYZER_V1_SPEC.md` default framework weights and framework set to match the canonical store defaults. Enterprise Governance is now correctly documented as optional/default-disabled. Default enabled weights are Gestalt 30, Cognitive 20, Data-Ink 15, Accessibility 15, Visual 20.
- Governance: `requirePageTitle` and the Enterprise Governance framework's page-title sub-criterion now apply a strict visible-title check — the title must be positioned in the top 15% of the canvas and must not be a vague placeholder such as `Page 1` or `Overview`. The `PageVisualMetadataSummary` payload exposes a new `strictVisiblePageTitle` field so panels can show which pages satisfy the strict rule.
- Governance: workspace-policy dynamic governance rules are now evaluated end-to-end. The 8 enforceable defaults (`maxVisualsPerPage`, `maxHiddenVisuals`, `minWhiteSpaceRatio`, `allowPieCharts`, `allowCustomVisuals`, `requirePageTitle`, `requireFilterPanel`, `themeStandard`) block publishing with specific reasons when violated. `maxBookmarksPerPage` and `maxLayoutStatesPerPage` are recognized but deferred until per-page bookmark state scoring lands.
- Accessibility: scoring now combines three sub-criteria — theme-palette contrast against white (40 pts), per-visual on-canvas text contrast using actual background/font colours when available (40 pts), and a deuteranopia-aware red/green pair check on the theme palette (20 pts). On-canvas failures cite the affected visuals so users can locate them in the explorer.
- Scoring: bookmark-aware per-state page scoring. Pages with bookmarks are now scored once per layout state (Default + one per bookmark) and the page composite is the average of the per-state composites. The per-state breakdown is surfaced on `ScoreResult.PerStateScores` (single-page mode) and `PageScore.PerStateScores` (report mode) so panels can show how each bookmark view affects the score.
- Configuration: introduced audience presets. Three bundled presets — Executive, Operational, Analyst — overlay sensible thresholds (`maxVisualsPerPage`, `maxHiddenVisuals`, `minWhiteSpaceRatio`, navigation scoring weight) onto the analyzer configuration in one click. Individual fields can still be tuned afterward.
- Performance: per-page scoring in the full-report path now runs in parallel (`Parallel.ForEach`, capped at 4 concurrent pages). Display order is preserved and behaviour is unchanged; large reports score noticeably faster.
- Quick fixes: the score panel now derives structured quick-fix suggestions from per-framework feedback items (in addition to recommendation strings). Four new fix types — `ConsolidateFilters`, `NormalizeCardAlignment`, `ReplaceDonutWithBar`, `StandardizeLabelNaming` — carry affected-visual references so users can locate the visuals that need adjustment.
- Export: new `PBIR Design Analyzer: Export Governance Report` command writes a combined score + governance summary to Markdown (suitable for PR comments) or JSON (suitable for CI/CD ingestion). Both formats include composite score, per-framework scores, governance pass/fail, and blocked reasons.
- Narrative scoring: sub-criteria now include visible page-purpose detection, KPI comparison context (prior-period delta, target reference, or trend sparkline), supporting evidence flow (explanatory chart near KPI cluster), and overview-to-detail readability (first page sparser than later pages). These were introduced as part of the enhanced-metadata branch and are active in this release.
- Chart semantics: three new findings active from the enhanced-metadata branch — categorical line chart detection (line chart whose X-axis field role hints at a non-temporal category), missing comparison on KPI-heavy pages (≥2 KPI cards with no bar/column visual), and redundant axis + data-labels (visual with both data labels and full axis labelling). Findings require the `FieldRoleHint`, `HasDataLabels`, and `HasAxisLabels` metadata fields introduced in this release cycle.
- Visual Audit Mode (Phase 1): users can upload report-page screenshots and receive AI-assisted visual findings aligned to PBIR pages. Screenshots are copied into extension `globalStorageUri` and persist across VS Code reloads. Auto-matching assigns screenshots to pages by normalized filename (leading numbers, state suffixes, and punctuation stripped); unmatched captures land in a review queue for manual assignment. An Anthropic Claude provider (`claude-haiku-4-5-20251001`) delivers structured findings (`findingType`, `severity`, `confidence`, `text`, `recommendation`, optional `regionHint`) per screenshot, rendered as a non-scored evidence layer in the score panel. API key is stored in VS Code `SecretStorage` via the new "Configure Visual Audit Provider" command. New commands: `PBIR Design Analyzer: Upload Report Screenshots`, `PBIR Design Analyzer: Configure Visual Audit Provider`.
- Telemetry: privacy-respecting telemetry wrapper implemented and wired to key events — `command.invoked` (score report, governance check), `scoring.completed` (page count, bucketed composite score, duration), and `governance.evaluated` (blocked flag, reason count). All calls are no-ops when VS Code's `telemetry.telemetryLevel` is `off` or when no instrumentation key is configured. No PII, file paths, or visual content is ever emitted.
- Visual Audit: added OpenAI GPT-4o Vision as a second supported AI provider alongside Anthropic Claude. The "Configure AI Provider" button and command palette entry now prompt you to select a provider first (Anthropic or OpenAI), then enter the matching API key. Provider choice and API keys are both stored in VS Code `SecretStorage`. The active provider is remembered across sessions and shown in the audit coverage card.
- Score panel UI: the summary card now shows an inline framework score bar list and recommendations directly below the composite score chip, so the key findings are visible without scrolling.
- Score panel UI: the **Parsed Visual Metadata** section is now collapsible — it starts closed and can be expanded via the caret next to the heading, keeping the recommendations and framework cards visible by default.
- Score panel UI: each framework card now shows a caret indicator directly beside the framework name, making the expand/collapse affordance immediately obvious.
- Score panel UI: the **Reveal** button in the Parsed Visual Metadata visual list now opens the visual's PBIR JSON file in the editor (previously it only selected the tree node without opening the file).
- Score panel UI: Visual Audit Coverage stats (`N of N pages covered`, `N pages with findings`, etc.) are now correctly spaced — the count and label were running together without a gap.

## 0.1.11

- Removed legacy PBIR refactor plumbing from the public release surface.
- Bundled the extension host to shrink the VSIX package.
- Updated extension dependencies and cleared known npm audit findings.
- Bumped the extension to `0.1.11`.

## 0.1.10

- Removed the public theme import surface.
- Bumped the extension to `0.1.10`.

## 0.1.9

- Added the drilldown score breakdown experience.
- Bumped the extension to `0.1.9`.
