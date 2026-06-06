# <img src="vscode-extension/resources/icon.png" alt="PBIR Design Analyzer logo" width="28" style="vertical-align: middle;" /> PBIR Design Analyzer

PBIR Design Analyzer 0.5.0 is the first cross-platform Analytics Experience Review Platform release.

It helps teams review PBIR reports and analytical Fabric Apps through one workspace built around Story Assessment, Issues, Fix Plan, Evidence, Fabric App Readiness, Fabric App Review, and AI Proposal Enrichment.

## Product Positioning

PBIR Design Analyzer is not positioned as a narrow PBIR metadata utility or a report linter.

It is designed to help teams answer higher-value review questions:

- What story is this report, dashboard, or app trying to tell?
- What prevents that story from succeeding?
- Which issues matter most for design quality, usability, actionability, navigation, accessibility, and governance?
- What should be fixed first?
- Which recommendations stay advisory, and which ones can be executed through a deterministic workflow?
- Which PBIR assets are strong candidates for Fabric App migration?
- How does an analytical Fabric App hold up under the same review lens?

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
- governance risks

### Fix Plan

- remediation guidance
- advisory recommendations
- business rationale and prioritization guidance
- deterministic fix opportunities for supported scenarios

### Evidence

- metadata evidence
- navigation evidence
- screenshot evidence
- semantic-model evidence
- code-derived evidence
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

### AI Proposal Enrichment

- clearer explanations
- business rationale
- prioritization guidance
- expected outcomes

This enrichment remains advisory and does not bypass deterministic execution boundaries.

## 0.5.0 Highlights

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

- cross-platform VSIX packages for Windows x64, Windows arm64, Linux x64, macOS x64, and macOS arm64
- backend startup detection and runtime checks
- clearer degraded-mode messaging when the backend is unavailable
- stronger review positioning for PBIR reports and analytical Fabric Apps
- deterministic score diagnostics and report fingerprinting

### Safety

- deterministic fix-engine hardening
- safer mutation planning with unsupported title and semantic-color writes held back
- corrected severity outcome reporting

## Review Workflow

1. Open a PBIP project or .Report folder.
2. Score the report or page.
3. Use Story Assessment and Overview to understand the intended narrative, top risks, and migration-readiness signals.
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

## Platform Support

- Windows x64
- Windows arm64
- Linux x64
- macOS x64
- macOS arm64

Each packaged release ships as a platform-targeted VSIX with the matching backend binary for that operating system and architecture.

Runtime expectation for the public `0.5.0` packages:

- Windows x64 requires the matching .NET 8 runtime
- Windows arm64 ships with a self-contained backend for `0.5.0`
- Linux x64 requires the matching .NET 8 runtime
- macOS x64 requires the matching .NET 8 runtime
- macOS arm64 requires the matching .NET 8 runtime

The Windows arm64 package is intentionally larger than the other target-specific VSIX files because it bundles the .NET runtime inside the backend payload for startup reliability on Windows 11 ARM.

If the backend runtime is missing or cannot start, the extension falls back to degraded mode:

- local PBIR tree browsing remains available
- backend-driven scoring and governance commands stay unavailable until the correct runtime or VSIX is installed
- the extension shows explicit degraded-mode messaging instead of failing silently

## Cross-Platform Score Determinism

`0.5.0` treats score determinism as a release gate.

- the same report fingerprint must produce the same score, issue counts, readiness score, analyzer metadata, and findings on every supported platform
- theme, locale, path separators, newline style, filesystem traversal order, and machine architecture must not change scoring outcomes

After scoring, use **PBIR Design Analyzer: Copy Score Diagnostics** to capture the active diagnostic payload. The payload is copied to the clipboard and also written to the **PBIR Score Diagnostics** output channel.

To compare two saved diagnostic payloads locally:

```bash
cd vscode-extension
node scripts/compare-score-diagnostics.mjs /path/to/first.json /path/to/second.json
```

If the fingerprints match, the score outputs must match. If the fingerprints differ, treat the report copies as non-identical input.

## Final 0.5.0 Package Set

Manual release packaging for `0.5.0` should produce these five files:

- `pbir-design-analyzer-0.5.0-win32-x64.vsix`
- `pbir-design-analyzer-0.5.0-win32-arm64.vsix`
- `pbir-design-analyzer-0.5.0-linux-x64.vsix`
- `pbir-design-analyzer-0.5.0-darwin-x64.vsix`
- `pbir-design-analyzer-0.5.0-darwin-arm64.vsix`

Install the VSIX that matches the target operating system and architecture.

## Icon Rendering Note

The source icon PNG is transparent and the packaged copy should match it byte-for-byte.

If VS Code shows the icon on a light tile in the extension details page, treat that as VS Code rendering behavior rather than a packaging defect.

## Manual Marketplace Publishing

`0.5.0` is prepared for manual Marketplace upload. Do not rely on repo-side publication automation for this release.

Manual release flow:

1. Rebuild and inspect the five target-specific VSIX files.
2. Keep all five artifacts for the same `0.5.0` extension listing.
3. Upload the matching package for each supported target during manual Marketplace publication.
4. Keep the Windows arm64 self-contained package in the release set alongside the framework-dependent Windows x64, Linux x64, macOS x64, and macOS arm64 packages.
5. Do not alter the icon asset unless the packaged icon no longer matches the source file.

## Documentation

- [Detailed How-To Guide](docs/HOW_TO_USE.md)
- [Extension README](vscode-extension/README.md)
- [Changelog](docs/CHANGELOG.md)
- [Release Guide](docs/RELEASING.md)
- [0.5.0 Release Summary](docs/releases/2026-06-05-0-5-0-release-summary.md)
- [Roadmap](docs/ROADMAP.md)

## Roadmap Summary

Planned follow-on areas include:

1. Advanced AI refactoring
2. Fabric App Review Mode expansion
3. Consultant Deliverables and Export Platform
4. Visual Intelligence and Screenshot Analysis
5. Enterprise Governance and Advanced Review

See [docs/ROADMAP.md](docs/ROADMAP.md) for detailed ordering and linked specs.

## Repository Layout

- `vscode-extension/` - shipped extension
- `service-dotnet/` - backend host and scoring services
- `docs/` - usage guidance, changelog, release notes, roadmap, and release support

## Installation

Build and package locally:

```bash
cd vscode-extension
npm install
npm run build
npm run package
```

Build all platform-targeted VSIX packages:

```bash
cd vscode-extension
npm run package:all
```

Run backend tests:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

## License

MIT. See [LICENSE](LICENSE).
