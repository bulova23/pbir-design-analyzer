# CI Failure Classification — 2026-08-21

## Source evidence

- Workflow: CI
- Commit: a67af7ad88bf2e7d6fd4bc162be84731d5ae1390
- Run: 32268663692
- Jobs: build-test on Ubuntu, Windows, and macOS
- Result: all three backend-test steps failed; extension builds and package-targets were skipped.

## Root causes

1. Phase35E timeout characterization used a 100 ms inner timeout and a 1 s outer cancellation. Under hosted-runner scheduling, the outer cancellation could win, and the runner intentionally classified external cancellation as Completed for its existing closed result contract. The test was timing-sensitive rather than proving the timeout path.
2. Canonical PBIR JSON and checked-in schema byte hashes were line-ending-sensitive on Windows. The repository had no .gitattributes policy, and indented JSON output/checksums could contain CRLF.
3. PbirLocalReportReader and PbirAuthoringEnvelopeReader received OS-native relative paths, while the PBIR contract uses forward-slash relative paths. This caused Windows identity/ownership and Phase 44 diagnostic failures.
4. Local generation used Path.IsPathFullyQualified for dataset validation. A rooted Unix-style path can be rooted but not fully qualified on Windows, allowing it through the v1 validation and failing later in the serializer with the wrong diagnostic code.
5. Phase35I path binding used raw session identifiers as directory names. The repository's session IDs contain colon separators, which are invalid Windows filename characters.
6. Phase35E tests used /usr/bin/true as a cross-platform test identity.
7. The portable Phase35I evidence test supplied job-assigned evidence while expecting the not-certified PartiallyProven status. The production status is evidence-driven; the test setup was claiming a Windows property without a measured runtime result.

## Changes made

- Canonical serializer output now normalizes CRLF to LF.
- .gitattributes pins repository text and JSON fixtures to LF.
- PBIR reader relative paths normalize to forward slashes.
- Dataset validation rejects rooted paths consistently.
- Session IDs are encoded as stable safe path segments.
- Phase35E test identity uses the test assembly instead of /usr/bin/true.
- Timeout test has a robust scheduling margin.
- Portable Phase35I evidence test no longer claims job assignment.
- Preview writer reports normalized contract paths.

## Verification

Targeted backend test filter passed: 111 passed, 0 failed, 0 skipped.

Full cross-platform CI rerun is required to confirm Windows and hosted-runner behavior. Local macOS cannot execute Windows-native Phase35I integration evidence.

## Follow-on stabilization evidence

- The sanitized `characterization-minimal` PBIR fixture and golden snapshot now freeze scores, findings, evidence, recommendations, diagnostics, readiness, and a deterministic fingerprint.
- The Darwin ARM64 VSIX was packaged and passed `scripts/verify-vsix.mjs`; it contained the matching 0.7.0 manifest and target backend entrypoint.
- Source-level architecture guards verify that the production composition root does not reach dormant Phase 35/provider/authoring infrastructure and that scoring does not depend on presentation or experimental infrastructure.
- The repository ESLint command now reports zero errors. The fixes were limited to unused imports/helpers and parameters, empty persisted-state type aliases, switch scoping, a non-constant report-root loop condition with equivalent fallback behavior, an ES-module PDF import, and test-only unsafe casts.
- Fast `.githooks/pre-commit` and moderate `.githooks/pre-push` controls are installed locally. Local hooks are convenience checks only; branch protection and CI remain the intended authoritative enforcement boundary.

## Final validation evidence — 2026-08-21

- `npm run lint`: passed with zero errors.
- `npm run compile`: passed.
- `npm test`: 100 extension suites passed, 532 tests passed; 11 webview suites passed, 68 tests passed.
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`: 1,033 passed, 11 expected Windows integration skips, 0 failed.
- `npm run package`: Darwin ARM64 VSIX packaged successfully.
- `npm run verify:vsix -- pbir-design-analyzer-0.7.0-darwin-arm64.vsix`: passed; 52 package entries, version 0.7.0, expected backend entrypoint.
- `npm run validate:release-contract`: passed for five targets at version 0.7.0.
- `npm run validate:protocol-contracts`: passed for three contracts.
- Ruby YAML parse of `.github/workflows/ci.yml` and `.github/workflows/release.yml`: passed.
- Architecture boundary filter: 2 passed, 0 failed.
- `git diff --check`: passed.

## Administrator handoff — GitHub repository rules not changed

No external GitHub settings were modified. An administrator must configure the following minimal protections on `main` before release merges are considered authoritative:

1. Require pull requests; prohibit direct pushes to `main`.
2. Require these CI status checks: `build-test (ubuntu-latest)`, `build-test (windows-latest)`, `build-test (macos-latest)`, and `package-targets`.
3. Require the branch to be up to date before merging, and disallow force-pushes and branch deletion.
4. Require conversation resolution and retain administrator bypass only for a documented emergency procedure.
5. Protect release tags matching `v*.*.*` from ordinary updates/deletion.

The local hooks are convenience checks and are not a security boundary; the required checks and repository rules must be enforced server-side.
