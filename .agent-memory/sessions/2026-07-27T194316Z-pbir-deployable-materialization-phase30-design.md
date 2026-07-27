# Repository Phase 30 — Safe Local Deployable PBIR Materialization

## Session Status

- Active.
- Repository Phase 30 maps to original roadmap Phase 4B.
- Design and implementation plan are proposed and await explicit user approval.
- No Phase 30 production implementation has begun.

## Starting Evidence

- Phase 29 is complete in the shared dirty worktree and remains uncommitted.
- Latest recorded validation:
  - focused backend 54/54;
  - full backend 617/617;
  - 105 Jest suites and 527 Jest tests;
  - TypeScript compilation passed.
- Existing Phase 29, memory, session, and unrelated Rayfin research edits were preserved.
- The preview writer remains preview-only and is not suitable for reuse or widening.

## Proposed Boundary

- Consume only validated pbir-deployable-artifact/v1 and pbir-deployable-manifest/v1.
- Add a separate backend-only materializer.
- Preview is read-only.
- Apply writes the complete artifact to staging, verifies exact bytes, and promotes a dedicated target directory by same-filesystem move.
- Arbitrary nonempty targets fail closed; only absent, empty, exact-match, or valid Phase 30-managed targets are supported.
- Persist receipts, journals, backups, and rollback quarantine outside the deployable target.
- Require a hashed ownership marker before using an existing private control directory.
- Record pre-mutation failures as terminal aborted transactions only after proving target and receipt state remained unchanged.
- Roll back or recover only the current transaction and never invent missing state.
- Preserve all provider, Skills, API, CLI, deployment, Desktop, Analyzer, semantic-model, PBIP, legacy PBIR, refinement-loop, Fabric App, Fabric Data App, and UI exclusions.

## Proposed Documents

- `docs/superpowers/specs/2026-07-27-safe-local-deployable-pbir-materialization-phase30-design.md`
- `docs/superpowers/plans/2026-07-27-safe-local-deployable-pbir-materialization-phase30-plan.md`

## Next Step

- Request explicit approval of both the Phase 30 boundary and implementation plan.
- Do not begin production implementation before approval.

## Document-Only Validation

- `git diff --check` passed.
- Both JSON examples parse.
- All 14 versioned contract names match between the design and plan.
- All 12 journal phases match between the design and plan.
- Control-root ownership and terminal pre-mutation abort semantics are explicit.
- Placeholder and superseded-name scan passed.
- Approval-state, roadmap-mapping, preview-writer separation, external-authority, and production-file absence checks passed.
- `.agent-memory/current-focus.md` contains one Active Session heading.
