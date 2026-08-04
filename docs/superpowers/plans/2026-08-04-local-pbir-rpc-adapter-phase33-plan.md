# Local PBIR RPC Adapter — Repository Phase 33 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose the Phase 31 materialization application boundary through three strictly validated local JSON-RPC operations on the hardened Phase 32 transport.

**Architecture:** Add one stateless adapter behind the existing dispatcher. RpcHost references Core through an internal friend boundary, so the adapter can construct and invoke the existing Phase 31 orchestration types without copying serializer or filesystem logic. The adapter owns only wire validation, safe response mapping, and operation limits.

**Tech Stack:** .NET 8, System.Text.Json, existing SimpleJsonRpcServer/RpcRequestRegistry/RpcResponseWriter, xUnit, deterministic in-memory streams and synchronization seams.

---

### Task 1: Establish the Core-to-RpcHost dependency boundary

**Files:**
- Modify: `service-dotnet/RpcHost/RpcHost.csproj`
- Modify: `service-dotnet/Properties/AssemblyInfo.cs`
- Test: `service-dotnet/tests/RpcHostScopeBoundaryTests.cs`

- [ ] Add a project reference from RpcHost to `PbirDesignAnalyzer.Core.csproj`, remove duplicate source-linked PBIR service compilation, and keep the existing public analyzer services available from Core.
- [ ] Add only `ModelingLanguageServer` to Core's internal friend list; do not make Phase 31 or Phase 30 services public.
- [ ] Add a boundary assertion that RpcHost contains the adapter but no direct route handler type for Phase 30 writer/apply/rollback services.
- [ ] Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~RpcHostScopeBoundaryTests` and expect the new boundary assertion to fail before the adapter exists.

### Task 2: Define strict adapter wire contracts and limits

**Files:**
- Create: `service-dotnet/RpcHost/PbirMaterializationRpcContracts.cs`
- Create: `service-dotnet/RpcHost/PbirMaterializationRpcValidation.cs`
- Test: `service-dotnet/tests/PbirMaterializationRpcContractTests.cs`

- [ ] Define operation constants, request/response version constants, safe adapter outcome strings, DTOs, and operation-specific limits lower than `RpcTransportOptions.Production`.
- [ ] Implement bounded UTF-8 payload validation with exact root and recursive property checks, duplicate-field rejection, strict `JsonSerializerOptions` with unmapped members disallowed, and safe fixed diagnostics.
- [ ] Validate safe identifiers, fresh transaction IDs, operation/version alignment, read-only policy flags, local destination syntax, relative artifact paths, schema versions, and supported modern artifact profile before constructing Phase 31 requests.
- [ ] Add failing tests for supported/unsupported versions, malformed/unknown/duplicate/unexpected fields, oversized and hostile fields, invalid IDs, unsafe destinations, unsupported artifacts, and exact response serialization.
- [ ] Run the focused contract tests and verify they fail for the absent adapter behavior.

### Task 3: Implement stateless adapter invocation and explicit outcome mapping

**Files:**
- Create: `service-dotnet/RpcHost/PbirMaterializationRpcAdapter.cs`
- Modify: `service-dotnet/RpcHost/AnalyzerRpcDispatcher.cs`
- Modify: `service-dotnet/RpcHost/Program.cs`
- Test: `service-dotnet/tests/PbirMaterializationRpcAdapterTests.cs`

- [ ] Inject one `PbirMaterializationOrchestrationService` into the adapter and invoke only `Preview`, `Apply`, or `InspectRecovery` after validation.
- [ ] Map all fifteen Phase 31 typed outcomes explicitly to stable wire strings and redact unsafe result/diagnostic fields.
- [ ] Register exactly the three supported routes, preserve existing routes, add no initialize capability, and retain Method Not Found for all other operations.
- [ ] Add tests for valid preview/apply/recovery requests, every typed outcome, exact preview identity, fresh transaction IDs, duplicate/reused IDs, handler faults, no direct writer exposure, and no provider/Skills/UI/deployment/Desktop/Analyzer/PBIP/legacy-report authority.

### Task 4: Verify transport lifecycle and concurrency integration

**Files:**
- Modify: `service-dotnet/tests/RpcHostLifecycleTests.cs`
- Modify: `service-dotnet/tests/RpcHostResponseWriterTests.cs`
- Create: `service-dotnet/tests/PbirMaterializationRpcTransportTests.cs`

- [ ] Use deterministic gates and in-memory streams to test cancellation before dispatch, during preview, during apply, after completion, repeated cancellation, completion-vs-cancellation arbitration, concurrent previews, apply contention, disconnect during each operation, and cleanup counts.
- [ ] Verify response frame integrity and request correlation when operations complete out of order.
- [ ] Verify recovery inspection changes no filesystem state and exposes no rollback route.
- [ ] Run RPC transport tests and the focused Phase 33 adapter suite.

### Task 5: Update current state, roadmap, and historical integration documents

**Files:**
- Create: `docs/current-state/pbir-materialization-rpc-adapter-state.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/superpowers/plans/2026-06-20-design-package-microsoft-skills-integration-plan.md`
- Modify: `docs/current-state/architecture-gap-analysis.md`
- Modify: `docs/current-state/pbir-materialization-provider-adapter-state.md`
- Modify: `.agent-memory/repo-map.md`
- Modify: `.agent-memory/current-focus.md`
- Modify: `.agent-memory/session-summaries.md`
- Create: `.agent-memory/sessions/2026-08-04T-phase33-local-pbir-rpc-adapter.md`

- [ ] Record the Phase 29–33 mapping, exact three operations, compatibility impact, inherited/operation-specific limits, and the provisional unauthorized Phase 34–44 sequence.
- [ ] Record that external providers and Microsoft Skills remain research/planning input only and that Phase 33 does not add any execution authority.
- [ ] Keep the session record concise and include failed validation commands if any cannot be repaired.

### Task 6: Run the complete validation inventory

**Files:**
- No production file changes; inspect the complete diff.

- [ ] Run focused Phase 33 tests, RPC transport tests, Phase 29–33 changed-file regression inventory, full backend tests with zero failures/skips, all eight pinned offline schema/boundary tests, extension and webview Jest, TypeScript compilation, scoped changed-file lint, repository lint baseline comparison, roadmap/document/placeholder/whitespace/scope/production-boundary/changed-boundary/repository-output gates, and `git diff --check`.
- [ ] Re-read the design and plan against the implementation and record exact totals.
- [ ] Leave the branch and worktree uncommitted and present the four requested disposition choices.

