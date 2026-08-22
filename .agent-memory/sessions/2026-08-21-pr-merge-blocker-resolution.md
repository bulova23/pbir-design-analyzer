# PR Merge-Blocker Resolution — 2026-08-21

## Outcome

Resolved the GitHub PR status-context blocker without merging PR #6 or bypassing protections.

## Changes

- Updated ruleset `main-production-protection` (`21156915`) through the GitHub API, replacing stale required context `package-targets` with all five actual matrix contexts:
  - `package-targets (win32-x64)`
  - `package-targets (win32-arm64)`
  - `package-targets (linux-x64)`
  - `package-targets (darwin-x64)`
  - `package-targets (darwin-arm64)`
- Preserved `build-test (ubuntu-latest)`, `build-test (windows-latest)`, and `build-test (macos-latest)` requirements, active pull-request/deletion/non-fast-forward protections, and the separate security/release workflows.
- Removed the unsupported indefinitely queued `macos-13` runtime-acceptance leg from `.github/workflows/ci.yml` and `.github/workflows/release.yml`.
- Documented manual Rosetta runtime acceptance for darwin-x64 in `docs/current-state/RELEASING.md`.

## Verification

- PR commit: `72e7287698eb50be162b0eb23a76eb047bd405fc`.
- CI run: `32516901572`, success; all build-test, five package-target, release-gate, contract, and packaged runtime jobs present in the revised workflow passed.
- PR #6 state: OPEN, `CLEAN`, `MERGEABLE`; no merge performed.
- Rulesets `21156915` and `21156927`: active, no bypass actors, current account cannot bypass.

## Remaining action

Merge PR #6 through the protected workflow when authorized. Version alignment and release validation remain downstream release tasks.
