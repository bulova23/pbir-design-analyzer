# 2026-06-06 Manual Marketplace Upload Research

## Goal

Determine the safest manual Visual Studio Marketplace upload procedure for the rebuilt `0.5.0` platform-targeted VSIX packages while PAT-based `vsce publish` is unavailable.

## Evidence Reviewed

- Official VS Code publishing documentation:
  - manual upload through the Visual Studio Marketplace publisher management page
  - platform-specific extensions published as separate packages
- Local `@vscode/vsce` `3.9.2` implementation:
  - duplicate detection checks `version` and `targetPlatform`
  - `publish --packagePath` extracts `TargetPlatform` from the VSIX manifest
- Direct inspection of rebuilt `0.5.0` VSIX manifests:
  - `win32-x64` package contains `TargetPlatform="win32-x64"`
  - `win32-arm64` package contains `TargetPlatform="win32-arm64"`

## Outcome

- Confirmed:
  - Marketplace manual upload is officially supported in general
  - platform-specific VSIX publication is officially supported
  - target-specific VSIX files carry the target in the package manifest
- Not explicitly documented:
  - portal-specific upload order requirements
  - explicit portal statement about append versus overwrite when uploading multiple target packages for the same version

## Release Guidance Added

Updated `docs/RELEASING.md` with:

- a platform-targeted manual upload procedure
- a recommended conservative upload order
- explicit watchpoints to stop if portal behavior appears replacement-oriented
- a documented split between:
  - official Marketplace behavior
  - `vsce` implementation inference

## Recommended Upload Order

1. `pbir-design-analyzer-0.5.0-win32-x64.vsix`
2. `pbir-design-analyzer-0.5.0-linux-x64.vsix`
3. `pbir-design-analyzer-0.5.0-darwin-x64.vsix`
4. `pbir-design-analyzer-0.5.0-darwin-arm64.vsix`
5. `pbir-design-analyzer-0.5.0-win32-arm64.vsix`

## Remaining Risk

- The public docs are more explicit about target-aware publishing through `vsce` than repeated manual portal uploads for one version.
- Manual upload remains acceptable for `0.5.0`, but should be done sequentially with verification after each upload.
