# Phase 47 — Interactive Mutation Workflow Implementation

## Delivered

- Added explicit preview and execute modes under pbir-authoring-rpc/v1.
- Exposed only one public mutation: RenamePage.
- Added backend-generated semantic preview data and deterministic preview IDs.
- Added backend-provided import page metadata for Quick Pick selection.
- Kept preview non-materializing and re-planned on execute.
- Returned a new opaque artifact handle after execution while preserving the imported snapshot.
- Returned analyzer-before/analyzer-after summaries, score delta, fidelity, preserved identities, structured diagnostics, and timing observations.
- Added one VS Code command using Quick Pick, Input Box, confirmation, and the existing output channel.
- Added deterministic same-name no-op behavior and no-undo documentation.

## Workflow

```text
Import
  ↓
Select Rename Page
  ↓
Backend Planner
  ↓
Typed Semantic Preview
  ↓
Confirm / Cancel
  ↓
Backend Re-plan and Mutate
  ↓
Analyzer
  ↓
Before / After Result
```

## Preview example

```text
Rename page

Current:
Overview

New:
Executive Summary
```

## Diff model

The backend returns mutation kind, target page identity, current and proposed
names, affected and preserved page/visual identities, affected object count,
planner diagnostics, execution admissibility, no-op state, and a deterministic
preview correlation ID. It does not return IR, raw PBIR JSON, envelope data,
filesystem paths, or execution plans.

## Performance evidence

The RPC timing contract now records dispatch, orchestration, serialization,
after-analyzer, planning, preview, and before-analyzer milliseconds. These are
observations only; no thresholds were added. Timings vary by fixture and host,
so the validation report records counts and presence of timing fields rather
than turning local measurements into release gates.

## Known limitations

- Rename Page is the only public mutation.
- The original snapshot is immutable and there is no undo/redo.
- Other typed mutation kinds remain backend-only.
- No dynamic capability discovery or mutation batching exists.
- No graphical designer, webview editor, raw JSON editor, Windows/Desktop execution, or hosted execution was added.

## Validation results

- Focused backend contract/mutation/import/adapter tests: 16 passed.
- Full backend Release tests: 986 passed, 11 expected Windows skips.
- Extension Jest: 502 passed across 98 suites.
- Webview Jest: 68 passed across 11 suites.
- TypeScript compilation, production build, and VSIX packaging passed.
- Changed-file ESLint passed. Full ESLint retains the existing 43-error baseline.
- `git diff --check` passed and no changes were staged.
