# Phase 35F macOS Containment Decision — Threat Model

| Threat | Evidence and boundary | Residual risk | Status |
| --- | --- | --- | --- |
| Seatbelt downgrade or abort | Seatbelt is not selected; direct probes are recorded as unsupported | A future caller could accidentally re-enable the old adapter | Fail closed; boundary scan required |
| App Sandbox mistaken for dynamic sandbox | Apple entitlement/bundle requirements are documented; current backend is not such a bundle | Packaging changes could silently change the security model | Explicit deployment gate |
| Signed-but-malicious executable | Phase35D identity verifies package/attestation identity | A certified binary can still be malicious | Residual |
| Path/hash TOCTOU | Phase35E hashes before launch | No atomic launch-time identity proof | Unresolved; blocks admission |
| Helper replacement | No helper selected or shipped | Future helper needs signed identity and parent launch constraints | Deferred |
| XPC impersonation | No XPC protocol exists | Future IPC needs authenticated session binding | Deferred |
| Localhost or Unix-socket escape | No local workload admitted | No proof of denial on current target | Unsupported |
| Secret leakage | No secret provider or credential injection exists | Future integration could leak through environment/artifacts | Deferred and prohibited |
| Orphan workload | No workload admitted; Phase35E lifecycle remains scoped | Host crash/termination behavior is not proven | Partial only |
| VM/container escape | No VM/container introduced | Future boundary depends on runtime/image hardening | Deferred |
| Unsupported OS upgrade | Selector records platform evidence and refuses unsupported capability states | New OS may change behavior | Fail closed |
| CI false confidence | Contract tests are separate from OS tests | Non-macOS CI cannot prove macOS enforcement | Deployment gate |

The principal security decision is to preserve absence of execution rather than accept a mechanism whose strongest claims are advisory, inherited, or host-side.
