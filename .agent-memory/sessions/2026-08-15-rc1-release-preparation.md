# 2026-08-15 — RC1 release preparation

## Scope

Prepared Release Candidate 1 for the frozen Phase 48 PBIR authoring platform.
No Phase 49 implementation or unrelated production changes were authorized.

## Evidence

- HEAD at start and close: `4cbcf3918a2f414e1301cde851229af12bbec76d`.
- Existing extension/package version: `0.6.0`.
- Backend: 996 passed, 11 expected Windows integration skips.
- Extension: 505 passed; webview: 68 passed.
- TypeScript compile, production build, RpcHost Release build, and package:all
  passed.
- Five target VSIXes were generated and inspected for version `0.6.0`, expected
  runtime/webview/config content, and no suspicious debug/placeholder paths.
- Full ESLint remains the known 43-error baseline; four nullable warnings were
  emitted by existing backend tests.

## Changes

Added ten untracked release documents under
`docs/releases/2026-08-15-0.6.0-rc1/`. Generated tracked target binaries were
restored after packaging. The VSIX files are ignored by git and remain local
for UAT.

## Closeout

All release preparation remains unstaged and uncommitted. Manual UAT,
Windows-specific execution validation, and product-owner sign-off remain
outstanding. The next agent should not start Phase 49 or create a release
commit.
