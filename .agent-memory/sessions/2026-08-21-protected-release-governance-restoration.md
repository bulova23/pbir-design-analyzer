# Protected Release Governance Restoration — 2026-08-21

## Outcome

Restored the required one-approval governance policy and stopped before merge because no independent authorized reviewer is available.

## Verified

- `main-production-protection` ruleset `21156915` is active with `required_approving_review_count: 1`.
- Required contexts remain the three build-test jobs and five package-target matrix jobs.
- `release-tag-protection` ruleset `21156927` remains active for `v*`, with deletion and non-fast-forward protection.
- Both rulesets have no bypass actors; the current account cannot bypass.
- PR #6 head is `72e7287698eb50be162b0eb23a76eb047bd405fc`; it is OPEN, `REVIEW_REQUIRED`, and `BLOCKED` despite passing CI run `32516901572`.
- `main` remains `a67af7ad`; no merge commit exists.
- Collaborator API returned only `bulova23` with push/admin permission. The owner’s `reviewed` comment is not an approving review.

## Not performed

- No self-approval, bypass, merge, version update, post-merge validation, tag, GitHub Release, or Marketplace publication.

## Next action

Obtain an independent authorized approval, or make a separately authorized governance decision to change the solo-maintainer policy. Then merge through the protected workflow and rerun release validation from the resulting protected `main` commit before version alignment.
