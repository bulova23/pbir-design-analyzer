# Phase 45 — Direct Typed PBIR Authoring Current State

Phase 45 established the transport-independent `pbir-authoring-rpc/v1` contract and dispatcher over the existing typed backend authoring services. Phase 46 consumes that contract through the thin `pbir/authoring` JSON-RPC route for Generate, Import, and Analyze only.

## Core contract operations

| Operation | Typed input | Existing backend path | Result |
| --- | --- | --- | --- |
| Generate | Existing generation request v1–v7 | `LocalPbirGenerationProviderService`, serializer/validator, round-trip analyzer | Typed artifact, diagnostics, analyzer evidence |
| Import | Supported source directory | `LocalPbirMutationProviderService` and reader | Opaque snapshot handle, diagnostics |
| Mutate | Imported snapshot and mutation request v1 | Existing mutation planner/executor/merge/serializer | Backend-only artifact and fidelity result |
| Validate | Opaque artifact handle | Existing serializer validator | Backend-only validation result |
| Analyze | Artifact handle, snapshot handle, or explicit report directory | Existing scoring service | Analyzer summary and timing |

The core boundary has no generic parameter map, arbitrary JSON mutation field, raw IR caller contract, or dependency on RpcHost or VS Code.

## Error categories

Existing typed provider diagnostics and readiness results remain authoritative. No new RPC error categories were introduced.

## Compatibility

Existing generation request schemas v1 through v7 and mutation request schema v1 remain unchanged. No generation or mutation schema was added or modified by Phase 45.

Imported authoring state remains typed within the existing backend snapshot. Handles are opaque and session-oriented.

## Equivalence and timing

Focused tests invoke the providers directly and verify pinned-schema rejection, deterministic hashes, typed/opaque merge preservation, stable identity, fidelity evidence, and absence of an RPC host surface in the core assembly.

## Boundaries and limitations

Phase 46 adds only the `pbir/authoring` JSON-RPC route and the three minimal commands documented in `docs/current-state/phase46-vscode-authoring-integration-state.md`. There is no HTTP/gRPC transport, hosted/Windows/Desktop execution, semantic-model/DAX generation, or VS Code mutation/Validate flow.
