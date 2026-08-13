# Phase 35G Containment Threat Model

| Threat | Virtualization.framework residual | Remote controlled mitigation | Status |
| --- | --- | --- | --- |
| VM/worker escape | Guest kernel, hypervisor, or worker boundary vulnerability | Disposable worker/VM, patching, least privilege, conformance | Deferred to 35H |
| Host filesystem exposure | Shared directory or transfer helper mistake | No host share; remote has no developer filesystem | VM design risk; remote advantage |
| Network exfiltration | NIC/NAT misconfiguration | Private API only; worker egress deny-by-default | Must prove |
| Image/runner compromise | Malicious or stale guest image | Hash/signer/patch binding and independent validation | Must prove |
| Provider replacement | Package/runner substitution | Remote revalidates exact certification and image/runner identity | Must prove |
| Child-process escape | Guest/worker process-tree escape or breakaway | Windows Job Object/no-breakaway or Linux cgroup/pid controls | Must prove |
| Resource exhaustion | Guest/host contention or disk growth | Hard worker quotas plus watchdog and queue limits | Must prove |
| Credential theft | Secret in image, mount, environment, or logs | Short-lived scoped grant and no secret audit contents | Design only |
| Replay/duplicate execution | Saved VM state or retry ambiguity | Request idempotency, remote replay ledger, reconciliation | Must prove |
| Artifact substitution | Guest/worker or transport swaps output | Manifest hashes, remote quarantine, local re-scan and lineage check | Must prove |
| Audit tampering | Local or guest audit loss | Independent remote chain correlated with local chain | Must prove |
| Service impersonation/MITM | Host helper/API trust misconfiguration | mTLS/workload identity, certificate rotation, private endpoint | Design only |
| Local/remote outage | Host crash or service unavailable | Explicit pending/unknown states, no duplicate retry, recovery | Design only |

The highest-risk unresolved items are independent platform enforcement, exact runner/image certification, credential grant implementation, production scanning, and durable replay/reconciliation. Phase 35G does not claim any of these are proven.

