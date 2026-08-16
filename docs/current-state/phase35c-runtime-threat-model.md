# Phase 35C Runtime Threat Model

| Threat | Trust boundary | Phase 35C mitigation | Residual risk | Enforcement phase |
|---|---|---|---|---|
| Malicious provider package or compromised binary | Provider implementation → runtime | Explicit implementation identity, version, attestation, expiry, and policy binding | No signed package/binary inspection | Future package-attestation phase |
| Fake attestation or downgrade | Attestation metadata → trust evaluator | Exact identity/version/capability/mode and policy-version comparison; stale records deny | Attestation source is still a fixture | Provider certification |
| Stale trust reuse | Attestation → activation gate | Injected clock and expiration check | No external revocation feed | Provider operations |
| Credential exfiltration or audit leakage | Credential boundary → diagnostics/audit | Opaque grants only; secret-like values rejected; audit outcome is hashed/ignored | No real secret broker exists | Secret-provider integration |
| Sandbox escape or inherited host authority | Policy → provider runtime | Explicit isolated process, network, filesystem, environment, child-process, dependency, and quota policy | No OS sandbox enforcement | Actual sandbox phase |
| Malicious artifact or scanner bypass/failure | Provider result → intake | Identity/type/size/redaction stages, clean/suspicious/malformed/unsupported/failure/unknown outcomes, quarantine, and fail-closed unknown | Scanner is synthetic and no bytes are inspected | Scanner integration |
| Replay, nonce collision, retry confusion | Request/session identity → execution admission | Execution identity registry distinguishes duplicate, modified, stale, and authorized retry | Store is local/in-memory | Durable replay ledger |
| Audit mutation, deletion, truncation | Lifecycle → audit store | Append-only sequence and previous/current SHA-256 chain validation | External persistence and authenticated storage deferred | Durable audit deployment |
| Quota bypass/resource exhaustion | Provider → future runtime | Finite policy validation for duration, attempts, artifacts, results, memory, bytes, and concurrency | No OS/runtime enforcement | Actual provider runtime |
| Conformance-suite gaming | Adapter → certification | Closed conformance evidence includes cancellation, failure mapping, lineage, classification, audit, and secret-free diagnostics | Evidence is not yet produced by a real provider | Provider certification |
| Policy-version substitution | Policy evaluator → historical decision | Policy versions are explicit in trust and activation records | No immutable external policy registry | Governance deployment |
| Activation-gate bypass | Registry/readiness → provider execution | Production catalog has no executable adapter; gate requires every decision | Future integration must preserve the gate | First provider adapter |

Phase 35C therefore reduces design ambiguity and makes future trust decisions testable, but it does not claim that policy contracts alone contain an untrusted executable.
