# Report Design Studio Task 8 Readiness Review

Date: 2026-06-13

## Scope

- Review Report Design Studio through Task 7 before starting Task 8 Analyzer Handoff
- No code changes

## Review Checklist

- Design Brief lineage
- Concept lineage
- Draft lineage
- Refinement lineage
- `MaterializationRequest` semantics
- `MaterializedSurfaceCandidate` semantics
- materialization diagnostics
- provenance trace
- approval workflow separation
- trust-boundary protections

## Status

- Complete

## Files Reviewed

- `docs/superpowers/specs/2026-06-12-report-design-studio-design.md`
- `docs/superpowers/plans/2026-06-12-report-design-studio-plan.md`
- `vscode-extension/src/design-studio/contracts/designStudioModels.ts`
- `vscode-extension/src/design-studio/contracts/designStudioProtocol.ts`
- `vscode-extension/src/design-studio/materialization/materializationCoordinator.ts`
- `vscode-extension/src/design-studio/materialization/materializationMapper.ts`
- `vscode-extension/src/design-studio/navigation/designArtifactBacklinkResolver.ts`
- `vscode-extension/src/design-studio/state/designBriefStore.ts`
- `vscode-extension/src/design-studio/state/conceptStore.ts`
- `vscode-extension/src/design-studio/state/draftStore.ts`
- `vscode-extension/src/design-studio/state/refinementStore.ts`
- `vscode-extension/src/analyzer/analyzers/registry.ts`
- `vscode-extension/src/analyzer/surfaces/discovery.ts`
- `vscode-extension/src/analyzer/surfaces/fabricAppDiscovery.ts`
- `vscode-extension/src/analyzer/fabric/review/fabricAppReviewAnalyzer.ts`
- `vscode-extension/src/test/designStudioContracts.test.ts`
- `vscode-extension/src/test/designStudioProtocol.test.ts`
- `vscode-extension/src/test/materializationCoordinator.test.ts`
- `vscode-extension/src/test/refinementStore.test.ts`
- `vscode-extension/src/test/designArtifactBacklinkResolver.test.ts`
- `service-dotnet/Services/DesignStudio/Models/DesignStudioModels.cs`
- `service-dotnet/tests/DesignStudio/DesignStudioModelBoundaryTests.cs`
- `service-dotnet/tests/DesignStudio/DesignStudioMaterializationTests.cs`

## Findings

1. Task 8 Analyzer Handoff is not fully ready yet because `MaterializedSurfaceCandidate` is structurally defined but not launch-ready for the existing analyzer path.
   - `materializationMapper.ts` emits synthetic `design-studio://materialization/...` source locations.
   - the existing analyzer path still expects real repository-backed locations, for example Fabric App review calls `createRepositorySnapshot(surface.sourceLocation)`.
   - before Task 8, define the handoff payload or resolver seam that converts a materialized candidate into something the analyzer workspace can actually open without ad hoc hidden state.
2. `MaterializedSurfaceCandidate` is sufficiently defined for Task 7 boundary validation, but not yet sufficient as the full long-term iteration contract.
   - it carries source lineage, provenance trace, diagnostics, derived surface metadata, and explicit handoff metadata.
   - it does not yet carry iteration linkage, analyzer-run linkage, or a durable executable surface reference.
3. Provenance and lineage are strong enough for analyzer round-tripping into Refinement Studio, but only for the current source-artifact return path.
   - exact version lineage exists across brief, concept, draft, refinement, request, and candidate records.
   - refinement ingestion rejects incomplete or stale artifact fingerprints and stable backlink identities survive page renames.
   - full closed-loop auditability still depends on Task 9 iteration records.
4. Materialization diagnostics are currently minimal.
   - they confirm mode and no-side-effect guarantees.
   - they do not explain mapping degradations, omitted evidence domains, unsupported artifact combinations, or how a candidate was made executable for analyzer launch.
5. Approval separation is directionally correct but not complete.
   - approval vocabularies are distinct and refinement proposals have explicit review, approve, and reject transitions.
   - `validationApproval` exists only as vocabulary today, and candidate approval must not be misread as analyzer validation success.
6. No direct trust-boundary leak was found in current code.
   - no direct report mutation, PBIR generation, deployment, provider execution, analyzer execution, or public score-panel contract widening was found.
7. A hidden analyzer ownership risk remains.
   - materialization currently duplicates analyzer and surface capability knowledge inside `buildDerivedSurface` instead of resolving it entirely through shared surface discovery and registry paths.
   - if Task 8 builds on that duplication, Design Studio will start owning analyzer compatibility decisions that should stay centralized.

## Validation

- `cd vscode-extension && npx jest --runTestsByPath src/test/materializationCoordinator.test.ts src/test/designStudioProtocol.test.ts src/test/refinementStore.test.ts src/test/designArtifactBacklinkResolver.test.ts`
  - passed
- `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudio`
  - passed

## Recommendation

- Pause before Task 8.
- Required cleanup before starting:
  - define a handoff-ready executable surface reference or resolver instead of relying on synthetic `design-studio://` source locations
  - decide whether analyzer compatibility should come from shared discovery or registry seams instead of materialization-local surface definitions
  - expand materialization diagnostics enough to explain mapping or degradation behavior that future refinement loops will need
  - document explicitly that `materializationApproval` is separate from future analyzer validation approval
