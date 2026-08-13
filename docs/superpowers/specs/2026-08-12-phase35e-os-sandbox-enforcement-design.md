# Phase 35E — OS Sandbox Enforcement and Controlled Execution Containment

## Goal

Attempt to turn the Phase 35C sandbox policy into a real, fail-closed process boundary for a repository-owned deterministic fixture, without activating a production provider. The attempt did not establish a supported boundary on the observed target; Phase 35F records the containment-mechanism decision.

## Platform decision

The observed runtime is macOS `osx-arm64`, but the required custom Seatbelt profile is not considered supported: direct probes of `/usr/bin/sandbox-exec` on Darwin 27 abort for deny-default profiles. The adapter therefore reports process/filesystem/network/environment isolation unsupported and admission fails closed. Windows and Linux also report unsupported capability states. This is intentional evidence, not a portability claim. Memory, CPU, and process-count limits are unsupported; no process is admitted until a safe macOS mechanism is available.

## Architecture

Phase35E adds a focused sandbox boundary beside Phase35C and Phase35D:

```text
Phase35C/35D admission data
  -> Phase35ESandboxAdmission
  -> Phase35EIdentityVerifier + Phase35EPolicyBinder
  -> Phase35EMacSandboxAdapter
  -> Phase35ESandboxedProcessRunner
  -> Phase35ELifecycle/Evidence/Audit
```

The runner accepts only an immutable, already-bound execution specification, but production admission currently cannot produce one because the platform capability report is unsupported. It does not accept shell strings, arbitrary paths, arbitrary environment maps, or caller-selected commands. OS behavior is behind a narrow adapter; only the Phase35E boundary owns `System.Diagnostics.Process`.

The executable is bound to provider/version/implementation/package/certification identity and an exact path/hash mapping. The attempted macOS adapter launches `/usr/bin/sandbox-exec` directly with a generated Seatbelt profile and the approved executable as its child command, but this path is not authoritative after Darwin 27 probes failed. No shell, network client, credential store, provider discovery, or provider execution adapter is introduced.

## Controls

Requested Phase35C controls are classified as enforced, verified, unsupported, or not applicable. Admission fails when a required control is unsupported. The implementation models host timeout/cancellation, bounded stdout/stderr, unique working-directory ownership, and scoped cleanup, but no real process is admitted while the platform boundary is unsupported. The host records the failed capability proof and exact policy/evidence hashes.

## Evidence and audit

Every admitted session produces canonical, bounded evidence containing identity, profile, platform capabilities, requested/enforced/unsupported controls, environment/filesystem hashes, resource limits, outcome, termination, cleanup, and an evidence hash. Lifecycle events append to the existing Phase35C hash-chain audit store. Raw secrets and unbounded output are never authoritative evidence.

## Non-goals and residual risk

This phase does not execute a real provider or the fixture through the failing adapter, issue credentials, generate PBIR, automate Desktop, publish, mutate Fabric, or provide general command execution. `sandbox-exec` is a deprecated macOS interface and failed under the observed probes; real-provider admission remains disabled until a supported macOS OS boundary, signed executable deployment/launch strategy, and stronger platform assurance are separately established. Phase35F concludes that no acceptable local mechanism is currently proven.
