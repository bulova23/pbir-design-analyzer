# Phase 35G Virtualization.framework Analysis

## Finding

Virtualization.framework is a credible future local boundary for a Linux-compatible provider, but it is not the selected architecture because it cannot host the likely Windows-dependent provider and would add a large native deployment surface to a VS Code extension.

Apple documents Linux and macOS guests on Apple silicon, requires the `com.apple.security.virtualization` entitlement, supports explicit CPU and memory sizing, optional network devices, explicit directory shares, and VM start/stop/save/restore lifecycle operations. Sources: [Virtualization framework](https://developer.apple.com/documentation/virtualization), [VM configuration](https://developer.apple.com/documentation/virtualization/vzvirtualmachineconfiguration), [Linux VM](https://developer.apple.com/documentation/virtualization/creating-and-running-a-linux-virtual-machine), [shared directories](https://developer.apple.com/documentation/virtualization/shared-directories), and [VM lifecycle](https://developer.apple.com/documentation/virtualization/vzvirtualmachine).

## Narrow design

```text
Signed macOS host helper
  ├─ Phase35B/C/D/E/F admission and audit
  ├─ VZVirtualMachineConfiguration: fixed CPU/RAM, no NIC, no share by default
  ├─ ephemeral arm64 Linux image bound by hash/signer/runner/policy
  └─ explicit bundle transfer → guest runner → manifest transfer → local intake
```

The guest should be Linux, not macOS, for a smaller image and fewer Apple-specific guest requirements. A host directory share is not the default; if used for a narrowly bounded transfer, input is read-only and output is a separate controlled share. A socket/serial transfer mechanism may be preferable, but its exact implementation remains unproven and belongs in a POC.

## Controls and gaps

- Filesystem: strong guest separation when no shares are configured; shared directories are an explicit residual escape surface.
- Network: omit `networkDevices` for offline work; any future cloud access requires a new policy and threat review. NAT would connect the guest to the host network and is not default-deny.
- Resources: CPU and memory are configurable; duration, disk growth, process count, and guest-level child-process policy still require guest/orchestrator enforcement.
- Lifecycle: start, pause, resume, stop, and save/restore exist. Destructive stop is available for timeout, but orphan recovery after host crash and disk cleanup require host bookkeeping.
- Trust: certification must bind provider package, runner version, guest image hash/provenance, patch level, and effective VM policy.
- Deployment: a signed native helper, virtualization entitlement, notarized distribution, guest image delivery/update channel, arm64 image support, local disk budget, and macOS integration tests are required.

## Disqualifier for this product direction

Apple’s documented framework supports macOS and Linux guests, not Windows guests. PBIR itself is cross-platform in the documented format, but Power BI Desktop is a Windows application. A Linux guest therefore cannot satisfy the likely Desktop verification/provider requirement. A macOS guest would not solve the Windows dependency and would increase image and licensing/support complexity.

## Residual risks

VM escape, helper compromise, guest-image compromise, shared-directory misuse, guest resource exhaustion, local host crash, and entitlement/signing distribution failures remain. Virtualization reduces the host attack surface but does not prove the provider is safe; image and runner certification remain mandatory.

