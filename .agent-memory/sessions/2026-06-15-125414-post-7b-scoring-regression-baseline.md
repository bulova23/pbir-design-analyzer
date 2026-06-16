# 2026-06-15 Post-7B Scoring Regression Baseline

## Objective

- create a stable representative post-7B scoring baseline before Workstream 7C
- close the validation gap left by Workstream 7B
- avoid scorer refactors, architecture changes, and Workstream 7C implementation

## Constraints

- validation and baseline capture only
- no behavior changes
- no architecture changes
- no Workstream 7C implementation
- existing dirty worktree must remain untouched outside the requested additive baseline work

## Planned Validation

- `dotnet test service-dotnet/tests/Tests.csproj -c Release`
- `cd vscode-extension && npm test`
- `cd vscode-extension && npm run compile`

## Notes

- session started after reviewing `AGENTS.md`, repo memory files, and the mandatory Superpowers skill instructions
- reused the existing official Story Assessment validation export harness and prior real-report validation corpus
- representative local corpus available:
  - `/Users/bcrowell/Documents/GitHub/PBITesting/Sales & Production.pbip`
  - `/Users/bcrowell/Documents/GitHub/PBITest2/Sales Analysis.pbip`
  - `/Users/bcrowell/Documents/GitHub/PBITest3/Running Record Dataverse.pbip`
  - `/Users/bcrowell/Documents/GitHub/PBITest4/Sales AWF.pbip`

## Implemented

- captured additive normalized baseline fixtures at:
  - `service-dotnet/tests/Baselines/Post7BScoring/sales-and-production.baseline.json`
  - `service-dotnet/tests/Baselines/Post7BScoring/sales-analysis.baseline.json`
  - `service-dotnet/tests/Baselines/Post7BScoring/running-record-dataverse.baseline.json`
  - `service-dotnet/tests/Baselines/Post7BScoring/sales-awf.baseline.json`
- added focused real-fixture baseline regression coverage in:
  - `service-dotnet/tests/Post7BScoringBaselineTests.cs`
- documented the baseline at:
  - `docs/story-assessment/2026-06-15-post-7b-scoring-regression-baseline.md`

## Baseline Design

- kept the baseline tied to the two existing authoritative backend paths:
  - `PbirScoringService.ScoreAsync(reportPath)`
  - `StoryAssessmentValidationExportService.CreateReportAsync(reportPath)`
- stored a compact normalized projection rather than raw full export dumps
- preserved stable comparison coverage for:
  - top-level scoring
  - top-level recommendations
  - Guided Story Improvements ids and rationale
  - report consistency output
  - per-page story/actionability summary
  - validation-export Story Assessment fields
  - validation-export Cross-Page Narrative fields

## Normalization Rules

- removed absolute report-path persistence from the checked-in baseline
- excluded runtime timestamps:
  - `scoredAt`
  - `generatedAtUtc`
- identified reports by fixture file name only
- did not need additional transient-id normalization for this slice

## Validation Results

- passed focused baseline gate:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~Post7BScoringBaselineTests`
- passed required full validation:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - `cd vscode-extension && npm test`
  - `cd vscode-extension && npm run compile`
- note:
  - `dotnet test` still emits the existing nullable warning in `service-dotnet/tests/DesignStudio/ConceptStudioBoundaryTests.cs`; no new baseline-related warnings were introduced

## Outcome

- post-7B scoring regression baseline captured within scope
- no scorer behavior changes introduced
- no Workstream 7C implementation introduced

## Next Recommended Step

- use `Post7BScoringBaselineTests` as the first regression gate for any future Workstream 7C or 7D scorer slice
