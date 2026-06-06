# 2026-06-05 Windows ARM64 Release Gate

## Objective

- Determine whether PBIR Design Analyzer `0.5.0` can safely add Windows ARM64 packaging support before publication.

## Scope

- Review current packaging scripts and release workflow.
- Add `win32-arm64` / `win-arm64` support if feasible.
- Build and inspect the resulting VSIX artifacts.
- Update release-facing docs and repo memory with the decision.

## Constraints

- Do not publish the extension.
- Do not add product features.
- Do not change analyzer behavior or PBIR fix behavior.

## Validation Plan

- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- `cd vscode-extension && npm run package:all`
- Inspect all generated VSIX artifacts and backend binaries, including Windows ARM64 if produced.

## Outcome

- Windows ARM64 support is feasible and included for `0.5.0`.
- Added `win32-arm64` packaging and `win-arm64` backend publish support.
- Updated release workflow, support-matrix docs, and packaging scripts to include Windows ARM64.

## Validation Results

- Passed:
  - `cd vscode-extension && npx jest src/test/analyzerBackendClient.test.ts --runInBand`
  - `cd vscode-extension && node scripts/build-backend.mjs --target win32-arm64`
  - `cd vscode-extension && node scripts/package-vsix.mjs --target win32-arm64`
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package:all`
- Built VSIX artifacts:
  - `pbir-design-analyzer-0.5.0-win32-x64.vsix`
  - `pbir-design-analyzer-0.5.0-win32-arm64.vsix`
  - `pbir-design-analyzer-0.5.0-linux-x64.vsix`
  - `pbir-design-analyzer-0.5.0-darwin-x64.vsix`
  - `pbir-design-analyzer-0.5.0-darwin-arm64.vsix`
- Package inspection confirmed:
  - Windows x64 backend = PE32+ x86-64
  - Windows ARM64 backend = PE32+ Aarch64
  - Linux x64 backend = ELF x86-64
  - macOS x64 backend = Mach-O x86_64
  - macOS arm64 backend = Mach-O arm64

## Remaining Risk

- Live backend startup was not executed on real Windows x64, Windows ARM64, Linux x64, or macOS x64 hardware in this local macOS arm64 session.
