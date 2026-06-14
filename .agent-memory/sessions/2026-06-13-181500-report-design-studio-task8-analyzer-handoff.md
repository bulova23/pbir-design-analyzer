# Report Design Studio Task 8 Analyzer Handoff

Date: 2026-06-13 18:15 America/New_York

## Scope

- Implement Task 8 only:
  - Analyzer Handoff as a peer workflow
- Do not implement Task 9 Closed Loop Optimization.
- Do not run analyzer execution automatically.
- Do not score automatically.
- Do not validate automatically.
- Do not mutate reports.
- Do not generate PBIR files.
- Do not deploy.

## Plan

- Add failing extension tests for executable handoff, blocked preview handoff, blocked unsupported handoff, payload preservation, and no-mutation boundaries.
- Implement `AnalyzerHandoffService` on top of the existing readiness resolver rather than duplicating compatibility or surface discovery logic.
- Add a non-executing Analyzer Workspace launch path that opens the existing workspace shell without scoring.
- Register an internal command seam for future Design Studio callers.
- Run required validation and record the boundary outcome.

## Outcome

- Implemented `AnalyzerHandoffService` in `vscode-extension/src/design-studio/materialization/analyzerHandoffService.ts`.
- Preserved centralized compatibility by reusing:
  - analyzer registry support through `getSupportedAnalyzersForSurface`
  - shared analyzable surface builders
  - existing materialized-candidate handoff resolver
  - existing analyzable surface discovery
- Added an internal `AnalyzerWorkspaceHandoffPayload` contract carrying:
  - candidate id
  - lineage
  - provenance
  - provenance trace
  - source artifact references
  - source artifact version references
  - materialization diagnostics
  - analyzer id
  - analyzer profile id
  - surface family
  - executable eligibility
  - handoff reference
  - compatibility status
- Added a peer-workflow launch seam by opening `PbirScorePanel` in a non-executing handoff shell state.
- Registered internal command `pbirAnalyzer.openAnalyzerWorkspaceHandoff` for future Design Studio callers.

## Boundary Verification

- Preserved analyzer ownership:
  - no automatic analyzer execution
  - no automatic scoring
  - no automatic validation
  - no report mutation
  - no PBIR file generation
- Successful handoff side effects are limited to:
  - analyzer handoff executed = true
  - analyzer workspace opened = true
  - PBIR files created = false
  - report mutation occurred = false
  - delivery triggered = false
  - provider execution triggered = false
- Blocked preview and unsupported candidates preserve:
  - analyzer handoff executed = false
  - analyzer workspace opened = false

## Validation

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- Focused extension validation also passed:
  - `cd vscode-extension && npx jest --runTestsByPath src/test/analyzerHandoffService.test.ts src/test/materializationHandoffResolver.test.ts src/test/materializationCoordinator.test.ts src/test/pbirReviewWorkflowExportCommand.test.ts`

## Deliverables

- `vscode-extension/src/design-studio/materialization/analyzerHandoffService.ts`
- `vscode-extension/src/design-studio/contracts/designStudioModels.ts`
- `vscode-extension/src/views/PbirScorePanel.ts`
- `vscode-extension/src/commands/register.ts`
- `vscode-extension/src/platform/extensionIds.ts`
- `vscode-extension/src/extension.ts`
- `vscode-extension/src/test/analyzerHandoffService.test.ts`
