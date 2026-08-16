# Phase 35I Windows Containment Profile

Profile ID: `windows-worker-proof/v1`  
Containment profile: `phase35i-job-restricted-token/v1`  
Runner: repository-owned `Phase35I.InertRunner`, certified package and executable hashes, certified relative executable identity.

## Native boundary

Only `service-dotnet/Phase35I.Runtime/` calls `OpenProcessToken`, `CreateRestrictedToken`, `CreateProcessAsUser`, `CreateJobObject`, `SetInformationJobObject`, `AssignProcessToJobObject`, `IsProcessInJob`, `ResumeThread`, `TerminateJobObject`, and `CloseHandle`. Core and the inert runner do not call these APIs.

## Launch and cleanup

1. Validate profile, package hash, executable hash, normalized root, session root, workload, finite policy, and audit correlation.
2. Open the worker token and create a maximum-privilege-disabled restricted token.
3. Create/configure a Job Object for kill-on-close, active-process and memory limits.
4. Build an explicit empty Unicode environment and create the certified runner suspended.
5. Assign and verify the process in the Job Object.
6. Resume the primary thread.
7. On cancellation/timeout terminate the Job Object; close thread, process, token, and Job handles in `finally`.

No caller-controlled executable path, command line, working directory, environment, or child executable is accepted. The current identity check is not atomic against replacement after verification; deployment must add stronger immutable installation/ACL controls before provider use.
