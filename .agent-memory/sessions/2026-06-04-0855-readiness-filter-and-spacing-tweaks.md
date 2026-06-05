# 2026-06-04 08:55 Readiness Filter And Spacing Tweaks

## Context

- User feedback on the analyzer score webview called out:
  - cramped spacing in `Page Purpose Analysis`
  - awkward `Readiness Role` label in `Issues`
  - missing way to hide Fabric readiness findings from the issue list

## Changes

- Updated `vscode-extension/webview-src/analyzer-score/styles.css`:
  - added extra spacing under the `Page Purpose Analysis` header
  - increased spacing inside the summary block
  - added a small bottom margin below `Show Full Reasoning`
- Updated `vscode-extension/webview-src/analyzer-score/App.tsx`:
  - renamed the issues filter label from `Readiness role` to `Fabric App Readiness`
  - renamed the filter accessibility label accordingly
  - added `Hide readiness issues` as a filter option
  - filtered out `fabricAppReadiness` findings when that option is selected
  - updated active-filter summary copy to use the new label
- Updated `vscode-extension/webview-src/analyzer-score/App.test.tsx`:
  - adjusted readiness filter expectations to the new label
  - added coverage for hiding readiness findings
  - tightened selectors where the new label now appears in multiple UI areas

## Validation

- Passed:
  - `cd vscode-extension && npx jest -c jest.webview.config.cjs --runInBand webview-src/analyzer-score/App.test.tsx`

## Notes

- Validation was targeted to the changed score-panel webview path only.
- No manual VS Code smoke check was run in this session.
