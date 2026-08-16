# Phase 35F macOS Containment Decision — Current State

## Executive decision

No acceptable local macOS mechanism is proven on the current target. Phase 35F selects `none-local-macos/v1`; the outcome is `NotAdmitted` with `PlatformContainmentUnavailable`. No provider or repository fixture executed.

The observed platform is macOS 27.0, Darwin 27.0.0, Apple silicon arm64, .NET runtime 10.0.11 with the project targeting .NET 8 and RID `osx-arm64`. `sandbox-exec` is present, but it is not an authoritative mechanism: the prior Phase35E custom deny-default profiles aborted with exit 134/137, and the current direct deny-default probe returned exit 71 with `Operation not permitted`. The profile text existing in Phase35E is therefore evidence of attempted policy construction only.

## Decision matrix

| Mechanism | Supported here | Filesystem | Network | Child processes | Resources | Identity | Deployment | Result |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| App Sandbox | Only in signed app/helper bundle | Enforced by static entitlements/container, not dynamically attached to current backend | Entitlement-dependent, not proven for full policy | Inheritance, not denial | Required hard memory/CPU/process limits not proven | Signing helps; no atomic hash-to-launch proof | Native bundle/signing required | Not selected |
| Hardened Runtime / signing | Yes for signed code | Unsupported | Unsupported | Unsupported | Unsupported | Partially enforced integrity | Signing/notarization | Not a sandbox |
| Signed sandbox helper | Feasible only with App Sandbox packaging | Not proven in this product | Not proven in this product | Not proven as denial | Not proven | Partially enforced | High | Not selected |
| XPC/helper constraints | Supported Apple facility | Unsupported by itself | Unsupported by itself | Unsupported by itself | Unsupported by itself | Caller binding only | High | Not selected |
| Virtualization.framework | Supported with native signed host/entitlement | Guest-enforced if shares are absent/bounded | Guest-enforced if network device is absent/bounded | Guest boundary | Guest/VM-configured | Strong image binding possible | Very high | Future option |
| Container runtime | Possible through external VM-backed runtime | Runtime-enforced if configured | Runtime-enforced if configured | Runtime-enforced if configured | Runtime-enforced if configured | Runtime/image dependent | External dependency | Not selected |
| Remote service | Separate architecture | Service-enforced | Service-enforced | Service-enforced | Service-enforced | Service attestation required | Product/operations change | Future fallback |

## Enforcement matrix

| Required control | Selected mechanism | State | Proof/current limitation |
| --- | --- | --- | --- |
| Filesystem read restriction | none-local-macos/v1 | Unsupported | No proven OS boundary restricts the workload to input/work/output roots. |
| Filesystem write restriction | none-local-macos/v1 | Unsupported | No proven OS boundary restricts writes to session roots. |
| Network denial | none-local-macos/v1 | Unsupported | No proof covers loopback, DNS, outbound TCP/UDP, or Unix sockets. |
| Child-process restriction | none-local-macos/v1 | Unsupported | App Sandbox inheritance is not denial; helper/XPC constraints do not supply this control. |
| Environment isolation | none-local-macos/v1 | Unsupported | Host allowlisting is a preparation step, not OS isolation. |
| Process identity binding | Phase35D + Phase35E | PartiallyEnforced | Exact pre-launch identity/hash checks exist; launch-time atomic identity binding remains unresolved. |
| Memory limit | none-local-macos/v1 | Unsupported | No proven hard limit. |
| CPU limit | none-local-macos/v1 | Unsupported | No proven hard limit. |
| Execution timeout | Phase35E host lifecycle | PartiallyEnforced | Host cancellation/termination is not containment proof. |
| Process-count limit | none-local-macos/v1 | Unsupported | No proven hard limit. |
| Secure termination | Phase35E host lifecycle | PartiallyEnforced | No admitted workload exists to prove orphan resistance. |
| Cleanup isolation | Phase35E session lifecycle | PartiallyEnforced | Scoped directory cleanup is not OS isolation. |
| Stdout/stderr bounds | Phase35E runner | PartiallyEnforced | Capture bounds are post-launch host behavior, not native resource limits. |

Only `Enforced` satisfies admission. The Phase35F selector returns a deterministic platform/capability evidence hash and never invokes a process boundary. The sanitized representative shape is:

```json
{
  "mechanism": "none-local-macos/v1",
  "platform": {"os": "macOS", "version": "27.0", "darwin": "27.0.0", "architecture": "Arm64", "runtime": "osx-arm64"},
  "admitted": false,
  "failure": "PlatformContainmentUnavailable",
  "capabilities": "per-control states and proof strings",
  "evidenceHash": "3c9ea5bb116357d99456971a81772b8390d897bdc1795989ccecbc27b7aca7ca"
}
```

## Deployment and CI

The current VS Code extension launches a packaged .NET backend, not a signed App Sandbox app bundle. App Sandbox therefore cannot be introduced as a dynamic opt-in around the existing backend. A future VM or remote service must add explicit deployment artifacts and separate macOS integration tests. Portable contract tests must never be treated as OS-enforcement tests; macOS signing/entitlement/VM tests remain deployment gates.

## Remaining blockers and Phase 35G

Controlled provider execution remains blocked by the absence of a proven local containment boundary, real credential issuance, production artifact scanning, provider-specific execution/result validation, and a TOCTOU-safe deployed executable binding. Phase 35G must not activate a provider. The strongest next architecture is a design-only comparison of Virtualization.framework guest execution and a controlled Windows/Linux remote execution service, with platform restriction and fail-closed admission retained meanwhile.
