# Phase 45 Roadmap Reconciliation

Date: 2026-08-14

## Decision

**OPTION B — DIRECT TYPED BACKEND CALLER SELECTED**.

Phase 45 resolves the architecture question without adding an RPC boundary. The existing typed backend services are sufficient for the demonstrated callers: backend orchestration and backend tests. The repository does not demonstrate a VS Code workflow that needs authoring generation, import, typed mutation, authoring validation, or authoring analysis across the existing stdio process boundary. Therefore no authoring RPC method, transport adapter, path contract, snapshot handle, concurrency contract, or response contract is approved or frozen by Phase 45.

## Phase 44 status

Phase 44 is **COMPLETE**, not deferred or protected future work. HEAD `8b109776` is the Phase 44 implementation commit and its subject is `feat: Implement Phase 44 Semantic Binding Projection and Full Round-Trip Fidelity`. The commit contains the Phase 44 design, plan, implementation note, semantic projection service/model/tests, roadmap closeout, and session closeout. Its recorded focused validation is 23/23, with full backend, build, extension, and diff checks passing.

Phase 44 completed descriptor-based semantic binding projection for the supported visual families, unsupported-role preservation and diagnostics, shared-IR semantic equivalence, imported analyzer comparison, and timing observations. It explicitly keeps RPC, VS Code integration, new visual families, and hosted execution out of scope. Therefore the 43 → 45 transition is justified because Phase 44 is already complete and its remaining limitations are bounded unsupported authoring domains—not a prerequisite for a caller-less RPC.

## Phase 43 relationship and protection

Phase 43 is treated as complete based on the current uncommitted completion evidence. Its uncommitted implementation/documentation changes were present at goal start and remain untouched. Some of those files overlap paths introduced by the Phase 44 commit, especially the report reader and fidelity tests; this overlap is pre-existing and is not a Phase 45 change.

The Phase 43 architecture remains the required domain boundary for any future authoring integration:

```text
typed request
  -> existing provider/mutation path
  -> single copy-on-write merge
  -> deterministic serializer
  -> pinned schema and fidelity validation
```

An RPC must delegate to this path and must not expose the envelope, opaque source content, generic JSON mutation, or internal IR.

## Existing RPC evidence

The existing boundary is local VS Code extension ↔ packaged .NET backend over stdio JSON-RPC 2.0 with LSP-style Content-Length framing. `RpcHost/Program.cs` composes `AnalyzerRpcDispatcher` and `SimpleJsonRpcServer`; `AnalyzerRpcDispatcher` currently registers analyzer/LSP methods and the three materialization routes:

- `model/ping`
- `model/pbir/getTree`
- `model/pbir/scoreReport`
- `model/pbir/governanceCheck`
- `pbir/materialization/preview`
- `pbir/materialization/apply`
- `pbir/materialization/recovery/inspect`

The extension calls these through `AnalyzerBridgeService` and `PbirMaterializationWorkflow`. No current extension caller requests PBIR generation, import, typed authoring mutation, authoring validation, or authoring analysis through RPC. The backend generation/mutation/provider services are directly callable in-process and their existing tests exercise that path.

## Option comparison

### Option A — VS Code RPC caller

Option A is not selected. The existing VS Code caller owns materialization preview/apply/recovery only. `AnalyzerBridgeService` provides a generic request mechanism, but that mechanism is not evidence of an authoring use case. `PbirMaterializationWorkflow` has no authoring generation/import/mutation/validation/analysis state or user workflow, and `AnalyzerRpcDispatcher` has no corresponding authoring route. Approving a five-operation authoring surface would freeze contracts before a caller, operation, output owner, or UX need exists.

### Option B — direct typed backend caller

Option B is selected. The existing typed boundary is the backend authoring service set:

- `LocalPbirGenerationProviderService` for generation requests v1–v7;
- `LocalPbirMutationProviderService` for typed import, planning, and execution over `LocalPbirMutationRequest` v1;
- existing `PbirAuthoringMergeService`, deterministic serializer, pinned-schema validator, and `PbirAuthoringFidelityService` as collaborators within the backend authoring path.

Backend tests and future backend orchestration are the intended callers. They can invoke these services directly in the same process, retain typed request/result semantics, and preserve the one copy-on-write merge boundary, typed/opaque separation, pinned-schema validation, deterministic serialization, stable identities, generation compatibility, and analyzer/scoring separation. No process boundary, extension-owned state, or user-facing response projection is currently required.

## Why the existing Phase 45 proposal is not an RPC

The pre-existing uncommitted Phase 45 design, plan, and untracked `PbirAuthoringRpc` files describe a transport-independent five-operation adapter. They are preserved as user work, but are not approved production behavior and do not establish a real RPC caller. The design and plan are reconciled below as superseded proposals rather than implementation authority.

The direct typed boundary is sufficient because all demonstrated authoring callers are backend-local. The current RPC boundary remains reserved for existing analyzer/materialization workflows and is unchanged.

## Contracts intentionally left unfrozen

The following remain deliberately undefined until a real cross-process caller is demonstrated:

- RPC method name, transport adapter, and registration point;
- VS Code owning workflow and single first operation;
- path, workspace, and report-location admission rules at an RPC boundary;
- snapshot/handle representation, lifetime, source identity, and stale-state behavior;
- cancellation, timeout, concurrency, serialization, idempotency, and snapshot semantics across processes;
- response, diagnostic, error, and analyzer-result projections;
- whether any authoring operation needs RPC at all;
- any Generate/Import/Mutate/Validate/Analyze operation catalog.

## Evidence that would justify a future RPC adapter

Reconsider Option A only after all of the following evidence exists: an approved VS Code user workflow; one narrow operation that cannot remain backend-local; a named extension owner and output consumer; a typed request/result contract derived from that workflow; explicit path/workspace and snapshot ownership; cancellation and concurrency behavior required by the workflow; compatibility requirements with existing extension contracts; and focused caller-to-backend tests proving the cross-process need. The first adapter, if later justified, must expose one existing workflow and one narrow operation, not the five-operation proposal by default.

## Reconciled roadmap

- Phase 42: COMPLETE
- Phase 43: COMPLETE
- Phase 44: COMPLETE
- Phase 45: DECISION RECORDED — OPTION B, direct typed backend caller; no RPC approved
- Phase 46: not started

## Non-goals for this gate

No RPC host registration, production authoring RPC implementation, VS Code UI, generation contract changes, Phase 44 changes, provider behavior changes, public API, generic command dispatch, arbitrary filesystem access, or new transport was made by this reconciliation.
