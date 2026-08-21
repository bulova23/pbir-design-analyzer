# 2026-08-21 Hosted packaged acceptance validation

## Objective

Execute hosted native packaged-acceptance validation for v1.0 readiness, prove Windows runtime support, and update release evidence without tagging or publishing.

## Results

- Created validation PR #6 on branch `codex/hosted-v1-readiness-validation-2026-08-21`; no merge, tag, publication, or ruleset change was performed.
- Repaired only validation/release plumbing discovered by hosted execution: Windows test portability, Windows webview process spawning, Node 20 packaging compatibility, webview build ordering, and cross-shell VSIX verifier invocation.
- CI run `32511720555` at commit `8d652c4c36b0e6a351c33f846aa2f241223d2148` passed release gates, build-test on Ubuntu/Windows/macOS, all five package-target jobs, and packaged runtime acceptance for linux-x64, win32-x64, and darwin-arm64.
- Windows job `96864952609` on Windows Server 2025 proved `runtimeProof: true`, `runtimeMode: native`, deterministic fingerprint `9d3393ff372deb045ee228dabefa31053a8119ff9e4ae0ddddd66d3209b7c9a9`, and PASS for mutation, rollback, and export. Evidence artifact: `packaged-acceptance-win32-x64`.
- macOS Intel `macos-13` remained queued and was canceled after the required Windows, Linux, and macOS arm64 legs completed. Existing darwin-x64 Rosetta proof remains recorded; Windows ARM64 remains package-only.
- Updated `docs/release-evidence/packaged-acceptance-win32-x64.json` and the current section of `docs/release-evidence/v1.0-readiness-report.md`; historical readiness records were preserved.

## Validation

- Hosted: run `32511720555` key jobs PASS; macOS Intel leg canceled for runner availability.
- Local: extension Jest 532 passed, webview Jest 68 passed, verifier target-mode smoke passed, acceptance JSON parsed, `npm run validate:docs` passed, and `git diff --check` passed.

## Handoff

Do not tag or publish from this validation branch. Release administration must use the protected PR path and repeat the release workflow from the intended release commit. The readiness report documents the remaining platform limitations.
