# Report Design Studio Task 7 Readiness Review

Date: 2026-06-13

## Scope

- Review Report Design Studio through Task 6 before starting Task 7 Materialization
- No code changes

## Files Reviewed

- `docs/superpowers/specs/2026-06-12-report-design-studio-design.md`
- `docs/superpowers/plans/2026-06-12-report-design-studio-plan.md`
- `docs/superpowers/implementation-notes/2026-06-13-report-design-studio-readiness-cleanup.md`
- `vscode-extension/src/design-studio/contracts/designStudioModels.ts`
- `vscode-extension/src/design-studio/contracts/designStudioProtocol.ts`
- `vscode-extension/src/design-studio/state/designBriefStore.ts`
- `vscode-extension/src/design-studio/state/conceptStore.ts`
- `vscode-extension/src/design-studio/state/draftStore.ts`
- `vscode-extension/src/design-studio/state/refinementStore.ts`
- `vscode-extension/src/design-studio/navigation/designArtifactBacklinkResolver.ts`
- `service-dotnet/Services/DesignStudio/Models/DesignStudioModels.cs`
- related Jest and xUnit Design Studio tests

## Findings

1. Task 6 preserves the advisory-only trust boundary and keeps analyzer ingestion separate from mutation, but Task 7 inputs are not exact enough yet.
   - `MaterializationRequest` currently carries only `sourceArtifactIds` and no exact source artifact version references.
   - stale analyzer-output rejection in `refinementStore` accepts any non-empty subset of matching version ids rather than enforcing a complete source-version fingerprint.
2. Refinement proposals are reasonably attributable for Task 6 review, but not fully ready for materialization selection.
   - proposals keep analyzer run id, result reference, payload, linked finding ids, affected artifact ids, and affected artifact version ids
   - there is no refinement approval transition workflow yet; `refinementApproval`, `validationApproval`, and `materializationApproval` exist as vocabulary only
3. Runtime protocol guards are only shallow for nested messages.
   - `requestMaterialization` and `materializationRequested` validate only that `request` is an object, not that its artifact ids, analyzer ids, approval state, or source versions are valid
4. Backlink resolution works for current Task 6 tests but remains heuristic.
   - current linkage depends on page-title matching plus impact-area rules rather than a stable materialized surface mapping
5. Provider provenance is adequate for draft attribution, but not yet rich enough for materialization diagnostics.
   - artifact provenance captures provider identity, capability, request/proposal ids, and model/version
   - deferred provider metadata for workflow phase, evidence-domain fit, and analyzer-handoff expectations is still absent

## Validation

- `cd vscode-extension && npm test -- --runInBand designBriefStore.test.ts conceptStore.test.ts draftStore.test.ts refinementStore.test.ts designArtifactBacklinkResolver.test.ts designStudioContracts.test.ts designStudioProtocol.test.ts`
  - extension Jest tests passed
  - package wrapper then invoked webview Jest with no matching files for that narrowed pattern and exited non-zero
- `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~DesignStudio`
  - passed

## Recommendation

- Pause before Task 7 Materialization.
- Required cleanup:
  - define exact source artifact version references on materialization inputs
  - tighten stale-output rejection from subset acceptance to complete source-set validation
  - add explicit refinement approval semantics and selection rules
  - hard-validate nested materialization protocol payloads
  - decide whether materialization will rely on heuristic backlinks or a stable source-to-surface mapping
