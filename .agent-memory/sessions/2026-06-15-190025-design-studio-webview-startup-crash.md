# 2026-06-15 Design Studio Webview Startup Crash

## Goal

- Fix the packaged Report Design Studio blank-page startup crash only.

## Scope Guardrails

- Bug fix only
- No new features
- No scoring changes
- No architecture changes

## Root-Cause Notes

- Confirmed the runtime crash came from the packaged `vscode-extension/webview-dist/design-studio.js` bundle, not from Design Studio application code.
- Exact failing bundle path:
  - React development-branch selection inside the built webview bundle still referenced `process.env.NODE_ENV`.
- Source of the runtime reference:
  - shared webview build-tooling leakage from the Vite webview configs
  - not a Design Studio feature path
  - not a third-party package misuse in application code
- Added a failing webview Jest smoke test that:
  - loads the built Design Studio bundle from `webview-dist`
  - deletes `window.process`
  - executes the bundle in jsdom
  - asserts the shell and workflow rail render
- Fix applied:
  - added a compile-time production constant in the three webview Vite configs so bundled React branches do not emit browser-incompatible `process.env.NODE_ENV` checks
- Validation passed:
  - targeted red/green guard:
    - `cd vscode-extension && npx jest -c jest.webview.config.cjs --runTestsByPath webview-src/design-studio/__tests__/bundleRuntime.test.ts`
  - rebuilt webview assets:
    - `cd vscode-extension && npm run build:webview`
  - required validation:
    - `cd vscode-extension && npm test`
    - `cd vscode-extension && npm run compile`
- Manual verification:
  - opened Report Design Studio after swapping in the rebuilt Design Studio assets for the active local verification host
  - confirmed the shell rendered instead of a blank page
  - confirmed the workflow rail rendered, including:
    - `Design Brief`
    - `Concept Studio`
    - `Draft Studio`
    - `Refinement Studio`
    - `Prepare For Review`
    - `Review Design`
    - `Compare Iterations`

## Outcome

- Bug fixed.
