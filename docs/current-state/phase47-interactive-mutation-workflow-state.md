# Phase 47 — Interactive Mutation Workflow Current State

Status: **IMPLEMENTED — RENAME PAGE PREVIEW/CONFIRM/EXECUTE WORKFLOW** on 2026-08-14.

Phase 47 exposes exactly one mutation through the existing authoring RPC route:
Rename Page. The imported snapshot remains immutable. VS Code receives page ID
and display-name metadata from the backend, requests a planner-generated
preview, asks for confirmation, and executes the same typed request only after
confirmation.

```text
Import → Select Rename Page → Preview → Confirm → Mutate → Analyze → Before/After Result
```

The preview is semantic rather than a raw JSON diff. It contains the mutation
kind, target identity, current and proposed names, affected and preserved
identities, object counts, planner diagnostics, admissibility, and no-op state.
The extension renders this model and does not calculate expected changes.

Execute re-resolves the opaque snapshot and re-plans authoritatively. The
existing executor, authoring merge, serializer, validation, materialization,
and analyzer services remain the only mutation path. A successful execution
returns a new opaque artifact handle; the original snapshot handle is not
advanced or mutated.

Same-name rename is a valid deterministic no-op: zero changed objects,
execution inadmissible, and no artifact materialization. Empty, invalid,
unknown, stale, unsupported, validation, execution, and analyzer failures are
returned through structured error categories and diagnostics.

Mutation matrix:

| User-facing operation | Public in Phase 47 | Backend internal contract |
| --- | --- | --- |
| Rename Page | Yes | Typed and mergeable |
| Add/Remove/Move Page | No | Backend-only |
| Add/Remove/Move/Resize Visual | No | Backend-only |
| Formatting, binding, filter, navigation, slicer | No | Backend-only or unsupported |

Undo/redo is intentionally not implemented. Mutation evidence is returned for
future undo design, but Phase 47 has no rollback authority.

Phase 48 should compare a small curated mutation catalog with backend-provided
capability discovery using real Rename Page workflow evidence. It should not
assume capability discovery is the default answer.
