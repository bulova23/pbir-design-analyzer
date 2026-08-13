# Phase 35G — Containment Architecture Decision

## Decision

Select `remote-controlled-execution/v1` as the future containment boundary. The selected design is not implemented or enabled by Phase 35G. Local macOS provider execution remains **NotAdmitted** under the historical Phase 35F decision.

The remote target is a controlled private execution service with a Windows worker as the first-class platform for providers that require Power BI Desktop or other Windows-only components. Linux workers remain a later, explicitly certified specialization for providers proven to be Linux-compatible.

## Evidence baseline

Phase 35A–D provide versioned provider, policy, certification, activation, audit, replay, resource, credential, conformance, and artifact-safety contracts. Phase 35E adds a process-boundary seam. Phase 35F proves no acceptable local macOS process mechanism on the observed macOS 27/Darwin 27 arm64 target and selects `none-local-macos/v1` with admission closed. The current extension launches a packaged .NET backend; it is not an App Sandbox bundle and has no native VM host.

Apple’s Virtualization framework documents macOS and Linux guests, virtualization entitlement requirements, explicit CPU/memory/device configuration, optional network devices, explicit shared-directory devices, and VM lifecycle operations. It does not provide a Windows guest option. Microsoft documents Power BI Desktop as a Windows application and documents PBIR as a format that can be edited by non-Power BI applications. Therefore PBIR serialization alone does not force Windows, but the likely future Desktop verification/provider scope does.

## Required properties

The following are admission requirements, not scoring preferences:

| Property | Priority | Required boundary evidence |
| --- | --- | --- |
| Workload isolated from the developer workstation | Must Have | Independent guest/worker enforcement and negative tests |
| No implicit host filesystem or environment access | Must Have | Explicit input/output transfer only; no inherited environment |
| Default-deny network | Must Have | No interface by default; policy-controlled exception path |
| Exact certified provider identity | Must Have | Independent revalidation at execution authority |
| Signed certification binding | Must Have | Provider + runner + image/worker policy binding |
| CPU, memory, process, and duration limits | Must Have | Hard platform controls plus orchestrator watchdog |
| Child-process containment and cleanup | Must Have | Process-tree or VM/worker kill semantics tested |
| Scoped credential delivery | Must Have | Opaque, short-lived, session-bound grant; no secret persistence |
| Tamper-evident correlated audit | Must Have | Independent remote chain plus local correlation |
| Replay protection and deterministic ownership | Must Have | Idempotent request identity and duplicate arbitration |
| Artifact scanning and output validation | Must Have | Remote quarantine plus local Phase 35C intake |
| Recoverable timeout/cancellation/failure model | Must Have | Explicit state machine and reconciliation |
| Deployment integrity and supportability | Should Have | Signed images/binaries, patch policy, health evidence |
| Offline developer workflow | Should Have | Contract/fake-worker tests without provider execution |
| Multi-worker scale | Deferred | Add only after single-worker semantics are proven |

## Compared designs

### Local Virtualization.framework

Use a signed native macOS host helper to create a disposable arm64 Linux guest with no network device and no shared directory by default. Transfer a certified input bundle through a narrow host/guest channel, execute a certified runner, stage an output manifest, scan and validate on the host, then stop and destroy the guest state. Bind certification to provider package, runner version, guest image hash, and sandbox policy.

This is a credible local isolation direction for Linux-compatible providers. It is not a path to a Windows guest under Apple’s documented framework, and it introduces native Swift/Objective-C packaging, entitlement, signing, image distribution, guest lifecycle, and local resource-support burdens.

### Controlled remote execution

The local backend submits a typed, signed execution request over an authenticated private API. The service independently validates provider certification, policy, replay identity, credentials, and artifact rules before dispatching to a disposable isolated worker. The worker returns only a manifest-addressed artifact set. The local Phase 35C intake revalidates hashes, lineage, scan status, and request/session binding.

The first worker platform is Windows because the likely Desktop-dependent provider cannot run in the Apple guest direction. Linux remains a separate worker profile, not an implicit compatibility promise.

## Boundary and non-goals

Phase 35G adds only the architecture decision contract, tests, and evidence documentation. It does not add a provider, shell bridge, remote command endpoint, VM manager, worker service, credentials, scanner integration, PBIR generation, Desktop automation, publication, Fabric mutation, MCP, Skills execution, or production adapter.

## Phase 35H prerequisite

The smallest next phase is a remote boundary proof: authenticated private domain-level protocol, one disposable Windows worker profile, independent certification/policy/replay validation, correlated audit, cancellation/reconciliation, and artifact-transfer proof using inert test payloads only. Provider activation remains a later phase after the containment boundary itself is proven.

