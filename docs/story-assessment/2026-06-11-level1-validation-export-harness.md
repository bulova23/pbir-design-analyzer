# Story Assessment 2.0 Level 1 Validation Export Harness

Status: Internal-only validation tooling

## Purpose

This harness makes backend-internal Story Assessment 2.0 outputs reviewable for Level 1 expert validation.

It is not part of the VS Code score-panel contract and must not be treated as a user-facing product surface.

## Internal Validation Label

**Internal Validation Export**

**Not User-Facing Contract**

## Command

Run the harness with:

```bash
dotnet run --project service-dotnet/tools/StoryAssessmentValidationExport -- <reportPath> [outputDir]
```

Examples:

```bash
dotnet run --project service-dotnet/tools/StoryAssessmentValidationExport -- /path/to/MyReport.Report
```

```bash
dotnet run --project service-dotnet/tools/StoryAssessmentValidationExport -- /path/to/MyPbipRoot /path/to/output
```

## Inputs

- `reportPath`
  - required
  - PBIP project root or `.Report` folder path accepted by backend scoring
- `outputDir`
  - optional
  - if omitted, the harness writes to a sibling folder named `story-assessment-validation-export`

## Outputs

The harness writes:

- `story-assessment-validation.json`
- `story-assessment-validation.md`

Both outputs are labeled:

- `Internal Validation Export`
- `Not User-Facing Contract`

## Per-Page Export Contents

Each page includes:

- page name
- detected story from current public logic
- internal signal registry summary
- internal archetype classification
- internal semantic coherence result
- internal competing-story status
- internal filter topology result
- internal story gaps
- internal confidence breakdown
- promotion states
- surface-scope classifications

## Guardrails

- no VS Code UI dependency
- no score-panel payload exposure
- no public `ScoreResult` field changes
- no public `PageScore` field changes
- no `RpcHost` changes

## Intended Use

Use this harness to prepare Level 1 expert-review artifacts for:

- reviewer walkthroughs
- machine comparison of internal Story Assessment outputs
- markdown-based narrative review packets

Do not use these files as:

- product contract definitions
- extension payloads
- end-user documentation
