# 2026-06-17 Design Studio Real Analyzer Return Integration

## Goal

- replace the seeded analyzer-return dependency in the primary Design Studio workflow with a real Analyzer Workspace return path
- preserve trust boundaries:
  - Analyzer Workspace remains authoritative for execution, findings, review completion, and validation approval
  - Design Studio may discover, display, and explicitly attach analyzer results only

## Constraints

- no provider-backed generation
- no Microsoft Skills integration
- no analyzer scoring changes
- no report mutation
- no deployment work
- no automatic validation approval

## Investigation Notes

- the current Review Design and refinement flow already had the right attach/refinement/workflow stages, but real analyzer results were still represented by seeded availability records
- the existing handoff contract did not persist enough return metadata to rediscover a completed analyzer review deterministically from the Analyzer Workspace shell
- attachment needed to ingest analyzer-backed refinement inputs without collapsing trust boundaries or passing full report payloads through the host/webview protocol
- full lineage validation needed two layers:
  - discovery/attach prevalidation against candidate lineage and handoff identity
  - iteration recording validation against the full approved draft artifact fingerprint

## Planned Validation

- targeted Jest coverage for:
  - analyzer return discovery
  - lineage rejection
  - atomic attachment
  - refinement gating
  - compare-iterations/workflow completion visibility
- `cd vscode-extension && npm test`
- `cd vscode-extension && npm run compile`
- `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## Implementation Summary

- added a persisted real Analyzer Workspace return store keyed to candidate lineage and handoff identity
- expanded the handoff contract to carry the return metadata needed for rediscovery:
  - thread id
  - request id
  - analyzer run id
  - analyzer result id
  - source candidate id
  - source artifact/version fingerprint
  - analyzer completion status
  - validation status
  - finding/recommendation references
  - provenance metadata
- persisted real analyzer return data from the score panel only when opened through the Design Studio handoff path
- added Review Design rediscovery so Design Studio can surface a completed analyzer review without seeded injection
- changed explicit result attachment to consume persisted real analyzer return payloads, ingest refinement inputs, and then record attached lineage atomically
- preserved analyzer-owned validation by keeping validation state in analyzer/workflow approval models rather than mutating analyzer findings or execution state from Design Studio
- updated Compare Iterations and Workflow Completion presentation to reflect:
  - analyzer run identity
  - analyzer review completion
  - attached result state

## Validation Results

- passed required validation:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## Manual Smoke

- not run in this session
- reason:
  - this slice was validated through automated extension, webview, and backend coverage only

## Status

- complete
