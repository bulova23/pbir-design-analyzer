# Optimization Report authoring-RPC regression — 2026-08-15

## Investigation

- The exact error is thrown only in `service-dotnet/RpcHost/PbirAuthoringRpcAdapter.cs` when JSON-RPC params are not a bounded object. It is a host-wire validation failure, before `PbirAuthoringRpcDispatcher.ValidateRequest`.
- `PbirAuthoringRpcDispatcher` already has operation-specific payload validation. Analyze requires one report directory, artifact handle, or snapshot handle; it does not require `PbirAuthoringGenerationRequest`.
- Commit `b31024ff` changed `vscode-extension/src/views/PbirScorePanel.ts` from `model/pbir/scoreReport` to authoring `Import` then `Analyze`, making ordinary scoring depend on the authoring route.

## Decision

Restore the Optimization Report to `model/pbir/scoreReport`; keep explicit Generate/Import/Mutate/Analyze authoring workflows and their contracts unchanged. Add tests before implementation.

## Changes

- `PbirScorePanel` no longer imports a report through `pbir/authoring` during ordinary Optimization Report refreshes. It sends the existing `model/pbir/scoreReport` request with report path, scoring config, and optional page name.
- Explicit authoring commands retain Generate, Import, Analyze, and curated Mutate behavior. No dispatcher or RPC contract compatibility hack was added.
- Added regression tests for the score request boundary, missing adapter params, and Analyze-specific dispatcher validation.

## Validation

- Focused extension tests: 17 passed.
- Full extension Jest: 524 passed; webview Jest: 68 passed.
- Full backend Release: 998 passed, 11 expected Windows integration skips.
- Production build passed.
- VSIX packaging passed.
- `git diff --check` passed.
