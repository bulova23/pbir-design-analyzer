# 2026-08-15 Rendered Design Review Integration

## Objective

Implement Phase 1 Rendered Design Review as an optional companion workflow to
PBIR Design Analyzer, using PBI Lens only for rendered observation.

## Changes

- Added pure rendered-review classification/checklist model with ten supported
  categories, guidance, immutable status/note/screenshot updates, and reserved
  Rendered Evidence Required classification.
- Added optional normalized-finding classification fields without changing
  deterministic score outputs.
- Added score-panel state/protocol handlers for checklist status, notes,
  manual screenshot attachment, and capability-safe PBI Lens action visibility.
- Reused existing screenshot upload/copy primitives; no pixel parsing or
  automated capture was added.
- Added rendered-review export fields and Markdown summary content.
- Added settings: Rendered Review Enabled, Suggest PBI Lens, and Show Rendered
  Review Checklist.
- Updated README, roadmap, design-governance strategy, current state, rendered
  review guide, UAT guide, and implementation notes.

## Validation

- Focused tests: 21 passed.
- Extension: 102 suites / 523 passed.
- Webview: 11 suites / 68 passed.
- Build and VSIX package: passed; macOS arm64 VSIX generated.
- Changed-file ESLint: passed.
- Backend: 995 passed, 11 expected Windows skips, one unrelated known Phase
  35E timeout-test failure (`Completed` instead of `TimedOut`).
- `git diff --check`: passed.

## Repository state

All changes remain unstaged and uncommitted. Generated build/package outputs
remain present according to the existing repository packaging workflow.
