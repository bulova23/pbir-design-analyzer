# Phase 35H Replay, Audit, and Artifact Model

Execution ID, session ID, canonical request hash, certification identity, worker profile, and workload type form the replay/idempotency binding. A repeated identical submission returns the existing execution record. A changed request using the existing execution ID is rejected. Restart reloads terminal records and converts incomplete records to `Uncertain`; uncertain work is never automatically replayed.

Remote audit events use the Phase 35C hash-chain store and include request receipt, accepted validation, workload completion, cancellation/timeout, and result disposition. The local client adds a `remote-response` event containing the request hash and remote response/evidence hash. The remote record remains authoritative for remote lifecycle; the local record only proves intake and correlation.

Synthetic artifacts are candidate/quarantined on the worker. Their manifest contains artifact ID, kind, content hash, size, request/session lineage, certification ID, worker ID, and manifest hash. Retrieval requires the exact execution/session/artifact identity. The client validates the byte hash and routes the bounded offline fixture through the existing Phase 35C artifact safety pipeline before local acceptance.
