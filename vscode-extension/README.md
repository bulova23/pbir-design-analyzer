# PBIR Design Analyzer

PBIR Design Analyzer 0.6.0 is the current cross-platform Analytics Experience Review Platform release.

It helps consultants, BI architects, analytics teams, Power BI developers, and Fabric developers review PBIR reports and analytical Fabric Apps through one shared workspace.

Documentation: [How To Use PBIR Design Analyzer](../docs/HOW_TO_USE.md)

## Why Teams Use It

Most report-review tools stop at scorecards or metadata checks.

PBIR Design Analyzer is designed for real review work:

- **Story Assessment** to show what a page or app experience is trying to communicate, whether the message succeeds, and what gets in the way
- **Issues Workspace** to surface findings across design quality, usability, accessibility, actionability, consistency, navigation, and governance
- **Fix Plan** to turn findings into remediation steps, advisory recommendations, and deterministic fix opportunities where safe support exists
- **Evidence** so recommendations stay tied to metadata, navigation, screenshots, semantic-model usage, and other supporting evidence
- **Fabric App Readiness** to assess which PBIR assets are strong candidates for Fabric App evolution and which ones need redesign first
- **Fabric App Review** to review analytical Fabric Apps through the same workspace
- **AI Proposal Enrichment** to provide clearer explanations, prioritization guidance, business rationale, and expected outcomes without bypassing deterministic execution
- **Cross-platform support** so the same workspace is available on Windows x64, Windows arm64, Linux x64, macOS x64, and macOS arm64

## What’s New In 0.6.0

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

- cross-platform support
- deterministic score diagnostics
- backend startup reliability
- runtime detection
- degraded-mode messaging

### Safety

- deterministic fix-engine hardening
- safer mutation planning
- severity outcome correction

## What The Platform Reviews

### Story Assessment

- page purpose and audience fit
- headline-to-evidence flow
- summary-to-detail sequencing
- analytical clarity and actionability

### Issues Workspace

- layout and density problems
- navigation and drill-path friction
- accessibility and readability concerns
- design consistency gaps
- weak benchmark or decision-support framing
- governance concerns

### Fix Plan

- remediation guidance tied to findings
- advisory recommendations that explain why a change matters
- deterministic fix opportunities for supported scenarios
- preview, apply, rollback, and re-analysis for the deterministic execution path

### Evidence

- metadata evidence
- navigation evidence
- screenshot evidence
- semantic-model evidence
- code-derived evidence
- framework analysis and supporting review detail

### Fabric App Readiness

- migration candidates
- blockers and unsupported patterns
- redesign effort
- Fabric App suitability

### Fabric App Review

- analytical Fabric App structure and experience quality
- navigation clarity
- design-token evidence
- screenshot-backed review
- semantic-model usage evidence

## Quick Start

1. Open a local PBIP project or `.Report` folder.
2. Run PBIR Design Analyzer: Score Report.
3. Start in Story Assessment and Overview to understand overall quality, top risks, and story health.
4. Use Issues to triage findings by severity, page, dimension, and scope.
5. Use Fix Plan to sequence remediation and apply supported deterministic fixes where available.
6. Use Evidence to inspect proof, supporting signals, and migration-readiness rationale.

## Review Workspace

### Story Assessment And Overview

Use Story Assessment and Overview to understand:

- overall analytics experience quality
- top strengths and top risks
- story gaps across the report
- which pages need attention first
- whether the asset is a strong Fabric App candidate

### Issues

Use Issues as the primary review surface when you need to inspect:

- design issues
- usability issues
- accessibility concerns
- navigation problems
- actionability gaps
- governance concerns

### Fix Plan

Use Fix Plan when you need an action-oriented remediation workflow instead of a raw list of findings.

It brings together:

- remediation guidance
- advisory recommendations
- business rationale
- deterministic fix opportunities for supported cases

### Evidence

Use Evidence when you want to verify why a finding or recommendation exists before you act on it.

### Export

Export stays downstream from review so the product remains focused on evaluation first and deliverables second.

## Deterministic Fix Workflow

PBIR Design Analyzer preserves a strict execution boundary.

Supported deterministic fixes can:

- preview exact changes before apply
- support grouped review and approval
- apply safe metadata and layout edits in supported areas
- roll back changes if the outcome is not acceptable
- trigger re-analysis after apply

The platform does not use advisory recommendations to generate or apply freeform mutations.

## Review Modes

The workspace supports review modes for:

- Default
- Executive
- Consultant
- Governance
- Accessibility

These modes change emphasis and presentation. They do not change scoring outcomes.

## Cross-Page Matrix

The Overview matrix helps reviewers move from high-level concerns to the exact page and review dimension that needs attention.

## Core Commands

- PBIR Design Analyzer: Open PBIP Project
- PBIR Design Analyzer: Refresh Reports
- PBIR Design Analyzer: Score Report
- PBIR Design Analyzer: Copy Score Diagnostics
- PBIR Design Analyzer: Configure Scoring
- PBIR Design Analyzer: Check Governance
- PBIR Design Analyzer: Export Governance Report
- PBIR Design Analyzer: Export Review Workflow Summary
- PBIR Design Analyzer: Upload Report Screenshots
- PBIR Design Analyzer: Configure Visual Audit Provider

## Cross-Platform Score Determinism

PBIR Design Analyzer treats cross-platform score determinism as a release requirement.

- the same report fingerprint must produce the same score, issue counts, readiness score, analyzer metadata, and findings on every supported platform
- theme, locale, path separators, newline style, and filesystem traversal order must not change scoring outcomes

Use PBIR Design Analyzer: Copy Score Diagnostics after scoring to capture:

- extension version
- backend version
- platform and architecture
- analyzer type and analyzer profile
- report fingerprint
- page processing order
- finding IDs and evidence counts

The command copies the current score diagnostic JSON to the clipboard and the same payload is written to the PBIR Score Diagnostics output channel.

To compare two captured diagnostics locally:

```bash
cd vscode-extension
node scripts/compare-score-diagnostics.mjs /path/to/first.json /path/to/second.json
```

## Settings

Current shared settings focus on governance:

- `powerbi-modeling.governance.enabled`
- `powerbi-modeling.governance.minimumCompositeScore`
- `powerbi-modeling.governance.approvedThemeIds`

Most review behavior and scoring emphasis are managed through the extension workspace rather than a large static settings surface.

## Requirements

- VS Code 1.93+
- .NET 8 available on the machine for the packaged analyzer backend on supported public targets
- a local Power BI project using PBIP or PBIR files

## Supported Platforms

- Windows x64
- Windows arm64
- Linux x64
- macOS x64
- macOS arm64

Install the VSIX that matches your operating system and architecture. Each package includes the correct backend binary for its target platform.

Runtime expectation for the public `0.6.0` packages:

- Windows x64 requires the matching .NET 8 runtime
- Windows arm64 ships with a self-contained backend for `0.6.0`
- Linux x64 requires the matching .NET 8 runtime
- macOS x64 requires the matching .NET 8 runtime
- macOS arm64 requires the matching .NET 8 runtime

The Windows arm64 package is intentionally larger than the other target-specific VSIX files because it bundles the .NET runtime inside the backend payload for startup reliability on Windows 11 ARM.

If the backend cannot be found or started, the extension enters degraded mode:

- local PBIR tree browsing still works
- score, governance, and backend-dependent review commands stay unavailable
- the status bar and startup messages explain what is missing

## Final 0.6.0 VSIX Files

The final `0.6.0` package set includes:

- `pbir-design-analyzer-0.6.0-win32-x64.vsix`
- `pbir-design-analyzer-0.6.0-win32-arm64.vsix`
- `pbir-design-analyzer-0.6.0-linux-x64.vsix`
- `pbir-design-analyzer-0.6.0-darwin-x64.vsix`
- `pbir-design-analyzer-0.6.0-darwin-arm64.vsix`

Install the exact file that matches the target operating system and architecture.

## Icon Rendering Note

The icon source PNG is transparent and the packaged icon should match it byte-for-byte.

If VS Code shows the icon on a light tile in the extension details page, treat that as VS Code rendering behavior rather than a package defect.

## Manual Marketplace Publishing

`0.6.0` is prepared for manual Marketplace upload by the release owner.

Keep all five target-specific VSIX files together for the same extension version and manually upload the full set for the `0.6.0` listing.

## Scope

PBIR Design Analyzer is built for local analytics experience review, governance review, and migration-readiness assessment. It is not positioned as a general Fabric authoring tool, TMDL editor, or live service-management console.

## Interactive Authoring

The authoring workflow currently supports one user-facing mutation: Rename Page.

Use the following flow:

- Import a supported PBIR report.
- Run Rename Page from the Command Palette.
- Select a page and enter its new display name.
- Review the backend-generated semantic preview.
- Confirm or cancel the mutation.
- Review the analyzer score before and after the rename.

The original imported snapshot remains unchanged. A successful mutation returns a new opaque artifact handle. Preview does not materialize a report, and same-name renames are deterministic no-ops. Other mutation kinds, undo/redo, graphical editing, and raw JSON editing are not exposed.

## Detailed Usage Guide

For setup, workflow details, scoring interpretation, and review guidance, see [How To Use PBIR Design Analyzer](../docs/HOW_TO_USE.md).

## Feedback And Issues

Submit bugs, feature requests, support questions, and documentation fixes in the GitHub repo:

- [Issue forms and request tracking](https://github.com/bulova23/pbir-design-analyzer/issues)
- [Project repository](https://github.com/bulova23/pbir-design-analyzer)
