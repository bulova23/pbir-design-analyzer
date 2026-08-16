# 2026-06-16 Story Assessment Navigation Target Fix

## Goal

Fix the Story Assessment `Open target` action so Top Story Improvements can reveal their page or visual target reliably.

## Root Cause

- The webview action was already posting a valid `navigateToTarget` message.
- The score-panel host router was already validating that payload and routing it to `revealNavigationTargetInPbirExplorer`.
- The failure was earlier in command-to-explorer state:
  - when `pbirAnalyzer.scoreReport` opened a report chosen from the picker, it did not sync `pbirTreeProvider` to that report path
  - navigation targets then resolved against an empty or unrelated explorer tree
- This made the Story Assessment button appear non-functional even though the click handler and protocol were intact.

## Changes

- Added `syncExplorerToReport(reportPath)` in `vscode-extension/src/commands/pbirCommands.ts`.
- Call it before opening the score panel from:
  - `pbirAnalyzer.scoreReport`
  - `pbirAnalyzer.exportReviewWorkflow`
  - `pbirAnalyzer.uploadScreenshots`

## Regression Coverage

- Added a failing-first regression to `vscode-extension/src/test/pbirScoreCommand.treeItem.test.ts`:
  - picker-based score launches must call `setProjectPath(reportRoot)` before `PbirScorePanel.createOrShow(...)`

## Validation

- Focused:
  - `cd vscode-extension && npx jest --runTestsByPath src/test/pbirScoreCommand.treeItem.test.ts`
  - `cd vscode-extension && npx jest --runTestsByPath src/test/pbirScorePanel.navigation.test.ts src/test/pbirExplorerReveal.test.ts`
- Required:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`

## Notes

- No scoring logic changed.
- No navigation-target heuristic changed.
- Manual VS Code recheck is still recommended after reloading the extension host or reinstalling the VSIX if the installed build is being tested.
