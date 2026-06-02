# 2026-06-01 09:23 - Single-Page Fix Planner Gap

## Objective

Fix the `0.3.0` deterministic fix-opportunity pipeline so real page-level PBIR scoring can generate Phase 1 opportunities from single-page payloads, not only from report-level `pageScores` or controlled fixtures.

## Root Cause

- Real `Net Sales` page scoring on `Sales & Production.pbip` produced deterministic issues and supported remediation titles, but zero fix opportunities.
- The mutation planner only read `result.pageScores`.
- Single-page scoring returned:
  - `scoredPageName`
  - top-level `visualMetadata`
  - no `pageScores`
- Result: planner had no page visuals to inspect, so every supported category returned zero mutations.

## Changes

- Updated `vscode-extension/src/analyzer/fixes/fixMutationPlanner.ts` to normalize planning pages from either:
  - report-level `pageScores[]`
  - or single-page `scoredPageName + visualMetadata`
- Kept report-level behavior intact.
- Updated advisory-only UI copy in `vscode-extension/webview-src/analyzer-score/App.tsx` to be more honest:
  - `Advisory only: no safe metadata-only fix is currently available for this remediation.`
- Added regression coverage in:
  - `vscode-extension/src/test/fixOpportunityBuilder.test.ts`
    - single-page top-level metadata planning works
    - missing single-page visual metadata safely returns zero opportunities
  - `vscode-extension/webview-src/analyzer-score/App.test.tsx`
    - advisory-only copy reflects non-fixable remediation honestly

## Validation

- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`
- `cd vscode-extension && npx eslint webview-src/analyzer-score/App.tsx webview-src/analyzer-score/App.test.tsx src/views/PbirScorePanel.ts src/analyzer/fixes/*.ts`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## Real Fixture Results

Fixture:

- `/Users/bcrowell/Documents/GitHub/PBITesting/Sales & Production.pbip`

Full report:

- still advisory-only
- `fixPlanCount: 8`
- all titles: `Add benchmarks and decision context`
- `opportunityCount: 0`
- This remains expected because that remediation family is unsupported in Phase 1.

Page-level `Net Sales`:

- `scoredPageName: Net Sales`
- top-level `visualMetadata.visualCount: 35`
- fix plan titles:
  - `Add benchmarks and decision context`
  - `Clarify page purpose and narrative framing`
  - `Reduce visual density and align layout`
  - `Normalize cross-page standards`
- fix opportunities now appear:
  - `Reduce visual density and align layout (alignment)`
  - `mutationCount: 20`
  - `rollbackPlan.fileBackups.length: 10`
  - state starts as `Previewed`

Sample preview rows from the real report:

- `Net Sales · c22adfcca77c7baf5f51 · position.x: 22.88431061806656 -> 0`
- `Net Sales · c22adfcca77c7baf5f51 · position.y: 71.06180665610142 -> 64`
- `Net Sales · 449292750b491085de51 · position.x: 276.1290322580645 -> 256`

## Remaining Limitations

- Full-report `Sales & Production` still does not expose Phase 1 opportunities because its remediation set is dominated by unsupported `Add benchmarks and decision context`.
- `Net Sales` now exposes a real page-level workflow, but only for the supported layout/alignment family; the other page-level remediation items remain advisory-only.
- I did not package a new `.vsix` or run a fresh VS Code extension-host smoke check in this session.
