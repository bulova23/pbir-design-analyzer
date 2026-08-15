# Phase 48 — Curated Mutation Expansion Implementation Notes

## Delivered

- Public adapter allowlist: RenamePage, AddPage, RemovePage, MovePage,
  MoveVisual, ResizeVisual.
- Public multi-operation requests rejected with structured
  PBIR-RPC-MUTATE-009; backend-only mutation kinds remain structured
  unsupportedAuthoring with PBIR-RPC-MUTATE-008.
- Planner preserves request order internally, detects duplicate targets,
  validates page positions/removal safety/layout bounds, and emits typed
  semantic diff records.
- Existing v1 preview/execute envelope now carries discriminated page/visual
  preview payloads and typed semantic diffs without exposing IR, raw PBIR JSON,
  authoring envelopes, or filesystem details.
- Import metadata includes backend-owned visual selection data.
- VS Code adds a single curated mutation picker and shared preview/confirm/
  execute helper; RenamePage compatibility behavior remains.

## Representative preview/diff

    MoveVisual
    visual: revenue-card
    page: overview → detail
    order: 1 → 2
    diff: visualMoved(revenue-card, overview → detail)

    ResizeVisual
    layout: (0,0,320,160) → (8,12,320,180)
    diff: visualResized(revenue-card)

## Evidence and performance

Analyzer before/after, score delta, fidelity, preserved identities, and timing
fields continue to be returned by execute. Planning, preview, serialization,
materialization, analyzer-before, analyzer-after, and dispatch timings are
observations only; no thresholds or optimization work was added.

One representative local ResizeVisual execute observation on the existing
fixture reported dispatch 0 ms, orchestration 78 ms, planning 8 ms, preview
1 ms, serialization 0 ms, analyzer-before 2 ms, and analyzer-after 1 ms.
These values are host/fixture observations, not performance guarantees.

## Limitations and Phase 49 gate

Add Visual and all other typed backend operations remain backend-only. Public
batching and capability discovery remain deferred. Phase 49 should evaluate
ordered batches only after the six single-operation workflows demonstrate
consistent preview/diff and UX semantics.
