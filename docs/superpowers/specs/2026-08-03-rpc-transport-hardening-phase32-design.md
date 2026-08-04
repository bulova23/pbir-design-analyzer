# Repository Phase 32 — RPC Transport Hardening Design

Date: 2026-08-03

Status: Implemented and validated for Repository Phase 32 on 2026-08-03; changes remain uncommitted

Implementation outcome: the focused extraction described here was delivered without a protocol-version change or a new route. Response bounding measures bytes actually emitted rather than relying on serializer buffer hints, cancellation parsing retains only the bounded raw params slice needed to validate request identity, and disconnect handling disables new writes synchronously before cancelling handlers. These implementation refinements preserve the design guarantees while closing allocation and completion-race gaps found by the red/green tests.

## Goal

Harden the existing local stdio JSON-RPC transport so future application operations can rely on strict bounded input, concurrent request execution, deterministic cancellation, serialized responses, and complete shutdown cleanup without adding any application operation or execution authority.

Repository Phase 32 is shared transport infrastructure. It is the first explicitly mapped prerequisite toward completing the remaining original Phase 4–7 roadmap, but it is not itself an original Phase 4 feature.

## Roadmap Boundary

Repository Phase 32 maps only to RPC Transport Hardening.

The following sequence is provisional planning and creates no implementation authority:

- Repository Phase 33: local PBIR RPC adapter over Repository Phase 31
- Repository Phase 34: VS Code local PBIR materialization workflow (separately authorized consumer of Phase 33)
- Repository Phase 35: Microsoft PBIR runtime-provider and Skills execution
- Repository Phase 36: generated PBIP/PBIR intake, quarantine, and validation
- Repository Phases 37–38: original Phase 5 Analyzer handoff
- Repository Phases 39–40: original Phase 6 refinement loop
- Repository Phases 41–43: original Phase 7 Fabric target mapping, generation, and review intake
- Repository Phase 44: release hardening, packaging, and publishing

No item in that sequence is authorized by Phase 32.

## Evidence And Current Compatibility Contract

The shipped extension starts RpcHost as a local child process and uses VS Code LanguageClient over stdin and stdout. The current wire contract is:

- LSP-style framing with an ASCII Content-Length header, CRLF header terminator, and a UTF-8 JSON body
- JSON-RPC protocol version 2.0
- request identifiers represented by JSON strings or numbers
- notifications represented by an omitted or null identifier
- existing initialize, lifecycle-notification, shutdown, exit, model/ping, model/pbir/getTree, model/pbir/scoreReport, and model/pbir/governanceCheck methods
- camel-case success and error responses
- a shutdown success response that includes an explicit null result

All valid traffic emitted by the existing LanguageClient remains accepted. Previous permissive acceptance of malformed framing, invalid identifiers, wrong-case envelope names, duplicate fields, unknown fields, unsupported versions, and trailing JSON is not a compatibility contract.

## Alternatives Considered

### Harden Program.cs In Place

This minimizes the number of files but leaves framing, parsing, dispatch, response writing, lifecycle state, and analyzer behavior coupled in one class. Concurrency tests would depend on private implementation details and future operations would make the file harder to maintain.

### Replace The Host With A Third-Party RPC Stack

A library could supply established protocol behavior, but changing the active stack would increase packaging and compatibility risk, obscure the repository-specific fail-closed policy, and risk creating a parallel transport during migration.

### Focused Extraction Inside The Existing RpcHost

This is the selected approach. It keeps one stdio transport and the existing dispatcher while separating generic protocol responsibilities into small internal units. It adds no production package dependency and gives deterministic tests direct access to framing and lifecycle seams.

## Architecture

### Composition Root

Program remains the executable entrypoint. It constructs the existing analyzer services, wraps them in the existing-method dispatcher, creates production transport options, and runs one SimpleJsonRpcServer.

### Existing-Method Dispatcher

AnalyzerRpcDispatcher owns the existing method switch and response payload construction. It implements a generic request-handler interface that receives a validated request and CancellationToken. It adds cancellation checkpoints around existing operations but does not add, rename, or change any route or Phase 29–31 service dependency.

### Framing Reader

JsonRpcFraming reads one frame at a time. It bounds header accumulation before allocating the declared body, accepts exactly one Content-Length header and at most one optional Content-Type header, and rejects all other or duplicate headers. It reads the exact declared UTF-8 byte count.

A framing error is terminal because the next byte boundary cannot be trusted. Oversized, malformed, or truncated frames stop intake and begin disconnect cleanup. The host never attempts byte scanning or resynchronization.

### Strict Envelope Parser

JsonRpcRequestParser uses Utf8JsonReader with a finite depth. It validates the top-level object without permissive object deserialization and permits only these exact case-sensitive fields:

- jsonrpc, required exactly once and equal to 2.0
- method, required exactly once and a bounded nonempty string
- id, optional at most once and either null, a bounded string, or an integral Int64 JSON number
- params, optional at most once and an object or array

Duplicate or unknown fields are rejected. Trailing tokens, invalid UTF-8, malformed JSON, and truncated JSON are rejected. The parser measures the raw params slice and non-params envelope bytes while reading so both limits are enforced without serializing JsonElement values or allocating avoidable strings.

An omitted or null id remains a notification for compatibility. It never creates a cancellable registration. Boolean, object, array, fractional, exponent-form, out-of-range, empty-string, control-character, and oversized identifiers are invalid.

### Request Identity

RpcRequestId preserves the JSON type and value. A canonical registry key includes the type so numeric 1 and string "1" remain distinct. Diagnostics never expose the raw value; a SHA-256-derived fixed-length correlation token is used instead.

### Request Registry And Scheduler

RpcRequestRegistry owns every request from acceptance through terminal completion. Registration occurs before a task waits for a dispatch slot, which makes queued requests cancellable before handler invocation.

The registry provides one linearization point for these terminal outcomes:

- handler completion
- handler fault
- request cancellation
- duplicate active identifier
- connection shutdown

At most one outcome may claim response authority. Completion then removes and disposes the registration exactly once.

The server permits a finite number of registered requests and a smaller finite number of executing handlers. Capacity rejection does not create an untracked task.

### Duplicate Identifiers

If a second request arrives while the same typed identifier is active, the registry cancels the original request and atomically claims the identifier for one Invalid Request error. The duplicate is not dispatched, and any later completion from the original handler is suppressed. This avoids two responses carrying the same ambiguous identifier.

An identifier may be reused after its prior registration is fully removed.

### Cancellation

The only cancellation notification is the standard $/cancelRequest method. Its params must be an object containing exactly one id field whose value passes the same identity validation as a request id.

- Before registration: ignored; no future tombstone is created.
- After registration but before dispatch: cancellation claims the terminal outcome and the handler is never invoked.
- During execution: the per-request token is cancelled. If cancellation claims terminal state first, one Request Cancelled error is written and later handler output is suppressed.
- After completion: ignored.
- Repeated cancellation: idempotent.
- Completion race: whichever terminal action acquires the registry transition first owns the only response.

The cancellation error code is -32800. Cancellation notifications never receive responses.

### Response Writer

RpcResponseWriter serializes a complete response into one bounded byte buffer, then holds one SemaphoreSlim while writing the header, body, and flush. Concurrent completions can be out of order, but their frames cannot interleave.

If serialization exceeds the response limit, the writer substitutes a bounded Internal Error response when possible. If the output is disconnected or shutdown has disabled writes, the response is suppressed and recorded only through safe diagnostics.

### Shutdown And Disconnect

The first shutdown, exit, terminal framing fault, external cancellation, or input EOF initiates shutdown exactly once.

- intake stops
- new registration fails closed
- active and queued request tokens are cancelled
- queued requests never enter the handler
- tracked handlers are awaited
- eligible shutdown or cancellation responses are written only while the connection remains writable
- registrations, cancellation sources, scheduling slots, writer state, and owned resources are disposed once
- RunAsync does not return while a tracked request task remains

A valid shutdown request receives one explicit null result after other eligible work has resolved. An exit notification and an input disconnect produce no response. Repeated shutdown signals join the same completion task.

.NET cannot forcibly terminate arbitrary code that ignores cancellation forever. The dispatcher contract therefore requires handlers to honor cancellation. Existing analyzer methods receive checkpoints before and after their existing work; this phase does not rewrite those services. A non-cooperative future handler is a residual extension risk and must not be registered without lifecycle tests.

## Production Defaults

All limits are immutable positive values validated at construction:

| Limit | Default |
| --- | ---: |
| Total header bytes | 8 KiB |
| Header line bytes | 4 KiB |
| Header count | 16 |
| Request body bytes | 8 MiB |
| Params payload bytes | 7 MiB |
| Non-params envelope bytes | 64 KiB |
| JSON depth | 64 |
| Method UTF-8 bytes | 256 |
| String request-id UTF-8 bytes | 128 |
| Response body bytes | 16 MiB |
| Concurrent handlers | 8 |
| Registered requests | 64 |

Boundary values equal to a limit are accepted. The next byte, item, or registration is rejected.

## Error And Connection Policy

| Condition | Behavior |
| --- | --- |
| Malformed or duplicate framing header | Terminal framing fault; stop intake |
| Missing or invalid Content-Length | Terminal framing fault; stop intake |
| Oversized declared body | Reject before body allocation; stop intake |
| Truncated body | Disconnect cleanup; no resynchronization |
| Malformed or trailing JSON in a complete frame | Parse Error with null id, then continue |
| Invalid envelope, version, field, method, params shape, or id | Invalid Request; use a validated id only when unambiguous |
| Unknown application method | Existing Method Not Found behavior |
| Handler cancellation | Request Cancelled if cancellation wins |
| Handler fault | Fixed Internal Error; no exception text |
| Capacity exceeded | Fixed Server Busy error; no dispatch |
| Output disconnect | Suppress further writes and shut down |

Well-framed invalid JSON can be rejected without losing the next frame boundary. Framing failures cannot.

## Diagnostics

Transport logs contain only:

- fixed event code
- bounded lifecycle state
- hashed correlation token when a validated id exists
- bounded counters such as active and queued request counts

They never contain request or response payloads, raw ids, methods supplied by an invalid peer, paths, artifact contents, exception messages, stack traces, Phase 29–31 transaction identifiers, or service-internal details. Handler faults are logged as a fixed classification.

## Test Strategy

Tests use MemoryStream, deliberately chunked streams, blocking streams, TaskCompletionSource gates, injectable request handlers, and injectable dispatch admission gates. No concurrency assertion relies on Thread.Sleep or wall-clock ordering.

Focused tests cover:

- every existing valid request shape and explicit-null shutdown response
- ASCII and multibyte UTF-8 byte framing
- malformed, truncated, oversized, duplicate-header, unknown-header, and trailing input
- missing, duplicate, unknown, wrong-case, invalid-type, and oversized envelope fields
- supported and unsupported versions
- string and integral numeric ids plus invalid ids
- configured limit values and one-unit-over boundaries
- duplicate active ids and safe reuse after cleanup
- queued, executing, completed, repeated, and racing cancellation
- independent concurrent dispatch and out-of-order completion
- non-interleaved response frames under partial writes
- zero, one, and multiple in-flight shutdown/disconnect cases
- slow, blocked, cancelled, and faulting handlers
- shutdown idempotency and resource cleanup
- fixed redacted diagnostics
- scope tests proving no Phase 31 adapter, PBIR operation, provider invocation, Skills execution, or UI authority was added

Full validation retains all existing RPC, backend, extension, webview, TypeScript, offline-schema, lint-baseline, document, scope, repository-output, whitespace, and diff gates.

## Compatibility Impact

No protocol-version change is required. Valid existing LanguageClient consumers retain the same framing, methods, request shapes, response shapes, notification behavior, and JSON-RPC version.

Intentional hardening affects only traffic that is invalid, ambiguous, oversized, unsupported, or dependent on undocumented permissive parsing. Concurrent requests may now complete out of order, which JSON-RPC clients must already correlate by id.

## Explicit Non-Goals

Phase 32 adds none of the following:

- a local PBIR RPC adapter or new PBIR route
- a Phase 31 transport operation
- a VS Code command, view, dialog, notification, or webview
- Microsoft PBIR provider or Skills execution
- external process, CLI, API, or network invocation
- PBIP generation or generated-artifact intake
- Desktop or Analyzer automation
- deployment, publishing, authentication, authorization, encryption, or remote transport
- Phase 29–31 behavioral changes
- release, packaging, versioning, commit, push, or merge work

## Self-Review

- Placeholder scan: no incomplete requirements or deferred implementation markers.
- Consistency: framing, parsing, cancellation, duplicate-id, completion-race, and shutdown rules use one request registry and one response writer.
- Scope: every production component remains generic except the existing-method dispatcher, which preserves current routes only.
- Compatibility: current valid LanguageClient framing and messages remain accepted without a protocol bump.
- Testability: every race-sensitive transition has an injectable or task-controlled seam.
