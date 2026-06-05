# 2026-06-03 Fabric Readiness UI Fixes

## Scope

- Fix Fabric App readiness workspace presentation regressions reported from the packaged `0.4.0` build.
- Keep the change scoped to webview presentation only.

## Implemented

- Separated Fabric readiness out of the shared overview badge row into a dedicated `Fabric App migration readiness` callout.
- Converted readiness labels from raw enum tokens to human-readable labels:
  - `Possible Candidate`
  - `Redesign Required`
  - `Keep As Report`
  - `Strong Candidate`
- Filtered the Evidence `Fabric App Readiness` section to the selected page when a page tab is active.
- Filtered the Executive Summary readiness callout to the selected page when a page tab is active.
- Filtered the Executive Summary `Top strengths`, `Top weaknesses`, `Top issues`, and `Top actions` cards to the selected page when a page tab is active.
- Hid the Fix Plan batch workflow block when the current context has no deterministic opportunities and replaced it with an advisory-only message.
- Synced the Issues page filter to the active page tab so page navigation updates the Issues context automatically.
- Added a dedicated `Readiness role` Issues filter so Fabric readiness blockers and related advisory classes do not overload `Scope`.
- Added a page-specific readiness evidence card with:
  - page readiness score
  - blockers
  - unsupported patterns
  - redesign-area count
  - page-scoped migration notes and evidence
- Added a page-specific overview readiness callout so the executive summary stays aligned with the active page context.
- Added page-specific overview card derivation from:
  - selected-page benchmark strengths
  - selected-page actionability strengths
  - selected-page readiness positive signals
  - selected-page filtered findings for weaknesses, issues, and actions
- Added explicit webview coverage for:
  - batch workflow visible when deterministic opportunities exist
  - batch workflow hidden when only advisory recommendations exist
  - advisory-only messaging displayed in the advisory state
  - issue page filter follows selected page-tab navigation
  - readiness-role filtering for Fabric readiness findings
- Fixed readiness card spacing so the content no longer sits flush against the left edge.
- Widened the readiness badge so multi-word states render cleanly.
- Rebuilt the packaged artifact:
  - `vscode-extension/pbir-design-analyzer-0.4.0.vsix`

## Validation

- `cd vscode-extension && npx jest -c jest.webview.config.cjs --runInBand webview-src/analyzer-score/App.test.tsx`
- `cd vscode-extension && npm test`
- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm run package`

## Notes

- No backend or scoring-contract changes were required.
- No `.NET` test rerun was needed because the change stayed inside the webview layer.

## Next Step

- Smoke the rebuilt VSIX in VS Code against the same PBIR report/page path that exposed the readiness evidence issue to confirm the visual result in the packaged extension host.
