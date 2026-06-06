# 2026-06-05 Windows ARM64 Scoring Failure

## Objective

- Fix the Windows 11 ARM scoring failure for the `0.5.0` `win32-arm64` package, or defer Windows ARM64 support safely before publication.

## Problem Statement

- The Windows ARM64 VSIX installs and the PBIR tree loads, but Score Report fails with the score panel message:
  - `LSP bridge not available. Is the .NET service running?`

## Investigation Scope

- Verify backend target packaging and runtime mapping.
- Add diagnostics for backend launch, runtime detection, and handshake failure.
- Determine whether a framework-dependent backend is the likely root cause on Windows ARM64.
- Keep or remove `win32-arm64` from release packaging based on confidence.

## Constraints

- Do not publish `0.5.0`.
- Do not add new product features.
- Do not change analyzer behavior or PBIR fix behavior.

## Outcome

- Windows ARM64 is deferred from the public `0.5.0` release target list.
- The failure is isolated to backend startup and bridge readiness, not extension activation or webview loading.
- Diagnostics and lifecycle handling were improved so startup failures surface actionable details instead of only the generic missing-bridge message.

## Root Cause Assessment

- Packaging target and binary architecture were correct:
  - the Windows ARM64 VSIX contained `ModelingLanguageServer.exe` as PE32+ Aarch64
- The likely failure class was backend startup before LSP readiness:
  - real-device evidence showed language-client `starting` / `startFailed` lifecycle errors
  - the score panel only saw an absent bridge and reported a generic LSP message
- Runtime ambiguity remained a credible cause:
  - the original Windows ARM64 backend packaging was framework-dependent
  - this session added a self-contained Windows ARM64 private test package to reduce runtime-dependency ambiguity for follow-up validation

## Changes

- Added backend launch diagnostics for:
  - selected target
  - `process.platform`
  - `process.arch`
  - resolved backend path
  - backend existence
  - backend binary type
  - launch command
  - dotnet detection
  - preflight exit code and stderr/stdout first lines
  - handshake failure reason
- Added backend launch preflight so obvious startup failures are detected before the language client enters `startFailed`.
- Improved score-panel error messaging to surface the recorded backend startup issue instead of only `LSP bridge not available`.
- Prevented unnecessary shutdown `stop()` calls for clients that never reached the running state.
- Kept a private Windows ARM64 packaging path available via:
  - `cd vscode-extension && node scripts/package-vsix.mjs --target win32-arm64`
- Removed Windows ARM64 from:
  - `package:all`
  - release workflow target matrix
  - public supported-platform docs for `0.5.0`

## Validation Results

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package:all`
- Built and inspected supported release artifacts:
  - `pbir-design-analyzer-0.5.0-win32-x64.vsix`
  - `pbir-design-analyzer-0.5.0-linux-x64.vsix`
  - `pbir-design-analyzer-0.5.0-darwin-x64.vsix`
  - `pbir-design-analyzer-0.5.0-darwin-arm64.vsix`
- Built and inspected private Windows ARM64 investigation artifact:
  - `pbir-design-analyzer-0.5.0-win32-arm64.vsix`
  - contains `ModelingLanguageServer.exe` as PE32+ Aarch64
  - uses a self-contained backend publish

## Remaining Risk

- This session could not execute the required Windows 11 ARM smoke test, so Windows ARM64 must not be published as supported in `0.5.0`.
