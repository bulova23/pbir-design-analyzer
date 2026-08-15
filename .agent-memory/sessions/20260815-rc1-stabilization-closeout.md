# 2026-08-15 RC1 stabilization and product repositioning closeout

## Scope

Audited the frozen RC1 feature set at HEAD `6a1fe4eb`. No product capability,
backend contract, extension behavior, or implementation phase was added.

## Deliverables

- Refreshed `docs/releases/2026-08-15-0.6.0-rc1/VALIDATION-RESULTS.md` with
  current HEAD, fresh counts, warning details, and package sizes.
- Added `COMPETITIVE-ANALYSIS.md` for Microsoft Fabric Skills, Power BI
  authoring skills, Fabric CLI, and PBI Lens.
- Reorganized `V2-ROADMAP.md` into Design Policy Engine, AI Companion,
  Enterprise Governance, Platform, and Rendered Intelligence.
- Updated RC1 summary and known-issues evidence, including the historical
  Phase 35E timeout-test classification issue.

## Fresh validation

- Backend: 996 passed, 11 expected Windows skips, 0 failed.
- Focused Phase 35E: 9 passed, 0 skipped, 0 failed on macOS.
- Extension: 523 passed.
- Webview: 68 passed.
- RpcHost Release build: succeeded with existing nullable warnings, 0 errors.
- Production build: passed.
- `npm run package:all`: passed; five 0.6.0 VSIX packages produced.
- `npm run lint`: failed with the pre-existing 43-error baseline.
- Documentation required-file/link checks: passed.
- `git diff --check`: passed.

## Release state

Recommendation remains **Ready for UAT**, not limited release. Manual VS Code,
Windows, virtual-workspace, and product-owner acceptance remain outstanding.
No files are staged or committed. Generated target binaries were restored after
packaging; ignored VSIX artifacts remain in `vscode-extension/`.
