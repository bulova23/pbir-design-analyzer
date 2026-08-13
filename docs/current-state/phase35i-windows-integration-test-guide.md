# Phase 35I Windows Integration Test Guide

Run on a real Windows worker with x64 architecture, .NET 8 runtime, the certified inert runner installed beneath the worker-controlled root, and no production credentials. The worker account must be able to create a restricted token and Job Object; no administrator elevation is assumed or granted by the test.

```text
dotnet test service-dotnet/tests/Tests.csproj -c Release --filter Category=WindowsIntegration
```

The suite must report successful inert launch, exact identity, launch ordering, Job Object assignment/limits, direct and nested child behavior, process count, timeout, cancellation, environment exclusion, ACL denial, cleanup, kill-on-close, and deterministic native failure mapping. On macOS/Linux, the suite is explicitly skipped as not applicable. In the current checkout the ten test methods are still skip-only scaffolds with empty bodies; they cannot be treated as executable coverage until implemented with closed inert-runner assertions. A green compile or a skipped count is not containment proof.
