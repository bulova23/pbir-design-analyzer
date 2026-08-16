# ADR: Select Controlled Remote Execution for Future Providers

## Status

Accepted for future design; not implemented and not enabled.

## Context

Phase 35F rejected local macOS process containment as authoritative. Apple Virtualization.framework can provide a new local guest boundary, but its documented guest choices are macOS and Linux. The repository’s likely future provider scope includes Power BI Desktop verification, and Microsoft documents Power BI Desktop as Windows-only. The repository also already has provider governance and artifact-intake boundaries that can carry a remote execution authority without changing scoring or deterministic report mutation.

## Decision

Use `remote-controlled-execution/v1` as the future containment boundary, with Windows as the first worker profile and Linux as a separate certified profile. Keep local macOS process execution `NotAdmitted`.

## Consequences

Positive: supports Windows-dependent providers, separates untrusted work from the developer workstation, allows independent worker/VM enforcement, and creates a path to horizontal scale.

Negative: adds service identity, private networking, worker patching, queueing, availability, audit, artifact storage, and operational support. The remote API must be a domain protocol, never a generic command runner.

## Rejected alternative

`local-virtualization/v1` remains a credible fallback for a Linux-only provider, but is not selected because it cannot satisfy the mandatory Windows provider requirement and would add a large signed native/guest distribution surface to the extension.

## Required invariants

The remote authority independently revalidates certification, policy, replay, credentials, resources, and artifacts. Only manifest-addressed, scanned, lineage-bound artifacts may cross back into the local Phase 35C intake. No provider activation occurs until Phase 35H proves the boundary.

