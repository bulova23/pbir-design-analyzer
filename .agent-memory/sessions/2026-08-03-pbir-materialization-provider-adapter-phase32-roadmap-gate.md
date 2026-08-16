# 2026-08-03 PBIR Materialization Provider Adapter Phase 32 Roadmap Gate

## Requested Objective

Implement a bounded provider-facing backend adapter over Repository Phase 31 materialization orchestration, but stop if repository evidence does not clearly identify that adapter as the next Repository Phase 32 slice.

## Evidence Rechecked

- `docs/ROADMAP.md` explicitly maps Repository Phase 29 to original Phase 4A, Repository Phase 30 to original Phase 4B, and Repository Phase 31 to post-4B application orchestration.
- `docs/ROADMAP.md` explicitly states that Repository Phase 32 is not mapped and that a provider-facing transport adapter over Phase 31 requires an approved mapping before implementation.
- The original Design Package to Microsoft Skills design and plan describe a broader Microsoft PBIR adapter downstream from Generation Request, provider planning, capability negotiation, and execution-provider contracts. They do not define a local Phase 31 transport wrapper.
- Provider Adapter Framework, Execution Provider Contract Framework, and Runtime Provider Framework remain contract-only and explicitly exclude provider invocation.
- `RpcHost/Program.cs` processes requests serially, deserializes with case-insensitive permissive options, accepts unbounded positive content lengths, lacks per-request cancellation registration and cancellation notifications, and reports unhandled exception messages. Those gaps prevent the requested strict, bounded, cancellable, concurrent adapter from being a thin reuse of an existing transport lifecycle.
- Phase 31 remains the only mapped orchestration boundary and already preserves exact preview identity, fresh transaction IDs, typed outcomes, cancellation safety, recovery inspection, deterministic Phase 29 output, and Phase 30 transaction truth.

## Decision

The requested provider-facing materialization adapter is not clearly the next roadmap slice. The user-defined stop condition therefore applies.

No Phase 32 design, implementation plan, production code, test, transport change, provider route, or validation claim was added. Existing uncommitted Phase 29–31 work and the prior Phase 32 roadmap-gate documentation were preserved.

## Smallest Roadmap-Aligned Alternative

Authorize a design-only roadmap amendment that names a local materialization transport adapter as Repository Phase 32 and explicitly separates two bounded slices:

1. RpcHost request-lifecycle hardening: strict envelope and payload deserialization, maximum request size, correlation, per-request cancellation, concurrent dispatch with serialized writes, disconnect-safe response handling, and redacted transport diagnostics.
2. A stateless local adapter exposing only preview, exact-preview apply, and read-only recovery inspection through `PbirMaterializationOrchestrationService`.

Keep this local transport path separate from the broader first runtime-provider implementation required by the original Phase 4 provider stack. After the roadmap amendment and design are approved, implementation can proceed test-first without inventing provider semantics.

## Validation

- Primary roadmap, original Phase 4 design/plan, Phase 29–31 state, provider framework state, Phase 31 contracts, RpcHost source, and RpcHost tests were inspected.
- Existing roadmap discrepancy statements remain present in ROADMAP.md, architecture-gap analysis, provider-adapter current state, and repository memory.
- No backend, Jest, TypeScript, or schema suite was rerun because the roadmap gate prohibited product and test changes.
- No commit, push, pull request, merge, discard, or cleanup action was performed.
