# PBIR Design Analyzer VS Code Extension

PBIR Design Analyzer is a focused VS Code extension for reviewing local Power BI PBIP/PBIR report projects before they are shared, governed, or published. It analyzes report pages, visuals, navigation, hidden states, theming, and design consistency so authors can improve report quality with a repeatable workflow.

Documentation: [How To Use PBIR Design Analyzer](../docs/HOW_TO_USE.md)

## Quick Start

1. Open a local PBIP project or `.Report` folder.
2. Run PBIR Design Analyzer: Score Report.
3. Review the Overview workspace, Issues, Fix Plan, and Evidence sections.
4. Open PBIR Design Analyzer: Configure Scoring if you want to tune framework weights or navigation treatment.
5. Use PBIR Design Analyzer: Check Governance only when your workspace has shared governance enabled.

## What’s New In 0.2.2

- `Fix Plan` now shows an explicit `Remediation Focus` so users can see the selected page and problem area driving the queue
- remediation actions derive from `Page`, `Dimension`, and `Impact` instead of mirroring every issue filter exactly
- `Severity`, `Scope`, and `Detection` still refine `Issues`, but they no longer hard-prune related remediation work
- each remediation action now shows finding coverage such as `1 High · 2 Medium` plus clearer source-finding traceability

## What It Analyzes

- PBIP/PBIR report structure, page order, visuals, bookmarks, and navigation controls
- page layout quality, grouping, spacing, alignment, and information density
- chart selection, labeling, decorative ink, and visual consistency
- contrast and readability signals that affect accessibility coverage
- storytelling flow from headline metrics through trends, comparisons, and drill paths
- optional enterprise governance scoring plus opt-in workspace governance checks

## Design Principles In The Configuration Panel

- Gestalt Principles. Evaluates grouping, alignment, proximity, similarity, and continuity across report layouts.
- Cognitive Load. Measures visual density, competing signals, and the mental effort required to interpret a page.
- Data-Ink Ratio. Rewards visuals that maximize data signal and minimize decorative or redundant ink.
- Graphical Perception. Evaluates whether chart encodings match how accurately people compare quantitative values.
- Accessibility (WCAG). Checks contrast, readability, and reporting choices that improve accessibility coverage.
- Visual Best Practices. Applies dashboard design guidance around chart choice, labeling, and consistency.
- Enterprise Governance. An optional scoring framework for team or enterprise design standards. Workspace publish governance is configured separately and is disabled by default until explicitly enabled.
- Stephen Few Principles. Applies Stephen Few dashboard heuristics such as KPI prominence and one-screen density.
- Tufte Minimalism. Emphasizes clarity, precision, and minimal chart junk in report presentation.
- Dashboard Density. Evaluates balance between information richness and crowding on each report page.
- Narrative Design. Evaluates how well page sequencing and layout guide a user through the report story.

## Sidecar Toolbar Guide

- Folder icon - Open PBIP Project. Open a local PBIP project or Report folder and populate the explorer tree.
- Refresh icon - Refresh Reports. Re-scan report metadata after file edits or Power BI Desktop saves.
- Chart icon - Score Report. Run the design analysis for the selected report or page and open the PBIR Optimization Report.
- Gear icon - Configure Scoring. Enable or disable principles, rebalance weights, tune navigation scoring, and review governance defaults.
- Shield icon - Check Governance. Evaluate the current report against workspace governance only when a shared governance policy is enabled.

## Score Panel Walkthrough

### Overview

Use Overview first for:

- overall score
- maturity/risk framing
- strengths and weaknesses
- top issues
- top actions
- cross-page summary
- cross-page matrix navigation

### Issues

Use Issues as the main review surface. The workspace supports filtering and grouping across normalized findings instead of forcing the user through framework cards first.

### Fix Plan

Use Fix Plan when you want an action-oriented remediation queue rather than raw findings.

### Evidence

Use Evidence for:

- Design Framework Analysis
- metadata inspection
- AI Screenshot Audit
- scoring internals
- review packet preview

### Export

Export remains a downstream action. The current release keeps review packet preview and export available without letting export dominate the main analysis path.

## Review Workflow

Recommended flow:

1. score the report
2. review Overview
3. triage Issues
4. sequence Fix Plan work
5. inspect Evidence when needed
6. export/share after review

## Review Modes

The workspace review modes are:

- Default
- Executive
- Consultant
- Governance
- Accessibility

These reorder and emphasize findings and actions. They do not change score values or backend scoring behavior.

## Cross-Page Matrix

The Overview matrix shows page rows and review-dimension columns. Clicking a cell filters Issues to the selected page and dimension and keeps the current review mode active.

## Typical Workflow

1. Open a PBIP project or `.Report` folder.
2. Refresh after external edits or after saving in Power BI Desktop.
3. Score the full report to review overall and page-level results.
4. Adjust principle weights and governance settings if your team uses a custom review standard.
5. Re-score and run governance checks before publish or handoff.

## Core Commands

- PBIR Design Analyzer: Open PBIP Project
- PBIR Design Analyzer: Refresh Reports
- PBIR Design Analyzer: Score Report
- PBIR Design Analyzer: Configure Scoring
- PBIR Design Analyzer: Check Governance
- PBIR Design Analyzer: Export Governance Report
- PBIR Design Analyzer: Export Review Workflow Summary
- PBIR Design Analyzer: Upload Report Screenshots
- PBIR Design Analyzer: Configure Visual Audit Provider

## Settings

Current shared settings focus on governance:

- powerbi-modeling.governance.enabled
- powerbi-modeling.governance.minimumCompositeScore
- powerbi-modeling.governance.approvedThemeIds

Analyzer scoring preferences and workspace behavior are managed through the extension panels rather than a large static settings surface.

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

- [Issue forms and request tracking](https://github.com/bulova23/pbir-design-analyzer/issues)
- [Project repository](https://github.com/bulova23/pbir-design-analyzer)
