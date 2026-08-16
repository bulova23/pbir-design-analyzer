# VS Code Local PBIR Materialization Workflow — Repository Phase 34 Design

Date: 2026-08-04

Status: Authorized for implementation in this session; changes remain uncommitted.

## Boundary

Repository Phase 34 is the VS Code workflow consumer of the completed Phase 33 local PBIR RPC adapter:

- Phase 29 = deterministic modern PBIR serialization.
- Phase 30 = safe local PBIR materialization and recovery authority.
- Phase 31 = application orchestration and typed outcomes.
- Phase 32 = bounded JSON-RPC transport lifecycle.
- Phase 33 = the three-route local PBIR RPC adapter.
- Phase 34 = extension-host and Design Studio workflow integration only.

Phase 34 adds no backend route, filesystem access, provider or Microsoft Skills execution, external process/API/CLI/network access, PBIP or semantic-model generation, Desktop or Analyzer automation, deployment, publishing, authentication, authorization, or transport. Phase 35 onward remains provisional and unauthorized.

## Architecture

The existing Report Design Studio is the single user entry point. Its materialize stage gains a local PBIR workflow card backed by a small extension-host coordinator. The coordinator is the only extension consumer of the adapter and calls exactly:

- `pbir/materialization/preview`
- `pbir/materialization/apply`
- `pbir/materialization/recovery/inspect`

The coordinator owns request generations, cancellation sources, in-flight gating, and the opaque validated preview required for apply. It never reads or writes the filesystem and never calls Phase 30 or 31 types. The webview receives only safe presentation data and emits intent messages; it does not retain raw payloads, paths, journals, transaction internals, or authority-bearing objects.

## State and flow

The workflow has these states: idle, previewing, preview-ready, confirming, applying, inspecting-recovery, applied, exact-match, managed-replacement, conflict, stale-preview, recovery-required, cancelled, failed, disconnected, and disposed. A monotonically increasing request generation makes late responses no-ops. Restart, disconnect, disposal, and cancellation clear in-flight UI state without claiming rollback or changing backend transaction truth.

Preview is read-only and renders safe summary, destination classification, artifact count, deterministic identity reference, conflict information, recovery status, and bounded diagnostics. Apply is enabled only for a current validated preview with an applyable outcome. Confirmation is explicit, keyboard accessible, and single-submit guarded. The coordinator creates a fresh transaction ID at apply time and sends the exact validated preview identity returned by preview. Applyable state is cleared after success, conflict, stale preview, failure, cancellation, or recovery-required; those outcomes require a new preview.

Recovery inspection is explicitly read-only and renders only the safe recovery result. Cancellation is available while preview, apply, or recovery inspection is in flight. Cancellation propagates through the existing LanguageClient request lifecycle; it is not interpreted as rollback. Disconnect and extension-host restart show a recoverable message and require a fresh preview or recovery inspection.

## Contracts and redaction

Phase 34 reuses the typed adapter request/response shapes locally, with a narrow TypeScript boundary that validates operation, response version, outcome, and safe fields before presentation. It forwards no absolute paths, staging paths, journals, backups, exceptions, payload contents, or transaction internals. Only bounded relative artifact names, counts, hashes/identity references, safe correlation IDs, typed outcomes, and fixed diagnostics are rendered.

## Testing

Host tests use deterministic bridge fakes and lifecycle seams. Webview tests use an in-memory VS Code API fake and controllable host messages. Coverage includes command registration, all fifteen typed outcomes, exact preview/fresh transaction propagation, confirmation and double-submit prevention, read-only recovery, cancellation/progress, stale/conflict/recovery reset, disconnect/restart/disposal/late responses, redaction, accessibility/keyboard behavior, and static scope checks proving no filesystem or forbidden authority is added.

## Long-term risks

1. Highest risk: duplicating backend contracts in the webview would drift the Phase 33 boundary. The host coordinator keeps the wire shape centralized and presents a smaller view model.
2. High risk: retaining a preview across restart or stale outcomes could apply the wrong bytes. Generation checks and mandatory regeneration prevent this.
3. High risk: allowing cancellation or UI disposal to imply rollback would corrupt Phase 30 truth. The workflow only forgets local UI state.
4. Medium risk: adding a second materialization panel would fragment terminology and accessibility. The existing Design Studio materialize stage remains the only surface.
