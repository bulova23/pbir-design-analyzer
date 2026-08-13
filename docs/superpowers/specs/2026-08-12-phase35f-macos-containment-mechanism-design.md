# Phase 35F — macOS Containment Mechanism Evaluation and Enforceable Sandbox Selection

## Decision

No acceptable local macOS containment mechanism is proven for the current target. Phase 35F selects `none-local-macos/v1`, reports `PlatformContainmentUnavailable`, and keeps provider and fixture admission closed.

The current target is macOS 27.0, Darwin 27.0.0, arm64, running the repository on `osx-arm64` with .NET 8 available and the .NET 10 SDK/host selected. The existing custom Seatbelt path is retained only as historical evidence and a non-authoritative probe seam. Its direct deny-default probes have failed on this target, including the prior aborting Phase 35E probes and the current direct probe that returned exit 71 with `Operation not permitted`.

## Candidate matrix

| Candidate | Current target support | Filesystem isolation | Network denial | Child-process control | Resource limits | Identity/TOCTOU | Deployment | Decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Apple App Sandbox | Supported only for a signed app/helper bundle with entitlements | Enforced within the static container/entitlement model; not dynamically attachable to the existing .NET backend | Enforced only according to the app’s network entitlements; no proof for the required complete policy in this repository | Partially enforced: child tools inherit the static sandbox; this is not child creation denial | Unsupported for the required memory/CPU/process-count controls | Code signing helps identity; no atomic provider hash-to-launch binding | High: native app bundle, signing, entitlements, helper packaging | Not selected |
| Hardened Runtime / code signing | Supported for signed macOS executables | Unsupported | Unsupported | Unsupported | Unsupported | Partially enforced for integrity and injection resistance | Medium/high signing and notarization | Not a sandbox |
| Sandboxed helper host | Feasible only as a signed App Sandbox bundle; not proven with the current VS Code/.NET packaging | Partially enforced until the complete bundle/entitlement proof exists | Partially enforced until entitlement proof covers all required paths | Unsupported for denial; inherited children remain a risk | Unsupported | Partially enforced | High | Not selected |
| XPC/service/helper launch constraints | Supported Apple IPC/signing facility | Unsupported by itself | Unsupported by itself | Unsupported by itself | Unsupported by itself | Partially enforced: caller identity can be constrained | High and requires native packaging | Not selected |
| Virtualization.framework | Supported on Apple silicon with a signed native host and virtualization entitlement | Enforced by guest boundary if shared volumes are absent or tightly bounded | Enforced by guest device configuration if no network device is exposed; requires guest proof | Enforced inside the guest only after guest image hardening | Enforced/configurable in the VM architecture, subject to guest proof | Stronger package/image binding is possible | Very high: native host, guest image, entitlement, lifecycle, artifact transfer | Future local option |
| Container runtime | Possible through a VM-backed third-party runtime | Enforced by runtime configuration if correctly isolated | Enforced by runtime configuration if correctly isolated | Enforced/configurable by runtime/guest | Enforced/configurable by runtime | Depends on image/runtime integrity | High external dependency and support burden | Not selected |
| Remote isolated execution | Supported as a separate controlled service architecture | Enforced by service boundary | Enforced by service network policy | Enforced by service boundary | Enforced by service policy | Strong if service attestation/binding is implemented | High product/operations change | Future preferred fallback if local macOS remains required |

The values above are evidence classifications, not marketing ratings. A candidate is admissible only when every required control is `Enforced` by the actual deployment and test environment.

## Architecture

```text
Phase35C/35D assurance and certified identity
        ↓
Phase35E runtime boundary and lifecycle contracts
        ↓
Phase35F containment selector
        ↓
none-local-macos/v1 → NotAdmitted
```

Phase35F adds no generic process runner, helper protocol, shell bridge, provider adapter, credential path, or fixture execution. `Phase35FContainmentSelector` produces exact platform evidence, an explicit per-control capability report, and a deterministic evidence hash. It is a decision/evidence component behind the Phase35E boundary, not a replacement for Phase35E lifecycle or identity contracts.

## Required control rule

`Enforced` is the only state that can satisfy a required control. `PartiallyEnforced`, `Unknown`, and `Unsupported` are non-admitting states. On the current target, filesystem read/write, network denial, child-process denial, environment isolation, memory, CPU, and process-count controls are unsupported. Identity binding, timeout, termination, cleanup, and output bounds remain host-side or pre-launch partial controls and cannot establish containment.

## Non-goals

This phase does not execute the repository fixture, activate a provider, retrieve secrets, use MCP or Microsoft Skills, generate PBIR, automate Power BI Desktop, publish, mutate Fabric, add a container dependency, or implement a VM. A future VM or remote-execution phase must first define its signed host, image/runtime identity, artifact transfer, networking, resource, and teardown contracts.

## Evidence basis

Apple documents App Sandbox as a signed entitlement-driven app boundary and documents sandbox inheritance for embedded command-line tools; it does not provide a dynamic API that turns the existing backend process into a newly bounded helper. Apple documents Hardened Runtime as runtime integrity and exploit-surface protection, not filesystem or network isolation. Apple documents Virtualization.framework as a VM boundary requiring a native host, guest configuration, and virtualization entitlement. These distinctions drive the selection.
