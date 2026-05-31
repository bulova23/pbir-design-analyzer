# PBIR Design Analyzer VS Code Extension

PBIR Design Analyzer helps you review local Power BI PBIP/PBIR reports in VS Code before they are shared or published. The extension now centers on an issue-centric review workspace rather than a long framework-first report.

## What’s New In 0.2.0

- Overview workspace for executive triage
- Issues workspace powered by normalized findings
- Fix Plan remediation queue
- Evidence moved behind secondary drilldown
- smart collapse defaults
- persona review modes
- cross-page matrix navigation
- review packet preview/export kept downstream from analysis

## Installation

Install the packaged VSIX or build locally from this repository.

Local package build:

```bash
npm install
npm run build
npm run package
```

## Core Commands

- `PBIR Design Analyzer: Open PBIP Project`
- `PBIR Design Analyzer: Refresh Reports`
- `PBIR Design Analyzer: Score Report`
- `PBIR Design Analyzer: Configure Scoring`
- `PBIR Design Analyzer: Check Governance`
- `PBIR Design Analyzer: Export Governance Report`
- `PBIR Design Analyzer: Export Review Workflow Summary`
- `PBIR Design Analyzer: Upload Report Screenshots`
- `PBIR Design Analyzer: Configure Visual Audit Provider`

## Settings

Current shared settings focus on governance:

- `powerbi-modeling.governance.enabled`
- `powerbi-modeling.governance.minimumCompositeScore`
- `powerbi-modeling.governance.approvedThemeIds`

Analyzer scoring preferences and workspace behavior are managed through the extension panels rather than a large static settings surface.

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

## Detailed Guidance

See the repo-level user guide for the full walkthrough:

- [docs/HOW_TO_USE.md](../docs/HOW_TO_USE.md)
