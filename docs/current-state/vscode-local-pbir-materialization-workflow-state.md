# VS Code Local PBIR Materialization Workflow Current State

## Status and mapping

Repository Phase 34 integrates the completed Phase 33 local PBIR RPC adapter into the existing Report Design Studio materialize stage.

- Phase 29 = deterministic modern PBIR serialization.
- Phase 30 = safe local PBIR materialization and recovery authority.
- Phase 31 = application orchestration and typed outcomes.
- Phase 32 = bounded RPC transport lifecycle.
- Phase 33 = local PBIR RPC adapter.
- Phase 34 = VS Code local PBIR materialization workflow.
- Phase 35 onward remains provisional and unauthorized.

## User workflow

The existing Design Studio is the only UI surface. The materialize stage and the optional `pbirAnalyzer.openLocalPbirMaterialization` command expose read-only preview, explicit apply confirmation, read-only recovery inspection, and cancellation. The extension host owns a small generation-guarded coordinator. The webview renders only a redacted presentation model.

Phase 34 calls exactly three routes: `pbir/materialization/preview`, `pbir/materialization/apply`, and `pbir/materialization/recovery/inspect`. Preview and recovery remain read-only. Apply forwards the exact validated preview identity and creates a fresh transaction ID only after explicit confirmation. Conflict, stale-preview, failure, cancellation, and recovery-required outcomes clear applyable state and require a fresh preview.

## Lifecycle and safety

Cancellation is propagated through the existing LanguageClient bridge. Panel disposal, bridge disconnect, extension-host restart, and late responses invalidate the local request generation without attempting rollback or changing transaction truth. Controls are disabled while work is in flight, with cancellation retained. The webview stores no canonical input, raw PBIR payload, absolute path, journal, backup, exception, or transaction internals.

Phase 34 does not add provider or Microsoft Skills execution, generated-artifact intake, Analyzer handoff or automation, refinement, Fabric App generation, Desktop verification, PBIP or semantic-model generation, deployment, publishing, authentication, authorization, or a new transport. It also does not add legacy root-level `report.json` authority.

## Residual risk

The Phase 33 routes require a complete Phase 31 canonical input. Phase 34 consumes that input through an explicit host provider seam; it does not invent a generator or read the filesystem to assemble one. Until an authorized upstream producer supplies that input, the UI explains that preview is unavailable while keeping the workflow boundary and safety behavior intact.
