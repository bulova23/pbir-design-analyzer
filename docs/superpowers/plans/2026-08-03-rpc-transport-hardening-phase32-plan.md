# Repository Phase 32 — RPC Transport Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Harden the existing local stdio JSON-RPC host with strict finite parsing, bounded concurrent dispatch, deterministic cancellation and duplicate-id handling, serialized response frames, safe shutdown, and redacted diagnostics without adding application operations.

**Architecture:** Keep one existing RpcHost stack. Extract generic framing, protocol parsing, response writing, registration, and lifecycle coordination behind internal interfaces; retain the current analyzer methods in a separate dispatcher and preserve all valid LanguageClient traffic.

**Tech Stack:** .NET 8, System.Text.Json Utf8JsonReader, Task and CancellationToken, SemaphoreSlim, Microsoft.Extensions.Logging, xUnit

---

Status: Executed and validated on 2026-08-03. The procedural checklist below is retained as the reviewed pre-implementation plan; the delivered behavior and exact validation evidence are recorded in the design outcome, RPC transport current-state document, and Phase 32 session record. No commit or push step was executed.

Implementation must remain uncommitted. Any commit steps normally required by the planning workflow are intentionally omitted because the Phase 32 authorization explicitly forbids commit and push.

## File Structure

- Create service-dotnet/RpcHost/RpcTransportOptions.cs for validated finite production limits.
- Create service-dotnet/RpcHost/JsonRpcProtocol.cs for request ids, validated requests, parse failures, strict envelope parsing, and response envelopes.
- Create service-dotnet/RpcHost/JsonRpcFraming.cs for bounded LSP header/body reads and terminal framing failures.
- Create service-dotnet/RpcHost/RpcResponseWriter.cs for bounded atomic frame serialization and connection write state.
- Create service-dotnet/RpcHost/RpcRequestRegistry.cs for canonical identity, per-request cancellation, terminal-state arbitration, registration cleanup, and request capacity.
- Create service-dotnet/RpcHost/SimpleJsonRpcServer.cs for bounded scheduling, cancellation notifications, duplicate handling, shutdown, disconnect, and task draining.
- Create service-dotnet/RpcHost/AnalyzerRpcDispatcher.cs for only the existing method switch and existing response payload behavior.
- Modify service-dotnet/RpcHost/Program.cs so it remains only the composition root plus AnalyzerServices.
- Modify service-dotnet/tests/RpcHostJsonRpcTests.cs to retain existing compatibility assertions against the extracted types.
- Create service-dotnet/tests/RpcHostRequestParsingTests.cs for strict framing, envelope, id, protocol, and limit boundaries.
- Create service-dotnet/tests/RpcHostResponseWriterTests.cs for atomic framing, response limits, and write shutdown.
- Create service-dotnet/tests/RpcHostLifecycleTests.cs for concurrency, cancellation, duplicate ids, completion races, shutdown, disconnect, and cleanup.
- Create service-dotnet/tests/RpcHostScopeBoundaryTests.cs for exact route inventory and forbidden dependency/authority checks.
- Create docs/current-state/rpc-transport-state.md and update the roadmap, original Phase 4–7 plan, architecture gap, provider-adapter state, repository map, and memory records.

### Task 1: Lock Roadmap Mapping And Pre-Implementation Records

**Files:**

- Modify: docs/ROADMAP.md
- Modify: docs/superpowers/plans/2026-06-20-design-package-microsoft-skills-integration-plan.md
- Modify: docs/current-state/architecture-gap-analysis.md
- Modify: docs/current-state/pbir-materialization-provider-adapter-state.md
- Modify: .agent-memory/repo-map.md

- [ ] **Step 1: Replace the unmapped Phase 32 statement with the explicit narrow mapping**

Record exactly:

```markdown
- Repository Phase 32: RPC Transport Hardening, shared infrastructure only; no original Phase 4 feature or application operation
```

- [ ] **Step 2: Record the provisional Phase 33–44 sequence**

State that the sequence is planning-only and does not authorize implementation. Keep Phase 33, PBIR operations, providers, Skills, UI, intake, Analyzer, deployment, publishing, and release work unimplemented.

- [ ] **Step 3: Update architecture and provider state without claiming an adapter exists**

Record that Phase 32 satisfies the transport prerequisite while the local Phase 31 RPC adapter remains Phase 33 and unimplemented.

- [ ] **Step 4: Run pre-production documentation gates**

Run:

```bash
rg -n "Repository Phase 32: RPC Transport Hardening|Repository Phase 33|Repository Phase 44" docs/ROADMAP.md docs/superpowers/plans/2026-06-20-design-package-microsoft-skills-integration-plan.md docs/current-state/architecture-gap-analysis.md docs/current-state/pbir-materialization-provider-adapter-state.md
rg -n "TO[D]O|TB[D]|implement la[t]er|fill in detai[l]s" docs/superpowers/specs/2026-08-03-rpc-transport-hardening-phase32-design.md docs/superpowers/plans/2026-08-03-rpc-transport-hardening-phase32-plan.md
git diff --check
```

Expected: mapping assertions are present; the placeholder scan has no output; diff check exits zero.

### Task 2: Add Strict Request Models And Parser

**Files:**

- Create: service-dotnet/tests/RpcHostRequestParsingTests.cs
- Create: service-dotnet/RpcHost/RpcTransportOptions.cs
- Create: service-dotnet/RpcHost/JsonRpcProtocol.cs

- [ ] **Step 1: Write failing option and parser tests**

Tests must use the wished-for API:

```csharp
var options = RpcTransportOptions.Production;
var result = JsonRpcRequestParser.Parse(Encoding.UTF8.GetBytes(json), options);

Assert.True(result.IsSuccess);
Assert.Equal(new RpcRequestId("s:request-1"), result.Request!.Id);
```

Add separate facts or theories for valid existing object params, multibyte UTF-8, omitted/null ids, string ids, integral numeric ids, malformed/truncated JSON, trailing JSON, missing fields, duplicates, unknown and wrong-case fields, unsupported versions, invalid ids, invalid params shapes, and every configured equality/one-over boundary.

- [ ] **Step 2: Run the parser tests and verify RED**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~RpcHostRequestParsingTests
```

Expected: compilation failure because the new parser types do not exist.

- [ ] **Step 3: Add validated finite options**

Implement this public shape internally:

```csharp
internal sealed record RpcTransportOptions(
    int MaxHeaderBytes,
    int MaxHeaderLineBytes,
    int MaxHeaderCount,
    int MaxRequestBytes,
    int MaxPayloadBytes,
    int MaxEnvelopeBytes,
    int MaxJsonDepth,
    int MaxMethodBytes,
    int MaxRequestIdBytes,
    int MaxResponseBytes,
    int MaxConcurrentRequests,
    int MaxRegisteredRequests)
{
    internal static RpcTransportOptions Production { get; } = new(
        8 * 1024, 4 * 1024, 16, 8 * 1024 * 1024, 7 * 1024 * 1024,
        64 * 1024, 64, 256, 128, 16 * 1024 * 1024, 8, 64);
}
```

The constructor must reject nonpositive values and inconsistent payload, envelope, request, and concurrency relationships.

- [ ] **Step 4: Implement strict parsing**

Use Utf8JsonReader with MaxDepth and CommentHandling.Disallow. Build a validated request only after the entire single top-level object is read. Store cloned params only after its measured slice fits MaxPayloadBytes. Return fixed parse classifications instead of exception text.

Core types:

```csharp
internal readonly record struct RpcRequestId(JsonValueKind Kind, string CanonicalValue, JsonElement JsonValue);
internal sealed record JsonRpcRequest(RpcRequestId? Id, string Method, JsonElement? Params);
internal sealed record JsonRpcParseResult(JsonRpcRequest? Request, RpcProtocolError? Error);
internal sealed record RpcProtocolError(int Code, string Message, RpcRequestId? ResponseId);
```

- [ ] **Step 5: Run parser tests and verify GREEN**

Run the Task 2 test command. Expected: all RpcHostRequestParsingTests pass.

### Task 3: Add Bounded Framing

**Files:**

- Modify: service-dotnet/tests/RpcHostRequestParsingTests.cs
- Create: service-dotnet/RpcHost/JsonRpcFraming.cs

- [ ] **Step 1: Add failing framing tests**

Exercise one-byte and chunked reads using:

```csharp
var result = await JsonRpcFraming.ReadFrameAsync(input, options, CancellationToken.None);
Assert.Equal(RpcFrameStatus.Frame, result.Status);
Assert.Equal(payloadBytes, result.Payload);
```

Cover exact Content-Length, optional supported Content-Type, multibyte bodies, EOF before a header, missing/duplicate/unknown headers, LF-only or malformed separators, header count/line/total boundaries, malformed/negative/zero/overflow length, exact request-size acceptance, one-byte-over rejection before body allocation, and truncated bodies.

- [ ] **Step 2: Run the framing tests and verify RED**

Run the Task 2 test command. Expected: failures because ReadFrameAsync and framing result types do not exist.

- [ ] **Step 3: Implement the bounded reader**

Use fixed-size pooled buffers and per-byte accounting for headers. Parse Content-Length with invariant integer rules. Rent the body only after validating it against MaxRequestBytes. Return one of Frame, EndOfStream, or TerminalFault; never scan for a new header after TerminalFault.

- [ ] **Step 4: Run the framing tests and verify GREEN**

Run the Task 2 test command. Expected: all parsing and framing tests pass.

### Task 4: Add The Atomic Bounded Response Writer

**Files:**

- Create: service-dotnet/tests/RpcHostResponseWriterTests.cs
- Create: service-dotnet/RpcHost/RpcResponseWriter.cs
- Modify: service-dotnet/RpcHost/JsonRpcProtocol.cs

- [ ] **Step 1: Write failing writer tests**

Use a stream that yields between partial writes and captures concurrent calls:

```csharp
await Task.WhenAll(
    writer.WriteResultAsync(firstId, new { value = "first" }, CancellationToken.None),
    writer.WriteResultAsync(secondId, new { value = "second" }, CancellationToken.None));

Assert.All(ParseFrames(output.Bytes), frame => Assert.True(frame.IsComplete));
```

Cover success, explicit null result, standard error, concurrent non-interleaving, exact response-size boundary, oversized-result substitution with a bounded fixed Internal Error, output failure, CloseAsync idempotency, and write suppression after close.

- [ ] **Step 2: Run writer tests and verify RED**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~RpcHostResponseWriterTests
```

Expected: compilation failure because RpcResponseWriter does not exist.

- [ ] **Step 3: Implement response envelopes and writer**

Keep JsonRpcSuccessResponse.Result marked JsonIgnoreCondition.Never. Serialize into ArrayBufferWriter<byte>, check WrittenCount, then acquire one SemaphoreSlim across header write, body write, and flush. Store a closed flag under the same lifecycle lock and dispose the semaphore once.

- [ ] **Step 4: Run writer tests and verify GREEN**

Run the Task 4 command. Expected: all writer tests pass.

### Task 5: Add Request Registration And Terminal-State Arbitration

**Files:**

- Create: service-dotnet/tests/RpcHostLifecycleTests.cs
- Create: service-dotnet/RpcHost/RpcRequestRegistry.cs

- [ ] **Step 1: Write failing registry tests**

Tests call the registry directly and assert one winning transition:

```csharp
Assert.True(registry.TryRegister(id, out var registration));
Assert.True(registration!.TryClaim(RpcTerminalOutcome.Cancelled));
Assert.False(registration.TryClaim(RpcTerminalOutcome.Completed));
Assert.True(registry.RemoveAndDispose(registration));
Assert.Equal(0, registry.Count);
```

Cover typed id distinction, capacity equality/one-over, duplicate active ids, cancellation before dispatch, cancellation during execution, cancellation after completion, repeated cancellation, completion/cancellation race using a Barrier or TaskCompletionSource, safe id reuse after removal, cancel-all, disposal, and exactly-once cancellation-source disposal.

- [ ] **Step 2: Run lifecycle tests and verify RED**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~RpcHostLifecycleTests
```

Expected: compilation failure because registry types do not exist.

- [ ] **Step 3: Implement registry state**

Use ConcurrentDictionary<string, RpcRequestRegistration> plus Interlocked state transitions. Canonical keys must include id type. Registration owns one linked CancellationTokenSource and exposes only TryMarkDispatched, TryClaim, Cancel, Token, and Dispose.

- [ ] **Step 4: Run registry tests and verify GREEN**

Run the Task 5 command. Expected: direct registry tests pass.

### Task 6: Add Concurrent Server Lifecycle

**Files:**

- Modify: service-dotnet/tests/RpcHostLifecycleTests.cs
- Create: service-dotnet/RpcHost/SimpleJsonRpcServer.cs

- [ ] **Step 1: Add failing server lifecycle tests**

Use an injected handler and admission seam:

```csharp
internal interface IRpcRequestHandler
{
    Task<object?> HandleAsync(JsonRpcRequest request, CancellationToken cancellationToken);
}
```

Build framed input streams and handler TaskCompletionSource gates. Cover existing valid request compatibility, simultaneous independent dispatch, out-of-order completion, queued cancellation before dispatch, executing cancellation, post-completion and repeated cancellation, completion races, deterministic duplicate ids, capacity rejection, handler cancellation, fixed handler-fault errors, and non-interleaved response frames.

- [ ] **Step 2: Run lifecycle tests and verify RED**

Run the Task 5 command. Expected: server-focused tests fail because SimpleJsonRpcServer is absent.

- [ ] **Step 3: Implement intake and scheduling**

The server loop reads frames sequentially, parses them strictly, handles $/cancelRequest inline, registers requests before awaiting a dispatch slot, and tracks every scheduled Task in a protected set. Use MaxConcurrentRequests for SemaphoreSlim and MaxRegisteredRequests in the registry.

- [ ] **Step 4: Implement deterministic response ownership**

For every handler path, call TryClaim before writing. Duplicate registration cancels the first registration, claims DuplicateId, writes one Invalid Request error, and suppresses both request bodies from further response authority.

- [ ] **Step 5: Run focused lifecycle tests and verify GREEN**

Run the Task 5 command. Expected: all lifecycle tests through concurrency, cancellation, duplicate ids, faults, and frame writing pass.

### Task 7: Add Shutdown, Disconnect, And Cleanup

**Files:**

- Modify: service-dotnet/tests/RpcHostLifecycleTests.cs
- Modify: service-dotnet/RpcHost/SimpleJsonRpcServer.cs
- Modify: service-dotnet/RpcHost/RpcResponseWriter.cs

- [ ] **Step 1: Add failing shutdown tests**

Cover zero, one, and multiple in-flight requests; queued plus executing requests; slow, blocked, cancelled, and faulting handlers; valid shutdown request; exit notification; truncated disconnect; output disconnect; repeated shutdown; and completion-versus-shutdown races. Assert after RunAsync:

```csharp
Assert.Equal(0, server.ActiveRequestCount);
Assert.Equal(0, server.TrackedTaskCount);
Assert.True(server.IsDisposed);
Assert.Equal(1, handler.DisposeCount);
```

Use internal observable state or injected disposal probes, not test-only production behavior.

- [ ] **Step 2: Run shutdown tests and verify RED**

Run the Task 5 command. Expected: new shutdown assertions fail.

- [ ] **Step 3: Implement one shutdown task**

Use an Interlocked shutdown owner and a shared TaskCompletionSource. Stop intake, reject registration, cancel all registrations, await the tracked-task set until empty, write a valid shutdown null result only when output remains connected, close the writer, and dispose owned resources. Repeated callers await the same task.

- [ ] **Step 4: Run shutdown tests and verify GREEN**

Run the Task 5 command. Expected: every lifecycle and cleanup test passes without sleeps.

### Task 8: Preserve Existing Analyzer Dispatch And Compatibility

**Files:**

- Create: service-dotnet/RpcHost/AnalyzerRpcDispatcher.cs
- Modify: service-dotnet/RpcHost/Program.cs
- Modify: service-dotnet/tests/RpcHostJsonRpcTests.cs

- [ ] **Step 1: Add failing compatibility tests**

Test the exact existing method inventory and representative initialize, notification, shutdown, ping, getTree invalid-params, scoreReport invalid-params, governanceCheck invalid-params, unknown-method, camel-case response, and explicit-null-result behavior.

Expected method inventory:

```csharp
new[]
{
    "initialize", "initialized", "textDocument/didOpen", "textDocument/didChange",
    "textDocument/didClose", "workspace/didChangeWatchedFiles",
    "workspace/didChangeConfiguration", "$/setTrace", "$/logTrace", "shutdown",
    "exit", "model/ping", "model/pbir/getTree", "model/pbir/scoreReport",
    "model/pbir/governanceCheck"
}
```

- [ ] **Step 2: Run existing and new compatibility tests and verify RED**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~RpcHostJsonRpcTests|FullyQualifiedName~RpcHostScopeBoundaryTests"
```

Expected: failures until dispatch is extracted and exposes the exact internal inventory.

- [ ] **Step 3: Extract the existing dispatcher**

Move the existing method switch and parameter helpers without adding methods. Pass CancellationToken to HandleAsync, call ThrowIfCancellationRequested before and after asynchronous work, and replace unhandled exception text with the server's fixed Internal Error.

- [ ] **Step 4: Reduce Program to composition**

Construct RpcTransportOptions.Production, AnalyzerRpcDispatcher, and SimpleJsonRpcServer over Console.OpenStandardInput and Console.OpenStandardOutput. Keep existing services and backend version unchanged.

- [ ] **Step 5: Run all focused RPC tests and verify GREEN**

Run:

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~RpcHost
```

Expected: every Phase 32 and existing RPC test passes.

### Task 9: Add Redacted Diagnostics And Scope Proof

**Files:**

- Modify: service-dotnet/tests/RpcHostLifecycleTests.cs
- Create: service-dotnet/tests/RpcHostScopeBoundaryTests.cs
- Modify: service-dotnet/RpcHost/SimpleJsonRpcServer.cs
- Modify: service-dotnet/RpcHost/RpcRequestRegistry.cs

- [ ] **Step 1: Add failing diagnostic tests**

Capture ILogger entries for a request containing a sensitive path and payload, a malicious method string, a raw id, a handler exception containing a transaction path, cancellation, duplicate id, and shutdown. Assert logs contain fixed event codes and a bounded correlation token but none of the supplied values or exception details.

- [ ] **Step 2: Add failing scope-boundary tests**

Read RpcHost source files and assert generic transport files do not contain PbirMaterializationOrchestrationService, Phase31, provider invocation, Skills execution, Process.Start, HttpClient, VS Code, webview, deployment, or publishing symbols. Assert the dispatcher method inventory equals the existing list exactly.

- [ ] **Step 3: Run focused tests and verify RED**

Run the Task 8 focused RPC command. Expected: diagnostic and boundary assertions fail until fixed logging and source boundaries are implemented.

- [ ] **Step 4: Implement fixed diagnostic events**

Use event ids and templates containing only event code, bounded state, counts, and SHA-256 correlation. Never pass request params, raw ids, handler exceptions, or peer-controlled invalid method strings to ILogger.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run the Task 8 focused RPC command. Expected: all RPC tests pass.

### Task 10: Complete Documentation And Repository Memory

**Files:**

- Create: docs/current-state/rpc-transport-state.md
- Modify: docs/superpowers/specs/2026-08-03-rpc-transport-hardening-phase32-design.md
- Modify: docs/superpowers/plans/2026-08-03-rpc-transport-hardening-phase32-plan.md
- Modify: docs/ROADMAP.md
- Modify: docs/superpowers/plans/2026-06-20-design-package-microsoft-skills-integration-plan.md
- Modify: docs/current-state/architecture-gap-analysis.md
- Modify: docs/current-state/pbir-materialization-provider-adapter-state.md
- Modify: .agent-memory/repo-map.md
- Modify: .agent-memory/current-focus.md
- Modify: .agent-memory/session-summaries.md
- Modify: .agent-memory/sessions/2026-08-03T184549Z-rpc-transport-hardening-phase32.md

- [ ] **Step 1: Document implemented guarantees and compatibility policy**

Record actual production limits, lifecycle state, error behavior, compatibility impact, ownership boundaries, and residual non-cooperative-handler/disconnect risks in rpc-transport-state.md.

- [ ] **Step 2: Record implementation outcomes in design and plan**

Change no requirement silently. If implementation evidence requires a design correction, state the correction and rationale explicitly.

- [ ] **Step 3: Close repository memory**

Record exact validation counts, warnings, residual risks, changed files, and the recommended next step. Keep Phase 33 and later phases provisional and unauthorized.

### Task 11: Run Required Validation And Completion Audit

**Files:**

- Inspect: all changed files and repository state

- [ ] **Step 1: Run focused Phase 32 and all existing RPC tests**

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~RpcHost
```

Expected: zero failures and zero skips; record the exact total.

- [ ] **Step 2: Run focused Phase 29–31 regression tests**

Use the existing Phase 29–31 filter inventory from their session records. Expected: zero failures and zero skips; record the exact total.

- [ ] **Step 3: Run the full backend suite**

```bash
dotnet test service-dotnet/tests/Tests.csproj -c Release
```

Expected: zero failures and zero skips; record the exact total.

- [ ] **Step 4: Run extension and webview Jest plus TypeScript compilation**

```bash
cd vscode-extension && npm test
cd vscode-extension && npm run compile
```

Expected: all Jest suites/tests pass and compilation exits zero; record exact totals.

- [ ] **Step 5: Run all eight pinned offline schema/boundary tests**

Run the exact existing filter used by the Phase 31 session and assert exactly eight discovered resources/tests with zero failures/skips.

- [ ] **Step 6: Run changed TypeScript/JavaScript scoped lint**

Derive changed TypeScript/JavaScript files from git diff and run ESLint only over that set. Expected: zero errors. If the set is empty, record zero changed files and zero scoped errors.

- [ ] **Step 7: Compare repository lint to b50d17d9**

Run repository lint in the active worktree and in a clean temporary worktree at b50d17d9. Normalize file, line, column, and rule tuples and compare them exactly. Expected: no new tuple; remove the temporary worktree afterward.

- [ ] **Step 8: Run document and scope gates**

Run placeholder, trailing-whitespace, roadmap-sequence, exact-route, forbidden-authority, production-boundary, changed-boundary, repository-output, and git diff --check assertions. Expected: every gate exits zero.

- [ ] **Step 9: Perform the requirement-by-requirement audit**

Map every requested behavior, test, document, non-goal, and validation gate to a test, source location, document statement, or fresh command result. Treat missing or indirect evidence as incomplete and continue work until resolved.

- [ ] **Step 10: Inspect final Git state without committing**

```bash
git status --short --branch
git diff --stat
git diff --check
```

Expected: requested branch, only scoped uncommitted Phase 32 changes, and no whitespace errors.

## Plan Self-Review

- Spec coverage: all framing, strict parsing, limits, concurrency, response serialization, cancellation states, duplicate ids, races, shutdown/disconnect, cleanup, diagnostics, compatibility, scope, documentation, and validation requirements map to explicit tasks.
- Placeholder scan: no unfinished markers, generic edge-case steps, or undefined follow-up steps remain.
- Type consistency: RpcTransportOptions, JsonRpcRequestParser, JsonRpcFraming, RpcResponseWriter, RpcRequestRegistry, SimpleJsonRpcServer, IRpcRequestHandler, and AnalyzerRpcDispatcher names and responsibilities are stable across tasks.
- Scope: the only application-aware file is AnalyzerRpcDispatcher, and it preserves the exact pre-Phase-32 route inventory.
- Git policy: no commit, push, merge, pull request, release, or discard step is present.
