# Session Note

Date: 2026-06-11

## Objective

Create the design specification and implementation plan for:

- Story Assessment 2.1 – Guided Story Improvements

using the Level 1 promotion decision report and validation findings as the authoritative inputs.

## Work Completed

- Wrote the design spec:
  - `docs/superpowers/specs/2026-06-11-guided-story-improvements-design.md`
- Wrote the implementation plan:
  - `docs/superpowers/plans/2026-06-11-guided-story-improvements-plan.md`

## Design Decision

- Used Option 3:
  - a small new `Guided Story Improvements` subsection
- Positioned it as:
  - `Story Assessment`
  - `Guided Story Improvements`
  - `Issues`
  - `Fix Plan`
- Kept the first user-facing slice constrained to the six validated Story Gap categories:
  - Missing Title / Question Anchor
  - Missing Benchmark / Target
  - Missing Prior-Period Context
  - Missing Primary Metric
  - Missing Primary Dimension
  - Scattered Filters

## Guardrails Preserved

- No code was implemented.
- No extension files were modified.
- No score-panel contract changes were made.
- No UI changes were made.
- Archetypes, coherence, confidence breakdown, competing stories, signal registry, promotion states, and surface scopes remain internal-only in the design.

## Planning Outcome

- Guided Story Improvements is defined as the source of truth for validated Story Assessment recommendations.
- Issues and Fix Plan are explicitly downstream consumers, not replacements.
- Special-page handling remains a hidden guardrail only.

