# 2026-06-01 22:05 - AI Fix Phase 1 Follow-Up And Phase 2 Planning

## Objective

Finish the remaining Phase 1 AI Fix Opportunities release follow-up work, package the single-page planner fix as a new release artifact, and create the Phase 2 hardening design spec and implementation plan without starting implementation.

## Phase 1 Follow-Up Decisions

- Did not extract `vscode-extension/webview-src/analyzer-score/fixOpportunities.ts`.
- Rationale:
  - the current fix-opportunity UI logic is still contained enough inside `App.tsx`
  - extracting only to match the original plan would create churn without improving trust, safety, or release scope

## What Changed

- Bumped the extension from `0.3.0` to `0.3.1` in:
  - `vscode-extension/package.json`
  - `vscode-extension/package-lock.json`
- Updated release-facing docs:
  - `docs/CHANGELOG.md`
  - `README.md`
  - `vscode-extension/README.md`
  - `docs/ROADMAP.md`
- Added new Phase 2 planning docs:
  - `docs/superpowers/specs/2026-06-01-ai-fix-phase2-hardening-design.md`
  - `docs/superpowers/plans/2026-06-01-ai-fix-phase2-hardening-plan.md`
- Updated the reconciled Phase 1 implementation plan to record the explicit helper-extraction decline rationale.

## Packaging And Validation

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package`
- New packaged artifact:
  - `vscode-extension/pbir-design-analyzer-0.3.1.vsix`

## Smoke Evidence

- Installed `vscode-extension/pbir-design-analyzer-0.3.1.vsix` into an isolated VS Code profile:
  - user data dir: `/tmp/pbir-analyzer-0.3.1-smoke.v83uiL/user`
  - extensions dir: `/tmp/pbir-analyzer-0.3.1-smoke.v83uiL/ext`
- Opened `/Users/bcrowell/Documents/GitHub/PBITesting` in the isolated profile and ran `PBIR Design Analyzer: Score Report`.
- Verified the packaged extension opened `PBIR Optimization Report` against the real `Sales & Production` fixture.
- Captured the live packaged window:
  - `/tmp/pbir-smoke-captures/full-report.png`
- Verified full-report behavior:
  - packaged score panel opened successfully
  - full report remained advisory-only for deterministic fixes on the real fixture
  - no PBIR-specific renderer errors were observed in the isolated profile logs
- Verified installed-extension single-page behavior with an isolated extension-host harness plus log inspection:
  - the installed `0.3.1` extension issued a score request with `pageName: "Net Sales"`
  - the response included top-level `scoredPageName: "Net Sales"`
  - the response included top-level `visualMetadata` for `Net Sales`
  - this confirms the packaged follow-up consumed single-page `scoredPageName + visualMetadata` instead of relying on `pageScores`

## Remaining Limitations

- The real full-report `Sales & Production` fixture still emits unsupported `Add benchmarks and decision context` remediation, so full-report deterministic opportunities remain advisory-only in Phase 1.
- The real page-level `Net Sales` workflow exposes supported deterministic opportunities for the layout/alignment family, but other remediation families on the page still remain advisory-only.
- Phase 2 planning is complete, but no Phase 2 implementation work has started.
