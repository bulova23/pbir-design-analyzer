# <img src="vscode-extension/resources/icon.png" alt="PBIR Design Analyzer logo" width="28" style="vertical-align: middle;" /> PBIR Design Analyzer

PBIR Design Analyzer is a VS Code extension for reviewing local Power BI PBIP/PBIR reports before they are shared, governed, or published. It analyzes report structure, layout, storytelling, accessibility, consistency, and review readiness, then presents the results in a modernized review workspace.

## 0.2.0 Highlights

- modernized score-panel workspace
- Overview workspace for executive triage
- Issues workspace built on normalized findings
- Fix Plan workspace for remediation sequencing
- Evidence workspace for framework, metadata, and audit drilldown
- smart collapse defaults to reduce information overload
- intent confirmation and review feedback workflow
- review packet preview and export kept downstream from analysis
- workspace review modes: Default, Executive, Consultant, Governance, Accessibility
- cross-page matrix navigation into filtered Issues

## Core Concepts

### Overview

The landing summary. Use it to answer:

- how healthy is the report overall
- what is wrong first
- what should be fixed first
- which pages look weak by review dimension

### Issues

The main working surface. Issues are normalized across multiple scoring/evidence systems so the user can triage by:

- severity
- confidence
- page
- dimension
- scope
- detection type

### Fix Plan

The consultant-facing remediation queue. It converts findings into action-oriented next steps with severity, effort, scope, and affected-page context.

### Evidence

The secondary drilldown layer. Framework analysis, metadata, screenshot audit, scoring internals, and packet preview still exist, but they no longer dominate the default reading path.

## Installation

Prerequisites:

- Node.js 18+
- .NET 8 SDK
- VS Code 1.93+

Build and package locally:

```bash
cd vscode-extension
npm install
npm run build
npm run package
```

Run backend tests:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

## Getting Started

1. Open a PBIP project or `.Report` folder in VS Code.
2. Run **PBIR Design Analyzer: Score Report**.
3. Start in **Overview** for executive triage.
4. Use **Issues** to inspect prioritized findings.
5. Use **Fix Plan** to sequence remediation.
6. Use **Evidence** only when you need deeper proof or source details.
7. Export or preview review packets after the review is complete.

## Review Modes

The workspace supports presentation-only review modes:

- Default
- Executive
- Consultant
- Governance
- Accessibility

These modes change how findings and actions are prioritized and explained. They do **not** change scores, severities, confidences, or backend scoring.

## Cross-Page Matrix Navigation

The Overview workspace includes a page-by-dimension matrix. Clicking a cell filters the Issues workspace directly to the relevant page and review dimension while preserving the active review mode.

## Documentation

- [Detailed How-To Guide](docs/HOW_TO_USE.md)
- [Extension README](vscode-extension/README.md)
- [Changelog](docs/CHANGELOG.md)
- [Roadmap](docs/ROADMAP.md)
- [Release Guide](docs/RELEASING.md)

## Roadmap Summary

The next planned epics after `0.2.0` are:

1. Consultant Deliverables & Export Platform
2. Visual Intelligence & Screenshot Analysis
3. Enterprise Governance & Advanced Review

See [docs/ROADMAP.md](docs/ROADMAP.md) for order, value, risk, complexity, and linked specs/plans.

## Repository Layout

- `vscode-extension/` — shipped extension
- `service-dotnet/` — backend host and PBIR scoring services
- `docs/` — release notes, how-to guidance, roadmap, and supporting documentation

## License

MIT. See [LICENSE](LICENSE).
