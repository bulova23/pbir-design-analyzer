# Phase 35J Windows Execution-Validation Plan

## Gate

Phase 35J is an execution-validation gate for Phase 35I. It may remediate only defects demonstrated by a real Windows run. It must not add provider execution, credentials, shell/process selection, PBIR generation, Desktop automation, networking architecture, MCP, Skills, publication, or Fabric mutation.

## Required worker

Run on a dedicated real Windows x64 worker with .NET 8, a worker-owned inert-runner installation root, no production credentials, and an account permitted to call the Phase35I native APIs. The authoritative command is:

```text
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter Category=WindowsIntegration --logger "console;verbosity=normal"
```

The worker setup must first build and stage `Phase35I.InertRunner`, compute its package and executable hashes, bind the exact worker profile, preserve failure artifacts, and then run the suite. The run must report discovered, executed, passed, failed, and skipped counts separately.

## Current gate result

This checkout is macOS, not Windows. The first unmodified run therefore discovered 10 tests and skipped all 10. No native Windows behavior was observed, so no Phase35I code was changed and no proof status upgrade is possible.

## Precondition found (resolved by Phase 35K)

`Phase35IWindowsIntegrationTests.cs` contained ten unconditional xUnit `Skip` attributes and empty test bodies. Phase 35K replaced that scaffold with eleven executable closed-fixture tests and a reusable runner-staging/evidence harness. The suite still skips at discovery on unsupported hosts and must now be validated on Windows; this remains test work, not a reason to add another containment layer.

## Proof order

Verify runner identity and profile, create the restricted token, create/configure the Job Object, launch suspended, assign and query membership, resume, run only closed inert workloads, collect bounded evidence, exercise timeout/cancellation/failure cleanup, and correlate the result to Phase35H. Classify the result as `ProvenForInertWorkload`, `PartiallyProven`, or `NotProven` from measured evidence.
