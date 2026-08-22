# Post-v1.0 Architecture Decomposition Design — 2026-08-21

## Scope

Architecture investigation, target design, controls, and implementation planning only. No production decomposition is authorized in this session.

## Baseline

- Authoritative release state: `origin/main` / tag `v1.0.0`, commit `4c56eaf37f4829640051ec121d9f6f5103aa7084`.
- v1.0 consolidation/stabilization is complete.
- Existing dirty worktree changes predate this session and are preserved.

## Initial evidence

- `PbirScoringService.cs`: 9,997 lines; combines report/page orchestration, ten scoring dimensions, visual metadata/story inference, bookmark-aware scoring, findings/feedback construction, and result assembly while delegating some story/cross-page/assembly work.
- `App.tsx`: 4,573 lines; contains pure projections/formatters, feature renderers, workspace state, host message consumption, and root composition.
- `PbirScorePanel.ts`: 920 lines; already delegates audit/export/fix workflows, but still combines VS Code lifecycle, surface selection, scoring invocation, normalized result/presentation assembly, persistence, diagnostics, protocol callback wiring, and message error containment.
- Existing controls include architecture tests, generated score-panel schema/freshness checks, protocol validation, selected-page clamping, representative scoring goldens, deterministic repeat, packaged workflow acceptance, and mutation/rollback acceptance.

## Next step

Complete responsibility/dependency maps and write the post-v1.0 design and implementation plan. Validate the artifacts and close the session without changing production behavior.
