# 2026-06-01 08:05 - Fix Opportunity Engine 0.3.0 Release Follow-Up

## Objective

Finish the release follow-up for Phase 1 of the deterministic Fix Opportunity Engine:

- manual smoke validation
- tab-context decision
- release-doc updates
- version bump
- package artifact generation

## What Changed

- Fixed the refresh-driven tab reset in:
  - `vscode-extension/src/views/PbirScorePanel.ts`
  - `vscode-extension/webview-src/analyzer-score/App.tsx`
- Added a regression test in `vscode-extension/webview-src/analyzer-score/App.test.tsx` to preserve the selected tab across loading/refresh.
- Bumped the extension from `0.2.2` to `0.3.0` in:
  - `vscode-extension/package.json`
  - `vscode-extension/package-lock.json`
- Updated release-facing docs:
  - `docs/CHANGELOG.md`
  - `README.md`
  - `vscode-extension/README.md`
  - `docs/ROADMAP.md`
- Packaged:
  - `vscode-extension/pbir-design-analyzer-0.3.0.vsix`

## Validation

### Automated

- `cd vscode-extension && npm test`
- `cd vscode-extension && npx eslint webview-src/analyzer-score/App.tsx webview-src/analyzer-score/App.test.tsx src/views/PbirScorePanel.ts`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- `cd vscode-extension && npm run package`

### Packaged Extension Smoke

Installed `vscode-extension/pbir-design-analyzer-0.3.0.vsix` into an isolated VS Code profile:

- user data dir: `/tmp/pbir-analyzer-0.3.0-smoke.eEWAAT/user`
- extensions dir: `/tmp/pbir-analyzer-0.3.0-smoke.eEWAAT/ext`

Verified:

- packaged extension installed as `bcrowell.pbir-design-analyzer@0.3.0`
- command execution opened `PBIR Optimization Report`
- real `Sales & Production` PBIR report scored in the packaged extension
- `Sales & Production` currently yields advisory-only remediation under Phase 1:
  - fix plan titles were all `Add benchmarks and decision context`
  - deterministic fix-opportunity count was `0`
- renderer log review did not show PBIR webview exceptions; observed errors were unrelated VS Code/Copilot cache/network noise

### Supported Trust Loop Validation

Because the available real business-report fixtures do not currently emit supported deterministic remediation categories, the preview/apply/rollback loop was validated against a concrete on-disk PBIR fixture that exercises the shipped mutation planner.

Observed:

- supported remediation titles:
  - `Clarify page purpose and narrative framing`
  - `Reduce visual density and align layout`
- deterministic fix opportunities generated successfully
- structured preview rows were present
- apply succeeded with validation-first behavior
- file backups existed before apply
- rollback restored original file contents
- automatic re-analysis executed after apply and after rollback
- `AppliedWithUnexpectedOutcome` surfaced correctly when the change did not clear the expected findings
- unsupported remediation remained advisory:
  - `Add benchmarks and decision context`

## Important Release Notes

- The tab-context reset issue was fixed for `0.3.0`; it is no longer a known limitation.
- Current real business-report fixtures do not yet produce supported deterministic opportunities in Phase 1, so the packaged real-report smoke covered:
  - score-panel open
  - advisory remediation behavior
  - log review
- The full trust loop was still validated end to end on a concrete PBIR fixture using the shipped backend and fix-engine modules.
