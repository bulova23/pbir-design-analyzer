# Post-7B Scoring Regression Baseline

Date: 2026-06-15

Status: Validation-only baseline capture before Workstream 7C

## Purpose

This baseline closes the remaining validation gap after Workstream 7B.

It does not change scorer behavior, extension behavior, architecture, or public contracts.

It captures a stable representative regression view that future Workstream 7C and 7D slices can compare against.

## Representative Reports

The baseline uses the same real-report corpus already exercised in prior validation:

- Sales & Production
- Sales Analysis
- Running Record Dataverse
- Sales AWF

Why this corpus:

- normal scoring and report-level recommendations are exercised across all four reports
- Story Assessment and Guided Story Improvements are exercised across the mixed page types
- Cross-Page Narrative is exercised on all four reports through the official validation export
- special-page handling is exercised by legal, tooltip, Q&A, validation, notes, and duplicate/supporting pages
- cross-page consistency and fragmented-report patterns are represented

## Authoritative Paths Used

Two existing backend paths remain authoritative:

1. `PbirScoringService.ScoreAsync(reportPath)`
2. `StoryAssessmentValidationExportService.CreateReportAsync(reportPath)`

The checked-in baseline is a compact normalized projection built from those two outputs.

This keeps the baseline tied to the real scorer and the official validation export without introducing a new product-facing contract.

## Baseline Files

Checked-in baselines live at:

- `service-dotnet/tests/Baselines/Post7BScoring/sales-and-production.baseline.json`
- `service-dotnet/tests/Baselines/Post7BScoring/sales-analysis.baseline.json`
- `service-dotnet/tests/Baselines/Post7BScoring/running-record-dataverse.baseline.json`
- `service-dotnet/tests/Baselines/Post7BScoring/sales-awf.baseline.json`

Regression coverage lives at:

- `service-dotnet/tests/Post7BScoringBaselineTests.cs`

## Projection Contents

Each baseline file preserves the fields that should remain stable across Workstream 7C and 7D unless an intentional scoring change is introduced and documented:

- top-level score summary
  - page count
  - composite score
  - framework scores
  - visual counts
- top-level recommendations
- top-level Guided Story Improvements ids and rationale
- report-consistency summary
  - issue count
  - overall finding
  - affected pages
  - issue categories, severities, confidence, affected pages
- per-page public scoring summary
  - page name
  - page composite score
  - detected story
  - story archetype
  - guided-improvement ids
  - recommendation count
  - benchmark archetype
  - actionability score
- validation-export Story Assessment projection
  - detected story
  - special-page result
  - archetype classification and suppression status
  - semantic coherence result
  - competing-story status
  - filter-topology result
  - story-gap ids
  - future-contract-candidate gap ids
  - confidence dimension ratings
  - promotion states
  - surface scopes
- validation-export Cross-Page Narrative projection
  - dominant report objective
  - main narrative path
  - page roles
  - orphan decisions
  - dimension scores
  - report-level gap stable ids

## Normalization Rules

The baseline intentionally removes runtime-only instability instead of storing raw live output.

Normalization rules:

- do not persist absolute filesystem paths
- do not persist `reportPath`
- do not persist `scoredAt`
- do not persist `generatedAtUtc`
- identify the source report by fixture file name only:
  - `Sales & Production.pbip`
  - `Sales Analysis.pbip`
  - `Running Record Dataverse.pbip`
  - `Sales AWF.pbip`
- persist only the compact projection fields listed above

No transient ids required additional normalization in this baseline slice.

## Command Used

Baseline capture was produced from the current scorer and official validation export using the local real-fixture corpus.

Representative generation loop:

```bash
dotnet run --project service-dotnet/tools/StoryAssessmentValidationExport -- "/Users/bcrowell/Documents/GitHub/PBITesting/Sales & Production.pbip"
dotnet run --project service-dotnet/tools/StoryAssessmentValidationExport -- "/Users/bcrowell/Documents/GitHub/PBITest2/Sales Analysis.pbip"
dotnet run --project service-dotnet/tools/StoryAssessmentValidationExport -- "/Users/bcrowell/Documents/GitHub/PBITest3/Running Record Dataverse.pbip"
dotnet run --project service-dotnet/tools/StoryAssessmentValidationExport -- "/Users/bcrowell/Documents/GitHub/PBITest4/Sales AWF.pbip"
```

The checked-in JSON baselines were then created from the same report paths by projecting:

- `PbirScoringService.ScoreAsync(reportPath)`
- `StoryAssessmentValidationExportService.CreateReportAsync(reportPath)`

into the compact normalized baseline shape now enforced by `Post7BScoringBaselineTests`.

## Future Workstream 7C And 7D Comparison Instructions

Before changing Workstream 7C or 7D scoring behavior:

1. Run `dotnet test service-dotnet/tests/Tests.csproj -c Release --filter FullyQualifiedName~Post7BScoringBaselineTests`
2. Review any baseline diff by report, then by:
   - top-level score summary
   - top-level recommendations
   - top-level Guided Story Improvements ids
   - per-page public story/actionability drift
   - special-page handling drift
   - Cross-Page Narrative path, roles, dimensions, and report-gap drift
3. Treat any diff in those fields as a scoring behavior change, not a refactor-only change
4. If the change is intentional, update:
   - the baseline files
   - this document
   - the related workstream session note
   - the regression rationale in code review or release notes

## Expected Stability Contract For 7C And 7D

Unless intentionally changed and documented, these should remain stable:

- score totals and framework scores for the representative corpus
- recommendation lists at the report level
- Guided Story Improvements ids and rationale ordering
- page names and page ordering
- page-level story classifications and guided-improvement ids
- special-page suppression outcomes
- Cross-Page Narrative dominant objective, page roles, path, dimensions, orphan decisions, and report-level gap ids
- report-consistency issue grouping and affected-page sets

If a future slice changes one of those fields unintentionally, treat it as a regression.
