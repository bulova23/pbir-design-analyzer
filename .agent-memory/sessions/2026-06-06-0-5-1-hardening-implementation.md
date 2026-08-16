# 2026-06-06 0.5.1 Hardening Implementation

## Objective

Implement Recommended `0.5.1` only from the engineering hardening roadmap:

- safe deterministic fix-engine trust restoration
- PBIR-derived governance theme verification
- screenshot-upload workflow repair

Explicitly out of scope:

- Recommended `0.5.2`
- Recommended `0.6.0`
- namespace cleanup
- telemetry decisions
- capabilities declarations
- protocol versioning
- scalability refactors

## Completed

### Checkpoint 1

- Formalized the currently supported deterministic mutation surface.
- Added stable page identity threading from backend score results into the extension payload.
- Replaced ambiguous page/display-name routing with page-ID keyed resolution when available and fail-closed duplicate display-name behavior otherwise.
- Added schema-correct PBIR title mutation shaping using:
  - `visual.visualContainerObjects.title[0].properties.text.expr.Literal.Value`

### Checkpoint 2

- Reworked deterministic fix application to use storage-path aware reads/writes.
- Added atomic temp-file plus rename persistence for single-fix and batch-fix execution.
- Added rollback-on-failure using pre-apply backups.
- Added post-write validation to verify persisted PBIR values round-trip to the expected mutation state.

### Checkpoint 3

- Expanded deterministic safety tests for:
  - schema-correct title writes
  - stale-target detection after title drift
  - duplicate page-name ambiguity fail-closed behavior
  - failed batch persistence with rollback protection
- Kept the current safe fallback explicit:
  - supported mutations use atomic validated canonical JSON rewrites until surgical format-preserving patching is implemented later

### Checkpoint 4

- Governance checks now read the report theme directly from PBIR metadata instead of prompting the user.
- `pbir.uploadScreenshots` now opens the active score panel and triggers screenshot upload directly instead of rescoring the report.

## Validation

### Focused

- Passed:
  - `cd vscode-extension && npx jest src/test/fixMutationPlanner.test.ts src/test/fixOpportunityBuilder.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/fixApplyEngine.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/fixMutationPlanner.test.ts src/test/fixApplyEngine.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/fixMutationPlanner.test.ts src/test/fixOpportunityBuilder.test.ts src/test/fixApplyEngine.test.ts --runInBand`
  - `cd vscode-extension && npx jest src/test/pbirGovernanceCommand.test.ts src/test/pbirUploadScreenshotsCommand.test.ts --runInBand`

### Full Required Validation

- Passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## Risks / Notes

- The implemented safe fallback is intentionally conservative:
  - canonical JSON rewrite is atomic and validated, but not format-preserving at the original Power BI serialization level
- That residual formatting concern is explicitly left for later hardening work and does not reopen corruption or partial-write risk inside the approved `0.5.1` scope
- Packaging was intentionally not rerun in this session

## Next Step

- Perform a manual VS Code smoke check of deterministic preview/apply/rollback against a real PBIR report before any `0.5.1` packaging or release cut.
