# Report Design Studio Materialization Readiness Hardening

Date: 2026-06-13

## Scope

- Implement readiness hardening only before any Task 7 Materialization work
- No materialization execution
- No analyzer handoff
- No analyzable surface creation
- No PBIR asset generation
- No report mutation

## Implemented

- Added exact source lineage infrastructure for:
  - `MaterializationRequest`
  - `MaterializedSurfaceCandidate`
  - `RefinementProposal`
- Source lineage entries now preserve:
  - `artifactId`
  - `artifactVersionId`
  - `approvalState`
  - `approvalTimestamp`
- Tightened refinement freshness validation from subset acceptance to complete fingerprint matching across the full active draft artifact set, including the draft report artifact and navigation artifacts.
- Added explicit stale rejection diagnostics through `StaleAnalyzerOutputError`.
- Added explicit refinement workflow transitions:
  - proposed
  - reviewed
  - approved
  - rejected
- Added deep nested protocol validation for materialization request payloads:
  - artifact ids
  - version ids
  - approval states
  - analyzer ids
  - analyzer profile ids
- Added stable backlink identities anchored on:
  - design artifact id/version id
  - draft artifact id/version id
- Verified backlink resolution can survive title changes when stable identities are available.

## Preserved Boundaries

- no Task 7 materialization logic
- no analyzer handoff
- no analyzable surface creation
- no PBIR asset generation
- no deployment
- no AI/provider execution
- no report mutation

## Validation

- Focused red/green validation:
  - `cd vscode-extension && npx jest --runInBand src/test/designStudioContracts.test.ts src/test/designStudioProtocol.test.ts src/test/designArtifactBacklinkResolver.test.ts src/test/refinementStore.test.ts`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudioModelBoundaryTests`
- Required validation passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## Next Recommended Step

- Re-run a short architecture review focused on whether the new lineage and refinement approval records are sufficient for Task 7 request shaping before implementing any materialization coordinator.
