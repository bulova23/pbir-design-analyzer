# 2026-06-16 18:54 Analyzer Return Loop UX

## Goal

Implement Report Design Studio MVP Workflow Completion Phase 7:

- Analyzer Return Loop UX

## Scope

- explicit Review Design return-loop states
- explicit Attach Analyzer Results action
- analyzer result lineage and provenance preservation
- Refinement Studio readiness updates from attached results
- Compare Iterations analyzer-return status updates
- Workflow Completion analyzer-return checklist updates

## Constraints

- preserve analyzer ownership
- preserve validation ownership
- no automatic validation approval
- no automatic analyzer execution
- no report mutation
- no provider execution

## Progress

- loaded repo guidance, memory, and required specs/docs
- traced current implementation across:
  - `reviewDesignStore`
  - `iterationStore`
  - `refinementStore`
  - `designStudioWorkspace`
  - Design Studio protocol
  - Design Studio webview shell
- identified missing seam:
  - Review Design currently stops at launch/completed and does not model result availability, explicit attachment, or downstream analyzer-return propagation
- next step:
  - add failing tests for return-loop states and explicit result attachment

## Delivered

- explicit Review Design return-loop states
- explicit `Attach Analyzer Results` action path in the shell and protocol
- persisted analyzer-result availability and attachment metadata
- iteration recording from explicitly attached analyzer results
- Refinement Studio unlock moved behind explicit result attachment
- Workflow Completion checklist updated for:
  - Review Design completed
  - Analyzer results attached
  - Refinement reviewed
  - Validation approval status recorded

## Preserved Boundaries

- analyzer ownership remains in Analyzer Workspace
- Design Studio does not self-validate
- no automatic validation approval
- no automatic analyzer execution
- no report mutation
- no provider execution

## Validation

- passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## Next Recommended Step

- stop here for this phase
- if follow-up work resumes, keep any real analyzer return plumbing separate from this UX slice
