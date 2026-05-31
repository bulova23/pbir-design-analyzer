# 0.2.0 Release Summary

## Release Scope

`0.2.0` finalizes the modernized PBIR Design Analyzer review workspace:

- Overview
- Issues
- Fix Plan
- Evidence
- secondary Export
- normalized findings
- smart collapse defaults
- intent confirmation and review feedback
- workspace review modes
- cross-page matrix navigation

## Architecture Snapshot

- scoring remains authoritative in the backend
- normalized findings remain the shared issue model
- overview/fix-plan/persona/matrix behaviors remain presentation-only
- Evidence preserves framework, metadata, and audit depth without dominating the default flow

## Release Guardrails

- no scoring rewrite
- no severity/confidence mutation
- no export redesign
- no deferred-epic implementation in `0.2.0`

## Validation Target

Release validation should include:

- `cd vscode-extension && npm run compile`
- `cd vscode-extension && npm test`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- package from `main`
- short VS Code smoke pass if practical

## Final Validation Record

- Merged curated feature payload into `main`
- Revalidated on `main` with:
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- Packaged from `main` with:
  - `cd vscode-extension && npm run package`
- Environment preparation required on `main` before the final validation rerun:
  - `cd vscode-extension && npm ci`
  - `dotnet build service-dotnet/RpcHost/RpcHost.csproj -c Release`

## Package Artifact

- `vscode-extension/pbir-design-analyzer-0.2.0.vsix`

## Known Release Gap

- A fresh manual VS Code smoke pass was not executed during the release-finalization session. Previous UAT history exists in earlier notes, but this final session stopped at compile/test/package validation.

## Next Practical Step

- Install and review the packaged `0.2.0` VSIX in VS Code, then move to roadmap Epic 1 rather than reopening `0.2.0` scope.
