# PBIR Design Analyzer

![PBIR Design Analyzer logo](resources/icon.png)

PBIR Design Analyzer is a focused VS Code extension for reviewing local Power BI PBIP/PBIR report projects before they are shared, governed, or published. It analyzes report pages, visuals, navigation, hidden states, theming, and design consistency so authors can improve report quality with a repeatable workflow.

## What It Analyzes

- PBIP/PBIR report structure, page order, visuals, bookmarks, and navigation controls
- page layout quality, grouping, spacing, alignment, and information density
- chart selection, labeling, decorative ink, and visual consistency
- contrast and readability signals that affect accessibility coverage
- storytelling flow from headline metrics through trends, comparisons, and drill paths
- enterprise governance checks that reflect team or corporate design policy

## Design Principles In The Configuration Panel

- Gestalt Principles. Evaluates grouping, alignment, proximity, similarity, and continuity across report layouts.
- Cognitive Load. Measures visual density, competing signals, and the mental effort required to interpret a page.
- Data-Ink Ratio. Rewards visuals that maximize data signal and minimize decorative or redundant ink.
- Graphical Perception. Evaluates whether chart encodings match how accurately people compare quantitative values.
- Accessibility (WCAG). Checks contrast, readability, and reporting choices that improve accessibility coverage.
- Visual Best Practices. Applies dashboard design guidance around chart choice, labeling, and consistency.
- Enterprise Governance. Scores reports against team or enterprise design standards and publishing policy. This is the principle intended for corporate-dictated rules such as approved themes, maximum visuals or bookmarks, required page titles, filter panel expectations, pie chart policy, custom visual policy, and similar internal standards.
- Stephen Few Principles. Applies Stephen Few dashboard heuristics such as KPI prominence and one-screen density.
- Tufte Minimalism. Emphasizes clarity, precision, and minimal chart junk in report presentation.
- Dashboard Density. Evaluates balance between information richness and crowding on each report page.
- Narrative Design. Evaluates how well page sequencing and layout guide a user through the report story.

## Sidecar Toolbar Guide

- Folder icon - Open PBIP Project. Open a local PBIP project or Report folder and populate the explorer tree.
- Refresh icon - Refresh Reports. Re-scan report metadata after file edits or Power BI Desktop saves.
- Chart icon - Score Report. Run the design analysis for the selected report or page and open the PBIR Optimization Report.
- Gear icon - Configure Scoring. Enable or disable principles, rebalance weights, tune navigation scoring, and review governance defaults.
- Shield icon - Check Governance. Evaluate the current report against minimum score thresholds, approved theme expectations, and enterprise rules.

## Typical Workflow

1. Open a PBIP project or Report folder.
2. Refresh after external edits or after saving in Power BI Desktop.
3. Score the full report to review overall and page-level results.
4. Adjust principle weights and governance settings if your team uses a custom review standard.
5. Re-score and run governance checks before publish or handoff.

## Detailed Usage Guide

For full setup, workflow, scoring interpretation, and design principle details, see [How To Use PBIR Design Analyzer](../docs/HOW_TO_USE.md).

## Requirements

- VS Code 1.93+
- .NET 8 available on the machine for the packaged analyzer backend
- a local Power BI project using PBIP/PBIR files

## Scope

This package is intentionally narrow. It is built for local PBIR design analysis and governance workflows, not general Fabric authoring, TMDL editing, or live service management.

## Feedback And Issues

Submit bugs, feature requests, support questions, and documentation fixes in the GitHub repo:

- [Issue forms and request tracking](https://github.com/bulova23/pbir-design-analyzer/issues/new/choose)
- [Project repository](https://github.com/bulova23/pbir-design-analyzer)
