# Phase 48 — Curated Mutation Expansion Current State

Status: **IMPLEMENTED LOCALLY — SIX-OPERATION CURATED PUBLIC WORKFLOW** on
2026-08-15.

The existing pbir-authoring-rpc/v1 route now admits exactly one operation from
the curated catalog:

| Mutation | Public | Preview payload | Semantic diff |
| --- | --- | --- | --- |
| Rename Page | Yes | display-name change | page renamed |
| Add Page | Yes | identity/name/position | page added |
| Remove Page | Yes | removed identity, remaining order, navigation impact | page removed |
| Move Page | Yes | current/proposed position | page moved |
| Move Visual | Yes | current/proposed page/order/layout | visual moved |
| Resize Visual | Yes | current/proposed layout | visual resized |

All other typed backend mutation kinds remain internal and return structured
unsupported-authoring responses at the public adapter. Requests containing
more than one operation are rejected deterministically.

The source snapshot handle is immutable. Preview is non-materializing and
execute re-plans from the snapshot before creating a new opaque artifact
handle. Analyzer before/after summaries, score delta, fidelity, identities,
diagnostics, and timing observations remain downstream response evidence.

The VS Code extension has one curated mutation picker using Quick Pick, Input
Box, and standard confirmation. It renders backend preview/diff data and does
not calculate geometry, page order, target resolution, or identity changes.

Known limitations: Add Page retains the existing narrow typed page shape;
navigation and layout validation are conservative; there is no public
batching, capability discovery, undo/redo, graphical editing, raw JSON
editing, or Windows/hosted execution. Phase 49 should first assess contract
consistency across the six single-operation workflows before considering
ordered batches.
