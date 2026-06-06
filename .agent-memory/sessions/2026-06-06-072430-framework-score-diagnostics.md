# 2026-06-06 Framework Score Diagnostics Expansion

## Objective

- Continue the `0.5.0` cross-platform scoring investigation after matching report fingerprints proved the remaining discrepancy is not caused by file enumeration or finding ordering.

## What Changed

- Expanded score determinism diagnostics to include:
  - overall framework scores from `ScoreResult`
  - per-page framework scores for every `PageScore`
- Kept the change presentation-only in diagnostics:
  - no scoring logic changed
  - no backend contract changed
  - no analyzer behavior changed
- Added focused regression assertions covering the new diagnostics fields.

## Validation

- Passed:
  - `cd vscode-extension && npx jest src/test/scoreDiagnostics.test.ts --runInBand`

## Findings

- Latest Windows ARM64 and macOS ARM64 captures now share the same report fingerprint:
  - `7badb276d6930febb802eef91bc0282fed9c8e6de01f1b189f427556e0d251db`
- They also share:
  - `issueCount`
  - `severityCounts`
  - `readinessScore`
  - `readinessBand`
  - `pageProcessingOrder`
  - page `visualIds`
  - normalized findings
  - `evidenceCount`
- Remaining mismatch is numeric scoring only:
  - Windows ARM64 overall score: `70.14`
  - macOS ARM64 overall score: `70.79`

## Next Step

- Re-run `PBIR Design Analyzer: Copy Score Diagnostics` on both machines with the updated build.
- Compare the new `overallFrameworkScores` and `pageSnapshots[].frameworkScores` payloads to identify the first drifting framework component.
