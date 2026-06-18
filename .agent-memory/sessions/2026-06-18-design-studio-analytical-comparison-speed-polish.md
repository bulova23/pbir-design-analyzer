# 2026-06-18 Design Studio Analytical & Comparison Speed Polish

## Objective

Reduce consultant reading burden and improve comparison speed in the current Design Studio shell without changing trust boundaries, workflow behavior, recommendation logic, or scoring.

## Scope

- Concept Comparison speed
- Compare Iterations speed
- Analytical Investigation readability
- Recommendation outcome visibility

## Constraints

- no provider-backed generation
- no Microsoft Skills integration
- no AI generation
- no new workflow stages
- no trust-boundary changes
- no recommendation logic changes
- no scoring changes

## Working Notes

- Session started.
- Loaded Round 6 findings, repo memory, and current Design Studio comparison surfaces.
- Confirmed current ConceptComparison leads with detailed list content before summary-level guidance.
- Confirmed current ClosedLoopView already contains comparison sections but still reads like an audit surface rather than a scan-first consultant summary.
- Confirmed canonical recommendation state already exists and should be reused instead of deriving new status logic in the UI.

## Validation Plan

- add failing Jest coverage before implementation
- run:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## Outcome

- Updated the current Design Studio webview only.
- Added summary-first Concept Studio comparison content before detailed comparison lists.
- Surfaced the analytical-investigation path earlier as:
  - Question
  - Investigation
  - Evidence
  - Conclusion
- Added a scan-first Compare Iterations summary layer with counts for:
  - accepted recommendations
  - rejected recommendations
  - deferred recommendations
  - outstanding recommendations
  - newly resolved issues
  - remaining issues
- Added an explicit `What Remains Unresolved` section to Compare Iterations.
- Added a recommendation-outcomes summary block to Refinement Studio.
- Changed proposed recommendation labeling in the UI to `Outstanding` while continuing to use the canonical recommendation state model.
- Added explicit `Why this matters` visibility on refinement proposals.

## Tests Added Or Updated

- `vscode-extension/webview-src/design-studio/__tests__/ConceptStudioView.test.tsx`
- `vscode-extension/webview-src/design-studio/__tests__/ClosedLoopView.test.tsx`
- `vscode-extension/webview-src/design-studio/__tests__/App.test.tsx`

## Validation

- `cd vscode-extension && npm test`
  - passed
- `cd vscode-extension && npm run compile`
  - passed
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - passed
  - existing nullable warnings were emitted in backend projects, but there were no test failures

## Next Recommended Step

- Re-run a consultant-style self-serve UAT check focused on whether the new summary layers reduce scan time enough in the analytical-investigation and iteration-comparison scenarios.
