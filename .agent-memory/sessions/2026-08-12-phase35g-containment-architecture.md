# Phase 35G Containment Architecture Session

## Scope

Evidence-first comparison of local Apple Virtualization.framework guest execution and controlled Windows/Linux remote execution. No provider execution or production infrastructure.

## Evidence

- Phase 35F remains authoritative: `none-local-macos/v1`, `NotAdmitted`.
- Apple documents Virtualization.framework for macOS/Linux guests, explicit device configuration, no required network device, directory shares, entitlements, and VM lifecycle controls.
- Microsoft documents Power BI Desktop as a Windows application and PBIR as a documented editable report format.
- Repository provider and runtime layers remain descriptive/non-executable; the backend is packaged .NET launched by the extension, not an App Sandbox bundle.

## Decision

Selected `remote-controlled-execution/v1`, with Windows as the first worker profile and Linux as a separately certified profile. Added only a non-enabling decision contract, focused tests, and architecture documentation. No POC was necessary to distinguish the options because the Windows guest/platform blocker is decisive.

## Validation

- Phase35G focused xUnit: 2/2 passed.
- Phase35A–F focused xUnit regression: 67/67 passed.
- Full backend xUnit: 840/840 passed, 0 skipped.
- RPC regression: 119/119 passed.
- Extension Jest: 494/494 passed; webview Jest: 68/68 passed.
- TypeScript compilation, backend build, full extension build, VSIX packaging, and packaged backend target verification (5/5) passed.
- Boundary scan, placeholder scan, and `git diff --check` passed.
- `npm run lint` reports the unchanged repository baseline of 43 errors; no Phase35G TypeScript files are in the lint surface.
- No proof-of-concept was needed; no provider, fixture, or external execution ran.

## Git state

- HEAD and origin: `de268944` (`Add Phase 35D components and corresponding tests`).
- Phase 35A/B are represented by committed history; Phase 35C–G remain uncommitted and unstaged in this checkout.
- No files are staged and no commits were made.
- Existing Phase 35C–F files, generated backend outputs, and unrelated dirty files were preserved.
