# 2026-06-10 0.6.0 Hardening Implementation

## Objective

Implement Recommended `0.6.0` only from the engineering hardening roadmap:

- shared repo snapshot seam
- async filesystem conversion
- Fabric evidence reuse
- protocol versioning and schema guards
- selected state validation
- scoring configuration externalization

Explicitly out of scope:

- new AI features
- new Fabric App features
- governance expansion
- deterministic fix changes
- report generation
- Design Studio work

## Startup Notes

- Goal created for the full `0.6.0` implementation, validation, packaging verification, docs, repo memory updates, and documented blocked-workspace smoke results.
- Authoritative roadmap sources:
  - `docs/superpowers/specs/2026-06-06-engineering-hardening-design.md`
  - `docs/superpowers/plans/2026-06-06-engineering-hardening-plan.md`
- Initial seam review:
  - `vscode-extension/src/analyzer/project/localTree.ts` is still synchronous
  - Fabric review evidence extractors still perform separate repo scans
  - score-panel host/webview messages have no explicit protocol or schema version
  - `selectedPageIndex` is not yet clamped or validated against payload shape

## Implementation Summary

- Added `vscode-extension/src/analyzer/project/repoSnapshot.ts` as the shared repository snapshot seam with cached text reads, explicit disposal, and analyzer-independent lifecycle.
- Moved Fabric review evidence extraction onto snapshot-backed async access:
  - `typescriptEvidence.ts`
  - `navigationEvidence.ts`
  - `designTokenEvidence.ts`
  - `screenshotEvidence.ts`
  - `semanticModelEvidence.ts`
  - `fabricAppReviewAnalyzer.ts`
- Converted the PBIR local fallback tree to async project discovery plus one snapshot-backed project read in:
  - `vscode-extension/src/analyzer/project/localTree.ts`
  - `vscode-extension/src/providers/PbirTreeProvider.ts`
- Added score-panel protocol/state hardening in:
  - `vscode-extension/src/views/scorePanelProtocol.ts`
  - `vscode-extension/src/analyzer/contracts/scorePanel.ts`
  - `vscode-extension/src/views/PbirScorePanel.ts`
  - `vscode-extension/webview-src/analyzer-score/App.tsx`
- Externalized Fabric review/readiness scoring constants with provenance and bounded override hooks in:
  - `vscode-extension/src/analyzer/fabric/config/fabricScoringConfig.ts`

## Focused Validation

- Passed:
  - `cd vscode-extension && npx jest src/test/repositorySnapshot.test.ts src/test/typescriptEvidence.test.ts src/test/navigationEvidence.test.ts src/test/designTokenEvidence.test.ts src/test/screenshotEvidence.test.ts src/test/semanticModelEvidence.test.ts src/test/fabricAppReviewAnalyzer.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/fabricScoringConfig.test.ts src/test/readinessScoring.test.ts src/test/scorePanelProtocol.test.ts webview-src/analyzer-score/App.test.tsx --runInBand`
  - `cd vscode-extension && npx jest src/test/pbirTreeProvider.localFallback.test.ts src/test/repositorySnapshot.test.ts src/test/fabricAppReviewAnalyzer.test.ts --runInBand`

## Final Validation

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package:all`
- VSIX inspection confirmed:
  - packaged version remained `0.5.0`
  - current target set remained:
    - Windows x64
    - Windows arm64
    - Linux x64
    - macOS x64
    - macOS arm64
  - packaged backend launchers remained target-specific
  - packaged manifest still uses `pbirAnalyzer.explorer`
  - packaged manifest still declares unsupported untrusted and virtual workspaces
  - no stale release-facing `powerbi-modeling.*` config keys or old explorer identifiers were reintroduced

## Workspace Posture Smoke

- Attempted actual untrusted-workspace runtime validation through `@vscode/test-electron` with:
  - a short `--user-data-dir`
  - `--disable-workspace-trust`
- Result:
  - the extension host still reported `vscode.workspace.isTrusted === true`
  - this environment therefore could not prove the blocked posture at runtime, even though the packaged manifest declares `supported: false`
- Virtual workspace runtime smoke remains unavailable here because no real virtual workspace provider/session is available locally.

## Closeout

- `0.6.0` implementation is complete on the active branch.
- Remaining external follow-up is runtime proof for:
  - actual untrusted-workspace blocked posture in an environment that can produce an untrusted file workspace
  - actual virtual-workspace blocked posture in an environment with a real virtual workspace provider
