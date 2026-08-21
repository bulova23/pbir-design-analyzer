# 2026-08-21 GitHub v1.0 governance verification

## Scope

Configure and verify GitHub repository rulesets only. No application code, tests, architecture, contracts, fixtures, or product scope were changed.

## Verification and changes

- Repository: `bulova23/pbir-design-analyzer`, default branch `main`.
- Initial read-only state differed from the expected handoff: rulesets 21156915 and 21156927 already existed and were active; the legacy branch-protection endpoint returned 404.
- The main ruleset incorrectly required stale/nonexistent logical check names. It was updated to the actual remote contexts: `build-test (ubuntu-latest)`, `build-test (windows-latest)`, `build-test (macos-latest)`, and `package-targets`.
- The tag ruleset was updated to protect `refs/tags/v*` from deletion and non-fast-forward movement. Tag creation remains available to support the release workflow.

## Evidence

- Main ruleset: ID `21156915`, active, target `refs/heads/main`.
- Tag ruleset: ID `21156927`, active, target `refs/tags/v*`.
- Both rulesets have no bypass actors.
- Post-write API verification passed at 2026-08-21 13:05:09 -04:00.
- Actual remote check contexts were independently read from the latest `main` commit.
- Release evidence updated at `docs/release-evidence/v1.0-readiness-report.md`.

## Outcome

GitHub administrator governance is verified PASS. Overall v1.0 readiness remains NOT READY because hosted native Windows packaged acceptance is still incomplete. No tag, release, Marketplace publication, or application-repository change was made.
