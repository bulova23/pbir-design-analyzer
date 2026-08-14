# Phase 45 — Direct Typed PBIR Authoring Current State

Phase 45 selects and validates the existing typed backend authoring services as the direct in-process boundary for backend orchestration and tests. No new façade or `pbir-authoring-rpc/v1` production contract is approved. The pre-existing `PbirAuthoringRpc` files remain preserved as superseded working-tree material and are not registered or used.

## Supported operations

| Direct caller | Typed input | Existing backend path | Result |
| --- | --- | --- | --- |
| Backend orchestration/tests | Existing generation request v1–v7 | `LocalPbirGenerationProviderService`, serializer/validator, round-trip analyzer | Typed artifact, validation, diagnostics, analyzer evidence |
| Backend orchestration/tests | Imported source directory and mutation request v1 | `LocalPbirMutationProviderService`, reader, planner, executor, merge, serializer, fidelity, analyzer | Typed import snapshot, mutation result, stable identity/fidelity evidence |

The direct boundary has no generic parameter map, arbitrary JSON field, RPC operation catalog, raw IR caller contract, transport registration, or cross-process response contract.

## Error categories

Existing typed provider diagnostics and readiness results remain authoritative. No new RPC error categories were introduced.

## Compatibility

Existing generation request schemas v1 through v7 and mutation request schema v1 remain unchanged. No generation or mutation schema was added or modified by Phase 45.

Imported authoring state remains typed within the existing backend snapshot and is not exposed through a new process boundary or handle contract.

## Equivalence and timing

Focused tests invoke the providers directly and verify pinned-schema rejection, deterministic hashes, typed/opaque merge preservation, stable identity, fidelity evidence, and absence of an RPC host surface in the core assembly.

## Boundaries and limitations

The existing JSON-RPC host and VS Code extension remain unchanged. There is no HTTP/gRPC transport, authentication, authorization, hosted/Windows/Desktop execution, semantic-model/DAX generation, or VS Code mutation flow. Reconsider an RPC adapter only after an approved VS Code workflow demonstrates one narrow cross-process need.
