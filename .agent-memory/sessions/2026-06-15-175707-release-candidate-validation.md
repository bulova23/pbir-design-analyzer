# 2026-06-15 Release Candidate Validation

## Goal

- Perform PBIR Engineering Remediation Release Candidate Validation only.

## Scope Guardrails

- No new features
- No refactors
- Validation, packaging, install, smoke testing, and documentation only

## Progress

- Confirmed Workstream 9 completion from repo memory.
- Capturing fresh validation, packaging, install, and smoke-test evidence.
- Full required validation passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run verify:backend:targets`
  - `cd vscode-extension && npm run package:all`
- Installed `vscode-extension/pbir-design-analyzer-0.6.0-darwin-arm64.vsix` into a clean VS Code host using:
  - user data dir: `/tmp/pbir-rc-vscode-user`
  - extensions dir: `/tmp/pbir-rc-vscode-ext`
- Clean-host validation confirmed:
  - extension activation
  - packaged backend startup from the installed VSIX path
  - score panel rendering
  - export workflow success
  - screenshot upload dialog availability
- Clean-host validation found release blockers:
  - packaged Design Studio opens a blank webview and VS Code `main.log` reports a blocked `vscode-webview` request for `bcrowell.pbir-design-analyzer`
  - `PBIR Score Diagnostics` still persists a large scored payload log by default, including findings and local report paths
- Wrote the RC validation report:
  - `docs/pbir-engineering-remediation-release-candidate-validation.md`

## Outcome

- Recommendation: not ready for internal install.
