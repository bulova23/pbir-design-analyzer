# 2026-06-14 16:33:41 ET — PBIR Engineering Remediation Bucket A

## Scope

- Implement Bucket A only from:
  - `docs/superpowers/specs/2026-06-14-pbir-engineering-remediation-design.md`
  - `docs/superpowers/plans/2026-06-14-pbir-engineering-remediation-plan.md`
- Included workstreams:
  - Workstream 1 — JSON-RPC framing fix
  - Workstream 3 — RPC logging redaction
  - Workstream 2A — score payload required-field validation
  - Workstream 4A — backend fallback cleanup
  - Workstream 5 — backend preflight cleanup
- Excluded:
  - `PbirScorePanel` decomposition
  - `PbirScoringService` decomposition
  - contract codegen/schema migration
  - fix engine persistence refactor
  - Design Studio backend abstraction cleanup
  - provider-backed generation
  - new product features

## Start State

- Read:
  - `AGENTS.md`
  - `.agent-memory/current-focus.md`
  - `.agent-memory/repo-map.md`
  - `.agent-memory/do-not-do-this.md`
  - `.agent-memory/failure-patterns.md`
  - remediation spec and plan
- Confirmed active branch:
  - `codex/ux-consolidation-remediation-0-2-2`
- Confirmed workspace state:
  - normal checkout, not an existing linked worktree
- Initial root-cause targets:
  - `service-dotnet/RpcHost/Program.cs` reads JSON-RPC bodies by character count, not byte count
  - `vscode-extension/src/services/rpc/AnalyzerBridgeService.ts` logs full params and responses by default
  - `vscode-extension/src/views/scoreResultPayload.ts` silently coerces required values to `0`, `false`, or empty collections
  - `vscode-extension/src/languageServer/analyzerBackendClient.ts` resolves repo-local Debug and Release fallback binaries
  - `vscode-extension/src/extension.ts` runs backend launch preflight by spawning the backend before the real client launch

## Execution Notes

- Plan:
  - add focused failing tests first
  - implement minimal Bucket A fixes
  - run focused validation after each material change
  - run required full validation and packaging last

## Implemented

- `service-dotnet/RpcHost/Program.cs`
  - changed JSON-RPC request reading from character-counted text reads to byte-counted stream framing
  - preserved protocol methods and response envelope semantics
  - added `JsonRpcFraming` helper and test visibility through `InternalsVisibleTo("Tests")`
- `vscode-extension/src/services/rpc/AnalyzerBridgeService.ts`
  - removed default payload logging for request params and responses
  - kept method, correlation, elapsed-time, and error logging
  - enabled payload logging only when `PBIR_ANALYZER_RPC_DIAGNOSTIC_MODE=true`
  - redacted paths, content/json payloads, findings, and evidence
  - cleared timeout handles to avoid lingering test/process handles
- `vscode-extension/src/views/scoreResultPayload.ts`
  - added explicit required-field validation for authoritative score payload numbers, strings, arrays, and nested required booleans in provided optional structures
  - preserved normalization behavior for valid payloads and truly optional absent structures
- `vscode-extension/src/languageServer/analyzerBackendClient.ts`
  - removed normal runtime backend fallback to repo-local Debug and Release outputs
  - kept packaged backend resolution only
  - added startup-preparation helper and explicit preflight gate
- `vscode-extension/src/extension.ts`
  - changed startup to run launch preflight only when `PBIR_ANALYZER_ENABLE_BACKEND_PREFLIGHT=true`
  - normal activation now uses a single backend launch path

## Focused Validation

- Passed:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~RpcHostJsonRpcTests`
  - `cd vscode-extension && npx jest --runTestsByPath src/test/AnalyzerBridgeService.test.ts src/test/analyzerBackendClient.test.ts src/test/scoreResultPayload.test.ts`

## Required Validation

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm run package:all`

## Notes

- `npm run package:all` rebuilt packaged backend target assets under `vscode-extension/backend/targets/`.
- Existing nullable warnings still appear during backend build/package in:
  - `service-dotnet/Services/Pbir/PbirScoringService.cs`
  - `service-dotnet/Services/Pbir/CrossPageNarrative/CrossPageNarrativeInputBuilder.cs`
- Those warnings predate Bucket A and were not expanded in scope here.

## End State

- Bucket A complete.
- No Bucket B, C, or D work started.
- Stop after Bucket A as requested.
