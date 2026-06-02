# Current Focus

## Active Branch

- Branch: `codex/ux-consolidation-remediation-0-2-2`

## Current Objective

- `0.3.0` source now includes a follow-up fix for the single-page deterministic fix planner so page-level real-report analysis can surface supported opportunities from top-level `scoredPageName + visualMetadata`. The immediate next decision is whether to package and smoke-test this fix as a follow-up release.

## Release Boundaries

- Keep completed product code, tests, docs, roadmap specs/plans, and compact durable memory.
- Do not implement deferred roadmap epics in this release.
- Keep scoring authoritative and unchanged.
- Keep Evidence and Export secondary in the shipped workspace UX.
- Keep `.vscode-test/` and other generated test-host artifacts out of commits.

## Release Outcome

- `main` now includes the full `0.2.0` review-workspace release.
- Packaged artifact: `vscode-extension/pbir-design-analyzer-0.2.0.vsix`
- New packaged artifact for the UX consolidation follow-up: `vscode-extension/pbir-design-analyzer-0.2.1.vsix`
- New packaged artifact for the remediation follow-up: `vscode-extension/pbir-design-analyzer-0.2.2.vsix`
- New packaged artifact for deterministic fix opportunities: `vscode-extension/pbir-design-analyzer-0.3.0.vsix`
- Validation completed on `main`:
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package`
- Automated smoke pass completed in an isolated VS Code extension host against `Sales & Production.pbip`:
  - `pbirAnalyzer.scoreReport` opened `PBIR Optimization Report`
  - `pbirAnalyzer.configureScoring` opened `Design Analyzer Configuration`
  - `pbirAnalyzer.checkGovernance` returned without crashing the extension host
- Environment preparation needed on `main` before final validation:
  - `cd vscode-extension && npm ci`
  - `dotnet build service-dotnet/RpcHost/RpcHost.csproj -c Release`

## 0.3.0 Smoke Highlights

- Installed `vscode-extension/pbir-design-analyzer-0.3.0.vsix` into an isolated VS Code profile.
- Scored the real `Sales & Production` fixture in the packaged extension and confirmed advisory-only remediation behavior with no PBIR-specific renderer errors.
- Validated preview/apply/re-analysis/rollback on a concrete PBIR fix-opportunity fixture using the shipped deterministic engine.
- Fixed the refresh-driven tab reset so active page context survives apply/rollback re-analysis.

## Post-0.3.0 Planner Follow-Up

- Fixed the page-level planner gap where single-page scoring exposed top-level visual metadata but no `pageScores`, which previously blocked all real page-level opportunities.
- Revalidated the real `Sales & Production.pbip` fixture:
  - full-report scoring still remains advisory-only because it produces only unsupported `Add benchmarks and decision context` remediation
  - page-level `Net Sales` now produces one real deterministic opportunity:
    - `Reduce visual density and align layout (alignment)`
    - `mutationCount: 20`
    - `rollback backups: 10`
- Updated advisory-only webview copy to make unsupported or non-safe remediation items read more honestly.

## Next Recommended Step

- Package and smoke-test this single-page planner fix as a follow-up release if the user wants the real-report `Net Sales` workflow shipped.
- After that, decide whether the next work should target:
  - broader Phase 1 opportunity coverage for unsupported remediation families like `Add benchmarks and decision context`
  - or the next roadmap epic outside the fix-opportunity pipeline
