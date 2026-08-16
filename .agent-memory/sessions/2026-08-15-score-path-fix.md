# Session — 2026-08-15 PBIR score path fix

## Request

Fix the new extension's failure to open and score a PBIR report:
`Scoring failed — Parameter 'reportPath' is required.`

## Diagnosis

The backend correctly rejects an empty `reportPath`. The extension's selected
tree-item resolver climbed from `definition/report.json` looking only for a
`definition.pbir` marker. Reports represented by `definition/report.json`
without that marker therefore resolved to no path, and the score panel was not
invoked with a valid report root.

## Changes

- Added a regression test for a report.json-backed report without
  `definition.pbir`.
- Updated `resolveReportPathFromNodePath` to recognize both
  `definition.pbir` and `definition/report.json` report roots.
- No backend, RPC, or product feature changes.

## Validation

- Focused tree-item tests: 6 passed.
- Extension and webview suites: 506 and 68 passed.
- TypeScript compilation: passed.
- Production build: passed; unrelated existing nullable-reference warnings
  remain in backend source.
- Changed-file ESLint: passed.
- `git diff --check`: passed.

## Closeout

All changes are uncommitted. Manual reload/reinstall and scoring of the user's
affected report remains the final confirmation step.
