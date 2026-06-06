# Current Focus

## Active Branch

- Branch: `codex/ux-consolidation-remediation-0-2-2`

## Current Objective

- Finalize the `0.5.0` release package set and documentation for manual Marketplace upload.
- Keep publication manual. Do not publish from the repo workflow during final release prep.

## In Progress

- No active code work remains in the local session.
- Final `0.5.0` documentation now reflects the five-target package set, the Windows arm64 self-contained backend note, the icon rendering note, and manual Marketplace upload steps.
- Manual Marketplace research now distinguishes:
  - officially documented support for manual VSIX upload
  - officially documented support for platform-specific packages as separate Marketplace packages
  - under-documented portal behavior for repeated same-version target uploads, mitigated by a conservative sequential upload procedure
- Clean rebuild completed for:
  - `pbir-design-analyzer-0.5.0-win32-x64.vsix`
  - `pbir-design-analyzer-0.5.0-win32-arm64.vsix`
  - `pbir-design-analyzer-0.5.0-linux-x64.vsix`
  - `pbir-design-analyzer-0.5.0-darwin-x64.vsix`
  - `pbir-design-analyzer-0.5.0-darwin-arm64.vsix`
- Package inspection confirmed:
  - `win32-x64`, `linux-x64`, `darwin-x64`, and `darwin-arm64` are framework-dependent
  - `win32-arm64` is intentionally self-contained
  - packaged icon matches the source icon byte-for-byte on all targets
  - no target contamination was observed in backend payloads

## Blockers

- Real live startup smoke for every x64 target was not rerun from this local macOS arm64 session during the final packaging pass.
- Marketplace publication is intentionally still pending manual user action.

## Validation Status

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package:all`
- Added and validated determinism hardening:
  - backend fallback page enumeration now sorts deterministically
  - backend Desktop-style visual enumeration now sorts deterministically
  - backend visual ordering now normalizes positional ties before heuristics run
  - tree-builder fallback ordering is deterministic for page selection/defaulting
  - score diagnostics now capture extension/backend version, platform, architecture, analyzer metadata, overall framework scores, page order, per-page framework scores, findings, evidence counts, backend binary path, and backend target/runtime
  - report fingerprinting now hashes normalized PBIR source paths with sorted SHA-256 file entries and excludes generated/cache files
  - `PBIR Design Analyzer: Copy Score Diagnostics` now exports the active diagnostic snapshot
- Confirmed:
  - `win32-arm64` VSIX size delta is caused by a self-contained .NET 8 runtime payload in `backend/rpc`
  - `darwin-arm64`, `darwin-x64`, `linux-x64`, and `win32-x64` current target packages are framework-dependent
  - Windows 11 ARM scoring succeeded with `backendRuntimeId: win-arm64`, `backendTarget: win32-arm64`, and `resultSource: freshAnalysis`
  - final rebuilt VSIX inspection results:
    - `win32-x64`: `1.55 MB`, `45` files, PE32+ x86-64 backend, framework-dependent
    - `win32-arm64`: `32.72 MB`, `226` files, PE32+ Aarch64 backend, self-contained
    - `linux-x64`: `1.52 MB`, `45` files, ELF x86-64 backend, framework-dependent
    - `darwin-x64`: `1.52 MB`, `45` files, Mach-O x86_64 backend, framework-dependent
    - `darwin-arm64`: `1.52 MB`, `45` files, Mach-O arm64 backend, framework-dependent
- Added and validated regression coverage:
  - backend page-order fallback test without `pages.json`
  - backend deterministic Desktop-style visual ordering test
  - extension normalized-finding ordering test
  - extension fingerprint stability and path-normalization tests
  - extension score-diagnostics command tests
  - extension framework-score diagnostics mapping test
  - extension local-tree fallback ordering test
  - extension fix-mutation-planner fallback ordering test

## Release Boundaries

- No new analyzer features beyond determinism diagnostics and fingerprinting.
- Manual Marketplace upload only for `0.5.0`.
- Treat the icon light-tile behavior in VS Code extension details as renderer behavior, not a package blocker, while the packaged icon matches the source file.

## Next Recommended Step

- Manually upload the five `0.5.0` VSIX files sequentially through the publisher management page when ready, following the documented conservative order in `docs/RELEASING.md`.
- If a final pre-publish smoke pass is desired, validate backend startup on Windows x64, Linux x64, macOS x64, macOS arm64, and Windows arm64 using the rebuilt packages.
- Keep the Windows arm64 self-contained note in release-facing documentation for `0.5.0`.
