# 2026-08-21 — Solo-maintainer governance resolution and post-merge validation

## Governance

- Live ruleset verification confirmed `main-production-protection` (`21156915`) is active with zero required approvals, all eight required build/package contexts, strict branch currency, conversation resolution, stale-review dismissal, force-update/deletion protections, and zero bypass actors.
- `release-tag-protection` (`21156927`) remains active for `v*`, blocks tag deletion/non-fast-forward movement, and has zero bypass actors.
- The solo-maintainer exception was documented in `docs/release-evidence/v1.0-readiness-report.md` before merge.

## Merge evidence

- PR #6 head after documentation: `1d8889c26d1445f2b1b0ed45e74e6d937455e3e1`.
- Merge timestamp: `2026-08-21T20:05:34Z`.
- Merge commit/resulting protected main: `7389c63fced90eff3008d2a918bf2212354178f1`.
- Merge used `gh pr merge --merge`; no bypass or direct main push.

## Post-merge validation

- Hosted CI run `32521799882`: all build/test, release-contract, release-gates, five package-target, and Linux/Windows/macOS arm64 packaged-runtime jobs passed.
- Hosted Security run `32521799883`: dependency policy and CodeQL C#/TypeScript passed.
- Local merged-main validation: backend 1,034 passed/11 expected Windows skips; extension 532; webview 68; TypeScript; ESLint; contract freshness/6 compatibility fixtures; release contract; docs; characterization repeat 2/2; security policy; five-target package creation and VSIX verification all passed.
- Merged-main artifact manifest was regenerated with source commit `7389c63fced90eff3008d2a918bf2212354178f1`, version `0.7.0`, per-target package/backend hashes, tool versions, fixture identity, and documented SBOM/provenance limitations.

## Version alignment

- Created and pushed PR #7: `chore(release): align metadata for v1.0.0`.
- PR #7 targets protected `main` at `7389c63fced90eff3008d2a918bf2212354178f1`, updates extension/backend version metadata to `1.0.0`, updates current release docs/changelog, preserves historical evidence, and includes the minimal ESLint 8 pre-commit compatibility fix.
- PR #7 is open and its required checks are in progress. No tag, release, or Marketplace action was performed.
