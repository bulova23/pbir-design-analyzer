# Phase 48 — Curated Mutation Expansion and Multi-Operation Planning

## Goal

Expand the public authoring workflow from RenamePage to six curated mutation
families while preserving the existing `pbir-authoring-rpc/v1` envelope,
backend authority, immutable snapshot lifecycle, and analyzer round trip.

## Public boundary

The public adapter admits exactly these operation kinds:

| Mutation | Public | Backend-only after Phase 48 |
| --- | --- | --- |
| RenamePage | Yes |  |
| AddPage | Yes |  |
| RemovePage | Yes |  |
| MovePage | Yes |  |
| MoveVisual | Yes |  |
| ResizeVisual | Yes |  |
| AddVisual, RemoveVisual, ReplaceVisual, UpdateBinding, UpdateFormatting, UpdateTheme, UpdateFilter, UpdateNavigation, UpdateSlicer | No | Yes |

No capability discovery, mutation registry, new RPC version, batch endpoint,
undo/redo, graphical editor, raw JSON editing, or hosted/Windows execution is
introduced.

## Architecture and data flow

The existing RPC operation remains one mutation request with preview and
execute modes:

```text
Import → opaque snapshot handle → planner → typed preview/diff
                                      ↓ confirmation
                              re-plan → execute → new artifact handle → analyzer
```

The planner remains authoritative for target resolution, one-operation
admission, no-op detection, page/navigation safety, layout validation,
affected/preserved identities, and semantic diffs. Request order is preserved
internally, but public requests containing anything other than exactly one
operation are rejected deterministically.

## Preview and diff contract

The common preview retains mutation kind, affected and preserved identities,
diagnostics, execution admissibility, and no-op state. A discriminated typed
payload carries operation-specific values:

- AddPage: display name, proposed position, deterministic new page identity.
- RemovePage: removed page identity/name, proposed remaining order, affected
  visuals, and navigation impact.
- MovePage: current and proposed page positions and order identities.
- RenamePage: current and proposed display names.
- MoveVisual: source/destination page and current/proposed visual order/layout.
- ResizeVisual: current and proposed typed layout dimensions.

Semantic diffs are typed records for page added/removed/moved/renamed and
visual moved/resized. Raw JSON, IR, authoring envelopes, and filesystem paths
never cross the RPC boundary.

## Validation and lifecycle

Planning fails closed for missing/deleted targets, duplicate targets, invalid
page positions, invalid page movement, page-removal navigation conflicts,
invalid bounds, layout conflicts, and unsupported operation kinds. No implicit
repair is performed. A same-value operation is a valid no-op and cannot
execute. Execute always resolves the original snapshot, re-plans, and creates
a new artifact handle; the source snapshot remains immutable. Analyzer before,
after, delta, fidelity, diagnostics, and timing evidence remain response data.

## VS Code workflow

The extension exposes one curated mutation picker. It collects only user intent
with Quick Pick, Input Box, and standard confirmation dialogs, then delegates
the shared Collect → Preview → Render backend diff → Confirm → Execute →
Present Analyzer flow. It does not retain a frontend report model or perform
validation, geometry, ordering, identity, or diff calculations.

## Testing and evidence

Focused tests cover the allowlist, every public operation, typed previews and
diffs, no-ops, planner diagnostics, navigation/layout rejection, stale and
immutable handles, deterministic identities, analyzer comparison,
cancellation, structured errors, and extension confirmation/cancellation.
Existing RenamePage tests remain unchanged in behavior. Timing fields are
observed for planning, preview, execution, serialization/materialization,
analyzer, and RPC dispatch without thresholds or optimization work.

## Phase 49 gate

After six workflows are validated, evaluate ordered batches as a separate
contract decision. If preview/diff or UX semantics are inconsistent, stabilize
the single-operation model first. Capability discovery remains deferred until
the curated catalog is stable.
