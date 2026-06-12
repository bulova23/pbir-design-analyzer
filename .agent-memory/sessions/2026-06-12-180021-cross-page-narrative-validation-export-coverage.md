# Session: Cross-Page Narrative Validation Export Coverage

## Started

- 2026-06-12 18:00:21 America/New_York

## Goal

- Investigate why the official validation export still emits placeholder Cross-Page Narrative fields on real reports and complete coverage if the fix is small and low risk.

## Constraints

- Do not add new Story Assessment logic.
- Do not add new Cross-Page Narrative logic.
- Do not modify Story Assessment UI.
- Do not modify score-panel contracts.

## Investigation Notes

- Reviewing report-level scoring, internal Cross-Page Narrative model shaping, and validation export reflection helpers.
- Early evidence indicates report-mode scoring populates `InternalCrossPageNarrativeAssessment`.
- Placeholder output appears concentrated in nested export shaping for page roles, graph path, and dimension scores.

## Validation Plan

- Add a focused regression test around nested Cross-Page Narrative shaping.
- If confirmed, apply the smallest reflection-helper fix inside the validation export harness only.
- Re-run targeted xUnit coverage, then run the official export CLI on:
  - Sales & Production
  - Sales Analysis
  - Running Record Dataverse
  - Sales AWF

## Root Cause

- The internal Cross-Page Narrative data was present in report-mode scoring.
- The official validation export adapter was the problem:
  - `GetInternalProperty` only searched non-public properties, so nested public properties on internal Cross-Page Narrative types were missed.
  - Affected nested artifacts:
    - `PageAssessment.RoleAssignment`
    - `Assessment.Graph`
    - `Assessment.ScoreSummary`
- This produced placeholder export values for:
  - page roles
  - role confidence
  - main narrative path
  - narrative dimension ids
  - narrative dimension scores
  - narrative dimension confidence
- After the reflection fix, the export still surfaced raw page ids for `MainNarrativePath`; that was a second adapter mapping gap rather than missing model data.

## Changes Applied

- Expanded validation-export reflection helpers to read both public and non-public properties.
- Mapped Cross-Page Narrative `MainNarrativePath` page ids to page names using the already-exported page metadata.
- Added regression coverage for nested public Cross-Page Narrative artifacts and readable narrative-path export shaping.

## Validation Results

- Focused validation passed:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter "FullyQualifiedName~StoryAssessmentValidationExportTests|FullyQualifiedName~CrossPageNarrativeExportAdapterTests|FullyQualifiedName~CrossPageNarrativeIntegrationTests"`
  - result: `12` passed, `0` failed
- Full backend validation passed:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - result: `247` passed, `0` failed
- Official export CLI rerun completed on:
  - `/Users/bcrowell/Documents/GitHub/PBITesting/Sales & Production.pbip`
  - `/Users/bcrowell/Documents/GitHub/PBITest2/Sales Analysis.pbip`
  - `/Users/bcrowell/Documents/GitHub/PBITest3/Running Record Dataverse.pbip`
  - `/Users/bcrowell/Documents/GitHub/PBITest4/Sales AWF.pbip`
- Cross-Page Narrative exports on all four reports now show concrete:
  - page roles
  - readable narrative path
  - dominant report objective
  - narrative dimensions
  - report-level gaps

## Outcome

- Cross-Page Narrative validation export coverage is complete for the current official harness and local Level 1 corpus.
- The fix stayed fully inside the internal validation export adapter and preserved all public product boundaries.

## Next Step

- Revisit whether the current Cross-Page Narrative role classification and dominant objective heuristics are accurate enough for promotion, now that the export no longer blocks corpus review.
