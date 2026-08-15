# RC1 Architecture Assessment

## Executive assessment

The platform is stable enough for controlled UAT and for narrowly scoped
follow-on planning. It is not yet a fully stable v1.0 public authoring SDK.
The strongest boundaries are the authoritative analyzer, shared authoring
envelope/IR, immutable snapshot lifecycle, typed planner, and versioned
`pbir-authoring-rpc/v1` boundary. The main risks are contract growth across
v1–v7, the split between broad internal mutation types and the narrow public
allowlist, checked-in generated target binaries, and the known lint baseline.

## Contract answers

| Boundary | Assessment | Evidence and risk |
| --- | --- | --- |
| Backend contract | Stable for additive local generation and curated mutation; not frozen as a public SDK | v1–v7 are additive, but the catalog is already broad and internal/public mutation scopes differ. |
| Mutation model | Semantically sound for RC1 | Preview/execute, fresh planning, immutable snapshots, typed diffs, validation, and new handles are coherent. Undo/redo and batch semantics are intentionally absent. |
| Descriptor catalog | Stable enough for current supported families | One catalog drives generation/projection, but unsupported imported roles remain a long-term compatibility boundary. |
| Shared IR | Stable enough for round-trip and mutation | Lossless envelope protects unsupported content within pinned schema ownership; arbitrary JSON mutation is correctly closed. |
| Authoring envelope | Stable for process-local workflows | Opaque handles and provenance are appropriate; persistence/version migration is not yet a public promise. |
| Analyzer pipeline | Stable and authoritative | Generated round-trip and mutation before/after analysis reuse scoring rather than duplicating it. |
| RPC contract | Stable for the existing local boundary | `pbir-authoring-rpc/v1` validates requests before consumption; public adapter admission is intentionally narrower than backend enums. |

## Maintainability findings ranked by long-term risk

### High

1. Public/internal capability drift: the backend contains more mutation kinds
   than the VS Code picker admits. Without a single explicit admission matrix,
   future contributors can accidentally expose a typed backend operation or
   document it as public.
2. Seven additive generation versions duplicate large request records. This is
   backward-compatible today but will increase validation and projection drift
   as more versions are added.
3. Checked-in target-specific backend binaries are rewritten by packaging and
   can create noisy, machine-dependent diffs. Release provenance is harder to
   review than source-only packaging.

### Medium

4. The 43-error ESLint baseline weakens the signal from future lint failures and
   makes AI-generated cleanup harder to review.
5. Backend dispatcher dictionaries hold process-local snapshots and artifacts;
   lifecycle, memory bounds, and restart behavior are contract-relevant but
   not yet a durable persistence design.
6. The descriptor/IR boundary has a growing set of typed records and fallback
   diagnostics. Without generated schema inventory or contract fixtures, new
   fields may be added inconsistently across generation and import.

### Low

7. Nullable warnings in tests obscure whether a new warning is meaningful.
8. Target package size variance, especially Windows ARM64 self-contained
   output, increases distribution cost but does not threaten correctness.

## Stability decision

Do not widen the public mutation surface during UAT. Use observed UAT failures
to decide whether the next major version needs contract consolidation,
capability negotiation, or simply more catalog coverage. Preserve analyzer,
IR, envelope, and RPC boundaries until that decision is made.
