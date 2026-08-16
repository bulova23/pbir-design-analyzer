# PBIR Engineering Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remediate the principal-architect repository review findings through staged reliability, security, contract, runtime, and decomposition work without changing product scope or trust boundaries.

**Architecture:** Land the work in dependency order. First harden runtime correctness, contract validation, logging, backend resolution, and startup behavior. Then harden persistence and contract-generation direction. Only after those foundations are stable should the team decompose `PbirScorePanel` and `PbirScoringService`. Treat speculative backend Design Studio abstractions as a separate cleanup decision rather than mixing them into critical runtime fixes.

**Tech Stack:** TypeScript, React webviews, VS Code extension host, .NET 8 backend, Jest, xUnit, VSIX packaging scripts

---

## Scope Guardrails

- This is implementation planning only.
- Do not implement code in this planning turn.
- Do not remove files in this planning turn.
- Preserve protocol compatibility unless a task explicitly documents a versioned compatibility change.
- Preserve scoring authority, deterministic mutation authority, and Design Studio trust boundaries.
- Do not begin provider-backed generation.

## Source Review Anchors

The plan is grounded in the repository review findings tied to these files:

- `service-dotnet/RpcHost/Program.cs`
- `service-dotnet/Services/Pbir/PbirScoringService.cs`
- `vscode-extension/src/views/PbirScorePanel.ts`
- `vscode-extension/src/views/scoreResultPayload.ts`
- `vscode-extension/src/services/rpc/AnalyzerBridgeService.ts`
- `vscode-extension/src/languageServer/analyzerBackendClient.ts`
- `vscode-extension/src/extension.ts`
- `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts`
- `service-dotnet/Services/DesignStudio/Models/DesignStudioModels.cs`
- `vscode-extension/src/design-studio/contracts/designStudioModels.ts`
- `vscode-extension/src/design-studio/contracts/designStudioProtocol.ts`
- `service-dotnet/Services/DesignStudio/Providers/IDesignStudioProvider.cs`
- `service-dotnet/Services/DesignStudio/Providers/ProviderCapabilityModels.cs`
- `service-dotnet/Services/DesignStudio/Materialization/MaterializationGatewayModels.cs`

## Recommended Execution Order

### Bucket A — Immediate Reliability/Security Patch

1. Workstream 1 — JSON-RPC framing fix
2. Workstream 3 — RPC logging redaction
3. Workstream 2A — score payload required-field validation
4. Workstream 4A — backend fallback cleanup
5. Workstream 5 — backend preflight cleanup

### Bucket B — Contract And Runtime Hardening

6. Workstream 2B — contract schema/codegen design plus negative contract coverage completion
7. Workstream 4B — build artifact ownership and backend target cleanup
8. Workstream 8 — fix engine persistence abstraction

### Bucket C — Architecture Decomposition

9. Workstream 6 — `PbirScorePanel` decomposition
10. Workstream 7 — `PbirScoringService` decomposition

### Bucket D — Design Studio Runtime Surface Cleanup

11. Workstream 9 — speculative backend Design Studio abstraction cleanup

## Release Buckets

### Release Bucket A

Theme:

- stop the most dangerous runtime and security behaviors first

Can ship together:

- Workstreams 1, 3, 2A, 4A, 5

### Release Bucket B

Theme:

- make contracts and runtime ownership explicit

Can ship together:

- Workstreams 2B, 4B, 8

### Release Bucket C

Theme:

- decompose hotspots only after foundational hardening

Can ship independently:

- Workstream 6
- Workstream 7

Recommended order inside Bucket C:

1. `PbirScorePanel`
2. `PbirScoringService`

### Release Bucket D

Theme:

- reduce speculative runtime surface area

Contains:

- Workstream 9

## Task-By-Task Implementation Sequence

## Workstream 1 — Critical Runtime Reliability

**Files:**

- Modify: `service-dotnet/RpcHost/Program.cs`
- Test: `service-dotnet/tests/RpcHostJsonRpcTests.cs`
- Possibly add: new targeted RPC framing tests under `service-dotnet/tests/`

- [ ] Define the current protocol compatibility surface.
- [ ] Replace the current character-based JSON-RPC body reader with a byte-accurate request reader.
- [ ] Preserve current method names, initialize/shutdown semantics, and serializer behavior.
- [ ] Add tests for:
  - ASCII payloads
  - multibyte UTF-8 payloads
  - malformed Content-Length
  - short-read handling
- [ ] Run focused .NET RPC host tests.
- [ ] Run full backend test suite.
- [ ] Run extension compile/test baseline to catch transport integration regressions.

**Focused tests:**

- new byte-framing unit tests
- `service-dotnet/tests/RpcHostJsonRpcTests.cs`

**Full validation:**

```bash
cd vscode-extension && npm test
cd vscode-extension && npm run compile
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

**Manual smoke checks:**

- open the extension
- score a real report
- verify backend initialize, ping, score, and shutdown still work

**Regression risks:**

- protocol breakage
- hanging reads
- response write regressions

**Rollback strategy:**

- revert the framing implementation only
- keep serializer and method dispatch unchanged

**Definition of done:**

- multibyte payloads are handled correctly
- existing protocol compatibility remains intact
- extension scoring smoke still works

## Workstream 2 — Contract Safety

### Workstream 2A — Score Payload Validation

**Files:**

- Modify: `vscode-extension/src/views/scoreResultPayload.ts`
- Inspect/possibly modify: `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- Inspect: `service-dotnet/Services/Pbir/Models/ScoreResult.cs`
- Test: `vscode-extension/src/test/scoreResultPayload.test.ts`

- [ ] Inventory score payload fields and classify them as required or optional.
- [ ] Replace silent coercion of missing required numbers/booleans/strings with explicit validation failures.
- [ ] Keep optional-field fallback behavior only where the contract truly allows absence.
- [ ] Add negative tests for:
  - missing required numeric fields
  - missing required booleans
  - renamed fields
  - malformed nested structures
- [ ] Ensure error messages are specific enough to diagnose producer-side regressions.
- [ ] Run focused Jest tests.
- [ ] Run full baseline validation.

**Focused tests:**

- `cd vscode-extension && npx jest --runTestsByPath src/test/scoreResultPayload.test.ts`

**Manual smoke checks:**

- score a report
- open score panel
- confirm valid payloads still render
- confirm invalid payload injection fails explicitly in test/harness conditions

**Regression risks:**

- false positives on older payloads
- over-strict validation on optional fields

**Rollback strategy:**

- revert only new required-field enforcement while preserving field inventory docs/tests

**Definition of done:**

- missing required fields no longer silently turn into `0` or `false`
- negative contract tests exist

### Workstream 2B — Schema And Cross-Language Contract Strategy

**Files:**

- Modify later: contract docs and validation helpers
- Inspect:
  - `service-dotnet/Services/DesignStudio/Models/DesignStudioModels.cs`
  - `vscode-extension/src/design-studio/contracts/designStudioModels.ts`
  - `vscode-extension/src/design-studio/contracts/designStudioProtocol.ts`

- [ ] Document the single-source schema strategy for extension/backend contracts.
- [ ] Choose the schema ownership model:
  - JSON Schema
  - code-first generation from one language
  - neutral IDL
- [ ] Define migration sequencing for:
  - score payload
  - Design Studio contracts
  - protocol envelopes
- [ ] Add contract-drift tests or generated snapshots proving C# and TypeScript stay aligned.
- [ ] Identify which contracts must remain hand-maintained until codegen lands.

**Focused tests:**

- contract validator tests
- schema-generation smoke tests if introduced

**Full validation:**

```bash
cd vscode-extension && npm test
cd vscode-extension && npm run compile
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

**Definition of done:**

- required versus optional rules are explicit
- schema/codegen direction is implementation-ready
- contract drift checks are defined

## Workstream 3 — Security And Logging Hygiene

**Files:**

- Modify: `vscode-extension/src/services/rpc/AnalyzerBridgeService.ts`
- Inspect/possibly modify:
  - `vscode-extension/src/platform/outputChannels.ts`
  - `vscode-extension/src/languageServer/analyzerBackendClient.ts`

- [ ] Define the default logging policy for RPC requests and responses.
- [ ] Redact params and results by default.
- [ ] Add diagnostic-mode-only payload logging.
- [ ] Add redaction rules for:
  - report paths
  - PBIR content
  - findings text
  - evidence payloads
- [ ] Add tests proving default logs do not emit sensitive payload content.
- [ ] Smoke check output-channel behavior.

**Focused tests:**

- new logging-policy tests
- `AnalyzerBridgeService` tests as needed

**Manual smoke checks:**

- enable normal mode and inspect output
- enable diagnostic mode and inspect redacted versus full behavior

**Regression risks:**

- reduced diagnosability
- incomplete redaction coverage

**Rollback strategy:**

- preserve new logging abstraction even if redaction rules need temporary rollback

**Definition of done:**

- sensitive report metadata is not logged by default
- diagnostic mode is explicit and test-covered

## Workstream 4 — Build And Runtime Reproducibility

### Workstream 4A — Runtime Backend Resolution Cleanup

**Files:**

- Modify: `vscode-extension/src/languageServer/analyzerBackendClient.ts`
- Inspect:
  - `vscode-extension/package.json`
  - `vscode-extension/scripts/build-backend.mjs`

- [ ] Remove Debug and Release backend runtime fallbacks from runtime resolution.
- [ ] Keep packaged backend assets as the only runtime execution source.
- [ ] Update resolver tests to prove local build leftovers cannot be selected.
- [ ] Run compile/test baseline.

**Focused tests:**

- `cd vscode-extension && npx jest --runTestsByPath src/test/analyzerBackendClient.test.ts`

**Manual smoke checks:**

- run from packaged-like layout
- verify backend starts without local `service-dotnet/RpcHost/bin` dependency

**Definition of done:**

- runtime launches packaged backend assets only

### Workstream 4B — Build Artifact Ownership And Backend Target Cleanup

**Files:**

- Modify docs:
  - `README.md`
  - `docs/RELEASING.md`
- Inspect backend target ownership under:
  - `vscode-extension/backend/targets/`

- [ ] Define packaging artifact ownership clearly.
- [ ] Define which backend assets belong in source control, if any.
- [ ] Plan the checked-in backend target cleanup sequence.
- [ ] Update release docs so build and packaging paths are reproducible and explicit.
- [ ] Validate packaging behavior with full multi-target packaging.

**Packaging validation:**

```bash
cd vscode-extension && npm run package:all
```

**Regression risks:**

- packaging misses runtime assets
- developer workflows break without documentation

**Definition of done:**

- packaging ownership is explicit
- cleanup path is documented and testable

## Workstream 5 — Backend Startup Reliability

**Files:**

- Modify:
  - `vscode-extension/src/extension.ts`
  - `vscode-extension/src/languageServer/analyzerBackendClient.ts`

- [ ] Remove normal-path sacrificial preflight launch.
- [ ] Decide between:
  - explicit troubleshooting mode
  - diagnostics folded into the real launch path
- [ ] Keep degraded-mode behavior and clear startup diagnostics.
- [ ] Add startup tests proving single-launch behavior.
- [ ] Run compile/test baseline.

**Focused tests:**

- backend client tests
- extension activation tests if present or newly added

**Manual smoke checks:**

- start extension in normal mode
- simulate missing runtime/backend
- verify degraded mode still explains the issue clearly

**Definition of done:**

- backend launches once in the normal path
- diagnostics remain understandable

## Workstream 6 — Panel Decomposition

**Files:**

- Modify: `vscode-extension/src/views/PbirScorePanel.ts`
- Add/modify likely new units under:
  - `vscode-extension/src/views/`
  - `vscode-extension/src/views/services/` or equivalent focused folder
- Tests:
  - `vscode-extension/src/test/pbirScorePanel.navigation.test.ts`
  - additional focused workflow tests

- [ ] Extract panel shell responsibilities from orchestration responsibilities.
- [ ] Introduce a message router that owns host-to-webview and webview-to-host dispatch.
- [ ] Extract score-state service.
- [ ] Extract audit workflow service.
- [ ] Extract export workflow service.
- [ ] Extract fix workflow service.
- [ ] Extract Design Studio handoff adapter if needed.
- [ ] Keep message shapes stable during extraction.
- [ ] Add focused tests per extracted service.
- [ ] Run full extension baseline and targeted manual smoke.

**Focused tests:**

- panel routing tests
- service-specific Jest tests

**Manual smoke checks:**

- score report
- navigate findings
- upload screenshots
- export review workflow
- preview/apply/rollback supported fixes
- open Design Studio handoff shell

**Regression risks:**

- broken host/webview coordination
- lost state transitions
- event ordering bugs

**Rollback strategy:**

- land extraction in small slices so each slice can be reverted independently

**Definition of done:**

- `PbirScorePanel` becomes a thin shell plus dispatch
- core workflows have focused service tests

## Workstream 7 — Scoring Service Decomposition

**Files:**

- Modify: `service-dotnet/Services/Pbir/PbirScoringService.cs`
- Add focused services under:
  - `service-dotnet/Services/Pbir/`
  - reuse existing `CrossPageNarrative/` units where appropriate
- Tests:
  - `service-dotnet/tests/Services/PbirScoringServiceTests.cs`
  - related scoring-model tests

- [ ] Inventory the responsibilities currently embedded in `PbirScoringService`.
- [ ] Extract report loading and discovery orchestration.
- [ ] Extract JSON parsing and model extraction.
- [ ] Extract theme resolution.
- [ ] Extract framework scoring units.
- [ ] Extract story assessment units.
- [ ] Extract cross-page narrative orchestration.
- [ ] Extract recommendation generation.
- [ ] Extract result assembly and backward-compat adapter logic.
- [ ] Preserve score outputs and determinism across refactor slices.
- [ ] Use diagnostics comparison as a release gate.

**Focused tests:**

- existing `PbirScoringServiceTests`
- new service-level tests per extracted scorer

**Manual smoke checks:**

- score representative reports
- compare score diagnostics before and after refactor

**Regression risks:**

- score drift
- performance regressions
- story-assessment output changes

**Rollback strategy:**

- extract one responsibility at a time behind stable interfaces

**Definition of done:**

- `PbirScoringService` becomes orchestration rather than implementation bulk
- determinism remains intact

## Workstream 8 — Fix Engine Persistence Safety

**Files:**

- Modify:
  - `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts`
  - `vscode-extension/src/analyzer/fixes/fixMutationPlanner.ts`
  - `vscode-extension/src/analyzer/fixes/fixSessionHistory.ts`
- Tests:
  - `vscode-extension/src/test/fixApplyEngine.test.ts`
  - related fix tests

- [ ] Introduce a persistence abstraction between workflow logic and file mutation.
- [ ] Replace extension-host-blocking synchronous mutation paths where practical.
- [ ] Add optimistic concurrency or version checks.
- [ ] Preserve atomic write semantics.
- [ ] Strengthen rollback safety and post-write validation.
- [ ] Add tests for:
  - concurrent drift
  - rollback after partial failure
  - batch apply/rollback
  - temp-file cleanup
- [ ] Run full baseline validation.

**Focused tests:**

- `cd vscode-extension && npx jest --runTestsByPath src/test/fixApplyEngine.test.ts src/test/fixMutationPlanner.test.ts src/test/fixSessionHistory.test.ts`

**Manual smoke checks:**

- preview/apply/rollback supported fixes on a sample report
- verify no host freeze during larger batch operations

**Definition of done:**

- persistence is abstracted
- concurrency safety is explicit
- rollback path is stronger and test-covered

## Workstream 9 — Design Studio Backend Abstraction Cleanup

**Files:**

- Inspect and possibly later modify:
  - `service-dotnet/Services/DesignStudio/Providers/IDesignStudioProvider.cs`
  - `service-dotnet/Services/DesignStudio/Providers/ProviderCapabilityModels.cs`
  - `service-dotnet/Services/DesignStudio/Materialization/MaterializationGatewayModels.cs`
  - `service-dotnet/tests/DesignStudio/*`

- [ ] Determine which backend Design Studio abstractions protect active runtime boundaries.
- [ ] Separate speculative future-facing models from active runtime models.
- [ ] Decide whether to:
  - keep and document
  - quarantine
  - remove in a later implementation turn
- [ ] Preserve only tests that protect active runtime boundary guarantees.
- [ ] Document why retained abstractions remain in runtime code.

**Focused tests:**

- Design Studio boundary tests

**Manual smoke checks:**

- none required unless implementation later changes build/runtime packaging

**Regression risks:**

- removing tests that still protect trust boundaries
- accidental mismatch with current Design Studio docs

**Definition of done:**

- speculative runtime surface area is explicitly justified or queued for removal

## What Can Ship Independently

- Workstream 1 can ship independently.
- Workstream 3 can ship independently.
- Workstream 2A can ship independently after Workstream 1.
- Workstream 4A can ship independently.
- Workstream 5 can ship independently if Workstream 4A is already in place or ships with it.
- Workstream 8 can ship independently.
- Workstream 6 and Workstream 7 can each ship independently in separate releases if regression risk needs tighter control.

## What Must Ship Together

- Workstream 4A and Workstream 5 should ideally ship together because backend startup behavior depends on runtime resolution clarity.
- Workstream 2B contract strategy and any first schema/codegen introduction should ship with drift-detection tests, not separately.
- Any file-removal implementation inside Workstream 9 must ship with corresponding documentation and boundary-test decisions.

## Regression Risk Summary

- Highest regression risk:
  - Workstream 1
  - Workstream 6
  - Workstream 7
  - Workstream 8
- Medium regression risk:
  - Workstream 2
  - Workstream 5
- Lower regression risk:
  - Workstream 3
  - Workstream 4 documentation slices
  - Workstream 9 documentation or quarantine slices

## Rollback Strategy

- Keep each workstream in separate PRs or implementation branches.
- Prefer one deployable slice per workstream rather than mixing unrelated changes.
- For Bucket C, use behavior-preserving extraction commits before semantic follow-up changes.
- If a runtime change fails validation, revert the workstream slice rather than layering compensating fixes on top.

## Validation Matrix

### Baseline commands for every workstream

```bash
cd vscode-extension && npm test
cd vscode-extension && npm run compile
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

### Packaging/runtime workstreams

```bash
cd vscode-extension && npm run package:all
```

### Suggested focused commands by workstream

Workstream 1:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~RpcHost
```

Workstream 2:

```bash
cd vscode-extension && npx jest --runTestsByPath src/test/scoreResultPayload.test.ts src/test/designStudioProtocol.test.ts src/test/designStudioContracts.test.ts
```

Workstream 3:

```bash
cd vscode-extension && npx jest --runTestsByPath src/test/AnalyzerBridgeService.test.ts src/test/outputChannels.test.ts
```

Workstream 4 and 5:

```bash
cd vscode-extension && npx jest --runTestsByPath src/test/analyzerBackendClient.test.ts src/test/packageManifest.test.ts
cd vscode-extension && npm run package:all
```

Workstream 6:

```bash
cd vscode-extension && npx jest --runTestsByPath src/test/pbirScorePanel.navigation.test.ts src/test/pbirReviewWorkflowExportCommand.test.ts src/test/pbirUploadScreenshotsCommand.test.ts
```

Workstream 7:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~PbirScoringService
```

Workstream 8:

```bash
cd vscode-extension && npx jest --runTestsByPath src/test/fixApplyEngine.test.ts src/test/fixMutationPlanner.test.ts src/test/fixSessionHistory.test.ts src/test/fixBatchPreview.test.ts
```

Workstream 9:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudio
```

## Final Completion Criteria

This remediation program is ready to execute when:

1. each workstream has a clear implementation scope
2. each workstream has focused and full validation commands
3. release sequencing is explicit
4. shipping dependencies are documented
5. rollback strategy is explicit
6. do-not-touch boundaries are preserved
