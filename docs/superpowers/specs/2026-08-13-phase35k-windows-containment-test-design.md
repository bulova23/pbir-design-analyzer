# Phase 35K Windows Containment Test Design

## Outcome

Phase 35K replaces the Phase35I Windows integration skip scaffold with executable xUnit coverage. The suite remains discovery-skipped on unsupported hosts and can classify real Windows inert-runner evidence as `ProvenForInertWorkload`, `PartiallyProven`, or `NotProven` through the existing Phase35I evidence contract.

## Test architecture

`Phase35IWindowsHarness` is test-only. It checks Windows/x64/.NET 8, locates only the repository-owned inert-runner build output at the fixed repository path, copies the complete package below a private worker root, hashes the executable and deterministic package manifest, creates the certified profile/request, invokes existing admission/runtime/evidence code, and removes its workspace idempotently.

The integration class uses a conditional xUnit Fact attribute. macOS/Linux discovery reports explicit `NotApplicable:Phase35I.WindowsIntegration:<reason>` skips; Windows executes the same test bodies. No caller executable, PATH lookup, shell, provider, credential, network, or mutation path is introduced.

## Coverage

The suite covers successful launch and evidence, the existing launch-state result contract, Job Object policy evidence, closed inert child-process attempts, timeout, cancellation, explicit environment, restricted-resource fixture setup, bounded artifact lineage, cleanup, and admission/failure taxonomy. Every runtime test validates canonical payload/hash, request and Phase35H correlation, worker profile, runner identity, and containment profile.

Phase35I currently does not expose native step-by-step telemetry, Job Object accounting snapshots, child PID lineage, ACL result fields, or artifact manifest fields in its evidence record. The tests therefore assert the existing evidence fields and closed runner behavior without inventing telemetry or changing Phase35I. A certified Windows run is required to determine whether those measured controls are sufficient for the requested proof classification.

## Proof boundary

The current host remains `PartiallyProven`. Phase35K proves that the repository now contains executable validation coverage; it does not claim Windows containment evidence from cross-compilation or skipped macOS execution. Phase35L should only execute this suite on a certified Windows worker and remediate measured failures.
