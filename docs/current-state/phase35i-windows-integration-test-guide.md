# Phase 35I Windows Integration Test Guide

Run on a real Windows worker with x64 architecture, .NET 8 runtime, the certified inert runner installed beneath the worker-controlled root, and no production credentials. The worker account must be able to create a restricted token and Job Object; no administrator elevation is assumed or granted by the test.

```text
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter Category=WindowsIntegration
```

Phase 35K provides eleven executable test methods covering successful inert launch, launch-state evidence, exact identity, Job Object assignment/limits, closed direct and nested-child runner attempts, timeout, cancellation, environment exclusion, restricted-resource setup, bounded artifact lineage, cleanup, and deterministic failure taxonomy. The reusable test harness stages only the repository-owned inert-runner build output, computes a deterministic package manifest hash and executable hash, creates the exact worker profile/request, invokes existing Phase35I admission/runtime/evidence code, and removes a private worker root.

The harness never resolves executables from PATH and never accepts a caller executable. A Windows test run must first build `service-dotnet/Phase35I.InertRunner` in the repository-owned Release or Debug `net8.0` output directory. The test then copies that package to its private worker root and verifies identity before every runtime execution.

The conditional xUnit Fact checks Windows, x64, and .NET 8 at discovery. On macOS/Linux, the suite is discovered and skipped with `NotApplicable:Phase35I.WindowsIntegration:<reason>`. The expected local result is 11 discovered, 0 executed, 0 passed, 0 failed, and 11 skipped. A green compile or skipped count is not Windows containment proof.

Phase35I's current evidence contract does not expose native step-by-step telemetry, Job Object accounting snapshots, child PID lineage, ACL result fields, or artifact manifest fields. Phase 35K asserts all available result/evidence fields and closed inert-runner behavior without adding runtime architecture. These remaining measured-proof questions belong to the certified Windows execution gate.
