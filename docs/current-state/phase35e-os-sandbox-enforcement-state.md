# Phase 35E OS Sandbox Enforcement — Current State

Phase35E adds the first execution-boundary seam over the Phase35C policy and Phase35D identity/certification data. It does not establish an enforceable local boundary on the observed macOS target and does not register or execute a production provider.

## Platform and architecture

The observed platform is macOS `osx-arm64`, but the adapter reports the required custom Seatbelt boundary unsupported after direct Darwin 27 probes aborted for deny-default profiles. A current direct probe also returned exit 71 with `Operation not permitted`. `Phase35EMacSandboxAdapter` retains the narrow profile-generation seam only as historical/non-authoritative evidence; `Phase35EMacSandboxProcessBoundary` is not admitted for production use. `Phase35ESandboxedProcessRunner` is the only Phase35E production component that owns process lifecycle orchestration, and the unrestricted fallback boundary was removed in Phase35F. Other platforms fail admission with `SandboxNotSupported`.

## Enforcement matrix

| Phase35C control | Phase35E status | Evidence |
| --- | --- | --- |
| isolated process | unsupported; admission denied | custom Seatbelt deny-default probe aborts on observed runtime |
| no child processes | unsupported; admission denied | no safe proof on observed runtime |
| network denied | unsupported; admission denied | no safe proof on observed runtime |
| filesystem roots | unsupported; admission denied | no safe proof on observed runtime |
| environment allowlist | modeled, not admitted | no process launch while OS boundary is unsupported |
| working directory | verified in lifecycle model | unique host-created session directory |
| timeout/cancellation | modeled, not admitted | linked cancellation token and owned termination seam |
| stdout/stderr bounds | modeled, not admitted | bounded result classification |
| artifact count/bytes | policy-bound, fixture-only | admission rejects invalid finite policy; provider artifacts remain disabled |
| memory/CPU/process-count | unsupported | macOS adapter reports false; required controls deny admission |
| credentials | not applicable | no credentials are injected in Phase35E |

## Identity and evidence

`Phase35EExecutableIdentityVerifier` requires exact provider/version/implementation/package/certification identity, an absolute caller-independent executable mapping, and a fresh SHA-256 match before process creation. Evidence is canonical, bounded, and hash-addressed. The Phase35C durable audit store receives sandbox admission and cleanup lifecycle events.

## Remaining provider blockers

The production catalog remains disabled. Phase35F evaluates App Sandbox, Hardened Runtime, signed helpers/XPC, Virtualization.framework, container runtimes, and remote execution and selects no local macOS mechanism. A future provider still needs a supported OS boundary (highest risk), a signed deployed executable with a TOCTOU-safe launch strategy, real credential-grant integration, artifact scanning, provider-specific result validation, and stronger resource controls. macOS Seatbelt is deprecated and is not claimed as a container or kernel-grade isolation boundary.
