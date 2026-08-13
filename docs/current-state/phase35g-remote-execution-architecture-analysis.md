# Phase 35G Controlled Remote Execution Analysis

## Finding

Controlled remote execution is selected as the future boundary. It creates a second enforcement authority, which is a substantial operational cost, but it can host both a Windows provider and a Linux-compatible provider without weakening local macOS admission.

## Narrow design

```text
Local backend
  └─ Phase35B/C/D/E/F + signed request
       ↓ mTLS/workload identity, private API
Remote execution authority
  ├─ revalidates provider/certification/policy/replay/credentials
  ├─ queues one domain-level execution request
  └─ starts disposable isolated worker
       ├─ Windows worker: Desktop-dependent provider profile
       └─ Linux worker: only explicitly certified cross-platform profile
             ↓ manifest + quarantine + scanner
Local Phase35C artifact intake and audit correlation
```

The API exposes submission, status, cancellation, manifest retrieval, and approved-artifact retrieval. It never accepts command text, shell text, arbitrary executables, or a caller-supplied approval boolean.

## Windows and Linux

Windows is the primary platform because Microsoft documents Power BI Desktop as a Windows application and recommends a client version of Windows rather than Windows Server. Windows AppContainer can restrict files, registry, network, credentials, and processes through capabilities; Windows Job Objects can group child processes, enforce resource/time limits, and terminate the tree on handle close. These mechanisms still require a dedicated worker/VM, exact token construction, no breakaway, and negative conformance tests. Sources: [Power BI Desktop requirements](https://learn.microsoft.com/en-us/power-bi/fundamentals/desktop-get-the-desktop), [AppContainer isolation](https://learn.microsoft.com/en-us/windows/win32/secauthz/appcontainer-isolation), [Job Objects](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects), and [Windows Sandbox](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/).

Linux is attractive for deterministic PBIR-only providers. A hardened disposable VM is preferred over treating a generic container as a complete boundary. If containers are used, namespaces, cgroup v2 controllers, seccomp, dropped capabilities, read-only roots, and a non-root worker must be proven together. Linux kernel documentation confirms cgroup v2 resource controllers and namespace visibility controls; Docker documents seccomp as a syscall-restriction mechanism. Sources: [cgroup v2](https://docs.kernel.org/admin-guide/cgroup-v2.html) and [Docker seccomp](https://docs.docker.com/engine/security/seccomp/).

## Remote trust boundary

The remote authority independently checks request hash, session ownership, exact provider/certification identity, runner/image identity, policy version, finite limits, credential grant reference, artifact policy, and replay identity. It records an independent tamper-evident audit chain. The local backend accepts an artifact only when request, session, remote execution, certification, manifest, hashes, scanner result, and lineage agree.

## Operational cost and failure model

New infrastructure includes private networking, service identity, mTLS or equivalent workload authentication, worker images, patching, queueing, monitoring, artifact storage/quarantine, audit retention, and availability/recovery operations. The service must reconcile ambiguous submission acknowledgements by idempotency key and never resubmit an execution whose ownership is unknown. Cancellation is advisory until remote acknowledgement; timeout closes the local request only after a reconciliation record is written.

## Residual risks

Service impersonation, worker compromise, cross-tenant leakage, stolen workload credentials, API replay, artifact substitution, remote privilege escalation, and service outage remain. Independent remote validation, worker isolation, defense-in-depth scanning, short-lived grants, and correlated audit reduce but do not eliminate these risks.

