# Session Note

Date: 2026-06-11

## Objective

Improve Story Assessment 2.0 backend accuracy without changing any public contract, UI, or score-panel payload by:

- adding conservative internal special-page recognition
- reducing generic archetype overclaiming on special pages
- making coherence scoring more discriminative
- filtering Story Gaps toward higher-value candidates
- extending the internal validation export for re-review

## Work Completed

- Added an internal `StorySpecialPageAssessment` pipeline between signal extraction and downstream Story Assessment stages.
- Implemented deterministic conservative detection for:
  - `Tooltip`
  - `Qna`
  - `WhatIf`
  - `KeyInfluencers`
  - `MarketBasket`
  - `ReferenceLegal`
  - `ValidationSandbox`
- Added special-page control flags for:
  - primary-narrative treatment
  - normal-gap suppression
  - generic-archetype-promotion suppression
- Added archetype guardrails so special pages are downgraded or treated as secondary instead of overclaiming generic `Comparison` or `PerformanceMonitor`.
- Tuned semantic coherence with:
  - page-title weighting
  - primary-visual weighting
  - KPI and field weighting
  - narrow analytics-term normalization
  - special-page diagnostic scoring mode
- Filtered internal Story Gaps to retain higher-value actionable candidates and suppress normal analytical gaps for reference/legal and validation/sandbox pages.
- Updated the internal validation export to include:
  - special page result
  - archetype suppression or downgrade status
  - coherence tuning details
  - per-gap future contract candidate flags

## Re-Review Outcome

- Reran the validation export on the same Level 1 corpus:
  - `Sales Analysis`
  - `Sales & Production`
  - duplicate `Sales & Production` copy
- Special-page false positives were materially reduced:
  - `Legal` is now `ReferenceLegal` and no longer emits normal analytical gaps
  - `Validation Page` and its duplicate are now `ValidationSandbox` and no longer emit normal analytical gaps
  - tooltip pages are now detected as `Tooltip` and gap volume dropped sharply
  - `Q&A1`, `Q&A2`, `WhatIf`, `KeyInfluencers`, and `Market Basket Analysis` now receive explicit special-page handling instead of generic classification
- Coherence is more discriminative for special pages:
  - special pages now enter `DiagnosticSpecialPage` mode instead of looking like failed normal pages
  - `Market Basket Analysis` and `Legal` now surface split concept results rather than collapsing to the same sparse baseline
- Gap usefulness improved:
  - average gaps on `Sales Analysis` dropped from `7.33` to `5.25`
  - average gaps on `Sales & Production` dropped from `7.76` to `4.52`
- Determinism held:
  - the duplicate `Sales & Production` copy produced an identical page array in the export

## Remaining False Positives

- `Customer Analysis` still overclaims a normal performance archetype.
- `RetKeyInf` remains unmatched because the compact label variant does not yet map to `KeyInfluencers`.
- Several special pages still keep the existing public detected-story wording because this slice only guards internal accuracy and does not change public story logic.

## Files Changed

- `service-dotnet/Services/Pbir/Models/StoryAssessmentValidationModels.cs`
- `service-dotnet/Services/Pbir/Models/ScoreResult.cs`
- `service-dotnet/Services/Pbir/Models/PageScore.cs`
- `service-dotnet/Services/Pbir/PbirScoringService.cs`
- `service-dotnet/tools/StoryAssessmentValidationExport/StoryAssessmentValidationExportModels.cs`
- `service-dotnet/tools/StoryAssessmentValidationExport/StoryAssessmentValidationExportService.cs`
- `service-dotnet/tools/StoryAssessmentValidationExport/StoryAssessmentValidationMarkdownRenderer.cs`
- `service-dotnet/tests/Services/PbirScoringServiceTests.cs`
- `service-dotnet/tests/StoryAssessmentValidationExportTests.cs`
- `.agent-memory/current-focus.md`
- `.agent-memory/session-summaries.md`

## Validation

- Required backend suite:
  - `dotnet test service-dotnet/tests/Tests.csproj -c Release`
  - result: `207` passed, `0` failed
- Re-review exports:
  - `/tmp/story-assessment-level1-rerun-20260611-163234b/pbitest2-sales-analysis`
  - `/tmp/story-assessment-level1-rerun-20260611-163234b/pbitesting-sales-production`
  - `/tmp/story-assessment-level1-rerun-20260611-163234b/mcp-docs-sales-production`

## Boundaries Preserved

- No VS Code UI files were changed.
- No score-panel payload fields were added.
- No extension payload files were changed.
- No public `ScoreResult` or `PageScore` fields were added.
- All new Story Assessment diagnostics remain backend-internal or internal-export only.
