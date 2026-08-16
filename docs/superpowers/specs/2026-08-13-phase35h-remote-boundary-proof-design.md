# Phase 35H Remote Boundary Proof Design

## Decision

Phase 35H adds a narrow, backend-only `remote-controlled-execution/v1` proof. The client and worker communicate through typed domain operations over an in-process transport harness. Each request is carried in a signed envelope; the worker verifies the client identity and the client verifies the worker identity. This proves contract and authentication semantics without misrepresenting a loopback harness as a deployed Windows service.

The worker owns a persisted execution ledger, replay ledger, and hash-chain audit. It independently validates protocol version, provider/certification identity, policy versions, worker profile, workload, credential-reference shape, replay identity, and finite resource policy before invoking a repository-owned inert runner. No generic process, shell, command, upload, network, or provider API exists in the protocol.

## Components

- `Phase35HContracts.cs`: closed enums and immutable request, envelope, lifecycle, result, manifest, audit, and failure records.
- `Phase35HAuthentication.cs`: ephemeral RSA signing/verification and explicit client/worker identity checks. Test keys are generated in memory and never persisted.
- `Phase35HWorker.cs`: authenticated operation boundary, independent validation, idempotent submission, lifecycle transitions, cancellation/timeout, persisted state, remote quarantine, and bounded artifact retrieval.
- `Phase35HInertRunner.cs`: a closed switch over `ReturnSuccess`, `ReturnDeterministicHash`, `CreateBoundedArtifact`, `WaitUntilCancelled`, `WaitUntilTimeout`, and `ReturnStructuredFailure`.
- `Phase35HClient.cs`: local request construction, signed transport calls, response verification, local manifest/hash validation, and local audit correlation.

The existing Phase 35C resource evaluator, credential-boundary policy, artifact-safety pipeline, and durable audit store remain the reusable lower-level primitives. Phase 35D certification identity is represented by exact provider/version/implementation/certification/evidence bindings; the fixture identity is explicitly a non-provider test certification.

## Allowed operations

The only domain operations are `SubmitExecution`, `GetExecutionStatus`, `CancelExecution`, `FetchArtifactManifest`, and `FetchArtifact`. There is no command, process, script, shell, executable upload, tool, MCP, or arbitrary path operation. Artifact retrieval accepts only an authenticated session-owned manifest artifact ID and returns exact bounded bytes.

## Proof boundary

The in-process harness proves typed contract behavior, signed identity semantics, independent worker validation, replay/idempotency, lifecycle, timeout/cancellation, bounded inert artifacts, quarantine, hash validation, audit correlation, and restart reconciliation. It does not prove Windows OS isolation, mTLS, private networking, worker-image authenticity, or production deployment. Windows Job Objects/restricted tokens or a stronger Windows VM boundary remain the next containment prerequisite.

## Failure behavior

Malformed, downgraded, unauthorized, replayed, mismatched, oversized, or tampered requests fail before workload start with structured failure codes. A repeated authoritative execution identity returns the existing state. A modified payload with the same identity is rejected. Unknown/incomplete persisted work is represented as `Uncertain` and is never automatically replayed. Completion wins only when a verified terminal record is available.

## Explicit exclusions

This phase adds no Power BI Desktop, PBIR generation/materialization, provider package or adapter, credentials, Skills, MCP, Fabric mutation/publication, arbitrary command, shell, arbitrary filesystem, HTTP proxy, or dynamic code execution path.
