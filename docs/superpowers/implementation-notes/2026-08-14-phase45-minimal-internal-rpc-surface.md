# Phase 45 — Minimal Direct Typed Backend Service

## Decision and implementation

Phase 45 established the transport-independent `pbir-authoring-rpc/v1` contract and dispatcher over the existing typed backend authoring services. Phase 46 is the first approved external consumer, adding a thin RpcHost adapter for Generate, Import, and Analyze only.

The direct boundary is:

- `LocalPbirGenerationProviderService` for the existing generation request contracts v1–v7;
- `LocalPbirMutationProviderService` for typed PBIR import, mutation planning, and mutation execution over mutation request v1;
- `PbirAuthoringMergeService`, deterministic serializer/validator, `PbirAuthoringFidelityService`, and analyzer/scoring services as existing collaborators in the backend path.

The focused `Phase45DirectTypedAuthoringBoundaryTests` exercises successful direct generation, pinned-schema rejection without a partial artifact, typed/opaque preservation at the single merge boundary, deterministic hashes, stable imported identity, round-trip fidelity evidence, and boundary enforcement.

## Preserved invariants

Direct invocation preserves typed/opaque separation, the single copy-on-write merge boundary, pinned-schema validation, deterministic serialization, stable imported identities, generation v1–v7 compatibility, analyzer/scoring separation, Phase 42 interactions, and existing backend/package behavior.

## Explicit non-goals

Phase 45 itself added no RPC registration or VS Code workflow. Those concerns are implemented additively in the Phase 46 adapter and command workflow; Mutation and standalone Validate remain unregistered.

## Validation evidence

- Focused direct-boundary tests: 5 passed.
- Existing backend generation, mutation, reader, serializer, fidelity, and analyzer tests remain the relevant regression surface.
- The implementation does not modify `RpcHost`, VS Code, or transport code.
