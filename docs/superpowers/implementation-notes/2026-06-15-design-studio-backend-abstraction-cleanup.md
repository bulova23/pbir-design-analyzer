# Design Studio Backend Abstraction Cleanup

Date: 2026-06-15

## Scope Implemented

- Workstream 9 only from the 2026-06-14 PBIR engineering remediation spec and plan
- No provider-backed generation
- No new Design Studio feature work
- No backend runtime provider wiring
- No TypeScript Design Studio runtime behavior changes

## Runtime Usage Audit

### Active backend ownership

- `service-dotnet/Services/DesignStudio/Models/DesignStudioModels.cs`
  - remains the backend-local mirror of the active Design Studio artifact and handoff contract vocabulary already used by the shipped TypeScript workflow
  - still defines trust-boundary semantics that backend tests lock:
    - approval separation
    - analyzer-owned validation provenance
    - non-mutating materialization and refinement guarantees
    - closed-loop guardrails

### Contract mirror retained intentionally

- `DesignProviderCapabilityKind`
  - retained only because `DesignArtifactProvenance.ProviderCapabilityKind` is part of the duplicated Design Studio contract vocabulary mirrored in TypeScript
  - this is not a runtime provider registry
  - this does not imply provider execution support

### Speculative runtime surface removed

- `service-dotnet/Services/DesignStudio/Providers/IDesignStudioProvider.cs`
- `service-dotnet/Services/DesignStudio/Providers/ProviderCapabilityModels.cs`
- `service-dotnet/Services/DesignStudio/Materialization/MaterializationGatewayModels.cs`

These files were backend-only speculative scaffolding:

- they had no runtime call sites
- they were not protecting a live execution boundary
- they duplicated semantics already represented in `DesignStudioModels.cs`
- they existed only because reflection tests asserted their presence

## Ownership Decision

- Keep as active runtime boundary:
  - none in the removed provider/materialization files
- Keep as contract mirror with documentation:
  - `DesignStudioModels.cs`
  - `DesignProviderCapabilityKind` inside that file
- Remove if unused and safe:
  - speculative provider registry interface and capability records
  - duplicate materialization gateway models namespace

## Trust Boundaries Preserved

The cleanup keeps backend tests that still protect current architecture:

- approval separation remains explicit
- validation stays analyzer-owned
- refinement stays no-mutation
- materialization stays candidate-only
- report mutation stays out of Design Studio

## Future Revisit Conditions

Provider-backed generation may revisit this area later only when all of the following are true:

- a real backend provider execution path exists
- the provider boundary is consumed by live runtime code instead of reflection-only tests
- trust, provenance, and failure semantics are specified for that concrete execution path
- documentation explains why the backend must own that abstraction rather than mirroring a TypeScript-only workflow contract
