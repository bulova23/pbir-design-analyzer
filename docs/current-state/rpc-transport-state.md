# RPC Transport Current State

Date: 2026-08-03

Status: Repository Phase 32 implemented and validated as shared transport infrastructure

## Scope

Repository Phase 32 hardens the existing local stdio JSON-RPC host. It adds no application operation and carries no PBIR, materialization, provider, Microsoft Skills, extension UI, deployment, or publishing authority.

The extension still launches the same RpcHost child process and communicates over stdin and stdout with Language Server Protocol-style Content-Length framing and JSON-RPC 2.0 envelopes. AnalyzerRpcDispatcher contains only the pre-Phase-32 route inventory. Generic framing, parsing, response serialization, request registration, and lifecycle coordination remain independent of analyzer and PBIR application concepts.

## Production Limits

All transport limits are finite and validated when options are constructed.

| Limit | Production value |
| --- | ---: |
| Total header bytes | 8 KiB |
| One header line | 4 KiB |
| Header count | 16 |
| Request body | 8 MiB |
| Params payload | 7 MiB |
| Non-params envelope | 64 KiB |
| JSON depth | 64 |
| Method name | 256 UTF-8 bytes |
| String request id | 128 UTF-8 bytes |
| Response body | 16 MiB |
| Concurrent handlers | 8 |
| Registered requests | 64 |

Declared body size is checked before body allocation. Header, envelope, params, identifier, method, JSON-depth, registration, concurrency, and actual serialized response size limits are enforced at their owning boundary.

## Framing And Envelope Policy

The host accepts exactly one Content-Length header and an optional supported UTF-8 Content-Type header. Headers require CRLF framing. Missing, duplicate, unknown, malformed, overflowing, zero, truncated, or oversized framing is terminal because the next byte boundary is no longer trustworthy.

Complete frames are parsed with a strict Utf8JsonReader policy. The request must be one top-level object with these exact case-sensitive fields:

- jsonrpc, required once and equal to 2.0
- method, required once and a bounded nonempty string
- id, optional once and null, a bounded string, or an integral Int64 JSON number
- params, optional once and an object or array

Unknown and duplicate fields, unsupported versions, invalid identifiers, invalid UTF-8, malformed or truncated JSON, invalid params shapes, and trailing content are rejected. A well-framed invalid request receives a fixed protocol error when output remains available; framing faults stop intake and begin disconnect cleanup.

This is an intentional fail-closed compatibility policy. Every valid existing LanguageClient request remains accepted without a protocol-version change. Traffic that depended on undocumented permissive parsing is now rejected. Concurrent responses may complete out of request order and remain correlated by id as JSON-RPC requires.

## Concurrency, Cancellation, And Duplicate IDs

Independent requests execute concurrently up to the configured handler limit. One response writer owns header, body, and flush as a single critical section, so response frames cannot interleave. Response size is measured from bytes actually serialized; an oversized or serialization-faulting result is replaced with one bounded fixed Internal Error when output is still usable.

Validated string and numeric ids have distinct canonical keys. Raw ids never appear in diagnostics. Each active request owns one linked cancellation source and one terminal-outcome claim.

Cancellation behavior is explicit:

- before dispatch: cancellation claims the queued request, prevents handler entry, and produces Request Cancelled
- during execution: the handler token is cancelled; cancellation produces Request Cancelled only if cancellation wins the terminal claim
- after completion: cancellation is ignored because the registration has already been removed
- repeated cancellation: only the first eligible cancellation changes state
- completion race: one atomic terminal claim owns response authority; the loser cannot emit another frame

A duplicate active id deterministically cancels and suppresses the original request, rejects the duplicate with one Invalid Request response, and prevents either handler result from later acquiring response authority. An id may be reused only after its prior registration is removed and disposed.

## Shutdown And Disconnect

Shutdown has one idempotent owner. It stops intake and registration, disables new output when the connection is unusable, cancels all eligible requests, drains every tracked operation, removes and disposes every registration, and disposes the writer, semaphores, linked cancellation sources, server resources, and an owned handler exactly once.

The shutdown request retains the existing explicit null result when the output connection remains available. Exit, input EOF, terminal framing faults, output faults, and external cancellation use the same cleanup path. After RunAsync returns, active registrations and tracked tasks are zero.

Handlers are required to honor cancellation. The host does not abandon a non-cooperative handler; graceful completion waits for it so no background handler is orphaned. Consequently, a handler that never completes can delay shutdown indefinitely. Also, bytes already handed to the operating-system stream at the instant of a disconnect cannot be recalled; synchronous write disabling prevents later frames and the active write is allowed to resolve before cleanup.

## Diagnostics

Transport diagnostics contain fixed event codes, bounded lifecycle state, bounded counts, and a 16-hex-character SHA-256-derived correlation token for validated ids. They exclude request and response payloads, raw ids, peer-controlled method text, paths, artifact contents, exception messages, stack traces, and Phase 29–31 transaction internals.

## Error Policy

- Parse Error for malformed or trailing JSON in a complete frame
- Invalid Request for invalid envelopes, versions, fields, identifiers, params shapes, cancellation request form, or duplicate active ids
- Method Not Found for unknown application methods, preserving the existing behavior
- Request Cancelled when cancellation wins terminal ownership
- Server Busy when registration capacity is exhausted
- Internal Error for handler faults, response serialization faults, or oversized responses

All public error messages are fixed and contain no internal exception detail.

## Roadmap Boundary

Repository Phase 32 maps explicitly to RPC Transport Hardening and completes only the shared transport prerequisite. The local PBIR RPC adapter over Repository Phase 31 remains unimplemented and provisionally mapped to Repository Phase 33. Repository Phases 34–44 remain provisional planning only and are neither authorized nor implemented by this phase.

## Validation

- Phase 32 and existing RPC tests: 107 passed, 0 failed, 0 skipped
- Phase 29–31 changed-file regression inventory: 116 passed, 0 failed, 0 skipped
- full backend: 761 passed, 0 failed, 0 skipped
- pinned offline schema/boundary gate: 8 passed, 0 failed, 0 skipped
- extension Jest: 95 suites and 462 tests passed
- webview Jest: 10 suites and 65 tests passed
- standalone TypeScript compilation: passed

Lint-baseline and final repository gates are recorded in the Phase 32 session record.
