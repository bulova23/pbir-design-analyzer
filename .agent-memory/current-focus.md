# Current Focus

## Active Branch

- Branch: `feat/semantic-color-chart-intent`

## Current Objective

- `0.2.0` is now merged into `main`, validated, packaged, and smoke-checked in an isolated VS Code host. The next practical step is future work on the deferred roadmap epics rather than more `0.2.0` churn.

## Release Boundaries

- Keep completed product code, tests, docs, roadmap specs/plans, and compact durable memory.
- Do not implement deferred roadmap epics in this release.
- Keep scoring authoritative and unchanged.
- Keep Evidence and Export secondary in the shipped workspace UX.
- Keep `.vscode-test/` and other generated test-host artifacts out of commits.

## Release Outcome

- `main` now includes the full `0.2.0` review-workspace release.
- Packaged artifact: `vscode-extension/pbir-design-analyzer-0.2.0.vsix`
- Validation completed on `main`:
  - `cd vscode-extension && npm run compile`
  - `cd vscode-extension && npm test`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package`
- Automated smoke pass completed in an isolated VS Code extension host against `Sales & Production.pbip`:
  - `pbirAnalyzer.scoreReport` opened `PBIR Optimization Report`
  - `pbirAnalyzer.configureScoring` opened `Design Analyzer Configuration`
  - `pbirAnalyzer.checkGovernance` returned without crashing the extension host
- Environment preparation needed on `main` before final validation:
  - `cd vscode-extension && npm ci`
  - `dotnet build service-dotnet/RpcHost/RpcHost.csproj -c Release`

## Next Recommended Step

- Start with roadmap Epic 1: Consultant Deliverables & Export Platform.
