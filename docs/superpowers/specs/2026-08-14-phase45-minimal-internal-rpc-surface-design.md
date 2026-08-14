# Phase 45 — Authoring Boundary Architecture Decision

## Status

**Decision recorded: Option B — direct typed backend caller.** This document does not approve an RPC contract or production behavior. The previously proposed transport-independent adapter is retained only as superseded working-tree material.

## Goal

Choose the smallest justified authoring boundary from demonstrated callers and preserve the existing backend authoring contracts until a real cross-process caller requires an adapter.

## Selected boundary and callers

The selected boundary is the existing typed backend service set, not an RPC namespace:

- `LocalPbirGenerationProviderService` accepts the existing generation request records v1–v7 and returns the existing typed generation result.
- `LocalPbirMutationProviderService` accepts a typed report source for import, plans `LocalPbirMutationRequest` v1, and executes the existing typed mutation plan.
- The existing authoring merge service, deterministic serializer, pinned-schema validator, fidelity service, and analyzer/scoring services remain collaborators of backend orchestration and tests.

No new façade is required for Phase 45. Backend orchestration and tests are the intended callers. Direct invocation is sufficient because the demonstrated callers are in-process, no extension workflow requests these operations, and no authoring output currently crosses the stdio boundary.

## Decision rationale and preserved invariants

Direct typed invocation preserves semantic-lossless authoring, one copy-on-write merge boundary, typed/opaque separation, pinned-schema validation, deterministic serialization, stable imported identities, analyzer/scoring separation, generation v1–v7 compatibility, Phase 42 interactions, existing RPC transport, existing extension contracts, backend/macOS execution, no Desktop dependency, no Windows requirement, and no hosted execution.

The direct boundary does not define path handling, workspace admission, wire errors, cancellation, cross-process concurrency, snapshot handles, or response projections. Those concerns belong to a future caller-specific adapter only if evidence justifies one.

## Intentionally unfrozen RPC questions

The RPC method name, transport registration, owning VS Code workflow, first operation, path/workspace rules, snapshot/handle lifetime, cancellation and timeout behavior, concurrency and idempotency semantics, response/error projection, and any Generate/Import/Mutate/Validate/Analyze catalog remain unfrozen. Future evidence must identify one real cross-process caller and one narrow operation before any of these are documented as contracts.

## Evidence that would justify a future RPC adapter

Reconsider Option A only after all of the following evidence exists: an approved VS Code user workflow; one narrow operation that cannot remain backend-local; a named extension owner and output consumer; a typed request/result contract derived from that workflow; explicit path/workspace and snapshot ownership; cancellation and concurrency behavior required by the workflow; compatibility requirements with existing extension contracts; and focused caller-to-backend tests proving the cross-process need. The first adapter, if later justified, must expose one existing workflow and one narrow operation, not the five-operation proposal by default.

## Explicit non-goals

No JSON-RPC host registration, VS Code command/UI, HTTP/gRPC transport, streaming, authentication, authorization, hosted/Windows/Desktop execution, semantic-model/DAX generation, new generation/mutation schema, or broad authoring API is included. The pre-existing `PbirAuthoringRpc` files are not production authority and must not be registered or treated as an approved Phase 45 implementation.
