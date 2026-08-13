# Phase 35H Windows Worker Containment Analysis

Phase 35H intentionally did not claim a Windows worker proof. Phase 35I now contains the narrow implementation-ready boundary, but the current checkout is macOS and exercises only portable contracts and boundary scans. The authoritative Phase 35I status remains `PartiallyProven`.

| Mechanism | Phase 35H status | Required future evidence |
|---|---|---|
| Windows Job Object | Not exercised | process-tree membership, no-breakaway, CPU/time/process limits |
| Restricted token | Not exercised | denied privilege/token capability tests |
| AppContainer | Not exercised | package/profile/network/filesystem capability tests |
| Windows Sandbox | Not exercised | disposable image, artifact channel, patch/identity binding |
| Hyper-V or isolated VM | Not exercised | image attestation, host-share absence, lifecycle/recovery |
| Windows container | Not exercised | image provenance, Desktop compatibility, process/resource controls |

The narrowest meaningful Phase 35I is one disposable Windows process boundary using Job Objects plus a restricted token, provided it can be certified with no-breakaway, explicit output directories, deny-by-default network, allowlisted environment, and bounded output. If the eventual provider requires stronger kernel/image isolation, the proof must be upgraded to a disposable VM rather than treating a process boundary as a sandbox.
