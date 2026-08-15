# PBI Lens Rendered Evidence Provider Current State

Date: 2026-08-15

## State

Capability-safe provider seam implemented. Automatic rendered scoring is
deferred because the installed PBI Lens 0.4.0 extension does not expose a
supported programmatic VS Code API, the PBI Lens CLI is not installed, and no
PBI Lens MCP server is installed or connected in the current environment.

## Implemented

- provider-independent rendered evidence and capability contracts
- PBI Lens extension ID, installed state, activation state, and version
  detection
- independent public API, CLI, MCP, page screenshot, report context, and visual
  context capability flags
- explicit provider statuses and bounded diagnostics
- no-fabrication evidence provider returning an empty evidence collection
- safe enhanced scoring settings with deterministic scoring unchanged
- one-time absent-extension recommendation with dismissal state
- additive score-panel status metadata and informational fallback messaging
- optional Rendered Review classification and finding-driven checklist
- manual reviewer status and note recording in the Optimization Report
- typed user-supplied screenshot evidence records reused from existing upload
  primitives
- rendered-review export fields for categories, notes, statuses, and screenshot
  counts

## Deliberately not implemented

- automatic screenshot acquisition
- CLI or MCP process/client adapters
- generic process execution
- interactive PBI Lens command automation
- private extension internals or undocumented exports
- automatic score weighting, image parsing, rendered scoring, or enhanced score
  calculation

## Proof boundary

The current provider can prove that the extension is installed and identify its
version. It cannot prove rendered evidence availability. That distinction is
represented in the capability report and is visible in the score panel.

## Rendered Review workflow

Findings can now be classified as Deterministic, Semantic, or Rendered Review
Recommended. Rendered Evidence Required is reserved and is not emitted. The
checklist covers whitespace balance, visual hierarchy, KPI prominence, title
wrapping, clipped labels, crowded visuals, table readability, visual balance,
color harmony, and page readability.

PBI Lens remains the observation companion. The Open in PBI Lens action is
disabled unless the provider reports a supported report-context interface.
Reviewers can attach screenshots manually; the analyzer does not inspect their
pixels. Mutation remains deterministic preview/apply/rollback followed by a
human before/after review.

## Next activation step

When a supported programmatic PBI Lens API, exercised CLI, or exercised MCP
connection is available, add exactly one concrete adapter behind the existing
provider contract and validate it manually before enabling rendered scoring.

## Validation

The rendered-review focused/model and provider tests pass (21 tests in the
focused run). The extension suite has 523 passing tests across 102 suites and
the webview suite has 68 passing tests across 11 suites. TypeScript
compilation, production build, VSIX packaging, changed-file lint, and
whitespace validation pass. The full backend run produced 995 passed, 11
expected Windows skips, and one unrelated known Phase 35E timeout-test flake.
