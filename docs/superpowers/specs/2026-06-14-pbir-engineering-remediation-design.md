# PBIR Engineering Remediation Design

Date: 2026-06-14

Status: Approved planning direction for staged hardening only; no code changes in this document

## Executive Summary

PBIR Design Analyzer has enough product capability to justify a focused engineering hardening phase before more feature expansion.

The current principal-architect repository review identified ten high-risk problem areas:

1. JSON-RPC byte versus character framing
2. `PbirScoringService` god class
3. `PbirScorePanel` orchestration hotspot
4. duplicated C# and TypeScript contracts
5. silent score payload coercion
6. verbose sensitive RPC payload logging
7. runtime backend fallbacks and checked-in binaries
8. backend startup double-launch
9. fix engine persistence safety
10. speculative backend Design Studio abstractions

This design turns those findings into nine remediation workstreams with an explicit dependency map, release sequence, safety boundaries, and validation strategy.

The immediate objective is not architectural novelty.

It is risk reduction in the areas most likely to cause:

- runtime failure
- protocol corruption
- contract drift
- sensitive-data leakage
- non-reproducible packaging behavior
- hard-to-maintain extension and backend orchestration

## Problem Statement

The repository’s current risk profile is no longer dominated by isolated defects.

It is dominated by cross-cutting engineering debt concentrated in:

- the custom extension-to-backend transport
- oversized orchestration classes in both TypeScript and C#
- manually duplicated contracts across runtime boundaries
- permissive normalization behavior that hides protocol breakage
- packaging/runtime fallbacks that weaken determinism
- persistence logic that is safe by intent but not yet hardened by abstraction and concurrency discipline

If left unresolved, these issues will make future releases slower, less trustworthy, and harder to validate.

They also increase the chance that future contributors work around the architecture rather than through it.

## Risk Classification

### Critical

- JSON-RPC byte framing bug in `service-dotnet/RpcHost/Program.cs`

### High

- `service-dotnet/Services/Pbir/PbirScoringService.cs` god class
- `vscode-extension/src/views/PbirScorePanel.ts` orchestration hotspot
- duplicated Design Studio contracts in:
  - `service-dotnet/Services/DesignStudio/Models/DesignStudioModels.cs`
  - `vscode-extension/src/design-studio/contracts/designStudioModels.ts`
  - `vscode-extension/src/design-studio/contracts/designStudioProtocol.ts`
- silent payload coercion in `vscode-extension/src/views/scoreResultPayload.ts`
- verbose RPC payload logging in `vscode-extension/src/services/rpc/AnalyzerBridgeService.ts`
- runtime backend fallback behavior in `vscode-extension/src/languageServer/analyzerBackendClient.ts`

### Medium

- backend startup double-launch across:
  - `vscode-extension/src/extension.ts`
  - `vscode-extension/src/languageServer/analyzerBackendClient.ts`
- fix-engine persistence safety in `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts`
- speculative backend Design Studio abstractions in:
  - `service-dotnet/Services/DesignStudio/Providers/IDesignStudioProvider.cs`
  - `service-dotnet/Services/DesignStudio/Providers/ProviderCapabilityModels.cs`
  - `service-dotnet/Services/DesignStudio/Materialization/MaterializationGatewayModels.cs`

## Non-Goals

This remediation phase does not:

- add new product features
- change analyzer scoring intent
- widen deterministic mutation scope
- begin provider-backed generation
- redesign the user-facing review workflow
- remove files in this planning phase
- change trust-boundary ownership between Design Studio and Analyzer Workspace
- replace the extension platform or move away from the current VS Code plus .NET shape

## Target Architecture

### Target Outcome

The repository should move toward a harder, more explicit architecture with:

- byte-correct transport behavior
- explicit contract validation rather than silent coercion
- redacted logging by default
- packaged-runtime-only backend execution
- single-launch backend startup
- smaller orchestration units in extension host and backend
- safer persistence abstractions for deterministic fixes
- reduced speculative runtime surface area

### Layer Boundaries To Preserve

- scoring remains authoritative for score outputs and findings
- normalized findings remain the shared issue model
- deterministic preview/apply/rollback remains the only report-edit execution path
- webviews remain presentation/state consumers rather than mutation authorities
- advisory enrichment remains advisory-only
- Design Studio remains separate from analyzer validation authority

### Target Structural Direction

#### Extension Runtime

Current hotspots:

- `vscode-extension/src/views/PbirScorePanel.ts`
- `vscode-extension/src/services/rpc/AnalyzerBridgeService.ts`
- `vscode-extension/src/languageServer/analyzerBackendClient.ts`

Target direction:

- panel shell
- message router
- score-state service
- audit workflow service
- export workflow service
- fix workflow service
- Design Studio handoff adapter
- logging policy abstraction
- packaged-backend resolver

#### Backend Runtime

Current hotspots:

- `service-dotnet/RpcHost/Program.cs`
- `service-dotnet/Services/Pbir/PbirScoringService.cs`

Target direction:

- byte-correct request reader
- protocol serializer and response writer
- report loading/discovery service
- JSON parsing/model extraction service
- theme resolution service
- framework scoring services
- story assessment services
- cross-page narrative services
- recommendation assembly service
- result assembly and backward-compat adapter

#### Cross-Boundary Contracts

Current problem:

- manual duplication across C# and TypeScript

Target direction:

- explicit required versus optional field rules
- negative contract tests
- single-source schema or codegen strategy
- versioned validation at every host/webview and extension/backend boundary

## Dependency Map

### Workstream 1 — Critical Runtime Reliability

Includes:

- JSON-RPC Content-Length byte framing fix
- multibyte payload tests
- protocol compatibility preservation

Primary files:

- `service-dotnet/RpcHost/Program.cs`
- `service-dotnet/tests/RpcHostJsonRpcTests.cs`

Dependencies:

- none

Blocks:

- reliable future contract hardening
- trustworthy runtime diagnostics

### Workstream 2 — Contract Safety

Includes:

- explicit score payload validation
- negative contract tests
- required versus optional payload definitions
- schema/codegen strategy design

Primary files:

- `vscode-extension/src/views/scoreResultPayload.ts`
- `vscode-extension/src/analyzer/contracts/scorePanel.ts`
- `service-dotnet/Services/Pbir/Models/ScoreResult.cs`
- `vscode-extension/src/design-studio/contracts/designStudioModels.ts`
- `service-dotnet/Services/DesignStudio/Models/DesignStudioModels.cs`

Dependencies:

- Workstream 1 should land first so transport failures do not mask contract failures

Blocks:

- safe decomposition of score-panel and scoring pipelines

### Workstream 3 — Security And Logging Hygiene

Includes:

- redacted RPC logging
- diagnostic-mode-only payload logging
- redaction rules for paths and content

Primary files:

- `vscode-extension/src/services/rpc/AnalyzerBridgeService.ts`
- `vscode-extension/src/platform/outputChannels.ts`
- `vscode-extension/src/languageServer/analyzerBackendClient.ts`

Dependencies:

- none

Blocks:

- safe operational troubleshooting

### Workstream 4 — Build And Runtime Reproducibility

Includes:

- remove Debug and Release backend runtime fallbacks
- packaged backend assets only
- checked-in backend target cleanup plan
- packaging artifact ownership clarification

Primary files:

- `vscode-extension/src/languageServer/analyzerBackendClient.ts`
- `vscode-extension/package.json`
- `README.md`
- `docs/RELEASING.md`

Dependencies:

- none for path cleanup
- packaging validation depends on existing build scripts

Blocks:

- deterministic runtime behavior across environments

### Workstream 5 — Backend Startup Reliability

Includes:

- remove normal-path backend preflight double launch
- move preflight to troubleshooting mode or fold diagnostics into real launch path

Primary files:

- `vscode-extension/src/extension.ts`
- `vscode-extension/src/languageServer/analyzerBackendClient.ts`

Dependencies:

- Workstream 4 should precede or ship with this workstream so the launch path is already unambiguous

Blocks:

- reliable startup behavior

### Workstream 6 — Panel Decomposition

Includes:

- decompose `PbirScorePanel`

Primary files:

- `vscode-extension/src/views/PbirScorePanel.ts`
- related helpers under:
  - `vscode-extension/src/views/`
  - `vscode-extension/src/analyzer/`

Dependencies:

- Workstream 2 should land first so the refactor happens on explicit contracts
- Workstream 3 should land first so logging policy is already isolated

Blocks:

- maintainable extension-host evolution

### Workstream 7 — Scoring Service Decomposition

Includes:

- decompose `PbirScoringService`

Primary files:

- `service-dotnet/Services/Pbir/PbirScoringService.cs`
- related service files under:
  - `service-dotnet/Services/Pbir/`
  - `service-dotnet/Services/Pbir/CrossPageNarrative/`

Dependencies:

- Workstream 1 should land first
- Workstream 2 should define stable contract boundaries first

Blocks:

- maintainable scoring evolution

### Workstream 8 — Fix Engine Persistence Safety

Includes:

- persistence abstraction
- optimistic concurrency or version checks
- atomic write preservation
- rollback safety improvements
- avoid blocking extension host

Primary files:

- `vscode-extension/src/analyzer/fixes/fixApplyEngine.ts`
- `vscode-extension/src/analyzer/fixes/fixMutationPlanner.ts`
- `vscode-extension/src/analyzer/fixes/fixSessionHistory.ts`

Dependencies:

- none structurally
- can benefit from Workstream 4 packaging clarity only indirectly

Blocks:

- hardening deterministic mutation trustworthiness

### Workstream 9 — Design Studio Backend Abstraction Cleanup

Includes:

- runtime-value decision for speculative backend Design Studio abstractions
- quarantine, document, or remove later when implementation is allowed
- preserve only active runtime boundary tests

Primary files:

- `service-dotnet/Services/DesignStudio/Providers/IDesignStudioProvider.cs`
- `service-dotnet/Services/DesignStudio/Providers/ProviderCapabilityModels.cs`
- `service-dotnet/Services/DesignStudio/Materialization/MaterializationGatewayModels.cs`
- `service-dotnet/tests/DesignStudio/*`

Dependencies:

- none

Blocks:

- reduction of speculative maintenance surface

## Release Sequencing

### Bucket A — Immediate Reliability/Security Patch

1. Workstream 1 — Critical Runtime Reliability
2. Workstream 3 — Security And Logging Hygiene
3. Workstream 2 — score payload validation slice
4. Workstream 4 — backend fallback cleanup slice
5. Workstream 5 — backend preflight cleanup

Reason:

- these items reduce immediate runtime risk without requiring large structural refactors

### Bucket B — Contract And Runtime Hardening

1. Workstream 2 — full contract safety completion
2. Workstream 4 — packaging artifact ownership and binary cleanup
3. Workstream 8 — fix engine persistence abstraction

Reason:

- these items stabilize boundaries and runtime reproducibility before major decomposition

### Bucket C — Architecture Decomposition

1. Workstream 6 — `PbirScorePanel` decomposition
2. Workstream 7 — `PbirScoringService` decomposition

Reason:

- decomposition work should happen only after transport, contracts, logging, and runtime resolution are harder and easier to validate

### Bucket D — Design Studio Runtime Surface Cleanup

1. Workstream 9 — speculative backend Design Studio abstraction cleanup

Reason:

- this is structurally important but lower urgency than transport, contract, and deterministic-mutation concerns

## Safety Boundaries

### Do Not Touch Boundaries

- do not change scoring semantics as part of Workstreams 1 through 5
- do not widen deterministic mutation authority
- do not introduce provider-backed generation
- do not collapse approval or trust-boundary distinctions
- do not move validation ownership out of Analyzer Workspace
- do not change public protocol versions casually; version changes require compatibility notes and tests

### Compatibility Boundaries

- extension-to-backend RPC method names must remain stable during Workstream 1
- score payload public shape must remain backward compatible unless the plan explicitly introduces a versioned contract change
- deterministic fix behaviors must remain preview/apply/rollback based
- packaged backend launch behavior must continue to support all declared platform targets

### Refactor Safety Rule

For Workstreams 6 and 7:

- behavior-preserving extraction must happen before behavior changes
- each extraction slice should land with focused regression coverage proving no semantic changes

## Validation Strategy

### Common Baseline Validation

Run after every material workstream implementation:

```bash
cd vscode-extension && npm test
cd vscode-extension && npm run compile
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

### Packaging Validation

Required when Workstreams 4 or 5 change runtime packaging or launch behavior:

```bash
cd vscode-extension && npm run package:all
```

### Workstream-Specific Validation Direction

#### Workstream 1

- multibyte JSON-RPC payload tests
- malformed header tests
- shutdown/initialize compatibility tests
- manual smoke: open extension, score report, verify no framing regressions

#### Workstream 2

- negative contract tests for missing required fields
- optional-field compatibility tests
- host/webview protocol rejection tests
- manual smoke: load score panel and verify invalid payloads fail loudly rather than degrade silently

#### Workstream 3

- logging unit tests for redaction behavior
- smoke check output channels with representative payloads
- verify no full params/results appear outside diagnostic mode

#### Workstream 4

- resolver tests proving only packaged assets are accepted
- packaging validation across target bundles
- manual smoke: packaged VSIX starts backend without local `bin` dependency

#### Workstream 5

- startup tests proving single-launch behavior
- degraded-mode tests
- manual smoke: startup diagnostics still understandable after preflight removal

#### Workstream 6

- panel message-routing tests
- workflow service tests
- focused smoke for score, audit, export, fix, and Design Studio handoff actions

#### Workstream 7

- scoring regression suite
- determinism checks
- performance comparison on representative reports
- manual smoke: score the same report before and after refactor, compare diagnostics

#### Workstream 8

- concurrency/conflict tests
- rollback safety tests
- batch apply plus rollback tests
- manual smoke: preview, apply, and rollback supported fixes against a real sample report

#### Workstream 9

- boundary-test audit
- runtime usage verification
- packaging/build validation if files are later moved or removed in an implementation turn

## Workstream Design Details

### Workstream 1 — Critical Runtime Reliability

Design intent:

- replace character-based JSON-RPC body reading with byte-accurate framing
- preserve current protocol method names and response semantics
- add multibyte payload regression coverage

Primary risk:

- introducing transport regressions while fixing framing correctness

Mitigation:

- keep the external transport contract identical
- validate with explicit initialize, ping, score, and shutdown flows

### Workstream 2 — Contract Safety

Design intent:

- stop treating missing required data as valid zero or false defaults
- make required versus optional payload rules explicit
- create a single-source schema path for future codegen

Primary risk:

- breaking current permissive consumers too abruptly

Mitigation:

- stage the change:
  - first introduce explicit validation with precise diagnostics
  - then add schema/codegen design
  - then migrate duplicate contracts incrementally

### Workstream 3 — Security And Logging Hygiene

Design intent:

- retain diagnosability without exposing sensitive report metadata by default

Primary risk:

- losing useful diagnostics during support investigation

Mitigation:

- introduce opt-in diagnostic mode
- define redaction rules centrally

### Workstream 4 — Build And Runtime Reproducibility

Design intent:

- make the launched backend deterministic and packaging-owned

Primary risk:

- breaking local developer convenience paths

Mitigation:

- document an explicit developer packaging workflow rather than allowing runtime path discovery to guess

### Workstream 5 — Backend Startup Reliability

Design intent:

- launch the real backend once
- collect diagnostics from the real path instead of a sacrificial preflight process

Primary risk:

- losing some startup-failure evidence

Mitigation:

- keep structured diagnostics collection in the real launch path
- retain explicit troubleshooting mode if needed

### Workstream 6 — Panel Decomposition

Design intent:

- shrink `PbirScorePanel` into testable, cohesive orchestration units

Primary risk:

- breaking the highest-traffic extension workflow during refactor

Mitigation:

- decompose by extracted responsibilities without changing message shapes first

### Workstream 7 — Scoring Service Decomposition

Design intent:

- convert `PbirScoringService` from a god class into an orchestration façade over focused domain services

Primary risk:

- accidental score drift

Mitigation:

- use score determinism diagnostics and regression comparisons as a release gate

### Workstream 8 — Fix Engine Persistence Safety

Design intent:

- preserve deterministic mutation authority while improving safety, concurrency behavior, and host responsiveness

Primary risk:

- subtle write or rollback regressions in a trust-sensitive subsystem

Mitigation:

- keep atomic semantics mandatory
- add stronger post-write and rollback validation

### Workstream 9 — Design Studio Backend Abstraction Cleanup

Design intent:

- reduce speculative runtime code until provider-backed generation has real implementation scope

Primary risk:

- deleting protective tests that still guard active trust boundaries

Mitigation:

- separate “protects active runtime boundary” from “protects future speculative model”

## Recommended Outcome

The repository should treat this remediation program as a pre-feature hardening roadmap.

The first success condition is not prettier code.

It is:

- correct transport
- explicit contracts
- safer logging
- reproducible runtime behavior
- single-launch startup

Only after those land should the repository spend major effort on decomposition.
