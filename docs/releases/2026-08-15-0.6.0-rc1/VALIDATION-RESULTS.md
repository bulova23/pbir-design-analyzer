# RC1 Validation Results

Date: 2026-08-15
HEAD: `4cbcf3918a2f414e1301cde851229af12bbec76d`

## Automated gates

| Check | Result |
| --- | --- |
| `dotnet test service-dotnet/tests/Tests.csproj -c Release --no-restore` | PASS — 996 passed, 11 skipped, 0 failed; skips are expected Windows integration tests on this host |
| `npm test -- --runInBand` | PASS — 505 extension tests and 68 webview tests passed |
| TypeScript compilation | PASS — run by npm test pretest and production build |
| `dotnet build service-dotnet/RpcHost/RpcHost.csproj -c Release --no-restore` | PASS — 0 warnings, 0 errors |
| `npm run build` | PASS — backend publish, TypeScript, extension bundle, and all three webviews built |
| `npm run package:all` | PASS — five target VSIX packages produced |
| `npm run lint` | FAIL — existing repository baseline of 43 errors; no source changes were made for RC1 |
| `git diff --check` | PASS for source/doc changes at validation time; rerun after final doc edits |
| Documentation validation | PASS when headings, links, and required deliverable files are checked after final edits |

## Packages

- `vscode-extension/pbir-design-analyzer-0.6.0-win32-x64.vsix`
- `vscode-extension/pbir-design-analyzer-0.6.0-win32-arm64.vsix`
- `vscode-extension/pbir-design-analyzer-0.6.0-linux-x64.vsix`
- `vscode-extension/pbir-design-analyzer-0.6.0-darwin-x64.vsix`
- `vscode-extension/pbir-design-analyzer-0.6.0-darwin-arm64.vsix`

Each package contains the extension manifest, backend, bundled extension,
configuration, resources, and analyzer/configuration/design-studio webviews.
The package manifest reports name `pbir-design-analyzer` and version `0.6.0`.
No debug, placeholder, scratch, fixture, or log paths were found in the VSIX
file lists inspected during packaging.

## Warnings and limits

The full backend test run emitted four nullable-reference warnings in existing
test files. They did not fail the build or tests. Full lint remains the known
43-error baseline. Manual UAT, Windows execution-specific validation, and
virtual-workspace runtime proof remain outstanding.

## Release gate interpretation

Automated validation supports **Ready for UAT**. It does not support a claim of
limited release until the UAT guide is executed and the lint baseline and
platform limitations are explicitly accepted.
