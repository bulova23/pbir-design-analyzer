# Optimization Report scoring contract — 2026-08-15

## Root-cause investigation

- Current HEAD `d9b421f3` had restored `PbirScorePanel.refresh()` to
  `model/pbir/scoreReport`, which requires `reportPath` in
  `AnalyzerRpcDispatcher.HandleScoreReportAsync`.
- The preceding handle-architecture implementation used the existing
  `pbir/authoring` adapter: Import returned an opaque snapshot, then Analyze
  resolved that snapshot in `PbirAuthoringRpcDispatcher.ResolveAnalyzeDirectory`.
- The exact bounded-object message is emitted only by
  `RpcHost/PbirAuthoringRpcAdapter.cs` when JSON-RPC params are null/non-object
  or oversized. Its test intentionally calls the adapter with null; it is not a
  valid scoring request failure.

## Boundary trace

- Webview/host request construction produces `pbir/authoring` Import with
  `sourceDirectory`, followed by Analyze with exactly one opaque snapshot handle.
- AnalyzerBridge sends both requests over the same JSON-RPC method; diagnostics
  record route, schema version, operation, source kind, path/handle presence,
  artifact presence, and authoring-request presence without values.
- RpcHost routes `pbir/authoring` through the bounded-object adapter. The adapter
  validates transport shape, schema version, and exposed operation; the core
  dispatcher validates the operation-specific payload.
- Analyze resolves the snapshot/artifact/reference in the backend and uses the
  host-composed scoring service. Legacy `model/pbir/scoreReport` remains a path
  compatibility adapter only.

## Implementation

- `PbirScorePanel` now delegates PBIR scoring to a typed Import → Analyze helper.
- The selected report path is supplied only to Import. Analyze receives the
  returned opaque snapshot handle, scoring config, and optional page name.
- Safe diagnostics identify operation, source kind, handle presence, and path
  presence without exposing path contents or opaque handle internals.
- Legacy path-based `model/pbir/scoreReport` remains registered for direct
  compatibility callers such as governance/export; it is no longer the
  Optimization Report scoring path.
- `AnalyzerRpcDispatcher` injects its existing project/scoring services into the
  authoring dispatcher, removing the second Analyze scorer/service composition.
- The adapter now rejects unsupported schema versions before typed payload
  dispatch; Generate/Import/Mutate/Analyze remain operation-specific.
- Added a real host dispatcher integration test that imports a schema-valid PBIR
  fixture and analyzes the returned snapshot handle.

## Validation

- Focused authoring/RPC tests: 28 passed.
- Extension Jest: 525 passed; webview Jest: 68 passed.
- Full backend Release: 999 passed, 11 expected Windows skips, 0 failed.
- TypeScript compile, production build, changed-file ESLint, `git diff --check`,
  and darwin-arm64 VSIX package passed.
- Exact darwin-arm64 VSIX installed through the local VS Code CLI. A real PBIR
  fixture was not configured, so Optimization Report score rendering was not
  manually verified.

## Disposition

- No commit or staging performed. The production build modified tracked target
  backend binaries; retain or restore them according to release packaging
  policy before handoff.
