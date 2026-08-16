# 2026-06-16 Report Design Studio Docs Shell-Alignment Correction

## Goal

Correct the new Report Design Studio user docs so they match the actual shipped shell instead of the underlying stage foundations.

## Problem

- The first-pass docs described Design Brief as if a consultant could type fields such as Audience and Business Objective directly in the shipped shell.
- The actual shell shown in the screenshot is primarily read-only in the early stages and does not expose save, submit, or approval controls for Design Brief, Concept Studio, or Draft Studio.

## Files Updated

- `docs/report-design-studio-user-guide.md`
- `docs/report-design-studio-workflow-walkthrough.md`

## Correction Made

- Reframed the shipped MVP as:
  - a workflow shell
  - a review surface
  - an approval-teaching surface
- Explicitly documented that the current shell does **not** expose:
  - writable Design Brief fields
  - Design Brief start/save/submit/approve controls
  - Concept generation or baseline approval controls
  - Draft generation or draft approval controls
- Explicitly documented the live controls that do exist:
  - Review Design handoff
  - Refinement proposal approve/reject/defer
  - Compare Iterations selectors

## Validation

- Re-read both docs after the patch.
- Confirmed the new wording explicitly matches the screenshoted UI state.

## Next Recommended Step

- Keep Design Studio docs anchored to shipped shell behavior.
- Only reintroduce field-by-field “how to complete” instructions if the main shell later exposes those authoring controls directly.
