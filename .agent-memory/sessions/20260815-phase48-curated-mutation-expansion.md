# Session 2026-08-15 — Phase 48 Curated Mutation Expansion

## Scope

Implemented the approved Phase 48 expansion from the Phase 47 RenamePage
workflow to a six-entry public mutation allowlist:

- RenamePage
- AddPage
- RemovePage
- MovePage
- MoveVisual
- ResizeVisual

The existing pbir-authoring-rpc/v1 envelope, preview/execute modes, opaque
snapshot handles, copy-on-write artifact handles, backend planner/executor,
serializer/materializer, and analyzer round trip were preserved.

## Implementation

- Public adapter admits only the six curated operation kinds.
- Multi-operation public requests reject deterministically with
  PBIR-RPC-MUTATE-009.
- Backend-only kinds remain structured unsupportedAuthoring responses with
  PBIR-RPC-MUTATE-008.
- Planner preserves request order internally, detects duplicate targets,
  validates page positions, removal/navigation safety, and visual bounds, and
  emits typed semantic diffs.
- v1 preview responses carry typed page/visual payloads and diffs without IR,
  raw JSON, authoring envelopes, or filesystem details.
- Import returns backend-owned visual metadata for the thin VS Code picker.
- Added one curated VS Code picker plus shared collect/preview/confirm/execute
  flow; existing RenamePage command behavior remains compatible.

## Validation

- Focused backend mutation/RPC/planner tests: 21 passed.
- Full backend Release tests: 996 passed, 11 expected Windows skips, 0 failed.
- Extension Jest: 505 passed across 98 suites.
- Webview Jest: 68 passed across 11 suites.
- TypeScript compilation: passed.
- Production build: passed.
- VSIX packaging: passed.
- Changed-file ESLint: passed.
- git diff --check: passed.
- Representative ResizeVisual execute timing: dispatch 0 ms, orchestration
  78 ms, planning 8 ms, preview 1 ms, serialization 0 ms,
  analyzer-before 2 ms, analyzer-after 1 ms.

## Limitations and next step

Overlap handling remains delegated to the existing serializer validator for
imported PBIR because the repository has intentionally layered fixtures; the
planner rejects impossible bounds and does not invent repairs. AddVisual and
all other non-curated typed operations remain backend-only. Capability
discovery, public batching, undo/redo, graphical editing, raw JSON editing,
and Windows/hosted execution remain deferred.

Phase 49 should first assess preview/diff and UX consistency across the six
single-operation workflows before deciding whether ordered batches are mature
enough for a public contract.
