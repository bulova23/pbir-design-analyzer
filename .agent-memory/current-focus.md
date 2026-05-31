# Current Focus

## Active Branch

- Branch: `feat/semantic-color-chart-intent`

## Current Objective

- `0.2.0` is now merged into `main`, validated, and packaged. The next practical step is manual VS Code review of the packaged artifact, followed by future work on the deferred roadmap epics rather than more `0.2.0` feature churn.

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
- Environment preparation needed on `main` before final validation:
  - `cd vscode-extension && npm ci`
  - `dotnet build service-dotnet/RpcHost/RpcHost.csproj -c Release`
- Manual VS Code smoke pass was not rerun in this release-finalization session.

## Next Recommended Step

- Review the packaged `0.2.0` VSIX in VS Code, then start with roadmap Epic 1: Consultant Deliverables & Export Platform.
