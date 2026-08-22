# Controlled v1.0 Release Administration — 2026-08-21

## Outcome

Completed the protected v1.0.0 version-alignment merge and merged-commit release-candidate validation. Stopped at tag readiness as instructed; no tag, GitHub Release, Marketplace publication, or ruleset bypass was performed.

## Final evidence

- PR #7 head `80ecb000141b6d3e1e1340e4f03d1a158f740dd9` merged at `2026-08-21T22:28:40Z`.
- Merge commit and protected main: `4c56eaf37f4829640051ec121d9f6f5103aa7084`.
- Merged-main CI run `32533118593`: success, 13/13 jobs; Security run `32533118569`: success.
- All five package-target artifacts and hosted Linux x64, Windows x64, and macOS arm64 acceptance artifacts passed; package/backend hashes and artifact IDs were recorded in `docs/release-evidence/artifact-manifest.json`.
- Readiness report now records the final release-candidate section, PR merge evidence, run IDs, hashes, platform matrix, governance, release-note scope, limitations, and READY FOR TAG.
- Version consistency was verified remotely at the merged commit: extension, lockfile, backend metadata, release docs, and changelog are 1.0.0.
- Rulesets 21156915 and 21156927 are active with zero bypass actors; `v1.0.0` and GitHub Releases are absent.

## Verified

- Checkout remained on `codex/hosted-v1-readiness-validation-2026-08-21` at `926523995e8c352e5c551757e238d4bba7c6c563`; the worktree was already dirty and was not reset.
- PR #6 targets `main`, is open, has no merge commit, and has no approving review.
- Rulesets `main-production-protection` (`21156915`) and `release-tag-protection` (`21156927`) are active; bypass actors are empty and the current account cannot bypass.
- Fresh PR CI run `32512437742` passed required build/test/package contexts and security checks. The darwin-x64 native acceptance job remained queued at closeout.
- Prior readiness evidence remains at commit `926523995e8c352e5c551757e238d4bba7c6c563`, with approved limitations for Windows ARM64 package-only support and macOS Intel hosted cancellation/local Rosetta proof.
- Authoritative package version is `0.7.0` in `vscode-extension/package.json` and `package-lock.json`; release documentation also describes `0.7.0`, so it does not yet satisfy a v1.0.0 tag/version gate.

## Not performed

- No protected merge, tag, GitHub Release, Marketplace publication, ruleset modification, or local artifact upload.
- No product behavior, scoring baseline, or release evidence history was changed.

## Next action

The next authorized action is creation of protected `v1.0.0`, followed by GitHub Release and Marketplace publication using only the validated CI artifacts. This session intentionally stopped before those actions.

## Final release closeout

The session continued under explicit release authorization. The annotated tag `v1.0.0` was created at `2026-08-21T22:44:09Z` and resolves to `4c56eaf37f4829640051ec121d9f6f5103aa7084`. Tag-triggered Release run `32534183564` passed all build/package/native acceptance jobs and created GitHub Release `374710589`, published `2026-08-21T22:47:25Z`, with five 1.0.0 VSIX assets. The workflow's Marketplace step failed only because `VSCE_PAT` is not configured; no credential was printed, created, or stored.

Final release asset identities are recorded in `docs/release-evidence/v1.0.0-release-assets.json`; they are release-workflow rebuilds from the exact tagged commit and are intentionally recorded separately from the prior CI artifact hashes. The readiness report was updated with tag, release, workflow, Marketplace, governance, and platform evidence. Marketplace remains the sole administrative follow-up.
