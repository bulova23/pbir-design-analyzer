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
