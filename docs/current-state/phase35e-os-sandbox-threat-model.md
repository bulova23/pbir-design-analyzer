# Phase 35E OS Sandbox Threat Model

| Threat | Trust boundary/enforcement | Residual risk | Phase35E status |
| --- | --- | --- | --- |
| executable substitution / TOCTOU | pre-launch absolute-path hash verification | file can change after hashing before OS launch | not fully mitigated; real provider remains disabled |
| shell or argument injection | typed arguments, direct helper/executable launch, no shell strings | future adapter code could widen the spec | mitigated for current boundary |
| environment leakage | cleared environment and explicit allowlist | runtime may require undocumented variables | mitigated/fail-closed |
| filesystem traversal/symlink escape | Seatbelt profile text was generated but not admitted | deprecated mechanism/platform semantics and no current OS proof | unsupported; Phase35F keeps admission closed |
| network/loopback bypass | Seatbelt network rule was generated but not admitted | no current proof for loopback, DNS, TCP, UDP, or Unix sockets | unsupported; Phase35F keeps admission closed |
| child-process escape | Seatbelt process-fork rule was generated but not admitted | App Sandbox inheritance is not denial | unsupported; Phase35F keeps admission closed |
| resource exhaustion | timeout and bounded output | memory/CPU/process count unsupported | partial; admission fails when required |
| orphan process / cleanup race | owned lifecycle and scoped working directory | abrupt host termination can leave OS resources | partial; cleanup failure is reported |
| policy downgrade | typed policy binding and unsupported-control denial | caller cannot prove external policy provenance alone | fail-closed at Phase35E boundary |
| malicious certified executable | OS boundary plus exact certification identity | a correctly certified malicious binary may attack kernel/runtime | residual risk; certification is not a malware guarantee |
