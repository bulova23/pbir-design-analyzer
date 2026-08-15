# RC1 Validation Results

Date: 2026-08-15
HEAD: `6a1fe4eb` (`feat: Implement PBI Lens capability detection and rendered review integration`)

## Automated gates

| Check | Result |
| --- | --- |
| `dotnet test service-dotnet/tests/Tests.csproj -c Release --no-restore` | PASS — 996 passed, 11 skipped, 0 failed; skips are expected Windows integration tests on this host |
| `dotnet test service-dotnet/tests/Tests.csproj -c Release --no-restore --filter FullyQualifiedName~Phase35E` | PASS — 9 passed, 0 skipped, 0 failed on this host |
| `npm test -- --runInBand` | PASS — 523 extension tests and 68 webview tests passed |
| TypeScript compilation | PASS — run by npm test pretest and production build |
| `dotnet build service-dotnet/RpcHost/RpcHost.csproj -c Release --no-restore` | PASS — 26 existing nullable warnings in core, 0 errors |
| `npm run build` | PASS — backend publish, TypeScript, extension bundle, and all three webviews built |
| `npm run package:all` | PASS — five target VSIX packages produced |
| `npm run lint` | FAIL — existing repository baseline of 43 errors; no source changes were made for RC1 |
| `git diff --check` | PASS after final documentation edits |
| Documentation validation | PASS — required deliverables, headings, local links, and user-facing formatting checked |

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

Observed package sizes from this run:

- Windows x64: 2.68 MB
- Windows ARM64: 33.85 MB
- Linux x64: 2.64 MB
- macOS x64: 2.64 MB
- macOS ARM64: 2.64 MB

## Warnings and limits

The full backend test/build run emitted 26 existing nullable-reference
warnings in core code and four in existing test files. They did not fail the
build or tests. Full lint remains the known 43-error baseline. Manual UAT,
Windows execution-specific validation, and virtual-workspace runtime proof
remain outstanding.

A historical Phase 35E timeout-test run reported `Completed` instead of
`TimedOut`. The focused portable Phase 35E suite passes on this host, while
the Windows containment suite remains skipped on macOS. This is retained as a
test-environment limitation, not a shipped-product failure; Phase 35 execution
is not enabled in the RC1 product path.

## Release gate interpretation

Automated validation supports **Ready for UAT**. It does not support a claim of
limited release until the UAT guide is executed and the lint baseline and
platform limitations are explicitly accepted.
