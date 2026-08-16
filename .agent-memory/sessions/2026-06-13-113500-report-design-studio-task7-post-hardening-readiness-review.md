# Report Design Studio Task 7 Post-Hardening Readiness Review

Date: 2026-06-13

## Scope

- Review Report Design Studio readiness for Task 7 Materialization after readiness hardening
- No code changes

## Files Reviewed

- `docs/superpowers/specs/2026-06-12-report-design-studio-design.md`
- `docs/superpowers/plans/2026-06-12-report-design-studio-plan.md`
- `docs/superpowers/implementation-notes/2026-06-13-report-design-studio-readiness-cleanup.md`
- `vscode-extension/src/design-studio/contracts/designStudioModels.ts`
- `vscode-extension/src/design-studio/contracts/designStudioProtocol.ts`
- `vscode-extension/src/design-studio/navigation/designArtifactBacklinkResolver.ts`
- `vscode-extension/src/design-studio/state/refinementStore.ts`
- `vscode-extension/src/test/designStudioContracts.test.ts`
- `vscode-extension/src/test/designStudioProtocol.test.ts`
- `vscode-extension/src/test/designArtifactBacklinkResolver.test.ts`
- `vscode-extension/src/test/refinementStore.test.ts`
- `service-dotnet/Services/DesignStudio/Models/DesignStudioModels.cs`
- `service-dotnet/tests/DesignStudio/DesignStudioModelBoundaryTests.cs`
- `service-dotnet/tests/DesignStudio/DesignStudioProviderBoundaryTests.cs`

## Findings

1. Task 7 is now safe to start, but the materialization request trust boundary is not deeply validated enough yet.
   - The protocol parser validates `sourceArtifactIds`, `sourceLineage`, and target enums.
   - It does not validate `approvalKind`, `lifecycleState`, `authorSource`, timestamp shape, positive integer versioning, source-lineage uniqueness, or `sourceArtifactIds` to `sourceLineage` correspondence.
   - This is a cleanup item to finish before the first real request executor trusts webview-originated materialization payloads.
2. Exact source artifact and version references are substantially improved, but attribution is still a little thin for future diagnostics.
   - `MaterializationRequest` and `MaterializedSurfaceCandidate` now carry `sourceLineage`.
   - `SourceArtifactLineageEntry` preserves artifact id, version id, approval state, and approval timestamp.
   - The lineage entry still omits explicit artifact kind or source role, so future diagnostics must infer that from ids or external lookups.
3. Stable backlinks are good enough to start Task 7, but only strongly demonstrated for page rename scenarios.
   - Stable identities survive title changes for page concept and draft page linkage.
   - The stable-identity re-entry path currently reconstructs page and draft-page candidates only, so richer layout or navigation diagnostics will still lean on resolver heuristics.

## Validation

- `npx jest --runTestsByPath src/test/designStudioProtocol.test.ts src/test/refinementStore.test.ts src/test/designArtifactBacklinkResolver.test.ts`
  - passed
- `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudio`
  - passed
- note:
  - `npm test -- --runTestsByPath ...` is not suitable for this focused validation because the package wrapper chains a second webview Jest invocation against the same paths and exits with "No tests found"

## Recommendation

- Proceed to Task 7 Materialization.
- Carry two explicit cleanup expectations into the first Task 7 slice:
  - harden `MaterializationRequest` semantic validation before trusting live execution inputs
  - decide whether Task 7 diagnostics need explicit artifact kind or role in source lineage instead of inferring it later
