# Session Note - 2026-05-30 15:19:00

## Objective

Fix the GitHub push rejection on root `main`.

## Root Cause

- An unpublished local commit on `main`, `f3e5ba5`, accidentally tracked the downloaded VS Code test host under `.vscode-test/`.
- That commit included `.../Electron Framework`, which is `158.76 MB` and exceeds GitHub's `100 MB` file-size limit.
- The rejection was not an auth or GitHub outage issue; it was a repository content issue in local unpublished history.

## Changes Made

- Created safety branch `backup/main-before-vscode-test-fix` at the rejected local tip.
- Dropped the unpublished bad local commit from `main` by rebasing `main` back onto `origin/main`.
- Added ignore protection in `.gitignore`:
  - `.vscode-test/`
  - `**/.vscode-test/`
- Committed the prevention rule as:
  - `244db1a fix(git): ignore vscode test bundles`

## Validation

- Confirmed the rejected path from VS Code Git logs and local commit inspection.
- Verified the rewritten branch no longer included `.vscode-test/` in the push range.
- Ran `git push --dry-run origin main` successfully.
- Ran `git push origin main` successfully.

## Next Recommended Step

- Keep root `main` limited to small operational fixes.
- Continue feature work from `.worktrees/feat-semantic-color-chart-intent`.
