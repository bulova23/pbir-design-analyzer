# Phase 35H Remote Boundary Threat Model

| Threat | Mitigation in Phase 35H | Residual risk |
|---|---|---|
| Fake client or worker | Ephemeral signed identities and bilateral verification | Harness keys are not production identity or certificate rotation |
| MITM or request tampering | Request/response signatures and canonical hashes | No real network confidentiality or mTLS proof |
| Replay or duplicate submit | Persisted execution identity and exact request-hash binding | Durable multi-node replay ledger is not implemented |
| Stale or substituted certification | Exact fixture certification/policy/profile binding | Production attestation and image certification are absent |
| Generic-command creep | Closed five-operation protocol and source boundary tests | Future APIs must preserve this exclusion |
| Worker compromise | Worker-side validation, bounded runner, quarantine | No OS-level Windows containment or image trust |
| Resource exhaustion | Finite duration, attempts, artifact, result, and concurrency policy | Hard CPU/memory/process enforcement is not exercised |
| Artifact substitution/leakage | Manifest/hash/session ownership and local Phase 35C scan | Production scanner and durable artifact store are absent |
| Credential theft | Only opaque grant metadata; forbidden value test | Real short-lived grant broker is absent |
| Audit divergence | Remote and local hash-chain correlation | Multi-worker durable audit replication is absent |
| Crash/cancellation abuse | Typed cancellation, timeout, uncertain restart state | Distributed worker recovery is not exercised |
