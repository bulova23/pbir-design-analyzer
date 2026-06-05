# <img src="vscode-extension/resources/icon.png" alt="PBIR Design Analyzer logo" width="28" style="vertical-align: middle;" /> PBIR Design Analyzer

PBIR Design Analyzer is an Analytics Experience Review Platform for reviewing Power BI reports and analytical experiences before they are shared, governed, migrated, or used as client-facing deliverables.

It combines story assessment, evidence-driven findings, remediation planning, deterministic fix workflows, governance review, and Fabric App readiness into one review workspace for consultants, BI architects, analytics teams, Power BI developers, and Fabric developers.

## Product Positioning

PBIR Design Analyzer is not positioned as a PBIR metadata utility or a report linter.

It is designed to help teams answer higher-value questions such as:

- What story is this page trying to tell?
- Does the analytical experience succeed for the intended audience?
- What design, usability, navigation, accessibility, or actionability issues are holding it back?
- What should be fixed first?
- Which changes are advisory, and which ones can be executed through a deterministic workflow?
- Which assets are strong candidates for Fabric App evolution, and which ones need redesign first?

## What The Platform Delivers

### Story Assessment

- page-purpose interpretation
- story quality and narrative flow review
- headline-to-supporting-evidence assessment
- executive and consultant-friendly review framing

### Issues Workspace

- design issues
- usability issues
- actionability gaps
- navigation problems
- accessibility concerns
- cross-page consistency signals

### Fix Plan

- remediation guidance
- advisory recommendations
- business rationale and prioritization guidance
- deterministic fix opportunities for supported scenarios

### AI Proposal Enrichment

- stronger explanation quality
- business rationale
- prioritization guidance
- expected-outcome framing

This enrichment remains advisory and does not bypass deterministic execution boundaries.

### Evidence-Driven Review

- metadata evidence
- navigation evidence
- screenshot evidence
- semantic-model evidence
- supporting framework analysis

### Fabric App Readiness

- migration candidates
- blockers
- redesign effort
- Fabric App suitability

### Fabric App Review

- analytical Fabric App review
- navigation and information-flow review
- design-token evidence
- screenshot-backed evidence
- semantic-model usage evidence

### Governance

- standards support
- consistency review
- accessibility coverage
- analytics-quality review

## 0.4.0 Highlights

- modern review workspace built around Overview, Issues, Fix Plan, Evidence, and Export
- stronger story assessment and review framing for analytical pages
- deterministic Fix Opportunities with preview, apply, rollback, grouped review safety, and re-analysis in supported areas
- AI proposal enrichment that improves recommendations without changing deterministic execution authority
- Fabric App Readiness for migration evaluation from PBIR assets
- Fabric App Review foundations for analytical Fabric experiences with richer evidence
- screenshot and semantic-model evidence support in the shared review workflow
- review modes for executive, consultant, governance, and accessibility-focused reading paths

## Review Workflow

1. Open a PBIP project or .Report folder.
2. Score the report or page.
3. Use Overview to understand story quality, top risks, and migration-readiness signals.
4. Use Issues to triage findings by severity, page, dimension, and scope.
5. Use Fix Plan to sequence remediation and apply supported deterministic fixes where available.
6. Use Evidence to inspect proof before acting on recommendations.
7. Export or share review outputs after the review is complete.

## Deterministic Execution Boundary

PBIR Design Analyzer separates advisory recommendations from executable changes.

Advisory capabilities can:

- explain issues more clearly
- improve remediation guidance
- compare alternatives
- strengthen business framing

Deterministic workflows can:

- preview exact supported changes
- apply safe supported mutations
- roll back applied changes
- re-analyze the result

Advisory capabilities do not generate freeform report mutations, DAX, or autonomous redesign behavior.

## Review Modes

The workspace supports presentation modes for:

- Default
- Executive
- Consultant
- Governance
- Accessibility

These modes change emphasis and sequencing, not core scoring outcomes.

## Cross-Page Review

The platform includes cross-page matrix navigation so reviewers can move from high-level weak areas directly into the exact page and review dimension that needs attention.

## Documentation

- [Detailed How-To Guide](docs/HOW_TO_USE.md)
- [Extension README](vscode-extension/README.md)
- [Changelog](docs/CHANGELOG.md)
- [Roadmap](docs/ROADMAP.md)
- [Release Guide](docs/RELEASING.md)

## Roadmap Summary

Planned follow-on areas include:

1. Advanced AI refactoring
2. Fabric App Review Mode expansion
3. Consultant Deliverables and Export Platform
4. Visual Intelligence and Screenshot Analysis
5. Enterprise Governance and Advanced Review

See [docs/ROADMAP.md](docs/ROADMAP.md) for detailed ordering and linked specs.

## Repository Layout

- vscode-extension/ - shipped extension
- service-dotnet/ - backend host and scoring services
- docs/ - usage guidance, changelog, roadmap, and release support

## Installation

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

## License

MIT. See [LICENSE](LICENSE).
