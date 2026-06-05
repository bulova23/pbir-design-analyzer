# <img src="vscode-extension/resources/icon.png" alt="PBIR Design Analyzer logo" width="28" style="vertical-align: middle;" /> PBIR Design Analyzer

PBIR Design Analyzer is a VS Code extension for reviewing local Power BI PBIP/PBIR reports before they are shared, governed, or published. It analyzes report structure, layout, storytelling, accessibility, consistency, and review readiness, then presents the results in a modernized review workspace with deterministic fix opportunities for selected remediation actions.

## 0.4.0 Highlights

- modernized score-panel workspace
- Overview workspace for executive triage
- Issues workspace built on normalized findings
- Fix Plan workspace for remediation sequencing
- deterministic fix opportunities with preview, apply, rollback, grouped workflow safety, and re-analysis for supported remediation domains
- advisory `AI-enriched guidance` now improves remediation wording, rationale, priority, and expected outcomes without changing deterministic execution
- provider-backed enrichment is currently disabled by default; fallback advisory guidance still appears
- single-page fix planning still works when page-level analysis is provided as top-level `scoredPageName + visualMetadata`
- unsupported remediation remains explicitly advisory when no safe metadata-only mutation exists
- Evidence workspace for framework, metadata, and audit drilldown
- smart collapse defaults to reduce information overload
- intent confirmation and review feedback workflow
- review packet preview and export kept downstream from analysis
- workspace review modes: Default, Executive, Consultant, Governance, Accessibility
- cross-page matrix navigation into filtered Issues
- foundational `Analyzable Surface` and analyzer-selection architecture for future non-PBIR analytical surfaces
- advisory `Fabric App Readiness Assessment` for PBIR reports and pages, including migration-readiness scoring, candidate pages, blockers, unsupported patterns, redesign effort, and next-step guidance

## Core Concepts

### Analyzable Surface

An `Analyzable Surface` is the thing being reviewed through the shared workspace.

Current surface:

- PBIR report

Designed next surfaces:

- Fabric App
- screenshot bundle

This keeps the product on one workspace with multiple analyzers instead of splitting into separate review tools.

### Fabric App Readiness Assessment

The first Fabric-oriented capability is not Fabric App repo review.

It is an advisory analyzer operating on a PBIR report surface to answer:

- should this report become a Fabric App
- which pages are strong candidates
- which patterns migrate poorly
- where redesign effort is required first

Readiness is scored across:

- layout portability
- interaction portability
- narrative portability
- semantic-model suitability
- navigation portability
- governance portability
- accessibility portability
- visualization-as-code opportunity

### Overview

The landing summary. Use it to answer:

- how healthy is the report overall
- what is wrong first
- what should be fixed first
- which pages look weak by review dimension
- whether the report is a strong Fabric App migration candidate

### Issues

The main working surface. Issues are normalized across multiple scoring/evidence systems so the user can triage by:

- severity
- confidence
- page
- dimension
- scope
- detection type

The readiness analyzer can also add advisory findings such as:

- Good Fabric App Candidate
- Migration Blocker
- Redesign Required
- Unsupported Pattern
- Visualization Opportunity

### Fix Plan

The consultant-facing remediation queue. It converts findings into action-oriented next steps with severity, effort, scope, and affected-page context. Supported remediation items can also expose fallback-safe `AI-enriched guidance` plus deterministic fix opportunities with explicit mutation previews and rollback. Advisory enrichment does not generate or apply mutations.

Readiness findings may also add advisory migration-preparation actions such as:

- Reduce Power BI-only dependencies
- Simplify navigation for app portability
- Improve semantic labeling for app reuse
- Improve narrative hierarchy for app migration

### Evidence

The secondary drilldown layer. Framework analysis, metadata, screenshot audit, scoring internals, and packet preview still exist, but they no longer dominate the default reading path.

Readiness evidence now includes portability rationale derived from PBIR metadata, interaction patterns, navigation structure, semantic-model cues, and other migration-readiness signals.

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
5. Use **Fix Plan** to sequence remediation and apply supported deterministic fixes.
6. Use **Evidence** only when you need deeper proof or source details.
7. Export or preview review packets after the review is complete.

## Execution Trust Boundary

Fabric readiness is advisory.

It may:

- generate findings
- generate fix-plan recommendations
- participate in proposal enrichment later

It may not:

- generate mutations
- create Fabric App code
- bypass deterministic preview, approval, apply, rollback, or re-analysis

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

The next planned epics after `0.4.0` are:

1. AI Fix Phase 4: Advanced AI refactoring
2. Fabric App Review Mode
3. Consultant Deliverables & Export Platform
4. Visual Intelligence & Screenshot Analysis
5. Enterprise Governance & Advanced Review

See [docs/ROADMAP.md](docs/ROADMAP.md) for order, value, risk, complexity, and linked specs/plans.

## Repository Layout

- `vscode-extension/` — shipped extension
- `service-dotnet/` — backend host and PBIR scoring services
- `docs/` — release notes, how-to guidance, roadmap, and supporting documentation

## License

MIT. See [LICENSE](LICENSE).
