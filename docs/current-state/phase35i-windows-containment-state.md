# Phase 35I Windows Containment — Current State

## Result

`PartiallyProven`. Portable admission/evidence, a cross-platform compile of the Windows runtime, and an executable Phase35K Windows integration suite exist. This macOS checkout discovers 11 Windows tests and skips all 11. Compilation and skipped discovery are not containment evidence.

## Components

- `Services/Discovery/Phase35I/`: closed contracts, exact worker/runner admission, Phase35C resource projection, session path binding, canonical evidence.
- `Phase35I.Runtime/`: the only Windows native boundary; owns restricted-token, suspended launch, Job Object, assignment, resume, termination, and handle cleanup APIs.
- `Phase35I.InertRunner/`: repository-owned closed workload executable; no shell, arbitrary process, arbitrary arguments, downloaded code, or provider behavior.

The launch proof is encoded as `Verify -> Restricted Token -> Job Object -> Suspended Launch -> Assignment -> Resume`. Failures before resume terminate/close without resuming. The runner is selected from a worker-controlled installation root plus certified relative identity; no caller path is accepted. Reparse/TOCTOU and DLL search-order risks remain residual risks.

## Controls

| Control | Mechanism | Current proof |
|---|---|---|
| timeout | Job Object/runtime termination | implementation-ready; Windows unproven |
| process count | Job Object active-process limit | implementation-ready; Windows unproven |
| memory | no Phase35C memory field; Job Object limit not configured | not proven; requires an intentional policy extension |
| output/result bytes | worker-owned bounded capture | portable contract only |
| artifact count/bytes | Phase35C worker policy | portable contract only |
| kill on close | Job Object limit | implementation-ready; Windows unproven |
| no breakaway | default Job Object semantics; no breakaway creation flag | implementation-ready; Windows unproven |
| network | none | unproven and outside Phase35I |

Environment is an explicit empty block in the native launch path. No unrelated handles are inherited. Restricted-token evidence records maximum privilege disablement, administrative-group handling, restricted-SID handling, and integrity assumptions; it is not VM-grade isolation.

Phase35H remains backward compatible. Phase35I adds result/evidence correlation through the existing request correlation ID and request hash; it does not alter H transport semantics or scoring.

## Test status

Portable Phase35I tests: 6 passed. Boundary tests: 2 passed. Phase35K discovery on this macOS host reports 11 discovered, 0 executed, 0 passed, 0 failed, and 11 skipped as `NotApplicable:Phase35I.WindowsIntegration:Windows OS is required`. No Windows evidence exists. The suite is now executable closed-fixture coverage and is ready for a certified Windows proof gate.

Phase 35L should only execute and, if necessary, remediate this suite on a real certified Windows worker. It should not add another architecture layer or activate a real provider.
