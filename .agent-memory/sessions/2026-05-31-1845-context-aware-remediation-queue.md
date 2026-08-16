# 2026-05-31 18:45 — Context-Aware Remediation Queue

## Objective

Implement the `0.2.2` follow-up to make `Fix Plan` context-aware without changing scoring semantics.

## Decisions

- Kept the enhancement presentation-only in the score webview.
- Derived remediation from normalized findings using remediation-driving filters only:
  - `Page`
  - `Dimension`
  - `Impact`
- Left diagnostic-only filters out of queue generation:
  - `Severity`
  - `Scope`
  - `Detection`
- Used diagnostic-only filters to continue shaping `Issues` while leaving remediation broader and steadier than the visible issue slice.
- Added explicit queue scope messaging:
  - `Remediation Focus`
  - helper copy explaining why remediation differs from the exact issue filter result
- Added per-action coverage summaries such as `1 High · 1 Medium`.
- Improved source traceability by listing finding titles and severity labels in each remediation action.
- Kept the Fix Plan section visible even when a narrow remediation focus produces no actions, so the scope explanation remains visible instead of disappearing.

## Implementation

- Added `webview-src/analyzer-score/remediationQueue.ts` to build a context-aware remediation queue from normalized findings.
- Updated `webview-src/analyzer-score/App.tsx` to:
  - derive remediation focus from the current page context plus active remediation-driving filters
  - render `Remediation Focus`
  - render coverage summaries
  - render source-finding traceability
  - keep empty remediation domains visible with explanatory copy
- Added tests in:
  - `webview-src/analyzer-score/remediationQueue.test.ts`
  - `webview-src/analyzer-score/App.test.tsx`
- Released as `vscode-extension/package.json` version `0.2.2`.
- Updated release notes in:
  - `docs/CHANGELOG.md`
  - `vscode-extension/README.md`

## Validation

- `cd vscode-extension && npm test`
- `cd vscode-extension && npx eslint webview-src/analyzer-score/App.tsx webview-src/analyzer-score/remediationQueue.ts webview-src/analyzer-score/App.test.tsx webview-src/analyzer-score/remediationQueue.test.ts`
- `cd vscode-extension && npm run package`

## Artifact

- `vscode-extension/pbir-design-analyzer-0.2.2.vsix`

## Residual Risk

- Manual VS Code smoke coverage has not yet been rerun against the packaged `0.2.2` artifact.
