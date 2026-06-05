# 2026-06-04 17:02 Review Commentary Evidence Relocation

## Context

- The score webview still surfaced `Reviewer Comment Generator` in the primary review flow after `Fix Plan`, which made the workflow feel redundant and interrupted the main analysis path.

## Changes

- Updated `vscode-extension/webview-src/analyzer-score/App.tsx`:
  - removed the top-level `Reviewer Comment Generator` section from the main workflow
  - renamed the renderer to `Review Commentary`
  - moved the commentary UI under `Evidence` as its own `details` subsection
  - kept the subsection collapsed by default
  - preserved the existing persona selector and `buildReviewerComments(...)` generation logic
  - kept commentary as derived/supporting evidence rather than primary analysis
- Updated `vscode-extension/webview-src/analyzer-score/App.test.tsx`:
  - proved the top-level reviewer-comment section no longer appears in the main workflow
  - proved `Evidence` contains `Review Commentary`
  - proved the subsection is collapsed by default
  - proved persona selection remains available and persona-aware commentary still renders
  - preserved existing export-path assertions in the same suite

## Validation

- Passed:
  - `cd vscode-extension && npx jest -c jest.webview.config.cjs --runInBand webview-src/analyzer-score/App.test.tsx`
  - `cd vscode-extension && npm run compile`

## Notes

- Export behavior was intentionally not redesigned in this slice.
- Story Assessment, Issues, and Fix Plan rendering were left in place.
