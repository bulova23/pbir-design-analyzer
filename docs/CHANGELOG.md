# Changelog

All notable changes to PBIR Design Analyzer are recorded here.

## Unreleased

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
