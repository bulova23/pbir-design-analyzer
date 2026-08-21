# <img src="vscode-extension/resources/icon.png" alt="PBIR Design Analyzer logo" width="28" style="vertical-align: middle;" /> PBIR Design Analyzer

PBIR Design Analyzer 1.0.0 is the current cross-platform Analytics Experience Review Platform release.

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

### Rendered Review

The Optimization Report can recommend a human Rendered Review for concerns
such as whitespace balance, KPI prominence, title wrapping, crowded visuals,
table readability, color harmony, and page readability. Reviewers can record a
status, add notes, and attach user-supplied screenshot evidence. Screenshot
capture is manual; anyone who wants a faster way to capture Power BI report
screenshots can use an external tool such as
[PBI Lens](https://github.com/thenguyentrong/pbi-lens) and attach the
resulting images here. PBIR Design Analyzer remains authoritative for design
judgment, scoring, governance, and deterministic remediation either way.

## 1.0.0 Highlights

### New

- Guided Story Improvements inside Story Assessment, with one-click navigation to the exact page or visual behind each recommendation
- a What Changed summary inside Story Assessment comparing the latest review against the previous one
- a collapsible workspace layout: Issues and Fix Plan now sit directly under Overview, and Issues, Fix Plan, Review Summary, Story Assessment, and Rendered Review are collapsed by default until opened

### Improved

- Optimization Report scoring is more resilient to reports exported by different Power BI Desktop versions
- navigation actions and other in-panel actions now surface a clear error message instead of failing silently

### Fixes

- fixed Attach Screenshot silently doing nothing inside the Rendered Review checklist
- fixed a transport-layer bug that could cause Optimization Report scoring to fail with a generic bounded-request error
- fixed inconsistent success and failure reporting when a report import could not be completed

## Review Workflow

1. Open a PBIP project or .Report folder.
2. Score the report or page.
3. Start in Overview for the intended narrative, top risks, and migration-readiness signals.
4. Use Issues and Fix Plan, directly beneath Overview, to triage findings and sequence remediation.
5. Open Story Assessment for guided top improvements and Evidence to inspect proof before acting on recommendations.
6. Export or share review outputs after the review is complete.

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

The local PBIR authoring workflow now exposes a curated set of six report
mutations: Rename Page, Add Page, Remove Page, Move Page, Move Visual, and
Resize Visual. Every mutation is planned and previewed by the backend before
confirmation, returns semantic diff and analyzer evidence, and produces a new
artifact handle while preserving the imported snapshot. Other typed backend
mutations, capability discovery, public batching, graphical editing, and raw
JSON editing remain deferred.

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

Runtime expectation for the public `1.0.0` packages:

- Windows x64 requires the matching .NET 8 runtime
- Windows arm64 ships with a self-contained backend for `1.0.0`
- Linux x64 requires the matching .NET 8 runtime
- macOS x64 requires the matching .NET 8 runtime
- macOS arm64 requires the matching .NET 8 runtime

The Windows arm64 package is intentionally larger than the other target-specific VSIX files because it bundles the .NET runtime inside the backend payload for startup reliability on Windows 11 ARM.

If the backend runtime is missing or cannot start, the extension falls back to degraded mode:

- local PBIR tree browsing remains available
- backend-driven scoring and governance commands stay unavailable until the correct runtime or VSIX is installed
- the extension shows explicit degraded-mode messaging instead of failing silently

## Cross-Platform Score Determinism

`1.0.0` treats score determinism as a release gate.

- the same report fingerprint must produce the same score, issue counts, readiness score, analyzer metadata, and findings on every supported platform
- theme, locale, path separators, newline style, filesystem traversal order, and machine architecture must not change scoring outcomes

After scoring, use **PBIR Design Analyzer: Copy Score Diagnostics** to capture the active diagnostic payload. The payload is copied to the clipboard and also written to the **PBIR Score Diagnostics** output channel.

To compare two saved diagnostic payloads locally:

```bash
cd vscode-extension
node scripts/compare-score-diagnostics.mjs /path/to/first.json /path/to/second.json
```

If the fingerprints match, the score outputs must match. If the fingerprints differ, treat the report copies as non-identical input.

## Final 1.0.0 Package Set

Manual release packaging for `1.0.0` should produce these five files:

- `pbir-design-analyzer-1.0.0-win32-x64.vsix`
- `pbir-design-analyzer-1.0.0-win32-arm64.vsix`
- `pbir-design-analyzer-1.0.0-linux-x64.vsix`
- `pbir-design-analyzer-1.0.0-darwin-x64.vsix`
- `pbir-design-analyzer-1.0.0-darwin-arm64.vsix`

Install the VSIX that matches the target operating system and architecture.

## Icon Rendering Note

The source icon PNG is transparent and the packaged copy should match it byte-for-byte.

If VS Code shows the icon on a light tile in the extension details page, treat that as VS Code rendering behavior rather than a packaging defect.

## Manual Marketplace Publishing

`1.0.0` is prepared for manual Marketplace upload. Do not rely on repo-side publication automation for this release.

Manual release flow:

1. Rebuild and inspect the five target-specific VSIX files.
2. Keep all five artifacts for the same `1.0.0` extension listing.
3. Upload the matching package for each supported target during manual Marketplace publication.
4. Keep the Windows arm64 self-contained package in the release set alongside the framework-dependent Windows x64, Linux x64, macOS x64, and macOS arm64 packages.
5. Do not alter the icon asset unless the packaged icon no longer matches the source file.

## Documentation

- [Detailed How-To Guide](docs/HOW_TO_USE.md)
- [Extension README](vscode-extension/README.md)
- [Changelog](docs/CHANGELOG.md)
- [Release Guide](docs/current-state/RELEASING.md)
- [0.5.0 Release Summary](docs/releases/2026-06-05-0-5-0-release-summary.md)
- [Roadmap](docs/ROADMAP.md)
- [Rendered Review guide](docs/integrations/rendered-review-guide.md)
- [Rendered Review UAT guide](docs/integrations/rendered-review-uat-guide.md)

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

## Backend Artifact Ownership

Workstream 4B makes the backend packaging boundary explicit.

Source-owned files:

- `service-dotnet/RpcHost/` and backend source under `service-dotnet/`
- extension packaging scripts under `vscode-extension/scripts/`
- extension manifest and release docs:
  - `vscode-extension/package.json`
  - `README.md`
  - `docs/current-state/RELEASING.md`

Generated backend files:

- `vscode-extension/backend/rpc/`
  - local development build output for the current machine
  - rebuilt by `npm run build` or `npm run build:backend`
  - ignored in git
- `vscode-extension/backend/targets/<target>/rpc/`
  - target-specific packaged backend staging outputs
  - rebuilt by `npm run package:all` or `node scripts/build-backend.mjs --target <target> --output backend/targets/<target>/rpc`
  - intentionally treated as generated artifacts, not hand-maintained source

Packaging-owned files:

- `pbir-design-analyzer-<version>-<target>.vsix`
- the staged backend payload copied into each packaged VSIX as `backend/rpc/`

Do not manually edit:

- files under `vscode-extension/backend/rpc/`
- files under `vscode-extension/backend/targets/`
- generated `.vsix` files

Current staged cleanup path:

- `vscode-extension/backend/targets/` remains checked in for now because it is the reproducible multi-target packaging staging area used by `package:all`
- treat those files as generated snapshots
- use verification and rebuild commands instead of editing them by hand
- if the repo later removes checked-in targets, that should happen as a separate intentional cleanup change with release-process validation

## Backend Packaging Workflow

Bucket A removed runtime fallback to repo-local `Debug` and `Release` publish leftovers.

Current behavior:

- repo-hosted development uses `vscode-extension/backend/rpc/` for the local machine after `npm run build` or `npm run build:backend`
- installed and packaged extension runtime uses only the packaged `backend/rpc/` payload inside the VSIX
- repo-local `service-dotnet/RpcHost/bin/Debug/...` and `service-dotnet/RpcHost/bin/Release/...` outputs are not part of runtime resolution

Useful commands:

```bash
cd vscode-extension
npm run build:backend
npm run verify:backend:targets
npm run clean:backend:targets
npm run package:all
```

Command intent:

- `npm run build:backend`
  - rebuilds `backend/rpc/` for the current platform only
- `npm run verify:backend:targets`
  - checks that all supported packaged targets exist and that each target still contains the runtime-critical backend files
- `npm run clean:backend:targets`
  - removes only the known generated target staging directories under `backend/targets/`
- `npm run package:all`
  - recompiles the extension/webviews
  - rebuilds every packaged backend target under `backend/targets/<target>/rpc/`
  - creates the five target-specific VSIX files

To validate packaged backend runtime behavior:

1. Run `cd vscode-extension && npm run verify:backend:targets`.
2. Run `cd vscode-extension && npm run package:all`.
3. Install the VSIX that matches the test machine target.
4. Score a real PBIR report and confirm the backend starts normally.
5. Treat any success that depends on repo-local `service-dotnet/RpcHost/bin/Debug` or `Release` outputs as a bug, because packaged-only runtime resolution is now the contract.

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
npm run verify:backend:targets
npm run package:all
```

Run backend tests:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

## License

MIT. See [LICENSE](LICENSE).
