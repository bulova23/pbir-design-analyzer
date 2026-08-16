# Phase 47 — Interactive Mutation Workflow and Visual Diff Preview

## Goal

Prove the first interactive authoring loop through VS Code with exactly one public mutation: `RenamePage`.

```text
Import → Select RenamePage → Backend Preview → Confirm → Backend Mutate → Analyze → Before/After Result
```

## Architecture

The existing Phase 46 layers remain unchanged in shape:

```text
VS Code command
  ↓
PbirAuthoringWorkflow
  ↓
AnalyzerBridgeService
  ↓
pbir/authoring JSON-RPC
  ↓
PbirAuthoringRpcAdapter
  ↓
PbirAuthoringRpcDispatcher
  ↓
LocalPbirMutationProviderService
  ↓
Planner / Executor / Merge / Serializer / Validator / Analyzer
```

The backend typed mutation contract remains broader for internal callers. The public boundary admits only one `RenamePage` operation. The adapter and VS Code workflow both enforce that boundary without duplicating planner behavior.

## Preview and execution

The existing `pbir-authoring-rpc/v1` Mutate operation gains an explicit `mode` of `preview` or `execute`. Both requests carry the same typed mutation request. Preview invokes the existing planner and returns a typed preview; it never materializes an artifact. Execute resolves the opaque snapshot, re-plans from authoritative state, then runs the existing executor, merge, serializer, validation, materialization, and analyzer path. Preview output is never accepted as execution authority.

The preview contains only transport-safe semantic data:

- mutation kind and target page ID
- current and proposed display names
- affected page and visual IDs
- preserved page and visual IDs
- expected affected object counts
- planner diagnostics
- execution admissibility and no-op status
- deterministic preview/request correlation evidence when it can be added without a separate contract layer

It does not expose shared IR, raw PBIR JSON, envelope contents, filesystem paths, or execution plans.

## Handles and comparison

Imported snapshot handles are immutable. A successful execution returns a new opaque artifact handle, and the extension retains that handle for subsequent analysis. The extension never infers an output directory.

The execution result returns backend-owned before/after analyzer summaries, score delta, fidelity, structured diagnostics, preserved identity evidence, artifact identity, and timings. TypeScript renders these values without recomputing them.

## UX and boundaries

The workflow uses Quick Pick, Input Box, confirmation, and output-channel primitives. It requires an imported snapshot, obtains page metadata from the import response, previews the rename, supports cancellation, and executes only after confirmation. Same-name requests are deterministic no-ops: zero changes, not executable, and never materialized. Empty and invalid names remain planner-authoritative.

No webview, graphical designer, dynamic capability menu, mutation batching, undo/redo, raw JSON editing, IR exposure, or additional public mutation is included. The no-undo boundary is explicit: mutation evidence is returned for future design, but rollback is not implemented in Phase 47.

## Testing and evidence

Focused backend, RPC, and extension tests cover admission, preview non-persistence, planner/executor separation, invalid targets and names, no-op behavior, stale handles, cancellation, execution and analyzer failures, stable identity preservation, transport mapping, and opaque handle lifecycle. Timings are observational only and have no thresholds.
