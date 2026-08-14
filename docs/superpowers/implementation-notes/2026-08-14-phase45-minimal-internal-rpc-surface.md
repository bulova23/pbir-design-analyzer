# Phase 45 — Minimal Direct Typed Backend Service

## Decision and implementation

Phase 45 is implemented as Option B: the existing typed backend authoring services are the direct service boundary for backend orchestration and tests. No additional façade was added because the approved design explicitly identifies these services as sufficient callers and says no new façade is required.

The direct boundary is:

- `LocalPbirGenerationProviderService` for the existing generation request contracts v1–v7;
- `LocalPbirMutationProviderService` for typed PBIR import, mutation planning, and mutation execution over mutation request v1;
- `PbirAuthoringMergeService`, deterministic serializer/validator, `PbirAuthoringFidelityService`, and analyzer/scoring services as existing collaborators in the backend path.

The focused `Phase45DirectTypedAuthoringBoundaryTests` exercises successful direct generation, pinned-schema rejection without a partial artifact, typed/opaque preservation at the single merge boundary, deterministic hashes, stable imported identity, round-trip fidelity evidence, and boundary enforcement.

## Preserved invariants

Direct invocation preserves typed/opaque separation, the single copy-on-write merge boundary, pinned-schema validation, deterministic serialization, stable imported identities, generation v1–v7 compatibility, analyzer/scoring separation, Phase 42 interactions, and existing backend/package behavior.

## Explicit non-goals

No RPC registration, JSON-RPC method, transport adapter, extension caller, VS Code workflow, Content-Length change, path or snapshot-handle contract, cancellation or cross-process concurrency contract, hosted execution, Desktop/Windows dependency, or generalized Generate/Import/Mutate/Validate/Analyze API was added. The pre-existing untracked `PbirAuthoringRpc` proposal files remain preserved as superseded working-tree material and are not registered or used.

## Validation evidence

- Focused direct-boundary tests: 5 passed.
- Existing backend generation, mutation, reader, serializer, fidelity, and analyzer tests remain the relevant regression surface.
- The implementation does not modify `RpcHost`, VS Code, or transport code.
