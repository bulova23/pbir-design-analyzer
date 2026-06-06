# 2026-06-05 Cross-Platform Scoring Determinism

## Objective

- Fix cross-platform scoring inconsistency before publishing `0.5.0`.

## Root Cause

- The backend still had nondeterministic filesystem fallbacks in the authoritative scoring path.
- Page fallback ordering used unsorted `Directory.GetDirectories(...)` results when `pages.json` was absent.
- Power BI Desktop-style visual loading from `visuals/*/visual.json` also used unsorted directory enumeration.
- Several scoring heuristics depend on page-leading or first-ranked items, so OS-specific enumeration order could shift the final score and downstream readiness findings.

## Fix

- Sorted fallback page directory enumeration with `StringComparer.Ordinal`.
- Sorted Desktop-style visual directory enumeration with `StringComparer.Ordinal`.
- Normalized parsed visual order before heuristics run using positional tie-breakers and stable visual IDs.
- Sorted tree-builder fallback page and visual enumeration so page selection/defaulting is deterministic too.
- Added extension-side deterministic score diagnostics:
  - report fingerprint with normalized separators, sorted paths, SHA-256 file hashes, and cache/generated-file exclusion
  - extension version, backend version, platform, architecture, analyzer metadata, page order, visual IDs, finding IDs, evidence counts, backend binary path, and backend target/runtime
- Added the `PBIR Design Analyzer: Copy Score Diagnostics` command and automatic PBIR Score Diagnostics output logging.
- Added a repo-side diagnostic comparison script:
  - `vscode-extension/scripts/compare-score-diagnostics.mjs`
- Sorted normalized findings deterministically so equal-severity findings do not depend on object/dictionary insertion order.
- Normalized remaining extension-side deterministic workflow orderings:
  - local PBIR tree fallback page/visual order
  - fix mutation planner fallback page/visual order
  - Fabric repo evidence file collection order

## Validation

- Passed targeted backend determinism tests:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirScoringServiceTests`
- Passed targeted extension determinism tests:
  - `cd vscode-extension && npx jest src/test/normalizedFindings.test.ts src/test/readinessFindings.test.ts src/test/scoreDiagnostics.test.ts --runInBand`
- Passed targeted extension workflow-order tests:
  - `cd vscode-extension && npx jest src/test/pbirScoreCommand.treeItem.test.ts src/test/scoreDiagnostics.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/fixMutationPlanner.test.ts src/test/pbirTreeProvider.localFallback.test.ts --runInBand`
- Passed required release validation:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package:all`

## Remaining Risk

- Cross-platform manual smoke has not been executed from this macOS session on real Windows ARM and a second supported platform using the same report copy.
- Publishing must remain blocked until matching report fingerprints are shown to produce matching diagnostics and score outputs on the real target machines.
- User-facing docs now describe the capture/compare workflow, but that workflow still needs real-machine evidence before the goal is complete.
