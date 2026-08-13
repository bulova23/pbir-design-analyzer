# Phase 35H Remote Boundary Proof — Current State

## Executive result

Phase 35H proves a typed `remote-execution/v1` domain boundary in a deterministic in-process transport harness. The proof includes ephemeral RSA request/response signatures, client and worker identity checks, independent worker revalidation, a persisted execution ledger, replay-safe idempotent submission, typed lifecycle/cancellation/timeout, bounded repository-owned inert workloads, remote quarantine, local hash and Phase 35C artifact-safety validation, and correlated hash-chain audit records.

This is contract and transport-semantics proof only. No Windows worker, network listener, mTLS connection, Windows Job Object, restricted token, AppContainer, Windows Sandbox, Hyper-V VM, or production image was exercised. The repository therefore does not claim Windows containment or production remote execution readiness.

## Boundary

```text
Local governed request
  -> signed typed envelope
  -> private in-process transport harness
  -> authenticated worker
  -> independent validation/certification/policy/replay checks
  -> closed inert runner
  -> remote quarantine + manifest
  -> client signature/hash/session validation
  -> Phase 35C safety pipeline
  -> local correlated audit
```

Allowed operations are `SubmitExecution`, `GetExecutionStatus`, `CancelExecution`, `FetchArtifactManifest`, and `FetchArtifact`. The protocol has no generic command, process, shell, script, upload, tool, MCP, Skills, path-read, proxy, provider, publication, or mutation operation.

## Evidence and remaining gap

The worker binds provider ID, provider version, implementation ID, certification ID, certification evidence hash shape, policy versions, worker profile, workload enum, credential-reference shape, replay identity, and finite resource policy before workload start. The fixture identity is explicitly `phase35h.inert-fixture`; it is not a production provider.

Phase 35I implements that narrow prerequisite through portable closed admission/evidence, a certified runner identity, one Windows-native Job Object/restricted-token boundary, and a repository-owned inert runner. The current macOS checkout can compile and test the portable layer but has not executed Windows integration evidence. The authoritative status remains `PartiallyProven`; network isolation, stronger filesystem isolation, image attestation, credentials, and provider execution remain deferred.
