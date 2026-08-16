# Phase 35I Windows Containment Design

## Outcome

Phase 35I adds an executable-ready Windows containment proof path for the existing Phase35H remote boundary, but does not claim Windows containment proof on non-Windows hosts. The authoritative status on the current macOS checkout is `PartiallyProven`.

## Architecture

The implementation has two layers:

1. **Portable admission and evidence** in `Services/Discovery/Phase35I`. It validates exact worker profiles, certified package/executable identity, closed inert workloads, finite Phase35C resource policy, session-owned paths, and produces canonical immutable evidence. It has no Windows process APIs.
2. **Windows runtime boundary** in `Phase35I.Runtime`, a dedicated `net8.0-windows` assembly. All P/Invoke, restricted-token construction, suspended process creation, Job Object configuration, handle ownership, explicit environment construction, resume, termination, and native failure mapping live here.

`Phase35I.InertRunner` is a repository-owned executable with a closed workload switch. It accepts only an internal workload enum and never accepts a process path, shell text, arbitrary arguments, arbitrary environment, or caller-selected working directory.

The runtime derives the executable from a worker-controlled installation root and certified relative runner identity. It validates normalized containment and hashes before launch. This is an identity check, not an atomic TOCTOU guarantee.

## Portable contracts and flow

The flow is:

`Phase35H validated request -> Phase35I admission -> exact worker profile -> exact runner identity -> Windows runtime -> containment result -> canonical evidence -> Phase35H audit correlation`.

Contracts use closed enums and immutable records for worker profile, package and executable identity, containment profile, workload mode, admission, resource projection, lifecycle/result, native failures, evidence, and proof classification. `Phase35CResourcePolicy` is projected into OS-enforced and worker-enforced controls without labeling host monitoring as Job Object enforcement. Network isolation remains unproven.

Admission fails closed for unknown profiles, identity mismatch, traversal, reparse ambiguity where detectable, unsupported required controls, non-finite policy, arbitrary launch inputs, missing audit correlation, and non-Windows runtime admission.

## Windows launch proof

The runtime enforces this order and records each completed step:

`Verify -> Restricted Token -> Job Object -> Suspended Launch -> Assignment -> Resume`.

Any failure before resume terminates and closes the suspended process safely without resuming it. Job policy is deliberately narrow: kill-on-job-close, active-process limit, no breakaway, memory limit, optional CPU/time limit where selected, and accounting/query support. No firewall, WFP, AppContainer networking, VM, credentials, provider, shell, or publication behavior is added.

The restricted-token record describes disabled privileges, administrative-group handling, restricted SIDs, and integrity assumptions. It is defense-in-depth and is not VM-grade isolation.

## Testing and proof status

Portable tests cover fail-closed admission, identity/path validation, resource projection, evidence hashing, audit correlation, proof classification, inert workload closure, and boundary scans. Boundary tests assert native API names occur only in the Windows runtime project and prohibited execution capabilities are absent.

Windows integration tests are explicitly marked `WindowsIntegration` and use actual OS behavior for successful launch, identity, ordering, Job Object assignment, child/nested child, process count, timeout, cancellation, environment exclusion, ACL denial, cleanup, kill-on-close, and native failure mapping. They are skipped with an explicit `NotApplicable: Windows integration requires a real Windows worker` reason on macOS/Linux. Compilation and discovery do not upgrade proof status.

## Residual risks

The threat model covers breakaway, child escape, Job Object and token misconfiguration, broad worker authority, token duplication, handle/environment leakage, runner substitution, TOCTOU, DLL search order, reparse points, directory ACLs, handle leaks, worker crash/orphans, native API misuse, accounting errors, unrestricted networking, and a signed but malicious runner. Each item records mitigation, evidence, residual risk, and whether it blocks real provider execution. Windows network isolation and stronger filesystem isolation remain future controls; no real provider is admitted by this phase.
