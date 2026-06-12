# 2026-06-12 Cross-Page Narrative Consistency Design And Plan

## Goal

Create the Cross-Page Narrative Consistency design specification and implementation plan as a planning-only Story Assessment 3.0 effort with no code implementation, no Story Assessment 2.2 redesign, and updated repo memory.

## Work Completed

- Reviewed:
  - `AGENTS.md`
  - `.agent-memory/current-focus.md`
  - `.agent-memory/repo-map.md`
  - `.agent-memory/do-not-do-this.md`
  - `.agent-memory/failure-patterns.md`
  - `docs/ROADMAP.md`
  - existing Story Assessment 2.0, 2.1, and 2.2 specs and plans
  - deferred roadmap epic specs
- Mapped the current Story Assessment, score-panel, and report-consistency seams to keep the design aligned with:
  - scoring authority
  - normalized findings
  - analyzable surface boundaries
  - internal-only Story Assessment promotion posture
- Wrote:
  - `docs/superpowers/specs/2026-06-12-cross-page-narrative-consistency-design.md`
  - `docs/superpowers/plans/2026-06-12-cross-page-narrative-consistency-plan.md`

## Key Design Decisions

- Cross-Page Narrative is a report-level internal Story Assessment layer, not a UI feature and not a second page-scoring system.
- The first implementation slice stays internal-only and validation-first.
- Existing page-level Story Assessment outputs are required inputs; duplicate page-level logic is explicitly disallowed.
- Page roles, narrative graphing, orphan detection, and report-level scoring are modeled as surface-neutral concepts with PBIR-first adapters.
- The likely future promotion candidate is report-level story gaps, not the full role taxonomy or composite score.

## Validation

- No build or test commands were run because this session was design and planning only.
- Validation in scope for this session was architectural consistency review against existing specs, contracts, and repo constraints.

## Risks / Follow-Up

- Page-role taxonomy will need careful Level 1 reviewer calibration to avoid unstable distinctions between adjacent roles.
- Existing report consistency logic must remain separate enough to avoid model overlap while still allowing supporting evidence reuse.
- Promotion pressure should be resisted until role accuracy, orphan precision, and low-confidence downgrade behavior are proven on a report corpus.

## Recommended Next Step

Use the new implementation plan as the execution baseline for an internal-only backend slice plus validation export expansion, then run Level 1 corpus review before any promotion discussion.
