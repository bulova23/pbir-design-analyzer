# Report Design Studio Readiness Cleanup

Date: 2026-06-13

## Scope Implemented

- Post-Task-5 readiness cleanup before any Task 6 Refinement Studio work

## What Changed

- Approval transitions now mint new immutable artifact versions for:
  - Design Brief approval
  - Concept approval
- Prior artifact history entries remain unchanged after approval.
- Concept Studio lineage now records exact approved brief provenance on concept artifacts:
  - `sourceBriefId`
  - `sourceBriefVersionId`
- Child concept artifacts now preserve exact concept-lineage references for the current concept version:
  - `sourceBriefVersionId`
  - `sourceReportConceptVersionId`
- Design Studio artifacts now carry explicit approval semantics through `approvalKind`:
  - `designApproval`
  - `refinementApproval`
  - `validationApproval`
  - `materializationApproval`
- Current Task 1 to Task 5 artifact flows use only `designApproval`.
- Design Studio host/webview messages now have runtime parsing and safe rejection for:
  - protocol version mismatch
  - missing or malformed required fields
  - unsupported message types

## Provider Metadata Readiness Decision

- Provider capability metadata was reviewed for:
  - workflow phase
  - evidence-domain fit
  - analyzer-handoff expectations
- Decision:
  - defer these additions for now
- Reason:
  - Task 6 readiness only required immutable lineage, explicit approval semantics, and runtime protocol validation.
  - No current provider execution path consumes refinement-phase orchestration metadata yet.
  - Adding speculative capability metadata now would overbuild Task 6 prerequisites and risk encoding premature workflow assumptions.
- Constraint:
  - if Task 6 introduces provider-assisted refinement selection or evidence-aware provider routing, add these fields in that slice with direct consuming behavior and tests.

## Preserved Boundaries

- no PBIR asset generation
- no analyzable surface creation
- no materialization
- no analyzer handoff execution
- no report mutation

## Validation

- Required validation passed:
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`

## Recommended Next Step

- Stop after this cleanup.
- Do not start Task 6, Task 7, materialization, or analyzer handoff on this branch unless explicitly requested.
