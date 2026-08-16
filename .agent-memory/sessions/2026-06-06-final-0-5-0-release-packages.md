# 2026-06-06 Final 0.5.0 Release Packages

## Objective

- Rebuild the final `0.5.0` VSIX package set from a clean state.
- Update release documentation for manual Marketplace upload.
- Inspect every package and confirm target isolation.

## Changes Made

- Updated `vscode-extension/scripts/package-vsix.mjs` so `package:all` now includes:
  - `win32-x64`
  - `win32-arm64`
  - `linux-x64`
  - `darwin-x64`
  - `darwin-arm64`
- Updated release-facing documentation:
  - `README.md`
  - `vscode-extension/README.md`
  - `docs/CHANGELOG.md`
  - `docs/ROADMAP.md`
  - `docs/RELEASING.md`
- Documented:
  - final `0.5.0` five-target package set
  - Windows arm64 support status
  - Windows arm64 self-contained backend note
  - icon rendering note
  - manual Marketplace upload instructions

## Clean Rebuild

- Removed old `0.5.0` VSIX files from `vscode-extension/`
- Removed `vscode-extension/backend/targets`
- Removed `vscode-extension/backend/rpc`
- Rebuilt using `cd vscode-extension && npm run package:all`

## Validation

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package:all`

## Final Package Inspection

- `pbir-design-analyzer-0.5.0-win32-x64.vsix`
  - size: `1.55 MB`
  - files: `45`
  - backend binary: `ModelingLanguageServer.exe`
  - backend type: `PE32+ executable x86-64`
  - backend RID: `.NETCoreApp,Version=v8.0/win-x64`
  - publish model: framework-dependent
  - icon included: yes
  - packaged icon matches source: yes
- `pbir-design-analyzer-0.5.0-win32-arm64.vsix`
  - size: `32.72 MB`
  - files: `226`
  - backend binary: `ModelingLanguageServer.exe`
  - backend type: `PE32+ executable Aarch64`
  - backend RID: `.NETCoreApp,Version=v8.0/win-arm64`
  - publish model: self-contained
  - icon included: yes
  - packaged icon matches source: yes
- `pbir-design-analyzer-0.5.0-linux-x64.vsix`
  - size: `1.52 MB`
  - files: `45`
  - backend binary: `ModelingLanguageServer`
  - backend type: `ELF x86-64`
  - backend RID: `.NETCoreApp,Version=v8.0/linux-x64`
  - publish model: framework-dependent
  - icon included: yes
  - packaged icon matches source: yes
- `pbir-design-analyzer-0.5.0-darwin-x64.vsix`
  - size: `1.52 MB`
  - files: `45`
  - backend binary: `ModelingLanguageServer`
  - backend type: `Mach-O x86_64`
  - backend RID: `.NETCoreApp,Version=v8.0/osx-x64`
  - publish model: framework-dependent
  - icon included: yes
  - packaged icon matches source: yes
- `pbir-design-analyzer-0.5.0-darwin-arm64.vsix`
  - size: `1.52 MB`
  - files: `45`
  - backend binary: `ModelingLanguageServer`
  - backend type: `Mach-O arm64`
  - backend RID: `.NETCoreApp,Version=v8.0/osx-arm64`
  - publish model: framework-dependent
  - icon included: yes
  - packaged icon matches source: yes

## Contamination Check

- No target contamination found.
- Each VSIX contains the expected backend binary type and RID for its declared target.
- `win32-arm64` remains the only self-contained package.
- The other four targets remain framework-dependent.

## Risks Remaining

- No Marketplace publish command was run.
- Live startup was not rerun from this macOS arm64 session against every rebuilt x64 target package.
- Manual release owner still needs to upload all five `0.5.0` VSIX files as one coherent Marketplace release set.
