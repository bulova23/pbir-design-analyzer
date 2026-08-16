# Report Discovery Wizard Phase 3 Recommendation Engine

Date: 2026-06-19

## Scope Implemented

- Phase 3 only from the 2026-06-18 Report Discovery Wizard spec and implementation plan
- internal Recommendation Engine layer only
- no Experience Blueprint generation
- no Design Studio seeding
- no provider-backed generation
- no Microsoft Skills integration
- no public score payload or page-score contract expansion

## Internal Layer Added

- `service-dotnet/Services/Discovery/Models/RecommendationModels.cs`
- `service-dotnet/Services/Discovery/RecommendationEngineService.cs`

The new layer sits strictly between:

- Discovery Profile
- Opportunity Catalog

and any future downstream blueprint or seeding work.

It remains:

- internal
- advisory-only
- provider-neutral

## Weighting Strategy

The recommendation engine uses a weighted score with seven dimensions:

- semantic coverage: 0.22
- business actionability: 0.18
- analytical fit: 0.16
- audience clarity: 0.12
- opportunity completeness: 0.12
- implementation complexity inverse: 0.08
- model confidence: 0.12

Rationale:

- semantic coverage is weighted highest so recommendations stay anchored in real model support
- business actionability and analytical fit are next so the output behaves like consultant curation rather than catalog search
- audience clarity and completeness ensure the recommendation is explainable and defensible
- complexity is a tie-breaker input rather than the dominant decision maker
- model confidence suppresses overstatement on sparse or ambiguous models

## Selection Heuristics

- near-duplicate opportunities collapse after scoring when they share:
  - materially similar audience
  - materially similar outcome or name token set
  - the same recommended experience type
- primary recommendations are selected greedily with diversity bonuses for:
  - new experience type
  - new audience
  - meaningfully different business outcome
- alternate recommendations use stronger diversity pressure so they behave like credible secondary paths instead of weaker clones
- the engine hard-caps output at:
  - 3 primary recommendations
  - 2 alternate recommendations
  - 5 total recommendations

## Explanation Strategy

Recommendation explanations are generated from structured semantic signals rather than free-form text alone:

- domain support
- date intelligence readiness
- KPI clusters
- dimension support
- audience cues
- measure and drill cues

Each recommendation includes:

- consultant-style why text
- supporting signals
- limiting factors
- confidence note
- complexity note

This keeps the explanation payload grounded and testable before any future UI or blueprint layer consumes it.
